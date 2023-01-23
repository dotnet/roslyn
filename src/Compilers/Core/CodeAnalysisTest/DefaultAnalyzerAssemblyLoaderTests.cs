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
        public void Exec(Action<string> testOutputHelper, AssemblyLoadContext alc, bool shadowLoad, string typeName, string methodName)
        {
            // Ensure that the test did not load any of the test fixture assemblies into 
            // the default load context. That should never happen. Assemblies should either 
            // load into the compiler or directory load context.
            //
            // Not only is this bad behavior it also pollutes future test results.
            var count = AssemblyLoadContext.Default.Assemblies.Count();
            using var fixture = new AssemblyLoadTestFixture();
            using var tempRoot = new TempRoot();
            var loader = shadowLoad
                ? new ShadowCopyAnalyzerAssemblyLoader(alc, tempRoot.CreateDirectory().Path)
                : new DefaultAnalyzerAssemblyLoader(alc);
            try
            {
                DefaultAnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName);
            }
            finally
            {
                testOutputHelper($"Test fixture root: {fixture.TempDirectory.Path}");

                foreach (var context in loader.GetDirectoryLoadContextsSnapshot())
                {
                    testOutputHelper($"Directory context: {context.Directory}");
                    foreach (var assembly in context.Assemblies)
                    {
                        testOutputHelper($"\t{assembly.FullName}");
                    }
                }

                if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
                {
                    testOutputHelper($"Shadow loader: {shadowLoader.BaseDirectory}");
                }

                testOutputHelper($"Loader path maps");
                foreach (var pair in loader.GetPathMapSnapshot())
                {
                    testOutputHelper($"\t{pair.OriginalAssemblyPath} -> {pair.RealAssemblyPath}");
                }

                Assert.Equal(count, AssemblyLoadContext.Default.Assemblies.Count());
            }
        }
    }

