// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Roslyn.Test.Utilities
{
    public static class SharedResourceHelpers
    {
        public static void CleanupAllGeneratedFiles(string filename)
        {
            // This will cleanup all files with same name but different extension
            // These are often used by command line tests which use temp files.
            // The temp file dispose method cleans up that specific temp file 
            // but anything that was generated from this will not be removed by dispose

            string directory = System.IO.Path.GetDirectoryName(filename);
            string filenamewithoutextension = System.IO.Path.GetFileNameWithoutExtension(filename);
            string searchfilename = filenamewithoutextension + ".*";
            foreach (string f in System.IO.Directory.GetFiles(directory, searchfilename))
            {
                if (System.IO.Path.GetFileName(f) != System.IO.Path.GetFileName(filename))
                {
                    try
                    {
                        System.IO.File.Delete(f);
                    }
                    catch
                    {
                        // Swallow any exceptions as the cleanup should not necessarily block the test
                    }
                }
            }
        }
    }
}
