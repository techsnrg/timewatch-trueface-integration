namespace TrueFaceConnector;

public sealed class PlaceholderTrueFaceSdkClient : ITrueFaceSdkClient
{
    public Task ConnectAsync(DeviceOptions device, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "Compile with TRUEFACE_NETSDK and add the vendor NetSDKCS wrapper/DLLs on Windows to enable device connectivity.");
    }

    public Task<IReadOnlyList<PunchRecord>> QueryRecordsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PunchRecord>>([]);
    }

    public Task SubscribeAsync(Func<PunchRecord, Task> onPunch, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
