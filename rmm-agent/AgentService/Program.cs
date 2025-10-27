using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RmmAgent;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using IHost host = Host.CreateDefaultBuilder(args)
            .UseWindowsService(options => { options.ServiceName = "CloudRmmAgent"; })
            .ConfigureServices((context, services) =>
            {
                services.Configure<AgentConfig>(context.Configuration.GetSection("Agent"));
                services.AddHttpClient<AgentClient>()
                    .AddPolicyHandler(AgentClient.CreateRetryPolicy());
                services.AddSingleton<AgentClient>();
                services.AddSingleton<SystemInfoCollector>();
                services.AddHostedService<AgentWorker>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
            })
            .Build();

        await host.RunAsync();
    }
}
