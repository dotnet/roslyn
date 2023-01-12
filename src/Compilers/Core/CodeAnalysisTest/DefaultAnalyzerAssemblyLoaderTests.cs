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
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    public sealed class DefaultAnalyzerAssemblyLoaderTests : TestBase
    {
#if NETCOREAPP
        private sealed class InvokeUtil
        {
            public void Exec(string typeName, string methodName) => InvokeTestCode(typeName, methodName);
        }
#else
        private sealed class InvokeUtil : MarshalByRefObject
        {
            public void Exec(string typeName, string methodName)
            {
                try
                {
                    InvokeTestCode(typeName, methodName);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is XunitException)
                {
                    var inner = ex.InnerException;
                    throw new Exception(inner.Message + inner.StackTrace);
                }
            }
        }
#endif

        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel);
        private readonly ITestOutputHelper _output;

        public DefaultAnalyzerAssemblyLoaderTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private void Run(Action<DefaultAnalyzerAssemblyLoader, AssemblyLoadTestFixture> action, [CallerMemberName] string? memberName = null)
        {
#if NETCOREAPP
            var alc = AssemblyLoadContextUtils.Create($"Test {memberName}");
            var assembly = alc.LoadFromAssemblyName(typeof(InvokeUtil).Assembly.GetName());
            var util = assembly.CreateInstance(typeof(InvokeUtil).FullName)!;
            var method = util.GetType().GetMethod("Exec", BindingFlags.Public | BindingFlags.Instance)!;
            method.Invoke(util, new object[] { action.Method.DeclaringType!.FullName!, action.Method.Name });

#else
            AppDomain? appDomain = null;
            try
            {
                appDomain = AppDomainUtils.Create($"Test {memberName}");
                var type = typeof(InvokeUtil);
                var util = (InvokeUtil)appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
                util.Exec(action.Method.DeclaringType.FullName, action.Method.Name);
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
        private static void InvokeTestCode(string typeName, string methodName)
        {
            var type = typeof(DefaultAnalyzerAssemblyLoaderTests).Assembly.GetType(typeName, throwOnError: false)!;
            var member = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;

            // A static lambda will still be an instance method so we need to create the closure
            // here.
            var obj = member.IsStatic
                ? null
                : type.Assembly.CreateInstance(typeName);

            using var fixture = new AssemblyLoadTestFixture();
            var loader = new DefaultAnalyzerAssemblyLoader();
            member.Invoke(obj, new object[] { loader, fixture });
        }

        [Fact, WorkItem(32226, "https://github.com/dotnet/roslyn/issues/32226")]
        public void LoadWithDependency()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AddDependencyLocationThrowsOnNull()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null!));
                Assert.Throws<ArgumentException>("fullPath", () => loader.AddDependencyLocation("a"));
            });
        }

        [Fact]
        public void ThrowsForMissingFile()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
                Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
            });
        }

        [Fact]
        public void BasicLoad()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Alpha.Path);
                Assembly alpha = loader.LoadFromPath(testFixture.Alpha.Path);

                Assert.NotNull(alpha);
            });
        }

        [Fact]
        public void AssemblyLoading()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_AssemblyLocationNotAdded()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Delta1.Path);
                Assert.Throws<FileNotFoundException>(() => loader.LoadFromPath(testFixture.Beta.Path));
            });
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_DependencyLocationNotAdded()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                // We don't pass Alpha's path to AddDependencyLocation here, and therefore expect
                // calling Beta.B.Write to fail.
                loader.AddDependencyLocation(testFixture.Gamma.Path);
                loader.AddDependencyLocation(testFixture.Beta.Path);
                Assembly beta = loader.LoadFromPath(testFixture.Beta.Path);

                var b = beta.CreateInstance("Beta.B")!;
                var writeMethod = b.GetType().GetMethod("Write")!;
                var exception = Assert.Throws<TargetInvocationException>(
                    () => writeMethod.Invoke(b, new object[] { sb, "Test B" }));
                Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);

                var actual = sb.ToString();
                Assert.Equal(@"", actual);
            });
        }

        private static void VerifyAssemblies(IEnumerable<Assembly> assemblies, params (string simpleName, string version, string path)[] expected)
        {
            Assert.Equal(expected, Roslyn.Utilities.EnumerableExtensions.Order(assemblies.Select(assembly => (assembly.GetName().Name!, assembly.GetName().Version!.ToString(), assembly.Location))));
        }

        /// <summary>
        /// Verify the set of asesmblies loaded as analyzer dependencies are the specified assembly paths
        /// </summary>
        private static void VerifyDependencyAssemblies(DefaultAnalyzerAssemblyLoader loader, params string[] assemblyPaths)
        {
            IEnumerable<Assembly> loadedAssemblies;

#if NETCOREAPP
            // This verify only works where there is a single load context.
            var alcs = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader);
            Assert.Equal(1, alcs.Length);

            loadedAssemblies = alcs[0].Assemblies;
