using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using AlterEgo.Api.Dtos;
using AlterEgo.Core.Entities;
using AlterEgo.Core.Repositories;
using AlterEgo.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlterEgo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;
    private const int LongPollTimeoutSeconds = 30;
    private const int ContextNeighborhoodSize = 8;
    private const int ContextNotesMaxLength = 1200;

    private static readonly object WaitersLock = new();
    private static readonly ConcurrentDictionary<long, List<TaskCompletionSource<MessageEntity>>> Waiters = new();

    private readonly IMessagesRepository _messagesRepository;
    private readonly IDialogContextsRepository _dialogContextsRepository;
    private readonly ILlmService _llmService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessagesRepository messagesRepository,
        IDialogContextsRepository dialogContextsRepository,
        ILlmService llmService,
        ILogger<MessagesController> logger)
    {
        _messagesRepository = messagesRepository;
        _dialogContextsRepository = dialogContextsRepository;
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request, CancellationToken cancellationToken)
    {
        var senderTelegramId = GetCurrentUserTelegramId();
        if (senderTelegramId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.OriginalText))
        {
            return BadRequest(new ErrorResponse("Original text cannot be empty"));
        }

        if (await _messagesRepository.ExistsAsync(request.DialogId, request.TelegramMessageId, cancellationToken))
        {
            return Conflict(new ErrorResponse("Message with this telegram ID already exists in this dialog"));
        }

        var dialogContext = await _dialogContextsRepository.GetByDialogIdAsync(request.DialogId, cancellationToken);
        if (dialogContext is null)
        {
            var now = DateTimeOffset.UtcNow;
            dialogContext = new DialogContextEntity
            {
                DialogId = request.DialogId,
                ContextNotes = string.Empty,
                RecentCoverMessages = string.Empty,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _dialogContextsRepository.AddAsync(dialogContext, cancellationToken);
        }

        var recentMessages = ParseRecentCoverMessages(dialogContext.RecentCoverMessages);

        string coverText;
        try
        {
            coverText = await _llmService.GenerateTextAsync(
                request.OriginalText,
                dialogContext.ContextNotes,
                recentMessages,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate cover text");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorResponse("Failed to generate cover message"));
        }

        UpdateDialogContext(dialogContext, coverText);

        var message = new MessageEntity
        {
            Id = Guid.NewGuid(),
            TelegramMessageId = request.TelegramMessageId,
            SenderTelegramId = senderTelegramId.Value,
            DialogId = request.DialogId,
            OriginalText = request.OriginalText,
            CoverTextHash = ComputeHash(coverText),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _messagesRepository.AddAsync(message, cancellationToken);
        await _messagesRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Message {TelegramMessageId} stored for dialog {DialogId}",
            request.TelegramMessageId, request.DialogId);

        NotifyWaiters(request.DialogId, message);

        var response = new CreateMessageResponse(
            message.Id,
            message.TelegramMessageId,
            message.SenderTelegramId,
            message.DialogId,
            message.OriginalText,
            coverText,
            message.CreatedAt);

        return CreatedAtAction(nameof(GetMessage), new { dialogId = message.DialogId, telegramMessageId = message.TelegramMessageId }, response);
    }

    [HttpPost("{dialogId:long}/context/reset")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetDialogContext(long dialogId, CancellationToken cancellationToken = default)
    {
        var dialogContext = await _dialogContextsRepository.GetByDialogIdAsync(dialogId, cancellationToken);
        if (dialogContext is not null)
        {
            _dialogContextsRepository.Remove(dialogContext);
            await _dialogContextsRepository.SaveChangesAsync(cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("{dialogId:long}/{telegramMessageId:long}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessage(long dialogId, long telegramMessageId, CancellationToken cancellationToken)
    {
        var message = await _messagesRepository.GetByTelegramMessageIdAsync(dialogId, telegramMessageId, cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(message));
    }

    [HttpPost("decode")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DecodeMessage([FromBody] DecodeMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CoverText))
        {
            return BadRequest(new ErrorResponse("Cover text cannot be empty"));
        }

        var hash = ComputeHash(request.CoverText);
        var message = await _messagesRepository.GetByCoverTextHashAsync(
            request.DialogId,
            request.SenderTelegramId,
            hash,
            request.ReceivedAt,
            cancellationToken);

        if (message is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(message));
    }

    [HttpGet("{dialogId:long}")]
    [ProducesResponseType(typeof(MessagesPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages(
        long dialogId,
        [FromQuery] DateTimeOffset? beforeCreatedAt = null,
        [FromQuery] int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, MaxLimit);

        var messages = await _messagesRepository.GetByDialogIdAsync(dialogId, beforeCreatedAt, limit + 1, cancellationToken);

        DateTimeOffset? nextCursor = null;
        if (messages.Count > limit)
        {
            nextCursor = messages[limit - 1].CreatedAt;
            messages = messages.Take(limit).ToList();
        }

        var response = new MessagesPageResponse(
            messages.Select(ToResponse).ToList(),
            nextCursor);

        return Ok(response);
    }

    [HttpGet("{dialogId:long}/updates")]
    [ProducesResponseType(typeof(MessagesUpdatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUpdates(
        long dialogId,
        [FromQuery] long? afterMessageId = null,
        [FromQuery] int timeout = LongPollTimeoutSeconds,
        CancellationToken cancellationToken = default)
    {
        timeout = Math.Clamp(timeout, 0, 60);

        var messages = await _messagesRepository.GetAfterMessageIdAsync(dialogId, afterMessageId, cancellationToken);

        if (messages.Count > 0)
        {
            return Ok(new MessagesUpdatesResponse(
                messages.Select(ToResponse).ToList(),
                messages.Max(m => m.TelegramMessageId)));
        }

        if (timeout == 0)
        {
            return Ok(new MessagesUpdatesResponse([], afterMessageId));
        }

        var tcs = new TaskCompletionSource<MessageEntity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiters = Waiters.GetOrAdd(dialogId, _ => []);

        lock (WaitersLock)
        {
            waiters.Add(tcs);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            try
            {
                var newMessage = await tcs.Task.WaitAsync(cts.Token);

                if (afterMessageId.HasValue && newMessage.TelegramMessageId <= afterMessageId.Value)
                {
                    return Ok(new MessagesUpdatesResponse([], afterMessageId));
                }

                return Ok(new MessagesUpdatesResponse(
                    [ToResponse(newMessage)],
                    newMessage.TelegramMessageId));
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Ok(new MessagesUpdatesResponse([], afterMessageId));
            }
        }
        finally
        {
            lock (WaitersLock)
            {
                waiters.Remove(tcs);
            }
        }
    }

    private static void NotifyWaiters(long dialogId, MessageEntity message)
    {
        if (!Waiters.TryGetValue(dialogId, out var waiters))
        {
            return;
        }

        List<TaskCompletionSource<MessageEntity>> toNotify;
        lock (WaitersLock)
        {
            toNotify = [..waiters];
            waiters.Clear();
        }

        foreach (var tcs in toNotify)
        {
            tcs.TrySetResult(message);
        }
    }

    private long? GetCurrentUserTelegramId()
    {
        var telegramIdClaim = User.FindFirst("telegram_id")?.Value;
        return long.TryParse(telegramIdClaim, out var telegramId) ? telegramId : null;
    }

    private static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private static MessageResponse ToResponse(MessageEntity message) =>
        new(message.Id, message.TelegramMessageId, message.SenderTelegramId, message.DialogId, message.OriginalText, message.CreatedAt);

    private static List<string> ParseRecentCoverMessages(string serializedMessages)
    {
        if (string.IsNullOrWhiteSpace(serializedMessages))
        {
            return [];
        }

        return serializedMessages
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static void UpdateDialogContext(DialogContextEntity dialogContext, string newCoverMessage)
    {
        var recentMessages = ParseRecentCoverMessages(dialogContext.RecentCoverMessages);
        var normalizedCoverMessage = newCoverMessage.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (!string.IsNullOrWhiteSpace(normalizedCoverMessage))
        {
            recentMessages.Add(normalizedCoverMessage);
        }

        if (recentMessages.Count > ContextNeighborhoodSize)
        {
            recentMessages = recentMessages.TakeLast(ContextNeighborhoodSize).ToList();
        }

        dialogContext.RecentCoverMessages = string.Join('\n', recentMessages);
        dialogContext.ContextNotes = Truncate(string.Join(" ", recentMessages), ContextNotesMaxLength);
        dialogContext.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value[..maxLength];
    }
}
