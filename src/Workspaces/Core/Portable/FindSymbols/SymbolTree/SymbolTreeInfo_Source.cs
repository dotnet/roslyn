using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        public static async Task<SymbolTreeInfo> GetInfoForSourceAssemblyAsync(
            Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // We want to know about internal symbols from source assemblies.  Thre's a reasonable
            // chance a project might have IVT access to it.
            return await LoadOrCreateSourceSymbolTreeInfoAsync(
                project.Solution, compilation.Assembly, project.FilePath,
                loadOnly: false, includeInternal: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        internal static SymbolTreeInfo CreateSourceSymbolTreeInfo(
            Solution solution, VersionStamp version, IAssemblySymbol assembly,
            string filePath, bool includeInternal, CancellationToken cancellationToken)
        {
            if (assembly == null)
            {
                return null;
            }

            var unsortedNodes = new List<Node> { new Node(assembly.GlobalNamespace.Name, Node.RootNodeParentIndex) };

            var lookup = includeInternal ? s_getMembersNoPrivate : s_getMembersNoPrivateOrInternal;
            GenerateSourceNodes(assembly.GlobalNamespace, unsortedNodes, lookup);

            return CreateSymbolTreeInfo(solution, version, filePath, unsortedNodes);
        }

        // generate nodes for the global namespace an all descendants
        private static void GenerateSourceNodes(
            INamespaceSymbol globalNamespace,
            List<Node> list,
            Func<ISymbol, IEnumerable<ISymbol>> lookup)
        {
            // Add all child members
            var memberLookup = lookup(globalNamespace).ToLookup(c => c.Name);

            foreach (var grouping in memberLookup)
            {
                GenerateSourceNodes(grouping.Key, 0 /*index of root node*/, grouping, list, lookup);
            }
        }

        private static readonly Func<ISymbol, bool> s_useSymbolNoPrivate =
            s => s.CanBeReferencedByName && s.DeclaredAccessibility != Accessibility.Private;

        private static readonly Func<ISymbol, bool> s_useSymbolNoPrivateOrInternal =
            s => s.CanBeReferencedByName &&
            s.DeclaredAccessibility != Accessibility.Private &&
            s.DeclaredAccessibility != Accessibility.Internal;

        // generate nodes for symbols that share the same name, and all their descendants
        private static void GenerateSourceNodes(
            string name,
            int parentIndex,
            IEnumerable<ISymbol> symbolsWithSameName,
            List<Node> list,
            Func<ISymbol, IEnumerable<ISymbol>> lookup)
        {
            var node = new Node(name, parentIndex);
            var nodeIndex = list.Count;
            list.Add(node);

            // Add all child members
            var membersByName = symbolsWithSameName.SelectMany(lookup).ToLookup(s => s.Name);

            foreach (var grouping in membersByName)
            {
                GenerateSourceNodes(grouping.Key, nodeIndex, grouping, list, lookup);
            }
        }

        private static Func<ISymbol, IEnumerable<ISymbol>> s_getMembersNoPrivate = symbol =>
        {
            var nt = symbol as INamespaceOrTypeSymbol;
            return nt != null
                ? nt.GetMembers().Where(s_useSymbolNoPrivate)
                : SpecializedCollections.EmptyEnumerable<ISymbol>();
        };

        private static Func<ISymbol, IEnumerable<ISymbol>> s_getMembersNoPrivateOrInternal = symbol =>
        {
            var nt = symbol as INamespaceOrTypeSymbol;
            return nt != null
                ? nt.GetMembers().Where(s_useSymbolNoPrivateOrInternal)
                : SpecializedCollections.EmptyEnumerable<ISymbol>();
        };
    }
}