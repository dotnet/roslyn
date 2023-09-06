// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class ExpressionSyntaxGeneratorVisitor : SymbolVisitor<ExpressionSyntax>
        {
            private static readonly ExpressionSyntaxGeneratorVisitor NameOnlyInstance = new(nameOnly: true);
            private static readonly ExpressionSyntaxGeneratorVisitor NotNameOnlyInstance = new(nameOnly: false);

            private readonly bool _nameOnly;

            private ExpressionSyntaxGeneratorVisitor(bool nameOnly)
                => _nameOnly = nameOnly;

            public static ExpressionSyntaxGeneratorVisitor Create(bool nameOnly)
                => nameOnly ? NameOnlyInstance : NotNameOnlyInstance;

            public override ExpressionSyntax DefaultVisit(ISymbol symbol)
                => symbol.Accept(TypeSyntaxGeneratorVisitor.Create(_nameOnly))!;

            private static TExpressionSyntax AddInformationTo<TExpressionSyntax>(TExpressionSyntax syntax, ISymbol symbol)
                where TExpressionSyntax : ExpressionSyntax
            {
                syntax = syntax.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker);
                syntax = syntax.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol));

                return syntax;
            }

            public override ExpressionSyntax VisitNamedType(INamedTypeSymbol symbol)
            {
                if (!_nameOnly && TypeSyntaxGeneratorVisitor.TryCreateNativeIntegerType(symbol, out var typeSyntax))
                    return typeSyntax;

                typeSyntax = TypeSyntaxGeneratorVisitor.Create().CreateSimpleTypeSyntax(symbol);
                if (typeSyntax is not SimpleNameSyntax)
                    return typeSyntax;

                var simpleNameSyntax = (SimpleNameSyntax)typeSyntax;
                if (symbol.ContainingType != null)
                {
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        return simpleNameSyntax;
                    }
                    else
                    {
                        var container = symbol.ContainingType.Accept(this)!;
                        return CreateMemberAccessExpression(symbol, container, simpleNameSyntax);
                    }
                }
                else if (symbol.ContainingNamespace != null)
                {
                    if (symbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        if (symbol.TypeKind != TypeKind.Error)
                        {
                            return AddInformationTo(
                                SyntaxFactory.AliasQualifiedName(
                                    SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                                    simpleNameSyntax), symbol);
                        }
                    }
                    else
                    {
                        var container = symbol.ContainingNamespace.Accept(this)!;
                        return CreateMemberAccessExpression(symbol, container, simpleNameSyntax);
                    }
                }

                return simpleNameSyntax;
            }

            public override ExpressionSyntax VisitNamespace(INamespaceSymbol symbol)
            {
                var syntax = AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
                if (symbol.ContainingNamespace == null)
                {
                    return syntax;
                }

                if (symbol.ContainingNamespace.IsGlobalNamespace)
                {
                    return AddInformationTo(
                        SyntaxFactory.AliasQualifiedName(
                            SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)),
                            syntax), symbol);
                }
                else
                {
                    var container = symbol.ContainingNamespace.Accept(this)!;
                    return CreateMemberAccessExpression(symbol, container, syntax);
                }
            }

            private static MemberAccessExpressionSyntax CreateMemberAccessExpression(
                ISymbol symbol, ExpressionSyntax container, SimpleNameSyntax syntax)
            {
                return AddInformationTo(SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    container, syntax), symbol);
            }
        }
    }
}
