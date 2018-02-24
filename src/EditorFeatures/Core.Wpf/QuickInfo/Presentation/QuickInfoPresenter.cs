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
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Export(typeof(IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession>))]
    [Order]
    [Name(PredefinedQuickInfoPresenterNames.RoslynQuickInfoPresenter)]
    [ContentType(ContentTypeNames.RoslynContentType)]
    internal partial class QuickInfoPresenter : ForegroundThreadAffinitizedObject, IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession>, IAsyncQuickInfoSourceProvider
    {
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly IProjectionBufferFactoryService _projectionBufferFactoryService;
        private readonly IEditorOptionsFactoryService _editorOptionsFactoryService;
        private readonly ITextEditorFactoryService _textEditorFactoryService;

        [ImportingConstructor]
        public QuickInfoPresenter(
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            ITextEditorFactoryService textEditorFactoryService)
        {
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _projectionBufferFactoryService = projectionBufferFactoryService;
            _editorOptionsFactoryService = editorOptionsFactoryService;
            _textEditorFactoryService = textEditorFactoryService;
        }

        IQuickInfoPresenterSession IIntelliSensePresenter<IQuickInfoPresenterSession, IAsyncQuickInfoSession>.CreateSession(
            ITextView textView, ITextBuffer subjectBuffer, IAsyncQuickInfoSession _)
        {
            return new QuickInfoPresenterSession(
                textView, subjectBuffer,
                _classificationTypeMap,
                _classificationFormatMapService,
                _projectionBufferFactoryService,
                _editorOptionsFactoryService,
                _textEditorFactoryService);
        }

        IAsyncQuickInfoSource IAsyncQuickInfoSourceProvider.TryCreateQuickInfoSource(ITextBuffer textBuffer)
        {
            return null;
        }
    }
}
