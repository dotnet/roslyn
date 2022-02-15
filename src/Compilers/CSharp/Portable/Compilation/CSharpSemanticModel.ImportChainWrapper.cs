// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class CSharpSemanticModel
    {
        private sealed class ImportChainWrapper : IImportChain
        {
            public IImportChain? Parent { get; }

            public ImmutableArray<IAliasSymbol> Aliases { get; }
            public ImmutableArray<IAliasSymbol> ExternAliases { get; }
            public ImmutableArray<INamespaceOrTypeSymbol> Imports { get; }

            public ImmutableArray<string> XmlNamespaces => ImmutableArray<string>.Empty;

            public ImportChainWrapper(
                IImportChain? parent,
                ImmutableArray<IAliasSymbol> aliases,
                ImmutableArray<IAliasSymbol> externAliases,
                ImmutableArray<INamespaceOrTypeSymbol> imports)
            {
                Parent = parent;
                Aliases = aliases;
                ExternAliases = externAliases;
                Imports = imports;
            }

            public static IImportChain? Convert(ImportChain? chain)
            {
                // Skip by any empty items in the chain.
                while (chain != null && chain.Imports.IsEmpty)
                    chain = chain.ParentOpt;

                // If we reached the end, there's nothing to return
                if (chain == null)
                    return null;

                var imports = chain.Imports;
                return new ImportChainWrapper(
                    Convert(chain.ParentOpt),
                    GetAliases(imports),
                    imports.ExternAliases.SelectAsArray(static e => e.Alias.GetPublicSymbol()),
                    imports.Usings.SelectAsArray(static n => n.NamespaceOrType.GetPublicSymbol()));
            }

            private static ImmutableArray<IAliasSymbol> GetAliases(Imports imports)
            {
                if (imports.UsingAliases.IsEmpty)
                    return ImmutableArray<IAliasSymbol>.Empty;

                var aliases = ArrayBuilder<IAliasSymbol>.GetInstance(imports.UsingAliases.Count);
                foreach (var kvp in imports.UsingAliases)
                    aliases.Add(kvp.Value.Alias.GetPublicSymbol());

                return aliases.ToImmutableAndFree();
            }
        }
    }
}
