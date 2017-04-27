// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using EditorCommands = Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;
using VSInsertSnippetCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InsertCommentCommandArgs;
using VSInvokeCompletionListCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InvokeCompletionListCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractCompletionCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<ToggleCompletionModeCommandArgs>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>,
        ICommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        ICommandHandler<PageUpKeyCommandArgs>,
        ICommandHandler<PageDownKeyCommandArgs>,
        ICommandHandler<CutCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<BackspaceKeyCommandArgs>,
        VSC.ICommandHandler<VSInsertSnippetCommandArgs>,
        VSC.ICommandHandler<EditorCommands.SurroundWithCommandArgs>,
        ICommandHandler<AutomaticLineEnderCommandArgs>,
        ICommandHandler<SaveCommandArgs>,
        ICommandHandler<DeleteKeyCommandArgs>,
        ICommandHandler<SelectAllCommandArgs>
    {
        private readonly IAsyncCompletionService _completionService;

        public bool InterestedInReadOnlyBuffer => false;

        protected AbstractCompletionCommandHandler(IAsyncCompletionService completionService)
        {
            _completionService = completionService;
        }

        private bool TryGetController(CommandArgs args, out Controller controller)
        {
            return _completionService.TryGetController(args.TextView, args.SubjectBuffer, out controller);
        }

        private bool TryGetController(VSC.Commands.CommandArgs args, out Controller controller)
        {
            return _completionService.TryGetController(args.TextView, args.SubjectBuffer, out controller);
        }

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out VSC.ICommandHandler<TCommandArgs> commandHandler)
            where TCommandArgs : VSC.Commands.CommandArgs
        {
            AssertIsForeground();
            if (!TryGetController(args, out var controller))
            {
                commandHandler = null;
                return false;
            }

            commandHandler = (VSC.ICommandHandler<TCommandArgs>)controller;
            return true;
        }

        private VSCommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args)
            where TCommandArgs : VSC.Commands.CommandArgs
        {
            AssertIsForeground();
            return TryGetControllerCommandHandler(args, out var commandHandler)
                ? commandHandler.GetCommandState(args)
                : VSCommandState.CommandIsUnavailable;
        }

        private bool ExecuteCommandWorker<TCommandArgs>(
            TCommandArgs args)
            where TCommandArgs : VSC.Commands.CommandArgs
        {
            AssertIsForeground();
            if (TryGetControllerCommandHandler(args, out var commandHandler))
            {
                return commandHandler.ExecuteCommand(args);
            }
            else
            {
                return false;
            }
        }

        CommandState ICommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        VSCommandState VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>.GetCommandState(VSInvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>.ExecuteCommand(VSInvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        CommandState ICommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool ICommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        VSCommandState VSC.ICommandHandler<VSInsertSnippetCommandArgs>.GetCommandState(VSInsertSnippetCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool VSC.ICommandHandler<VSInsertSnippetCommandArgs>.ExecuteCommand(VSInsertSnippetCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        VSC.CommandState VSC.ICommandHandler<EditorCommands.SurroundWithCommandArgs>.GetCommandState(EditorCommands.SurroundWithCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        public CommandState GetCommandState(AutomaticLineEnderCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        public bool ExecuteCommand(AutomaticLineEnderCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        bool VSC.ICommandHandler<EditorCommands.SurroundWithCommandArgs>.ExecuteCommand(EditorCommands.SurroundWithCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        internal bool TryHandleEscapeKey(EscapeKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleEscapeKey();
        }

        internal bool TryHandleUpKey(UpKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleUpKey();
        }

        internal bool TryHandleDownKey(DownKeyCommandArgs commandArgs)
        {
            if (!TryGetController(commandArgs, out var controller))
            {
                return false;
            }

            return controller.TryHandleDownKey();
        }

        public CommandState GetCommandState(SaveCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        public bool ExecuteCommand(SaveCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        public CommandState GetCommandState(DeleteKeyCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        public bool ExecuteCommand(DeleteKeyCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        public CommandState GetCommandState(SelectAllCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        public bool ExecuteCommand(SelectAllCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }
    }
}
