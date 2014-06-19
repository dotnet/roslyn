using System;

namespace Roslyn.Compilers.CSharp.Descriptions
{
    internal partial class SymbolDescriptionVisitor
    {
        protected internal override object VisitType(TypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            switch (symbol.TypeKind)
            {
                case TypeKind.ArrayType:
                    return VisitArrayType((ArrayTypeSymbol)symbol, builder);
                case TypeKind.PointerType:
                    return VisitPointerType((PointerTypeSymbol)symbol, builder);
                case TypeKind.TypeParameter:
                    return VisitTypeParameter((TypeParameterSymbol)symbol, builder);
                case TypeKind.DynamicType:
                    return VisitDynamicType((DynamicTypeSymbol)symbol, builder);
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Enum:
                case TypeKind.Delegate:
                    return VisitNamedType((NamedTypeSymbol)symbol, builder);
                case TypeKind.Error:
                    throw new NotImplementedException(); //TODO
                default:
                    throw new ArgumentException("symbol", string.Format("Unknown symbol type kind '{0}'", symbol.TypeKind));
            }
        }

        protected internal override object VisitArrayType(ArrayTypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            //See spec section 12.1 for the order of rank specificiers
            //e.g. int[][,][,,] is stored as 
            //     ArrayType
            //         Rank = 1
            //         ElementType = ArrayType
            //             Rank = 2
            //             ElementType = ArrayType
            //                 Rank = 3
            //                 ElementType = int

            TypeSymbol underlyingNonArrayType = symbol.ElementType;
            while (underlyingNonArrayType.TypeKind == TypeKind.ArrayType)
            {
                underlyingNonArrayType = ((ArrayTypeSymbol)underlyingNonArrayType).ElementType;
            }

            VisitType(underlyingNonArrayType, builder);

            ArrayTypeSymbol arrayType = symbol;
            while (arrayType != null)
            {
                AddArrayRank(arrayType, builder);
                arrayType = arrayType.ElementType as ArrayTypeSymbol;
            }

            return null;
        }

        private static void AddArrayRank(ArrayTypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            AddPunctuation(SyntaxKind.OpenBracketToken, builder);
            for (int i = 0; i < symbol.Rank - 1; i++)
            {
                AddPunctuation(SyntaxKind.CommaToken, builder);
            }
            AddPunctuation(SyntaxKind.CloseBracketToken, builder);
        }

        protected internal override object VisitPointerType(PointerTypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            VisitType(symbol.PointedAtType, builder);
            AddPunctuation(SyntaxKind.AsteriskToken, builder);

            return null;
        }

        protected internal override object VisitTypeParameter(TypeParameterSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            //variance and constraints are handled by methods and named types
            builder.Add(new SymbolDescriptionPart
            {
                Kind = SymbolDescriptionPartKind.TypeParameterName,
                Text = symbol.Name,
            });

            return null;
        }

        internal override object VisitDynamicType(DynamicTypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            AddKeyword(SyntaxKind.DynamicKeyword, builder);

            return null;
        }

        protected internal override object VisitNamedType(NamedTypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.PrettyPrintingFlags.HasFlag(PrettyPrintingFlags.UseSpecialTypeKeywords) && AddSpecialTypeKeyword(symbol, builder))
            {
                //if we're using special type keywords and this is a special type, then no other work is required
                return null;
            }

            //if we're expanding nullable, we just visit nullable types normally
            if (format.PrettyPrintingFlags.HasFlag(PrettyPrintingFlags.AbbreviateNullable) &&
                symbol.IsNullableType() && !ReferenceEquals(symbol, symbol.OriginalDefinition))
            {
                VisitType(symbol.GetNullableUnderlyingType(), builder);
                AddPunctuation(SyntaxKind.QuestionToken, builder);

                //visiting the underlying type did all of the work for us
                return null;
            }

            //TODO: shortest unambiguous
            //TODO: aliases
            
            //only visit the namespace if the style requires it and there isn't an enclosing type
            if (format.TypeQualificationStyle == QualificationStyle.NameAndContainingTypesAndNamespaces &&
                symbol.ContainingSymbol != null && symbol.ContainingSymbol.Kind == SymbolKind.Namespace)
            {
                VisitNamespace(symbol.ContainingNamespace, builder);
                AddPunctuation(SyntaxKind.DotToken, builder);
            }

            //visit the enclosing type if the style requires it
            if (format.TypeQualificationStyle == QualificationStyle.NameAndContainingTypes || 
                format.TypeQualificationStyle == QualificationStyle.NameAndContainingTypesAndNamespaces)
            {
                if (symbol.ContainingType != null)
                {
                    VisitType(symbol.ContainingType, builder);
                    AddPunctuation(SyntaxKind.DotToken, builder);
                }
            }

            SymbolDescriptionPartKind partKind;

            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                    partKind = SymbolDescriptionPartKind.ClassName;
                    break;
                case TypeKind.Delegate:
                    partKind = SymbolDescriptionPartKind.DelegateName;
                    break;
                case TypeKind.Enum:
                    partKind = SymbolDescriptionPartKind.EnumName;
                    break;
                case TypeKind.Interface:
                    partKind = SymbolDescriptionPartKind.InterfaceName;
                    break;
                case TypeKind.Struct:
                    partKind = SymbolDescriptionPartKind.StructureName;
                    break;
                default:
                    partKind = SymbolDescriptionPartKind.Identifier;
                    break;
            }

