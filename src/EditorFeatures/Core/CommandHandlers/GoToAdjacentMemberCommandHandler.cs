// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Roslyn.Utilities;
using EditorCommanding = Microsoft.VisualStudio.Text.UI.Commanding;
using EditorCommands = Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [EditorCommanding.ExportCommandHandler(PredefinedCommandHandlerNames.GoToAdjacentMember, ContentTypeNames.RoslynContentType)]
    internal class GoToAdjacentMemberCommandHandler : 
        EditorCommanding.ICommandHandler<EditorCommands.GoToNextMemberCommandArgs>,
        EditorCommanding.ICommandHandler<EditorCommands.GoToPreviousMemberCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly IOutliningManagerService _outliningManagerService;

        public bool InterestedInReadOnlyBuffer => true;

        [ImportingConstructor]
        public GoToAdjacentMemberCommandHandler(IWaitIndicator waitIndicator, IOutliningManagerService outliningManagerService)
        {
            _waitIndicator = waitIndicator;
            _outliningManagerService = outliningManagerService;
        }

        public EditorCommanding.CommandState GetCommandState(EditorCommands.GoToNextMemberCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            return IsAvailable(document, caretPoint) ? EditorCommanding.CommandState.CommandIsAvailable : EditorCommanding.CommandState.CommandIsUnavailable;
        }

        public EditorCommanding.CommandState GetCommandState(EditorCommands.GoToPreviousMemberCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            return IsAvailable(document, caretPoint) ? EditorCommanding.CommandState.CommandIsAvailable : EditorCommanding.CommandState.CommandIsUnavailable;
        }

        private static bool IsAvailable(Document document, SnapshotPoint? caretPoint)
        {
            if (document?.SupportsSyntaxTree != true)
            {
                return false;
            }

            if (!caretPoint.HasValue)
            {
                return false;
            }

            var documentSupportsFeatureService = document.Project.Solution.Workspace.Services.GetService<IDocumentSupportsFeatureService>();
            return documentSupportsFeatureService?.SupportsNavigationToAnyPosition(document) == true;
        }

        public bool ExecuteCommand(EditorCommands.GoToNextMemberCommandArgs args)
        {
            return ExecuteCommand(args.TextView, args.SubjectBuffer, next: true);
        }

        public bool ExecuteCommand(EditorCommands.GoToPreviousMemberCommandArgs args)
        {
            return ExecuteCommand(args.TextView, args.SubjectBuffer, next: false);
        }

        private bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, bool next)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!IsAvailable(document, caretPoint))
            {
                return false;
            }

            int? targetPosition = null;
            var waitResult = _waitIndicator.Wait(EditorFeaturesResources.Navigating, allowCancel: true, action: waitContext =>
            {
                var task = GetTargetPositionAsync(document, caretPoint.Value.Position, next, waitContext.CancellationToken);
                targetPosition = task.WaitAndGetResult(waitContext.CancellationToken);
            });

            if (waitResult == WaitIndicatorResult.Canceled || targetPosition == null)
            {
                return false;
            }

            textView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, targetPosition.Value), _outliningManagerService);
            return true;
        }

        /// <summary>
        /// Internal for testing purposes.
        /// </summary>
        internal static async Task<int?> GetTargetPositionAsync(Document document, int caretPosition, bool next, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                return null;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(true);
            var members = syntaxFactsService.GetMethodLevelMembers(root);
            if (members.Count == 0)
            {
                return null;
            }

            var starts = members.Select(m => MemberStart(m)).ToArray();
            var index = Array.BinarySearch(starts, caretPosition);
            if (index >= 0)
            {
                // We're actually contained in a member, go to the next or previous.
                index = next ? index + 1 : index - 1;
            }
            else
            {
                // We're in between to members, ~index gives us the member we're before, so we'll just
                // advance to the start of it
                index = next ? ~index : ~index - 1;
            }

            // Wrap if necessary
            if (index >= members.Count)
            {
                index = 0;
            }
            else if (index < 0)
            {
                index = members.Count - 1;
            }

            return MemberStart(members[index]);
        }

        private static int MemberStart(SyntaxNode node)
        {
            // TODO: Better position within the node (e.g. attributes?)
            return node.SpanStart;
        }
    }
}
