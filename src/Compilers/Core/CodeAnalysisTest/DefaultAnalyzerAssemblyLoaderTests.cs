// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    public sealed class DefaultAnalyzerAssemblyLoaderTests : TestBase
    {
        private readonly AssemblyLoadTestFixture _testResources;
        public DefaultAnalyzerAssemblyLoaderTests(AssemblyLoadTestFixture testResources)
        {
            _testResources = testResources;
        }

        [Fact]
        public void AddDependencyLocationThrowsOnNull()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();

            Assert.Throws<ArgumentNullException>("fullPath", () => loader.AddDependencyLocation(null));
            Assert.Throws<ArgumentException>("fullPath", () => loader.AddDependencyLocation("a"));
        }

        [Fact]
        public void ThrowsForMissingFile()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");

            var loader = new DefaultAnalyzerAssemblyLoader();

            Assert.ThrowsAny<Exception>(() => loader.LoadFromPath(path));
        }

        [Fact]
        public void BasicLoad()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Alpha.Path);
            Assembly alpha = loader.LoadFromPath(_testResources.Alpha.Path);

            Assert.NotNull(alpha);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Alpha.Path);
            loader.AddDependencyLocation(_testResources.Beta.Path);
            loader.AddDependencyLocation(_testResources.Gamma.Path);
            loader.AddDependencyLocation(_testResources.Delta1.Path);

            Assembly alpha = loader.LoadFromPath(_testResources.Alpha.Path);

            var a = alpha.CreateInstance("Alpha.A")!;
            a.GetType().GetMethod("Write")!.Invoke(a, new object[] { sb, "Test A" });

            Assembly beta = loader.LoadFromPath(_testResources.Beta.Path);

            var b = beta.CreateInstance("Beta.B")!;
            b.GetType().GetMethod("Write")!.Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_AssemblyLocationNotAdded()
        {
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Gamma.Path);
            loader.AddDependencyLocation(_testResources.Delta1.Path);
            Assert.Throws<ArgumentException>(() => loader.LoadFromPath(_testResources.Beta.Path));
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AssemblyLoading_DependencyLocationNotAdded()
        {
            StringBuilder sb = new StringBuilder();
            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Gamma.Path);
            loader.AddDependencyLocation(_testResources.Beta.Path);
            Assembly beta = loader.LoadFromPath(_testResources.Beta.Path);

            var b = beta.CreateInstance("Beta.B")!;
            var writeMethod = b.GetType().GetMethod("Write")!;
            var exception = Assert.Throws<TargetInvocationException>(
                () => writeMethod.Invoke(b, new object[] { sb, "Test B" }));
            Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);

            var actual = sb.ToString();
            Assert.Equal(@"", actual);
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Gamma.Path);
            loader.AddDependencyLocation(_testResources.Delta1.Path);
            loader.AddDependencyLocation(_testResources.Epsilon.Path);
            loader.AddDependencyLocation(_testResources.Delta2.Path);

            Assembly gamma = loader.LoadFromPath(_testResources.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(_testResources.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E")!;
            e.GetType().GetMethod("Write")!.Invoke(e, new object[] { sb, "Test E" });

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
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions_MissingVersion()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(_testResources.Gamma.Path);
            loader.AddDependencyLocation(_testResources.Delta1.Path);
            loader.AddDependencyLocation(_testResources.Epsilon.Path);

            Assembly gamma = loader.LoadFromPath(_testResources.Gamma.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(_testResources.Epsilon.Path);
            var e = epsilon.CreateInstance("Epsilon.E")!;
            var eWrite = e.GetType().GetMethod("Write")!;

            var actual = sb.ToString();
            if (ExecutionConditionUtil.IsCoreClr)
            {
                var exception = Assert.Throws<TargetInvocationException>(() => eWrite.Invoke(e, new object[] { sb, "Test E" }));
                Assert.IsAssignableFrom<FileNotFoundException>(exception.InnerException);
            }
            else
            {
                eWrite.Invoke(e, new object[] { sb, "Test E" });
                Assert.Equal(
@"Delta: Gamma: Test G
",
                    actual);
            }
        }
    }
}
