using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ChatService.Api.Demo;

public sealed class ChatExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (path.StartsWith("api/dev/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("api/demo/", StringComparison.OrdinalIgnoreCase))
            operation.Security = [];
        if (operation.RequestBody?.Content is not { } content) return;
        Dictionary<string, (string Description, object Body)>? examples = null;
        if (path.Equals("api/dev/token", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Получить демонстрационный JWT: выберите участника в Examples";
            operation.Description = "Только Development. Скопируйте поле token и нажмите Authorize; вставляйте сам JWT без префикса Bearer.";
            examples = new()
            {
                ["Alex"] = ("Участник разговора, заказчик", new { userId = 1, role = "customer", tenantId = "demo" }),
                ["Sam"] = ("Участник разговора, исполнитель", new { userId = 2, role = "executor", tenantId = "demo" }),
                ["Visitor"] = ("Не состоит в разговоре: ожидается 403", new { userId = 3, role = "customer", tenantId = "demo" }),
                ["OtherTenant"] = ("ID Alex в другом tenant: ожидается 403", new { userId = 1, role = "customer", tenantId = "other" })
            };
        }
        else if (context.ApiDescription.HttpMethod == "POST" && path.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
            examples = new()
            {
                ["TextMessage"] = ("Обычный текст: ожидается 200", new { text = "Привет! Это демонстрационное сообщение." }),
                ["EmptyMessage"] = ("Пустое сообщение: ожидается 400", new { text = "" })
            };
        else if (context.ApiDescription.HttpMethod == "PUT" && path.Contains("/messages/", StringComparison.OrdinalIgnoreCase))
            examples = new() { ["EditText"] = ("Редактирование собственного сообщения", new { text = "Исправленный текст сообщения" }) };
        else if (context.ApiDescription.HttpMethod == "POST" && path.EndsWith("/manage", StringComparison.OrdinalIgnoreCase))
            examples = new()
            {
                ["Pin"] = ("Закрепить для текущего участника: 204", new { action = "Pin" }),
                ["Unpin"] = ("Открепить: 204", new { action = "Unpin" })
            };
        if (examples is null) return;
        foreach (var media in content.Values)
            media.Examples = examples.ToDictionary(item => item.Key, item => (IOpenApiExample)new OpenApiExample
            {
                Summary = item.Key, Description = item.Value.Description,
                Value = JsonSerializer.SerializeToNode(item.Value.Body)
            });
    }
}
