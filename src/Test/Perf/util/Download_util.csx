#load "test_util.csx"
#load "tools_util.csx"

using System;
using System.IO;

void DownloadTools()
{
    DownloadCPC();
    DownloadViBenchToJson();
}

void DownloadCPC()
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

void DownloadViBenchToJson()
{
    var destinationFilePath = GetViBenchToJsonExeFilePath();
    var sourceFile = @"\\mlangfs1\public\basoundr\vibenchcsv2json\ViBenchToJson.exe";
    
    File.Copy(sourceFile, destinationFilePath, true);
}