// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal struct NativeIntegerTypeDecoder
    {
        internal static TypeSymbol TransformType(TypeSymbol type, EntityHandle handle, PEModuleSymbol containingModule)
        {
            return containingModule.Module.HasNativeIntegerAttribute(handle, out var transformFlags) ?
                TransformType(type, transformFlags) :
                type;
        }

        private static TypeSymbol TransformType(TypeSymbol type, ImmutableArray<bool> transformFlags)
        {
            var decoder = new NativeIntegerTypeDecoder(transformFlags);
            try
            {
                var result = decoder.TransformType(type);
                if (decoder._index == transformFlags.Length)
                {
                    return result;
                }
            }
            catch (ArgumentException)
            {
            }
            return type;
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
            return type switch
            {
                NamedTypeSymbol namedType => TransformNamedType(namedType),
                ArrayTypeSymbol arrayType => TransformArrayType(arrayType),
                PointerTypeSymbol pointerType => TransformPointerType(pointerType),
                _ => type,
            };
        }

        private NamedTypeSymbol TransformNamedType(NamedTypeSymbol type)
        {
            int index = Increment();

            if (!type.IsGenericType)
            {
                return _transformFlags[index] ? TransformTypeDefinition(type) : type;
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
            Increment();
            return type.WithElementType(TransformTypeWithAnnotations(type.ElementTypeWithAnnotations));
        }

        private PointerTypeSymbol TransformPointerType(PointerTypeSymbol type)
        {
            Increment();
            return type.WithPointedAtType(TransformTypeWithAnnotations(type.PointedAtTypeWithAnnotations));
        }

        private int Increment()
        {
            if (_index < _transformFlags.Length)
            {
                return _index++;
            }
            throw new ArgumentException();
        }

        private static NamedTypeSymbol TransformTypeDefinition(NamedTypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return type.AsNativeInt(true);
                default:
                    throw new ArgumentException();
            }
        }
    }
}
