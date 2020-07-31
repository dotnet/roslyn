// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServerIndexFormat;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    /// <summary>
    /// Allows searching for symbols by <see cref="SymbolMoniker"/>.  These calls will be passed
    /// onto Rich-Nav (if available) and their results will be converted back to the forms that
    /// Roslyn can present.
    /// </summary>
    internal interface IFindSymbolMonikerUsagesService : IWorkspaceService
    {
        IAsyncEnumerable<ExternalReferenceItem> FindReferencesByMonikerAsync(DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers, CancellationToken cancellationToken);
        IAsyncEnumerable<DefinitionItem> FindDefinitionsByMonikerAsync(SymbolMoniker moniker, CancellationToken cancellationToken);
        IAsyncEnumerable<DefinitionItem> FindImplementationsByMonikerAsync(SymbolMoniker moniker, CancellationToken cancellationToken);
    }

    internal abstract class AbstractFindSymbolMonikerUsagesService : IFindSymbolMonikerUsagesService
    {
        public virtual IAsyncEnumerable<DefinitionItem> FindDefinitionsByMonikerAsync(SymbolMoniker moniker, CancellationToken cancellationToken)
            => EmptyAsyncEnumerable<DefinitionItem>.Instance;

        public virtual IAsyncEnumerable<DefinitionItem> FindImplementationsByMonikerAsync(SymbolMoniker moniker, CancellationToken cancellationToken)
            => EmptyAsyncEnumerable<DefinitionItem>.Instance;

        public virtual IAsyncEnumerable<ExternalReferenceItem> FindReferencesByMonikerAsync(DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers, CancellationToken cancellationToken)
            => EmptyAsyncEnumerable<ExternalReferenceItem>.Instance;
    }

    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService)), Shared]
    internal class DefaultFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultFindSymbolMonikerUsagesService()
        {
        }
    }
}
