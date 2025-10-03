// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal readonly struct PathResolver(DiagnosticReporter? reporter, string? baseDirectory = null)
{
    private readonly DiagnosticReporter? _reporter = reporter;
    private readonly string? _baseDirectory = baseDirectory;

    public PathResolver WithBaseDirectory(string baseDirectory)
        => new(_reporter, baseDirectory);

    public bool TryGetAbsoluteSolutionFilePath(string filePath, DiagnosticReportingMode reportingMode, [NotNullWhen(true)] out string? absoluteFilePath)
    {
        try
        {
            absoluteFilePath = GetAbsoluteFilePath(filePath);
        }
        catch (Exception)
        {
            _reporter?.Report(reportingMode, string.Format(WorkspacesResources.Invalid_solution_file_path_colon_0, filePath));
            absoluteFilePath = null;
            return false;
        }

        if (!File.Exists(absoluteFilePath))
        {
            _reporter?.Report(
                reportingMode,
                string.Format(WorkspacesResources.Solution_file_not_found_colon_0, absoluteFilePath),
                msg => new FileNotFoundException(msg));
            return false;
        }

        return true;
    }

    public bool TryGetAbsoluteProjectFilePath(string filePath, DiagnosticReportingMode reportingMode, [NotNullWhen(true)] out string? absoluteFilePath)
    {
        try
        {
            absoluteFilePath = GetAbsoluteFilePath(filePath);
        }
        catch (Exception)
        {
            _reporter?.Report(reportingMode, string.Format(WorkspacesResources.Invalid_project_file_path_colon_0, filePath));
            absoluteFilePath = null;
            return false;
        }

        if (!File.Exists(absoluteFilePath))
        {
            _reporter?.Report(
                reportingMode,
                string.Format(WorkspacesResources.Project_file_not_found_colon_0, absoluteFilePath),
                msg => new FileNotFoundException(msg));
            return false;
        }

        return true;
    }

    private string GetAbsoluteFilePath(string filePath)
        => FileUtilities.NormalizeAbsolutePath(FileUtilities.ResolveRelativePath(filePath, _baseDirectory) ?? filePath);
}
