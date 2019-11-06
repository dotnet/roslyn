// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), ServiceLayer.Host), Shared]
    internal class VisualStudioPersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
        public override bool IsSupported(Workspace workspace)
            => workspace is VisualStudioWorkspace;
    }
}
