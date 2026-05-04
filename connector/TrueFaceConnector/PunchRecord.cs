using System.Text.Json.Serialization;

namespace TrueFaceConnector;

public sealed record PunchRecord
{
    [JsonPropertyName("device_serial")]
    public string DeviceSerial { get; init; } = "";

    [JsonPropertyName("record_number")]
    public string? RecordNumber { get; init; }

    [JsonPropertyName("event_id")]
    public string? EventId { get; init; }

    [JsonPropertyName("user_id")]
    public string UserId { get; init; } = "";

    [JsonPropertyName("card_no")]
    public string? CardNo { get; init; }

    [JsonPropertyName("punch_time")]
    public DateTime PunchTime { get; init; }

    [JsonPropertyName("direction")]
    public string? Direction { get; init; }

    [JsonPropertyName("attendance_state")]
    public string? AttendanceState { get; init; }

    [JsonPropertyName("status")]
    public bool Status { get; init; } = true;

    [JsonPropertyName("open_method")]
    public string? OpenMethod { get; init; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }
}
