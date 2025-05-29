// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal partial class SolutionFileReader
{
    public static Task<(string AbsoluteSolutionPath, ImmutableArray<(string ProjectPath, string ProjectGuid)> Projects)> ReadSolutionFileAsync(string solutionFilePath, CancellationToken cancellationToken)
    {
        return ReadSolutionFileAsync(solutionFilePath, new PathResolver(diagnosticReporter: null), cancellationToken);
    }

    public static async Task<(string AbsoluteSolutionPath, ImmutableArray<(string ProjectPath, string ProjectGuid)> Projects)> ReadSolutionFileAsync(string solutionFilePath, PathResolver pathResolver, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(pathResolver.TryGetAbsoluteSolutionPath(solutionFilePath, baseDirectory: Directory.GetCurrentDirectory(), DiagnosticReportingMode.Throw, out var absoluteSolutionPath));

        // When passed a solution filter, we need to read the filter file to get the solution path and included project paths.
        var projectFilter = ImmutableHashSet<string>.Empty;
        if (SolutionFilterReader.IsSolutionFilterFilename(absoluteSolutionPath) &&
            !SolutionFilterReader.TryRead(absoluteSolutionPath, pathResolver, out absoluteSolutionPath, out projectFilter))
        {
            throw new Exception(string.Format(WorkspaceMSBuildResources.Failed_to_load_solution_filter_0, solutionFilePath));
        }

        var projects = await TryReadSolutionFileAsync(absoluteSolutionPath, pathResolver, projectFilter, cancellationToken).ConfigureAwait(false);
        if (!projects.HasValue)
        {
            throw new Exception(string.Format(WorkspaceMSBuildResources.Failed_to_load_solution_0, absoluteSolutionPath));
        }

        return (absoluteSolutionPath, projects.Value);
    }

    private static async Task<ImmutableArray<(string ProjectPath, string ProjectGuid)>?> TryReadSolutionFileAsync(string solutionFilePath, PathResolver pathResolver, ImmutableHashSet<string> projectFilter, CancellationToken cancellationToken)
    {
        var serializer = SolutionSerializers.GetSerializerByMoniker(solutionFilePath);
        if (serializer == null)
        {
            return null;
        }

        // The solution folder is the base directory for project paths.
        var baseDirectory = Path.GetDirectoryName(solutionFilePath);
        RoslynDebug.AssertNotNull(baseDirectory);

        var solutionModel = await serializer.OpenAsync(solutionFilePath, cancellationToken).ConfigureAwait(false);

        var builder = ImmutableArray.CreateBuilder<(string ProjectPath, string ProjectGuid)>();
        foreach (var projectModel in solutionModel.SolutionProjects)
        {
            // If we are filtering based on a solution filter, then we need to verify the project is included.
            if (!projectFilter.IsEmpty)
            {
                Contract.ThrowIfFalse(pathResolver.TryGetAbsoluteProjectPath(projectModel.FilePath, baseDirectory, DiagnosticReportingMode.Throw, out var absoluteProjectPath));
                if (!projectFilter.Contains(absoluteProjectPath))
                {
                    continue;
                }
            }

            builder.Add((projectModel.FilePath, projectModel.Id.ToString()));
        }

        return builder.ToImmutable();
    }
}
