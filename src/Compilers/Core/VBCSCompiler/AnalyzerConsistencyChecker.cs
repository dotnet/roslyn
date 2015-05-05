using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class AnalyzerConsistencyChecker
    {
        public static bool Check(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzerReferences, ImmutableArray<string> referenceWhiteList, IAnalyzerAssemblyLoader loader)
        {
            try
            {
                CompilerServerLogger.Log("Begin Analyzer Consistency Check");
                return CheckCore(baseDirectory, analyzerReferences, referenceWhiteList, loader);
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

        private static bool CheckCore(string baseDirectory, ImmutableArray<CommandLineAnalyzerReference> analyzerReferences, ImmutableArray<string> referenceWhiteList, IAnalyzerAssemblyLoader loader)
        {
            var resolvedPaths = new List<string>();

            foreach (var analyzerReference in analyzerReferences)
            {
                string resolvedPath = FileUtilities.ResolveRelativePath(analyzerReference.FilePath, basePath: null, baseDirectory: baseDirectory, searchPaths: SpecializedCollections.EmptyEnumerable<string>(), fileExists: File.Exists);
                if (File.Exists(resolvedPath))
                {
                    resolvedPath = FileUtilities.TryNormalizeAbsolutePath(resolvedPath);
                    if (resolvedPath != null)
                    {
                        resolvedPaths.Add(resolvedPath);
                    }
                }

                // Don't worry about paths we can't resolve. The compiler will report an error for that later.
            }

            // First, check that the set of references is complete, modulo items in the whitelist.
            foreach (var resolvedPath in resolvedPaths)
            {
                var missingDependencies = AssemblyUtilities.IdentifyMissingDependencies(resolvedPath, resolvedPaths);

                foreach (var missingDependency in missingDependencies)
                {
                    if (!referenceWhiteList.Any(name => missingDependency.Name.StartsWith(name)))
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
