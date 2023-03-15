// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal abstract class AbstractSnippetCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<BackTabKeyCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>,
        IChainedCommandHandler<AutomaticLineEnderCommandArgs>
    {
        protected readonly SignatureHelpControllerProvider SignatureHelpControllerProvider;
        protected readonly IEditorCommandHandlerServiceFactory EditorCommandHandlerServiceFactory;
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly EditorOptionsService EditorOptionsService;
        protected readonly SVsServiceProvider ServiceProvider;

        public string DisplayName => FeaturesResources.Snippets;

        public AbstractSnippetCommandHandler(
            IThreadingContext threadingContext,
            SignatureHelpControllerProvider signatureHelpControllerProvider,
            IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            EditorOptionsService editorOptionsService,
            SVsServiceProvider serviceProvider)
            : base(threadingContext)
        {
            SignatureHelpControllerProvider = signatureHelpControllerProvider;
            EditorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
            EditorAdaptersFactoryService = editorAdaptersFactoryService;
            EditorOptionsService = editorOptionsService;
            ServiceProvider = serviceProvider;
        }

        protected abstract AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer);
        protected abstract bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken);
        protected abstract bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false);

        protected virtual bool TryInvokeSnippetPickerOnQuestionMark(ITextView textView, ITextBuffer textBuffer)
            => false;

        public bool ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient) &&
                snippetExpansionClient.TryHandleTab())
            {
                return true;
            }

            // Insert snippet/show picker only if we don't have a selection: the user probably wants to indent instead
            if (args.TextView.Selection.IsEmpty)
            {
                if (TryHandleTypedSnippet(args.TextView, args.SubjectBuffer, context.OperationContext.UserCancellationToken))
                {
                    return true;
                }

                if (TryInvokeSnippetPickerOnQuestionMark(args.TextView, args.SubjectBuffer))
                {
                    return true;
                }
            }

            return false;
        }

        public CommandState GetCommandState(TabKeyCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return CommandState.Unspecified;
            }

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextCommandHandler)
        {
            return nextCommandHandler();
        }

        public void ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
        {
            AssertIsForeground();
            if (AreSnippetsEnabled(args)
                && args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient)
                && snippetExpansionClient.IsFullMethodCallSnippet)
            {
                // Commit the snippet. Leave the caret in place, but clear the selection. Subsequent handlers in the
                // chain will handle the remaining Smart Break Line operations.
                snippetExpansionClient.CommitSnippet(leaveCaret: true);
                args.TextView.Selection.Clear();
            }

            nextCommandHandler();
        }

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient) &&
                snippetExpansionClient.TryHandleReturn())
            {
                return true;
            }

            return false;
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return CommandState.Unspecified;
            }

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient) &&
                snippetExpansionClient.TryHandleEscape())
            {
                return true;
            }

            return false;
        }

        public CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return CommandState.Unspecified;
            }

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(BackTabKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out AbstractSnippetExpansionClient snippetExpansionClient) &&
                snippetExpansionClient.TryHandleBackTab())
            {
                return true;
            }

            return false;
        }

        public CommandState GetCommandState(BackTabKeyCommandArgs args)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return CommandState.Unspecified;
            }

            if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
            {
                return CommandState.Unspecified;
            }

            return CommandState.Available;
        }

        public bool ExecuteCommand(InsertSnippetCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer);
        }

        public CommandState GetCommandState(InsertSnippetCommandArgs args)
        {
            AssertIsForeground();

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

        protected bool TryHandleTypedSnippet(ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var currentText = subjectBuffer.AsTextContainer().CurrentText;
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

            var endPositionInSubjectBuffer = textView.GetCaretPoint(subjectBuffer);
            if (endPositionInSubjectBuffer == null)
            {
                return false;
            }

            if (!SnippetUtilities.TryGetWordOnLeft(endPositionInSubjectBuffer.Value.Position, currentText, syntaxFactsService, out var span))
            {
                return false;
            }

            if (!IsSnippetExpansionContext(document, span.Value.Start, cancellationToken))
            {
                return false;
            }

            return GetSnippetExpansionClient(textView, subjectBuffer).TryInsertExpansion(span.Value.Start, span.Value.End, cancellationToken);
        }

        protected bool TryGetExpansionManager(out IVsExpansionManager expansionManager)
        {
            var textManager = (IVsTextManager2)ServiceProvider.GetService(typeof(SVsTextManager));
            if (textManager == null)
            {
                expansionManager = null;
                return false;
            }

            textManager.GetExpansionManager(out expansionManager);
            return expansionManager != null;
        }

        protected bool AreSnippetsEnabled(EditorCommandArgs args)
        {
            // Don't execute in cloud environment, should be handled by LSP
            if (args.SubjectBuffer.IsInLspEditorContext())
            {
                return false;
            }

            return EditorOptionsService.GlobalOptions.GetOption(SnippetsOptionsStorage.Snippets) &&
                // TODO (https://github.com/dotnet/roslyn/issues/5107): enable in interactive
                !(Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out var workspace) && workspace.Kind == WorkspaceKind.Interactive);
        }
    }
}
