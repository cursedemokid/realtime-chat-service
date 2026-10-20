const caseEl = id => document.getElementById(id);
let chatCatalog;
const caseDelay = ms => new Promise(resolve => setTimeout(resolve, ms));
function checkCase(condition, message) { if (!condition) throw Error(message); }
function logCase(message) { const item = document.createElement('li'); item.textContent = message; caseEl('case-log').append(item); }

async function caseRequest(path, token, body, expected = 200, method = body === undefined ? 'GET' : 'POST') {
  const headers = token ? { Authorization: 'Bearer ' + token } : {};
  if (body !== undefined && !(body instanceof FormData)) headers['Content-Type'] = 'application/json';
  const response = await fetch(path, { method, headers, body: body === undefined ? undefined : body instanceof FormData ? body : JSON.stringify(body) });
  checkCase(response.status === expected, method + ' ' + path + ': ожидался ' + expected + ', получен ' + response.status);
  logCase(method + ' ' + path + ' → ' + response.status);
  return response;
}

async function waitCase(predicate, description) {
  const deadline = Date.now() + 15000;
  while (Date.now() < deadline) { if (predicate()) return; await caseDelay(100); }
  throw Error('Не получено событие: ' + description);
}

function renderChatCase() {
  const test = chatCatalog.cases.find(item => item.id === caseEl('chat-case').value);
  caseEl('case-description').textContent = test.description + ' Ожидается: ' + test.expected + '.';
  caseEl('case-json').value = JSON.stringify({ caseId: test.id, expected: test.expected,
    tokenEndpoint: '/api/dev/token', tokenRequests: chatCatalog.tokenRequests,
    instructions: test.swaggerSteps, requests: test.requests }, null, 2);
}
caseEl('chat-case').onchange = renderChatCase;
caseEl('copy-case').onclick = async () => {
  try { await navigator.clipboard.writeText(caseEl('case-json').value); caseEl('case-result').textContent = 'Шаги скопированы.'; }
  catch { caseEl('case-json').select(); caseEl('case-result').textContent = 'JSON выделен — используйте «Копировать».'; }
};

caseEl('run-case').onclick = async () => {
  const id = caseEl('chat-case').value;
  caseEl('run-case').disabled = true; caseEl('chat-case').disabled = true;
  caseEl('case-log').replaceChildren(); caseEl('case-result').textContent = 'Выполняется…';
  try {
    const [alex, sam] = state;
    checkCase(alex?.ws.readyState === 1 && sam?.ws.readyState === 1, 'Оба участника должны быть подключены. Обновите страницу.');
    const path = '/api/Chat/' + room;
    const text = 'Кейс ' + id + ' · ' + crypto.randomUUID().slice(0, 8);
    const send = async (messageText, attachments) => (await caseRequest(path + '/messages', alex.token, { text: messageText, attachments })).json();
    if (id === 'CHAT-001') {
      const message = await send(text);
      await waitCase(() => state.every(user => user.events.some(event => event.target === 'MessageReceived' && event.arguments[0].id === message.id)), 'MessageReceived у двух участников');
      logCase('Оба WebSocket-клиента получили MessageReceived с ID ' + message.id);
    } else if (id === 'CHAT-002') {
      const message = await send(text);
      const edited = text + ' — исправлено';
      await caseRequest(path + '/messages/' + message.id, alex.token, { text: edited }, 200, 'PUT');
      await waitCase(() => sam.events.some(event => event.target === 'MessageEdited' && event.arguments[0].id === message.id), 'MessageEdited');
      const search = await (await caseRequest(path + '/messages/search?q=' + encodeURIComponent(text), sam.token)).json();
      checkCase(JSON.stringify(search).includes(edited), 'Поиск не вернул изменённый текст');
      logCase('Новый текст сохранён и найден от имени Sam');
    } else if (id === 'CHAT-003') {
      const message = await send(text);
      await caseRequest(path + '/read', sam.token, undefined, 204, 'POST');
      await waitCase(() => alex.events.some(event => event.target === 'MessageRead' && event.arguments[0].messageId === message.id), 'MessageRead');
      logCase('Alex получил подтверждение прочтения своего сообщения');
    } else if (id === 'CHAT-004') {
      await caseRequest(path + '/messages', null, undefined, 401);
    } else if (id === 'CHAT-005' || id === 'CHAT-006') {
      const input = id === 'CHAT-005' ? chatCatalog.tokenRequests.visitor : chatCatalog.tokenRequests.otherTenant;
      const token = await (await caseRequest('/api/dev/token', null, input)).json();
      await caseRequest(path + '/messages', token.token, undefined, 403);
    } else if (id === 'CHAT-007') {
      const data = new FormData(); data.append('file', new Blob([text], { type: 'text/plain' }), 'demo-case.txt');
      const file = await (await caseRequest('/api/Chat/upload', alex.token, data, 201)).json();
      await caseRequest('/api/files/' + file.fileId + '/download', sam.token, undefined, 403);
      await send(text, [{ id: file.fileId, name: file.fileName, size: file.size, mimeType: file.contentType, attachmentType: 'Document', url: file.url }]);
      const download = await caseRequest('/api/files/' + file.fileId + '/download', sam.token);
      checkCase(await download.text() === text, 'Содержимое файла не совпало');
      const visitor = await (await caseRequest('/api/dev/token', null, chatCatalog.tokenRequests.visitor)).json();
      await caseRequest('/api/files/' + file.fileId + '/download', visitor.token, undefined, 403);
      const history = await (await caseRequest(path + '/messages', sam.token)).json();
      checkCase(JSON.stringify(history).includes(file.fileId), 'Вложение не найдено в истории');
    } else if (id === 'CHAT-008') {
      await caseRequest(path + '/manage', alex.token, { action: 'Pin' }, 204);
      await caseRequest(path + '/manage', alex.token, { action: 'Unpin' }, 204);
    } else if (id === 'CHAT-009') {
      const topology = await (await caseRequest('/api/demo/rabbitmq/prepare', null, undefined, 200, 'POST')).json();
      logCase('Очередь: ' + topology.queue + ' · exchange: ' + topology.exchange);
      const message = await send(text);
      let found = false;
      for (let attempt = 0; attempt < 100; attempt++) {
        const result = await (await caseRequest('/api/demo/rabbitmq/consume', null, undefined, 200, 'POST')).json();
        if (result.hasMessage) {
          const payload = JSON.parse(result.body.Payload ?? result.body.payload);
          if (payload.messageId === message.id) { found = true; logCase('Получено notification.requested для сообщения ' + message.id); break; }
        }
        await caseDelay(100);
      }
      checkCase(found, 'Уведомление не поступило в RabbitMQ');
    }
    caseEl('case-result').textContent = 'PASS · ' + id + ' · ожидания совпали';
  } catch (error) { caseEl('case-result').textContent = 'FAIL · ' + error.message; }
  finally { caseEl('run-case').disabled = false; caseEl('chat-case').disabled = false; }
};

window.chatReady.then(async () => {
  const response = await fetch('/api/demo/cases');
  if (!response.ok) throw Error('Каталог кейсов недоступен');
  chatCatalog = await response.json();
  caseEl('chat-case').replaceChildren();
  for (const test of chatCatalog.cases) {
    const option = document.createElement('option'); option.value = test.id; option.textContent = test.id + ' · ' + test.title;
    caseEl('chat-case').append(option);
  }
  renderChatCase(); caseEl('run-case').disabled = false;
}).catch(error => { caseEl('case-result').textContent = error.message; });
