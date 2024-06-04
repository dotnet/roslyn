// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.VisualBasic;

#if NETCOREAPP
using Roslyn.Test.Utilities.CoreClr;
using System.Runtime.Loader;
#else
using Roslyn.Test.Utilities.Desktop;
#endif

namespace Microsoft.CodeAnalysis.UnitTests
{
    public enum AnalyzerTestKind
    {
        LoadDirect,
        ShadowLoad,
#if NETCOREAPP
        LoadStream,
#endif
    }

    /// <summary>
    /// Contains the bulk of our analyzer / generator loading tests.
    /// </summary>
    /// <remarks>
    /// These tests often have quirks associated with fundamental limitation issues around either 
    /// .NET Framework, .NET Core or our own legacy decisions. Rather than repeating a specific rationale
    /// at all the tests that hit them, the common are outlined below and referenced with the following 
    /// comment style within the test.
    ///
    ///    // See limitation 1
    ///
    /// This allows us to provide central description of the limitations that can be easily referenced in the impacted
    /// tests. For all the descriptions below assume that A.dll depends on B.dll. 
    ///
    /// Limitation 1: .NET Framework probing path.
    ///
    /// The .NET Framework assembly loader will only call AppDomain.AssemblyResolve when it cannot satisfy a load
    /// request. One of the places the assembly loader will always consider when looking for dependencies of A.dll
    /// is the directory that A.dll was loading from (it's added to the probing path). That means if B.dll is in the
    /// same directory then the runtime will silently load it without a way for us to intervene.
    ///
    /// Note: this only applies when A.dll is in the Load or LoadFrom context which is always true for these tests
    /// 
    /// Limitation 2: Dependency is already loaded.
    ///
    /// Similar to Limitation 1 is when the dependency, B.dll, is already present in the Load or LoadFrom context 
    /// then that will be used. The runtime will not attempt to load a better version (an exact match for example).
    ///
    /// Limitation 3: Shadow copy breaks up directories
    ///
    /// The shadow copy loader strategy is to put every analyzer dependency into a different shadow directory. That 
    /// means if A.dll and B.dll are in the same directory for a normal load, they are in different directories 
    /// during a shadow copy load.
    /// 
    /// This causes significant issues in .NET Framework because we don't have the ability to know where a load
    /// is coming from. The AppDomain.AssemblyResolve event just requests "B, Version=1.0.0.0" but gives no context 
    /// as to where the request is coming from. That means we often end up loading a different copy of B.dll in a
    /// shadow load scenario. 
    /// 
    /// Long term this is something that needs to be addressed. Tracked by https://github.com/dotnet/roslyn/issues/66532
    ///
    /// </remarks>
    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public sealed class AnalyzerAssemblyLoaderTests : TestBase
    {
        public ITestOutputHelper TestOutputHelper { get; }

        public AssemblyLoadTestFixture TestFixture { get; }

        public AnalyzerAssemblyLoaderTests(ITestOutputHelper testOutputHelper, AssemblyLoadTestFixture testFixture)
        {
            TestOutputHelper = testOutputHelper;
            TestFixture = testFixture;
        }

#if NETCOREAPP

        private void Run(AnalyzerTestKind kind, Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction, IAnalyzerAssemblyResolver[]? externalResolvers = null, [CallerMemberName] string? memberName = null) =>
            Run(
                kind,
                static (_, _) => { },
                testAction,
                externalResolvers,
                memberName);

        private void Run(
            AnalyzerTestKind kind,
            Action<AssemblyLoadContext, AssemblyLoadTestFixture> prepLoadContextAction,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction,
            IAnalyzerAssemblyResolver[]? externalResolvers = null,
            [CallerMemberName] string? memberName = null)
        {
            var alc = new AssemblyLoadContext($"Test {memberName}", isCollectible: true);
            try
            {
                prepLoadContextAction(alc, TestFixture);
                var util = new InvokeUtil();
                util.Exec(TestOutputHelper, alc, TestFixture, kind, testAction.Method.DeclaringType!.FullName!, testAction.Method.Name, externalResolvers ?? []);
            }
            finally
            {
                alc.Unload();
            }
        }

#else

        private void Run(
            AnalyzerTestKind kind,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction,
            IAnalyzerAssemblyResolver[]? externalResolvers = null,
            [CallerMemberName] string? memberName = null)
        {
            AppDomain? appDomain = null;
            try
            {
                appDomain = AppDomainUtils.Create($"Test {memberName}");
                var testOutputHelper = new AppDomainTestOutputHelper(TestOutputHelper);
                var type = typeof(InvokeUtil);
                var util = (InvokeUtil)appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
                util.Exec(testOutputHelper, TestFixture, kind, testAction.Method.DeclaringType.FullName, testAction.Method.Name, externalResolvers ?? []);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

#endif

        /// <summary>
        /// This is called from our newly created AppDomain or AssemblyLoadContext and needs to get 
        /// us back to the actual test code to execute. The intent is to invoke the lambda / static
        /// local func where the code exists.
        /// </summary>
        internal static void InvokeTestCode(AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture fixture, string typeName, string methodName)
        {
            var type = typeof(AnalyzerAssemblyLoaderTests).Assembly.GetType(typeName, throwOnError: false)!;
            var member = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;

            // A static lambda will still be an instance method so we need to create the closure
            // here.
            var obj = member.IsStatic
                ? null
                : type.Assembly.CreateInstance(typeName);

            member.Invoke(obj, new object[] { loader, fixture });
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var analyzerDependencyFile = testFixture.AnalyzerDependency;
                var analyzerMainFile = testFixture.AnalyzerWithDependency;
                loader.AddDependencyLocation(analyzerDependencyFile);

                var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile, loader);
                analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);
                var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile, loader);
                analyzerDependencyReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);

