// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    /// <summary>
    /// Features like add-using want to be able to quickly search symbol indices for projects and
    /// metadata.  However, creating those indices can be expensive.  As such, we don't want to
    /// construct them during the add-using process itself.  Instead, we expose this type as an 
    /// Incremental-Analyzer to walk our projects/metadata in the background to keep the indices
    /// up to date.
    /// 
    /// We also then export this type as a service that can give back the index for a project or
    /// metadata dll on request.  If the index has been produced then it will be returned and 
    /// can be used by add-using.  Otherwise, nothing is returned and no results will be found.
    /// 
    /// This means that as the project is being indexed, partial results may be returned.  However
    /// once it is fully indexed, then total results will be returned.
    /// </summary>
    [Shared]
    [ExportIncrementalAnalyzerProvider(nameof(SymbolTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.RemoteWorkspace })]
    [ExportWorkspaceServiceFactory(typeof(ISymbolTreeInfoCacheService))]
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider, IWorkspaceServiceFactory
    {
        // Concurrent dictionaries so they can be read from the SymbolTreeInfoCacheService while they are being
        // populated/updated by the IncrementalAnalyzer.
        //
        // We always  keep around the latest computed information produced by the incremental analyzer.  That way the
        // values are hopefully ready when someone calls on them for the current solution.

        private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectIdToInfo = new ConcurrentDictionary<ProjectId, SymbolTreeInfo>();
        private readonly ConcurrentDictionary<MetadataId, MetadataInfo> _metadataIdToInfo = new ConcurrentDictionary<MetadataId, MetadataInfo>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolTreeInfoIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new SymbolTreeInfoIncrementalAnalyzer(_projectIdToInfo, _metadataIdToInfo);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SymbolTreeInfoCacheService(_projectIdToInfo, _metadataIdToInfo);
    }
}
