// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class EvaluatedConstant
    {
        public readonly ConstantValue Value;
        public readonly ImmutableArray<Diagnostic> Diagnostics;

        public EvaluatedConstant(ConstantValue value, ImmutableArray<Diagnostic> diagnostics)
        {
            this.Value = value;
            this.Diagnostics = diagnostics;
        }
    }

    internal static class ConstantValueUtils
    {
        public static ConstantValue EvaluateFieldConstant(
            SourceFieldSymbol symbol,
            EqualsValueClauseSyntax equalsValueNode,
            HashSet<SourceFieldSymbolWithSyntaxReference> dependencies,
            bool earlyDecodingWellKnownAttributes,
            DiagnosticBag diagnostics)
        {
            var compilation = symbol.DeclaringCompilation;
            var binderFactory = compilation.GetBinderFactory((SyntaxTree)symbol.Locations[0].SourceTree);
            var binder = binderFactory.GetBinder(equalsValueNode);
            if (earlyDecodingWellKnownAttributes)
            {
                binder = new EarlyWellKnownAttributeBinder(binder);
            }
            var inProgressBinder = new ConstantFieldsInProgressBinder(new ConstantFieldsInProgress(symbol, dependencies), binder);

            var scopeBinder = new ScopedExpressionBinder(inProgressBinder, equalsValueNode.Value);
            var boundValue = BindFieldOrEnumInitializer(scopeBinder, symbol, equalsValueNode, diagnostics);

            if (!scopeBinder.Locals.IsDefaultOrEmpty)
            {
                boundValue = scopeBinder.AddLocalScopeToExpression(boundValue);
            }

            var initValueNodeLocation = equalsValueNode.Value.Location;

            var value = GetAndValidateConstantValue(boundValue, symbol, symbol.Type, initValueNodeLocation, diagnostics);
            Debug.Assert(value != null);

            return value;
        }

        private static BoundExpression BindFieldOrEnumInitializer(
            Binder binder,
            FieldSymbol fieldSymbol,
            EqualsValueClauseSyntax initializer,
            DiagnosticBag diagnostics)
        {
            var enumConstant = fieldSymbol as SourceEnumConstantSymbol;
            var collisionDetector = new LocalScopeBinder(binder);
            if ((object)enumConstant != null)
            {
                return collisionDetector.BindEnumConstantInitializer(enumConstant, initializer.Value, diagnostics);
            }
            else
            {
                return collisionDetector.BindVariableOrAutoPropInitializer(initializer, fieldSymbol.Type, diagnostics);
            }
        }

        internal static ConstantValue GetAndValidateConstantValue(
            BoundExpression boundValue,
            Symbol thisSymbol,
            TypeSymbol typeSymbol,
            Location initValueNodeLocation,
            DiagnosticBag diagnostics)
        {
            var value = ConstantValue.Bad;
            if (!boundValue.HasAnyErrors)
            {
                if (typeSymbol.TypeKind == TypeKind.TypeParameter)
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidConstantDeclarationType, initValueNodeLocation, thisSymbol, typeSymbol);
                }
                else
                {
                    bool hasDynamicConversion = false;
                    var unconvertedBoundValue = boundValue;
                    while (unconvertedBoundValue.Kind == BoundKind.Conversion)
                    {
                        var conversion = (BoundConversion)unconvertedBoundValue;
                        hasDynamicConversion = hasDynamicConversion || conversion.ConversionKind.IsDynamic();
                        unconvertedBoundValue = conversion.Operand;
                    }

                    var unconvertedConstantValue = unconvertedBoundValue.ConstantValue;
                    if (unconvertedConstantValue != null &&
                        !unconvertedConstantValue.IsNull &&
                        typeSymbol.IsReferenceType &&
                        typeSymbol.SpecialType != SpecialType.System_String)
                    {
                        // Suppose we are in this case:
                        //
                        // const object x = "some_string"
                        //
                        // A constant of type object can only be initialized to
                        // null; it may not contain an implicit reference conversion
                        // from string.
                        //
                        // Give a special error for that case.
                        diagnostics.Add(ErrorCode.ERR_NotNullConstRefField, initValueNodeLocation, thisSymbol, typeSymbol);
                    }

                    // If we have already computed the unconverted constant value, then this call is cheap
                    // because BoundConversions store their constant values (i.e. not recomputing anything).
                    var constantValue = boundValue.ConstantValue;

                    // If we saw ERR_NotNullConstRefField above, then the constant value will likely be null.
                    // However, it seems reasonable to assume that the programmer will correct the error not
                    // by changing the value to "null", but by updating the type of the constant.  Consequently,
                    // we retain the unconverted constant value so that it can propagate through the rest of
                    // constant folding.
                    constantValue = constantValue ?? unconvertedConstantValue;

                    if (constantValue != null && !hasDynamicConversion)
                    {
                        value = constantValue;
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_NotConstantExpression, initValueNodeLocation, thisSymbol);
                    }
                }
            }
            return value;
        }

        internal struct DecimalValue
        {
            internal readonly uint Low;
            internal readonly uint Mid;
            internal readonly uint High;
            internal readonly byte Scale;
            internal readonly bool IsNegative;

            public DecimalValue(decimal value)
            {
                int[] bits = System.Decimal.GetBits(value);

                // The return value is a four-element array of 32-bit signed integers.
                // The first, second, and third elements of the returned array contain the low, middle, and high 32 bits of the 96-bit integer number.
                Low = unchecked((uint)bits[0]);
                Mid = unchecked((uint)bits[1]);
                High = unchecked((uint)bits[2]);

                // The fourth element of the returned array contains the scale factor and sign. It consists of the following parts:
                // Bits 0 to 15, the lower word, are unused and must be zero.
                // Bits 16 to 23 must contain an exponent between 0 and 28, which indicates the power of 10 to divide the integer number.
                // Bits 24 to 30 are unused and must be zero.
                // Bit 31 contains the sign; 0 meaning positive, and 1 meaning negative.
                Scale = (byte)((bits[3] & 0xFF0000) >> 16);
                IsNegative = ((bits[3] & 0x80000000) != 0);
            }
        }
    }
}
