// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
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
        public static ConstantValue GetSymConstantValue(ITypeSymbolInternal type, object symValue)
        {
            if (type.TypeKind == TypeKind.Enum)
            {
                type = ((INamedTypeSymbolInternal)type).EnumUnderlyingType!;
            }

            return (type.SpecialType, symValue) switch
            {
                (SpecialType.System_Boolean, short shortVal) => ConstantValue.Create(shortVal != 0),
                (SpecialType.System_Byte, short shortVal) when unchecked((byte)shortVal) == shortVal => ConstantValue.Create((byte)shortVal),
                (SpecialType.System_SByte, short shortVal) when unchecked((sbyte)shortVal) == shortVal => ConstantValue.Create((sbyte)shortVal),
                (SpecialType.System_Int16, short shortVal) => ConstantValue.Create(shortVal),
                (SpecialType.System_Char, ushort ushortVal) => ConstantValue.Create((char)ushortVal),
                (SpecialType.System_UInt16, ushort ushortVal) => ConstantValue.Create(ushortVal),
                (SpecialType.System_Int32, int intVal) => ConstantValue.Create(intVal),
                (SpecialType.System_UInt32, uint uintVal) => ConstantValue.Create(uintVal),
                (SpecialType.System_Int64, long longVal) => ConstantValue.Create(longVal),
                (SpecialType.System_UInt64, ulong ulongVal) => ConstantValue.Create(ulongVal),
                (SpecialType.System_Single, float floatVal) => ConstantValue.Create(floatVal),
                (SpecialType.System_Double, double doubleVal) => ConstantValue.Create(doubleVal),
                (SpecialType.System_String, 0) => ConstantValue.Null,
                (SpecialType.System_String, null) => ConstantValue.Create(string.Empty),
                (SpecialType.System_String, string str) => ConstantValue.Create(str),
                (SpecialType.System_Object, 0) => ConstantValue.Null,
                (SpecialType.System_Decimal, decimal decimalValue) => ConstantValue.Create(decimalValue),
                (SpecialType.System_DateTime, double doubleVal) => ConstantValue.Create(DateTimeUtilities.ToDateTime(doubleVal)),
                (SpecialType.None, 0) when type.IsReferenceType => ConstantValue.Null,
                _ => ConstantValue.Bad,
            };
        }
    }
}

