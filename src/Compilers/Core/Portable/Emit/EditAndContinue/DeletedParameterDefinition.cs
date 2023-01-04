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
    internal sealed class DeletedParameterDefinition : DeletedDefinition<IParameterDefinition>, IParameterDefinition
    {
        public DeletedParameterDefinition(IParameterDefinition oldParameter, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldParameter, typesUsedByDeletedMembers)
        {
        }

        public bool HasDefaultValue => OldDefinition.HasDefaultValue;

        public bool IsIn => OldDefinition.IsIn;

        public bool IsMarshalledExplicitly => OldDefinition.IsMarshalledExplicitly;

        public bool IsOptional => OldDefinition.IsOptional;

        public bool IsOut => OldDefinition.IsOut;

        public IMarshallingInformation? MarshallingInformation => OldDefinition.MarshallingInformation;

        public ImmutableArray<byte> MarshallingDescriptor => OldDefinition.MarshallingDescriptor;

        public string? Name => OldDefinition.Name;

        public ImmutableArray<ICustomModifier> CustomModifiers => OldDefinition.CustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => OldDefinition.RefCustomModifiers;

        public bool IsByReference => OldDefinition.IsByReference;

        public ushort Index => OldDefinition.Index;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return this;
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            OldDefinition.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return WrapAttributes(OldDefinition.GetAttributes(context));
        }

        public MetadataConstant? GetDefaultValue(EmitContext context)
        {
            return OldDefinition.GetDefaultValue(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return OldDefinition.GetInternalSymbol();
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }
    }
}
