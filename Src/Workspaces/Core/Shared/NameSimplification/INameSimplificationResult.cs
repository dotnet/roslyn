#if false
using System.Collections.Generic;
using Roslyn.Utilities;
using Roslyn.Compilers;

namespace Roslyn.Services.NameSimplification
{
    public interface INameSimplificationResult
    {
        bool ContainsChanges { get; }
        IList<TextChange> TextChanges { get; }
        IDocument UpdatedDocument { get; }
    }
}
#endif