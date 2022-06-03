// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Specifies the syntactic construct that a user defined variable comes from.
    /// </summary>
    internal enum LocalDeclarationKind : byte
    {
        /// <summary>
        /// The local is not user defined nor it is a copy of a user defined local (e.g. with a substituted type).
        /// Check the value of <see cref="LocalSymbol.SynthesizedKind"/> for the kind of synthesized variable.
        /// </summary>
        None,

        /// <summary>
        /// User defined local variable declared by <see cref="LocalDeclarationStatementSyntax"/>.
        /// </summary>
        RegularVariable,

        /// <summary>
        /// User defined local constant declared by <see cref="LocalDeclarationStatementSyntax"/>.
        /// </summary>
        Constant,

        /// <summary>
        /// User defined local variable declared by <see cref="VariableDeclarationSyntax"/> in <see cref="FixedStatementSyntax"/>.
        /// </summary>
        FixedVariable,

        /// <summary>
        /// User defined local variable declared by <see cref="VariableDeclarationSyntax"/> in <see cref="UsingStatementSyntax"/>.
        /// </summary>
        UsingVariable,

        /// <summary>
        /// User defined local variable declared by <see cref="CatchClauseSyntax"/>.
        /// </summary>
        CatchVariable,

        /// <summary>
        /// User defined local variable declared by <see cref="ForEachStatementSyntax"/> or <see cref="ForEachVariableStatementSyntax"/>.
        /// </summary>
        ForEachIterationVariable,

        /// <summary>
        /// The variable that captures the result of a pattern matching operation like "i" in "expr is int i"
        /// </summary>
        PatternVariable,

        /// <summary>
        /// User variable declared by a declaration expression in the left-hand-side of a deconstruction assignment.
        /// </summary>
        DeconstructionVariable,

        /// <summary>
        /// User variable declared as an out argument.
        /// </summary>
        OutVariable,

        /// <summary>
        /// User variable declared by a declaration expression in some unsupported context.
        /// This occurs as a result of error recovery in incorrect code.
        /// </summary>
        DeclarationExpressionVariable,
    }
}
