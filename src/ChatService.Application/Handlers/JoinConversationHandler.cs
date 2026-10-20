using ChatService.Application.Commands;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class JoinConversationHandler : IRequestHandler<JoinConversationCommand>
{
    private readonly IConversationRepository _conversationRepository;

    public JoinConversationHandler(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    public async Task Handle(JoinConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation == null) throw new NotFoundException(nameof(conversation), request.ConversationId);

        if (!conversation.IsParticipant(request.ParticipantId, request.ParticipantType))
            throw new DomainException("Participant is not a member of conversation");
    }
}
