#if false
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services.NameSimplification
{
    internal abstract partial class AbstractNameSimplificationService
    {
        protected internal class NameSimplificationResult : INameSimplificationResult
        {
            public IList<TextChange> TextChanges { get; private set; }
            public IDocument UpdatedDocument { get; private set; }

            public NameSimplificationResult(IEnumerable<TextChange> textChanges, IDocument updatedDocument)
            {
                this.TextChanges = textChanges.ToImmutableList();
                this.UpdatedDocument = updatedDocument;
            }

            public bool ContainsChanges
            {
                get { return this.TextChanges.Count > 0; }
            }
        }
    }
}
#endif