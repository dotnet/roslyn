// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class TypeSyntaxGeneratorVisitor : SymbolVisitor<TypeSyntax>
        {
            public static readonly TypeSyntaxGeneratorVisitor Instance = new TypeSyntaxGeneratorVisitor();

            private TypeSyntaxGeneratorVisitor()
            {
            }

            public override TypeSyntax DefaultVisit(ISymbol node)
            {
                throw new NotImplementedException();
            }

            private TTypeSyntax AddInformationTo<TTypeSyntax>(TTypeSyntax syntax, ISymbol symbol)
                where TTypeSyntax : TypeSyntax
            {
                syntax = syntax.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker);
                syntax = syntax.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol));

                return syntax;
            }

            public override TypeSyntax VisitAlias(IAliasSymbol symbol)
            {
                return AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
            }

            public override TypeSyntax VisitArrayType(IArrayTypeSymbol symbol)
            {
                var underlyingNonArrayType = symbol.ElementType;
                while (underlyingNonArrayType.Kind == SymbolKind.ArrayType)
                {
                    underlyingNonArrayType = ((IArrayTypeSymbol)underlyingNonArrayType).ElementType;
                }

                var elementTypeSyntax = underlyingNonArrayType.Accept(this);
                var ranks = new List<ArrayRankSpecifierSyntax>();

                var arrayType = symbol;
                while (arrayType != null)
                {
                    ranks.Add(SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), arrayType.Rank))));

                    arrayType = arrayType.ElementType as IArrayTypeSymbol;
                }

                var arrayTypeSyntax = SyntaxFactory.ArrayType(elementTypeSyntax, ranks.ToSyntaxList());
                return AddInformationTo(arrayTypeSyntax, symbol);
            }

            public override TypeSyntax VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return AddInformationTo(
                    SyntaxFactory.IdentifierName("dynamic"), symbol);
            }

            public TypeSyntax CreateSimpleTypeSyntax(INamedTypeSymbol symbol)
            {
                switch (symbol.SpecialType)
                {
                    case SpecialType.System_Object:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Object"));
                    case SpecialType.System_Void:
                        return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                    case SpecialType.System_Boolean:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Boolean"));
                    case SpecialType.System_Char:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Char"));
                    case SpecialType.System_SByte:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("SByte"));
                    case SpecialType.System_Byte:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Byte"));
                    case SpecialType.System_Int16:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int16"));
                    case SpecialType.System_UInt16:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt16"));
                    case SpecialType.System_Int32:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int32"));
                    case SpecialType.System_UInt32:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt32"));
                    case SpecialType.System_Int64:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Int64"));
                    case SpecialType.System_UInt64:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("UInt64"));
                    case SpecialType.System_Decimal:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Decimal"));
                    case SpecialType.System_Single:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Single"));
                    case SpecialType.System_Double:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Double"));
                    case SpecialType.System_String:
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("String"));
                }

                if (symbol.Name == string.Empty || symbol.IsAnonymousType)
                {
                    return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Object"));
                }

                if (symbol.IsNullable())
                {
                    // Can't have a nullable of a pointer type.  i.e. "int*?" is illegal.
                    var innerType = symbol.TypeArguments.First();
                    if (innerType.TypeKind != TypeKind.Pointer)
                    {
                        return AddInformationTo(
                            SyntaxFactory.NullableType(innerType.Accept(this)), symbol);
                    }
                }

                if (symbol.TypeParameters.Length == 0)
                {
                    if (symbol.TypeKind == TypeKind.Error && symbol.Name == "var")
                    {
                        return SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("Object"));
                    }

                    return symbol.Name.ToIdentifierName();
                }

                var typeArguments = symbol.IsUnboundGenericType
                    ? Enumerable.Repeat(SyntaxFactory.OmittedTypeArgument(), symbol.TypeArguments.Length)
                    : symbol.TypeArguments.Select(t => t.Accept(this));

                return SyntaxFactory.GenericName(
                    symbol.Name.ToIdentifierToken(),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments)));
            }

            public override TypeSyntax VisitNamedType(INamedTypeSymbol symbol)
            {
                var typeSyntax = CreateSimpleTypeSyntax(symbol);
                if (!(typeSyntax is SimpleNameSyntax))
                {
                    return typeSyntax;
                }

                var simpleNameSyntax = (SimpleNameSyntax)typeSyntax;
                if (symbol.ContainingType != null)
                {
                    if (symbol.ContainingType.TypeKind == TypeKind.Submission)
                    {
                        return typeSyntax;
                    }
                    else
                    {
                        var containingTypeSyntax = symbol.ContainingType.Accept(this);
                        if (containingTypeSyntax is NameSyntax)
                        {
                            return AddInformationTo(
                                SyntaxFactory.QualifiedName((NameSyntax)containingTypeSyntax, simpleNameSyntax),
                                symbol);
                        }
                        else
                        {
                            return AddInformationTo(simpleNameSyntax, symbol);
                        }
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
                        var container = symbol.ContainingNamespace.Accept(this);
                        return AddInformationTo(SyntaxFactory.QualifiedName(
                            (NameSyntax)container,
                            simpleNameSyntax), symbol);
                    }
                }

                return simpleNameSyntax;
            }

            public override TypeSyntax VisitNamespace(INamespaceSymbol symbol)
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
                    var container = symbol.ContainingNamespace.Accept(this);
                    return AddInformationTo(SyntaxFactory.QualifiedName(
                        (NameSyntax)container,
                        syntax), symbol);
                }
            }

            public override TypeSyntax VisitPointerType(IPointerTypeSymbol symbol)
            {
                return AddInformationTo(
                    SyntaxFactory.PointerType(symbol.PointedAtType.Accept(this)),
                    symbol);
            }

            public override TypeSyntax VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                return AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
            }
        }
    }
}
