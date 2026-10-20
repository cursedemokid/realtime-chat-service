using ChatService.Domain.Aggregates;
using ChatService.Domain.Entities;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Repositories;
using Xunit;

namespace ChatService.Infrastructure.IntegrationTests;

public class RepositoryIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public RepositoryIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Attachments_and_their_history_materialize_in_a_fresh_context()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");
        var db = _fixture.DbContext;
        var author = new ConversationParticipant(User(900_010), ParticipantType.Customer, "Demo author");
        var other = new ConversationParticipant(User(900_011), ParticipantType.Executor, "Demo participant");
        var conversation = Conversation.CreateDirect(author, other);
        var fileId = Guid.NewGuid();
        var attachments = new List<Attachment> { new(fileId, "demo.txt", 4,
            "text/plain", AttachmentType.Document, $"/api/files/{fileId}/download") };
        var message = Message.Create(conversation.Id, MessageAuthor.FromParticipant(author),
            MessageType.Attachment, new MessageContent("Attached", attachments));
        db.Conversations.Add(conversation);
        db.Messages.Add(message);
        db.MessageVersions.Add(new MessageVersion(message.Id, 1, "Attached", attachments,
            author.UserId.UserId, author.Type, DateTime.UtcNow));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var loaded = await new EfMessageRepository(db).GetByIdAsync(message.Id);
        Assert.NotNull(loaded);
        Assert.Equal(fileId, Assert.Single(loaded.Content.Attachments).Id);
        var versions = await new EfMessageVersionRepository(db).GetByMessageIdAsync(message.Id, 0, 10);
        Assert.Equal(fileId, Assert.Single(Assert.Single(versions).Attachments).Id);
    }

    [SkippableFact]
    public async Task GetPageByParticipantAsync_returns_conversations()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");

        var conversationRepo = new EfConversationRepository(_fixture.DbContext);
        var initiator = new ConversationParticipant(User(900_001), ParticipantType.Customer, "Integration Test", null);

        var conversation = Conversation.CreateSupportTicket("integration-test", initiator);
        await conversationRepo.AddAsync(conversation);
        await _fixture.DbContext.SaveChangesAsync();

        var (items, total) = await conversationRepo.GetPageByParticipantAsync(
            initiator.UserId, initiator.Type, skip: 0, take: 10);

        Assert.True(total >= 1);
        Assert.Contains(items, c => c.Id == conversation.Id);
    }

    [SkippableFact]
    public async Task GetThemesByConversationIdsAsync_with_guids()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");

        var conversationRepo = new EfConversationRepository(_fixture.DbContext);
        var ticketRepo = new EfSupportTicketRepository(_fixture.DbContext);

        var initiator = new TicketParticipant(User(900_002), ParticipantType.Customer, "Test", null);
        var ticket = SupportTicket.Create(initiator, "Integration theme");
        var conversation = Conversation.CreateSupportTicket("theme-test", new ConversationParticipant(
            initiator.UserId, initiator.Type, initiator.DisplayName, initiator.AvatarUrl));
        ticket.LinkConversation(conversation.Id);

        await conversationRepo.AddAsync(conversation);
        await ticketRepo.AddAsync(ticket);
        await _fixture.DbContext.SaveChangesAsync();

        var themes = await ticketRepo.GetThemesByConversationIdsAsync([conversation.Id.Value]);

        Assert.True(themes.TryGetValue(conversation.Id.Value, out var theme));
        Assert.Equal("Integration theme", theme);
    }

    [SkippableFact]
    public async Task SearchAsync_does_not_throw()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");

        var conversationRepo = new EfConversationRepository(_fixture.DbContext);
        var initiator = new ConversationParticipant(User(900_003), ParticipantType.Customer, "Searchable User", null);
        var conversation = Conversation.CreateSupportTicket("searchable-ref-unique", initiator);

        await conversationRepo.AddAsync(conversation);
        await _fixture.DbContext.SaveChangesAsync();

        var (items, total) = await conversationRepo.SearchAsync(
            initiator.UserId, initiator.Type, "searchable", skip: 0, take: 10);

        Assert.True(total >= 1);
        Assert.Contains(items, c => c.Id == conversation.Id);
    }

    [SkippableFact]
    public async Task CountUnreadTotalAsync_does_not_throw()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");

        var conversationRepo = new EfConversationRepository(_fixture.DbContext);
        var messageRepo = new EfMessageRepository(_fixture.DbContext);

        var customer = new ConversationParticipant(User(900_004), ParticipantType.Customer, "Customer", null);
        var executor = new ConversationParticipant(User(900_005), ParticipantType.Executor, "Executor", null);
        var conversation = Conversation.CreateOrderConversation("order-unread-test", customer, executor);

        var message = Message.Create(
            conversation.Id,
            MessageAuthor.FromParticipant(executor),
            MessageType.Text,
            new MessageContent("Unread integration message"));

        await conversationRepo.AddAsync(conversation);
        await _fixture.DbContext.SaveChangesAsync();

        await messageRepo.AddAsync(message);
        await _fixture.DbContext.SaveChangesAsync();

        var unread = await messageRepo.CountUnreadTotalAsync(customer.UserId);
        Assert.True(unread >= 1);
    }

    [SkippableFact]
    public async Task ParticipantNotificationMute_crud()
    {
        Skip.IfNot(_fixture.IsConfigured, "PostgreSQL connection string is not configured.");

        var conversationRepo = new EfConversationRepository(_fixture.DbContext);
        var muteRepo = new EfParticipantNotificationMuteRepository(_fixture.DbContext);

        var viewer = new ConversationParticipant(User(900_006), ParticipantType.Customer, "Viewer", null);
        var target = new ConversationParticipant(User(900_007), ParticipantType.Executor, "Target", null);
        var conversation = Conversation.CreateOrderConversation("order-mute-test", viewer, target);

        await conversationRepo.AddAsync(conversation);
        await _fixture.DbContext.SaveChangesAsync();

        Assert.False(await muteRepo.IsMutedAsync(
            conversation.Id, viewer.UserId, target.UserId));

        var mute = new ParticipantNotificationMute(
            conversation.Id, viewer.UserId, target.UserId);
        await muteRepo.AddAsync(mute);
        await _fixture.DbContext.SaveChangesAsync();

        Assert.True(await muteRepo.IsMutedAsync(
            conversation.Id, viewer.UserId, target.UserId));

        await muteRepo.RemoveAsync(
            conversation.Id, viewer.UserId, target.UserId);
        await _fixture.DbContext.SaveChangesAsync();

        Assert.False(await muteRepo.IsMutedAsync(
            conversation.Id, viewer.UserId, target.UserId));
    }

    private static ChatUserId User(long id) => new("integration-tests", id);
}
