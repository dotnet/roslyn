// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;

namespace MSBuildToolset
{
    public sealed class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: RoslynRestore <solutionFile> <nugetExePath> <dotnetPath>");
                return -1;
            }

            var nugetExePath = Path.GetFullPath(args[1]);
            var dotnetPath = Path.GetFullPath(args[2]);

            var solutionFile = SolutionFile.Parse(args[0]);
            var projectPaths = solutionFile.ProjectsInOrder
                .Select(p => p.AbsolutePath)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !p.EndsWith("shproj") && p.EndsWith("proj"))
                .ToArray();

            var nugetExeDir = Path.GetDirectoryName(nugetExePath);
            string msbuildPath = Path.Combine(AppContext.BaseDirectory, "MSBuild.exe");
            string entryPointTargetPath = Path.Combine(nugetExeDir,
                "build/Targets/GetProjectsReferencingProjectJsonFilesEntryPoint.targets");
            string afterBuildsTargetPath = Path.Combine(nugetExeDir,
                "build/Targets/GetProjectsReferencingProjectJsonFiles.targets");
            string resultsFilePath = Path.ChangeExtension(Path.GetTempFileName(), "dg");

            // Build up a command line to run a specific MSBuild target
            // (NuGet_GetProjectsReferencingProjectJson) against every project
            // in the solution. This target spits out all the P2P references of
            // the project, eventually combined into a .dg with a graph of all the
            // P2P references in the solution. This file is eventually passed to
            // dotnet restore, where it is used by NuGet to resolve packages across
            // P2P references.
            var argumentBuilder = new StringBuilder(
                $"\"{msbuildPath}\" " +
                "-t:NuGet_GetProjectsReferencingProjectJson " +
                "-nologo -v:q " +
                "-p:BuildProjectReferences=false " +
                $"-p:NuGetTasksAssemblyPath=\"{nugetExePath}\" " +
                $"-p:NuGetCustomAfterBuildTargetPath=\"{afterBuildsTargetPath}\" " +
                $"-p:ResultsFile=\"{resultsFilePath}\" " +
                $"-p:NuGet_ProjectReferenceToResolve=\"\\\"");

            foreach (var projectPath in projectPaths)
            {
                argumentBuilder.Append(projectPath).Append(";");
            }

            argumentBuilder.Append("\\\"\" ");
            argumentBuilder.Append($"\"{entryPointTargetPath}\"");

            var arguments = argumentBuilder.ToString();

            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = Path.Combine(AppContext.BaseDirectory, "corerun"),
                Arguments = arguments
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine("MSBuild dg file generation failed");
                    return -1;
                }
            }

            // Run dotnet restore with the aforementioned .dg P2P file
            arguments = $"restore --disable-parallel -v Warning {resultsFilePath}";

            psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = dotnetPath,
                Arguments = arguments
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Console.Error.WriteLine("Restore failed");
                    return -1;
                }
            }

            try
            {
                File.Delete(resultsFilePath);
            }
            catch
            {
                // Ignore failure
            }

            return 0;
        }
    }
}
