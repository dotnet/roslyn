// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
            internal readonly DeclarationTableInput DeclarationTableInput;

            internal State(
                ImmutableArray<SyntaxTree> syntaxTrees,
                ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap,
                ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
                ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap,
                ImmutableDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> rootNamespaces,
                DeclarationTableInput declarationTableInput)
            {
                Debug.Assert(syntaxTrees.All(tree => syntaxTrees[syntaxTreeOrdinalMap[tree]] == tree));
                Debug.Assert(syntaxTrees.SetEquals(rootNamespaces.Keys.AsImmutable(), EqualityComparer<SyntaxTree>.Default));

                this.SyntaxTrees = syntaxTrees;
                this.OrdinalMap = syntaxTreeOrdinalMap;
                this.LoadDirectiveMap = loadDirectiveMap;
                this.LoadedSyntaxTreeMap = loadedSyntaxTreeMap;
                this.RootNamespaces = rootNamespaces;
                this.DeclarationTableInput = declarationTableInput;
            }
        }
    }
}
