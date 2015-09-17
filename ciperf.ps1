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

.PARAMETER SubmitConnectionString
The connection string to use when submitting the final job to Helix

.PARAMETER SCRAMScope
The scope to use if connection strings and account keys are to be retrieved from SCRAM

.PARAMETER UseDevelopmentStorage
Use the Azure Storage Emulator on the local machine for storage. Useful for testing. The job cannot be submitted.

.EXAMPLE
C:\PS> .\CIPerf.ps1 -BinariesDirectory D:\Roslyn\Open\Binaries\Release -StorageAccountName roslyn -StorageContainer drops -SCRAMScope Azure
Typical usage. Uses SCRAM to Azure credentials. Packages up the drop, uploads it to Azure storage, and kicks off a performance run.

.EXAMPLE
C:\PS> .\CIPerf.ps1 -UseDevelopmentStorage -BinariesDirectory D:\Roslyn\Open\Binaries\Release -StorageContainer test
Test this script using the Azure Storage Emulator. All generated artifacts are uploaded to development storage but no job is submitted
to Helix.
#>

param (
    [Parameter(ParameterSetName='namedStorage_Submit_SCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_Submit_NoSCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_NoSubmit_SCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_NoSubmit_NoSCRAM', Mandatory = $true)]
    [String] $StorageAccountName,
    [Parameter(ParameterSetName='namedStorage_Submit_NoSCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_NoSubmit_NoSCRAM', Mandatory = $true)]
    [String] $StorageAccountKey,
    [Parameter(ParameterSetName='namedStorage_Submit_SCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_NoSubmit_SCRAM', Mandatory = $true)]
    [String] $SCRAMScope,
    [Parameter(ParameterSetName='devStorage', Mandatory = $true)]
    [switch] $UseDevelopmentStorage,
    [parameter(Mandatory = $true)]
    [String] $StorageContainer,
    [switch] $NoUpload,
    [Parameter(ParameterSetName='namedStorage_NoSubmit_SCRAM', Mandatory = $true)]
    [Parameter(ParameterSetName='namedStorage_NoSubmit_NoSCRAM', Mandatory = $true)]
    [switch] $NoSubmit,
    [Parameter(ParameterSetName='namedStorage_Submit_NoSCRAM', Mandatory = $true)]
    [String] $SubmitConnectionString,
    [parameter(Mandatory = $true)]
    [String] $BinariesDirectory,
    [String] $Repository = "Roslyn",
    [String] $Branch = "master",
    [String] $JobId = $env:USERNAME + "_" + [System.DateTime]::UtcNow.ToString("yyyyMMddTHHmmss"),
    [String] $JobType = "CIPerf",
    [String] $Platform = "Windows",
    [String] $Queue = "Windows"
)

# Create a new SAS token, but don't include the leading question mark
function CreateSASToken(
    [Parameter(Mandatory = $true)]
    [Microsoft.WindowsAzure.Commands.Common.Storage.AzureStorageContext] $Context,
    [Parameter(Mandatory = $true)]
    [String] $Container,
    [String] $Permission = "r",
    [System.TimeSpan] $Duration = [System.TimeSpan]::FromDays(7)
    ) {

    $expiryTime = [System.DateTime]::UtcNow.Date + $Duration
    $token = New-AzureStorageContainerSASToken -Context $Context -Permission $Permission -Container $Container -ExpiryTime $expiryTime

    # SAS tokens in Helix should not include the query char
    if ($token[0] -eq '?') {
      $token = $token.Substring(1)
    }

    return $token
}

# Construct a full URI with a SAS token for the given storage context and blob name
function BuildUri(
    [Parameter(Mandatory = $true)]
    [Microsoft.WindowsAzure.Commands.Common.Storage.AzureStorageContext] $Context,
    [Parameter(Mandatory = $true)]
    [String] $Container,
    [String] $BlobName,
    [String] $SASToken
) {

    $ub = New-Object System.UriBuilder -ArgumentList $Context.BlobEndPoint
    if ([System.String]::IsNullOrEmpty($BlobName)) {
        $ub.Path = $Container
    } else {
        $ub.Path += $Container + "/" + $BlobName
    }
    $ub.Query = $SASToken
    
    # Using OriginalString because that preserves escaping in the query string
    return $ub.Uri.OriginalString
}


function CreateDrop(
    [Parameter(Mandatory = $true)]
    [String] $Binaries,
    [Parameter(Mandatory = $true)]
    [String] $ZipFile
    ) {

    Write-Host "Zipping $Binaries to $ZipFile"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($Binaries, $ZipFile, $compressionLevel, $false)
}

