// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedGenericParameter : IGenericMethodParameter
    {
        private readonly IGenericMethodParameter _oldParameter;
        private readonly DeletedMethodDefinition _method;
        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;

        public DeletedGenericParameter(IGenericMethodParameter oldParameter, DeletedMethodDefinition method, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            _oldParameter = oldParameter;
            _method = method;
            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
        }

        public IMethodDefinition DefiningMethod => _method;

        public bool MustBeReferenceType => _oldParameter.MustBeReferenceType;

        public bool MustBeValueType => _oldParameter.MustBeValueType;

        public bool MustHaveDefaultConstructor => _oldParameter.MustHaveDefaultConstructor;

        public TypeParameterVariance Variance => _oldParameter.Variance;

        public IGenericMethodParameter? AsGenericMethodParameter => _oldParameter.AsGenericMethodParameter;

        public IGenericTypeParameter? AsGenericTypeParameter => _oldParameter.AsGenericTypeParameter;

        public bool IsEnum => _oldParameter.IsEnum;

        public bool IsValueType => _oldParameter.IsValueType;

        public Cci.PrimitiveTypeCode TypeCode => _oldParameter.TypeCode;

        public TypeDefinitionHandle TypeDef => _oldParameter.TypeDef;

        public IGenericMethodParameterReference? AsGenericMethodParameterReference => _oldParameter.AsGenericMethodParameterReference;

        public IGenericTypeInstanceReference? AsGenericTypeInstanceReference => _oldParameter.AsGenericTypeInstanceReference;

        public IGenericTypeParameterReference? AsGenericTypeParameterReference => _oldParameter.AsGenericTypeParameterReference;

        public INamespaceTypeReference? AsNamespaceTypeReference => _oldParameter.AsNamespaceTypeReference;

        public INestedTypeReference? AsNestedTypeReference => _oldParameter.AsNestedTypeReference;

        public ISpecializedNestedTypeReference? AsSpecializedNestedTypeReference => _oldParameter.AsSpecializedNestedTypeReference;

        public string? Name => _oldParameter.Name;

        public ushort Index => _oldParameter.Index;

        IMethodReference IGenericMethodParameterReference.DefiningMethod => ((IGenericMethodParameterReference)_oldParameter).DefiningMethod;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return _oldParameter.AsDefinition(context);
        }

        public INamespaceTypeDefinition? AsNamespaceTypeDefinition(EmitContext context)
        {
            return _oldParameter.AsNamespaceTypeDefinition(context);
        }

        public INestedTypeDefinition? AsNestedTypeDefinition(EmitContext context)
        {
            return _oldParameter.AsNestedTypeDefinition(context);
        }

        public ITypeDefinition? AsTypeDefinition(EmitContext context)
        {
            return _oldParameter.AsTypeDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            _oldParameter.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _oldParameter.GetAttributes(context);
        }

        public IEnumerable<TypeReferenceWithAttributes> GetConstraints(EmitContext context)
        {
            return _oldParameter.GetConstraints(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return _oldParameter.GetInternalSymbol();
        }

        public ITypeDefinition? GetResolvedType(EmitContext context)
        {
            return (ITypeDefinition?)DeletedTypeDefinition.TryCreate(_oldParameter.GetResolvedType(context), _typesUsedByDeletedMembers);
        }
    }
}
