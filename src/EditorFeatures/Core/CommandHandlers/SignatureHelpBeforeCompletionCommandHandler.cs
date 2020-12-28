﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    /// <summary>
    /// There are two forms of intellisense that may be active at the same time. Completion and
    /// SigHelp. Completion precedes SigHelp in the command handler because it wants to make sure
    /// it's operating on a buffer *after* Completion has changed it. i.e. if "WriteL(" is typed,
    /// sig help wants to allow completion to complete that to "WriteLine(" before it tried to
    /// proffer sig help. If we were to reverse things, then we'd get a bogus situation where sig
    /// help would see "WriteL(" would have nothing to offer and would return.
    /// 
    /// However, despite wanting sighelp to receive typechar first and then defer it to completion,
    /// we want completion to receive other events first (like escape, and navigation keys). We
    /// consider completion to have higher priority for those commands. In order to accomplish that,
    /// we introduced <see cref="SignatureHelpAfterCompletionCommandHandler"/>
    /// This command handler then delegates escape, up and down to those command handlers. 
    /// It is called before <see cref="PredefinedCompletionNames.CompletionCommandHandler"/>.
    /// </summary>
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.SignatureHelpBeforeCompletion)]
    [Order(Before = PredefinedCompletionNames.CompletionCommandHandler)]
    // Ensure roslyn comes after LSP to allow them to provide results.
    // https://github.com/dotnet/roslyn/issues/42338
    [Order(After = "LSP SignatureHelpCommandHandler")]
    internal class SignatureHelpBeforeCompletionCommandHandler :
        AbstractSignatureHelpCommandHandler,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<InvokeSignatureHelpCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Signature_Help;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpBeforeCompletionCommandHandler(
            IThreadingContext threadingContext,
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
            IAsyncCompletionBroker completionBroker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, signatureHelpProviders, signatureHelpPresenters, completionBroker, listenerProvider)
        {
        }

        private bool TryGetControllerCommandHandler<TCommandArgs>(TCommandArgs args, out ICommandHandler commandHandler)
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

        private CommandState GetCommandStateWorker<TCommandArgs>(
            TCommandArgs args,
            Func<CommandState> nextHandler)
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
            if (!TryGetControllerCommandHandler(args, out var commandHandler))
            {
                nextHandler();
            }
            else
            {
                commandHandler.ExecuteCommand(args, nextHandler, context);
            }
        }

        CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        CommandState IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.GetCommandState(InvokeSignatureHelpCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return CommandState.Available;
        }

        void IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.ExecuteCommand(InvokeSignatureHelpCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }
    }
}
