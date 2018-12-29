﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToAdjacentMember)]
    internal class GoToAdjacentMemberCommandHandler :
        VSCommanding.ICommandHandler<GoToNextMemberCommandArgs>,
        VSCommanding.ICommandHandler<GoToPreviousMemberCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService;

        public string DisplayName => EditorFeaturesResources.Go_To_Adjacent_Member;

        [ImportingConstructor]
        public GoToAdjacentMemberCommandHandler(IOutliningManagerService outliningManagerService)
        {
            _outliningManagerService = outliningManagerService;
        }

        public VSCommanding.CommandState GetCommandState(GoToNextMemberCommandArgs args)
        {
            return GetCommandStateImpl(args);
        }

        public bool ExecuteCommand(GoToNextMemberCommandArgs args, CommandExecutionContext context)
        {
            return ExecuteCommandImpl(args, gotoNextMember: true, context);
        }

        public VSCommanding.CommandState GetCommandState(GoToPreviousMemberCommandArgs args)
        {
            return GetCommandStateImpl(args);
        }

        public bool ExecuteCommand(GoToPreviousMemberCommandArgs args, CommandExecutionContext context)
        {
            return ExecuteCommandImpl(args, gotoNextMember: false, context);
        }

        private VSCommanding.CommandState GetCommandStateImpl(EditorCommandArgs args)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            return IsAvailable(document, caretPoint) ? VSCommanding.CommandState.Available : VSCommanding.CommandState.Unspecified;
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

        private bool ExecuteCommandImpl(EditorCommandArgs args, bool gotoNextMember, CommandExecutionContext context)
        {
            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var caretPoint = args.TextView.GetCaretPoint(args.SubjectBuffer);
            if (!IsAvailable(document, caretPoint))
            {
                return false;
            }

            int? targetPosition = null;

            using (context.OperationContext.AddScope(allowCancellation: true, description: EditorFeaturesResources.Navigating))
            {
                var task = GetTargetPositionAsync(document, caretPoint.Value.Position, gotoNextMember, context.OperationContext.UserCancellationToken);
                targetPosition = task.WaitAndGetResult(context.OperationContext.UserCancellationToken);
            }

            if (targetPosition != null)
            {
                args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(args.SubjectBuffer.CurrentSnapshot, targetPosition.Value), _outliningManagerService);
            }

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
