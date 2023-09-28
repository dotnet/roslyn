// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions
{
    [ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
    internal sealed class FeaturesFixAllGetFixesService : AbstractFixAllGetFixesService, IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FeaturesFixAllGetFixesService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => this;
    }
}
