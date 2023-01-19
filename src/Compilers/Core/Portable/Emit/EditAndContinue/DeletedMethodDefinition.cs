// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedMethodDefinition : DeletedDefinition<IMethodDefinition>, IMethodDefinition
    {
        private readonly ITypeDefinition _containingTypeDef;
        private readonly ImmutableArray<DeletedParameterDefinition> _parameters;
        private DeletedMethodBody? _body;

        public DeletedMethodDefinition(IMethodDefinition oldMethod, ITypeDefinition containingTypeDef, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldMethod, typesUsedByDeletedMembers)
        {
            _containingTypeDef = containingTypeDef;

            _parameters = WrapParameters(oldMethod.Parameters);
        }

        public IEnumerable<IGenericMethodParameter> GenericParameters
        {
            get
            {
                return WrapGenericMethodParameters(this, OldDefinition.GenericParameters);
            }
        }

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

        public ITypeDefinition ContainingTypeDefinition => _containingTypeDef;

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public bool AcceptsExtraArguments => OldDefinition.AcceptsExtraArguments;

        public ushort GenericParameterCount => OldDefinition.GenericParameterCount;

        public bool IsGeneric => OldDefinition.IsGeneric;

        public ImmutableArray<IParameterTypeInformation> ExtraParameters => OldDefinition.ExtraParameters;

        public IGenericMethodInstanceReference? AsGenericMethodInstanceReference => OldDefinition.AsGenericMethodInstanceReference;

        public ISpecializedMethodReference? AsSpecializedMethodReference => OldDefinition.AsSpecializedMethodReference;

        public CallingConvention CallingConvention => OldDefinition.CallingConvention;

        public ushort ParameterCount => (ushort)_parameters.Length;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => OldDefinition.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => OldDefinition.RefCustomModifiers;

        public bool ReturnValueIsByRef => OldDefinition.ReturnValueIsByRef;

        public string? Name => OldDefinition.Name;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return OldDefinition.AsDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return WrapAttributes(OldDefinition.GetAttributes(context));
        }

        public IMethodBody GetBody(EmitContext context)
        {
            _body ??= new DeletedMethodBody(this, context);
            return _body;
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _containingTypeDef;
        }

        public MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return OldDefinition.GetImplementationAttributes(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return OldDefinition.GetInternalSymbol();
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
        {
            return WrapAttributes(OldDefinition.GetReturnValueAttributes(context));
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }

        public sealed override bool Equals(object? obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable();
        }
    }
}
