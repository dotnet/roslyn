// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    [Export]
    [Shared]
    internal class SignatureHelpControllerProvider : ForegroundThreadAffinitizedObject
    {
        private static readonly object s_controllerPropertyKey = new();

        private readonly IGlobalOptionService _globalOptions;
        private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IAsyncCompletionBroker _completionBroker;
        private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpControllerProvider(
            IGlobalOptionService globalOptions,
            IThreadingContext threadingContext,
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
            [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
            IAsyncCompletionBroker completionBroker,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(threadingContext)
        {
            _globalOptions = globalOptions;
            _signatureHelpPresenter = ExtensionOrderer.Order(signatureHelpPresenters).Select(lazy => lazy.Value).FirstOrDefault();
            _listener = listenerProvider.GetListener(FeatureAttribute.SignatureHelp);
            _completionBroker = completionBroker;
            _signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders);
        }

        public Controller? GetController(ITextView textView, ITextBuffer subjectBuffer)
        {
            AssertIsForeground();

            // If we don't have a presenter, then there's no point in us even being involved.
            if (_signatureHelpPresenter == null)
            {
                return null;
            }

            if (textView.TryGetPerSubjectBufferProperty<Controller, ITextView>(subjectBuffer, s_controllerPropertyKey, out var controller))
                return controller;

            return GetControllerSlow(textView, subjectBuffer);
        }

        private Controller GetControllerSlow(ITextView textView, ITextBuffer subjectBuffer)
        {
            AssertIsForeground();

            return textView.GetOrCreatePerSubjectBufferProperty(
                subjectBuffer,
                s_controllerPropertyKey,
                (textView, subjectBuffer) => new Controller(
                    _globalOptions,
                    ThreadingContext,
                    textView,
                    subjectBuffer,
                    _signatureHelpPresenter,
                    _listener,
                    new DocumentProvider(ThreadingContext),
                    _signatureHelpProviders,
                    _completionBroker));
        }
    }
}
