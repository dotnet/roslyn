param ([string]$nugetZipUrl = $(throw "Need an URL to the NuGet zip") )

$destination = ${env:UserProfile}
$outFilePath = [IO.Path]::ChangeExtension([IO.Path]::GetTempFileName(), "zip")
write-host "Downloading $nugetZipUrl -> $outFilePath"
$client = new-object System.Net.WebClient
$client.DownloadFile($nugetZipUrl, $outFilePath)

# It's possible for restore to run in parallel on the test machines.  As such
# we need to restore only new files to handle simultaneous restore scenarios.
write-host "Extracting"
Add-Type -assembly "System.IO.Compression.Filesystem"
$archive = [IO.Compression.ZipFile]::OpenRead($outFilePath)
foreach ($entry in $archive.Entries) {
    $entryFilePath = join-path $destination $entry.FullName
    $entryDirectory = split-path -parent $entryFilePath

    if (test-path $entryFilePath) {
        continue;
    }

    $null = mkdir $entryDirectory -errorAction SilentlyContinue
    trap {
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $entryFilePath) 
    }
}
