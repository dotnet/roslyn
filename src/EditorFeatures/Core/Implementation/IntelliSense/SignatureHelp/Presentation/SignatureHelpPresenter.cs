﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation
{
    [Export(typeof(ISignatureHelpSourceProvider))]
    [Export(typeof(IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>))]
    [Name(PredefinedSignatureHelpPresenterNames.RoslynSignatureHelpPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class SignatureHelpPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>, ISignatureHelpSourceProvider
    {
        private static readonly object s_augmentSessionKey = new();

        private readonly ISignatureHelpBroker _sigHelpBroker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpPresenter(IThreadingContext threadingContext, ISignatureHelpBroker sigHelpBroker)
            : base(threadingContext)
        {
            _sigHelpBroker = sigHelpBroker;
        }

        ISignatureHelpPresenterSession IIntelliSensePresenter<ISignatureHelpPresenterSession, ISignatureHelpSession>.CreateSession(ITextView textView, ITextBuffer subjectBuffer, ISignatureHelpSession sessionOpt)
        {
            AssertIsForeground();
            return new SignatureHelpPresenterSession(ThreadingContext, _sigHelpBroker, textView);
        }

        ISignatureHelpSource ISignatureHelpSourceProvider.TryCreateSignatureHelpSource(ITextBuffer textBuffer)
        {
            AssertIsForeground();
            return new SignatureHelpSource(ThreadingContext);
        }
    }
}
