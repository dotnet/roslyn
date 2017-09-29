// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractCompletionCommandHandler :
        ForegroundThreadAffinitizedObject,
        ILegacyCommandHandler<TabKeyCommandArgs>,
        ILegacyCommandHandler<ToggleCompletionModeCommandArgs>,
        ILegacyCommandHandler<TypeCharCommandArgs>,
        ILegacyCommandHandler<ReturnKeyCommandArgs>,
        ILegacyCommandHandler<InvokeCompletionListCommandArgs>,
        ILegacyCommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        ILegacyCommandHandler<PageUpKeyCommandArgs>,
        ILegacyCommandHandler<PageDownKeyCommandArgs>,
        ILegacyCommandHandler<CutCommandArgs>,
        ILegacyCommandHandler<PasteCommandArgs>,
        ILegacyCommandHandler<BackspaceKeyCommandArgs>,
        ILegacyCommandHandler<InsertSnippetCommandArgs>,
        ILegacyCommandHandler<SurroundWithCommandArgs>,
        ILegacyCommandHandler<AutomaticLineEnderCommandArgs>,
        ILegacyCommandHandler<SaveCommandArgs>,
        ILegacyCommandHandler<DeleteKeyCommandArgs>,
        ILegacyCommandHandler<SelectAllCommandArgs>
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

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out ILegacyCommandHandler<TCommandArgs> commandHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            if (!TryGetController(args, out var controller))
            {
                commandHandler = null;
                return false;
            }

            commandHandler = (ILegacyCommandHandler<TCommandArgs>)controller;
            return true;
        }

        private CommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args,
            Func<CommandState> nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            return TryGetControllerCommandHandler(args, out var commandHandler)
                ? commandHandler.GetCommandState(args, nextHandler)
                : nextHandler();
        }

        private void ExecuteCommandWorker<TCommandArgs>(
            TCommandArgs args,
            Action nextHandler)
            where TCommandArgs : CommandArgs
        {
            AssertIsForeground();
            if (TryGetControllerCommandHandler(args, out var commandHandler))
            {
                commandHandler.ExecuteCommand(args, nextHandler);
            }
            else
            {
                nextHandler();
            }
        }

        CommandState ILegacyCommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, System.Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ILegacyCommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
        }

        CommandState ILegacyCommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<CommandState> nextHandler)
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

        void ILegacyCommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler);
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
