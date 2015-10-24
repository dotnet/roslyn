// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    [ExportCommandHandler("Go to next and previous method command handler", ContentTypeNames.RoslynContentType)]
    class GoToNextAndPreviousMethodCommandHandler : ICommandHandler<GoToNextMethodCommandArgs>, ICommandHandler<GoToPreviousMethodCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;

        [ImportingConstructor]
        public GoToNextAndPreviousMethodCommandHandler(IWaitIndicator waitIndicator)
        {
            _waitIndicator = waitIndicator;
        }

        public CommandState GetCommandState(GoToNextMethodCommandArgs args, Func<CommandState> nextHandler)
            => GetCommandState(args.SubjectBuffer, nextHandler);

        public void ExecuteCommand(GoToNextMethodCommandArgs args, Action nextHandler)
            => ExecuteCommand(nextHandler, args.SubjectBuffer, args.TextView, next: true);

        public CommandState GetCommandState(GoToPreviousMethodCommandArgs args, Func<CommandState> nextHandler)
            => GetCommandState(args.SubjectBuffer, nextHandler);

        public void ExecuteCommand(GoToPreviousMethodCommandArgs args, Action nextHandler)
            => ExecuteCommand(nextHandler, args.SubjectBuffer, args.TextView, next: false);

        private static CommandState GetCommandState(ITextBuffer subjectBuffer, Func<CommandState> nextHandler)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null || !document.SupportsSyntaxTree)
            {
                return nextHandler();
            }

            return CommandState.Available;
        }


        private void ExecuteCommand(Action nextHandler, ITextBuffer subjectBuffer, ITextView textView, bool next)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null || !document.SupportsSyntaxTree)
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
            var waitResult = _waitIndicator.Wait("", allowCancel: true, action: waitContext =>
            {
                targetPosition = GetTargetPosition(document, caretPoint.Value.Position, next, waitContext.CancellationToken);
            });

            if (waitResult == WaitIndicatorResult.Canceled || targetPosition == null)
            {
                return;
            }

            var targetLocation = new SnapshotPoint(subjectBuffer.CurrentSnapshot, targetPosition.Value);
            var viewLocation = textView.BufferGraph.MapUpToBuffer(targetLocation, PointTrackingMode.Positive, PositionAffinity.Successor, textView.TextBuffer);
            if (viewLocation.HasValue)
            {
                textView.Caret.MoveTo(viewLocation.Value);
            }
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
            var currentMember = GetMember(syntaxFactsService, caretPosition, root);
            if (currentMember == null)
            {
                return null;
            }

            var members = syntaxFactsService.GetMethodLevelMembers(root);
            var index = members.IndexOf(currentMember);
            if (index < 0)
            {
                Debug.Fail("Couldn't find current member in members?");
                return null;
            }

            if (next)
            {
                // If we're in leading trivia, we just want to go to the start of this member.
                if (caretPosition >= currentMember.Span.Start)
                {
                    index++;
                    if (index == members.Count)
                    {
                        index = 0;
                    }
                }
            }
            else
            {
                // If we're in trailing trivia, we just want to go to the start of this member.
                if (caretPosition <= currentMember.Span.End)
                {
                    index--;
                    if (index < 0)
                    {
                        index = members.Count - 1;
                    }
                }
            }

            // TODO: Better position within the node (e.g. attributes?)
            return members[index].Span.Start;
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
