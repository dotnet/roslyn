// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorParsingPhase : RazorEnginePhaseBase, IRazorParsingPhase
{
    private static readonly ConditionalWeakTable<RazorSourceDocument, RazorSyntaxTree> s_importTrees = new();

#if !NET
    private static readonly object s_importTreesLock = new();
#endif

    protected override RazorCodeDocument ExecuteCore(RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var options = codeDocument.ParserOptions;
        var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source, options, cancellationToken);

        using var importSyntaxTrees = new PooledArrayBuilder<RazorSyntaxTree>(codeDocument.Imports.Length);

        foreach (var import in codeDocument.Imports)
        {
            // Attempt to pull the parsed import tree from the CWT
            if (!TryGetCachedImportTree(import, options, out var tree))
            {
                // We don't have a cached version, parse the import and add it to the CWT
                tree = RazorSyntaxTree.Parse(import, options, cancellationToken);

#if NET
                s_importTrees.AddOrUpdate(import, tree);
#else
                // NetStandard2.0 doesn't have a nice AddOrUpdate method, so we'll use our own locking to
                // ensure the CWT is updated correctly.
                lock (s_importTreesLock)
                {
                    if (TryGetCachedImportTree(import, options, out var cachedTree))
                    {
                        // Someone else added it while we were parsing, use theirs.
                        tree = cachedTree;
                    }
                    else
                    {
                        if (cachedTree is not null)
                        {
                            // If there is a cachedTree, it must have different options. Remove it from the cache
                            s_importTrees.Remove(import);
                        }

                        // Add the tree we created to the cache
                        s_importTrees.Add(import, tree);
                    }
                }
#endif
            }

            importSyntaxTrees.Add(tree);
        }

        return codeDocument
            .WithSyntaxTree(syntaxTree)
            .WithImportSyntaxTrees(importSyntaxTrees.ToImmutableAndClear());

        static bool TryGetCachedImportTree(RazorSourceDocument import, RazorParserOptions options, [NotNullWhen(true)] out RazorSyntaxTree? tree)
            => s_importTrees.TryGetValue(import, out tree) && tree.Options.Equals(options);
    }
}
