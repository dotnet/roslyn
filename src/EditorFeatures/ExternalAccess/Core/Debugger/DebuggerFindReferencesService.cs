// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.Debugger;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Debugger;
#endif

[Export]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DebuggerFindReferencesService(
    IGlobalOptionService globalOptions,
    Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
{
    private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter = streamingPresenter;

    public async Task FindSymbolReferencesAsync(ISymbol symbol, Project project, CancellationToken cancellationToken)
    {
        var streamingPresenter = _streamingPresenter.Value;

        // Let the presenter know we're starting a search.  It will give us back
        // the context object that the FAR service will push results into.
        //
        // We're awaiting the work to find the symbols (as opposed to kicking it off in a
        // fire-and-forget streaming fashion).  As such, we do not want to use the cancellation
        // token provided by the presenter.  Instead, we'll let our caller own if this work
        // is cancelable.
        var (context, _) = streamingPresenter.StartSearch(EditorFeaturesResources.Find_References, new StreamingFindUsagesPresenterOptions { SupportsReferences = true });

        var classificationOptions = globalOptions.GetClassificationOptionsProvider();

        try
        {
            await AbstractFindUsagesService.FindSymbolReferencesAsync(context, symbol, project, classificationOptions, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
