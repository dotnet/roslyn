// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal struct NativeIntegerTypeDecoder
    {
        internal static TypeSymbol TransformType(TypeSymbol type, EntityHandle handle, PEModuleSymbol containingModule, TypeSymbol? containingType)
        {
            // Note: We avoid any cycles when loading members of System.Runtime.CompilerServices.RuntimeFeature
            if (containingType?.SpecialType == SpecialType.System_Runtime_CompilerServices_RuntimeFeature
                || type.ContainingAssembly?.RuntimeSupportsNumericIntPtr == true)
            {
                return type;
            }

            return containingModule.Module.HasNativeIntegerAttribute(handle, out var transformFlags) ?
                TransformType(type, transformFlags) :
                type;
        }

        internal static TypeSymbol TransformType(TypeSymbol type, ImmutableArray<bool> transformFlags)
        {
            var decoder = new NativeIntegerTypeDecoder(transformFlags);
            try
            {
                var result = decoder.TransformType(type);
                if (decoder._hitErrorType)
                {
                    // If we failed to decode because there was an error type involved, marking the
                    // metadata as unsupported means that we'll cover up the error that would otherwise
                    // be reported for the type. This would likely lead to a worse error message as we
                    // would just report a BindToBogus, so return the type unchanged.
                    Debug.Assert(type.ContainsErrorType());
                    Debug.Assert(result is null);
                    return type;
                }
                else if (decoder._index == transformFlags.Length)
                {
                    Debug.Assert(result is object);
                    return result;
                }
                else
                {
                    return new UnsupportedMetadataTypeSymbol();
                }
            }
            catch (UnsupportedSignatureContent)
            {
                return new UnsupportedMetadataTypeSymbol();
            }
        }

        private readonly ImmutableArray<bool> _transformFlags;
        private int _index;
        private bool _hitErrorType;

        private NativeIntegerTypeDecoder(ImmutableArray<bool> transformFlags)
        {
            _transformFlags = transformFlags;
            _index = 0;
            _hitErrorType = false;
        }

        private TypeWithAnnotations? TransformTypeWithAnnotations(TypeWithAnnotations type)
        {
            if (TransformType(type.Type) is { } transformedType)
            {
                return type.WithTypeAndModifiers(transformedType, type.CustomModifiers);
            }

            return null;
        }

        private TypeSymbol? TransformType(TypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Array:
                    return TransformArrayType((ArrayTypeSymbol)type);
                case TypeKind.Pointer:
                    return TransformPointerType((PointerTypeSymbol)type);
                case TypeKind.FunctionPointer:
                    return TransformFunctionPointerType((FunctionPointerTypeSymbol)type);
                case TypeKind.TypeParameter:
                case TypeKind.Dynamic:
                    return type;
                case TypeKind.Class:
                case TypeKind.Struct:
                case TypeKind.Interface:
                case TypeKind.Delegate:
                case TypeKind.Enum:
                    return TransformNamedType((NamedTypeSymbol)type);
                default:
                    Debug.Assert(type.TypeKind == TypeKind.Error);
                    _hitErrorType = true;
                    return null;
            }
        }

        private NamedTypeSymbol? TransformNamedType(NamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                        if (_index >= _transformFlags.Length)
                        {
                            throw new UnsupportedSignatureContent();
                        }
                        return (_transformFlags[_index++], type.IsNativeIntegerWrapperType) switch
                        {
                            (false, true) => type.NativeIntegerUnderlyingType,
                            (true, false) => type.AsNativeInteger(),
                            _ => type,
                        };
                }
            }

            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            type.GetAllTypeArgumentsNoUseSiteDiagnostics(allTypeArguments);

            bool haveChanges = false;
            for (int i = 0; i < allTypeArguments.Count; i++)
            {
                TypeWithAnnotations oldTypeArgument = allTypeArguments[i];
                if (TransformTypeWithAnnotations(oldTypeArgument) is not { } newTypeArgument)
                {
                    return null;
                }

                if (!oldTypeArgument.IsSameAs(newTypeArgument))
                {
                    allTypeArguments[i] = newTypeArgument;
                    haveChanges = true;
                }
            }

            NamedTypeSymbol result = haveChanges ? type.WithTypeArguments(allTypeArguments.ToImmutable()) : type;
            allTypeArguments.Free();
            return result;
        }

        private ArrayTypeSymbol? TransformArrayType(ArrayTypeSymbol type)
        {
            if (TransformTypeWithAnnotations(type.ElementTypeWithAnnotations) is { } elementType)
            {
                return type.WithElementType(elementType);
            }

            return null;
        }

        private PointerTypeSymbol? TransformPointerType(PointerTypeSymbol type)
        {
            if (TransformTypeWithAnnotations(type.PointedAtTypeWithAnnotations) is { } pointedAtType)
            {
                return type.WithPointedAtType(pointedAtType);
            }

            return null;
        }

        private FunctionPointerTypeSymbol? TransformFunctionPointerType(FunctionPointerTypeSymbol type)
        {
            if (TransformTypeWithAnnotations(type.Signature.ReturnTypeWithAnnotations) is not { } transformedReturnType)
            {
                return null;
            }

            var transformedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            var paramsModified = false;

            if (type.Signature.ParameterCount > 0)
            {
                var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(type.Signature.ParameterCount);
                foreach (var param in type.Signature.Parameters)
                {
                    if (TransformTypeWithAnnotations(param.TypeWithAnnotations) is not { } transformedParam)
                    {
                        return null;
                    }

                    paramsModified = paramsModified || !transformedParam.IsSameAs(param.TypeWithAnnotations);
                    builder.Add(transformedParam);
                }

                if (paramsModified)
                {
                    transformedParameterTypes = builder.ToImmutableAndFree();
                }
                else
                {
                    transformedParameterTypes = type.Signature.ParameterTypesWithAnnotations;
                    builder.Free();
                }
            }

            if (paramsModified || !transformedReturnType.IsSameAs(type.Signature.ReturnTypeWithAnnotations))
            {
                return type.SubstituteTypeSymbol(transformedReturnType, transformedParameterTypes, refCustomModifiers: default, paramRefCustomModifiers: default);
            }
            else
            {
                return type;
            }
        }
    }
}
