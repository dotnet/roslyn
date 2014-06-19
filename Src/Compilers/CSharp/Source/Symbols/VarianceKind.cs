using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    public enum VarianceKind
    {
        VarianceNone,  // invariant
        VarianceOut,   // "Out" - covariant
        VarianceIn,    // "In" - contravariant
    }
}