                var analyzers = analyzerMainReference.GetAnalyzersForAllLanguages();
                Assert.Equal(1, analyzers.Length);
                Assert.Equal("TestAnalyzer", analyzers[0].ToString());

                Assert.Equal(0, analyzerDependencyReference.GetAnalyzersForAllLanguages().Length);

                Assert.NotNull(analyzerDependencyReference.GetAssembly());
            });
        }

        [Theory]
        [CombinatorialData]
        public void AddDependencyLocationThrowsOnNull(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null!));
                Assert.Throws<ArgumentException>("fullPath", () => loader.AddDependencyLocation("a"));
            });
        }

        [Theory]
        [CombinatorialData]
        public void ThrowsForMissingFile(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
                Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
            });
        }

        [Theory]
        [CombinatorialData]
        public void BasicLoad(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Alpha);
                Assembly alpha = loader.LoadFromPath(testFixture.Alpha);

                Assert.NotNull(alpha);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_Multiple(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Alpha);
                loader.AddDependencyLocation(testFixture.Beta);
                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Delta1);

                Assembly alpha = loader.LoadFromPath(testFixture.Alpha);

                var a = alpha.CreateInstance("Alpha.A")!;
                a.GetType().GetMethod("Write")!.Invoke(a, new object[] { sb, "Test A" });

                Assembly beta = loader.LoadFromPath(testFixture.Beta);

                var b = beta.CreateInstance("Beta.B")!;
                b.GetType().GetMethod("Write")!.Invoke(b, new object[] { sb, "Test B" });

                var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

                var actual = sb.ToString();

                Assert.Equal(expected, actual);
            });
        }

        /// <summary>
        /// The loaders should not actually look at the contents of the disk until a <see cref="AnalyzerAssemblyLoader.LoadFromPath(string)"/>
        /// call has occurred. This is historical behavior that doesn't have a clear reason for existing. There
        /// is strong suspicion it's to delay loading of analyzers until absolutely necessary. As such we're
        /// enshrining the behavior here so it is not _accidentally_ changed.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_OverwriteBeforeLoad(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var delta1Copy = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                loader.AddDependencyLocation(delta1Copy);
                File.Copy(testFixture.Delta2, delta1Copy, overwrite: true);
                var assembly = loader.LoadFromPath(delta1Copy);

                var name = AssemblyName.GetAssemblyName(testFixture.Delta2);
                Assert.Equal(name.FullName, assembly.GetName().FullName);

                VerifyDependencyAssemblies(
                    loader,
                    delta1Copy);
            });
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/66621")]
        [CombinatorialData]
        public void AssemblyLoading_AssemblyLocationNotAdded(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Delta1);
                Assert.Throws<InvalidOperationException>(() => loader.LoadFromPath(testFixture.Beta));
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyLocationNotAdded(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Beta);
                Assembly beta = loader.LoadFromPath(testFixture.Beta);

                var b = beta.CreateInstance("Beta.B")!;
                var writeMethod = b.GetType().GetMethod("Write")!;

                if (ExecutionConditionUtil.IsCoreClr || loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // We don't pass Alpha's path to AddDependencyLocation here, and therefore expect
                    // calling Beta.B.Write to fail because loader will prevent the load of Alpha
                    var exception = Assert.Throws<TargetInvocationException>(
                        () => writeMethod.Invoke(b, new object[] { sb, "Test B" }));
                    Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);

                    var actual = sb.ToString();
                    Assert.Equal(@"", actual);
                }
                else
                {
                    // See limitation 1
                    writeMethod.Invoke(b, new object[] { sb, "Test B" });
                    var actual = sb.ToString();
                    Assert.Equal(@"Delta: Gamma: Beta: Test B
", actual);
                }
            });
        }

        private static void VerifyAssemblies(AnalyzerAssemblyLoader loader, IEnumerable<Assembly> assemblies, params (string simpleName, string version, string path)[] expected) =>
            VerifyAssemblies(loader, assemblies, expectedCopyCount: null, expected);

        private static void VerifyAssemblies(AnalyzerAssemblyLoader loader, IEnumerable<Assembly> assemblies, int? expectedCopyCount, params (string simpleName, string version, string path)[] expected)
        {
            Assert.Equal(
                expected
                    .Select(x => (x.simpleName, x.version, getExpectedLoadPath(x.path)))
                    .ToArray(),
                assemblies.Select(assembly => (assembly.GetName().Name!, assembly.GetName().Version!.ToString(), assembly.Location))
                    .OrderBy(static x => x)
                    .ToArray());

            if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
            {
                Assert.All(assemblies, x => x.Location.StartsWith(shadowLoader.BaseDirectory, StringComparison.Ordinal));
                Assert.Equal(expectedCopyCount ?? expected.Length, shadowLoader.CopyCount);
            }

            string getExpectedLoadPath(string path)
            {
#if NETCOREAPP
                if (loader is AnalyzerAssemblyLoader { AnalyzerLoadOption: AnalyzerLoadOption.LoadFromStream })
                {
                    return "";
                }
#endif

                if (path.EndsWith(".resources.dll", StringComparison.Ordinal))
                {
                    return getRealSatelliteLoadPath(path) ?? "";
                }
                return loader.GetRealAnalyzerLoadPath(path ?? "");
            }

            // When PreparePathToLoad is overridden this returns the most recent
            // real path for the given analyzer satellite assembly path
            string? getRealSatelliteLoadPath(string originalSatelliteFullPath)
            {
                // This is a satellite assembly, need to find the mapped path of the real assembly, then 
                // adjust that mapped path for the suffix of the satellite assembly
                //
                // Example of dll and it's corresponding satellite assembly
                //
                //  c:\some\path\en-GB\util.resources.dll
                //  c:\some\path\util.dll
                var assemblyFileName = Path.ChangeExtension(Path.GetFileNameWithoutExtension(originalSatelliteFullPath), ".dll");

                var assemblyDir = Path.GetDirectoryName(originalSatelliteFullPath)!;
                var cultureInfo = CultureInfo.GetCultureInfo(Path.GetFileName(assemblyDir));
                assemblyDir = Path.GetDirectoryName(assemblyDir)!;

                // Real assembly is located in the directory above this one
                var assemblyPath = Path.Combine(assemblyDir, assemblyFileName);
                return loader.GetRealSatelliteLoadPath(assemblyPath, cultureInfo);
            }
        }

        private static void VerifyAssemblies(AnalyzerAssemblyLoader loader, IEnumerable<Assembly> assemblies, int? copyCount, params string[] assemblyPaths)
        {
            var data = assemblyPaths
                .Select(x =>
                {
                    var name = AssemblyName.GetAssemblyName(x);
                    return (name.Name!, name.Version?.ToString() ?? "", x);
                })
                .ToArray();
            VerifyAssemblies(loader, assemblies, copyCount, data);
        }

        /// <summary>
        /// Verify the set of assemblies loaded as analyzer dependencies are the specified assembly paths
        /// </summary>
        private static void VerifyDependencyAssemblies(AnalyzerAssemblyLoader loader, params string[] assemblyPaths) =>
            VerifyDependencyAssemblies(loader, copyCount: null, assemblyPaths);

        private static void VerifyDependencyAssemblies(AnalyzerAssemblyLoader loader, int? copyCount, params string[] assemblyPaths)
        {
            IEnumerable<Assembly> loadedAssemblies;

#if NETCOREAPP
            // This verify only works where there is a single load context.
            var alcs = loader.GetDirectoryLoadContextsSnapshot();
            Assert.Equal(1, alcs.Length);

            loadedAssemblies = alcs[0].Assemblies;
#else

            // The assemblies in the LoadFrom context are the assemblies loaded from 
            // analyzer dependencies.
            loadedAssemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => isInLoadFromContext(loader, x));

            static bool isInLoadFromContext(AnalyzerAssemblyLoader loader, Assembly assembly)
            {
                var undidHook = false;
                try
                {
                    // Have to unhook resolve here otherwise calls to Load will hit the resolver and 
                    // we will end up bringing these assemblies into the Load context by handling them
                    // there.
                    undidHook = loader.EnsureResolvedUnhooked();
                    var name = assembly.FullName;
                    var other = AppDomain.CurrentDomain.Load(name);
                    return other is null || other != assembly;
                }
                catch
                {
                    return true;
                }
                finally
                {
                    if (undidHook)
                    {
                        loader.EnsureResolvedHooked();
                    }
                }
            }

