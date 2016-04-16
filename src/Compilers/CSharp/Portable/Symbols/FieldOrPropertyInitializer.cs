// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// A reference to <see cref="EqualsValueClauseSyntax"/> or top-level <see cref="StatementSyntax"/> in script code.
        /// </summary>
        internal readonly SyntaxReference Syntax;

        /// <summary>
        /// A sum of widths of spans of all preceding initializers 
        /// (instance and static initializers are summed separately, and trivias are not counted).
        /// </summary>
        internal readonly int PrecedingInitializersLength;

        public FieldOrPropertyInitializer(FieldSymbol fieldOpt, SyntaxNode syntax, int precedingInitializersLength)
        {
            Debug.Assert(syntax.IsKind(SyntaxKind.EqualsValueClause) && fieldOpt != null || syntax is StatementSyntax);

            FieldOpt = fieldOpt;
            Syntax = syntax.GetReference();
            PrecedingInitializersLength = precedingInitializersLength;
        }
    }
}
