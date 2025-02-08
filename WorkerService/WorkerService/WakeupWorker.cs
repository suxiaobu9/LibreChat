namespace WorkerService;

public class WakeupWorker(
    ILogger<WakeupWorker> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Wake up host start.");

        var hostAddress = configuration["Wakeup:HostAddress"];
        var delayMin = configuration.GetValue<int>("Wakeup:DelayMinutes");

        if (delayMin <= 0)
        {
            delayMin = 1;
        }

        if (string.IsNullOrWhiteSpace(hostAddress))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                using var response = await httpClient.PostAsync(hostAddress, new StringContent(""), stoppingToken);

                var content = await response.Content.ReadAsStringAsync(stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Wake up host success.");
                }
                else
                {
                    logger.LogInformation("Wake up host fail.{content}", content);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Wake up get exception.");
            }

            await Task.Delay(TimeSpan.FromMinutes(delayMin), stoppingToken);

        }

        logger.LogInformation("Wake up host end.");
    }
}
