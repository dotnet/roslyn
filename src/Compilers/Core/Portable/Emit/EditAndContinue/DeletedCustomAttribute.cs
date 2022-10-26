// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedCustomAttribute : ICustomAttribute
    {
        private readonly ICustomAttribute _oldAttribute;
        private readonly Dictionary<ITypeDefinition, DeletedTypeDefinition> _typesUsedByDeletedMembers;

        public DeletedCustomAttribute(ICustomAttribute oldAttribute, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
        {
            _oldAttribute = oldAttribute;
            _typesUsedByDeletedMembers = typesUsedByDeletedMembers;
        }

        public int ArgumentCount => _oldAttribute.ArgumentCount;

        public ushort NamedArgumentCount => _oldAttribute.NamedArgumentCount;

        public bool AllowMultiple => _oldAttribute.AllowMultiple;

        public IMethodReference Constructor(EmitContext context, bool reportDiagnostics)
        {
            return _oldAttribute.Constructor(context, reportDiagnostics);
        }

        public ImmutableArray<IMetadataExpression> GetArguments(EmitContext context)
        {
            return _oldAttribute.GetArguments(context);
        }

        public ImmutableArray<IMetadataNamedArgument> GetNamedArguments(EmitContext context)
        {
            return _oldAttribute.GetNamedArguments(context);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return DeletedTypeDefinition.TryCreate(_oldAttribute.GetType(context), _typesUsedByDeletedMembers);
        }
    }
}
