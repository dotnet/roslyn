// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsService<T> : ILanguageService, IEqualityComparer<T> where T : SyntaxNode
    {
        protected abstract IEnumerable<T> GetUnusedUsings(SemanticModel model, SyntaxNode root, CancellationToken cancellationToken);

        protected async Task<HashSet<T>> GetCommonUnnecessaryImportsOfAllContextAsync(Document document, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var unnecessaryImports = new HashSet<T>(GetUnnecessaryImportsOrEmpty(model, root, cancellationToken), this);
            foreach (var current in document.GetLinkedDocuments())
            {
                var currentModel = await current.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var currentRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                unnecessaryImports.IntersectWith(GetUnnecessaryImportsOrEmpty(currentModel, currentRoot, cancellationToken));
            }

            return unnecessaryImports;
        }

        private IEnumerable<T> GetUnnecessaryImportsOrEmpty(SemanticModel model, SyntaxNode root, CancellationToken cancellationToken)
        {
            var imports = GetUnusedUsings(model, root, cancellationToken) ?? SpecializedCollections.EmptyEnumerable<T>();
            return imports.Cast<T>();
        }

        bool IEqualityComparer<T>.Equals(T x, T y)
        {
            return x.Span == y.Span;
        }

        int IEqualityComparer<T>.GetHashCode(T obj)
        {
            return obj.Span.GetHashCode();
        }
    }
}
