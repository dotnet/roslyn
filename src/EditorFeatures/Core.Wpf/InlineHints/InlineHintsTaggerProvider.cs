// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineHints
{
    /// <summary>
    /// The provider that is used as a middleman to create the tagger so that the data tag
    /// can be used to create the UI tag
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Name(nameof(InlineHintsTaggerProvider))]
    internal class InlineHintsTaggerProvider : IViewTaggerProvider
    {
        private readonly IViewTagAggregatorFactoryService _viewTagAggregatorFactoryService;
        public readonly IClassificationFormatMapService ClassificationFormatMapService;
        public readonly IClassificationTypeRegistryService ClassificationTypeRegistryService;
        public readonly IThreadingContext ThreadingContext;
        public readonly IUIThreadOperationExecutor OperationExecutor;
        public readonly IAsynchronousOperationListener AsynchronousOperationListener;
        public readonly IToolTipService ToolTipService;
        public readonly ClassificationTypeMap TypeMap;
        public readonly Lazy<IStreamingFindUsagesPresenter> StreamingFindUsagesPresenter;
        public readonly EditorOptionsService EditorOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineHintsTaggerProvider(
            IViewTagAggregatorFactoryService viewTagAggregatorFactoryService,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListenerProvider listenerProvider,
            IToolTipService toolTipService,
            ClassificationTypeMap typeMap,
            Lazy<IStreamingFindUsagesPresenter> streamingFindUsagesPresenter,
            EditorOptionsService editorOptionsService)
        {
            _viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
            ClassificationFormatMapService = classificationFormatMapService;
            ClassificationTypeRegistryService = classificationTypeRegistryService;
            ThreadingContext = threadingContext;
            OperationExecutor = operationExecutor;
            ToolTipService = toolTipService;
            StreamingFindUsagesPresenter = streamingFindUsagesPresenter;
            TypeMap = typeMap;
            EditorOptionsService = editorOptionsService;

            AsynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.InlineHints);
        }

        public ITagger<T>? CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.IsNotSurfaceBufferOfTextView(buffer))
            {
                return null;
            }

            var tagAggregator = _viewTagAggregatorFactoryService.CreateTagAggregator<InlineHintDataTag>(textView);
            return new InlineHintsTagger(this, (IWpfTextView)textView, buffer, tagAggregator) as ITagger<T>;
        }
    }
}
