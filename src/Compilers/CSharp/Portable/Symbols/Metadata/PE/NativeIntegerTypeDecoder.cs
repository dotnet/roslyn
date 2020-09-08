// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal struct NativeIntegerTypeDecoder
    {
        private sealed class ErrorTypeException : Exception { }

        internal static TypeSymbol TransformType(TypeSymbol type, EntityHandle handle, PEModuleSymbol containingModule)
        {
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
                if (decoder._index == transformFlags.Length)
                {
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
            catch (ErrorTypeException)
            {
                // If we failed to decode because there was an error type involved, marking the
                // metadata as unsupported means that we'll cover up the error that would otherwise
                // be reported for the type. This would likely lead to a worse error message as we
                // would just report a BindToBogus, so return the type unchanged.
                Debug.Assert(type.ContainsErrorType());
                return type;
            }
        }

        private readonly ImmutableArray<bool> _transformFlags;
        private int _index;

        private NativeIntegerTypeDecoder(ImmutableArray<bool> transformFlags)
        {
            _transformFlags = transformFlags;
            _index = 0;
        }

        private TypeWithAnnotations TransformTypeWithAnnotations(TypeWithAnnotations type)
        {
            return type.WithTypeAndModifiers(TransformType(type.Type), type.CustomModifiers);
        }

        private TypeSymbol TransformType(TypeSymbol type)
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
                    throw new ErrorTypeException();
            }
        }

        private NamedTypeSymbol TransformNamedType(NamedTypeSymbol type)
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
                        return (_transformFlags[_index++], type.IsNativeIntegerType) switch
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
                TypeWithAnnotations newTypeArgument = TransformTypeWithAnnotations(oldTypeArgument);
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

        private ArrayTypeSymbol TransformArrayType(ArrayTypeSymbol type)
        {
            return type.WithElementType(TransformTypeWithAnnotations(type.ElementTypeWithAnnotations));
        }

        private PointerTypeSymbol TransformPointerType(PointerTypeSymbol type)
        {
            return type.WithPointedAtType(TransformTypeWithAnnotations(type.PointedAtTypeWithAnnotations));
        }

        private FunctionPointerTypeSymbol TransformFunctionPointerType(FunctionPointerTypeSymbol type)
        {
            var transformedReturnType = TransformTypeWithAnnotations(type.Signature.ReturnTypeWithAnnotations);
            var transformedParameterTypes = ImmutableArray<TypeWithAnnotations>.Empty;
            var paramsModified = false;

            if (type.Signature.ParameterCount > 0)
            {
                var builder = ArrayBuilder<TypeWithAnnotations>.GetInstance(type.Signature.ParameterCount);
                foreach (var param in type.Signature.Parameters)
                {
                    var transformedParam = TransformTypeWithAnnotations(param.TypeWithAnnotations);
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
