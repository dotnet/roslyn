// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Emit.NoPia
{
    internal sealed class VtblGap : Cci.IMethodDefinition
    {
        public readonly Cci.ITypeDefinition ContainingType;
        private readonly string _name;

        public VtblGap(Cci.ITypeDefinition containingType, string name)
        {
            this.ContainingType = containingType;
            _name = name;
        }

        Cci.IMethodBody Cci.IMethodDefinition.GetBody(EmitContext context)
        {
            return null;
        }

        IEnumerable<Cci.IGenericMethodParameter> Cci.IMethodDefinition.GenericParameters
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.IGenericMethodParameter>(); }
        }

        bool Cci.IMethodDefinition.IsImplicitlyDeclared
        {
            get { return true; }
        }

        bool Cci.IMethodDefinition.HasDeclarativeSecurity
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsAbstract
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsAccessCheckedOnOverride
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsConstructor
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsExternal
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsHiddenBySignature
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsNewSlot
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsPlatformInvoke
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsRuntimeSpecial
        {
            get { return true; }
        }

        bool Cci.IMethodDefinition.IsSealed
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsSpecialName
        {
            get { return true; }
        }

        bool Cci.IMethodDefinition.IsStatic
        {
            get { return false; }
        }

        bool Cci.IMethodDefinition.IsVirtual
        {
            get { return false; }
        }

        System.Reflection.MethodImplAttributes Cci.IMethodDefinition.GetImplementationAttributes(EmitContext context)
        {
            return System.Reflection.MethodImplAttributes.Managed | System.Reflection.MethodImplAttributes.Runtime;
        }

        ImmutableArray<Cci.IParameterDefinition> Cci.IMethodDefinition.Parameters
        {
            get { return ImmutableArray<Cci.IParameterDefinition>.Empty; }
        }

        Cci.IPlatformInvokeInformation Cci.IMethodDefinition.PlatformInvokeData
        {
            get { return null; }
        }

        bool Cci.IMethodDefinition.RequiresSecurityObject
        {
            get { return false; }
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IMethodDefinition.GetReturnValueAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        bool Cci.IMethodDefinition.ReturnValueIsMarshalledExplicitly
        {
            get { return false; }
        }

        Cci.IMarshallingInformation Cci.IMethodDefinition.ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        ImmutableArray<byte> Cci.IMethodDefinition.ReturnValueMarshallingDescriptor
        {
            get { return default(ImmutableArray<byte>); }
        }

        IEnumerable<Cci.SecurityAttribute> Cci.IMethodDefinition.SecurityAttributes
        {
            get { return SpecializedCollections.EmptyEnumerable<Cci.SecurityAttribute>(); }
        }

        Cci.ITypeDefinition Cci.ITypeDefinitionMember.ContainingTypeDefinition
        {
            get { return ContainingType; }
        }

        Cci.INamespace Cci.IMethodDefinition.ContainingNamespace
        {
            get
            {
                // The containing namespace is only used for methods for which we generate debug information.
                return null;
            }
        }

        Cci.TypeMemberVisibility Cci.ITypeDefinitionMember.Visibility
        {
            get { return Cci.TypeMemberVisibility.Public; }
        }

        Cci.ITypeReference Cci.ITypeMemberReference.GetContainingType(EmitContext context)
        {
            return ContainingType;
        }

        IEnumerable<Cci.ICustomAttribute> Cci.IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();
        }

        void Cci.IReference.Dispatch(Cci.MetadataVisitor visitor)
        {
            visitor.Visit((Cci.IMethodDefinition)this);
        }

        Cci.IDefinition Cci.IReference.AsDefinition(EmitContext context)
        {
            return this;
        }

        string Cci.IMethodDefinition.Name
        {
            get { return _name; }
        }

        string Cci.INamedEntity.Name
        {
            get { return _name; }
        }

        bool Cci.IMethodReference.AcceptsExtraArguments
        {
            get { return false; }
        }

        ushort Cci.IMethodReference.GenericParameterCount
        {
            get { return 0; }
        }

        bool Cci.IMethodReference.IsGeneric
        {
            get { return false; }
        }

        Cci.IMethodDefinition Cci.IMethodReference.GetResolvedMethod(EmitContext context)
        {
            return this;
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.IMethodReference.ExtraParameters
        {
            get { return ImmutableArray<Cci.IParameterTypeInformation>.Empty; }
        }

        Cci.IGenericMethodInstanceReference Cci.IMethodReference.AsGenericMethodInstanceReference
        {
            get { return null; }
        }

        Cci.ISpecializedMethodReference Cci.IMethodReference.AsSpecializedMethodReference
        {
            get { return null; }
        }

        Cci.CallingConvention Cci.ISignature.CallingConvention
        {
            get { return Cci.CallingConvention.Default | Cci.CallingConvention.HasThis; }
        }

        ushort Cci.ISignature.ParameterCount
        {
            get { return 0; }
        }

        ImmutableArray<Cci.IParameterTypeInformation> Cci.ISignature.GetParameters(EmitContext context)
        {
            return ImmutableArray<Cci.IParameterTypeInformation>.Empty;
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.ReturnValueCustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        ImmutableArray<Cci.ICustomModifier> Cci.ISignature.RefCustomModifiers
        {
            get { return ImmutableArray<Cci.ICustomModifier>.Empty; }
        }

        bool Cci.ISignature.ReturnValueIsByRef
        {
            get { return false; }
        }

        Cci.ITypeReference Cci.ISignature.GetType(EmitContext context)
        {
            return context.Module.GetPlatformType(Cci.PlatformType.SystemVoid, context);
        }
    }
}
