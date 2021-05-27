// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindReferences
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.FindReferences)]
    internal class FindReferencesCommandHandler : ICommandHandler<FindReferencesCommandArgs>
    {
        private readonly IStreamingFindUsagesPresenter _streamingPresenter;

        private readonly IAsynchronousOperationListener _asyncListener;

        public string DisplayName => EditorFeaturesResources.Find_References;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public FindReferencesCommandHandler(
            IStreamingFindUsagesPresenter streamingPresenter,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            Contract.ThrowIfNull(listenerProvider);

            _streamingPresenter = streamingPresenter;
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

        private static (Document, IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess) GetDocumentAndService(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
#pragma warning disable CS0618 // Type or member is obsolete
            var legacyService = document?.GetLanguageService<IFindUsagesService>();
#pragma warning restore CS0618 // Type or member is obsolete
            return legacyService == null
                ? (document, document?.GetLanguageService<IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess>())
                : (document, new FindUsagesServiceWrapper(legacyService));
        }

        private bool TryExecuteCommand(int caretPosition, Document document, IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess findUsagesService)
        {
            // See if we're running on a host that can provide streaming results.
            // We'll both need a FAR service that can stream results to us, and 
            // a presenter that can accept streamed results.
            if (findUsagesService != null && _streamingPresenter != null)
            {
                _ = StreamingFindReferencesAsync(document, caretPosition, findUsagesService, _streamingPresenter);
                return true;
            }

            return false;
        }

        private async Task StreamingFindReferencesAsync(
            Document document, int caretPosition,
            IFindUsagesServiceRenameOnceTypeScriptMovesToExternalAccess findUsagesService,
            IStreamingFindUsagesPresenter presenter)
        {
            try
            {
                using var token = _asyncListener.BeginAsyncOperation(nameof(StreamingFindReferencesAsync));

                // Let the presented know we're starting a search.  It will give us back the context object that the FAR
                // service will push results into. This operation is not externally cancellable.  Instead, the find refs
                // window will cancel it if another request is made to use it.
                var context = presenter.StartSearchWithCustomColumns(
                    EditorFeaturesResources.Find_References,
                    supportsReferences: true,
                    includeContainingTypeAndMemberColumns: document.Project.SupportsCompilation,
                    includeKindColumn: document.Project.Language != LanguageNames.FSharp,
                    CancellationToken.None);

                using (Logger.LogBlock(
                    FunctionId.CommandHandler_FindAllReference,
                    KeyValueLogMessage.Create(LogType.UserAction, m => m["type"] = "streaming"),
                    context.CancellationToken))
                {
                    try
                    {
                        await findUsagesService.FindReferencesAsync(document, caretPosition, context).ConfigureAwait(false);
                    }
                    finally
                    {
                        await context.OnCompletedAsync().ConfigureAwait(false);
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
}
