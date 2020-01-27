// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE
{
    internal static class NativeIntegerTypeDecoder
    {
        internal static TypeSymbol TransformType(TypeSymbol type, EntityHandle handle, PEModuleSymbol containingModule)
        {
            if (containingModule.Module.HasNativeIntegerAttribute(handle, out var transformFlags))
            {
                var state = (transformFlags, 0);
                // PROTOTYPE: Test too few and too many bools.
                return TransformType(type, ref state);
            }

            return type;
        }

        private static TypeWithAnnotations TransformTypeWithAnnotations(TypeWithAnnotations type, ref (ImmutableArray<bool>, int) arg)
        {
            return type.WithTypeAndModifiers(TransformType(type.Type, ref arg), type.CustomModifiers);
        }

        private static TypeSymbol TransformType(TypeSymbol type, ref (ImmutableArray<bool>, int) arg)
        {
            return type switch
            {
                NamedTypeSymbol namedType => TransformNamedType(namedType, ref arg),
                ArrayTypeSymbol arrayType => TransformArrayType(arrayType, ref arg),
                PointerTypeSymbol pointerType => TransformPointerType(pointerType, ref arg),
                _ => throw ExceptionUtilities.UnexpectedValue(type)
            };
        }

        private static NamedTypeSymbol TransformNamedType(NamedTypeSymbol type, ref (ImmutableArray<bool>, int) arg)
        {
            if (!type.IsGenericType)
            {
                return arg.Item1[arg.Item2++] ? type.AsNativeInt(true) : type;
            }

            arg.Item2++;

            var allTypeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            type.GetAllTypeArgumentsNoUseSiteDiagnostics(allTypeArguments);

            bool haveChanges = false;
            for (int i = 0; i < allTypeArguments.Count; i++)
            {
                TypeWithAnnotations oldTypeArgument = allTypeArguments[i];
                TypeWithAnnotations newTypeArgument = TransformTypeWithAnnotations(oldTypeArgument, ref arg);
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

        private static ArrayTypeSymbol TransformArrayType(ArrayTypeSymbol type, ref (ImmutableArray<bool>, int) arg)
        {
            arg.Item2++;
            return type.WithElementType(TransformTypeWithAnnotations(type.ElementTypeWithAnnotations, ref arg));
        }

        private static PointerTypeSymbol TransformPointerType(PointerTypeSymbol type, ref (ImmutableArray<bool>, int) arg)
        {
            arg.Item2++;
            return type.WithPointedAtType(TransformTypeWithAnnotations(type.PointedAtTypeWithAnnotations, ref arg));
        }
    }
}
