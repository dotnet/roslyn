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
        private readonly SymbolCategorizationService service;

        [ImportingConstructor]
        public SymbolCategorizationServiceFactory([ImportMany] IEnumerable<ISymbolCategorizer> symbolCategorizers)
        {
            service = new SymbolCategorizationService(symbolCategorizers);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return service;
        }

        internal class SymbolCategorizationService : ISymbolCategorizationService
        {
            private ImmutableArray<ISymbolCategorizer> _symbolCategorizers;

            public SymbolCategorizationService(IEnumerable<ISymbolCategorizer> symbolCategorizers)
            {
                this._symbolCategorizers = symbolCategorizers.ToImmutableArray();
            }

            public ImmutableArray<ISymbolCategorizer> GetCategorizers()
            {
                return _symbolCategorizers;
            }
        }
    }
}
