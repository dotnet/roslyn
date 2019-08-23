// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal partial class ITypeSymbolExtensions
    {
        private class TypeSyntaxGeneratorVisitor : SymbolVisitor<TypeSyntax>
        {
            private readonly bool _nameOnly;

            private static readonly TypeSyntaxGeneratorVisitor NameOnlyInstance = new TypeSyntaxGeneratorVisitor(nameOnly: true);
            private static readonly TypeSyntaxGeneratorVisitor NotNameOnlyInstance = new TypeSyntaxGeneratorVisitor(nameOnly: false);

            private TypeSyntaxGeneratorVisitor(bool nameOnly)
            {
                _nameOnly = nameOnly;
            }

            public static TypeSyntaxGeneratorVisitor Create(bool nameOnly = false)
            {
                return nameOnly ? NameOnlyInstance : NotNameOnlyInstance;
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

            private void ThrowIfNameOnly()
            {
                if (_nameOnly)
                {
                    throw new InvalidOperationException("This symbol cannot be converted into a NameSyntax");
                }
            }

            public override TypeSyntax VisitArrayType(IArrayTypeSymbol symbol)
            {
                ThrowIfNameOnly();

                ITypeSymbol underlyingType = symbol;

                while (underlyingType is IArrayTypeSymbol innerArray)
                {
                    underlyingType = innerArray.GetElementTypeWithAnnotatedNullability();

                    if (underlyingType.GetNullability() == NullableAnnotation.Annotated)
                    {
                        // If the inner array we just moved to is also nullable, then
                        // we must terminate the digging now so we produce the syntax for that,
                        // and then append the ranks we passed through at the end. This is because
                        // nullability annotations acts as a "barrier" where we won't reorder array
                        // through. So whereas:
                        //
                        //     string[][,]
                        //
                        // is really an array of rank 1 that has an element of rank 2,
                        //
                        //     string[]?[,]
                        //
                        // is really an array of rank 2 that has nullable elements of rank 1.

                        break;
                    }
                }

                var elementTypeSyntax = underlyingType.GenerateTypeSyntax();
                var ranks = new List<ArrayRankSpecifierSyntax>();

                var arrayType = symbol;
                while (arrayType != null && !arrayType.Equals(underlyingType))
                {
                    ranks.Add(SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), arrayType.Rank))));

                    arrayType = arrayType.GetElementTypeWithAnnotatedNullability() as IArrayTypeSymbol;
                }

                TypeSyntax arrayTypeSyntax = SyntaxFactory.ArrayType(elementTypeSyntax, ranks.ToSyntaxList());

                if (symbol.GetNullability() == NullableAnnotation.Annotated)
                {
                    arrayTypeSyntax = SyntaxFactory.NullableType(arrayTypeSyntax);
                }

                return AddInformationTo(arrayTypeSyntax, symbol);
            }

            public override TypeSyntax VisitDynamicType(IDynamicTypeSymbol symbol)
            {
                return AddInformationTo(
                    SyntaxFactory.IdentifierName("dynamic"), symbol);
            }

            public TypeSyntax CreateSimpleTypeSyntax(INamedTypeSymbol symbol)
            {
                if (!_nameOnly)
                {
                    var syntax = TryCreateSpecializedNamedTypeSyntax(symbol);
                    if (syntax != null)
                    {
                        return syntax;
                    }
                }

                if (symbol.IsTupleType && symbol.TupleUnderlyingType != null && !symbol.Equals(symbol.TupleUnderlyingType))
                {
                    return CreateSimpleTypeSyntax(symbol.TupleUnderlyingType);
                }

                if (symbol.Name == string.Empty || symbol.IsAnonymousType)
                {
                    return CreateSystemObject();
                }

                if (symbol.TypeParameters.Length == 0)
                {
                    if (symbol.TypeKind == TypeKind.Error && symbol.Name == "var")
                    {
                        return CreateSystemObject();
                    }

                    return symbol.Name.ToIdentifierName();
                }

                var typeArguments = symbol.IsUnboundGenericType
                    ? Enumerable.Repeat((TypeSyntax)SyntaxFactory.OmittedTypeArgument(), symbol.TypeArguments.Length)
                    : symbol.TypeArguments.ZipAsArray(symbol.TypeArgumentNullableAnnotations, (t, n) => t.WithNullability(n).GenerateTypeSyntax());

                return SyntaxFactory.GenericName(
                    symbol.Name.ToIdentifierToken(),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(typeArguments)));
            }

            private static QualifiedNameSyntax CreateSystemObject()
            {
                return SyntaxFactory.QualifiedName(
                    SyntaxFactory.AliasQualifiedName(
                        CreateGlobalIdentifier(),
                        SyntaxFactory.IdentifierName("System")),
                    SyntaxFactory.IdentifierName("Object"));
            }

            private static IdentifierNameSyntax CreateGlobalIdentifier()
            {
                return SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
            }

            private TypeSyntax TryCreateSpecializedNamedTypeSyntax(INamedTypeSymbol symbol)
            {
                if (symbol.SpecialType == SpecialType.System_Void)
                {
                    return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
                }

                if (symbol.IsTupleType && symbol.TupleElements.Length >= 2)
                {
                    return CreateTupleTypeSyntax(symbol);
                }

                if (symbol.IsNullable())
                {
                    // Can't have a nullable of a pointer type.  i.e. "int*?" is illegal.
                    var innerType = symbol.TypeArguments.First();
                    if (innerType.TypeKind != TypeKind.Pointer)
                    {
                        return AddInformationTo(
                            SyntaxFactory.NullableType(innerType.GenerateTypeSyntax()), symbol);
                    }
                }

                return null;
            }

            private TupleTypeSyntax CreateTupleTypeSyntax(INamedTypeSymbol symbol)
            {
                var list = new SeparatedSyntaxList<TupleElementSyntax>();

                foreach (var element in symbol.TupleElements)
                {
                    var name = element.IsImplicitlyDeclared ? default : SyntaxFactory.Identifier(element.Name);
                    list = list.Add(SyntaxFactory.TupleElement(element.GetTypeWithAnnotatedNullability().GenerateTypeSyntax(), name));
                }

                return AddInformationTo(SyntaxFactory.TupleType(list), symbol);
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
                    if (symbol.ContainingType.TypeKind != TypeKind.Submission)
                    {
                        var containingTypeSyntax = symbol.ContainingType.Accept(this);
                        if (containingTypeSyntax is NameSyntax name)
                        {
                            typeSyntax = AddInformationTo(
                                SyntaxFactory.QualifiedName(name, simpleNameSyntax),
                                symbol);
                        }
                        else
                        {
                            typeSyntax = AddInformationTo(simpleNameSyntax, symbol);
                        }
                    }
                }
                else if (symbol.ContainingNamespace != null)
                {
                    if (symbol.ContainingNamespace.IsGlobalNamespace)
                    {
                        if (symbol.TypeKind != TypeKind.Error)
                        {
                            typeSyntax = AddGlobalAlias(symbol, simpleNameSyntax);
                        }
                    }
                    else
                    {
                        var container = symbol.ContainingNamespace.Accept(this);
                        typeSyntax = AddInformationTo(SyntaxFactory.QualifiedName(
                            (NameSyntax)container,
                            simpleNameSyntax), symbol);
                    }
                }

                if (symbol.GetNullability() == NullableAnnotation.Annotated)
                {
                    typeSyntax = AddInformationTo(SyntaxFactory.NullableType(typeSyntax), symbol);
                }

                return typeSyntax;
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
                    return AddGlobalAlias(symbol, syntax);
                }
                else
                {
                    var container = symbol.ContainingNamespace.Accept(this);
                    return AddInformationTo(SyntaxFactory.QualifiedName(
                        (NameSyntax)container,
                        syntax), symbol);
                }
            }

            /// <summary>
            /// We always unilaterally add "global::" to all named types/namespaces.  This
            /// will then be trimmed off if possible by calls to 
            /// <see cref="Simplifier.ReduceAsync(Document, OptionSet, CancellationToken)"/>
            /// </summary>
            private TypeSyntax AddGlobalAlias(INamespaceOrTypeSymbol symbol, SimpleNameSyntax syntax)
            {
                return AddInformationTo(
                    SyntaxFactory.AliasQualifiedName(
                        CreateGlobalIdentifier(),
                        syntax), symbol);
            }

            public override TypeSyntax VisitPointerType(IPointerTypeSymbol symbol)
            {
                ThrowIfNameOnly();

                return AddInformationTo(
                    SyntaxFactory.PointerType(symbol.PointedAtType.GenerateTypeSyntax()),
                    symbol);
            }

            public override TypeSyntax VisitTypeParameter(ITypeParameterSymbol symbol)
            {
                return AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
            }
        }
    }
}
