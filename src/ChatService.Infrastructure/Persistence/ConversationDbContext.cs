using ChatService.Domain.Aggregates;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Infrastructure.Persistence;

public class ConversationDbContext : DbContext
{
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<UserPresence> UserPresences { get; set; }
    public DbSet<MessageVersion> MessageVersions { get; set; }
    public DbSet<ConversationAuditEntry> ConversationAuditEntries { get; set; }
    public DbSet<ParticipantNotificationMute> ParticipantNotificationMutes { get; set; }

    public ConversationDbContext(DbContextOptions<ConversationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredFile>().ToTable("StoredFiles");
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).HasConversion(id => id.Value, v => ConversationId.From(v));
            entity.Property(c => c.Kind).HasConversion<int>();
            entity.Property(c => c.Status).HasConversion<int>();
            entity.Property(c => c.ContextReference).HasMaxLength(200);
            entity.Property(c => c.Title).HasMaxLength(200);
            entity.Property(c => c.Description).HasMaxLength(2000);
            entity.Property(c => c.AvatarUrl).HasMaxLength(500);
            entity.Property(c => c.CreatedAt);
            entity.Property(c => c.LastActivityAt);

            entity.OwnsMany(c => c.Participants, participantBuilder =>
            {
                participantBuilder.WithOwner().HasForeignKey("ConversationId");
                participantBuilder.ToTable("ConversationParticipants");

                participantBuilder.Property(p => p.UserId)
                    .HasConversion(
                        v => v.ToString(),
                        v => ChatUserId.FromString(v)
                    )
                    .HasColumnName("UserId")
                    .HasMaxLength(150); 

                participantBuilder.Property(p => p.Type).HasColumnName("ParticipantType").HasConversion<int>();
                participantBuilder.Property(p => p.Role).HasColumnName("Role").HasConversion<int>().HasDefaultValue(ConversationRole.Member);
                participantBuilder.Property(p => p.DisplayName).HasColumnName("DisplayName").HasMaxLength(200);
                participantBuilder.Property(p => p.AvatarUrl).HasColumnName("AvatarUrl").HasMaxLength(500);
                participantBuilder.Property(p => p.JoinedAt).HasColumnName("JoinedAt");
                participantBuilder.Property(p => p.LeftAt).HasColumnName("LeftAt");
                participantBuilder.Property(p => p.IsMuted).HasColumnName("IsMuted").HasDefaultValue(false);
                participantBuilder.Property(p => p.IsWriteBlocked).HasColumnName("IsWriteBlocked").HasDefaultValue(false);
                participantBuilder.Property(p => p.IsPinned).HasColumnName("IsPinned").HasDefaultValue(false);

                participantBuilder.HasKey("ConversationId", "UserId", "Type");
            });
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("Messages");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasConversion(id => id.Value, v => MessageId.From(v));
            entity.Property(m => m.ConversationId).HasConversion(id => id.Value, v => ConversationId.From(v));
            entity.Property(m => m.Type).HasConversion<int>();
            entity.Property(m => m.CreatedAt);
            entity.Property(m => m.EditedAt);
            entity.Property(m => m.DeletedAt);
            entity.HasIndex(m => m.ConversationId);

            entity.OwnsOne(m => m.Author, authorBuilder =>
            {
                authorBuilder.Property(a => a.UserId)
                    .HasConversion(
                        v => v.ToString(),
                        v => ChatUserId.FromString(v)
                    )
                    .HasColumnName("AuthorUserId")
                    .HasMaxLength(150);

                authorBuilder.Property(a => a.Type)
                    .HasColumnName("AuthorType")
                    .HasConversion<int>();

                authorBuilder.Property(a => a.Role)
                    .HasColumnName("AuthorRole")
                    .HasConversion<int>()
                    .HasDefaultValue(ConversationRole.Member);

                authorBuilder.Property(a => a.DisplayName)
                    .HasColumnName("AuthorDisplayName")
                    .HasMaxLength(200);

                authorBuilder.Property(a => a.AvatarUrl)
                    .HasColumnName("AuthorAvatarUrl");
            });

