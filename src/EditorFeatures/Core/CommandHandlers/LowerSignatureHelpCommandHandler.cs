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
    [Export]
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.LowerSignatureHelp)]
    [Order(After = PredefinedCommandHandlerNames.Completion)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal class LowerSignatureHelpCommandHandler :
        AbstractSignatureHelpCommandHandler,
        IChainedCommandHandler<EscapeKeyCommandArgs>,
        IChainedCommandHandler<UpKeyCommandArgs>,
        IChainedCommandHandler<DownKeyCommandArgs>
    {
        public string DisplayName => EditorFeaturesResources.Lower_Signature_Help;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LowerSignatureHelpCommandHandler(
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