#endif
            VerifyAssemblies(loader, loadedAssemblies, copyCount, assemblyPaths);
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_Simple(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Gamma);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma);
                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.Delta1,
                    testFixture.Gamma);
            });
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/runtime/issues/81108")]
        [CombinatorialData]
        public void AssemblyLoading_DependencyInDifferentDirectory(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                StringBuilder sb = new StringBuilder();

                var deltaFile = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var gammaFile = tempDir.CreateDirectory("b").CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma).Path;

                loader.AddDependencyLocation(deltaFile);
                loader.AddDependencyLocation(gammaFile);
                Assembly gamma = loader.LoadFromPath(gammaFile);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    deltaFile,
                    gammaFile);
            });
        }

#if NET472
        /// <summary>
        /// Verify that MS.CA.EA.RazorCompiler will be loaded from the compiler directory not the 
        /// analyzer directory.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_RazorCompiler1(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();

                var externalAccessRazorPath = typeof(Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.GeneratorExtensions).Assembly.Location;
                var alternatePath = tempDir.CreateDirectory("a").CreateFile("Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.dll").CopyContentFrom(externalAccessRazorPath).Path;

                loader.AddDependencyLocation(alternatePath);
                Assembly assembly = loader.LoadFromPath(alternatePath);

                Assert.Equal(externalAccessRazorPath, assembly.Location);

                // Even though EA.RazorCompiler is loaded from the compiler directory the shadow copy loader
                // still does a defensive copy.
                var copyCount = loader is ShadowCopyAnalyzerAssemblyLoader
                    ? 1
                    : (int?)null;

                VerifyDependencyAssemblies(
                    loader,
                    copyCount: copyCount,
                    []);
            });
        }

        /// <summary>
        /// Verify that MS.CA.EA.RazorCompiler will be loaded from the compiler directory not the 
        /// analyzer directory.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_RazorCompiler2(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();

                var externalAccessRazorPath = typeof(Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.GeneratorExtensions).Assembly.Location;
                var dir = tempDir.CreateDirectory("a");
                var alternatePath = dir.CreateFile("Microsoft.CodeAnalysis.ExternalAccess.RazorCompiler.dll").CopyContentFrom(externalAccessRazorPath).Path;
                var deltaFile = dir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;

                loader.AddDependencyLocation(alternatePath);
                loader.AddDependencyLocation(deltaFile);
                Assembly razorAssembly = loader.LoadFromPath(alternatePath);
                _ = loader.LoadFromPath(deltaFile);

                Assert.Equal(externalAccessRazorPath, razorAssembly.Location);

                // Even though EA.RazorCompiler is loaded from the compiler directory the shadow copy loader
                // still does a defensive copy.
                var copyCount = loader is ShadowCopyAnalyzerAssemblyLoader
                    ? 2
                    : (int?)null;
                VerifyDependencyAssemblies(
                    loader,
                    copyCount: copyCount,
                    deltaFile);
            });
        }

