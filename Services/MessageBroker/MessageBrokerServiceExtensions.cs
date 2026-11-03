using System.Reflection;
using MassTransit;

namespace Portal.Services.MessageBroker
{
    public static class MessageBrokerServiceExtensions
    {
        public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure MassTransit with RabbitMQ
            services.AddMassTransit(x =>
            {
                x.AddConsumer<BrokerMessageConsumer>();

                // === RABBITMQ CONFIGURATION (ACTIVE) ===
                //var rabbitMqHost = builder.Configuration["MessageBroker:RabbitMQ:Host"]
                //    ?? throw new InvalidOperationException("RabbitMQ host not configured. Set MessageBroker:RabbitMQ:Host in appsettings.");
                //var rabbitMqPort = builder.Configuration.GetValue<int>("MessageBroker:RabbitMQ:Port", 5672);
                //var rabbitMqUsername = builder.Configuration["MessageBroker:RabbitMQ:Username"]
                //    ?? throw new InvalidOperationException("RabbitMQ username not configured. Set MessageBroker:RabbitMQ:Username in appsettings.");
                //var rabbitMqPassword = builder.Configuration["MessageBroker:RabbitMQ:Password"]
                //    ?? throw new InvalidOperationException("RabbitMQ password not configured. Set MessageBroker:RabbitMQ:Password in appsettings.");
                //var rabbitMqVirtualHost = builder.Configuration["MessageBroker:RabbitMQ:VirtualHost"]
                //    ?? throw new InvalidOperationException("RabbitMQ virtual host not configured. Set MessageBroker:RabbitMQ:VirtualHost in appsettings.");
                //var useSsl = builder.Configuration.GetValue<bool>("MessageBroker:RabbitMQ:UseSsl", true);

                //x.UsingRabbitMq((context, cfg) =>
                //{
                //    cfg.Host(new Uri($"amqps://{rabbitMqUsername}:{rabbitMqPassword}@{rabbitMqHost}:{rabbitMqPort}/{rabbitMqVirtualHost}"));

                //    cfg.ConfigureEndpoints(context);
                //});

                // === AZURE SERVICE BUS CONFIGURATION (COMMENTED OUT) ===
                //var serviceBusConnectionString = builder.Configuration.GetConnectionString("AzureServiceBus")
                //    ?? builder.Configuration["MessageBroker:AzureServiceBus:ConnectionString"]
                //    ?? throw new InvalidOperationException("Azure Service Bus connection string not configured. Set MessageBroker:AzureServiceBus:ConnectionString in appsettings.");

                //x.UsingAzureServiceBus((context, cfg) =>
                //{
                //    cfg.Host(serviceBusConnectionString);

                //    // Configure for Azure Service Bus Basic tier (queues only, no topics)
                //    // This disables topic-based messaging and uses queue-based endpoints instead
                //    cfg.ConfigureEndpoints(context, new DefaultEndpointNameFormatter("Portal", false));
                //});

                // === IN-MEMORY CONFIGURATION (COMMENTED OUT) ===
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            });

            services.AddMessageConsumers();
            services.AddScoped<IOutboxService, OutboxService>();
            services.AddScoped<IOutboxPublisher, OutboxPublisher>();
            services.AddHostedService<OutboxPollingService>();

            // Dead-letter notification service (sends email alerts to configured recipients)
            // To use the email notification service, replace with: DeadLetterEmailNotificationService
            // And configure MessageBroker:DeadLetterNotification:EmailRecipients in appsettings.json
            services.AddScoped<Portal.Services.MessageBroker.IDeadLetterNotificationService, Portal.Services.MessageBroker.DeadLetterEmailNotificationService>();

            // Idempotency for message consumption
            services.Configure<Portal.Services.MessageBroker.IdempotencySettings>(configuration.GetSection("MessageBroker:Idempotency"));
            services.AddScoped<Portal.Services.MessageBroker.IIdempotencyService, Portal.Services.MessageBroker.IdempotencyService>();
            services.AddHostedService<Portal.Services.MessageBroker.IdempotencyCleanupService>();

            // Outbox cleanup for processed messages
            services.Configure<Portal.Services.MessageBroker.OutboxCleanupSettings>(configuration.GetSection("MessageBroker:OutboxCleanup"));
            services.AddHostedService<Portal.Services.MessageBroker.OutboxCleanupService>();


            return services;
        }

        /// <summary>
        /// Registers every concrete <see cref="IMessageConsumer"/> in the assembly as a scoped service.
        /// </summary>
        public static IServiceCollection AddMessageConsumers(this IServiceCollection services, Assembly? assembly = null)
        {
            assembly ??= typeof(IMessageConsumer).Assembly;

            var consumerTypes = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IMessageConsumer).IsAssignableFrom(t));

            foreach (var type in consumerTypes)
            {
                services.AddScoped(typeof(IMessageConsumer), type);
            }

            return services;
        }
    }
}
