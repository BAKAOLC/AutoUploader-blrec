using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoUploader
{
    public class WebHookServerService : IHostedService
    {
        private const ushort WebHookPort = 6780;
        private const string WebHookPath = "/webhook";

        private readonly ILogger<WebHookServerService> _logger;

        private bool _isRunning;

        public WebHookServerService(ILogger<WebHookServerService> logger)
        {
            _logger        = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _isRunning = true;
            _logger.LogInformation("started");
            BeginHttpListener();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _logger.LogInformation("stopped");
            return Task.CompletedTask;
        }

        private void BeginHttpListener()
        {
            _logger.LogInformation("listening on http://127.0.0.1:{Port}{Path}/", WebHookPort, WebHookPath);
            HttpListener listener = new();
            listener.Prefixes.Add($"http://127.0.0.1:{WebHookPort}{WebHookPath}/");
            listener.Start();
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    HttpListenerContext context = await listener.GetContextAsync().ConfigureAwait(false);
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    if (request.HttpMethod == "POST")
                    {
                        string body = await new StreamReader(request.InputStream).ReadToEndAsync()
                           .ConfigureAwait(false);
                        response.StatusCode = WebHookHandler.Handle(body) ? 200 : 400;
                        response.Close();
                    }
                    else
                    {
                        response.StatusCode = 405;
                        response.Close();
                    }
                }

                listener.Stop();
            });
        }
    }
}
