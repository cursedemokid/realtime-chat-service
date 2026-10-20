using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;
using static ChatService.Application.ConversationAuthorization;

namespace ChatService.Application.Handlers;

public class UpdateConversationStatusHandler : IRequestHandler<UpdateConversationStatusCommand>
{
    private readonly IConversationRepository _conversationRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateConversationStatusHandler(IConversationRepository conversationRepo, IUnitOfWork unitOfWork)
    {
        _conversationRepo = conversationRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateConversationStatusCommand cmd, CancellationToken ct)
    {
        var conversation = await _conversationRepo.GetByIdAsync(ConversationId.From(cmd.ConversationId), ct);
        if (conversation == null) throw new NotFoundException(nameof(Conversation), cmd.ConversationId);

        EnsureCanRead(conversation, cmd.ActorId, cmd.ActorType);
        conversation.ChangeStatus(cmd.NewStatus);
        await _conversationRepo.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(ct);
    }
}
