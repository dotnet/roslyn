// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Scripting.TestCompilationFactory;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Test
{
    public class InteractiveSessionReferencesTests : TestBase
    {
        /// <summary>
        /// Test adding a reference to NetStandard 2.0 library.
        /// Validates that we resolve all references correctly in the first and the subsequent submissions.
        /// </summary>
        [Fact]
        [WorkItem(345, "https://github.com/dotnet/try/issues/345")]
        public async Task LibraryReference_NetStandard20()
        {
            var libSource = @"
public class C
{
    public readonly int X = 1;
}
";
            var libImage = CreateCSharpCompilation(libSource, TargetFrameworkUtil.NetStandard20References, "lib").EmitToArray();

            var dir = Temp.CreateDirectory();
            var libFile = dir.CreateFile("lib.dll").WriteAllBytes(libImage);

            var s0 = CSharpScript.Create($@"
#r ""{libFile.Path}""
int F(C c) => c.X;
");
            var s1 = s0.ContinueWith($@"
F(new C())
");
            var diagnostics = s1.Compile();
            Assert.Empty(diagnostics);

            var result = await s1.EvaluateAsync();
            Assert.Equal(1, (int)result);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LibraryReference_MissingDependency_MultipleResolveAttempts(bool swapReferences)
        {
            var libASource = @"
public class A
{
    public readonly int X = D.Y;
}
";
            var libBSource = @"
public class B
{
    public readonly int X = D.Y;
}
";
            var libDSource = @"
public class D
{
    public static readonly int Y = 1;
}
";

            var dir = Temp.CreateDirectory();

            // 1\a.dll
            // 2\{b.dll, d.dll}
            var dir1 = dir.CreateDirectory("1");
            var dir2 = dir.CreateDirectory("2");

            var libDImage = CreateCSharpCompilation(libDSource, TargetFrameworkUtil.NetStandard20References, "libD").EmitToArray();
            var libDFile = dir2.CreateFile("libD.dll").WriteAllBytes(libDImage);
            var libDRef = MetadataReference.CreateFromFile(libDFile.Path);

            var libAImage = CreateCSharpCompilation(libASource, TargetFrameworkUtil.NetStandard20References.Concat(libDRef), "libA").EmitToArray();
            var libAFile = dir1.CreateFile("libA.dll").WriteAllBytes(libAImage);

            var libBImage = CreateCSharpCompilation(libBSource, TargetFrameworkUtil.NetStandard20References.Concat(libDRef), "libB").EmitToArray();
            var libBFile = dir2.CreateFile("libB.dll").WriteAllBytes(libBImage);

            var r1 = swapReferences ? libBFile.Path : libAFile.Path;
            var r2 = swapReferences ? libAFile.Path : libBFile.Path;

            var s0 = CSharpScript.Create($@"
#r ""{r1}""
#r ""{r2}""
new A().X + new B().X
");
            var diagnostics0 = s0.Compile();
            Assert.Empty(diagnostics0);

            var result = await s0.EvaluateAsync();
            Assert.Equal(2, (int)result);
        }

        [Fact]
        public void LibraryReference_MissingDependency()
        {
            var libASource = @"
public class C
{
    public readonly int X = D.Y;
}
";
            var libBSource = @"
public class D
{
    public static readonly int Y = 1;
    public static readonly int Z = 2;
}
";

            var dir = Temp.CreateDirectory();
            var libBImage = CreateCSharpCompilation(libBSource, TargetFrameworkUtil.NetStandard20References, "libB").EmitToArray();

            // store the reference under a different file name, so that it is not found by the resolver:
            var libBFile = dir.CreateFile("libB1.dll").WriteAllBytes(libBImage);
            var libBRef = MetadataReference.CreateFromFile(libBFile.Path);

            var libAImage = CreateCSharpCompilation(libASource, TargetFrameworkUtil.NetStandard20References.Concat(libBRef), "libA").EmitToArray();
            var libAFile = dir.CreateFile("libA.dll").WriteAllBytes(libAImage);

            var s0 = CSharpScript.Create($@"
#r ""{libAFile.Path}""
int F(C c) => c.X;
");
            var diagnostics0 = s0.Compile();
            Assert.Empty(diagnostics0);

            File.Move(libBFile.Path, Path.Combine(dir.Path, "libB.dll"));

            var s1 = s0.ContinueWith($@"
F(new C())
");
            var diagnostics1 = s1.Compile();
            Assert.Empty(diagnostics1);

            var m = s1.GetCompilation().Assembly.Modules.Single();
            Assert.False(m.ReferencedAssemblies.Any(a => a.Name == "libB"));
            var missingB = m.ReferencedAssemblySymbols.Single(a => a.Name == "libA").Modules.Single().ReferencedAssemblySymbols.Single(a => a.Name == "libB");
            Assert.IsType<MissingAssemblySymbol>(missingB);
        }
    }
}
