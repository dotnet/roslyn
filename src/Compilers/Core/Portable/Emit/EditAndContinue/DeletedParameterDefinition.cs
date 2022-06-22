// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedParameterDefinition : IParameterDefinition
    {
        private readonly IParameterDefinition _oldParameter;
        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;

        public DeletedParameterDefinition(IParameterDefinition oldParameter, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            _oldParameter = oldParameter;
            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
        }

        public bool HasDefaultValue => _oldParameter.HasDefaultValue;

        public bool IsIn => _oldParameter.IsIn;

        public bool IsMarshalledExplicitly => _oldParameter.IsMarshalledExplicitly;

        public bool IsOptional => _oldParameter.IsOptional;

        public bool IsOut => _oldParameter.IsOut;

        public IMarshallingInformation? MarshallingInformation => _oldParameter.MarshallingInformation;

        public ImmutableArray<byte> MarshallingDescriptor => _oldParameter.MarshallingDescriptor;

        public string? Name => _oldParameter.Name;

        public ImmutableArray<ICustomModifier> CustomModifiers => _oldParameter.CustomModifiers;

        public ImmutableArray<ICustomModifier> RefCustomModifiers => _oldParameter.RefCustomModifiers;

        public bool IsByReference => _oldParameter.IsByReference;

        public ushort Index => _oldParameter.Index;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return this;
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            _oldParameter.Dispatch(visitor);
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return _oldParameter.GetAttributes(context).Select(a => new DeletedCustomAttribute(a, _typesUsedByDeletedMembers));
        }

        public MetadataConstant? GetDefaultValue(EmitContext context)
        {
            return _oldParameter.GetDefaultValue(context);
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return _oldParameter.GetInternalSymbol();
        }

        public ITypeReference GetType(EmitContext context)
        {
            return DeletedTypeDefinition.TryCreate(_oldParameter.GetType(context), _typesUsedByDeletedMembers);
        }
    }
}
