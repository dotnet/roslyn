// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class ProjectFileLoaderRegistry
    {
        private readonly SolutionServices _solutionServices;
        private readonly DiagnosticReporter _diagnosticReporter;
        private readonly Dictionary<string, string> _extensionToLanguageMap;
        private readonly NonReentrantLock _dataGuard;

        public ProjectFileLoaderRegistry(SolutionServices solutionServices, DiagnosticReporter diagnosticReporter)
        {
            _solutionServices = solutionServices;
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

        public bool TryGetLoaderFromProjectPath(string? projectFilePath, [NotNullWhen(true)] out IProjectFileLoader? loader)
        {
            return TryGetLoaderFromProjectPath(projectFilePath, DiagnosticReportingMode.Ignore, out loader);
        }

        public bool TryGetLoaderFromProjectPath(string? projectFilePath, DiagnosticReportingMode mode, [NotNullWhen(true)] out IProjectFileLoader? loader)
        {
            using (_dataGuard.DisposableWait())
            {
                var extension = Path.GetExtension(projectFilePath);
                if (extension is null)
                {
                    loader = null;
                    _diagnosticReporter.Report(mode, $"Project file path was 'null'");
                    return false;
                }

                if (extension is ['.', .. var rest])
                    extension = rest;

                if (_extensionToLanguageMap.TryGetValue(extension, out var language))
                {
                    if (_solutionServices.SupportedLanguages.Contains(language))
                    {
                        loader = _solutionServices.GetLanguageServices(language).GetService<IProjectFileLoader>();
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
                    loader = ProjectFileLoader.GetLoaderForProjectFileExtension(_solutionServices, extension);

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
                    var commandLineParser = _solutionServices
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
