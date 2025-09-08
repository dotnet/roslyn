# escape=`

FROM mcr.microsoft.com/windows/servercore:ltsc2025

# The initial shell is PowerShell Desktop.
SHELL ["powershell", "-Command"]

# Download and install Git for Windows
RUN Invoke-WebRequest -Uri https://github.com/git-for-windows/git/releases/download/v2.50.0.windows.1/MinGit-2.50.0-64-bit.zip -OutFile MinGit.zip; `
    Expand-Archive c:\\MinGit.zip -DestinationPath C:\\git; `
    Remove-Item C:\\MinGit.zip

# Install PowerShell Core
RUN Invoke-WebRequest -Uri https://github.com/PowerShell/PowerShell/releases/download/v7.5.2/PowerShell-7.5.2-win-x64.msi -OutFile PowerShell.msi; `
    Start-Process msiexec.exe -Wait -ArgumentList '/I PowerShell.msi /quiet'; `
    Remove-Item PowerShell.msi

# Install .NET SDK 10.0.100-preview.6.25358.103 - Must match global.json
RUN Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1; `
    powershell -ExecutionPolicy Bypass -File dotnet-install.ps1 -Version 10.0.100-preview.6.25358.103 -InstallDir 'C:\Program Files\dotnet'; `
    Remove-Item C:\\dotnet-install.ps1

RUN $env:PATH

# Add to PATH
ENV CUSTOM_PATH="C:\\Program Files\\PowerShell\\7;C:\\git\\cmd;C:\\git\\bin;C:\\git\\usr\\bin"
RUN cmd /c "setx PATH \"$env:CUSTOM_PATH;$env:PATH\" /M"

# Prepare PostSharp environment
ENV PSExecutionPolicyPreference=Bypass
ENV POWERSHELL_UPDATECHECK=FALSE
