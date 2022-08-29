// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedPropertyDefinition : IPropertyDefinition
    {
        private readonly IPropertyDefinition _oldProperty;
        private readonly ITypeDefinition _containingTypeDef;
        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;
        private readonly ImmutableArray<DeletedParameterDefinition> _parameters;

        private readonly IMethodReference? _getter;
        private readonly IMethodReference? _setter;

        public DeletedPropertyDefinition(IPropertyDefinition oldProperty, DeletedMethodDefinition? getter, DeletedMethodDefinition? setter, ITypeDefinition containingTypeDef, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            _oldProperty = oldProperty;
            _containingTypeDef = containingTypeDef;
            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
            _getter = getter;
            _setter = setter;

            _parameters = _oldProperty.Parameters.SelectAsArray(p => new DeletedParameterDefinition(p, typesUsedByDeletedMembers));
        }

        public MetadataConstant? DefaultValue => _oldProperty.DefaultValue;

        public IMethodReference? Getter => _getter;

        public bool HasDefaultValue => _oldProperty.HasDefaultValue;

        public bool IsRuntimeSpecial => _oldProperty.IsRuntimeSpecial;

        public bool IsSpecialName => _oldProperty.IsSpecialName;

        public ImmutableArray<IParameterDefinition> Parameters => StaticCast<IParameterDefinition>.From(_parameters);

        public IMethodReference? Setter => _setter;

        public CallingConvention CallingConvention => _oldProperty.CallingConvention;

        public ushort ParameterCount => (ushort)_parameters.Length;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => _oldProperty.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => _oldProperty.RefCustomModifiers;

        public bool ReturnValueIsByRef => _oldProperty.ReturnValueIsByRef;

        public ITypeDefinition ContainingTypeDefinition => _containingTypeDef;

        public TypeMemberVisibility Visibility => _oldProperty.Visibility;

        public string? Name => _oldProperty.Name;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return _oldProperty.AsDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public IEnumerable<IMethodReference> GetAccessors(EmitContext context)
        {
            if (_getter is not null)
                yield return _getter;

            if (_setter is not null)
                yield return _setter;
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _oldProperty.GetAttributes(context).Select(a => new DeletedCustomAttribute(a, _typesUsedByDeletedMembers));
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _containingTypeDef;
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return _oldProperty.GetInternalSymbol();
        }

        public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
        {
            return StaticCast<IParameterTypeInformation>.From(_parameters);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return DeletedTypeDefinition.TryCreate(_oldProperty.GetType(context), _typesUsedByDeletedMembers);
        }
    }
}
