// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
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
        private readonly IBufferTagAggregatorFactoryService _bufferTagAggregatorFactoryService;
        public readonly IClassificationFormatMapService ClassificationFormatMapService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParameterNameHintsTaggerProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService, [Import] IClassificationFormatMapService classificationFormatMapService)
        {
            _bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
            this.ClassificationFormatMapService = classificationFormatMapService;
        }

        /*
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _bufferTagAggregatorFactoryService.CreateTagAggregator<InlineParameterNameHintDataTag>(buffer);
            return new InlineParameterNameHintsTagger(buffer, tagAggregator) as ITagger<T>;
        }
        */

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _bufferTagAggregatorFactoryService.CreateTagAggregator<InlineParameterNameHintDataTag>(buffer);
            return new InlineParameterNameHintsTagger(this, textView, buffer, tagAggregator) as ITagger<T>;
        }
    }
}
