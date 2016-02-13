// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class PdbHelpers
    {
        /// <remarks>
        /// Test helper.
        /// </remarks>
        internal static void GetAllScopes(this ISymUnmanagedMethod method, ArrayBuilder<ISymUnmanagedScope> builder)
        {
            var unused = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            GetAllScopes(method, builder, unused, offset: -1, isScopeEndInclusive: false);
            unused.Free();
        }

        internal static void GetAllScopes(
            this ISymUnmanagedMethod method,
            ArrayBuilder<ISymUnmanagedScope> allScopes,
            ArrayBuilder<ISymUnmanagedScope> containingScopes,
            int offset,
            bool isScopeEndInclusive)
        {
            GetAllScopes(method.GetRootScope(), allScopes, containingScopes, offset, isScopeEndInclusive);
        }

        private static void GetAllScopes(
            ISymUnmanagedScope root,
            ArrayBuilder<ISymUnmanagedScope> allScopes,
            ArrayBuilder<ISymUnmanagedScope> containingScopes,
            int offset,
            bool isScopeEndInclusive)
        {
            var stack = ArrayBuilder<ISymUnmanagedScope>.GetInstance();
            stack.Push(root);

            while (stack.Any())
            {
                var scope = stack.Pop();
                allScopes.Add(scope);
                if (offset >= 0 && scope.IsInScope(offset, isScopeEndInclusive))
                {
                    containingScopes.Add(scope);
                }

                foreach (var nested in scope.GetScopes())
                {
                    stack.Push(nested);
                }
            }

            stack.Free();
        }

        /// <summary>
        /// Translates the value of a constant returned by <see cref="ISymUnmanagedConstant.GetValue(out object)"/> to a <see cref="ConstantValue"/>.
        /// </summary>
        public static ConstantValue GetSymConstantValue(ITypeSymbol type, object symValue)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                type = ((INamedTypeSymbol)type).EnumUnderlyingType;
            }

            short shortValue;
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                    if (!(symValue is short))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((short)symValue != 0);

                case SpecialType.System_Byte:
                    if (!(symValue is short))
                    {
                        return ConstantValue.Bad;
                    }

                    shortValue = (short)symValue;
                    if (unchecked((byte)shortValue) != shortValue)
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((byte)shortValue);

                case SpecialType.System_SByte:
                    if (!(symValue is short))
                    {
                        return ConstantValue.Bad;
                    }

                    shortValue = (short)symValue;
                    if (unchecked((sbyte)shortValue) != shortValue)
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((sbyte)shortValue);

                case SpecialType.System_Int16:
                    if (!(symValue is short))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((short)symValue);

                case SpecialType.System_Char:
                    if (!(symValue is ushort))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((char)(ushort)symValue);

                case SpecialType.System_UInt16:
                    if (!(symValue is ushort))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((ushort)symValue);

                case SpecialType.System_Int32:
                    if (!(symValue is int))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((int)symValue);

                case SpecialType.System_UInt32:
                    if (!(symValue is uint))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((uint)symValue);

                case SpecialType.System_Int64:
                    if (!(symValue is long))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((long)symValue);

                case SpecialType.System_UInt64:
                    if (!(symValue is ulong))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((ulong)symValue);

                case SpecialType.System_Single:
                    if (!(symValue is float))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((float)symValue);

                case SpecialType.System_Double:
                    if (!(symValue is double))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((double)symValue);

                case SpecialType.System_String:
                    if (symValue is int && (int)symValue == 0)
                    {
                        return ConstantValue.Null;
                    }

                    if (symValue == null)
                    {
                        return ConstantValue.Create(string.Empty);
                    }

                    var str = symValue as string;
                    if (str == null)
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create(str);

                case SpecialType.System_Object:
                    if (symValue is int && (int)symValue == 0)
                    {
                        return ConstantValue.Null;
                    }

                    return ConstantValue.Bad;

                case SpecialType.System_Decimal:
                    if (!(symValue is decimal))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create((decimal)symValue);

                case SpecialType.System_DateTime:
                    if (!(symValue is double))
                    {
                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Create(DateTimeUtilities.ToDateTime((double)symValue));

                case SpecialType.None:
                    if (type.IsReferenceType)
                    {
                        if (symValue is int && (int)symValue == 0)
                        {
                            return ConstantValue.Null;
                        }

                        return ConstantValue.Bad;
                    }

                    return ConstantValue.Bad;

                default:
                    return ConstantValue.Bad;
            }
        }
    }
}

