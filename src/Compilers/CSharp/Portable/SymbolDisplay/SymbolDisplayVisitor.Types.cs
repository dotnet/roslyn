// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SymbolDisplay;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        public override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            VisitArrayTypeWithoutNullability(symbol);
            AddNullableAnnotations(symbol);
        }

        private void VisitArrayTypeWithoutNullability(IArrayTypeSymbol symbol)
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

            ITypeSymbol underlyingType = symbol;
            do
            {
                underlyingType = ((IArrayTypeSymbol)underlyingType).ElementType;
            }
            while (underlyingType.Kind == SymbolKind.ArrayType && !ShouldAddNullableAnnotation(underlyingType));

            underlyingType.Accept(this.NotFirstVisitor);

            var arrayType = symbol;
            while (arrayType != null && arrayType != underlyingType)
            {
                if (!this.isFirstSymbolVisited)
                {
                    AddCustomModifiersIfNeeded(arrayType.CustomModifiers, leadingSpace: true);
                }

                AddArrayRank(arrayType);
                arrayType = arrayType.ElementType as IArrayTypeSymbol;
            }
        }

        private void AddNullableAnnotations(ITypeSymbol type)
        {
            if (ShouldAddNullableAnnotation(type))
            {
                AddPunctuation(type.NullableAnnotation == CodeAnalysis.NullableAnnotation.Annotated ? SyntaxKind.QuestionToken : SyntaxKind.ExclamationToken);
            }
        }

        private bool ShouldAddNullableAnnotation(ITypeSymbol type)
        {
            switch (type.NullableAnnotation)
            {
                case CodeAnalysis.NullableAnnotation.Annotated:
                    if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier) &&
                        !ITypeSymbolHelpers.IsNullableType(type) && !type.IsValueType)
                    {
                        return true;
                    }
                    break;

                case CodeAnalysis.NullableAnnotation.NotAnnotated:
                    if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier) &&
                        !type.IsValueType &&
                        (type as Symbols.PublicModel.TypeSymbol)?.UnderlyingTypeSymbol.IsTypeParameterDisallowingAnnotationInCSharp8() != true)
                    {
                        return true;
                    }
                    break;
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
                if (!symbol.IsSZArray)
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
            symbol.PointedAtType.Accept(this.NotFirstVisitor);

            AddNullableAnnotations(symbol);

            if (!this.isFirstSymbolVisited)
            {
                AddCustomModifiersIfNeeded(symbol.CustomModifiers, leadingSpace: true);
            }
            AddPunctuation(SyntaxKind.AsteriskToken);
        }

        public override void VisitFunctionPointerType(IFunctionPointerTypeSymbol symbol)
        {
            VisitMethod(symbol.Signature);
        }

        public override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            if (this.isFirstSymbolVisited)
            {
                AddTypeParameterVarianceIfNeeded(symbol);
            }

            //variance and constraints are handled by methods and named types
            builder.Add(CreatePart(SymbolDisplayPartKind.TypeParameterName, symbol, symbol.Name));

            AddNullableAnnotations(symbol);
        }

        public override void VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, symbol.Name));

            AddNullableAnnotations(symbol);
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            if ((format.CompilerInternalOptions & SymbolDisplayCompilerInternalOptions.IncludeFileLocalTypesPrefix) != 0
                && symbol is Symbols.PublicModel.Symbol { UnderlyingSymbol: NamedTypeSymbol { } internalSymbol1 }
                && internalSymbol1.GetFileLocalTypeMetadataNamePrefix() is { } fileLocalNamePrefix)
            {
                builder.Add(CreatePart(SymbolDisplayPartKind.ModuleName, symbol, fileLocalNamePrefix));
            }

            VisitNamedTypeWithoutNullability(symbol);
            AddNullableAnnotations(symbol);

            if ((format.CompilerInternalOptions & SymbolDisplayCompilerInternalOptions.IncludeContainingFileForFileTypes) != 0
                && symbol is Symbols.PublicModel.Symbol { UnderlyingSymbol: NamedTypeSymbol { AssociatedFileIdentifier: { } identifier } internalSymbol2 })
            {
                var fileDescription = identifier.DisplayFilePath is { Length: not 0 } path ? path
                    : internalSymbol2.GetFirstLocationOrNone().SourceTree is { } tree ? $"<tree {internalSymbol2.DeclaringCompilation.GetSyntaxTreeOrdinal(tree)}>"
                    : "<unknown>";

                builder.Add(CreatePart(SymbolDisplayPartKind.Punctuation, symbol, "@"));
                builder.Add(CreatePart(SymbolDisplayPartKind.ModuleName, symbol, fileDescription));
            }
        }

        private void VisitNamedTypeWithoutNullability(INamedTypeSymbol symbol)
        {
            if (this.IsMinimizing && TryAddAlias(symbol, builder))
            {
                return;
            }

            if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseSpecialTypes) ||
                (symbol.IsNativeIntegerType && !format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType)))
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
                        AddCustomModifiersIfNeeded(symbol.GetTypeArgumentCustomModifiers(0), leadingSpace: true, trailingSpace: false);

                        AddPunctuation(SyntaxKind.QuestionToken);

                        //visiting the underlying type did all of the work for us
                        return;
                    }
                }
            }

            if (this.IsMinimizing || (symbol.IsTupleType && !ShouldDisplayAsValueTuple(symbol)))
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
                        AddRefIfNeeded();
                    }
                    else if (invokeMethod.ReturnsByRefReadonly)
                    {
                        AddRefReadonlyIfNeeded();
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

                    if (format.CompilerInternalOptions.HasFlag(SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes))
                    {
                        AddPunctuation(SyntaxKind.PlusToken);
                    }
                    else
                    {
                        AddPunctuation(SyntaxKind.DotToken);
                    }
                }
            }

            AddNameAndTypeArgumentsOrParameters(symbol);
        }

        private bool ShouldDisplayAsValueTuple(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsTupleType);

            if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.ExpandValueTuple))
            {
                return true;
            }

            return !CanUseTupleSyntax(symbol);
        }

        private void AddNameAndTypeArgumentsOrParameters(INamedTypeSymbol symbol)
        {
            if (symbol.IsAnonymousType && symbol.TypeKind != TypeKind.Delegate)
            {
                AddAnonymousTypeName(symbol);
                return;
            }
            else if (symbol.IsTupleType && !ShouldDisplayAsValueTuple(symbol))
            {
                AddTupleTypeName(symbol);
                return;
            }

            string symbolName = null;

            // It would be nice to handle VB NoPia symbols too, but it's not worth the effort.

            NamedTypeSymbol underlyingTypeSymbol = (symbol as Symbols.PublicModel.NamedTypeSymbol)?.UnderlyingNamedTypeSymbol;
            var illegalGenericInstantiationSymbol = underlyingTypeSymbol as NoPiaIllegalGenericInstantiationSymbol;

            if (illegalGenericInstantiationSymbol is not null)
            {
                symbol = illegalGenericInstantiationSymbol.UnderlyingSymbol.GetPublicSymbol();
            }
            else
            {
                var ambiguousCanonicalTypeSymbol = underlyingTypeSymbol as NoPiaAmbiguousCanonicalTypeSymbol;
                if (ambiguousCanonicalTypeSymbol is not null)
                {
                    symbol = ambiguousCanonicalTypeSymbol.FirstCandidate.GetPublicSymbol();
                }
                else
                {
                    var missingCanonicalTypeSymbol = underlyingTypeSymbol as NoPiaMissingCanonicalTypeSymbol;

                    if (missingCanonicalTypeSymbol is not null)
                    {
                        symbolName = missingCanonicalTypeSymbol.FullTypeName;
                    }
                }
            }

            if (symbolName is null && symbol.IsAnonymousType && symbol.TypeKind == TypeKind.Delegate)
            {
                symbolName = "<anonymous delegate>";
            }

            var partKind = GetPartKind(symbol);

            symbolName ??= symbol.Name;

            if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName) &&
                partKind == SymbolDisplayPartKind.ErrorTypeName &&
                string.IsNullOrEmpty(symbolName))
            {
                builder.Add(CreatePart(partKind, symbol, "?"));
            }
            else
            {
                symbolName = RemoveAttributeSuffixIfNecessary(symbol, symbolName);
                builder.Add(CreatePart(partKind, symbol, symbolName));
            }

            if (format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes))
            {
                // Only the compiler can set the internal option and the compiler doesn't use other implementations of INamedTypeSymbol.
                if (underlyingTypeSymbol?.MangleName == true)
                {
                    Debug.Assert(symbol.Arity > 0);
                    builder.Add(CreatePart(InternalSymbolDisplayPartKind.Arity, null,
                        MetadataHelpers.GetAritySuffix(symbol.Arity)));
                }
            }
            else if (symbol.Arity > 0 && format.GenericsOptions.IncludesOption(SymbolDisplayGenericsOptions.IncludeTypeParameters))
            {
                // It would be nice to handle VB symbols too, but it's not worth the effort.
                if (underlyingTypeSymbol is UnsupportedMetadataTypeSymbol || underlyingTypeSymbol is MissingMetadataTypeSymbol || symbol.IsUnboundGenericType)
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
                    AddTypeArguments(symbol, GetTypeArgumentsModifiers(underlyingTypeSymbol));
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
            if (underlyingTypeSymbol?.OriginalDefinition is MissingMetadataTypeSymbol &&
                format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes))
            {
                //add it as punctuation - it's just for testing
                AddPunctuation(SyntaxKind.OpenBracketToken);
                builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, symbol, "missing"));
                AddPunctuation(SyntaxKind.CloseBracketToken);
            }
        }

        private ImmutableArray<ImmutableArray<CustomModifier>> GetTypeArgumentsModifiers(NamedTypeSymbol underlyingTypeSymbol)
        {
            if (this.format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers))
            {
                if ((object)underlyingTypeSymbol != null)
                {
                    return underlyingTypeSymbol.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.SelectAsArray(a => a.CustomModifiers);
                }
            }

            return default;
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
                    AddParametersIfNeeded(hasThisParameter: false, isVarargs: method.IsVararg, parameters: method.Parameters);
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
        private bool CanUseTupleSyntax(INamedTypeSymbol tupleSymbol)
        {
            if (containsModopt(tupleSymbol))
            {
                return false;
            }

            INamedTypeSymbol currentUnderlying = GetTupleUnderlyingTypeOrSelf(tupleSymbol);
            if (currentUnderlying.Arity <= 1)
            {
                return false;
            }

            while (currentUnderlying.Arity == NamedTypeSymbol.ValueTupleRestPosition)
            {
                tupleSymbol = (INamedTypeSymbol)currentUnderlying.TypeArguments[NamedTypeSymbol.ValueTupleRestPosition - 1];
                Debug.Assert(tupleSymbol.IsTupleType);

                if (tupleSymbol.TypeKind == TypeKind.Error ||
                    HasNonDefaultTupleElements(tupleSymbol) ||
                    containsModopt(tupleSymbol))
                {
                    return false;
                }

                currentUnderlying = GetTupleUnderlyingTypeOrSelf(tupleSymbol);
            }

            return true;

            bool containsModopt(INamedTypeSymbol symbol)
            {
                NamedTypeSymbol underlyingTypeSymbol = (symbol as Symbols.PublicModel.NamedTypeSymbol)?.UnderlyingNamedTypeSymbol;
                ImmutableArray<ImmutableArray<CustomModifier>> modifiers = GetTypeArgumentsModifiers(underlyingTypeSymbol);
                if (modifiers.IsDefault)
                {
                    return false;
                }

                return modifiers.Any(static m => !m.IsEmpty);
            }
        }

        private static INamedTypeSymbol GetTupleUnderlyingTypeOrSelf(INamedTypeSymbol type)
        {
            return type.TupleUnderlyingType ?? type;
        }

        private static bool HasNonDefaultTupleElements(INamedTypeSymbol tupleSymbol)
        {
            return tupleSymbol.TupleElements.Any(static e => !e.IsDefaultTupleElement());
        }

        private void AddTupleTypeName(INamedTypeSymbol symbol)
        {
            Debug.Assert(symbol.IsTupleType);

            if (this.format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.CollapseTupleTypes))
            {
                builder.Add(CreatePart(SymbolDisplayPartKind.StructName, symbol, "<tuple>"));
                return;
            }

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
                if (element.IsExplicitlyNamedTupleElement)
                {
                    AddSpace();
                    builder.Add(CreatePart(SymbolDisplayPartKind.FieldName, element, element.Name));
                }
            }

            AddPunctuation(SyntaxKind.CloseParenToken);

            if (symbol.TypeKind == TypeKind.Error &&
                format.CompilerInternalOptions.IncludesOption(SymbolDisplayCompilerInternalOptions.FlagMissingMetadataTypes))
            {
                //add it as punctuation - it's just for testing
                AddPunctuation(SyntaxKind.OpenBracketToken);
                builder.Add(CreatePart(InternalSymbolDisplayPartKind.Other, symbol, "missing"));
                AddPunctuation(SyntaxKind.CloseBracketToken);
            }
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
                case TypeKind.Class when symbol.IsRecord:
                    return SymbolDisplayPartKind.RecordClassName;
                case TypeKind.Struct when symbol.IsRecord:
                    return SymbolDisplayPartKind.RecordStructName;
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
            var specialTypeName = GetSpecialTypeName(symbol);
            if (specialTypeName == null)
            {
                return false;
            }

            // cheat - skip escapeKeywordIdentifiers. not calling AddKeyword because someone
            // else is working out the text for us
            builder.Add(CreatePart(SymbolDisplayPartKind.Keyword, symbol, specialTypeName));
            return true;
        }

        private static string GetSpecialTypeName(INamedTypeSymbol symbol)
        {
            switch (symbol.SpecialType)
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
                case SpecialType.System_IntPtr when symbol.IsNativeIntegerType:
                    return "nint";
                case SpecialType.System_UIntPtr when symbol.IsNativeIntegerType:
                    return "nuint";
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
                if (symbol.IsAnonymousType && symbol.TypeKind != TypeKind.Delegate)
                {
                    builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, null, "AnonymousType"));
                    AddSpace();
                }
                else if (symbol.IsTupleType && !ShouldDisplayAsValueTuple(symbol))
                {
                    builder.Add(new SymbolDisplayPart(SymbolDisplayPartKind.AnonymousTypeIndicator, null, "Tuple"));
                    AddSpace();
                }
                else
                {
                    switch (symbol.TypeKind)
                    {
                        case TypeKind.Class when symbol.IsRecord:
                            AddKeyword(SyntaxKind.RecordKeyword);
                            AddSpace();
                            break;

                        case TypeKind.Struct when symbol.IsRecord:
                            // In case ref record structs are allowed in future, call AddKeyword(SyntaxKind.RefKeyword) and remove assertion.
                            Debug.Assert(!symbol.IsRefLikeType);

                            if (symbol.IsReadOnly)
                            {
                                AddKeyword(SyntaxKind.ReadOnlyKeyword);
                                AddSpace();
                            }

                            AddKeyword(SyntaxKind.RecordKeyword);
                            AddSpace();
                            AddKeyword(SyntaxKind.StructKeyword);
                            AddSpace();
                            break;

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
                            if (symbol.IsReadOnly)
                            {
                                AddKeyword(SyntaxKind.ReadOnlyKeyword);
                                AddSpace();
                            }

                            if (symbol.IsRefLikeType)
                            {
                                AddKeyword(SyntaxKind.RefKeyword);
                                AddSpace();
                            }

                            AddKeyword(SyntaxKind.StructKeyword);
                            AddSpace();
                            break;
                    }
                }
            }
        }

        private void AddTypeParameterVarianceIfNeeded(ITypeParameterSymbol symbol)
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

            if (owner.Kind == SymbolKind.Method)
            {
                typeArguments = ((IMethodSymbol)owner).TypeArguments;
            }
            else
            {
                typeArguments = ((INamedTypeSymbol)owner).TypeArguments;
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

                        AddTypeParameterVarianceIfNeeded(typeParam);

                        visitor = this.NotFirstVisitor;
                    }
                    else
                    {
                        visitor = this.NotFirstVisitorNamespaceOrType;
                    }

                    typeArg.Accept(visitor);

                    if (!modifiers.IsDefault)
                    {
                        AddCustomModifiersIfNeeded(modifiers[i], leadingSpace: true, trailingSpace: false);
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

                            //class/struct constraint must be first
                            if (typeParam.HasReferenceTypeConstraint)
                            {
                                AddKeyword(SyntaxKind.ClassKeyword);

                                switch (typeParam.ReferenceTypeConstraintNullableAnnotation)
                                {
                                    case CodeAnalysis.NullableAnnotation.Annotated:
                                        if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier))
                                        {
                                            AddPunctuation(SyntaxKind.QuestionToken);
                                        }
                                        break;

                                    case CodeAnalysis.NullableAnnotation.NotAnnotated:
                                        if (format.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier))
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

                            for (int i = 0; i < typeParam.ConstraintTypes.Length; i++)
                            {
                                ITypeSymbol baseType = typeParam.ConstraintTypes[i];
                                if (needComma)
                                {
                                    AddPunctuation(SyntaxKind.CommaToken);
                                    AddSpace();
                                }

                                baseType.Accept(this.NotFirstVisitor);
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
