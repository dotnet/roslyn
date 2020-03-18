// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.SymbolMonikers;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal interface IFindSymbolMonikerUsagesService : IWorkspaceService
    {
        Task<ImmutableArray<ExternalReferenceItem>> FindReferencesByMonikerAsync(IEnumerable<SymbolMoniker> monikers, int page, CancellationToken cancellationToken);
        Task<ImmutableArray<DefinitionItem>> FindDefinitionsByMonikerAsync(SymbolMoniker moniker, int page, CancellationToken cancellationToken);
        Task<ImmutableArray<DefinitionItem>> FindImplementationsByMonikerAsync(SymbolMoniker moniker, int page, CancellationToken cancellationToken);
    }

    internal abstract class AbstractFindSymbolMonikerUsagesService : IFindSymbolMonikerUsagesService
    {
        public virtual Task<ImmutableArray<DefinitionItem>> FindDefinitionsByMonikerAsync(SymbolMoniker moniker, int page, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<DefinitionItem>();

        public virtual Task<ImmutableArray<DefinitionItem>> FindImplementationsByMonikerAsync(SymbolMoniker moniker, int page, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<DefinitionItem>();

        public virtual Task<ImmutableArray<ExternalReferenceItem>> FindReferencesByMonikerAsync(IEnumerable<SymbolMoniker> monikers, int page, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyImmutableArray<ExternalReferenceItem>();
    }

    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService)), Shared]
    internal class DefaultFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        [ImportingConstructor]
        public DefaultFindSymbolMonikerUsagesService()
        {
        }
    }
}
