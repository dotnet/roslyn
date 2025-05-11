// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets;

[Export(typeof(ICommandHandler))]
[ContentType(Microsoft.CodeAnalysis.Editor.ContentTypeNames.CSharpContentType)]
[Name("CSharp Snippets")]
[Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
[Order(After = Microsoft.CodeAnalysis.Editor.PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
[Order(Before = nameof(CompleteStatementCommandHandler))]
[Order(Before = Microsoft.CodeAnalysis.Editor.PredefinedCommandHandlerNames.AutomaticLineEnder)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SnippetCommandHandler(
    IThreadingContext threadingContext,
    IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
    IVsService<SVsTextManager, IVsTextManager2> textManager,
    EditorOptionsService editorOptionsService) :
    AbstractSnippetCommandHandler(threadingContext, editorOptionsService, textManager),
    ICommandHandler<SurroundWithCommandArgs>,
    IChainedCommandHandler<TypeCharCommandArgs>
{
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService = editorAdaptersFactoryService;

    public bool ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return false;
        }

        return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer, surroundWith: true);
    }

    public CommandState GetCommandState(SurroundWithCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!args.SubjectBuffer.TryGetWorkspace(out var workspace) ||
            !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextCommandHandler)
    {
        return nextCommandHandler();
    }

    public void ExecuteCommand(TypeCharCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (args.TypedChar == ';'
            && AreSnippetsEnabledWithClient(args, out var snippetExpansionClient)
            && snippetExpansionClient.IsFullMethodCallSnippet)
        {
            // Commit the snippet. Leave the caret in place, but clear the selection. Subsequent handlers in the
            // chain will handle the remaining Complete Statement (';' insertion) operations only if there is no
            // active selection.
            snippetExpansionClient.CommitSnippet(leaveCaret: true);
            args.TextView.Selection.Clear();
        }

        nextCommandHandler();
    }

    protected override bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false)
    {
        if (!TryGetExpansionManager(out var expansionManager))
        {
            return false;
        }

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        expansionManager.InvokeInsertionUI(
            _editorAdaptersFactoryService.GetViewAdapter(textView),
            GetSnippetExpansionClientFactory(document).GetOrCreateSnippetExpansionClient(document, textView, subjectBuffer),
            Guids.CSharpLanguageServiceId,
            bstrTypes: surroundWith ? ["SurroundsWith"] : ["Expansion", "SurroundsWith"],
            iCountTypes: surroundWith ? 1 : 2,
            fIncludeNULLType: 1,
            bstrKinds: null,
            iCountKinds: 0,
            fIncludeNULLKind: 0,
            bstrPrefixText: surroundWith ? CSharpVSResources.Surround_With : ServicesVSResources.Insert_Snippet,
            bstrCompletionChar: null);

        return true;
    }

    protected override bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken)
    {
        var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);

        return !syntaxTree.IsEntirelyWithinStringOrCharLiteral(startPosition, cancellationToken) &&
            !syntaxTree.IsEntirelyWithinComment(startPosition, cancellationToken);
    }
}
