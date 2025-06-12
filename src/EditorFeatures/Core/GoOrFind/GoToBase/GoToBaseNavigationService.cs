// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoOrFind;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.GoToBase;

[Export(typeof(GoToBaseNavigationService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class GoToBaseNavigationService(
    IThreadingContext threadingContext,
    IStreamingFindUsagesPresenter streamingPresenter,
    IAsynchronousOperationListenerProvider listenerProvider,
    IGlobalOptionService globalOptions)
    : AbstractGoOrFindNavigationService<IGoToBaseService>(
        threadingContext,
        streamingPresenter,
        listenerProvider.GetListener(FeatureAttribute.GoToBase),
        globalOptions)
{
    public override string DisplayName => EditorFeaturesResources.Go_To_Base;

    protected override FunctionId FunctionId => FunctionId.CommandHandler_GoToBase;

    /// <summary>
    /// If we find a single results quickly enough, we do want to take the user directly to it,
    /// instead of popping up the FAR window to show it.
    /// </summary>
    protected override bool NavigateToSingleResultIfQuick => true;

    protected override Task FindActionAsync(IFindUsagesContext context, Document document, IGoToBaseService service, int caretPosition, CancellationToken cancellationToken)
        => service.FindBasesAsync(context, document, caretPosition, ClassificationOptionsProvider, cancellationToken);
}