#endif

        /// <summary>
        /// Similar to <see cref="AssemblyLoading_DependencyInDifferentDirectory"/> except want to validate
        /// a dependency in the same directory is preferred over one in a different directory.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyInDifferentDirectory2(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();

                // It's important that we create these directories in a deterministic order so that 
                // our test has reliably output. Part of our resolution code will search the registered
                // paths in a sorted order.
                var deltaFile1 = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var tempSubDir = tempDir.CreateDirectory("b");
                var gammaFile = tempSubDir.CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma).Path;
                var deltaFile2 = tempSubDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;

                loader.AddDependencyLocation(deltaFile1);
                loader.AddDependencyLocation(deltaFile2);
                loader.AddDependencyLocation(gammaFile);
                Assembly gamma = loader.LoadFromPath(gammaFile);

                StringBuilder sb = new StringBuilder();
                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                if (ExecutionConditionUtil.IsDesktop && loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // See limitation 3
                    VerifyDependencyAssemblies(
                        loader,
                        deltaFile1,
                        gammaFile);
                }
                else
                {
                    VerifyDependencyAssemblies(
                        loader,
                        deltaFile2,
                        gammaFile);

                }
            });
        }

        /// <summary>
        /// This is very similar to <see cref="AssemblyLoading_DependencyInDifferentDirectory2"/> except 
        /// that we ensure the code does not prefer a dependency in the same directory if it's 
        /// unregistered
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyInDifferentDirectory3(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                StringBuilder sb = new StringBuilder();

                var deltaFile = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var gammaFile = tempDir.CreateDirectory("b").CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma).Path;

                loader.AddDependencyLocation(deltaFile);
                loader.AddDependencyLocation(gammaFile);
                Assembly gamma = loader.LoadFromPath(gammaFile);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    deltaFile,
                    gammaFile);
            });
        }

        [ConditionalTheory(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/66626")]
        [CombinatorialData]
        [WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void AssemblyLoading_DependencyInDifferentDirectory4(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var analyzerDependencyFile = testFixture.AnalyzerDependency;
                var analyzerMainFile = testFixture.AnalyzerWithDependency;

                var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile, loader);
                analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);
                var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile, loader);
                analyzerDependencyReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);

                Assert.True(loader.IsAnalyzerDependencyPath(analyzerMainFile));
                Assert.True(loader.IsAnalyzerDependencyPath(analyzerDependencyFile));

                var analyzers = analyzerMainReference.GetAnalyzersForAllLanguages();
                Assert.Equal(1, analyzers.Length);
                Assert.Equal("TestAnalyzer", analyzers[0].ToString());
                Assert.Equal(0, analyzerDependencyReference.GetAnalyzersForAllLanguages().Length);
                Assert.NotNull(analyzerDependencyReference.GetAssembly());

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.AnalyzerWithDependency,
                    testFixture.AnalyzerDependency);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Epsilon);
                loader.AddDependencyLocation(testFixture.Delta2);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });
                var actual = sb.ToString();

#if NETCOREAPP
                var alcs = loader.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(2, alcs.Length);

                VerifyAssemblies(
                    loader,
                    alcs[0].Assemblies,
                    expectedCopyCount: 4,
                    ("Delta", "1.0.0.0", testFixture.Delta1),
                    ("Gamma", "0.0.0.0", testFixture.Gamma)
                );

                VerifyAssemblies(
                    loader,
                    alcs[1].Assemblies,
                    expectedCopyCount: 4,
                    ("Delta", "2.0.0.0", testFixture.Delta2),
                    ("Epsilon", "0.0.0.0", testFixture.Epsilon));

                Assert.Equal(
    @"Delta: Gamma: Test G
Delta.2: Epsilon: Test E
",
                    actual);
#else
                Assert.Equal(
    @"Delta: Gamma: Test G
Delta: Epsilon: Test E
",
                    actual);

