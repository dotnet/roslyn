// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;
using System.Collections.Generic;
using System;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Completion.Presentation
{
    [Export(typeof(IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>))]
    [Export(typeof(ICompletionSourceProvider))]
    [ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)]
    internal partial class CompletionPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>, ICompletionSourceProvider
    {
        private readonly ICompletionBroker _completionBroker;
        private readonly IGlyphService _glyphService;
        private readonly ICompletionSetFactory _completionSetFactory;

        [ImportingConstructor]
        public CompletionPresenter(
            ICompletionBroker completionBroker,
            IGlyphService glyphService,
            [ImportMany] IEnumerable<Lazy<ICompletionSetFactory, VisualStudioVersionMetadata>> completionSetFactories)
        {
            _completionBroker = completionBroker;
            _glyphService = glyphService;
            _completionSetFactory = VersionSelector.SelectHighest(completionSetFactories);
        }

        ICompletionPresenterSession IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>.CreateSession(ITextView textView, ITextBuffer subjectBuffer, ICompletionSession sessionOpt)
        {
            AssertIsForeground();
            return new CompletionPresenterSession(
                _completionSetFactory, _completionBroker, _glyphService, textView, subjectBuffer);
        }

        ICompletionSource ICompletionSourceProvider.TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            AssertIsForeground();
            return new CompletionSource();
        }
    }
}
