using ChatService.Api.Filters;
using ChatService.Domain.Aggregates;
using ChatService.Domain.Enums;
using ChatService.Domain.ValueObjects;
using ChatService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Api.Controllers;

[ApiController, DevelopmentOnly, Route("api/demo")]
public sealed class DemoController(ConversationDbContext db) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<IActionResult> Bootstrap(CancellationToken ct)
    {
        var conversation = await db.Conversations.SingleOrDefaultAsync(c => c.ContextReference == "portfolio-demo", ct);
        if (conversation is null)
        {
            conversation = Conversation.CreateOrderConversation("portfolio-demo",
                new(new("demo", 1), ParticipantType.Customer, "Alex"),
                new(new("demo", 2), ParticipantType.Executor, "Sam"));
            db.Conversations.Add(conversation);
            await db.SaveChangesAsync(ct);
        }
        return Ok(new { conversationId = conversation.Id.Value,
            users = new[] { new { userId = 1, role = "customer", name = "Alex" },
                new { userId = 2, role = "executor", name = "Sam" },
                new { userId = 3, role = "customer", name = "Visitor" } } });
    }
}
