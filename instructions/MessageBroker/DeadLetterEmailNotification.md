# Dead Letter Email Notifications

The `DeadLetterEmailNotificationService` sends HTML email alerts when outbox messages fail to publish after the maximum number of retries and are moved to dead-letter status.

## Configuration

### 1. Update `appsettings.json`

Add or update the `MessageBroker.DeadLetterNotification.EmailRecipients` setting with one or more email addresses (semicolon or comma-separated):

```json
{
  "MessageBroker": {
	"DeadLetterNotification": {
	  "EmailRecipients": "admin@example.com;support@example.com"
	}
  }
}
```

### 2. Register the Service in `Program.cs`

The default registration in `Program.cs` is already configured to use `DeadLetterEmailNotificationService`:

```csharp
builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterEmailNotificationService>();
```

If you previously registered `DeadLetterNotificationService` (logging-only), replace it with `DeadLetterEmailNotificationService`.

### 3. Email Settings

Ensure `EmailSettings` is configured in `appsettings.json` (this is used by `IEmailService`):

```json
{
  "EmailSettings": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"EnableSsl": true,
	"FromAddress": "noreply@example.com",
	"FromName": "Portal Application",
	"UserName": "your-email@gmail.com",
	"Password": "your-app-password"
  }
}
```

## Email Content

When a message reaches the dead-letter state, the notification email includes:

- **Message ID** – Unique identifier of the failed message
- **Message Type** – Category/type (e.g., "OrderCreated", "InventoryUpdated")
- **Correlation ID** – Tracking ID for related messages across the system
- **Created At** – When the message was initially published (UTC)
- **Retry Count** – Number of attempts made (will be at least MaxRetries)
- **Last Error** – The exception message from the final publishing attempt
- **Payload Preview** – First 500 characters of the message content for context
- **Action Items** – Guidance on investigating and resolving the issue

## Behavior

1. **Logging**: A critical log entry is always recorded (regardless of email configuration)
2. **Email**: If `MessageBroker:DeadLetterNotification:EmailRecipients` is configured:
   - Email is sent to all configured recipients
   - If email send fails, a warning is logged but the system continues (email failures don't affect message processing)
3. **Fallback**: If no recipients are configured, only logging occurs (no email sent)

## Switching Back to Logging Only

If you want to disable email notifications and revert to logging-only:

```csharp
// In Program.cs
builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterNotificationService>();
```

And remove or comment out `MessageBroker:DeadLetterNotification` in `appsettings.json`.

## Extending the Service

To add additional notification channels (webhooks, Slack, PagerDuty, etc.), create a new implementation of `IDeadLetterNotificationService`:

```csharp
public class DeadLetterCompoundNotificationService : IDeadLetterNotificationService
{
	private readonly IDeadLetterNotificationService[] _notifiers;

	public DeadLetterCompoundNotificationService(params IDeadLetterNotificationService[] notifiers)
	{
		_notifiers = notifiers;
	}

	public async Task NotifyAsync(OutboxMessage message, string? exception, CancellationToken cancellationToken = default)
	{
		var tasks = _notifiers.Select(n => n.NotifyAsync(message, exception, cancellationToken));
		await Task.WhenAll(tasks);
	}
}
```

Then register it in `Program.cs`:

```csharp
builder.Services.AddScoped<IDeadLetterNotificationService>(sp =>
	new DeadLetterCompoundNotificationService(
		new DeadLetterEmailNotificationService(sp.GetRequiredService<IEmailService>(), ...),
		new DeadLetterSlackNotificationService(...),
		new DeadLetterWebhookNotificationService(...)
	));
```

## Querying Dead Letters

To find all dead-lettered messages in the database:

```sql
SELECT 
	Id,
	MessageType,
	CorrelationId,
	CreatedAt,
	ProcessedAt,
	RetryCount,
	Status,
	Exception
FROM dbo.OutboxMessage
WHERE Status = 3  -- DeadLetter
ORDER BY ProcessedAt DESC;
```

Or using EF Core LINQ:

```csharp
var deadLetters = await _context.OutboxMessage
	.Where(m => m.Status == OutboxMessageStatus.DeadLetter)
	.OrderByDescending(m => m.ProcessedAt)
	.ToListAsync();
```

## Troubleshooting

### "No recipients configured" warning

If you see this warning, add recipients to `appsettings.json`:
```json
"MessageBroker": {
  "DeadLetterNotification": {
	"EmailRecipients": "admin@example.com"
  }
}
```

### "Failed to send dead-letter email notification"

This is logged as an error but doesn't prevent the message from being dead-lettered. Check:
1. Email settings are correct (`EmailSettings` in `appsettings.json`)
2. Network connectivity to SMTP server
3. Email credentials are valid
4. Firewall/proxy isn't blocking SMTP port

The system will continue operating; only the email alert will fail.
