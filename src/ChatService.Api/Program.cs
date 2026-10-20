using ChatService.Api;
using ChatService.Api.Hubs;
using ChatService.Api.Infrastructure.HealthChecks;
using ChatService.Domain.Exceptions;
using ChatService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ConversationDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ForbiddenException ex)
    {
        if (context.Response.HasStarted)
            throw;

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
    catch (NotFoundException ex)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
    catch (DomainException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }

});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.Write
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<ChatHub>("/api/chat/hub");

app.Run();

public partial class Program;
