// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
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
                if (offset >= 0 && IsInScope(scope, offset, isScopeEndInclusive))
                {
                    containingScopes.Add(scope);
                }

                foreach (var nested in scope.GetChildren())
                {
                    stack.Push(nested);
                }
            }

            stack.Free();
        }

        private static bool IsInScope(ISymUnmanagedScope scope, int offset, bool isEndInclusive)
        {
            int startOffset = scope.GetStartOffset();
            if (offset < startOffset)
            {
                return false;
            }

            int endOffset = scope.GetEndOffset();

            // In PDBs emitted by VB the end offset is inclusive, 
            // in PDBs emitted by C# the end offset is exclusive.
            return isEndInclusive ? offset <= endOffset : offset < endOffset;
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

            return type.SpecialType switch
            {
                SpecialType.System_Boolean => symValue is short shortVal
                    ? ConstantValue.Create(shortVal != 0)
                    : ConstantValue.Bad,

                SpecialType.System_Byte => symValue is short shortVal && unchecked((byte)shortVal) == shortVal
                    ? ConstantValue.Create((byte)shortVal)
                    : ConstantValue.Bad,

                SpecialType.System_SByte => symValue is short shortVal && unchecked((sbyte)shortVal) == shortVal
                    ? ConstantValue.Create((sbyte)shortVal)
                    : ConstantValue.Bad,

                SpecialType.System_Int16 => symValue is short shortVal
                    ? ConstantValue.Create(shortVal)
                    : ConstantValue.Bad,

                SpecialType.System_Char => symValue is ushort ushortVal
                    ? ConstantValue.Create((char)ushortVal)
                    : ConstantValue.Bad,

                SpecialType.System_UInt16 => symValue is ushort ushortVal
                    ? ConstantValue.Create(ushortVal)
                    : ConstantValue.Bad,

                SpecialType.System_Int32 => symValue is int intVal
                    ? ConstantValue.Create(intVal)
                    : ConstantValue.Bad,

                SpecialType.System_UInt32 => symValue is uint uintVal
                    ? ConstantValue.Create(uintVal)
                    : ConstantValue.Bad,

                SpecialType.System_Int64 => symValue is long longVal
                    ? ConstantValue.Create(longVal)
                    : ConstantValue.Bad,

                SpecialType.System_UInt64 => symValue is ulong ulongVal
                    ? ConstantValue.Create(ulongVal)
                    : ConstantValue.Bad,

                SpecialType.System_Single => symValue is float floatVal
                    ? ConstantValue.Create(floatVal)
                    : ConstantValue.Bad,

                SpecialType.System_Double => symValue is double doubleVal
                    ? ConstantValue.Create(doubleVal)
                    : ConstantValue.Bad,

                SpecialType.System_String => symValue switch
                {
                    0 => ConstantValue.Null,
                    null => ConstantValue.Create(string.Empty),
                    string str => ConstantValue.Create(str),
                    _ => ConstantValue.Bad,
                },

                SpecialType.System_Object => symValue is 0
                    ? ConstantValue.Null
                    : ConstantValue.Bad,

                SpecialType.System_Decimal => symValue is decimal decimalValue
                    ? ConstantValue.Create(decimalValue)
                    : ConstantValue.Bad,

                SpecialType.System_DateTime => symValue is double doubleVal
                    ? ConstantValue.Create(DateTimeUtilities.ToDateTime(doubleVal))
                    : ConstantValue.Bad,

                SpecialType.None => type.IsReferenceType && symValue is 0
                    ? ConstantValue.Null
                    : ConstantValue.Bad,

                _ => ConstantValue.Bad,
            };
        }
    }
}

