// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    [Export(typeof(ISignatureHelpSourceProvider))]
    [Export(typeof(IIntelliSensePresenter<ISignatureHelpPresenterSession>))]
    [Name(PredefinedSignatureHelpPresenterNames.RoslynSignatureHelpPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class SignatureHelpPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<ISignatureHelpPresenterSession>, ISignatureHelpSourceProvider
    {
        private static readonly object s_augmentSessionKey = new object();

        private readonly ISignatureHelpBroker _sigHelpBroker;

        [ImportingConstructor]
        public SignatureHelpPresenter(ISignatureHelpBroker sigHelpBroker)
        {
            _sigHelpBroker = sigHelpBroker;
        }

        ISignatureHelpPresenterSession IIntelliSensePresenter<ISignatureHelpPresenterSession>.CreateSession(ITextView textView, ITextBuffer subjectBuffer)
        {
            AssertIsForeground();
            return new SignatureHelpPresenterSession(_sigHelpBroker, textView, subjectBuffer);
        }

        ISignatureHelpSource ISignatureHelpSourceProvider.TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            AssertIsForeground();
            return new SignatureHelpSource();
        }
    }
}
