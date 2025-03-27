// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolDisplay
{
    internal abstract partial class AbstractSymbolDisplayVisitor : SymbolVisitor
    {
        // These values will be set to non-null by 'Allocate' when an instance is obtained from the pool
        private ArrayBuilder<SymbolDisplayPart> _builder = null!;
        private SymbolDisplayFormat _format = null!;
        private bool _isFirstSymbolVisited;
        private bool _inNamespaceOrType;

        private SemanticModel? _semanticModelOpt;
        private int _positionOpt;

        private AbstractSymbolDisplayVisitor? _lazyNotFirstVisitor;
        private AbstractSymbolDisplayVisitor? _lazyNotFirstVisitorNamespaceOrType;

        protected AbstractSymbolDisplayVisitor()
        {
        }

        protected ArrayBuilder<SymbolDisplayPart> Builder => _builder;

        protected SymbolDisplayFormat Format => _format;

        protected bool IsFirstSymbolVisited => _isFirstSymbolVisited;

        protected bool InNamespaceOrType => _inNamespaceOrType;

        protected SemanticModel? SemanticModelOpt => _semanticModelOpt;

        protected int PositionOpt => _positionOpt;

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

        protected AbstractSymbolDisplayVisitor NotFirstVisitorNamespaceOrType
        {
            get
            {
                if (_lazyNotFirstVisitorNamespaceOrType == null)
                {
                    _lazyNotFirstVisitorNamespaceOrType = MakeNotFirstVisitor(inNamespaceOrType: true);
                }

                return _lazyNotFirstVisitorNamespaceOrType;
            }
        }

        protected void Initialize(
            ArrayBuilder<SymbolDisplayPart> builder,
            SymbolDisplayFormat format,
            bool isFirstSymbolVisited,
            SemanticModel? semanticModelOpt,
            int positionOpt,
            bool inNamespaceOrType)
        {
            Debug.Assert(format != null);

            if (_format is not null)
            {
                // Should not be re-initializing a visitor which is already in use.
                throw new InvalidOperationException();
            }

            _builder = builder;
            _format = format;
            _isFirstSymbolVisited = isFirstSymbolVisited;

            _semanticModelOpt = semanticModelOpt;
            _positionOpt = positionOpt;
            _inNamespaceOrType = inNamespaceOrType;

            // If we're not the first symbol visitor, then we will just recurse into ourselves.
            if (!isFirstSymbolVisited)
            {
                _lazyNotFirstVisitor = this;
            }
        }

        public virtual void Free()
        {
            if (_lazyNotFirstVisitor != this && _lazyNotFirstVisitor is not null)
                FreeNotFirstVisitor(_lazyNotFirstVisitor);

            if (_lazyNotFirstVisitorNamespaceOrType != this && _lazyNotFirstVisitorNamespaceOrType is not null)
                FreeNotFirstVisitor(_lazyNotFirstVisitorNamespaceOrType);

            _builder = null!;
            _isFirstSymbolVisited = false;
            _inNamespaceOrType = false;

            _semanticModelOpt = null;
            _positionOpt = 0;
            _lazyNotFirstVisitor = null;
            _lazyNotFirstVisitorNamespaceOrType = null;

            _format = null!;
        }

        protected abstract AbstractSymbolDisplayVisitor MakeNotFirstVisitor(bool inNamespaceOrType = false);

        protected abstract void FreeNotFirstVisitor(AbstractSymbolDisplayVisitor visitor);

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

            if (ITypeSymbolHelpers.IsNullableType(type))
            {
                type = ITypeSymbolHelpers.GetNullableUnderlyingType(type);
            }

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
            Debug.Assert(enumType.EnumUnderlyingType is not null);

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
            Debug.Assert(enumType.EnumUnderlyingType is not null);
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            var constantValueULong = underlyingSpecialType.ConvertUnderlyingValueToUInt64(constantValue);

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

                    ((IFieldSymbol)usedFieldsAndValues[i].IdentityOpt!).Accept(this.NotFirstVisitor);
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
                    Debug.Assert(zeroField.IdentityOpt is not null);
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
            Debug.Assert(enumType.EnumUnderlyingType is not null);
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            foreach (var member in enumType.GetMembers())
            {
                if (member.Kind == SymbolKind.Field)
                {
                    var field = (IFieldSymbol)member;
                    if (field.HasConstantValue)
                    {
                        var enumField = new EnumField(field.Name, underlyingSpecialType.ConvertUnderlyingValueToUInt64(field.ConstantValue), field);
                        enumFields.Add(enumField);
                    }
                }
            }

            enumFields.Sort(EnumField.Comparer);
        }

        private void AddNonFlagsEnumConstantValue(INamedTypeSymbol enumType, object constantValue)
        {
            Debug.Assert(enumType.EnumUnderlyingType is not null);
            var underlyingSpecialType = enumType.EnumUnderlyingType.SpecialType;
            var constantValueULong = underlyingSpecialType.ConvertUnderlyingValueToUInt64(constantValue);

            var enumFields = ArrayBuilder<EnumField>.GetInstance();
            GetSortedEnumFields(enumType, enumFields);

            // See if there's a member with this value.  If so, then use that.
            var match = EnumField.FindValue(enumFields, constantValueULong);
            if (!match.IsDefault)
            {
                Debug.Assert(match.IdentityOpt is not null);
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
