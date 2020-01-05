// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation
{
    [ExportWorkspaceService(typeof(INavigateToLinkService), layer: ServiceLayer.Default)]
    [Shared]
    internal sealed class DefaultNavigateToLinkService : INavigateToLinkService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultNavigateToLinkService()
        {
        }

        public Task<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken)
            => SpecializedTasks.False;
    }
}
