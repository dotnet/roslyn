// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedGenericParameter : DeletedDefinition<IGenericMethodParameter>, IGenericMethodParameter
    {
        private readonly DeletedMethodDefinition _method;

        public DeletedGenericParameter(IGenericMethodParameter oldParameter, DeletedMethodDefinition method, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldParameter, typesUsedByDeletedMembers)
        {
            _method = method;
        }

        public IMethodDefinition DefiningMethod => _method;

        public bool MustBeReferenceType => OldDefinition.MustBeReferenceType;

        public bool MustBeValueType => OldDefinition.MustBeValueType;

        public bool MustHaveDefaultConstructor => OldDefinition.MustHaveDefaultConstructor;

        public TypeParameterVariance Variance => OldDefinition.Variance;

        public IGenericMethodParameter? AsGenericMethodParameter => OldDefinition.AsGenericMethodParameter;

        public IGenericTypeParameter? AsGenericTypeParameter => OldDefinition.AsGenericTypeParameter;

        public bool IsEnum => OldDefinition.IsEnum;

        public bool IsValueType => OldDefinition.IsValueType;

        public Cci.PrimitiveTypeCode TypeCode => OldDefinition.TypeCode;

        public TypeDefinitionHandle TypeDef => OldDefinition.TypeDef;

        public IGenericMethodParameterReference? AsGenericMethodParameterReference => OldDefinition.AsGenericMethodParameterReference;

        public IGenericTypeInstanceReference? AsGenericTypeInstanceReference => OldDefinition.AsGenericTypeInstanceReference;

        public IGenericTypeParameterReference? AsGenericTypeParameterReference => OldDefinition.AsGenericTypeParameterReference;

        public INamespaceTypeReference? AsNamespaceTypeReference => OldDefinition.AsNamespaceTypeReference;

        public INestedTypeReference? AsNestedTypeReference => OldDefinition.AsNestedTypeReference;

        public ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference => OldDefinition.AsSpecializedNestedTypeReference;

        public string? Name => OldDefinition.Name;

        public ushort Index => OldDefinition.Index;

        IMethodReference IGenericMethodParameterReference.DefiningMethod => ((IGenericMethodParameterReference)OldDefinition).DefiningMethod;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return OldDefinition.AsDefinition(context);
        }

        public INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context)
        {
            return OldDefinition.AsNamespaceTypeDefinition(context);
        }

        public INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context)
        {
            return OldDefinition.AsNestedTypeDefinition(context);
        }

        public ITypeDefinition? AsTypeDefinition(EmitContext context)
        {
            return OldDefinition.AsTypeDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            OldDefinition.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return OldDefinition.GetAttributes(context);
        }

        public IEnumerable<TypeReferenceWithAttributes> GetConstraints(EmitContext context)
        {
            return OldDefinition.GetConstraints(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return OldDefinition.GetInternalSymbol();
        }

        public ITypeDefinition? GetResolvedType(EmitContext context)
        {
            return (ITypeDefinition?)WrapType(OldDefinition.GetResolvedType(context));
        }
    }
}
