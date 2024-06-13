// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class AnalyzerConsistencyChecker
    {
        public static bool Check(
            string baseDirectory,
            IEnumerable<CommandLineAnalyzerReference> analyzerReferences,
            IAnalyzerAssemblyLoader loader,
            ICompilerServerLogger? logger = null) => Check(baseDirectory, analyzerReferences, loader, logger, out var _);

        public static bool Check(
            string baseDirectory,
            IEnumerable<CommandLineAnalyzerReference> analyzerReferences,
            IAnalyzerAssemblyLoader loader,
            ICompilerServerLogger? logger,
            [NotNullWhen(false)]
            out List<string>? errorMessages)
        {
            errorMessages = null;
            try
            {
                logger?.Log($"Begin Analyzer Consistency Check for {baseDirectory}");
                return CheckCore(baseDirectory, analyzerReferences, loader, logger, out errorMessages);
            }
            catch (Exception e)
            {
                logger?.LogException(e, "Analyzer Consistency Check");
                errorMessages ??= new List<string>();
                errorMessages.Add(e.Message);
                return false;
            }
            finally
            {
                logger?.Log("End Analyzer Consistency Check");
            }
        }

        private static bool CheckCore(
            string baseDirectory,
            IEnumerable<CommandLineAnalyzerReference> analyzerReferences,
            IAnalyzerAssemblyLoader loader,
            ICompilerServerLogger? logger,
            [NotNullWhen(false)] out List<string>? errorMessages)
        {
            errorMessages = null;
            var resolvedPaths = new List<string>();

            foreach (var analyzerReference in analyzerReferences)
            {
                string? resolvedPath = FileUtilities.ResolveRelativePath(analyzerReference.FilePath, basePath: null, baseDirectory: baseDirectory, searchPaths: SpecializedCollections.EmptyEnumerable<string>(), fileExists: File.Exists);
                if (resolvedPath != null)
                {
                    resolvedPath = FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
                    if (resolvedPath != null)
                    {
                        resolvedPaths.Add(resolvedPath);
                    }
                }

                // Don't worry about paths we can't resolve. The compiler will report an error for that later.
            }

            // Register analyzers and their dependencies upfront, 
            // so that assembly references can be resolved:
            foreach (var resolvedPath in resolvedPaths)
            {
                loader.AddDependencyLocation(resolvedPath);
            }

            // Load all analyzer assemblies:
            var loadedAssemblies = new List<Assembly>();
            foreach (var resolvedPath in resolvedPaths)
            {
                loadedAssemblies.Add(loader.LoadFromPath(resolvedPath));
            }

            // Third, check that the MVIDs of the files on disk match the MVIDs of the loaded assemblies.
            var comparer = PathUtilities.Comparer;
            var compilerDirectory = Path.GetDirectoryName(typeof(AnalyzerConsistencyChecker).Assembly.CodeBase);

            for (int i = 0; i < resolvedPaths.Count; i++)
            {
                var resolvedPath = resolvedPaths[i];
                var loadedAssembly = loadedAssemblies[i];

                // When an assembly is loaded from the GAC then the load result would be the same if 
                // this ran on command line compiler. So there is no consistency issue here, this 
                // is just runtime rules expressing themselves.
                if (loadedAssembly.GlobalAssemblyCache)
                {
                    continue;
                }

                // When an assembly is loaded from the compiler directory then this means it's assembly
                // binding redirects taking over. For example it's moving from an older version of System.Memory
                // to the one shipping in the compiler. This is not a consistency issue.
                if (PathUtilities.Comparer.Equals(compilerDirectory, Path.GetDirectoryName(loadedAssembly.CodeBase)))
                {
                    continue;
                }

                var resolvedPathMvid = AssemblyUtilities.ReadMvid(resolvedPath);
                var loadedAssemblyMvid = loadedAssembly.ManifestModule.ModuleVersionId;
                if (resolvedPathMvid != loadedAssemblyMvid)
                {
                    var message = $"analyzer assembly '{resolvedPath}' has MVID '{resolvedPathMvid}' but loaded assembly '{loadedAssembly.Location}' has MVID '{loadedAssemblyMvid}'";
                    errorMessages ??= new List<string>();
                    errorMessages.Add(message);
                    logger.LogError(message);
                }
            }

            return errorMessages == null;
        }
    }
}

#endif
