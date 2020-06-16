// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class ProjectFileLoaderRegistry
    {
        private readonly HostWorkspaceServices _workspaceServices;
        private readonly DiagnosticReporter _diagnosticReporter;
        private readonly Dictionary<string, string> _extensionToLanguageMap;
        private readonly NonReentrantLock _dataGuard;

        public ProjectFileLoaderRegistry(HostWorkspaceServices workspaceServices, DiagnosticReporter diagnosticReporter)
        {
            _workspaceServices = workspaceServices;
            _diagnosticReporter = diagnosticReporter;
            _extensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _dataGuard = new NonReentrantLock();
        }

        /// <summary>
        /// Associates a project file extension with a language name.
        /// </summary>
        public void AssociateFileExtensionWithLanguage(string fileExtension, string language)
        {
            using (_dataGuard.DisposableWait())
            {
                _extensionToLanguageMap[fileExtension] = language;
            }
        }

        public bool TryGetLoaderFromProjectPath(string projectFilePath, out IProjectFileLoader loader)
        {
            return TryGetLoaderFromProjectPath(projectFilePath, DiagnosticReportingMode.Ignore, out loader);
        }

        public bool TryGetLoaderFromProjectPath(string projectFilePath, DiagnosticReportingMode mode, out IProjectFileLoader loader)
        {
            using (_dataGuard.DisposableWait())
            {
                var extension = Path.GetExtension(projectFilePath);
                if (extension.Length > 0 && extension[0] == '.')
                {
                    extension = extension.Substring(1);
                }

                if (_extensionToLanguageMap.TryGetValue(extension, out var language))
                {
                    if (_workspaceServices.SupportedLanguages.Contains(language))
                    {
                        loader = _workspaceServices.GetLanguageServices(language).GetService<IProjectFileLoader>();
                    }
                    else
                    {
                        loader = null;
                        _diagnosticReporter.Report(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectFilePath, language));
                        return false;
                    }
                }
                else
                {
                    loader = ProjectFileLoader.GetLoaderForProjectFileExtension(_workspaceServices, extension);

                    if (loader == null)
                    {
                        _diagnosticReporter.Report(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, projectFilePath, Path.GetExtension(projectFilePath)));
                        return false;
                    }
                }

                // since we have both C# and VB loaders in this same library, it no longer indicates whether we have full language support available.
                if (loader != null)
                {
                    language = loader.Language;

                    // check for command line parser existing... if not then error.
                    var commandLineParser = _workspaceServices
                        .GetLanguageServices(language)
                        .GetService<ICommandLineParserService>();

                    if (commandLineParser == null)
                    {
                        loader = null;
                        _diagnosticReporter.Report(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_language_1_is_not_supported, projectFilePath, language));
                        return false;
                    }
                }

                return loader != null;
            }
        }
    }
}
