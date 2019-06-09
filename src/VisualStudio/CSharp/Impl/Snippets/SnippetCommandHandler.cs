// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(ICommandHandler))]
    [ContentType(CodeAnalysis.Editor.ContentTypeNames.CSharpContentType)]
    [Name("CSharp Snippets")]
    [Order(After = CodeAnalysis.Editor.PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    [Order(After = CodeAnalysis.Editor.PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    internal sealed class SnippetCommandHandler :
        AbstractSnippetCommandHandler,
        ICommandHandler<SurroundWithCommandArgs>
    {
        [ImportingConstructor]
        public SnippetCommandHandler(IThreadingContext threadingContext, IVsEditorAdaptersFactoryService editorAdaptersFactoryService, SVsServiceProvider serviceProvider)
            : base(threadingContext, editorAdaptersFactoryService, serviceProvider)
        {
        }

        public bool ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer, surroundWith: true);
        }

        public CommandState GetCommandState(SurroundWithCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return CommandState.Unspecified;
            }

            if (!CodeAnalysis.Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out var workspace))
            {
                return CommandState.Unspecified;
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        protected override AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
        {
            if (!textView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient expansionClient))
            {
                expansionClient = new SnippetExpansionClient(ThreadingContext, Guids.CSharpLanguageServiceId, textView, subjectBuffer, EditorAdaptersFactoryService);
                textView.Properties.AddProperty(typeof(AbstractSnippetExpansionClient), expansionClient);
            }

            return expansionClient;
        }

        protected override bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false)
        {
            if (!TryGetExpansionManager(out var expansionManager))
            {
                return false;
            }

            expansionManager.InvokeInsertionUI(
                EditorAdaptersFactoryService.GetViewAdapter(textView),
                GetSnippetExpansionClient(textView, subjectBuffer),
                Guids.CSharpLanguageServiceId,
                bstrTypes: surroundWith ? new[] { "SurroundsWith" } : new[] { "Expansion", "SurroundsWith" },
                iCountTypes: surroundWith ? 1 : 2,
                fIncludeNULLType: 1,
                bstrKinds: null,
                iCountKinds: 0,
                fIncludeNULLKind: 0,
                bstrPrefixText: surroundWith ? CSharpVSResources.Surround_With : CSharpVSResources.Insert_Snippet,
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
}
