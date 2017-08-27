﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        protected readonly ArrayBuilder<SymbolDisplayPart> builder;
        protected readonly SymbolDisplayFormat format;
        protected readonly bool isFirstSymbolVisited;

        protected readonly SemanticModel semanticModelOpt;
        protected readonly int positionOpt;

        private AbstractSymbolDisplayVisitor _lazyNotFirstVisitor;

        protected AbstractSymbolDisplayVisitor(
            ArrayBuilder<SymbolDisplayPart> builder,
            SymbolDisplayFormat format,
            bool isFirstSymbolVisited,
            SemanticModel semanticModelOpt,
            int positionOpt)
        {
            Debug.Assert(format != null);

            this.builder = builder;
            this.format = format;
            this.isFirstSymbolVisited = isFirstSymbolVisited;

            this.semanticModelOpt = semanticModelOpt;
            this.positionOpt = positionOpt;

            // If we're not the first symbol visitor, then we will just recurse into ourselves.
            if (!isFirstSymbolVisited)
            {
                _lazyNotFirstVisitor = this;
            }
        }

        protected AbstractSymbolDisplayVisitor NotFirstVisitor
        {
            get
            {
                if (_lazyNotFirstVisitor == null)
                {
                    _lazyNotFirstVisitor = MakeNotFirstVisitor();
                }

                return _lazyNotFirstVisitor;
            }
        }

        protected abstract AbstractSymbolDisplayVisitor MakeNotFirstVisitor();

        protected abstract void AddLiteralValue(SpecialType type, object value);
        protected abstract void AddExplicitlyCastedLiteralValue(INamedTypeSymbol namedType, SpecialType type, object value);
        protected abstract void AddSpace();
        protected abstract void AddBitwiseOr();

        /// <summary>
        /// Append a default argument (i.e. the default argument of an optional parameter).
        /// Assumed to be non-null.
        /// </summary>
        protected void AddNonNullConstantValue(ITypeSymbol type, object constantValue, bool preferNumericValueOrExpandedFlagsForEnum = false)
        {
            Debug.Assert(constantValue != null);
            if (type.TypeKind == TypeKind.Enum)
            {
                AddEnumConstantValue((INamedTypeSymbol)type, constantValue, preferNumericValueOrExpandedFlagsForEnum);
            }
            else
            {
                AddLiteralValue(type.SpecialType, constantValue);
            }
        }

        private void AddEnumConstantValue(INamedTypeSymbol enumType, object constantValue, bool preferNumericValueOrExpandedFlags)
        {
            // Code copied from System.Enum            
            var isFlagsEnum = IsFlagsEnum(enumType);
            if (isFlagsEnum)
            {
                AddFlagsEnumConstantValue(enumType, constantValue, preferNumericValueOrExpandedFlags);
            }
            else if (preferNumericValueOrExpandedFlags)
            {
                // This isn't a flags enum, so just add the numeric value.
                AddLiteralValue(enumType.EnumUnderlyingType.SpecialType, constantValue);
            }
            else
            {
                // Try to see if its one of the enum values.  If so, add that.  Otherwise, just add
                // the literal value of the enum.
                AddNonFlagsEnumConstantValue(enumType, constantValue);
            }
        }

        /// <summary>
        /// Check if the given type is an enum with System.FlagsAttribute.
        /// </summary>
        /// <remarks>
        /// TODO: Can/should this be done using WellKnownAttributes?
        /// </remarks>
        private static bool IsFlagsEnum(ITypeSymbol typeSymbol)
        {
            Debug.Assert(typeSymbol != null);

            if (typeSymbol.TypeKind != TypeKind.Enum)
            {
                return false;
            }

            foreach (var attribute in typeSymbol.GetAttributes())
            {
                var ctor = attribute.AttributeConstructor;
                if (ctor != null)
                {
                    var type = ctor.ContainingType;
                    if (!ctor.Parameters.Any() && type.Name == "FlagsAttribute")
                    {
                        var containingSymbol = type.ContainingSymbol;
                        if (containingSymbol.Kind == SymbolKind.Namespace &&
                            containingSymbol.Name == "System" &&
                            ((INamespaceSymbol)containingSymbol.ContainingSymbol).IsGlobalNamespace)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void AddFlagsEnumConstantValue(INamedTypeSymbol enumType, object constantValue, bool preferNumericValueOrExpandedFlags)
        {
            // These values are sorted by value. Don't change this.
            var allFieldsAndValues = ArrayBuilder<EnumField>.GetInstance();
            GetSortedEnumFields(enumType, allFieldsAndValues);

            var usedFieldsAndValues = ArrayBuilder<EnumField>.GetInstance();
            try
            {
                AddFlagsEnumConstantValue(enumType, constantValue, allFieldsAndValues, usedFieldsAndValues, preferNumericValueOrExpandedFlags);
            }
            finally
            {
                allFieldsAndValues.Free();
                usedFieldsAndValues.Free();
            }
        }

        private void AddFlagsEnumConstantValue(
            INamedTypeSymbol enumType, object constantValue,
            ArrayBuilder<EnumField> allFieldsAndValues,
            ArrayBuilder<EnumField> usedFieldsAndValues,
            bool preferNumericValueOrExpandedFlags)
        {
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            var constantValueULong = EnumUtilities.ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType);

            var result = constantValueULong;

            // We will not optimize this code further to keep it maintainable. There are some
            // boundary checks that can be applied to minimize the comparisons required. This code
            // works the same for the best/worst case. In general the number of items in an enum are
            // sufficiently small and not worth the optimization.
            if (result != 0)
            {
                foreach (EnumField fieldAndValue in allFieldsAndValues)
                {
                    var valueAtIndex = fieldAndValue.Value;

                    // In the case that we prefer a numeric value or expanded flags, we don't want to add the
                    // field matching this precise value because we'd rather see the constituent parts.
                    if (preferNumericValueOrExpandedFlags && valueAtIndex == constantValueULong)
                    {
                        continue;
                    }

                    if (valueAtIndex != 0 && (result & valueAtIndex) == valueAtIndex)
                    {
                        usedFieldsAndValues.Add(fieldAndValue);
                        result -= valueAtIndex;
                        if (result == 0) break;
                    }
                }
            }

            // We were able to represent this number as a bitwise or of valid flags.
            if (result == 0 && usedFieldsAndValues.Count > 0)
            {
                // We want to emit the fields in lower to higher value.  So we walk backward.
                for (int i = usedFieldsAndValues.Count - 1; i >= 0; i--)
                {
                    if (i != (usedFieldsAndValues.Count - 1))
                    {
                        AddSpace();
                        AddBitwiseOr();
                        AddSpace();
                    }

                    ((IFieldSymbol)usedFieldsAndValues[i].IdentityOpt).Accept(this.NotFirstVisitor);
                }
            }
            else
            {
                // We couldn't find fields to OR together to make the value.

                if (preferNumericValueOrExpandedFlags)
                {
                    AddLiteralValue(underlyingSpecialType, constantValue);
                    return;
                }

                // If we had 0 as the value, and there's an enum value equal to 0, then use that.
                var zeroField = constantValueULong == 0
                    ? EnumField.FindValue(allFieldsAndValues, 0)
                    : default(EnumField);
                if (!zeroField.IsDefault)
                {
                    ((IFieldSymbol)zeroField.IdentityOpt).Accept(this.NotFirstVisitor);
                }
                else
                {
                    // Add anything else in as a literal value.
                    AddExplicitlyCastedLiteralValue(enumType, underlyingSpecialType, constantValue);
                }
            }
        }

        private static void GetSortedEnumFields(
            INamedTypeSymbol enumType,
            ArrayBuilder<EnumField> enumFields)
        {
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            foreach (var member in enumType.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    var field = (IFieldSymbol)member;
                    if (field.HasConstantValue)
                    {
                        var enumField = new EnumField(field.Name, EnumUtilities.ConvertEnumUnderlyingTypeToUInt64(field.ConstantValue, underlyingSpecialType), field);
                        enumFields.Add(enumField);
                    }
                }
            }

            enumFields.Sort(EnumField.Comparer);
        }

        private void AddNonFlagsEnumConstantValue(INamedTypeSymbol enumType, object constantValue)
        {
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            var constantValueULong = EnumUtilities.ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType);

            var enumFields = ArrayBuilder<EnumField>.GetInstance();
            GetSortedEnumFields(enumType, enumFields);

            // See if there's a member with this value.  If so, then use that.
            var match = EnumField.FindValue(enumFields, constantValueULong);
            if (!match.IsDefault)
            {
                ((IFieldSymbol)match.IdentityOpt).Accept(this.NotFirstVisitor);
            }
            else
            {
                // Otherwise, just add the enum as a literal.
                AddExplicitlyCastedLiteralValue(enumType, underlyingSpecialType, constantValue);
            }
            enumFields.Free();
        }
    }
}
