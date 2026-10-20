using ChatService.Application.Abstractions;
using ChatService.Application.Commands;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Exceptions;
using ChatService.Domain.Repositories;
using ChatService.Domain.ValueObjects;
using MediatR;

namespace ChatService.Application.Handlers;

public class UpdateConversationMetadataHandler : IRequestHandler<UpdateConversationMetadataCommand>
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateConversationMetadataHandler(
        IConversationRepository conversationRepository,
        IUnitOfWork unitOfWork)
    {
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateConversationMetadataCommand request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationRepository.GetByIdAsync(
            ConversationId.From(request.ConversationId), cancellationToken);
        if (conversation == null)
            throw new NotFoundException(nameof(Conversation), request.ConversationId);

        ConversationAuthorization.EnsureCanRead(conversation, request.ActorId, request.ActorType);
        conversation.UpdateMetadata(
            request.ActorId,
            request.ActorType,
            request.Title,
            request.Description,
            request.AvatarUrl);

        await _conversationRepository.UpdateAsync(conversation, cancellationToken);
        await _unitOfWork.SaveChangesWithDomainEventsAsync(cancellationToken);
    }
}
