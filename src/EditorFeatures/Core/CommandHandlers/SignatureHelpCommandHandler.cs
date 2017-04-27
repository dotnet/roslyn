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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using VSInvokeSignatureHelpCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InvokeSignatureHelpCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    [Export]
    [VSC.ExportCommandHandler(PredefinedCommandHandlerNames.SignatureHelp, ContentTypeNames.RoslynContentType)]
    [Order(Before = PredefinedCommandHandlerNames.Completion)]
    internal class SignatureHelpCommandHandler :
        ForegroundThreadAffinitizedObject,
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<VSInvokeSignatureHelpCommandArgs>
    {
        private readonly IInlineRenameService _inlineRenameService;
        private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _asyncListeners;
        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;

        public bool InterestedInReadOnlyBuffer => false;

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

            // If we don't have a presenter, then there's no point in us even being involved.  Just
            // defer to the next handler in the chain.
            if (_signatureHelpPresenter == null)
            {
                controller = null;
                return false;
            }

            controller = Controller.GetInstance(
                args.TextView, args.SubjectBuffer, _signatureHelpPresenter,
                new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.SignatureHelp),
                _signatureHelpProviders);

            return true;
        }

        private bool TryGetController(VSC.Commands.CommandArgs args, out Controller controller)
        {
            AssertIsForeground();

            // If args is `InvokeSignatureHelpCommandArgs` then sig help was explicitly invoked by the user and should
            // be shown whether or not the option is set.
            if (!(args is VSInvokeSignatureHelpCommandArgs) && !args.SubjectBuffer.GetFeatureOnOffOption(SignatureHelpOptions.ShowSignatureHelp))
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
                args.TextView, args.SubjectBuffer, _signatureHelpPresenter,
                new AggregateAsynchronousOperationListener(_asyncListeners, FeatureAttribute.SignatureHelp),
                _signatureHelpProviders);

            return true;
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
            if (!TryGetControllerCommandHandler(args, out var commandHandler))
            {
                return false;
            }
            else
            {
                return commandHandler.ExecuteCommand(args);
            }
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

        VSCommandState VSC.ICommandHandler<VSInvokeSignatureHelpCommandArgs>.GetCommandState(VSInvokeSignatureHelpCommandArgs args)
        {
            AssertIsForeground();
            return GetCommandStateWorker(args);
        }

        bool VSC.ICommandHandler<VSInvokeSignatureHelpCommandArgs>.ExecuteCommand(VSInvokeSignatureHelpCommandArgs args)
        {
            AssertIsForeground();
            return ExecuteCommandWorker(args);
        }

        internal bool TryHandleEscapeKey(VSC.Commands.EscapeKeyCommandArgs commandArgs)
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
