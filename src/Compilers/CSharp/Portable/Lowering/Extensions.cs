// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class OperatorKindExtensions
    {
        public static RefKind RefKinds(this ImmutableArray<RefKind> ArgumentRefKinds, int index)
        {
            if (!ArgumentRefKinds.IsDefault && index < ArgumentRefKinds.Length)
            {
                return ArgumentRefKinds[index];
            }
            else
            {
                return RefKind.None;
            }
        }
    }

    internal static partial class BoundExpressionExtensions
    {
        public static bool NullableAlwaysHasValue(this BoundExpression expr)
        {
            Debug.Assert(expr != null);
            if ((object)expr.Type == null)
            {
                return false;
            }

            if (expr.Type.IsDynamic())
            {
                return false;
            }

            if (!expr.Type.IsNullableType())
            {
                return true;
            }

            // new int?(123) always has a value:
            if (expr.Kind == BoundKind.ObjectCreationExpression)
            {
                var creation = (BoundObjectCreationExpression)expr;
                return creation.Constructor.ParameterCount != 0;
            }
            else if (expr.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)expr;
                switch (conversion.ConversionKind)
                {
                    case ConversionKind.ImplicitNullable:
                    case ConversionKind.ExplicitNullable:
                        // A conversion from X? to Y? will be non-null if the operand is non-null,
                        // so simply recurse.
                        return conversion.Operand.NullableAlwaysHasValue();
                    case ConversionKind.ImplicitEnumeration:
                        // The C# specification categorizes conversion from literal zero to nullable enum as 
                        // an Implicit Enumeration Conversion. 
                        return conversion.Operand.NullableAlwaysHasValue();
                }
            }

            return false;
        }

        public static bool NullableNeverHasValue(this BoundExpression expr)
        {
            Debug.Assert(expr != null);

            if ((object)expr.Type == null && expr.ConstantValue == ConstantValue.Null)
            {
                return true;
            }

            if ((object)expr.Type == null || !expr.Type.IsNullableType())
            {
                return false;
            }

            // "default(int?)" and "default" never have a value.
            if (expr is BoundDefaultLiteral || expr is BoundDefaultExpression)
            {
                return true;
            }

            // "new int?()" never has a value, but "new int?(x)" always does.
            if (expr.Kind == BoundKind.ObjectCreationExpression)
            {
                var creation = (BoundObjectCreationExpression)expr;
                return creation.Constructor.ParameterCount == 0;
            }

            if (expr.Kind == BoundKind.Conversion)
            {
                var conversion = (BoundConversion)expr;
                switch (conversion.ConversionKind)
                {
                    case ConversionKind.NullLiteral:
                        // Any null literal conversion is a conversion from the literal null to
                        // a nullable value type; obviously it never has a value.
                        return true;
                    case ConversionKind.DefaultLiteral:
                        // Any default literal to a nullable value type never has a value. 
                        return true;
                    case ConversionKind.ImplicitNullable:
                    case ConversionKind.ExplicitNullable:
                        // A conversion from X? to Y? will be null if the operand is null,
                        // so simply recurse.
                        return conversion.Operand.NullableNeverHasValue();
                }
            }

            // UNDONE: We could be more sophisticated here. For example, most lifted operators that have 
            // UNDONE: a known-to-be-null operand are also known to be null.

            return false;
        }

        public static bool IsNullableNonBoolean(this BoundExpression expr)
        {
            Debug.Assert(expr != null);
            if (expr.Type.IsNullableType() && expr.Type.GetNullableUnderlyingType().SpecialType != SpecialType.System_Boolean)
                return true;
            return false;
        }
    }
}
