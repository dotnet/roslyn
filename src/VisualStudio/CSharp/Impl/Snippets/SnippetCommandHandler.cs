// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    [Export(typeof(ICommandHandler))]
    [ContentType(Microsoft.CodeAnalysis.Editor.ContentTypeNames.CSharpContentType)]
    [Name("CSharp Snippets")]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    [Order(After = Microsoft.CodeAnalysis.Editor.PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    [Order(Before = nameof(CompleteStatementCommandHandler))]
    [Order(Before = Microsoft.CodeAnalysis.Editor.PredefinedCommandHandlerNames.AutomaticLineEnder)]
    internal sealed class SnippetCommandHandler :
        AbstractSnippetCommandHandler,
        ICommandHandler<SurroundWithCommandArgs>,
        IChainedCommandHandler<TypeCharCommandArgs>
    {
        private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _argumentProviders;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SnippetCommandHandler(
            IThreadingContext threadingContext,
            SignatureHelpControllerProvider signatureHelpControllerProvider,
            IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            SVsServiceProvider serviceProvider,
            [ImportMany] IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders,
            IGlobalOptionService globalOptions)
            : base(threadingContext, signatureHelpControllerProvider, editorCommandHandlerServiceFactory, editorAdaptersFactoryService, globalOptions, serviceProvider)
        {
            _argumentProviders = argumentProviders.ToImmutableArray();
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

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out var workspace))
            {
                return CommandState.Unspecified;
            }

            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
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
            AssertIsForeground();
            if (args.TypedChar == ';'
                && AreSnippetsEnabled(args)
                && args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient)
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

        protected override AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
        {
            if (!textView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient expansionClient))
            {
                expansionClient = new SnippetExpansionClient(
                    ThreadingContext,
                    Guids.CSharpLanguageServiceId,
                    textView,
                    subjectBuffer,
                    SignatureHelpControllerProvider,
                    EditorCommandHandlerServiceFactory,
                    EditorAdaptersFactoryService,
                    _argumentProviders,
                    GlobalOptions);

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
