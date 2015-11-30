// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly bool _includeDirectReferences;
            private readonly Project _project;

            public ProjectSearchScope(Project project, bool includeDirectReferences, bool ignoreCase, CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                _project = project;
                _includeDirectReferences = includeDirectReferences;
            }

            public override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                return SymbolFinder.FindDeclarationsAsync(
                    _project, name, ignoreCase, filter, _includeDirectReferences, cancellationToken);
            }

            public override SymbolReference CreateReference(INamespaceOrTypeSymbol symbol)
            {
                return new ProjectSymbolReference(symbol, _project.Id);
            }
        }

        private class MetadataSearchScope : SearchScope
        {
            private readonly IAssemblySymbol _assembly;
            private readonly PortableExecutableReference _metadataReference;
            private readonly Solution _solution;

            public MetadataSearchScope(
                Solution solution,
                IAssemblySymbol assembly,
                PortableExecutableReference metadataReference,
                bool ignoreCase,
                CancellationToken cancellationToken)
                : base(ignoreCase, cancellationToken)
            {
                _solution = solution;
                _assembly = assembly;
                _metadataReference = metadataReference;
            }

            public override SymbolReference CreateReference(INamespaceOrTypeSymbol symbol)
            {
                return new MetadataSymbolReference(symbol, _metadataReference);
            }

            public override Task<IEnumerable<ISymbol>> FindDeclarationsAsync(string name, SymbolFilter filter)
            {
                return SymbolFinder.FindDeclarationsAsync(_solution, _assembly, _metadataReference.FilePath, name, ignoreCase, filter, cancellationToken);
            }
        }
    }
}
