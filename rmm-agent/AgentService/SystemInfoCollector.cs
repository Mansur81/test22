using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace RmmAgent;

public class SystemInfoCollector
{
    public async Task<AgentPayload> CreatePayloadAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        var memory = GetMemoryInfo();
        var snapshot = new SystemSnapshot
        {
            MachineName = Environment.MachineName,
            OperatingSystem = GetOperatingSystem(),
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            Uptime = GetSystemUptime(),
            CpuLoadPercentage = await GetCpuLoadAsync(cancellationToken),
            MemoryUsedMb = memory.used,
            MemoryTotalMb = memory.total,
            Disks = GetDiskSnapshots().ToArray(),
            Processes = config.CollectProcesses ? GetProcessSnapshots().ToArray() : null
        };

        return new AgentPayload
        {
            AgentId = config.AgentId,
            TimestampUtc = DateTime.UtcNow,
            Snapshot = snapshot
        };
    }

    private static string GetOperatingSystem()
    {
        return $"{Environment.OSVersion.Platform} {Environment.OSVersion.VersionString}";
    }

    private static TimeSpan GetSystemUptime()
    {
        try
        {
            using var uptime = new PerformanceCounter("System", "System Up Time");
            uptime.NextValue();
            return TimeSpan.FromSeconds(uptime.NextValue());
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static async Task<double> GetCpuLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            await Task.Delay(1000, cancellationToken);
            return Math.Round(cpuCounter.NextValue(), 2);
        }
        catch
        {
            return 0;
        }
    }

    private static (double used, double total) GetMemoryInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get().Cast<ManagementObject>())
            {
                var totalKb = Convert.ToDouble(obj["TotalVisibleMemorySize"]);
                var freeKb = Convert.ToDouble(obj["FreePhysicalMemory"]);
                var totalMb = totalKb / 1024;
                var usedMb = (totalKb - freeKb) / 1024;
                return (Math.Round(usedMb, 2), Math.Round(totalMb, 2));
            }
        }
        catch
        {
        }

        return (0, 0);
    }

    private static IEnumerable<DiskSnapshot> GetDiskSnapshots()
    {
        var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady);
        foreach (var drive in drives)
        {
            var totalGb = Math.Round(drive.TotalSize / 1024d / 1024d / 1024d, 2);
            var freeGb = Math.Round(drive.TotalFreeSpace / 1024d / 1024d / 1024d, 2);
            yield return new DiskSnapshot
            {
                Name = drive.Name,
                TotalGb = totalGb,
                FreeGb = freeGb
            };
        }
    }

    private static IEnumerable<ProcessSnapshot> GetProcessSnapshots()
    {
        foreach (var process in Process.GetProcesses())
        {
            double memoryMb = 0;
            try
            {
                memoryMb = Math.Round(process.WorkingSet64 / 1024d / 1024d, 2);
            }
            catch
            {
            }

            yield return new ProcessSnapshot
            {
                Pid = process.Id,
                Name = process.ProcessName,
                MemoryMb = memoryMb
            };
        }
    }
}
