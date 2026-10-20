# Демонстрационные тест-кейсы

Предусловия: `docker compose up --build -d`. Compose включает Development и создаёт демонстрационную комнату при открытии **http://localhost:8030**. На странице должны подключиться Alex и Sam.

Выберите кейс в верхнем блоке и нажмите **«Запустить кейс»**. Шаги выполняются настоящими HTTP/WebSocket-запросами. В журнале отображаются коды ответов и проверенные события, затем PASS или FAIL. Кнопка «Копировать шаги» даёт JSON запросов и данные для получения токенов.

| ID | Сценарий | Ожидаемый результат |
| --- | --- | --- |
| CHAT-001 | Alex отправляет сообщение | HTTP 200, оба клиента получают `MessageReceived` с тем же ID |
| CHAT-002 | Отправка, редактирование и поиск | HTTP 200, Sam получает `MessageEdited`, поиск возвращает новый текст |
| CHAT-003 | Sam читает новое сообщение Alex | HTTP 204, Alex получает `MessageRead` |
| CHAT-004 | Чтение без JWT | HTTP 401 |
| CHAT-005 | Чтение от Visitor, не состоящего в комнате | HTTP 403 |
| CHAT-006 | Чтение от ID Alex в другом tenant | HTTP 403 |
| CHAT-007 | Приватность вложения | Загрузка 201; Sam до прикрепления 403, после — 200; Visitor — 403; файл есть в истории |
| CHAT-008 | Закрепление и открепление для себя | Pin и Unpin возвращают 204 без явного target participant |
| CHAT-009 | Внешнее уведомление RabbitMQ | В демоочереди появляется `notification.requested`, содержащий ID отправленного сообщения |

## Воспроизведение в Swagger

1. Открыть **http://localhost:8030/swagger**.
2. Выполнить `POST /api/demo/bootstrap`; сохранить `conversationId`.
3. В `POST /api/dev/token` выбрать участника в **Examples** и нажать Execute.
4. Скопировать поле `token`, нажать **Authorize** и вставить сам JWT. Префикс Bearer Swagger добавляет автоматически.
5. Выполнять запросы из таблицы или получить готовые шаги через `GET /api/demo/cases`. После bootstrap каталог возвращает действительный ID комнаты в URL.

| Example токена | Назначение |
| --- | --- |
| Alex | `userId=1`, customer, tenant demo |
| Sam | `userId=2`, executor, tenant demo |
| Visitor | `userId=3`, customer, tenant demo; не состоит в разговоре |
| OtherTenant | `userId=1`, customer, tenant other; другая организация |

Для CHAT-004 нажмите Logout в окне Authorize. Для CHAT-005/006 смените токен. Для редактирования скопируйте ID из ответа на создание сообщения. Для CHAT-007 скопируйте `fileId` из загрузки и вставьте в JSON вложения.

У `POST /api/Chat/{conversationId}/messages` есть Examples обычного и пустого сообщения; пустое сообщение ожидаемо возвращает 400. У `PUT .../messages/{messageId}` есть готовое тело редактирования. У `POST .../manage` можно выбрать Pin или Unpin.

Такие же JSON-тела сохранены в [examples/tokens](../examples/tokens) и [examples/messages](../examples/messages). Это запросы для получения токенов, а не сами JWT: выполните `POST /api/dev/token`, затем используйте свежий ответ в Authorize.

После перезагрузки страницы каталог остаётся доступен; сохранённая история читается из PostgreSQL. Создавайте новый текст сообщения для каждого воспроизведения, чтобы отличать события от предыдущих запусков.

## Проверки RabbitMQ

Для CHAT-009 отдельные шаги доступны прямо в Swagger:

1. `POST /api/demo/rabbitmq/prepare` — создать и привязать очередь `demo.chat.notifications`.
2. От имени Alex отправить сообщение Sam.
3. `POST /api/demo/rabbitmq/consume` — получить и подтвердить одно событие.

Подробности в [RABBITMQ.ru.md](RABBITMQ.ru.md). Последний шаг удаляет полученное событие из демоочереди.

Автоматическая проверка основных сценариев и уведомления RabbitMQ:

```sh
node scripts/smoke.mjs
```
