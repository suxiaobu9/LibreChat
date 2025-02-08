using System.Text.Json.Serialization;

namespace WorkerService;

public class UpdateDnsRecordModel
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("proxied")]
    public bool Proxied { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }
}
