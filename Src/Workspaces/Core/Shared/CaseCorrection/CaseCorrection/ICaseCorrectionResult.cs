#if false
using System.Collections.Generic;
using Roslyn.Compilers;

namespace Roslyn.Services.CaseCorrection
{
    public interface ICaseCorrectionResult
    {
        bool ContainsChanges { get; }
        IList<TextChange> TextChanges { get; }
        IDocument UpdatedDocument { get; }
    }
}
#endif