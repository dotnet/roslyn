// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedPropertyDefinition : DeletedDefinition<IPropertyDefinition>, IPropertyDefinition
    {
        private readonly ITypeDefinition _containingTypeDef;
        private readonly ImmutableArray<DeletedParameterDefinition> _parameters;

        private readonly IMethodReference? _getter;
        private readonly IMethodReference? _setter;

        public DeletedPropertyDefinition(IPropertyDefinition oldProperty, DeletedMethodDefinition? getter, DeletedMethodDefinition? setter, ITypeDefinition containingTypeDef, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldProperty, typesUsedByDeletedMembers)
        {
            _containingTypeDef = containingTypeDef;
            _getter = getter;
            _setter = setter;

            _parameters = WrapParameters(oldProperty.Parameters);
        }

        public MetadataConstant? DefaultValue => OldDefinition.DefaultValue;

        public IMethodReference? Getter => _getter;

        public bool HasDefaultValue => OldDefinition.HasDefaultValue;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public ImmutableArray<IParameterDefinition> Parameters => StaticCast<IParameterDefinition>.From(_parameters);

        public IMethodReference? Setter => _setter;

        public CallingConvention CallingConvention => OldDefinition.CallingConvention;

        public ushort ParameterCount => (ushort)_parameters.Length;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => OldDefinition.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => OldDefinition.RefCustomModifiers;

        public bool ReturnValueIsByRef => OldDefinition.ReturnValueIsByRef;

        public ITypeDefinition ContainingTypeDefinition => _containingTypeDef;

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public string? Name => OldDefinition.Name;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return OldDefinition.AsDefinition(context);
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
            return WrapAttributes(OldDefinition.GetAttributes(context));
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _containingTypeDef;
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return OldDefinition.GetInternalSymbol();
        }

        public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
        {
            return StaticCast<IParameterTypeInformation>.From(_parameters);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }
    }
}
