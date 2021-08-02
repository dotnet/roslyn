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

    public class AssemblyLoadTestFixture : IDisposable
    {
        private static readonly CSharpCompilationOptions s_dllWithMaxWarningLevel = new(OutputKind.DynamicallyLinkedLibrary, warningLevel: CodeAnalysis.Diagnostic.MaxWarningLevel);

        private readonly TempRoot _temp;
        private readonly TempDirectory _directory;

        public TempFile Delta1 { get; }
        public TempFile Gamma { get; }
        public TempFile Beta { get; }
        public TempFile Alpha { get; }

        public TempFile Delta2 { get; }
        public TempFile Epsilon { get; }

        public AssemblyLoadTestFixture()
        {
            _temp = new TempRoot();
            _directory = _temp.CreateDirectory();

            Delta1 = GenerateDll("Delta", _directory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""1.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta: "" + s);
        }
    }
}
");
            var delta1Reference = MetadataReference.CreateFromFile(Delta1.Path);
            Gamma = GenerateDll("Gamma", _directory, @"
using System.Text;
using Delta;

namespace Gamma
{
    public class G
    {
        public void Write(StringBuilder sb, string s)
        {
            D d = new D();

            d.Write(sb, ""Gamma: "" + s);
        }
    }
}
", delta1Reference);

            var gammaReference = MetadataReference.CreateFromFile(Gamma.Path);
            Beta = GenerateDll("Beta", _directory, @"
using System.Text;
using Gamma;

namespace Beta
{
    public class B
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();

            g.Write(sb, ""Beta: "" + s);
        }
    }
}
", gammaReference);

            Alpha = GenerateDll("Alpha", _directory, @"
using System.Text;
using Gamma;

namespace Alpha
{
    public class A
    {
        public void Write(StringBuilder sb, string s)
        {
            G g = new G();
            g.Write(sb, ""Alpha: "" + s);
        }
    }
}
", gammaReference);

            var v2Directory = _directory.CreateDirectory("Version2");
            Delta2 = GenerateDll("Delta", v2Directory, @"
using System.Text;

[assembly: System.Reflection.AssemblyTitle(""Delta"")]
[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]

namespace Delta
{
    public class D
    {
        public void Write(StringBuilder sb, string s)
        {
            sb.AppendLine(""Delta.2: "" + s);
        }
    }
}
");
            var delta2Reference = MetadataReference.CreateFromFile(Delta2.Path);
            Epsilon = GenerateDll("Epsilon", v2Directory, @"
using System.Text;
using Delta;

namespace Epsilon
{
    public class E
    {
        public void Write(StringBuilder sb, string s)
        {
            D d = new D();

            d.Write(sb, ""Epsilon: "" + s);
        }
    }
}
", delta2Reference);
        }


        private static TempFile GenerateDll(string assemblyName, TempDirectory directory, string csSource, params MetadataReference[] additionalReferences)
        {
            var analyzerDependencyCompilation = CSharpCompilation.Create(
                assemblyName: assemblyName,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(csSource) },
                references: new MetadataReference[]
                {
                    TestMetadata.NetStandard20.mscorlib,
                    TestMetadata.NetStandard20.netstandard,
                    TestMetadata.NetStandard20.SystemRuntime
                }.Concat(additionalReferences),
                options: s_dllWithMaxWarningLevel);

            var tempFile = directory.CreateFile($"{assemblyName}.dll");
            tempFile.WriteAllBytes(analyzerDependencyCompilation.EmitToArray());
            return tempFile;
        }

        public void Dispose()
        {
            _temp.Dispose();
        }
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
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);

            var loader = new DefaultAnalyzerAssemblyLoader();

            Assembly alpha = loader.LoadFromPath(alphaDll.Path);

            Assert.NotNull(alpha);
        }

        [Fact]
        public void AssemblyLoading()
        {
            StringBuilder sb = new StringBuilder();

            var loader = new DefaultAnalyzerAssemblyLoader();
            // loader.AddDependencyLocation(alphaDll.Path);
            // loader.AddDependencyLocation(betaDll.Path);
            // loader.AddDependencyLocation(gammaDll.Path);
            // loader.AddDependencyLocation(deltaDll.Path);

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

        [Fact]
        public void AssemblyLoading_DependencyLocationNotAdded()
        {
            StringBuilder sb = new StringBuilder();
            var directory = Temp.CreateDirectory();

            var alphaDll = directory.CreateFile("Alpha.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Alpha);
            var betaDll = directory.CreateFile("Beta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Beta);
            var gammaDll = directory.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var deltaDll = directory.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta1);

            var loader = new DefaultAnalyzerAssemblyLoader();
            //loader.AddDependencyLocation(alphaDll.Path);
            //loader.AddDependencyLocation(betaDll.Path);
            //loader.AddDependencyLocation(gammaDll.Path);
            //loader.AddDependencyLocation(deltaDll.Path);

            Assembly alpha = loader.LoadFromPath(alphaDll.Path);

            var a = alpha.CreateInstance("Alpha.A")!;
            a.GetType().GetMethod("Write")!.Invoke(a, new object[] { sb, "Test A" });

            Assembly beta = loader.LoadFromPath(betaDll.Path);

            var b = beta.CreateInstance("Beta.B")!;
            b.GetType().GetMethod("Write")!.Invoke(b, new object[] { sb, "Test B" });

            var expected = @"Delta: Gamma: Alpha: Test A
Delta: Gamma: Beta: Test B
";

            var actual = sb.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void AssemblyLoading_MultipleVersions()
        {
            StringBuilder sb = new StringBuilder();

            var path1 = Temp.CreateDirectory();
            var gammaDll = path1.CreateFile("Gamma.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Gamma);
            var delta1Dll = path1.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta1);

            var path2 = Temp.CreateDirectory();
            var epsilonDll = path2.CreateFile("Epsilon.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Epsilon);
            var delta2Dll = path2.CreateFile("Delta.dll").WriteAllBytes(TestResources.AssemblyLoadTests.Delta2);

            var loader = new DefaultAnalyzerAssemblyLoader();
            loader.AddDependencyLocation(gammaDll.Path);
            loader.AddDependencyLocation(delta1Dll.Path);
            loader.AddDependencyLocation(epsilonDll.Path);
            loader.AddDependencyLocation(delta2Dll.Path);

            Assembly gamma = loader.LoadFromPath(gammaDll.Path);
            var g = gamma.CreateInstance("Gamma.G")!;
            g.GetType().GetMethod("Write")!.Invoke(g, new object[] { sb, "Test G" });

            Assembly epsilon = loader.LoadFromPath(epsilonDll.Path);
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
    }
}