#else

    public sealed class InvokeUtil : MarshalByRefObject
    {
        public void Exec(ITestOutputHelper testOutputHelper, bool shadowLoad, string typeName, string methodName)
        {
            using var fixture = new AssemblyLoadTestFixture();
            using var tempRoot = new TempRoot();
            var loader = shadowLoad
                ? new ShadowCopyAnalyzerAssemblyLoader(tempRoot.CreateDirectory().Path)
                : new DefaultAnalyzerAssemblyLoader();

            try
            {
                DefaultAnalyzerAssemblyLoaderTests.InvokeTestCode(loader, fixture, typeName, methodName);
            }
            catch (TargetInvocationException ex) when (ex.InnerException is XunitException)
            {
                var inner = ex.InnerException;
                throw new Exception(inner.Message + inner.StackTrace);
            }
            finally
            {
                testOutputHelper.WriteLine($"Test fixture root: {fixture.TempDirectory.Path}");

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

    public sealed class DefaultAnalyzerAssemblyLoaderTests : TestBase
    {
        public ITestOutputHelper TestOutputHelper { get; }

        public DefaultAnalyzerAssemblyLoaderTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        private void Run(bool shadowLoad, Action<DefaultAnalyzerAssemblyLoader, AssemblyLoadTestFixture> action, [CallerMemberName] string? memberName = null)
        {
#if NETCOREAPP
            var alc = AssemblyLoadContextUtils.Create($"Test {memberName}");
            var assembly = alc.LoadFromAssemblyName(typeof(InvokeUtil).Assembly.GetName());
            var util = assembly.CreateInstance(typeof(InvokeUtil).FullName)!;
            var method = util.GetType().GetMethod("Exec", BindingFlags.Public | BindingFlags.Instance)!;
            var outputHelper = (string msg) => TestOutputHelper.WriteLine(msg);
            method.Invoke(util, new object[] { outputHelper, alc, shadowLoad, action.Method.DeclaringType!.FullName!, action.Method.Name });

#else
            AppDomain? appDomain = null;
            try
            {
                appDomain = AppDomainUtils.Create($"Test {memberName}");
                var testOutputHelper = new AppDomainTestOutputHelper(TestOutputHelper);
                var type = typeof(InvokeUtil);
                var util = (InvokeUtil)appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
                util.Exec(testOutputHelper, shadowLoad, action.Method.DeclaringType.FullName, action.Method.Name);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
#endif
        }

        /// <summary>
        /// This is called from our newly created AppDomain or AssemblyLoadContext and needs to get 
        /// us back to the actual test code to execute. The intent is to invoke the lambda / static
        /// local func where the code exists.
        /// </summary>
        internal static void InvokeTestCode(DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture fixture, string typeName, string methodName)
        {
            var type = typeof(DefaultAnalyzerAssemblyLoaderTests).Assembly.GetType(typeName, throwOnError: false)!;
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
        public void LoadWithDependency(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var analyzerDependencyFile = testFixture.AnalyzerDependency;
                var analyzerMainFile = testFixture.AnalyzerWithDependency;
                loader.AddDependencyLocation(analyzerDependencyFile.Path);

                var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile.Path, loader);
                analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);
                var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile.Path, loader);
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
        public void AddDependencyLocationThrowsOnNull(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null!));
                Assert.Throws<ArgumentException>("fullPath", () => loader.AddDependencyLocation("a"));
            });
        }

        [Theory]
        [CombinatorialData]
        public void ThrowsForMissingFile(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
                Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
            });
        }

        [Theory]
        [CombinatorialData]
        public void BasicLoad(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Alpha.Path);
                Assembly alpha = loader.LoadFromPath(testFixture.Alpha.Path);

                Assert.NotNull(alpha);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_Multiple(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Alpha.Path);
                loader.AddDependencyLocation(testFixture.Beta.Path);
                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Delta1.Path);

                Assembly alpha = loader.LoadFromPath(testFixture.Alpha.Path);

                var a = alpha.CreateInstance("Alpha.A")!;
                a.GetType().GetMethod("Write")!.Invoke(a, new object[] { sb, "Test A" });

                Assembly beta = loader.LoadFromPath(testFixture.Beta.Path);

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
        public void AssemblyLoading_OverwriteBeforeLoad(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Delta1.Path);
                testFixture.Delta1.WriteAllBytes(testFixture.Delta2.ReadAllBytes());
                var assembly = loader.LoadFromPath(testFixture.Delta1.Path);

                var name = AssemblyName.GetAssemblyName(testFixture.Delta2.Path);
                Assert.Equal(name.FullName, assembly.GetName().FullName);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_AssemblyLocationNotAdded(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Delta1.Path);
                Assert.Throws<InvalidOperationException>(() => loader.LoadFromPath(testFixture.Beta.Path));
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyLocationNotAdded(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Beta.Path);
                Assembly beta = loader.LoadFromPath(testFixture.Beta.Path);

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
                    // In .NET Framework we cannot prevent the load of Alpha. Once Beta is loaded 
                    // into the LoadFrom it's directory is added to the probing path for Beta's
                    // dependencies. When Beta causes an implicit load of Alpha the runtime will just
                    // grab it from the probing path and there is no way for us to stop it. It's 
                    // a limitation that we have to accept
                    writeMethod.Invoke(b, new object[] { sb, "Test B" });
                    var actual = sb.ToString();
                    Assert.Equal(@"Delta: Gamma: Beta: Test B
", actual);
                }
            });
        }

        private static void VerifyAssemblies(DefaultAnalyzerAssemblyLoader loader, IEnumerable<Assembly> assemblies, params (string simpleName, string version, string path)[] expected)
        {
            expected = expected
                .Select(x => (x.simpleName, x.version, loader.GetRealLoadPath(x.path)))
                .ToArray();

            Assert.Equal(
                expected,
                Roslyn.Utilities
                    .EnumerableExtensions
                    .Order(assemblies.Select(assembly => (assembly.GetName().Name!, assembly.GetName().Version!.ToString(), assembly.Location)))
                    .ToArray());

            if (loader is ShadowCopyAnalyzerAssemblyLoader shadowLoader)
            {
                Assert.All(assemblies, x => x.Location.StartsWith(shadowLoader.BaseDirectory, StringComparison.Ordinal));
            }
        }

        private static void VerifyAssemblies(DefaultAnalyzerAssemblyLoader loader, IEnumerable<Assembly> assemblies, params string[] assemblyPaths)
        {
            var data = assemblyPaths
                .Select(x =>
                {
                    var name = AssemblyName.GetAssemblyName(x);
                    return (name.Name!, name.Version?.ToString() ?? "", x);
                })
                .ToArray();
            VerifyAssemblies(loader, assemblies, data);
        }

        /// <summary>
        /// Verify the set of assemblies loaded as analyzer dependencies are the specified assembly paths
        /// </summary>
        private static void VerifyDependencyAssemblies(DefaultAnalyzerAssemblyLoader loader, params string[] assemblyPaths)
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

            static bool isInLoadFromContext(DefaultAnalyzerAssemblyLoader loader, Assembly assembly)
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
            VerifyAssemblies(loader, loadedAssemblies, assemblyPaths);
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_Simple(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1.Path);
                loader.AddDependencyLocation(testFixture.Gamma.Path);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma.Path);
                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.Delta1.Path,
                    testFixture.Gamma.Path);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyInDifferentDirectory(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                StringBuilder sb = new StringBuilder();

                var deltaFile = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var gammaFile = tempDir.CreateDirectory("b").CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);

                loader.AddDependencyLocation(deltaFile.Path);
                loader.AddDependencyLocation(gammaFile.Path);
                Assembly gamma = loader.LoadFromPath(gammaFile.Path);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    deltaFile.Path,
                    gammaFile.Path);
            });
        }

        /// <summary>
        /// Similar to <see cref="AssemblyLoading_DependencyInDifferentDirectory"/> except want to validate
        /// a dependency in the same directory is preferred over one in a different directory.
        /// </summary>
        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DependencyInDifferentDirectory2(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();

                // It's important that we create these directories in a deterministic order so that 
                // our test has reliably output. Part of our resolution code will search the registered
                // paths in a sorted order.
                var deltaFile1 = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var tempSubDir = tempDir.CreateDirectory("b");
                var gammaFile = tempSubDir.CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);
                var deltaFile2 = tempSubDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);

                loader.AddDependencyLocation(deltaFile1.Path);
                loader.AddDependencyLocation(deltaFile2.Path);
                loader.AddDependencyLocation(gammaFile.Path);
                Assembly gamma = loader.LoadFromPath(gammaFile.Path);

                StringBuilder sb = new StringBuilder();
                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                if (ExecutionConditionUtil.IsDesktop && loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // In desktop + shadow we lose the ability to related dlls in the same directory
                    VerifyDependencyAssemblies(
                        loader,
                        deltaFile1.Path,
                        gammaFile.Path);
                }
                else
                {
                    VerifyDependencyAssemblies(
                        loader,
                        deltaFile2.Path,
                        gammaFile.Path);

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
        public void AssemblyLoading_DependencyInDifferentDirectory3(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                StringBuilder sb = new StringBuilder();

                var deltaFile = tempDir.CreateDirectory("a").CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var gammaFile = tempDir.CreateDirectory("b").CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);

                loader.AddDependencyLocation(deltaFile.Path);
                loader.AddDependencyLocation(gammaFile.Path);
                Assembly gamma = loader.LoadFromPath(gammaFile.Path);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);

                VerifyDependencyAssemblies(
                    loader,
                    deltaFile.Path,
                    gammaFile.Path);
            });
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void AssemblyLoading_DependencyInDifferentDirectory4(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var analyzerDependencyFile = testFixture.AnalyzerDependency;
                var analyzerMainFile = testFixture.AnalyzerWithDependency;

                var analyzerMainReference = new AnalyzerFileReference(analyzerMainFile.Path, loader);
                analyzerMainReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);
                var analyzerDependencyReference = new AnalyzerFileReference(analyzerDependencyFile.Path, loader);
                analyzerDependencyReference.AnalyzerLoadFailed += (_, e) => AssertEx.Fail(e.Exception!.Message);

                Assert.True(loader.IsAnalyzerDependencyPath(analyzerMainFile.Path));
                Assert.True(loader.IsAnalyzerDependencyPath(analyzerDependencyFile.Path));

                var analyzers = analyzerMainReference.GetAnalyzersForAllLanguages();
                Assert.Equal(1, analyzers.Length);
                Assert.Equal("TestAnalyzer", analyzers[0].ToString());
                Assert.Equal(0, analyzerDependencyReference.GetAnalyzersForAllLanguages().Length);
                Assert.NotNull(analyzerDependencyReference.GetAssembly());

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.AnalyzerWithDependency.Path,
                    testFixture.AnalyzerDependency.Path);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Delta1.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);
                loader.AddDependencyLocation(testFixture.Delta2.Path);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma.Path);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

