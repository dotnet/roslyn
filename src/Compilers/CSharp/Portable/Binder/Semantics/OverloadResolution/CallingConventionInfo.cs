#nullable enable

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal readonly struct CallingConventionInfo
    {
        internal Cci.CallingConvention CallKind { get; }
        internal ImmutableHashSet<CustomModifier>? CallingConventionTypes { get; }

        public CallingConventionInfo(Cci.CallingConvention callKind, ImmutableHashSet<CustomModifier> callingConventionTypes)
        {
            Debug.Assert(callingConventionTypes.IsEmpty || callKind == Cci.CallingConvention.Unmanaged);
            CallKind = callKind;
            CallingConventionTypes = callingConventionTypes;
        }
    }
}
