// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    internal partial class CSharpRecommendationServiceRunner
    {
        private static readonly ImmutableArray<SpecialType> s_builtInEnumConversionTargets = ImmutableArray.Create(
            // Presorted alphabetical to reduce sorting cost of the completion items. 
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Decimal,
            SpecialType.System_Double,
            SpecialType.System_Single,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16);

        private static readonly ImmutableArray<SpecialType> s_sbyteConversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16);

        private static readonly ImmutableArray<SpecialType> s_byteConversions = ImmutableArray.Create(
            SpecialType.System_Char,
            SpecialType.System_SByte);

        private static readonly ImmutableArray<SpecialType> s_int16Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte);

        private static readonly ImmutableArray<SpecialType> s_uint16Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_int32Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_UInt32,
            SpecialType.System_UInt16,
            SpecialType.System_UInt64);

        private static readonly ImmutableArray<SpecialType> s_uint32Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_UInt16);

        private static readonly ImmutableArray<SpecialType> s_int64Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_uint64Conversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_charConversions = ImmutableArray.Create(
             SpecialType.System_Byte,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_singleConversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Decimal,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_doubleConversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Decimal,
            SpecialType.System_Single,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private static readonly ImmutableArray<SpecialType> s_decimalConversions = ImmutableArray.Create(
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Double,
            SpecialType.System_Single,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16);

        private void AddConversions(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            AddUserDefinedConversionsOfType(container, symbols);
            if (container is INamedTypeSymbol namedType)
            {
                AddBuiltInNumericConversions(namedType, symbols);
                AddBuiltInEnumConversions(namedType, symbols);
            }
        }

        private void AddUserDefinedConversionsOfType(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            // Base types are valid sources for user-defined conversions.
            // Note: We only look in the source (aka container), because target could be any type (in scope) of the compilation.
            // No need to check for accessibility as operators must always be public.
            // The target type is lifted, if containerIsNullable and the target of the conversion is a struct

            foreach (var type in container.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers(WellKnownMemberNames.ExplicitConversionName))
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (!method.IsConversion())
                        continue;

                    if (method.Parameters.Length != 1)
                        continue;

                    if (!type.Equals(method.Parameters[0].Type))
                        continue;

                    symbols.Add(LiftIfNecessary(method));
                    //builder.Add(CreateSymbolCompletionItem(
                    //    targetTypeName: method.ReturnType.ToMinimalDisplayString(semanticModel, position),
                    //    targetTypeIsNullable: containerIsNullable && method.ReturnType.IsStructType(),
                    //    position,
                    //    method));
                }
            }
        }

        private IMethodSymbol LiftIfNecessary(IMethodSymbol method)
        {
            throw new NotImplementedException();
        }

        private void AddBuiltInNumericConversions(INamedTypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            if (container.SpecialType == SpecialType.System_Decimal)
            {
                // Decimal is defined in the spec with integrated conversions, but is the only type that reports it's
                // conversions as normal method symbols
                return;
            }

            var numericConversions = GetBuiltInNumericConversions(container);
            if (!numericConversions.HasValue)
                return;

            AddCompletionItemsForSpecialTypes(container, symbols, numericConversions.Value);

        }

        // Sorted alphabetical
        public static ImmutableArray<SpecialType>? GetBuiltInNumericConversions(ITypeSymbol container)
            => container.SpecialType switch
            {
                SpecialType.System_SByte => s_sbyteConversions,
                SpecialType.System_Byte => s_byteConversions,
                SpecialType.System_Int16 => s_int16Conversions,
                SpecialType.System_UInt16 => s_uint16Conversions,
                SpecialType.System_Int32 => s_int32Conversions,
                SpecialType.System_UInt32 => s_uint32Conversions,
                SpecialType.System_Int64 => s_int64Conversions,
                SpecialType.System_UInt64 => s_uint64Conversions,
                SpecialType.System_Char => s_charConversions,
                SpecialType.System_Single => s_singleConversions,
                SpecialType.System_Double => s_doubleConversions,
                SpecialType.System_Decimal => s_decimalConversions,
                _ => null,
            };

        private void AddCompletionItemsForSpecialTypes(
            INamedTypeSymbol container, ArrayBuilder<ISymbol> symbols, ImmutableArray<SpecialType> specialTypes)
        {
            foreach (var specialType in specialTypes)
            {
                var targetTypeSymbol = _context.SemanticModel.Compilation.GetSpecialType(specialType);
                symbols.Add(CreateConversion(from: container, to: targetTypeSymbol));
                //var targetTypeName = targetTypeSymbol.ToMinimalDisplayString(semanticModel, position);
                //builder.Add(CreateSymbolCompletionItem(
                //    targetTypeName, targetTypeIsNullable: containerIsNullable, position, fromType, targetTypeSymbol));
            }
        }

        private ISymbol CreateConversion(INamedTypeSymbol from, INamedTypeSymbol to)
        {
            throw new NotImplementedException();
        }

        private void AddBuiltInEnumConversions(INamedTypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            // Three kinds of conversions are defined in the spec.
            // Suggestion are made for one kind:
            // * From any enum_type to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal.
            // No suggestion for the other two kinds of conversions:
            // * From sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal to any enum_type.
            // * From any enum_type to any other enum_type.

            // Suggest after enum members: SomeEnum.EnumMember.$$ 
            // or on enum values: someEnumVariable.$$
            if (container.IsEnumMember() || container.IsEnumType())
                AddCompletionItemsForSpecialTypes(container, symbols, s_builtInEnumConversionTargets);
        }
    }
}
