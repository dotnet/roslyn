// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal partial class SolutionFileReader
{
    public static (string AbsoluteSolutionPath, ImmutableArray<(string ProjectPath, string ProjectGuid)> Projects) ReadSolutionFile(string solutionFilePath)
    {
        return ReadSolutionFile(solutionFilePath, new PathResolver(diagnosticReporter: null));
    }

    public static (string AbsoluteSolutionPath, ImmutableArray<(string ProjectPath, string ProjectGuid)> Projects) ReadSolutionFile(string solutionFilePath, PathResolver pathResolver)
    {
        if (!pathResolver.TryGetAbsoluteSolutionPath(solutionFilePath, baseDirectory: Directory.GetCurrentDirectory(), DiagnosticReportingMode.Throw, out var absoluteSolutionPath))
        {
            // TryGetAbsoluteSolutionPath should throw before we get here.
            return (solutionFilePath, []);
        }

        var projectFilter = ImmutableHashSet<string>.Empty;
        if (SolutionFilterReader.IsSolutionFilterFilename(absoluteSolutionPath) &&
            !SolutionFilterReader.TryRead(absoluteSolutionPath, pathResolver, out absoluteSolutionPath, out projectFilter))
        {
            throw new Exception(string.Format(WorkspaceMSBuildResources.Failed_to_load_solution_filter_0, solutionFilePath));
        }

        if (!TryReadSolutionFile(absoluteSolutionPath, pathResolver, projectFilter, out var projects))
        {
            throw new Exception(string.Format(WorkspaceMSBuildResources.Failed_to_load_solution_0, absoluteSolutionPath));
        }

        return (absoluteSolutionPath, projects);
    }

    private static bool TryReadSolutionFile(string solutionFilePath, PathResolver pathResolver, ImmutableHashSet<string> projectFilter, out ImmutableArray<(string ProjectPath, string ProjectGuid)> projects)
    {
        // Get the serializer for the solution file
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath);
        if (serializer == null)
        {
            projects = [];
            return false;
        }

        // The base directory for projects is the solution folder.
        var baseDirectory = Path.GetDirectoryName(solutionFilePath);
        RoslynDebug.AssertNotNull(baseDirectory);
        var solutionModel = serializer.OpenAsync(solutionFilePath, CancellationToken.None).Result;

        var builder = ImmutableArray.CreateBuilder<(string ProjectPath, string ProjectGuid)>();
        foreach (var projectModel in solutionModel.SolutionProjects)
        {
            // If we are filtering based on a solution filter then we need to verify the project is included.
            if (!projectFilter.IsEmpty)
            {
                if (!pathResolver.TryGetAbsoluteProjectPath(projectModel.FilePath, baseDirectory, DiagnosticReportingMode.Throw, out var absoluteProjectPath)
                    || !projectFilter.Contains(absoluteProjectPath))
                {
                    continue;
                }
            }

            builder.Add((projectModel.FilePath, projectModel.Id.ToString()));
        }

        projects = builder.ToImmutable();
        return true;
    }
}
