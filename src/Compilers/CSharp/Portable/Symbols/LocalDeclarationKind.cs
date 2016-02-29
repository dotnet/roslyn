// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Specifies the syntax that a user defined variable comes from.
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
        /// User defined local variable declared by <see cref="VariableDeclarationSyntax"/> in <see cref="ForStatementSyntax"/>.
        /// </summary>
        ForInitializerVariable,

        /// <summary>
        /// User defined local variable declared by <see cref="ForEachStatementSyntax"/>.
        /// </summary>
        ForEachIterationVariable,

        /// <summary>
        /// The variable that captures the result of a pattern matching operation like "i" in "expr is int i"
        /// </summary>
        PatternVariable,
    }
}
