// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.PreferFrameworkType;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpPreferFrameworkTypeDiagnosticAnalyzer : 
        PreferFrameworkTypeDiagnosticAnalyzerBase<SyntaxKind, ExpressionSyntax, PredefinedTypeSyntax>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest => 
            ImmutableArray.Create(SyntaxKind.PredefinedType);

        protected override bool IsPredefinedTypeReplaceableWithFrameworkType(PredefinedTypeSyntax node)
        {
            var syntaxKind = node.Keyword.Kind();

            // every predefined type keyword except `void` can be replaced by its framework type in code.
            return syntaxKind != SyntaxKind.VoidKeyword && SyntaxFacts.IsPredefinedType(syntaxKind);
        }

        protected override bool IsInMemberAccessOrCrefReferenceContext(ExpressionSyntax node) => 
            node.IsInMemberAccessContext() || node.InsideCrefReference();
    }
}
