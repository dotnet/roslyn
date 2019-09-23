// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

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
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.SignatureHelpAfterCompletion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
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
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext, signatureHelpProviders, signatureHelpPresenters, listenerProvider)
        {
        }

        public VSCommanding.CommandState GetCommandState(EscapeKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public VSCommanding.CommandState GetCommandState(UpKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public VSCommanding.CommandState GetCommandState(DownKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

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
