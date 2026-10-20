using ChatService.Api.Authentication;
using ChatService.Application.Commands;
using ChatService.Application.Dto;
using ChatService.Application.Queries;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using ChatService.Domain.Repositories;
using ChatService.Domain.Exceptions;

namespace ChatService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISupportTicketRepository _supportTickets;
    private readonly IMapper _mapper;

    public ChatController(IMediator mediator, ISupportTicketRepository supportTickets, IMapper mapper)
    {
        _mediator = mediator;
        _supportTickets = supportTickets;
        _mapper = mapper;
    }

    private ChatUserId GetCurrentUserId()
    {
        if (!ChatPrincipal.TryResolve(User, out var userId, out _))
            throw new UnauthorizedAccessException("Unauthorized");
        return userId;
    }

    private ParticipantType GetCurrentUserType()
    {
        if (!ChatPrincipal.TryResolve(User, out _, out var participantType))
            throw new UnauthorizedAccessException("Unknown role");
        return participantType;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConversationsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationsPageDto>> GetConversations(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var query = new GetConversationsQuery(userId, userType, skip, take);
        var page = await _mediator.Send(query);
        return Ok(page);
    }


    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchConversationsResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchConversationsResultDto>> SearchConversations(
        [FromQuery] string q,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mediator.Send(new SearchConversationsQuery(userId, userType, q, skip, take));
        return Ok(result);
    }

    [HttpGet("messages/search")]
    [ProducesResponseType(typeof(SearchMessagesResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SearchMessagesResultDto>> SearchMessages(
        [FromQuery] string q,
        [FromQuery] Guid? conversationId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mediator.Send(new SearchMessagesQuery(userId, userType, q, conversationId, skip, take));
        return Ok(result);
    }

    [HttpGet("{conversationId:guid}/messages/search")]
    public async Task<ActionResult<SearchMessagesResultDto>> SearchConversationMessages(
        Guid conversationId, [FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new SearchMessagesQuery(
            GetCurrentUserId(), GetCurrentUserType(), q, conversationId, skip, Math.Clamp(limit, 1, 100)));
        return Ok(result);
    }

    [HttpPut("presence")]
    [ProducesResponseType(typeof(PresenceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PresenceDto>> UpdatePresence([FromBody] UpdatePresenceRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var presence = await _mediator.Send(new UpdatePresenceCommand(userId, userType, request.Status, request.ClearManualStatus));
        return Ok(presence);
    }

    [HttpPost("support-tickets")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SupportTicketDto>> CreateSupportTicket([FromBody] CreateSupportTicketRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var ticket = await _mediator.Send(new CreateSupportTicketCommand(
            userId,
            userType,
            request.Theme,
            request.AssignedManagerId,
            request.InitialMessage));
        return Ok(ticket);
    }

    [HttpGet("support-tickets/{ticketId:guid}")]
    public async Task<ActionResult<SupportTicketDto>> GetSupportTicket(Guid ticketId, CancellationToken ct)
    {
        var ticket = await _supportTickets.GetByIdAsync(SupportTicketId.From(ticketId), ct);
        if (ticket is null) return NotFound();
        var currentId = GetCurrentUserId();
        if (GetCurrentUserType() != ParticipantType.Manager && ticket.Initiator.UserId != currentId)
            return Forbid();
        return Ok(_mapper.Map<SupportTicketDto>(ticket));
    }

    [HttpGet("support-tickets")]
    public async Task<ActionResult> GetSupportTickets([FromQuery] int skip = 0, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var (items, total) = await _supportTickets.GetPageAsync(GetCurrentUserId(), GetCurrentUserType(), skip, limit, ct);
        return Ok(new { items = _mapper.Map<List<SupportTicketDto>>(items), total, nextCursor = skip + items.Count < total ? (skip + items.Count).ToString() : null, hasMore = skip + items.Count < total });
    }

    [HttpGet("{conversationId:guid}/participants")]
    [ProducesResponseType(typeof(ConversationParticipantsPageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConversationParticipantsPageDto>> GetParticipants(
        Guid conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] bool activeOnly = true)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var query = new GetConversationParticipantsQuery(conversationId, userId, userType, skip, take, activeOnly);
        var page = await _mediator.Send(query);
        return Ok(page);
    }

    [HttpPut("{conversationId:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MessageDto>> EditMessage(
        Guid conversationId,
        Guid messageId,
        [FromBody] EditMessageRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var message = await _mediator.Send(new EditMessageCommand(
            conversationId, messageId, userId, userType, request.Text, request.Attachments));
        return Ok(message);
    }

    [HttpDelete("{conversationId:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteMessage(Guid conversationId, Guid messageId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        await _mediator.Send(new DeleteMessageCommand(conversationId, messageId, userId, userType));
        return NoContent();
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetMessages(
        Guid conversationId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 100)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var messages = await _mediator.Send(new GetConversationMessagesQuery(conversationId, userId, userType, skip, take));
        return Ok(messages);
    }

    [HttpGet("{conversationId:guid}/messages/{messageId:guid}")]
    public async Task<ActionResult<MessageDto>> GetMessage(Guid conversationId, Guid messageId)
    {
        var messages = await _mediator.Send(new GetConversationMessagesQuery(conversationId, GetCurrentUserId(), GetCurrentUserType(), 0, 1000));
        var message = messages.SingleOrDefault(x => x.Id == messageId);
        return message is null ? NotFound() : Ok(message);
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ConversationSummaryDto>> GetConversation(Guid conversationId)
    {
        var summary = await _mediator.Send(new GetConversationSummaryQuery(conversationId, GetCurrentUserId(), GetCurrentUserType()));
        return Ok(summary);
    }

    [HttpGet("{conversationId:guid}/summary")]
    public async Task<ActionResult<ConversationSummaryDto>> GetSummary(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var summary = await _mediator.Send(new GetConversationSummaryQuery(conversationId, userId, userType));
        return Ok(summary);
    }

    [HttpGet("{conversationId:guid}/unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var summary = await _mediator.Send(new GetConversationSummaryQuery(conversationId, userId, userType));
        return Ok(summary.UnreadCount);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult> GetUnreadTotal()
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var total = await _mediator.Send(new GetUnreadTotalQuery(userId, userType));
        return Ok(new { totalUnreadCount = total });
    }

    [HttpPost("{conversationId:guid}/join")]
    public async Task<IActionResult> JoinConversation(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        await _mediator.Send(new JoinConversationCommand(conversationId, userId, userType));
        return NoContent();
    }

    [HttpPost("{conversationId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid conversationId, [FromBody] SendMessageRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var result = await _mediator.Send(new SendMessageCommand(
            conversationId, userId, userType, request.Text, request.Attachments, request.IsDateProposal));
        return Ok(result);
    }

    [HttpPost("{conversationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid conversationId)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        await _mediator.Send(new MarkMessagesReadCommand(conversationId, userId, userType));
        return NoContent();
    }

    [HttpPatch("{conversationId:guid}/status")]
    [HttpPut("{conversationId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid conversationId, [FromBody] UpdateStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        await _mediator.Send(new UpdateConversationStatusCommand(conversationId, request.Status, userId, userType));
        return NoContent();
    }

    [HttpPatch("{conversationId:guid}")]
    [HttpPost("{conversationId:guid}/manage")]
    public async Task<IActionResult> ManageConversation(Guid conversationId, [FromBody] ManageConversationRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        ChatUserId? targetUserId = null;
        if (request.TargetParticipantId.HasValue && request.TargetParticipantType.HasValue)
        {
            targetUserId = new ChatUserId(userId.TenantId, request.TargetParticipantId.Value);
        }

        await _mediator.Send(new ManageConversationCommand(
            conversationId,
            request.Action,
            userId,
            userType,
            targetUserId,
            request.TargetParticipantType,
            request.NewRole));
        return NoContent();
    }

    [HttpPatch("support-tickets/{ticketId:guid}")]
    [HttpPost("support-tickets/{ticketId:guid}/manage")]
    public async Task<ActionResult<SupportTicketDto>> ManageSupportTicket(Guid ticketId, [FromBody] ManageSupportTicketRequest request)
    {
        var userId = GetCurrentUserId();
        var userType = GetCurrentUserType();
        var ticket = await _mediator.Send(new ManageSupportTicketCommand(
            ticketId, request.Action, userId, userType, request.Reason));
        return Ok(ticket);
    }
}

public record SendMessageRequest(string? Text, List<MessageAttachmentDto>? Attachments, bool IsDateProposal = false);
public record EditMessageRequest(string Text, List<MessageAttachmentDto>? Attachments = null);
public record UpdateStatusRequest(ConversationStatus Status);
public record UpdatePresenceRequest(PresenceStatus? Status, bool ClearManualStatus = false);
public record CreateSupportTicketRequest(
    string Theme,
    long? AssignedManagerId = null,
    string? InitialMessage = null);
public record ManageConversationRequest(
    ConversationAction Action,
    long? TargetParticipantId = null,
    ParticipantType? TargetParticipantType = null,
    ConversationRole? NewRole = null);
public record ManageSupportTicketRequest(SupportTicketAction Action, string? Reason = null);
