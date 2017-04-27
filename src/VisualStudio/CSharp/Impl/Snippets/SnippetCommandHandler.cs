// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [VSC.ExportCommandHandler("CSharp Snippets", ContentTypeNames.CSharpContentType)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCommandHandlerNames.IntelliSense)]
    internal sealed class SnippetCommandHandler :
        AbstractSnippetCommandHandler,
        VSC.ICommandHandler<SurroundWithCommandArgs>
    {
        [ImportingConstructor]
        public SnippetCommandHandler(IVsEditorAdaptersFactoryService editorAdaptersFactoryService, SVsServiceProvider serviceProvider)
            : base(editorAdaptersFactoryService, serviceProvider)
        {
        }

        public bool ExecuteCommand(SurroundWithCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args.SubjectBuffer))
            {   
                return false;
            }

            return !InvokeInsertionUI(args.TextView, args.SubjectBuffer, surroundWith: true);
        }

        public VSC.CommandState GetCommandState(SurroundWithCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args.SubjectBuffer))
            {
                return VSC.CommandState.CommandIsUnavailable;
            }

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out var workspace))
            {
                return VSC.CommandState.CommandIsUnavailable;
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return VSC.CommandState.CommandIsUnavailable;
            }

            return VSC.CommandState.CommandIsAvailable;
        }

        protected override AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
        {
            if (!textView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient expansionClient))
            {
                expansionClient = new SnippetExpansionClient(Guids.CSharpLanguageServiceId, textView, subjectBuffer, EditorAdaptersFactoryService);
                textView.Properties.AddProperty(typeof(AbstractSnippetExpansionClient), expansionClient);
            }

            return expansionClient;
        }

        protected override bool InvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false)
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
