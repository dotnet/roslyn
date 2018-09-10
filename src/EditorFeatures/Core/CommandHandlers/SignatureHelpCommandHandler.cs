// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.SignatureHelp)]
    [Order(Before = PredefinedCommandHandlerNames.Completion)]
    internal class SignatureHelpCommandHandler :
        ForegroundThreadAffinitizedObject,
        IChainedCommandHandler<TypeCharCommandArgs>,
        IChainedCommandHandler<InvokeSignatureHelpCommandArgs>
    {
        private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;

        public string DisplayName => EditorFeaturesResources.Signature_Help;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpCommandHandler(
            IThreadingContext threadingContext,
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext)
        {
            _signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders);
            _listener = listenerProvider.GetListener(FeatureAttribute.SignatureHelp);
            _signatureHelpPresenter = ExtensionOrderer.Order(signatureHelpPresenters).Select(lazy => lazy.Value).FirstOrDefault();
        }

        private bool TryGetController(EditorCommandArgs args, out Controller controller)
        {
            AssertIsForeground();

            // If args is `InvokeSignatureHelpCommandArgs` then sig help was explicitly invoked by the user and should
            // be shown whether or not the option is set.
            if (!(args is InvokeSignatureHelpCommandArgs) && !args.SubjectBuffer.GetFeatureOnOffOption(SignatureHelpOptions.ShowSignatureHelp))
            {
                controller = null;
                return false;
            }

            // If we don't have a presenter, then there's no point in us even being involved.  Just
            // defer to the next handler in the chain.
            if (_signatureHelpPresenter == null)
            {
                controller = null;
                return false;
            }

            controller = Controller.GetInstance(
                ThreadingContext,
                args, _signatureHelpPresenter,
                _listener, _signatureHelpProviders);

            return true;
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
            if (!TryGetControllerCommandHandler(args, out var commandHandler))
            {
                nextHandler();
            }
            else
            {
                commandHandler.ExecuteCommand(args, nextHandler, context);
            }
        }

        VSCommanding.CommandState IChainedCommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
        }

        VSCommanding.CommandState IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.GetCommandState(InvokeSignatureHelpCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void IChainedCommandHandler<InvokeSignatureHelpCommandArgs>.ExecuteCommand(InvokeSignatureHelpCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            ExecuteCommandWorker(args, nextHandler, context);
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
    }
}
