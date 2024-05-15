// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
#if NETCOREAPP
using Roslyn.Test.Utilities.CoreClr;
using System.Runtime.Loader;
#else
using Roslyn.Test.Utilities.Desktop;
#endif

namespace Microsoft.CodeAnalysis.UnitTests
{

#if NETCOREAPP

    public sealed class InvokeUtil
    {
        internal void Exec(ITestOutputHelper testOutputHelper, AssemblyLoadContext compilerContext, AssemblyLoadTestFixture fixture, AnalyzerTestKind kind, string typeName, string methodName, IAnalyzerAssemblyResolver[] externalResolvers)
        {
            // Ensure that the test did not load any of the test fixture assemblies into 
            // the default load context. That should never happen. Assemblies should either 
            // load into the compiler or directory load context.
            //
            // Not only is this bad behavior it also pollutes future test results.
            var defaultContextCount = AssemblyLoadContext.Default.Assemblies.Count();
            var compilerContextCount = compilerContext.Assemblies.Count();

            using var tempRoot = new TempRoot();
            AnalyzerAssemblyLoader loader = kind switch
            {
                AnalyzerTestKind.LoadDirect => new DefaultAnalyzerAssemblyLoader(compilerContext, AnalyzerLoadOption.LoadFromDisk, externalResolvers.ToImmutableArray()),
                AnalyzerTestKind.LoadStream => new DefaultAnalyzerAssemblyLoader(compilerContext, AnalyzerLoadOption.LoadFromStream, externalResolvers.ToImmutableArray()),
                AnalyzerTestKind.ShadowLoad => new ShadowCopyAnalyzerAssemblyLoader(compilerContext, tempRoot.CreateDirectory().Path, externalResolvers.ToImmutableArray()),
                _ => throw ExceptionUtilities.Unreachable()
            };

            try
            {
                AnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName);
            }
            finally
            {
                loader.UnloadAll();
                testOutputHelper.WriteLine($"Test fixture root: {fixture.TempDirectory}");

                foreach (var context in loader.GetDirectoryLoadContextsSnapshot())
                {
                    testOutputHelper.WriteLine($"Directory context: {context.Directory}");
                    foreach (var assembly in context.Assemblies)
                    {
                        testOutputHelper.WriteLine($"\t{assembly.FullName}");
                    }
                }

                if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
                {
                    testOutputHelper.WriteLine($"Shadow loader: {shadowLoader.BaseDirectory}");
                }

                testOutputHelper.WriteLine($"Loader path maps");
                foreach (var pair in loader.GetPathMapSnapshot())
                {
                    testOutputHelper.WriteLine($"\t{pair.OriginalAssemblyPath} -> {pair.RealAssemblyPath}");
                }

                Assert.Equal(defaultContextCount, AssemblyLoadContext.Default.Assemblies.Count());
                Assert.Equal(compilerContextCount, compilerContext.Assemblies.Count());
            }
        }
    }

#else

    public sealed class InvokeUtil : MarshalByRefObject
    {
        internal void Exec(ITestOutputHelper testOutputHelper, AssemblyLoadTestFixture fixture, AnalyzerTestKind kind, string typeName, string methodName, IAnalyzerAssemblyResolver[] externalResolvers)
        {
            using var tempRoot = new TempRoot();
            AnalyzerAssemblyLoader loader = kind switch
            {
                AnalyzerTestKind.LoadDirect => new DefaultAnalyzerAssemblyLoader(externalResolvers.ToImmutableArray()),
                AnalyzerTestKind.ShadowLoad => new ShadowCopyAnalyzerAssemblyLoader(tempRoot.CreateDirectory().Path, externalResolvers.ToImmutableArray()),
                _ => throw ExceptionUtilities.Unreachable()
            };

            try
            {
                AnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName);
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

                if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
                {
                    testOutputHelper.WriteLine($"Shadow loader: {shadowLoader.BaseDirectory}");
                }

                testOutputHelper.WriteLine($"Loader path maps");
                foreach (var pair in loader.GetPathMapSnapshot())
                {
                    testOutputHelper.WriteLine($"\t{pair.OriginalAssemblyPath} -> {pair.RealAssemblyPath}");
                }
            }
        }
    }

#endif

}
