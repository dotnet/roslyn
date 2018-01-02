// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo.Presentation
{
    [Export(typeof(IQuickInfoSourceProvider))]
    [Export(typeof(IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession>))]
    [Order]
    [Name(PredefinedQuickInfoPresenterNames.RoslynQuickInfoPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class QuickInfoPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession>, IQuickInfoSourceProvider
    {
        private static readonly object s_augmentSessionKey = new object();

        private readonly IQuickInfoBroker _quickInfoBroker;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        [ImportingConstructor]
        public QuickInfoPresenter(
            IQuickInfoBroker quickInfoBroker,
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _quickInfoBroker = quickInfoBroker;
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        IQuickInfoPresenterSession IIntelliSensePresenter<IQuickInfoPresenterSession, IQuickInfoSession>.CreateSession(ITextView textView, ITextBuffer subjectBuffer, IQuickInfoSession sessionOpt)
        {
            AssertIsForeground();

            return new QuickInfoPresenterSession(
                _quickInfoBroker,
                textView, subjectBuffer, sessionOpt,
                _classificationTypeMap,
                _classificationFormatMapService,
                _projectionBufferFactoryService,
                _editorOptionsFactoryService,
                _textEditorFactoryService);
        }

        IQuickInfoSource IQuickInfoSourceProvider.TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            AssertIsForeground();
            return new QuickInfoSource();
        }
    }
}
