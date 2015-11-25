using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider
    {
        private abstract class SearchScope
        {
            protected readonly bool ignoreCase;
            protected readonly CancellationToken cancellationToken;

            protected SearchScope(bool ignoreCase, CancellationToken cancellationToken)
            {
                this.ignoreCase = ignoreCase;
                this.cancellationToken = cancellationToken;
            }

            public abstract Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter);
            public abstract SymbolReference CreateReference(INamespaceOrTypeSymbol symbol);
        }

        private class ProjectSearchScope : SearchScope
        {
            private readonly bool includeDirectReferences;
            private readonly Project project;

            public ProjectSearchScope(Project project, bool includeDirectReferences, bool ignoreCase, CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                this.project = project;
                this.includeDirectReferences = includeDirectReferences;
            }

            public override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    project, name, ignoreCase, filter, includeDirectReferences, cancellationToken);
            }

            public override SymbolReference CreateReference(INamespaceOrTypeSymbol symbol)
            {
                return new ProjectSymbolReference(symbol, project.Id);
            }
        }

        private class MetadataSearchScope : SearchScope
        {
            private readonly IAssemblySymbol assembly;
            private readonly PortableExecutableReference metadataReference;
            private readonly Solution solution;

            public MetadataSearchScope(
                Solution solution,
                IAssemblySymbol assembly,
                PortableExecutableReference metadataReference,
                bool ignoreCase,
                CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                this.solution = solution;
                this.assembly = assembly;
                this.metadataReference = metadataReference;
            }

            public override SymbolReference CreateReference(INamespaceOrTypeSymbol symbol)
            {
                return new MetadataSymbolReference(symbol, metadataReference);
            }

            public override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                return SymbolFinder.FindDeclarationsAsync(solution, assembly, metadataReference.FilePath, name, ignoreCase, filter, cancellationToken);
            }
        }
    }
}
