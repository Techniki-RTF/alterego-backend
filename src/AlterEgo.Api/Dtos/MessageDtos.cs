namespace AlterEgo.Api.Dtos;

public record CreateMessageRequest(long TelegramMessageId, long DialogId, string OriginalText);

public record CreateMessageResponse(
    Guid Id,
    long TelegramMessageId,
    long SenderTelegramId,
    long DialogId,
    string OriginalText,
    string CoverText,
    DateTimeOffset CreatedAt);

public record DecodeMessageRequest(long DialogId, long SenderTelegramId, string CoverText, DateTimeOffset ReceivedAt);

public record MessageResponse(
    Guid Id,
    long TelegramMessageId,
    long SenderTelegramId,
    long DialogId,
    string OriginalText,
    DateTimeOffset CreatedAt);

public record MessagesPageResponse(List<MessageResponse> Messages, long? NextCursor);

public record MessagesUpdatesResponse(List<MessageResponse> Messages, long? LastMessageId);
