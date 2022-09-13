// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedEventDefinition : DeletedDefinition<IEventDefinition>, IEventDefinition
    {
        private readonly ITypeDefinition _containingTypeDef;
        private readonly DeletedMethodDefinition _adder;
        private readonly DeletedMethodDefinition _remover;
        private readonly DeletedMethodDefinition? _caller;

        public DeletedEventDefinition(IEventDefinition oldEvent, DeletedMethodDefinition adder, DeletedMethodDefinition remover, DeletedMethodDefinition? caller, ITypeDefinition containingTypeDef, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldEvent, typesUsedByDeletedMembers)
        {

            _containingTypeDef = containingTypeDef;
            _adder = adder;
            _remover = remover;
            _caller = caller;
        }

        public IMethodReference Adder => _adder;

        public IMethodReference? Caller => _caller;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public IMethodReference Remover => _remover;

        public ITypeDefinition ContainingTypeDefinition => _containingTypeDef;

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public string? Name => OldDefinition.Name;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return OldDefinition.AsDefinition(context);
        }

        public void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public IEnumerable<IMethodReference> GetAccessors(EmitContext context)
        {
            if (_adder is not null)
                yield return _adder;

            if (_remover is not null)
                yield return _remover;

            if (_caller is not null)
                yield return _caller;
        }

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
        {
            return WrapAttributes(OldDefinition.GetAttributes(context));
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _containingTypeDef;
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
