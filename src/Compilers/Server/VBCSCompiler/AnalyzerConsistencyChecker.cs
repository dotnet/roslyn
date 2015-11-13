// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class AnalyzerConsistencyChecker
    {
        private static readonly ImmutableArray<string> s_defaultIgnorableReferenceNames = ImmutableArray.Create("mscorlib", "System", "Microsoft.CodeAnalysis");

        public static bool Check(string baseDirectory, IEnumerable<CommandLineAnalyzerReference> analyzerReferences, IAnalyzerAssemblyLoader loader, IEnumerable<string> ignorableReferenceNames = null)
        {
            if (ignorableReferenceNames == null)
            {
                ignorableReferenceNames = s_defaultIgnorableReferenceNames;
            }

            try
            {
                CompilerServerLogger.Log("Begin Analyzer Consistency Check");
                return CheckCore(baseDirectory, analyzerReferences, loader, ignorableReferenceNames);
            }
            catch (Exception e)
            {
                CompilerServerLogger.LogException(e, "Analyzer Consistency Check");
                return false;
            }
            finally
            {
                CompilerServerLogger.Log("End Analyzer Consistency Check");
            }
        }

        private static bool CheckCore(string baseDirectory, IEnumerable<CommandLineAnalyzerReference> analyzerReferences, IAnalyzerAssemblyLoader loader, IEnumerable<string> ignorableReferenceNames)
        {
            var resolvedPaths = new List<string>();

            foreach (var analyzerReference in analyzerReferences)
            {
                string resolvedPath = FileUtilities.ResolveRelativePath(analyzerReference.FilePath, basePath: null, baseDirectory: baseDirectory, searchPaths: SpecializedCollections.EmptyEnumerable<string>(), fileExists: File.Exists);
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

            // First, check that the set of references is complete, modulo items in the safe list.
            foreach (var resolvedPath in resolvedPaths)
            {
                var missingDependencies = AssemblyUtilities.IdentifyMissingDependencies(resolvedPath, resolvedPaths);

                foreach (var missingDependency in missingDependencies)
                {
                    if (!ignorableReferenceNames.Any(name => missingDependency.Name.StartsWith(name)))
                    {
                        CompilerServerLogger.Log($"Analyzer assembly {resolvedPath} depends on '{missingDependency}' but it was not found.");
                        return false;
                    }
                }
            }

            // Second, load all of the assemblies upfront.
            foreach (var resolvedPath in resolvedPaths)
            {
                loader.AddDependencyLocation(resolvedPath);
            }

            var loadedAssemblies = new List<Assembly>();
            foreach (var resolvedPath in resolvedPaths)
            {
                loadedAssemblies.Add(loader.LoadFromPath(resolvedPath));
            }

            // Third, check that the MVIDs of the files on disk match the MVIDs of the loaded assemblies.
            for (int i = 0; i < resolvedPaths.Count; i++)
            {
                var resolvedPath = resolvedPaths[i];
                var loadedAssembly = loadedAssemblies[i];
                var resolvedPathMvid = AssemblyUtilities.ReadMvid(resolvedPath);
                var loadedAssemblyMvid = loadedAssembly.ManifestModule.ModuleVersionId;

                if (resolvedPathMvid != loadedAssemblyMvid)
                {
                    CompilerServerLogger.Log($"Analyzer assembly {resolvedPath} has MVID '{resolvedPathMvid}' but loaded assembly '{loadedAssembly.FullName}' has MVID '{loadedAssemblyMvid}'.");
                    return false;
                }
            }

            return true;
        }
    }
}
