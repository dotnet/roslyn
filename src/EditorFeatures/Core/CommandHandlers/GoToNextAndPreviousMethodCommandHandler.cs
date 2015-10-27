// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.SuggestionSupport;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.GoToNextAndPreviousMethod, ContentTypeNames.RoslynContentType)]
    internal class GoToNextAndPreviousMethodCommandHandler : ICommandHandler<GoToNextMethodCommandArgs>, ICommandHandler<GoToPreviousMethodCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IOutliningManagerService _outliningManagerService;

        [ImportingConstructor]
        public GoToNextAndPreviousMethodCommandHandler(IWaitIndicator waitIndicator, IOutliningManagerService outliningManagerService)
        {
            _waitIndicator = waitIndicator;
            _outliningManagerService = outliningManagerService;
        }

        public CommandState GetCommandState(GoToNextMethodCommandArgs args, Func<CommandState> nextHandler)
            => GetCommandState(args.SubjectBuffer, args.TextView, nextHandler);

        public void ExecuteCommand(GoToNextMethodCommandArgs args, Action nextHandler)
            => ExecuteCommand(nextHandler, args.SubjectBuffer, args.TextView, next: true);

        public CommandState GetCommandState(GoToPreviousMethodCommandArgs args, Func<CommandState> nextHandler)
            => GetCommandState(args.SubjectBuffer, args.TextView, nextHandler);

        public void ExecuteCommand(GoToPreviousMethodCommandArgs args, Action nextHandler)
            => ExecuteCommand(nextHandler, args.SubjectBuffer, args.TextView, next: false);

        private static CommandState GetCommandState(ITextBuffer subjectBuffer, ITextView textView, Func<CommandState> nextHandler)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var documentSupportsSuggestionService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsSuggestionService>();
            if (document == null || !document.SupportsSyntaxTree || !documentSupportsSuggestionService.SupportsNavigationToAnyPosition(document))
            {
                return nextHandler();
            }

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                return nextHandler();
            }

            return CommandState.Available;
        }


        private void ExecuteCommand(Action nextHandler, ITextBuffer subjectBuffer, ITextView textView, bool next)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var documentSupportsSuggestionService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsSuggestionService>();
            if (document == null || !document.SupportsSyntaxTree || !documentSupportsSuggestionService.SupportsNavigationToAnyPosition(document))
            {
                nextHandler();
                return;
            }

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                nextHandler();
                return;
            }

            int? targetPosition = null;
            var waitResult = _waitIndicator.Wait(EditorFeaturesResources.Navigating, allowCancel: true, action: waitContext =>
            {
                targetPosition = GetTargetPosition(document, caretPoint.Value.Position, next, waitContext.CancellationToken);
            });

            if (waitResult == WaitIndicatorResult.Canceled || targetPosition == null)
            {
                return;
            }

            textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, targetPosition.Value), _outliningManagerService);
        }

        /// <summary>
        /// Internal for testing purposes.
        /// </summary>
        internal static int? GetTargetPosition(Document document, int caretPosition, bool next, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return null;
            }

            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var members = syntaxFactsService.GetMethodLevelMembers(root);

            if (members.Count == 0)
            {
                return null;
            }

            var currentMember = GetMember(syntaxFactsService, caretPosition, root);
            int indexOfCurrentMember;
            if (currentMember == null)
            {
                // We're not contained in any member currently.  Find the first member to start after our
                // position and use that.
                for (indexOfCurrentMember = 0; indexOfCurrentMember < members.Count; indexOfCurrentMember++)
                {
                    if (members[indexOfCurrentMember].FullSpan.Start > caretPosition)
                    {
                        break;
                    }
                }

                if (indexOfCurrentMember == members.Count)
                {
                    // After the last member, count ourselves as part of that last member, so that we wrap
                    // to the first, or back to the beginning of last.
                    indexOfCurrentMember--;
                }

                currentMember = members[indexOfCurrentMember];
            }
            else
            {
                indexOfCurrentMember = members.IndexOf(currentMember);
                if (indexOfCurrentMember < 0)
                {
                    Debug.Fail("Couldn't find current member in members?");
                    return null;
                }
            }

            if (next)
            {
                // If we're in leading trivia, we just want to go to the start of this member.
                if (caretPosition >= currentMember.Span.Start)
                {
                    indexOfCurrentMember++;
                    if (indexOfCurrentMember == members.Count)
                    {
                        indexOfCurrentMember = 0;
                    }
                }
            }
            else
            {
                // If we're in trailing trivia, we just want to go to the start of this member.
                if (caretPosition <= currentMember.Span.End)
                {
                    indexOfCurrentMember--;
                    if (indexOfCurrentMember < 0)
                    {
                        indexOfCurrentMember = members.Count - 1;
                    }
                }
            }

            // TODO: Better position within the node (e.g. attributes?)
            return members[indexOfCurrentMember].Span.Start;
        }

        private static SyntaxNode GetMember(ISyntaxFactsService syntaxFactService, int position, SyntaxNode root)
        {
            var node = root.FindToken(position).Parent;
            while (node != null && !syntaxFactService.IsMethodLevelMember(node))
            {
                node = node.Parent;
            }

            return node;
        }
    }
}
