using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TrueFaceConnector;

public sealed class Worker : BackgroundService
{
    private readonly ConnectorOptions _options;
    private readonly ITrueFaceSdkClientFactory _sdkFactory;
    private readonly PunchQueue _queue;
    private readonly ErpNextClient _erpNextClient;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IOptions<ConnectorOptions> options,
        ITrueFaceSdkClientFactory sdkFactory,
        PunchQueue queue,
        ErpNextClient erpNextClient,
        ILogger<Worker> logger)
    {
        _options = options.Value;
        _sdkFactory = sdkFactory;
        _queue = queue;
        _erpNextClient = erpNextClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> tasks = [];
        foreach (DeviceOptions device in _options.Devices.Where(d => d.Enabled))
        {
            tasks.Add(RunDeviceAsync(device, stoppingToken));
        }
        tasks.Add(RunUploaderAsync(stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task RunDeviceAsync(DeviceOptions device, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using ITrueFaceSdkClient client = _sdkFactory.Create(device);
                await client.ConnectAsync(device, stoppingToken);

                DateTime catchupFrom = DateTime.Now.AddMinutes(-Math.Abs(_options.CatchupMinutesOnStartup));
                IReadOnlyList<PunchRecord> catchup = await client.QueryRecordsAsync(catchupFrom, DateTime.Now, stoppingToken);
                foreach (PunchRecord punch in catchup)
                {
                    await _queue.EnqueueAsync(punch, stoppingToken);
                }

                await client.SubscribeAsync(
                    punch => _queue.EnqueueAsync(punch, stoppingToken),
                    stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.PollIntervalSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TrueFace device loop failed for {DeviceId}; retrying.", device.DeviceId);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task RunUploaderAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<QueuedPunch> rows = await _queue.GetBatchAsync(_options.BatchSize, stoppingToken);
            if (rows.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            foreach (IGrouping<string, QueuedPunch> group in rows.GroupBy(row => row.DeviceId))
            {
                try
                {
                    await _erpNextClient.SendPunchesAsync(group.Key, group.Select(row => row.Punch).ToList(), stoppingToken);
                    await _queue.MarkSentAsync(group.Select(row => row.Id), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload TrueFace punch batch for {DeviceId}.", group.Key);
                    await _queue.MarkFailedAsync(group.Select(row => row.Id), stoppingToken);
                }
            }
        }
    }
}
