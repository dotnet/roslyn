// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Serialization
{
    [ExportWorkspaceService(typeof(IAssemblySerializationInfoService), ServiceLayer.Default)]
    [Shared]
    internal class AssemblySerializationInfoService : IAssemblySerializationInfoService
    {
        public bool Serializable(Solution solution, string assemblyFilePath)
        {
            return false;
        }

        public bool TryGetSerializationPrefixAndVersion(Solution solution, string assemblyFilePath, out string prefix, out VersionStamp version)
        {
            prefix = string.Empty;
            version = VersionStamp.Default;

            return false;
        }
    }
}
