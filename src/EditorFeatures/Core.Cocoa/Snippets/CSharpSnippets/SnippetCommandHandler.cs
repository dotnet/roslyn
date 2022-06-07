// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using Microsoft.VisualStudio.UI;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(ICommandHandler))]
    [ContentType(CodeAnalysis.Editor.ContentTypeNames.CSharpContentType)]
    [Name("CSharp Snippets")]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    [Order(After = CodeAnalysis.Editor.PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    internal sealed class SnippetCommandHandler :
        AbstractSnippetCommandHandler,
        ICommandHandler<SurroundWithCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SnippetCommandHandler(
            IThreadingContext threadingContext,
            IExpansionServiceProvider expansionServiceProvider,
            IExpansionManager expansionManager,
            IGlobalOptionService globalOptions)
            : base(threadingContext, expansionServiceProvider, expansionManager, globalOptions)
        {
        }

        public bool ExecuteCommand(SurroundWithCommandArgs args, CommandExecutionContext context)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer, surroundWith: true);
        }

        public CommandState GetCommandState(SurroundWithCommandArgs args)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

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
                expansionClient = new SnippetExpansionClient(subjectBuffer.ContentType, textView, subjectBuffer, ExpansionServiceProvider, GlobalOptions);
                textView.Properties.AddProperty(typeof(AbstractSnippetExpansionClient), expansionClient);
            }

            return expansionClient;
        }

        protected override bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false)
        {
            ExpansionServiceProvider.GetExpansionService(textView).InvokeInsertionUI(
                GetSnippetExpansionClient(textView, subjectBuffer),
                subjectBuffer.ContentType,
                types: surroundWith ? new[] { "SurroundsWith" } : new[] { "Expansion", "SurroundsWith" },
                includeNullType: true,
                kinds: null,
                includeNullKind: false,
                prefixText: surroundWith ? GettextCatalog.GetString("Surround With") : GettextCatalog.GetString("Insert Snippet"),
                completionChar: null);

            return true;
        }

        protected override bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            var token = syntaxTree.GetRoot(cancellationToken).FindToken(startPosition);
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(startPosition);
            return !(trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || token.IsKind(SyntaxKind.StringLiteralToken));
        }
    }
}
