#load "test_util.csx"

using System;
using System.IO;

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