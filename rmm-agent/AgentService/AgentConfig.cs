namespace RmmAgent;

public class AgentConfig
{
    public string ServerUrl { get; set; } = string.Empty;

    public string AgentId { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public int HeartbeatIntervalSeconds { get; set; } = 60;

    public bool CollectProcesses { get; set; } = false;

    public string ConfigFilePath { get; set; } = "config.json";
}
