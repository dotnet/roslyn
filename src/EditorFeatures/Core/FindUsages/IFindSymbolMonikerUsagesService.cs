﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.LanguageServiceIndexFormat;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
        IAsyncEnumerable<ExternalReferenceItem> FindReferencesByMoniker(DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers, IStreamingProgressTracker progress, CancellationToken cancellationToken);
        IAsyncEnumerable<DefinitionItem> FindDefinitionsByMoniker(SymbolMoniker moniker, IStreamingProgressTracker progress, CancellationToken cancellationToken);
        IAsyncEnumerable<DefinitionItem> FindImplementationsByMoniker(SymbolMoniker moniker, IStreamingProgressTracker progress, CancellationToken cancellationToken);
    }

    internal abstract class AbstractFindSymbolMonikerUsagesService : IFindSymbolMonikerUsagesService
    {
        public virtual IAsyncEnumerable<DefinitionItem> FindDefinitionsByMoniker(SymbolMoniker moniker, IStreamingProgressTracker progress, CancellationToken cancellationToken)
            => EmptyAsyncEnumerable<DefinitionItem>.Instance;

        public virtual IAsyncEnumerable<DefinitionItem> FindImplementationsByMoniker(SymbolMoniker moniker, IStreamingProgressTracker progress, CancellationToken cancellationToken)
            => EmptyAsyncEnumerable<DefinitionItem>.Instance;

        public virtual IAsyncEnumerable<ExternalReferenceItem> FindReferencesByMoniker(DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers, IStreamingProgressTracker progress, CancellationToken cancellationToken)
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
