// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class AssemblyReferencesTests : EditAndContinueTestBase
    {
        private static readonly CSharpCompilationOptions s_signedDll =
            TestOptions.ReleaseDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2);

        /// <summary>
        /// The baseline metadata might have less (or even different) references than
        /// the current compilation. We shouldn't assume that the reference sets are the same.
        /// </summary>
        [Fact]
        public void CompilationReferences_Less()
        {
            // Add some references that are actually not used in the source.
            // The only actual reference stored in the metadata image would be: mscorlib (rowid 1).
            // If we incorrectly assume the references are the same we will map TypeRefs of 
            // Mscorlib to System.Windows.Forms.
            MetadataReference[] references = new[] { SystemWindowsFormsRef, MscorlibRef, SystemCoreRef };

            string src1 = @"
using System;
using System.Threading.Tasks;

class C 
{ 
    public Task<int> F() { Console.WriteLine(123); return null; }
    public static void Main() { Console.WriteLine(1); } 
}
";
            string src2 = @"
using System;
using System.Threading.Tasks;

class C 
{ 
    public Task<int> F() { Console.WriteLine(123); return null; }
    public static void Main() { Console.WriteLine(2); }
}
";

            CSharpCompilation c1 = CreateEmptyCompilation(src1, references);
            CSharpCompilation c2 = c1.WithSource(src2);
            var md1 = AssemblyMetadata.CreateFromStream(c1.EmitToStream());
            var baseline = EmitBaseline.CreateInitialBaseline(md1.GetModules()[0], handle => default(EditAndContinueMethodDebugInformation));

            var mdStream = new MemoryStream();
            var ilStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var updatedMethods = new List<MethodDefinitionHandle>();

            SemanticEdit[] edits = new[]
            {
                new SemanticEdit(
                    SemanticEditKind.Update,
                    c1.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("Main"),
                    c2.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("Main"))
            };

            c2.EmitDifference(baseline, edits, mdStream, ilStream, pdbStream, updatedMethods);

            var actualIL = ImmutableArray.Create(ilStream.ToArray()).GetMethodIL();
            var expectedIL = @"
{
  // Code size        7 (0x7)
  .maxstack  8
  IL_0000:  ldc.i4.2
  IL_0001:  call       0x0A000006
  IL_0006:  ret
}";
            // If the references are mismatched then the symbol matcher won't be able to find Task<T>
            // and will recompile the method body of F (even though the method hasn't changed).
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
        }

        /// <summary>
        /// The baseline metadata might have more references than the current compilation. 
        /// References that aren't found in the compilation are treated as missing.
        /// </summary>
        [Fact]
        public void CompilationReferences_More()
        {
            string src1 = @"
using System;
class C 
{ 
    public static int F(object a) { return 1; }
    public static void Main() { Console.WriteLine(F(null)); } 
}
";
            string src2 = @"
using System;
class C 
{ 
    public static int F(object a) { return 1; }
    public static void Main() { F(null); }
}
";

            // Let's say an IL rewriter inserts a new overload of F that references 
            // a type in a new AssemblyRef.
            string srcPE = @"
using System;
class C 
{ 
    public static int F(System.Diagnostics.Process a) { return 2; }
    public static int F(object a) { return 1; }

    public static void Main() { F(null); }
}
";
            var md1 = AssemblyMetadata.CreateFromStream(CreateEmptyCompilation(srcPE, new[] { MscorlibRef, SystemRef }).EmitToStream());

            CSharpCompilation c1 = CreateEmptyCompilation(src1, new[] { MscorlibRef });
            CSharpCompilation c2 = c1.WithSource(src2);
            var baseline = EmitBaseline.CreateInitialBaseline(md1.GetModules()[0], handle => default(EditAndContinueMethodDebugInformation));

            var mdStream = new MemoryStream();
            var ilStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var updatedMethods = new List<MethodDefinitionHandle>();

            SemanticEdit[] edits = new[]
            {
                new SemanticEdit(
                    SemanticEditKind.Update,
                    c1.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("Main"),
                    c2.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember("Main"))
            };

            c2.EmitDifference(baseline, edits, mdStream, ilStream, pdbStream, updatedMethods);

            var actualIL = ImmutableArray.Create(ilStream.ToArray()).GetMethodIL();

            // Symbol matcher should ignore overloads with missing type symbols and match 
            // F(object).
            var expectedIL = @"
{
  // Code size        8 (0x8)
  .maxstack  8
  IL_0000:  ldnull
  IL_0001:  call       0x06000002
  IL_0006:  pop
  IL_0007:  ret
}";
            AssertEx.AssertEqualToleratingWhitespaceDifferences(expectedIL, actualIL);
        }

        /// <summary>
        /// Symbol matcher considers two source types that only differ in the declaring compilations different.
        /// </summary>
        [Fact]
        public void ChangingCompilationDependencies()
        {
            string srcLib = @"
public class D { }
";

            string src0 = @"
class C 
{ 
    public static int F(D a) { return 1; }
}
";
            string src1 = @"
class C 
{ 
    public static int F(D a) { return 2; }
}
";
            string src2 = @"
class C 
{ 
    public static int F(D a) { return 3; }
}
";
            CSharpCompilation lib0 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib0.VerifyDiagnostics();

            CSharpCompilation lib1 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib1.VerifyDiagnostics();

            CSharpCompilation lib2 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib2.VerifyDiagnostics();

            CSharpCompilation compilation0 = CreateEmptyCompilation(src0, new[] { MscorlibRef, lib0.ToMetadataReference() }, assemblyName: "C", options: TestOptions.DebugDll);
            CSharpCompilation compilation1 = compilation0.WithSource(src1).WithReferences(new[] { MscorlibRef, lib1.ToMetadataReference() });
            CSharpCompilation compilation2 = compilation1.WithSource(src2).WithReferences(new[] { MscorlibRef, lib2.ToMetadataReference() });

            CompilationVerifier v0 = CompileAndVerify(compilation0);
            CompilationVerifier v1 = CompileAndVerify(compilation1);
            CompilationVerifier v2 = CompileAndVerify(compilation2);

            MethodSymbol f0 = compilation0.GetMember<MethodSymbol>("C.F");
            MethodSymbol f1 = compilation1.GetMember<MethodSymbol>("C.F");
            MethodSymbol f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            CompilationDifference diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            diff1.EmitResult.Diagnostics.Verify();

            CompilationDifference diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2)));

            diff2.EmitResult.Diagnostics.Verify();
        }

        [Fact]
        public void DependencyVersionWildcards_Compilation()
        {
            TestDependencyVersionWildcards(
                "1.0.0.*",
                new Version(1, 0, 2000, 1001),
                new Version(1, 0, 2000, 1001),
                new Version(1, 0, 2000, 1002));

            TestDependencyVersionWildcards(
                "1.0.0.*",
                new Version(1, 0, 2000, 1001),
                new Version(1, 0, 2000, 1002),
                new Version(1, 0, 2000, 1002));

            TestDependencyVersionWildcards(
                "1.0.0.*",
                new Version(1, 0, 2000, 1003),
                new Version(1, 0, 2000, 1002),
                new Version(1, 0, 2000, 1001));

            TestDependencyVersionWildcards(
                "1.0.*",
                new Version(1, 0, 2000, 1001),
                new Version(1, 0, 2000, 1002),
                new Version(1, 0, 2000, 1003));

            TestDependencyVersionWildcards(
                "1.0.*",
                new Version(1, 0, 2000, 1001),
                new Version(1, 0, 2000, 1005),
                new Version(1, 0, 2000, 1002));
        }

        private void TestDependencyVersionWildcards(string sourceVersion, Version version0, Version version1, Version version2)
        {
            string srcLib = $@"
[assembly: System.Reflection.AssemblyVersion(""{sourceVersion}"")]

public class D {{ }}
";

            string src0 = @"
class C 
{ 
    public static int F(D a) { return 1; }
}
";
            string src1 = @"
class C 
{ 
    public static int F(D a) { return 2; }
}
";
            string src2 = @"
class C 
{ 
    public static int F(D a) { return 3; }
    public static int G(D a) { return 4; }
}
";
            CSharpCompilation lib0 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);

            ((SourceAssemblySymbol)lib0.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", version0);
            lib0.VerifyDiagnostics();

            CSharpCompilation lib1 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            ((SourceAssemblySymbol)lib1.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", version1);
            lib1.VerifyDiagnostics();

            CSharpCompilation lib2 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            ((SourceAssemblySymbol)lib2.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", version2);

            lib2.VerifyDiagnostics();

            CSharpCompilation compilation0 = CreateEmptyCompilation(src0, new[] { MscorlibRef, lib0.ToMetadataReference() }, assemblyName: "C", options: TestOptions.DebugDll);
            CSharpCompilation compilation1 = compilation0.WithSource(src1).WithReferences(new[] { MscorlibRef, lib1.ToMetadataReference() });
            CSharpCompilation compilation2 = compilation1.WithSource(src2).WithReferences(new[] { MscorlibRef, lib2.ToMetadataReference() });

            CompilationVerifier v0 = CompileAndVerify(compilation0);
            CompilationVerifier v1 = CompileAndVerify(compilation1);
            CompilationVerifier v2 = CompileAndVerify(compilation2);

            MethodSymbol f0 = compilation0.GetMember<MethodSymbol>("C.F");
            MethodSymbol f1 = compilation1.GetMember<MethodSymbol>("C.F");
            MethodSymbol f2 = compilation2.GetMember<MethodSymbol>("C.F");
            MethodSymbol g2 = compilation2.GetMember<MethodSymbol>("C.G");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            CompilationDifference diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            CompilationDifference diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2),
                    new SemanticEdit(SemanticEditKind.Insert, null, g2)));

            PinnedMetadata md1 = diff1.GetMetadata();
            PinnedMetadata md2 = diff2.GetMetadata();

            var aggReader = new AggregatedMetadataReader(md0.MetadataReader, md1.Reader, md2.Reader);

            // all references to Lib should be to the baseline version:
            VerifyAssemblyReferences(aggReader, new[]
            {
                "mscorlib, 4.0.0.0",
                "Lib, " + lib0.Assembly.Identity.Version,
                "mscorlib, 4.0.0.0",
                "Lib, " + lib0.Assembly.Identity.Version,
                "mscorlib, 4.0.0.0",
                "Lib, " + lib0.Assembly.Identity.Version,
            });
        }

        [Fact]
        public void DependencyVersionWildcards_Metadata()
        {
            string srcLib = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.*"")]

