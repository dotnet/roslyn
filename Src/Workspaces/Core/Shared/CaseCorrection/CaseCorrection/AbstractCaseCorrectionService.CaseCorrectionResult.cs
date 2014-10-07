#if false
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Utilities;

namespace Roslyn.Services.CaseCorrection
{
    internal abstract partial class AbstractCaseCorrectionService
    {
        private class CaseCorrectionResult : ICaseCorrectionResult
        {
            public IList<TextChange> TextChanges { get; private set; }
            public IDocument UpdatedDocument { get; private set; }

            public CaseCorrectionResult(IEnumerable<TextChange> textChanges, IDocument updatedDocument)
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