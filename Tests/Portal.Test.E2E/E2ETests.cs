using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace Portal.Test.E2E
{
    public class E2ETests:IClassFixture<KestrelAppFixture>
    {
        private readonly KestrelAppFixture _fixture;
        public E2ETests(KestrelAppFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [InlineData("/", "Davao Car Rental")]
        [InlineData("/Home/OurProducts", "Car Rental Fleet")]
        [InlineData("/Home/OurServices", "Car Rental Services")]
        [InlineData("/Home/Articles", "Car Rental Articles")]
        public async Task ShouldOpenPage(string url, string expectedTitle)
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}{url}");

            Assert.Contains(expectedTitle, await page.TitleAsync());
        }


        [Fact]
        public async Task ShouldOpenOurProductsPage()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/Home/OurProducts");
            Assert.Contains("Car Rental Fleet", await page.TitleAsync());
        }

        [Fact]
        public async Task ShouldOpenOurServicesPage()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/Home/OurServices");
            Assert.Contains("Car Rental Services", await page.TitleAsync());
        }

        [Fact]
        public async Task ShouldOpenArticlesPage()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            var page = await browser.NewPageAsync();
            await page.GotoAsync($"{_fixture.BaseUrl}/Home/Articles");
            Assert.Contains("Car Rental Articles", await page.TitleAsync());
        }

    }
}
