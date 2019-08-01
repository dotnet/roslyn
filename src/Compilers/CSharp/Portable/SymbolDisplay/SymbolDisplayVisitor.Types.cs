// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.SymbolDisplay;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        private void VisitTypeWithAnnotations(TypeWithAnnotations type, AbstractSymbolDisplayVisitor visitorOpt = null)
        {
            var visitor = visitorOpt ?? this.NotFirstVisitor;
            var typeSymbol = type.Type;

            typeSymbol.Accept(visitor);
            AddNullableAnnotations(type);
        }

        public override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            VisitArrayType(symbol, typeOpt: default);
        }

        private void VisitArrayType(IArrayTypeSymbol symbol, TypeWithAnnotations typeOpt)
        {
            if (TryAddAlias(symbol, builder))
            {
                return;
            }

            //See spec section 12.1 for the order of rank specifiers
            //e.g. int[][,][,,] is stored as
            //     ArrayType
            //         Rank = 1
            //         ElementType = ArrayType
            //             Rank = 2
            //             ElementType = ArrayType
            //                 Rank = 3
            //                 ElementType = int

            if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers))
            {
                // Ironically, reverse order is simpler - we just have to recurse on the element type and then add a rank specifier.
                symbol.ElementType.Accept(this);
                AddArrayRank(symbol);
                return;
            }

            TypeWithAnnotations underlyingTypeWithAnnotations;
            ITypeSymbol underlyingType = symbol;
            do
            {
                underlyingTypeWithAnnotations = (underlyingType as ArrayTypeSymbol)?.ElementTypeWithAnnotations ?? default;
                underlyingType = ((IArrayTypeSymbol)underlyingType).ElementType;
            }
            while (underlyingType.Kind == SymbolKind.ArrayType && !ShouldAddNullableAnnotation(underlyingTypeWithAnnotations));

            if (underlyingTypeWithAnnotations.HasType)
            {
                VisitTypeWithAnnotations(underlyingTypeWithAnnotations);
            }
            else
            {
                underlyingType.Accept(this.NotFirstVisitor);
            }

            var arrayType = symbol;
            while (arrayType != null && arrayType != underlyingType)
            {
                if (!this.isFirstSymbolVisited)
                {
                    AddCustomModifiersIfRequired(arrayType.CustomModifiers, leadingSpace: true);
                }

                AddArrayRank(arrayType);
                arrayType = arrayType.ElementType as IArrayTypeSymbol;
            }
        }

        private void AddNullableAnnotations(TypeWithAnnotations typeOpt)
        {
            if (ShouldAddNullableAnnotation(typeOpt))
            {
                AddPunctuation(typeOpt.NullableAnnotation.IsAnnotated() ? SyntaxKind.QuestionToken : SyntaxKind.ExclamationToken);
            }
        }

        private bool ShouldAddNullableAnnotation(TypeWithAnnotations typeOpt)
        {
            if (!typeOpt.HasType)
            {
                return false;
            }
            else if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                !typeOpt.IsNullableType() && !typeOpt.Type.IsValueType &&
                typeOpt.NullableAnnotation.IsAnnotated())
            {
                return true;
            }
            else if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier) &&
                !typeOpt.Type.IsValueType &&
                typeOpt.NullableAnnotation.IsNotAnnotated() && !typeOpt.Type.IsTypeParameterDisallowingAnnotation())
            {
                return true;
            }

            return false;
        }

        private void AddArrayRank(IArrayTypeSymbol symbol)
        {
            bool insertStars = format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays);

            AddPunctuation(SyntaxKind.OpenBracketToken);

            if (symbol.Rank > 1)
            {
                if (insertStars)
                {
                    AddPunctuation(SyntaxKind.AsteriskToken);
                }
            }
            else
            {
                var array = symbol as ArrayTypeSymbol;

                if ((object)array != null && !array.IsSZArray)
                {
                    // Always add an asterisk in this case in order to distinguish between SZArray and MDArray.
                    AddPunctuation(SyntaxKind.AsteriskToken);
                }
            }

            for (int i = 0; i < symbol.Rank - 1; i++)
            {
                AddPunctuation(SyntaxKind.CommaToken);

                if (insertStars)
                {
                    AddPunctuation(SyntaxKind.AsteriskToken);
                }
            }

            AddPunctuation(SyntaxKind.CloseBracketToken);
        }

        public override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            var pointer = symbol as PointerTypeSymbol;

            if ((object)pointer == null)
            {
                symbol.PointedAtType.Accept(this.NotFirstVisitor);
            }
            else
            {
                VisitTypeWithAnnotations(pointer.PointedAtTypeWithAnnotations);
            }

            if (!this.isFirstSymbolVisited)
            {
                AddCustomModifiersIfRequired(symbol.CustomModifiers, leadingSpace: true);
            }

            AddPunctuation(SyntaxKind.AsteriskToken);
        }

        public override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            if (this.isFirstSymbolVisited)
            {
                AddTypeParameterVarianceIfRequired(symbol);
            }

            //variance and constraints are handled by methods and named types
            builder.Add(CreatePart(SymbolDisplayPartKind.TypeParameterName, symbol, symbol.Name));
        }

        public override void VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, symbol.Name));
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if (this.IsMinimizing && TryAddAlias(symbol, builder))
            {
                return;
            }

            if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseSpecialTypes))
            {
                if (AddSpecialTypeKeyword(symbol))
                {
                    //if we're using special type keywords and this is a special type, then no other work is required
                    return;
                }
            }

            if (!format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.ExpandNullable))
            {
                //if we're expanding nullable, we just visit nullable types normally
                if (ITypeSymbolHelpers.IsNullableType(symbol) && !symbol.IsDefinition)
                {
                    // Can't have a type called "int*?".
                    var typeArg = symbol.TypeArguments[0];
                    if (typeArg.TypeKind != TypeKind.Pointer)
                    {
                        typeArg.Accept(this.NotFirstVisitor);
                        AddCustomModifiersIfRequired(symbol.GetTypeArgumentCustomModifiers(0), leadingSpace: true, trailingSpace: false);

                        AddPunctuation(SyntaxKind.QuestionToken);

                        //visiting the underlying type did all of the work for us
                        return;
                    }
                }
            }

            if (this.IsMinimizing || symbol.IsTupleType)
            {
                MinimallyQualify(symbol);
                return;
            }

            AddTypeKind(symbol);

            if (CanShowDelegateSignature(symbol))
            {
                if (format.DelegateStyle == SymbolDisplayDelegateStyle.NameAndSignature)
                {
                    var invokeMethod = symbol.DelegateInvokeMethod;
                    if (invokeMethod.ReturnsByRef)
                    {
                        AddRefIfRequired();
                    }
                    else if (invokeMethod.ReturnsByRefReadonly)
                    {
                        AddRefReadonlyIfRequired();
                    }

                    if (invokeMethod.ReturnsVoid)
                    {
                        AddKeyword(SyntaxKind.VoidKeyword);
                    }
                    else
                    {
                        AddReturnType(symbol.DelegateInvokeMethod);
                    }

                    AddSpace();
                }
            }

            //only visit the namespace if the style requires it and there isn't an enclosing type
            var containingSymbol = symbol.ContainingSymbol;
            if (ShouldVisitNamespace(containingSymbol))
            {
                var namespaceSymbol = (INamespaceSymbol)containingSymbol;
                var shouldSkip = namespaceSymbol.IsGlobalNamespace && symbol.TypeKind == TypeKind.Error;

                if (!shouldSkip)
                {
                    namespaceSymbol.Accept(this.NotFirstVisitor);
                    AddPunctuation(namespaceSymbol.IsGlobalNamespace ? SyntaxKind.ColonColonToken : SyntaxKind.DotToken);
                }
            }

            //visit the enclosing type if the style requires it
            if (format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameAndContainingTypes ||
                format.TypeQualificationStyle == SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
            {
                if (IncludeNamedType(symbol.ContainingType))
                {
                    symbol.ContainingType.Accept(this.NotFirstVisitor);
                    AddPunctuation(SyntaxKind.DotToken);
                }
            }

            AddNameAndTypeArgumentsOrParameters(symbol);
        }

        private void AddNameAndTypeArgumentsOrParameters(INamedTypeSymbol symbol)
        {
            if (symbol.IsAnonymousType)
            {
                AddAnonymousTypeName(symbol);
                return;
            }
            else if (symbol.IsTupleType)
            {
                // If top level tuple uses non-default names, there is no way to preserve them
                // unless we use tuple syntax for the type. So, we give them priority.
                if (!format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseValueTuple))
                {
                    if (HasNonDefaultTupleElements(symbol) || CanUseTupleTypeName(symbol))
                    {
                        AddTupleTypeName(symbol);
                        return;
                    }
                }
                // Fall back to displaying the underlying type.
                symbol = symbol.TupleUnderlyingType;
            }

            string symbolName = null;

            // It would be nice to handle VB NoPia symbols too, but it's not worth the effort.

            var illegalGenericInstantiationSymbol = symbol as NoPiaIllegalGenericInstantiationSymbol;

            if ((object)illegalGenericInstantiationSymbol != null)
            {
                symbol = illegalGenericInstantiationSymbol.UnderlyingSymbol;
            }
            else
            {
                var ambiguousCanonicalTypeSymbol = symbol as NoPiaAmbiguousCanonicalTypeSymbol;

                if ((object)ambiguousCanonicalTypeSymbol != null)
                {
                    symbol = ambiguousCanonicalTypeSymbol.FirstCandidate;
                }
                else
                {
                    var missingCanonicalTypeSymbol = symbol as NoPiaMissingCanonicalTypeSymbol;

                    if ((object)missingCanonicalTypeSymbol != null)
                    {
                        symbolName = missingCanonicalTypeSymbol.FullTypeName;
                    }
                }
            }

            var partKind = GetPartKind(symbol);

            if (symbolName == null)
            {
                symbolName = symbol.Name;
            }

            if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName) &&
                partKind == SymbolDisplayPartKind.ErrorTypeName &&
                string.IsNullOrEmpty(symbolName))
            {
                builder.Add(CreatePart(partKind, symbol, "?"));
            }
            else
            {
                symbolName = RemoveAttributeSufficeIfNecessary(symbol, symbolName);
                builder.Add(CreatePart(partKind, symbol, symbolName));
            }

            if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes))
            {
                // Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
                if (((NamedTypeSymbol)symbol).MangleName)
                {
                    Debug.Assert(symbol.Arity > 0);
                    builder.Add(CreatePart(InternalSymbolDisplayPartKind.Arity, null,
                        MetadataHelpers.GetAritySuffix(symbol.Arity)));
                }
            }
            else if (symbol.Arity > 0 && format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters))
            {
                // It would be nice to handle VB symbols too, but it's not worth the effort.
                if (symbol is UnsupportedMetadataTypeSymbol || symbol is MissingMetadataTypeSymbol || symbol.IsUnboundGenericType)
                {
                    AddPunctuation(SyntaxKind.LessThanToken);
                    for (int i = 0; i < symbol.Arity - 1; i++)
                    {
                        AddPunctuation(SyntaxKind.CommaToken);
                    }

                    AddPunctuation(SyntaxKind.GreaterThanToken);
                }
                else
                {
                    var modifiers = default(ImmutableArray<ImmutableArray<CustomModifier>>);

                    if (this.format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers))
                    {
                        var namedType = symbol as NamedTypeSymbol;
                        if ((object)namedType != null)
                        {
                            modifiers = namedType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(a => a.CustomModifiers);
                        }
                    }

                    AddTypeArguments(symbol, modifiers);

                    AddDelegateParameters(symbol);

                    // TODO: do we want to skip these if we're being visited as a containing type?
                    AddTypeParameterConstraints(symbol.TypeArguments);
                }
            }
            else
            {
                AddDelegateParameters(symbol);
            }

            // Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
            if (symbol.OriginalDefinition is MissingMetadataTypeSymbol &&
                format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes))
            {
                //add it as punctuation - it's just for testing
                AddPunctuation(SyntaxKind.OpenBracketToken);
                builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, symbol, "missing"));
                AddPunctuation(SyntaxKind.CloseBracketToken);
            }
        }

        private void AddDelegateParameters(INamedTypeSymbol symbol)
        {
            if (CanShowDelegateSignature(symbol))
            {
                if (format.DelegateStyle == SymbolDisplayDelegateStyle.NameAndParameters ||
                    format.DelegateStyle == SymbolDisplayDelegateStyle.NameAndSignature)
                {
                    var method = symbol.DelegateInvokeMethod;
                    AddPunctuation(SyntaxKind.OpenParenToken);
                    AddParametersIfRequired(hasThisParameter: false, isVarargs: method.IsVararg, parameters: method.Parameters);
                    AddPunctuation(SyntaxKind.CloseParenToken);
                }
            }
        }

        private void AddAnonymousTypeName(INamedTypeSymbol symbol)
        {
            // TODO: revise to generate user-friendly name 
            var members = string.Join(", ", symbol.GetMembers().OfType<IPropertySymbol>().Select(CreateAnonymousTypeMember));

            if (members.Length == 0)
            {
                builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, "<empty anonymous type>"));
            }
            else
            {
                var name = $"<anonymous type: {members}>";
                builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.ClassName, symbol, name));
            }
        }

        /// <summary>
        /// Returns true if tuple type syntax can be used to refer to the tuple type without loss of information.
        /// For example, it cannot be used when extension tuple is using non-default friendly names. 
        /// </summary>
        /// <param name="tupleSymbol"></param>
        /// <returns></returns>
        private bool CanUseTupleTypeName(INamedTypeSymbol tupleSymbol)
        {
            INamedTypeSymbol currentUnderlying = tupleSymbol.TupleUnderlyingType;

            if (currentUnderlying.Arity == 1)
            {
                return false;
            }

            while (currentUnderlying.Arity == TupleTypeSymbol.RestPosition)
            {
                tupleSymbol = (INamedTypeSymbol)currentUnderlying.TypeArguments[TupleTypeSymbol.RestPosition - 1];
                Debug.Assert(tupleSymbol.IsTupleType);

                if (HasNonDefaultTupleElements(tupleSymbol))
                {
                    return false;
                }

                currentUnderlying = tupleSymbol.TupleUnderlyingType;
            }

            return true;
        }

        private static bool HasNonDefaultTupleElements(INamedTypeSymbol tupleSymbol)
        {
            return tupleSymbol.TupleElements.Any(e => !e.IsDefaultTupleElement());
        }

        private void AddTupleTypeName(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsTupleType);

            ImmutableArray<IFieldSymbol> elements = symbol.TupleElements;

            AddPunctuation(SyntaxKind.OpenParenToken);
            for (int i = 0; i < elements.Length; i++)
            {
                var element = elements[i];

                if (i != 0)
                {
                    AddPunctuation(SyntaxKind.CommaToken);
                    AddSpace();
                }

                VisitFieldType(element);
                if (!element.IsImplicitlyDeclared)
                {
                    AddSpace();
                    builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, symbol, element.Name));
                }
            }

            AddPunctuation(SyntaxKind.CloseParenToken);
        }

        private string CreateAnonymousTypeMember(IPropertySymbol property)
        {
            return property.Type.ToDisplayString(format) + " " + property.Name;
        }

        private bool CanShowDelegateSignature(INamedTypeSymbol symbol)
        {
            return
                isFirstSymbolVisited &&
                symbol.TypeKind == TypeKind.Delegate &&
                format.DelegateStyle != SymbolDisplayDelegateStyle.NameOnly &&
                symbol.DelegateInvokeMethod != null;
        }

        private static SymbolDisplayPartKind GetPartKind(INamedTypeSymbol symbol)
        {
            switch (symbol.TypeKind)
            {
                case TypeKind.Submission:
                case TypeKind.Module:
                case TypeKind.Class:
                    return SymbolDisplayPartKind.ClassName;
                case TypeKind.Delegate:
                    return SymbolDisplayPartKind.DelegateName;
                case TypeKind.Enum:
                    return SymbolDisplayPartKind.EnumName;
                case TypeKind.Error:
                    return SymbolDisplayPartKind.ErrorTypeName;
                case TypeKind.Interface:
                    return SymbolDisplayPartKind.InterfaceName;
                case TypeKind.Struct:
                    return SymbolDisplayPartKind.StructName;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.TypeKind);
            }
        }

        private bool AddSpecialTypeKeyword(INamedTypeSymbol symbol)
        {
            var specialTypeName = GetSpecialTypeName(symbol.SpecialType);
            if (specialTypeName == null)
            {
                return false;
            }

            // cheat - skip escapeKeywordIdentifiers. not calling AddKeyword because someone
            // else is working out the text for us
            builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, specialTypeName));
            return true;
        }

        private static string GetSpecialTypeName(SpecialType specialType)
        {
            switch (specialType)
            {
                case SpecialType.System_Void:
                    return "void";
                case SpecialType.System_SByte:
                    return "sbyte";
                case SpecialType.System_Int16:
                    return "short";
                case SpecialType.System_Int32:
                    return "int";
                case SpecialType.System_Int64:
                    return "long";
                case SpecialType.System_Byte:
                    return "byte";
                case SpecialType.System_UInt16:
                    return "ushort";
                case SpecialType.System_UInt32:
                    return "uint";
                case SpecialType.System_UInt64:
                    return "ulong";
                case SpecialType.System_Single:
                    return "float";
                case SpecialType.System_Double:
                    return "double";
                case SpecialType.System_Decimal:
                    return "decimal";
                case SpecialType.System_Char:
                    return "char";
                case SpecialType.System_Boolean:
                    return "bool";
                case SpecialType.System_String:
                    return "string";
                case SpecialType.System_Object:
                    return "object";
                default:
                    return null;
            }
        }

        private void AddTypeKind(INamedTypeSymbol symbol)
        {
            if (isFirstSymbolVisited && format.KindOptions.IncludesOption(SymbolDisplayKindOptions.IncludeTypeKeyword))
            {
                if (symbol.IsAnonymousType)
                {
                    builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, null, "AnonymousType"));
                    AddSpace();
                }
                else if (symbol.IsTupleType)
                {
                    builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, null, "Tuple"));
                    AddSpace();
                }
                else
                {
                    switch (symbol.TypeKind)
                    {
                        case TypeKind.Module:
                        case TypeKind.Class:
                            AddKeyword(SyntaxKind.ClassKeyword);
                            AddSpace();
                            break;

                        case TypeKind.Enum:
                            AddKeyword(SyntaxKind.EnumKeyword);
                            AddSpace();
                            break;

                        case TypeKind.Delegate:
                            AddKeyword(SyntaxKind.DelegateKeyword);
                            AddSpace();
                            break;

                        case TypeKind.Interface:
                            AddKeyword(SyntaxKind.InterfaceKeyword);
                            AddSpace();
                            break;

                        case TypeKind.Struct:
                            if (symbol is NamedTypeSymbol csharpType)
                            {
                                if (csharpType.IsReadOnly)
                                {
                                    AddKeyword(SyntaxKind.ReadOnlyKeyword);
                                    AddSpace();
                                }

                                if (csharpType.IsRefLikeType)
                                {
                                    AddKeyword(SyntaxKind.RefKeyword);
                                    AddSpace();
                                }
                            }

                            AddKeyword(SyntaxKind.StructKeyword);
                            AddSpace();
                            break;
                    }
                }
            }
        }

        private void AddTypeParameterVarianceIfRequired(ITypeParameterSymbol symbol)
        {
            if (format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeVariance))
            {
                switch (symbol.Variance)
                {
                    case VarianceKind.In:
                        AddKeyword(SyntaxKind.InKeyword);
                        AddSpace();
                        break;
                    case VarianceKind.Out:
                        AddKeyword(SyntaxKind.OutKeyword);
                        AddSpace();
                        break;
                }
            }
        }

        //returns true if there are constraints
        private void AddTypeArguments(ISymbol owner, ImmutableArray<ImmutableArray<CustomModifier>> modifiers)
        {
            ImmutableArray<ITypeSymbol> typeArguments;
            ImmutableArray<TypeWithAnnotations>? typeArgumentsWithAnnotations;

            if (owner.Kind == SymbolKind.Method)
            {
                typeArguments = ((IMethodSymbol)owner).TypeArguments;
                typeArgumentsWithAnnotations = (owner as MethodSymbol)?.TypeArgumentsWithAnnotations;
            }
            else
            {
                typeArguments = ((INamedTypeSymbol)owner).TypeArguments;
                typeArgumentsWithAnnotations = (owner as NamedTypeSymbol)?.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics;
            }

            if (typeArguments.Length > 0 && format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters))
            {
                AddPunctuation(SyntaxKind.LessThanToken);

                var first = true;
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    var typeArg = typeArguments[i];

                    if (!first)
                    {
                        AddPunctuation(SyntaxKind.CommaToken);
                        AddSpace();
                    }
                    first = false;

                    AbstractSymbolDisplayVisitor visitor;

                    if (typeArg.Kind == SymbolKind.TypeParameter)
                    {
                        var typeParam = (ITypeParameterSymbol)typeArg;

                        AddTypeParameterVarianceIfRequired(typeParam);

                        visitor = this.NotFirstVisitor;
                    }
                    else
                    {
                        visitor = this.NotFirstVisitorNamespaceOrType;
                    }

                    if (typeArgumentsWithAnnotations == null)
                    {
                        typeArg.Accept(visitor);
                    }
                    else
                    {
                        VisitTypeWithAnnotations(typeArgumentsWithAnnotations.GetValueOrDefault()[i], visitor);
                    }

                    if (!modifiers.IsDefault)
                    {
                        AddCustomModifiersIfRequired(modifiers[i], leadingSpace: true, trailingSpace: false);
                    }
                }

                AddPunctuation(SyntaxKind.GreaterThanToken);
            }
        }

        private static bool TypeParameterHasConstraints(ITypeParameterSymbol typeParam)
        {
            return !typeParam.ConstraintTypes.IsEmpty || typeParam.HasConstructorConstraint ||
                typeParam.HasReferenceTypeConstraint || typeParam.HasValueTypeConstraint ||
                typeParam.HasNotNullConstraint;
        }

        private void AddTypeParameterConstraints(ImmutableArray<ITypeSymbol> typeArguments)
        {
            if (this.isFirstSymbolVisited && format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeConstraints))
            {
                foreach (var typeArg in typeArguments)
                {
                    if (typeArg.Kind == SymbolKind.TypeParameter)
                    {
                        var typeParam = (ITypeParameterSymbol)typeArg;

                        if (TypeParameterHasConstraints(typeParam))
                        {
                            AddSpace();
                            AddKeyword(SyntaxKind.WhereKeyword);
                            AddSpace();

                            typeParam.Accept(this.NotFirstVisitor);

                            AddSpace();
                            AddPunctuation(SyntaxKind.ColonToken);
                            AddSpace();

                            bool needComma = false;
                            var typeParameterSymbol = typeParam as TypeParameterSymbol;

                            //class/struct constraint must be first
                            if (typeParam.HasReferenceTypeConstraint)
                            {
                                AddKeyword(SyntaxKind.ClassKeyword);

                                switch (typeParameterSymbol?.ReferenceTypeConstraintIsNullable) // https://github.com/dotnet/roslyn/issues/26198 Switch to public API when we will have one.
                                {
                                    case true:
                                        if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))
                                        {
                                            AddPunctuation(SyntaxKind.QuestionToken);
                                        }
                                        break;

                                    case false:
                                        if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier))
                                        {
                                            AddPunctuation(SyntaxKind.ExclamationToken);
                                        }
                                        break;
                                }

                                needComma = true;
                            }
                            else if (typeParam.HasUnmanagedTypeConstraint)
                            {
                                builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "unmanaged"));
                                needComma = true;
                            }
                            else if (typeParam.HasValueTypeConstraint)
                            {
                                AddKeyword(SyntaxKind.StructKeyword);
                                needComma = true;
                            }
                            else if (typeParam.HasNotNullConstraint)
                            {
                                builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "notnull"));
                                needComma = true;
                            }

                            ImmutableArray<TypeWithAnnotations>? annotatedConstraints = typeParameterSymbol?.ConstraintTypesNoUseSiteDiagnostics; // https://github.com/dotnet/roslyn/issues/26198 Switch to public API when we will have one.

                            for (int i = 0; i < typeParam.ConstraintTypes.Length; i++)
                            {
                                ITypeSymbol baseType = typeParam.ConstraintTypes[i];
                                if (needComma)
                                {
                                    AddPunctuation(SyntaxKind.CommaToken);
                                    AddSpace();
                                }

                                if (annotatedConstraints.HasValue)
                                {
                                    VisitTypeWithAnnotations(annotatedConstraints.GetValueOrDefault()[i], this.NotFirstVisitor);
                                }
                                else
                                {
                                    baseType.Accept(this.NotFirstVisitor);
                                }

                                needComma = true;
                            }

                            //ctor constraint must be last
                            if (typeParam.HasConstructorConstraint)
                            {
                                if (needComma)
                                {
                                    AddPunctuation(SyntaxKind.CommaToken);
                                    AddSpace();
                                }

                                AddKeyword(SyntaxKind.NewKeyword);
                                AddPunctuation(SyntaxKind.OpenParenToken);
                                AddPunctuation(SyntaxKind.CloseParenToken);
                            }
                        }
                    }
                }
            }
        }
    }
}
