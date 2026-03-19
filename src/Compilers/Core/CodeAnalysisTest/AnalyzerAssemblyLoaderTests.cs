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
using Microsoft.CodeAnalysis.Text;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using System.Diagnostics;

#if NET
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
#if NET
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

#if NET

        private void Run(
            AnalyzerTestKind kind,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers = default,
            ImmutableArray<IAnalyzerAssemblyResolver> assemblyResolvers = default) =>
            Run(
                kind,
                state: null,
                testAction.Method,
                pathResolvers.NullToEmpty(),
                assemblyResolvers.NullToEmpty());

        private void Run(
            AnalyzerTestKind kind,
            object state,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture, object> testAction,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers = default,
            ImmutableArray<IAnalyzerAssemblyResolver> assemblyResolvers = default) =>
            Run(
                kind,
                state,
                testAction.Method,
                pathResolvers.NullToEmpty(),
                assemblyResolvers.NullToEmpty());

        private void Run(
            AnalyzerTestKind kind,
            object? state,
            MethodInfo method,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers,
            ImmutableArray<IAnalyzerAssemblyResolver> assemblyResolvers)
        {
            var util = new InvokeUtil();
            util.Exec(
                TestOutputHelper,
                pathResolvers,
                assemblyResolvers,
                TestFixture,
                kind,
                method.DeclaringType!.FullName!,
                method.Name,
                state);
        }

        private void Run(
            AnalyzerAssemblyLoader loader,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction)
        {
            var util = new InvokeUtil();
            util.Exec(
                TestOutputHelper,
                TestFixture,
                loader,
                testAction.Method.DeclaringType!.FullName!,
                testAction.Method.Name,
                state: null);
        }

        private void Run(
            AnalyzerAssemblyLoader loader,
            object state,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture, object> testAction)
        {
            var util = new InvokeUtil();
            util.Exec(
                TestOutputHelper,
                TestFixture,
                loader,
                testAction.Method.DeclaringType!.FullName!,
                testAction.Method.Name,
                state: state);
        }
