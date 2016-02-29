// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class CompilationAPITests : CSharpTestBase
    {
        [WorkItem(8360, "https://github.com/dotnet/roslyn/issues/8360")]
        [Fact]
        public void PublicSignWithRelativeKeyPath()
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithPublicSign(true).WithCryptoKeyFile("test.snk");
            var comp = CSharpCompilation.Create("test", options: options);
            comp.VerifyDiagnostics(
    // error CS7088: Invalid 'CryptoKeyFile' value: 'test.snk'.
    Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("CryptoKeyFile", "test.snk").WithLocation(1, 1),
    // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
    Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));
        }

        [Fact]
        public void CompilationName()
        {
            // report an error, rather then silently ignoring the directory
            // (see cli partition II 22.30) 
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"C:/foo/Test.exe"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"C:\foo\Test.exe"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"\foo/Test.exe"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"C:Test.exe"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"Te\0st.exe"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"   \t  "));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"\uD800"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@""));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@" a"));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create(@"\u2000a")); // U+20700 is whitespace
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("..\\..\\RelativePath"));

            // other characters than directory and volume separators are ok:
            CSharpCompilation.Create(@";,*?<>#!@&");
            CSharpCompilation.Create("foo");
            CSharpCompilation.Create(".foo");
            CSharpCompilation.Create("foo "); // can end with whitespace
            CSharpCompilation.Create("....");
            CSharpCompilation.Create(null);
        }

        [Fact]
        public void CreateAPITest()
        {
            var listSyntaxTree = new List<SyntaxTree>();
            var listRef = new List<MetadataReference>();

            var s1 = @"using Foo; 
namespace A.B { 
   class C { 
     class D { 
       class E { }
     }
   }
   class G<T> {
     class Q<S1,S2> { }
   }
   class G<T1,T2> { }
}";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            listSyntaxTree.Add(t1);

            // System.dll
            listRef.Add(TestReferences.NetFx.v4_0_30319.System.WithEmbedInteropTypes(true));
            var ops = TestOptions.ReleaseExe;
            // Create Compilation with Option is not null
            var comp = CSharpCompilation.Create("Compilation", listSyntaxTree, listRef, ops);
            Assert.Equal(ops, comp.Options);
            Assert.NotNull(comp.SyntaxTrees);
            Assert.NotNull(comp.References);
            Assert.Equal(1, comp.SyntaxTrees.Length);
            Assert.Equal(1, comp.ExternalReferences.Length);
            var ref1 = comp.ExternalReferences[0];
            Assert.True(ref1.Properties.EmbedInteropTypes);
            Assert.True(ref1.Properties.Aliases.IsEmpty);

            // Create Compilation with PreProcessorSymbols of Option is empty
            var ops1 = TestOptions.DebugExe;

            // Create Compilation with Assembly name contains invalid char
            var asmname = "ÃƒÆ’Ã†â€™Ãƒâ€ Ã¢â‚¬â„¢ÃƒÆ’Ã¢â‚¬Â";
            comp = CSharpCompilation.Create(asmname, listSyntaxTree, listRef, ops);
            var comp1 = CSharpCompilation.Create(asmname, listSyntaxTree, listRef, null);
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToMemoryStreams()
        {
            var comp = CSharpCompilation.Create("Compilation", options: TestOptions.ReleaseDll);

            using (var output = new MemoryStream())
            {
                using (var outputPdb = new MemoryStream())
                {
                    using (var outputxml = new MemoryStream())
                    {
                        var result = comp.Emit(output, outputPdb, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(peStream: output, xmlDocumentationStream: null);
                        Assert.True(result.Success);
                        result = comp.Emit(peStream: output, pdbStream: outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb);
                        Assert.True(result.Success);
                        result = comp.Emit(output, outputPdb, outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, null, null, null);
                        Assert.True(result.Success);
                        result = comp.Emit(output);
                        Assert.True(result.Success);
                        result = comp.Emit(output, null, outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, xmlDocumentationStream: outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, xmlDocumentationStream: outputxml);
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: null);
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithHighEntropyVirtualAddressSpace(true));
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithOutputNameOverride("foo"));
                        Assert.True(result.Success);
                        result = comp.Emit(output, options: EmitOptions.Default.WithPdbFilePath("foo.pdb"));
                        Assert.True(result.Success);
                    }
                }
            }
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToBoundedStreams()
        {
            var pdbArray = new byte[100000];
            var pdbStream = new MemoryStream(pdbArray, 1, pdbArray.Length - 10);

            var peArray = new byte[100000];
            var peStream = new MemoryStream(peArray);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToStreamWithNonZeroPosition()
        {
            var pdbStream = new MemoryStream();
            pdbStream.WriteByte(0x12);
            var peStream = new MemoryStream();
            peStream.WriteByte(0x12);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();

            AssertEx.Equal(new byte[] { 0x12, (byte)'M', (byte)'i', (byte)'c', (byte)'r', (byte)'o' }, pdbStream.GetBuffer().Take(6).ToArray());
            AssertEx.Equal(new byte[] { 0x12, (byte)'M', (byte)'Z' }, peStream.GetBuffer().Take(3).ToArray());
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void EmitToNonSeekableStreams()
        {
            var peStream = new TestStream(canRead: false, canSeek: false, canWrite: true);
            var pdbStream = new TestStream(canRead: false, canSeek: false, canWrite: true);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            var r = c.Emit(peStream, pdbStream);
            r.Diagnostics.Verify();
        }

        [Fact]
        public void EmitToNonWritableStreams()
        {
            var peStream = new TestStream(canRead: false, canSeek: false, canWrite: false);
            var pdbStream = new TestStream(canRead: false, canSeek: false, canWrite: false);

            var c = CSharpCompilation.Create("a",
                new[] { SyntaxFactory.ParseSyntaxTree("class C { static void Main() {} }") },
                new[] { MscorlibRef });

            Assert.Throws<ArgumentException>(() => c.Emit(peStream));
            Assert.Throws<ArgumentException>(() => c.Emit(new MemoryStream(), pdbStream));
        }

        [Fact]
        public void EmitOptionsDiagnostics()
        {
            var c = CreateCompilationWithMscorlib("class C {}");
            var stream = new MemoryStream();

            var options = new EmitOptions(
                debugInformationFormat: (DebugInformationFormat)(-1),
                outputNameOverride: " ",
                fileAlignment: 513,
                subsystemVersion: SubsystemVersion.Create(1000000, -1000000));

            EmitResult result = c.Emit(stream, options: options);

            result.Diagnostics.Verify(
                // error CS2042: Invalid debug information format: -1
                Diagnostic(ErrorCode.ERR_InvalidDebugInformationFormat).WithArguments("-1"),
                // error CS2041: Invalid output name: Name cannot start with whitespace.
                Diagnostic(ErrorCode.ERR_InvalidOutputName).WithArguments(CodeAnalysisResources.NameCannotStartWithWhitespace),
                // error CS2024: Invalid file section alignment '513'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("513"),
                // error CS1773: Invalid version 1000000.-1000000 for /subsystemversion. The version must be 6.02 or greater for ARM or AppContainerExe, and 4.00 or greater otherwise
                Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("1000000.-1000000"));

            Assert.False(result.Success);
        }

        [ClrOnlyFact(ClrOnlyReason.Pdb)]
        public void NegEmit()
        {
            var ops = TestOptions.ReleaseDll;
            var comp = CSharpCompilation.Create("Compilation", null, null, ops);
            using (MemoryStream output = new MemoryStream())
            {
                using (MemoryStream outputPdb = new MemoryStream())
                {
                    using (MemoryStream outputxml = new MemoryStream())
                    {
                        var result = comp.Emit(output, outputPdb, outputxml);
                        Assert.True(result.Success);
                    }
                }
            }

            Assert.Throws<ArgumentNullException>(() => comp.Emit(peStream: null));
        }

        [Fact]
        public void ReferenceAPITest()
        {
            var opt = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
            // Create Compilation takes two args
            var comp = CSharpCompilation.Create("Compilation", options: new CSharpCompilationOptions(OutputKind.ConsoleApplication));
            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var ref2 = TestReferences.NetFx.v4_0_30319.System;
            var ref3 = new TestMetadataReference(fullPath: @"c:\xml.bms");
            var ref4 = new TestMetadataReference(fullPath: @"c:\aaa.dll");
            // Add a new empty item 
            comp = comp.AddReferences(Enumerable.Empty<MetadataReference>());
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Add a new valid item 
            comp = comp.AddReferences(ref1);
            var assemblySmb = comp.GetReferencedAssemblySymbol(ref1);
            Assert.NotNull(assemblySmb);
            Assert.Equal("mscorlib", assemblySmb.Name, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref1, comp.ExternalReferences[0]);

            // Replace an existing item with another valid item 
            comp = comp.ReplaceReference(ref1, ref2);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref2, comp.ExternalReferences[0]);

            // Remove an existing item 
            comp = comp.RemoveReferences(ref2);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Overload with Hashset
            var hs = new HashSet<MetadataReference> { ref1, ref2, ref3 };
            var compCollection = CSharpCompilation.Create("Compilation", references: hs, options: opt);
            compCollection = compCollection.AddReferences(ref1, ref2, ref3, ref4).RemoveReferences(hs);
            Assert.Equal(1, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(hs).RemoveReferences(ref1, ref2, ref3, ref4);
            Assert.Equal(0, compCollection.ExternalReferences.Length);

            // Overload with Collection
            var col = new Collection<MetadataReference> { ref1, ref2, ref3 };
            compCollection = CSharpCompilation.Create("Compilation", references: col, options: opt);
            compCollection = compCollection.AddReferences(col).RemoveReferences(ref1, ref2, ref3);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref1, ref2, ref3).RemoveReferences(col);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Overload with ConcurrentStack
            var stack = new ConcurrentStack<MetadataReference> { };
            stack.Push(ref1);
            stack.Push(ref2);
            stack.Push(ref3);
            compCollection = CSharpCompilation.Create("Compilation", references: stack, options: opt);
            compCollection = compCollection.AddReferences(stack).RemoveReferences(ref1, ref3, ref2);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(stack);
            Assert.Equal(0, compCollection.ExternalReferences.Length);

            // Overload with ConcurrentQueue
            var queue = new ConcurrentQueue<MetadataReference> { };
            queue.Enqueue(ref1);
            queue.Enqueue(ref2);
            queue.Enqueue(ref3);
            compCollection = CSharpCompilation.Create("Compilation", references: queue, options: opt);
            compCollection = compCollection.AddReferences(queue).RemoveReferences(ref3, ref2, ref1);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
            compCollection = compCollection.AddReferences(ref2, ref1, ref3).RemoveReferences(queue);
            Assert.Equal(0, compCollection.ExternalReferences.Length);
        }

        [Fact]
        public void ReferenceDirectiveTests()
        {
            var t1 = Parse(@"
#r ""a.dll"" 
#r ""a.dll""
", filename: "1.csx", options: TestOptions.Script);

            var rd1 = t1.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(2, rd1.Length);

            var t2 = Parse(@"
#r ""a.dll""
#r ""b.dll""
", options: TestOptions.Script);

            var rd2 = t2.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(2, rd2.Length);

            var t3 = Parse(@"
#r ""a.dll""
", filename: "1.csx", options: TestOptions.Script);

            var rd3 = t3.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(1, rd3.Length);

            var t4 = Parse(@"
#r ""a.dll""
", filename: "4.csx", options: TestOptions.Script);

            var rd4 = t4.GetRoot().GetDirectives().Cast<ReferenceDirectiveTriviaSyntax>().ToArray();
            Assert.Equal(1, rd4.Length);

            var c = CreateCompilationWithMscorlib45(new[] { t1, t2 }, options: TestOptions.ReleaseDll.WithMetadataReferenceResolver(
                new TestMetadataReferenceResolver(files: new Dictionary<string, PortableExecutableReference>()
                {
                    { @"a.dll", TestReferences.NetFx.v4_0_30319.Microsoft_CSharp },
                    { @"b.dll", TestReferences.NetFx.v4_0_30319.Microsoft_VisualBasic },
                })));

            c.VerifyDiagnostics();

            // same containing script file name and directive string
            Assert.Same(TestReferences.NetFx.v4_0_30319.Microsoft_CSharp, c.GetDirectiveReference(rd1[0]));
            Assert.Same(TestReferences.NetFx.v4_0_30319.Microsoft_CSharp, c.GetDirectiveReference(rd1[1]));
            Assert.Same(TestReferences.NetFx.v4_0_30319.Microsoft_CSharp, c.GetDirectiveReference(rd2[0]));
            Assert.Same(TestReferences.NetFx.v4_0_30319.Microsoft_VisualBasic, c.GetDirectiveReference(rd2[1]));
            Assert.Same(TestReferences.NetFx.v4_0_30319.Microsoft_CSharp, c.GetDirectiveReference(rd3[0]));

            // different script name or directive string:
            Assert.Null(c.GetDirectiveReference(rd4[0]));
        }

        [Fact, WorkItem(530131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530131")]
        public void MetadataReferenceWithInvalidAlias()
        {
            var refcomp = CSharpCompilation.Create("DLL",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree("public class C {}") },
                references: new MetadataReference[] { MscorlibRef });

            var mtref = refcomp.EmitToImageReference(aliases: ImmutableArray.Create("a", "Alias(*#$@^%*&)"));

            // not use exported type
            var comp = CSharpCompilation.Create("APP",
                options: TestOptions.ReleaseDll,
                syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"class D {}"
                    ) },
                references: new MetadataReference[] { MscorlibRef, mtref }
                );

            Assert.Empty(comp.GetDiagnostics());

            // use exported type with partial alias
            comp = CSharpCompilation.Create("APP1",
             options: TestOptions.ReleaseDll,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    @"extern alias Alias; class D : Alias::C {}"
                    ) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            var errs = comp.GetDiagnostics();
            //  error CS0430: The extern alias 'Alias' was not specified in a /reference option
            Assert.Equal(430, errs.FirstOrDefault().Code);

            // use exported type with invalid alias
            comp = CSharpCompilation.Create("APP2",
             options: TestOptions.ReleaseDll,
             syntaxTrees: new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(
                    "extern alias Alias(*#$@^%*&); class D : Alias(*#$@^%*&).C {}"
                    ) },
             references: new MetadataReference[] { MscorlibRef, mtref }
             );

            comp.VerifyDiagnostics(
                // (1,19): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "("),
                // (1,19): error CS1022: Type or namespace definition, or end-of-file expected
                Diagnostic(ErrorCode.ERR_EOFExpected, "("),
                // (1,20): error CS1031: Type expected
                Diagnostic(ErrorCode.ERR_TypeExpected, "*"),
                // (1,21): error CS1040: Preprocessor directives must appear as the first non-whitespace character on a line
                Diagnostic(ErrorCode.ERR_BadDirectivePlacement, "#"),
                // (1,14): error CS0430: The extern alias 'Alias' was not specified in a /reference option
                Diagnostic(ErrorCode.ERR_BadExternAlias, "Alias").WithArguments("Alias"),
                // (1,1): info CS8020: Unused extern alias.
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Alias")
                );
        }

        [Fact]
        public void SyntreeAPITest()
        {
            var s1 = "namespace System.Linq {}";
            var s2 = @"
namespace NA.NB
{
  partial class C<T>
  { 
    public partial class D
    {
      intttt F;
    }
  }
  class C { }
}
";
            var s3 = @"int x;";
            var s4 = @"Imports System ";
            var s5 = @"
class D 
{
    public static int Foo()
    {
        long l = 25l;   
        return 0;
    }
}
";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            SyntaxTree withErrorTree = SyntaxFactory.ParseSyntaxTree(s2);
            SyntaxTree withErrorTree1 = SyntaxFactory.ParseSyntaxTree(s3);
            SyntaxTree withErrorTreeVB = SyntaxFactory.ParseSyntaxTree(s4);
            SyntaxTree withExpressionRootTree = SyntaxFactory.ParseExpression(s3).SyntaxTree;
            var withWarning = SyntaxFactory.ParseSyntaxTree(s5);

            // Create compilation takes three args
            var comp = CSharpCompilation.Create("Compilation", syntaxTrees: new[] { SyntaxFactory.ParseSyntaxTree(s1) }, options: TestOptions.ReleaseDll);
            comp = comp.AddSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeVB);
            Assert.Equal(5, comp.SyntaxTrees.Length);
            comp = comp.RemoveSyntaxTrees(t1, withErrorTree, withErrorTree1, withErrorTreeVB);
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Add a new empty item
            comp = comp.AddSyntaxTrees(Enumerable.Empty<SyntaxTree>());
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Add a new valid item
            comp = comp.AddSyntaxTrees(t1);
            Assert.Equal(2, comp.SyntaxTrees.Length);
            Assert.Contains(t1, comp.SyntaxTrees);
            Assert.False(comp.SyntaxTrees.Contains(SyntaxFactory.ParseSyntaxTree(s1)));

            comp = comp.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(s1));
            Assert.Equal(3, comp.SyntaxTrees.Length);

            // Replace an existing item with another valid item 
            comp = comp.ReplaceSyntaxTree(t1, SyntaxFactory.ParseSyntaxTree(s1));
            Assert.Equal(3, comp.SyntaxTrees.Length);

            // Replace an existing item with same item 
            comp = comp.AddSyntaxTrees(t1).ReplaceSyntaxTree(t1, t1);
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // add again and verify that it throws
            Assert.Throws<ArgumentException>(() => comp.AddSyntaxTrees(t1));

            // replace with existing and verify that it throws
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(t1, comp.SyntaxTrees[0]));

            // SyntaxTrees have reference equality. This removal should fail.
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveSyntaxTrees(SyntaxFactory.ParseSyntaxTree(s1)));
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // Remove non-existing item 
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveSyntaxTrees(withErrorTree));
            Assert.Equal(4, comp.SyntaxTrees.Length);

            // Add syntaxtree with error
            comp = comp.AddSyntaxTrees(withErrorTree1);
            var error = comp.GetDiagnostics();
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);
            Assert.InRange(comp.GetDeclarationDiagnostics().Count(), 0, int.MaxValue);

            Assert.True(comp.SyntaxTrees.Contains(t1));

            SyntaxTree t4 = SyntaxFactory.ParseSyntaxTree("Using System;");
            SyntaxTree t5 = SyntaxFactory.ParseSyntaxTree("Usingsssssssssssss System;");
            SyntaxTree t6 = SyntaxFactory.ParseSyntaxTree("Import System;");

            // Overload with Hashset
            var hs = new HashSet<SyntaxTree> { t4, t5, t6 };
            var compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: hs);
            compCollection = compCollection.RemoveSyntaxTrees(hs);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            compCollection = compCollection.AddSyntaxTrees(hs).RemoveSyntaxTrees(t4, t5, t6);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with Collection
            var col = new Collection<SyntaxTree> { t4, t5, t6 };
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: col);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t5, t6);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t5).RemoveSyntaxTrees(col));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with ConcurrentStack
            var stack = new ConcurrentStack<SyntaxTree> { };
            stack.Push(t4);
            stack.Push(t5);
            stack.Push(t6);
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: stack);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(stack));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Overload with ConcurrentQueue
            var queue = new ConcurrentQueue<SyntaxTree> { };
            queue.Enqueue(t4);
            queue.Enqueue(t5);
            queue.Enqueue(t6);
            compCollection = CSharpCompilation.Create("Compilation", syntaxTrees: queue);
            compCollection = compCollection.RemoveSyntaxTrees(t4, t6, t5);
            Assert.Equal(0, compCollection.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => compCollection = compCollection.AddSyntaxTrees(t4, t6).RemoveSyntaxTrees(queue));
            Assert.Equal(0, compCollection.SyntaxTrees.Length);

            // Get valid binding
            var bind = comp.GetSemanticModel(syntaxTree: t1);
            Assert.Equal(t1, bind.SyntaxTree);
            Assert.Equal("C#", bind.Language);

            // Remove syntaxtree without error
            comp = comp.RemoveSyntaxTrees(t1);
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);

            // Remove syntaxtree with error
            comp = comp.RemoveSyntaxTrees(withErrorTree1);
            var e = comp.GetDiagnostics(cancellationToken: default(CancellationToken));
            Assert.Equal(0, comp.GetDiagnostics(cancellationToken: default(CancellationToken)).Count());

            // Add syntaxtree which is VB language
            comp = comp.AddSyntaxTrees(withErrorTreeVB);
            error = comp.GetDiagnostics(cancellationToken: CancellationToken.None);
            Assert.InRange(comp.GetDiagnostics().Count(), 0, int.MaxValue);

            comp = comp.RemoveSyntaxTrees(withErrorTreeVB);
            Assert.Equal(0, comp.GetDiagnostics().Count());

            // Add syntaxtree with error
            comp = comp.AddSyntaxTrees(withWarning);
            error = comp.GetDeclarationDiagnostics();
            Assert.InRange(error.Count(), 1, int.MaxValue);

            comp = comp.RemoveSyntaxTrees(withWarning);
            Assert.Equal(0, comp.GetDiagnostics().Count());

            // Compilation.Create with syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.False(withExpressionRootTree.HasCompilationUnitRoot, "how did we get a CompilationUnit root?");
            Assert.Throws<ArgumentException>(() => CSharpCompilation.Create("Compilation", new SyntaxTree[] { withExpressionRootTree }));

            // AddSyntaxTrees with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws<ArgumentException>(() => comp.AddSyntaxTrees(withExpressionRootTree));

            // ReplaceSyntaxTrees syntaxtree with a non-CompilationUnit root node: should throw an ArgumentException.
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(comp.SyntaxTrees[0], withExpressionRootTree));
        }

        [Fact]
        public void ChainedOperations()
        {
            var s1 = "using System.Linq;";
            var s2 = "";
            var s3 = "Import System";
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree(s1);
            SyntaxTree t2 = SyntaxFactory.ParseSyntaxTree(s2);
            SyntaxTree t3 = SyntaxFactory.ParseSyntaxTree(s3);

            var listSyntaxTree = new List<SyntaxTree>();
            listSyntaxTree.Add(t1);
            listSyntaxTree.Add(t2);

            // Remove second SyntaxTree
            CSharpCompilation comp = CSharpCompilation.Create(options: TestOptions.ReleaseDll, assemblyName: "Compilation", references: null, syntaxTrees: null);
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2);
            Assert.Equal(1, comp.SyntaxTrees.Length);

            // Remove mid SyntaxTree
            listSyntaxTree.Add(t3);
            comp = comp.RemoveSyntaxTrees(t1).AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(t2);
            Assert.Equal(2, comp.SyntaxTrees.Length);

            // remove list
            listSyntaxTree.Remove(t2);
            comp = comp.AddSyntaxTrees().RemoveSyntaxTrees(listSyntaxTree);
            comp = comp.AddSyntaxTrees(listSyntaxTree).RemoveSyntaxTrees(listSyntaxTree);
            Assert.Equal(0, comp.SyntaxTrees.Length);

            listSyntaxTree.Clear();
            listSyntaxTree.Add(t1);
            listSyntaxTree.Add(t1);
            // Chained operation count > 2
            Assert.Throws<ArgumentException>(() => comp = comp.AddSyntaxTrees(listSyntaxTree).AddReferences().ReplaceSyntaxTree(t1, t2));
            comp = comp.AddSyntaxTrees(t1).AddReferences().ReplaceSyntaxTree(t1, t2);

            Assert.Equal(1, comp.SyntaxTrees.Length);
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Create compilation with args is disordered
            CSharpCompilation comp1 = CSharpCompilation.Create(assemblyName: "Compilation", syntaxTrees: null, options: TestOptions.ReleaseDll, references: null);
            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var listRef = new List<MetadataReference>();
            listRef.Add(ref1);
            listRef.Add(ref1);

            // Remove with no args
            comp1 = comp1.AddReferences(listRef).AddSyntaxTrees(t1).RemoveReferences().RemoveSyntaxTrees();
            Assert.Equal(1, comp1.ExternalReferences.Length);
            Assert.Equal(1, comp1.SyntaxTrees.Length);
        }

        [WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")]
        [ClrOnlyFact]
        public void MissedModuleA()
        {
            var netModule1 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                sources: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a2",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                sources: new string[] {
                    @"
public class C2 { 
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var assembly = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule2.EmitToImageReference() },
                sources: new string[] {
                @"
public class C3 { 
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments("a1.netmodule"));

            assembly = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference(), netModule2.EmitToImageReference() },
                sources: new string[] {
                @"
public class C3 { 
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics();
            CompileAndVerify(assembly);
        }

        [WorkItem(713356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/713356")]
        [Fact]
        public void MissedModuleB_OneError()
        {
            var netModule1 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                sources: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a2",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                sources: new string[] {
                    @"
public class C2 { 
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var netModule3 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a3",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                sources: new string[] {
                    @"
public class C2a { 
public static void M() {
    var a = new C1();
}
}"
                });
            netModule3.VerifyEmitDiagnostics();

            var assembly = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule2.EmitToImageReference(), netModule3.EmitToImageReference() },
                sources: new string[] {
                @"
public class C3 { 
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(
                Diagnostic(ErrorCode.ERR_MissingNetModuleReference).WithArguments("a1.netmodule"));
        }

        [WorkItem(718500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718500")]
        [WorkItem(716762, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/716762")]
        [Fact]
        public void MissedModuleB_NoErrorForUnmanagedModules()
        {
            var netModule1 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                sources: new string[] {
                    @"
using System;
using System.Runtime.InteropServices;

public class C2 { 
    [DllImport(""user32.dll"", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, String text, String caption, uint type);
}"
                });
            netModule1.VerifyEmitDiagnostics();

            var assembly = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                sources: new string[] {
                @"
public class C3 { 
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics();
        }

        [WorkItem(715872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715872")]
        [Fact()]
        public void MissedModuleC()
        {
            var netModule1 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                sources: new string[] { "public class C1 {}" });
            netModule1.VerifyEmitDiagnostics();

            var netModule2 = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseModule,
                assemblyName: "a1",
                references: new MetadataReference[] { netModule1.EmitToImageReference() },
                sources: new string[] {
                    @"
public class C2 { 
public static void M() {
    var a = new C1();
}
}"
                });
            netModule2.VerifyEmitDiagnostics();

            var assembly = CreateCompilationWithMscorlib(
                options: TestOptions.ReleaseExe,
                assemblyName: "a",
                references: new MetadataReference[] { netModule1.EmitToImageReference(), netModule2.EmitToImageReference() },
                sources: new string[] {
                @"
public class C3 { 
public static void Main(string[] args) {
var a = new C2();
}
}"
            });
            assembly.VerifyEmitDiagnostics(Diagnostic(ErrorCode.ERR_NetModuleNameMustBeUnique).WithArguments("a1.netmodule"));
        }

        [Fact]
        public void MixedRefType()
        {
            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            var comp = CSharpCompilation.Create("Compilation");

            vbComp = vbComp.AddReferences(SystemRef);

            // Add VB reference to C# compilation
            foreach (var item in vbComp.References)
            {
                comp = comp.AddReferences(item);
                comp = comp.ReplaceReference(item, item);
            }
            Assert.Equal(1, comp.ExternalReferences.Length);

            var text1 = @"class A {}";
            var comp1 = CSharpCompilation.Create("Test1", new[] { SyntaxFactory.ParseSyntaxTree(text1) });
            var comp2 = CSharpCompilation.Create("Test2", new[] { SyntaxFactory.ParseSyntaxTree(text1) });

            var compRef1 = comp1.ToMetadataReference();
            var compRef2 = comp2.ToMetadataReference();

            var compRef = vbComp.ToMetadataReference(embedInteropTypes: true);

            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var ref2 = TestReferences.NetFx.v4_0_30319.System;

            // Add CompilationReference
            comp = CSharpCompilation.Create(
                "Test1",
                new[] { SyntaxFactory.ParseSyntaxTree(text1) },
                new MetadataReference[] { compRef1, compRef2 });

            Assert.Equal(2, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(compRef1));
            Assert.True(comp.References.Contains(compRef2));
            var smb = comp.GetReferencedAssemblySymbol(compRef1);
            Assert.Equal(smb.Kind, SymbolKind.Assembly);
            Assert.Equal("Test1", smb.Identity.Name, StringComparer.OrdinalIgnoreCase);

            // Mixed reference type
            comp = comp.AddReferences(ref1);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(ref1));

            // Replace Compilation reference with Assembly file reference
            comp = comp.ReplaceReference(compRef2, ref2);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(ref2));

            // Replace Assembly file reference with Compilation reference
            comp = comp.ReplaceReference(ref1, compRef2);
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.True(comp.References.Contains(compRef2));

            var modRef1 = TestReferences.MetadataTests.NetModule01.ModuleCS00;

            // Add Module file reference
            comp = comp.AddReferences(modRef1);
            // Not implemented code
            //var modSmb = comp.GetReferencedModuleSymbol(modRef1);
            //Assert.Equal("ModuleCS00.mod", modSmb.Name);
            //Assert.Equal(4, comp.References.Count);
            //Assert.True(comp.References.Contains(modRef1));

            //smb = comp.GetReferencedAssemblySymbol(reference: modRef1);
            //Assert.Equal(smb.Kind, SymbolKind.Assembly);
            //Assert.Equal("Test1", smb.Identity.Name, StringComparer.OrdinalIgnoreCase);

            // GetCompilationNamespace Not implemented(Derived Class AssemblySymbol)
            //var m = smb.GlobalNamespace.GetMembers();
            //var nsSmb = smb.GlobalNamespace.GetMembers("NS").Single() as NamespaceSymbol;
            //var ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            //var asbSmb = smb as Symbol;
            //var ns1 = comp.GetCompilationNamespace(ns: asbSmb as NamespaceSymbol);
            //Assert.Equal(ns1.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns1.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Get Referenced Module Symbol
            //var moduleSmb = comp.GetReferencedModuleSymbol(reference: modRef1);
            //Assert.Equal(SymbolKind.NetModule, moduleSmb.Kind);
            //Assert.Equal("ModuleCS00.mod", moduleSmb.Name, StringComparer.OrdinalIgnoreCase);

            // GetCompilationNamespace Not implemented(Derived Class ModuleSymbol)
            //nsSmb = moduleSmb.GlobalNamespace.GetMembers("Runtime").Single() as NamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            //var modSmbol = moduleSmb as Symbol;
            //ns1 = comp.GetCompilationNamespace(ns: modSmbol as NamespaceSymbol);
            //Assert.Equal(ns1.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns1.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Get Compilation Namespace
            //nsSmb = comp.GlobalNamespace;
            //ns = comp.GetCompilationNamespace(ns: nsSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class MergedNamespaceSymbol)
            //NamespaceSymbol merged = MergedNamespaceSymbol.Create(new NamespaceExtent(new MockAssemblySymbol("Merged")), null, null);
            //ns = comp.GetCompilationNamespace(ns: merged);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class RetargetingNamespaceSymbol)
            //Retargeting.RetargetingNamespaceSymbol retargetSmb = nsSmb as Retargeting.RetargetingNamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: retargetSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // GetCompilationNamespace Not implemented(Derived Class PENamespaceSymbol)
            //Symbols.Metadata.PE.PENamespaceSymbol pensSmb = nsSmb as Symbols.Metadata.PE.PENamespaceSymbol;
            //ns = comp.GetCompilationNamespace(ns: pensSmb);
            //Assert.Equal(ns.Kind, SymbolKind.Namespace);
            //Assert.True(String.Equals(ns.Name, "Compilation", StringComparison.OrdinalIgnoreCase));

            // Replace Module file reference with compilation reference
            comp = comp.RemoveReferences(compRef1).ReplaceReference(modRef1, compRef1);
            Assert.Equal(3, comp.ExternalReferences.Length);
            // Check the reference order after replace
            Assert.True(comp.ExternalReferences[2] is CSharpCompilationReference, "Expected compilation reference");
            Assert.Equal(compRef1, comp.ExternalReferences[2]);

            // Replace compilation Module file reference with Module file reference
            comp = comp.ReplaceReference(compRef1, modRef1);
            // Check the reference order after replace
            Assert.Equal(3, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Module, comp.ExternalReferences[2].Properties.Kind);
            Assert.Equal(modRef1, comp.ExternalReferences[2]);

            // Add VB compilation ref
            Assert.Throws<ArgumentException>(() => comp.AddReferences(compRef));

            foreach (var item in comp.References)
            {
                comp = comp.RemoveReferences(item);
            }
            Assert.Equal(0, comp.ExternalReferences.Length);

            // Not Implemented
            // var asmByteRef = MetadataReference.CreateFromImage(new byte[5], embedInteropTypes: true);
            //var asmObjectRef = new AssemblyObjectReference(assembly: System.Reflection.Assembly.GetAssembly(typeof(object)),embedInteropTypes :true);
            //comp =comp.AddReferences(asmByteRef, asmObjectRef);
            //Assert.Equal(2, comp.References.Count);
            //Assert.Equal(ReferenceKind.AssemblyBytes, comp.References[0].Kind);
            //Assert.Equal(ReferenceKind.AssemblyObject , comp.References[1].Kind);
            //Assert.Equal(asmByteRef, comp.References[0]);
            //Assert.Equal(asmObjectRef, comp.References[1]);
            //Assert.True(comp.References[0].EmbedInteropTypes);
            //Assert.True(comp.References[1].EmbedInteropTypes);
        }

        [Fact]
        public void NegGetCompilationNamespace()
        {
            var comp = CSharpCompilation.Create("Compilation");

            // Throw exception when the parameter of GetCompilationNamespace is null
            Assert.Throws<NullReferenceException>(
            delegate
            {
                comp.GetCompilationNamespace(namespaceSymbol: null);
            });
        }

        [WorkItem(537623, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537623")]
        [Fact]
        public void NegCreateCompilation()
        {
            Assert.Throws<ArgumentNullException>(() => CSharpCompilation.Create("foo", syntaxTrees: new SyntaxTree[] { null }));
            Assert.Throws<ArgumentNullException>(() => CSharpCompilation.Create("foo", references: new MetadataReference[] { null }));
        }

        [WorkItem(537637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537637")]
        [Fact]
        public void NegGetSymbol()
        {
            // Create Compilation with miss mid args
            var comp = CSharpCompilation.Create("Compilation");
            Assert.Null(comp.GetReferencedAssemblySymbol(reference: MscorlibRef));

            var modRef1 = TestReferences.MetadataTests.NetModule01.ModuleCS00;
            // Get not exist Referenced Module Symbol
            Assert.Null(comp.GetReferencedModuleSymbol(modRef1));

            // Throw exception when the parameter of GetReferencedAssemblySymbol is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.GetReferencedAssemblySymbol(null);
            });

            // Throw exception when the parameter of GetReferencedModuleSymbol is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                var modSmb1 = comp.GetReferencedModuleSymbol(null);
            });
        }

        [WorkItem(537778, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537778")]
        // Throw exception when the parameter of the parameter type of GetReferencedAssemblySymbol is VB.CompilationReference
        [Fact]
        public void NegGetSymbol1()
        {
            var opt = TestOptions.ReleaseDll;
            var comp = CSharpCompilation.Create("Compilation");
            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            vbComp = vbComp.AddReferences(SystemRef);
            var compRef = vbComp.ToMetadataReference();
            Assert.Throws<ArgumentException>(() => comp.AddReferences(compRef));

            // Throw exception when the parameter of GetBinding is null
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.GetSemanticModel(null);
            });

            // Throw exception when the parameter of GetTypeByNameAndArity is NULL 
            //Assert.Throws<Exception>(
            //delegate
            //{
            //    comp.GetTypeByNameAndArity(fullName: null, arity: 1);
            //});

            // Throw exception when the parameter of GetTypeByNameAndArity is less than 0 
            //Assert.Throws<Exception>(
            //delegate
            //{
            //    comp.GetTypeByNameAndArity(string.Empty, -4);
            //});
        }

        // Add already existing item 
        [Fact, WorkItem(537574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537574")]
        public void NegReference2()
        {
            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var ref2 = TestReferences.NetFx.v4_0_30319.System;
            var ref3 = TestReferences.NetFx.v4_0_30319.System_Data;
            var ref4 = TestReferences.NetFx.v4_0_30319.System_Xml;
            var comp = CSharpCompilation.Create("Compilation");

            comp = comp.AddReferences(ref1, ref1);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref1, comp.ExternalReferences[0]);

            var listRef = new List<MetadataReference> { ref1, ref2, ref3, ref4 };
            // Chained operation count > 3
            // ReplaceReference throws if the reference to be replaced is not found.
            comp = comp.AddReferences(listRef).AddReferences(ref2).RemoveReferences(ref1, ref3, ref4).ReplaceReference(ref2, ref2);
            Assert.Equal(1, comp.ExternalReferences.Length);
            Assert.Equal(MetadataImageKind.Assembly, comp.ExternalReferences[0].Properties.Kind);
            Assert.Equal(ref2, comp.ExternalReferences[0]);
            Assert.Throws<ArgumentException>(() => comp.AddReferences(listRef).AddReferences(ref2).RemoveReferences(ref1, ref2, ref3, ref4).ReplaceReference(ref2, ref2));
        }

        // Add a new invalid item 
        [Fact, WorkItem(537575, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537575")]
        public void NegReference3()
        {
            var ref1 = InvalidRef;
            var comp = CSharpCompilation.Create("Compilation");
            // Remove non-existing item
            Assert.Throws<ArgumentException>(() => comp = comp.RemoveReferences(ref1));
            // Add a new invalid item
            comp = comp.AddReferences(ref1);
            Assert.Equal(1, comp.ExternalReferences.Length);
            // Replace an non-existing item with another invalid item
            Assert.Throws<ArgumentException>(() => comp = comp.ReplaceReference(MscorlibRef, ref1));
            Assert.Equal(1, comp.ExternalReferences.Length);
        }

        // Replace an non-existing item with null
        [Fact, WorkItem(537567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537567")]
        public void NegReference4()
        {
            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var comp = CSharpCompilation.Create("Compilation");

            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceReference(ref1, null);
            });

            // Replace null and the arg order of replace is vise 
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp = comp.ReplaceReference(newReference: ref1, oldReference: null);
            });
        }

        // Replace a non-existing item with another valid item
        [Fact, WorkItem(537566, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537566")]
        public void NegReference5()
        {
            var ref1 = TestReferences.NetFx.v4_0_30319.mscorlib;
            var ref2 = TestReferences.NetFx.v4_0_30319.System_Xml;
            var comp = CSharpCompilation.Create("Compilation");
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceReference(ref1, ref2);
            });


            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            // Replace an non-existing item with another valid item and disorder the args
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp.ReplaceReference(newReference: TestReferences.NetFx.v4_0_30319.System, oldReference: ref2);
            });
            Assert.Equal(0, comp.SyntaxTrees.Length);
            Assert.Throws<ArgumentException>(() => comp.ReplaceSyntaxTree(newTree: SyntaxFactory.ParseSyntaxTree("Using System;"), oldTree: t1));
            Assert.Equal(0, comp.SyntaxTrees.Length);
        }

        [WorkItem(527256, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527256")]
        // Throw exception when the parameter of SyntaxTrees.Contains is null
        [Fact]
        public void NegSyntaxTreesContains()
        {
            var comp = CSharpCompilation.Create("Compilation");
            Assert.False(comp.SyntaxTrees.Contains(null));
        }

        [WorkItem(537784, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537784")]
        // Throw exception when the parameter of GetSpecialType() is out of range
        [Fact]
        public void NegGetSpecialType()
        {
            var comp = CSharpCompilation.Create("Compilation");

            // Throw exception when the parameter of GetBinding is out of range
            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType((SpecialType)100);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType(SpecialType.None);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType((SpecialType)000);
            });

            Assert.Throws<ArgumentOutOfRangeException>(
            delegate
            {
                comp.GetSpecialType(default(SpecialType));
            });
        }

        [WorkItem(538168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538168")]
        // Replace an non-existing item with another valid item and disorder the args
        [Fact]
        public void NegTree2()
        {
            var comp = CSharpCompilation.Create("API");
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp = comp.ReplaceSyntaxTree(newTree: SyntaxFactory.ParseSyntaxTree("Using System;"), oldTree: t1);
            });
        }

        [WorkItem(537576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537576")]
        // Add already existing item
        [Fact]
        public void NegSynTree1()
        {
            var comp = CSharpCompilation.Create("Compilation");
            SyntaxTree t1 = SyntaxFactory.ParseSyntaxTree("Using System;");
            Assert.Throws<ArgumentException>(() => (comp.AddSyntaxTrees(t1, t1)));
            Assert.Equal(0, comp.SyntaxTrees.Length);
        }

        [Fact]
        public void NegSynTree()
        {
            var comp = CSharpCompilation.Create("Compilation");
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree("Using Foo;");
            // Throw exception when add null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.AddSyntaxTrees(null);
            });

            // Throw exception when Remove null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp.RemoveSyntaxTrees(null);
            });

            // No exception when replacing a SyntaxTree with null
            var compP = comp.AddSyntaxTrees(syntaxTree);
            comp = compP.ReplaceSyntaxTree(syntaxTree, null);
            Assert.Equal(0, comp.SyntaxTrees.Length);

            // Throw exception when remove null SyntaxTree
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                comp = comp.ReplaceSyntaxTree(null, syntaxTree);
            });

            var s1 = "Imports System.Text";
            SyntaxTree t1 = VB.VisualBasicSyntaxTree.ParseText(s1);
            SyntaxTree t2 = t1;
            var t3 = t2;

            var vbComp = VB.VisualBasicCompilation.Create("CompilationVB");
            vbComp = vbComp.AddSyntaxTrees(t1, VB.VisualBasicSyntaxTree.ParseText("Using Foo;"));
            // Throw exception when cast SyntaxTree
            foreach (var item in vbComp.SyntaxTrees)
            {
                t3 = item;
                Exception invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.AddSyntaxTrees(t3);
                });
                invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.RemoveSyntaxTrees(t3);
                });
                invalidCastSynTreeEx = Assert.Throws<InvalidCastException>(
                delegate
                {
                    comp = comp.ReplaceSyntaxTree(t3, t3);
                });
            }
            // Get Binding with tree is not exist
            SyntaxTree t4 = SyntaxFactory.ParseSyntaxTree(s1);
            Assert.Throws<ArgumentException>(
            delegate
            {
                comp.RemoveSyntaxTrees(new SyntaxTree[] { t4 }).GetSemanticModel(t4);
            });
        }

        [Fact]
        public void GetEntryPoint_Exe()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();

            var mainMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>("Main");

            Assert.Equal(mainMethod, compilation.GetEntryPoint(default(CancellationToken)));

            var entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol);
            entryPointAndDiagnostics.Diagnostics.Verify();
        }

        [Fact]
        public void GetEntryPoint_Dll()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll);
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.GetEntryPoint(default(CancellationToken)));
            Assert.Null(compilation.GetEntryPointAndDiagnostics(default(CancellationToken)));
        }

        [Fact]
        public void GetEntryPoint_Module()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseModule);
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.GetEntryPoint(default(CancellationToken)));
            Assert.Null(compilation.GetEntryPointAndDiagnostics(default(CancellationToken)));
        }

        [Fact]
        public void CreateCompilationForModule()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            // equivalent of csc with no /moduleassemblyname specified:
            var compilation = CSharpCompilation.Create(assemblyName: null, options: TestOptions.ReleaseModule, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.AssemblyName);
            Assert.Equal("?", compilation.Assembly.Name);
            Assert.Equal("?", compilation.Assembly.Identity.Name);

            // no name is allowed for assembly as well, although it isn't useful:
            compilation = CSharpCompilation.Create(assemblyName: null, options: TestOptions.ReleaseDll, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyDiagnostics();

            Assert.Null(compilation.AssemblyName);
            Assert.Equal("?", compilation.Assembly.Name);
            Assert.Equal("?", compilation.Assembly.Identity.Name);

            // equivalent of csc with /moduleassemblyname specified:
            compilation = CSharpCompilation.Create(assemblyName: "ModuleAssemblyName", options: TestOptions.ReleaseModule, syntaxTrees: new[] { Parse(source) }, references: new[] { MscorlibRef });
            compilation.VerifyDiagnostics();

            Assert.Equal("ModuleAssemblyName", compilation.AssemblyName);
            Assert.Equal("ModuleAssemblyName", compilation.Assembly.Name);
            Assert.Equal("ModuleAssemblyName", compilation.Assembly.Identity.Name);
        }

        [WorkItem(3719, "https://github.com/dotnet/roslyn/issues/3719")]
        [Fact]
        public void GetEntryPoint_Script()
        {
            var source = @"System.Console.WriteLine(1);";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Main>");
            Assert.NotNull(scriptMethod);

            var method = compilation.GetEntryPoint(default(CancellationToken));
            Assert.Equal(method, scriptMethod);
            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
        }

        [Fact]
        public void GetEntryPoint_Script_MainIgnored()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Main>");
            Assert.NotNull(scriptMethod);

            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));
        }

        [Fact]
        public void GetEntryPoint_Submission()
        {
            var source = @"1 + 1";
            var compilation = CSharpCompilation.CreateScriptCompilation("sub",
                references: new[] { MscorlibRef },
                syntaxTree: Parse(source, options: TestOptions.Script));
            compilation.VerifyDiagnostics();

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Factory>");
            Assert.NotNull(scriptMethod);

            var method = compilation.GetEntryPoint(default(CancellationToken));
            Assert.Equal(method, scriptMethod);
            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify();
        }

        [Fact]
        public void GetEntryPoint_Submission_MainIgnored()
        {
            var source = @"
class A
{
    static void Main() { }
}
";
            var compilation = CSharpCompilation.CreateScriptCompilation("sub",
                references: new[] { MscorlibRef },
                syntaxTree: Parse(source, options: TestOptions.Script));
            compilation.VerifyDiagnostics(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));

            Assert.True(compilation.IsSubmission);

            var scriptMethod = compilation.GetMember<MethodSymbol>("Script.<Factory>");
            Assert.NotNull(scriptMethod);

            var entryPoint = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(entryPoint.MethodSymbol, scriptMethod);
            entryPoint.Diagnostics.Verify(
                // (4,17): warning CS7022: The entry point of the program is global script code; ignoring 'A.Main()' entry point.
                //     static void Main() { }
                Diagnostic(ErrorCode.WRN_MainIgnored, "Main").WithArguments("A.Main()").WithLocation(4, 17));
        }

        [Fact]
        public void GetEntryPoint_MainType()
        {
            var source = @"
class A
{
    static void Main() { }
}

class B
{
    static void Main() { }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseExe.WithMainTypeName("B"));
            compilation.VerifyDiagnostics();

            var mainMethod = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("B").GetMember<MethodSymbol>("Main");

            Assert.Equal(mainMethod, compilation.GetEntryPoint(default(CancellationToken)));

            var entryPointAndDiagnostics = compilation.GetEntryPointAndDiagnostics(default(CancellationToken));
            Assert.Equal(mainMethod, entryPointAndDiagnostics.MethodSymbol);
            entryPointAndDiagnostics.Diagnostics.Verify();
        }

        [Fact]
        public void CanReadAndWriteDefaultWin32Res()
        {
            var comp = CSharpCompilation.Create("Compilation");
            var mft = new MemoryStream(new byte[] { 0, 1, 2, 3, });
            var res = comp.CreateDefaultWin32Resources(true, false, mft, null);
            var list = comp.MakeWin32ResourceList(res, new DiagnosticBag());
            Assert.Equal(2, list.Count);
        }

        [Fact, WorkItem(750437, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/750437")]
        public void ConflictingAliases()
        {
            var alias = TestReferences.NetFx.v4_0_30319.System.WithAliases(new[] { "alias" });

            var text =
@"extern alias alias;
using alias=alias;
class myClass : alias::Uri
{
}";
            var comp = CreateCompilationWithMscorlib(text, references: new MetadataReference[] { alias });
            Assert.Equal(2, comp.References.Count());
            Assert.Equal("alias", comp.References.Last().Properties.Aliases.Single());
            comp.VerifyDiagnostics(
                // (2,1): error CS1537: The using alias 'alias' appeared previously in this namespace
                // using alias=alias;
                Diagnostic(ErrorCode.ERR_DuplicateAlias, "using alias=alias;").WithArguments("alias"),
                // (3,17): error CS0104: 'alias' is an ambiguous reference between '<global namespace>' and '<global namespace>'
                // class myClass : alias::Uri
                Diagnostic(ErrorCode.ERR_AmbigContext, "alias").WithArguments("alias", "<global namespace>", "<global namespace>"));
        }

        [WorkItem(546088, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546088")]
        [Fact]
        public void CompilationDiagsIncorrectResult()
        {
            string source1 = @"
using SysAttribute = System.Attribute;
using MyAttribute = MyAttribute2Attribute;

public class MyAttributeAttribute : SysAttribute  {}
public class MyAttribute2Attribute : SysAttribute {}

[MyAttribute]
public class TestClass
{
}
";
            string source2 = @"";

            // Ask for model diagnostics first.
            {
                var compilation = CreateCompilationWithMscorlib(sources: new string[] { source1, source2 });

                var tree2 = compilation.SyntaxTrees[1]; //tree for empty file
                var model2 = compilation.GetSemanticModel(tree2);

                model2.GetDiagnostics().Verify(); // None, since the file is empty.
                compilation.GetDiagnostics().Verify(
                    // (8,2): error CS1614: 'MyAttribute' is ambiguous between 'MyAttribute2Attribute' and 'MyAttributeAttribute'; use either '@MyAttribute' or 'MyAttributeAttribute'
                    // [MyAttribute]
                    Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "MyAttribute").WithArguments("MyAttribute", "MyAttribute2Attribute", "MyAttributeAttribute"));
            }

            // Ask for compilation diagnostics first.
            {
                var compilation = CreateCompilationWithMscorlib(sources: new string[] { source1, source2 });

                var tree2 = compilation.SyntaxTrees[1]; //tree for empty file
                var model2 = compilation.GetSemanticModel(tree2);

                compilation.GetDiagnostics().Verify(
                    // (10,2): error CS1614: 'MyAttribute' is ambiguous between 'MyAttribute2Attribute' and 'MyAttributeAttribute'; use either '@MyAttribute' or 'MyAttributeAttribute'
                    // [MyAttribute]
                    Diagnostic(ErrorCode.ERR_AmbiguousAttribute, "MyAttribute").WithArguments("MyAttribute", "MyAttribute2Attribute", "MyAttributeAttribute"));
                model2.GetDiagnostics().Verify(); // None, since the file is empty.
            }
        }

        [Fact]
        public void ReferenceManagerReuse_WithOptions()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseExe);
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication));
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll);
            Assert.True(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule));
            Assert.False(c1.ReferenceManagerEquals(c2));


            c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseModule);

            c2 = c1.WithOptions(TestOptions.ReleaseExe);
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(TestOptions.ReleaseDll);
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(new CSharpCompilationOptions(OutputKind.WindowsApplication));
            Assert.False(c1.ReferenceManagerEquals(c2));

            c2 = c1.WithOptions(new CSharpCompilationOptions(OutputKind.NetModule).WithAllowUnsafe(true));
            Assert.True(c1.ReferenceManagerEquals(c2));
        }

        [Fact]
        public void ReferenceManagerReuse_WithMetadataReferenceResolver()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(new TestMetadataReferenceResolver()));

            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithOptions(TestOptions.ReleaseDll.WithMetadataReferenceResolver(c1.Options.MetadataReferenceResolver));
            Assert.True(c1.ReferenceManagerEquals(c3));
        }

        [Fact]
        public void ReferenceManagerReuse_WithXmlFileResolver()
        {
            var c1 = CSharpCompilation.Create("c", options: TestOptions.ReleaseDll);

            var c2 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(new XmlFileResolver(null)));
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithOptions(TestOptions.ReleaseDll.WithXmlReferenceResolver(c1.Options.XmlReferenceResolver));
            Assert.True(c1.ReferenceManagerEquals(c3));
        }

        [Fact]
        public void ReferenceManagerReuse_WithName()
        {
            var c1 = CSharpCompilation.Create("c1");

            var c2 = c1.WithAssemblyName("c2");
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c1.WithAssemblyName("c1");
            Assert.True(c1.ReferenceManagerEquals(c3));

            var c4 = c1.WithAssemblyName(null);
            Assert.False(c1.ReferenceManagerEquals(c4));

            var c5 = c4.WithAssemblyName(null);
            Assert.True(c4.ReferenceManagerEquals(c5));
        }

        [Fact]
        public void ReferenceManagerReuse_WithReferences()
        {
            var c1 = CSharpCompilation.Create("c1");

            var c2 = c1.WithReferences(new[] { MscorlibRef });
            Assert.False(c1.ReferenceManagerEquals(c2));

            var c3 = c2.WithReferences(new[] { MscorlibRef, SystemCoreRef });
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.AddReferences(SystemCoreRef);
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.RemoveAllReferences();
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.ReplaceReference(MscorlibRef, SystemCoreRef);
            Assert.False(c3.ReferenceManagerEquals(c2));

            c3 = c2.RemoveReferences(MscorlibRef);
            Assert.False(c3.ReferenceManagerEquals(c2));
        }

        [Fact]
        public void ReferenceManagerReuse_WithSyntaxTrees()
        {
            var ta = Parse("class C { }");

            var tb = Parse(@"
class C { }", options: TestOptions.Script);

            var tc = Parse(@"
#r ""bar""  // error: #r in regular code
class D { }");

            var tr = Parse(@"
#r ""foo""
class C { }", options: TestOptions.Script);

            var ts = Parse(@"
#r ""bar""
class C { }", options: TestOptions.Script);

            var a = CSharpCompilation.Create("c", syntaxTrees: new[] { ta });

            // add:

            var ab = a.AddSyntaxTrees(tb);
            Assert.True(a.ReferenceManagerEquals(ab));

            var ac = a.AddSyntaxTrees(tc);
            Assert.True(a.ReferenceManagerEquals(ac));

            var ar = a.AddSyntaxTrees(tr);
            Assert.False(a.ReferenceManagerEquals(ar));

            var arc = ar.AddSyntaxTrees(tc);
            Assert.True(ar.ReferenceManagerEquals(arc));

            // remove:

            var ar2 = arc.RemoveSyntaxTrees(tc);
            Assert.True(arc.ReferenceManagerEquals(ar2));

            var c = arc.RemoveSyntaxTrees(ta, tr);
            Assert.False(arc.ReferenceManagerEquals(c));

            var none1 = c.RemoveSyntaxTrees(tc);
            Assert.True(c.ReferenceManagerEquals(none1));

            var none2 = arc.RemoveAllSyntaxTrees();
            Assert.False(arc.ReferenceManagerEquals(none2));

            var none3 = ac.RemoveAllSyntaxTrees();
            Assert.True(ac.ReferenceManagerEquals(none3));

            // replace:

            var asc = arc.ReplaceSyntaxTree(tr, ts);
            Assert.False(arc.ReferenceManagerEquals(asc));

            var brc = arc.ReplaceSyntaxTree(ta, tb);
            Assert.True(arc.ReferenceManagerEquals(brc));

            var abc = arc.ReplaceSyntaxTree(tr, tb);
            Assert.False(arc.ReferenceManagerEquals(abc));

            var ars = arc.ReplaceSyntaxTree(tc, ts);
            Assert.False(arc.ReferenceManagerEquals(ars));
        }

        private sealed class EvolvingTestReference : PortableExecutableReference
        {
            private readonly IEnumerator<Metadata> _metadataSequence;
            public int QueryCount;

            public EvolvingTestReference(IEnumerable<Metadata> metadataSequence)
                : base(MetadataReferenceProperties.Assembly)
            {
                _metadataSequence = metadataSequence.GetEnumerator();
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                return DocumentationProvider.Default;
            }

            protected override Metadata GetMetadataImpl()
            {
                QueryCount++;
                _metadataSequence.MoveNext();
                return _metadataSequence.Current;
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public void MetadataConsistencyWhileEvolvingCompilation()
        {
            var md1 = AssemblyMetadata.CreateFromImage(CreateCompilationWithMscorlib("public class C { }").EmitToArray());
            var md2 = AssemblyMetadata.CreateFromImage(CreateCompilationWithMscorlib("public class D { }").EmitToArray());

            var reference = new EvolvingTestReference(new[] { md1, md2 });

            var c1 = CreateCompilation("public class Main { public static C C; }", new[] { MscorlibRef, reference, reference });
            var c2 = c1.WithAssemblyName("c2");
            var c3 = c2.AddSyntaxTrees(Parse("public class Main2 { public static int a; }"));
            var c4 = c3.WithOptions(new CSharpCompilationOptions(OutputKind.NetModule));
            var c5 = c4.WithReferences(new[] { MscorlibRef, reference });

            c3.VerifyDiagnostics();
            c1.VerifyDiagnostics();
            c4.VerifyDiagnostics();
            c2.VerifyDiagnostics();

            Assert.Equal(1, reference.QueryCount);

            c5.VerifyDiagnostics(
                // (1,36): error CS0246: The type or namespace name 'C' could not be found (are you missing a using directive or an assembly reference?)
                // public class Main2 { public static C C; }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "C").WithArguments("C"));

            Assert.Equal(2, reference.QueryCount);
        }

        [Fact]
        public unsafe void LinkedNetmoduleMetadataMustProvideFullPEImage()
        {
            var netModule = TestResources.MetadataTests.NetModule01.ModuleCS00;
            PEHeaders h = new PEHeaders(new MemoryStream(netModule));

            fixed (byte* ptr = &netModule[h.MetadataStartOffset])
            {
                using (var mdModule = ModuleMetadata.CreateFromMetadata((IntPtr)ptr, h.MetadataSize))
                {
                    var c = CSharpCompilation.Create("Foo", references: new[] { MscorlibRef, mdModule.GetReference(display: "ModuleCS00") }, options: TestOptions.ReleaseDll);
                    c.VerifyDiagnostics(
                        // error CS7098: Linked netmodule metadata must provide a full PE image: 'ModuleCS00'.
                        Diagnostic(ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage).WithArguments("ModuleCS00").WithLocation(1, 1));
                }
            }
        }

        [Fact]
        public void AppConfig1()
        {
            var references = new MetadataReference[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.silverlight_v5_0_5_0.System
            };

            var compilation = CreateCompilation(
                new[] { Parse("") },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            compilation.VerifyDiagnostics(
                // error CS1703: Multiple assemblies with equivalent identity have been imported: 'System.dll' and 'System.v5.0.5.0_silverlight.dll'. Remove one of the duplicate references.
                Diagnostic(ErrorCode.ERR_DuplicateImport).WithArguments("System.dll", "System.v5.0.5.0_silverlight.dll"));

            var appConfig = new MemoryStream(Encoding.UTF8.GetBytes(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>"));

            var comparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfig);

            compilation = CreateCompilation(
                new[] { Parse("") },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(comparer));

            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AppConfig2()
        {
            // Create a dll with a reference to .net system
            string libSource = @"
using System.Runtime.Versioning;
public class C { public static FrameworkName Foo() { return null; }}";
            var libComp = CreateCompilationWithMscorlib(
                libSource,
                references: new[] { TestReferences.NetFx.v4_0_30319.System },
                options: TestOptions.ReleaseDll);

            libComp.VerifyDiagnostics();

            var refData = libComp.EmitToArray();
            var mdRef = MetadataReference.CreateFromImage(refData);

            var references = new[]
            {
                TestReferences.NetFx.v4_0_30319.mscorlib,
                TestReferences.NetFx.v4_0_30319.System,
                TestReferences.NetFx.silverlight_v5_0_5_0.System,
                mdRef
            };

            // Source references the type in the dll
            string src1 = @"class A { public static void Main(string[] args) { C.Foo(); } }";

            var c1 = CreateCompilation(
                new[] { Parse(src1) },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default));

            c1.VerifyDiagnostics(
                // error CS1703: Multiple assemblies with equivalent identity have been imported: 'System.dll' and 'System.v5.0.5.0_silverlight.dll'. Remove one of the duplicate references.
                Diagnostic(ErrorCode.ERR_DuplicateImport).WithArguments("System.dll", "System.v5.0.5.0_silverlight.dll"),
                // error CS7069: Reference to type 'System.Runtime.Versioning.FrameworkName' claims it is defined in 'System', but it could not be found
                Diagnostic(ErrorCode.ERR_MissingTypeInAssembly, "C.Foo").WithArguments(
                    "System.Runtime.Versioning.FrameworkName", "System"));

            var appConfig = new MemoryStream(Encoding.UTF8.GetBytes(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>"));

            var comparer = DesktopAssemblyIdentityComparer.LoadFromXml(appConfig);

            var src2 = @"class A { public static void Main(string[] args) { C.Foo(); } }";
            var c2 = CreateCompilation(
                new[] { Parse(src2) },
                references,
                options: TestOptions.ReleaseDll.WithAssemblyIdentityComparer(comparer));

            c2.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(797640, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/797640")]
        public void GetMetadataReferenceAPITest()
        {
            var comp = CSharpCompilation.Create("Compilation");
            var metadata = TestReferences.NetFx.v4_0_30319.mscorlib;
            comp = comp.AddReferences(metadata);
            var assemblySmb = comp.GetReferencedAssemblySymbol(metadata);
            var reference = comp.GetMetadataReference(assemblySmb);
            Assert.NotNull(reference);

            var comp2 = CSharpCompilation.Create("Compilation");
            comp2 = comp2.AddReferences(metadata);
            var reference2 = comp2.GetMetadataReference(assemblySmb);
            Assert.NotNull(reference2);
        }

        [Fact]
        public void ConsistentParseOptions()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            var tree2 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            var tree3 = SyntaxFactory.ParseSyntaxTree("", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));

            var assemblyName = GetUniqueName();
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            CSharpCompilation.Create(assemblyName, new[] { tree1, tree2 }, new[] { MscorlibRef }, compilationOptions);
            Assert.Throws(typeof(ArgumentException), () =>
            {
                CSharpCompilation.Create(assemblyName, new[] { tree1, tree3 }, new[] { MscorlibRef }, compilationOptions);
            });
        }

        [Fact]
        public void SubmissionCompilation_Errors()
        {
            var genericParameter = typeof(List<>).GetGenericArguments()[0];
            var open = typeof(Dictionary<,>).MakeGenericType(typeof(int), genericParameter);
            var ptr = typeof(int).MakePointerType();
            var byref = typeof(int).MakeByRefType();

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", returnType: byref));

            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: genericParameter));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: open));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: typeof(void)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: typeof(int)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: ptr));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", globalsType: byref));

            var s0 = CSharpCompilation.CreateScriptCompilation("a0", globalsType: typeof(List<int>));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a1", previousScriptCompilation: s0, globalsType: typeof(List<bool>)));

            // invalid options:
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseExe));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.NetModule)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeMetadata)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsRuntimeApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithOutputKind(OutputKind.WindowsApplication)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithCryptoKeyContainer("foo")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithCryptoKeyFile("foo.snk")));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithDelaySign(true)));
            Assert.Throws<ArgumentException>(() => CSharpCompilation.CreateScriptCompilation("a", options: TestOptions.ReleaseDll.WithDelaySign(false)));
        }

        [Fact]
        public void HasSubmissionResult()
        {
            Assert.False(CSharpCompilation.CreateScriptCompilation("sub").HasSubmissionResult());
            Assert.True(CreateSubmission("1", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("1;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("void foo() { }", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("using System;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("int i;", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("System.Console.WriteLine();", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.False(CreateSubmission("System.Console.WriteLine()", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.True(CreateSubmission("null", parseOptions: TestOptions.Script).HasSubmissionResult());
            Assert.True(CreateSubmission("System.Console.WriteLine", parseOptions: TestOptions.Script).HasSubmissionResult());
        }

        /// <summary>
        /// Previous submission has to have no errors.
        /// </summary>
        [Fact]
        public void PreviousSubmissionWithError()
        {
            var s0 = CreateSubmission("int a = \"x\";");
            s0.VerifyDiagnostics(
                // (1,9): error CS0029: Cannot implicitly convert type 'string' to 'int'
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""x""").WithArguments("string", "int"));

            Assert.Throws<InvalidOperationException>(() => CreateSubmission("a + 1", previous: s0));
        }

        #region Script return values

        [Fact]
        public void ReturnNullAsObject()
        {
            var script = CreateSubmission("return null;", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnStringAsObject()
        {
            var script = CreateSubmission("return \"¡Hola!\";", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnIntAsObject()
        {
            var script = CreateSubmission("return 42;", returnType: typeof(object));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void TrailingReturnVoidAsObject()
        {
            var script = CreateSubmission("return", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (1,7): error CS1733: Expected expression
                // return
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(1, 7),
                // (1,7): error CS1002: ; expected
                // return
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 7));
            Assert.False(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnIntAsInt()
        {
            var script = CreateSubmission("return 42;", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnNullResultType()
        {
            // test that passing null is the same as passing typeof(object)
            var script = CreateSubmission("return 42;", returnType: null);
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnNoSemicolon()
        {
            var script = CreateSubmission("return 42", returnType: typeof(uint));
            script.VerifyDiagnostics(
                // (1,10): error CS1002: ; expected
                // return 42
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 10));
            Assert.False(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnAwait()
        {
            var script = CreateSubmission("return await System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission("return await System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(Task<int>));
            script.VerifyDiagnostics(
                // (1,8): error CS0029: Cannot implicitly convert type 'int' to 'System.Threading.Tasks.Task<int>'
                // return await System.Threading.Tasks.Task.FromResult(42);
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "await System.Threading.Tasks.Task.FromResult(42)").WithArguments("int", "System.Threading.Tasks.Task<int>").WithLocation(1, 8));
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnTaskNoAwait()
        {
            var script = CreateSubmission("return System.Threading.Tasks.Task.FromResult(42);", returnType: typeof(int));
            script.VerifyDiagnostics(
                // (1,8): error CS4016: Since this is an async method, the return expression must be of type 'int' rather than 'Task<int>'
                // return System.Threading.Tasks.Task.FromResult(42);
                Diagnostic(ErrorCode.ERR_BadAsyncReturnExpression, "System.Threading.Tasks.Task.FromResult(42)").WithArguments("int").WithLocation(1, 8));
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedScopes()
        {
            var script = CreateSubmission(@"
bool condition = false;
if (condition)
{
    return 1;
}
else
{
    return -1;
}", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedScopeWithTrailingExpression()
        {
            var script = CreateSubmission(@"
if (true)
{
    return 1;
}
System.Console.WriteLine();", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (6,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(6, 1));
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission(@"
if (true)
{
    return 1;
}
System.Console.WriteLine()", returnType: typeof(object));
            script.VerifyDiagnostics(
                // (6,1): warning CS0162: Unreachable code detected
                // System.Console.WriteLine();
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(6, 1));
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedScopeNoTrailingExpression()
        {
            var script = CreateSubmission(@"
bool condition = false;
if (condition)
{
    return 1;
}
System.Console.WriteLine();", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedMethod()
        {
            var script = CreateSubmission(@"
int TopMethod()
{
    return 42;
}", returnType: typeof(string));
            script.VerifyDiagnostics();
            Assert.False(script.HasSubmissionResult());

            script = CreateSubmission(@"
object TopMethod()
{
    return new System.Exception();
}
TopMethod().ToString()", returnType: typeof(string));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedLambda()
        {
            var script = CreateSubmission(@"
System.Func<object> f = () =>
{
    return new System.Exception();
};
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            script = CreateSubmission(@"
System.Func<object> f = () => new System.Exception();
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnInNestedAnonymousMethod()
        {
            var script = CreateSubmission(@"
System.Func<object> f = delegate ()
{
    return new System.Exception();
};
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void LoadedFileWithWrongReturnType()
        {
            var resolver = TestSourceReferenceResolver.Create(
                KeyValuePair.Create("a.csx", "return \"Who returns a string?\";"));
            var script = CreateSubmission(@"
#load ""a.csx""
42", returnType: typeof(int), options: TestOptions.DebugDll.WithSourceReferenceResolver(resolver));
            script.VerifyDiagnostics(
                // a.csx(1,8): error CS0029: Cannot implicitly convert type 'string' to 'int'
                // return "Who returns a string?"
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""Who returns a string?""").WithArguments("string", "int").WithLocation(1, 8),
                // (3,1): warning CS0162: Unreachable code detected
                // 42
                Diagnostic(ErrorCode.WRN_UnreachableCode, "42").WithLocation(3, 1));
            Assert.True(script.HasSubmissionResult());
        }

        [Fact]
        public void ReturnVoidInNestedMethodOrLambda()
        {
            var script = CreateSubmission(@"
void M1()
{
    return;
}
System.Action a = () => { return; };
42", returnType: typeof(int));
            script.VerifyDiagnostics();
            Assert.True(script.HasSubmissionResult());

            var compilation = CreateCompilationWithMscorlib45(@"
void M1()
{
    return;
}
System.Action a = () => { return; };
42", parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();
        }

        #endregion
    }
}
