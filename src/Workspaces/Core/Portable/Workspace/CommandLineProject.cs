// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class CommandLineProject
    {
        /// <summary>
        /// Create a <see cref="ProjectInfo"/> structure initialized from a compilers command line arguments.
        /// </summary>
#pragma warning disable RS0026 // Type is forwarded from MS.CA.Workspaces.Desktop.
        public static ProjectInfo CreateProjectInfo(string projectName, string language, IEnumerable<string> commandLineArgs, string projectDirectory, Workspace? workspace = null)
#pragma warning restore RS0026 // Type is forwarded from MS.CA.Workspaces.Desktop.
        {
            // TODO (tomat): the method may throw all sorts of exceptions.
            workspace ??= new AdhocWorkspace();
            var languageServices = workspace.Services.SolutionServices.GetLanguageServices(language);
            if (languageServices == null)
            {
                throw new ArgumentException(WorkspacesResources.Unrecognized_language_name);
            }

            var commandLineParser = languageServices.GetRequiredService<ICommandLineParserService>();
            var commandLineArguments = commandLineParser.Parse(commandLineArgs, projectDirectory, isInteractive: false, sdkDirectory: RuntimeEnvironment.GetRuntimeDirectory());

            var metadataService = languageServices.SolutionServices.GetRequiredService<IMetadataService>();

            // we only support file paths in /r command line arguments
            var relativePathResolver =
                new RelativePathResolver(commandLineArguments.ReferencePaths, commandLineArguments.BaseDirectory);
            var commandLineMetadataReferenceResolver = new WorkspaceMetadataFileReferenceResolver(
                metadataService, relativePathResolver);

            var analyzerLoader = languageServices.SolutionServices.GetRequiredService<IAnalyzerService>().GetLoader();
            var xmlFileResolver = new XmlFileResolver(commandLineArguments.BaseDirectory);
            var strongNameProvider = new DesktopStrongNameProvider(commandLineArguments.KeyFileSearchPaths);

            // Resolve all metadata references.
            //
            // In the command line compiler, it's entirely possible that duplicate reference paths may appear in this list; in the compiler
            // each MetadataReference object is a distinct instance, and the deduplication is ultimately performed in the ReferenceManager
            // once the Compilation actually starts to read metadata. In this code however,  we're resolving with the IMetadataService, which
            // has a default implementation to cache and return the same MetadataReference instance for a duplicate. This means duplicate
            // reference path will create duplicate MetadataReference objects, which is disallowed by ProjectInfo.Create -- even though the
            // compiler eventually would have dealt with it just fine. It's reasonable the Workspace APIs disallow duplicate reference objects
            // since it makes the semantics of APIs like Add/RemoveMetadataReference tricky. But since we want to not break for command lines
            // with duplicate references, we'll do a .Distinct() here, and let the Compilation do any further deduplication
            // that isn't handled by this explicit instance check. This does mean that the Compilations produced through this API
            // won't produce the "duplicate metadata reference" diagnostic like the real command line compiler would, but that's probably fine.
            //
            // Alternately, we could change the IMetadataService behavior to simply not cache, but that could theoretically break other
            // callers that would now see references across projects not be the same, or hurt performance for users of MSBuildWorkspace. Given
            // this is an edge case, it's not worth the larger fix here.
            var boundMetadataReferences = commandLineArguments.ResolveMetadataReferences(commandLineMetadataReferenceResolver).Distinct().ToList();
            var unresolvedMetadataReferences = boundMetadataReferences.FirstOrDefault(r => r is UnresolvedMetadataReference);
            if (unresolvedMetadataReferences != null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.Can_t_resolve_metadata_reference_colon_0, ((UnresolvedMetadataReference)unresolvedMetadataReferences).Reference));
            }

            // resolve all analyzer references.
            foreach (var path in commandLineArguments.AnalyzerReferences.Select(r => r.FilePath))
            {
                analyzerLoader.AddDependencyLocation(relativePathResolver.ResolvePath(path, baseFilePath: null));
            }

            var boundAnalyzerReferences = commandLineArguments.ResolveAnalyzerReferences(analyzerLoader).Distinct().ToList();
            var unresolvedAnalyzerReferences = boundAnalyzerReferences.FirstOrDefault(r => r is UnresolvedAnalyzerReference);
            if (unresolvedAnalyzerReferences != null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.Can_t_resolve_analyzer_reference_colon_0, ((UnresolvedAnalyzerReference)unresolvedAnalyzerReferences).Display));
            }

            AssemblyIdentityComparer assemblyIdentityComparer;
            if (commandLineArguments.AppConfigPath != null)
            {
                try
                {
                    using var appConfigStream = new FileStream(commandLineArguments.AppConfigPath, FileMode.Open, FileAccess.Read);

                    assemblyIdentityComparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream);
                }
                catch (Exception e)
                {
                    throw new ArgumentException(string.Format(WorkspacesResources.An_error_occurred_while_reading_the_specified_configuration_file_colon_0, e.Message));
                }
            }
            else
            {
                assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            }

            var projectId = ProjectId.CreateNewId(debugName: projectName);

            // If /out is not specified and the project is a console app the csc.exe finds out the Main method
            // and names the compilation after the file that contains it. We don't want to create a compilation, 
            // bind Mains etc. here. Besides the msbuild always includes /out in the command line it produces.
            // So if we don't have the /out argument we name the compilation "<anonymous>".
            var assemblyName = (commandLineArguments.OutputFileName != null) ?
                Path.GetFileNameWithoutExtension(commandLineArguments.OutputFileName) : "<anonymous>";

            // TODO (tomat): what should be the assemblyName when compiling a netmodule? Should it be /moduleassemblyname

            var projectInfo = ProjectInfo.Create(
                new ProjectInfo.ProjectAttributes(
                    id: projectId,
                    version: VersionStamp.Create(),
                    name: projectName,
                    assemblyName: assemblyName,
                    language: language,
                    compilationOutputFilePaths: new CompilationOutputInfo(commandLineArguments.OutputFileName != null ? commandLineArguments.GetOutputFilePath(commandLineArguments.OutputFileName) : null),
                    checksumAlgorithm: commandLineArguments.ChecksumAlgorithm),
                compilationOptions: commandLineArguments.CompilationOptions
                    .WithXmlReferenceResolver(xmlFileResolver)
                    .WithAssemblyIdentityComparer(assemblyIdentityComparer)
                    .WithStrongNameProvider(strongNameProvider)
                    // TODO (https://github.com/dotnet/roslyn/issues/4967): 
                    .WithMetadataReferenceResolver(new WorkspaceMetadataFileReferenceResolver(metadataService, new RelativePathResolver(ImmutableArray<string>.Empty, projectDirectory))),
                parseOptions: commandLineArguments.ParseOptions,
                documents: CreateDocuments(commandLineArguments.SourceFiles),
                projectReferences: null,
                metadataReferences: boundMetadataReferences,
                analyzerReferences: boundAnalyzerReferences,
                additionalDocuments: CreateDocuments(commandLineArguments.AdditionalFiles),
                analyzerConfigDocuments: CreateDocuments(commandLineArguments.AnalyzerConfigPaths.SelectAsArray(p => new CommandLineSourceFile(p, isScript: false))),
                hostObjectType: null);

            return projectInfo;

            IList<DocumentInfo> CreateDocuments(ImmutableArray<CommandLineSourceFile> files)
            {
                var documents = new List<DocumentInfo>();

                foreach (var fileArg in files)
                {
                    var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
                        ? Path.GetFullPath(fileArg.Path)
                        : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

                    var relativePath = PathUtilities.GetRelativePath(projectDirectory, absolutePath);
                    var isWithinProject = PathUtilities.IsChildPath(projectDirectory, absolutePath);

                    var folderRoot = isWithinProject ? Path.GetDirectoryName(relativePath) : "";
                    var folders = isWithinProject ? GetFolders(relativePath) : null;
                    var name = Path.GetFileName(relativePath);
                    var id = DocumentId.CreateNewId(projectId, absolutePath);

                    var doc = DocumentInfo.Create(
                       id,
                       name,
                       folders: folders,
                       sourceCodeKind: fileArg.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular,
                       loader: new WorkspaceFileTextLoader(languageServices.SolutionServices, absolutePath, commandLineArguments.Encoding),
                       filePath: absolutePath);

                    documents.Add(doc);
                }

                return documents;
            }
        }

        /// <summary>
        /// Create a <see cref="ProjectInfo"/> structure initialized with data from a compiler command line.
        /// </summary>
#pragma warning disable RS0026 // Type is forwarded from MS.CA.Workspaces.Desktop.
        public static ProjectInfo CreateProjectInfo(string projectName, string language, string commandLine, string baseDirectory, Workspace? workspace = null)
#pragma warning restore RS0026 // Type is forwarded from MS.CA.Workspaces.Desktop.
        {
            var args = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: true);
            return CreateProjectInfo(projectName, language, args, baseDirectory, workspace);
        }

        private static readonly char[] s_folderSplitters = new char[] { Path.DirectorySeparatorChar };

        private static IList<string> GetFolders(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                return ImmutableArray.Create<string>();
            }
            else
            {
                return directory.Split(s_folderSplitters, StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            }
        }
    }
}
