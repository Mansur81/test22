# Cloud RMM Agent MSI Packaging

The Windows agent is published as a `net6.0` worker service that can run as a Windows Service. Use the WiX Toolset to create an MSI installer that installs and registers the service.

## Prerequisites

1. Install [.NET 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
2. Install [WiX Toolset 3.11](https://wixtoolset.org/releases/) and ensure `candle.exe` and `light.exe` are on your `PATH`.

## Build and publish the agent

```powershell
# From the repository root
$publishDir = "$PSScriptRoot/../publish"
dotnet publish ../AgentService/AgentService.csproj -c Release -r win-x64 -o $publishDir --self-contained false
```

Copy `config.example.json` to `config.json` and update the settings if you want default overrides baked into the installer.

## Build the MSI

```powershell
$publishDir = Resolve-Path "$PSScriptRoot/../publish"
$wxs = Resolve-Path "$PSScriptRoot/CloudRmmAgent.wxs"

candle.exe -dPublishFolder=$publishDir.FullName $wxs -out "$PSScriptRoot/CloudRmmAgent.wixobj"
light.exe "$PSScriptRoot/CloudRmmAgent.wixobj" -ext WixUtilExtension.dll -out "$PSScriptRoot/CloudRmmAgent.msi"
```

The resulting `CloudRmmAgent.msi` installs the service to `C:\Program Files\Cloud RMM Agent`. During installation, the service is started automatically and continues to run on startup.

## Silent installation

```powershell
msiexec /i CloudRmmAgent.msi /qn /l*v install.log
```

Use the `/qn` flag for a completely silent deployment suitable for remote management tools.
