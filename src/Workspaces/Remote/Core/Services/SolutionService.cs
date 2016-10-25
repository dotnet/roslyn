// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Provide solution from given checksum
    /// 
    /// TODO: change this to workspace service
    /// </summary>
    internal class SolutionService
    {
        public const string WorkspaceKind_RemoteWorkspace = "RemoteWorkspace";

        // TODO: make this simple cache better
        // this simple cache hold onto the last solution created
        private static ValueTuple<Checksum, Solution> s_lastSolution;

        private readonly AssetService _assetService;

        public SolutionService(AssetService assetService)
        {
            _assetService = assetService;
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            if (s_lastSolution.Item1 == solutionChecksum)
            {
                return s_lastSolution.Item2;
            }

            // create new solution
            var solution = await CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // save it
            s_lastSolution = ValueTuple.Create(solutionChecksum, solution);

            return solution;
        }

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // since option belong to workspace, we can't share solution

            // create new solution
            var solution = await CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // set merged options
            solution.Workspace.Options = MergeOptions(solution.Workspace.Options, optionSet);

            // return new solution
            return solution;
        }

        private OptionSet MergeOptions(OptionSet workspaceOptions, OptionSet userOptions)
        {
            var newOptions = workspaceOptions;
            foreach (var key in userOptions.GetChangedOptions(workspaceOptions))
            {
                newOptions = newOptions.WithChangedOption(key, userOptions.GetOption(key));
            }

            return newOptions;
        }

        private async Task<Solution> CreateSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            // synchronize whole solution first
            await _assetService.SynchronizeSolutionAssetsAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionStateChecksums>(solutionChecksum, cancellationToken).ConfigureAwait(false);

            var workspace = new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: WorkspaceKind_RemoteWorkspace);
            var solutionInfo = await _assetService.GetAssetAsync<SolutionInfo.SolutionAttributes>(solutionChecksumObject.Info, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects)
            {
                var projectSnapshot = await _assetService.GetAssetAsync<ProjectStateChecksums>(projectChecksum, cancellationToken).ConfigureAwait(false);
                var projectInfo = await _assetService.GetAssetAsync<ProjectInfo.ProjectAttributes>(projectSnapshot.Info, cancellationToken).ConfigureAwait(false);
                if (!workspace.Services.IsSupported(projectInfo.Language))
                {
                    // only add project our workspace supports. 
                    // workspace doesn't allow creating project with unknown languages
                    continue;
                }

                var documents = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.Documents)
                {
                    var documentSnapshot = await _assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, cancellationToken).ConfigureAwait(false);
                    var documentInfo = await _assetService.GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

                    var textLoader = TextLoader.From(
                        TextAndVersion.Create(
                            new ChecksumSourceText(
                                documentSnapshot.Text,
                                await _assetService.GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false)),
                            VersionStamp.Create(),
                            documentInfo.FilePath));

                    // TODO: do we need version?
                    documents.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            textLoader,
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var p2p = new List<ProjectReference>();
                foreach (var checksum in projectSnapshot.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await _assetService.GetAssetAsync<ProjectReference>(checksum, cancellationToken).ConfigureAwait(false);
                    p2p.Add(reference);
                }

                var metadata = new List<MetadataReference>();
                foreach (var checksum in projectSnapshot.MetadataReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await _assetService.GetAssetAsync<MetadataReference>(checksum, cancellationToken).ConfigureAwait(false);
                    metadata.Add(reference);
                }

                var analyzers = new List<AnalyzerReference>();
                foreach (var checksum in projectSnapshot.AnalyzerReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await _assetService.GetAssetAsync<AnalyzerReference>(checksum, cancellationToken).ConfigureAwait(false);
                    analyzers.Add(reference);
                }

                var additionals = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.AdditionalDocuments)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentSnapshot = await _assetService.GetAssetAsync<DocumentStateChecksums>(documentChecksum, cancellationToken).ConfigureAwait(false);
                    var documentInfo = await _assetService.GetAssetAsync<DocumentInfo.DocumentAttributes>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

                    var textLoader = TextLoader.From(
                        TextAndVersion.Create(
                            new ChecksumSourceText(
                                documentSnapshot.Text,
                            await _assetService.GetAssetAsync<SourceText>(documentSnapshot.Text, cancellationToken).ConfigureAwait(false)),
                            VersionStamp.Create(),
                            documentInfo.FilePath));

                    // TODO: do we need version?
                    additionals.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            textLoader,
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var compilationOptions = await _assetService.GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions, cancellationToken).ConfigureAwait(false);
                var parseOptions = await _assetService.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, cancellationToken).ConfigureAwait(false);

                projects.Add(
                    ProjectInfo.Create(
                        projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                        projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                        compilationOptions, parseOptions,
                        documents, p2p, metadata, analyzers, additionals, projectInfo.IsSubmission));
            }

            return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
        }
    }
}
