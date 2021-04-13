// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Debugger
{
    [Export]
    [Shared]
    internal sealed class DebuggerFindReferencesService
    {
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DebuggerFindReferencesService(
            IThreadingContext threadingContext,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            _streamingPresenter = streamingPresenter;
        }

        public async Task FindSymbolReferencesAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var streamingPresenter = _streamingPresenter.Value;

            // Let the presenter know we're starting a search.  It will give us back
            // the context object that the FAR service will push results into.
            var context = streamingPresenter.StartSearch(EditorFeaturesResources.Find_References, supportsReferences: true, cancellationToken);

            try
            {
                await AbstractFindUsagesService.FindSymbolReferencesAsync(context, symbol, project).ConfigureAwait(false);
            }
            finally
            {
                await context.OnCompletedAsync().ConfigureAwait(false);
            }
        }
    }
}
