// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Extensibility.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    [Export(typeof(IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>))]
    [Export(typeof(ICompletionSourceProvider))]
    [Name(PredefinedCompletionPresenterNames.RoslynCompletionPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal sealed class CompletionPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>, ICompletionSourceProvider
    {
        private readonly ICompletionBroker _completionBroker;
        private readonly IGlyphService _glyphService;
        private readonly ImmutableArray<Lazy<ICompletionSetFactory, VisualStudioVersionMetadata>> _completionSetFactories;

        [ImportingConstructor]
        public CompletionPresenter(
            ICompletionBroker completionBroker,
            IGlyphService glyphService,
            [ImportMany] IEnumerable<Lazy<ICompletionSetFactory, VisualStudioVersionMetadata>> completionSetFactories)
        {
            _completionBroker = completionBroker;
            _glyphService = glyphService;
            _completionSetFactories = completionSetFactories.AsImmutableOrEmpty();
        }

        ICompletionPresenterSession IIntelliSensePresenter<ICompletionPresenterSession, ICompletionSession>.CreateSession(
            ITextView textView, ITextBuffer subjectBuffer, ICompletionSession session)
        {
            AssertIsForeground();

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();

            var completionSetFactory = document != null && NeedsDev15CompletionSetFactory(document.Project.Solution.Options, document.Project.Language)
                ? VersionSelector.SelectHighest(_completionSetFactories)
                : VersionSelector.SelectVersion(_completionSetFactories, VisualStudioVersion.Dev14);

            return new CompletionPresenterSession(
                completionSetFactory, _completionBroker, _glyphService, textView, subjectBuffer);
        }

        private bool NeedsDev15CompletionSetFactory(OptionSet options, string language)
        {
            return CompletionOptions.GetDev15CompletionOptions().Any(
                o => options.GetOption(o, language));
        }

        ICompletionSource ICompletionSourceProvider.TryCreateCompletionSource(ITextBuffer textBuffer)
        {
            AssertIsForeground();
            return new CompletionSource();
        }
    }
}
