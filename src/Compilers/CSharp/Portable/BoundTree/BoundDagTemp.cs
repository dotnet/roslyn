// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if DEBUG
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
#endif
    partial class BoundDagTemp
    {
        /// <summary>
        /// Does this dag temp represent the original input of the pattern-matching operation?
        /// </summary>
        public bool IsOriginalInput => this.Source is null;

        public static BoundDagTemp ForOriginalInput(SyntaxNode syntax, TypeSymbol type) => new BoundDagTemp(syntax, type, source: null, 0);

        public override bool Equals(object? obj) => obj is BoundDagTemp other && this.Equals(other);

        public bool Equals(BoundDagTemp other)
        {
            return
                this.Type.Equals(other.Type, TypeCompareKind.AllIgnoreOptions) &&
                object.Equals(this.Source, other.Source) &&
                this.Index == other.Index;
        }

        /// <summary>
        /// Check if this is equivalent to the <paramref name="other"/> node, ignoring the source.
        /// </summary>
        public bool IsEquivalentTo(BoundDagTemp other)
        {
            return
                this.Type.Equals(other.Type, TypeCompareKind.AllIgnoreOptions) &&
                this.Index == other.Index;
        }

        /// <summary>
        /// Determine if two <see cref="BoundDagTemp"/>s represent the same value for the purpose of a pattern evaluation.
        /// </summary>
        public bool IsSameValue(BoundDagTemp other)
        {
            var current = originalInput(this);
            other = originalInput(other);

            if ((object)current == other)
            {
                return true;
            }

            return current.Index == other.Index &&
                (current.Source, other.Source) switch
                {
                    (null, null) => true,
                    ({ } s1, { } s2) => s1.IsSameValueEvaluation(s2),
                    _ => false
                };

            static BoundDagTemp originalInput(BoundDagTemp input)
            {
                // Type evaluations do not change identity
                while (input.Source is BoundDagTypeEvaluation source)
                {
                    Debug.Assert(input.Index == 0);
                    input = source.Input;
                }

                return input;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Type.GetHashCode(), Hash.Combine(this.Source?.GetHashCode() ?? 0, this.Index));
        }

#if DEBUG
        internal new string GetDebuggerDisplay()
        {
            var name = Source?.Id switch
            {
                -1 => "<uninitialized>",

                // Note that we never expect to have a non-null source with id 0
                // because id 0 is reserved for the original input.
                // However, we also don't want to assert in a debugger display method.
                0 => "<error>",

                null => "t0",
                var id => $"t{id}"
            };
            return $"{name}{(Source is BoundDagDeconstructEvaluation ? $".Item{(Index + 1).ToString()}" : "")}";
        }
#endif
    }
}
