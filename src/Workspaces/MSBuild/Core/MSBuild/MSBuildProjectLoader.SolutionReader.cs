// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using Roslyn.Utilities;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;
using System.Threading;
using Microsoft.Build.Evaluation;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private static class SolutionReader
        {
            public static bool IsSlnFilename(string filename)
            {
                ISolutionSerializer ? serializer = SolutionSerializers.GetSerializerByMoniker(filename);
                return serializer == null ? false : true;
            }
            //Rename the filter files and user proper namming convension
            public static bool TryRead(string filename, PathResolver pathResolver, ImmutableHashSet<string> projectFilter, out ImmutableArray<string> projectPaths)
            {
                try
                {
                    // Get the serializer for the solution file
                    ISolutionSerializer? serializer = SolutionSerializers.GetSerializerByMoniker(filename);
                    SolutionModel solutionModel;
                    var projects = ImmutableArray.CreateBuilder<string>();

                    if (serializer != null)
                    {
                        // The base directory for projects is the solution folder.
                        var baseDirectory = Path.GetDirectoryName(filename)!;
                        RoslynDebug.AssertNotNull(baseDirectory);

                        solutionModel = serializer.OpenAsync(filename, CancellationToken.None).Result;
                        foreach (SolutionProjectModel projectModel in solutionModel.SolutionProjects)
                        {
                            // Load project if we have an empty project filter and the project path is present.
          
                            if (projectFilter.IsEmpty ||
                                (pathResolver.TryGetAbsoluteProjectPath(projectModel.FilePath, baseDirectory, DiagnosticReportingMode.Throw, out var absoluteProjectPath) && projectFilter.Contains(absoluteProjectPath)))
                            {                              
                                projects.Add(projectModel.FilePath);
                            }                        
                        }
                        
                    }
                    projectPaths = projects.ToImmutable();
                    return true;
             
                }
                catch
                {

                    projectPaths = [];
                    return false;
                }
            }
        }
    }
}
