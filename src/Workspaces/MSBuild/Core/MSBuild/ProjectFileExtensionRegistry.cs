// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectFileExtensionRegistry
{
    private readonly DiagnosticReporter _diagnosticReporter;
    private readonly IFileBasedProgramService? _fileBasedProgramService;
    private readonly Dictionary<string, string> _extensionToLanguageMap;
    private readonly NonReentrantLock _dataGuard;

    public ProjectFileExtensionRegistry(DiagnosticReporter diagnosticReporter, IFileBasedProgramService? fileBasedProgramService)
    {
        _diagnosticReporter = diagnosticReporter;
        _fileBasedProgramService = fileBasedProgramService;

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
        return TryGetLanguageNameFromProjectPath(projectFilePath, mode, out languageName, out _);
    }

    public bool TryGetLanguageNameFromProjectPath(string? projectFilePath, DiagnosticReportingMode mode, [NotNullWhen(true)] out string? languageName, out bool isFileBasedApp)
    {
        using (_dataGuard.DisposableWait())
        {
            var extension = Path.GetExtension(projectFilePath);
            if (extension is null)
            {
                languageName = null;
                isFileBasedApp = false;
                _diagnosticReporter.Report(mode, $"Project file path was 'null'");
                return false;
            }

            Debug.Assert(projectFilePath != null);

            if (extension is ['.', .. var rest])
                extension = rest;

            if (_extensionToLanguageMap.TryGetValue(extension, out languageName))
            {
                isFileBasedApp = false;
                return true;
            }

            if (_fileBasedProgramService?.IsValidEntryPointPath(projectFilePath) == true)
            {
                languageName = LanguageNames.CSharp;
                isFileBasedApp = true;
                return true;
            }

            isFileBasedApp = false;
            _diagnosticReporter.Report(mode, string.Format(WorkspacesResources.Cannot_open_project_0_because_the_file_extension_1_is_not_associated_with_a_language, projectFilePath, Path.GetExtension(projectFilePath)));
            return false;
        }
    }
}
