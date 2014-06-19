using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis
{
    internal sealed class LocalScopeProvider : Microsoft.Cci.ILocalScopeProvider
    {
        internal static readonly LocalScopeProvider Instance = new LocalScopeProvider();

        private LocalScopeProvider()
        {
        }

        public ImmutableArray<Microsoft.Cci.ILocalScope> GetLocalScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return methodBody.LocalScopes;
        }

        public ImmutableArray<Microsoft.Cci.INamespaceScope> GetNamespaceScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return methodBody.NamespaceScopes;
        }

        public ImmutableArray<Microsoft.Cci.ILocalScope> GetIteratorScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return methodBody.IteratorScopes;
        }

        public IEnumerable<Microsoft.Cci.ILocalDefinition> GetConstantsInScope(Microsoft.Cci.ILocalScope scope)
        {
            return ((LocalScope)scope).Constants;
        }

        public IEnumerable<Microsoft.Cci.ILocalDefinition> GetVariablesInScope(Microsoft.Cci.ILocalScope scope)
        {
            return ((LocalScope)scope).Variables;
        }

        public string IteratorClassName(Microsoft.Cci.IMethodBody methodBody)
        {
            return methodBody.IteratorClassName;
        }
    }
}