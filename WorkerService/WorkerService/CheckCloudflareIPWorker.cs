using System.Net.Http.Json;

namespace WorkerService
{
    public class CheckCloudflareIPWorker : BackgroundService
    {
        public CheckCloudflareIPWorker(
            ILogger<CheckCloudflareIPWorker> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            hostNames = configuration["Duc:HostNames"] ?? "";
            zoneId = configuration["Duc:ZoneId"] ?? "";
            delayMin = configuration.GetValue<int>("Duc:DelayMinutes");
            this.logger = logger;
            this.configuration = configuration;

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Auth-Email", configuration["Duc:username"] ?? "");
            client.DefaultRequestHeaders.Add("X-Auth-Key", configuration["Duc:password"] ?? "");
            httpClient = client;
        }
        private readonly string hostNames;
        private readonly string zoneId;
        private readonly int delayMin;
        private readonly HttpClient httpClient;
        private readonly ILogger<CheckCloudflareIPWorker> logger;
        private readonly IConfiguration configuration;
        private string oldIp = "";

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrWhiteSpace(hostNames))
            {
                return;
            }

            logger.LogInformation("Duc start.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ChangeDnsIp(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(delayMin), stoppingToken);
            }

            logger.LogInformation("Duc end.");

            httpClient.Dispose();
        }

        private async Task ChangeDnsIp(CancellationToken stoppingToken)
        {
            try
            {
                // 取得目前的公開IP位址
                var currentIpTask = GetPublicIpAddress(stoppingToken);

                var getDnsRecordsApi = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records";

                using var getZoneResponse = await httpClient.GetAsync(getDnsRecordsApi, stoppingToken);

                var zonesResult = await getZoneResponse.Content.ReadFromJsonAsync<CloudflareZoneResponse>(cancellationToken: stoppingToken);

                if (zonesResult?.ZoneAry == null)
                {
                    logger.LogWarning("Dns records get null.");
                    return;
                }

                var currentIp = await currentIpTask;

                if (!string.IsNullOrWhiteSpace(currentIp) && currentIp == oldIp)
                {
                    logger.LogInformation("IP address unchanged.");
                    return;
                }

                var hostNames = this.hostNames.Split(",");

                if (hostNames == null || hostNames.Length == 0)
                {
                    logger.LogWarning("Host name get null.");
                    return;
                }

                List<Task<HttpResponseMessage>> tasks = [];

                foreach (var dnsRecord in zonesResult.ZoneAry)
                {
                    if (!hostNames.Any(x => x == dnsRecord.Name))
                        continue;

                    var updateDnsApi = $"https://api.cloudflare.com/client/v4/zones/{zoneId}/dns_records/{dnsRecord.Id}";

                    var model = new UpdateDnsRecordModel
                    {
                        Content = currentIp,
                        Name = dnsRecord.Name,
                        Proxied = dnsRecord.Proxied,
                        Ttl = dnsRecord.Ttl,
                        Type = dnsRecord.Type
                    };

                    tasks.Add(httpClient.PutAsJsonAsync(updateDnsApi, model, cancellationToken: stoppingToken));
                }

                var responses = await Task.WhenAll(tasks);

                foreach (HttpResponseMessage response in responses)
                {
                    using (response)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        string url = response.RequestMessage!.RequestUri!.ToString();
                        var content = await response.Content.ReadAsStringAsync(stoppingToken);

                        logger.LogWarning("Update dns fail.{url} {content}", url, content);
                    }
                }
                oldIp = currentIp;

                logger.LogInformation("IP address changed to {ip}.", currentIp);

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Duc get exception.");
            }
        }

        private async Task<string> GetPublicIpAddress(CancellationToken stoppingToken)
        {
            var apiUrlAry = new string[]
            {
                "https://checkip.amazonaws.com",
                "https://ifconfig.co/ip",
                "https://ipinfo.io/ip"
            };

            foreach (var url in apiUrlAry)
            {
                using HttpResponseMessage response = await httpClient.GetAsync(url, stoppingToken);
                string content = await response.Content.ReadAsStringAsync(stoppingToken);

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Get ip error.{content}", content);
                    continue;
                }

                return content.Trim();
            }

            throw new Exception("Failed to retrieve public IP address.");
        }
    }
}
