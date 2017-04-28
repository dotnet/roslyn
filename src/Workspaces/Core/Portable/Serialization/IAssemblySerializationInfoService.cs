// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal interface IAssemblySerializationInfoService : IWorkspaceService
    {
        bool Serializable(Solution solution, string assemblyFilePath);
        bool TryGetSerializationPrefix(Solution solution, string assemblyFilePath, out string prefix);
    }
}
