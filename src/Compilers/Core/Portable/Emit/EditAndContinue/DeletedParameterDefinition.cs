// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal class DeletedParameterDefinition : IParameterDefinition
    {
        private readonly IParameterDefinition _oldParameter;

        public DeletedParameterDefinition(IParameterDefinition oldParameter)
        {
            _oldParameter = oldParameter;
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
            return _oldParameter.GetAttributes(context);
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
            if (_oldParameter.GetType(context) is ITypeDefinition typeDef)
            {
                return new DeletedTypeDefinition(typeDef);
            }
            return _oldParameter.GetType(context);
        }
    }
}
