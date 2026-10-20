using AutoMapper;
using ChatService.Application.Dto;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Entities;
using ChatService.Domain.ValueObjects;

namespace ChatService.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ConversationParticipant, ParticipantDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.UserId.UserId))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role))
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl))
            .ForMember(d => d.IsMuted, o => o.MapFrom(s => s.IsMuted))
            .ForMember(d => d.IsWriteBlocked, o => o.MapFrom(s => s.IsWriteBlocked))
            .ForMember(d => d.IsNotificationsMutedByViewer, o => o.Ignore())
            .ForMember(d => d.IsPinned, o => o.MapFrom(s => s.IsPinned))
            .ForMember(d => d.LeftAt, o => o.MapFrom(s => s.LeftAt))
            .ForMember(d => d.PresenceStatus, o => o.Ignore())
            .ForMember(d => d.LastSeenAt, o => o.Ignore());
        CreateMap<MessageAuthor, MessageAuthorDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.UserId))  
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role))
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl));
        CreateMap<Message, MessageDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value))
            .ForMember(d => d.ConversationId, o => o.MapFrom(s => s.ConversationId.Value))
            .ForMember(d => d.Author, o => o.MapFrom(s => s.Author))
            .ForMember(d => d.TextMessage, o => o.MapFrom(s => s.Content.Text))
            .ForMember(d => d.Attachments, o => o.MapFrom(s => s.Content.Attachments))
            .ForMember(d => d.CreationDate, o => o.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.EditedAt, o => o.MapFrom(s => s.EditedAt))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.ReadBy, o => o.Ignore())
            .ForMember(d => d.IsAuthor, o => o.Ignore())
            .ForMember(d => d.ReadStatus, o => o.Ignore());
        CreateMap<ReplyReference, ReplyToDto>()
            .ForMember(d => d.MessageId, opt => opt.MapFrom(src => src.ReferencedMessageId.Value));
        CreateMap<Attachment, MessageAttachmentDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
            .ForMember(d => d.Name, o => o.MapFrom(s => s.FileName))
            .ForMember(d => d.Size, o => o.MapFrom(s => s.Size))
            .ForMember(d => d.MimeType, o => o.MapFrom(s => s.MimeType))
            .ForMember(d => d.AttachmentType, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.Url, o => o.MapFrom(s => s.Url));
        CreateMap<MessageVersion, MessageVersionDto>()
            .ForMember(d => d.Attachments, opt => opt.MapFrom(src => src.Attachments));
        CreateMap<ConversationAuditEntry, ConversationAuditEntryDto>()
            .ForMember(d => d.ConversationId, o => o.MapFrom(s => s.ConversationId.Value))
            .ForMember(d => d.ActorId, o => o.MapFrom(s => s.ActorId == null ? (long?)null : s.ActorId.UserId))
            .ForMember(d => d.TargetParticipantId, o => o.MapFrom(s => s.TargetParticipantId == null ? (long?)null : s.TargetParticipantId.UserId));
        CreateMap<SupportTicket, SupportTicketDto>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.Id.Value))
            .ForMember(d => d.ConversationId, o => o.MapFrom(s => s.ConversationId != null ? s.ConversationId.Value : (Guid?)null))
            .ForMember(d => d.Initiator, o => o.MapFrom(s => s.Initiator))
            .ForMember(d => d.Theme, o => o.MapFrom(s => s.Theme))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
            .ForMember(d => d.AssignedManagerId, o => o.MapFrom(s => s.AssignedManagerId == null ? null : s.AssignedManagerId.ToString()))
            .ForMember(d => d.OpenedAt, o => o.MapFrom(s => s.OpenedAt))
            .ForMember(d => d.ClosedAt, o => o.MapFrom(s => s.ClosedAt))
            .ForMember(d => d.EscalationLevel, o => o.MapFrom(s => s.EscalationLevel))
            .ForMember(d => d.CloseReason, o => o.MapFrom(s => s.CloseReason));

        CreateMap<TicketParticipant, TicketParticipantDto>()
            .ForMember(d => d.ChatUserId, o => o.MapFrom(s => s.UserId.ToString()))
            .ForMember(d => d.Type, o => o.MapFrom(s => s.Type))
            .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl));
        CreateMap<UserPresence, PresenceDto>()
            .ForMember(d => d.TenantId, opt => opt.MapFrom(s => s.UserId.TenantId))
            .ForMember(d => d.ParticipantId, o => o.MapFrom(s => s.UserId.UserId))
            .ForMember(d => d.ParticipantType, o => o.Ignore())
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
            .ForMember(d => d.LastSeenAt, o => o.MapFrom(s => s.LastSeenAt))
            .ForMember(d => d.LastActivityAt, o => o.MapFrom(s => s.LastActivityAt));
    }
}
