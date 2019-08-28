// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SemanticModelWorkspaceService
{
    [ExportWorkspaceServiceFactory(typeof(ISemanticModelService), ServiceLayer.Default), Shared]
    internal class SemanticModelWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public SemanticModelWorkspaceServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SemanticModelService();
        }

        private class SemanticModelService : ISemanticModelService
        {
            private static readonly ConditionalWeakTable<Workspace, ConditionalWeakTable<BranchId, Dictionary<ProjectId, CompilationSet>>> s_map =
                new ConditionalWeakTable<Workspace, ConditionalWeakTable<BranchId, Dictionary<ProjectId, CompilationSet>>>();

            private static readonly ConditionalWeakTable<Compilation, ConditionalWeakTable<SyntaxNode, WeakReference<SemanticModel>>> s_semanticModelMap =
                new ConditionalWeakTable<Compilation, ConditionalWeakTable<SyntaxNode, WeakReference<SemanticModel>>>();

            private readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

#nullable enable

            public async Task<SemanticModel> GetSemanticModelForNodeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken = default)
            {
                var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
                var semanticFactsService = document.GetLanguageService<ISemanticFactsService>();

                if (syntaxFactsService == null || semanticFactsService == null)
                {
                    // it only works if we can track member
                    return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                }

                if (IsPrimaryBranch(document) && !document.IsOpen())
                {
                    // for ones in primary branch, we only support opened documents (mostly to help typing scenario)
                    return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                }

                var versionMap = GetVersionMapFromBranchOrPrimary(document.Project.Solution.Workspace, document.Project.Solution.BranchId);

                var projectId = document.Project.Id;
                var version = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);

                CompilationSet compilationSet;
                using (_gate.DisposableRead())
                {
                    versionMap.TryGetValue(projectId, out compilationSet);
                }

                // this is first time
                if (compilationSet == null)
                {
                    // update the cache
                    await AddVersionCacheAsync(document.Project, version, cancellationToken).ConfigureAwait(false);

                    // get the base one
                    return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                }

                // we have compilation set check whether it is something we can use
                if (version.Equals(compilationSet.Version))
                {
                    if (!compilationSet.Compilation.TryGetValue(out var oldCompilation))
                    {
                        await AddVersionCacheAsync(document.Project, version, cancellationToken).ConfigureAwait(false);

                        // get the base one
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }
                    // first check whether the set has this document
                    if (!compilationSet.Trees.TryGetValue(document.Id, out var oldTree))
                    {
                        // noop.
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // Yes, we have compilation we might be able to re-use
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    if (root.SyntaxTree == oldTree)
                    {
                        // the one we have and the one in the document is same one. but tree in other file might
                        // have changed (no top level change). in that case, just use one from the document.
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // let's track member that we can re-use
                    var member = syntaxFactsService.GetContainingMemberDeclaration(root, node.SpanStart);
                    if (!syntaxFactsService.IsMethodLevelMember(member))
                    {
                        // oops, given node is not something we can support
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // check whether we already have speculative semantic model for this
                    var cachedModel = GetCachedSemanticModel(oldCompilation, member);
                    if (cachedModel != null)
                    {
                        // Yes!
                        return cachedModel;
                    }

                    // alright, we have member id. find same member from old compilation
                    var memberId = syntaxFactsService.GetMethodLevelMemberId(root, member);
                    var oldRoot = await oldTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                    var oldMember = syntaxFactsService.GetMethodLevelMember(oldRoot, memberId);
                    if (oldMember == null)
                    {
                        // oops, something went wrong. we can't find old member. 
                        //
                        // due to how we do versioning (filestamp based versioning), there is always a possibility that 
                        // sources get changed without proper version changes in some rare situations,
                        // so in those rare cases which we can't control until we move to content based versioning,
                        // just bail out and use full semantic model
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var oldModel = oldCompilation.GetSemanticModel(oldTree);
                    if (!semanticFactsService.TryGetSpeculativeSemanticModel(oldModel, oldMember, member, out var model))
                    {
                        return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // cache the new speculative semantic model for the given node
                    Contract.ThrowIfNull(model);
                    return CacheSemanticModel(oldCompilation, member, model);
                }

                // oops, it looks like we can't use cached one. 
                // update the cache
                await UpdateVersionCacheAsync(document.Project, version, compilationSet, cancellationToken).ConfigureAwait(false);

                // get the base one
                return await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            }

            private bool IsPrimaryBranch(Document document)
            {
                return document.Project.Solution.BranchId == document.Project.Solution.Workspace.PrimaryBranchId;
            }

#nullable restore

            private Task AddVersionCacheAsync(Project project, VersionStamp version, CancellationToken cancellationToken)
            {
                return UpdateVersionCacheAsync(project, version, primarySet: null, cancellationToken: cancellationToken);
            }

            private async Task UpdateVersionCacheAsync(Project project, VersionStamp version, CompilationSet primarySet, CancellationToken cancellationToken)
            {
                var versionMap = GetVersionMapFromBranch(project.Solution.Workspace, project.Solution.BranchId);
                if (!AlreadyHasLatestCompilationSet(versionMap, project.Id, version, out var compilationSet) ||
                    !compilationSet.Compilation.TryGetValue(out var compilation))
                {
                    var newSet = await CompilationSet.CreateAsync(project, compilationSet ?? primarySet, cancellationToken).ConfigureAwait(false);

                    using (_gate.DisposableWrite())
                    {
                        // we still don't have it or if someone has beaten us, check what we have is newer
                        if (!versionMap.TryGetValue(project.Id, out compilationSet) || version != compilationSet.Version)
                        {
                            versionMap[project.Id] = newSet;
                        }
                    }
                }
            }

            private bool AlreadyHasLatestCompilationSet(
                Dictionary<ProjectId, CompilationSet> versionMap, ProjectId projectId, VersionStamp version, out CompilationSet compilationSet)
            {
                using (_gate.DisposableRead())
                {
                    // we still don't have it or if someone has beaten us, check what we have is newer
                    return versionMap.TryGetValue(projectId, out compilationSet) && version == compilationSet.Version;
                }
            }

            private static readonly ConditionalWeakTable<BranchId, Dictionary<ProjectId, CompilationSet>>.CreateValueCallback s_createVersionMap =
                _ => new Dictionary<ProjectId, CompilationSet>();

            private static readonly ConditionalWeakTable<Compilation, ConditionalWeakTable<SyntaxNode, WeakReference<SemanticModel>>>.CreateValueCallback s_createNodeMap =
                _ => new ConditionalWeakTable<SyntaxNode, WeakReference<SemanticModel>>();

            private static SemanticModel GetCachedSemanticModel(
                ConditionalWeakTable<SyntaxNode, WeakReference<SemanticModel>> nodeMap, SyntaxNode newMember)
            {
                if (!nodeMap.TryGetValue(newMember, out var cached) || !cached.TryGetTarget(out var model))
                {
                    return null;
                }

                return model;
            }

            private static SemanticModel GetCachedSemanticModel(Compilation oldCompilation, SyntaxNode newMember)
            {
                var nodeMap = s_semanticModelMap.GetValue(oldCompilation, s_createNodeMap);

                // see whether we have cached one
                return GetCachedSemanticModel(nodeMap, newMember);
            }

            private static SemanticModel CacheSemanticModel(Compilation oldCompilation, SyntaxNode newMember, SemanticModel speculativeSemanticModel)
            {
                var nodeMap = s_semanticModelMap.GetValue(oldCompilation, s_createNodeMap);

                // check whether somebody already have put one for me
                var model = GetCachedSemanticModel(nodeMap, newMember);
                if (model != null)
                {
                    return model;
                }

                // noop. put one
                var weakReference = new WeakReference<SemanticModel>(speculativeSemanticModel);
                var cached = nodeMap.GetValue(newMember, _ => weakReference);
                if (cached.TryGetTarget(out var cachedModel))
                {
                    return cachedModel;
                }

                // oops. somebody has beaten me, but the model has gone.
                // set me as new target
                cached.SetTarget(speculativeSemanticModel);
                return speculativeSemanticModel;
            }

            private Dictionary<ProjectId, CompilationSet> GetVersionMapFromBranchOrPrimary(Workspace workspace, BranchId branchId)
            {
                var branchMap = GetBranchMap(workspace);
                // check whether we already have one
                if (branchMap.TryGetValue(branchId, out var versionMap))
                {
                    return versionMap;
                }

                // check primary branch
                if (branchMap.TryGetValue(workspace.PrimaryBranchId, out versionMap))
                {
                    return versionMap;
                }

                // okay, create one
                return branchMap.GetValue(branchId, s_createVersionMap);
            }

            private Dictionary<ProjectId, CompilationSet> GetVersionMapFromBranch(Workspace workspace, BranchId branchId)
            {
                var branchMap = GetBranchMap(workspace);

                return branchMap.GetValue(branchId, s_createVersionMap);
            }

            private ConditionalWeakTable<BranchId, Dictionary<ProjectId, CompilationSet>> GetBranchMap(Workspace workspace)
            {
                if (!s_map.TryGetValue(workspace, out var branchMap))
                {
                    var newBranchMap = new ConditionalWeakTable<BranchId, Dictionary<ProjectId, CompilationSet>>();

                    branchMap = s_map.GetValue(workspace, _ => newBranchMap);
                    if (branchMap == newBranchMap)
                    {
                        // it is first time we see this workspace. subscribe to it
                        workspace.DocumentClosed += OnDocumentClosed;
                        workspace.WorkspaceChanged += OnWorkspaceChanged;
                    }
                }

                return branchMap;
            }

            private void OnDocumentClosed(object sender, DocumentEventArgs e)
            {
                ClearVersionMap(e.Document.Project.Solution.Workspace, e.Document.Id);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                switch (e.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionRemoved:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                        ClearVersionMap(e.NewSolution.Workspace, e.NewSolution.ProjectIds);
                        break;
                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectRemoved:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        ClearVersionMap(e.NewSolution.Workspace, e.ProjectId);
                        break;
                    case WorkspaceChangeKind.DocumentRemoved:
                        ClearVersionMap(e.NewSolution.Workspace, e.DocumentId);
                        break;
                    case WorkspaceChangeKind.DocumentAdded:
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                    case WorkspaceChangeKind.AdditionalDocumentAdded:
                    case WorkspaceChangeKind.AdditionalDocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentChanged:
                    case WorkspaceChangeKind.AdditionalDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                        break;
                    default:
                        Contract.Fail("Unknown event");
                        break;
                }
            }

            private void ClearVersionMap(Workspace workspace, DocumentId documentId)
            {
                if (workspace.GetOpenDocumentIds(documentId.ProjectId).Any())
                {
                    return;
                }

                var versionMap = GetVersionMapFromBranch(workspace, workspace.PrimaryBranchId);

                using (_gate.DisposableWrite())
                {
                    versionMap.Remove(documentId.ProjectId);
                }
            }

            private void ClearVersionMap(Workspace workspace, ProjectId projectId)
            {
                var versionMap = GetVersionMapFromBranch(workspace, workspace.PrimaryBranchId);

                using (_gate.DisposableWrite())
                {
                    versionMap.Remove(projectId);
                }
            }

            private void ClearVersionMap(Workspace workspace, IReadOnlyList<ProjectId> projectIds)
            {
                var versionMap = GetVersionMapFromBranch(workspace, workspace.PrimaryBranchId);

                using (_gate.DisposableWrite())
                {
                    using var pooledObject = SharedPools.Default<HashSet<ProjectId>>().GetPooledObject();
                    var set = pooledObject.Object;

                    set.UnionWith(versionMap.Keys);
                    set.ExceptWith(projectIds);

                    foreach (var projectId in set)
                    {
                        versionMap.Remove(projectId);
                    }
                }
            }

            private class CompilationSet
            {
                private const int RebuildThreshold = 3;

                public readonly VersionStamp Version;
                public readonly ValueSource<Compilation> Compilation;
                public readonly ImmutableDictionary<DocumentId, SyntaxTree> Trees;

                public static async Task<CompilationSet> CreateAsync(Project project, CompilationSet oldCompilationSet, CancellationToken cancellationToken)
                {
                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var version = await project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                    var map = GetTreeMap(project, compilation, oldCompilationSet, cancellationToken);

                    ValidateTreeMap(map, project, compilation);
                    return new CompilationSet(version, GetCompilation(project, compilation), map);
                }

                private CompilationSet(VersionStamp version, ValueSource<Compilation> compilation, ImmutableDictionary<DocumentId, SyntaxTree> map)
                {
                    this.Version = version;
                    this.Compilation = compilation;
                    this.Trees = map;
                }

                private static ImmutableDictionary<DocumentId, SyntaxTree> GetTreeMap(Project project, Compilation compilation, CompilationSet oldCompilationSet, CancellationToken cancellationToken)
                {
                    // enumerable count should take a quick path since ImmutableArray implements ICollection
                    var newTreeCount = compilation.SyntaxTrees.Count();

                    // TODO: all this could go away if this is maintained by project itself and one can just get the map from it.
                    if (oldCompilationSet == null || Math.Abs(oldCompilationSet.Trees.Count - newTreeCount) > RebuildThreshold)
                    {
                        return ImmutableDictionary.CreateRange(GetNewTreeMap(project, compilation));
                    }

                    var map = AddOrUpdateNewTreeToOldMap(project, compilation, oldCompilationSet, cancellationToken);

                    // check simple case. most of typing case should hit this.
                    // number of items in the map is same as number of new trees and old compilation doesn't have
                    // more trees than current one
                    if (map.Count == newTreeCount && oldCompilationSet.Trees.Count <= newTreeCount)
                    {
                        return map;
                    }

                    // a bit more expensive case where there is a document in oldCompilationSet that doesn't exist in new compilation
                    return RemoveOldTreeFromMap(compilation, oldCompilationSet.Trees, map, cancellationToken);
                }

                private static ImmutableDictionary<DocumentId, SyntaxTree> RemoveOldTreeFromMap(
                    Compilation newCompilation,
                    ImmutableDictionary<DocumentId, SyntaxTree> oldMap, ImmutableDictionary<DocumentId, SyntaxTree> map,
                    CancellationToken cancellationToken)
                {
                    foreach (var oldIdAndTree in oldMap)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // check whether new compilation still has the tree
                        if (newCompilation.ContainsSyntaxTree(oldIdAndTree.Value))
                        {
                            continue;
                        }

                        var documentId = oldIdAndTree.Key;
                        // check whether the tree has been updated
                        if (!map.TryGetValue(documentId, out var currentTree) ||
                            currentTree != oldIdAndTree.Value)
                        {
                            continue;
                        }

                        // this has been removed
                        map = map.Remove(documentId);
                    }

                    return map;
                }

                private static ImmutableDictionary<DocumentId, SyntaxTree> AddOrUpdateNewTreeToOldMap(
                    Project newProject, Compilation newCompilation, CompilationSet oldSet, CancellationToken cancellationToken)
                {
                    if (!oldSet.Compilation.TryGetValue(out var oldCompilation))
                    {
                        return ImmutableDictionary.CreateRange(GetNewTreeMap(newProject, newCompilation));
                    }

                    var map = oldSet.Trees;
                    foreach (var newTree in newCompilation.SyntaxTrees)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (oldCompilation.ContainsSyntaxTree(newTree))
                        {
                            continue;
                        }

                        var documentId = newProject.GetDocumentId(newTree);

                        // GetDocumentId will return null for #load'ed trees.
                        // TODO:  Remove this check and add logic to fetch the #load'ed tree's
                        // Document once https://github.com/dotnet/roslyn/issues/5260 is fixed.
                        if (documentId == null)
                        {
                            Debug.Assert(newProject.Solution.Workspace.Kind == WorkspaceKind.Interactive || newProject.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles);
                            continue;
                        }

                        map = map.SetItem(documentId, newTree);
                    }

                    return map;
                }

                private static IEnumerable<KeyValuePair<DocumentId, SyntaxTree>> GetNewTreeMap(Project project, Compilation compilation)
                {
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        var documentId = project.GetDocumentId(tree);
                        if (documentId != null)
                        {
                            yield return KeyValuePairUtil.Create(documentId, tree);
                        }
                    }
                }

                private static ValueSource<Compilation> GetCompilation(Project project, Compilation compilation)
                {
                    var cache = project.Solution.Workspace.Services.GetService<IProjectCacheHostService>();
                    if (cache != null && project.Solution.BranchId == project.Solution.Workspace.PrimaryBranchId)
                    {
                        return new WeakConstantValueSource<Compilation>(cache.CacheObjectIfCachingEnabledForKey(project.Id, project, compilation));
                    }

                    return new ConstantValueSource<Compilation>(compilation);
                }

                [Conditional("DEBUG")]
                private static void ValidateTreeMap(ImmutableDictionary<DocumentId, SyntaxTree> actual, Project project, Compilation compilation)
                {
                    var expected = ImmutableDictionary.CreateRange(GetNewTreeMap(project, compilation));
                    Debug.Assert(actual.SetEquals(expected));
                }
            }
        }
    }
}