#else

            // Unfortunately in .NET Framework we cannot determine which Assemblies are in the LoadFrom context
            // which is what we want here. Our ability to verify here is less robust and instead we filter out 
            // all the DLLs we know are simply a part of the application and examine what is left.
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
            var data = assemblyPaths
                .Select(x =>
                {
                    var name = AssemblyName.GetAssemblyName(x);
                    return (name.Name!, name.Version?.ToString() ?? "", x);
                })
                .ToArray();

            VerifyAssemblies(loadedAssemblies, data);
        }

        [Fact]
        public void AssemblyLoading_DependencyInDifferentDirectory()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var deltaFile = temp.CreateDirectory().CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var gammaFile = temp.CreateDirectory().CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);

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
        [Fact]
        public void AssemblyLoading_DependencyInDifferentDirectory2()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var deltaFile1 = temp.CreateDirectory().CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var tempDir = temp.CreateDirectory();
                var gammaFile = tempDir.CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);
                var deltaFile2 = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);

                loader.AddDependencyLocation(deltaFile1.Path);
                loader.AddDependencyLocation(deltaFile2.Path);
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
                    deltaFile2.Path,
                    gammaFile.Path);
            });
        }

        /// <summary>
        /// This is very similar to <see cref="AssemblyLoading_DependencyInDifferentDirectory2"/> except 
        /// that we ensure the code does not prefer a dependency in the same directory if it's 
        /// unregistered
        /// </summary>
        [Fact]
        public void AssemblyLoading_DependencyInDifferentDirectory3()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var deltaFile = temp.CreateDirectory().CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                var gammaFile = temp.CreateDirectory().CreateFile("Gamma.dll").CopyContentFrom(testFixture.Gamma.Path);

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

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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
                var alcs = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader);
                Assert.Equal(2, alcs.Length);

                VerifyAssemblies(
                    alcs[0].Assemblies,
                    ("Delta", "1.0.0.0", testFixture.Delta1.Path),
                    ("Gamma", "0.0.0.0", testFixture.Gamma.Path)
                );

                VerifyAssemblies(
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

        [Fact]
        public void AssemblyLoading_MultipleVersions_NoExactMatch()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta1.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);
                loader.AddDependencyLocation(testFixture.Delta3.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr)
                {
                    // In .NET Core we have _full_ control over assembly loading and can prevent implicit
                    // loads from probing paths. That means we can avoid implicitly loading the Delta v2 
                    // next to Epsilon
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

        [Fact]
        public void AssemblyLoading_MultipleVersions_MultipleEqualMatches()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                // Delta2B and Delta2 have the same version, but we prefer Delta2 because it's in the same directory as Epsilon.
                loader.AddDependencyLocation(testFixture.Delta2B.Path);
                loader.AddDependencyLocation(testFixture.Delta2.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.Delta2.Path,
                    testFixture.Epsilon.Path);

                var actual = sb.ToString();
                Assert.Equal(
@"Delta.2: Epsilon: Test E
",
                    actual);
            });
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions_MultipleVersionsOfSameAnalyzerItself()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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
                Assert.Equal(testFixture.Delta2.Path, delta2.Location);
                Assert.Equal(testFixture.Delta2B.Path, delta2B.Location);

#else

                // In non-core, we cache by assembly identity; since we don't use multiple AppDomains we have no
                // way to load different assemblies with the same identity, no matter what. Thus, we'll get the
                // same assembly for both of these.
                Assert.Same(delta2B, delta2);