public class D { }
";

            string src0 = @"
class C 
{ 
    public static int F(D a) { return 1; }
}
";
            string src1 = @"
class C 
{ 
    public static int F(D a) { return 2; }
}
";
            string src2 = @"
class C 
{ 
    public static int F(D a) { return 3; }
    public static int G(D a) { return 4; }
}
";
            CSharpCompilation lib0 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            ((SourceAssemblySymbol)lib0.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", new Version(1, 0, 2000, 1001));
            lib0.VerifyDiagnostics();

            CSharpCompilation lib1 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            ((SourceAssemblySymbol)lib1.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", new Version(1, 0, 2000, 1002));
            lib1.VerifyDiagnostics();

            CSharpCompilation lib2 = CreateCompilation(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            ((SourceAssemblySymbol)lib2.Assembly).lazyAssemblyIdentity = new AssemblyIdentity("Lib", new Version(1, 0, 2000, 1003));
            lib2.VerifyDiagnostics();

            CSharpCompilation compilation0 = CreateEmptyCompilation(src0, new[] { MscorlibRef, lib0.EmitToImageReference() }, assemblyName: "C", options: TestOptions.DebugDll);
            CSharpCompilation compilation1 = compilation0.WithSource(src1).WithReferences(new[] { MscorlibRef, lib1.EmitToImageReference() });
            CSharpCompilation compilation2 = compilation1.WithSource(src2).WithReferences(new[] { MscorlibRef, lib2.EmitToImageReference() });

            CompilationVerifier v0 = CompileAndVerify(compilation0);
            CompilationVerifier v1 = CompileAndVerify(compilation1);
            CompilationVerifier v2 = CompileAndVerify(compilation2);

            MethodSymbol f0 = compilation0.GetMember<MethodSymbol>("C.F");
            MethodSymbol f1 = compilation1.GetMember<MethodSymbol>("C.F");
            MethodSymbol f2 = compilation2.GetMember<MethodSymbol>("C.F");
            MethodSymbol g2 = compilation2.GetMember<MethodSymbol>("C.G");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            CompilationDifference diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            diff1.EmitResult.Diagnostics.Verify(
                // error CS7038: Failed to emit module 'C'.
                Diagnostic(ErrorCode.ERR_ModuleEmitFailure).WithArguments("C"));
        }

        [Fact, WorkItem(9004, "https://github.com/dotnet/roslyn/issues/9004")]
        public void DependencyVersionWildcardsCollisions()
        {
            string srcLib01 = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.1"")]

public class D { }
";
            string srcLib02 = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.0.2"")]

