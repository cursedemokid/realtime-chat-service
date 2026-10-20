# Готовые запросы

`tokens/*.json` — тело `POST /api/dev/token` для Alex, Sam, Visitor и OtherTenant. Endpoint доступен в Development. Выполните запрос, скопируйте поле `token` из ответа и вставьте в Swagger **Authorize** без префикса Bearer.

`messages/*.json` — тела создания и редактирования сообщений и действий Pin/Unpin. Пустой текст — отрицательный пример с ожидаемым HTTP 400.

Сначала выполните `POST /api/demo/bootstrap` и сохраните `conversationId`. Для редактирования нужен `messageId` из ответа создания сообщения. Все готовые URL и ожидаемые результаты доступны в `GET /api/demo/cases` после bootstrap.

Для RabbitMQ сначала создайте демоочередь вызовом `POST /api/demo/rabbitmq/prepare`, отправьте сообщение как Alex, затем прочитайте событие через `POST /api/demo/rabbitmq/consume`. Этот вызов подтверждает и удаляет одно уведомление из демоочереди.

Пошаговые инструкции: [тест-кейсы](../docs/TEST-CASES.ru.md), [RabbitMQ](../docs/RABBITMQ.ru.md).
