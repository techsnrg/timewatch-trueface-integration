namespace TrueFaceConnector;

public sealed class ConnectorOptions
{
    public string ErpNextBaseUrl { get; set; } = "";
    public string ApiToken { get; set; } = "";
    public int BatchSize { get; set; } = 25;
    public int PollIntervalSeconds { get; set; } = 30;
    public int CatchupMinutesOnStartup { get; set; } = 1440;
    public string QueueDatabasePath { get; set; } = "trueface-queue.db";
    public List<DeviceOptions> Devices { get; set; } = [];
}

public sealed class DeviceOptions
{
    public string DeviceId { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public int Port { get; set; } = 37777;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Timezone { get; set; } = "Asia/Kolkata";
    public bool Enabled { get; set; } = true;
}
