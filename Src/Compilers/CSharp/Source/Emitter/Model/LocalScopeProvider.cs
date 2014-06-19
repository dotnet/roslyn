using System.Collections.Generic;
using Roslyn.Compilers.CodeGen;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    internal class LocalScopeProvider : Microsoft.Cci.ILocalScopeProvider
    {
        public IEnumerable<Microsoft.Cci.ILocalScope> GetLocalScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return ((MethodBody)methodBody).LocalScopes;
        }

        public IEnumerable<Microsoft.Cci.INamespaceScope> GetNamespaceScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return ((MethodBody)methodBody).NamespaceScopes;
        }

        public IList<Microsoft.Cci.ILocalScope> GetIteratorScopes(Microsoft.Cci.IMethodBody methodBody)
        {
            return ((MethodBody)methodBody).IteratorScopes;
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
            return ((MethodBody)methodBody).IteratorClassName;
        }
    }
}