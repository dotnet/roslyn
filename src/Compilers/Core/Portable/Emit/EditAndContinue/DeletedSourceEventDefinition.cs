// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedSourceEventDefinition
        : DeletedSourceDefinition<IEventDefinition>, IDeletedEventDefinition
    {
        private readonly EventDefinitionHandle _handle;

        public DeletedSourceEventDefinition(IEventDefinition oldEvent, EventDefinitionHandle handle, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers, ICustomAttribute? deletedAttribute)
            : base(oldEvent, typesUsedByDeletedMembers, deletedAttribute)
        {
            _handle = handle;
        }

        public EventDefinitionHandle MetadataHandle
            => _handle;

        public bool IsRuntimeSpecial => OldDefinition.IsRuntimeSpecial;

        public bool IsSpecialName => OldDefinition.IsSpecialName;

        public TypeMemberVisibility Visibility => OldDefinition.Visibility;

        public string? Name => OldDefinition.Name;

        public ITypeDefinition ContainingTypeDefinition => throw ExceptionUtilities.Unreachable();

        public override void Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }

        // Accessors only needed to emit MethodSemantics, which we do not need for deleted events
        public IMethodReference Adder => throw ExceptionUtilities.Unreachable();
        public IMethodReference? Caller => throw ExceptionUtilities.Unreachable();
        public IMethodReference Remover => throw ExceptionUtilities.Unreachable();

        public IEnumerable<IMethodReference> GetAccessors(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

        public ITypeReference GetContainingType(EmitContext context)
            => throw ExceptionUtilities.Unreachable();

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
}
