using System.Diagnostics;

namespace Portal.Services.Observability
{
    /// <summary>
    /// Provides a centralized ActivitySource for Portal application custom instrumentation.
    /// Use this source to create activities for message consumption, business logic tracing, etc.
    /// </summary>
    public static class PortalActivitySource
    {
        public const string ServiceName = "Portal";

        public static readonly ActivitySource Instance = new ActivitySource(ServiceName);

        /// <summary>
        /// Activity names used throughout the Portal application for consistency and discoverability.
        /// </summary>
        public static class ActivityNames
        {
            public const string MessageConsume = "message.consume";
            public const string MessagePublish = "message.publish";
            public const string ServiceOperation = "service.operation";
            public const string RepositoryQuery = "repository.query";
            public const string CacheOperation = "cache.operation";
        }

        /// <summary>
        /// Standard attribute names for Portal activities following OpenTelemetry conventions.
        /// </summary>
        public static class AttributeNames
        {
            // Message attributes
            public const string MessageId = "messaging.message_id";
            public const string MessageType = "messaging.message_type";
            public const string PartitionKey = "messaging.partition_key";
            public const string DeadLettered = "messaging.dead_lettered";

            // Service attributes
            public const string ServiceName = "service.name";
            public const string ServiceMethod = "service.method";

            // Cache attributes
            public const string CacheKey = "cache.key";
            public const string CacheHit = "cache.hit";

            // Database attributes
            public const string DatabaseQuery = "db.statement";
            public const string DatabaseName = "db.name";
            public const string DatabaseSystem = "db.system";
            public const string DatabaseOperation = "db.operation";
            public const string DatabaseUser = "db.user";

            // MSSQL-specific attributes
            public const string MssqlServer = "db.mssql.server_instance";
            public const string MssqlDatabase = "db.mssql.database";
            public const string MssqlCommandType = "db.mssql.command_type";
            public const string MssqlDurationMs = "db.mssql.duration_ms";
        }
    }
}
