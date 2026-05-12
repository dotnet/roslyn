// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.CodeAnalysis.VisualBasic;
#if NET
using Roslyn.Test.Utilities.CoreClr;
using System.Runtime.Loader;
#else
using Roslyn.Test.Utilities.Desktop;
#endif

namespace Microsoft.CodeAnalysis.UnitTests
{

#if NET

    public sealed class InvokeUtil
    {
        internal void Exec(
            ITestOutputHelper testOutputHelper,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers,
            ImmutableArray<IAnalyzerAssemblyResolver> assemblyResolvers,
            AssemblyLoadTestFixture fixture,
            AnalyzerTestKind kind,
            string typeName,
            string methodName,
            object? state = null)
        {
            using var tempRoot = new TempRoot();
            switch (kind)
            {
                case AnalyzerTestKind.LoadDirect:
                    assemblyResolvers = [.. assemblyResolvers, AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver];
                    break;
                case AnalyzerTestKind.LoadStream:
                    assemblyResolvers = [.. assemblyResolvers, AnalyzerAssemblyLoader.StreamAnalyzerAssemblyResolver];
                    break;
                case AnalyzerTestKind.ShadowLoad:
                    pathResolvers = [.. pathResolvers, new ShadowCopyAnalyzerPathResolver(tempRoot.CreateDirectory().Path)];
                    assemblyResolvers = [.. assemblyResolvers, AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver];
                    break;
                default:
                    throw ExceptionUtilities.Unreachable();
            }

            var loader = new AnalyzerAssemblyLoader(pathResolvers, assemblyResolvers, compilerLoadContext: null);

            var compilerContextAssemblies = new HashSet<string>(loader.CompilerLoadContext.Assemblies.Select(a => a.FullName!));
            try
            {
                Exec(testOutputHelper, fixture, loader, typeName, methodName, state);
            }
            finally
            {
                // Verify that the test did not load any unexpected assemblies into the compiler load
                // context. Assemblies that are next to the unit test DLL are fine as those are normal
                // lazily loaded test infrastructure or runtime assemblies. What we want to catch is
                // test fixture assemblies (compiled on the fly) being incorrectly loaded here.
                VerifyNoNewNonLocalAssemblies(
                    testOutputHelper,
                    compilerContextAssemblies,
                    loader.CompilerLoadContext.Assemblies,
                    "CompilerLoadContext");
            }
        }

        internal void Exec(
            ITestOutputHelper testOutputHelper,
            AssemblyLoadTestFixture fixture,
            AnalyzerAssemblyLoader loader,
            string typeName,
            string methodName,
            object? state = null)
        {
            // Ensure that the test did not load any of the test fixture assemblies into 
            // the default load context. That should never happen. Assemblies should either 
            // load into the compiler or directory load context.
            //
            // Not only is this bad behavior it also pollutes future test results.
            var defaultContextAssemblies = new HashSet<string>(AssemblyLoadContext.Default.Assemblies.Select(a => a.FullName!));
            using var tempRoot = new TempRoot();

            try
            {
                AnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName, state);
            }
            finally
            {
                testOutputHelper.WriteLine($"Test fixture root: {fixture.TempDirectory}");

                foreach (var context in loader.GetDirectoryLoadContextsSnapshot())
                {
                    testOutputHelper.WriteLine($"Directory context: {context.Directory}");
                    foreach (var assembly in context.Assemblies)
                    {
                        testOutputHelper.WriteLine($"\t{assembly.FullName}");
                    }
                }

                if (loader.AnalyzerPathResolvers.OfType<ShadowCopyAnalyzerPathResolver>().FirstOrDefault() is { } shadowResolver)
                {
                    testOutputHelper.WriteLine($"{nameof(ShadowCopyAnalyzerPathResolver)}: {shadowResolver.BaseDirectory}");
                }

                testOutputHelper.WriteLine($"Loader path maps");
                foreach (var pair in loader.GetPathMapSnapshot())
                {
                    testOutputHelper.WriteLine($"\t{pair.OriginalAssemblyPath} -> {pair.ResolvedAssemblyPath}");
                }

                // Verify that the test did not load any unexpected assemblies into the default load
                // context. Assemblies next to the unit test DLL are fine (normal lazy loads from test
                // infrastructure or the runtime). What we want to catch is test fixture assemblies
                // being incorrectly loaded into the default context.
                VerifyNoNewNonLocalAssemblies(
                    testOutputHelper,
                    defaultContextAssemblies,
                    AssemblyLoadContext.Default.Assemblies,
                    "AssemblyLoadContext.Default");
            }
        }

