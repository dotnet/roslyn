// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class PortableExternAliasRecord<TModuleSymbol> : ExternAliasRecord
        where TModuleSymbol : class, IModuleSymbol
    {
        private readonly TModuleSymbol _owningModule;
        private readonly PEModule _owningPEModule;
        private readonly AssemblyReferenceHandle _targetAssemblyHandle;

        public PortableExternAliasRecord(
            string alias,
            TModuleSymbol owningModule,
            PEModule owningPEModule,
            AssemblyReferenceHandle targetAssemblyHandle)
            : base(alias)
        {
            _owningModule = owningModule;
            _owningPEModule = owningPEModule;
            _targetAssemblyHandle = targetAssemblyHandle;
        }

        public override int GetIndexOfTargetAssembly<TSymbol>(
            ImmutableArray<TSymbol> assembliesAndModules,
            AssemblyIdentityComparer unused)
        {
            var index = _owningPEModule.GetAssemblyReferenceIndexOrThrow(_targetAssemblyHandle);
            var referencedAssembly = _owningModule.ReferencedAssemblySymbols[index];
            return assembliesAndModules.IndexOf((TSymbol)referencedAssembly);
        }
    }
}
