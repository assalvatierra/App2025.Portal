# MessageBroker Documentation Map

## Visual Overview

```
📚 MessageBroker Documentation
│
├─ 🌟 START HERE
│  ├─ README.md ......................... Complete index, quick links, FAQs
│  └─ DEADLETTER_IMPLEMENTATION_COMPLETE.md .. Implementation summary & checklist
│
├─ 📖 Core Architecture
│  ├─ implementation.md ................. Full system design, components, flow
│  └─ reference.md ...................... API reference, code examples
│
├─ 💀 Dead Letter Documentation
│  ├─ DeadLetterHandling.md ............. Detailed behavior & operations
│  ├─ DeadLetterEmailNotification.md .... Email setup & custom notifications
│  └─ DEADLETTER_EMAIL_SETUP.md ......... Quick-start (3 steps)
│
├─ 🔐 Additional Features
│  └─ Idempotency.md .................... Duplicate message prevention
│
└─ 🗄️ Database & SQL
   ├─ CreateOutboxTable.sql ............. Outbox table DDL
   ├─ CreateProcessedMessageTable.sql ... Idempotency table DDL
   └─ DeadLetterIndex.sql ............... Status column, indexes, migration
```

---

## Reading Guide by Use Case

### "I'm new to this system"
1. Read: **README.md** (overview, architecture, file locations)
2. Read: **implementation.md** (design patterns, flow)
3. Skim: **reference.md** (API reference)
4. **Status**: Ready to publish messages and create consumers

### "I need to set up email alerts"
1. Read: **DEADLETTER_EMAIL_SETUP.md** (3-step quick-start)
2. Reference: **appsettings.json** example
3. Reference: **DeadLetterEmailNotification.md** (troubleshooting)
4. **Status**: Email alerts configured

### "A message went to dead-letter, now what?"
1. Read: **DeadLetterHandling.md** (query examples, root cause investigation)
2. SQL: Query with provided examples
3. Action: Fix issue and optionally re-publish
4. **Status**: Message resolved

### "I want to customize notifications"
1. Read: **DeadLetterEmailNotification.md** (extension section)
2. Implement: Custom `IDeadLetterNotificationService`
3. Register: In `Program.cs`
4. **Status**: Custom notifications active

### "I'm debugging a publishing issue"
1. Check: **implementation.md** → "Outbox Pattern" section (flow diagram)
2. Query: `OutboxMessage` table using examples from README
3. Review: Logs for exceptions
4. Read: **DeadLetterHandling.md** → "Troubleshooting"
5. **Status**: Root cause identified

---

## Document Relationships

```
┌─ README.md (Hub)
│  ├─ links to: implementation.md
│  ├─ links to: reference.md
│  ├─ links to: DeadLetterHandling.md
│  ├─ links to: DeadLetterEmailNotification.md
│  ├─ links to: Idempotency.md
│  └─ provides: SQL queries, config examples, troubleshooting
│
├─ implementation.md (Architecture)
│  ├─ describes: BrokerMessage, Outbox, Idempotency, Consumers
│  ├─ references: DeadLetterHandling at end
│  ├─ links to: relevant task files
│  └─ includes: "Next Steps" checklist
│
├─ reference.md (API)
│  ├─ documents: All public classes and methods
│  ├─ includes: Code examples
│  ├─ appended: Dead letter section
│  └─ links to: Detailed docs
│
├─ DeadLetterHandling.md (Operations)
│  ├─ explains: Status enum, retry logic, cleanup
│  ├─ provides: SQL queries for dead letters
│  ├─ covers: Extending notification services
│  └─ includes: Troubleshooting guide
│
└─ DeadLetterEmailNotification.md (Setup)
   ├─ step-by-step: Configuration guide
   ├─ shows: Email content and formatting
   ├─ explains: Behavior and error handling
   ├─ provides: Extension examples (webhooks, Slack, etc.)
   └─ includes: Troubleshooting
```

---

## File Locations Reference

### Documentation Files
```
instructions/MessageBroker/
├─ README.md ............................... Complete guide & index
├─ implementation.md ........................ Architecture & design
├─ reference.md ............................ API reference
├─ DeadLetterHandling.md ................... Dead-letter operations
├─ DeadLetterEmailNotification.md .......... Email notifications
├─ DEADLETTER_EMAIL_SETUP.md ............... Quick-start setup
├─ Idempotency.md .......................... Duplicate prevention
├─ CreateOutboxTable.sql ................... Outbox table DDL
├─ CreateProcessedMessageTable.sql ......... Idempotency table DDL
└─ DeadLetterIndex.sql ..................... Status column & indexes
```

