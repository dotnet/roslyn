using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.InlineParamNameHints
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("csharp")]
    [TagType(typeof(IntraTextAdornmentTag))]
    [Name(nameof(InlineParamNameHintsTaggerProvider))]
    class InlineParamNameHintsTaggerProvider : ITaggerProvider
    {
        private IBufferTagAggregatorFactoryService _bufferTagAggregatorFactoryService;
        [ImportingConstructor]
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
