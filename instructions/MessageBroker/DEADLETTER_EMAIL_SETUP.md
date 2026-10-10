# Dead Letter Email Notification Implementation Summary

## What Was Done

A new `DeadLetterEmailNotificationService` has been created and registered to send HTML email alerts when outbox messages fail to publish after max retries.

## Files Created

1. **`Services/MessageBroker/DeadLetterEmailNotificationService.cs`**
   - Implements `IDeadLetterNotificationService`
   - Injects `IEmailService` and `IConfiguration`
   - Sends formatted HTML emails with complete message context and error details
   - Logs all events (critical for dead-letter, info for successful sends, errors for failures)
   - Graceful fallback if email configuration is missing

2. **`instructions/MessageBroker/DeadLetterEmailNotification.md`**
   - Complete configuration guide
   - Email content description
   - Behavior overview
   - Extension examples (webhooks, Slack, etc.)
   - Troubleshooting guide

## Files Modified

1. **`Program.cs`**
   - Changed dead-letter service registration to use `DeadLetterEmailNotificationService` (was `DeadLetterNotificationService`)

2. **`appsettings.json`**
   - Added `MessageBroker:DeadLetterNotification:EmailRecipients` configuration (placeholder: "admin@example.com;support@example.com")

## Quick Start

### 1. Update Email Recipients in `appsettings.json`

```json
"MessageBroker": {
  "DeadLetterNotification": {
	"EmailRecipients": "admin@example.com;support@example.com"
  }
}
```

Supports semicolon or comma-separated values.

### 2. Verify Email Settings

Make sure `EmailSettings` is configured in `appsettings.json` (should already be set up):

```json
"EmailSettings": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "FromAddress": "noreply@example.com",
  "FromName": "Portal Application",
  "UserName": "your-email@gmail.com",
  "Password": "your-app-password"
}
```

### 3. Done!

When a message reaches dead-letter status, recipients will receive an HTML email with:
- Message ID, Type, Correlation ID
- Creation and dead-letter timestamps
- Retry count and last error
- Payload preview (first 500 chars)
- Action items for investigation

## Behavior

- **Always**: Logs a critical entry when message is dead-lettered
- **If recipients configured**: Sends HTML email to all recipients
- **If email send fails**: Logs a warning but continues (doesn't block message processing)
- **If no recipients**: Skips email, only logs (no error)

## Rollback to Logging Only

If you want to disable email notifications:

1. Replace in `Program.cs`:
```csharp
builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterNotificationService>();
```

2. Remove `MessageBroker:DeadLetterNotification` from `appsettings.json`

## Extension Points

The service is designed to be extended or composed with other notifiers:
- Email (current)
- Webhook
- Slack
- PagerDuty
- Application Insights / Log Analytics
- Custom alerting systems

See `instructions/MessageBroker/DeadLetterEmailNotification.md` for examples.

## Related Files

- `Services/MessageBroker/OutboxMessageStatus.cs` – Status enum (Pending, Failed, Processed, DeadLetter)
- `Services/MessageBroker/OutboxPollingService.cs` – Detects max retries and triggers dead-letter flow
- `Services/MessageBroker/DeadLetterNotificationService.cs` – Logging-only implementation (still available)
- `instructions/MessageBroker/DeadLetterHandling.md` – General dead-letter handling documentation
