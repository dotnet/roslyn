// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class SyntaxAndDeclarationManager : CommonSyntaxAndDeclarationManager
    {
        internal sealed class State
        {
            internal readonly ImmutableArray<SyntaxTree> SyntaxTrees; // In ordinal order.
            internal readonly ImmutableDictionary<SyntaxTree, int> OrdinalMap; // Inverse of syntaxTrees array (i.e. maps tree to index)
            internal readonly ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> LoadDirectiveMap;
            internal readonly ImmutableDictionary<string, SyntaxTree> LoadedSyntaxTreeMap;
            internal readonly ImmutableDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> RootNamespaces;

            /// <summary>
            /// The last fully computed member names for the top-most type (in lexicographic order) for this file.
            /// Member names often don't change for most edits, so being able to reuse the same set from the last time
            /// things were computed saves on a lot of memory churn producing the new set, then GC'ing the last set
            /// (esp. for very large types).  We only track top-level types as that is the most common case for all
            /// files is just to have a single type, and by only tracking that we vastly simplify keeping track and
            /// mapping this state forward (otherwise, we'd have to have to keep track of which type in the file these
            /// member names corresponded to).
            /// </summary>
            internal readonly ImmutableDictionary<SyntaxTree, ImmutableSegmentedHashSet<string>> LastComputedTopLevelTypeMemberNames;
            internal readonly DeclarationTable DeclarationTable;

            internal State(
                ImmutableArray<SyntaxTree> syntaxTrees,
                ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap,
                ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
                ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap,
                ImmutableDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> rootNamespaces,
                ImmutableDictionary<SyntaxTree, ImmutableSegmentedHashSet<string>> lastComputedTopLevelTypeMemberNames,
                DeclarationTable declarationTable)
            {
                Debug.Assert(syntaxTrees.All(tree => syntaxTrees[syntaxTreeOrdinalMap[tree]] == tree));
                Debug.Assert(syntaxTrees.SetEquals(rootNamespaces.Keys.AsImmutable(), EqualityComparer<SyntaxTree>.Default));

                this.SyntaxTrees = syntaxTrees;
                this.OrdinalMap = syntaxTreeOrdinalMap;
                this.LoadDirectiveMap = loadDirectiveMap;
                this.LoadedSyntaxTreeMap = loadedSyntaxTreeMap;
                this.RootNamespaces = rootNamespaces;
                this.LastComputedTopLevelTypeMemberNames = lastComputedTopLevelTypeMemberNames;
                this.DeclarationTable = declarationTable;
            }
        }
    }
}