            entity.OwnsOne(m => m.Content, contentBuilder =>
            {
                contentBuilder.Property(c => c.Text).HasColumnName("Text").HasColumnType("text");
                contentBuilder.OwnsMany(c => c.Attachments, attachmentBuilder =>
                {
                    attachmentBuilder.WithOwner().HasForeignKey("MessageId");
                    attachmentBuilder.ToTable("MessageAttachments");
                    attachmentBuilder.Property(a => a.Id).HasColumnName("Id");
                    attachmentBuilder.Property(a => a.FileName).HasColumnName("FileName");
                    attachmentBuilder.Property(a => a.Size).HasColumnName("Size");
                    attachmentBuilder.Property(a => a.MimeType).HasColumnName("MimeType");
                    attachmentBuilder.Property(a => a.Type).HasColumnName("Type").HasConversion<int>();
                    attachmentBuilder.Property(a => a.Url).HasColumnName("Url");
                    attachmentBuilder.HasKey("MessageId", "Id");
                });
            });

            entity.OwnsOne(m => m.ReplyTo, replyBuilder =>
            {
                replyBuilder.Property(r => r.ReferencedMessageId).HasColumnName("ReplyTo_MessageId")
                    .HasConversion(id => id.Value, v => MessageId.From(v));
                replyBuilder.Property(r => r.AuthorDisplayName).HasColumnName("ReplyTo_AuthorDisplayName").HasMaxLength(200);
                replyBuilder.Property(r => r.TextSnippet).HasColumnName("ReplyTo_TextSnippet").HasMaxLength(120);
                replyBuilder.Property(r => r.IsDeleted).HasColumnName("ReplyTo_IsDeleted");
            });

