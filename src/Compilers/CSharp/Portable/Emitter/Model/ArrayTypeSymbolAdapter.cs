// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class ArrayTypeSymbol :
        Cci.IArrayTypeReference
    {
        Cci.ITypeReference Cci.IArrayTypeReference.GetElementType(EmitContext context)
        {
            PEModuleBuilder moduleBeingBuilt = (PEModuleBuilder)context.Module;

            TypeSymbolWithAnnotations elementType = this.ElementType;
            var type = moduleBeingBuilt.Translate(elementType.TypeSymbol, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);

            if (elementType.CustomModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, elementType.CustomModifiers.As<Cci.ICustomModifier>());
            }
        }

        bool Cci.IArrayTypeReference.IsSZArray
        {
            get
            {
                return this.IsSZArray;
            }
        }

        IEnumerable<int> Cci.IArrayTypeReference.LowerBounds
        {
            get
            {
                var lowerBounds = this.LowerBounds;

                if (lowerBounds.IsDefault)
                {
                    return Enumerable.Repeat(0, Rank);
                }
                else
                {
                    return lowerBounds;
                }
            }
        }

        uint Cci.IArrayTypeReference.Rank
        {
            get
            {
                return (uint)this.Rank;
            }
        }

        IEnumerable<ulong> Cci.IArrayTypeReference.Sizes
        {
            get
            {
                if (this.Sizes.IsEmpty)
                {
                    return SpecializedCollections.EmptyEnumerable<ulong>();
                }

                return GetSizes();
            }
        }

        private IEnumerable<ulong> GetSizes()
        {
            foreach (var size in this.Sizes)
            {
                yield return (ulong)size;
            }
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IArrayTypeReference)this);
        }

        bool Cci.ITypeReference.IsEnum => false;
        bool Cci.ITypeReference.IsValueType => false;

        TypeDefinitionHandle Cci.ITypeReference.TypeDef => default(TypeDefinitionHandle);
        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode(EmitContext context) => Cci.PrimitiveTypeCode.NotPrimitive;

        Cci.ITypeDefinition Cci.ITypeReference.GetResolvedType(EmitContext context) => null;
        Cci.IGenericMethodParameterReference Cci.ITypeReference.AsGenericMethodParameterReference => null;
        Cci.IGenericTypeInstanceReference Cci.ITypeReference.AsGenericTypeInstanceReference => null;
        Cci.IGenericTypeParameterReference Cci.ITypeReference.AsGenericTypeParameterReference => null;
        Cci.INamespaceTypeDefinition Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => null;
        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference => null;
        Cci.INestedTypeDefinition Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context) => null;
        Cci.INestedTypeReference Cci.ITypeReference.AsNestedTypeReference => null;
        Cci.ISpecializedNestedTypeReference Cci.ITypeReference.AsSpecializedNestedTypeReference => null;
        Cci.ITypeDefinition Cci.ITypeReference.AsTypeDefinition(EmitContext context) => null;
        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context) => null;
    }
}
