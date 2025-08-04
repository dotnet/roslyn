// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    /// <summary>
    /// The compiler server is a long lived process and loads analyzers from a series of build 
    /// commands. This type is responsible for ensuring that analyzers loaded into the server 
    /// match, as closely as possible, what would be loaded from a single invocation of csc / vbc.
    ///
    /// There are a few type of events that can lead to incorrect <see cref="Assembly"/> loads:
    ///
    ///  1. <see cref="AppDomain"/> pollution: On .NET Framework all analyzers are loaded into the same 
    ///     <see cref="AppDomain"/> instance. When analyzers have dependencies at different versions 
    ///     that can lead to them binding to different dependencies than they would through a single 
    ///     invocation of csc.
    ///  2. File system changes: The implementations of <see cref="IAnalyzerAssemblyLoader"/> assume 
    ///     that the file system is unchanged during build. If the file system does change (say if 
    ///     an analyzer is rebuilt) then the new instances need to be loaded not the previous ones.
    ///
    /// When these type of events happen the consistency checker should fail.
    /// </summary>
    /// <remarks>
    /// The fact that <see cref="IAnalyzerAssemblyLoader"/> believe the file system is unchanging
    /// makes sense for environments like IDEs but makes little sense for a build server. A  possible
    /// future improvement is reworking the server implementation to be resilient to file system
    /// changing events. At least on .NET Core that could lead to less server restarts.
    /// </remarks>
    internal static class AnalyzerConsistencyChecker
    {
        public static bool Check(
            string baseDirectory,
            IEnumerable<CommandLineAnalyzerReference> analyzerReferences,
            IAnalyzerAssemblyLoaderInternal loader,
            ICompilerServerLogger logger) => Check(baseDirectory, analyzerReferences, loader, logger, out var _);

        public static bool Check(
            string baseDirectory,
            IEnumerable<CommandLineAnalyzerReference> analyzerReferences,
            IAnalyzerAssemblyLoaderInternal loader,
            ICompilerServerLogger logger,
            [NotNullWhen(false)] out List<string>? errorMessages)
        {
            errorMessages = null;
            try
            {
                logger.Log($"Begin Analyzer Consistency Check for {baseDirectory}");
                return CheckCore(baseDirectory, analyzerReferences, loader, logger, out errorMessages);
            }
            catch (Exception e)
            {
                logger.LogException(e, "Analyzer Consistency Check");
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
            IAnalyzerAssemblyLoaderInternal loader,
            ICompilerServerLogger logger,
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

            for (int i = 0; i < resolvedPaths.Count; i++)
            {
                var resolvedPath = resolvedPaths[i];
                var loadedAssembly = loadedAssemblies[i];

                // Do not perform consistency checks on assemblies that are owned by the host. These
                // always loaded from paths and at versions controlled by the compiler host. It's 
                // expected that the version the compilation specifies may get overriden.
                if (loader.IsHostAssembly(loadedAssembly))
                {
                    continue;
                }

                var resolvedPathMvid = AssemblyUtilities.ReadMvid(resolvedPath);
                var loadedAssemblyMvid = loadedAssembly.ManifestModule.ModuleVersionId;
                if (resolvedPathMvid != loadedAssemblyMvid)
                {
                    var loadedAssemblyLocation = loader.GetOriginalDependencyLocation(loadedAssembly.GetName()) ?? loadedAssembly.Location;
                    var message = $"analyzer assembly '{resolvedPath}' has MVID '{resolvedPathMvid}' but loaded assembly '{loadedAssemblyLocation}' has MVID '{loadedAssemblyMvid}'";
                    errorMessages ??= new List<string>();
                    errorMessages.Add(message);
                    logger.LogError(message);
                }
            }

            return errorMessages == null;
        }
    }
}
