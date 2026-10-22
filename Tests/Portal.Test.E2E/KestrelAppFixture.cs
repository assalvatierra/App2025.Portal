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
            var appProjectPath = Path.Combine(testAssemblyPath, "../../../../..");
            var appProjectAbsolutePath = Path.GetFullPath(appProjectPath);
            var appProjectAbsolutePathtmp = "C:\\Code\\Github-2\\App2025.Portal";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --urls=http://localhost:5000",
                WorkingDirectory = appProjectAbsolutePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            _app = Process.Start(startInfo);
            await Task.Delay(5000); // Wait for the app to start
                                    // Wait until Kestrel is ready

            using var http = new HttpClient();
            var timeout = TimeSpan.FromSeconds(60);
            var start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                try
                {
                    var res = await http.GetAsync(BaseUrl);
                    if (res.IsSuccessStatusCode)
                        return;
                }
                catch { }

                await Task.Delay(2000);
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
