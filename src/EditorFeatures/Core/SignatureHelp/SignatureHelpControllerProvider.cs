// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;

[Export, Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SignatureHelpControllerProvider(
    IGlobalOptionService globalOptions,
    IThreadingContext threadingContext,
    [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> signatureHelpProviders,
    [ImportMany] IEnumerable<Lazy<IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, OrderableMetadata>> signatureHelpPresenters,
    IAsyncCompletionBroker completionBroker,
    IAsynchronousOperationListenerProvider listenerProvider)
{
    private static readonly object s_controllerPropertyKey = new();

    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession> _signatureHelpPresenter = ExtensionOrderer.Order(signatureHelpPresenters).Select(lazy => lazy.Value).FirstOrDefault();
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.SignatureHelp);
    private readonly IAsyncCompletionBroker _completionBroker = completionBroker;
    private readonly IList<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _signatureHelpProviders = ExtensionOrderer.Order(signatureHelpProviders);

    public Controller? GetController(ITextView textView, ITextBuffer subjectBuffer)
    {
        _threadingContext.ThrowIfNotOnUIThread();

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
        _threadingContext.ThrowIfNotOnUIThread();

        return textView.GetOrCreatePerSubjectBufferProperty(
            subjectBuffer,
            s_controllerPropertyKey,
            (textView, subjectBuffer) => new Controller(
                _globalOptions,
                _threadingContext,
                textView,
                subjectBuffer,
                _signatureHelpPresenter,
                _listener,
                new DocumentProvider(_threadingContext),
                _signatureHelpProviders,
                _completionBroker));
    }
}
