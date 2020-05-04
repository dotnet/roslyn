// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNamingStyleDiagnosticAnalyzer : NamingStyleDiagnosticAnalyzerBase<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> SupportedSyntaxKinds { get; } =
            ImmutableArray.Create(
                SyntaxKind.VariableDeclarator,
                SyntaxKind.ForEachStatement,
                SyntaxKind.CatchDeclaration,
                SyntaxKind.SingleVariableDesignation,
                SyntaxKind.LocalFunctionStatement,
                SyntaxKind.Parameter,
                SyntaxKind.TypeParameter);
    }
}
