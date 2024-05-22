// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    internal DocumentState? GetDocumentState(SyntaxTree? syntaxTree, ProjectId? projectId)
    {
        if (syntaxTree != null)
        {
            // is this tree known to be associated with a document?
            var documentId = DocumentState.GetDocumentIdForTree(syntaxTree);
            if (documentId != null && (projectId == null || documentId.ProjectId == projectId))
            {
                // does this solution even have the document?
                var projectState = this.SolutionState.GetProjectState(documentId.ProjectId);
                if (projectState != null)
                {
                    var document = projectState.DocumentStates.GetState(documentId);
                    if (document != null)
                    {
                        // does this document really have the syntax tree?
                        if (document.TryGetSyntaxTree(out var documentTree) && documentTree == syntaxTree)
                        {
                            return document;
                        }
                    }
                    else
                    {
                        var generatedDocument = this.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(documentId);

                        if (generatedDocument != null)
                        {
                            // does this document really have the syntax tree?
                            if (generatedDocument.TryGetSyntaxTree(out var documentTree) && documentTree == syntaxTree)
                            {
                                return generatedDocument;
                            }
                        }
                    }
                }
            }
        }

        return null;
    }

    public OriginatingProjectInfo? GetOriginatingProjectInfo(ISymbol? symbol)
    {
        if (symbol == null)
        {
            return null;
        }

        var unrootedSymbolInfo = GetOriginatingProjectInfoWorker(symbol);

        // Validate some invariants we think should hold.  We want to know if this breaks, which indicates some part
        // of our system not working as we might expect.  If they break, create NFWs so we can find out and
        // investigate.

        if (SymbolKey.IsBodyLevelSymbol(symbol))
        {
            var projectId = unrootedSymbolInfo?.ProjectId;

            // If this is a method-body-level symbol, then we will have it's syntax tree.  Since  we already have a
            // mapping from syntax-trees to docs, so we can immediately map this back to it's originating project.
            //
            // Note: we don't do this for all source symbols, only method-body-level ones.  That's because other
            // source symbols may be *retargetted*.  So you can have the same symbol retargetted into multiple
            // projects, but which have the same syntax-tree (which is only in one project).  We need to actually
            // check it's assembly symbol so that we get the actual project it is from (the original project, or the
            // retargetted project).
            var syntaxTree = symbol.Locations[0].SourceTree;
            Contract.ThrowIfNull(syntaxTree);

            var documentId = this.GetDocumentState(syntaxTree, projectId: null)?.Id;
            if (documentId == null)
            {
                try
                {
                    throw new InvalidOperationException(
                        $"We should always be able to map a body symbol back to a document:\r\n{symbol.Kind}\r\n{symbol.Name}\r\n{syntaxTree.FilePath}\r\n{projectId}");
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                }
            }
            else if (documentId.ProjectId != projectId)
            {
                try
                {
                    throw new InvalidOperationException(
                        $"Syntax tree for a body symbol should map to the same project as the body symbol's assembly:\r\n{symbol.Kind}\r\n{symbol.Name}\r\n{syntaxTree.FilePath}\r\n{projectId}\r\n{documentId.ProjectId}");
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                {
                }
            }
        }

        return unrootedSymbolInfo;
    }

    private OriginatingProjectInfo? GetOriginatingProjectInfoWorker(ISymbol symbol)
    {
        InterlockedOperations.Initialize(ref _unrootedSymbolToProjectId, s_createTable);

        // Walk up the symbol so we can get to the containing namespace/assembly that will be used to map
        // back to a project.

        while (symbol != null)
        {
            var result = GetOriginatingProjectInfoDirectly(symbol, _unrootedSymbolToProjectId);
            if (result != null)
                return result;

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    private OriginatingProjectInfo? GetOriginatingProjectInfoDirectly(ISymbol symbol, ConditionalWeakTable<ISymbol, OriginatingProjectInfo?> unrootedSymbolToProjectId)
    {
        if (symbol.IsKind(SymbolKind.Namespace, out INamespaceSymbol? ns))
        {
            if (ns.ContainingCompilation != null)
            {
                // A namespace that spans a compilation.  These don't belong to an assembly/module directly.
                // However, as we're looking for the project this corresponds to, we can look for the
                // source-module component (the first in the constituent namespaces) and then search using that.
                return GetOriginatingProjectInfo(ns.ConstituentNamespaces[0]);
            }
        }
        else if (symbol.IsKind(SymbolKind.Assembly) ||
                 symbol.IsKind(SymbolKind.NetModule) ||
                 symbol.IsKind(SymbolKind.DynamicType))
        {
            if (!unrootedSymbolToProjectId.TryGetValue(symbol, out var projectId))
            {
                // First, look through all the projects, and see if this symbol came from the primary assembly for
                // that project.  (i.e. was a source symbol from that project, or was retargetted into that
                // project).  If so, that's the originating project.
                //
                // If we don't find any primary results, then look through all the secondary assemblies (i.e.
                // references) for that project.  This is the case for metadata symbols.  A metadata symbol might be
                // found in many projects, so we just return the first result as that's just as good for finding the
                // metadata symbol as any other project.
                projectId = FindProject(symbol, primary: true) ??
                            FindProject(symbol, primary: false);

                // Have to lock as there's no atomic AddOrUpdate in netstandard2.0 and we could throw if two
                // threads tried to add the same item.
#if !NETCOREAPP
                lock (unrootedSymbolToProjectId)
                {
                    unrootedSymbolToProjectId.Remove(symbol);
                    unrootedSymbolToProjectId.Add(symbol, projectId);
                }
#else
                unrootedSymbolToProjectId.AddOrUpdate(symbol, projectId);
#endif
            }

            return projectId;
        }
        else if (symbol is ITypeParameterSymbol
        {
            TypeParameterKind: TypeParameterKind.Cref,
            Locations: [{ SourceTree: var typeParameterSourceTree }, ..],
        })
        {
            // Cref type parameters don't belong to any containing symbol.  But we can map them to a doc/project
            // using the declaring syntax of the type parameter itself.
            if (GetDocumentState(typeParameterSourceTree, projectId: null) is { } document)
                return new OriginatingProjectInfo(document.Id.ProjectId, Compilation: null, ReferencedThrough: null);
        }

        return null;

        OriginatingProjectInfo? FindProject(ISymbol symbol, bool primary)
        {
            foreach (var (id, tracker) in _projectIdToTrackerMap)
            {
                if (tracker.ContainsAssemblyOrModuleOrDynamic(symbol, primary, out var compilation, out var referencedThrough))
                    return new OriginatingProjectInfo(id, compilation, referencedThrough);
            }

            return null;
        }
    }
}
