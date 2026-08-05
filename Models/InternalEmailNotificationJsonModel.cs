
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Portal.Models
{
    public class InternalEmailNotificationJsonModel
    {
        public string InternalNotificationEmail { get; set; }
        public string InternalNotificationEmailSubject { get; set; }
        public string InternalNotificationEmailTitle { get; set; }
        public string InternalNotificationEmailMessage { get; set; }
        public string ProceedToPayment { get; set; }

        public string OtpTimeout { get; set; }

        [JsonConverter(typeof(InternalEmailNotificationJsonModelStringConverter))]
        public string InternalNotificationEmails { get; set; }
    }



    public class InternalEmailNotificationJsonModelStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var jsonDoc = JsonDocument.ParseValue(ref reader);
            return jsonDoc.RootElement.GetRawText();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            using var document = JsonDocument.Parse(value);
            document.RootElement.WriteTo(writer);
        }
    }

}
