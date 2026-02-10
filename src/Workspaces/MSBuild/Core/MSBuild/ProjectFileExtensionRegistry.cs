// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectFileExtensionRegistry
{
    private readonly DiagnosticReporter _diagnosticReporter;
    private readonly Dictionary<string, string> _extensionToLanguageMap;
    private readonly NonReentrantLock _dataGuard;

    public ProjectFileExtensionRegistry(DiagnosticReporter diagnosticReporter)
    {
        _diagnosticReporter = diagnosticReporter;

        _extensionToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "csproj", LanguageNames.CSharp },
            { "vbproj", LanguageNames.VisualBasic },
            { "fsproj", LanguageNames.FSharp }
        };

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

    public bool TryGetLanguageNameFromProjectPath(string? projectFilePath, DiagnosticReportingMode mode, [NotNullWhen(true)] out string? languageName)
    {
        using (_dataGuard.DisposableWait())
        {
            var extension = Path.GetExtension(projectFilePath);
            if (extension is null)
            {
                languageName = null;
                _diagnosticReporter.Report(mode, $"Project file path was 'null'");
                return false;
            }

            if (extension is ['.', .. var rest])
                extension = rest;

            if (_extensionToLanguageMap.TryGetValue(extension, out languageName))
                return true;

            _diagnosticReporter.Report(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, projectFilePath, Path.GetExtension(projectFilePath)));
            return false;
        }
    }
}
