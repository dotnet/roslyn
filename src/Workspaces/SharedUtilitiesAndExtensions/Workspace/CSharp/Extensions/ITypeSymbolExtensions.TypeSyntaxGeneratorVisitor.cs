﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
#if !CODE_STYLE
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
#endif
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

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
                => _nameOnly = nameOnly;

            public static TypeSyntaxGeneratorVisitor Create(bool nameOnly = false)
                => nameOnly ? NameOnlyInstance : NotNameOnlyInstance;

            public override TypeSyntax DefaultVisit(ISymbol node)
                => throw new NotImplementedException();

            private static TTypeSyntax AddInformationTo<TTypeSyntax>(TTypeSyntax syntax, ISymbol symbol)
                where TTypeSyntax : TypeSyntax
            {
                syntax = syntax.WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker);
                syntax = syntax.WithAdditionalAnnotations(SymbolAnnotation.Create(symbol));

                return syntax;
            }

            public override TypeSyntax VisitAlias(IAliasSymbol symbol)
                => AddInformationTo(symbol.Name.ToIdentifierName(), symbol);

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
                    underlyingType = innerArray.ElementType;

                    if (underlyingType.NullableAnnotation == NullableAnnotation.Annotated)
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
                using var _ = ArrayBuilder<ArrayRankSpecifierSyntax>.GetInstance(out var ranks);

                var arrayType = symbol;
                while (arrayType != null && !arrayType.Equals(underlyingType))
                {
                    ranks.Add(SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SeparatedList(Enumerable.Repeat<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression(), arrayType.Rank))));

                    arrayType = arrayType.ElementType as IArrayTypeSymbol;
                }

                TypeSyntax arrayTypeSyntax = SyntaxFactory.ArrayType(elementTypeSyntax, ranks.ToSyntaxList());

                if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    arrayTypeSyntax = SyntaxFactory.NullableType(arrayTypeSyntax);
                }

                return AddInformationTo(arrayTypeSyntax, symbol);
            }

            public override TypeSyntax VisitDynamicType(IDynamicTypeSymbol symbol)
                => AddInformationTo(SyntaxFactory.IdentifierName("dynamic"), symbol);

            public static bool TryCreateNativeIntegerType(INamedTypeSymbol symbol, out TypeSyntax syntax)
            {
#if !CODE_STYLE // TODO: Remove the #if once IsNativeIntegerType is available.
                // https://github.com/dotnet/roslyn/issues/41462 tracks adding this support
                if (symbol.IsNativeIntegerType)
                {
                    syntax = SyntaxFactory.IdentifierName(symbol.SpecialType == SpecialType.System_IntPtr ? "nint" : "nuint");
                    return true;
                }
#endif

                syntax = null;
                return false;
            }

#if !CODE_STYLE

            public override TypeSyntax VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
            {
                // TODO(https://github.com/dotnet/roslyn/issues/39865): generate the calling convention once exposed through the API
                var parameters = symbol.Signature.Parameters.Select(p => (p.Type, RefKindModifiers: CSharpSyntaxGenerator.GetParameterModifiers(p.RefKind)))
                    .Concat(SpecializedCollections.SingletonEnumerable((
                        Type: symbol.Signature.ReturnType,
                        RefKindModifiers: CSharpSyntaxGenerator.GetParameterModifiers(symbol.Signature.RefKind, forFunctionPointerReturnParameter: true))))
                    .SelectAsArray(t => SyntaxFactory.Parameter(SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken)).WithModifiers(t.RefKindModifiers).WithType(t.Type.GenerateTypeSyntax()));

                return AddInformationTo(
                    SyntaxFactory.FunctionPointerType(SyntaxFactory.SeparatedList(parameters)), symbol);
            }

#endif

            public TypeSyntax CreateSimpleTypeSyntax(INamedTypeSymbol symbol)
            {
                if (!_nameOnly)
                {
                    var syntax = TryCreateSpecializedNamedTypeSyntax(symbol);
                    if (syntax != null)
                        return syntax;
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
                    : symbol.TypeArguments.SelectAsArray(t => t.GenerateTypeSyntax());

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
                => SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));

            private static TypeSyntax TryCreateSpecializedNamedTypeSyntax(INamedTypeSymbol symbol)
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

            private static TupleTypeSyntax CreateTupleTypeSyntax(INamedTypeSymbol symbol)
            {
                var list = new SeparatedSyntaxList<TupleElementSyntax>();

                foreach (var element in symbol.TupleElements)
                {
                    var name = element.IsImplicitlyDeclared ? default : SyntaxFactory.Identifier(element.Name);
                    list = list.Add(SyntaxFactory.TupleElement(element.Type.GenerateTypeSyntax(), name));
                }

                return AddInformationTo(SyntaxFactory.TupleType(list), symbol);
            }

            public override TypeSyntax VisitNamedType(INamedTypeSymbol symbol)
            {
                if (TryCreateNativeIntegerType(symbol, out var typeSyntax))
                    return typeSyntax;

                typeSyntax = CreateSimpleTypeSyntax(symbol);
                if (!(typeSyntax is SimpleNameSyntax))
                    return typeSyntax;

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

                if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
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
            private static TypeSyntax AddGlobalAlias(INamespaceOrTypeSymbol symbol, SimpleNameSyntax syntax)
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
                TypeSyntax typeSyntax = AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
                if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
                    typeSyntax = AddInformationTo(SyntaxFactory.NullableType(typeSyntax), symbol);

                return typeSyntax;
            }
        }
    }
}
