// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.LanguageServices;
using CustomProtocol = Microsoft.VisualStudio.LanguageServices.LiveShare.CustomProtocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Projects
{
    [Export(typeof(IRemoteProjectInfoProvider))]
    internal class RoslynRemoteProjectInfoProvider : IRemoteProjectInfoProvider
    {
        private const string SystemUriSchemeExternal = "vslsexternal";

        private readonly CSharpLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;

        [ImportingConstructor]
        public RoslynRemoteProjectInfoProvider(CSharpLspClientServiceFactory roslynLspClientServiceFactory, RemoteLanguageServiceWorkspace remoteLanguageServiceWorkspace)
        {
            _roslynLspClientServiceFactory = roslynLspClientServiceFactory ?? throw new ArgumentNullException(nameof(roslynLspClientServiceFactory));
            _remoteLanguageServiceWorkspace = remoteLanguageServiceWorkspace ?? throw new ArgumentNullException(nameof(RemoteLanguageServiceWorkspace));
        }

        public async Task<ImmutableArray<ProjectInfo>> GetRemoteProjectInfosAsync(CancellationToken cancellationToken)
        {
            if (!_remoteLanguageServiceWorkspace.IsRemoteSession)
            {
                return ImmutableArray<ProjectInfo>.Empty;
            }

            var lspClient = _roslynLspClientServiceFactory.ActiveLanguageServerClient;
            if (lspClient == null)
            {
                return ImmutableArray<ProjectInfo>.Empty;
            }

            CustomProtocol.Project[] projects;
            try
            {
                var request = new LspRequest<object, CustomProtocol.Project[]>(CustomProtocol.RoslynMethods.ProjectsName);
                projects = await lspClient.RequestAsync(request, new object(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                projects = null;
            }

            if (projects == null)
            {
                return ImmutableArray<ProjectInfo>.Empty;
            }

            var projectInfos = ImmutableArray.CreateBuilder<ProjectInfo>();
            foreach (var project in projects)
            {
                // We don't want to add cshtml files to the workspace since the Roslyn will add the generated secondary buffer of a cshtml
                // file to a different project but with the same path. This used to be ok in Dev15 but in Dev16 this confuses Roslyn and causes downstream
                // issues. There's no need to add the actual cshtml file to the workspace - so filter those out.
                var filesTasks = project.SourceFiles
                    .Where(f => f.Scheme != SystemUriSchemeExternal)
                    .Where(f => !f.LocalPath.EndsWith(".cshtml"))
                    .Select(f => lspClient.ProtocolConverter.FromProtocolUriAsync(f, false, cancellationToken));
                var files = await Task.WhenAll(filesTasks).ConfigureAwait(false);
                string language;
                switch (project.Language)
                {
                    case LanguageNames.CSharp:
                        language = StringConstants.CSharpLspLanguageName;
                        break;
                    case LanguageNames.VisualBasic:
                        language = StringConstants.VBLspLanguageName;
                        break;
                    default:
                        language = project.Language;
                        break;
                }
                var projectInfo = CreateProjectInfo(project.Name, language, files.Select(f => f.LocalPath).ToImmutableArray());
                projectInfos.Add(projectInfo);
            }

            return projectInfos.ToImmutableArray();
        }

        private static ProjectInfo CreateProjectInfo(string projectName, string language, ImmutableArray<string> files)
        {
            var projectId = ProjectId.CreateNewId();
            var docInfos = ImmutableArray.CreateBuilder<DocumentInfo>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(projectId),
                    fileName,
                    filePath: file,
                    loader: new FileTextLoaderNoException(file, null));
                docInfos.Add(docInfo);
            }

            return ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                projectName,
                projectName,
                language,
                documents: docInfos.ToImmutable());
        }
    }
}
