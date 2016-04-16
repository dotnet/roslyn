// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    internal abstract class AbstractSnippetCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<BackTabKeyCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>
    {
        protected readonly IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;
        protected readonly SVsServiceProvider ServiceProvider;

        public AbstractSnippetCommandHandler(IVsEditorAdaptersFactoryService editorAdaptersFactoryService, SVsServiceProvider serviceProvider)
        {
            this.EditorAdaptersFactoryService = editorAdaptersFactoryService;
            this.ServiceProvider = serviceProvider;
        }

        protected abstract AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer);
        protected abstract bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken);
        protected abstract void InvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, Action nextHandler, bool surroundWith = false);

        protected virtual bool TryInvokeSnippetPickerOnQuestionMark(ITextView textView, ITextBuffer textBuffer)
        {
            return false;
        }

        public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            AbstractSnippetExpansionClient snippetExpansionClient;
            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out snippetExpansionClient) &&
                snippetExpansionClient.TryHandleTab())
            {
                return;
            }

            // Insert snippet/show picker only if we don't have a selection: the user probably wants to indent instead
            if (args.TextView.Selection.IsEmpty)
            {
                if (TryHandleTypedSnippet(args.TextView, args.SubjectBuffer))
                {
                    return;
                }

                if (TryInvokeSnippetPickerOnQuestionMark(args.TextView, args.SubjectBuffer))
                {
                    return;
                }
            }

            nextHandler();
        }

        public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
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

            return CommandState.Available;
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            AbstractSnippetExpansionClient snippetExpansionClient;
            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out snippetExpansionClient) &&
                snippetExpansionClient.TryHandleReturn())
            {
                return;
            }

            nextHandler();
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
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

            return CommandState.Available;
        }

        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            AbstractSnippetExpansionClient snippetExpansionClient;
            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out snippetExpansionClient) &&
                snippetExpansionClient.TryHandleEscape())
            {
                return;
            }

            nextHandler();
        }

        public CommandState GetCommandState(EscapeKeyCommandArgs args, Func<CommandState> nextHandler)
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

            return CommandState.Available;
        }

        public void ExecuteCommand(BackTabKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            AbstractSnippetExpansionClient snippetExpansionClient;
            if (args.TextView.Properties.TryGetProperty(typeof(AbstractSnippetExpansionClient), out snippetExpansionClient) &&
                snippetExpansionClient.TryHandleBackTab())
            {
                return;
            }

            nextHandler();
        }

        public CommandState GetCommandState(BackTabKeyCommandArgs args, Func<CommandState> nextHandler)
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

            return CommandState.Available;
        }

        public void ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (!AreSnippetsEnabled(args))
            {
                nextHandler();
                return;
            }

            InvokeInsertionUI(args.TextView, args.SubjectBuffer, nextHandler);
        }

        public CommandState GetCommandState(InsertSnippetCommandArgs args, Func<CommandState> nextHandler)
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

        protected bool TryHandleTypedSnippet(ITextView textView, ITextBuffer subjectBuffer)
        {
            AssertIsForeground();

            Document document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var currentText = subjectBuffer.AsTextContainer().CurrentText;
            var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

            var endPositionInSubjectBuffer = textView.GetCaretPoint(subjectBuffer);
            if (endPositionInSubjectBuffer == null)
            {
                return false;
            }

            var endPosition = endPositionInSubjectBuffer.Value.Position;
            var startPosition = endPosition;

            // Find the snippet shortcut
            while (startPosition > 0)
            {
                char c = currentText[startPosition - 1];
                if (!syntaxFactsService.IsIdentifierPartCharacter(c) && c != '#' && c != '~')
                {
                    break;
                }

                startPosition--;
            }

            if (startPosition == endPosition)
            {
                return false;
            }

            if (!IsSnippetExpansionContext(document, startPosition, CancellationToken.None))
            {
                return false;
            }

            return GetSnippetExpansionClient(textView, subjectBuffer).TryInsertExpansion(startPosition, endPosition);
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

        protected static bool AreSnippetsEnabled(CommandArgs args)
        {
            Workspace workspace;
            return args.SubjectBuffer.GetOption(InternalFeatureOnOffOptions.Snippets) &&
                // TODO (https://github.com/dotnet/roslyn/issues/5107): enable in interactive
                !(Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out workspace) && workspace.Kind == WorkspaceKind.Interactive);
        }
    }
}