#endif
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_NoExactMatch(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Epsilon);
                loader.AddDependencyLocation(testFixture.Delta3);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr || loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // In .NET Core we have _full_ control over assembly loading and can prevent implicit
                    // loads from probing paths. That means we can avoid implicitly loading the Delta v2 
                    // next to Epsilon
                    //
                    // Similarly in the shadow copy scenarios the assemblies are not side by side so the 
                    // load is controllable.
                    //
                    // There is an extra copy count here as both deltas are read from disk in order to 
                    // get AssemblyName so the code can determine which is the best match. 
                    VerifyDependencyAssemblies(
                        loader,
                        copyCount: 3,
                        testFixture.Delta3,
                        testFixture.Epsilon);
                    Assert.Equal(
@"Delta.3: Epsilon: Test E
",
                    actual);
                }
                else
                {
                    // See limitation 1
                    // The Epsilon.dll has Delta.dll (v2) next to it in the directory. 
                    Assert.Throws<InvalidOperationException>(() => loader.GetRealAnalyzerLoadPath(testFixture.Delta2));

                    // Fake the dependency so we can verify the rest of the load
                    loader.AddDependencyLocation(testFixture.Delta2);
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2,
                        testFixture.Epsilon);

                    Assert.Equal(
    @"Delta.2: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MultipleEqualMatches(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B);
                loader.AddDependencyLocation(testFixture.Delta2);
                loader.AddDependencyLocation(testFixture.Epsilon);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                if (ExecutionConditionUtil.IsDesktop && loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // Delta2B and Delta2 have the same version, but we prefer Delta2B because it's added first and 
                    // in shadow loader we can't fall back to same directory because the runtime doesn't provide
                    // context for who requested the load. Just have to go to best version.
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2B,
                        testFixture.Epsilon);

                    var actual = sb.ToString();
                    Assert.Equal(
    @"Delta.2B: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // See limitation 1
                    // Delta2B and Delta2 have the same version, but we prefer Delta2 because it's in the same directory as Epsilon.
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2,
                        testFixture.Epsilon);

                    var actual = sb.ToString();
                    Assert.Equal(
    @"Delta.2: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MultipleVersionsOfSameAnalyzerItself(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2);
                loader.AddDependencyLocation(testFixture.Delta2B);

                Assembly delta2 = loader.LoadFromPath(testFixture.Delta2);
                Assembly delta2B = loader.LoadFromPath(testFixture.Delta2B);

                // 2B or not 2B? That is the question...that depends on whether we're on .NET Core or not.

#if NETCOREAPP

                // On Core, we're able to load both of these into separate AssemblyLoadContexts.
                if (loader.AnalyzerLoadOption == AnalyzerLoadOption.LoadFromDisk)
                {
                    Assert.NotEqual(delta2B.Location, delta2.Location);
                    Assert.Equal(loader.GetRealAnalyzerLoadPath(testFixture.Delta2), delta2.Location);
                    Assert.Equal(loader.GetRealAnalyzerLoadPath(testFixture.Delta2B), delta2B.Location);
                }

#else

                // See limitation 2
                // In non-core, we cache by assembly identity; since we don't use multiple AppDomains we have no
                // way to load different assemblies with the same identity, no matter what. Thus, we'll get the
                // same assembly for both of these.
                Assert.Same(delta2B, delta2);
#endif
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_ExactAndGreaterMatch(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B);
                loader.AddDependencyLocation(testFixture.Delta3);
                loader.AddDependencyLocation(testFixture.Epsilon);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr || loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // This works in CoreClr because we have full control over assembly loading. It 
                    // works in shadow copy because all the DLLs are put into different directories
                    // so everything is a AppDomain.AssemblyResolve event and we get full control there.
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2B,
                        testFixture.Epsilon);

                    Assert.Equal(
    @"Delta.2B: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // See limitation 2
                    Assert.Throws<InvalidOperationException>(() => loader.GetRealAnalyzerLoadPath(testFixture.Delta2));

                    // Fake the dependency so we can verify the rest of the load
                    loader.AddDependencyLocation(testFixture.Delta2);
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2,
                        testFixture.Epsilon);

                    Assert.Equal(
                        @"Delta.2: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_WorseMatchInSameDirectory(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var tempDir1 = tempDir.CreateDirectory("a");
                var tempDir2 = tempDir.CreateDirectory("b");
                var epsilonFile = tempDir1.CreateFile("Epsilon.dll").CopyContentFrom(testFixture.Epsilon).Path;
                var delta1File = tempDir1.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var delta2File = tempDir2.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta2).Path;

                loader.AddDependencyLocation(delta1File);
                loader.AddDependencyLocation(delta2File);
                loader.AddDependencyLocation(epsilonFile);

                Assembly epsilon = loader.LoadFromPath(epsilonFile);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                if (ExecutionConditionUtil.IsDesktop && loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // In desktop + shadow load the dependencies are in different directories with 
                    // no context available when the load for Delta comes in. So we pick the best 
                    // option.
                    // Epsilon wants Delta2, but since Delta1 is in the same directory, we prefer Delta1 over Delta2.
                    VerifyDependencyAssemblies(
                        loader,
                        copyCount: 3,
                        delta2File,
                        epsilonFile);

                    var actual = sb.ToString();
                    Assert.Equal(
        @"Delta.2: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // See limitation 2
                    VerifyDependencyAssemblies(
                        loader,
                        delta1File,
                        epsilonFile);

                    var actual = sb.ToString();
                    Assert.Equal(
        @"Delta: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MultipleLoaders(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader1, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader1.AddDependencyLocation(testFixture.Gamma);
                loader1.AddDependencyLocation(testFixture.Delta1);

                var loader2 = new DefaultAnalyzerAssemblyLoader();
                loader2.AddDependencyLocation(testFixture.Epsilon);
                loader2.AddDependencyLocation(testFixture.Delta2);

                Assembly gamma = loader1.LoadFromPath(testFixture.Gamma);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader2.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

#if NETCOREAPP
                var alcs1 = loader1.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(1, alcs1.Length);

                VerifyAssemblies(
                    loader1,
                    alcs1[0].Assemblies,
                    ("Delta", "1.0.0.0", testFixture.Delta1),
                    ("Gamma", "0.0.0.0", testFixture.Gamma));

                var alcs2 = loader2.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(1, alcs2.Length);

                VerifyAssemblies(
                    loader2,
                    alcs2[0].Assemblies,
                    ("Delta", "2.0.0.0", testFixture.Delta2),
                    ("Epsilon", "0.0.0.0", testFixture.Epsilon));
#endif

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr)
                {
                    Assert.Equal(
    @"Delta: Gamma: Test G
Delta.2: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    Assert.Equal(
    @"Delta: Gamma: Test G
Delta: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MissingVersion(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Epsilon);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                var eWrite = e.GetType().GetMethod("Write")!;

                var actual = sb.ToString();
                eWrite.Invoke(e, new object[] { sb, "Test E" });
                Assert.Equal(
    @"Delta: Gamma: Test G
",
                    actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_UnifyToHighest(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var sb = new StringBuilder();

                // Gamma depends on Delta v1, and Epsilon depends on Delta v2. But both should load
                // and both use Delta v2. We intentionally here are not adding a reference to Delta1, since
                // this test is testing what happens if it's not present. A simple example for this scenario
                // is an analyzer that depends on both Gamma and Epsilon; an analyzer package can't reasonably
                // package both Delta v1 and Delta v2, so it'll only package the highest and things should work.
                loader.AddDependencyLocation(testFixture.GammaReferencingPublicSigned);
                loader.AddDependencyLocation(testFixture.EpsilonReferencingPublicSigned);
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned2);

                var gamma = loader.LoadFromPath(testFixture.GammaReferencingPublicSigned);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                var epsilon = loader.LoadFromPath(testFixture.EpsilonReferencingPublicSigned);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                var actual = sb.ToString();

                Assert.Equal(
    @"Delta.2: Gamma: Test G
Delta.2: Epsilon: Test E
",
                    actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_CanLoadDifferentVersionsDirectly(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var sb = new StringBuilder();

                // Ensure that no matter what, if we have two analyzers of different versions, we never unify them.
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned1);
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned2);

                var delta1Assembly = loader.LoadFromPath(testFixture.DeltaPublicSigned1);
                var delta1Instance = delta1Assembly.CreateInstance("Delta.D")!;
                delta1Instance.GetType().GetMethod("Write")!.Invoke(delta1Instance, new object[] { sb, "Test D1" });

                var delta2Assembly = loader.LoadFromPath(testFixture.DeltaPublicSigned2);
                var delta2Instance = delta2Assembly.CreateInstance("Delta.D")!;
                delta2Instance.GetType().GetMethod("Write")!.Invoke(delta2Instance, new object[] { sb, "Test D2" });

                var actual = sb.ToString();

                Assert.Equal(
    @"Delta: Test D1
Delta.2: Test D2
",
                    actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_01(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable);
                loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable1);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable1);
                var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;

                if (ExecutionConditionUtil.IsCoreClr)
                {
                    var ex = Assert.ThrowsAny<Exception>(() => analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb }));
                    Assert.True(ex is MissingMethodException or TargetInvocationException, $@"Unexpected exception type: ""{ex.GetType()}""");
                }
                else
                {
                    analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
                    Assert.Equal("42", sb.ToString());
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_02(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable);
                loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable2);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable2);
                var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
                analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
                Assert.Equal(ExecutionConditionUtil.IsCoreClr ? "1" : "42", sb.ToString());
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_CompilerDependencyDuplicated(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var assembly = typeof(ImmutableArray<int>).Assembly;

                // Copy the dependency to a new location to simulate it being deployed by an 
                // analyzer / generator
                using var tempRoot = new TempRoot();
                var destFile = tempRoot.CreateDirectory().CreateOrOpenFile($"{assembly.GetName().Name}.dll").Path;
                File.Copy(assembly.Location, destFile, overwrite: true);
                loader.AddDependencyLocation(destFile);

                var copiedAssembly = loader.LoadFromPath(destFile);
                Assert.Single(AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName == assembly.FullName));
                Assert.Same(copiedAssembly, assembly);
            });
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [CombinatorialData]
        public void AssemblyLoading_NativeDependency(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                const int INVALID_FILE_ATTRIBUTES = -1;
                loader.AddDependencyLocation(testFixture.AnalyzerWithNativeDependency);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerWithNativeDependency);
                var analyzer = analyzerAssembly.CreateInstance("Class1")!;
                var result = analyzer.GetType().GetMethod("GetFileAttributes")!.Invoke(analyzer, new[] { testFixture.AnalyzerWithNativeDependency });
                Assert.NotEqual(INVALID_FILE_ATTRIBUTES, result);
                Assert.Equal(FileAttributes.Archive | FileAttributes.ReadOnly, (FileAttributes)result!);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DeleteAfterLoad1(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                loader.AddDependencyLocation(deltaCopy);
                _ = loader.LoadFromPath(deltaCopy);

                if (loader is ShadowCopyAnalyzerAssemblyLoader || !ExecutionConditionUtil.IsWindows)
                {
                    File.Delete(deltaCopy);
                }
                else
                {
                    Assert.Throws<UnauthorizedAccessException>(() => File.Delete(testFixture.Delta1));
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DeleteAfterLoad2(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                loader.AddDependencyLocation(deltaCopy);
                Assembly? delta = loader.LoadFromPath(deltaCopy);

                if (loader is ShadowCopyAnalyzerAssemblyLoader || !ExecutionConditionUtil.IsWindows)
                {
                    File.Delete(deltaCopy);
                }

                // Ensure everything is functioning still 
                var d = delta.CreateInstance("Delta.D");
                d!.GetType().GetMethod("Write")!.Invoke(d, new object[] { sb, "Test D" });

                var actual = sb.ToString();
                Assert.Equal(
    @"Delta: Test D
",
                    actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DeleteAfterLoad3(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var sb = new StringBuilder();

                var tempDir1 = tempDir.CreateDirectory("a");
                var tempDir2 = tempDir.CreateDirectory("b");
                var tempDir3 = tempDir.CreateDirectory("c");

                var delta1File = tempDir1.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var delta2File = tempDir2.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta2).Path;
                var gammaFile = tempDir3.CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma).Path;

                loader.AddDependencyLocation(delta1File);
                loader.AddDependencyLocation(delta2File);
                loader.AddDependencyLocation(gammaFile);
                Assembly gamma = loader.LoadFromPath(gammaFile);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                if (loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    File.Delete(delta1File);
                    File.Delete(delta2File);
                    File.Delete(gammaFile);
                }

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_RepeatedLoads1(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var path = testFixture.Delta1;
                loader.AddDependencyLocation(path);
                var expected = loader.LoadFromPath(path);

                for (var i = 0; i < 5; i++)
                {
                    loader.AddDependencyLocation(path);
                    var actual = loader.LoadFromPath(path);
                    Assert.Same(expected, actual);
                }

                VerifyDependencyAssemblies(loader, path);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_RepeatedLoads2(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var tempFile = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1);
                var path = tempFile.Path;
                loader.AddDependencyLocation(path);
                var expected = loader.LoadFromPath(path);

                for (var i = 0; i < 5; i++)
                {
                    if (loader is ShadowCopyAnalyzerAssemblyLoader)
                    {
                        File.WriteAllBytes(path, new byte[] { 42 });
                    }
                    loader.AddDependencyLocation(path);
                    var actual = loader.LoadFromPath(path);
                    Assert.Same(expected, actual);
                }

                if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
                {
                    // Ensure that despite the on disk changes only one shadow copy occurred
                    Assert.Equal(1, shadowLoader.CopyCount);
                    tempFile.CopyContentFrom(testFixture.Delta1);
                }

                VerifyDependencyAssemblies(loader, path);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_Resources(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var analyzerPath = tempDir.CreateFile("AnalyzerWithLoc.dll").CopyContentFrom(testFixture.AnalyzerWithLoc).Path;
                var analyzerResourcesPath = tempDir.CreateDirectory("en-GB").CreateFile("AnalyzerWithLoc.resources.dll").CopyContentFrom(testFixture.AnalyzerWithLocResourceEnGB).Path;
                loader.AddDependencyLocation(analyzerPath);
                var assembly = loader.LoadFromPath(analyzerPath);
                var methodInfo = assembly
                    .GetType("AnalyzerWithLoc.Util")!
                    .GetMethod("Exec", BindingFlags.Static | BindingFlags.Public)!;
                methodInfo.Invoke(null, ["en-GB"]);

                // The copy count is 1 here as only one real assembly was copied, the resource 
                // dlls don't apply for this count.
                VerifyDependencyAssemblies(loader, copyCount: 1, analyzerPath, analyzerResourcesPath);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_ResourcesInParent(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var analyzerPath = tempDir.CreateFile("AnalyzerWithLoc.dll").CopyContentFrom(testFixture.AnalyzerWithLoc).Path;
                var analyzerResourcesPath = tempDir.CreateDirectory("es").CreateFile("AnalyzerWithLoc.resources.dll").CopyContentFrom(testFixture.AnalyzerWithLocResourceEnGB).Path;
                loader.AddDependencyLocation(analyzerPath);
                var assembly = loader.LoadFromPath(analyzerPath);
                var methodInfo = assembly
                    .GetType("AnalyzerWithLoc.Util")!
                    .GetMethod("Exec", BindingFlags.Static | BindingFlags.Public)!;
                methodInfo.Invoke(null, ["es-ES"]);

                // The copy count is 1 here as only one real assembly was copied, the resource 
                // dlls don't apply for this count.
                VerifyDependencyAssemblies(loader, copyCount: 1, analyzerPath, analyzerResourcesPath);
            });
        }

#if NETCOREAPP

        [Theory]
        [CombinatorialData]
        public void AssemblyLoadingInNonDefaultContext_AnalyzerReferencesSystemCollectionsImmutable(AnalyzerTestKind kind)
        {
            Run(kind,
                static (AssemblyLoadContext compilerContext, AssemblyLoadTestFixture testFixture) =>
                {
                    // Load the compiler assembly and a modified version of S.C.I into the compiler load context. We
                    // expect the analyzer will use the bogus S.C.I in the compiler context instead of the one 
                    // in the host context.
                    _ = compilerContext.LoadFromAssemblyPath(testFixture.UserSystemCollectionsImmutable);
                    _ = compilerContext.LoadFromAssemblyPath(typeof(AnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location);
                },
                static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
                {
                    StringBuilder sb = new StringBuilder();

                    loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable);
                    loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable1);

                    Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable1);
                    var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
                    analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });

                    Assert.Equal("42", sb.ToString());
                });
        }
