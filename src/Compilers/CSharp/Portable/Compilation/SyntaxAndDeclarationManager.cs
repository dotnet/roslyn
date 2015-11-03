// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class SyntaxAndDeclarationManager : CommonSyntaxAndDeclarationManager
    {
        private State _lazyState;

        internal SyntaxAndDeclarationManager(
            ImmutableArray<SyntaxTree> externalSyntaxTrees,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission,
            State state)
            : base(externalSyntaxTrees, scriptClassName, resolver, messageProvider, isSubmission)
        {
            _lazyState = state;
        }

        internal State GetLazyState()
        {
            if (_lazyState == null)
            {
                Interlocked.CompareExchange(ref _lazyState, CreateState(this.ExternalSyntaxTrees, this.ScriptClassName, this.Resolver, this.MessageProvider, this.IsSubmission), null);
            }

            return _lazyState;
        }

        private static State CreateState(
            ImmutableArray<SyntaxTree> externalSyntaxTrees,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission)
        {
            var treesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var ordinalMapBuilder = PooledDictionary<SyntaxTree, int>.GetInstance();
            var loadDirectiveMapBuilder = PooledDictionary<SyntaxTree, ImmutableArray<LoadDirective>>.GetInstance();
            var loadedSyntaxTreeMapBuilder = PooledDictionary<string, SyntaxTree>.GetInstance();
            var declMapBuilder = PooledDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>>.GetInstance();
            var declTable = DeclarationTable.Empty;

            foreach (var tree in externalSyntaxTrees)
            {
                AppendAllSyntaxTrees(
                    treesBuilder,
                    tree,
                    scriptClassName,
                    resolver,
                    messageProvider,
                    isSubmission,
                    ordinalMapBuilder,
                    loadDirectiveMapBuilder,
                    loadedSyntaxTreeMapBuilder,
                    declMapBuilder,
                    ref declTable);
            }

            return new State(
                treesBuilder.ToImmutableAndFree(),
                ordinalMapBuilder.ToImmutableDictionaryAndFree(),
                loadDirectiveMapBuilder.ToImmutableDictionaryAndFree(),
                loadedSyntaxTreeMapBuilder.ToImmutableDictionaryAndFree(),
                declMapBuilder.ToImmutableDictionaryAndFree(),
                declTable);
        }

        public SyntaxAndDeclarationManager AddSyntaxTrees(IEnumerable<SyntaxTree> trees)
        {
            var scriptClassName = this.ScriptClassName;
            var resolver = this.Resolver;
            var messageProvider = this.MessageProvider;
            var isSubmission = this.IsSubmission;

            var state = _lazyState;
            var newExternalSyntaxTrees = this.ExternalSyntaxTrees.AddRange(trees);
            if (state == null)
            {
                return this.WithExternalSyntaxTrees(newExternalSyntaxTrees);
            }

            var ordinalMapBuilder = state.OrdinalMap.ToBuilder();
            var loadDirectiveMapBuilder = state.LoadDirectiveMap.ToBuilder();
            var loadedSyntaxTreeMapBuilder = state.LoadedSyntaxTreeMap.ToBuilder();
            var declMapBuilder = state.RootNamespaces.ToBuilder();
            var declTable = state.DeclarationTable;

            var treesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            treesBuilder.AddRange(state.SyntaxTrees);

            foreach (var tree in trees)
            {
                AppendAllSyntaxTrees(
                        treesBuilder,
                        tree,
                        scriptClassName,
                        resolver,
                        messageProvider,
                        isSubmission,
                        ordinalMapBuilder,
                        loadDirectiveMapBuilder,
                        loadedSyntaxTreeMapBuilder,
                        declMapBuilder,
                        ref declTable);
            }

            state = new State(
                treesBuilder.ToImmutableAndFree(),
                ordinalMapBuilder.ToImmutableDictionary(),
                loadDirectiveMapBuilder.ToImmutableDictionary(),
                loadedSyntaxTreeMapBuilder.ToImmutableDictionary(),
                declMapBuilder.ToImmutableDictionary(),
                declTable);

            return new SyntaxAndDeclarationManager(
                newExternalSyntaxTrees,
                scriptClassName,
                resolver,
                messageProvider,
                isSubmission,
                state);
        }

        /// <summary>
        /// Appends all trees (including any trees from #load'ed files).
        /// </summary>
        private static void AppendAllSyntaxTrees(
            ArrayBuilder<SyntaxTree> treesBuilder,
            SyntaxTree tree,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission,
            IDictionary<SyntaxTree, int> ordinalMapBuilder,
            IDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMapBuilder,
            IDictionary<string, SyntaxTree> loadedSyntaxTreeMapBuilder,
            IDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> declMapBuilder,
            ref DeclarationTable declTable)
        {
            var sourceCodeKind = tree.Options.Kind;
            if (sourceCodeKind == SourceCodeKind.Script)
            {
                AppendAllLoadedSyntaxTrees(treesBuilder, tree, scriptClassName, resolver, messageProvider, isSubmission, ordinalMapBuilder, loadDirectiveMapBuilder, loadedSyntaxTreeMapBuilder, declMapBuilder, ref declTable);
            }

            AddSyntaxTreeToDeclarationMapAndTable(tree, scriptClassName, isSubmission, declMapBuilder, ref declTable);

            treesBuilder.Add(tree);

            ordinalMapBuilder.Add(tree, ordinalMapBuilder.Count);
        }

        private static void AppendAllLoadedSyntaxTrees(
            ArrayBuilder<SyntaxTree> treesBuilder,
            SyntaxTree tree,
            string scriptClassName,
            SourceReferenceResolver resolver,
            CommonMessageProvider messageProvider,
            bool isSubmission,
            IDictionary<SyntaxTree, int> ordinalMapBuilder,
            IDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMapBuilder,
            IDictionary<string, SyntaxTree> loadedSyntaxTreeMapBuilder,
            IDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> declMapBuilder,
            ref DeclarationTable declTable)
        {
            ArrayBuilder<LoadDirective> loadDirectives = null;

            foreach (var directive in tree.GetCompilationUnitRoot().GetLoadDirectives())
            {
                var fileToken = directive.File;
                var path = (string)fileToken.Value;
                if (path == null)
                {
                    // If there is no path, the parser should have some Diagnostics to report (if we're in an active region).
                    Debug.Assert(!directive.IsActive || tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error));
                    continue;
                }

                var diagnostics = DiagnosticBag.GetInstance();
                string resolvedFilePath = null;
                if (resolver == null)
                {
                    diagnostics.Add(
                        messageProvider.CreateDiagnostic(
                            (int)ErrorCode.ERR_SourceFileReferencesNotSupported,
                            directive.Location));
                }
                else
                {
                    resolvedFilePath = resolver.ResolveReference(path, baseFilePath: tree.FilePath);
                    if (resolvedFilePath == null)
                    {
                        diagnostics.Add(
                            messageProvider.CreateDiagnostic(
                                (int)ErrorCode.ERR_NoSourceFile,
                                fileToken.GetLocation(),
                                path,
                                CSharpResources.CouldNotFindFile));
                    }
                    else if (!loadedSyntaxTreeMapBuilder.ContainsKey(resolvedFilePath))
                    {
                        try
                        {
                            var code = resolver.ReadText(resolvedFilePath);
                            var loadedTree = SyntaxFactory.ParseSyntaxTree(
                                code,
                                tree.Options, // Use ParseOptions propagated from "external" tree.
                                resolvedFilePath);

                            // All #load'ed trees should have unique path information.
                            loadedSyntaxTreeMapBuilder.Add(loadedTree.FilePath, loadedTree);

                            AppendAllSyntaxTrees(
                                treesBuilder,
                                loadedTree,
                                scriptClassName,
                                resolver,
                                messageProvider,
                                isSubmission,
                                ordinalMapBuilder,
                                loadDirectiveMapBuilder,
                                loadedSyntaxTreeMapBuilder,
                                declMapBuilder,
                                ref declTable);
                        }
                        catch (Exception e)
                        {
                            diagnostics.Add(
                                CommonCompiler.ToFileReadDiagnostics(messageProvider, e, resolvedFilePath),
                                fileToken.GetLocation());
                        }
                    }
                    else
                    {
                        // The path resolved, but we've seen this file before,
                        // so don't attempt to load it again.
                        Debug.Assert(diagnostics.IsEmptyWithoutResolution);
                    }
                }

                if (loadDirectives == null)
                {
                    loadDirectives = ArrayBuilder<LoadDirective>.GetInstance();
                }
                loadDirectives.Add(new LoadDirective(resolvedFilePath, diagnostics.ToReadOnlyAndFree()));
            }

            if (loadDirectives != null)
            {
                loadDirectiveMapBuilder.Add(tree, loadDirectives.ToImmutableAndFree());
            }
        }

        private static void AddSyntaxTreeToDeclarationMapAndTable(
            SyntaxTree tree,
            string scriptClassName,
            bool isSubmission,
            IDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> declMapBuilder,
            ref DeclarationTable declTable)
        {
            var lazyRoot = new Lazy<RootSingleNamespaceDeclaration>(() => DeclarationTreeBuilder.ForTree(tree, scriptClassName, isSubmission));
            declMapBuilder.Add(tree, lazyRoot); // Callers are responsible for checking for existing entries.
            declTable = declTable.AddRootDeclaration(lazyRoot);
        }

        public SyntaxAndDeclarationManager RemoveSyntaxTrees(HashSet<SyntaxTree> trees)
        {
            var state = _lazyState;
            var newExternalSyntaxTrees = this.ExternalSyntaxTrees.RemoveAll(t => trees.Contains(t));
            if (state == null)
            {
                return this.WithExternalSyntaxTrees(newExternalSyntaxTrees);
            }

            var syntaxTrees = state.SyntaxTrees;
            var loadDirectiveMap = state.LoadDirectiveMap;
            var loadedSyntaxTreeMap = state.LoadedSyntaxTreeMap;
            var removeSet = PooledHashSet<SyntaxTree>.GetInstance();
            foreach (var tree in trees)
            {
                int unused1;
                ImmutableArray<LoadDirective> unused2;
                GetRemoveSet(
                    tree,
                    includeLoadedTrees: true,
                    syntaxTrees: syntaxTrees,
                    syntaxTreeOrdinalMap: state.OrdinalMap,
                    loadDirectiveMap: loadDirectiveMap,
                    loadedSyntaxTreeMap: loadedSyntaxTreeMap,
                    removeSet: removeSet,
                    totalReferencedTreeCount: out unused1,
                    oldLoadDirectives: out unused2);
            }

            var treesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
            var ordinalMapBuilder = PooledDictionary<SyntaxTree, int>.GetInstance();
            var declMapBuilder = state.RootNamespaces.ToBuilder();
            var declTable = state.DeclarationTable;
            foreach (var tree in syntaxTrees)
            {
                if (removeSet.Contains(tree))
                {
                    loadDirectiveMap = loadDirectiveMap.Remove(tree);
                    loadedSyntaxTreeMap = loadedSyntaxTreeMap.Remove(tree.FilePath);
                    RemoveSyntaxTreeFromDeclarationMapAndTable(tree, declMapBuilder, ref declTable);
                }
                else if (!IsLoadedSyntaxTree(tree, loadedSyntaxTreeMap))
                {
                    UpdateSyntaxTreesAndOrdinalMapOnly(
                        treesBuilder,
                        tree,
                        ordinalMapBuilder,
                        loadDirectiveMap,
                        loadedSyntaxTreeMap);
                }
            }
            removeSet.Free();

            state = new State(
                treesBuilder.ToImmutableAndFree(),
                ordinalMapBuilder.ToImmutableDictionaryAndFree(),
                loadDirectiveMap,
                loadedSyntaxTreeMap,
                declMapBuilder.ToImmutableDictionary(),
                declTable);

            return new SyntaxAndDeclarationManager(
                newExternalSyntaxTrees,
                this.ScriptClassName,
                this.Resolver,
                this.MessageProvider,
                this.IsSubmission,
                state);
        }

        /// <summary>
        /// Collects all the trees #load'ed by <paramref name="oldTree"/> (as well as
        /// <paramref name="oldTree"/> itself) and populates <paramref name="removeSet"/>
        /// with all the trees that are safe to remove (not #load'ed by any other tree).
        /// </summary>
        private static void GetRemoveSet(
            SyntaxTree oldTree,
            bool includeLoadedTrees,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableDictionary<SyntaxTree, int> syntaxTreeOrdinalMap,
            ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
            ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap,
            HashSet<SyntaxTree> removeSet,
            out int totalReferencedTreeCount,
            out ImmutableArray<LoadDirective> oldLoadDirectives)
        {
            if (includeLoadedTrees && loadDirectiveMap.TryGetValue(oldTree, out oldLoadDirectives))
            {
                Debug.Assert(!oldLoadDirectives.IsEmpty);
                GetRemoveSetForLoadedTrees(oldLoadDirectives, loadDirectiveMap, loadedSyntaxTreeMap, removeSet);
            }

            removeSet.Add(oldTree);
            totalReferencedTreeCount = removeSet.Count;

            // Check subsequent trees to see if they #load any of the trees we're about
            // to remove.  We don't want to remove a tree until the last external tree
            // that depends on it is removed.
            if (removeSet.Count > 1)
            {
                var ordinal = syntaxTreeOrdinalMap[oldTree];
                for (int i = ordinal + 1; i < syntaxTrees.Length; i++)
                {
                    var tree = syntaxTrees[i];
                    ImmutableArray<LoadDirective> loadDirectives;
                    if (loadDirectiveMap.TryGetValue(tree, out loadDirectives))
                    {
                        Debug.Assert(!loadDirectives.IsEmpty);
                        foreach (var directive in loadDirectives)
                        {
                            var loadedTree = loadedSyntaxTreeMap[directive.ResolvedPath];
                            if (removeSet.Contains(loadedTree))
                            {
                                removeSet.Remove(loadedTree);
                            }
                        }
                    }
                }
            }
        }

        private static void GetRemoveSetForLoadedTrees(
            ImmutableArray<LoadDirective> loadDirectives,
            ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
            ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap,
            HashSet<SyntaxTree> removeSet)
        {
            foreach (var directive in loadDirectives)
            {
                if (directive.ResolvedPath != null)
                {
                    var loadedTree = loadedSyntaxTreeMap[directive.ResolvedPath];
                    ImmutableArray<LoadDirective> nestedLoadDirectives;
                    if (loadDirectiveMap.TryGetValue(loadedTree, out nestedLoadDirectives))
                    {
                        Debug.Assert(!nestedLoadDirectives.IsEmpty);
                        GetRemoveSetForLoadedTrees(nestedLoadDirectives, loadDirectiveMap, loadedSyntaxTreeMap, removeSet);
                    }

                    removeSet.Add(loadedTree);
                }
            }
        }

        private static void RemoveSyntaxTreeFromDeclarationMapAndTable(
            SyntaxTree tree,
            IDictionary<SyntaxTree, Lazy<RootSingleNamespaceDeclaration>> declMap,
            ref DeclarationTable declTable)
        {
            var lazyRoot = declMap[tree];
            declTable = declTable.RemoveRootDeclaration(lazyRoot);
            declMap.Remove(tree);
        }

        public SyntaxAndDeclarationManager ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
            var state = _lazyState;
            var newExternalSyntaxTrees = this.ExternalSyntaxTrees.Replace(oldTree, newTree);
            if (state == null)
            {
                return this.WithExternalSyntaxTrees(newExternalSyntaxTrees);
            }

            var newLoadDirectivesSyntax = newTree.GetCompilationUnitRoot().GetLoadDirectives();
            var loadDirectivesHaveChanged = !oldTree.GetCompilationUnitRoot().GetLoadDirectives().SequenceEqual(newLoadDirectivesSyntax);
            var syntaxTrees = state.SyntaxTrees;
            var ordinalMap = state.OrdinalMap;
            var loadDirectiveMap = state.LoadDirectiveMap;
            var loadedSyntaxTreeMap = state.LoadedSyntaxTreeMap;
            var removeSet = PooledHashSet<SyntaxTree>.GetInstance();
            int totalReferencedTreeCount;
            ImmutableArray<LoadDirective> oldLoadDirectives;
            GetRemoveSet(
                oldTree,
                loadDirectivesHaveChanged,
                syntaxTrees,
                ordinalMap,
                loadDirectiveMap,
                loadedSyntaxTreeMap,
                removeSet,
                out totalReferencedTreeCount,
                out oldLoadDirectives);

            var loadDirectiveMapBuilder = loadDirectiveMap.ToBuilder();
            var loadedSyntaxTreeMapBuilder = loadedSyntaxTreeMap.ToBuilder();
            var declMapBuilder = state.RootNamespaces.ToBuilder();
            var declTable = state.DeclarationTable;
            foreach (var tree in removeSet)
            {
                loadDirectiveMapBuilder.Remove(tree);
                loadedSyntaxTreeMapBuilder.Remove(tree.FilePath);
                RemoveSyntaxTreeFromDeclarationMapAndTable(tree, declMapBuilder, ref declTable);
            }
            removeSet.Free();

            var oldOrdinal = ordinalMap[oldTree];
            ImmutableArray<SyntaxTree> newTrees;
            if (loadDirectivesHaveChanged)
            {
                // Should have been removed above...
                Debug.Assert(!loadDirectiveMapBuilder.ContainsKey(oldTree));
                Debug.Assert(!loadDirectiveMapBuilder.ContainsKey(newTree));

                // If we're inserting new #load'ed trees, we'll rebuild
                // the whole syntaxTree array and the ordinalMap.
                var treesBuilder = ArrayBuilder<SyntaxTree>.GetInstance();
                var ordinalMapBuilder = PooledDictionary<SyntaxTree, int>.GetInstance();
                for (var i = 0; i <= (oldOrdinal - totalReferencedTreeCount); i++)
                {
                    var tree = syntaxTrees[i];
                    treesBuilder.Add(tree);
                    ordinalMapBuilder.Add(tree, i);
                }

                AppendAllSyntaxTrees(
                    treesBuilder,
                    newTree,
                    this.ScriptClassName,
                    this.Resolver,
                    this.MessageProvider,
                    this.IsSubmission,
                    ordinalMapBuilder,
                    loadDirectiveMapBuilder,
                    loadedSyntaxTreeMapBuilder,
                    declMapBuilder,
                    ref declTable);

                for (var i = oldOrdinal + 1; i < syntaxTrees.Length; i++)
                {
                    var tree = syntaxTrees[i];
                    if (!IsLoadedSyntaxTree(tree, loadedSyntaxTreeMap))
                    {
                        UpdateSyntaxTreesAndOrdinalMapOnly(
                            treesBuilder,
                            tree,
                            ordinalMapBuilder,
                            loadDirectiveMap,
                            loadedSyntaxTreeMap);
                    }
                }

                newTrees = treesBuilder.ToImmutableAndFree();
                ordinalMap = ordinalMapBuilder.ToImmutableDictionaryAndFree();
                Debug.Assert(newTrees.Length == ordinalMap.Count);
            }
            else
            {
                AddSyntaxTreeToDeclarationMapAndTable(newTree, this.ScriptClassName, this.IsSubmission, declMapBuilder, ref declTable);

                if (newLoadDirectivesSyntax.Any())
                {
                    // If load directives have not changed and there are new directives,
                    // then there should have been (matching) old directives as well.
                    Debug.Assert(!oldLoadDirectives.IsDefault);
                    Debug.Assert(!oldLoadDirectives.IsEmpty);
                    Debug.Assert(oldLoadDirectives.Length == newLoadDirectivesSyntax.Count);
                    loadDirectiveMapBuilder[newTree] = oldLoadDirectives;
                }

                Debug.Assert(ordinalMap.ContainsKey(oldTree)); // Checked by RemoveSyntaxTreeFromDeclarationMapAndTable

                newTrees = syntaxTrees.SetItem(oldOrdinal, newTree);

                ordinalMap = ordinalMap.Remove(oldTree);
                ordinalMap = ordinalMap.SetItem(newTree, oldOrdinal);
            }

            state = new State(
                newTrees,
                ordinalMap,
                loadDirectiveMapBuilder.ToImmutable(),
                loadedSyntaxTreeMapBuilder.ToImmutable(),
                declMapBuilder.ToImmutable(),
                declTable);

            return new SyntaxAndDeclarationManager(
                newExternalSyntaxTrees,
                this.ScriptClassName,
                this.Resolver,
                this.MessageProvider,
                this.IsSubmission,
                state);
        }

        internal SyntaxAndDeclarationManager WithExternalSyntaxTrees(ImmutableArray<SyntaxTree> trees)
        {
            return new SyntaxAndDeclarationManager(trees, this.ScriptClassName, this.Resolver, this.MessageProvider, this.IsSubmission, state: null);
        }

        internal static bool IsLoadedSyntaxTree(SyntaxTree tree, ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap)
        {
            SyntaxTree loadedTree;
            return loadedSyntaxTreeMap.TryGetValue(tree.FilePath, out loadedTree) && (tree == loadedTree);
        }

        private static void UpdateSyntaxTreesAndOrdinalMapOnly(
            ArrayBuilder<SyntaxTree> treesBuilder,
            SyntaxTree tree,
            IDictionary<SyntaxTree, int> ordinalMapBuilder,
            ImmutableDictionary<SyntaxTree, ImmutableArray<LoadDirective>> loadDirectiveMap,
            ImmutableDictionary<string, SyntaxTree> loadedSyntaxTreeMap)
        {
            var sourceCodeKind = tree.Options.Kind;
            if (sourceCodeKind == SourceCodeKind.Script)
            {
                ImmutableArray<LoadDirective> loadDirectives;
                if (loadDirectiveMap.TryGetValue(tree, out loadDirectives))
                {
                    Debug.Assert(!loadDirectives.IsEmpty);
                    foreach (var directive in loadDirectives)
                    {
                        var resolvedPath = directive.ResolvedPath;
                        Debug.Assert((resolvedPath != null) || !directive.Diagnostics.IsEmpty);
                        if (resolvedPath != null)
                        {
                            var loadedTree = loadedSyntaxTreeMap[directive.ResolvedPath];
                            UpdateSyntaxTreesAndOrdinalMapOnly(
                                treesBuilder,
                                loadedTree,
                                ordinalMapBuilder,
                                loadDirectiveMap,
                                loadedSyntaxTreeMap);
                        }

                        treesBuilder.Add(tree);

                        ordinalMapBuilder.Add(tree, ordinalMapBuilder.Count);
                    }
                }
            }

            treesBuilder.Add(tree);

            ordinalMapBuilder.Add(tree, ordinalMapBuilder.Count);
        }

        internal bool MayHaveReferenceDirectives()
        {
            var state = _lazyState;
            if (state == null)
            {
                var externalSyntaxTrees = this.ExternalSyntaxTrees;
                return externalSyntaxTrees.Any(t => t.HasReferenceOrLoadDirectives());
            }

            return state.DeclarationTable.ReferenceDirectives.Any();
        }
    }
}
