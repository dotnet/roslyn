// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal abstract class DeletedDefinition<T>
    {
        protected readonly T OldDefinition;

        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;

        /// <summary>
        /// Constructs a deleted definition
        /// </summary>
        /// <param name="oldDefinition">The old definition of the member</param>
        /// <param name="typesUsedByDeletedMembers">
        /// Cache of type definitions used in signatures of deleted members. Used so that if a method 'C M(C c)' is deleted
        /// we use the same <see cref="DeletedTypeDefinition"/> instance for the method return type, and the parameter type.
        /// </param>
        protected DeletedDefinition(T oldDefinition, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            OldDefinition = oldDefinition;

            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
        }

        protected ImmutableArray<DeletedParameterDefinition> WrapParameters(ImmutableArray<IParameterDefinition> parameters)
        {
            return parameters.SelectAsArray(p => new DeletedParameterDefinition(p, _typesUsedByDeletedMembers));
        }

        protected IEnumerable<DeletedGenericParameter> WrapGenericMethodParameters(DeletedMethodDefinition methodDefinition, IEnumerable<IGenericMethodParameter> genericParameters)
        {
            return genericParameters.Select(p => new DeletedGenericParameter(p, methodDefinition, _typesUsedByDeletedMembers));
        }

        protected IEnumerable<DeletedCustomAttribute> WrapAttributes(IEnumerable<ICustomAttribute> attributes)
        {
            return attributes.Select(a => new DeletedCustomAttribute(a, _typesUsedByDeletedMembers));
        }

        [return: NotNullIfNotNull("typeReference")]
        protected ITypeReference? WrapType(ITypeReference? typeReference)
        {
            if (typeReference is ITypeDefinition typeDef)
            {
                if (!_typesUsedByDeletedMembers.TryGetValue(typeDef, out var deletedType))
                {
                    deletedType = new DeletedTypeDefinition(typeDef);
                    _typesUsedByDeletedMembers.Add(typeDef, deletedType);
                }

                return deletedType;
            }

            return typeReference;
        }
    }
}
