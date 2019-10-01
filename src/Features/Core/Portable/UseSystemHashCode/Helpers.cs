// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseSystemHashCode
{
    internal static class Helpers
    {
        public static bool IsLocalReference(IOperation value, ILocalSymbol accumulatorVariable)
           => Helpers.Unwrap(value) is ILocalReferenceOperation localReference && accumulatorVariable.Equals(localReference.Local);


        public static bool OverridesSystemObject(IMethodSymbol objectGetHashCodeMethod, IMethodSymbol method)
        {
            for (var current = method; current != null; current = current.OverriddenMethod)
            {
                if (Equals(objectGetHashCodeMethod, current))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Matches positive and negative numeric literals.
        /// </summary>
        public static bool IsLiteralNumber(IOperation value)
        {
            value = Unwrap(value);
            return value is IUnaryOperation unary
                ? unary.OperatorKind == UnaryOperatorKind.Minus && IsLiteralNumber(unary.Operand)
                : value.IsNumericLiteral();
        }

        public static IOperation Unwrap(IOperation value)
        {
            // ReSharper and VS generate different patterns for parentheses (which also depends on
            // the particular parentheses settings the user has enabled).  So just descend through
            // any parentheses we see to create a uniform view of the code.
            //
            // Also, lots of operations in a GetHashCode impl will involve conversions all over the
            // place (for example, some computations happen in 64bit, but convert to/from 32bit
            // along the way).  So we descend through conversions as well to create a uniform view
            // of things.
            while (true)
            {
                if (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }
                else if (value is IParenthesizedOperation parenthesized)
                {
                    value = parenthesized.Operand;
                }
                else
                {
                    return value;
                }
            }
        }

    }
}
