// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The provider that is used as a middleman to create the tagger so that the data tag
    /// can be used to create the UI tag
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Name(nameof(InlineParamNameHintsTaggerProvider))]
    class InlineParamNameHintsTaggerProvider : ITaggerProvider
    {
        private readonly IBufferTagAggregatorFactoryService _bufferTagAggregatorFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InlineParamNameHintsTaggerProvider(IBufferTagAggregatorFactoryService bufferTagAggregatorFactoryService)
        {
            _bufferTagAggregatorFactoryService = bufferTagAggregatorFactoryService;
        }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            var tagAggregator = _bufferTagAggregatorFactoryService.CreateTagAggregator<InlineParamNameHintDataTag>(buffer);
            return new InlineParamNameHintsTagger(buffer, tagAggregator) as ITagger<T>;
        }
    }
}
