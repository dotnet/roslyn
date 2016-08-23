// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PreferFrameworkType;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpPreferFrameworkTypeDiagnosticAnalyzer : PreferFrameworkTypeDiagnosticAnalyzerBase<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => 
            ImmutableArray.Create(SyntaxKind.PredefinedType);

        protected override bool IsPredefinedTypeAndReplaceableWithFrameworkType(SyntaxNode node)
        {
            var syntaxKind = (node as PredefinedTypeSyntax)?.Keyword.Kind();

            // every predefined type keyword except `void` can be replaced by its framework type in code.
            return syntaxKind != null && 
                   syntaxKind != SyntaxKind.VoidKeyword && 
                   SyntaxFacts.IsPredefinedType(syntaxKind.Value);
        }

        protected override bool IsInMemberAccessContext(SyntaxNode node, SemanticModel semanticModel)
        {
            var expression = (node as ExpressionSyntax);
            if (expression == null)
            {
                return false;
            }

            return expression.IsInMemberAccessContext() || expression.InsideCrefReference();
        }
    }
}