#endif
            });
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/60763")]
        public void AssemblyLoading_MultipleVersions_ExactAndGreaterMatch()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                StringBuilder sb = new StringBuilder();

                loader.AddDependencyLocation(testFixture.Delta2B.Path);
                loader.AddDependencyLocation(testFixture.Delta3.Path);
                loader.AddDependencyLocation(testFixture.Epsilon.Path);

                Assembly epsilon = loader.LoadFromPath(testFixture.Epsilon.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                VerifyDependencyAssemblies(
                    loader,
                    testFixture.Delta2B.Path,
                    testFixture.Epsilon.Path);

                var actual = sb.ToString();
                if (ExecutionConditionUtil.IsCoreClr)
                {
                    Assert.Equal(
    @"Delta.2B: Epsilon: Test E
",
                        actual);
                }
                else
                {
                    Assert.Equal(
    @"Delta: Epsilon: Test E
",
                        actual);
                }
            });
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions_WorseMatchInSameDirectory()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var epsilonFile = tempDir.CreateFile("Epsilon.dll").CopyContentFrom(testFixture.Epsilon.Path);
                var delta1File = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);

                // Epsilon wants Delta2, but since Delta1 is in the same directory, we prefer Delta1 over Delta2.
                // This is because the CLR will see it first and load it, without giving us any chance to redirect
                // in the AssemblyResolve hook.
                loader.AddDependencyLocation(delta1File.Path);
                loader.AddDependencyLocation(testFixture.Delta2.Path);
                loader.AddDependencyLocation(epsilonFile.Path);

                Assembly epsilon = loader.LoadFromPath(epsilonFile.Path);
                var e = epsilon.CreateInstance("Epsilon.E")!;
                e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

                VerifyDependencyAssemblies(
                    loader,
                    delta1File.Path,
                    epsilonFile.Path);

                var actual = sb.ToString();
                Assert.Equal(
    @"Delta: Epsilon: Test E
",
                    actual);
            });
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions_MultipleLoaders()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader1, AssemblyLoadTestFixture testFixture) =>
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
                var alcs1 = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader1);
                Assert.Equal(1, alcs1.Length);

                VerifyAssemblies(
                    alcs1[0].Assemblies,
                    ("Delta", "1.0.0.0", testFixture.Delta1.Path),
                    ("Gamma", "0.0.0.0", testFixture.Gamma.Path));

                var alcs2 = DefaultAnalyzerAssemblyLoader.TestAccessor.GetOrderedLoadContexts(loader2);
                Assert.Equal(1, alcs2.Length);

                VerifyAssemblies(
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

        [Fact]
        public void AssemblyLoading_MultipleVersions_MissingVersion()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AssemblyLoading_UnifyToHighest()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AssemblyLoading_CanLoadDifferentVersionsDirectly()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_01()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_02()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/66104")]
        public void AssemblyLoading_CompilerDependencyDuplicated()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void AssemblyLoading_NativeDependency()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
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

        [Fact]
        public void AssemblyLoading_Delete()
        {
            Run(static (DefaultAnalyzerAssemblyLoader loader, AssemblyLoadTestFixture testFixture) =>
            {
                using var temp = new TempRoot();
                StringBuilder sb = new StringBuilder();

                var tempDir = temp.CreateDirectory();
                var deltaCopy = tempDir.CreateFile("Delta.dll").CopyContentFrom(testFixture.Delta1.Path);
                loader.AddDependencyLocation(deltaCopy.Path);
                Assembly delta = loader.LoadFromPath(deltaCopy.Path);

                try
                {
                    File.Delete(deltaCopy.Path);
                }
                catch (UnauthorizedAccessException)
                {
                    return;
                }

                // The above call may or may not throw depending on the platform configuration.
                // If it doesn't throw, we might as well check that things are still functioning reasonably.

                var d = delta.CreateInstance("Delta.D");
                d!.GetType().GetMethod("Write")!.Invoke(d, new object[] { sb, "Test D" });

                var actual = sb.ToString();
                Assert.Equal(
    @"Delta: Test D
",
                    actual);
            });
        }

#if NETCOREAPP
        [Fact]
        public void VerifyCompilerAssemblySimpleNames()
        {
            var caAssembly = typeof(Microsoft.CodeAnalysis.SyntaxNode).Assembly;
            var caReferences = caAssembly.GetReferencedAssemblies();
            var allReferenceSimpleNames = ArrayBuilder<string>.GetInstance();
            allReferenceSimpleNames.Add(caAssembly.GetName().Name ?? throw new InvalidOperationException());
            foreach (var reference in caReferences)
            {
                allReferenceSimpleNames.Add(reference.Name ?? throw new InvalidOperationException());
            }

            var csAssembly = typeof(Microsoft.CodeAnalysis.CSharp.CSharpSyntaxNode).Assembly;
            allReferenceSimpleNames.Add(csAssembly.GetName().Name ?? throw new InvalidOperationException());
            var csReferences = csAssembly.GetReferencedAssemblies();
            foreach (var reference in csReferences)
            {
                var name = reference.Name ?? throw new InvalidOperationException();
                if (!allReferenceSimpleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    allReferenceSimpleNames.Add(name);
                }
            }

            var vbAssembly = typeof(Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode).Assembly;
            var vbReferences = vbAssembly.GetReferencedAssemblies();
            allReferenceSimpleNames.Add(vbAssembly.GetName().Name ?? throw new InvalidOperationException());
            foreach (var reference in vbReferences)
            {
                var name = reference.Name ?? throw new InvalidOperationException();
                if (!allReferenceSimpleNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    allReferenceSimpleNames.Add(name);
                }
            }

            if (!DefaultAnalyzerAssemblyLoader.CompilerAssemblySimpleNames.SetEquals(allReferenceSimpleNames))
            {
                allReferenceSimpleNames.Sort();
                var allNames = string.Join(",\r\n                ", allReferenceSimpleNames.Select(name => $@"""{name}"""));
                _output.WriteLine("        internal static readonly ImmutableHashSet<string> CompilerAssemblySimpleNames =");
                _output.WriteLine("            ImmutableHashSet.Create(");
                _output.WriteLine("                StringComparer.OrdinalIgnoreCase,");
                _output.WriteLine($"                {allNames});");
                allReferenceSimpleNames.Free();
                Assert.True(false, $"{nameof(DefaultAnalyzerAssemblyLoader)}.{nameof(DefaultAnalyzerAssemblyLoader.CompilerAssemblySimpleNames)} is not up to date. Paste in the standard output of this test to update it.");
            }
            else
            {
                allReferenceSimpleNames.Free();
            }
        }

        [Fact]
        public void AssemblyLoadingInNonDefaultContext_AnalyzerReferencesSystemCollectionsImmutable()
        {
            using var testFixture = new AssemblyLoadTestFixture();

            // Create a separate ALC as the compiler context, load the compiler assembly and a modified version of S.C.I into it,
            // then use that to load and run `AssemblyLoadingInNonDefaultContextHelper1` below. We expect the analyzer running in
            // its own `DirectoryLoadContext` would use the bogus S.C.I loaded in the compiler load context instead of the real one
            // in the default context.
            var compilerContext = new System.Runtime.Loader.AssemblyLoadContext("compilerContext");
            _ = compilerContext.LoadFromAssemblyPath(testFixture.UserSystemCollectionsImmutable.Path);
            _ = compilerContext.LoadFromAssemblyPath(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location);

            var testAssembly = compilerContext.LoadFromAssemblyPath(typeof(DefaultAnalyzerAssemblyLoaderTests).GetTypeInfo().Assembly.Location);
            var testObject = testAssembly.CreateInstance(typeof(DefaultAnalyzerAssemblyLoaderTests).FullName!,
                ignoreCase: false, BindingFlags.Default, binder: null, args: new object[] { _output, }, null, null)!;

            StringBuilder sb = new StringBuilder();
            testObject.GetType().GetMethod(nameof(AssemblyLoadingInNonDefaultContextHelper1), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(testObject, new object[] { sb });
            Assert.Equal("42", sb.ToString());
        }

        // This helper does the same thing as in `AssemblyLoading_AnalyzerReferencesSystemCollectionsImmutable_01` test above except the assertions.
        private void AssemblyLoadingInNonDefaultContextHelper1(StringBuilder sb)
        {
            using var testFixture = new AssemblyLoadTestFixture();
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(testFixture.UserSystemCollectionsImmutable.Path);
            loader.AddDependencyLocation(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);

            Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesSystemCollectionsImmutable1.Path);
            var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
            analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
        }

        [Fact]
        public void AssemblyLoadingInNonDefaultContext_AnalyzerReferencesNonCompilerAssemblyUsedByDefaultContext()
        {
            using var testFixture = new AssemblyLoadTestFixture();
            // Load the V2 of Delta to default ALC, then create a separate ALC for compiler and load compiler assembly.
            // Next use compiler context to load and run `AssemblyLoadingInNonDefaultContextHelper2` below. We expect the analyzer running in
            // its own `DirectoryLoadContext` would load and use Delta V1 located in its directory instead of V2 already loaded in the default context.
            _ = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(testFixture.Delta2.Path);
            var compilerContext = new System.Runtime.Loader.AssemblyLoadContext("compilerContext");
            _ = compilerContext.LoadFromAssemblyPath(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location);

            var testAssembly = compilerContext.LoadFromAssemblyPath(typeof(DefaultAnalyzerAssemblyLoaderTests).GetTypeInfo().Assembly.Location);
            var testObject = testAssembly.CreateInstance(typeof(DefaultAnalyzerAssemblyLoaderTests).FullName!,
                ignoreCase: false, BindingFlags.Default, binder: null, args: new object[] { _output }, null, null)!;

            StringBuilder sb = new StringBuilder();
            testObject.GetType().GetMethod(nameof(AssemblyLoadingInNonDefaultContextHelper2), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(testObject, new object[] { sb });
            Assert.Equal(
@"Delta: Hello
",
                sb.ToString());
        }

        private void AssemblyLoadingInNonDefaultContextHelper2(StringBuilder sb)
        {
            using var testFixture = new AssemblyLoadTestFixture();
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(testFixture.AnalyzerReferencesDelta1.Path);
            loader.AddDependencyLocation(testFixture.Delta1.Path);

            Assembly analyzerAssembly = loader.LoadFromPath(testFixture.AnalyzerReferencesDelta1.Path);
            var analyzer = analyzerAssembly.CreateInstance("Analyzer")!;
            analyzer.GetType().GetMethod("Method")!.Invoke(analyzer, new object[] { sb });
        }
#endif
    }
}
