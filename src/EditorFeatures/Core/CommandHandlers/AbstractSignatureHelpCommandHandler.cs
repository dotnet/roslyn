// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    internal abstract class AbstractSignatureHelpCommandHandler :
        ForegroundThreadAffinitizedObject
    {
        private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;

        public AbstractSignatureHelpCommandHandler(
            IThreadingContext threadingContext,
            IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext)
        {
            _signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders);
            _listener = listenerProvider.GetListener(FeatureAttribute.SignatureHelp);
            _signatureHelpPresenter = ExtensionOrderer.Order(signatureHelpPresenters).Select(lazy => lazy.Value).FirstOrDefault();
        }

        protected bool TryGetController(EditorCommandArgs args, out Controller controller)
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
    }
}
