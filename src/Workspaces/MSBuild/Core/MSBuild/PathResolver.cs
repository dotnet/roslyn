// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal class PathResolver
    {
        private readonly DiagnosticReporter _diagnosticReporter;

        public PathResolver(DiagnosticReporter diagnosticReporter)
        {
            _diagnosticReporter = diagnosticReporter;
        }

        public bool TryGetAbsoluteSolutionPath(string path, string baseDirectory, DiagnosticReportingMode reportingMode, [NotNullWhen(true)] out string? absolutePath)
        {
            try
            {
                absolutePath = GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                _diagnosticReporter.Report(reportingMode, string.Format(WorkspacesResources.Invalid_solution_file_path_colon_0, path));
                absolutePath = null;
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                _diagnosticReporter.Report(
                    reportingMode,
                    string.Format(WorkspacesResources.Solution_file_not_found_colon_0, absolutePath),
                    msg => new FileNotFoundException(msg));
                return false;
            }

            return true;
        }

        public bool TryGetAbsoluteProjectPath(string path, string baseDirectory, DiagnosticReportingMode reportingMode, [NotNullWhen(true)] out string? absolutePath)
        {
            try
            {
                absolutePath = GetAbsolutePath(path, baseDirectory);
            }
            catch (Exception)
            {
                _diagnosticReporter.Report(reportingMode, string.Format(WorkspacesResources.Invalid_project_file_path_colon_0, path));
                absolutePath = null;
                return false;
            }

            if (!File.Exists(absolutePath))
            {
                _diagnosticReporter.Report(
                    reportingMode,
                    string.Format(WorkspacesResources.Project_file_not_found_colon_0, absolutePath),
                    msg => new FileNotFoundException(msg));
                return false;
            }

            return true;
        }

        private static string GetAbsolutePath(string path, string baseDirectory)
            => FileUtilities.NormalizeAbsolutePath(FileUtilities.ResolveRelativePath(path, baseDirectory) ?? path);
    }
}