#else

        private void Run(
            AnalyzerTestKind kind,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture> testAction,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers = default,
            [CallerMemberName] string? memberName = null) =>
            Run(
                kind,
                state: null,
                testAction.Method,
                pathResolvers.NullToEmpty(),
                memberName);

        private void Run(
            AnalyzerTestKind kind,
            object state,
            Action<AnalyzerAssemblyLoader, AssemblyLoadTestFixture, object> testAction,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers = default,
            [CallerMemberName] string? memberName = null) =>
            Run(kind,
                state,
                testAction.Method,
                pathResolvers.NullToEmpty(),
                memberName);

        private void Run(
            AnalyzerTestKind kind,
            object state,
            MethodInfo method,
            ImmutableArray<IAnalyzerPathResolver> pathResolvers,
            string? memberName)
        {
            AppDomain? appDomain = null;
            try
            {
                appDomain = AppDomainUtils.Create($"Test {memberName}");
                var testOutputHelper = new AppDomainTestOutputHelper(TestOutputHelper);
                var type = typeof(InvokeUtil);
                var util = (InvokeUtil)appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
                util.Exec(testOutputHelper, TestFixture, kind, method.DeclaringType.FullName, method.Name, pathResolvers.ToArray(), state);
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
        internal static void InvokeTestCode(AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture fixture, string typeName, string methodName, object? state)
        {
            var type = typeof(AnalyzerAssemblyLoaderTests).Assembly.GetType(typeName, throwOnError: false)!;
            var member = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;

            // A static lambda will still be an instance method so we need to create the closure
            // here.
            var obj = member.IsStatic
                ? null
                : type.Assembly.CreateInstance(typeName);

            object[] args = state is null
                ? [loader, fixture]
                : [loader, fixture, state];
            member.Invoke(obj, args);
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
                Assert.Throws<ArgumentNullException>("originalPath", () => loader.AddDependencyLocation(null!));
                Assert.Throws<ArgumentException>("originalPath", () => loader.AddDependencyLocation("a"));
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
        /// The loaders should not _require_ contents to actually be on disk until the
        /// <see cref="AnalyzerAssemblyLoader.LoadFromPath(string)"/> call has occurred. If the file
        /// contents were required immediately then <see cref="AnalyzerReference"/> would throw in its ctor 
        /// rather than when using the reference.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_AssemblyLocationInvalid(AnalyzerTestKind kind)
        {
            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var analyzerPath = Path.Combine(tempDir.CreateDirectory("a").Path, "analyzer.dll");
                loader.AddDependencyLocation(analyzerPath);
                Assert.Throws<ArgumentException>(() => loader.LoadFromPath(analyzerPath));
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
                Assert.Throws<ArgumentException>(() => loader.LoadFromPath(testFixture.Beta));
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyLocationNotAdded(AnalyzerTestKind kind)
        {
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma);
                loader.AddDependencyLocation(testFixture.Beta);
                Assembly beta = loader.LoadFromPath(testFixture.Beta);

                var b = beta.CreateInstance("Beta.B")!;
                var writeMethod = b.GetType().GetMethod("Write")!;

                if (ExecutionConditionUtil.IsCoreClr || state is AnalyzerTestKind.ShadowLoad)
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
            var expectedVersions = expected
                .Select(x => $"{x.simpleName} {x.version}")
                .OrderBy(static x => x)
                .ToArray();
            var assemblyVersions = assemblies
                    .Select(assembly => $"{assembly.GetName().Name!} {assembly.GetName().Version}")
                    .OrderBy(static x => x)
                    .ToArray();
            Assert.Equal(expectedVersions, assemblyVersions);

            var expectedPaths = expected
                .Select(x => getExpectedLoadPath(x.path))
                .OrderBy(static x => x)
                .ToArray();
            var assemblyPaths = assemblies
                .Select(x => x.Location)
                .OrderBy(static x => x)
                .ToArray();
            Assert.Equal(expectedPaths, assemblyPaths);

            if (loader.AnalyzerPathResolvers.OfType<ShadowCopyAnalyzerPathResolver>().FirstOrDefault() is { } shadowLoader)
            {
                Assert.All(assemblies, x => x.Location.StartsWith(shadowLoader.BaseDirectory, StringComparison.Ordinal));
                Assert.Equal(expectedCopyCount ?? expected.Length, shadowLoader.CopyCount);
            }

            string getExpectedLoadPath(string path)
            {
#if NET
                if (loader.AnalyzerAssemblyResolvers.Any(x => x == AnalyzerAssemblyLoader.StreamAnalyzerAssemblyResolver))
                {
                    return "";
                }
#endif

                if (path.EndsWith(".resources.dll", StringComparison.Ordinal))
                {
                    return getRealSatellitePath(path) ?? "";
                }
                return loader.GetResolvedAnalyzerPath(path ?? "");
            }

            // When PreparePathToLoad is overridden this returns the most recent
            // real path for the given analyzer satellite assembly path
            string? getRealSatellitePath(string originalSatelliteFullPath)
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
                return loader.GetResolvedSatellitePath(assemblyPath, cultureInfo);
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

#if NET
            var alcs = loader.GetDirectoryLoadContextsSnapshot();
            loadedAssemblies = alcs.SelectMany(x => x.Assemblies);
#else

            // The assemblies in the LoadFrom context are the assemblies loaded from 
            // analyzer dependencies.
            loadedAssemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(x => isInLoadFromContext(loader, x));

            // When debugging, the debugger will load this DLL and that can throw off the debugging 
            // session so exclude it here.
            if (Debugger.IsAttached)
            {
                loadedAssemblies = loadedAssemblies.Where(x => x.GetName().Name != "Microsoft.VisualStudio.Debugger.Runtime.Desktop");
            }

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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
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
                var copyCount = state is AnalyzerTestKind.ShadowLoad
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
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
                var copyCount = state is AnalyzerTestKind.ShadowLoad
                    ? 2
                    : (int?)null;
                VerifyDependencyAssemblies(
                    loader,
                    copyCount: copyCount,
                    assemblyPaths: [deltaFile]);
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
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

                VerifyDependencyAssemblies(
                    loader,
                    copyCount: 3,
                    deltaFile2,
                    gammaFile);
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

                Assert.NotNull(loader.GetResolvedAnalyzerPath(analyzerMainFile));
                Assert.NotNull(loader.GetResolvedAnalyzerPath(analyzerDependencyFile));

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

#if NET
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Epsilon);
                loader.AddDependencyLocation(testFixture.Delta3);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr || state is AnalyzerTestKind.ShadowLoad)
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
                    Assert.Throws<ArgumentException>(() => loader.GetResolvedAnalyzerPath(testFixture.Delta2));

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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B);
                loader.AddDependencyLocation(testFixture.Delta2);
                loader.AddDependencyLocation(testFixture.Epsilon);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                // See limitation 1
                // Delta2B and Delta2 have the same version, but we prefer Delta2 because it's in the same directory as Epsilon.
                VerifyDependencyAssemblies(
                    loader,
                    copyCount: 3,
                    testFixture.Delta2,
                    testFixture.Epsilon);

                var actual = sb.ToString();
                Assert.Equal(
@"Delta.2: Epsilon: Test E
",
                    actual);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MultipleVersionsOfSameAnalyzerItself(AnalyzerTestKind kind)
        {
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2);
                loader.AddDependencyLocation(testFixture.Delta2B);

                Assembly delta2 = loader.LoadFromPath(testFixture.Delta2);
                Assembly delta2B = loader.LoadFromPath(testFixture.Delta2B);

                // 2B or not 2B? That is the question...that depends on whether we're on .NET Core or not.

#if NET

                // On Core, we're able to load both of these into separate AssemblyLoadContexts.
                if (state is AnalyzerTestKind.LoadDirect)
                {
                    Assert.NotEqual(delta2B.Location, delta2.Location);
                    Assert.Equal(loader.GetResolvedAnalyzerPath(testFixture.Delta2), delta2.Location);
                    Assert.Equal(loader.GetResolvedAnalyzerPath(testFixture.Delta2B), delta2B.Location);
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();

                // This test is about validating how dependencies resolve when there are multiple versions
                // on disk with some registered and some not-registered. In this case Epislon has a dependency
                // on Delta2. 

                var dir1 = tempDir.CreateDirectory("1");
                var unregisteredDeltaPath = dir1.CopyFile(testFixture.Delta2).Path;
                var epsilonPath = dir1.CopyFile(testFixture.Epsilon).Path;

                var dir2 = tempDir.CreateDirectory("2");
                var registeredDeltaPath = dir2.CopyFile(testFixture.Delta2).Path;

                loader.AddDependencyLocation(registeredDeltaPath);
                loader.AddDependencyLocation(epsilonPath);

                Assembly epsilon = loader.LoadFromPath(epsilonPath);
                StringBuilder sb = new StringBuilder();
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });
                Assert.Equal(
                    @"Delta.2: Epsilon: Test E
",
                    sb.ToString());

                if (ExecutionConditionUtil.IsCoreClr || state is AnalyzerTestKind.ShadowLoad)
                {
                    // This works in CoreClr because we have full control over assembly loading.
                    // This works in ShadowLoad because the unregistered dependency is not copied hence can't be 
                    // implicitly loaded
                    VerifyDependencyAssemblies(
                        loader,
                        copyCount: 2,
                        registeredDeltaPath,
                        epsilonPath);
                }
                else
                {
                    // On desktop without shadow load then the desktop loader will grab the unregistered
                    // dependency because it's in the same directory as the main assembly and LoadFrom
                    // rules will pick it without a chance to intervene

                    // Add the dependency location just so we can run the verify below
                    loader.AddDependencyLocation(unregisteredDeltaPath);
                    VerifyDependencyAssemblies(
                        loader,
                        copyCount: 2,
                        unregisteredDeltaPath,
                        epsilonPath);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_WorseMatchInSameDirectory(AnalyzerTestKind kind)
        {
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var tempDir1 = tempDir.CreateDirectory("1");
                var tempDir2 = tempDir.CreateDirectory("2");
                var epsilonFile = tempDir1.CreateFile("Epsilon.dll").CopyContentFrom(testFixture.Epsilon).Path;
                var delta1File = tempDir1.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                var delta2File = tempDir2.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta2).Path;

                loader.AddDependencyLocation(delta1File);
                loader.AddDependencyLocation(delta2File);
                loader.AddDependencyLocation(epsilonFile);

                Assembly epsilon = loader.LoadFromPath(epsilonFile);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                // See limitation 2
                VerifyDependencyAssemblies(
                    loader,
                    copyCount: 3,
                    delta1File,
                    epsilonFile);

                var actual = sb.ToString();
                Assert.Equal(
    @"Delta: Epsilon: Test E
",
                    actual);
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

                var loader2 = new AnalyzerAssemblyLoader();
                loader2.AddDependencyLocation(testFixture.Epsilon);
                loader2.AddDependencyLocation(testFixture.Delta2);

                Assembly gamma = loader1.LoadFromPath(testFixture.Gamma);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader2.LoadFromPath(testFixture.Epsilon);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

#if NET
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

        /// <summary>
        /// Test the case where a utility is loaded by multiple analyzers at different versions. Ensure that no matter
        /// what order we load the analyzers we correctly resolve the utility version.
        /// </summary>
        [ConditionalTheory(typeof(DesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/79352")]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_AnalyzerDependency(AnalyzerTestKind kind, bool normalOrder)
        {
            Run(kind, state: normalOrder, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                using var temp = new TempRoot();
                var analyzerFilePaths = new List<string>();
                var compilerReference = MetadataReference.CreateFromFile(typeof(SyntaxNode).Assembly.Location);
                var immutableReference = MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location);

                var testCode = """
                    using System;

                    Console.WriteLine("Hello World");
                    """;

                var compilation = CSharpCompilation.Create(
                    "test",
                    [CSharpSyntaxTree.ParseText(SourceText.From(testCode, encoding: null, checksumAlgorithm: SourceHashAlgorithms.Default))],
                    NetStandard20.References.All);

                // Test loading the analyzers in different orders. That makes sure we verify the loading handles
                // the higher version of delta being loaded first or second.
                ImmutableArray<DiagnosticAnalyzer> analyzers = state is true
                    ? [loadAnalyzer1(), loadAnalyzer2()]
                    : [loadAnalyzer2(), loadAnalyzer1()];
                var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
                compilation.VerifyEmitDiagnostics();
                Assert.Empty(compilationWithAnalyzers.GetAllDiagnosticsAsync().Result);

                foreach (var analyzer in analyzers)
                {
                    assertRan(analyzer);
                }

                VerifyDependencyAssemblies(loader, analyzerFilePaths.ToArray());

                void assertRan(DiagnosticAnalyzer a)
                {
                    var prop = a.GetType().GetProperty("Ran", BindingFlags.Public | BindingFlags.Instance);
                    Assert.NotNull(prop);
                    var value = prop.GetValue(a, null);
                    Assert.True(value is true);
                }

                DiagnosticAnalyzer loadAnalyzer1()
                {
                    var code = """

                        using System;
                        using System.Collections.Immutable;
                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.Diagnostics;

                        [DiagnosticAnalyzer(LanguageNames.CSharp)]
                        public class Analyzer1: DiagnosticAnalyzer
                        {
                            public static readonly DiagnosticDescriptor Warning = new DiagnosticDescriptor(
                                "Warning2",
                                "",
                                "",
                                "",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true);
                            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray<DiagnosticDescriptor>.Empty.Add(Warning);
                            public bool Ran { get; set; }
                            public override void Initialize(AnalysisContext context)
                            {
                                var d = new Delta.D();
                                d.M1();
                                Ran = true;
                            }
                        }
                        """;
                    var assemblyFilePath = buildWithCode("analyzer1", code, testFixture.DeltaPublicSigned1);
                    var assembly = loader.LoadFromPath(assemblyFilePath);
                    return (DiagnosticAnalyzer)assembly.CreateInstance("Analyzer1")!;
                }

                DiagnosticAnalyzer loadAnalyzer2()
                {
                    var code = """
                        using System;
                        using System.Collections.Immutable;
                        using Microsoft.CodeAnalysis;
                        using Microsoft.CodeAnalysis.Diagnostics;

                        [DiagnosticAnalyzer(LanguageNames.CSharp)]
                        public class Analyzer2: DiagnosticAnalyzer
                        {
                            public static readonly DiagnosticDescriptor Warning = new DiagnosticDescriptor(
                                "Warning1",
                                "",
                                "",
                                "",
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault: true);
                            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray<DiagnosticDescriptor>.Empty.Add(Warning);

                            public bool Ran { get; set; }
                            public override void Initialize(AnalysisContext context)
                            {
                                var d = new Delta.D();
                                d.M2();
                                Ran = true;
                            }
                        }
                        """;
                    var assemblyFilePath = buildWithCode("analyzer2", code, testFixture.DeltaPublicSigned2);
                    var assembly = loader.LoadFromPath(assemblyFilePath);
                    return (DiagnosticAnalyzer)assembly.CreateInstance("Analyzer2")!;
                }

                string buildWithCode(string assemblyName, string analyzerCode, string deltaFilePath)
                {
                    var dir = temp.CreateDirectory();
                    var deltaNewFilePath = dir.CopyFile(deltaFilePath).Path;

                    var compilation = CSharpCompilation.Create(
                        assemblyName,
                        [CSharpSyntaxTree.ParseText(SourceText.From(analyzerCode, encoding: null, checksumAlgorithm: SourceHashAlgorithms.Default))],
                        [
                            .. NetStandard20.References.All,
                            compilerReference,
                            immutableReference,
                            MetadataReference.CreateFromFile(deltaFilePath)
                        ],
                        TestOptions.DebugDll.WithPublicSign(true).WithCryptoPublicKey(SigningTestHelpers.PublicKey));

                    var array = compilation.EmitToArray(EmitOptions.Default);
                    var assemblyFilePath = dir.CreateFile(assemblyName + ".dll").WriteAllBytes(array).Path;
                    loader.AddDependencyLocation(deltaNewFilePath);
                    analyzerFilePaths.Add(deltaNewFilePath);
                    loader.AddDependencyLocation(assemblyFilePath);
                    analyzerFilePaths.Add(assemblyFilePath);
                    return assemblyFilePath;
                }
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
                Assert.Single(AppDomain.CurrentDomain.GetAssemblies(), x => x.FullName == assembly.FullName);
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                loader.AddDependencyLocation(deltaCopy);
                _ = loader.LoadFromPath(deltaCopy);

                if (state is AnalyzerTestKind.ShadowLoad || !ExecutionConditionUtil.IsWindows)
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1).Path;
                loader.AddDependencyLocation(deltaCopy);
                Assembly? delta = loader.LoadFromPath(deltaCopy);

                if (state is AnalyzerTestKind.ShadowLoad || !ExecutionConditionUtil.IsWindows)
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
            Run(kind, state: kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
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

                if (state is AnalyzerTestKind.ShadowLoad)
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

                var shadowLoader = loader.AnalyzerPathResolvers.OfType<ShadowCopyAnalyzerPathResolver>().FirstOrDefault();
                for (var i = 0; i < 5; i++)
                {
                    if (shadowLoader is not null)
                    {
                        File.WriteAllBytes(path, new byte[] { 42 });
                    }
                    loader.AddDependencyLocation(path);
                    var actual = loader.LoadFromPath(path);
                    Assert.Same(expected, actual);
                }

                if (shadowLoader is not null)
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
                VerifyDependencyAssemblies(loader, copyCount: 2, analyzerPath, analyzerResourcesPath);
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

                VerifyDependencyAssemblies(loader, copyCount: 2, analyzerPath, analyzerResourcesPath);
            });
        }

#if NET

        [Theory]
        [CombinatorialData]
        public void AssemblyLoadingInNonDefaultContext_AnalyzerReferencesSystemCollectionsImmutable(AnalyzerTestKind kind)
        {
            // Load the compiler assembly and a modified version of S.C.I into the compiler load context. We
            // expect the analyzer will use the bogus S.C.I in the compiler context instead of the one 
            // in the host context.
            var alc = new AssemblyLoadContext(nameof(AssemblyResolver_FirstOneWins), isCollectible: false);
            _ = alc.LoadFromAssemblyPath(TestFixture.UserSystemCollectionsImmutable);
            _ = alc.LoadFromAssemblyPath(typeof(AnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location);
            var loader = kind switch
            {
                AnalyzerTestKind.LoadStream => new AnalyzerAssemblyLoader([], [AnalyzerAssemblyLoader.StreamAnalyzerAssemblyResolver], alc),
                AnalyzerTestKind.LoadDirect => new AnalyzerAssemblyLoader([], [AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver], alc),
                AnalyzerTestKind.ShadowLoad => new AnalyzerAssemblyLoader([new ShadowCopyAnalyzerPathResolver(Temp.CreateDirectory().Path)], [AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver], alc),
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };

            Run(loader, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DoesNotUseCollectibleALCs(AnalyzerTestKind kind)
        {
            // This validation is critical to our VS / CLI performance. We ship several analyzers and source-generators in the
            // SDK (NetAnalyzers & Razor generators) that are added to most projects. We want to ship these as Ready2Run so that
            // we reduce JIT time. However, when an assembly is loaded into a collectible AssemblyLoadContext it prevents any of
            // the R2R logic from being used.

            Run(kind, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1);
                loader.AddDependencyLocation(testFixture.Gamma);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma);
                Assert.NotNull(gamma);

                var contexts = loader.GetDirectoryLoadContextsSnapshot();
                Assert.NotEmpty(contexts);

                foreach (var context in contexts)
                {
                    Assert.False(context.IsCollectible, "AnalyzerAssemblyLoader should not use collectible assembly load contexts.");
                }
            });
        }
#endif

        [Theory]
        [CombinatorialData]
        public void PathResolver_CanIntercept_ReturningNull(AnalyzerTestKind kind)
        {
            var resolver = new TestAnalyzerPathResolver(n => null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);
                VerifyDependencyAssemblies(loader, testFixture.Delta1);

            }, pathResolvers: [resolver]);
            Assert.Equal([TestFixture.Delta1], resolver.CalledFor);
        }

        [Theory]
        [CombinatorialData]
        public void PathResolver_CanIntercept_ReturningAssembly_Or_Null(AnalyzerTestKind kind)
        {
            var resolver1 = new TestAnalyzerPathResolver(n => n == TestFixture.Alpha ? n : null);
            var resolver2 = new TestAnalyzerPathResolver(n => n);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Alpha);
                Assembly alpha = loader.LoadFromPath(testFixture.Alpha);
                Assert.NotNull(alpha);

                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);

            }, pathResolvers: [resolver1, resolver2]);

            Assert.Equal([TestFixture.Alpha, TestFixture.Delta1], resolver1.CalledFor);
        }

        [Theory]
        [CombinatorialData]
        public void PathResolver_MultipleResolvers_CanIntercept_ReturningNull(AnalyzerTestKind kind)
        {
            var resolver1 = new TestAnalyzerPathResolver(n => null);
            var resolver2 = new TestAnalyzerPathResolver(n => null);
            Run(kind, (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly delta = loader.LoadFromPath(testFixture.Delta1);
                Assert.NotNull(delta);
                VerifyDependencyAssemblies(loader, testFixture.Delta1);

            }, pathResolvers: [resolver1, resolver2]);
            Assert.Equal([TestFixture.Delta1], resolver1.CalledFor);
            Assert.Equal([TestFixture.Delta1], resolver2.CalledFor);
        }

