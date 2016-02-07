using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    /// <summary>
    /// Used so we can mock out how the search service does IO for testing purposes.
    /// </summary>
    internal interface IPackageSearchIOService
    {
        void Create(DirectoryInfo directory);
        void Delete(FileInfo file);
        IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directory);
        bool Exists(FileSystemInfo info);
        byte[] ReadAllBytes(string path);
        void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors);
        void WriteAndFlushAllBytes(string path, byte[] bytes);
    }
}
