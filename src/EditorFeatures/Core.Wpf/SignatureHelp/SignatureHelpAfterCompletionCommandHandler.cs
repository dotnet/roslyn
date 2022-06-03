// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    /// <summary>
    /// There are two forms of intellisense that may be active at the same time. Completion and
    /// SigHelp. Completion precedes SigHelp in <see cref="SignatureHelpBeforeCompletionCommandHandler"/> 
    /// because it wants to make sure
    /// it's operating on a buffer *after* Completion has changed it. i.e. if "WriteL(" is typed,
    /// sig help wants to allow completion to complete that to "WriteLine(" before it tried to
    /// proffer sig help. If we were to reverse things, then we'd get a bogus situation where sig
    /// help would see "WriteL(" would have nothing to offer and would return.
    /// 
    /// However, despite wanting sighelp to receive typechar first and then defer it to completion,
    /// we want completion to receive other events first (like escape, and navigation keys). We
    /// consider completion to have higher priority for those commands. In order to accomplish that,
    /// we introduced the current command handler. This command handler then delegates escape, up and
    /// down to those command handlers.     
    /// It is called after <see cref="PredefinedCompletionNames.CompletionCommandHandler"/>.
    /// </summary>
    [Export]
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    // Ensure roslyn comes after LSP to allow them to provide results.
    // https://github.com/dotnet/roslyn/issues/42338
    [Order(After = "LSP SignatureHelpCommandHandler")]
    internal class SignatureHelpAfterCompletionCommandHandler :
        AbstractSignatureHelpCommandHandler,
        IChainedCommandHandler<EscapeKeyCommandArgs>,
        IChainedCommandHandler<UpKeyCommandArgs>,
        IChainedCommandHandler<DownKeyCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Signature_Help;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpAfterCompletionCommandHandler(
            IThreadingContext threadingContext,
            SignatureHelpControllerProvider controllerProvider,
            IGlobalOptionService globalOptions)
            : base(threadingContext, controllerProvider, globalOptions)
        {
        }

        public CommandState GetCommandState(EscapeKeyCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public CommandState GetCommandState(UpKeyCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public CommandState GetCommandState(DownKeyCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            if (TryGetController(args, out var controller) && controller.TryHandleEscapeKey())
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(UpKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            if (TryGetController(args, out var controller) && controller.TryHandleUpKey())
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(DownKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            if (TryGetController(args, out var controller) && controller.TryHandleDownKey())
            {
                return;
            }

            nextHandler();
        }
    }
}
