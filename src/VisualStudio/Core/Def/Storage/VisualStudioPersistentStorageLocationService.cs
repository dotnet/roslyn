// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), ServiceLayer.Host), Shared]
    internal class VisualStudioPersistentStorageLocationService : DefaultPersistentStorageLocationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioPersistentStorageLocationService()
        {
        }

        public override bool IsSupported(Workspace workspace)
            => workspace is VisualStudioWorkspace;
    }
}
