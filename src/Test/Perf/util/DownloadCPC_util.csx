#load "test_util.csx"

using System;
using System.IO;

InitUtilities();

public void DownloadCPC()
{
    var cpcDestinationPath = GetCPCDirectoryPath();
    var cpcSourceBinaryLocation = GetCPCSourceBinaryLocation();
    
    
    // Delete the existing CPC folder
    if (Directory.Exists(cpcDestinationPath))
    {
        Directory.Delete(cpcDestinationPath, true);
    }
    
    // Copy CPC from the share to cpcDestinationPath
    CopyDirectory(cpcSourceBinaryLocation, cpcDestinationPath);
}

void CopyDirectory(string source, string destination)
{
    var result = ShellOutUsingShellExecute("Robocopy", $"/mir {source} {destination}");
    if (!result.Succeeded)
    {
        throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
    }
}
