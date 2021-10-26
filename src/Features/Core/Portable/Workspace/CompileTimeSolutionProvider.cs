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
        private const string RazorSourceGeneratorAssemblyName = "Microsoft.NET.Sdk.Razor.SourceGenerators";
        private const string RazorSourceGeneratorTypeName = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";
        private static readonly string s_razorSourceGeneratorFileNamePrefix = Path.Combine(RazorSourceGeneratorAssemblyName, RazorSourceGeneratorTypeName);

        private readonly Workspace _workspace;

        private readonly object _gate = new();

#if NETCOREAPP
        private readonly ConditionalWeakTable<Solution, Solution> _designTimeToCompileTimeSoution = new();
#else
        // Framework lacks both a .Clear() method.  So for Framework we simulate that by just overwriting this with a
        // new instance.  This happens under a lock, so everyone sees a consistent dictionary.
        private ConditionalWeakTable<Solution, Solution> _designTimeToCompileTimeSoution = new();
#endif

        public CompileTimeSolutionProvider(Workspace workspace)
        {
            workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind is WorkspaceChangeKind.SolutionCleared or WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
#if NETCOREAPP
                        _designTimeToCompileTimeSoution.Clear();
#else
                        _designTimeToCompileTimeSoution = new();
#endif
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
                if (!_designTimeToCompileTimeSoution.TryGetValue(designTimeSolution, out var compileTimeSolution))
                {
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

                    compileTimeSolution = designTimeSolution
                        .RemoveAnalyzerConfigDocuments(configIdsToRemove.ToImmutable())
                        .RemoveDocuments(documentIdsToRemove.ToImmutable());

                    _designTimeToCompileTimeSoution.Add(designTimeSolution, compileTimeSolution);
                }

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
            => documentState.Attributes.DesignTimeOnly && documentState.FilePath?.EndsWith(".razor.g.cs") == true;

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
            var generatedDocumentPathNet6Preview7 = BuildGeneratedDocumentPathNet6Preview7(designTimeProjectDirectoryName, designTimeDocument.FilePath!, generatedDocumentPathPrefix);

            var sourceGeneratedDocuments = await compileTimeSolution.GetRequiredProject(designTimeDocument.Project.Id).GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            return sourceGeneratedDocuments.SingleOrDefault(d => d.FilePath == generatedDocumentPath || d.FilePath == generatedDocumentPathNet6Preview7);
        }

        /// <summary>
        /// Prior to .net6 preview 7, the source generated path was built using a relative path with a preceding \
        /// and without only .cs as part of the extension.
        /// </summary>
        private static string BuildGeneratedDocumentPath(string designTimeProjectDirectoryName, string designTimeDocumentFilePath, string? generatedDocumentPathPrefix)
        {
            var relativeDocumentPath = GetRelativeDocumentPathWithPrecedingSlash(designTimeProjectDirectoryName, designTimeDocumentFilePath);
            var generatedDocumentPath = GetGeneratedDocumentPathWithoutExtension(relativeDocumentPath, generatedDocumentPathPrefix) + ".cs";
            return generatedDocumentPath;
        }

        /// <summary>
        /// In .net6 p7 the source generator changed to passing in the relative doc path without a leading \ to GetIdentifierFromPath
        /// which caused the source generated file name to no longer be prefixed by an _.  Additionally, the file extension was changed to .g.cs
        /// </summary>
        private static string BuildGeneratedDocumentPathNet6Preview7(string designTimeProjectDirectoryName, string designTimeDocumentFilePath, string? generatedDocumentPathPrefix)
        {
            var relativeDocumentPath = GetRelativeDocumentPath(designTimeProjectDirectoryName, designTimeDocumentFilePath);
            var generatedDocumentPath = GetGeneratedDocumentPathWithoutExtension(relativeDocumentPath, generatedDocumentPathPrefix) + ".g.cs";
            return generatedDocumentPath;
        }

        private static string GetRelativeDocumentPath(string projectDirectory, string designTimeDocumentFilePath)
            => PathUtilities.GetRelativePath(projectDirectory, designTimeDocumentFilePath)[..^".g.cs".Length];

        private static string GetRelativeDocumentPathWithPrecedingSlash(string projectDirectory, string designTimeDocumentFilePath)
            => Path.Combine("\\", GetRelativeDocumentPath(projectDirectory, designTimeDocumentFilePath));

        private static string GetGeneratedDocumentPathWithoutExtension(string relativeDocumentPath, string? generatedDocumentPathPrefix)
            => Path.Combine(generatedDocumentPathPrefix ?? s_razorSourceGeneratorFileNamePrefix, GetIdentifierFromPath(relativeDocumentPath));

        private static bool HasMatchingFilePath(string designTimeDocumentFilePath, string designTimeProjectDirectory, string compileTimeFilePath)
        {
            // Check for matching file names created from a relative path with and without a preceding slash as both
            // are valid depening on the which sdk.  See BuildGeneratedDocumentPathNet6Preview7.
            var relativeDocumentPath = GetRelativeDocumentPath(designTimeProjectDirectory, designTimeDocumentFilePath);
            var relativeDocumentPathWithSlash = GetRelativeDocumentPathWithPrecedingSlash(designTimeProjectDirectory, designTimeDocumentFilePath);

            var compileTimeFileName = PathUtilities.GetFileName(compileTimeFilePath, includeExtension: false);

            // Sdks including and after .net6 preview7 have compile time file names ending with ".g.cs".
            if (compileTimeFileName.EndsWith(".g"))
                compileTimeFileName = compileTimeFileName[..^".g".Length];

            return compileTimeFileName == GetIdentifierFromPath(relativeDocumentPath) || compileTimeFileName == GetIdentifierFromPath(relativeDocumentPathWithSlash);
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
