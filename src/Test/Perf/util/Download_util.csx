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
    var directoryUtil = new RelativeDirectory();
    var cpcDestinationPath = directoryUtil.CPCDirectoryPath;
    var cpcSourceBinaryLocation = @"\\mlangfs1\public\basoundr\CpcBinaries";


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
    var directoryUtil = new RelativeDirectory();
    var destinationFolderPath = directoryUtil.CPCDirectoryPath;
    var sourceFile = @"\\mlangfs1\public\basoundr\vibenchcsv2json";

    CopyDirectory(sourceFile, destinationFolderPath, @"/s");
}
