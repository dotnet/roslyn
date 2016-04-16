// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractCompletionCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TabKeyCommandArgs>,
        ICommandHandler<ToggleCompletionModeCommandArgs>,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<InvokeCompletionListCommandArgs>,
        ICommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        ICommandHandler<PageUpKeyCommandArgs>,
        ICommandHandler<PageDownKeyCommandArgs>,
        ICommandHandler<CutCommandArgs>,
        ICommandHandler<PasteCommandArgs>,
        ICommandHandler<BackspaceKeyCommandArgs>,
        ICommandHandler<InsertSnippetCommandArgs>,
        ICommandHandler<SurroundWithCommandArgs>,
        ICommandHandler<AutomaticLineEnderCommandArgs>,
        ICommandHandler<SaveCommandArgs>,
        ICommandHandler<DeleteKeyCommandArgs>,
        ICommandHandler<SelectAllCommandArgs>
    {
        private readonly IAsyncCompletionService _completionService;

        protected AbstractCompletionCommandHandler(IAsyncCompletionService completionService)
        {
            _completionService = completionService;
        }

        private bool TryGetController(CommandArgs args, out Controller controller)
        {
            return _completionService.TryGetController(args.TextView, args.SubjectBuffer, out controller);
        }

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out ICommandHandler<TCommandArgs> commandHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();

            Controller controller;
            if (!TryGetController(args, out controller))
            {
                commandHandler = null;
                return false;
            }

            commandHandler = (ICommandHandler<TCommandArgs>)controller;
            return true;
        }

        private CommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args,
            Func<CommandState> nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();

            ICommandHandler<TCommandArgs> commandHandler;
            return TryGetControllerCommandHandler(args, out commandHandler)
                ? commandHandler.GetCommandState(args, nextHandler)
                : nextHandler();
        }

        private void ExecuteCommandWorker<TCommandArgs>(
            TCommandArgs args,
            Action nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();

            ICommandHandler<TCommandArgs> commandHandler;
            if (TryGetControllerCommandHandler(args, out commandHandler))
            {
                commandHandler.ExecuteCommand(args, nextHandler);
            }
            else
            {
                nextHandler();
            }
        }

        CommandState ICommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ICommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        void ICommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        internal bool TryHandleEscapeKey(EscapeKeyCommandArgs commandArgs)
        {
            Controller controller;
            if (!TryGetController(commandArgs, out controller))
            {
                return false;
            }

            return controller.TryHandleEscapeKey();
        }

        internal bool TryHandleUpKey(UpKeyCommandArgs commandArgs)
        {
            Controller controller;
            if (!TryGetController(commandArgs, out controller))
            {
                return false;
            }

            return controller.TryHandleUpKey();
        }

        internal bool TryHandleDownKey(DownKeyCommandArgs commandArgs)
        {
            Controller controller;
            if (!TryGetController(commandArgs, out controller))
            {
                return false;
            }

            return controller.TryHandleDownKey();
        }

        public CommandState GetCommandState(SaveCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(SaveCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        public CommandState GetCommandState(DeleteKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(DeleteKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        public CommandState GetCommandState(SelectAllCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(SelectAllCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }
    }
}
