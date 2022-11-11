// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Defines a default workspace for opened LSP files that are not found in any
    /// workspace registered by the <see cref="LspWorkspaceRegistrationService"/>.
    /// If a document added here is subsequently found in a registered workspace, 
    /// the document is removed from this workspace.
    /// 
    /// Future work for this workspace includes supporting basic metadata references (mscorlib, System dlls, etc),
    /// but that is dependent on having a x-plat mechanism for retrieving those references from the framework / sdk.
    /// </summary>
    internal class LspMiscellaneousFilesWorkspace : Workspace, ILspService
    {
        private static readonly LanguageInformation s_csharpLanguageInformation = new(LanguageNames.CSharp, ".csx");
        private static readonly LanguageInformation s_vbLanguageInformation = new(LanguageNames.VisualBasic, ".vbx");

        private static readonly Dictionary<string, LanguageInformation> s_extensionToLanguageInformation = new()
        {
            { ".cs", s_csharpLanguageInformation },
            { ".csx", s_csharpLanguageInformation },
            { ".vb", s_vbLanguageInformation },
            { ".vbx", s_vbLanguageInformation },
        };

        public LspMiscellaneousFilesWorkspace() : base(MefHostServices.DefaultHost, WorkspaceKind.MiscellaneousFiles)
        {
        }

        /// <summary>
        /// Takes in a file URI and text and creates a misc project and document for the file.
        /// 
        /// Calls to this method and <see cref="TryRemoveMiscellaneousDocument(Uri)"/> are made
        /// from LSP text sync request handling which do not run concurrently.
        /// </summary>
        public Document? AddMiscellaneousDocument(Uri uri, SourceText documentText, ILspLogger logger)
        {
            var uriAbsolutePath = uri.AbsolutePath;
            if (!s_extensionToLanguageInformation.TryGetValue(Path.GetExtension(uriAbsolutePath), out var languageInformation))
            {
                // Only log here since throwing here could take down the LSP server.
                logger.LogError($"Could not find language information for {uri} with absolute path {uriAbsolutePath}");
                return null;
            }

            var sourceTextLoader = new SourceTextLoader(documentText, uriAbsolutePath);

            var projectInfo = MiscellaneousFileUtilities.CreateMiscellaneousProjectInfoForDocument(uri.AbsolutePath, sourceTextLoader, languageInformation, documentText.ChecksumAlgorithm, Services.SolutionServices, ImmutableArray<MetadataReference>.Empty);
            OnProjectAdded(projectInfo);

            var id = projectInfo.Documents.Single().Id;
            return CurrentSolution.GetRequiredDocument(id);
        }

        /// <summary>
        /// Removes a document with the matching file path from this workspace.
        /// 
        /// Calls to this method and <see cref="AddMiscellaneousDocument(Uri, SourceText, ILspLogger)"/> are made
        /// from LSP text sync request handling which do not run concurrently.
        /// </summary>
        public void TryRemoveMiscellaneousDocument(Uri uri)
        {
            var uriAbsolutePath = uri.AbsolutePath;

            // We only add misc files to this workspace using the absolute file path.
            var matchingDocument = CurrentSolution.GetDocumentIdsWithFilePath(uriAbsolutePath).SingleOrDefault();
            if (matchingDocument != null)
            {
                OnDocumentRemoved(matchingDocument);

                // Also remove the project - we always create a new project for each misc file we add
                // so it should never have other documents in it.
                var project = CurrentSolution.GetRequiredProject(matchingDocument.ProjectId);
                OnProjectRemoved(project.Id);
            }
        }

        private sealed class SourceTextLoader : TextLoader
        {
            private readonly SourceText _sourceText;
            private readonly string _fileUri;

            public SourceTextLoader(SourceText sourceText, string fileUri)
            {
                _sourceText = sourceText;
                _fileUri = fileUri;
            }

            internal override string? FilePath
                => _fileUri;

            // TODO (https://github.com/dotnet/roslyn/issues/63583): Use options.ChecksumAlgorithm 
            internal override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
                => Task.FromResult(TextAndVersion.Create(_sourceText, VersionStamp.Create(), _fileUri));
        }
    }
}
