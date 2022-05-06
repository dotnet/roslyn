// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed partial class DeletedMethodDefinition : IMethodDefinition
    {
        private IMethodSymbolInternal _methodDef;
        private IMethodDefinition _oldMethod;

        public DeletedMethodDefinition(IMethodDefinition oldMethod, IMethodSymbolInternal methodDef)
        {
            _oldMethod = oldMethod;
            _methodDef = methodDef;
        }

        public IEnumerable<IGenericMethodParameter> GenericParameters => _oldMethod.GenericParameters;

        public bool HasDeclarativeSecurity => _oldMethod.HasDeclarativeSecurity;

        public bool IsAbstract => _oldMethod.IsAbstract;

        public bool IsAccessCheckedOnOverride => _oldMethod.IsAccessCheckedOnOverride;

        public bool IsConstructor => _oldMethod.IsConstructor;

        public bool IsExternal => _oldMethod.IsExternal;

        public bool IsHiddenBySignature => _oldMethod.IsHiddenBySignature;

        public bool IsNewSlot => _oldMethod.IsNewSlot;

        public bool IsPlatformInvoke => _oldMethod.IsPlatformInvoke;

        public bool IsRuntimeSpecial => _oldMethod.IsRuntimeSpecial;

        public bool IsSealed => _oldMethod.IsSealed;

        public bool IsSpecialName => _oldMethod.IsSpecialName;

        public bool IsStatic => _oldMethod.IsStatic;

        public bool IsVirtual => _oldMethod.IsVirtual;

        public ImmutableArray<IParameterDefinition> Parameters => _oldMethod.Parameters;

        public IPlatformInvokeInformation PlatformInvokeData => _oldMethod.PlatformInvokeData;

        public bool RequiresSecurityObject => _oldMethod.RequiresSecurityObject;

        public bool ReturnValueIsMarshalledExplicitly => _oldMethod.ReturnValueIsMarshalledExplicitly;

        public IMarshallingInformation ReturnValueMarshallingInformation => _oldMethod.ReturnValueMarshallingInformation;

        public ImmutableArray<byte> ReturnValueMarshallingDescriptor => _oldMethod.ReturnValueMarshallingDescriptor;

        public IEnumerable<SecurityAttribute> SecurityAttributes => _oldMethod.SecurityAttributes;

        public INamespace ContainingNamespace => _oldMethod.ContainingNamespace;

        public ITypeDefinition ContainingTypeDefinition => _oldMethod.ContainingTypeDefinition;

        public TypeMemberVisibility Visibility => _oldMethod.Visibility;

        public bool AcceptsExtraArguments => _oldMethod.AcceptsExtraArguments;

        public ushort GenericParameterCount => _oldMethod.GenericParameterCount;

        public bool IsGeneric => _oldMethod.IsGeneric;

        public ImmutableArray<IParameterTypeInformation> ExtraParameters => _oldMethod.ExtraParameters;

        public IGenericMethodInstanceReference AsGenericMethodInstanceReference => _oldMethod.AsGenericMethodInstanceReference;

        public ISpecializedMethodReference AsSpecializedMethodReference => _oldMethod.AsSpecializedMethodReference;

        public CallingConvention CallingConvention => _oldMethod.CallingConvention;

        public ushort ParameterCount => _oldMethod.ParameterCount;

        public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => _oldMethod.ReturnValueCustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => _oldMethod.RefCustomModifiers;

        public bool ReturnValueIsByRef => _oldMethod.ReturnValueIsByRef;

        public string Name => _oldMethod.Name;

        public IDefinition AsDefinition(EmitContext context)
        {
            return _oldMethod.AsDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            _oldMethod.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _oldMethod.GetAttributes(context);
        }

        public IMethodBody GetBody(EmitContext context)
        {
            return this;
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _oldMethod.GetContainingType(context);
        }

        public MethodImplAttributes GetImplementationAttributes(EmitContext context)
        {
            return _oldMethod.GetImplementationAttributes(context);
        }

        public ISymbolInternal GetInternalSymbol()
        {
            return _oldMethod.GetInternalSymbol();
        }

        public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context)
        {
            return _oldMethod.GetParameters(context);
        }

        public IMethodDefinition GetResolvedMethod(EmitContext context)
        {
            return _oldMethod.GetResolvedMethod(context);
        }

        public IEnumerable<ICustomAttribute> GetReturnValueAttributes(EmitContext context)
        {
            return _oldMethod.GetReturnValueAttributes(context);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return _oldMethod.GetType(context);
        }

        public sealed override bool Equals(object obj)
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
        }

        public sealed override int GetHashCode()
        {
            // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            throw Roslyn.Utilities.ExceptionUtilities.Unreachable;
        }

    }
}
