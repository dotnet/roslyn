// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.Expansion;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal abstract class AbstractSnippetCommandHandler :
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<BackTabKeyCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<EscapeKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>
    {
        protected readonly IThreadingContext ThreadingContext;
        protected readonly IExpansionServiceProvider ExpansionServiceProvider;
        protected readonly IExpansionManager ExpansionManager;
        protected readonly IGlobalOptionService GlobalOptions;

        public string DisplayName => FeaturesResources.Snippets;

        public AbstractSnippetCommandHandler(
            IThreadingContext threadingContext,
            IExpansionServiceProvider expansionServiceProvider,
            IExpansionManager expansionManager,
            IGlobalOptionService globalOptions)
        {
            ThreadingContext = threadingContext;
            ExpansionServiceProvider = expansionServiceProvider;
            ExpansionManager = expansionManager;
            GlobalOptions = globalOptions;
        }

        protected abstract AbstractSnippetExpansionClient GetSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer);
        protected abstract bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken);
        protected abstract bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false);

        protected virtual bool TryInvokeSnippetPickerOnQuestionMark(ITextView textView, ITextBuffer textBuffer)
        {
            return false;
        }

        public bool ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext context)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
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
                if (TryHandleTypedSnippet(args.TextView, args.SubjectBuffer))
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
            ThreadingContext.ThrowIfNotOnUIThread();

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

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            ThreadingContext.ThrowIfNotOnUIThread();
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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            ThreadingContext.ThrowIfNotOnUIThread();
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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            ThreadingContext.ThrowIfNotOnUIThread();
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
            ThreadingContext.ThrowIfNotOnUIThread();

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
            ThreadingContext.ThrowIfNotOnUIThread();

            if (!AreSnippetsEnabled(args))
            {
                return false;
            }

            return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer);
        }

        public CommandState GetCommandState(InsertSnippetCommandArgs args)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

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

        protected bool TryHandleTypedSnippet(ITextView textView, ITextBuffer subjectBuffer)
        {
            ThreadingContext.ThrowIfNotOnUIThread();

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var currentText = subjectBuffer.AsTextContainer().CurrentText;
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

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
                var c = currentText[startPosition - 1];
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

        protected bool AreSnippetsEnabled(EditorCommandArgs args)
        {
            return GlobalOptions.GetOption(InternalFeatureOnOffOptions.Snippets) &&
                // TODO (https://github.com/dotnet/roslyn/issues/5107): enable in interactive
                !(Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out var workspace) && workspace.Kind == WorkspaceKind.Interactive);
        }
    }
}
