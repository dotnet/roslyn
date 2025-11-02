// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal partial class ITypeSymbolExtensions
{
    private sealed class TypeSyntaxGeneratorVisitor(bool nameOnly) : SymbolVisitor<TypeSyntax>
    {
        private static readonly TypeSyntaxGeneratorVisitor NameOnlyInstance = new(nameOnly: true);
        private static readonly TypeSyntaxGeneratorVisitor NotNameOnlyInstance = new(nameOnly: false);

        private static readonly QualifiedNameSyntax SystemObjectType =
            QualifiedName(
                AliasQualifiedName(
                    CreateGlobalIdentifier(),
                    IdentifierName("System")),
                IdentifierName("Object"));

        private readonly bool _nameOnly = nameOnly;

        public static TypeSyntaxGeneratorVisitor Create(bool nameOnly = false)
            => nameOnly ? NameOnlyInstance : NotNameOnlyInstance;

        public override TypeSyntax DefaultVisit(ISymbol node)
            => throw new NotImplementedException();

        private static TTypeSyntax AddInformationTo<TTypeSyntax>(TTypeSyntax syntax, ISymbol symbol)
            where TTypeSyntax : TypeSyntax
        {
            syntax = syntax.WithPrependedLeadingTrivia(ElasticMarker).WithAppendedTrailingTrivia(ElasticMarker);
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
                ranks.Add(ArrayRankSpecifier(
                    [.. Enumerable.Repeat<ExpressionSyntax>(OmittedArraySizeExpression(), arrayType.Rank)]));

                arrayType = arrayType.ElementType as IArrayTypeSymbol;
            }

            TypeSyntax arrayTypeSyntax = ArrayType(elementTypeSyntax, [.. ranks]);

            if (symbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                arrayTypeSyntax = NullableType(arrayTypeSyntax);
            }

            return AddInformationTo(arrayTypeSyntax, symbol);
        }

        public override TypeSyntax VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            var typeSyntax = IdentifierName("dynamic");
            return symbol.NullableAnnotation is NullableAnnotation.Annotated
                ? AddInformationTo(NullableType(typeSyntax), symbol)
                : AddInformationTo(typeSyntax, symbol);
        }

        public static bool TryCreateNativeIntegerType(INamedTypeSymbol symbol, [NotNullWhen(true)] out TypeSyntax? syntax)
        {
            if (symbol.IsNativeIntegerType)
            {
                syntax = IdentifierName(symbol.SpecialType == SpecialType.System_IntPtr ? "nint" : "nuint");
                return true;
            }

            syntax = null;
            return false;
        }

        public override TypeSyntax VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            FunctionPointerCallingConventionSyntax? callingConventionSyntax = null;
            // For varargs there is no C# syntax. You get a use-site diagnostic if you attempt to use it, and just
            // making a default-convention symbol is likely good enough. This is only observable through metadata
            // that always be uncompilable in C# anyway.
            if (symbol.Signature.CallingConvention is not System.Reflection.Metadata.SignatureCallingConvention.Default
                and not System.Reflection.Metadata.SignatureCallingConvention.VarArgs)
            {
                var conventionsList = symbol.Signature.CallingConvention switch
                {
                    System.Reflection.Metadata.SignatureCallingConvention.CDecl => [GetConventionForString("Cdecl")],
                    System.Reflection.Metadata.SignatureCallingConvention.StdCall => [GetConventionForString("Stdcall")],
                    System.Reflection.Metadata.SignatureCallingConvention.ThisCall => [GetConventionForString("Thiscall")],
                    System.Reflection.Metadata.SignatureCallingConvention.FastCall => [GetConventionForString("Fastcall")],
                    System.Reflection.Metadata.SignatureCallingConvention.Unmanaged =>
                        // All types that come from CallingConventionTypes start with "CallConv". We don't want the prefix for the actual
                        // syntax, so strip it off
                        symbol.Signature.UnmanagedCallingConventionTypes.IsEmpty
                            ? null : symbol.Signature.UnmanagedCallingConventionTypes.Select(type => GetConventionForString(type.Name["CallConv".Length..])),

                    _ => throw ExceptionUtilities.UnexpectedValue(symbol.Signature.CallingConvention),
                };

                callingConventionSyntax = FunctionPointerCallingConvention(
                    UnmanagedKeyword,
                    conventionsList is object
                        ? FunctionPointerUnmanagedCallingConventionList([.. conventionsList])
                        : null);

                static FunctionPointerUnmanagedCallingConventionSyntax GetConventionForString(string identifier)
                    => FunctionPointerUnmanagedCallingConvention(Identifier(identifier));
            }

            var parameters = symbol.Signature.Parameters.Select(p => (p.Type, RefKindModifiers: CSharpSyntaxGeneratorInternal.GetParameterModifiers(p)))
                .Concat([(
                    Type: symbol.Signature.ReturnType,
                    RefKindModifiers: CSharpSyntaxGeneratorInternal.GetParameterModifiers(isScoped: false, symbol.Signature.RefKind, isParams: false, forFunctionPointerReturnParameter: true))])
                .SelectAsArray(t => FunctionPointerParameter(t.Type.GenerateTypeSyntax()).WithModifiers(t.RefKindModifiers));

            return AddInformationTo(
                FunctionPointerType(callingConventionSyntax, FunctionPointerParameterList([.. parameters])), symbol);
        }

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
                return SystemObjectType;

            if (symbol.TypeParameters.Length == 0)
            {
                if (symbol.TypeKind == TypeKind.Error && symbol.Name == "var")
                    return SystemObjectType;

                return symbol.Name.ToIdentifierName();
            }

            var typeArguments = symbol.IsUnboundGenericType
                ? Enumerable.Repeat((TypeSyntax)OmittedTypeArgument(), symbol.TypeArguments.Length)
                : symbol.TypeArguments.SelectAsArray(t => t.GenerateTypeSyntax());

            return GenericName(
                symbol.Name.ToIdentifierToken(),
                TypeArgumentList([.. typeArguments]));
        }

        public static QualifiedNameSyntax CreateSystemObject()
            => SystemObjectType;

        private static IdentifierNameSyntax CreateGlobalIdentifier()
            => IdentifierName(GlobalKeyword);

        private static TypeSyntax? TryCreateSpecializedNamedTypeSyntax(INamedTypeSymbol symbol)
        {
            if (symbol.SpecialType == SpecialType.System_Void)
            {
                return PredefinedType(VoidKeyword);
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
                        NullableType(innerType.GenerateTypeSyntax()), symbol);
                }
            }

            return null;
        }

        private static TupleTypeSyntax CreateTupleTypeSyntax(INamedTypeSymbol symbol)
        {
            var list = new SeparatedSyntaxList<TupleElementSyntax>();

            foreach (var element in symbol.TupleElements)
            {
                var name = element.IsImplicitlyDeclared ? default : element.Name.ToIdentifierToken();
                list = list.Add(TupleElement(element.Type.GenerateTypeSyntax(), name));
            }

            return AddInformationTo(TupleType(list), symbol);
        }

        public override TypeSyntax VisitNamedType(INamedTypeSymbol symbol)
        {
            if (TryCreateNativeIntegerType(symbol, out var typeSyntax))
                return typeSyntax;

            typeSyntax = CreateSimpleTypeSyntax(symbol);
            if (typeSyntax is not SimpleNameSyntax)
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
                            QualifiedName(name, simpleNameSyntax),
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
                    var container = symbol.ContainingNamespace.Accept(this)!;
                    typeSyntax = AddInformationTo(QualifiedName(
                        (NameSyntax)container,
                        simpleNameSyntax), symbol);
                }
            }

            if (symbol is { IsValueType: false, NullableAnnotation: NullableAnnotation.Annotated })
            {
                // value type with nullable annotation may be composed from unconstrained nullable generic
                // doesn't mean nullable value type in this case
                typeSyntax = AddInformationTo(NullableType(typeSyntax), symbol);
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
                var container = symbol.ContainingNamespace.Accept(this)!;
                return AddInformationTo(QualifiedName(
                    (NameSyntax)container,
                    syntax), symbol);
            }
        }

        /// <summary>
        /// We always unilaterally add "global::" to all named types/namespaces.  This
        /// will then be trimmed off if possible by the simplifier.
        /// </summary>
        private static TypeSyntax AddGlobalAlias(INamespaceOrTypeSymbol symbol, SimpleNameSyntax syntax)
        {
            return AddInformationTo(
                AliasQualifiedName(
                    CreateGlobalIdentifier(),
                    syntax), symbol);
        }

        public override TypeSyntax VisitPointerType(IPointerTypeSymbol symbol)
        {
            ThrowIfNameOnly();

            return AddInformationTo(
                PointerType(symbol.PointedAtType.GenerateTypeSyntax()),
                symbol);
        }

        public override TypeSyntax VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            TypeSyntax typeSyntax = AddInformationTo(symbol.Name.ToIdentifierName(), symbol);
            if (symbol is { IsValueType: false, NullableAnnotation: NullableAnnotation.Annotated })
            {
                // value type with nullable annotation may be composed from unconstrained nullable generic
                // doesn't mean nullable value type in this case
                typeSyntax = AddInformationTo(NullableType(typeSyntax), symbol);
            }

            return typeSyntax;
        }
    }
}
