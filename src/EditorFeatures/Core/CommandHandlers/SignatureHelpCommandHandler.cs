// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    [ExportCommandHandler(PredefinedCommandHandlerNames.SignatureHelp, ContentTypeNames.RoslynContentType)]
    [Order(Before = PredefinedCommandHandlerNames.Completion)]
    internal class SignatureHelpCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<InvokeSignatureHelpCommandArgs>
    {
        private readonly IInlineRenameService _inlineRenameService;
        private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;
        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;

        [ImportingConstructor]
        public SignatureHelpCommandHandler(
            IInlineRenameService inlineRenameService,
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters)
            : this(inlineRenameService,
                   ExtensionOrderer.Order(signatureHelpPresenters).Select(lazy => lazy.Value).FirstOrDefault(),
                   signatureHelpProviders, asyncListeners)
        {
        }

        // For testing purposes.
        public SignatureHelpCommandHandler(
            IInlineRenameService inlineRenameService,
            IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> signatureHelpPresenter,
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _inlineRenameService = inlineRenameService;
            _signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders);
            _asyncListeners = asyncListeners;
            _signatureHelpPresenter = signatureHelpPresenter;
        }

        private bool TryGetController(CommandArgs args, out Controller controller)
        {
            AssertIsForeground();

            // If args is `InvokeSignatureHelpCommandArgs` then sig help was explicitly invoked by the user and should
            // be shown whether or not the option is set.
            if (!(args is InvokeSignatureHelpCommandArgs) && !args.SubjectBuffer.GetOption(SignatureHelpOptions.ShowSignatureHelp))
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
                args, _signatureHelpPresenter,
                new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.SignatureHelp),
                _signatureHelpProviders);

            return true;
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
            if (!TryGetControllerCommandHandler(args, out commandHandler))
            {
                nextHandler();
            }
            else
            {
                commandHandler.ExecuteCommand(args, nextHandler);
            }
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

        CommandState ICommandHandler<InvokeSignatureHelpCommandArgs>.GetCommandState(InvokeSignatureHelpCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args, nextHandler);
        }

        void ICommandHandler<InvokeSignatureHelpCommandArgs>.ExecuteCommand(InvokeSignatureHelpCommandArgs args, Action nextHandler)
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
    }
}
