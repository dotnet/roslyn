// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides a compile-time view of the current workspace solution.
    /// Workaround for Razor projects which generate both design-time and compile-time source files.
    /// TODO: remove https://github.com/dotnet/roslyn/issues/51678
    /// </summary>
    internal sealed class CompileTimeSolutionProvider : ICompileTimeSolutionProvider
    {
        [ExportWorkspaceServiceFactory(typeof(ICompileTimeSolutionProvider), WorkspaceKind.Host), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
                => new CompileTimeSolutionProvider(workspaceServices.Workspace);
        }

        private const string RazorEncConfigFileName = "RazorSourceGenerator.razorencconfig";
        private const string RazorSourceGeneratorAssemblyName = "Microsoft.NET.Sdk.Razor.SourceGenerators";
        private const string RazorSourceGeneratorTypeName = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";
        private static readonly string s_razorSourceGeneratorFileNamePrefix = Path.Combine(RazorSourceGeneratorAssemblyName, RazorSourceGeneratorTypeName);

        private readonly Workspace _workspace;

        private readonly object _gate = new();

        /// <summary>
        /// Cached compile time solution corresponding to the <see cref="Workspace.PrimaryBranchId"/>
        /// </summary>
        private (int DesignTimeSolutionVersion, BranchId DesignTimeSolutionBranch, Solution CompileTimeSolution)? _primaryBranchCompileTimeCache;

        /// <summary>
        /// Cached compile time solution for a forked branch.  This is used primarily by LSP cases where
        /// we fork the workspace solution and request diagnostics for the forked solution.
        /// </summary>
        private (int DesignTimeSolutionVersion, BranchId DesignTimeSolutionBranch, Solution CompileTimeSolution)? _forkedBranchCompileTimeCache;

        public CompileTimeSolutionProvider(Workspace workspace)
        {
            workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
                        _primaryBranchCompileTimeCache = null;
                        _forkedBranchCompileTimeCache = null;
                    }
                }
            };
            _workspace = workspace;
        }

        private static bool IsRazorAnalyzerConfig(TextDocumentState documentState)
            => documentState.FilePath != null && documentState.FilePath.EndsWith(RazorEncConfigFileName, StringComparison.OrdinalIgnoreCase);

        public Solution GetCompileTimeSolution(Solution designTimeSolution)
        {
            lock (_gate)
            {
                var cachedCompileTimeSolution = GetCachedCompileTimeSolution(designTimeSolution);

                // Design time solution hasn't changed since we calculated the last compile-time solution:
                if (cachedCompileTimeSolution != null)
                {
                    return cachedCompileTimeSolution;
                }

                using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var configIdsToRemove);
                using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var documentIdsToRemove);

                foreach (var (_, projectState) in designTimeSolution.State.ProjectStates)
                {
                    var anyConfigs = false;

                    foreach (var (_, configState) in projectState.AnalyzerConfigDocumentStates.States)
                    {
                        if (IsRazorAnalyzerConfig(configState))
                        {
                            configIdsToRemove.Add(configState.Id);
                            anyConfigs = true;
                        }
                    }

                    // only remove design-time only documents when source-generated ones replace them
                    if (anyConfigs)
                    {
                        foreach (var (_, documentState) in projectState.DocumentStates.States)
                        {
                            if (documentState.Attributes.DesignTimeOnly)
                            {
                                documentIdsToRemove.Add(documentState.Id);
                            }
                        }
                    }
                }

                var compileTimeSolution = designTimeSolution
                    .RemoveAnalyzerConfigDocuments(configIdsToRemove.ToImmutable())
                    .RemoveDocuments(documentIdsToRemove.ToImmutable());

                UpdateCachedCompileTimeSolution(designTimeSolution, compileTimeSolution);

                return compileTimeSolution;
            }
        }

        private Solution? GetCachedCompileTimeSolution(Solution designTimeSolution)
        {
            // If the design time solution is for the primary branch, retrieve the last cached solution for it.
            // Otherwise this is a forked solution, so retrieve the last forked compile time solution we calculated.
            var cachedCompileTimeSolution = designTimeSolution.BranchId == _workspace.PrimaryBranchId ? _primaryBranchCompileTimeCache : _forkedBranchCompileTimeCache;

            // Verify that the design time solution has not changed since the last calculated compile time solution and that
            // the design time solution branch matches the branch of the design time solution we calculated the compile time solution for.
            if (cachedCompileTimeSolution != null
                    && designTimeSolution.WorkspaceVersion == cachedCompileTimeSolution.Value.DesignTimeSolutionVersion
                    && designTimeSolution.BranchId == cachedCompileTimeSolution.Value.DesignTimeSolutionBranch)
            {
                return cachedCompileTimeSolution.Value.CompileTimeSolution;
            }

            return null;
        }

        private void UpdateCachedCompileTimeSolution(Solution designTimeSolution, Solution compileTimeSolution)
        {
            if (designTimeSolution.BranchId == _workspace.PrimaryBranchId)
            {
                _primaryBranchCompileTimeCache = (designTimeSolution.WorkspaceVersion, designTimeSolution.BranchId, compileTimeSolution);
            }
            else
            {
                _forkedBranchCompileTimeCache = (designTimeSolution.WorkspaceVersion, designTimeSolution.BranchId, compileTimeSolution);
            }
        }

        // Copied from
        // https://github.com/dotnet/sdk/blob/main/src/RazorSdk/SourceGenerators/RazorSourceGenerator.Helpers.cs#L32
        private static string GetIdentifierFromPath(string filePath)
        {
            var builder = new StringBuilder(filePath.Length);

            for (var i = 0; i < filePath.Length; i++)
            {
                switch (filePath[i])
                {
                    case ':' or '\\' or '/':
                    case char ch when !char.IsLetterOrDigit(ch):
                        builder.Append('_');
                        break;
                    default:
                        builder.Append(filePath[i]);
                        break;
                }
            }

            return builder.ToString();
        }

        private static bool IsRazorDesignTimeDocument(DocumentState documentState)
            => documentState.Attributes.DesignTimeOnly && (documentState.FilePath?.EndsWith(".razor.g.cs") == true || documentState.FilePath?.EndsWith(".cshtml.g.cs") == true);

        internal static async Task<Document?> TryGetCompileTimeDocumentAsync(
            Document designTimeDocument,
            Solution compileTimeSolution,
            CancellationToken cancellationToken,
            string? generatedDocumentPathPrefix = null)
        {
            var compileTimeDocument = await compileTimeSolution.GetDocumentAsync(designTimeDocument.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (compileTimeDocument != null)
            {
                return compileTimeDocument;
            }

            if (!IsRazorDesignTimeDocument(designTimeDocument.DocumentState))
            {
                return null;
            }

            var designTimeProjectDirectoryName = PathUtilities.GetDirectoryName(designTimeDocument.Project.FilePath)!;

            var generatedDocumentPath = BuildGeneratedDocumentPath(designTimeProjectDirectoryName, designTimeDocument.FilePath!, generatedDocumentPathPrefix);

            var sourceGeneratedDocuments = await compileTimeSolution.GetRequiredProject(designTimeDocument.Project.Id).GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            return sourceGeneratedDocuments.SingleOrDefault(d => d.FilePath == generatedDocumentPath);
        }

        /// <summary>
        /// Note that in .NET 6 Preview 7 the source generator changed to passing in the relative doc path without a leading \ to GetIdentifierFromPath
        /// which caused the source generated file name to no longer be prefixed by an _.  Additionally, the file extension was changed to .g.cs
        /// </summary>
        private static string BuildGeneratedDocumentPath(string designTimeProjectDirectoryName, string designTimeDocumentFilePath, string? generatedDocumentPathPrefix)
        {
            var relativeDocumentPath = GetRelativeDocumentPath(designTimeProjectDirectoryName, designTimeDocumentFilePath);
            return GetGeneratedDocumentPathWithoutExtension(relativeDocumentPath, generatedDocumentPathPrefix) + ".g.cs";
        }

        private static string GetRelativeDocumentPath(string projectDirectory, string designTimeDocumentFilePath)
            => PathUtilities.GetRelativePath(projectDirectory, designTimeDocumentFilePath)[..^".g.cs".Length];

        private static string GetGeneratedDocumentPathWithoutExtension(string relativeDocumentPath, string? generatedDocumentPathPrefix)
            => Path.Combine(generatedDocumentPathPrefix ?? s_razorSourceGeneratorFileNamePrefix, GetIdentifierFromPath(relativeDocumentPath));

        private static bool HasMatchingFilePath(string designTimeDocumentFilePath, string designTimeProjectDirectory, string compileTimeFilePath)
        {
            var relativeDocumentPath = GetRelativeDocumentPath(designTimeProjectDirectory, designTimeDocumentFilePath);

            var compileTimeFileName = PathUtilities.GetFileName(compileTimeFilePath, includeExtension: false);

            if (compileTimeFileName.EndsWith(".g", StringComparison.Ordinal))
                compileTimeFileName = compileTimeFileName[..^".g".Length];

            return compileTimeFileName == GetIdentifierFromPath(relativeDocumentPath);
        }

        internal static async Task<ImmutableArray<DocumentId>> GetDesignTimeDocumentsAsync(
            Solution compileTimeSolution,
            ImmutableArray<DocumentId> compileTimeDocumentIds,
            Solution designTimeSolution,
            CancellationToken cancellationToken,
            string? generatedDocumentPathPrefix = null)
        {
            using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var result);
            using var _2 = PooledDictionary<ProjectId, ArrayBuilder<string>>.GetInstance(out var compileTimeFilePathsByProject);

            generatedDocumentPathPrefix ??= s_razorSourceGeneratorFileNamePrefix;

            foreach (var compileTimeDocumentId in compileTimeDocumentIds)
            {
                if (designTimeSolution.ContainsDocument(compileTimeDocumentId))
                {
                    result.Add(compileTimeDocumentId);
                }
                else
                {
                    var compileTimeDocument = await compileTimeSolution.GetTextDocumentAsync(compileTimeDocumentId, cancellationToken).ConfigureAwait(false);
                    var filePath = compileTimeDocument?.State.FilePath;
                    if (filePath?.StartsWith(generatedDocumentPathPrefix) == true)
                    {
                        compileTimeFilePathsByProject.MultiAdd(compileTimeDocumentId.ProjectId, filePath);
                    }
                }
            }

            if (result.Count == compileTimeDocumentIds.Length)
            {
                Debug.Assert(compileTimeFilePathsByProject.Count == 0);
                return compileTimeDocumentIds;
            }

            foreach (var (projectId, compileTimeFilePaths) in compileTimeFilePathsByProject)
            {
                var designTimeProjectState = designTimeSolution.GetProjectState(projectId);
                if (designTimeProjectState == null)
                {
                    continue;
                }

                var designTimeProjectDirectory = PathUtilities.GetDirectoryName(designTimeProjectState.FilePath)!;

                foreach (var (_, designTimeDocumentState) in designTimeProjectState.DocumentStates.States)
                {
                    if (IsRazorDesignTimeDocument(designTimeDocumentState) &&
                        compileTimeFilePaths.Any(compileTimeFilePath => HasMatchingFilePath(designTimeDocumentState.FilePath!, designTimeProjectDirectory, compileTimeFilePath)))
                    {
                        result.Add(designTimeDocumentState.Id);
                    }
                }
            }

            compileTimeFilePathsByProject.FreeValues();
            return result.ToImmutable();
        }
    }
}
