// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagEvaluation
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public virtual bool Equals([NotNullWhen(true)] BoundDagEvaluation? other)
        {
            return !(other is null) &&
                this.Kind == other.Kind &&
                this.GetOriginalInput().Equals(other.GetOriginalInput()) &&
                this.Symbol.Equals(other.Symbol, TypeCompareKind.AllIgnoreOptions);
        }
        private Symbol Symbol
        {
            get
            {
                switch (this)
                {
                    case BoundDagFieldEvaluation e: return e.Field.CorrespondingTupleField ?? e.Field;
                    case BoundDagPropertyEvaluation e: return e.Property;
                    case BoundDagTypeEvaluation e: return e.Type;
                    case BoundDagDeconstructEvaluation e: return e.DeconstructMethod;
                    case BoundDagIndexEvaluation e: return e.Property;
                    default: throw ExceptionUtilities.UnexpectedValue(this.Kind);
                }
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(GetOriginalInput().GetHashCode(), Symbol.GetHashCode());
        }

        /// <summary>
        /// Returns the original input for this evaluation, stripped of all Type Evaluations.
        /// 
        /// A BoundDagTypeEvaluation doesn't change the underlying object being pointed to
        /// So two evaluations act on the same input so long as they have the same original input.
        /// </summary>
        private BoundDagTemp GetOriginalInput()
        {
            var input = this.Input;
            while (input.Source is BoundDagTypeEvaluation source)
            {
                input = source.Input;
            }
            return input;
        }

        public static bool operator ==(BoundDagEvaluation? left, BoundDagEvaluation? right)
        {
            return (left is null) ? right is null : left.Equals(right);
        }
        public static bool operator !=(BoundDagEvaluation? left, BoundDagEvaluation? right)
        {
            return !(left == right);
        }
    }

    partial class BoundDagIndexEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool Equals(BoundDagEvaluation? obj)
        {
            return base.Equals(obj) &&
                // base.Equals checks the kind field, so the following cast is safe
                this.Index == ((BoundDagIndexEvaluation)obj).Index;
        }
    }
}
