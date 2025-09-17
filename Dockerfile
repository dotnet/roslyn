# escape=`

FROM mcr.microsoft.com/windows/servercore:ltsc2025

# The initial shell is PowerShell Desktop.
SHELL ["powershell", "-Command"]

# Download and install Git for Windows.
RUN Invoke-WebRequest -Uri https://github.com/git-for-windows/git/releases/download/v2.50.0.windows.1/MinGit-2.50.0-64-bit.zip -OutFile MinGit.zip; `
    Expand-Archive c:\\MinGit.zip -DestinationPath C:\\git; `
    Remove-Item C:\\MinGit.zip

# Install PowerShell Core.
RUN Invoke-WebRequest -Uri https://github.com/PowerShell/PowerShell/releases/download/v7.5.2/PowerShell-7.5.2-win-x64.msi -OutFile PowerShell.msi; `
    $process = Start-Process msiexec.exe -Wait -PassThru -ArgumentList '/I PowerShell.msi /quiet'; `
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }; `
    Remove-Item PowerShell.msi

# Download the .NET installer. We will need it several times.
RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; 

# Install .NET SDK 9 for PostSharp.Engineering.
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 9.0.304 -InstallDir 'C:\Program Files\dotnet'; 

# Install .NET SDK 10.0.100-preview.6.25358.103 - Must match global.json.
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 10.0.100-preview.6.25358.103 -InstallDir 'C:\Program Files\dotnet'; 


# Copy Visual Studio configuration
COPY vsconfig.json /vsconfig.json

# Install Visual Studio Build Tools.
# To find a version, inspect the JSON file https://aka.ms/vs/17/release/channel and choose the payload URL.
RUN Invoke-WebRequest -Uri https://aka.ms/vs/17/release/vs_buildtools.exe -OutFile vs_buildtools.exe; `
    $process = Start-Process .\vs_buildtools.exe -NoNewWindow -Wait -PassThru `
        -ArgumentList  "--quiet", "--wait", "--norestart", "--nocache",  "--installPath", "C:\BuildTools", "--installChannelUri", "https://aka.ms/vs/17/release/channel", "--installCatalogUri", "https://download.visualstudio.microsoft.com/download/pr/c2e2845d-bdff-44fc-ac00-3d488e9f5675/dc1d78c601c2839b8099ef634ff1f8304b1bd26a2dd485e3b2a70d12f7f9ae7c/VisualStudio.vsman", "--productId", "Microsoft.VisualStudio.Product.BuildTools", "--config", "\vsconfig.json"; `
        Get-ChildItem "$env:TEMP\dd_*.log" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "=== Contents of $($_.Name) ==="; Get-Content $_.FullName; Write-Host "=== End of $($_.Name) ===" }; `
    if ($process.ExitCode -ne 0) { exit $process.ExitCode }; `
    Remove-Item C:\\vs_buildtools.exe;


# Clean up.
RUN Remove-Item C:\\dotnet-install.ps1

# Enable long path support
RUN Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1

# Add to PATH
RUN cmd /c "setx PATH \"$env:PATH;C:\\Program Files\\PowerShell\\7;C:\\git\\cmd;C:\\git\\bin;C:\\git\\usr\\bin;C:\\Program Files\\dotnet\" /M"

# Prepare environment
ENV PSExecutionPolicyPreference=Bypass
ENV POWERSHELL_UPDATECHECK=FALSE

##################################################################################################################################
## The following is required for integration with DockerBuild.ps1 and PostSharp.Engineering. It should not be modified.         ##


# Create directories for mountpoints
ARG MOUNTPOINTS
RUN if ($env:MOUNTPOINTS) { `
        $mounts = $env:MOUNTPOINTS -split ';'; `
        foreach ($dir in $mounts) { `
            if ($dir) { `
                Write-Host "Creating directory $dir`."; `
                New-Item -ItemType Directory -Path $dir -Force | Out-Null; `
            } `
        } `
    }

# Import secrets
COPY ReadSecrets.ps1 c:\ReadSecrets.ps1    
COPY secrets.g.json c:\secrets.g.json
RUN c:\ReadSecrets.ps1 c:\secrets.g.json   

# Configure NuGet
ENV NUGET_PACKAGES=c:\packages

# Configure git
ARG SRC_DIR
RUN git config --global --add safe.directory $env:SRC_DIR/

##                                                                                                                              ##
##################################################################################################################################