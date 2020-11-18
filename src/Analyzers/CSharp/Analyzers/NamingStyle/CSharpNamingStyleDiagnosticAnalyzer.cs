// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

        // Parameters of positional record declarations should be ignored because they also
        // considered properties, and that naming style makes more sense
        protected override bool ShouldIgnore(ISymbol symbol)
            => symbol.IsKind(SymbolKind.Parameter)
            && IsParameterOfRecordDeclaration(symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax());

        private static bool IsParameterOfRecordDeclaration(SyntaxNode? node)
            => node is ParameterSyntax
            {
                Parent: ParameterListSyntax
                {
                    Parent: RecordDeclarationSyntax
                }
            };
    }
}
