using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Processor;

namespace GitHubNotifications.Server
{
    public class EventHubProcessor : IHostedService, IDisposable
    {
        private readonly ILogger<EventHubProcessor> _logger;
        private readonly EventProcessorClient _processor;
        private readonly EventDispatcher _dispatcher;

        public EventHubProcessor(
            ILogger<EventHubProcessor> logger,
            EventProcessorClient processor,
            EventDispatcher eventDispatcher)
        {
            _logger = logger;
            _dispatcher = eventDispatcher;
            _processor = processor;
            processor.ProcessErrorAsync += async (args) =>
            {
                _logger.LogError(args.Exception.ToString());
                await Task.Yield();
            };

            processor.ProcessEventAsync += async (args) =>
            {
                await DoWork(args);
            };
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
#if DEBUG
            _logger.LogError("Not starting eventHub processor in DEBUG mode");
            return;
#else
            _logger.LogInformation($"{nameof(EventHubProcessor)} Hosted Service running.");
            await _processor.StartProcessingAsync(stoppingToken);
#endif
        }

        private async Task DoWork(object state)
        {
            ProcessEventArgs args = (ProcessEventArgs)state;

            if (args.HasEvent)
            {
                await _dispatcher.ProcessEvent(args.Data.EventBody.ToString());
                await args.UpdateCheckpointAsync();
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"{nameof(EventHubProcessor)} Hosted Service is stopping.");

            return Task.CompletedTask;
        }

        public void Dispose()
        { }
    }
}