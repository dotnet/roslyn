// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum BestIndexKind
    {
        None,
        Best,
        Ambiguous
    }

    internal readonly struct BestIndex
    {
        internal readonly BestIndexKind Kind;
        internal readonly int Best;
        internal readonly int Ambiguous1;
        internal readonly int Ambiguous2;

        public static BestIndex None() { return new BestIndex(BestIndexKind.None, 0, 0, 0); }
        public static BestIndex HasBest(int best) { return new BestIndex(BestIndexKind.Best, best, 0, 0); }
        public static BestIndex IsAmbiguous(int ambig1, int ambig2) { return new BestIndex(BestIndexKind.Ambiguous, 0, ambig1, ambig2); }

        private BestIndex(BestIndexKind kind, int best, int ambig1, int ambig2)
        {
            this.Kind = kind;
            this.Best = best;
            this.Ambiguous1 = ambig1;
            this.Ambiguous2 = ambig2;
        }
    }
}
