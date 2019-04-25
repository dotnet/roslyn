// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagTemp
    {
        /// <summary>
        /// Does this dag temp represent the original input of the pattern-matching operation?
        /// </summary>
        public bool IsOriginalInput => this.Source is null;

        public static BoundDagTemp ForOriginalInput(SyntaxNode syntax, TypeSymbol type) => new BoundDagTemp(syntax, type, null, 0);

        public override bool Equals(object obj) => obj is BoundDagTemp other && this.Equals(other);
        public bool Equals(BoundDagTemp other)
        {
            return other != (object)null && this.Type.Equals(other.Type, TypeCompareKind.AllIgnoreOptions) && object.Equals(this.Source, other.Source) && this.Index == other.Index;
        }
        public override int GetHashCode()
        {
            return Hash.Combine(this.Type.GetHashCode(), Hash.Combine(this.Source?.GetHashCode() ?? 0, this.Index));
        }
        public static bool operator ==(BoundDagTemp left, BoundDagTemp right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(BoundDagTemp left, BoundDagTemp right)
        {
            return !left.Equals(right);
        }
    }
}
