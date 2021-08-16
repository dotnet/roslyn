// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin
{
    [Export(typeof(IWpfTextViewMarginProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(InheritanceGlyphFactoryProvider))]
    [MarginContainer(PredefinedMarginNames.Left)]
    [Order(After = PredefinedMarginNames.Glyph)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal class InheritanceMarginViewMarginProvider : IWpfTextViewMarginProvider
    {
        private readonly IViewTagAggregatorFactoryService _tagAggregatorFactoryService;
        private readonly IThreadingContext _threadingContext;
        private readonly IStreamingFindUsagesPresenter _streamingFindUsagesPresenter;
        private readonly IClassificationFormatMapService _classificationFormatMapService;
        private readonly ClassificationTypeMap _classificationTypeMap;
        private readonly IUIThreadOperationExecutor _operationExecutor;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InheritanceMarginViewMarginProvider(
            IThreadingContext threadingContext,
            IStreamingFindUsagesPresenter streamingFindUsagesPresenter,
            ClassificationTypeMap classificationTypeMap,
            IClassificationFormatMapService classificationFormatMapService,
            IUIThreadOperationExecutor operationExecutor,
            IViewTagAggregatorFactoryService tagAggregatorFactoryService)
        {
            _threadingContext = threadingContext;
            _streamingFindUsagesPresenter = streamingFindUsagesPresenter;
            _classificationTypeMap = classificationTypeMap;
            _classificationFormatMapService = classificationFormatMapService;
            _operationExecutor = operationExecutor;
            _tagAggregatorFactoryService = tagAggregatorFactoryService;
        }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            var tagAggregator = _tagAggregatorFactoryService.CreateTagAggregator<InheritanceMarginTag>(wpfTextViewHost.TextView);

            return new InheritanceMarginViewMargin(
                wpfTextViewHost,
                _threadingContext,
                _streamingFindUsagesPresenter,
                _operationExecutor,
                _classificationFormatMapService.GetClassificationFormatMap("tooltip"),
                _classificationTypeMap,
                tagAggregator);
        }
    }
}