            builder.Add(new SymbolDescriptionPart
            {
                Kind = partKind,
                Text = symbol.Name,
            });

            if (symbol.Arity > 0 && format.GenericsFlags.HasFlag(GenericsFlags.IncludeTypeParameters))
            {
                bool hasContraints = AddTypeArguments(symbol.TypeArguments, builder);
                if (hasContraints && format.GenericsFlags.HasFlag(GenericsFlags.IncludeTypeConstraints))
                {
                    //TODO: do we want to skip these if we're being visited as a containing type?
                    AddTypeParameterConstraints(symbol.TypeArguments, builder);
                }
            }

            return null;
        }

        private static bool AddSpecialTypeKeyword(TypeSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            var specialType = symbol.GetSpecialTypeSafe();
            switch (specialType)
            {
                case SpecialType.System_Void:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Byte:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_Char:
                case SpecialType.System_Boolean:
                case SpecialType.System_String:
                case SpecialType.System_Object:
                    //not calling AddKeyword because someone else is working out the text for us
                    builder.Add(new SymbolDescriptionPart
                    {
                        Kind = SymbolDescriptionPartKind.Keyword,
                        Text = SemanticFacts.GetLanguageName(specialType),
                    });
                    return true;
                default:
                    return false;
            }
        }

        private void AddTypeParameterVarianceIfRequired(TypeParameterSymbol symbol, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            if (format.GenericsFlags.HasFlag(GenericsFlags.IncludeVariance))
            {
                switch (symbol.Variance)
                {
                    case VarianceKind.VarianceIn:
                        AddKeyword(SyntaxKind.InKeyword, builder);
                        AddSpace(builder);
                        break;
                    case VarianceKind.VarianceOut:
                        AddKeyword(SyntaxKind.OutKeyword, builder);
                        AddSpace(builder);
                        break;
                }
            }
        }

        //returns true if there are constraints
        private bool AddTypeArguments(ReadOnlyArray<TypeSymbol> typeArguments, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            bool hasConstraints = false;

            AddPunctuation(SyntaxKind.LessThanToken, builder);

            bool first = true;
            foreach (var typeArg in typeArguments)
            {
                if (!first)
                {
                    AddPunctuation(SyntaxKind.CommaToken, builder);
                    AddSpace(builder);
                }
                first = false;

                if (typeArg.TypeKind == TypeKind.TypeParameter)
                {
                    TypeParameterSymbol typeParam = (TypeParameterSymbol)typeArg;

                    AddTypeParameterVarianceIfRequired(typeParam, builder);

                    VisitTypeParameter(typeParam, builder);

                    hasConstraints |= TypeParameterHasContraints(typeParam);
                }
                else
                {
                    VisitType(typeArg, builder);
                }
            }

            AddPunctuation(SyntaxKind.GreaterThanToken, builder);

            return hasConstraints;
        }

        private static bool TypeParameterHasContraints(TypeParameterSymbol typeParam)
        {
            return !typeParam.ConstraintTypes.IsEmpty() || typeParam.HasConstructorConstraint ||
                typeParam.HasReferenceTypeConstraint || typeParam.HasValueTypeConstraint;
        }

        private void AddTypeParameterConstraints(ReadOnlyArray<TypeSymbol> typeArguments, ArrayBuilder<SymbolDescriptionPart> builder)
        {
            foreach (var typeArg in typeArguments)
            {
                if (typeArg.TypeKind == TypeKind.TypeParameter)
                {
                    TypeParameterSymbol typeParam = (TypeParameterSymbol)typeArg;

                    if (TypeParameterHasContraints(typeParam))
                    {
                        AddSpace(builder);
                        AddKeyword(SyntaxKind.WhereKeyword, builder);
                        AddSpace(builder);

                        VisitTypeParameter(typeParam, builder);

                        AddPunctuation(SyntaxKind.ColonToken, builder);
                        AddSpace(builder);

                        bool needComma = false;

                        //class/struct contraint must be first
                        if (typeParam.HasReferenceTypeConstraint)
                        {
                            AddKeyword(SyntaxKind.ClassKeyword, builder);
                            needComma = true;
                        }
                        else if (typeParam.HasValueTypeConstraint)
                        {
                            AddKeyword(SyntaxKind.StructKeyword, builder);
                            needComma = true;
                        }

                        foreach (var baseType in typeParam.ConstraintTypes)
                        {
                            if (needComma)
                            {
                                AddPunctuation(SyntaxKind.CommaToken, builder);
                                AddSpace(builder);
                            }

                            VisitType(baseType, builder);

                            needComma = true;
                        }

                        //ctor constraint must be last
                        if (typeParam.HasConstructorConstraint)
                        {
                            if (needComma)
                            {
                                AddPunctuation(SyntaxKind.CommaToken, builder);
                                AddSpace(builder);
                            }

                            AddKeyword(SyntaxKind.NewKeyword, builder);
                            AddPunctuation(SyntaxKind.OpenParenToken, builder);
                            AddPunctuation(SyntaxKind.CloseParenToken, builder);
                        }
                    }
                }
            }
        }
    }
}