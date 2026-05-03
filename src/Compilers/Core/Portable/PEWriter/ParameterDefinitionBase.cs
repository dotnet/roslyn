// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.Cci;

internal abstract class ParameterDefinitionBase : Cci.IParameterDefinition
{
    public bool HasDefaultValue => false;
    public bool IsIn => false;
    public virtual bool IsMarshalledExplicitly => false;
    public bool IsOptional => false;
    public bool IsOut => false;
    public virtual Cci.IMarshallingInformation? MarshallingInformation => null;
    public virtual ImmutableArray<byte> MarshallingDescriptor => default;
    public bool IsEncDeleted => false;
    public abstract string Name { get; }
    public virtual ImmutableArray<Cci.ICustomModifier> CustomModifiers => [];
    public virtual ImmutableArray<Cci.ICustomModifier> RefCustomModifiers => [];
    public virtual bool IsByReference => false;
    public abstract ushort Index { get; }

    public Cci.IDefinition? AsDefinition(EmitContext context) => this;
    public void Dispatch(Cci.MetadataVisitor visitor) => visitor.Visit(this);
    public virtual IEnumerable<Cci.ICustomAttribute> GetAttributes(EmitContext context) => [];
    public MetadataConstant? GetDefaultValue(EmitContext context) => null;
    public ISymbolInternal? GetInternalSymbol() => null;
    public abstract Cci.ITypeReference GetType(EmitContext context);

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
