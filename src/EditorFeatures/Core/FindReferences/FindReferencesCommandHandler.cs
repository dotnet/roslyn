// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindReferences;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.FindReferences)]
internal class FindReferencesCommandHandler : ICommandHandler<FindReferencesCommandArgs>
{
    private readonly IStreamingFindUsagesPresenter _streamingPresenter;
    private readonly IGlobalOptionService _globalOptions;
    private readonly IAsynchronousOperationListener _asyncListener;

    public string DisplayName => EditorFeaturesResources.Find_References;

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public FindReferencesCommandHandler(
        IStreamingFindUsagesPresenter streamingPresenter,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        Contract.ThrowIfNull(listenerProvider);

        _streamingPresenter = streamingPresenter;
        _globalOptions = globalOptions;
        _asyncListener = listenerProvider.GetListener(FeatureAttribute.FindReferences);
    }

    public CommandState GetCommandState(FindReferencesCommandArgs args)
    {
        var (_, service) = GetDocumentAndService(args.SubjectBuffer.CurrentSnapshot);
        return service != null
            ? CommandState.Available
            : CommandState.Unspecified;
    }

    public bool ExecuteCommand(FindReferencesCommandArgs args, CommandExecutionContext context)
    {
        var subjectBuffer = args.SubjectBuffer;

        // Get the selection that user has in our buffer (this also works if there
        // is no selection and the caret is just at a single position).  If we 
        // can't get the selection, or there are multiple spans for it (i.e. a 
        // box selection), then don't do anything.
        var snapshotSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
        if (snapshotSpans.Count == 1)
        {
            var selectedSpan = snapshotSpans[0];
            var (document, service) = GetDocumentAndService(subjectBuffer.CurrentSnapshot);
            if (document != null)
            {
                // Do a find-refs at the *start* of the selection.  That way if the
                // user has selected a symbol that has another symbol touching it
                // on the right (i.e.  Goo++  ), then we'll do the find-refs on the
                // symbol selected, not the symbol following.
                if (TryExecuteCommand(selectedSpan.Start, document, service))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static (Document, IFindUsagesService) GetDocumentAndService(ITextSnapshot snapshot)
    {
        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
        return (document, document?.GetLanguageService<IFindUsagesService>());
    }

    private bool TryExecuteCommand(int caretPosition, Document document, IFindUsagesService findUsagesService)
    {
        // See if we're running on a host that can provide streaming results.
        // We'll both need a FAR service that can stream results to us, and 
        // a presenter that can accept streamed results.
        if (findUsagesService != null && _streamingPresenter != null)
        {
            // kick this work off in a fire and forget fashion.  Importantly, this means we do
            // not pass in any ambient cancellation information as the execution of this command
            // will complete and will have no bearing on the computation of the references we compute.
            _ = StreamingFindReferencesAsync(document, caretPosition, findUsagesService, _streamingPresenter);
            return true;
        }

        return false;
    }

    private async Task StreamingFindReferencesAsync(
        Document document,
        int caretPosition,
        IFindUsagesService findUsagesService,
        IStreamingFindUsagesPresenter presenter)
    {
        try
        {
            using var token = _asyncListener.BeginAsyncOperation(nameof(StreamingFindReferencesAsync));
            var classificationOptions = _globalOptions.GetClassificationOptionsProvider();

            // Let the presented know we're starting a search.  It will give us back the context object that the FAR
            // service will push results into. This operation is not externally cancellable.  Instead, the find refs
            // window will cancel it if another request is made to use it.
            var (context, cancellationToken) = presenter.StartSearch(
                EditorFeaturesResources.Find_References,
                new StreamingFindUsagesPresenterOptions()
                {
                    SupportsReferences = true,
                    IncludeContainingTypeAndMemberColumns = document.Project.SupportsCompilation,
                    IncludeKindColumn = document.Project.Language != LanguageNames.FSharp
                });

            using (Logger.LogBlock(
                FunctionId.CommandHandler_FindAllReference,
                KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                cancellationToken))
            {
                try
                {
                    await findUsagesService.FindReferencesAsync(context, document, caretPosition, classificationOptions, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    await context.OnCompletedAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }
    }
}
