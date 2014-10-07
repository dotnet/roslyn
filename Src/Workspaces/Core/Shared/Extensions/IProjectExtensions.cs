using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Common.Semantics;
using Microsoft.CodeAnalysis.Common.Symbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IProjectExtensions
    {
        private static IEnumerable<INamespaceOrTypeSymbol> GetNamespacesOrTypes<T>(
            IEnumerable<Tuple<INamespaceOrTypeSymbol, T>> pairs)
        {
            return pairs.Select(t => t.Item1);
        }

        /// <summary>
        /// Searches namespaceSymbol (and all descendants) for types with the provided name.  If
        /// arity is negative then this will find types with any arity, otherwise the type's arity
        /// must match.  The namespaces containing the type will be returned.  Nested types will not
        /// be found with this search.
        /// </summary>
        public static async Task<IEnumerable<INamespaceSymbol>> FindNamespacesWithAccessibleTypeNameAsync(
            this Project project,
            ISymbol context,
            string typeName,
            int arity,
            CancellationToken cancellationToken)
        {
            var results = await FindNamespacesOrTypesWithAccessibleTypeNameAsync(project, context, typeName, arity, cancellationToken).ConfigureAwait(false);
            return results.OfType<INamespaceSymbol>();
        }

        public static async Task<IEnumerable<INamespaceOrTypeSymbol>> FindNamespacesOrTypesWithAccessibleTypeNameAsync(
            this Project project,
            ISymbol context,
            string typeName,
            int arity,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context);

            var results = await project.FindAccessibleTypesAndParentsAsync(context, typeName, arity, cancellationToken).ConfigureAwait(false);
            return GetNamespacesOrTypes(results);
        }

        public static async Task<IEnumerable<Tuple<INamespaceOrTypeSymbol, INamedTypeSymbol>>> FindAccessibleTypesAndParentsAsync(
            this Project project,
            ISymbol context,
            string typeName,
            int arity,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context);
            var semanticFacts = LanguageService.GetService<ISemanticFactsService>(project);
            var assembly = context as IAssemblySymbol ?? context.ContainingAssembly;

            var results = await project.FindTypesAndParentsAsync(typeName, arity, cancellationToken).ConfigureAwait(false);
            return results.Where(t => t.Item2.IsAccessibleWithin(assembly));
        }

        public static async Task<IEnumerable<Tuple<INamespaceOrTypeSymbol, INamedTypeSymbol>>> FindTypesAndParentsAsync(
            this Project project,
            string typeName,
            int arity,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            
            var stack = new Stack<INamespaceOrTypeSymbol>();
            stack.Push(compilation.GlobalNamespace);

            var results = new List<Tuple<INamespaceOrTypeSymbol, INamedTypeSymbol>>();

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = stack.Pop();

                var types = current is INamespaceSymbol
                    ? ((INamespaceSymbol)current).GetTypeMembers(typeName)
                    : ((INamedTypeSymbol)current).GetTypeMembers(typeName);

                foreach (var typeSymbol in types)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (arity < 0 || arity == typeSymbol.Arity)
                    {
                        results.Add(new Tuple<INamespaceOrTypeSymbol, INamedTypeSymbol>(current, typeSymbol));
                    }
                }

                var children = current is INamespaceSymbol
                    ? ((INamespaceSymbol)current).GetMembers()
                    : ((INamedTypeSymbol)current).GetTypeMembers().AsEnumerable();

                foreach (var child in children)
                {
                    stack.Push(child);
                }
            }

            return results;
        }
    }
}