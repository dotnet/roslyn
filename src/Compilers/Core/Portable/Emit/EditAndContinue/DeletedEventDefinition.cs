// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit.EditAndContinue;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit
{
    internal class DeletedEventDefinition : IEventDefinition
    {
        private readonly IEventDefinition _oldEvent;
        private readonly ITypeDefinition _containingTypeDef;
        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;

        private readonly DeletedMethodDefinition _adder;
        private readonly DeletedMethodDefinition _remover;
        private readonly DeletedMethodDefinition? _caller;

        public DeletedEventDefinition(IEventDefinition oldEvent, DeletedMethodDefinition adder, DeletedMethodDefinition remover, DeletedMethodDefinition? caller, ITypeDefinition containingTypeDef, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            _oldEvent = oldEvent;
            _containingTypeDef = containingTypeDef;
            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
            _adder = adder;
            _remover = remover;
            _caller = caller;
        }

        public IMethodReference Adder => _adder;

        public IMethodReference? Caller => _caller;

        public bool IsRuntimeSpecial => _oldEvent.IsRuntimeSpecial;

        public bool IsSpecialName => _oldEvent.IsSpecialName;

        public IMethodReference Remover => _remover;

        public ITypeDefinition ContainingTypeDefinition => _containingTypeDef;

        public TypeMemberVisibility Visibility => _oldEvent.Visibility;

        public string? Name => _oldEvent.Name;

        public IDefinition? AsDefinition(EmitContext context)
        {
            return _oldEvent.AsDefinition(context);
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
            return _oldEvent.GetAttributes(context).Select(a => new DeletedCustomAttribute(a, _typesUsedByDeletedMembers));
        }

        public ITypeReference GetContainingType(EmitContext context)
        {
            return _containingTypeDef;
        }

        public ISymbolInternal? GetInternalSymbol()
        {
            return _oldEvent.GetInternalSymbol();
        }

        public ITypeReference GetType(EmitContext context)
        {
            return DeletedTypeDefinition.TryCreate(_oldEvent.GetType(context), _typesUsedByDeletedMembers);
        }
    }
}
