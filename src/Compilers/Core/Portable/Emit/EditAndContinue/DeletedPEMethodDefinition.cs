// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedPEMethodDefinition : IDeletedMethodDefinition
    {
        private readonly IMethodSymbolInternal _oldMethod;
        private readonly DeletedMethodBody _body;

        public DeletedPEMethodDefinition(IMethodSymbolInternal oldMethod, ImmutableArray<byte> bodyIL)
        {
            Debug.Assert(oldMethod.MetadataToken != 0);

            _oldMethod = oldMethod;
            _body = new DeletedMethodBody(this, bodyIL);
        }

        public bool IsEncDeleted
            => true;

        public string? Name
            => _oldMethod.Name;

        public MethodDefinitionHandle MetadataHandle
            => MetadataTokens.MethodDefinitionHandle(_oldMethod.MetadataToken);

        public BlobHandle MetadataSignatureHandle
            => _oldMethod.MetadataSignatureHandle;

        public bool HasDeclarativeSecurity
            => _oldMethod.HasDeclarativeSecurity;

        public bool IsAbstract
            => _oldMethod.IsAbstract;

        public bool IsAccessCheckedOnOverride
            => _oldMethod.IsAccessCheckedOnOverride;

        public bool IsExternal
            => _oldMethod.IsExternal;

        public bool IsHiddenBySignature
            => _oldMethod.IsHiddenBySignature;

        public bool IsNewSlot
            => _oldMethod.IsMetadataNewSlot;

        public bool IsPlatformInvoke
            => _oldMethod.IsPlatformInvoke;

        public bool IsRuntimeSpecial
            => _oldMethod.HasRuntimeSpecialName;

        public bool IsSealed
            => _oldMethod.IsMetadataFinal;

        public bool IsSpecialName
            => _oldMethod.HasSpecialName;

        public bool IsStatic
            => _oldMethod.IsStatic;

        public bool IsVirtual
            => _oldMethod.IsVirtual;

        public bool RequiresSecurityObject
            => _oldMethod.RequiresSecurityObject;

        public Cci.TypeMemberVisibility Visibility
            => _oldMethod.MetadataVisibility;

        public MethodImplAttributes GetImplementationAttributes(EmitContext context)
            => _oldMethod.ImplementationAttributes;

        public bool HasBody
            => true;

        public Cci.IMethodBody GetBody(EmitContext context)
            => _body;

        public void Dispatch(Cci.MetadataVisitor visitor)
            => visitor.Visit(this);

        public ISymbolInternal? GetInternalSymbol()
            => _oldMethod;

        public IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context)
            => SpecializedCollections.EmptyEnumerable<Cci.ICustomAttribute>();

        public Cci.ITypeDefinition ContainingTypeDefinition
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<Cci.IGenericMethodParameter> GenericParameters
            => throw ExceptionUtilities.Unreachable();

        public bool IsConstructor
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<Cci.IParameterDefinition> Parameters
            => throw ExceptionUtilities.Unreachable();

        public Cci.IPlatformInvokeInformation PlatformInvokeData
            => throw ExceptionUtilities.Unreachable();

        public bool ReturnValueIsMarshalledExplicitly
            => throw ExceptionUtilities.Unreachable();

        public Cci.IMarshallingInformation ReturnValueMarshallingInformation
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<byte> ReturnValueMarshallingDescriptor
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<Cci.SecurityAttribute> SecurityAttributes
            => throw ExceptionUtilities.Unreachable();

        public Cci.INamespace ContainingNamespace
            => throw ExceptionUtilities.Unreachable();

        public bool AcceptsExtraArguments
            => throw ExceptionUtilities.Unreachable();

        public ushort GenericParameterCount
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<Cci.IParameterTypeInformation> ExtraParameters
            => throw ExceptionUtilities.Unreachable();

        public Cci.IGenericMethodInstanceReference? AsGenericMethodInstanceReference
            => throw ExceptionUtilities.Unreachable();

        public Cci.ISpecializedMethodReference? AsSpecializedMethodReference
            => throw ExceptionUtilities.Unreachable();

        public Cci.CallingConvention CallingConvention
            => throw ExceptionUtilities.Unreachable();

        public ushort ParameterCount
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<Cci.ICustomModifier> ReturnValueCustomModifiers
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<Cci.ICustomModifier> RefCustomModifiers
            => throw ExceptionUtilities.Unreachable();

        public bool ReturnValueIsByRef
            => throw ExceptionUtilities.Unreachable();

        public Cci.IDefinition? AsDefinition(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public Cci.ITypeReference GetContainingType(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public ImmutableArray<Cci.IParameterTypeInformation> GetParameters(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public Cci.IMethodDefinition GetResolvedMethod(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public IEnumerable<Cci.ICustomAttribute> GetReturnValueAttributes(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public Cci.ITypeReference GetType(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
        public sealed override bool Equals(object? obj)
            => throw ExceptionUtilities.Unreachable();

        // It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
        public sealed override int GetHashCode()
            => throw ExceptionUtilities.Unreachable();
    }
}
