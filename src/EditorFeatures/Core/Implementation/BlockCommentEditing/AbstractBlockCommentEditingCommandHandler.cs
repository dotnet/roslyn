// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.Operations;
using VSCommanding = Microsoft.VisualStudio.Commanding;
using VSEditorCommands = Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.BlockCommentEditing
{
    internal abstract class AbstractBlockCommentEditingCommandHandler : BaseAbstractBlockCommentEditingCommandHandler,
        VSCommanding.ICommandHandler<VSEditorCommands.ReturnKeyCommandArgs>
    {
        protected AbstractBlockCommentEditingCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        public string DisplayName => EditorFeaturesResources.Block_Comment_Editing;

        public VSCommanding.CommandState GetCommandState(VSEditorCommands.ReturnKeyCommandArgs args) => VSCommanding.CommandState.Unspecified;

        public bool ExecuteCommand(VSEditorCommands.ReturnKeyCommandArgs args, VSCommanding.CommandExecutionContext context)
        {
            return TryHandleReturnKey(args.SubjectBuffer, args.TextView);
        }
    }
}