public class D { }
";

            string srcLib11 = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.1.1"")]

public class D { }
";
            string srcLib12 = @"
[assembly: System.Reflection.AssemblyVersion(""1.0.1.2"")]

public class D { }
";

            string src0 = @"
extern alias L0;
extern alias L1;

class C 
{ 
    public static int F(L0::D a, L1::D b) => 1;
}
";
            string src1 = @"
extern alias L0;
extern alias L1;

class C 
{ 
    public static int F(L0::D a, L1::D b) => 2;
}
";
            CSharpCompilation lib01 = CreateCompilation(srcLib01, assemblyName: "Lib", options: s_signedDll).VerifyDiagnostics();
            CompilationReference ref01 = lib01.ToMetadataReference(ImmutableArray.Create("L0"));

            CSharpCompilation lib02 = CreateCompilation(srcLib02, assemblyName: "Lib", options: s_signedDll).VerifyDiagnostics();
            CompilationReference ref02 = lib02.ToMetadataReference(ImmutableArray.Create("L0"));

            CSharpCompilation lib11 = CreateCompilation(srcLib11, assemblyName: "Lib", options: s_signedDll).VerifyDiagnostics();
            CompilationReference ref11 = lib11.ToMetadataReference(ImmutableArray.Create("L1"));

            CSharpCompilation lib12 = CreateCompilation(srcLib12, assemblyName: "Lib", options: s_signedDll).VerifyDiagnostics();
            CompilationReference ref12 = lib12.ToMetadataReference(ImmutableArray.Create("L1"));

            CSharpCompilation compilation0 = CreateEmptyCompilation(src0, new[] { MscorlibRef, ref01, ref11 }, assemblyName: "C", options: TestOptions.DebugDll);
            CSharpCompilation compilation1 = compilation0.WithSource(src1).WithReferences(new[] { MscorlibRef, ref02, ref12 });

            CompilationVerifier v0 = CompileAndVerify(compilation0);

            MethodSymbol f0 = compilation0.GetMember<MethodSymbol>("C.F");
            MethodSymbol f1 = compilation1.GetMember<MethodSymbol>("C.F");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            CompilationDifference diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            // TODO: message should be: Changing the version of an assembly reference is not allowed during debugging
            diff1.EmitResult.Diagnostics.Verify(
                // error CS7038: Failed to emit module 'C'.
                Diagnostic(ErrorCode.ERR_ModuleEmitFailure).WithArguments("C"));
        }

        private void VerifyAssemblyReferences(AggregatedMetadataReader reader, string[] expected)
        {
            AssertEx.Equal(expected, reader.GetAssemblyReferences().Select(aref => $"{reader.GetString(aref.Name)}, {aref.Version}"));
        }

        [Fact]
        [WorkItem(202017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/202017")]
        public void CurrentCompilationVersionWildcards()
        {
            SourceWithMarkedNodes source0 = MarkedSource(@"
using System;
[assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")]

class C
{
    static void M()
    {
        new Action(<N:0>() => { Console.WriteLine(1); }</N:0>).Invoke();
    }

    static void F()
    {
    }
}");
            SourceWithMarkedNodes source1 = MarkedSource(@"
using System;
[assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")]

class C
{
    static void M()
    {
        new Action(<N:0>() => { Console.WriteLine(1); }</N:0>).Invoke();
        new Action(<N:1>() => { Console.WriteLine(2); }</N:1>).Invoke();
    }

    static void F()
    {
    }
}");
            SourceWithMarkedNodes source2 = MarkedSource(@"
using System;
[assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")]

class C
{
    static void M()
    {
        new Action(<N:0>() => { Console.WriteLine(1); }</N:0>).Invoke();
        new Action(<N:1>() => { Console.WriteLine(2); }</N:1>).Invoke();
    }

    static void F()
    {
        Console.WriteLine(1);
    }
}");
            SourceWithMarkedNodes source3 = MarkedSource(@"
using System;
[assembly: System.Reflection.AssemblyVersion(""1.0.0.*"")]

class C
{
    static void M()
    {
        new Action(<N:0>() => { Console.WriteLine(1); }</N:0>).Invoke();
        new Action(<N:1>() => { Console.WriteLine(2); }</N:1>).Invoke();
        new Action(<N:2>() => { Console.WriteLine(3); }</N:2>).Invoke();
        new Action(<N:3>() => { Console.WriteLine(4); }</N:3>).Invoke();
    }

    static void F()
    {
        Console.WriteLine(1);
    }
}");

            CSharpCompilationOptions options = ComSafeDebugDll.WithCryptoPublicKey(TestResources.TestKeys.PublicKey_ce65828c82a341f2);

            CSharpCompilation compilation0 = CreateCompilation(source0.Tree, options: options.WithCurrentLocalTime(new DateTime(2016, 1, 1, 1, 0, 0)));
            CSharpCompilation compilation1 = compilation0.WithSource(source1.Tree).WithOptions(options.WithCurrentLocalTime(new DateTime(2016, 1, 1, 1, 0, 10)));
            CSharpCompilation compilation2 = compilation1.WithSource(source2.Tree).WithOptions(options.WithCurrentLocalTime(new DateTime(2016, 1, 1, 1, 0, 20)));
            CSharpCompilation compilation3 = compilation2.WithSource(source3.Tree).WithOptions(options.WithCurrentLocalTime(new DateTime(2016, 1, 1, 1, 0, 30)));

            CompilationVerifier v0 = CompileAndVerify(compilation0, verify: Verification.Passes);
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            MetadataReader reader0 = md0.MetadataReader;

            MethodSymbol m0 = compilation0.GetMember<MethodSymbol>("C.M");
            MethodSymbol m1 = compilation1.GetMember<MethodSymbol>("C.M");
            MethodSymbol m2 = compilation2.GetMember<MethodSymbol>("C.M");
            MethodSymbol m3 = compilation3.GetMember<MethodSymbol>("C.M");

            MethodSymbol f1 = compilation1.GetMember<MethodSymbol>("C.F");
            MethodSymbol f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            // First update adds some new synthesized members (lambda related)
            CompilationDifference diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, m0, m1, GetSyntaxMapFromMarkers(source0, source1), preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <M>b__0_0, <M>b__0_1#1}");

            // Second update is to a method that doesn't produce any synthesized members 
            CompilationDifference diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f1, f2, GetSyntaxMapFromMarkers(source1, source2), preserveLocalVariables: true)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <M>b__0_0, <M>b__0_1#1}");

            // Last update again adds some new synthesized members (lambdas).
            // Synthesized members added in the first update need to be mapped to the current compilation.
            // Their containing assembly version is different than the version of the previous assembly and 
            // hence we need to account for wildcards when comparing the versions.
            CompilationDifference diff3 = compilation3.EmitDifference(
                diff2.NextGeneration,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, m2, m3, GetSyntaxMapFromMarkers(source2, source3), preserveLocalVariables: true)));

            diff3.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__0_0, <>9__0_1#1, <>9__0_2#3, <>9__0_3#3, <M>b__0_0, <M>b__0_1#1, <M>b__0_2#3, <M>b__0_3#3}");
        }
    }
}
