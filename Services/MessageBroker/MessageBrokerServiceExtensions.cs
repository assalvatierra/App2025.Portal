using System.Reflection;

namespace Portal.Services.MessageBroker
{
    public static class MessageBrokerServiceExtensions
    {
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
