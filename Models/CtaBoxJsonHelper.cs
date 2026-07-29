using System.Text.Json;

namespace App2025.Portal.Models
{
    /// <summary>
    /// Helper class for serializing and deserializing CtaBoxViewModel from JSON
    /// </summary>
    public static class CtaBoxJsonHelper
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        /// <summary>
        /// Deserialize JSON string to CtaBoxViewModel
        /// </summary>
        public static CtaBoxViewModel FromJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return new CtaBoxViewModel();

            return JsonSerializer.Deserialize<CtaBoxViewModel>(jsonString, JsonOptions);
        }

        /// <summary>
        /// Serialize CtaBoxViewModel to JSON string
        /// </summary>
        public static string ToJson(CtaBoxViewModel model)
        {
            if (model == null)
                return string.Empty;

            return JsonSerializer.Serialize(model, JsonOptions);
        }
    }
}
