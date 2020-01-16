// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class DynamicTypeSymbol :
        Cci.ITypeReference,
        Cci.INamedTypeReference,
        Cci.INamespaceTypeReference
    {
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
            // dynamic can't be used in mscorlib, so the containing module is never the module being built
            return null;
        }

        Cci.PrimitiveTypeCode Cci.ITypeReference.TypeCode
        {
            get { return Cci.PrimitiveTypeCode.NotPrimitive; }
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

        Cci.INamespaceTypeReference Cci.ITypeReference.AsNamespaceTypeReference
        {
            get { return this; }
        }

        Cci.INamespaceTypeDefinition? Cci.ITypeReference.AsNamespaceTypeDefinition(EmitContext context)
        {
            // dynamic can't be used in mscorlib, so the containing module is never the module being built
            return null;
        }

        Cci.INestedTypeReference? Cci.ITypeReference.AsNestedTypeReference
        {
            get { return null; }
        }

        Cci.INestedTypeDefinition? Cci.ITypeReference.AsNestedTypeDefinition(EmitContext context)
        {
            return null;
        }

        Cci.ISpecializedNestedTypeReference? Cci.ITypeReference.AsSpecializedNestedTypeReference
        {
            get { return null; }
        }

        Cci.ITypeDefinition? Cci.ITypeReference.AsTypeDefinition(EmitContext context)
        {
            // dynamic can't be used in mscorlib, so the containing module is never the module being built
            return null;
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            throw ExceptionUtilities.Unreachable;
        }

        Cci.IDefinition? Cci.IReference.AsDefinition(EmitContext context)
        {
            // dynamic can't be used in mscorlib, so the containing module is never the module being built
            return null;
        }

        ushort Cci.INamedTypeReference.GenericParameterCount
        {
            get { return 0; }
        }

        bool Cci.INamedTypeReference.MangleName
        {
            get { return false; }
        }

        string Cci.INamedEntity.Name
        {
            get { return "Object"; }
        }

        Cci.IUnitReference Cci.INamespaceTypeReference.GetUnit(EmitContext context)
        {
            var obj = ((PEModuleBuilder)context.Module).GetSpecialType(Microsoft.CodeAnalysis.SpecialType.System_Object,
                                                              syntaxNodeOpt: (CSharpSyntaxNode)context.SyntaxNodeOpt,
                                                              diagnostics: context.Diagnostics);
            return ((Cci.INamespaceTypeReference)obj).GetUnit(context);
        }

        string Cci.INamespaceTypeReference.NamespaceName
        {
            get { return "System"; }
        }
    }
}
