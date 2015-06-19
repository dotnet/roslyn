// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class NativeExternAliasRecord<TAssemblySymbol> : ExternAliasRecord
        where TAssemblySymbol : class, IAssemblySymbol
    {
        private readonly AssemblyIdentity _targetAssemblyIdentity;

        public NativeExternAliasRecord(
            string alias,
            AssemblyIdentity targetAssemblyIdentity)
            : base(alias)
        {
            _targetAssemblyIdentity = targetAssemblyIdentity;
        }

        public override int GetIndexOfTargetAssembly<TSymbol>(
            ImmutableArray<TSymbol> assembliesAndModules,
            AssemblyIdentityComparer assemblyIdentityComparer)
        {
            for (int i = 0; i < assembliesAndModules.Length; i++)
            {
                var assembly = assembliesAndModules[i] as TAssemblySymbol;
                if (assembly != null && assemblyIdentityComparer.ReferenceMatchesDefinition(_targetAssemblyIdentity, assembly.Identity))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
