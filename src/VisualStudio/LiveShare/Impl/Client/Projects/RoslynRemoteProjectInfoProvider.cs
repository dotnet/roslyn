// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects
{
    [Export(typeof(IRemoteProjectInfoProvider))]
    internal class RoslynRemoteProjectInfoProvider : IRemoteProjectInfoProvider
    {
        private const string SystemUriSchemeExternal = "vslsexternal";

        private readonly string[] _secondaryBufferFileExtensions = new string[] { ".cshtml", ".razor", ".html", ".aspx", ".vue" };
        private readonly CSharpLspClientServiceFactory _roslynLspClientServiceFactory;
        private readonly RemoteLanguageServiceWorkspace _remoteLanguageServiceWorkspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
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
                // This is also the case for files for which TypeScript adds the generated TypeScript buffer to a different project.
                var filesTasks = project.SourceFiles
                    .Where(f => f.Scheme != SystemUriSchemeExternal)
                    .Where(f => !this._secondaryBufferFileExtensions.Any(ext => f.LocalPath.EndsWith(ext)))
                    .Select(f => lspClient.ProtocolConverter.FromProtocolUriAsync(f, false, cancellationToken));
                var files = await Task.WhenAll(filesTasks).ConfigureAwait(false);
                var projectInfo = CreateProjectInfo(project.Name, project.Language, files.Select(f => f.LocalPath).ToImmutableArray());
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
