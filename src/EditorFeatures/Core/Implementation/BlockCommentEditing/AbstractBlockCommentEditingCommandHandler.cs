// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.Operations;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using VSEditorCommands = Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
{
    /// <summary>
    /// This class implements both legacy and modern editor command handler becuase TypeScript
    /// uses it to implement legacy Microsoft.CodeAnalysis.Editor.ICommandHandler based command.
    /// Once TypeScript migrates to the modern editor commanding (tracked by
    /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/548409), the part implementing
    /// Microsoft.CodeAnalysis.Editor.ICommandHandler can be deleted.
    /// </summary>
    internal abstract class AbstractBlockCommentEditingCommandHandler : BaseAbstractBlockCommentEditingCommandHandler,
        ICommandHandler<ReturnKeyCommandArgs>,
        VSCommanding.ICommandHandler<VSEditorCommands.ReturnKeyCommandArgs>
    {
        protected AbstractBlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        #region Legacy ICommandHandler

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler) => nextHandler();

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            if (TryHandleReturnKey(args.SubjectBuffer, args.TextView))
            {
                return;
            }

            nextHandler();
        }

        #endregion

        #region Modern editor ICommandHandler

        public string DisplayName => EditorFeaturesResources.Block_Comment_Editing;

        public VSCommanding.CommandState GetCommandState(VSEditorCommands.ReturnKeyCommandArgs args) => VSCommanding.CommandState.Unspecified;

        public bool ExecuteCommand(VSEditorCommands.ReturnKeyCommandArgs args, VSCommanding.CommandExecutionContext context)
        {
            return TryHandleReturnKey(args.SubjectBuffer, args.TextView);
        }

        #endregion
    }
}
