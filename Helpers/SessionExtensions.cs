using System.Text.Json;

namespace Portal.Helpers
{
    public static class SessionExtensions
    {
        /// <summary>
        /// Set an object to session by serializing it to JSON
        /// </summary>
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            var jsonValue = JsonSerializer.Serialize(value);
            session.SetString(key, jsonValue);
        }

        /// <summary>
        /// Get an object from session by deserializing it from JSON
        /// </summary>
        public static T? GetObject<T>(this ISession session, string key)
        {
            var jsonValue = session.GetString(key);
            return jsonValue == null ? default : JsonSerializer.Deserialize<T>(jsonValue);
        }
    }
}
