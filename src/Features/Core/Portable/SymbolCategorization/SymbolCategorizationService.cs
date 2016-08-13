// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SymbolCategorization
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolCategorizationService)), Shared]
    internal class SymbolCategorizationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly SymbolCategorizationService _service;

        [ImportingConstructor]
        public SymbolCategorizationServiceFactory([ImportMany] IEnumerable<ISymbolCategorizer> symbolCategorizers)
        {
            _service = new SymbolCategorizationService(symbolCategorizers);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _service;
        }

        internal class SymbolCategorizationService : ISymbolCategorizationService
        {
            private ImmutableArray<ISymbolCategorizer> _symbolCategorizers;

            public SymbolCategorizationService(IEnumerable<ISymbolCategorizer> symbolCategorizers)
            {
                _symbolCategorizers = symbolCategorizers.ToImmutableArray();
            }

            public ImmutableArray<ISymbolCategorizer> GetCategorizers()
            {
                return _symbolCategorizers;
            }
        }
    }
}
