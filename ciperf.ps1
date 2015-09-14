<#
.SYNOPSIS
Uploads a drop to Azure storage and kicks off a job in Helix to run the perf tests.

.DESCRIPTION
Uploads a drop to Azure storage and kicks off a job in Helix to run the perf tests.

.PARAMETER BinariesDirectory
Specifies the location of the built binaries.

.PARAMETER Branch
Specifies the name of the branch. Defaults to 'master'

.PARAMETER JobId
Specifies the job identifier. e.g. 'PR1234'. Defaults to 'usernameNNNN' where NNN is determined by the current date and time

.PARAMETER JobType
Specifies the job type. Defaults to 'CIPerf'

.PARAMETER Platform
Specifies the platform name. Defaults to 'Windows'

.PARAMETER Queue
Specifies the Helix queue name. Defaults to 'Windows'

.PARAMETER Repository
Specifies the name of the repository. Defaults to 'Roslyn'

.PARAMETER StorageAccountKey
Azure account key to use for accessing Azure storage to store drops, work items and results.

.PARAMETER StorageAccountName
Azure account name to use for accessing Azure storage to store drops, work items and results.

.PARAMETER StorageContainer
The name of the Azure blob storage container for drops, work items and results

.PARAMETER NoUpload
Don't actually upload anything to blob storage (useful for debugging)

.PARAMETER NoSubmit
Don't submit the job at the end (useful for debugging)

.EXAMPLE 1
C:\PS> .\CIPerf.ps1 -BinariesDirectory "Open\Binaries\Release" -JobId "PR1234" -Queue "RoslynAzureWindows10x86"
#>

param (
    [parameter(Mandatory = $true)]
    [String] $BinariesDirectory,
    [String] $Branch = "master",
    [String] $JobId = $env:USERNAME + "_" + [System.DateTime]::UtcNow.ToString("yyyyMMddThhmmss"),
    [String] $JobType = "CIPerf",
    [String] $Platform = "Windows",
    [String] $Queue = "Windows",
    [String] $Repository = "Roslyn",
    [parameter(Mandatory = $true)]
    [String] $StorageAccountKey,
    [parameter(Mandatory = $true)]
    [String] $StorageAccountName,
    [parameter(Mandatory = $true)]
    [String] $StorageContainer,
    [switch] $NoUpload,
    [switch] $NoSubmit
)

