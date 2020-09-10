// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a field initializer, a property initializer, or a global statement in script code.
    /// </summary>
    internal struct FieldOrPropertyInitializer
    {
        /// <summary>
        /// The field being initialized (possibly a backing field of a property), or null if this is a top-level statement in script code.
        /// </summary>
        internal readonly FieldSymbol FieldOpt;

        /// <summary>
        /// A reference to <see cref="EqualsValueClauseSyntax"/>,
        /// or top-level <see cref="StatementSyntax"/> in script code,
        /// or <see cref="ParameterSyntax"/> for an initialization of a generated property based on record parameter.
        /// </summary>
        internal readonly SyntaxReference Syntax;

        /// <summary>
        /// A sum of widths of spans of all preceding initializers 
        /// (instance and static initializers are summed separately, and trivias are not counted).
        /// </summary>
        internal readonly int PrecedingInitializersLength;

        public FieldOrPropertyInitializer(FieldSymbol fieldOpt, SyntaxNode syntax, int precedingInitializersLength)
        {
            Debug.Assert(((syntax.IsKind(SyntaxKind.EqualsValueClause) || syntax.IsKind(SyntaxKind.Parameter)) && fieldOpt != null) || syntax is StatementSyntax);

            FieldOpt = fieldOpt;
            Syntax = syntax.GetReference();
            PrecedingInitializersLength = precedingInitializersLength;
        }

        internal struct Builder
        {
            /// <summary>
            /// The field being initialized (possibly a backing field of a property), or null if this is a top-level statement in script code.
            /// </summary>
            internal readonly FieldSymbol FieldOpt;

            /// <summary>
            /// A reference to <see cref="EqualsValueClauseSyntax"/>,
            /// or top-level <see cref="StatementSyntax"/> in script code,
            /// or <see cref="ParameterSyntax"/> for an initialization of a generated property based on record parameter.
            /// </summary>
            internal readonly SyntaxNode Syntax;

            public Builder(FieldSymbol fieldOpt, SyntaxNode syntax)
            {
                Debug.Assert(((syntax.IsKind(SyntaxKind.EqualsValueClause) || syntax.IsKind(SyntaxKind.Parameter)) && fieldOpt != null) || syntax is StatementSyntax);

                FieldOpt = fieldOpt;
                Syntax = syntax;
            }
        }
    }
}