#if NET

        [Theory]
        [CombinatorialData]
        public void AssemblyResolver_CanIntercept_Identity(AnalyzerTestKind kind)
        {
            var assembly = typeof(AnalyzerAssemblyLoaderTests).Assembly;
            var resolver = new TestAnalyzerAssemblyResolver((_, _, assemblyName, _) => assemblyName.Name == assembly.GetName().Name ? assembly : null);
            Run(kind, state: assembly, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                // net core assembly loader checks that the resolved assembly name is the same as the requested one
                // so we use the assembly the tests are contained in as its already be loaded
                var assembly = (Assembly)state;
                loader.AddDependencyLocation(assembly.Location);
                Assembly loaded = loader.LoadFromPath(assembly.Location);
                Assert.Same(assembly, loaded);
            }, assemblyResolvers: [resolver, AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver]);
        }

        [Fact]
        public void AssemblyResolver_FirstOneWins()
        {
            var alc = new AssemblyLoadContext(nameof(AssemblyResolver_FirstOneWins), isCollectible: true);
            var name = Path.GetFileNameWithoutExtension(TestFixture.Delta1);
            var resolver1 = new TestAnalyzerAssemblyResolver((_, assemblyName, current, _) =>
                assemblyName.Name == name ? current.LoadFromAssemblyPath(TestFixture.Delta1) : null);
            var resolver2 = new TestAnalyzerAssemblyResolver((_, _, assemblyName, _) => null);
            var loader = new AnalyzerAssemblyLoader([], [resolver1, resolver2], alc);

            Run(loader, state: name, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture, object state) =>
            {
                // net core assembly loader checks that the resolved assembly name is the same as the requested one
                // so we use the assembly the tests are contained in as its already be loaded
                loader.AddDependencyLocation(testFixture.Delta1);
                Assembly loaded = loader.LoadFromPath(testFixture.Delta1);
                Assert.Equal((string)state, loaded.GetName().Name);
            });

            Assert.Equal([name], resolver1.CalledFor.Select(x => x.Name));
            Assert.Empty(resolver2.CalledFor);
            alc.Unload();
        }

        [Fact]
        public void AssemblyResolver_ThrowOnNoMatch()
        {
            var name = Path.GetFileNameWithoutExtension(TestFixture.Delta1);
            var alc = new AssemblyLoadContext(nameof(AssemblyResolver_FirstOneWins), isCollectible: true);
            var resolver = new TestAnalyzerAssemblyResolver((_, _, assemblyName, _) => null);
            var loader = new AnalyzerAssemblyLoader([], [resolver], alc);

            Run(loader, static (AnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                // net core assembly loader checks that the resolved assembly name is the same as the requested one
                // so we use the assembly the tests are contained in as its already be loaded
                loader.AddDependencyLocation(testFixture.Delta1);
                Assert.Throws<InvalidOperationException>(() => loader.LoadFromPath(testFixture.Delta1));
            });

            Assert.Equal([Path.GetFileNameWithoutExtension(TestFixture.Delta1)], resolver.CalledFor.Select(x => x.Name));
        }
