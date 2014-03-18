// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class CommandLineProject
    {
        /// <summary>
        /// Create a ProjectInfo structure initialized from a compilers command line arguments.
        /// </summary>
        public static ProjectInfo CreateProjectInfo(Workspace workspace, string projectName, string language, IEnumerable<string> commandLineArgs, string projectDirectory)
        {
            // TODO (tomat): the method may throw all sorts of exceptions.

            var languageServices = LanguageService.GetProvider(workspace, language);
            if (languageServices == null)
            {
                throw new ArgumentException(WorkspacesResources.UnrecognizedLanguageName);
            }

            var commandLineArgumentsFactory = languageServices.GetService<ICommandLineArgumentsFactoryService>();
            var commandLineArguments = commandLineArgumentsFactory.CreateCommandLineArguments(commandLineArgs, projectDirectory, isInteractive: false);

            // TODO (tomat): to match csc.exe/vbc.exe we should use CommonCommandLineCompiler.ExistingReferencesResolver to deal with #r's
            var fileResolver = new FileResolver(commandLineArguments.ReferencePaths, commandLineArguments.BaseDirectory);

            var strongNameProvider = new DesktopStrongNameProvider(commandLineArguments.KeyFileSearchPaths);

            // resolve all references, ignore those that can't be resolved
            var boundReferences = commandLineArguments.ResolveMetadataReferences(fileResolver, workspace.CurrentSolution.MetadataReferenceProvider);
            var unresolved = boundReferences.FirstOrDefault(r => r is UnresolvedMetadataReference);
            if (unresolved != null)
            {
                throw new ArgumentException(string.Format(WorkspacesResources.CantResolveMetadataReference, ((UnresolvedMetadataReference)unresolved).Reference));
            }

            AssemblyIdentityComparer assemblyIdentityComparer;
            if (commandLineArguments.AppConfigPath != null)
            {
                try
                {
                    using (var appConfigStream = new FileStream(commandLineArguments.AppConfigPath, FileMode.Open, FileAccess.Read))
                    {
                        assemblyIdentityComparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfigStream);
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException(string.Format(WorkspacesResources.ErrorWhileReadingSpecifiedConfigFile, e.Message));
                }
            }
            else
            {
                assemblyIdentityComparer = DesktopAssemblyIdentityComparer.Default;
            }

            var projectId = ProjectId.CreateNewId(debugName: projectName);

            // construct file infos
            var docs = new List<DocumentInfo>();
            var ids = new HashSet<DocumentId>();
            foreach (var fileArg in commandLineArguments.SourceFiles)
            {
                var absolutePath = Path.IsPathRooted(fileArg.Path) || string.IsNullOrEmpty(projectDirectory)
                    ? Path.GetFullPath(fileArg.Path)
                    : Path.GetFullPath(Path.Combine(projectDirectory, fileArg.Path));

                var relativePath = FilePathUtilities.GetRelativePath(projectDirectory, absolutePath);
                var isWithinProject = !Path.IsPathRooted(relativePath);

                var folderRoot = isWithinProject ? Path.GetDirectoryName(relativePath) : "";
                var folders = isWithinProject ? GetFolders(relativePath) : null;
                var name = Path.GetFileName(relativePath);
                var id = GetUniqueDocumentId(projectId, name, folderRoot, ids);

                var doc = DocumentInfo.Create(
                   id: id,
                   name: name,
                   folders: folders,
                   sourceCodeKind: fileArg.IsScript ? SourceCodeKind.Script : SourceCodeKind.Regular,
                   loader: new FileTextLoader(absolutePath),
                   filePath: absolutePath);

                docs.Add(doc);
            }

            // If /out is not specified and the project is a console app the csc.exe finds out the Main method
            // and names the compilation after the file that contains it. We don't want to create a compilation, 
            // bind Mains etc. here. Besides the msbuild always includes /out in the command line it produces.
            // So if we don't have the /out argument we name the compilation "<anonymous>".
            string assemblyName = (commandLineArguments.OutputFileName != null) ?
                Path.GetFileNameWithoutExtension(commandLineArguments.OutputFileName) : "<anonymous>";

            // TODO (tomat): what should be the assemblyName when compiling a netmodule? Should it be /moduleassemblyname

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectName,
                assemblyName,
                language: language,
                compilationOptions: commandLineArguments.CompilationOptions
                    .WithFileResolver(fileResolver)
                    .WithAssemblyIdentityComparer(assemblyIdentityComparer)
                    .WithStrongNameProvider(strongNameProvider),
                parseOptions: commandLineArguments.ParseOptions,
                documents: docs,
                metadataReferences: boundReferences);

            return projectInfo;
        }

        /// <summary>
        /// Create a ProjectInfo structure initialized with data from a compiler command line.
        /// </summary>
        public static ProjectInfo CreateProjectInfo(Workspace workspace, string projectName, string language, string commandLine, string baseDirectory)
        {
            var args = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: true);
            return CreateProjectInfo(workspace, projectName, language, args, baseDirectory);
        }

        private static readonly char[] folderSplitters = new char[] { Path.DirectorySeparatorChar };

        private static IList<string> GetFolders(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
            {
                return ImmutableList.Create<string>();
            }
            else
            {
                return directory.Split(folderSplitters, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
            }
        }

        private static DocumentId GetUniqueDocumentId(ProjectId projectId, string name, string folderRoot, HashSet<DocumentId> ids)
        {
            var id = DocumentId.CreateNewId(projectId, Path.Combine(folderRoot, name));
            int n = 1;

            while (ids.Contains(id))
            {
                id = DocumentId.CreateNewId(projectId, Path.Combine(folderRoot, name + (n++)));
            }

            return id;
        }
    }
}