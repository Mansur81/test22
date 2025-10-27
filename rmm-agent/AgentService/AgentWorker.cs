using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RmmAgent;

public class AgentWorker : BackgroundService
{
    private readonly AgentClient _client;
    private readonly ILogger<AgentWorker> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly SystemInfoCollector _systemInfoCollector;
    private AgentConfig _config;

    public AgentWorker(
        AgentClient client,
        IOptionsMonitor<AgentConfig> optionsMonitor,
        ILogger<AgentWorker> logger,
        IHostEnvironment hostEnvironment,
        SystemInfoCollector systemInfoCollector)
    {
        _client = client;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _systemInfoCollector = systemInfoCollector;
        _config = optionsMonitor.CurrentValue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RMM agent with id {AgentId}", _config.AgentId);
        await LoadExternalConfigAsync(stoppingToken);

        if (string.IsNullOrWhiteSpace(_config.ServerUrl) || string.IsNullOrWhiteSpace(_config.AgentId))
        {
            _logger.LogCritical("ServerUrl and AgentId must be configured before the agent can start");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var payload = await _systemInfoCollector.CreatePayloadAsync(_config, stoppingToken);
                await _client.SendHeartbeatAsync(_config, payload, stoppingToken);
                _logger.LogInformation("Heartbeat sent to {ServerUrl}", _config.ServerUrl);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send heartbeat");
            }

            var delay = TimeSpan.FromSeconds(Math.Max(15, _config.HeartbeatIntervalSeconds));
            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task LoadExternalConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            var basePath = Path.GetDirectoryName(_hostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
            var configPath = Path.Combine(basePath, _config.ConfigFilePath);

            if (!File.Exists(configPath))
            {
                _logger.LogWarning("Configuration file {ConfigPath} not found. Using default configuration.", configPath);
                return;
            }

            await using var stream = File.OpenRead(configPath);
            var config = await JsonSerializer.DeserializeAsync<AgentConfig>(stream, cancellationToken: cancellationToken);
            if (config is null)
            {
                _logger.LogWarning("Configuration file {ConfigPath} is empty or invalid", configPath);
                return;
            }

            _config = MergeConfigs(_config, config);
            _logger.LogInformation("Loaded configuration overrides from {ConfigPath}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to read configuration overrides");
        }
    }

    private static AgentConfig MergeConfigs(AgentConfig baseConfig, AgentConfig overrides)
    {
        return new AgentConfig
        {
            ServerUrl = string.IsNullOrWhiteSpace(overrides.ServerUrl) ? baseConfig.ServerUrl : overrides.ServerUrl,
            AgentId = string.IsNullOrWhiteSpace(overrides.AgentId) ? baseConfig.AgentId : overrides.AgentId,
            ApiKey = string.IsNullOrWhiteSpace(overrides.ApiKey) ? baseConfig.ApiKey : overrides.ApiKey,
            HeartbeatIntervalSeconds = overrides.HeartbeatIntervalSeconds > 0 ? overrides.HeartbeatIntervalSeconds : baseConfig.HeartbeatIntervalSeconds,
            CollectProcesses = overrides.CollectProcesses,
            ConfigFilePath = string.IsNullOrWhiteSpace(overrides.ConfigFilePath) ? baseConfig.ConfigFilePath : overrides.ConfigFilePath
        };
    }
}
