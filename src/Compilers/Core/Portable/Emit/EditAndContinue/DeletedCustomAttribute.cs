// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Emit.EditAndContinue
{
    internal sealed class DeletedCustomAttribute : DeletedDefinition<ICustomAttribute>, ICustomAttribute
    {
        public DeletedCustomAttribute(ICustomAttribute oldAttribute, Dictionary<ITypeDefinition, DeletedTypeDefinition> typesUsedByDeletedMembers)
            : base(oldAttribute, typesUsedByDeletedMembers)
        {
        }

        public int ArgumentCount => OldDefinition.ArgumentCount;

        public ushort NamedArgumentCount => OldDefinition.NamedArgumentCount;

        public bool AllowMultiple => OldDefinition.AllowMultiple;

        public IMethodReference Constructor(EmitContext context, bool reportDiagnostics)
        {
            return OldDefinition.Constructor(context, reportDiagnostics);
        }

        public ImmutableArray<IMetadataExpression> GetArguments(EmitContext context)
        {
            return OldDefinition.GetArguments(context);
        }

        public ImmutableArray<IMetadataNamedArgument> GetNamedArguments(EmitContext context)
        {
            return OldDefinition.GetNamedArguments(context);
        }

        public ITypeReference GetType(EmitContext context)
        {
            return WrapType(OldDefinition.GetType(context));
        }
    }
}