function CreateXUnitFixture(
    [Parameter(Mandatory = $true)]
    [String] $StagingPath,
    [Parameter(Mandatory = $true)]
    [String] $ZipFile
    ) {
    
    $PackagesPath = Join-Path -Path $StagingPath -ChildPath Packages
    & $NuGetExe install -OutputDirectory $PackagesPath -NonInteractive -ExcludeVersion xunit.runner.console -Version 2.1.0-beta4-build3109 -Source https://www.nuget.org/api/v2/
    & $NuGetExe install -OutputDirectory $PackagesPath -NonInteractive -ExcludeVersion Microsoft.DotNet.xunit.performance.runner.Windows -Version 1.0.0-alpha-build0013 -Source https://www.myget.org/F/dotnet-buildtools/

    $ToZipPath = Join-Path $StagingPath -ChildPath ToZip
    mkdir $ToZipPath > $null

    # Move the contents of all "Tools" folders into the root of the archive (overwriting any duplicates)
    (Get-ChildItem -Path $PackagesPath -Recurse -Directory -Include "Tools").FullName | Get-ChildItem | Move-Item -Destination $ToZipPath -Force

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory($ToZipPath, $ZipFile, $compressionLevel, $false)
}

function GetXUnitFixtureUri(
    [Parameter(Mandatory = $true)]
    [string] $BlobName,
    [Parameter(Mandatory = $true)]
    [Microsoft.WindowsAzure.Commands.Common.Storage.AzureStorageContext] $StorageContext,
    [Parameter(Mandatory = $true)]
    [String] $StorageContainer,
    [Parameter(Mandatory = $true)]
    [String] $StorageContainerRSAS,
    [Parameter(Mandatory = $true)]
    [String] $HelixStage
   ) {

    # Look for xunit.zip in the "fixtures" folder and create it if it doesn't already exist
    $xunitBlob = Get-AzureStorageBlob -Container $StorageContainer -Blob $BlobName -Context $StorageContext -ErrorAction Ignore
    if ($xunitBlob -eq $null) {

        Write-Host "Xunit fixture is missing. Creating it and uploading to storage..."
        
        $FixturesStagingPath = Join-Path -Path $HelixStage -ChildPath fixtures
        $xunitZip = Join-Path $FixturesStagingPath -ChildPath xunit.zip

        CreateXUnitFixture -StagingPath $FixturesStagingPath -ZipFile $xunitZip

        if (!$NoUpload) {
            Write-Host "Uploading xunit fixture"
            Set-AzureStorageBlobContent -File $xunitZip -Container $StorageContainer -Blob $BlobName -Context $StorageContext
        }
    }

    $XunitFixtureUri = BuildUri -Context $StorageContext -Container $StorageContainer -BlobName $BlobName -SASToken $StorageContainerRSAS
}

function SubmitJobToHelix(
    [Parameter(Mandatory = $true)]
    [string] $JobJsonPath,
    [Parameter(Mandatory = $true)]
    [string] $EventHubConnectionString
    ) {

    Write-Host "Submitting job to event hub."
    $EventHubClient = [Microsoft.ServiceBus.Messaging.EventHubClient]::CreateFromConnectionString($EventHubConnectionString)
    $JobJsonStream = [System.IO.File]::Open($JobJsonPath, [System.IO.FileMode]::Open)
    $EventData = New-Object Microsoft.ServiceBus.Messaging.EventData -ArgumentList $JobJsonStream
    $EventHubClient.Send($EventData)
    $JobJsonStream.Dispose()
}

