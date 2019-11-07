// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    /// <summary>
    /// Abstract value content data value for <see cref="AnalysisEntity"/>/<see cref="IOperation"/> tracked by <see cref="ValueContentAnalysis"/>.
    /// </summary>
    public partial class ValueContentAbstractValue : CacheBasedEquatable<ValueContentAbstractValue>
    {
        // Ensure we bound the number of value content literals and avoid infinite analysis iterations.
        private const int LiteralsBound = 10;

        public static ValueContentAbstractValue UndefinedState { get; } = new ValueContentAbstractValue(ImmutableHashSet<object?>.Empty, ValueContainsNonLiteralState.Undefined);
        public static ValueContentAbstractValue InvalidState { get; } = new ValueContentAbstractValue(ImmutableHashSet<object?>.Empty, ValueContainsNonLiteralState.Invalid);
        public static ValueContentAbstractValue MayBeContainsNonLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet<object?>.Empty, ValueContainsNonLiteralState.Maybe);
        public static ValueContentAbstractValue DoesNotContainLiteralOrNonLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet<object?>.Empty, ValueContainsNonLiteralState.No);
        public static ValueContentAbstractValue ContainsNullLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create((object?)null), ValueContainsNonLiteralState.No);
        public static ValueContentAbstractValue ContainsEmptyStringLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(string.Empty), ValueContainsNonLiteralState.No);
        public static ValueContentAbstractValue ContainsZeroIntergralLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(0), ValueContainsNonLiteralState.No);
        public static ValueContentAbstractValue ContainsOneIntergralLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(1), ValueContainsNonLiteralState.No);
        private static ValueContentAbstractValue ContainsTrueLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(true), ValueContainsNonLiteralState.No);
        private static ValueContentAbstractValue ContainsFalseLiteralState { get; } = new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(false), ValueContainsNonLiteralState.No);

        private ValueContentAbstractValue(ImmutableHashSet<object?> literalValues, ValueContainsNonLiteralState nonLiteralState)
        {
            LiteralValues = literalValues;
            NonLiteralState = nonLiteralState;
        }

        internal static ValueContentAbstractValue Create(object literal, ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                    if (DiagnosticHelpers.TryConvertToUInt64(literal, type.SpecialType, out ulong convertedValue) &&
                        convertedValue == 0)
                    {
                        return ContainsZeroIntergralLiteralState;
                    }

                    break;

                case SpecialType.System_String:
                    if (((string)literal).Length == 0)
                    {
                        return ContainsEmptyStringLiteralState;
                    }

                    break;

                case SpecialType.System_Boolean:
                    return ((bool)literal) ? ContainsTrueLiteralState : ContainsFalseLiteralState;
            }

            return new ValueContentAbstractValue(ImmutableHashSet.Create<object?>(literal), ValueContainsNonLiteralState.No);
        }

        private static ValueContentAbstractValue Create(ImmutableHashSet<object?> literalValues, ValueContainsNonLiteralState nonLiteralState)
        {
            if (literalValues.IsEmpty)
            {
                return nonLiteralState switch
                {
                    ValueContainsNonLiteralState.Undefined => UndefinedState,
                    ValueContainsNonLiteralState.Invalid => InvalidState,
                    ValueContainsNonLiteralState.No => DoesNotContainLiteralOrNonLiteralState,
                    _ => MayBeContainsNonLiteralState,
                };
            }
            else if (literalValues.Count == 1 && nonLiteralState == ValueContainsNonLiteralState.No)
            {
                switch (literalValues.Single())
                {
                    case bool boolVal:
                        return boolVal ? ContainsTrueLiteralState : ContainsFalseLiteralState;

                    case string stringVal:
                        if (stringVal.Length == 0)
                        {
                            return ContainsEmptyStringLiteralState;
                        }

                        break;

                    case int intValue:
                        if (intValue == 0)
                        {
                            return ContainsZeroIntergralLiteralState;
                        }

                        break;
                }
            }


            return new ValueContentAbstractValue(literalValues, nonLiteralState);
        }

        internal static bool IsSupportedType(ITypeSymbol type, [NotNullWhen(returnValue: true)] out ITypeSymbol? valueTypeSymbol)
        {
            if (type.IsPrimitiveType())
            {
                valueTypeSymbol = type;
                return true;
            }
            else if (type is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.EnumUnderlyingType != null)
            {
                valueTypeSymbol = namedTypeSymbol.EnumUnderlyingType;
                return true;
            }
            else
            {
                valueTypeSymbol = null;
                return false;
            }
        }

        /// <summary>
        /// Indicates if this variable contains non literal operands or not.
        /// </summary>
        public ValueContainsNonLiteralState NonLiteralState { get; }

        /// <summary>
        /// Gets a collection of the literals that could possibly make up the contents of this abstract value.
        /// </summary>
        public ImmutableHashSet<object?> LiteralValues { get; }

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(LiteralValues));
            addPart(NonLiteralState.GetHashCode());
        }

        /// <summary>
        /// Performs the union of this state and the other state 
        /// and returns a new <see cref="ValueContentAbstractValue"/> with the result.
        /// </summary>
        internal ValueContentAbstractValue Merge(ValueContentAbstractValue otherState)
        {
            if (otherState == null)
            {
                throw new ArgumentNullException(nameof(otherState));
            }

            ImmutableHashSet<object?> mergedLiteralValues = LiteralValues.AddRange(otherState.LiteralValues);
            if (mergedLiteralValues.Count > LiteralsBound)
            {
                return MayBeContainsNonLiteralState;
            }

            ValueContainsNonLiteralState mergedNonLiteralState = Merge(NonLiteralState, otherState.NonLiteralState);
            return Create(mergedLiteralValues, mergedNonLiteralState);
        }

        private static ValueContainsNonLiteralState Merge(ValueContainsNonLiteralState value1, ValueContainsNonLiteralState value2)
        {
            // + U I M N
            // U U U M N
            // I U I M N
            // M M M M M
            // N N N M N
            if (value1 == ValueContainsNonLiteralState.Maybe || value2 == ValueContainsNonLiteralState.Maybe)
            {
                return ValueContainsNonLiteralState.Maybe;
            }
            else if (value1 == ValueContainsNonLiteralState.Invalid || value1 == ValueContainsNonLiteralState.Undefined)
            {
                return value2;
            }
            else if (value2 == ValueContainsNonLiteralState.Invalid || value2 == ValueContainsNonLiteralState.Undefined)
            {
                return value1;
            }

            Debug.Assert(value1 == ValueContainsNonLiteralState.No);
            Debug.Assert(value2 == ValueContainsNonLiteralState.No);
            return ValueContainsNonLiteralState.No;
        }

        public bool IsLiteralState => !LiteralValues.IsEmpty && NonLiteralState == ValueContainsNonLiteralState.No;

        /// <summary>
        /// For super simple cases: If this abstract value is a single non-null literal, then get that literal value.
        /// </summary>
        /// <typeparam name="T">Type of the expected literal value.</typeparam>
        /// <param name="literalValue">Literal value, or its default if not a single non-null literal value.</param>
        /// <returns>True if a non-null literal value was found, false otherwise.</returns>
        /// <remarks>If you're looking for null, you should be looking at <see cref="PointsToAnalysis"/>.</remarks>
        public bool TryGetSingleNonNullLiteral<T>(out T literalValue)
        {
            if (!IsLiteralState || LiteralValues.Count != 1)
            {
                literalValue = default!;
                return false;
            }

            object? o = LiteralValues.First();
            if (o is T v)
            {
                literalValue = v;
                return true;
            }
            else
            {
                literalValue = default!;
                return false;
            }
        }

        internal ValueContentAbstractValue IntersectLiteralValues(ValueContentAbstractValue value2)
        {
            Debug.Assert(IsLiteralState);
            Debug.Assert(value2.IsLiteralState);

            // Merge Literals
            var mergedLiteralValues = this.LiteralValues.Intersect(value2.LiteralValues);
            return mergedLiteralValues.IsEmpty ? InvalidState : new ValueContentAbstractValue(mergedLiteralValues, ValueContainsNonLiteralState.No);
        }

        /// <summary>
        /// Performs the union of this state and the other state for a Binary operation
        /// and returns a new <see cref="ValueContentAbstractValue"/> with the result.
        /// </summary>
        internal ValueContentAbstractValue MergeBinaryOperation(
            ValueContentAbstractValue otherState,
            BinaryOperatorKind binaryOperatorKind,
            ITypeSymbol leftType,
            ITypeSymbol rightType,
            ITypeSymbol resultType)
        {
            if (otherState == null)
            {
                throw new ArgumentNullException(nameof(otherState));
            }

            // Merge Literals
            var builder = PooledHashSet<object?>.GetInstance();
            foreach (var leftLiteral in LiteralValues)
            {
                foreach (var rightLiteral in otherState.LiteralValues)
                {
                    if (!TryMerge(leftLiteral, rightLiteral, binaryOperatorKind, leftType, rightType, resultType, out object? result))
                    {
                        return MayBeContainsNonLiteralState;
                    }

                    builder.Add(result);
                }
            }

            ImmutableHashSet<object?> mergedLiteralValues = builder.ToImmutableAndFree();
            ValueContainsNonLiteralState mergedNonLiteralState = Merge(NonLiteralState, otherState.NonLiteralState);

            return Create(mergedLiteralValues, mergedNonLiteralState);
        }

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "L({0}) NL:{1}", LiteralValues.Count, NonLiteralState.ToString()[0]);

        private static bool TryMerge(object? value1, object? value2, BinaryOperatorKind binaryOperatorKind, ITypeSymbol type1, ITypeSymbol type2, ITypeSymbol resultType, [NotNullWhen(returnValue: true)] out object? result)
        {
            result = null;

            if (value1 == null || value2 == null)
            {
                return false;
            }

            try
            {
                switch (type1.SpecialType)
                {
                    case SpecialType.System_String:
                        return type2.SpecialType == SpecialType.System_String &&
                            TryMerge((string)value1, (string)value2, binaryOperatorKind, out result);

                    case SpecialType.System_Char:
                        return type2.SpecialType == SpecialType.System_Char &&
                            TryMerge((char)value1, (char)value2, binaryOperatorKind, out result);

                    case SpecialType.System_Boolean:
                        return type2.SpecialType == SpecialType.System_Boolean &&
                            TryMerge((bool)value1, (bool)value2, binaryOperatorKind, out result);

                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_SByte:
                    case SpecialType.System_UInt64:
                        if (DiagnosticHelpers.TryConvertToUInt64(value1, type1.SpecialType, out ulong convertedValue1) &&
                            DiagnosticHelpers.TryConvertToUInt64(value2, type2.SpecialType, out ulong convertedValue2) &&
                            TryMerge(convertedValue1, convertedValue2, binaryOperatorKind, out var convertedResult))
                        {
                            switch (resultType.SpecialType)
                            {
                                case SpecialType.System_SByte:
                                    result = (sbyte)convertedResult;
                                    return true;

                                case SpecialType.System_Int16:
                                    result = (short)convertedResult;
                                    return true;

                                case SpecialType.System_Int32:
                                    result = (int)convertedResult;
                                    return true;

                                case SpecialType.System_Int64:
                                    result = (long)convertedResult;
                                    return true;

                                case SpecialType.System_Byte:
                                    result = (byte)convertedResult;
                                    return true;

                                case SpecialType.System_UInt16:
                                    result = (ushort)convertedResult;
                                    return true;

                                case SpecialType.System_UInt32:
                                    result = (uint)convertedResult;
                                    return true;

                                case SpecialType.System_UInt64:
                                    result = convertedResult;
                                    return true;

                                case SpecialType.System_Boolean:
                                    result = convertedResult != 0UL;
                                    return true;
                            }
                        }

                        break;

                    case SpecialType.System_Double:
                    case SpecialType.System_Single:
                        switch (type2.SpecialType)
                        {
                            case SpecialType.System_Single:
                            case SpecialType.System_Double:
                                if (TryMerge((double)value1, (double)value2, binaryOperatorKind, out double doubleResult))
                                {
                                    switch (resultType.SpecialType)
                                    {
                                        case SpecialType.System_Single:
                                            result = (float)doubleResult;
                                            return true;

                                        case SpecialType.System_Double:
                                            result = doubleResult;
                                            return true;

                                        case SpecialType.System_Boolean:
                                            result = doubleResult != 0;
                                            return true;
                                    }
                                }

                                break;
                        }

                        break;
                }
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
            {
                // Catch all arithmetic exceptions, and conservatively bail out.
            }
#pragma warning restore CA1031 // Do not catch general exception types

            return false;
        }

        private static bool TryMerge(char value1, char value2, BinaryOperatorKind binaryOperatorKind, [NotNullWhen(returnValue: true)] out object? result)
        {
            switch (binaryOperatorKind)
            {
                case BinaryOperatorKind.Add:
                case BinaryOperatorKind.Concatenate:
                    result = value1 + value2;
                    return true;
            }

            result = null;
            return false;
        }

        private static bool TryMerge(string value1, string value2, BinaryOperatorKind binaryOperatorKind, [NotNullWhen(returnValue: true)] out object? result)
        {
            if (value1 != null && value2 != null)
            {
                switch (binaryOperatorKind)
                {
                    case BinaryOperatorKind.Add:
                    case BinaryOperatorKind.Concatenate:
                        result = value1 + value2;
                        return true;
                }
            }

            result = null;
            return false;
        }

        private static bool TryMerge(bool value1, bool value2, BinaryOperatorKind binaryOperatorKind, [NotNullWhen(returnValue: true)] out object? result)
        {
            switch (binaryOperatorKind)
            {
                case BinaryOperatorKind.And:
                case BinaryOperatorKind.ConditionalAnd:
                    result = value1 && value2;
                    return true;

                case BinaryOperatorKind.Or:
                case BinaryOperatorKind.ConditionalOr:
                    result = value1 || value2;
                    return true;

                case BinaryOperatorKind.Equals:
                    result = value1 == value2;
                    return true;

                case BinaryOperatorKind.NotEquals:
                    result = value1 != value2;
                    return true;
            }

            result = null;
            return false;
        }

        private static bool TryMerge(ulong value1, ulong value2, BinaryOperatorKind binaryOperatorKind, out ulong result)
        {
            switch (binaryOperatorKind)
            {
                case BinaryOperatorKind.Add:
                    result = value1 + value2;
                    return true;

                case BinaryOperatorKind.Subtract:
                    result = value1 - value2;
                    return true;

                case BinaryOperatorKind.Multiply:
                    result = value1 * value2;
                    return true;

                case BinaryOperatorKind.Divide:
                    if (value2 != 0)
                    {
                        result = value1 / value2;
                        return true;
                    }

                    break;

                case BinaryOperatorKind.And:
                    result = value1 & value2;
                    return true;

                case BinaryOperatorKind.Or:
                    result = value1 | value2;
                    return true;

                case BinaryOperatorKind.Remainder:
                    result = value1 % value2;
                    return true;

                case BinaryOperatorKind.Power:
                    result = (ulong)Math.Pow(value1, value2);
                    return true;

                case BinaryOperatorKind.LeftShift:
                    if ((uint)value2 == value2)
                    {
                        result = value1 << (int)value2;
                        return true;
                    }

                    break;

                case BinaryOperatorKind.RightShift:
                    if ((uint)value2 == value2)
                    {
                        result = value1 >> (int)value2;
                        return true;
                    }

                    break;

                case BinaryOperatorKind.ExclusiveOr:
                    result = value1 ^ value2;
                    return true;

                case BinaryOperatorKind.Equals:
                    result = value1 == value2 ? 1UL : 0UL;
                    return true;

                case BinaryOperatorKind.NotEquals:
                    result = value1 != value2 ? 1UL : 0UL;
                    return true;

                case BinaryOperatorKind.LessThan:
                    result = value1 < value2 ? 1UL : 0UL;
                    return true;

                case BinaryOperatorKind.LessThanOrEqual:
                    result = value1 <= value2 ? 1UL : 0UL;
                    return true;

                case BinaryOperatorKind.GreaterThan:
                    result = value1 > value2 ? 1UL : 0UL;
                    return true;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    result = value1 >= value2 ? 1UL : 0UL;
                    return true;
            }

            result = 0;
            return false;
        }

        private static bool TryMerge(double value1, double value2, BinaryOperatorKind binaryOperatorKind, out double result)
        {
            switch (binaryOperatorKind)
            {
                case BinaryOperatorKind.Add:
                    result = value1 + value2;
                    return true;

                case BinaryOperatorKind.Subtract:
                    result = value1 - value2;
                    return true;

                case BinaryOperatorKind.Multiply:
                    result = value1 * value2;
                    return true;

                case BinaryOperatorKind.Divide:
                    if (value2 != 0)
                    {
                        result = value1 / value2;
                        return true;
                    }

                    break;

                case BinaryOperatorKind.Remainder:
                    result = value1 % value2;
                    return true;

                case BinaryOperatorKind.Power:
                    result = Math.Pow(value1, value2);
                    return true;

                case BinaryOperatorKind.Equals:
                    result = value1 == value2 ? 1.0 : 0.0;
                    return true;

                case BinaryOperatorKind.NotEquals:
                    result = value1 != value2 ? 1.0 : 0.0;
                    return true;

                case BinaryOperatorKind.LessThan:
                    result = value1 < value2 ? 1.0 : 0.0;
                    return true;

                case BinaryOperatorKind.LessThanOrEqual:
                    result = value1 <= value2 ? 1.0 : 0.0;
                    return true;

                case BinaryOperatorKind.GreaterThan:
                    result = value1 > value2 ? 1.0 : 0.0;
                    return true;

                case BinaryOperatorKind.GreaterThanOrEqual:
                    result = value1 >= value2 ? 1.0 : 0.0;
                    return true;
            }

            result = 0;
            return false;
        }
    }
}
