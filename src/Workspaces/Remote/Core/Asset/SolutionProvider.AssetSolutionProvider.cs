using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Roslyn.Assets
{
    public static partial class SolutionProvider
    {
        private class AssetSolutionProvider : ISolutionProvider
        {
            private readonly ISolutionAsset _asset;
            private readonly ImmutableArray<Assembly> _hostAssemblies;

            public AssetSolutionProvider(ISolutionAsset asset, IEnumerable<Assembly> hostAssemblies)
            {
                _asset = asset;
                _hostAssemblies = hostAssemblies.ToImmutableArray();
            }

            public async Task<Solution> CreateSolutionAsync(CancellationToken cancellationToken)
            {
                var projectInfos = new List<ProjectInfo>();
                var projectAssets = await this._asset.GetProjectsAsync(cancellationToken).ConfigureAwait(false);

                var projectIdMap = CreateProjectIdMap(projectAssets, cancellationToken);

                foreach (var projectAsset in projectAssets)
                {
                    var projectId = projectIdMap[projectAsset.Id];

                    // when projectAsset.Id should be used?

                    // current API is little bit inconvinience to use since one needs to tie together various information themselves, and there is
                    // no clear one thing to correlate information together. it is sometime uniquekey sometimes full path and etc.

                    // we will need these eventually. just not now
                    // we need to provide xml file resolver that is not based on file system
                    // .WithXmlReferenceResolver(new XmlFileResolver(projectDirectory))
                    // we don't support scripting for now. so we are good. otherwise, we need to provide one that is not based on file system
                    // .WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray<string>.Empty, projectDirectory))
                    // we need strong name provider to support IVT. but not based on file system
                    // .WithStrongNameProvider(new DesktopStrongNameProvider(commandLineArgs.KeyFileSearchPaths))
                    //
                    // we won't need this
                    // we give in metadata reference directly, so no need for metadata resolver
                    // .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory)))
                    // for now, we use default. it is used for symbol retarget and etc.
                    // .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default);

                    // var additionalDocuments = CreateDocumentInfos(projectFileInfo.AdditionalDocuments, projectId, commandLineArgs.Encoding);
                    // var analyzerReferences = ResolveAnalyzerReferences(commandLineArgs);
                    var commandLineArgs = ParseAndGetCommandLineArguments(projectAsset);
                    var version = VersionStamp.Create();

                    // analyzerReferences: analyzerReferences,
                    // additionalDocuments: additionalDocuments,
                    var projectInfo = ProjectInfo.Create(
                        projectId,
                        version,
                        projectAsset.Name,
                        commandLineArgs.CompilationName,
                        projectAsset.LanguageName,
                        projectAsset.FullPath,
                        outputFilePath: NormalizeFilePath(Path.Combine(commandLineArgs.OutputDirectory, commandLineArgs.OutputFileName)),
                        compilationOptions: commandLineArgs.CompilationOptions,
                        parseOptions: commandLineArgs.ParseOptions,
                        documents: await CreateDocumentInfosAsync(commandLineArgs, projectId, projectAsset, cancellationToken).ConfigureAwait(false),
                        projectReferences: await CreateProjectRefernceAsync(projectIdMap, commandLineArgs, projectAsset, cancellationToken).ConfigureAwait(false),
                        metadataReferences: await CreateMetadataReferencesAsync(commandLineArgs, projectAsset, cancellationToken).ConfigureAwait(false),
                        isSubmission: false,
                        hostObjectType: null);

                    projectInfos.Add(projectInfo);
                }

                var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId("No solution info"), VersionStamp.Create(), projects: projectInfos);

                var workspace = new AdhocWorkspace(MefHostServices.Create(ExternalHostAssemblies.Concat(_hostAssemblies).Distinct()));
                return workspace.AddSolution(solutionInfo);
            }

            private static CommandLineArguments ParseAndGetCommandLineArguments(IProjectAsset projectAsset)
            {
                var arguments = CommandLineParser.SplitCommandLineIntoArguments(projectAsset.CommandLineArgs, removeHashComments: false);

                // remove arguments we can't support
                arguments = arguments.Where(a => !a.StartsWith("/ruleset:", StringComparison.OrdinalIgnoreCase));

                var projectPath = projectAsset.FullPath;

                if (projectAsset.LanguageName == LanguageNames.CSharp)
                {
                    return CSharpCommandLineParser.Default.Parse(
                        arguments,
                        Path.GetDirectoryName(projectPath),
                        sdkDirectory: null);
                }

                // return VB command line arguments later
                return null;
            }

            private static Dictionary<object, ProjectId> CreateProjectIdMap(IEnumerable<IProjectAsset> projectAssets, CancellationToken cancellationToken)
            {
                // build projectAsset to projectId pair
                var projectIdMap = new Dictionary<object, ProjectId>();
                foreach (var projectAsset in projectAssets)
                {
                    var key = projectAsset.Id;
                    var projectId = ProjectId.CreateNewId(projectAsset.FullPath);

                    projectIdMap.Add(key, projectId);
                }

                return projectIdMap;
            }

            private static string NormalizeFilePath(string filePath)
            {
                if (filePath == null)
                {
                    return null;
                }

                // this won't 100% work I believe since one can put escape in path in windows I believe?
                return filePath.Replace('\\', '/');
            }

            private static async Task<IEnumerable<DocumentInfo>> CreateDocumentInfosAsync(CommandLineArguments commandLineArgs, ProjectId projectId, IProjectAsset project, CancellationToken cancellationToken)
            {
                var documents = new List<DocumentInfo>();

                foreach (var document in await project.GetDocumentsAsync(cancellationToken).ConfigureAwait(false))
                {
                    var filePath = document.FilePath;
                    var lastModified = document.LastModified;

                    documents.Add(DocumentInfo.Create(
                        DocumentId.CreateNewId(projectId, filePath),
                        document.Name,
                        document.Folders,
                        loader: new StreamLoader(document, commandLineArgs.Encoding, lastModified, filePath),
                        filePath: filePath));
                }

                return documents;
            }

            private static async Task<IEnumerable<ProjectReference>> CreateProjectRefernceAsync(
                Dictionary<object, ProjectId> projectIdMap, CommandLineArguments commandLineArgs, IProjectAsset project, CancellationToken cancellationToken)
            {
                var references = new List<ProjectReference>();
                var currentProjectId = projectIdMap[project.Id];

                foreach (var (fullPath, id) in await project.GetProjectReferencesAsync(cancellationToken).ConfigureAwait(false))
                {
                    var commandLineReference = commandLineArgs.MetadataReferences.Single(r => r.Reference == fullPath);
                    var referencedId = projectIdMap[id];

                    references.Add(new ProjectReference(referencedId, commandLineReference.Properties.Aliases, commandLineReference.Properties.EmbedInteropTypes));
                }

                return references;
            }

            private static async Task<IEnumerable<MetadataReference>> CreateMetadataReferencesAsync(CommandLineArguments commandLineArgs, IProjectAsset project, CancellationToken cancellationToken)
            {
                var references = new List<MetadataReference>();
                foreach (var (fullPath, stream) in await project.GetMetadataReferencesAsync(cancellationToken).ConfigureAwait(false))
                {
                    var commandLineReference = commandLineArgs.MetadataReferences.First(r => r.Reference == fullPath);

                    // no cancellation...
                    // properties needs to be set. right now, no way to correlate references to command line args
                    if (stream == null)
                    {
                        // how?
                        continue;
                    }

                    var metadataReference = MetadataReference.CreateFromStream(stream, commandLineReference.Properties, filePath: fullPath);
                    if (metadataReference == null)
                    {
                        // how?
                        continue;
                    }

                    references.Add(metadataReference);
                }

                return references;
            }

            private class StreamLoader : TextLoader
            {
                private readonly IDocumentAsset document;

                private readonly Encoding encoding;
                private readonly DateTimeOffset? lastModified;
                private readonly string filePath;

                public StreamLoader(IDocumentAsset document, Encoding encoding, DateTimeOffset? lastModified, string filePath)
                {
                    this.document = document;
                    this.encoding = encoding;
                    this.lastModified = lastModified;
                    this.filePath = filePath;
                }

                public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var version = VersionStamp.Create(this.lastModified?.UtcDateTime ?? DateTime.UtcNow);

                    try
                    {
                        // no cancellation?
                        using (var stream = await this.document.GetContentAsync(cancellationToken).ConfigureAwait(false))
                        {
                            return TextAndVersion.Create(SourceText.From(stream, this.encoding), version, this.filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        var dummy = ex;

                        // this should never happen...
                        return TextAndVersion.Create(SourceText.From(string.Empty, this.encoding), version, this.filePath);
                    }
                }
            }
        }
    }
}