try {

    if (!(Test-Path $BinariesDirectory)) {
        Write-Error "Could not find binaries directory ($BinariesDirectory)"
        exit 1
    }

    if ($UseDevelopmentStorage) {
        $NoSubmit = $true
    }

    if ([System.String]::IsNullOrEmpty($SCRAMScope)) {
        # Credentials must be passed on the command line
        if ([System.String]::IsNullOrEmpty($StorageAccountKey) -and !$UseDevelopmentStorage) {
            Write-Error "If SCRAMScope is not specified, you must supply the StorageAccountKey parameter or use development storage."
            exit 1
        }

        if (!($NoSubmit) -and [System.String]::IsNullOrEmpty($SubmitConnectionString)) {
            Write-Error "You must supply a connetion string to a Service Bus Event Hub end point to submit the job to Helix or use the -NoSubmit switch"
            Write-Error "The connection string may either be retrieved from SCRAM by passing a -SCRAMScope parameter on the command line"
            Write-Error "or passed using the -SubmitConnectionString parameter."
            Write-Error "The string should be like ""Endpoint=sb://something.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...;EntityPath=..."""
            exit 1
        }
    } else {
        if ([System.String]::IsNullOrEmpty($StorageAccountKey)) {
            $StorageAccountKey = (Get-GenericCredential -Scope $SCRAMScope -UserName $StorageAccountName).Password
        }

        if (!($NoSubmit) -and [System.String]::IsNullOrEmpty($SubmitConnectionString)) {
            $SubmitConnectionString = (Get-GenericCredential -Scope $SCRAMScope -UserName "HelixEventHub").Password
        }
    }

    # TODO: Validate args:
    # Since Repostory, Branch, JobId, JobType and Platform are used to create paths
    # in the storage container, they may not contain punctuation or non-ASCII chars (whitespace should also be discouraged)
    # Hyphen and periods might be allowed (period for the JobId, for example)

    $NuGetExe = Join-Path -Path $PSScriptRoot -ChildPath NuGet.exe
    if (!(Test-Path $NuGetExe)) {
        Write-Error "Could not find nuget.exe at $NuGetExe."
        exit 1
    }

    if (!$NoSubmit) {
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
    if ($UseDevelopmentStorage) {
        $StorageContext = New-AzureStorageContext -Local
    }
    else {
        $StorageContext = New-AzureStorageContext -StorageAccountName $StorageAccountName -StorageAccountKey $StorageAccountKey
    }

    $HelixStage = Join-Path $env:TEMP -ChildPath Helix
    if (Test-Path $HelixStage) {
        Remove-Item -Recurse -Force $HelixStage
    }

    mkdir $HelixStage > $null

    $DropZip = Join-Path $HelixStage -ChildPath Drop.zip

    CreateDrop -Binaries $BinariesDirectory -ZipFile $DropZip

    $BlobRootName = "$Repository/$Branch/$JobId/$JobType"
    $BlobName = $BlobRootName + "_Drop.zip"
    if (!$NoUpload) {
        Write-Host "Uploading drop"
        Set-AzureStorageBlobContent -File $DropZip -Container $StorageContainer -Blob $BlobName -Context $StorageContext
    }

    $StorageContainerRSAS = CreateSASToken -Context $StorageContext -Container $StorageContainer -Permission r

    $DropUri = BuildUri -Context $StorageContext -Container $StorageContainer -BlobName $BlobName -SASToken $StorageContainerRSAS

    # Look for xunit.zip in the "fixtures/xunit2.1" folder and create it if it doesn't already exist
    $XunitFixtureUri = GetXUnitFixtureUri -BlobName "$Repository/$Branch/fixtures/xunit2.1/xunit.zip" -StorageContext $StorageContext -StorageContainer $StorageContainer -StorageContainerRSAS $StorageContainerRSAS -HelixStage $HelixStage
    
    Write-Host "Creating work item list"

    $sb = New-Object -TypeName System.Text.StringBuilder
    [void] $sb.AppendLine("[")

    foreach ($TestAssembly in $TestAssemblies) {
        $WorkItemId = $BlobRootName + "/" + $TestAssembly.BaseName
        Write-Host "  " $WorkItemId
        if ($sb.Length -gt 3) { $sb.AppendLine(",") }
        
        [void] $sb.AppendLine("  {")
        [void] $sb.AppendLine("    ""Command"": ""Performance\\Perf-Test.cmd $TestAssembly"",")
        [void] $sb.AppendLine("    ""CorrelationPayloadUris"": [")
        [void] $sb.AppendLine("        ""$XunitFixtureUri""")
        [void] $sb.AppendLine("    ],")
        [void] $sb.AppendLine("    ""PayloadUri"": ""$DropUri"",")
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

    $ListUri = BuildUri -Context $StorageContext -Container $StorageContainer -BlobName $BlobName -SASToken $StorageContainerRSAS
    
    # Using the same storage account and container for results as for the payload
    # In the future, we may want to put results elsewhere
    $ResultsUri = BuildUri -Context $StorageContext -Container $StorageContainer
    $ResultsUriRSAS =  $StorageContainerRSAS
    $ResultsUriWSAS = CreateSASToken -Context $StorageContext -Container $StorageContainer -Permission w

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

    # Submit event to the Helix Event Hub
    if (!$NoSubmit) {
        SubmitJobToHelix -JobJsonPath $JobJson -EventHubConnectionString $SubmitConnectionString
    }

    exit 0
}
catch [exception] {
    Write-Error -Exception $_.Exception
    exit 1
}
