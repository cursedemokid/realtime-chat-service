using System.Text.Json;
using ChatService.Application.Abstractions;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Services;

public interface IConversationAuditService
{
    Task AppendAsync(
        ConversationId conversationId,
        ConversationAuditAction action,
        ChatUserId? actorId,
        ParticipantType? actorType,
        ChatUserId? targetParticipantId = null,
        ParticipantType? targetParticipantType = null,
        object? payload = null,
        CancellationToken ct = default);
}

public class ConversationAuditService : IConversationAuditService
{
    private readonly IConversationAuditRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ConversationAuditService(IConversationAuditRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task AppendAsync(
        ConversationId conversationId,
        ConversationAuditAction action,
        ChatUserId? actorId,
        ParticipantType? actorType,
        ChatUserId? targetParticipantId = null,
        ParticipantType? targetParticipantType = null,
        object? payload = null,
        CancellationToken ct = default)
    {
        var json = payload == null ? null : JsonSerializer.Serialize(payload);
        var entry = new ConversationAuditEntry(
            conversationId,
            action,
            actorId,
            actorType,
            targetParticipantId,
            targetParticipantType,
            json);
        await _repository.AddAsync(entry, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