            entity.OwnsMany(m => m.ReadReceipts, receiptBuilder =>
            {
                receiptBuilder.WithOwner().HasForeignKey("MessageId");
                receiptBuilder.ToTable("MessageReadReceipts");

                receiptBuilder.Property(r => r.ReaderId)
                    .HasConversion(
                        v => v.ToString(),
                        v => ChatUserId.FromString(v)
                    )
                    .HasColumnName("ReaderId")
                    .HasMaxLength(150);        

                receiptBuilder.Property(r => r.ReaderType)
                    .HasColumnName("ReaderType")
                    .HasConversion<int>();

                receiptBuilder.Property(r => r.ReadAt).HasColumnName("ReadAt");

                receiptBuilder.HasKey("MessageId", "ReaderId", "ReaderType");
            });
        });

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("SupportTickets");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasConversion(id => id.Value, v => SupportTicketId.From(v));
            entity.Property(t => t.ConversationId).HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                v => v.HasValue ? ConversationId.From(v.Value) : null);
            entity.Property(t => t.Status).HasConversion<int>();
            entity.Property(t => t.Theme).HasMaxLength(200);
            entity.Property(t => t.CloseReason).HasMaxLength(500);
            entity.Property(t => t.AssignedManagerId)
                .HasConversion(
                    v => v == null ? null : v.ToString(),
                    v => string.IsNullOrEmpty(v) ? null : ChatUserId.FromString(v)
                )
                .HasColumnName("AssignedManagerId")
                .HasMaxLength(150);
            entity.Ignore(t => t.AssignedManager);
            entity.Ignore(t => t.Sla);

            entity.OwnsOne(t => t.Initiator, ib =>
            {
                ib.OwnsOne(i => i.UserId, uid =>
                {
                    uid.Property(u => u.TenantId).HasColumnName("InitiatorTenantId").HasMaxLength(100);
                    uid.Property(u => u.UserId).HasColumnName("InitiatorUserId");
                });
                ib.Property(i => i.Type).HasColumnName("InitiatorType").HasConversion<int>();
                ib.Ignore(i => i.DisplayName);
                ib.Ignore(i => i.AvatarUrl);
            });

            entity.OwnsMany(t => t.History, historyBuilder =>
            {
                historyBuilder.WithOwner().HasForeignKey("TicketId");
                historyBuilder.ToTable("TicketHistories");

                historyBuilder.Property(h => h.ActorId)
                    .HasConversion(
                        v => v == null ? (string?)null : v.ToString(),
                        v => string.IsNullOrEmpty(v) ? null : ChatUserId.FromString(v)
                    )
                    .HasColumnName("ActorId")
                    .HasMaxLength(150);

                historyBuilder.Property(h => h.Action).HasMaxLength(200);
                historyBuilder.Property(h => h.OccurredAt);
                historyBuilder.HasKey("TicketId", "OccurredAt", "Action");
            });
        });

        modelBuilder.Entity<UserPresence>(entity =>
        {
            entity.ToTable("UserPresences");
            entity.Property(p => p.UserId)
              .HasConversion(
                  v => v.ToString(),
                  v => ChatUserId.FromString(v)
              );
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.Status).HasConversion<int>();
            entity.Property(p => p.ConnectionCount);
            entity.Property(p => p.LastSeenAt);
            entity.Property(p => p.LastActivityAt);
            entity.Property(p => p.StatusUpdatedAt);
            entity.Property(p => p.IsManualStatus);
        });

        modelBuilder.Entity<MessageVersion>(entity =>
        {
            entity.ToTable("MessageVersions");
            entity.HasKey(v => v.Id);
            entity.Property(v => v.MessageId).HasConversion(id => id.Value, v => MessageId.From(v));
            entity.Property(v => v.EditorType).HasConversion<int>();
            entity.OwnsMany(v => v.Attachments, attachmentBuilder =>
            {
                attachmentBuilder.WithOwner().HasForeignKey("MessageVersionId");
                attachmentBuilder.ToTable("MessageVersionAttachments");
                attachmentBuilder.Property(a => a.Id).HasColumnName("Id");
                attachmentBuilder.Property(a => a.FileName).HasColumnName("FileName");
                attachmentBuilder.Property(a => a.Size).HasColumnName("Size");
                attachmentBuilder.Property(a => a.MimeType).HasColumnName("MimeType");
                attachmentBuilder.Property(a => a.Type).HasColumnName("Type").HasConversion<int>();
                attachmentBuilder.Property(a => a.Url).HasColumnName("Url");
                attachmentBuilder.HasKey("MessageVersionId", "Id");
            });
        });

        modelBuilder.Entity<ConversationAuditEntry>(entity =>
        {
            entity.ToTable("ConversationAuditEntries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConversationId).HasConversion(id => id.Value, v => ConversationId.From(v));
            entity.Property(e => e.Action).HasConversion<int>();
            entity.Property(e => e.ActorType).HasConversion<int?>();
            entity.Property(e => e.TargetParticipantType).HasConversion<int?>();
            entity.Property(e => e.Payload).HasColumnType("jsonb");

            entity.Property(e => e.ActorId)
                .HasConversion(
                    v => v == null ? null : v.ToString(),
                    v => string.IsNullOrEmpty(v) ? null : ChatUserId.FromString(v)
                )
                .HasMaxLength(150);

            entity.Property(e => e.TargetParticipantId)
                .HasConversion(
                    v => v == null ? null : v.ToString(),
                    v => string.IsNullOrEmpty(v) ? null : ChatUserId.FromString(v)
                )
                .HasMaxLength(150);

            entity.HasIndex(e => new { e.ConversationId, e.OccurredAt });
        });

        modelBuilder.Entity<ParticipantNotificationMute>(entity =>
        {
            entity.ToTable("ParticipantNotificationMutes");

            entity.Property(m => m.ViewerId)
                .HasConversion(
                    v => v.ToString(),
                    v => ChatUserId.FromString(v)
                )
                .HasColumnName("ViewerId")
                .HasMaxLength(150);

            entity.Property(m => m.MutedParticipantId)
                .HasConversion(
                    v => v.ToString(),
                    v => ChatUserId.FromString(v)
                )
                .HasColumnName("MutedParticipantId")
                .HasMaxLength(150);

            entity.Property(m => m.ConversationId)
                .HasConversion(id => id.Value, v => ConversationId.From(v));

            entity.HasKey(m => new { m.ConversationId, m.ViewerId, m.MutedParticipantId });

            entity.Property(m => m.CreatedAt);
            entity.HasIndex(m => new { m.ConversationId, m.ViewerId });
        });
    }
}
