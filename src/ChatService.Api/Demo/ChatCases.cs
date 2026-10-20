namespace ChatService.Api.Demo;

public sealed record ChatDemoCase(string Id, string Title, string Description, string Expected,
    string SwaggerSteps, object[] Requests);

public static class ChatCases
{
    public static ChatDemoCase[] Create(Guid? conversationId)
    {
        var room = conversationId?.ToString() ?? "{conversationId-из-bootstrap}";
        var path = $"/api/Chat/{room}";
        return
        [
            new("CHAT-001", "Доставка двум участникам", "Alex отправляет сообщение; обе панели получают MessageReceived.",
                "HTTP 200 и событие у Alex и Sam", "Получить токен Alex, нажать Authorize, отправить текст через POST messages. Обе панели страницы должны быть открыты.",
                [new { method = "POST", url = path + "/messages", token = "Alex", body = new { text = "Проверяем доставку сообщения" } }]),
            new("CHAT-002", "Редактирование и поиск", "Сохранить сообщение, изменить текст и найти новую версию.",
                "HTTP 200; поиск содержит изменённый текст", "Создать сообщение от Alex, скопировать id, затем PUT messages/{messageId}; поиск выполнить от Sam.",
                [new { method = "POST", url = path + "/messages", token = "Alex", body = new { text = "Первоначальный текст" } },
                 new { method = "PUT", url = path + "/messages/{messageId}", token = "Alex", body = new { text = "Исправленный текст" } },
                 new { method = "GET", url = path + "/messages/search?q=Исправленный", token = "Sam" }]),
            new("CHAT-003", "Подтверждение прочтения", "Alex отправляет новое сообщение, Sam отмечает разговор прочитанным.",
                "HTTP 204 и событие MessageRead у Alex", "После создания сообщения переключить Authorize на токен Sam и выполнить POST read.",
                [new { method = "POST", url = path + "/messages", token = "Alex", body = new { text = "Сообщение для прочтения" } },
                 new { method = "POST", url = path + "/read", token = "Sam" }]),
            new("CHAT-004", "Запрос без JWT", "Получить историю без Authorization.", "HTTP 401",
                "В Authorize нажать Logout, затем GET messages.", [new { method = "GET", url = path + "/messages", token = "Без токена" }]),
            new("CHAT-005", "Посторонний пользователь", "Visitor с ID 3 не состоит в разговоре.", "HTTP 403",
                "POST dev/token → Example Visitor. Вставить токен в Authorize и выполнить GET messages.", [new { method = "GET", url = path + "/messages", token = "Visitor" }]),
            new("CHAT-006", "Другой tenant", "ID пользователя совпадает с Alex, но tenant другой.", "HTTP 403",
                "POST dev/token → Example OtherTenant. Вставить токен в Authorize и выполнить GET messages.", [new { method = "GET", url = path + "/messages", token = "OtherTenant" }]),
            new("CHAT-007", "Приватное вложение", "До отправки файл доступен владельцу; после отправки — участникам, но не Visitor.",
                "Upload 201; Sam до отправки 403, после 200; Visitor 403",
                "Загрузить file от Alex, скопировать fileId и URL. До прикрепления попробовать скачать от Sam. Затем отправить вложение и повторить скачивание.",
                [new { method = "POST", url = "/api/Chat/upload", token = "Alex", multipart = "file: любой небольшой .txt" },
                 new { method = "POST", url = path + "/messages", token = "Alex", body = new { text = "Документ", attachments = new[] {
                     new { id = "{fileId-из-upload}", name = "demo.txt", size = 4, mimeType = "text/plain", attachmentType = "Document", url = "/api/files/{fileId}/download" } } } },
                 new { method = "GET", url = "/api/files/{fileId}/download", token = "Sam / Visitor" }]),
            new("CHAT-008", "Закрепление разговора", "Alex закрепляет и открепляет разговор без указания target participant.",
                "Оба запроса HTTP 204", "Использовать токен Alex. В POST manage выбрать Example Pin, затем Unpin.",
                [new { method = "POST", url = path + "/manage", token = "Alex", body = new { action = "Pin" } },
                 new { method = "POST", url = path + "/manage", token = "Alex", body = new { action = "Unpin" } }]),
            new("CHAT-009", "Уведомление через RabbitMQ", "Привязать демоочередь, отправить сообщение и получить notification.requested для Sam.",
                "Событие в demo.chat.notifications содержит ID отправленного сообщения",
                "POST demo/rabbitmq/prepare → отправить сообщение от Alex → POST demo/rabbitmq/consume. Consume подтверждает и удаляет одно событие из демоочереди.",
                [new { method = "POST", url = "/api/demo/rabbitmq/prepare", token = "Не требуется в Development" },
                 new { method = "POST", url = path + "/messages", token = "Alex", body = new { text = "Проверяем RabbitMQ" } },
                 new { method = "POST", url = "/api/demo/rabbitmq/consume", token = "Не требуется в Development" }])
        ];
    }
}