try {

    if (!(Test-Path $BinariesDirectory)) {
        Write-Error "Could not find binaries directory ($BinariesDirectory)"
        exit 1
    }

    $NuGetExe = Join-Path -Path $PSScriptRoot -ChildPath NuGet.exe
    if (!(Test-Path $NuGetExe)) {
        Write-Error "Could not find nuget.exe at $NuGetExe."
        exit 1
    }

    try {
        # Reference the latest version of Microsoft.ServiceBus.dll
        Write-Output "Adding the [Microsoft.ServiceBus.dll] assembly to the script..."

        & $NuGetExe install WindowsAzure.ServiceBus -Version 3.0.2 -NonInteractive -ExcludeVersion -Source https://www.nuget.org/api/v2/ -OutputDirectory $env:TEMP

        $packagesFolder = Join-Path $env:TEMP -ChildPath WindowsAzure.ServiceBus
        $assembly = Get-ChildItem $packagesFolder -Include "Microsoft.ServiceBus.dll" -Recurse
        Add-Type -Path $assembly.FullName

        Write-Output "The [Microsoft.ServiceBus.dll] assembly has been successfully added to the script."
    }
    catch [exception] {
        Write-Error "Could not add the Microsoft.ServiceBus.dll assembly to the script."
        exit 1
    }

    Write-Host "Scanning for performance test assemblies"
    $TestAssemblies = Get-ChildItem -File -Path $BinariesDirectory -Recurse -Filter "*PerformanceTests.dll"
    if ($TestAssemblies.count -le 0) {
        Write-Error "No test assemblies found in $BinariesDirectory"
        exit 1
    }

    $CorrelationId = "$Repository-$Branch-$JobId-$JobType"
    Write-Host "Creating Helix job with CorrelationId $CorrelationId"

    Write-Host "Connecting to Azure storage account"
    $StorageContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $StorageAccountKey

    $HelixStage = Join-Path $env:TEMP -ChildPath Helix
    if (Test-Path $HelixStage) {
        Remove-Item -Recurse -Force $HelixStage
    }

    mkdir $HelixStage > $null

    $DropZip = Join-Path $HelixStage -ChildPath Drop.zip

    Write-Host "Zipping $BinariesDirectory to $DropZip"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($BinariesDirectory, $DropZip, $compressionLevel, $false)

    $BlobRootName = "$Repository/$Branch/$JobId/$JobType"
    $BlobName = $BlobRootName + "_Drop.zip"
    if (!$NoUpload) {
        Write-Host "Uploading drop"
        Set-AzureStorageBlobContent -File $DropZip -Container $StorageContainer -Blob $BlobName -Context $StorageContext
    }

    $StorageContainerRSAS = New-AzureStorageContainerSASToken -Context $StorageContext -Permission r -Container $StorageContainer -StartTime ([System.DateTime]::UtcNow.Date) -ExpiryTime ([System.DateTime]::UtcNow.Date + [System.TimeSpan]::FromDays(7))

    $ub = New-Object System.UriBuilder -ArgumentList $StorageContext.BlobEndPoint
    $ub.Path += $StorageContainer + "/" + $BlobName
    $ub.Query = $StorageContainerRSAS.Substring(1)
    $PayloadUri = $ub.Uri

    Write-Host "Creating work item list"

    $sb = New-Object -TypeName System.Text.StringBuilder
    [void] $sb.AppendLine("[")

    foreach ($TestAssembly in $TestAssemblies) {
        $WorkItemId = $BlobRootName + "/" + $TestAssembly.BaseName
        Write-Host "  " $WorkItemId
        if ($sb.Length -gt 3) { $sb.AppendLine(",") }
        
        [void] $sb.AppendLine("  {")
        [void] $sb.AppendLine("    ""Command"": ""Perf-Run.cmd $TestAssembly"",")
        [void] $sb.AppendLine("    ""CorrelationPayloadUris"": [")
        [void] $sb.AppendLine("    ],")
        [void] $sb.AppendLine("    ""PayloadUri"": ""$PayloadUri"",")
        [void] $sb.AppendLine("    ""WorkItemId"": ""$WorkItemId""")
        [void] $sb.Append("  }")
    }

    [void] $sb.AppendLine()
    [void] $sb.AppendLine("]")

    $WorkItemListJson = Join-Path $HelixStage -ChildPath WorkItemList.json
    [System.IO.File]::WriteAllLines($WorkItemListJson, $sb.ToString())

    $BlobName = $BlobRootName + "_WorkItemList.json"
    if (!$NoUpload) {
        Write-Host "Uploading work item list"
        Set-AzureStorageBlobContent -File $WorkItemListJson -Container $StorageContainer -Blob $BlobName -Context $StorageContext
    }

    $ub = New-Object System.UriBuilder -ArgumentList $StorageContext.BlobEndPoint
    $ub.Path = $StorageContainer + "/" + $BlobName
    $ub.Query = $StorageContainerRSAS.Substring(1)
    $ListUri = $ub.Uri
    
    # Using the same storage account and container for results as for the payload
    $ub = New-Object System.UriBuilder -ArgumentList $StorageContext.BlobEndPoint
    $ub.Path = $StorageContainer
    $ResultsUri = $ub.Uri

    $ResultsUriRSAS =  $StorageContainerRSAS
    $ResultsUriWSAS = New-AzureStorageContainerSASToken -Context $StorageContext -Permission w -Container $StorageContainer -StartTime ([System.DateTime]::UtcNow.Date) -ExpiryTime ([System.DateTime]::UtcNow.Date + [System.TimeSpan]::FromDays(7))

    # Create and upload Job.json
    Write-Host "Creating job event"
    $sb = New-Object -TypeName System.Text.StringBuilder

    [void] $sb.AppendLine("{")
    [void] $sb.AppendLine("  ""CorrelationId"": ""$CorrelationId"",")
    [void] $sb.AppendLine("  ""DropContainerSAS"": ""$StorageContainerRSAS"",")
    [void] $sb.AppendLine("  ""ListUri"": ""$ListUri"",")
    [void] $sb.AppendLine("  ""QueueId"": ""$Queue"",")
    [void] $sb.AppendLine("  ""ResultsUri"": ""$ResultsUri"",")
    [void] $sb.AppendLine("  ""ResultsUriRSAS"": ""$ResultsUriRSAS"",")
    [void] $sb.AppendLine("  ""ResultsUriWSAS"": ""$ResultsUriWSAS""")
    [void] $sb.AppendLine("}")

    $JobJson = Join-Path $HelixStage -ChildPath Job.json
    [System.IO.File]::WriteAllLines($JobJson, $sb.ToString())

    # We're uploading the job event to storage only for archival/debugging purposes. It's not actually needed by Helix
    $BlobName = $BlobRootName + "_" + $Queue + ".json"
    if (!$NoUpload) {
        Write-Host "Uploading job event (for archive)"
        Set-AzureStorageBlobContent -File $JobJson -Container $StorageContainer -Blob $BlobName -Context $StorageContext
    }

    # Submit event to the EventBus
    if (!$NoSubmit) {
        Write-Host "Submitting job to event hub."
        $EventHubConnectionString = "Endpoint=sb://dotnethelix.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=qVhbrtIm7tmmYKohdYht0MspbuPvnf7huE5d5U8lPGE="
        $EventHubEntityPath = "controler" # Sic: one 'l' in controler
        $EventHubClient = [Microsoft.ServiceBus.Messaging.EventHubClient]::CreateFromConnectionString($EventHubConnectionString, $EventHubEntityPath)
        $JobJsonStream = [System.IO.File]::Open($JobJson, [System.IO.FileMode]::Open)
        $EventData = New-Object Microsoft.ServiceBus.Messaging.EventData -ArgumentList $JobJsonStream
        $EventHubClient.Send($EventData)
        $JobJsonStream.Dispose()
    }

    exit 0
}
catch [exception] {
    Write-Error -Exception $_.Exception
    exit 1
}
