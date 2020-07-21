// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The provider that is used as a middleman to create the tagger so that the data tag
    /// can be used to create the UI tag
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Name(nameof(InlineParameterNameHintsTaggerProvider))]
    internal class InlineParameterNameHintsTaggerProvider : IViewTaggerProvider
    {
        private readonly IViewTagAggregatorFactoryService _viewTagAggregatorFactoryService;
        public readonly IClassificationFormatMapService ClassificationFormatMapService;
        public readonly IClassificationTypeRegistryService ClassificationTypeRegistryService;
        public readonly IThreadingContext ThreadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParameterNameHintsTaggerProvider(IViewTagAggregatorFactoryService viewTagAggregatorFactoryService,
                                                       IClassificationFormatMapService classificationFormatMapService,
                                                       IClassificationTypeRegistryService classificationTypeRegistryService,
                                                       IThreadingContext threadingContext)
        {
            _viewTagAggregatorFactoryService = viewTagAggregatorFactoryService;
            this.ClassificationFormatMapService = classificationFormatMapService;
            this.ClassificationTypeRegistryService = classificationTypeRegistryService;
            this.ThreadingContext = threadingContext;
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Determining of the textView's buffer does not match the buffer in order to skip showing the hints for
            // the interactive window
            if (buffer != textView.TextBuffer)
            {
                return null;
            }

            var tagAggregator = _viewTagAggregatorFactoryService.CreateTagAggregator<InlineParameterNameHintDataTag>(textView);
            return new InlineParameterNameHintsTagger(this, textView, buffer, tagAggregator) as ITagger<T>;
        }
    }
}
