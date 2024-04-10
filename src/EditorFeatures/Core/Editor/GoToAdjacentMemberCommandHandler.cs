// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name(PredefinedCommandHandlerNames.GoToAdjacentMember)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class GoToAdjacentMemberCommandHandler(IOutliningManagerService outliningManagerService) :
    ICommandHandler<GoToNextMemberCommandArgs>,
    ICommandHandler<GoToPreviousMemberCommandArgs>
{
    private readonly IOutliningManagerService _outliningManagerService = outliningManagerService;

    public string DisplayName => EditorFeaturesResources.Go_To_Adjacent_Member;

    public CommandState GetCommandState(GoToNextMemberCommandArgs args)
        => GetCommandStateImpl(args);

    public bool ExecuteCommand(GoToNextMemberCommandArgs args, CommandExecutionContext context)
        => ExecuteCommandImpl(args, gotoNextMember: true, context);

    public CommandState GetCommandState(GoToPreviousMemberCommandArgs args)
        => GetCommandStateImpl(args);

    public bool ExecuteCommand(GoToPreviousMemberCommandArgs args, CommandExecutionContext context)
        => ExecuteCommandImpl(args, gotoNextMember: false, context);

    private static CommandState GetCommandStateImpl(EditorCommandArgs args)
    {
        var subjectBuffer = args.SubjectBuffer;
        var caretPoint = args.TextView.GetCaretPoint(subjectBuffer);
        if (!caretPoint.HasValue || !subjectBuffer.SupportsNavigationToAnyPosition())
        {
            return CommandState.Unspecified;
        }

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document?.SupportsSyntaxTree != true)
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    private bool ExecuteCommandImpl(EditorCommandArgs args, bool gotoNextMember, CommandExecutionContext context)
    {
        var subjectBuffer = args.SubjectBuffer;
        var caretPoint = args.TextView.GetCaretPoint(subjectBuffer);
        if (!caretPoint.HasValue || !subjectBuffer.SupportsNavigationToAnyPosition())
        {
            return false;
        }

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        var syntaxFactsService = document?.GetLanguageService<ISyntaxFactsService>();
        if (syntaxFactsService == null)
        {
            return false;
        }

        int? targetPosition = null;
        using (context.OperationContext.AddScope(allowCancellation: true, description: EditorFeaturesResources.Navigating))
        {
            var root = document.GetSyntaxRootSynchronously(context.OperationContext.UserCancellationToken);
            targetPosition = GetTargetPosition(syntaxFactsService, root, caretPoint.Value.Position, gotoNextMember);
        }

        if (targetPosition != null)
        {
            args.TextView.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(subjectBuffer.CurrentSnapshot, targetPosition.Value), _outliningManagerService);
        }

        return true;
    }

    /// <summary>
    /// Internal for testing purposes.
    /// </summary>
    internal static int? GetTargetPosition(ISyntaxFactsService service, SyntaxNode root, int caretPosition, bool next)
    {
        var members = service.GetMethodLevelMembers(root);
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
