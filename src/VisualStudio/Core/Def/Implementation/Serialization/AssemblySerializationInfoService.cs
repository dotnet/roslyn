// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    [ExportWorkspaceService(typeof(IAssemblySerializationInfoService), ServiceLayer.Host)]
    [Shared]
    internal class AssemblySerializationInfoService : IAssemblySerializationInfoService
    {
        public bool Serializable(Solution solution, string assemblyFilePath)
        {
            if (assemblyFilePath == null || !File.Exists(assemblyFilePath))
            {
                return false;
            }

            // if solution is not from a disk, just create one.
            if (solution.FilePath == null || !File.Exists(solution.FilePath))
            {
                return false;
            }

            return true;
        }

        public bool TryGetSerializationPrefixAndVersion(Solution solution, string assemblyFilePath, out string prefix, out VersionStamp version)
        {
            prefix = FilePathUtilities.GetRelativePath(solution.FilePath, assemblyFilePath);
            version = VersionStamp.Create(File.GetLastWriteTimeUtc(assemblyFilePath));

            return true;
        }
    }
}
