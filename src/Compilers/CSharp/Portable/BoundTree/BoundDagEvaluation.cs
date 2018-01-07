// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagEvaluation
    {
        public override bool Equals(object obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public bool Equals(BoundDagEvaluation other)
        {
            return other != (object)null && this.Kind == other.Kind && this.Input.Equals(other.Input) && this.Symbol == other.Symbol;
        }
        private Symbol Symbol
        {
            get
            {
                switch (this)
                {
                    case BoundDagFieldEvaluation e: return e.Field;
                    case BoundDagPropertyEvaluation e: return e.Property;
                    case BoundDagTypeEvaluation e: return e.Type;
                    case BoundDagDeconstructEvaluation e: return e.DeconstructMethod;
                    default: throw ExceptionUtilities.UnexpectedValue(this.Kind);
                }
            }
        }
        public override int GetHashCode()
        {
            return this.Input.GetHashCode() ^ (this.Symbol?.GetHashCode() ?? 0);
        }
        public static bool operator ==(BoundDagEvaluation left, BoundDagEvaluation right)
        {
            return (left == (object)null) ? right == (object)null : left.Equals(right);
        }
        public static bool operator !=(BoundDagEvaluation left, BoundDagEvaluation right)
        {
            return !(left == right);
        }
    }
}
