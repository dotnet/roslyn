// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

/// <summary>
/// Also known as "rename smart tag," this watches text changes in open buffers, determines
/// whether they can be interpreted as an identifier rename, and if so displays a smart tag 
/// that can perform a rename on that symbol. Each text buffer is tracked independently.
/// </summary>
[Export(typeof(ITaggerProvider))]
[TagType(typeof(RenameTrackingTag))]
[TagType(typeof(IErrorTag))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(PredefinedTextViewRoles.Editable)]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class RenameTrackingTaggerProvider(
    IThreadingContext threadingContext,
    IInlineRenameService inlineRenameService,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : ITaggerProvider
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.RenameTracking);
    private readonly IInlineRenameService _inlineRenameService = inlineRenameService;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        var stateMachine = buffer.Properties.GetOrCreateSingletonProperty(() => new StateMachine(_threadingContext, buffer, _inlineRenameService, _globalOptions, _asyncListener));
        return new Tagger(stateMachine) as ITagger<T>;
    }

    internal static void ResetRenameTrackingState(Workspace workspace, DocumentId documentId)
        => ResetRenameTrackingStateWorker(workspace, documentId, visible: false);

    internal static bool ResetVisibleRenameTrackingState(Workspace workspace, DocumentId documentId)
        => ResetRenameTrackingStateWorker(workspace, documentId, visible: true);

    internal static bool ResetRenameTrackingStateWorker(Workspace workspace, DocumentId documentId, bool visible)
    {
        if (workspace.IsDocumentOpen(documentId))
        {
            var document = workspace.CurrentSolution.GetDocument(documentId);
            ITextBuffer textBuffer;
            if (document != null &&
                document.TryGetText(out var text))
            {
                textBuffer = text.Container.TryGetTextBuffer();
                if (textBuffer == null)
                {
                    var ex = new InvalidOperationException(string.Format(
                        "document with name {0} is open but textBuffer is null. Textcontainer is of type {1}.",
                        document.Name,
                        text.Container.GetType().FullName));
                    FatalError.ReportAndCatch(ex);
                    return false;
                }

                if (textBuffer.Properties.TryGetProperty(typeof(StateMachine), out StateMachine stateMachine))
                {
                    if (visible)
                    {
                        return stateMachine.ClearVisibleTrackingSession();
                    }
                    else
                    {
                        return stateMachine.ClearTrackingSession();
                    }
                }
            }
        }

        return false;
    }

    public static (CodeAction action, TextSpan renameSpan) TryGetCodeAction(
        Document document, TextSpan textSpan,
        IEnumerable<IRefactorNotifyService> refactorNotifyServices,
        ITextUndoHistoryRegistry undoHistoryRegistry)
    {
        try
        {
            if (document != null && document.TryGetText(out var text))
            {
                var textBuffer = text.Container.TryGetTextBuffer();
                if (textBuffer != null &&
                    textBuffer.Properties.TryGetProperty(typeof(StateMachine), out StateMachine stateMachine))
                {
                    return stateMachine.TryGetCodeAction(
                        document, text, textSpan, refactorNotifyServices, undoHistoryRegistry);
                }
            }

            return default;
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, ErrorSeverity.General))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal static bool IsRenamableIdentifierFastCheck(
        Task<TriggerIdentifierKind> isRenamableIdentifierTask, out TriggerIdentifierKind identifierKind)
    {
        if (isRenamableIdentifierTask.Status == TaskStatus.RanToCompletion)
        {
            var kind = isRenamableIdentifierTask.Result;
            if (kind != TriggerIdentifierKind.NotRenamable)
            {
                identifierKind = kind;
                return true;
            }
        }

        identifierKind = default;
        return false;
    }
}
