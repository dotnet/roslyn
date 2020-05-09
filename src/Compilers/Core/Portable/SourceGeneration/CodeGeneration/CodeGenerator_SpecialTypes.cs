// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.SourceGeneration
{
    internal static partial class CodeGenerator
    {
        public static readonly INamedTypeSymbol System_Object = CreateSpecialType(CodeAnalysis.SpecialType.System_Object, typeof(Object));
        public static readonly INamedTypeSymbol System_Enum = CreateSpecialType(CodeAnalysis.SpecialType.System_Enum, typeof(Enum));
        public static readonly INamedTypeSymbol System_MulticastDelegate = CreateSpecialType(CodeAnalysis.SpecialType.System_MulticastDelegate, typeof(MulticastDelegate));
        public static readonly INamedTypeSymbol System_Delegate = CreateSpecialType(CodeAnalysis.SpecialType.System_Delegate, typeof(Delegate));
        public static readonly INamedTypeSymbol System_ValueType = CreateSpecialType(CodeAnalysis.SpecialType.System_ValueType, typeof(ValueType));
        public static readonly INamedTypeSymbol System_Void = CreateSpecialType(CodeAnalysis.SpecialType.System_Void, typeof(void));
        public static readonly INamedTypeSymbol System_Boolean = CreateSpecialType(CodeAnalysis.SpecialType.System_Boolean, typeof(Boolean));
        public static readonly INamedTypeSymbol System_Char = CreateSpecialType(CodeAnalysis.SpecialType.System_Char, typeof(Char));
        public static readonly INamedTypeSymbol System_SByte = CreateSpecialType(CodeAnalysis.SpecialType.System_SByte, typeof(SByte));
        public static readonly INamedTypeSymbol System_Byte = CreateSpecialType(CodeAnalysis.SpecialType.System_Byte, typeof(Byte));
        public static readonly INamedTypeSymbol System_Int16 = CreateSpecialType(CodeAnalysis.SpecialType.System_Int16, typeof(Int16));
        public static readonly INamedTypeSymbol System_UInt16 = CreateSpecialType(CodeAnalysis.SpecialType.System_UInt16, typeof(UInt16));
        public static readonly INamedTypeSymbol System_Int32 = CreateSpecialType(CodeAnalysis.SpecialType.System_Int32, typeof(Int32));
        public static readonly INamedTypeSymbol System_UInt32 = CreateSpecialType(CodeAnalysis.SpecialType.System_UInt32, typeof(UInt32));
        public static readonly INamedTypeSymbol System_Int64 = CreateSpecialType(CodeAnalysis.SpecialType.System_Int64, typeof(Int64));
        public static readonly INamedTypeSymbol System_UInt64 = CreateSpecialType(CodeAnalysis.SpecialType.System_UInt64, typeof(UInt64));
        public static readonly INamedTypeSymbol System_Decimal = CreateSpecialType(CodeAnalysis.SpecialType.System_Decimal, typeof(Decimal));
        public static readonly INamedTypeSymbol System_Single = CreateSpecialType(CodeAnalysis.SpecialType.System_Single, typeof(Single));
        public static readonly INamedTypeSymbol System_Double = CreateSpecialType(CodeAnalysis.SpecialType.System_Double, typeof(Double));
        public static readonly INamedTypeSymbol System_String = CreateSpecialType(CodeAnalysis.SpecialType.System_String, typeof(String));
        public static readonly INamedTypeSymbol System_IntPtr = CreateSpecialType(CodeAnalysis.SpecialType.System_IntPtr, typeof(IntPtr));
        public static readonly INamedTypeSymbol System_UIntPtr = CreateSpecialType(CodeAnalysis.SpecialType.System_UIntPtr, typeof(UIntPtr));
        public static readonly INamedTypeSymbol System_Array = CreateSpecialType(CodeAnalysis.SpecialType.System_Array, typeof(Array));
        public static readonly INamedTypeSymbol System_Collections_IEnumerable = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_IEnumerable, typeof(IEnumerable));
        public static readonly INamedTypeSymbol System_Collections_Generic_IEnumerable_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerable_T, typeof(IEnumerable<>));
        public static readonly INamedTypeSymbol System_Collections_Generic_IList_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_IList_T, typeof(IList<>));
        public static readonly INamedTypeSymbol System_Collections_Generic_ICollection_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_ICollection_T, typeof(ICollection<>));
        public static readonly INamedTypeSymbol System_Collections_IEnumerator = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_IEnumerator, typeof(IEnumerator));
        public static readonly INamedTypeSymbol System_Collections_Generic_IEnumerator_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerator_T, typeof(IEnumerator<>));
        public static readonly INamedTypeSymbol System_Collections_Generic_IReadOnlyCollection_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_IReadOnlyCollection_T, typeof(IReadOnlyCollection<>));
        public static readonly INamedTypeSymbol System_Collections_Generic_IReadOnlyList_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Collections_Generic_IReadOnlyList_T, typeof(IReadOnlyList<>));
        public static readonly INamedTypeSymbol System_Nullable_T = CreateSpecialType(CodeAnalysis.SpecialType.System_Nullable_T, typeof(Nullable<>));
        public static readonly INamedTypeSymbol System_DateTime = CreateSpecialType(CodeAnalysis.SpecialType.System_DateTime, typeof(DateTime));
        public static readonly INamedTypeSymbol System_Runtime_CompilerServices_IsVolatile = CreateSpecialType(CodeAnalysis.SpecialType.System_Runtime_CompilerServices_IsVolatile, typeof(IsVolatile));
        public static readonly INamedTypeSymbol System_IDisposable = CreateSpecialType(CodeAnalysis.SpecialType.System_IDisposable, typeof(IDisposable));
        public static readonly INamedTypeSymbol System_TypedReference = CreateSpecialType(CodeAnalysis.SpecialType.System_TypedReference, typeof(TypedReference));
        public static readonly INamedTypeSymbol System_ArgIterator = CreateSpecialType(CodeAnalysis.SpecialType.System_ArgIterator, TypeKind.Struct, "System", "ArgIterator");
        public static readonly INamedTypeSymbol System_RuntimeArgumentHandle = CreateSpecialType(CodeAnalysis.SpecialType.System_RuntimeArgumentHandle, typeof(RuntimeArgumentHandle));
        public static readonly INamedTypeSymbol System_RuntimeFieldHandle = CreateSpecialType(CodeAnalysis.SpecialType.System_RuntimeFieldHandle, typeof(RuntimeFieldHandle));
        public static readonly INamedTypeSymbol System_RuntimeMethodHandle = CreateSpecialType(CodeAnalysis.SpecialType.System_RuntimeMethodHandle, typeof(RuntimeMethodHandle));
        public static readonly INamedTypeSymbol System_RuntimeTypeHandle = CreateSpecialType(CodeAnalysis.SpecialType.System_RuntimeTypeHandle, typeof(RuntimeTypeHandle));
        public static readonly INamedTypeSymbol System_IAsyncResult = CreateSpecialType(CodeAnalysis.SpecialType.System_IAsyncResult, typeof(IAsyncResult));
        public static readonly INamedTypeSymbol System_AsyncCallback = CreateSpecialType(CodeAnalysis.SpecialType.System_AsyncCallback, typeof(AsyncCallback));
        public static readonly INamedTypeSymbol System_Runtime_CompilerServices_RuntimeFeature = CreateSpecialType(CodeAnalysis.SpecialType.System_Runtime_CompilerServices_RuntimeFeature, TypeKind.Class, "System.Runtime.CompilerServices", "RuntimeFeature");

        static CodeGenerator()
        {
            if (CodeAnalysis.SpecialType.Count != CodeAnalysis.SpecialType.System_Runtime_CompilerServices_RuntimeFeature)
                throw new NotImplementedException("Must update code generator with new special type");
        }

        private static INamedTypeSymbol CreateSpecialType(
            SpecialType specialType, TypeKind typeKind, string ns, string name)
        {
            return new NamedTypeSymbol(
                specialType,
                typeKind,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                name,
                typeArguments: default,
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements: default,
                delegateInvokeMethod: null,
                enumUnderlyingType: null,
                nullableAnnotation: default,
                containingSymbol: GetContainingNamespace(ns.Split('.')));
        }

        private static INamedTypeSymbol CreateSpecialType(SpecialType specialType, Type type)
        {
            var name = type.Name;

            var backtick = name.IndexOf('`');
            if (backtick >= 0)
                name = name.Substring(0, backtick);

            var typeKind =
                type.IsInterface ? TypeKind.Interface :
                type.IsValueType ? TypeKind.Struct : TypeKind.Class;

            return new NamedTypeSymbol(
                specialType,
                typeKind,
                attributes: default,
                declaredAccessibility: default,
                modifiers: default,
                name,
                typeArguments: GetTypeArguments(type.GetGenericArguments()),
                baseType: null,
                interfaces: default,
                members: default,
                tupleElements: default,
                delegateInvokeMethod: null,
                enumUnderlyingType: null,
                nullableAnnotation: default,
                containingSymbol: GetContainingNamespace(type.Namespace!.Split('.')));

            static ImmutableArray<ITypeSymbol> GetTypeArguments(Type[]? arguments)
            {
                if (arguments == null)
                    return ImmutableArray<ITypeSymbol>.Empty;

                var builder = ArrayBuilder<ITypeSymbol>.GetInstance();

                foreach (var arg in arguments)
                    builder.Add(TypeParameter(arg.Name));

                return builder.ToImmutableAndFree();
            }
        }

        private static INamespaceSymbol GetContainingNamespace(string[] namespaces)
        {
            return GetContainingNamespaceRecurse(namespaces, namespaces.Length - 1);

            static INamespaceSymbol GetContainingNamespaceRecurse(string[] namespaces, int index)
            {
                if (index == -1)
                    return GlobalNamespace();

                return Namespace(
                    namespaces[index],
                    containingSymbol: GetContainingNamespaceRecurse(namespaces, index - 1));
            }
        }

        public static INamedTypeSymbol SpecialType(
            SpecialType specialType,
            ImmutableArray<ITypeSymbol> typeArguments = default,
            NullableAnnotation nullableAnnotation = default)
        {
            var type = GetSpecialType(specialType);
            return typeArguments != default || nullableAnnotation != default
                ? type.With(typeArguments: typeArguments, nullableAnnotation: nullableAnnotation)
                : type;

            static INamedTypeSymbol GetSpecialType(SpecialType specialType)
                => specialType switch
                {
                    CodeAnalysis.SpecialType.System_Object => System_Object,
                    CodeAnalysis.SpecialType.System_Enum => System_Enum,
                    CodeAnalysis.SpecialType.System_MulticastDelegate => System_MulticastDelegate,
                    CodeAnalysis.SpecialType.System_Delegate => System_Delegate,
                    CodeAnalysis.SpecialType.System_ValueType => System_ValueType,
                    CodeAnalysis.SpecialType.System_Void => System_Void,
                    CodeAnalysis.SpecialType.System_Boolean => System_Boolean,
                    CodeAnalysis.SpecialType.System_Char => System_Char,
                    CodeAnalysis.SpecialType.System_SByte => System_SByte,
                    CodeAnalysis.SpecialType.System_Byte => System_Byte,
                    CodeAnalysis.SpecialType.System_Int16 => System_Int16,
                    CodeAnalysis.SpecialType.System_UInt16 => System_UInt16,
                    CodeAnalysis.SpecialType.System_Int32 => System_Int32,
                    CodeAnalysis.SpecialType.System_UInt32 => System_UInt32,
                    CodeAnalysis.SpecialType.System_Int64 => System_Int64,
                    CodeAnalysis.SpecialType.System_UInt64 => System_UInt64,
                    CodeAnalysis.SpecialType.System_Decimal => System_Decimal,
                    CodeAnalysis.SpecialType.System_Single => System_Single,
                    CodeAnalysis.SpecialType.System_Double => System_Double,
                    CodeAnalysis.SpecialType.System_String => System_String,
                    CodeAnalysis.SpecialType.System_IntPtr => System_IntPtr,
                    CodeAnalysis.SpecialType.System_UIntPtr => System_UIntPtr,
                    CodeAnalysis.SpecialType.System_Array => System_Array,
                    CodeAnalysis.SpecialType.System_Collections_IEnumerable => System_Collections_IEnumerable,
                    CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerable_T => System_Collections_Generic_IEnumerable_T,
                    CodeAnalysis.SpecialType.System_Collections_Generic_IList_T => System_Collections_Generic_IList_T,
                    CodeAnalysis.SpecialType.System_Collections_Generic_ICollection_T => System_Collections_Generic_ICollection_T,
                    CodeAnalysis.SpecialType.System_Collections_IEnumerator => System_Collections_IEnumerator,
                    CodeAnalysis.SpecialType.System_Collections_Generic_IEnumerator_T => System_Collections_Generic_IEnumerator_T,
                    CodeAnalysis.SpecialType.System_Collections_Generic_IReadOnlyList_T => System_Collections_Generic_IReadOnlyList_T,
                    CodeAnalysis.SpecialType.System_Collections_Generic_IReadOnlyCollection_T => System_Collections_Generic_IReadOnlyCollection_T,
                    CodeAnalysis.SpecialType.System_Nullable_T => System_Nullable_T,
                    CodeAnalysis.SpecialType.System_DateTime => System_DateTime,
                    CodeAnalysis.SpecialType.System_Runtime_CompilerServices_IsVolatile => System_Runtime_CompilerServices_IsVolatile,
                    CodeAnalysis.SpecialType.System_IDisposable => System_IDisposable,
                    CodeAnalysis.SpecialType.System_TypedReference => System_TypedReference,
                    CodeAnalysis.SpecialType.System_ArgIterator => System_ArgIterator,
                    CodeAnalysis.SpecialType.System_RuntimeArgumentHandle => System_RuntimeArgumentHandle,
                    CodeAnalysis.SpecialType.System_RuntimeFieldHandle => System_RuntimeFieldHandle,
                    CodeAnalysis.SpecialType.System_RuntimeMethodHandle => System_RuntimeMethodHandle,
                    CodeAnalysis.SpecialType.System_RuntimeTypeHandle => System_RuntimeTypeHandle,
                    CodeAnalysis.SpecialType.System_IAsyncResult => System_IAsyncResult,
                    CodeAnalysis.SpecialType.System_AsyncCallback => System_AsyncCallback,
                    CodeAnalysis.SpecialType.System_Runtime_CompilerServices_RuntimeFeature => System_Runtime_CompilerServices_RuntimeFeature,
                    _ => throw new ArgumentException("Invalid SpecialType: " + specialType),
                };
        }
    }
}
