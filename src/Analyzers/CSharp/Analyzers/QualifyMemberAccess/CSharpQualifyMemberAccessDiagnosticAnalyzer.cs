// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.QualifyMemberAccess;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.QualifyMemberAccess;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpQualifyMemberAccessDiagnosticAnalyzer
    : AbstractQualifyMemberAccessDiagnosticAnalyzer<SyntaxKind, ExpressionSyntax, SimpleNameSyntax>
{
    protected override ISimplification Simplification
        => CSharpSimplification.Instance;

    protected override bool IsAlreadyQualifiedMemberAccess(ExpressionSyntax node)
        => node.IsKind(SyntaxKind.ThisExpression);

    // If the member is already qualified with `base.`,
    // or member is in object initialization context,
    // or member in property or field initialization,
    // or member in constructor initializer, it cannot be qualified.
    protected override bool CanMemberAccessBeQualified(ISymbol containingSymbol, SyntaxNode node)
    {
        if (node.GetAncestorOrThis<AttributeSyntax>() != null)
            return false;

        if (node.GetAncestorOrThis<ConstructorInitializerSyntax>() != null)
            return false;

        if (node.IsKind(SyntaxKind.BaseExpression))
            return false;

        if (IsInPropertyOrFieldInitialization(containingSymbol, node))
            return false;

        if (node.Parent is AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax(SyntaxKind.ObjectInitializerExpression), Left: var left } &&
            left == node)
        {
            return false;
        }

        return true;
    }

    private static bool IsInPropertyOrFieldInitialization(ISymbol containingSymbol, SyntaxNode node)
    {
        return (containingSymbol.Kind is SymbolKind.Field or SymbolKind.Property) &&
            containingSymbol.DeclaringSyntaxReferences
                .Select(declaringSyntaxReferences => declaringSyntaxReferences.GetSyntax())
                .Any(declaringSyntax => IsInPropertyInitialization(declaringSyntax, node) || IsInFieldInitialization(declaringSyntax, node));
    }

    private static bool IsInPropertyInitialization(SyntaxNode declarationSyntax, SyntaxNode node)
        => declarationSyntax.IsKind(SyntaxKind.PropertyDeclaration) && declarationSyntax.Contains(node);

    private static bool IsInFieldInitialization(SyntaxNode declarationSyntax, SyntaxNode node)
        => declarationSyntax.GetAncestorsOrThis(n => n.IsKind(SyntaxKind.FieldDeclaration) && n.Contains(node)).Any();

    protected override Location GetLocation(IOperation operation) => operation.Syntax.GetLocation();
}
