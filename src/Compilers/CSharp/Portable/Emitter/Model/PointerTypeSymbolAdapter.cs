// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class PointerTypeSymbol :
        Cci.IPointerTypeReference
    {
        Cci.ITypeReference Cci.IPointerTypeReference.GetTargetType(EmitContext context)
        {
            var type = ((PEModuleBuilder)context.Module).Translate(this.PointedAtType, syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt, diagnostics: context.Diagnostics);

            if (this.PointedAtTypeWithAnnotations.CustomModifiers.Length == 0)
            {
                return type;
            }
            else
            {
                return new Cci.ModifiedTypeReference(type, this.PointedAtTypeWithAnnotations.CustomModifiers.As<Cci.ICustomModifier>());
            }
        }

        bool Cci.ITypeReference.IsEnum
        {
            get { return false; }
        }

        bool Cci.ITypeReference.IsValueType
        {
            get { return false; }
        }

        Cci.ITypeDefinition? Cci.ITypeReference.GetResolvedType(EmitContext context)
        {
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get { return Cci.PrimitiveTypeCode.Pointer; }
        }

        TypeDefinitionHandle Cci.ITypeReference.TypeDef
        {
            get { return default(TypeDefinitionHandle); }
        }

        Cci.IGenericMethodParameterReference? Cci.ITypeReference.AsGenericMethodParameterReference
        {
            get { return null; }
        }

        Cci.IGenericTypeInstanceReference? Cci.ITypeReference.AsGenericTypeInstanceReference
        {
            get { return null; }
        }

        Cci.IGenericTypeParameterReference? Cci.ITypeReference.AsGenericTypeParameterReference
        {
            get { return null; }
        }

        Cci.INamespaceTypeDefinition? Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INamespaceTypeReference? Cci.ITypeReference.AsNamespaceTypeReference
        {
            get { return null; }
        }

        Cci.INestedTypeDefinition? Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.INestedTypeReference? Cci.ITypeReference.AsNestedTypeReference
        {
            get { return null; }
        }

        Cci.ISpecializedNestedTypeReference? Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        Cci.ITypeDefinition? Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IPointerTypeReference)this);
        }

        Cci.IDefinition? Cci.IReference.AsDefinition(EmitContext context)
        {
            return null;
        }
    }
}
