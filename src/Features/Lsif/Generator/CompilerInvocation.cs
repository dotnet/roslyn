﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator
{
    internal class CompilerInvocation
    {
        public Compilation Compilation { get; }
        public HostLanguageServices LanguageServices { get; internal set; }
        public string ProjectFilePath { get; internal set; }

        public CompilerInvocation(Compilation compilation, HostLanguageServices languageServices, string projectFilePath)
        {
            Compilation = compilation;
            LanguageServices = languageServices;
            ProjectFilePath = projectFilePath;
        }

        public static async Task<CompilerInvocation> CreateFromJsonAsync(string jsonContents)
        {
            var invocationInfo = JsonConvert.DeserializeObject<CompilerInvocationInfo>(jsonContents);

            // We will use a Workspace to simplify the creation of the compilation, but will be careful not to return the Workspace instance from this class.
            // We will still provide the language services which are used by the generator itself, but we don't tie it to a Workspace object so we can
            // run this as an in-proc source generator if one day desired.
            var workspace = new AdhocWorkspace();

            var languageName = GetLanguageName(invocationInfo);
            var languageServices = workspace.Services.GetLanguageServices(languageName);

            var mapPath = GetPathMapper(invocationInfo);

            var splitCommandLine = CommandLineParser.SplitCommandLineIntoArguments(invocationInfo.Arguments, removeHashComments: false).ToList();

            // Unfortunately for us there are a few paths that get directly read by the command line parse which we need to remap,
            // such as /ruleset files. So let's go through and process them now.
            for (var i = 0; i < splitCommandLine.Count; i++)
            {
                const string RuleSetSwitch = "/ruleset:";

                if (splitCommandLine[i].StartsWith(RuleSetSwitch, StringComparison.Ordinal))
                {
                    var rulesetPath = splitCommandLine[i].Substring(RuleSetSwitch.Length);

                    var quoted = rulesetPath.Length > 2 &&
                        rulesetPath.StartsWith("\"", StringComparison.Ordinal) &&
                        rulesetPath.EndsWith("\"", StringComparison.Ordinal);

                    if (quoted)
                    {
                        rulesetPath = rulesetPath.Substring(1, rulesetPath.Length - 2);
                    }

                    rulesetPath = mapPath(rulesetPath);

                    if (quoted)
                    {
                        rulesetPath = "\"" + rulesetPath + "\"";
                    }

                    splitCommandLine[i] = RuleSetSwitch + rulesetPath;
                }
            }

            var commandLineParserService = languageServices.GetRequiredService<ICommandLineParserService>();
            var parsedCommandLine = commandLineParserService.Parse(splitCommandLine, Path.GetDirectoryName(invocationInfo.ProjectFilePath), isInteractive: false, sdkDirectory: null);

            var projectId = ProjectId.CreateNewId(invocationInfo.ProjectFilePath);

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Default,
                name: Path.GetFileNameWithoutExtension(invocationInfo.ProjectFilePath),
                assemblyName: parsedCommandLine.CompilationName!,
                language: languageName,
                filePath: invocationInfo.ProjectFilePath,
                outputFilePath: parsedCommandLine.OutputFileName,
                parsedCommandLine.CompilationOptions,
                parsedCommandLine.ParseOptions,
                parsedCommandLine.SourceFiles.Select(s => CreateDocumentInfo(unmappedPath: s.Path)),
                metadataReferences: parsedCommandLine.MetadataReferences.Select(r => MetadataReference.CreateFromFile(mapPath(r.Reference), r.Properties)),
                additionalDocuments: parsedCommandLine.AdditionalFiles.Select(f => CreateDocumentInfo(unmappedPath: f.Path)))
                .WithAnalyzerConfigDocuments(parsedCommandLine.AnalyzerConfigPaths.Select(CreateDocumentInfo));

            workspace.AddProject(projectInfo);

            var compilation = await workspace.CurrentSolution.GetProject(projectId)!.GetRequiredCompilationAsync(CancellationToken.None);

            return new CompilerInvocation(compilation, languageServices, invocationInfo.ProjectFilePath);

            // Local methods:
            DocumentInfo CreateDocumentInfo(string unmappedPath)
            {
                var mappedPath = mapPath(unmappedPath);
                return DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId, mappedPath),
                    name: mappedPath,
                    filePath: mappedPath,
                    loader: new FileTextLoader(mappedPath, parsedCommandLine.Encoding));
            }
        }

        private static string GetLanguageName(CompilerInvocationInfo invocationInfo)
        {
            return invocationInfo.Tool switch
            {
                "csc" => LanguageNames.CSharp,
                "vbc" => LanguageNames.VisualBasic,
                _ => throw new NotSupportedException($"Tool '{invocationInfo.Tool}' is not supported."),
            };
        }

        /// <summary>
        /// Given the JSON description, returns a function that will map paths from the original paths to the current paths.
        /// </summary>
        /// <remarks>
        /// The compiler invocation JSON input is allowed to specify a map of file paths. The scenario here is to allow us to
        /// do LSIF indexing on another machine than an original build might have been done on. For example, say the main build process
        /// for a repository has the source synchronized to the S:\source1, but we want to do analysis on a different machine which has
        /// the source in a folder S:\source2. If we have the original compilation command line when it was built under S:\source1, and
        /// know that any time we see S:\source1 we should actually read the file out of S:\source2, then we analyze on a separate machine.
        /// 
        /// This is used to enable some internal-to-Microsoft build environments which have a mechanism to run "analysis" passes like
        /// the LSIF tool independent from the main build machines, and can restore source and build artifacts to provide the environment
        /// that is close enough to match the original.
        /// </remarks>
        private static Func<string, string> GetPathMapper(CompilerInvocationInfo invocationInfo)
        {
            return unmappedPath =>
            {
                foreach (var potentialPathMapping in invocationInfo.PathMappings)
                {
                    // If it's just a file name being mapped, just a direct map
                    if (unmappedPath.Equals(potentialPathMapping.From, StringComparison.OrdinalIgnoreCase))
                    {
                        return potentialPathMapping.To;
                    }

                    // Map arbitrary contents under subdirectories
                    var fromWithDirectorySuffix = AddDirectorySuffixIfMissing(potentialPathMapping.From);

                    if (unmappedPath.StartsWith(fromWithDirectorySuffix, StringComparison.OrdinalIgnoreCase))
                    {
                        // Trim off any leading \, which would happen if you have a path like C:\Directory\\File.cs with a double slash, and happen to be
                        // mapping C:\Directory somewhere.
                        var relativePath = unmappedPath.Substring(fromWithDirectorySuffix.Length).TrimStart('\\');

                        return Path.Combine(AddDirectorySuffixIfMissing(potentialPathMapping.To), relativePath);
                    }
                }

                return unmappedPath;
            };

            static string AddDirectorySuffixIfMissing(string path)
            {
                return path.EndsWith("\\", StringComparison.OrdinalIgnoreCase) ? path : path + "\\";
            }
        }

        /// <summary>
        /// A simple data class that represents the schema for JSON serialization.
        /// </summary>
        private sealed class CompilerInvocationInfo
        {
#nullable disable // this class is used for deserialization by Newtonsoft.Json, so we don't really need warnings about this class itself

            public string Tool { get; set; }

            public string Arguments { get; set; }

            public string ProjectFilePath { get; set; }

            public List<PathMapping> PathMappings { get; set; } = new List<PathMapping>();

            public sealed class PathMapping
            {
                public string From { get; set; }
                public string To { get; set; }
            }

#nullable disable
        }
    }
}