### Code Files
```
Services/MessageBroker/
├─ BrokerMessage.cs ........................ Message contract
├─ OutboxMessage.cs ........................ EF entity (with Status)
├─ OutboxMessageStatus.cs .................. Status enum
├─ OutboxService.cs ........................ Outbox operations
├─ OutboxPublisher.cs ...................... Message publisher
├─ OutboxPollingService.cs ................. Background polling worker
├─ IMessageConsumer.cs ..................... Consumer interface
├─ MessageConsumerBase.cs .................. Consumer base class
├─ BrokerMessageConsumer.cs ................ MassTransit dispatcher
├─ IDeadLetterNotificationService.cs ....... Notification interface
├─ DeadLetterNotificationService.cs ........ Logging-only implementation
└─ DeadLetterEmailNotificationService.cs ... Email implementation
```

---

## Quick Links Index

### Setup & Configuration
- Configuration example: See **README.md** → "Configuration Reference"
- Email setup: See **DEADLETTER_EMAIL_SETUP.md**
- Detailed config: See **DeadLetterEmailNotification.md** → "Configuration"

### Publishing & Consuming
- How to publish: See **reference.md** → "IOutboxPublisher"
- How to consume: See **implementation.md** → "Consumer Abstraction"
- First event example: See **implementation.md** → "ReservationVerified"

### Queries & Operations
- Query dead letters: See **README.md** → "Common Tasks"
- Track message: See **README.md** → "Common Tasks"
- Find stuck messages: See **README.md** → "Common Tasks"
- Republish message: See **README.md** → "Common Tasks"

### Troubleshooting
- General: See **README.md** → "Troubleshooting"
- Messages not publishing: See **DeadLetterHandling.md** → "Troubleshooting"
- Emails not sending: See **DeadLetterEmailNotification.md** → "Troubleshooting"
- Duplicates: See **Idempotency.md** → "Troubleshooting"

### Testing
- Test scenarios: See **README.md** → "Getting Started Checklist"
- Dead-letter tests: See **DeadLetterHandling.md** → "Detecting and Handling"
- Email testing: See **DeadLetterEmailNotification.md** → "Email Content"

### Extension & Customization
- Custom notifications: See **DeadLetterEmailNotification.md** → "Extending the Service"
- Add consumers: See **implementation.md** → "Consumer Abstraction"
- Add event types: See **reference.md** → "BrokerMessage"

---

## Document Content Summary

### README.md (Long)
- **Purpose**: Comprehensive overview and navigation hub
- **Length**: ~600 lines
- **Contains**: 
  - Visual overview diagram
  - Quick links by use case
  - File locations reference
  - Configuration reference
  - Status summary table
  - Getting started checklist
  - Common SQL queries
  - Troubleshooting guide
  - Next steps & future work

### implementation.md (Medium)
- **Purpose**: Architectural overview and design documentation
- **Length**: ~150 lines
- **Contains**:
  - Overview and core components
  - Outbox pattern section (implemented)
  - Idempotency section (implemented)
  - Consumer abstraction section (implemented)
  - Dead-letter handling section (NEW)
  - First event example (ReservationVerified)
  - Next steps checklist (updated)

### reference.md (Long)
- **Purpose**: API and code reference documentation
- **Length**: ~400 lines (original)
- **Contains**:
  - BrokerMessage class definition
  - Properties and constructors
  - IOutboxPublisher interface
  - IMessageConsumer interface
  - MessageConsumerBase generic class
  - Configuration and DI setup
  - Outbox pattern summary (appended)
  - Dead letter handling summary (appended)

### DeadLetterHandling.md (Medium)
- **Purpose**: Dead-letter behavior, operations, and troubleshooting
- **Length**: ~150 lines
- **Contains**:
  - Behavior summary
  - Configuration options
  - Database schema
  - Querying dead letters (SQL)
  - Extending services
  - Troubleshooting guide

