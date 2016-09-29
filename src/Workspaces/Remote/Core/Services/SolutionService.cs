// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Options;
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
        private ValueTuple<Checksum, Solution> _lastSolution;

        public async Task<Solution> GetSolutionAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            if (_lastSolution.Item1 == solutionChecksum)
            {
                return _lastSolution.Item2;
            }

            // create new solution
            var solution = await CreateSolutionAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // save it
            _lastSolution = ValueTuple.Create(solutionChecksum, solution);

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
            var solutionChecksumObject = await RoslynServices.AssetService.GetAssetAsync<SolutionChecksumObject>(solutionChecksum, cancellationToken).ConfigureAwait(false);

            // TODO: Make these to do work concurrently
            var workspace = new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: WorkspaceKind_RemoteWorkspace);
            var solutionInfo = await RoslynServices.AssetService.GetAssetAsync<SolutionChecksumObjectInfo>(solutionChecksumObject.Info, cancellationToken).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectChecksum in solutionChecksumObject.Projects)
            {
                var projectSnapshot = await RoslynServices.AssetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);

                var projectInfo = await RoslynServices.AssetService.GetAssetAsync<ProjectChecksumObjectInfo>(projectSnapshot.Info, cancellationToken).ConfigureAwait(false);
                if (!workspace.Services.IsSupported(projectInfo.Language))
                {
                    // only add project our workspace supports. 
                    // workspace doesn't allow creating project with unknown languages
                    continue;
                }

                var documents = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.Documents)
                {
                    var documentSnapshot = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObject>(documentChecksum, cancellationToken).ConfigureAwait(false);

                    var documentInfo = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObjectInfo>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

                    // TODO: do we need version?
                    documents.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            new RemoteTextLoader(documentSnapshot.Text),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var p2p = new List<ProjectReference>();
                foreach (var checksum in projectSnapshot.ProjectReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<ProjectReference>(checksum, cancellationToken).ConfigureAwait(false);
                    p2p.Add(reference);
                }

                var metadata = new List<MetadataReference>();
                foreach (var checksum in projectSnapshot.MetadataReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<MetadataReference>(checksum, cancellationToken).ConfigureAwait(false);
                    metadata.Add(reference);
                }

                var analyzers = new List<AnalyzerReference>();
                foreach (var checksum in projectSnapshot.AnalyzerReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var reference = await RoslynServices.AssetService.GetAssetAsync<AnalyzerReference>(checksum, cancellationToken).ConfigureAwait(false);
                    analyzers.Add(reference);
                }

                var additionals = new List<DocumentInfo>();
                foreach (var documentChecksum in projectSnapshot.AdditionalDocuments)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentSnapshot = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObject>(documentChecksum, cancellationToken).ConfigureAwait(false);

                    var documentInfo = await RoslynServices.AssetService.GetAssetAsync<DocumentChecksumObjectInfo>(documentSnapshot.Info, cancellationToken).ConfigureAwait(false);

                    // TODO: do we need version?
                    additionals.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            new RemoteTextLoader(documentSnapshot.Text),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var compilationOptions = await RoslynServices.AssetService.GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions, cancellationToken).ConfigureAwait(false);
                var parseOptions = await RoslynServices.AssetService.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions, cancellationToken).ConfigureAwait(false);

                projects.Add(
                    ProjectInfo.Create(
                        projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                        projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                        compilationOptions, parseOptions,
                        documents, p2p, metadata, analyzers, additionals));
            }

            return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
        }

        private class RemoteTextLoader : TextLoader
        {
            private readonly Checksum _checksum;

            public RemoteTextLoader(Checksum checksum)
            {
                _checksum = checksum;
            }

            public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var text = await RoslynServices.AssetService.GetAssetAsync<SourceText>(_checksum, cancellationToken).ConfigureAwait(false);
                return TextAndVersion.Create(text, VersionStamp.Create());
            }
        }
    }
}
