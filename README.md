# test22

This repository now includes a production-ready Remote Monitoring and Management (RMM) Windows agent alongside the sample front-end assets. The agent is implemented as a .NET 6 worker service that runs as a Windows Service, publishes device health metrics to your cloud RMM endpoint, and can be packaged into an MSI for streamlined deployment.

## Cloud RMM Agent

The agent source code lives in [`rmm-agent/AgentService`](rmm-agent/AgentService). Key features include:

- Configurable cloud endpoint URL, agent identifier, API key, and heartbeat frequency via `appsettings.json` or an optional `config.json` override file.
- Heartbeat payload with device name, OS details, uptime, CPU load, memory usage, fixed-disk capacity, and (optionally) the top-level process list.
- Resilient HTTP client with retry logic and structured logging.

### Building

```bash
dotnet restore rmm-agent/AgentService/AgentService.csproj
dotnet publish rmm-agent/AgentService/AgentService.csproj -c Release -r win-x64 --self-contained false
```

### MSI packaging

Instructions for bundling the agent into an MSI installer are provided in [`rmm-agent/installer/README.md`](rmm-agent/installer/README.md). The WiX definition (`CloudRmmAgent.wxs`) installs the service, copies configuration files, and supports silent deployment for remote rollouts.