        /// <summary>
        /// Verifies that any new assemblies loaded into the given context since the snapshot was taken
        /// are located next to the unit test DLL. Assemblies next to the test DLL are expected to be
        /// lazily loaded by the test infrastructure or the runtime itself. Assemblies from other locations
        /// (like the test fixture temp directory) would indicate incorrect loading behavior.
        /// </summary>
        private static void VerifyNoNewNonLocalAssemblies(
            ITestOutputHelper testOutputHelper,
            HashSet<string> snapshotAssemblyNames,
            IEnumerable<Assembly> currentAssemblies,
            string contextName)
        {
            var testDllDirectory = Path.GetDirectoryName(typeof(InvokeUtil).Assembly.Location)!;
            var unexpectedAssemblies = new List<string>();

            foreach (var assembly in currentAssemblies)
            {
                if (snapshotAssemblyNames.Contains(assembly.FullName!))
                {
                    continue;
                }

                if (assembly.IsDynamic)
                {
                    continue;
                }

                var location = assembly.Location;

                // Only truly dynamic assemblies are exempt from location-based checks.
                // Stream-loaded assemblies can also have no location and should still be flagged.
                if (string.IsNullOrEmpty(location))
                {
                    unexpectedAssemblies.Add($"{assembly.FullName} at <no location>");
                    continue;
                }

                // Assemblies next to the unit test DLL are expected lazy loads
                var assemblyDirectory = Path.GetDirectoryName(location)!;
                if (PathUtilities.Comparer.Equals(assemblyDirectory, testDllDirectory))
                {
                    continue;
                }

                unexpectedAssemblies.Add($"{assembly.FullName} at {location}");
            }

            if (unexpectedAssemblies.Count > 0)
            {
                testOutputHelper.WriteLine($"Unexpected assemblies loaded into {contextName}:");
                foreach (var item in unexpectedAssemblies)
                {
                    testOutputHelper.WriteLine($"\t{item}");
                }

                Assert.Fail($"Test loaded unexpected assemblies into {contextName}:{Environment.NewLine}{string.Join(Environment.NewLine, unexpectedAssemblies)}");
            }
        }
    }

#else

    public sealed class InvokeUtil : MarshalByRefObject
    {
        internal void Exec(
            ITestOutputHelper testOutputHelper,
            AssemblyLoadTestFixture fixture,
            AnalyzerTestKind kind,
            string typeName,
            string methodName,
            IAnalyzerPathResolver[] pathResolvers,
            object? state)
        {
            using var tempRoot = new TempRoot();
            pathResolvers = kind switch
            {
                AnalyzerTestKind.LoadDirect => pathResolvers,
                AnalyzerTestKind.ShadowLoad => [.. pathResolvers, new ShadowCopyAnalyzerPathResolver(tempRoot.CreateDirectory().Path)],
                _ => throw ExceptionUtilities.Unreachable(),
            };

            var loader = new AnalyzerAssemblyLoader(pathResolvers.ToImmutableArray());

            try
            {
                AnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName, state);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is XunitException)
            {
                var inner = ex.InnerException;
                throw new Exception(inner.Message + inner.StackTrace);
            }
            finally
            {
                testOutputHelper.WriteLine($"Test fixture root: {fixture.TempDirectory}");

                testOutputHelper.WriteLine($"Loaded Assemblies");
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderByDescending(x => x.FullName))
                {
                    testOutputHelper.WriteLine($"\t{assembly.FullName} -> {assembly.Location}");
                }

                if (loader.AnalyzerPathResolvers.OfType<ShadowCopyAnalyzerPathResolver>().FirstOrDefault() is { } shadowResolver)
                {
                    testOutputHelper.WriteLine($"{nameof(ShadowCopyAnalyzerPathResolver)}: {shadowResolver.BaseDirectory}");
                }

                testOutputHelper.WriteLine($"Loader path maps");
                foreach (var pair in loader.GetPathMapSnapshot())
                {
                    testOutputHelper.WriteLine($"\t{pair.OriginalAssemblyPath} -> {pair.ResolvedAssemblyPath}");
                }
            }
        }
    }

#endif

}
