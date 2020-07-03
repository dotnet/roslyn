// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

        public override bool Equals(object? obj) => obj is BoundDagTemp other && this.Equals(other);

        public bool Equals(BoundDagTemp other)
        {
            return other is { } &&
                this.Type.Equals(other.Type, TypeCompareKind.AllIgnoreOptions) &&
                object.Equals(this.Source, other.Source) && this.Index == other.Index;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Type.GetHashCode(), Hash.Combine(this.Source?.GetHashCode() ?? 0, this.Index));
        }
    }
}
