using System.Text.Json.Serialization;

namespace WorkerService;

public class CloudflareZoneResponse
{
    [JsonPropertyName("result")]
    public Zone[]? ZoneAry { get; set; }
}

public class Zone
{

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("zone_id")]
    public string? ZoneId { get; set; }

    [JsonPropertyName("zone_name")]
    public string? ZoneName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("proxied")]
    public bool Proxied { get; set; }

    [JsonPropertyName("ttl")]
    public int Ttl { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
