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

        [Fact]
        public async Task ShouldOpenHomePage()
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });

            var page = await browser.NewPageAsync();
            await page.GotoAsync(_fixture.BaseUrl);

            Assert.Contains("Davao Car Rental", await page.TitleAsync());
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
