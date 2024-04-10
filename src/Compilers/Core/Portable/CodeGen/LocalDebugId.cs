// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Id that associates an emitted user-defined or long-lived synthesized local variable 
    /// with a syntax node that defined it. If a syntax node defines multiple variables it 
    /// provides information necessary to identify which one of these variables is it.
    /// </summary>
    internal readonly struct LocalDebugId : IEquatable<LocalDebugId>
    {
        /// <summary>
        /// We calculate a "syntax offset" for each user-defined and long-lived synthesized variable. 
        /// Every such variable symbol has to be associated with a syntax node (its declarator). 
        /// In usual cases this is the textual distance of the declarator from the start of the method body. 
        /// It gets a bit complicated when the containing method body is not contiguous (constructors). 
        /// If the variable is in the body of the constructor the definition of syntax offset is the same. 
        /// If the variable is defined in a constructor  initializer or in a member initializer 
        /// (this is only possible when declaration expressions or closures in primary constructors are involved) 
        /// then the distance is a negative sum of the widths of all the initializers that succeed the declarator 
        /// of the variable in the emitted constructor body plus the relative offset of the declarator from 
        /// the start of the containing initializer.
        /// </summary>
        public readonly int SyntaxOffset;

        /// <summary>
        /// If a single node is a declarator for multiple variables of the same synthesized kind (it can only happen for synthesized variables) 
        /// we calculate additional number "ordinal" for such variable. We assign the ordinals to the synthesized variables with the same kind
        /// and syntax offset in the order as they appear in the lowered bound tree. It is important that a valid EnC edit can't change 
        /// the ordinal of a synthesized variable. If it could it would need to be assigned a different kind or associated with a different declarator node.
        /// </summary>
        public readonly int Ordinal;

        public static readonly LocalDebugId None = new LocalDebugId(isNone: true);

        private LocalDebugId(bool isNone)
        {
            Debug.Assert(isNone);

            this.SyntaxOffset = -1;
            this.Ordinal = -1;
        }

        public LocalDebugId(int syntaxOffset, int ordinal = 0)
        {
            Debug.Assert(ordinal >= 0);

            this.SyntaxOffset = syntaxOffset;
            this.Ordinal = ordinal;
        }

        public bool IsNone
        {
            get
            {
                return Ordinal == -1;
            }
        }

        public bool Equals(LocalDebugId other)
        {
            return SyntaxOffset == other.SyntaxOffset
                && Ordinal == other.Ordinal;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(SyntaxOffset, Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is LocalDebugId && Equals((LocalDebugId)obj);
        }

        public override string ToString()
        {
            return SyntaxOffset + ":" + Ordinal;
        }
    }
}