#endif

        private class TestAnalyzerPathResolver(Func<string, string?> getRealFilePathFunc) : MarshalByRefObject, IAnalyzerPathResolver
        {
            private readonly Func<string, string?> _getRealFilePathFunc = getRealFilePathFunc;

            public List<string> CalledFor { get; } = [];

            public bool IsAnalyzerPathHandled(string originalPath)
            {
                CalledFor.Add(originalPath);
                return _getRealFilePathFunc(originalPath) is not null;
            }

            public string GetResolvedAnalyzerPath(string originalAnalyzerPath) => _getRealFilePathFunc(originalAnalyzerPath)!;

            public string? GetResolvedSatellitePath(string originalAnalyzerPath, CultureInfo cultureInfo) => null;
        }

#if NET

        private class TestAnalyzerAssemblyResolver(Func<AnalyzerAssemblyLoader, AssemblyName, AssemblyLoadContext, string, Assembly?> resolveFunc) : IAnalyzerAssemblyResolver
        {
            private readonly Func<AnalyzerAssemblyLoader, AssemblyName, AssemblyLoadContext, string, Assembly?> _resolveFunc = resolveFunc;

            public List<AssemblyName> CalledFor { get; } = [];

            public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory)
            {
                CalledFor.Add(assemblyName);
                return _resolveFunc(loader, assemblyName, directoryContext, directory);
            }
        }

#endif
    }
}