#endif

        [Theory]
        [CombinatorialData]
        public void ExternalResolver_CanIntercept_ReturningNull(AnalyzerTestKind kind)
        {
            var resolver = new TestAnalyzerAssemblyResolver(n => null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);
                VerifyDependencyAssemblies(loader, testFixture.Delta1);

            }, externalResolvers: [resolver]);
            Assert.Collection(resolver.CalledFor, (a => Assert.Equal("Delta", a.Name)));
        }

        [Theory]
        [CombinatorialData]
        public void ExternalResolver_CanIntercept_ReturningAssembly(AnalyzerTestKind kind)
        {
            var resolver = new TestAnalyzerAssemblyResolver(n => GetType().Assembly);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                // net core assembly loader checks that the resolved assembly name is the same as the requested one
                // so we use the assembly the tests are contained in as its already be loaded
                var thisAssembly = typeof(AnalyzerAssemblyLoaderTests).Assembly;
                loader.AddDependencyLocation(thisAssembly.Location);
                Assembly loaded = loader.LoadFromPath(thisAssembly.Location);
                Assert.Equal(thisAssembly, loaded);

            }, externalResolvers: [resolver]);
            Assert.Collection(resolver.CalledFor, (a => Assert.Equal(GetType().Assembly.GetName().Name, a.Name)));
        }

        [Theory]
        [CombinatorialData]
        public void ExternalResolver_CanIntercept_ReturningAssembly_Or_Null(AnalyzerTestKind kind)
        {
            var thisAssemblyName = GetType().Assembly.GetName();
            var resolver = new TestAnalyzerAssemblyResolver(n => n == thisAssemblyName ? GetType().Assembly : null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var thisAssembly = typeof(AnalyzerAssemblyLoaderTests).Assembly;

                loader.AddDependencyLocation(testFixture.Alpha);
                Assembly alpha = loader.LoadFromPath(testFixture.Alpha);
                Assert.NotNull(alpha);

                loader.AddDependencyLocation(thisAssembly.Location);
                Assembly loaded = loader.LoadFromPath(thisAssembly.Location);
                Assert.Equal(thisAssembly, loaded);

                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);

            }, externalResolvers: [resolver]);
            Assert.Collection(resolver.CalledFor, (a => Assert.Equal("Alpha", a.Name)), a => Assert.Equal(thisAssemblyName.Name, a.Name), a => Assert.Equal("Delta", a.Name));
        }

        [Theory]
        [CombinatorialData]
        public void ExternalResolver_MultipleResolvers_CanIntercept_ReturningNull(AnalyzerTestKind kind)
        {
            var resolver1 = new TestAnalyzerAssemblyResolver(n => null);
            var resolver2 = new TestAnalyzerAssemblyResolver(n => null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);
                VerifyDependencyAssemblies(loader, testFixture.Delta1);

            }, externalResolvers: [resolver1, resolver2]);
            Assert.Collection(resolver1.CalledFor, (a => Assert.Equal("Delta", a.Name)));
            Assert.Collection(resolver2.CalledFor, (a => Assert.Equal("Delta", a.Name)));
        }

        [Theory]
        [CombinatorialData]
        public void ExternalResolver_MultipleResolvers_ResolutionStops_AfterFirstResolve(AnalyzerTestKind kind)
        {
            var resolver1 = new TestAnalyzerAssemblyResolver(n => GetType().Assembly);
            var resolver2 = new TestAnalyzerAssemblyResolver(n => null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var thisAssembly = typeof(AnalyzerAssemblyLoaderTests).Assembly;
                loader.AddDependencyLocation(thisAssembly.Location);
                Assembly loaded = loader.LoadFromPath(thisAssembly.Location);
                Assert.Equal(thisAssembly, loaded);

            }, externalResolvers: [resolver1, resolver2]);
            Assert.Collection(resolver1.CalledFor, (a => Assert.Equal(GetType().Assembly.GetName().Name, a.Name)));
            Assert.Empty(resolver2.CalledFor);
        }

        [Serializable]
        private class TestAnalyzerAssemblyResolver(Func<AssemblyName, Assembly?> func) : MarshalByRefObject, IAnalyzerAssemblyResolver
        {
            private readonly Func<AssemblyName, Assembly?> _func = func;

            public List<AssemblyName> CalledFor { get; } = [];

            public Assembly? ResolveAssembly(AssemblyName assemblyName)
            {
                CalledFor.Add(assemblyName);
                return _func(assemblyName);
            }
        }
    }
}
