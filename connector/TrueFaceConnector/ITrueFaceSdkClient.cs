namespace TrueFaceConnector;

public interface ITrueFaceSdkClient : IAsyncDisposable
{
    Task ConnectAsync(DeviceOptions device, CancellationToken cancellationToken);
    Task<IReadOnlyList<PunchRecord>> QueryRecordsAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task SubscribeAsync(Func<PunchRecord, Task> onPunch, CancellationToken cancellationToken);
}

public interface ITrueFaceSdkClientFactory
{
    ITrueFaceSdkClient Create(DeviceOptions device);
}

public sealed class TrueFaceSdkClientFactory : ITrueFaceSdkClientFactory
{
    public ITrueFaceSdkClient Create(DeviceOptions device)
    {
#if TRUEFACE_NETSDK
        return new NetSdkTrueFaceClient();
#else
        return new PlaceholderTrueFaceSdkClient();
#endif
    }
}
