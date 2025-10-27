using System.Collections.Generic;

namespace RmmAgent;

public record AgentPayload
{
    public required string AgentId { get; init; }
    public required DateTime TimestampUtc { get; init; }
    public required SystemSnapshot Snapshot { get; init; }
}

public record SystemSnapshot
{
    public required string MachineName { get; init; }
    public required string OperatingSystem { get; init; }
    public required string Architecture { get; init; }
    public required TimeSpan Uptime { get; init; }
    public required double CpuLoadPercentage { get; init; }
    public required double MemoryUsedMb { get; init; }
    public required double MemoryTotalMb { get; init; }
    public required IEnumerable<DiskSnapshot> Disks { get; init; }
    public required IEnumerable<ProcessSnapshot>? Processes { get; init; }
}

public record DiskSnapshot
{
    public required string Name { get; init; }
    public required double TotalGb { get; init; }
    public required double FreeGb { get; init; }
}

public record ProcessSnapshot
{
    public required int Pid { get; init; }
    public required string Name { get; init; }
    public required double MemoryMb { get; init; }
}
