// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class ExternAliasRecord
    {
        public readonly string Alias;

        public ExternAliasRecord(string alias)
        {
            Alias = alias;
        }

        /// <remarks>
        /// <paramref name="assemblyIdentityComparer"/> is only here to make the call
        /// pattern uniform - it's actually only used by one subtype.
        /// </remarks>
        public abstract int GetIndexOfTargetAssembly<TSymbol>(
            ImmutableArray<TSymbol> assembliesAndModules,
            AssemblyIdentityComparer assemblyIdentityComparer)
            where TSymbol : class, ISymbol;
    }
}
