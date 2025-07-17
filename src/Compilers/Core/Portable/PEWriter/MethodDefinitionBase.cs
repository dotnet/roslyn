// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Microsoft.Cci;

internal abstract class MethodDefinitionBase : IMethodDefinition, IMethodBody
{
    public MethodDefinitionBase(ITypeDefinition containingTypeDefinition, ushort maxStack, ImmutableArray<byte> il)
    {
        ContainingTypeDefinition = containingTypeDefinition;
        MaxStack = maxStack;
        IL = il;
    }

    // IMethodDefinition implementation

    bool IDefinition.IsEncDeleted => false;

    public ITypeDefinition ContainingTypeDefinition { get; }

    public abstract string Name { get; }

    public bool HasBody => true;

    public IMethodBody GetBody(EmitContext context) => this;

    public IEnumerable<IGenericMethodParameter> GenericParameters => SpecializedCollections.EmptyEnumerable<IGenericMethodParameter>();

    public bool HasDeclarativeSecurity => false;

    public bool IsAbstract => false;

    public bool IsAccessCheckedOnOverride => false;

    public bool IsConstructor => false;

    public bool IsExternal => false;

    public bool IsHiddenBySignature => true;

    public bool IsNewSlot => false;

    public bool IsPlatformInvoke => false;

    public virtual bool IsRuntimeSpecial => false;

    public bool IsSealed => false;

    public virtual bool IsSpecialName => false;

    public bool IsStatic => true;

    public bool IsVirtual => false;

    public virtual ImmutableArray<IParameterDefinition> Parameters => ImmutableArray<IParameterDefinition>.Empty;

#nullable disable
    public IPlatformInvokeInformation PlatformInvokeData => null;

    public bool RequiresSecurityObject => false;

    public bool ReturnValueIsMarshalledExplicitly => false;

    public IMarshallingInformation ReturnValueMarshallingInformation => null;

    public ImmutableArray<byte> ReturnValueMarshallingDescriptor => default;

    public IEnumerable<SecurityAttribute> SecurityAttributes => null;

    public INamespace ContainingNamespace => null;

    public abstract TypeMemberVisibility Visibility { get; }

    public bool AcceptsExtraArguments => false;

    public ushort GenericParameterCount => 0;

    public ImmutableArray<IParameterTypeInformation> ExtraParameters => ImmutableArray<IParameterTypeInformation>.Empty;

    public IGenericMethodInstanceReference AsGenericMethodInstanceReference => null;

    public ISpecializedMethodReference AsSpecializedMethodReference => null;

    public CallingConvention CallingConvention => CallingConvention.Default;

    public ushort ParameterCount => (ushort)Parameters.Length;

    public ImmutableArray<ICustomModifier> ReturnValueCustomModifiers => ImmutableArray<ICustomModifier>.Empty;

    public ImmutableArray<ICustomModifier> RefCustomModifiers => ImmutableArray<ICustomModifier>.Empty;

    public bool ReturnValueIsByRef => false;

    public IDefinition AsDefinition(EmitContext context) => this;

    CodeAnalysis.Symbols.ISymbolInternal Cci.IReference.GetInternalSymbol() => null;

    public void Dispatch(MetadataVisitor visitor) => visitor.Visit((IMethodDefinition)this);

    public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();

    public ITypeReference GetContainingType(EmitContext context) => ContainingTypeDefinition;

    public MethodImplAttributes GetImplementationAttributes(EmitContext context) => default;

    public ImmutableArray<IParameterTypeInformation> GetParameters(EmitContext context) => Parameters.CastArray<IParameterTypeInformation>();

    public IMethodDefinition GetResolvedMethod(EmitContext context) => this;

    public IEnumerable<ICustomAttribute> GetReturnValueAttributes(EmitContext context) => SpecializedCollections.EmptyEnumerable<ICustomAttribute>();

    public virtual ITypeReference GetType(EmitContext context) => context.Module.GetPlatformType(PlatformType.SystemVoid, context);

    // IMethodBody implementation

    public ushort MaxStack { get; }

    public ImmutableArray<byte> IL { get; }

    public IMethodDefinition MethodDefinition => this;

    public ImmutableArray<ExceptionHandlerRegion> ExceptionRegions => ImmutableArray<ExceptionHandlerRegion>.Empty;

    public bool AreLocalsZeroed => false;

    public bool HasStackalloc => false;

    public ImmutableArray<ILocalDefinition> LocalVariables => ImmutableArray<ILocalDefinition>.Empty;

    public StateMachineMoveNextBodyDebugInfo MoveNextBodyInfo => null;

    public ImmutableArray<SequencePoint> SequencePoints => ImmutableArray<SequencePoint>.Empty;

    public bool HasDynamicLocalVariables => false;

    public ImmutableArray<LocalScope> LocalScopes => ImmutableArray<LocalScope>.Empty;

    public IImportScope ImportScope => null;

    public DebugId MethodId => default;

    public ImmutableArray<StateMachineHoistedLocalScope> StateMachineHoistedLocalScopes => default;

    public string StateMachineTypeName => null;

    public ImmutableArray<EncHoistedLocalInfo> StateMachineHoistedLocalSlots => default;

    public ImmutableArray<ITypeReference> StateMachineAwaiterSlots => default;

    public ImmutableArray<EncClosureInfo> ClosureDebugInfo => ImmutableArray<EncClosureInfo>.Empty;

    public ImmutableArray<EncLambdaInfo> LambdaDebugInfo => ImmutableArray<EncLambdaInfo>.Empty;

    public ImmutableArray<LambdaRuntimeRudeEditInfo> OrderedLambdaRuntimeRudeEdits => ImmutableArray<LambdaRuntimeRudeEditInfo>.Empty;

    public StateMachineStatesDebugInfo StateMachineStatesDebugInfo => default;

    public ImmutableArray<SourceSpan> CodeCoverageSpans => ImmutableArray<SourceSpan>.Empty;

    public bool IsPrimaryConstructor => false;

    public sealed override bool Equals(object obj)
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
