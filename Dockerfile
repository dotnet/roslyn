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
    Start-Process msiexec.exe -Wait -ArgumentList '/I PowerShell.msi /quiet'; `
    Remove-Item PowerShell.msi

# Copy Visual Studio configuration
COPY docker.vsconfig /docker.vsconfig

# Install Visual Studio Build Tools.
# This is necessary, otherwise eng/build.ps will attempt to download them, and will fail because it depends on Microsoft's private resources.
# TODO: The following downloads the latest version. Make sure it downloads a specific version.
RUN Invoke-WebRequest -Uri https://aka.ms/vs/17/release/vs_buildtools.exe -OutFile vs_buildtools.exe; `
    Start-Process -Wait -FilePath vs_buildtools.exe -ArgumentList '--wait', '--quiet', '--config', 'C:\docker.vsconfig', '--norestart', '--locale en-US'; `
    Remove-Item C:\\vs_buildtools.exe; `
    Remove-Item C:\\docker.vsconfig

# Download the .NET installer. We will need it several times.
RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; 

# Install .NET SDK 9 for PostSharp.Engineering.
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 9.0.304 -InstallDir 'C:\Program Files\dotnet'; 

# Install .NET SDK 10.0.100-preview.6.25358.103 - Must match global.json.
RUN powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 10.0.100-preview.6.25358.103 -InstallDir 'C:\Program Files\dotnet'; 

# Clean up.
RUN Remove-Item C:\\dotnet-install.ps1

# Enable long path support
RUN Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name 'LongPathsEnabled' -Value 1

# Add to PATH
RUN cmd /c "setx PATH \"$env:PATH;C:\\Program Files\\PowerShell\\7;C:\\git\\cmd;C:\\git\\bin;C:\\git\\usr\\bin;C:\\Program Files\\dotnet\" /M"

# Prepare environment
ENV PSExecutionPolicyPreference=Bypass
ENV POWERSHELL_UPDATECHECK=FALSE

# Create NuGet cache directory and set environment variable
RUN New-Item -ItemType Directory -Path C:\nuget -Force | Out-Null
ENV NUGET_PACKAGES=C:\nuget


# Configure git
RUN New-Item -ItemType Directory -Path C:\src -Force | Out-Null; `
    git config --global --add safe.directory C:/src/; `
    New-Item -ItemType Directory -Path C:\BuildAgent\system\git -Force | Out-Null
