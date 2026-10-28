using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Portal.Test.E2E
{
    public class KestrelAppFixture : IAsyncLifetime
    {
        private Process? _app;
        public string BaseUrl => "http://localhost:5000";

        public async Task InitializeAsync()
        {
            // Get the absolute path to the Portal project
            var testAssemblyPath = Path.GetDirectoryName(typeof(KestrelAppFixture).Assembly.Location);
            var appProjectPath = Path.Combine(testAssemblyPath??"", "../../../../..");
            var appProjectAbsolutePath = Path.GetFullPath(appProjectPath);
            //var appProjectAbsolutePathtmp = "C:\\Code\\Github-2\\App2025.Portal";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --no-build --urls=http://localhost:5000",
                WorkingDirectory = appProjectAbsolutePath,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            _app = Process.Start(startInfo);

            //var startInfo = new ProcessStartInfo
            //{
            //    FileName = "dotnet",
            //    Arguments = $"{appProjectAbsolutePath}/bin/Release/net8.0/YourApp.dll --urls=http://localhost:55006",
            //    WorkingDirectory = appProjectAbsolutePath,
            //    RedirectStandardOutput = false,
            //    RedirectStandardError = false,
            //    UseShellExecute = true,
            //    CreateNoWindow = false
            //};
            //_app = Process.Start(startInfo);

            //var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            //{
            //    ContentRootPath = appProjectAbsolutePath,
            //    Args = new[] { "--urls=http://localhost:55006" }
            //});
            //var app = builder.Build();
            //await app.StartAsync();


            await Task.Delay(30000); // Wait for the app to start
                                    // Wait until Kestrel is ready

               using var http = new HttpClient();
            var timeout = TimeSpan.FromSeconds(600);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                try
                {
                    var res = await http.GetAsync(BaseUrl);
                    if (res.IsSuccessStatusCode)
                        return;
                }
                catch(Exception e) 
                {
                    Console.WriteLine(e);
                }

                await Task.Delay(3000);
            }

            throw new Exception("Web app did not start in time.");
        }

        public Task DisposeAsync()
        {
            if (_app != null && !_app.HasExited)
            {
                _app.Kill();
                _app.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}
