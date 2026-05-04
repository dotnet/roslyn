// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class AssemblyReference : IAssemblyReference
    {
        private readonly AssemblyIdentity _identity;

        internal AssemblyReference(AssemblyIdentity identity)
        {
            _identity = identity;
        }

        AssemblyIdentity IAssemblyReference.Identity => _identity;
        Version? IAssemblyReference.AssemblyVersionPattern => null;
        string INamedEntity.Name => _identity.Name;

        IAssemblyReference IModuleReference.GetContainingAssembly(EmitContext context)
        {
            return this;
        }

        IDefinition? IReference.AsDefinition(EmitContext context)
        {
            return null;
        }

        void IReference.Dispatch(MetadataVisitor visitor)
        {
            visitor.Visit(this);
        }

        IEnumerable<ICustomAttribute> IReference.GetAttributes(EmitContext context)
        {
            return SpecializedCollections.EmptyEnumerable<ICustomAttribute>();
        }

        Symbols.ISymbolInternal? IReference.GetInternalSymbol() => null;
    }
}
