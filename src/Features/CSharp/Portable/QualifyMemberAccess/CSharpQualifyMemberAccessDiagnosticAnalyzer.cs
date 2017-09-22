// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.QualifyMemberAccess;

namespace Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpQualifyMemberAccessDiagnosticAnalyzer : AbstractQualifyMemberAccessDiagnosticAnalyzer<SyntaxKind>
    {
        protected override string GetLanguageName()
            => LanguageNames.CSharp;

        protected override bool IsAlreadyQualifiedMemberAccess(SyntaxNode node)
            => node.IsKind(SyntaxKind.ThisExpression);

        // If the member is already qualified with `base.`, it cannot be further qualified.
        protected override bool CanMemberAccessBeQualified(ISymbol containingSymbol, SyntaxNode node)
            => !(node.IsKind(SyntaxKind.BaseExpression) || IsInPropertyOrFieldInitialization(containingSymbol, node));

        private bool IsInPropertyOrFieldInitialization(ISymbol containingSymbol, SyntaxNode node)
        {
            return (containingSymbol.Kind == SymbolKind.Field || containingSymbol.Kind == SymbolKind.Property) &&
                containingSymbol.DeclaringSyntaxReferences
                    .Select(declaringSyntaxReferences => declaringSyntaxReferences.GetSyntax())
                    .Any(declaringSyntax => IsInPropertyInitialization(declaringSyntax, node) || IsInFieldInitialization(declaringSyntax, node));
        }

        private bool IsInPropertyInitialization(SyntaxNode declarationSyntax, SyntaxNode node)
            => declarationSyntax.IsKind(SyntaxKind.PropertyDeclaration) && declarationSyntax.Contains(node);

        private bool IsInFieldInitialization(SyntaxNode declarationSyntax, SyntaxNode node)
            => declarationSyntax.GetAncestorsOrThis(n => n.IsKind(SyntaxKind.FieldDeclaration) && n.Contains(node)).Any();
    }
}
