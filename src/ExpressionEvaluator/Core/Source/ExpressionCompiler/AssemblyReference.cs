// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

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
        Version IAssemblyReference.AssemblyVersionPattern => null;
        string INamedEntity.Name => _identity.Name;

        IAssemblyReference IModuleReference.GetContainingAssembly(EmitContext context)
        {
            return this;
        }

        IDefinition IReference.AsDefinition(EmitContext context)
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
    }
}
