using System.Text.Json;
using Portal.Models;
using Xunit;

namespace Portal.Tests.Models
{
    public class HomeConfigurationTests
    {
        [Fact]
        public void Deserialize_WithSections_PreservesConfiguration()
        {
            var json = """
                {
                    "Title": "Home",
                    "Description": "Description",
                    "h1Title": "Heading",
                    "h1Subtitle": "Subtitle",
                    "Sections": [
                        {
                            "Section": "Services",
                            "Title": "Section title",
                            "Intro": "Introduction",
                            "FieldLabel": "Field label",
                            "FieldText": "Field text",
                            "ButtonLabel": "Button label",
                            "ButtonText": "Button text",
                            "ExtraInfo": "Extra",
                            "DataFilter": "Service",
                            "Enabled": true,
                            "itemsToShow": 3
                        },
                        { "Section": "Faq" }
                    ]
                }
                """;

            var configuration = JsonSerializer.Deserialize<HomeConfiguration>(json);

            Assert.NotNull(configuration);
            Assert.Equal("Home", configuration.Title);
            Assert.Equal("Description", configuration.Description);
            Assert.Equal("Heading", configuration.h1Title);
            Assert.Equal("Subtitle", configuration.h1Subtitle);
            Assert.NotNull(configuration.Sections);
            Assert.Equal(2, configuration.Sections.Count);
            var section = configuration.Sections[0];
            Assert.Equal("Services", section.Section);
            Assert.Equal("Section title", section.Title);
            Assert.Equal("Introduction", section.Intro);
            Assert.Equal("Field label", section.FieldLabel);
            Assert.Equal("Field text", section.FieldText);
            Assert.Equal("Button label", section.ButtonLabel);
            Assert.Equal("Button text", section.ButtonText);
            Assert.Equal("Extra", section.ExtraInfo);
            Assert.Equal("Service", section.DataFilter);
            Assert.True(section.Enabled);
            Assert.Equal(3, section.itemsToShow);
            Assert.False(configuration.Sections[1].Enabled);
            Assert.Equal(0, configuration.Sections[1].itemsToShow);
        }
    }
}