#if NETCOREAPP
                var alcs = loader.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(2, alcs.Length);

                VerifyAssemblies(
                    loader,
                    alcs[0].Assemblies,
                    ("Delta", "1.0.0.0", testFixture.Delta1.Path),
                    ("Gamma", "0.0.0.0", testFixture.Gamma.Path)
                );

                VerifyAssemblies(
                    loader,
                    alcs[1].Assemblies,
                    ("Delta", "2.0.0.0", testFixture.Delta2.Path),
                    ("Epsilon", "0.0.0.0", testFixture.Epsilon.Path));
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
        public void AssemblyLoading_MultipleVersions_NoExactMatch(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);
                loader.AddDependencyLocation(testFixture.Delta3.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
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
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta3.Path,
                        testFixture.Epsilon.Path);
                    Assert.Equal(
@"Delta.3: Epsilon: Test E
",
                    actual);
                }
                else
                {
                    // The Epsilon.dll has Delta.dll (v2) next to it in the directory. The .NET Framework 
                    // will implicitly load this due to normal probing rules. No way for us to intercept
                    // this and we end up with v2 here where it wasn't specified as a dependency.
                    Assert.Throws<InvalidOperationException>(() => loader.GetRealLoadPath(testFixture.Delta2.Path));

                    // Fake the dependency so we can verify the rest of the load
                    loader.AddDependencyLocation(testFixture.Delta2.Path);
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2.Path,
                        testFixture.Epsilon.Path);

                    Assert.Equal(
    @"Delta.2: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_MultipleEqualMatches(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B.Path);
                loader.AddDependencyLocation(testFixture.Delta2.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                if (ExecutionConditionUtil.IsDesktop && loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    // Delta2B and Delta2 have the same version, but we prefer Delta2B because it's added first and 
                    // in shadow loader we can't fall back to same directory because the runtime doesn't provide
                    // context for who requested the load. Just have to go to best version.
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2B.Path,
                        testFixture.Epsilon.Path);

                    var actual = sb.ToString();
                    Assert.Equal(
    @"Delta.2B: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // Delta2B and Delta2 have the same version, but we prefer Delta2 because it's in the same directory as Epsilon.
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2.Path,
                        testFixture.Epsilon.Path);

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
        public void AssemblyLoading_MultipleVersions_MultipleVersionsOfSameAnalyzerItself(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2.Path);
                loader.AddDependencyLocation(testFixture.Delta2B.Path);

                Assembly delta2 = loader.LoadFromPath(testFixture.Delta2.Path);
                Assembly delta2B = loader.LoadFromPath(testFixture.Delta2B.Path);

                // 2B or not 2B? That is the question...that depends on whether we're on .NET Core or not.

#if NETCOREAPP

                // On Core, we're able to load both of these into separate AssemblyLoadContexts.
                Assert.NotEqual(delta2B.Location, delta2.Location);
                Assert.Equal(loader.GetRealLoadPath(testFixture.Delta2.Path), delta2.Location);
                Assert.Equal(loader.GetRealLoadPath(testFixture.Delta2B.Path), delta2B.Location);

#else

                // In non-core, we cache by assembly identity; since we don't use multiple AppDomains we have no
                // way to load different assemblies with the same identity, no matter what. Thus, we'll get the
                // same assembly for both of these.
                Assert.Same(delta2B, delta2);
#endif
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_ExactAndGreaterMatch(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B.Path);
                loader.AddDependencyLocation(testFixture.Delta3.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
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
                        testFixture.Delta2B.Path,
                        testFixture.Epsilon.Path);

                    Assert.Equal(
    @"Delta.2B: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // This is another case where the private probing path wins on .NET framework and
                    // there is no way for us to work around it. It works in shadow copying because 
                    // the DLls are not side by side 
                    Assert.Throws<InvalidOperationException>(() => loader.GetRealLoadPath(testFixture.Delta2.Path));

                    // Fake the dependency so we can verify the rest of the load
                    loader.AddDependencyLocation(testFixture.Delta2.Path);
                    VerifyDependencyAssemblies(
                        loader,
                        testFixture.Delta2.Path,
                        testFixture.Epsilon.Path);

                    Assert.Equal(
                        @"Delta.2: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_MultipleVersions_WorseMatchInSameDirectory(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var epsilonFile = tempDir.CreateFile("Epsilon.dll").CopyContentFrom(testFixture.Epsilon.Path);
                var delta1File = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);

                loader.AddDependencyLocation(delta1File.Path);
                loader.AddDependencyLocation(testFixture.Delta2.Path);
                loader.AddDependencyLocation(epsilonFile.Path);

                Assembly epsilon = loader.LoadFromPath(epsilonFile.Path);
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
                        testFixture.Delta2.Path,
                        epsilonFile.Path);

                    var actual = sb.ToString();
                    Assert.Equal(
        @"Delta.2: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    // Epsilon wants Delta2, but since Delta1 is in the same directory, we prefer Delta1 over Delta2.
                    VerifyDependencyAssemblies(
                        loader,
                        delta1File.Path,
                        epsilonFile.Path);

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
        public void AssemblyLoading_MultipleVersions_MultipleLoaders(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader1, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader1.AddDependencyLocation(testFixture.Gamma.Path);
                loader1.AddDependencyLocation(testFixture.Delta1.Path);

                var loader2 = new DefaultAnalyzerAssemblyLoader();
                loader2.AddDependencyLocation(testFixture.Epsilon.Path);
                loader2.AddDependencyLocation(testFixture.Delta2.Path);

                Assembly gamma = loader1.LoadFromPath(testFixture.Gamma.Path);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader2.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

#if NETCOREAPP
                var alcs1 = loader1.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(1, alcs1.Length);

                VerifyAssemblies(
                    loader1,
                    alcs1[0].Assemblies,
                    ("Delta", "1.0.0.0", testFixture.Delta1.Path),
                    ("Gamma", "0.0.0.0", testFixture.Gamma.Path));

                var alcs2 = loader2.GetDirectoryLoadContextsSnapshot();
                Assert.Equal(1, alcs2.Length);

                VerifyAssemblies(
                    loader2,
                    alcs2[0].Assemblies,
                    ("Delta", "2.0.0.0", testFixture.Delta2.Path),
                    ("Epsilon", "0.0.0.0", testFixture.Epsilon.Path));
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
        public void AssemblyLoading_MultipleVersions_MissingVersion(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Delta1.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);

                Assembly gamma = loader.LoadFromPath(testFixture.Gamma.Path);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
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
        public void AssemblyLoading_UnifyToHighest(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var sb = new StringBuilder();

                // Gamma depends on Delta v1, and Epsilon depends on Delta v2. But both should load
                // and both use Delta v2. We intentionally here are not adding a reference to Delta1, since
                // this test is testing what happens if it's not present. A simple example for this scenario
                // is an analyzer that depends on both Gamma and Epsilon; an analyzer package can't reasonably
                // package both Delta v1 and Delta v2, so it'll only package the highest and things should work.
                loader.AddDependencyLocation(testFixture.GammaReferencingPublicSigned.Path);
                loader.AddDependencyLocation(testFixture.EpsilonReferencingPublicSigned.Path);
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned2.Path);

                var gamma = loader.LoadFromPath(testFixture.GammaReferencingPublicSigned.Path);
                var g = gamma.CreateInstance("Gamma.G")!;
                g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

                var epsilon = loader.LoadFromPath(testFixture.EpsilonReferencingPublicSigned.Path);
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
        public void AssemblyLoading_CanLoadDifferentVersionsDirectly(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var sb = new StringBuilder();

                // Ensure that no matter what, if we have two analyzers of different versions, we never unify them.
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned1.Path);
                loader.AddDependencyLocation(testFixture.DeltaPublicSigned2.Path);

                var delta1Assembly = loader.LoadFromPath(testFixture.DeltaPublicSigned1.Path);
                var delta1Instance = delta1Assembly.CreateInstance("Delta.D")!;
                delta1Instance.GetType().GetMethod("Write")!.Invoke(delta1Instance, new object[] { sb, "Test D1" });

                var delta2Assembly = loader.LoadFromPath(testFixture.DeltaPublicSigned2.Path);
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
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_01(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable.Path);
                loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);
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
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_02(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable.Path);
                loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable2.Path);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable2.Path);
                var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
                analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
                Assert.Equal(ExecutionConditionUtil.IsCoreClr ? "1" : "42", sb.ToString());
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_CompilerDependencyDuplicated(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var assembly = typeof(ImmutableArray<int>).Assembly;

                // Copy the dependency to a new location to simulate it being deployed by an 
                // analyzer / generator
                using var tempRoot = new TempRoot();
                var destFile = tempRoot.CreateDirectory().CreateOrOpenFile($"{assembly.GetName().Name}.dll");
                destFile.CopyContentFrom(assembly.Location);
                loader.AddDependencyLocation(destFile.Path);

                var copiedAssembly = loader.LoadFromPath(destFile.Path);
                Assert.Single(AppDomain.CurrentDomain.GetAssemblies().Where(x => x.FullName == assembly.FullName));
                Assert.Same(copiedAssembly, assembly);
            });
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [CombinatorialData]
        public void AssemblyLoading_NativeDependency(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                const int INVALID_FILE_ATTRIBUTES = -1;
                loader.AddDependencyLocation(testFixture.AnalyzerWithNativeDependency.Path);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerWithNativeDependency.Path);
                var analyzer = analyzerAssembly.CreateInstance("Class1")!;
                var result = analyzer.GetType().GetMethod("GetFileAttributes")!.Invoke(analyzer, new[] { testFixture.AnalyzerWithNativeDependency.Path });
                Assert.NotEqual(INVALID_FILE_ATTRIBUTES, result);
                Assert.Equal(FileAttributes.Archive, (FileAttributes)result!);
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DeleteAfterLoad1(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1.Path);
                _ = loader.LoadFromPath(testFixture.Delta1.Path);

                if (loader is ShadowCopyAnalyzerAssemblyLoader || !ExecutionConditionUtil.IsWindows)
                {
                    File.Delete(testFixture.Delta1.Path);
                }
                else
                {
                    Assert.Throws<UnauthorizedAccessException>(() => File.Delete(testFixture.Delta1.Path));
                }
            });
        }

        [Theory]
        [CombinatorialData]
        public void AssemblyLoading_DeleteAfterLoad2(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                loader.AddDependencyLocation(deltaCopy.Path);
                Assembly? delta = loader.LoadFromPath(deltaCopy.Path);

                if (loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    File.Delete(deltaCopy.Path);
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
        public void AssemblyLoading_DeleteAfterLoad3(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                var tempDir = temp.CreateDirectory();
                var sb = new StringBuilder();

                var tempDir1 = tempDir.CreateDirectory("a");
                var tempDir2 = tempDir.CreateDirectory("b");
                var tempDir3 = tempDir.CreateDirectory("c");

                var delta1File = tempDir1.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var delta2File = tempDir2.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta2.Path);
                var gammaFile = tempDir3.CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);

                loader.AddDependencyLocation(delta1File.Path);
                loader.AddDependencyLocation(delta2File.Path);
                loader.AddDependencyLocation(gammaFile.Path);
                Assembly gamma = loader.LoadFromPath(gammaFile.Path);

                var b = gamma.CreateInstance("Gamma.G")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                writeMethod.Invoke(b, new object[] { sb, "Test G" });

                if (loader is ShadowCopyAnalyzerAssemblyLoader)
                {
                    File.Delete(delta1File.Path);
                    File.Delete(delta2File.Path);
                    File.Delete(gammaFile.Path);
                }

                var actual = sb.ToString();
                Assert.Equal(@"Delta: Gamma: Test G
", actual);
            });
        }

#if NETCOREAPP

        [Theory]
        [CombinatorialData]
        public void AssemblyLoadingInNonDefaultContext_AnalyzerReferencesSystemCollectionsImmutable(bool shadowLoad)
        {
            Run(shadowLoad, static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                // Create a separate ALC as the compiler context, load the compiler assembly and a modified version of S.C.I into it,
                // then use that to load and run `AssemblyLoadingInNonDefaultContextHelper1` below. We expect the analyzer running in
                // its own `DirectoryLoadContext` would use the bogus S.C.I loaded in the compiler load context instead of the real one
                // in the default context.
                var compilerContext = loader.CompilerLoadContext;
                _ = compilerContext.LoadFromAssemblyPath(testFixture.UserSystemCollectionsImmutable.Path);
                _ = compilerContext.LoadFromAssemblyPath(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location);

                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable.Path);
                loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);

                Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);
                var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
                analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });

                Assert.Equal("42", sb.ToString());
            });
        }
#endif
    }
}
