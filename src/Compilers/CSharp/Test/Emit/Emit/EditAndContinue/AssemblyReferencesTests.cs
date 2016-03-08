// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
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
            var references = new[] { SystemWindowsFormsRef, MscorlibRef, SystemCoreRef };

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

            var c1 = CreateCompilation(src1, references);
            var c2 = c1.WithSource(src2);
            var md1 = AssemblyMetadata.CreateFromStream(c1.EmitToStream());
            var baseline = EmitBaseline.CreateInitialBaseline(md1.GetModules()[0], handle => default(EditAndContinueMethodDebugInformation));

            var mdStream = new MemoryStream();
            var ilStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var updatedMethods = new List<MethodDefinitionHandle>();

            var edits = new[]
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
            var md1 = AssemblyMetadata.CreateFromStream(CreateCompilation(srcPE, new[] { MscorlibRef, SystemRef }).EmitToStream());

            var c1 = CreateCompilation(src1, new[] { MscorlibRef });
            var c2 = c1.WithSource(src2);
            var baseline = EmitBaseline.CreateInitialBaseline(md1.GetModules()[0], handle => default(EditAndContinueMethodDebugInformation));

            var mdStream = new MemoryStream();
            var ilStream = new MemoryStream();
            var pdbStream = new MemoryStream();
            var updatedMethods = new List<MethodDefinitionHandle>();

            var edits = new[]
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
            var lib0 = CreateCompilationWithMscorlib(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib0.VerifyDiagnostics();

            var lib1 = CreateCompilationWithMscorlib(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib1.VerifyDiagnostics();

            var lib2 = CreateCompilationWithMscorlib(srcLib, assemblyName: "Lib", options: TestOptions.DebugDll);
            lib2.VerifyDiagnostics();

            var compilation0 = CreateCompilation(src0, new[] { MscorlibRef, lib0.ToMetadataReference() }, assemblyName: "C", options: TestOptions.DebugDll);
            var compilation1 = compilation0.WithSource(src1).WithReferences(new[] { MscorlibRef, lib1.ToMetadataReference() });
            var compilation2 = compilation1.WithSource(src2).WithReferences(new[] { MscorlibRef, lib2.ToMetadataReference() });

            var v0 = CompileAndVerify(compilation0);
            var v1 = CompileAndVerify(compilation1);
            var v2 = CompileAndVerify(compilation2);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);
            var generation0 = EmitBaseline.CreateInitialBaseline(md0, EmptyLocalsProvider);

            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(new SemanticEdit(SemanticEditKind.Update, f0, f1)));

            diff1.EmitResult.Diagnostics.Verify();

            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2)));

            diff2.EmitResult.Diagnostics.Verify();
        }
    }
}
