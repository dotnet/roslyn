// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
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
using Roslyn.Utilities;

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
    IDiagnosticAnalyzerService diagnosticAnalyzerService,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : ITaggerProvider
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IAsynchronousOperationListener _asyncListener = listenerProvider.GetListener(FeatureAttribute.RenameTracking);
    private readonly IInlineRenameService _inlineRenameService = inlineRenameService;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService = diagnosticAnalyzerService;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
    {
        var stateMachine = buffer.Properties.GetOrCreateSingletonProperty(() => new StateMachine(_threadingContext, buffer, _inlineRenameService, _diagnosticAnalyzerService, _globalOptions, _asyncListener));
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
                        "document with name {0} is open but textBuffer is null. Textcontainer is of type {1}. SourceText is: {2}",
                        document.Name,
                        text.Container.GetType().FullName,
                        text.ToString()));
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
            ITextUndoHistoryRegistry undoHistoryRegistry,
            CancellationToken cancellationToken)
    {
        try
        {
            if (document != null && document.TryGetText(out var text))
            {
                var textBuffer = text.Container.TryGetTextBuffer();
                if (textBuffer != null &&
                    textBuffer.Properties.TryGetProperty(typeof(StateMachine), out StateMachine stateMachine) &&
                    stateMachine.CanInvokeRename(out _, cancellationToken: cancellationToken))
                {
                    return stateMachine.TryGetCodeAction(
                        document, text, textSpan, refactorNotifyServices, undoHistoryRegistry, cancellationToken);
                }
            }

            return default;
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken, ErrorSeverity.General))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal static bool IsRenamableIdentifier(Task<TriggerIdentifierKind> isRenamableIdentifierTask, bool waitForResult, CancellationToken cancellationToken)
    {
        if (isRenamableIdentifierTask.Status == TaskStatus.RanToCompletion && isRenamableIdentifierTask.Result != TriggerIdentifierKind.NotRenamable)
        {
            return true;
        }
        else if (isRenamableIdentifierTask.Status == TaskStatus.Canceled)
        {
            return false;
        }
        else if (waitForResult)
        {
            return WaitForIsRenamableIdentifier(isRenamableIdentifierTask, cancellationToken);
        }
        else
        {
            return false;
        }
    }

    internal static bool WaitForIsRenamableIdentifier(Task<TriggerIdentifierKind> isRenamableIdentifierTask, CancellationToken cancellationToken)
    {
        try
        {
            return isRenamableIdentifierTask.WaitAndGetResult_CanCallOnBackground(cancellationToken) != TriggerIdentifierKind.NotRenamable;
        }
        catch (OperationCanceledException e) when (e.CancellationToken != cancellationToken || cancellationToken == CancellationToken.None)
        {
            // We passed in a different cancellationToken, so if there's a race and 
            // isRenamableIdentifierTask was cancelled, we'll get a OperationCanceledException
            return false;
        }
    }
}
