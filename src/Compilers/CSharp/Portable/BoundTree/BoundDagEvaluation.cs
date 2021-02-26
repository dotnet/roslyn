// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagEvaluation
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public virtual bool Equals(BoundDagEvaluation other)
        {
            return this == other ||
                this.Kind == other.Kind &&
                this.Input.Equals(other.Input) &&
                Symbol.Equals(this.Symbol, other.Symbol, TypeCompareKind.AllIgnoreOptions);
        }
        private Symbol? Symbol
        {
            get
            {
                return this switch
                {
                    BoundDagFieldEvaluation e => e.Field.CorrespondingTupleField ?? e.Field,
                    BoundDagPropertyEvaluation e => e.Property,
                    BoundDagTypeEvaluation e => e.Type,
                    BoundDagDeconstructEvaluation e => e.DeconstructMethod,
                    BoundDagMethodEvaluation e => e.Method,
                    BoundDagEnumeratorEvaluation e => e.EnumeratorInfo.GetEnumeratorInfo.Method,
                    BoundDagIndexEvaluation e => e.Property,
                    BoundDagSliceEvaluation e => e.SliceMethod,
                    BoundDagArrayIndexEvaluation or
                    BoundDagArrayLengthEvaluation or
                    BoundDagIncrementEvaluation => null,
                    _ => throw ExceptionUtilities.UnexpectedValue(this.Kind)
                };
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Input.GetHashCode(), this.Symbol?.GetHashCode() ?? 0);
        }
    }

    partial class BoundDagIndexEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Index == ((BoundDagIndexEvaluation)obj).Index;
        }
    }

    partial class BoundDagSliceEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.StartIndex ^ this.EndIndex;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                (BoundDagSliceEvaluation)obj is var e &&
                this.StartIndex == e.StartIndex && this.EndIndex == e.EndIndex;
        }
    }

    partial class BoundDagArrayIndexEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ Hash.CombineValues(this.Indices);
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Indices.SequenceEqual(((BoundDagArrayIndexEvaluation)obj).Indices);
        }
    }

    partial class BoundDagArrayLengthEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Dimension;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Dimension == ((BoundDagArrayLengthEvaluation)obj).Dimension;
        }
    }

    partial class BoundDagMethodEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Index == ((BoundDagMethodEvaluation)obj).Index;
        }
    }

    partial class BoundDagIncrementEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
               base.Equals(obj) &&
               // base.Equals checks the kind field, so the following cast is safe
               this.Index == ((BoundDagIncrementEvaluation)obj).Index;
        }
    }

    partial class BoundDagPropertyEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool Equals(BoundDagEvaluation obj)
        {
            return this == obj ||
                base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Index == ((BoundDagPropertyEvaluation)obj).Index;
        }
    }
}
