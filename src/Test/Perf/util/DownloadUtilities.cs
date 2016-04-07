using System.IO;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using static Roslyn.Test.Performance.Utilities.Tools;
namespace Roslyn.Test.Performance.Utilities
{
    internal class DownloadUtilities
    {


    public static void DownloadTools()
    {
        DownloadCPC();
        DownloadViBenchToJson();
    }

    public static void DownloadCPC()
    {
        var cpcDestinationPath = GetCPCDirectoryPath();
        var cpcSourceBinaryLocation = @"\\mlangfs1\public\basoundr\CpcBinaries";


        // Delete the existing CPC folder
        if (Directory.Exists(cpcDestinationPath))
        {
            Directory.Delete(cpcDestinationPath, true);
        }

        // Copy CPC from the share to cpcDestinationPath
        CopyDirectory(cpcSourceBinaryLocation, TrivialLogger.Instance, cpcDestinationPath);
    }

    public static void DownloadViBenchToJson()
    {
        var destinationFolderPath = GetCPCDirectoryPath();
        var sourceFile = @"\\mlangfs1\public\basoundr\vibenchcsv2json";

        CopyDirectory(sourceFile, TrivialLogger.Instance, destinationFolderPath, @"/s");
    }
}
}
