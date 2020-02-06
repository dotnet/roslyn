// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#nullable enable

using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed partial class FunctionPointerTypeSymbol : IFunctionPointerTypeReference
    {
        ISignature IFunctionPointerTypeReference.Signature => Signature;
        void IReference.Dispatch(MetadataVisitor visitor) => visitor.Visit((IFunctionPointerTypeReference)this);

        bool ITypeReference.IsEnum => false;
        Cci.PrimitiveTypeCode ITypeReference.TypeCode => Cci.PrimitiveTypeCode.FunctionPointer;
        TypeDefinitionHandle ITypeReference.TypeDef => default;
        IGenericMethodParameterReference? ITypeReference.AsGenericMethodParameterReference => null;
        IGenericTypeInstanceReference? ITypeReference.AsGenericTypeInstanceReference => null;
        IGenericTypeParameterReference? ITypeReference.AsGenericTypeParameterReference => null;
        INamespaceTypeReference? ITypeReference.AsNamespaceTypeReference => null;
        INestedTypeReference? ITypeReference.AsNestedTypeReference => null;
        ISpecializedNestedTypeReference? ITypeReference.AsSpecializedNestedTypeReference => null;
        INamespaceTypeDefinition? ITypeReference.AsNamespaceTypeDefinition(EmitContext context) => null;
        INestedTypeDefinition? ITypeReference.AsNestedTypeDefinition(EmitContext context) => null;
        ITypeDefinition? ITypeReference.AsTypeDefinition(EmitContext context) => null;
        ITypeDefinition? ITypeReference.GetResolvedType(EmitContext context) => null;
        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        IDefinition? IReference.AsDefinition(EmitContext context) => null;
    }
}
