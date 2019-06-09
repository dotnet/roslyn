// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractCompletionCommandHandler :
        ForegroundThreadAffinitizedObject,
        IChainedCommandHandler<TabKeyCommandArgs>,
        IChainedCommandHandler<ToggleCompletionModeCommandArgs>,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<ReturnKeyCommandArgs>,
        IChainedCommandHandler<InvokeCompletionListCommandArgs>,
        IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>,
        IChainedCommandHandler<PageUpKeyCommandArgs>,
        IChainedCommandHandler<PageDownKeyCommandArgs>,
        IChainedCommandHandler<CutCommandArgs>,
        IChainedCommandHandler<PasteCommandArgs>,
        IChainedCommandHandler<BackspaceKeyCommandArgs>,
        IChainedCommandHandler<InsertSnippetCommandArgs>,
        IChainedCommandHandler<SurroundWithCommandArgs>,
        IChainedCommandHandler<AutomaticLineEnderCommandArgs>,
        IChainedCommandHandler<SaveCommandArgs>,
        IChainedCommandHandler<DeleteKeyCommandArgs>,
        IChainedCommandHandler<SelectAllCommandArgs>,
        IChainedCommandHandler<EscapeKeyCommandArgs>,
        IChainedCommandHandler<UpKeyCommandArgs>,
        IChainedCommandHandler<DownKeyCommandArgs>
    {
        private readonly IAsyncCompletionService _completionService;

        public string DisplayName => EditorFeaturesResources.Code_Completion;

        protected AbstractCompletionCommandHandler(IThreadingContext threadingContext, IAsyncCompletionService completionService)
            : base(threadingContext)
        {
            _completionService = completionService;
        }

        private bool TryGetController(EditorCommandArgs args, out Controller controller)
        {
            return _completionService.TryGetController(args.TextView, args.SubjectBuffer, out controller);
        }

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out VSCommanding.ICommandHandler commandHandler)
            where TCommandArgs : EditorCommandArgs
        {
            AssertIsForeground();
            if (!TryGetController(args, out var controller))
            {
                commandHandler = null;
                return false;
            }

            commandHandler = controller;
            return true;
        }

        private VSCommanding.CommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args,
            Func<VSCommanding.CommandState> nextHandler)
            where TCommandArgs : EditorCommandArgs
        {
            AssertIsForeground();
            return TryGetControllerCommandHandler(args, out var commandHandler)
                ? commandHandler.GetCommandState(args, nextHandler)
                : nextHandler();
        }

        private void ExecuteCommandWorker<TCommandArgs>(
            TCommandArgs args,
            Action nextHandler,
            CommandExecutionContext context)
            where TCommandArgs : EditorCommandArgs
        {
            AssertIsForeground();
            if (TryGetControllerCommandHandler(args, out var commandHandler))
            {
                commandHandler.ExecuteCommand(args, nextHandler, context);
            }
            else
            {
                nextHandler();
            }
        }

        VSCommanding.CommandState IChainedCommandHandler<TabKeyCommandArgs>.GetCommandState(TabKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<TabKeyCommandArgs>.ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<ToggleCompletionModeCommandArgs>.GetCommandState(ToggleCompletionModeCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<ToggleCompletionModeCommandArgs>.ExecuteCommand(ToggleCompletionModeCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();

            Logger.Log(FunctionId.Completion_ExecuteCommand_TypeChar, a => a.TypedChar.ToString(), args);
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args, System.Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<PageUpKeyCommandArgs>.GetCommandState(PageUpKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<PageUpKeyCommandArgs>.ExecuteCommand(PageUpKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<PageDownKeyCommandArgs>.GetCommandState(PageDownKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<PageDownKeyCommandArgs>.ExecuteCommand(PageDownKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.GetCommandState(CommitUniqueCompletionListItemCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<CommitUniqueCompletionListItemCommandArgs>.ExecuteCommand(CommitUniqueCompletionListItemCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<BackspaceKeyCommandArgs>.GetCommandState(BackspaceKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<BackspaceKeyCommandArgs>.ExecuteCommand(BackspaceKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public VSCommanding.CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        void IChainedCommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        public VSCommanding.CommandState GetCommandState(SaveCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(SaveCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        public VSCommanding.CommandState GetCommandState(DeleteKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(DeleteKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        public VSCommanding.CommandState GetCommandState(SelectAllCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        public void ExecuteCommand(SelectAllCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        public VSCommanding.CommandState GetCommandState(EscapeKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (TryGetController(args, out var controller) && controller.TryHandleEscapeKey())
            {
                return;
            }

            nextHandler();
        }

        public VSCommanding.CommandState GetCommandState(UpKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(UpKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (TryGetController(args, out var controller) && controller.TryHandleUpKey())
            {
                return;
            }

            nextHandler();
        }

        public VSCommanding.CommandState GetCommandState(DownKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(DownKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (TryGetController(args, out var controller) && controller.TryHandleDownKey())
            {
                return;
            }

            nextHandler();
        }
    }
}
