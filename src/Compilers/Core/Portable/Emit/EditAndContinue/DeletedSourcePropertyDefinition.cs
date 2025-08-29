// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedSourcePropertyDefinition
        : DeletedSourceDefinition<IPropertyDefinition>, IDeletedPropertyDefinition
    {
        private readonly PropertyDefinitionHandle _handle;
        private readonly ImmutableArray<DeletedSourceParameterDefinition> _parameters;

        public DeletedSourcePropertyDefinition(IPropertyDefinition oldProperty, PropertyDefinitionHandle handle, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers, ICustomAttribute? deletedAttribute)
            : base(oldProperty, typesUsedByDeletedMembers, deletedAttribute)
        {
            _handle = handle;
            _parameters = WrapParameters(oldProperty.Parameters);
        }

        public PropertyDefinitionHandle MetadataHandle
            => _handle;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public ImmutableArray<IParameterDefinition> Parameters => StaticCast<IParameterDefinition>.From(_parameters);

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public CallingConvention CallingConvention => OldDefinition.CallingConvention;

        public ushort ParameterCount => (ushort)_parameters.Length;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => OldDefinition.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => OldDefinition.RefCustomModifiers;

        public bool ReturnValueIsByRef => OldDefinition.ReturnValueIsByRef;

        public string? Name => OldDefinition.Name;

        public MetadataConstant? DefaultValue => OldDefinition.DefaultValue;

        public bool HasDefaultValue => OldDefinition.HasDefaultValue;

        public ITypeDefinition ContainingTypeDefinition => throw ExceptionUtilities.Unreachable();

        public override void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }

        // Accessors only needed to emit MethodSemantics, which we do not need for deleted properties
        public IMethodReference? Getter
            => throw ExceptionUtilities.Unreachable();

        public IMethodReference? Setter
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IMethodReference> GetAccessors(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
            => StaticCast<IParameterTypeInformation>.From(_parameters);

        public ITypeReference GetContainingType(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw ExceptionUtilities.Unreachable();
        }
    }
}
