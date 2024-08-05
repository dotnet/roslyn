// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ExtensionTypeEraser
    {
        internal static TypeSymbol TransformType(TypeSymbol type, out bool changed)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return TransformArrayType((ArrayTypeSymbol)type, out changed);
                case TypeKind.Pointer:
                    return TransformPointerType((PointerTypeSymbol)type, out changed);
                case TypeKind.FunctionPointer:
                    return TransformFunctionPointerType((FunctionPointerTypeSymbol)type, out changed);
                case TypeKind.TypeParameter:
                case TypeKind.Dynamic:
                    changed = false;
                    return type;
                case TypeKind.Submission:
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Delegate:
                case TypeKind.Enum:
                case TypeKind.Error:
                    return TransformExcludingSelf((NamedTypeSymbol)type, out changed);
                case TypeKind.Extension:
                    return TransformExtension((NamedTypeSymbol)type, out changed);
                default:
                    throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
            }
        }

        private static TypeSymbol TransformExtension(NamedTypeSymbol type, out bool changed)
        {
            changed = false;

            while (type.GetExtendedTypeNoUseSiteDiagnostics(null) is { } extended)
            {
                changed = true;

                if (extended is NamedTypeSymbol named)
                {
                    type = named;
                }
                else
                {
                    return TransformType(extended, changed: out _);
                }
            }

            TypeSymbol transformedExtended = TransformExcludingSelf(type, out bool changedExtended);
            if (changedExtended)
            {
                changed = true;
            }

            return transformedExtended;
        }

        private static TypeWithAnnotations TransformTypeWithAnnotations(TypeWithAnnotations type, out bool changed)
        {
            TypeSymbol transformedType = TransformType(type.Type, out changed);

            if (!changed)
            {
                return type;
            }

            return type.WithTypeAndModifiers(transformedType, type.CustomModifiers);
        }

        internal static NamedTypeSymbol TransformExcludingSelf(NamedTypeSymbol type, out bool changed)
        {
            changed = false;

            if (type.IsAnonymousType)
            {
                var anonymous = (AnonymousTypeManager.AnonymousTypeOrDelegatePublicSymbol)type;
                AnonymousTypeDescriptor descriptor = anonymous.TypeDescriptor;

                var fieldTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance(descriptor.Fields.Length);
                fieldTypes.AddRange(descriptor.Fields, static (f) => f.TypeWithAnnotations);

                for (int i = 0; i < descriptor.Fields.Length; i++)
                {
                    TypeWithAnnotations transformed = TransformTypeWithAnnotations(fieldTypes[i], out bool typeArgumentChanged);
                    if (typeArgumentChanged)
                    {
                        changed = true;
                        fieldTypes[i] = transformed;
                    }
                }

                anonymous = changed ? anonymous.WithTypeDescriptor(descriptor.WithNewFieldsTypes(fieldTypes.ToImmutable())) : anonymous;
                fieldTypes.Free();
                return anonymous;
            }

            if (!type.IsGenericType)
            {
                return type;
            }

            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            type.GetAllTypeArgumentsNoUseSiteDiagnostics(allTypeArguments);

            for (int i = 0; i < allTypeArguments.Count; i++)
            {
                TypeWithAnnotations transformed = TransformTypeWithAnnotations(allTypeArguments[i], out bool typeArgumentChanged);
                if (typeArgumentChanged)
                {
                    changed = true;
                    allTypeArguments[i] = transformed;
                }
            }

            NamedTypeSymbol result = changed ? type.WithTypeArguments(allTypeArguments.ToImmutable()) : type;
            allTypeArguments.Free();
            return result;
        }

        private static ArrayTypeSymbol TransformArrayType(ArrayTypeSymbol type, out bool changed)
        {
            TypeWithAnnotations transformed = TransformTypeWithAnnotations(type.ElementTypeWithAnnotations, out changed);
            if (changed)
            {
                return type.WithElementType(transformed);
            }

            return type;
        }

        private static PointerTypeSymbol TransformPointerType(PointerTypeSymbol type, out bool changed)
        {
            TypeWithAnnotations transformed = TransformTypeWithAnnotations(type.PointedAtTypeWithAnnotations, out changed);
            if (changed)
            {
                return type.WithPointedAtType(transformed);
            }

            return type;
        }

        private static FunctionPointerTypeSymbol TransformFunctionPointerType(FunctionPointerTypeSymbol type, out bool changed)
        {
            var transformedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            changed = false;

            if (type.Signature.ParameterCount > 0)
            {
                var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(type.Signature.ParameterCount);
                foreach (var param in type.Signature.Parameters)
                {
                    TypeWithAnnotations transformedParam = TransformTypeWithAnnotations(param.TypeWithAnnotations, out bool paramChanged);
                    if (paramChanged)
                    {
                        changed = true;
                    }

                    builder.Add(transformedParam);
                }

                if (changed)
                {
                    transformedParameterTypes = builder.ToImmutableAndFree();
                }
                else
                {
                    transformedParameterTypes = type.Signature.ParameterTypesWithAnnotations;
                    builder.Free();
                }
            }

            TypeWithAnnotations transformedReturnType = TransformTypeWithAnnotations(type.Signature.ReturnTypeWithAnnotations, out bool returnChanged);

            if (changed || returnChanged)
            {
                changed = true;
                return type.SubstituteTypeSymbol(transformedReturnType, transformedParameterTypes, refCustomModifiers: default, paramRefCustomModifiers: default);
            }
            else
            {
                return type;
            }
        }
    }
}
