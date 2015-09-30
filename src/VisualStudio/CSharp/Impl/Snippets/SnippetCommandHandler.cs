// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [ExportCommandHandler("CSharp Snippets", ContentTypeNames.CSharpContentType)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCommandHandlerNames.IntelliSense)]
    internal sealed class SnippetCommandHandler :
        AbstractSnippetCommandHandler,
        ICommandHandler<SurroundWithCommandArgs>
    {
        [ImportingConstructor]
        public SnippetCommandHandler(IVsEditorAdaptersFactoryService editorAdaptersFactoryService, SVsServiceProvider serviceProvider)
            : base(editorAdaptersFactoryService, serviceProvider)
        {
        }

        public void ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            InvokeInsertionUI(args.TextView, args.SubjectBuffer, nextHandler, surroundWith: true);
        }

        public CommandState GetCommandState(SurroundWithCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return nextHandler();
            }

            Workspace workspace;
            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out workspace))
            {
                return nextHandler();
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return nextHandler();
            }

            return CommandState.Available;
        }

        protected override AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
        {
            AbstractSnippetExpansionClient expansionClient;
            if (!textView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out expansionClient))
            {
                expansionClient = new SnippetExpansionClient(Guids.CSharpLanguageServiceId, textView, subjectBuffer, EditorAdaptersFactoryService);
                textView.Properties.AddProperty(typeof(AbstractSnippetExpansionClient), expansionClient);
            }

            return expansionClient;
        }

        protected override void InvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, Action nextHandler, bool surroundWith = false)
        {
            IVsExpansionManager expansionManager;

            if (!TryGetExpansionManager(out expansionManager))
            {
                nextHandler();
                return;
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
                bstrPrefixText: surroundWith ? CSharpVSResources.SurroundWith : CSharpVSResources.InsertSnippet,
                bstrCompletionChar: null);
        }

        protected override bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            return !syntaxTree.IsEntirelyWithinStringOrCharLiteral(startPosition, cancellationToken) &&
                !syntaxTree.IsEntirelyWithinComment(startPosition, cancellationToken);
        }
    }
}
