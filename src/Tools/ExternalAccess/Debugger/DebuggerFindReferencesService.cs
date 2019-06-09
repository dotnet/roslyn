// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IThreadingContext _threadingContext;
        private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DebuggerFindReferencesService(
            IThreadingContext threadingContext,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            _threadingContext = threadingContext;
            _streamingPresenter = streamingPresenter;
        }

        public async Task FindSymbolReferencesAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var streamingPresenter = _streamingPresenter.Value;

            // Let the presenter know we're starting a search.  It will give us back
            // the context object that the FAR service will push results into.
            var context = streamingPresenter.StartSearch(EditorFeaturesResources.Find_References, supportsReferences: true);

            await AbstractFindUsagesService.FindSymbolReferencesAsync(_threadingContext, context, symbol, project, cancellationToken).ConfigureAwait(false);

            // Note: we don't need to put this in a finally.  The only time we might not hit
            // this is if cancellation or another error gets thrown.  In the former case,
            // that means that a new search has started.  We don't care about telling the
            // context it has completed.  In the latter case something wrong has happened
            // and we don't want to run any more code in this particular context.
            await context.OnCompletedAsync().ConfigureAwait(false);
        }
    }
}
