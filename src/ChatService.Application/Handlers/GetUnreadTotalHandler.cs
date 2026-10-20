using ChatService.Application.Queries;
using ChatService.Domain.Repositories;
using MediatR;

namespace ChatService.Application.Handlers;

public class GetUnreadTotalHandler : IRequestHandler<GetUnreadTotalQuery, int>
{
    private readonly IMessageRepository _messageRepository;

    public GetUnreadTotalHandler(IMessageRepository messageRepository)
    {
        _messageRepository = messageRepository;
    }

    public Task<int> Handle(GetUnreadTotalQuery request, CancellationToken cancellationToken) =>
        _messageRepository.CountUnreadTotalAsync(request.ViewerId, cancellationToken);
}
