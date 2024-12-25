// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations;

internal partial class CSharpRecommendationService
{
    /// <summary>
    /// Adds user defined and predefined conversions to the unnamed recommendation set.
    /// </summary>
    private sealed partial class CSharpRecommendationServiceRunner
    {
        private static readonly ImmutableArray<SpecialType> s_predefinedEnumConversionTargets =
        [
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
            SpecialType.System_UInt16,
        ];

        private static readonly ImmutableArray<SpecialType> s_sbyteConversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
        ];

        private static readonly ImmutableArray<SpecialType> s_byteConversions = [SpecialType.System_Char, SpecialType.System_SByte];

        private static readonly ImmutableArray<SpecialType> s_int16Conversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
        ];

        private static readonly ImmutableArray<SpecialType> s_uint16Conversions = [SpecialType.System_Byte, SpecialType.System_Char, SpecialType.System_SByte, SpecialType.System_Int16];

        private static readonly ImmutableArray<SpecialType> s_int32Conversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_UInt32,
            SpecialType.System_UInt16,
            SpecialType.System_UInt64,
        ];

        private static readonly ImmutableArray<SpecialType> s_uint32Conversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
            SpecialType.System_UInt16,
        ];

        private static readonly ImmutableArray<SpecialType> s_int64Conversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
        ];

        private static readonly ImmutableArray<SpecialType> s_uint64Conversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
        ];

        private static readonly ImmutableArray<SpecialType> s_charConversions = [SpecialType.System_Byte, SpecialType.System_SByte, SpecialType.System_Int16];

        private static readonly ImmutableArray<SpecialType> s_singleConversions =
        [
            SpecialType.System_Byte,
            SpecialType.System_Char,
            SpecialType.System_Decimal,
            SpecialType.System_Int32,
            SpecialType.System_Int64,
            SpecialType.System_UInt32,
            SpecialType.System_UInt64,
            SpecialType.System_UInt16,
            SpecialType.System_SByte,
            SpecialType.System_Int16,
        ];

        private static readonly ImmutableArray<SpecialType> s_doubleConversions =
        [
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
            SpecialType.System_Int16,
        ];

        private void AddConversions(ITypeSymbol container, ArrayBuilder<ISymbol> symbols)
        {
            if (container.RemoveNullableIfPresent() is INamedTypeSymbol namedType)
            {
                AddUserDefinedConversionsOfType(container, namedType, symbols);
                AddBuiltInNumericConversions(container, namedType, symbols);
                AddBuiltInEnumConversions(container, namedType, symbols);
            }
        }

        private static ITypeSymbol TryMakeNullable(Compilation compilation, ITypeSymbol container)
        {
            return container.IsNonNullableValueType()
                ? compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(container)
                : container;
        }

        private void AddUserDefinedConversionsOfType(
            ITypeSymbol container, INamedTypeSymbol containerWithoutNullable, ArrayBuilder<ISymbol> symbols)
        {
            var compilation = _context.SemanticModel.Compilation;
            var containerIsNullable = container.IsNullable();

            foreach (var type in containerWithoutNullable.GetBaseTypesAndThis())
            {
                foreach (var member in type.GetMembers(WellKnownMemberNames.ExplicitConversionName))
                {
                    if (member is not IMethodSymbol method)
                        continue;

                    if (!method.IsConversion())
                        continue;

                    if (method.Parameters.Length != 1)
                        continue;

                    // Has to be a conversion that actually converts the type we're operating on.
                    if (!type.Equals(method.Parameters[0].Type))
                        continue;

                    // If this is a nullable context, then 'lift' the conversion so we offer the nullable form of it to
                    // the user instead.
                    symbols.Add(containerIsNullable && IsLiftableConversion(method)
                        ? LiftConversion(compilation, method)
                        : method);
                }
            }

            return;

            // https://github.com/dotnet/csharplang/blob/main/spec/conversions.md#lifted-conversion-operators      
            //
            // Given a user-defined conversion operator that converts from a non-nullable value type S to a non-nullable
            // value type T, a lifted conversion operator exists that converts from S? to T?
            static bool IsLiftableConversion(IMethodSymbol method)
                => method.ReturnType.IsNonNullableValueType() && method.Parameters.Single().Type.IsNonNullableValueType();
        }

        private IMethodSymbol LiftConversion(Compilation compilation, IMethodSymbol method)
            => CreateConversion(
                method.ContainingType,
                TryMakeNullable(compilation, method.Parameters.Single().Type),
                TryMakeNullable(compilation, method.ReturnType),
                method.GetDocumentationCommentXml(cancellationToken: _cancellationToken));

        private void AddBuiltInNumericConversions(
            ITypeSymbol container, INamedTypeSymbol containerWithoutNullable, ArrayBuilder<ISymbol> symbols)
        {
            var conversions = GetPredefinedNumericConversions(containerWithoutNullable);
            if (!conversions.HasValue)
                return;

            AddCompletionItemsForSpecialTypes(container, containerWithoutNullable, symbols, conversions.Value);
        }

        public static ImmutableArray<SpecialType>? GetPredefinedNumericConversions(ITypeSymbol container)
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
                // Decimal intentionally not here as it exposes its conversions as normal methods in the symbol model.
                _ => null,
            };

        private void AddCompletionItemsForSpecialTypes(
            ITypeSymbol container, INamedTypeSymbol containerWithoutNullable, ArrayBuilder<ISymbol> symbols, ImmutableArray<SpecialType> specialTypes)
        {
            var compilation = _context.SemanticModel.Compilation;

            foreach (var specialType in specialTypes)
            {
                var targetTypeSymbol = _context.SemanticModel.Compilation.GetSpecialType(specialType);
                var conversion = CreateConversion(
                    containerWithoutNullable, fromType: containerWithoutNullable, toType: targetTypeSymbol,
                    CreateConversionDocumentationCommentXml(containerWithoutNullable, targetTypeSymbol));

                symbols.Add(container.IsNullable() ? LiftConversion(compilation, conversion) : conversion);
            }

            return;

            static string CreateConversionDocumentationCommentXml(ITypeSymbol fromType, ITypeSymbol toType)
            {
                var summary = string.Format(WorkspacesResources.Predefined_conversion_from_0_to_1,
                    SeeTag(fromType.GetDocumentationCommentId()),
                    SeeTag(toType.GetDocumentationCommentId()));

                return $"<summary>{summary}</summary>";

                static string SeeTag(string? id)
                    => $@"<see cref=""{id}""/>";
            }
        }

        private static IMethodSymbol CreateConversion(INamedTypeSymbol containingType, ITypeSymbol fromType, ITypeSymbol toType, string? documentationCommentXml)
            => CodeGenerationSymbolFactory.CreateConversionSymbol(
                toType: toType,
                fromType: CodeGenerationSymbolFactory.CreateParameterSymbol(fromType, "value"),
                containingType: containingType,
                documentationCommentXml: documentationCommentXml);

        private void AddBuiltInEnumConversions(
            ITypeSymbol container, INamedTypeSymbol containerWithoutNullable, ArrayBuilder<ISymbol> symbols)
        {
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/conversions#explicit-enumeration-conversions
            // Three kinds of conversions are defined in the spec.
            // Suggestion are made for one kind:
            // * From any enum_type to sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal.
            // No suggestion for the other two kinds of conversions:
            // * From sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, or decimal to any enum_type.
            // * From any enum_type to any other enum_type.

            if (containerWithoutNullable.IsEnumType())
                AddCompletionItemsForSpecialTypes(container, containerWithoutNullable, symbols, s_predefinedEnumConversionTargets);
        }
    }
}
