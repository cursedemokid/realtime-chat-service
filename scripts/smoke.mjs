// Node.js 22+; no npm packages. Exercises the real API, WebSocket hub and database.
import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';

const base = process.env.CHAT_URL ?? 'http://127.0.0.1:8030';
const sockets = [];

async function api(path, { token, body, method = body === undefined ? 'GET' : 'POST', expected = 200 } = {}) {
  const headers = token ? { Authorization: `Bearer ${token}` } : {};
  if (body !== undefined && !(body instanceof FormData)) headers['Content-Type'] = 'application/json';
  const response = await fetch(base + path, { method, headers,
    body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body) });
  const text = await response.text();
  assert.equal(response.status, expected, `${method} ${path}: ${response.status}: ${text.slice(0, 500)}`);
  return text ? JSON.parse(text) : null;
}

async function waitUntil(condition, description, timeout = 15000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    if (await condition()) return;
    await delay(100);
  }
  throw new Error(`Timed out: ${description}`);
}

async function token(userId, role, tenantId = 'demo') {
  return (await api('/api/dev/token', { body: { userId, role, tenantId } })).token;
}

async function connect(jwt, room) {
  const socket = new WebSocket(base.replace(/^http/, 'ws') + '/chatHub?access_token=' + encodeURIComponent(jwt));
  sockets.push(socket);
  const events = [], completions = new Map();
  let connected = false, counter = 0;
  socket.addEventListener('open', () => socket.send(JSON.stringify({ protocol: 'json', version: 1 }) + '\x1e'));
  socket.addEventListener('message', event => {
    for (const part of String(event.data).split('\x1e').filter(Boolean)) {
      const message = JSON.parse(part);
      if (message.error && !connected) throw new Error(message.error);
      if (!connected) { connected = true; continue; }
      if (message.type === 1) events.push(message);
      if (message.type === 3) completions.set(message.invocationId, message);
    }
  });
  await waitUntil(() => connected, 'SignalR handshake');
  const invoke = async (target, ...args) => {
    const id = String(++counter);
    socket.send(JSON.stringify({ type: 1, invocationId: id, target, arguments: args }) + '\x1e');
    await waitUntil(() => completions.has(id), target);
    const response = completions.get(id);
    assert.equal(response.error, undefined, `${target}: ${response.error}`);
    return response.result;
  };
  await invoke('JoinConversation', room);
  return { events, invoke };
}

try {
  await waitUntil(async () => {
    try { return (await fetch(base + '/health')).ok; } catch { return false; }
  }, 'chat readiness', 180000);
  const { conversationId: room } = await api('/api/demo/bootstrap', { method: 'POST' });
  const [alex, sam, visitor, otherTenant] = await Promise.all([
    token(1, 'customer'), token(2, 'executor'), token(3, 'customer'), token(1, 'customer', 'other')
  ]);
  await api(`/api/Chat/${room}/messages`, { expected: 401 });
  await api(`/api/Chat/${room}/messages`, { token: visitor, expected: 403 });
  await api(`/api/Chat/${room}/messages`, { token: otherTenant, expected: 403 });
  console.log('PASS: JWT required; non-members and a different tenant cannot read the conversation');

  const [first, second] = await Promise.all([connect(alex, room), connect(sam, room)]);
  const text = 'Smoke ' + crypto.randomUUID();
  await first.invoke('SendMessage', { conversationId: room, text });
  await waitUntil(() => [first, second].every(client => client.events.some(event =>
    event.target === 'MessageReceived' && event.arguments[0].textMessage === text)), 'realtime message delivery');
  const message = second.events.find(event => event.target === 'MessageReceived' && event.arguments[0].textMessage === text).arguments[0];
  await api(`/api/Chat/${room}/read`, { token: sam, method: 'POST', expected: 204 });
  await waitUntil(() => first.events.some(event => event.target === 'MessageRead'), 'read receipt');
  await second.invoke('StartTyping', room);
  await waitUntil(() => first.events.some(event => event.target === 'UserTyping'), 'typing notification');
  const edited = text + ' edited';
  await api(`/api/Chat/${room}/messages/${message.id}`, { token: alex, method: 'PUT', body: { text: edited } });
  await waitUntil(() => second.events.some(event => event.target === 'MessageEdited' &&
    event.arguments[0].textMessage === edited), 'message edit');
  const search = await api(`/api/Chat/${room}/messages/search?q=${encodeURIComponent(text)}`, { token: sam });
  assert.ok(JSON.stringify(search).includes(edited));
  await api(`/api/Chat/${room}/manage`, { token: alex, body: { action: 'Pin' }, expected: 204 });
  await api(`/api/Chat/${room}/manage`, { token: alex, body: { action: 'Unpin' }, expected: 204 });
  console.log('PASS: real SignalR delivery, read receipts, typing, editing, search and pinning');

  const data = new FormData();
  data.append('file', new Blob(['portfolio attachment'], { type: 'text/plain' }), 'demo.txt');
  const file = await api('/api/Chat/upload', { token: alex, body: data, expected: 201 });
  await api(`/api/files/${file.fileId}/download`, { token: visitor, expected: 403 });
  await api(`/api/files/${file.fileId}/download`, { token: sam, expected: 403 });
  await api(`/api/Chat/${room}/messages`, { token: alex, body: { text: 'Attachment', attachments: [{
    id: file.fileId, name: 'untrusted-name', size: 0, mimeType: 'text/html',
    attachmentType: 'Document', url: 'https://invalid.example/untrusted'
  }] } });
  const download = await fetch(base + `/api/files/${file.fileId}/download`, { headers: { Authorization: 'Bearer ' + sam } });
  assert.equal(download.status, 200);
  assert.equal(await download.text(), 'portfolio attachment');
  assert.ok(download.headers.get('content-disposition').includes('demo.txt'));
  await api(`/api/files/${file.fileId}/download`, { token: visitor, expected: 403 });
  await api(`/api/files/${file.fileId}/download`, { token: otherTenant, expected: 403 });
  const history = await api(`/api/Chat/${room}/messages`, { token: sam });
  assert.ok(JSON.stringify(history).includes(file.fileId));
  console.log('PASS: private uploads, participant downloads, trusted metadata and reloaded attachment history');
} finally {
  for (const socket of sockets) socket.close();
}