### DeadLetterEmailNotification.md (Long)
- **Purpose**: Email notification setup and customization
- **Length**: ~200 lines
- **Contains**:
  - Configuration guide (3 sections)
  - Email content description
  - Behavior and error handling
  - Fallback behavior
  - Switching back to logging-only
  - Extension examples (webhooks, Slack, PagerDuty)
  - Query examples
  - Troubleshooting guide

### DEADLETTER_EMAIL_SETUP.md (Short)
- **Purpose**: Quick-start guide for email setup
- **Length**: ~80 lines
- **Contains**:
  - Implementation summary
  - Files created/modified
  - Quick start (3 steps)
  - Behavior overview
  - Rollback instructions
  - Extension points

### DEADLETTER_IMPLEMENTATION_COMPLETE.md (Long)
- **Purpose**: Implementation summary and project completion report
- **Length**: ~250 lines
- **Contains**:
  - Overview
  - What was implemented (detailed)
  - Configuration guide
  - Behavior flow diagram
  - File changes summary
  - Testing scenarios
  - Email content description
  - Query examples
  - Next steps (Immediate, Short, Long term)
  - Troubleshooting
  - Implementation checklist

### Idempotency.md (Medium)
- **Purpose**: Duplicate message prevention details
- **Length**: ~200 lines
- **Contains**:
  - Problem statement
  - Solution approach
  - ProcessedMessage table schema
  - Per-consumer semantics
  - Cleanup strategy
  - Troubleshooting

---

## How to Update Documentation

When making changes to the MessageBroker system:

1. **Code Changes** → Update **reference.md** with new API signatures
2. **Behavior Changes** → Update **implementation.md** and **DeadLetterHandling.md**
3. **Configuration** → Update **DeadLetterEmailNotification.md** and **README.md**
4. **New Features** → Add section to appropriate document and update README index
5. **Bug Fixes** → Update **Troubleshooting** sections in relevant documents
6. **Next Steps** → Update checklist in **implementation.md**

---

## Print-Friendly Documents

For printing or offline reading:
1. **README.md** - Best overview (print for offices/briefing)
2. **implementation.md** - Best for architecture discussions
3. **DeadLetterEmailNotification.md** - Best for setup instructions
4. **reference.md** - Best as developer quick-reference

---

## Document Version History

| Document | Version | Last Updated | Status |
|----------|---------|--------------|--------|
| README.md | 1.0 | Dec 2024 | ✅ Complete |
| implementation.md | 1.1 | Dec 2024 | ✅ Updated (Dead Letter) |
| reference.md | 1.1 | Dec 2024 | ✅ Updated (Dead Letter) |
| DeadLetterHandling.md | 1.0 | Dec 2024 | ✅ New |
| DeadLetterEmailNotification.md | 1.0 | Dec 2024 | ✅ New |
| DEADLETTER_EMAIL_SETUP.md | 1.0 | Dec 2024 | ✅ New |
| DEADLETTER_IMPLEMENTATION_COMPLETE.md | 1.0 | Dec 2024 | ✅ New |
| Idempotency.md | 1.0 | Oct 2024 | ✅ Existing |

---

## Related Code References

### Configuration Files
- `Program.cs` - Service registration and MassTransit setup
- `appsettings.json` - Email and MessageBroker configuration
- `Portal.csproj` - Package dependencies (MassTransit, MailKit)

### Database Entities
- `Data/ApplicationDbContext.cs` - EF Core mappings and configuration
- `Data/Migrations/` - Database migration history

### Consumers Directory
- `Services/Consumers/ReservationVerifiedConsumer.cs` - Example consumer
- `Services/Consumers/ReservationVerifiedConsumerEmailSender.cs` - Example consumer
- `Services/Consumers/ReservationVerifiedInternalEmailSender.cs` - Example consumer

### Controllers/Pages
- `Controllers/PortalReservationController.cs` - Example publisher integration
- Any page handler using `IOutboxPublisher` for event publishing

---

## External References

### MassTransit Documentation
- [MassTransit Getting Started](https://masstransit.io/getting-started)
- [MassTransit Consumers](https://masstransit.io/documentation/concepts/consumers)
- [MassTransit Outbox Pattern](https://masstransit.io/documentation/patterns/outbox)

### .NET 10 Features Used
- Records and nullable annotations
- Async/await patterns
- Dependency injection
- Configuration providers
- Entity Framework Core 8+

---

This map should help you navigate the MessageBroker documentation system efficiently!
