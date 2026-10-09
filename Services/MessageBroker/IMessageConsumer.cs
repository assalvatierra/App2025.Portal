namespace Portal.Services.MessageBroker
{
    /// <summary>
    /// Handles broker messages of a single <see cref="MessageType"/>.
    /// Implementations are discovered via DI and dispatched by <see cref="BrokerMessageConsumer"/>.
    /// </summary>
    public interface IMessageConsumer
    {
        /// <summary>
        /// The <see cref="BrokerMessage.MessageType"/> this consumer handles.
        /// </summary>
        string MessageType { get; }

        Task ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default);
    }
}
