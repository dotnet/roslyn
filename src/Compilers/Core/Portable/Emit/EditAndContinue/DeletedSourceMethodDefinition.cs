// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedSourceMethodDefinition
        : DeletedSourceDefinition<IMethodDefinition>, IDeletedMethodDefinition
    {
        private readonly MethodDefinitionHandle _handle;
        private readonly ImmutableArray<DeletedSourceParameterDefinition> _parameters;
        private readonly DeletedMethodBody _body;

        public DeletedSourceMethodDefinition(IMethodDefinition oldMethod, MethodDefinitionHandle handle, ImmutableArray<byte> bodyIL, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers, ICustomAttribute? deletedAttribute)
            : base(oldMethod, typesUsedByDeletedMembers, deletedAttribute)
        {
            _handle = handle;
            _parameters = WrapParameters(oldMethod.Parameters);
            _body = new DeletedMethodBody(this, bodyIL);
        }

        public MethodDefinitionHandle MetadataHandle
            => _handle;

        public IEnumerable<IGenericMethodParameter> GenericParameters
            => throw ExceptionUtilities.Unreachable();

        public bool HasDeclarativeSecurity => OldDefinition.HasDeclarativeSecurity;

        public bool IsAbstract => OldDefinition.IsAbstract;

        public bool IsAccessCheckedOnOverride => OldDefinition.IsAccessCheckedOnOverride;

        public bool IsConstructor => OldDefinition.IsConstructor;

        public bool IsExternal => OldDefinition.IsExternal;

        public bool IsHiddenBySignature => OldDefinition.IsHiddenBySignature;

        public bool IsNewSlot => OldDefinition.IsNewSlot;

        public bool IsPlatformInvoke => OldDefinition.IsPlatformInvoke;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSealed => OldDefinition.IsSealed;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public bool IsStatic => OldDefinition.IsStatic;

        public bool IsVirtual => OldDefinition.IsVirtual;

        public ImmutableArray<IParameterDefinition> Parameters => StaticCast<IParameterDefinition>.From(_parameters);

        public IPlatformInvokeInformation PlatformInvokeData => OldDefinition.PlatformInvokeData;

        public bool RequiresSecurityObject => OldDefinition.RequiresSecurityObject;

        public bool ReturnValueIsMarshalledExplicitly => OldDefinition.ReturnValueIsMarshalledExplicitly;

        public IMarshallingInformation ReturnValueMarshallingInformation => OldDefinition.ReturnValueMarshallingInformation;

        public ImmutableArray<byte> ReturnValueMarshallingDescriptor => OldDefinition.ReturnValueMarshallingDescriptor;

        public IEnumerable<SecurityAttribute> SecurityAttributes => OldDefinition.SecurityAttributes;

        public INamespace ContainingNamespace => OldDefinition.ContainingNamespace;

        public ITypeDefinition ContainingTypeDefinition => throw ExceptionUtilities.Unreachable();

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public bool AcceptsExtraArguments => OldDefinition.AcceptsExtraArguments;

        public ushort GenericParameterCount => OldDefinition.GenericParameterCount;

        public ImmutableArray<IParameterTypeInformation> ExtraParameters => OldDefinition.ExtraParameters;

        public IGenericMethodInstanceReference? AsGenericMethodInstanceReference => OldDefinition.AsGenericMethodInstanceReference;

        public ISpecializedMethodReference? AsSpecializedMethodReference => OldDefinition.AsSpecializedMethodReference;

        public CallingConvention CallingConvention => OldDefinition.CallingConvention;

        public ushort ParameterCount => (ushort)_parameters.Length;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => OldDefinition.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => OldDefinition.RefCustomModifiers;

        public bool ReturnValueIsByRef => OldDefinition.ReturnValueIsByRef;

        public string? Name => OldDefinition.Name;

        public override void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public bool HasBody
            => true;

        public IMethodBody GetBody(EmitContext context)
            => _body;

        public ITypeReference GetContainingType(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return OldDefinition.GetImplementationAttributes(context);
        }

        public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
        {
            return StaticCast<IParameterTypeInformation>.From(_parameters);
        }

        public IMethodDefinition GetResolvedMethod(EmitContext context)
        {
            return this;
        }

        public IEnumerable<ICustomAttribute> GetReturnValueAttributes(EmitContext context)
            // attributes shouldn't be emitted for deleted definitions
            => throw ExceptionUtilities.Unreachable();

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }

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
