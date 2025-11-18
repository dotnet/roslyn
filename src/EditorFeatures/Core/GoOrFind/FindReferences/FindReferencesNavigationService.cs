// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoOrFind;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.FindReferences;

[Export(typeof(FindReferencesNavigationService))]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class FindReferencesNavigationService(
    IThreadingContext threadingContext,
    IStreamingFindUsagesPresenter streamingPresenter,
    IAsynchronousOperationListenerProvider listenerProvider,
    IGlobalOptionService globalOptions) : AbstractGoOrFindNavigationService<IFindUsagesService>(
        threadingContext,
        streamingPresenter,
        listenerProvider.GetListener(FeatureAttribute.FindReferences),
        globalOptions)
{
    public override string DisplayName => EditorFeaturesResources.Find_References;

    protected override FunctionId FunctionId => FunctionId.CommandHandler_FindAllReference;

    /// <summary>
    /// For find-refs, we *always* use the window.  Even if there is only a single result.  This is not a 'go' command 
    /// which imperatively tries to navigate to the location if possible.  The intent here is to keep the results in view
    /// so that the user can always refer to them, even as they do other work.
    /// </summary>
    protected override bool NavigateToSingleResultIfQuick => false;

    protected override StreamingFindUsagesPresenterOptions GetStreamingPresenterOptions(Document document)
        => new()
        {
            SupportsReferences = true,
            IncludeContainingTypeAndMemberColumns = document.Project.SupportsCompilation,
            IncludeKindColumn = document.Project.Language != LanguageNames.FSharp
        };

    protected override Task FindActionAsync(IFindUsagesContext context, Document document, IFindUsagesService service, int caretPosition, CancellationToken cancellationToken)
        => service.FindReferencesAsync(context, document, caretPosition, this.ClassificationOptionsProvider, cancellationToken);
}
