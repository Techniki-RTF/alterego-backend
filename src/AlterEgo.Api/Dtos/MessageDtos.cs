namespace AlterEgo.Api.Dtos;

public record GenerateMaskRequest(long DialogId, string OriginalText);

public record GenerateMaskResponse(
    long DialogId,
    string OriginalText,
    string CoverText);

public record PushMessageRequest(
    long TelegramMessageId,
    long DialogId,
    string OriginalText,
    string CoverText,
    DateTimeOffset CreatedAt);

public record PushMessageResponse(
    Guid Id,
    long TelegramMessageId,
    long SenderTelegramId,
    long DialogId,
    string OriginalText,
    DateTimeOffset CreatedAt);

public record DecodeMessageRequest(long DialogId, long SenderTelegramId, string CoverText, DateTimeOffset ReceivedAt);

public record MessageResponse(
    Guid Id,
    long TelegramMessageId,
    long SenderTelegramId,
    long DialogId,
    string OriginalText,
    DateTimeOffset CreatedAt);

public record MessagesPageResponse(List<MessageResponse> Messages, DateTimeOffset? NextCursor);

public record MessagesUpdatesResponse(List<MessageResponse> Messages, DateTimeOffset? LastCreatedAt);
