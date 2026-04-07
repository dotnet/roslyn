// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedSourceParameterDefinition : DeletedSourceDefinition<IParameterDefinition>, IParameterDefinition
    {
        public DeletedSourceParameterDefinition(IParameterDefinition oldParameter, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers)
            : base(oldParameter, typesUsedByDeletedMembers, deletedAttribute: null)
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

        public MetadataConstant? GetDefaultValue(EmitContext context)
        {
            return OldDefinition.GetDefaultValue(context);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }

        public override void Dispatch(MetadataVisitor visitor)
            => visitor.Visit(this);
    }
}
