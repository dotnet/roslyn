// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            /// Mapping from a syntax tree to the last fully computed member names for each the types (in lexical order)
            /// for this file.  Specifically, the key of the collection is the tree the data is cached for.  The <see
            /// cref="OneOrMany"/> is a compact array of items, each of which corresponds to the prior type-declaration
            /// in the tree that contributed members (in lexical order).  Each item in that compact array is then the
            /// member names for that particular type declaration.
            /// </summary>
            /// <remarks>
            /// Member names often don't change for most edits, so being able to reuse the same set from the last time
            /// things were computed saves on a lot of memory churn producing the new set, then GC'ing the last set
            /// (esp. for very large types). The value is stored as a <see cref="OneOrMany"/> as the most common case
            /// for most files is a single type declaration.
            /// <para/>
            /// We store this with weak references so that we can obtain this optimization in the common case where the 
            /// old declaration is still around rooting the old names.  If the decls are gone though, the names are subject
            /// to being cleaned up by the GC and we may not be able to use them.
            /// </remarks>
            internal readonly ImmutableDictionary<SyntaxTree, OneOrMany<WeakReference<StrongBox<ImmutableSegmentedHashSet<string>>>>> LastComputedMemberNames;
            internal readonly DeclarationTable DeclarationTable;

            internal State(
                ImmutableArray<SyntaxTree> syntaxTrees,
                ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap,
                ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
                ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap,
                ImmutableDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> rootNamespaces,
                ImmutableDictionary<SyntaxTree, OneOrMany<WeakReference<StrongBox<ImmutableSegmentedHashSet<string>>>>> lastComputedMemberNames,
                DeclarationTable declarationTable)
            {
                Debug.Assert(syntaxTrees.All(tree => syntaxTrees[syntaxTreeOrdinalMap[tree]] == tree));
                Debug.Assert(syntaxTrees.SetEquals(rootNamespaces.Keys.AsImmutable(), EqualityComparer<SyntaxTree>.Default));

                this.SyntaxTrees = syntaxTrees;
                this.OrdinalMap = syntaxTreeOrdinalMap;
                this.LoadDirectiveMap = loadDirectiveMap;
                this.LoadedSyntaxTreeMap = loadedSyntaxTreeMap;
                this.RootNamespaces = rootNamespaces;
                this.LastComputedMemberNames = lastComputedMemberNames;
                this.DeclarationTable = declarationTable;
            }
        }
    }
}
