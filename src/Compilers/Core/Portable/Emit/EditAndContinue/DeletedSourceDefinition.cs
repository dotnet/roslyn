// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Symbols;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal abstract class DeletedSourceDefinition<T> : IDefinition
        where T : IDefinition
    {
        public readonly T OldDefinition;

        private readonly Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> _typesUsedByDeletedMembers;
        private readonly IEnumerable<ICustomAttribute> _attributes;

        /// <summary>
        /// Constructs a deleted definition
        /// </summary>
        /// <param name="oldDefinition">The old definition of the member</param>
        /// <param name="typesUsedByDeletedMembers">
        /// Cache of type definitions used in signatures of deleted members. Used so that if a method 'C M(C c)' is deleted
        /// we use the same <see cref="DeletedSourceTypeDefinition"/> instance for the method return type, and the parameter type.
        /// </param>
        protected DeletedSourceDefinition(T oldDefinition, Dictionary<ITypeDefinition, DeletedSourceTypeDefinition> typesUsedByDeletedMembers, ICustomAttribute? deletedAttribute)
        {
            OldDefinition = oldDefinition;

            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
            _attributes = deletedAttribute != null ? [deletedAttribute] : [];
        }

        public bool IsEncDeleted
            => true;

        public IEnumerable<ICustomAttribute> GetAttributes(EmitContext context)
            => _attributes;

        public ISymbolInternal? GetInternalSymbol()
            => OldDefinition.GetInternalSymbol();

        public abstract void Dispatch(MetadataVisitor visitor);

        public IDefinition? AsDefinition(EmitContext context)
            => this;

        protected ImmutableArray<DeletedSourceParameterDefinition> WrapParameters(ImmutableArray<IParameterDefinition> parameters)
        {
            return parameters.SelectAsArray(p => new DeletedSourceParameterDefinition(p, _typesUsedByDeletedMembers));
        }

        [return: NotNullIfNotNull(nameof(typeReference))]
        protected ITypeReference? WrapType(ITypeReference? typeReference)
        {
            if (typeReference is ITypeDefinition typeDef)
            {
                if (!_typesUsedByDeletedMembers.TryGetValue(typeDef, out var deletedType))
                {
                    deletedType = new DeletedSourceTypeDefinition(typeDef, _typesUsedByDeletedMembers);
                    _typesUsedByDeletedMembers.Add(typeDef, deletedType);
                }

                return deletedType;
            }

            return typeReference;
        }
    }
}
