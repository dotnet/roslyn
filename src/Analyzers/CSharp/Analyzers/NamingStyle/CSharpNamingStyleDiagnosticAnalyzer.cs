// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.NamingStyles;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpNamingStyleDiagnosticAnalyzer : NamingStyleDiagnosticAnalyzerBase<SyntaxKind>
{
    protected override ImmutableArray<SyntaxKind> SupportedSyntaxKinds { get; } =
        [
            SyntaxKind.VariableDeclarator,
            SyntaxKind.ForEachStatement,
            SyntaxKind.CatchDeclaration,
            SyntaxKind.SingleVariableDesignation,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.Parameter,
            SyntaxKind.TypeParameter,
        ];

    protected override bool ShouldIgnore(ISymbol symbol)
    {
        if (symbol.IsKind(SymbolKind.Parameter)
            && symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is ParameterSyntax
            {
                Parent: ParameterListSyntax
                {
                    Parent: RecordDeclarationSyntax
                }
            })
        {
            // Parameters of positional record declarations should be ignored because they also
            // considered properties, and that naming style makes more sense
            return true;
        }

        if (!symbol.CanBeReferencedByName)
        {
            // Explicit interface implementation falls into here, as they don't own their names
            // Two symbols are involved here, and symbol.ExplicitInterfaceImplementations only applies for one
            return true;
        }

        if (symbol.IsExtern)
        {
            // Extern symbols are mainly P/Invoke and runtime invoke, probably requiring their name
            // to match external definition exactly.
            // Simply ignoring them.
            return true;
        }

        return false;
    }
}
