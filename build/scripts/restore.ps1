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
    if (test-path $entryFilePath) {
        continue;
    }

    # If it's a directory then create it now.  Calling ExtractToFile will
    # throw on a directory.  There is no way I can find to ask a
    # ZipArchiveEntry if it is a directory hence we just check for a file
    # extension
    [string]$ext = [IO.Path]::GetExtension($entryFilePath)
    if ($ext -eq "") {
        $null = mkdir $entryFilePath -errorAction SilentlyContinue
        continue;
    }

    $entryDirectory = split-path -parent $entryFilePath
    if (-not (test-path $entryDirectory)) {
        $null = mkdir $entryDirectory -errorAction SilentlyContinue
    }

    try {
        write-host "Restoring $entryFilePath"
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $entryFilePath) 
    } catch {
        write-host "Unable to restore $($entry.FullName) $Error"
    }
}

$archive.Dispose()
rm $outFilePath
