// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private const string RazorSourceGeneratorTypeName = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";
        private static readonly ImmutableArray<string> s_razorSourceGeneratorAssemblyNames = ImmutableArray.Create(
            "Microsoft.NET.Sdk.Razor.SourceGenerators",
            "Microsoft.CodeAnalysis.Razor.Compiler.SourceGenerators",
            "Microsoft.CodeAnalysis.Razor.Compiler");
        private static readonly ImmutableArray<string> s_razorSourceGeneratorFileNamePrefixes = s_razorSourceGeneratorAssemblyNames
            .SelectAsArray(static assemblyName => Path.Combine(assemblyName, RazorSourceGeneratorTypeName));

        private readonly object _gate = new();

        /// <summary>
        /// Cached compile-time solution corresponding to an existing design-time solution.
        /// </summary>
#if NETCOREAPP
        private readonly ConditionalWeakTable<Solution, Solution> _designTimeToCompileTimeSolution = new();
#else
        private ConditionalWeakTable<Solution, Solution> _designTimeToCompileTimeSolution = new();
#endif

        private Solution? _lastCompileTimeSolution;

        public CompileTimeSolutionProvider(Workspace workspace)
        {
            workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
#if NETCOREAPP
                        _designTimeToCompileTimeSolution.Clear();
#else
                        _designTimeToCompileTimeSolution = new();
#endif
                        _lastCompileTimeSolution = null;
                    }
                }
            };
        }

        private static bool IsRazorAnalyzerConfig(TextDocumentState documentState)
            => documentState.FilePath != null && documentState.FilePath.EndsWith(RazorEncConfigFileName, StringComparison.OrdinalIgnoreCase);

        public Solution GetCompileTimeSolution(Solution designTimeSolution)
        {
            lock (_gate)
            {
                _designTimeToCompileTimeSolution.TryGetValue(designTimeSolution, out var cachedCompileTimeSolution);

                // Design time solution hasn't changed since we calculated the last compile-time solution:
                if (cachedCompileTimeSolution != null)
                    return cachedCompileTimeSolution;

                var staleSolution = _lastCompileTimeSolution;
                var compileTimeSolution = designTimeSolution;

                foreach (var (_, projectState) in compileTimeSolution.SolutionState.ProjectStates)
                {
                    using var _1 = ArrayBuilder<DocumentId>.GetInstance(out var configIdsToRemove);
                    using var _2 = ArrayBuilder<DocumentId>.GetInstance(out var documentIdsToRemove);

                    foreach (var (_, configState) in projectState.AnalyzerConfigDocumentStates.States)
                    {
                        if (IsRazorAnalyzerConfig(configState))
                        {
                            configIdsToRemove.Add(configState.Id);
                        }
                    }

                    // only remove design-time only documents when source-generated ones replace them
                    if (configIdsToRemove.Count > 0)
                    {
                        foreach (var (_, documentState) in projectState.DocumentStates.States)
                        {
                            if (documentState.Attributes.DesignTimeOnly || IsRazorDesignTimeDocument(documentState))
                            {
                                documentIdsToRemove.Add(documentState.Id);
                            }
                        }

                        compileTimeSolution = compileTimeSolution
                            .RemoveAnalyzerConfigDocuments(configIdsToRemove.ToImmutable())
                            .RemoveDocuments(documentIdsToRemove.ToImmutable());

                        if (staleSolution is not null)
                        {
                            var existingStaleProject = staleSolution.GetProject(projectState.Id);
                            if (existingStaleProject is not null)
                                compileTimeSolution = compileTimeSolution.WithCachedSourceGeneratorState(projectState.Id, existingStaleProject);
                        }
                    }
                }

                compileTimeSolution = _designTimeToCompileTimeSolution.GetValue(designTimeSolution, _ => compileTimeSolution);
                _lastCompileTimeSolution = compileTimeSolution;

                return compileTimeSolution;
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
            => documentState.FilePath?.EndsWith(".razor.g.cs") == true || documentState.FilePath?.EndsWith(".cshtml.g.cs") == true;

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

            var generatedDocumentPaths = BuildGeneratedDocumentPaths(designTimeProjectDirectoryName, designTimeDocument.FilePath!, generatedDocumentPathPrefix);

            var sourceGeneratedDocuments = await compileTimeSolution.GetRequiredProject(designTimeDocument.Project.Id).GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            return sourceGeneratedDocuments.SingleOrDefault(d => d.FilePath != null && generatedDocumentPaths.Contains(d.FilePath));
        }

        /// <summary>
        /// Note that in .NET 6 Preview 7 the source generator changed to passing in the relative doc path without a leading \ to GetIdentifierFromPath
        /// which caused the source generated file name to no longer be prefixed by an _.  Additionally, the file extension was changed to .g.cs
        /// </summary>
        private static OneOrMany<string> BuildGeneratedDocumentPaths(string designTimeProjectDirectoryName, string designTimeDocumentFilePath, string? generatedDocumentPathPrefix)
        {
            var relativeDocumentPath = GetRelativeDocumentPath(designTimeProjectDirectoryName, designTimeDocumentFilePath);

            if (generatedDocumentPathPrefix is not null)
            {
                return OneOrMany.Create(GetGeneratedDocumentPath(generatedDocumentPathPrefix, relativeDocumentPath));
            }

            return OneOrMany.Create(s_razorSourceGeneratorFileNamePrefixes.SelectAsArray(
                static (prefix, relativeDocumentPath) => GetGeneratedDocumentPath(prefix, relativeDocumentPath), relativeDocumentPath));

            static string GetGeneratedDocumentPath(string prefix, string relativeDocumentPath)
            {
                return Path.Combine(prefix, GetIdentifierFromPath(relativeDocumentPath)) + ".g.cs";
            }
        }

        private static string GetRelativeDocumentPath(string projectDirectory, string designTimeDocumentFilePath)
            => PathUtilities.GetRelativePath(projectDirectory, designTimeDocumentFilePath)[..^".g.cs".Length];

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
                    if (filePath != null && (generatedDocumentPathPrefix != null
                        ? filePath.StartsWith(generatedDocumentPathPrefix)
                        : s_razorSourceGeneratorFileNamePrefixes.Any(static (prefix, filePath) => filePath.StartsWith(prefix), filePath)))
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
