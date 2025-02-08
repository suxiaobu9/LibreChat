using WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WakeupWorker>();
builder.Services.AddHostedService<CheckCloudflareIPWorker>();
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();
