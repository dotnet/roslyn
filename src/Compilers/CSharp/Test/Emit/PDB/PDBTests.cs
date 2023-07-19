// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTests : CSharpPDBTestBase
    {
        public const PdbValidationOptions SyntaxOffsetPdbValidationOptions =
            PdbValidationOptions.ExcludeDocuments |
            PdbValidationOptions.ExcludeSequencePoints |
            PdbValidationOptions.ExcludeScopes |
            PdbValidationOptions.ExcludeNamespaces;

        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        #region General

        [Fact]
        public void EmitDebugInfoForSourceTextWithoutEncoding1()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class A { }", encoding: null, path: "Foo.cs");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class B { }", encoding: null, path: "");
            var tree3 = SyntaxFactory.ParseSyntaxTree(SourceText.From("class C { }", encoding: null), path: "Bar.cs");
            var tree4 = SyntaxFactory.ParseSyntaxTree("class D { }", encoding: Encoding.UTF8, path: "Baz.cs");

            var comp = CSharpCompilation.Create("Compilation", new[] { tree1, tree2, tree3, tree4 }, new[] { MscorlibRef }, options: TestOptions.ReleaseDll);

            var result = comp.Emit(new MemoryStream(), pdbStream: new MemoryStream());
            result.Diagnostics.Verify(
                // Foo.cs(1,1): error CS8055: Cannot emit debug information for a source text without encoding.
                Diagnostic(ErrorCode.ERR_EncodinglessSyntaxTree, "class A { }").WithLocation(1, 1),
                // Bar.cs(1,1): error CS8055: Cannot emit debug information for a source text without encoding.
                Diagnostic(ErrorCode.ERR_EncodinglessSyntaxTree, "class C { }").WithLocation(1, 1));

            Assert.False(result.Success);
        }

        [Fact]
        public void EmitDebugInfoForSourceTextWithoutEncoding2()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class A { public void F() { } }", encoding: Encoding.Unicode, path: "Foo.cs");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class B { public void F() { } }", encoding: null, path: "");
            var tree3 = SyntaxFactory.ParseSyntaxTree("class C { public void F() { } }", encoding: new UTF8Encoding(true, false), path: "Bar.cs");
            var tree4 = SyntaxFactory.ParseSyntaxTree(SourceText.From("class D { public void F() { } }", new UTF8Encoding(false, false)), path: "Baz.cs");

            var comp = CSharpCompilation.Create("Compilation", new[] { tree1, tree2, tree3, tree4 }, new[] { MscorlibRef }, options: TestOptions.ReleaseDll);

            var result = comp.Emit(new MemoryStream(), pdbStream: new MemoryStream());
            result.Diagnostics.Verify();
            Assert.True(result.Success);

            var hash1 = CryptographicHashProvider.ComputeSha1(Encoding.Unicode.GetBytesWithPreamble(tree1.ToString())).ToArray();
            var hash3 = CryptographicHashProvider.ComputeSha1(new UTF8Encoding(true, false).GetBytesWithPreamble(tree3.ToString())).ToArray();
            var hash4 = CryptographicHashProvider.ComputeSha1(new UTF8Encoding(false, false).GetBytesWithPreamble(tree4.ToString())).ToArray();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""Foo.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""" + BitConverter.ToString(hash1) + @""" />
    <file id=""2"" name="""" language=""C#"" />
    <file id=""3"" name=""Bar.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""" + BitConverter.ToString(hash3) + @""" />
    <file id=""4"" name=""Baz.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""" + BitConverter.ToString(hash4) + @""" />
  </files>
</symbols>", options: PdbValidationOptions.ExcludeMethods);
        }

        [Fact]
        public void SourceGeneratedFiles()
        {
            Compilation compilation = CreateCompilation("class C { }", options: TestOptions.DebugDll, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics();

            Assert.Single(compilation.SyntaxTrees);

            var testGenerator = new TestSourceGenerator()
            {
                ExecuteImpl = context =>
                {
                    context.AddSource("hint1", "class G1 { void F() {} }");
                    context.AddSource("hint2", SourceText.From("class G2 { void F() {} }", Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256));

                    Assert.Throws<ArgumentException>(() => context.AddSource("hint3", SourceText.From("class G3 { void F() {} }", encoding: null, checksumAlgorithm: SourceHashAlgorithm.Sha256)));
                }
            };

            var driver = CSharpGeneratorDriver.Create(new[] { testGenerator }, parseOptions: TestOptions.Regular);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            var result = outputCompilation.Emit(new MemoryStream(), pdbStream: new MemoryStream());
            result.Diagnostics.Verify();
            Assert.True(result.Success);

            var path1 = Path.Combine("Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint1.cs");
            var path2 = Path.Combine("Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint2.cs");

            outputCompilation.VerifyPdb($@"
<symbols>
  <files>
    <file id=""1"" name=""{path1}"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""D8-87-89-A3-FE-EA-FD-AB-49-31-5A-25-B0-05-6B-6F-00-00-C2-DD"" />
    <file id=""2"" name=""{path2}"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""64-A9-4B-81-04-84-18-CD-73-F7-F8-3B-06-32-4B-9C-F9-36-D4-7A-7B-D0-2F-34-ED-8C-B7-AA-48-43-55-35"" />
  </files>
</symbols>", options: PdbValidationOptions.ExcludeMethods);
        }

        [Fact]
        public void EmitDebugInfoForSynthesizedSyntaxTree()
        {
            var tree1 = SyntaxFactory.ParseCompilationUnit(@"
#line 1 ""test.cs""
class C { void M() {} }
").SyntaxTree;
            var tree2 = SyntaxFactory.ParseCompilationUnit(@"
class D { void M() {} }
").SyntaxTree;

            var comp = CSharpCompilation.Create("test", new[] { tree1, tree2 }, TargetFrameworkUtil.StandardReferences, TestOptions.DebugDll);

            var result = comp.Emit(new MemoryStream(), pdbStream: new MemoryStream());
            result.Diagnostics.Verify();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
    <file id=""2"" name=""test.cs"" language=""C#"" />
  </files>
</symbols>
", format: DebugInformationFormat.PortablePdb, options: PdbValidationOptions.ExcludeMethods);
        }

        [WorkItem(846584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846584")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void RelativePathForExternalSource_Sha1_Windows()
        {
            var text1 = WithWindowsLineBreaks(@"
#pragma checksum ""..\Test2.cs"" ""{406ea660-64cf-4c82-b6f0-42d48172a799}"" ""BA8CBEA9C2EFABD90D53B616FB80A081""

public class C
{
    public void InitializeComponent() {
        #line 4 ""..\Test2.cs""
        InitializeComponent();
        #line default
    }
}
");

            var compilation = CreateCompilation(
                new[] { Parse(text1, @"C:\Folder1\Folder2\Test1.cs") },
                options: TestOptions.DebugDll.WithSourceReferenceResolver(SourceFileResolver.Default));

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""C:\Folder1\Folder2\Test1.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""40-A6-20-02-2E-60-7D-4F-2D-A8-F4-A6-ED-2E-0E-49-8D-9F-D7-EB"" />
    <file id=""2"" name=""C:\Folder1\Test2.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""BA-8C-BE-A9-C2-EF-AB-D9-0D-53-B6-16-FB-80-A0-81"" />
  </files>
  <methods>
    <method containingType=""C"" name=""InitializeComponent"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""39"" endLine=""6"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""9"" endLine=""4"" endColumn=""31"" document=""2"" />
        <entry offset=""0x8"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(846584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846584")]
        [ConditionalFact(typeof(UnixLikeOnly))]
        public void RelativePathForExternalSource_Sha1_Unix()
        {
            var text1 = WithWindowsLineBreaks(@"
#pragma checksum ""../Test2.cs"" ""{406ea660-64cf-4c82-b6f0-42d48172a799}"" ""BA8CBEA9C2EFABD90D53B616FB80A081""

public class C
{
    public void InitializeComponent() {
        #line 4 ""../Test2.cs""
        InitializeComponent();
        #line default
    }
}
");

            var compilation = CreateCompilation(
                new[] { Parse(text1, @"/Folder1/Folder2/Test1.cs") },
                options: TestOptions.DebugDll.WithSourceReferenceResolver(SourceFileResolver.Default));

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""/Folder1/Folder2/Test1.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""82-08-07-BA-BA-52-02-D8-1D-1F-7C-E7-95-8A-6C-04-64-FF-50-31"" />
    <file id=""2"" name=""/Folder1/Test2.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""BA-8C-BE-A9-C2-EF-AB-D9-0D-53-B6-16-FB-80-A0-81"" />
  </files>
  <methods>
    <method containingType=""C"" name=""InitializeComponent"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""39"" endLine=""6"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""9"" endLine=""4"" endColumn=""31"" document=""2"" />
        <entry offset=""0x8"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SymWriterErrors()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilation(source0, options: TestOptions.DebugDll);

            // Verify full metadata contains expected rows.
            var result = compilation.Emit(
                peStream: new MemoryStream(),
                metadataPEStream: null,
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: null,
                cancellationToken: default,
                win32Resources: null,
                manifestResources: null,
                options: null,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                rebuildData: null,
                testData: new CompilationTestData() { SymWriterFactory = _ => new MockSymUnmanagedWriter() });

            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'MockSymUnmanagedWriter error message'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("MockSymUnmanagedWriter error message"));

            Assert.False(result.Success);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SymWriterErrors2()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilation(source0, options: TestOptions.DebugDll);

            // Verify full metadata contains expected rows.
            var result = compilation.Emit(
                peStream: new MemoryStream(),
                metadataPEStream: null,
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: null,
                cancellationToken: default,
                win32Resources: null,
                manifestResources: null,
                options: null,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                rebuildData: null,
                testData: new CompilationTestData() { SymWriterFactory = SymWriterTestUtilities.ThrowingFactory });

            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'The version of Windows PDB writer is older than required: '<lib name>''
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(string.Format(CodeAnalysisResources.SymWriterOlderVersionThanRequired, "<lib name>")));

            Assert.False(result.Success);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SymWriterErrors3()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilation(source0, options: TestOptions.DebugDll.WithDeterministic(true));

            // Verify full metadata contains expected rows.
            var result = compilation.Emit(
                peStream: new MemoryStream(),
                metadataPEStream: null,
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: null,
                cancellationToken: default,
                win32Resources: null,
                manifestResources: null,
                options: null,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                rebuildData: null,
                testData: new CompilationTestData() { SymWriterFactory = SymWriterTestUtilities.ThrowingFactory });

            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'Windows PDB writer doesn't support deterministic compilation: '<lib name>''
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(string.Format(CodeAnalysisResources.SymWriterNotDeterministic, "<lib name>")));

            Assert.False(result.Success);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SymWriterErrors4()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateCompilation(source0);

            // Verify full metadata contains expected rows.
            var result = compilation.Emit(
                peStream: new MemoryStream(),
                metadataPEStream: null,
                pdbStream: new MemoryStream(),
                xmlDocumentationStream: null,
                cancellationToken: default,
                win32Resources: null,
                manifestResources: null,
                options: null,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                rebuildData: null,
                testData: new CompilationTestData() { SymWriterFactory = _ => throw new DllNotFoundException("xxx") });

            result.Diagnostics.Verify(
                // error CS0041: Unexpected error writing debug information -- 'xxx'
                Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments("xxx"));

            Assert.False(result.Success);
        }

        [WorkItem(1067635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067635")]
        [Fact]
        public void SuppressDynamicAndEncCDIForWinRT()
        {
            var source = @"
public class C
{
    public static void F()
    {
        dynamic a = 1;
        int b = 2;
        foreach (var x in new[] { 1,2,3 })
        {
            System.Console.WriteLine(a * b);
        }
    }
}
";

            var debug = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.DebugWinMD);
            debug.VerifyPdb(@"
<symbols>
    <files>
      <file id=""1"" name="""" language=""C#"" />
    </files>
    <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" document=""1"" />
        <entry offset=""0xa"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" document=""1"" />
        <entry offset=""0xb"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x24"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" document=""1"" />
        <entry offset=""0xe6"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe7"" hidden=""true"" document=""1"" />
        <entry offset=""0xeb"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0xf4"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xf5"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xf5"" attributes=""0"" />
        <scope startOffset=""0x24"" endOffset=""0xe7"">
          <local name=""x"" il_index=""4"" il_start=""0x24"" il_end=""0xe7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb, options: PdbValidationOptions.SkipConversionValidation);

            var release = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.ReleaseWinMD);
            release.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" document=""1"" />
        <entry offset=""0x9"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x22"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x26"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" document=""1"" />
        <entry offset=""0xdd"" hidden=""true"" document=""1"" />
        <entry offset=""0xe1"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0xea"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xeb"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb, options: PdbValidationOptions.SkipConversionValidation);
        }

        [WorkItem(1067635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067635")]
        [Fact]
        public void SuppressTupleElementNamesCDIForWinRT()
        {
            var source =
@"class C
{
    static void F()
    {
        (int A, int B) o = (1, 2);
    }
}";

            var debug = CreateCompilation(source, options: TestOptions.DebugWinMD);
            debug.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""35"" document=""1"" />
        <entry offset=""0x9"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb, options: PdbValidationOptions.SkipConversionValidation);

            var release = CreateCompilation(source, options: TestOptions.ReleaseWinMD);
            release.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb, options: PdbValidationOptions.SkipConversionValidation);
        }

        [Fact]
        public void DuplicateDocuments()
        {
            var source1 = @"class C { static void F() { } }";
            var source2 = @"class D { static void F() { } }";

            var tree1 = Parse(source1, @"foo.cs");
            var tree2 = Parse(source2, @"foo.cs");

            var comp = CreateCompilation(new[] { tree1, tree2 });

            // the first file wins (checksum CB 22 ...)
            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""foo.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""CB-22-D8-03-D3-27-32-64-2C-BC-7D-67-5D-E3-CB-AC-D1-64-25-83"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""29"" endLine=""1"" endColumn=""30"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""D"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""29"" endLine=""1"" endColumn=""30"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void CustomDebugEntryPoint_DLL()
        {
            var source = @"class C { static void F() { } }";

            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var f = c.GetMember<MethodSymbol>("C.F");

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""F"" />
  <methods/>
</symbols>", debugEntryPoint: f.GetPublicSymbol(), options: PdbValidationOptions.ExcludeScopes | PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeCustomDebugInformation);

            var peReader = new PEReader(c.EmitToArray(debugEntryPoint: f.GetPublicSymbol()));
            int peEntryPointToken = peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress;

            Assert.Equal(0, peEntryPointToken);
        }

        [Fact]
        public void CustomDebugEntryPoint_EXE()
        {
            var source = @"class M { static void Main() { } } class C { static void F<S>() { } }";

            var c = CreateCompilation(source, options: TestOptions.DebugExe);
            var f = c.GetMember<MethodSymbol>("C.F");

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""F"" />
  <methods/>
</symbols>", debugEntryPoint: f.GetPublicSymbol(), options: PdbValidationOptions.ExcludeScopes | PdbValidationOptions.ExcludeSequencePoints | PdbValidationOptions.ExcludeCustomDebugInformation);

            var peReader = new PEReader(c.EmitToArray(debugEntryPoint: f.GetPublicSymbol()));
            int peEntryPointToken = peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress;

            var mdReader = peReader.GetMetadataReader();
            var methodDef = mdReader.GetMethodDefinition((MethodDefinitionHandle)MetadataTokens.Handle(peEntryPointToken));
            Assert.Equal("Main", mdReader.GetString(methodDef.Name));
        }

        [Fact]
        public void CustomDebugEntryPoint_Errors()
        {
            var source1 = @"class C { static void F() { } } class D<T> { static void G<S>() {} }";
            var source2 = @"class C { static void F() { } }";

            var c1 = CreateCompilation(source1, options: TestOptions.DebugDll);
            var c2 = CreateCompilation(source2, options: TestOptions.DebugDll);

            var f1 = c1.GetMember<MethodSymbol>("C.F");
            var f2 = c2.GetMember<MethodSymbol>("C.F");
            var g = c1.GetMember<MethodSymbol>("D.G");
            var d = c1.GetMember<NamedTypeSymbol>("D");
            Assert.NotNull(f1);
            Assert.NotNull(f2);
            Assert.NotNull(g);
            Assert.NotNull(d);

            var stInt = c1.GetSpecialType(SpecialType.System_Int32);
            var d_t_g_int = g.Construct(stInt);
            var d_int = d.Construct(stInt);
            var d_int_g = d_int.GetMember<MethodSymbol>("G");
            var d_int_g_int = d_int_g.Construct(stInt);

            var result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: f2.GetPublicSymbol());
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_t_g_int.GetPublicSymbol());
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_int_g.GetPublicSymbol());
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_int_g_int.GetPublicSymbol());
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));
        }

        [Fact]
        [WorkItem(768862, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/768862")]
        public void TestLargeLineDelta()
        {
            var verbatim = string.Join("\r\n", Enumerable.Repeat("x", 1000));

            var source = $@"
class C {{ public static void Main() => System.Console.WriteLine(@""{verbatim}""); }}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""2"" startColumn=""40"" endLine=""1001"" endColumn=""4"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
", format: DebugInformationFormat.PortablePdb);

            // Native PDBs only support spans with line delta <= 127 (7 bit)
            // https://github.com/Microsoft/microsoft-pdb/blob/main/include/cvinfo.h#L4621
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""2"" startColumn=""40"" endLine=""129"" endColumn=""4"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
", format: DebugInformationFormat.Pdb);
        }

        [Fact]
        [WorkItem(20118, "https://github.com/dotnet/roslyn/issues/20118")]
        public void TestLargeStartAndEndColumn_SameLine()
        {
            var spaces = new string(' ', 0x10000);

            var source = $@"
class C 
{{ 
    public static void Main() => 
        {spaces}System.Console.WriteLine(""{spaces}""); 
}}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""65533"" endLine=""5"" endColumn=""65534"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        [WorkItem(20118, "https://github.com/dotnet/roslyn/issues/20118")]
        public void TestLargeStartAndEndColumn_DifferentLine()
        {
            var spaces = new string(' ', 0x10000);

            var source = $@"
class C 
{{ 
    public static void Main() => 
        {spaces}System.Console.WriteLine(
        ""{spaces}""); 
}}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""65534"" endLine=""6"" endColumn=""65534"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        #endregion

        #region Method Bodies

        [Fact]
        public void TestBasic()
        {
            var source = WithWindowsLineBreaks(@"
class Program
{
    Program() { }

    static void Main(string[] args)
    {
        Program p = new Program();
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""35"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <local name=""p"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestSimpleLocals()
        {
            var source = WithWindowsLineBreaks(@"
class C 
{ 
    void Method()
    {   //local at method scope
        object version = 6;
        System.Console.WriteLine(""version {0}"", version);
        {
            //a scope that defines no locals
            {
                //a nested local
                object foob = 1;
                System.Console.WriteLine(""foob {0}"", foob);
            }
            {
                //a nested local
                int foob1 = 1;
                System.Console.WriteLine(""foob1 {0}"", foob1);
            }
            System.Console.WriteLine(""Eva"");
        }
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Method"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""44"" />
          <slot kind=""0"" offset=""246"" />
          <slot kind=""0"" offset=""402"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""28"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""58"" document=""1"" />
        <entry offset=""0x14"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x15"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""14"" document=""1"" />
        <entry offset=""0x16"" startLine=""12"" startColumn=""17"" endLine=""12"" endColumn=""33"" document=""1"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""60"" document=""1"" />
        <entry offset=""0x29"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2a"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2b"" startLine=""17"" startColumn=""17"" endLine=""17"" endColumn=""31"" document=""1"" />
        <entry offset=""0x2d"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""62"" document=""1"" />
        <entry offset=""0x3e"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" document=""1"" />
        <entry offset=""0x3f"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""45"" document=""1"" />
        <entry offset=""0x4a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4b"" startLine=""22"" startColumn=""5"" endLine=""22"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4c"">
        <local name=""version"" il_index=""0"" il_start=""0x0"" il_end=""0x4c"" attributes=""0"" />
        <scope startOffset=""0x15"" endOffset=""0x2a"">
          <local name=""foob"" il_index=""1"" il_start=""0x15"" il_end=""0x2a"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x2a"" endOffset=""0x3f"">
          <local name=""foob1"" il_index=""2"" il_start=""0x2a"" il_end=""0x3f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(7244, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/7244")]
        public void ConstructorsWithoutInitializers()
        {
            var source = WithWindowsLineBreaks(
@"class C
{
    C()
    {
        object o;
    }
    C(object x)
    {
        object y = x;
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""8"" document=""1"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <scope startOffset=""0x7"" endOffset=""0x9"">
          <local name=""o"" il_index=""0"" il_start=""0x7"" il_end=""0x9"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""16"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""22"" document=""1"" />
        <entry offset=""0xa"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <scope startOffset=""0x7"" endOffset=""0xb"">
          <local name=""y"" il_index=""0"" il_start=""0x7"" il_end=""0xb"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(7244, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/7244")]
        [Fact]
        public void ConstructorsWithInitializers()
        {
            var source = WithWindowsLineBreaks(
@"class C
{
    static object G = 1;
    object F = G;
    C()
    {
        object o;
    }
    C(object x)
    {
        object y = x;
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""18"" document=""1"" />
        <entry offset=""0xb"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""8"" document=""1"" />
        <entry offset=""0x12"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x13"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <scope startOffset=""0x12"" endOffset=""0x14"">
          <local name=""o"" il_index=""0"" il_start=""0x12"" il_end=""0x14"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""18"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""16"" document=""1"" />
        <entry offset=""0x12"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x13"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""22"" document=""1"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <scope startOffset=""0x12"" endOffset=""0x16"">
          <local name=""y"" il_index=""0"" il_start=""0x12"" il_end=""0x16"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ConstructorsWithSharedInitializers()
        {
            var source = @"
class B
{
    public B(int z) {}
}

class C : B
{
    int a = 1;
    bool b = true;

    C(int x)
        : base(3)
    {
        a = x;
    }
    
    C(bool y)
        : base(4)
    {
        b = y;
    }
}

";

            var c = CompileAndVerify(source);

            c.VerifyMethodBody("C..ctor(int)", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  // sequence point: int a = 1;
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int C.a""
  // sequence point: bool b = true;
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""bool C.b""
  // sequence point: base(3)
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.3
  IL_0010:  call       ""B..ctor(int)""
  // sequence point: a = x;
  IL_0015:  ldarg.0
  IL_0016:  ldarg.1
  IL_0017:  stfld      ""int C.a""
  // sequence point: }
  IL_001c:  ret
}
");
            c.VerifyMethodBody("C..ctor(bool)", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  // sequence point: int a = 1;
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int C.a""
  // sequence point: bool b = true;
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.1
  IL_0009:  stfld      ""bool C.b""
  // sequence point: base(4)
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.4
  IL_0010:  call       ""B..ctor(int)""
  // sequence point: b = y;
  IL_0015:  ldarg.0
  IL_0016:  ldarg.1
  IL_0017:  stfld      ""bool C.b""
  // sequence point: }
  IL_001c:  ret
}
");
        }

        /// <summary>
        /// Although the debugging info attached to DebuggerHidden method is not used by the debugger 
        /// (the debugger doesn't ever stop in the method) Dev11 emits the info and so do we.
        /// 
        /// StepThrough method needs the information if JustMyCode is disabled and a breakpoint is set within the method.
        /// NonUserCode method needs the information if JustMyCode is disabled.
        /// 
        /// It's up to the tool that consumes the debugging information, not the compiler to decide whether to ignore the info or not.
        /// BTW, the information can actually be retrieved at runtime from the PDB file via Reflection StackTrace.
        /// </summary>
        [Fact]
        public void MethodsWithDebuggerAttributes()
        {
            var source = WithWindowsLineBreaks(@"
using System;
using System.Diagnostics;

class Program
{
    [DebuggerHidden]
    static void Hidden()
    {
        int x = 1;
        Console.WriteLine(x);
    }

    [DebuggerStepThrough]
    static void StepThrough()
    {
        int y = 1;
        Console.WriteLine(y);
    }

    [DebuggerNonUserCode]
    static void NonUserCode()
    {
        int z = 1;
        Console.WriteLine(z);
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Hidden"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""30"" document=""1"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <namespace name=""System"" />
        <namespace name=""System.Diagnostics"" />
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""StepThrough"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Hidden"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""30"" document=""1"" />
        <entry offset=""0xa"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""y"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""Program"" name=""NonUserCode"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Hidden"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""30"" document=""1"" />
        <entry offset=""0xa"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb"">
        <local name=""z"" il_index=""0"" il_start=""0x0"" il_end=""0xb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        /// <summary>
        /// If a synthesized method contains any user code,
        /// the method must have a sequence point at
        /// offset 0 for correct stepping behavior.
        /// </summary>
        [WorkItem(804681, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/804681")]
        [Fact]
        public void SequencePointAtOffset0()
        {
            string source = WithWindowsLineBreaks(
@"using System;
class C
{
    static Func<object, int> F = x =>
    {
        Func<object, int> f = o => 1;
        Func<Func<object, int>, Func<object, int>> g = h => y => h(y);
        return g(f)(null);
    };
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
 <symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".cctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""-45"" />
          <lambda offset=""-147"" />
          <lambda offset=""-109"" />
          <lambda offset=""-45"" />
          <lambda offset=""-40"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""9"" endColumn=""7"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-118"" />
          <slot kind=""0"" offset=""-54"" />
          <slot kind=""21"" offset=""-147"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""38"" document=""1"" />
        <entry offset=""0x21"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""71"" document=""1"" />
        <entry offset=""0x41"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""27"" document=""1"" />
        <entry offset=""0x51"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <local name=""f"" il_index=""0"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
        <local name=""g"" il_index=""1"" il_start=""0x0"" il_end=""0x53"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_1"" parameterNames=""o"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""36"" endLine=""6"" endColumn=""37"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__2_2"" parameterNames=""h"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-45"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""7"" startColumn=""61"" endLine=""7"" endColumn=""70"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1a"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass2_0"" name=""&lt;.cctor&gt;b__3"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""66"" endLine=""7"" endColumn=""70"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        /// <summary>
        /// Leading trivia is not included in the syntax offset.
        /// </summary>
        [Fact]
        public void SyntaxOffsetInPresenceOfTrivia_Methods()
        {
            string source = @"
class C
{
    public static void Main1() /*Comment1*/{/*Comment2*/int a = 1;/*Comment3*/}/*Comment4*/
    public static void Main2() {/*Comment2*/int a = 2;/*Comment3*/}/*Comment4*/
}";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // verify that both syntax offsets are the same
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main1"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""17"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""44"" endLine=""4"" endColumn=""45"" document=""1"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""57"" endLine=""4"" endColumn=""67"" document=""1"" />
        <entry offset=""0x3"" startLine=""4"" startColumn=""79"" endLine=""4"" endColumn=""80"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C"" name=""Main2"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""17"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""32"" endLine=""5"" endColumn=""33"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""45"" endLine=""5"" endColumn=""55"" document=""1"" />
        <entry offset=""0x3"" startLine=""5"" startColumn=""67"" endLine=""5"" endColumn=""68"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x4"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        /// <summary>
        /// Leading and trailing trivia are not included in the syntax offset.
        /// </summary>
        [Fact]
        public void SyntaxOffsetInPresenceOfTrivia_Initializers()
        {
            string source = @"
using System;
class C1
{
    public static Func<int> e=() => 0;
    public static Func<int> f/*Comment0*/=/*Comment1*/() => 1;/*Comment2*/
    public static Func<int> g=() => 2;
}
class C2
{
    public static Func<int> e=() => 0;
    public static Func<int> f=/*Comment1*/() => 1;
    public static Func<int> g=() => 2;
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // verify that syntax offsets of both .cctor's are the same
            c.VerifyPdb("C1..cctor", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C1"" name="".cctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>4</methodOrdinal>
          <lambda offset=""-29"" />
          <lambda offset=""-9"" />
          <lambda offset=""-1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""39"" document=""1"" />
        <entry offset=""0x15"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""63"" document=""1"" />
        <entry offset=""0x2a"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""39"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");

            c.VerifyPdb("C2..cctor", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C2"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".cctor"" />
        <encLambdaMap>
          <methodOrdinal>4</methodOrdinal>
          <lambda offset=""-29"" />
          <lambda offset=""-9"" />
          <lambda offset=""-1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""39"" document=""1"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""51"" document=""1"" />
        <entry offset=""0x2a"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""39"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region ReturnStatement

        [Fact]
        public void Return_Method1()
        {
            var source = WithWindowsLineBreaks(@"
class Program
{
    static int Main()
    {
        return 1;
    }
}
");

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // In order to place a breakpoint on the closing brace we need to save the return expression value to 
            // a local and then load it again (since sequence point needs an empty stack). This variable has to be marked as long-lived.
            v.VerifyIL("Program.Main", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0005
 -IL_0005:  ldloc.0
  IL_0006:  ret
}", sequencePoints: "Program.Main");

            v.VerifyPdb("Program.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""18"" document=""1"" />
        <entry offset=""0x5"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Return_Property1()
        {
            var source = WithWindowsLineBreaks(@"
class C
{
    static int P
    {
        get { return 1; }
    }
}
");

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // In order to place a breakpoint on the closing brace we need to save the return expression value to 
            // a local and then load it again (since sequence point needs an empty stack). This variable has to be marked as long-lived.
            v.VerifyIL("C.P.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (int V_0)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0005
 -IL_0005:  ldloc.0
  IL_0006:  ret
}", sequencePoints: "C.get_P");

            v.VerifyPdb("C.get_P", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""13"" endLine=""6"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""15"" endLine=""6"" endColumn=""24"" document=""1"" />
        <entry offset=""0x5"" startLine=""6"" startColumn=""25"" endLine=""6"" endColumn=""26"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Return_Void1()
        {
            var source = @"
class Program
{
    static void Main()
    {
        return;
    }
}
";

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Main", @"
{
  // Code size        4 (0x4)
  .maxstack  0
 -IL_0000:  nop
 -IL_0001:  br.s       IL_0003
 -IL_0003:  ret
}", sequencePoints: "Program.Main");
        }

        [Fact]
        public void Return_ExpressionBodied1()
        {
            var source = @"
class Program
{
    static int Main() => 1;
}
";

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  1
 -IL_0000:  ldc.i4.1
  IL_0001:  ret
}", sequencePoints: "Program.Main");
        }

        [Fact]
        public void Return_FromExceptionHandler1()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class Program
{
    static int Main() 
    {
        try
        {
            Console.WriteLine();
            return 1;
        }
        catch (Exception)
        {
            return 2;
        }
    }
}
");
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Main", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int V_0)
 -IL_0000:  nop
  .try
  {
   -IL_0001:  nop
   -IL_0002:  call       ""void System.Console.WriteLine()""
    IL_0007:  nop
   -IL_0008:  ldc.i4.1
    IL_0009:  stloc.0
    IL_000a:  leave.s    IL_0012
  }
  catch System.Exception
  {
   -IL_000c:  pop
   -IL_000d:  nop
   -IL_000e:  ldc.i4.2
    IL_000f:  stloc.0
    IL_0010:  leave.s    IL_0012
  }
 -IL_0012:  ldloc.0
  IL_0013:  ret
}", sequencePoints: "Program.Main");

            v.VerifyPdb("Program.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""33"" document=""1"" />
        <entry offset=""0x8"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""22"" document=""1"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""26"" document=""1"" />
        <entry offset=""0xd"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""22"" document=""1"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region IfStatement

        [Fact]
        public void IfStatement()
        {
            var source = WithWindowsLineBreaks(@"
class C 
{ 
    void Method()
    {   
        bool b = true;
        if (b)
        {
            string s = ""true"";
            System.Console.WriteLine(s);
        } 
        else 
        {
            string s = ""false"";
            int i = 1;

            while (i < 100)
            {
                int j = i, k = 1;
                System.Console.WriteLine(j);  
                i = j + k;                
            }         
            
            i = i + 1;
        }
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Method"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""1"" offset=""38"" />
          <slot kind=""0"" offset=""76"" />
          <slot kind=""0"" offset=""188"" />
          <slot kind=""0"" offset=""218"" />
          <slot kind=""0"" offset=""292"" />
          <slot kind=""0"" offset=""299"" />
          <slot kind=""1"" offset=""240"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""15"" document=""1"" />
        <entry offset=""0x5"" hidden=""true"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x9"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""31"" document=""1"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""1"" />
        <entry offset=""0x16"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x17"" hidden=""true"" document=""1"" />
        <entry offset=""0x19"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1a"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""32"" document=""1"" />
        <entry offset=""0x20"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""23"" document=""1"" />
        <entry offset=""0x23"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""14"" document=""1"" />
        <entry offset=""0x26"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""26"" document=""1"" />
        <entry offset=""0x2a"" startLine=""19"" startColumn=""28"" endLine=""19"" endColumn=""33"" document=""1"" />
        <entry offset=""0x2d"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""45"" document=""1"" />
        <entry offset=""0x35"" startLine=""21"" startColumn=""17"" endLine=""21"" endColumn=""27"" document=""1"" />
        <entry offset=""0x3c"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""14"" document=""1"" />
        <entry offset=""0x3d"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""28"" document=""1"" />
        <entry offset=""0x45"" hidden=""true"" document=""1"" />
        <entry offset=""0x49"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4f"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" document=""1"" />
        <entry offset=""0x50"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x51"">
        <local name=""b"" il_index=""0"" il_start=""0x0"" il_end=""0x51"" attributes=""0"" />
        <scope startOffset=""0x8"" endOffset=""0x17"">
          <local name=""s"" il_index=""2"" il_start=""0x8"" il_end=""0x17"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x19"" endOffset=""0x50"">
          <local name=""s"" il_index=""3"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
          <local name=""i"" il_index=""4"" il_start=""0x19"" il_end=""0x50"" attributes=""0"" />
          <scope startOffset=""0x25"" endOffset=""0x3d"">
            <local name=""j"" il_index=""5"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
            <local name=""k"" il_index=""6"" il_start=""0x25"" il_end=""0x3d"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region WhileStatement

        [WorkItem(538299, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538299")]
        [Fact]
        public void WhileStatement()
        {
            var source = @"using System;

public class SeqPointForWhile
{
    public static void Main()
    {
        SeqPointForWhile obj = new SeqPointForWhile();
        obj.While(234);
    }

    int field;
    public void While(int p)
    {
        while (p > 0) // SeqPt should be generated at the end of loop
        {
            p = (int)(p / 2);

            if (p > 100)
            {
                continue;
            }
            else if (p > 10)
            {
                int x = p;
                field = x;
            }
            else
            {
                int x = p;
                Console.WriteLine(x);
                break;
            }
        }
        field = -1;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);

            // Offset 0x01 should be:
            //  <entry offset=""0x1"" hidden=""true"" document=""1"" />
            // Move original offset 0x01 to 0x33
            //  <entry offset=""0x33"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" document=""1"" />
            // 
            // Note: 16707566 == 0x00FEEFEE
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" document=""1"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""1"" />
        <entry offset=""0xf"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x10"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointForWhile"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" document=""1"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""1"" />
        <entry offset=""0xc"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" document=""1"" />
        <entry offset=""0x11"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" document=""1"" />
        <entry offset=""0x13"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1a"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1d"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""38"" document=""1"" />
        <entry offset=""0x22"" startLine=""31"" startColumn=""17"" endLine=""31"" endColumn=""23"" document=""1"" />
        <entry offset=""0x24"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" document=""1"" />
        <entry offset=""0x28"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""20"" document=""1"" />
        <entry offset=""0x2f"" startLine=""35"" startColumn=""5"" endLine=""35"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x30"">
        <scope startOffset=""0x11"" endOffset=""0x1a"">
          <local name=""x"" il_index=""0"" il_start=""0x11"" il_end=""0x1a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region ForStatement

        [Fact]
        public void ForStatement1()
        {
            var source = WithWindowsLineBreaks(@"
class C
{
    static bool F(int i) { return true; }
    static void G(int i) { }
    
    static void M()
    {
        for (int i = 1; F(i); G(i))
        {
            System.Console.WriteLine(1);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" parameterNames=""i"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""20"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x3"" hidden=""true"" document=""1"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""41"" document=""1"" />
        <entry offset=""0xd"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""9"" startColumn=""31"" endLine=""9"" endColumn=""35"" document=""1"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""29"" document=""1"" />
        <entry offset=""0x1c"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x20"">
        <scope startOffset=""0x1"" endOffset=""0x1f"">
          <local name=""i"" il_index=""0"" il_start=""0x1"" il_end=""0x1f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForStatement2()
        {
            var source = @"
class C
{
    static void M()
    {
        for (;;)
        {
            System.Console.WriteLine(1);
        }
    }
}";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForStatement3()
        {
            var source = WithWindowsLineBreaks(@"
class C
{
    static void M()
    {
        int i = 0;
        for (;;i++)
        {
            System.Console.WriteLine(i);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" hidden=""true"" document=""1"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x6"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""41"" document=""1"" />
        <entry offset=""0xd"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""16"" endLine=""7"" endColumn=""19"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <local name=""i"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region ForEachStatement

        [Fact]
        public void ForEachStatement_String()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var c in ""hello"")
        {
            System.Console.WriteLine(c);
        }
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) '"hello"'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) Hidden index increment.
            // 10) 'in'
            // 11) Close brace at end of method

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""34"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x11"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" document=""1"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForEachStatement_Array()
        {
            var source = WithWindowsLineBreaks(@"
public class C
{
    public static void Main()
    {
        foreach (var x in new int[2])
        {
            System.Console.WriteLine(x);
        }
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new int[2]'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) Hidden index increment.
            // 10) 'in'
            // 11) Close brace at end of method

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""37"" document=""1"" />
        <entry offset=""0xb"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" document=""1"" />
        <entry offset=""0x11"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" document=""1"" />
        <entry offset=""0x19"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1a"" hidden=""true"" document=""1"" />
        <entry offset=""0x1e"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" document=""1"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x25"">
        <scope startOffset=""0xd"" endOffset=""0x1a"">
          <local name=""x"" il_index=""2"" il_start=""0xd"" il_end=""0x1a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(544937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544937")]
        [Fact]
        public void ForEachStatement_MultiDimensionalArray()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var x in new int[2, 3])
        {
            System.Console.WriteLine(x);
        }
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new int[2, 3]'
            // 4) Hidden initial jump (of for loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) 'in'
            // 10) Close brace at end of method

            v.VerifyIL("C.Main", @"
{
  // Code size       88 (0x58)
  .maxstack  3
  .locals init (int[,] V_0,
                int V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  ldc.i4.2
  IL_0003:  ldc.i4.3
  IL_0004:  newobj     ""int[*,*]..ctor""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0011:  stloc.1
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.1
  IL_0014:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0019:  stloc.2
  IL_001a:  ldloc.0
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0021:  stloc.3
 ~IL_0022:  br.s       IL_0053
  IL_0024:  ldloc.0
  IL_0025:  ldc.i4.1
  IL_0026:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_002b:  stloc.s    V_4
 ~IL_002d:  br.s       IL_004a
 -IL_002f:  ldloc.0
  IL_0030:  ldloc.3
  IL_0031:  ldloc.s    V_4
  IL_0033:  call       ""int[*,*].Get""
  IL_0038:  stloc.s    V_5
 -IL_003a:  nop
 -IL_003b:  ldloc.s    V_5
  IL_003d:  call       ""void System.Console.WriteLine(int)""
  IL_0042:  nop
 -IL_0043:  nop
 ~IL_0044:  ldloc.s    V_4
  IL_0046:  ldc.i4.1
  IL_0047:  add
  IL_0048:  stloc.s    V_4
 -IL_004a:  ldloc.s    V_4
  IL_004c:  ldloc.2
  IL_004d:  ble.s      IL_002f
 ~IL_004f:  ldloc.3
  IL_0050:  ldc.i4.1
  IL_0051:  add
  IL_0052:  stloc.3
 -IL_0053:  ldloc.3
  IL_0054:  ldloc.1
  IL_0055:  ble.s      IL_0024
 -IL_0057:  ret
}
", sequencePoints: "C.Main");
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ConditionalInAsyncMethod()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class Program
{
    public static async void Test()
    {
        int i = 0;

        if (i != 0)
            Console
                .WriteLine();
    }
}
");
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (int V_0,
                bool V_1,
                System.Exception V_2)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0;
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: if (i != 0)
    IL_000f:  ldarg.0
    IL_0010:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0015:  ldc.i4.0
    IL_0016:  cgt.un
    IL_0018:  stloc.1
    // sequence point: <hidden>
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0022
    // sequence point: Console ... .WriteLine()
    IL_001c:  call       ""void System.Console.WriteLine()""
    IL_0021:  nop
    // sequence point: <hidden>
    IL_0022:  leave.s    IL_003c
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0024:  stloc.2
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.s   -2
    IL_0028:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_002d:  ldarg.0
    IL_002e:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_0033:  ldloc.2
    IL_0034:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0039:  nop
    IL_003a:  leave.s    IL_0050
  }
  // sequence point: }
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.s   -2
  IL_003f:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0044:  ldarg.0
  IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_004f:  nop
  IL_0050:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);

            v.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Test"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Test&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;Test&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x51"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""1"" offset=""33"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" document=""1"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""11"" startColumn=""13"" endLine=""12"" endColumn=""30"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
        <entry offset=""0x44"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x51"">
        <namespace name=""System"" />
      </scope>
      <asyncInfo>
        <catchHandler offset=""0x24"" />
        <kickoffMethod declaringType=""Program"" methodName=""Test"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ConditionalBeforeLocalFunction()
        {
            var source = @"
class C
{
    void M()
    {
        int i = 0;
        if (i != 0)
        {
            return;
        }

        string local()
        {
            throw null;
        }

        System.Console.Write(1);
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("C.M", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (int V_0, //i
                bool V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: int i = 0;
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  // sequence point: if (i != 0)
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  cgt.un
  IL_0007:  stloc.1
  // sequence point: <hidden>
  IL_0008:  ldloc.1
  IL_0009:  brfalse.s  IL_000e
  // sequence point: {
  IL_000b:  nop
  // sequence point: return;
  IL_000c:  br.s       IL_0016
  // sequence point: <hidden>
  IL_000e:  nop
  // sequence point: System.Console.Write(1);
  IL_000f:  ldc.i4.1
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  nop
  // sequence point: }
  IL_0016:  ret
}
", sequencePoints: "C.M", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ConditionalInAsyncMethodWithExplicitReturn()
        {
            var source = @"
using System;

class Program
{
    public static async void Test()
    {
        int i = 0;

        if (i != 0)
            Console
                .WriteLine();

        return;
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       81 (0x51)
  .maxstack  2
  .locals init (int V_0,
                bool V_1,
                System.Exception V_2)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0;
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: if (i != 0)
    IL_000f:  ldarg.0
    IL_0010:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0015:  ldc.i4.0
    IL_0016:  cgt.un
    IL_0018:  stloc.1
    // sequence point: <hidden>
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0022
    // sequence point: Console ... .WriteLine()
    IL_001c:  call       ""void System.Console.WriteLine()""
    IL_0021:  nop
    // sequence point: return;
    IL_0022:  leave.s    IL_003c
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0024:  stloc.2
    IL_0025:  ldarg.0
    IL_0026:  ldc.i4.s   -2
    IL_0028:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_002d:  ldarg.0
    IL_002e:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_0033:  ldloc.2
    IL_0034:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0039:  nop
    IL_003a:  leave.s    IL_0050
  }
  // sequence point: }
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.s   -2
  IL_003f:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0044:  ldarg.0
  IL_0045:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_004a:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_004f:  nop
  IL_0050:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ConditionalInSimpleMethod()
        {
            var source = @"
using System;

class Program
{
    public static void Test()
    {
        int i = 0;

        if (i != 0)
            Console.WriteLine();
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Test()", @"
{
  // Code size       18 (0x12)
  .maxstack  2
  .locals init (int V_0, //i
                bool V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: int i = 0;
  IL_0001:  ldc.i4.0
  IL_0002:  stloc.0
  // sequence point: if (i != 0)
  IL_0003:  ldloc.0
  IL_0004:  ldc.i4.0
  IL_0005:  cgt.un
  IL_0007:  stloc.1
  // sequence point: <hidden>
  IL_0008:  ldloc.1
  IL_0009:  brfalse.s  IL_0011
  // sequence point: Console.WriteLine();
  IL_000b:  call       ""void System.Console.WriteLine()""
  IL_0010:  nop
  // sequence point: }
  IL_0011:  ret
}
", sequencePoints: "Program.Test", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ElseConditionalInAsyncMethod()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class Program
{
    public static async void Test()
    {
        int i = 0;

        if (i != 0)
            Console.WriteLine(""one"");
        else
            Console.WriteLine(""other"");
    }
}
");
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       99 (0x63)
  .maxstack  2
  .locals init (int V_0,
                bool V_1,
                System.Exception V_2)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0;
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: if (i != 0)
    IL_000f:  ldarg.0
    IL_0010:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0015:  ldc.i4.0
    IL_0016:  cgt.un
    IL_0018:  stloc.1
    // sequence point: <hidden>
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0029
    // sequence point: Console.WriteLine(""one"");
    IL_001c:  ldstr      ""one""
    IL_0021:  call       ""void System.Console.WriteLine(string)""
    IL_0026:  nop
    // sequence point: <hidden>
    IL_0027:  br.s       IL_0034
    // sequence point: Console.WriteLine(""other"");
    IL_0029:  ldstr      ""other""
    IL_002e:  call       ""void System.Console.WriteLine(string)""
    IL_0033:  nop
    // sequence point: <hidden>
    IL_0034:  leave.s    IL_004e
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0036:  stloc.2
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.s   -2
    IL_003a:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_0045:  ldloc.2
    IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_004b:  nop
    IL_004c:  leave.s    IL_0062
  }
  // sequence point: }
  IL_004e:  ldarg.0
  IL_004f:  ldc.i4.s   -2
  IL_0051:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0056:  ldarg.0
  IL_0057:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_005c:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_0061:  nop
  IL_0062:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);

            v.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Test"">
      <customDebugInfo>
        <forwardIterator name=""&lt;Test&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""Program+&lt;Test&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x63"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""1"" offset=""33"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" document=""1"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""38"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""40"" document=""1"" />
        <entry offset=""0x34"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" hidden=""true"" document=""1"" />
        <entry offset=""0x4e"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x56"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x63"">
        <namespace name=""System"" />
      </scope>
      <asyncInfo>
        <catchHandler offset=""0x36"" />
        <kickoffMethod declaringType=""Program"" methodName=""Test"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ConditionalInTry()
        {
            var source = WithWindowsLineBreaks(@"
using System;

class Program
{
    public static void Test()
    {
        try
        {
            int i = 0;

            if (i != 0)
                Console.WriteLine();
        }
        catch { }
    }
}
");
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Test", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (int V_0, //i
                bool V_1)
  // sequence point: {
  IL_0000:  nop
  .try
  {
    // sequence point: {
    IL_0001:  nop
    // sequence point: int i = 0;
    IL_0002:  ldc.i4.0
    IL_0003:  stloc.0
    // sequence point: if (i != 0)
    IL_0004:  ldloc.0
    IL_0005:  ldc.i4.0
    IL_0006:  cgt.un
    IL_0008:  stloc.1
    // sequence point: <hidden>
    IL_0009:  ldloc.1
    IL_000a:  brfalse.s  IL_0012
    // sequence point: Console.WriteLine();
    IL_000c:  call       ""void System.Console.WriteLine()""
    IL_0011:  nop
    // sequence point: }
    IL_0012:  nop
    IL_0013:  leave.s    IL_001a
  }
  catch object
  {
    // sequence point: catch
    IL_0015:  pop
    // sequence point: {
    IL_0016:  nop
    // sequence point: }
    IL_0017:  nop
    IL_0018:  leave.s    IL_001a
  }
  // sequence point: }
  IL_001a:  ret
}
", sequencePoints: "Program.Test", source: source);

            v.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Test"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""43"" />
          <slot kind=""1"" offset=""65"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""37"" document=""1"" />
        <entry offset=""0x12"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x15"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""14"" document=""1"" />
        <entry offset=""0x16"" startLine=""15"" startColumn=""15"" endLine=""15"" endColumn=""16"" document=""1"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""17"" endLine=""15"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1a"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1b"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x13"">
          <local name=""i"" il_index=""0"" il_start=""0x1"" il_end=""0x13"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(544937, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544937")]
        [Fact]
        public void ForEachStatement_MultiDimensionalArrayBreakAndContinue()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        int[, ,] array = new[,,]
        {
            { {1, 2}, {3, 4} },
            { {5, 6}, {7, 8} },
        };

        foreach (int i in array)
        {
            if (i % 2 == 1) continue;
            if (i > 4) break;
            Console.WriteLine(i);
        }
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithModuleName("MODULE"));

            // Stepping:
            //   After "continue", step to "in".
            //   After "break", step to first sequence point following loop body (in this case, method close brace).
            v.VerifyIL("C.Main", @"
{
  // Code size      169 (0xa9)
  .maxstack  4
  .locals init (int[,,] V_0, //array
                int[,,] V_1,
                int V_2,
                int V_3,
                int V_4,
                int V_5,
                int V_6,
                int V_7,
                int V_8, //i
                bool V_9,
                bool V_10)
 -IL_0000:  nop
 -IL_0001:  ldc.i4.2
  IL_0002:  ldc.i4.2
  IL_0003:  ldc.i4.2
  IL_0004:  newobj     ""int[*,*,*]..ctor""
  IL_0009:  dup
  IL_000a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.8B4B2444E57AED8C2D05A1293255DA1B048C63224317D4666230760935FA4A18""
  IL_000f:  call       ""void System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(System.Array, System.RuntimeFieldHandle)""
  IL_0014:  stloc.0
 -IL_0015:  nop
 -IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ldc.i4.0
  IL_001a:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_001f:  stloc.2
  IL_0020:  ldloc.1
  IL_0021:  ldc.i4.1
  IL_0022:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0027:  stloc.3
  IL_0028:  ldloc.1
  IL_0029:  ldc.i4.2
  IL_002a:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_002f:  stloc.s    V_4
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.0
  IL_0033:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0038:  stloc.s    V_5
 ~IL_003a:  br.s       IL_00a3
  IL_003c:  ldloc.1
  IL_003d:  ldc.i4.1
  IL_003e:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0043:  stloc.s    V_6
 ~IL_0045:  br.s       IL_0098
  IL_0047:  ldloc.1
  IL_0048:  ldc.i4.2
  IL_0049:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_004e:  stloc.s    V_7
 ~IL_0050:  br.s       IL_008c
 -IL_0052:  ldloc.1
  IL_0053:  ldloc.s    V_5
  IL_0055:  ldloc.s    V_6
  IL_0057:  ldloc.s    V_7
  IL_0059:  call       ""int[*,*,*].Get""
  IL_005e:  stloc.s    V_8
 -IL_0060:  nop
 -IL_0061:  ldloc.s    V_8
  IL_0063:  ldc.i4.2
  IL_0064:  rem
  IL_0065:  ldc.i4.1
  IL_0066:  ceq
  IL_0068:  stloc.s    V_9
 ~IL_006a:  ldloc.s    V_9
  IL_006c:  brfalse.s  IL_0070
 -IL_006e:  br.s       IL_0086
 -IL_0070:  ldloc.s    V_8
  IL_0072:  ldc.i4.4
  IL_0073:  cgt
  IL_0075:  stloc.s    V_10
 ~IL_0077:  ldloc.s    V_10
  IL_0079:  brfalse.s  IL_007d
 -IL_007b:  br.s       IL_00a8
 -IL_007d:  ldloc.s    V_8
  IL_007f:  call       ""void System.Console.WriteLine(int)""
  IL_0084:  nop
 -IL_0085:  nop
 ~IL_0086:  ldloc.s    V_7
  IL_0088:  ldc.i4.1
  IL_0089:  add
  IL_008a:  stloc.s    V_7
 -IL_008c:  ldloc.s    V_7
  IL_008e:  ldloc.s    V_4
  IL_0090:  ble.s      IL_0052
 ~IL_0092:  ldloc.s    V_6
  IL_0094:  ldc.i4.1
  IL_0095:  add
  IL_0096:  stloc.s    V_6
 -IL_0098:  ldloc.s    V_6
  IL_009a:  ldloc.3
  IL_009b:  ble.s      IL_0047
 ~IL_009d:  ldloc.s    V_5
  IL_009f:  ldc.i4.1
  IL_00a0:  add
  IL_00a1:  stloc.s    V_5
 -IL_00a3:  ldloc.s    V_5
  IL_00a5:  ldloc.2
  IL_00a6:  ble.s      IL_003c
 -IL_00a8:  ret
}
", sequencePoints: "C.Main");
        }

        [Fact]
        public void ForEachStatement_Enumerator()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        foreach (var x in new System.Collections.Generic.List<int>())
        {
            System.Console.WriteLine(x);
        }
    }
}
";

            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            // Sequence points:
            // 1) Open brace at start of method
            // 2) 'foreach'
            // 3) 'new System.Collections.Generic.List<int>()'
            // 4) Hidden initial jump (of while loop)
            // 5) 'var c'
            // 6) Open brace of loop
            // 7) Loop body
            // 8) Close brace of loop
            // 9) 'in'
            // 10) hidden point in Finally
            // 11) Close brace at end of method

            v.VerifyIL("C.Main", @"
{
  // Code size       59 (0x3b)
  .maxstack  1
  .locals init (System.Collections.Generic.List<int>.Enumerator V_0,
                int V_1) //x
 -IL_0000:  nop
 -IL_0001:  nop
 -IL_0002:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0007:  call       ""System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()""
  IL_000c:  stloc.0
  .try
  {
   ~IL_000d:  br.s       IL_0020
   -IL_000f:  ldloca.s   V_0
    IL_0011:  call       ""int System.Collections.Generic.List<int>.Enumerator.Current.get""
    IL_0016:  stloc.1
   -IL_0017:  nop
   -IL_0018:  ldloc.1
    IL_0019:  call       ""void System.Console.WriteLine(int)""
    IL_001e:  nop
   -IL_001f:  nop
   -IL_0020:  ldloca.s   V_0
    IL_0022:  call       ""bool System.Collections.Generic.List<int>.Enumerator.MoveNext()""
    IL_0027:  brtrue.s   IL_000f
    IL_0029:  leave.s    IL_003a
  }
  finally
  {
   ~IL_002b:  ldloca.s   V_0
    IL_002d:  constrained. ""System.Collections.Generic.List<int>.Enumerator""
    IL_0033:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0038:  nop
    IL_0039:  endfinally
  }
 -IL_003a:  ret
}
", sequencePoints: "C.Main");
        }

        [WorkItem(718501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718501")]
        [Fact]
        public void ForEachNops()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static List<List<int>> l = new List<List<int>>();

    static void Main(string[] args)
        {
            foreach (var i in l.AsEnumerable())
            {
                switch (i.Count)
                {
                    case 1:
                        break;

                    default:
                        if (i.Count != 0)
                        {
                        }

                        break;
                }
            }
        }
}
");
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""5"" offset=""15"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""35"" offset=""83"" />
          <slot kind=""1"" offset=""83"" />
          <slot kind=""1"" offset=""237"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""20"" document=""1"" />
        <entry offset=""0x2"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""47"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" startLine=""12"" startColumn=""22"" endLine=""12"" endColumn=""27"" document=""1"" />
        <entry offset=""0x1b"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1c"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""33"" document=""1"" />
        <entry offset=""0x23"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x2b"" startLine=""17"" startColumn=""25"" endLine=""17"" endColumn=""31"" document=""1"" />
        <entry offset=""0x2d"" startLine=""20"" startColumn=""25"" endLine=""20"" endColumn=""42"" document=""1"" />
        <entry offset=""0x38"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""21"" startColumn=""25"" endLine=""21"" endColumn=""26"" document=""1"" />
        <entry offset=""0x3d"" startLine=""22"" startColumn=""25"" endLine=""22"" endColumn=""26"" document=""1"" />
        <entry offset=""0x3e"" startLine=""24"" startColumn=""25"" endLine=""24"" endColumn=""31"" document=""1"" />
        <entry offset=""0x40"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" document=""1"" />
        <entry offset=""0x41"" startLine=""12"" startColumn=""28"" endLine=""12"" endColumn=""30"" document=""1"" />
        <entry offset=""0x4b"" hidden=""true"" document=""1"" />
        <entry offset=""0x55"" hidden=""true"" document=""1"" />
        <entry offset=""0x56"" startLine=""27"" startColumn=""9"" endLine=""27"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x57"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <scope startOffset=""0x14"" endOffset=""0x41"">
          <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x41"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>"
);
        }

        [Fact]
        public void ForEachStatement_Deconstruction()
        {
            var source = WithWindowsLineBreaks(@"
public class C
{
    public static (int, (bool, double))[] F() => new[] { (1, (true, 2.0)) };

    public static void Main()
    {
        foreach (var (c, (d, e)) in F())
        {
            System.Console.WriteLine(c);
        }
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            v.VerifyIL("C.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init (System.ValueTuple<int, System.ValueTuple<bool, double>>[] V_0,
                int V_1,
                int V_2, //c
                bool V_3, //d
                double V_4, //e
                System.ValueTuple<bool, double> V_5)
  // sequence point: {
  IL_0000:  nop
  // sequence point: foreach
  IL_0001:  nop
  // sequence point: F()
  IL_0002:  call       ""System.ValueTuple<int, System.ValueTuple<bool, double>>[] C.F()""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  // sequence point: <hidden>
  IL_000a:  br.s       IL_003f
  // sequence point: var (c, (d, e))
  IL_000c:  ldloc.0
  IL_000d:  ldloc.1
  IL_000e:  ldelem     ""System.ValueTuple<int, System.ValueTuple<bool, double>>""
  IL_0013:  dup
  IL_0014:  ldfld      ""System.ValueTuple<bool, double> System.ValueTuple<int, System.ValueTuple<bool, double>>.Item2""
  IL_0019:  stloc.s    V_5
  IL_001b:  ldfld      ""int System.ValueTuple<int, System.ValueTuple<bool, double>>.Item1""
  IL_0020:  stloc.2
  IL_0021:  ldloc.s    V_5
  IL_0023:  ldfld      ""bool System.ValueTuple<bool, double>.Item1""
  IL_0028:  stloc.3
  IL_0029:  ldloc.s    V_5
  IL_002b:  ldfld      ""double System.ValueTuple<bool, double>.Item2""
  IL_0030:  stloc.s    V_4
  // sequence point: {
  IL_0032:  nop
  // sequence point: System.Console.WriteLine(c);
  IL_0033:  ldloc.2
  IL_0034:  call       ""void System.Console.WriteLine(int)""
  IL_0039:  nop
  // sequence point: }
  IL_003a:  nop
  // sequence point: <hidden>
  IL_003b:  ldloc.1
  IL_003c:  ldc.i4.1
  IL_003d:  add
  IL_003e:  stloc.1
  // sequence point: in
  IL_003f:  ldloc.1
  IL_0040:  ldloc.0
  IL_0041:  ldlen
  IL_0042:  conv.i4
  IL_0043:  blt.s      IL_000c
  // sequence point: }
  IL_0045:  ret
}
", sequencePoints: "C.Main", source: source);

            v.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""50"" endLine=""4"" endColumn=""76"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""25"" />
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""32"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""37"" endLine=""8"" endColumn=""40"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""33"" document=""1"" />
        <entry offset=""0x32"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x33"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" document=""1"" />
        <entry offset=""0x3a"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3b"" hidden=""true"" document=""1"" />
        <entry offset=""0x3f"" startLine=""8"" startColumn=""34"" endLine=""8"" endColumn=""36"" document=""1"" />
        <entry offset=""0x45"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x46"">
        <scope startOffset=""0xc"" endOffset=""0x3b"">
          <local name=""c"" il_index=""2"" il_start=""0xc"" il_end=""0x3b"" attributes=""0"" />
          <local name=""d"" il_index=""3"" il_start=""0xc"" il_end=""0x3b"" attributes=""0"" />
          <local name=""e"" il_index=""4"" il_start=""0xc"" il_end=""0x3b"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Switch

        [Fact]
        public void SwitchWithPattern_01()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static List<List<int>> l = new List<List<int>>();

    static void Main(string[] args)
    {
        Student s = new Student();
        s.Name = ""Bozo"";
        s.GPA = 2.3;
        Operate(s);  
    }

    static string Operate(Person p)
    {
        switch (p)
        {
            case Student s when s.GPA > 3.5:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Student s:
                return $""Student {s.Name} ({s.GPA:N1})"";
            case Teacher t:
                return $""Teacher {t.Name} of {t.Subject}"";
            default:
                return $""Person {p.Name}"";
        }
    }
}

class Person { public string Name; }
class Teacher : Person { public string Subject; }
class Student : Person { public double GPA; }
");
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Operate",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Operate"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""59"" />
          <slot kind=""0"" offset=""163"" />
          <slot kind=""0"" offset=""250"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""19"" document=""1"" />
        <entry offset=""0x4"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""44"" document=""1"" />
        <entry offset=""0x2e"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""57"" document=""1"" />
        <entry offset=""0x4f"" hidden=""true"" document=""1"" />
        <entry offset=""0x53"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""57"" document=""1"" />
        <entry offset=""0x72"" hidden=""true"" document=""1"" />
        <entry offset=""0x74"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""59"" document=""1"" />
        <entry offset=""0x93"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""43"" document=""1"" />
        <entry offset=""0xa7"" startLine=""31"" startColumn=""5"" endLine=""31"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xaa"">
        <scope startOffset=""0x1d"" endOffset=""0x4f"">
          <local name=""s"" il_index=""0"" il_start=""0x1d"" il_end=""0x4f"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x4f"" endOffset=""0x72"">
          <local name=""s"" il_index=""1"" il_start=""0x4f"" il_end=""0x72"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x72"" endOffset=""0x93"">
          <local name=""t"" il_index=""2"" il_start=""0x72"" il_end=""0x93"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SwitchWithPattern_02()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static List<List<int>> l = new List<List<int>>();

    static void Main(string[] args)
    {
        Student s = new Student();
        s.Name = ""Bozo"";
        s.GPA = 2.3;
        Operate(s);  
    }

    static System.Func<string> Operate(Person p)
    {
        switch (p)
        {
            case Student s when s.GPA > 3.5:
                return () => $""Student {s.Name} ({s.GPA:N1})"";
            case Student s:
                return () => $""Student {s.Name} ({s.GPA:N1})"";
            case Teacher t:
                return () => $""Teacher {t.Name} of {t.Subject}"";
            default:
                return () => $""Person {p.Name}"";
        }
    }
}

class Person { public string Name; }
class Teacher : Person { public string Subject; }
class Student : Person { public double GPA; }
");
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Operate",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Operate"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""30"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""11"" />
          <lambda offset=""109"" closure=""1"" />
          <lambda offset=""202"" closure=""1"" />
          <lambda offset=""295"" closure=""1"" />
          <lambda offset=""383"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""44"" document=""1"" />
        <entry offset=""0x5d"" hidden=""true"" document=""1"" />
        <entry offset=""0x5f"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""63"" document=""1"" />
        <entry offset=""0x6f"" hidden=""true"" document=""1"" />
        <entry offset=""0x7d"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""63"" document=""1"" />
        <entry offset=""0x8d"" hidden=""true"" document=""1"" />
        <entry offset=""0x8f"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""65"" document=""1"" />
        <entry offset=""0x9f"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""49"" document=""1"" />
        <entry offset=""0xaf"" startLine=""31"" startColumn=""5"" endLine=""31"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb2"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0xb2"" attributes=""0"" />
        <scope startOffset=""0xe"" endOffset=""0xaf"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0xe"" il_end=""0xaf"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SwitchWithPatternAndLocalFunctions()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static List<List<int>> l = new List<List<int>>();

    static void Main(string[] args)
    {
        Student s = new Student();
        s.Name = ""Bozo"";
        s.GPA = 2.3;
        Operate(s);  
    }

    static System.Func<string> Operate(Person p)
    {
        switch (p)
        {
            case Student s when s.GPA > 3.5:
                string f1() => $""Student {s.Name} ({s.GPA:N1})"";
                return f1;
            case Student s:
                string f2() => $""Student {s.Name} ({s.GPA:N1})"";
                return f2;
            case Teacher t:
                string f3() => $""Teacher {t.Name} of {t.Subject}"";
                return f3;
            default:
                string f4() => $""Person {p.Name}"";
                return f4;
        }
    }
}

class Person { public string Name; }
class Teacher : Person { public string Subject; }
class Student : Person { public double GPA; }
");
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Operate", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Operate"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""30"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""0"" />
          <closure offset=""11"" />
          <lambda offset=""111"" closure=""1"" />
          <lambda offset=""234"" closure=""1"" />
          <lambda offset=""357"" closure=""1"" />
          <lambda offset=""475"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0xe"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""44"" document=""1"" />
        <entry offset=""0x5d"" hidden=""true"" document=""1"" />
        <entry offset=""0x60"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" document=""1"" />
        <entry offset=""0x70"" hidden=""true"" document=""1"" />
        <entry offset=""0x7f"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""27"" document=""1"" />
        <entry offset=""0x8f"" hidden=""true"" document=""1"" />
        <entry offset=""0x92"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""27"" document=""1"" />
        <entry offset=""0xa2"" hidden=""true"" document=""1"" />
        <entry offset=""0xa3"" startLine=""33"" startColumn=""17"" endLine=""33"" endColumn=""27"" document=""1"" />
        <entry offset=""0xb3"" startLine=""35"" startColumn=""5"" endLine=""35"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xb6"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0xb6"" attributes=""0"" />
        <scope startOffset=""0xe"" endOffset=""0xb3"">
          <local name=""CS$&lt;&gt;8__locals1"" il_index=""1"" il_start=""0xe"" il_end=""0xb3"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(17090, "https://github.com/dotnet/roslyn/issues/17090"), WorkItem(19731, "https://github.com/dotnet/roslyn/issues/19731")]
        [Fact]
        public void SwitchWithConstantPattern()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        M1();
        M2();
    }

    static void M1()
    {
        switch
            (1)
        {
            case 0 when true:
                ;
            case 1:
                Console.Write(1);
                break;
            case 2:
                ;
        }
    }

    static void M2()
    {
        switch
            (nameof(M2))
        {
            case nameof(M1) when true:
                ;
            case nameof(M2):
                Console.Write(nameof(M2));
                break;
            case nameof(Main):
                ;
        }
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "1M2");

            verifier.VerifyIL(qualifiedMethodName: "Program.M1", sequencePoints: "Program.M1", source: source,
expectedIL: @"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0,
                int V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch ...           (1
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.1
  IL_0003:  ldc.i4.1
  IL_0004:  stloc.0
  // sequence point: <hidden>
  IL_0005:  br.s       IL_0007
  // sequence point: Console.Write(1);
  IL_0007:  ldc.i4.1
  IL_0008:  call       ""void System.Console.Write(int)""
  IL_000d:  nop
  // sequence point: break;
  IL_000e:  br.s       IL_0010
  // sequence point: }
  IL_0010:  ret
}");
            verifier.VerifyIL(qualifiedMethodName: "Program.M2", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (string V_0,
                string V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch ...  (nameof(M2)
  IL_0001:  ldstr      ""M2""
  IL_0006:  stloc.1
  IL_0007:  ldstr      ""M2""
  IL_000c:  stloc.0
  // sequence point: <hidden>
  IL_000d:  br.s       IL_000f
  // sequence point: Console.Write(nameof(M2));
  IL_000f:  ldstr      ""M2""
  IL_0014:  call       ""void System.Console.Write(string)""
  IL_0019:  nop
  // sequence point: break;
  IL_001a:  br.s       IL_001c
  // sequence point: }
  IL_001c:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            c.VerifyDiagnostics();
            verifier = CompileAndVerify(c, expectedOutput: "1M2");

            verifier.VerifyIL("Program.M1",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");
            verifier.VerifyIL("Program.M2",
@"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""M2""
  IL_0005:  call       ""void System.Console.Write(string)""
  IL_000a:  ret
}");
        }

        [WorkItem(19734, "https://github.com/dotnet/roslyn/issues/19734")]
        [Fact]
        public void SwitchWithConstantGenericPattern_01()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        M1<int>();    // 1
        M1<long>();   // 2
        M2<string>(); // 3
        M2<int>();    // 4
    }

    static void M1<T>()
    {
        switch (1)
        {
            case T t:
                Console.Write(1);
                break;
            case int i:
                Console.Write(2);
                break;
        }
    }

    static void M2<T>()
    {
        switch (nameof(M2))
        {
            case T t:
                Console.Write(3);
                break;
            case string s:
                Console.Write(4);
                break;
            case null:
                ;
        }
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "1234");

            verifier.VerifyIL(qualifiedMethodName: "Program.M1<T>", sequencePoints: "Program.M1", source: source,
expectedIL: @"{
  // Code size       60 (0x3c)
  .maxstack  1
  .locals init (T V_0, //t
                int V_1, //i
                int V_2)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (1)
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.2
  IL_0003:  ldc.i4.1
  IL_0004:  stloc.1
  // sequence point: <hidden>
  IL_0005:  ldloc.1
  IL_0006:  box        ""int""
  IL_000b:  isinst     ""T""
  IL_0010:  brfalse.s  IL_0030
  IL_0012:  ldloc.1
  IL_0013:  box        ""int""
  IL_0018:  isinst     ""T""
  IL_001d:  unbox.any  ""T""
  IL_0022:  stloc.0
  // sequence point: <hidden>
  IL_0023:  br.s       IL_0025
  // sequence point: <hidden>
  IL_0025:  br.s       IL_0027
  // sequence point: Console.Write(1);
  IL_0027:  ldc.i4.1
  IL_0028:  call       ""void System.Console.Write(int)""
  IL_002d:  nop
  // sequence point: break;
  IL_002e:  br.s       IL_003b
  // sequence point: <hidden>
  IL_0030:  br.s       IL_0032
  // sequence point: Console.Write(2);
  IL_0032:  ldc.i4.2
  IL_0033:  call       ""void System.Console.Write(int)""
  IL_0038:  nop
  // sequence point: break;
  IL_0039:  br.s       IL_003b
  // sequence point: }
  IL_003b:  ret
}");
            verifier.VerifyIL(qualifiedMethodName: "Program.M2<T>", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       58 (0x3a)
  .maxstack  1
  .locals init (T V_0, //t
                string V_1, //s
                string V_2)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (nameof(M2))
  IL_0001:  ldstr      ""M2""
  IL_0006:  stloc.2
  IL_0007:  ldstr      ""M2""
  IL_000c:  stloc.1
  // sequence point: <hidden>
  IL_000d:  ldloc.1
  IL_000e:  isinst     ""T""
  IL_0013:  brfalse.s  IL_002e
  IL_0015:  ldloc.1
  IL_0016:  isinst     ""T""
  IL_001b:  unbox.any  ""T""
  IL_0020:  stloc.0
  // sequence point: <hidden>
  IL_0021:  br.s       IL_0023
  // sequence point: <hidden>
  IL_0023:  br.s       IL_0025
  // sequence point: Console.Write(3);
  IL_0025:  ldc.i4.3
  IL_0026:  call       ""void System.Console.Write(int)""
  IL_002b:  nop
  // sequence point: break;
  IL_002c:  br.s       IL_0039
  // sequence point: <hidden>
  IL_002e:  br.s       IL_0030
  // sequence point: Console.Write(4);
  IL_0030:  ldc.i4.4
  IL_0031:  call       ""void System.Console.Write(int)""
  IL_0036:  nop
  // sequence point: break;
  IL_0037:  br.s       IL_0039
  // sequence point: }
  IL_0039:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            verifier = CompileAndVerify(c, expectedOutput: "1234");

            verifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  box        ""int""
  IL_0008:  isinst     ""T""
  IL_000d:  brfalse.s  IL_0016
  IL_000f:  ldc.i4.1
  IL_0010:  call       ""void System.Console.Write(int)""
  IL_0015:  ret
  IL_0016:  ldc.i4.2
  IL_0017:  call       ""void System.Console.Write(int)""
  IL_001c:  ret
}");
            verifier.VerifyIL("Program.M2<T>",
@"{
  // Code size       28 (0x1c)
  .maxstack  1
  .locals init (string V_0) //s
  IL_0000:  ldstr      ""M2""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  isinst     ""T""
  IL_000c:  brfalse.s  IL_0015
  IL_000e:  ldc.i4.3
  IL_000f:  call       ""void System.Console.Write(int)""
  IL_0014:  ret
  IL_0015:  ldc.i4.4
  IL_0016:  call       ""void System.Console.Write(int)""
  IL_001b:  ret
}");
        }

        [WorkItem(19734, "https://github.com/dotnet/roslyn/issues/19734")]
        [Fact]
        public void SwitchWithConstantGenericPattern_02()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        M2<string>(); // 6
        M2<int>();    // 6
    }

    static void M2<T>()
    {
        const string x = null;
        switch (x)
        {
            case T t:
                ;
            case string s:
                ;
            case null:
                Console.Write(6);
                break;
        }
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "66");

            verifier.VerifyIL(qualifiedMethodName: "Program.M2<T>", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (T V_0, //t
                string V_1, //s
                string V_2,
                string V_3)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (x)
  IL_0001:  ldnull
  IL_0002:  stloc.3
  IL_0003:  ldnull
  IL_0004:  stloc.2
  // sequence point: <hidden>
  IL_0005:  br.s       IL_0007
  // sequence point: Console.Write(6);
  IL_0007:  ldc.i4.6
  IL_0008:  call       ""void System.Console.Write(int)""
  IL_000d:  nop
  // sequence point: break;
  IL_000e:  br.s       IL_0010
  // sequence point: }
  IL_0010:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            verifier = CompileAndVerify(c, expectedOutput: "66");

            verifier.VerifyIL("Program.M2<T>",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.6
  IL_0001:  call       ""void System.Console.Write(int)""
  IL_0006:  ret
}");
        }

        [Fact]
        [WorkItem(31665, "https://github.com/dotnet/roslyn/issues/31665")]
        public void TestSequencePoints_31665()
        {
            var source = @"
using System;

internal class Program
{
    private static void Main(string[] args)
    {
        var s = ""1"";
        if (true)
            switch (s)
            {
                case ""1"":
                    Console.Out.WriteLine(""Input was 1"");
                    break;
                default:
                    throw new Exception(""Default case"");
            }
        else
            Console.Out.WriteLine(""Too many inputs"");
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.Main(string[])", @"
    {
      // Code size       60 (0x3c)
      .maxstack  2
      .locals init (string V_0, //s
                    bool V_1,
                    string V_2,
                    string V_3)
      // sequence point: {
      IL_0000:  nop
      // sequence point: var s = ""1"";
      IL_0001:  ldstr      ""1""
      IL_0006:  stloc.0
      // sequence point: if (true)
      IL_0007:  ldc.i4.1
      IL_0008:  stloc.1
      // sequence point: switch (s)
      IL_0009:  ldloc.0
      IL_000a:  stloc.3
      // sequence point: <hidden>
      IL_000b:  ldloc.3
      IL_000c:  stloc.2
      // sequence point: <hidden>
      IL_000d:  ldloc.2
      IL_000e:  ldstr      ""1""
      IL_0013:  call       ""bool string.op_Equality(string, string)""
      IL_0018:  brtrue.s   IL_001c
      IL_001a:  br.s       IL_002e
      // sequence point: Console.Out.WriteLine(""Input was 1"");
      IL_001c:  call       ""System.IO.TextWriter System.Console.Out.get""
      IL_0021:  ldstr      ""Input was 1""
      IL_0026:  callvirt   ""void System.IO.TextWriter.WriteLine(string)""
      IL_002b:  nop
      // sequence point: break;
      IL_002c:  br.s       IL_0039
      // sequence point: throw new Exception(""Default case"");
      IL_002e:  ldstr      ""Default case""
      IL_0033:  newobj     ""System.Exception..ctor(string)""
      IL_0038:  throw
      // sequence point: <hidden>
      IL_0039:  br.s       IL_003b
      // sequence point: }
      IL_003b:  ret
    }
", sequencePoints: "Program.Main", source: source);
        }

        [Fact]
        [WorkItem(17076, "https://github.com/dotnet/roslyn/issues/17076")]
        public void TestSequencePoints_17076()
        {
            var source = @"
using System.Threading.Tasks;

internal class Program
{
    private static void Main(string[] args)
    {
        M(new Node()).GetAwaiter().GetResult();
    }

    static async Task M(Node node)
    {
        while (node != null)
        {
            if (node is A a)
            {
                await Task.Yield();
                return;
            }
            else if (node is B b)
            {
                await Task.Yield();
                return;
            }

            node = node.Parent;
        }
    }
}

class Node
{
    public Node Parent = null;
}
class A : Node { }
class B : Node { }
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<M>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
    {
      // Code size      403 (0x193)
      .maxstack  3
      .locals init (int V_0,
                    bool V_1,
                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                    System.Runtime.CompilerServices.YieldAwaitable V_3,
                    Program.<M>d__1 V_4,
                    bool V_5,
                    System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_6,
                    bool V_7,
                    System.Exception V_8)
      // sequence point: <hidden>
      IL_0000:  ldarg.0
      IL_0001:  ldfld      ""int Program.<M>d__1.<>1__state""
      IL_0006:  stloc.0
      .try
      {
        // sequence point: <hidden>
        IL_0007:  ldloc.0
        IL_0008:  brfalse.s  IL_0012
        IL_000a:  br.s       IL_000c
        IL_000c:  ldloc.0
        IL_000d:  ldc.i4.1
        IL_000e:  beq.s      IL_0014
        IL_0010:  br.s       IL_0019
        IL_0012:  br.s       IL_007e
        IL_0014:  br         IL_0109
        // sequence point: {
        IL_0019:  nop
        // sequence point: <hidden>
        IL_001a:  br         IL_0150
        // sequence point: {
        IL_001f:  nop
        // sequence point: if (node is A a)
        IL_0020:  ldarg.0
        IL_0021:  ldarg.0
        IL_0022:  ldfld      ""Node Program.<M>d__1.node""
        IL_0027:  isinst     ""A""
        IL_002c:  stfld      ""A Program.<M>d__1.<a>5__1""
        IL_0031:  ldarg.0
        IL_0032:  ldfld      ""A Program.<M>d__1.<a>5__1""
        IL_0037:  ldnull
        IL_0038:  cgt.un
        IL_003a:  stloc.1
        // sequence point: <hidden>
        IL_003b:  ldloc.1
        IL_003c:  brfalse.s  IL_00a7
        // sequence point: {
        IL_003e:  nop
        // sequence point: await Task.Yield();
        IL_003f:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
        IL_0044:  stloc.3
        IL_0045:  ldloca.s   V_3
        IL_0047:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
        IL_004c:  stloc.2
        // sequence point: <hidden>
        IL_004d:  ldloca.s   V_2
        IL_004f:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
        IL_0054:  brtrue.s   IL_009a
        IL_0056:  ldarg.0
        IL_0057:  ldc.i4.0
        IL_0058:  dup
        IL_0059:  stloc.0
        IL_005a:  stfld      ""int Program.<M>d__1.<>1__state""
        // async: yield
        IL_005f:  ldarg.0
        IL_0060:  ldloc.2
        IL_0061:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_0066:  ldarg.0
        IL_0067:  stloc.s    V_4
        IL_0069:  ldarg.0
        IL_006a:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M>d__1.<>t__builder""
        IL_006f:  ldloca.s   V_2
        IL_0071:  ldloca.s   V_4
        IL_0073:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<M>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<M>d__1)""
        IL_0078:  nop
        IL_0079:  leave      IL_0192
        // async: resume
        IL_007e:  ldarg.0
        IL_007f:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_0084:  stloc.2
        IL_0085:  ldarg.0
        IL_0086:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_008b:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
        IL_0091:  ldarg.0
        IL_0092:  ldc.i4.m1
        IL_0093:  dup
        IL_0094:  stloc.0
        IL_0095:  stfld      ""int Program.<M>d__1.<>1__state""
        IL_009a:  ldloca.s   V_2
        IL_009c:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
        IL_00a1:  nop
        // sequence point: return;
        IL_00a2:  leave      IL_017e
        // sequence point: if (node is B b)
        IL_00a7:  ldarg.0
        IL_00a8:  ldarg.0
        IL_00a9:  ldfld      ""Node Program.<M>d__1.node""
        IL_00ae:  isinst     ""B""
        IL_00b3:  stfld      ""B Program.<M>d__1.<b>5__2""
        IL_00b8:  ldarg.0
        IL_00b9:  ldfld      ""B Program.<M>d__1.<b>5__2""
        IL_00be:  ldnull
        IL_00bf:  cgt.un
        IL_00c1:  stloc.s    V_5
        // sequence point: <hidden>
        IL_00c3:  ldloc.s    V_5
        IL_00c5:  brfalse.s  IL_0130
        // sequence point: {
        IL_00c7:  nop
        // sequence point: await Task.Yield();
        IL_00c8:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
        IL_00cd:  stloc.3
        IL_00ce:  ldloca.s   V_3
        IL_00d0:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
        IL_00d5:  stloc.s    V_6
        // sequence point: <hidden>
        IL_00d7:  ldloca.s   V_6
        IL_00d9:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
        IL_00de:  brtrue.s   IL_0126
        IL_00e0:  ldarg.0
        IL_00e1:  ldc.i4.1
        IL_00e2:  dup
        IL_00e3:  stloc.0
        IL_00e4:  stfld      ""int Program.<M>d__1.<>1__state""
        // async: yield
        IL_00e9:  ldarg.0
        IL_00ea:  ldloc.s    V_6
        IL_00ec:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_00f1:  ldarg.0
        IL_00f2:  stloc.s    V_4
        IL_00f4:  ldarg.0
        IL_00f5:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M>d__1.<>t__builder""
        IL_00fa:  ldloca.s   V_6
        IL_00fc:  ldloca.s   V_4
        IL_00fe:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<M>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<M>d__1)""
        IL_0103:  nop
        IL_0104:  leave      IL_0192
        // async: resume
        IL_0109:  ldarg.0
        IL_010a:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_010f:  stloc.s    V_6
        IL_0111:  ldarg.0
        IL_0112:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<M>d__1.<>u__1""
        IL_0117:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
        IL_011d:  ldarg.0
        IL_011e:  ldc.i4.m1
        IL_011f:  dup
        IL_0120:  stloc.0
        IL_0121:  stfld      ""int Program.<M>d__1.<>1__state""
        IL_0126:  ldloca.s   V_6
        IL_0128:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
        IL_012d:  nop
        // sequence point: return;
        IL_012e:  leave.s    IL_017e
        // sequence point: <hidden>
        IL_0130:  ldarg.0
        IL_0131:  ldnull
        IL_0132:  stfld      ""B Program.<M>d__1.<b>5__2""
        // sequence point: node = node.Parent;
        IL_0137:  ldarg.0
        IL_0138:  ldarg.0
        IL_0139:  ldfld      ""Node Program.<M>d__1.node""
        IL_013e:  ldfld      ""Node Node.Parent""
        IL_0143:  stfld      ""Node Program.<M>d__1.node""
        // sequence point: }
        IL_0148:  nop
        IL_0149:  ldarg.0
        IL_014a:  ldnull
        IL_014b:  stfld      ""A Program.<M>d__1.<a>5__1""
        // sequence point: while (node != null)
        IL_0150:  ldarg.0
        IL_0151:  ldfld      ""Node Program.<M>d__1.node""
        IL_0156:  ldnull
        IL_0157:  cgt.un
        IL_0159:  stloc.s    V_7
        // sequence point: <hidden>
        IL_015b:  ldloc.s    V_7
        IL_015d:  brtrue     IL_001f
        IL_0162:  leave.s    IL_017e
      }
      catch System.Exception
      {
        // sequence point: <hidden>
        IL_0164:  stloc.s    V_8
        IL_0166:  ldarg.0
        IL_0167:  ldc.i4.s   -2
        IL_0169:  stfld      ""int Program.<M>d__1.<>1__state""
        IL_016e:  ldarg.0
        IL_016f:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M>d__1.<>t__builder""
        IL_0174:  ldloc.s    V_8
        IL_0176:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
        IL_017b:  nop
        IL_017c:  leave.s    IL_0192
      }
      // sequence point: }
      IL_017e:  ldarg.0
      IL_017f:  ldc.i4.s   -2
      IL_0181:  stfld      ""int Program.<M>d__1.<>1__state""
      // sequence point: <hidden>
      IL_0186:  ldarg.0
      IL_0187:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<M>d__1.<>t__builder""
      IL_018c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
      IL_0191:  nop
      IL_0192:  ret
    }
", sequencePoints: "Program+<M>d__1.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(28288, "https://github.com/dotnet/roslyn/issues/28288")]
        public void TestSequencePoints_28288()
        {
            var source = @"
using System.Threading.Tasks;

public class C
{
    public static async Task Main()
    {
        object o = new C();
        switch (o)
        {
            case C c:
                System.Console.Write(1);
                break;
            default:
                return;
        }

        if (M() != null)
        {
        }
    }

    private static object M()
    {
        return new C();
    }
}";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("C.<Main>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
    {
      // Code size      162 (0xa2)
      .maxstack  2
      .locals init (int V_0,
                    object V_1,
                    bool V_2,
                    System.Exception V_3)
      // sequence point: <hidden>
      IL_0000:  ldarg.0
      IL_0001:  ldfld      ""int C.<Main>d__0.<>1__state""
      IL_0006:  stloc.0
      .try
      {
        // sequence point: {
        IL_0007:  nop
        // sequence point: object o = new C();
        IL_0008:  ldarg.0
        IL_0009:  newobj     ""C..ctor()""
        IL_000e:  stfld      ""object C.<Main>d__0.<o>5__1""
        // sequence point: switch (o)
        IL_0013:  ldarg.0
        IL_0014:  ldarg.0
        IL_0015:  ldfld      ""object C.<Main>d__0.<o>5__1""
        IL_001a:  stloc.1
        // sequence point: <hidden>
        IL_001b:  ldloc.1
        IL_001c:  stfld      ""object C.<Main>d__0.<>s__3""
        // sequence point: <hidden>
        IL_0021:  ldarg.0
        IL_0022:  ldarg.0
        IL_0023:  ldfld      ""object C.<Main>d__0.<>s__3""
        IL_0028:  isinst     ""C""
        IL_002d:  stfld      ""C C.<Main>d__0.<c>5__2""
        IL_0032:  ldarg.0
        IL_0033:  ldfld      ""C C.<Main>d__0.<c>5__2""
        IL_0038:  brtrue.s   IL_003c
        IL_003a:  br.s       IL_0047
        // sequence point: <hidden>
        IL_003c:  br.s       IL_003e
        // sequence point: System.Console.Write(1);
        IL_003e:  ldc.i4.1
        IL_003f:  call       ""void System.Console.Write(int)""
        IL_0044:  nop
        // sequence point: break;
        IL_0045:  br.s       IL_0049
        // sequence point: return;
        IL_0047:  leave.s    IL_0086
        // sequence point: <hidden>
        IL_0049:  ldarg.0
        IL_004a:  ldnull
        IL_004b:  stfld      ""C C.<Main>d__0.<c>5__2""
        IL_0050:  ldarg.0
        IL_0051:  ldnull
        IL_0052:  stfld      ""object C.<Main>d__0.<>s__3""
        // sequence point: if (M() != null)
        IL_0057:  call       ""object C.M()""
        IL_005c:  ldnull
        IL_005d:  cgt.un
        IL_005f:  stloc.2
        // sequence point: <hidden>
        IL_0060:  ldloc.2
        IL_0061:  brfalse.s  IL_0065
        // sequence point: {
        IL_0063:  nop
        // sequence point: }
        IL_0064:  nop
        // sequence point: <hidden>
        IL_0065:  leave.s    IL_0086
      }
      catch System.Exception
      {
        // sequence point: <hidden>
        IL_0067:  stloc.3
        IL_0068:  ldarg.0
        IL_0069:  ldc.i4.s   -2
        IL_006b:  stfld      ""int C.<Main>d__0.<>1__state""
        IL_0070:  ldarg.0
        IL_0071:  ldnull
        IL_0072:  stfld      ""object C.<Main>d__0.<o>5__1""
        IL_0077:  ldarg.0
        IL_0078:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
        IL_007d:  ldloc.3
        IL_007e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
        IL_0083:  nop
        IL_0084:  leave.s    IL_00a1
      }
      // sequence point: }
      IL_0086:  ldarg.0
      IL_0087:  ldc.i4.s   -2
      IL_0089:  stfld      ""int C.<Main>d__0.<>1__state""
      // sequence point: <hidden>
      IL_008e:  ldarg.0
      IL_008f:  ldnull
      IL_0090:  stfld      ""object C.<Main>d__0.<o>5__1""
      IL_0095:  ldarg.0
      IL_0096:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder C.<Main>d__0.<>t__builder""
      IL_009b:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
      IL_00a0:  nop
      IL_00a1:  ret
    }
", sequencePoints: "C+<Main>d__0.MoveNext", source: source);
        }

        [Fact]
        public void SwitchExpressionWithPattern()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    static string M(object o)
    {
        return o switch
        {
            int i => $""Number: {i}"",
            _ => ""Don't know""
        };
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""55"" />
          <slot kind=""temp"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""10"" endColumn=""11"" document=""1"" />
        <entry offset=""0x4"" startLine=""6"" startColumn=""18"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x5"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" hidden=""true"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x18"" startLine=""8"" startColumn=""22"" endLine=""8"" endColumn=""36"" document=""1"" />
        <entry offset=""0x2b"" startLine=""9"" startColumn=""18"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x33"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" startLine=""6"" startColumn=""9"" endLine=""10"" endColumn=""11"" document=""1"" />
        <entry offset=""0x37"" hidden=""true"" document=""1"" />
        <entry offset=""0x3b"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x3d"">
        <scope startOffset=""0x16"" endOffset=""0x2b"">
          <local name=""i"" il_index=""0"" il_start=""0x16"" il_end=""0x2b"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region DoStatement

        [Fact]
        public void DoStatement()
        {
            var source = WithWindowsLineBreaks(
@"using System;

public class SeqPointForWhile
{
    public static void Main()
    {
        SeqPointForWhile obj = new SeqPointForWhile();
        obj.While(234);
    }

    int field;
    public void While(int p)
    {
        do
        {
            p = (int)(p / 2);

            if (p > 100)
            {
                continue;
            }
            else if (p > 10)
            {
                field = 1;
            }
            else
            {
                break;
            }
        } while (p > 0); // SeqPt should be generated for [while (p > 0);]

        field = -1;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""28"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" document=""1"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x14"">
        <namespace name=""System"" />
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x14"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointForWhile"" name=""While"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointForWhile"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""71"" />
          <slot kind=""1"" offset=""159"" />
          <slot kind=""1"" offset=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" document=""1"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""1"" />
        <entry offset=""0xd"" hidden=""true"" document=""1"" />
        <entry offset=""0x10"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" document=""1"" />
        <entry offset=""0x11"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""26"" document=""1"" />
        <entry offset=""0x13"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1d"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" document=""1"" />
        <entry offset=""0x24"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""14"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x27"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""14"" document=""1"" />
        <entry offset=""0x28"" startLine=""28"" startColumn=""17"" endLine=""28"" endColumn=""23"" document=""1"" />
        <entry offset=""0x2a"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2b"" startLine=""30"" startColumn=""11"" endLine=""30"" endColumn=""25"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""20"" document=""1"" />
        <entry offset=""0x3a"" startLine=""33"" startColumn=""5"" endLine=""33"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Constructor

        [WorkItem(538317, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538317")]
        [Fact]
        public void ConstructorSequencePoints1()
        {
            var source = WithWindowsLineBreaks(
@"namespace NS
{
    public class MyClass
    {
        int intTest;
        public MyClass()
        {
            intTest = 123;
        }

        public MyClass(params int[] values)
        {
            intTest = values[0] + values[1] + values[2];
        }

        public static int Main()
        {
            int intI = 1, intJ = 8;
            int intK = 3;

            // Can't step into Ctor
            MyClass mc = new MyClass();

            // Can't step into Ctor
            mc = new MyClass(intI, intJ, intK);

            return mc.intTest - 12;
        }
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // Dev10 vs. Roslyn
            // 
            // Default Ctor (no param)
            //    Dev10                                                 Roslyn
            // ======================================================================================
            //  Code size       18 (0x12)                               // Code size       16 (0x10)
            //  .maxstack  8                                            .maxstack  8
            //* IL_0000:  ldarg.0                                      *IL_0000:  ldarg.0
            //  IL_0001:  call                                          IL_0001:  callvirt
            //      instance void [mscorlib]System.Object::.ctor()         instance void [mscorlib]System.Object::.ctor()
            //  IL_0006:  nop                                          *IL_0006:  nop
            //* IL_0007:  nop
            //* IL_0008:  ldarg.0                                      *IL_0007:  ldarg.0
            //  IL_0009:  ldc.i4.s   123                                IL_0008:  ldc.i4.s   123
            //  IL_000b:  stfld      int32 NS.MyClass::intTest          IL_000a:  stfld      int32 NS.MyClass::intTest
            //  IL_0010:  nop                                           
            //* IL_0011:  ret                                          *IL_000f:  ret
            //  -----------------------------------------------------------------------------------------
            //  SeqPoint: 0, 7 ,8, 0x10                                 0, 6, 7, 0xf

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""NS.MyClass"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""25"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""27"" document=""1"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""NS.MyClass"" name="".ctor"" parameterNames=""values"">
      <customDebugInfo>
        <forward declaringType=""NS.MyClass"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""44"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""57"" document=""1"" />
        <entry offset=""0x19"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""NS.MyClass"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""NS.MyClass"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""29"" />
          <slot kind=""0"" offset=""56"" />
          <slot kind=""0"" offset=""126"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" document=""1"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""27"" endLine=""18"" endColumn=""35"" document=""1"" />
        <entry offset=""0x5"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""26"" document=""1"" />
        <entry offset=""0x7"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""40"" document=""1"" />
        <entry offset=""0xd"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""48"" document=""1"" />
        <entry offset=""0x25"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""36"" document=""1"" />
        <entry offset=""0x32"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <local name=""intI"" il_index=""0"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intJ"" il_index=""1"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""intK"" il_index=""2"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
        <local name=""mc"" il_index=""3"" il_start=""0x0"" il_end=""0x35"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ConstructorSequencePoints2()
        {
            TestSequencePoints(
@"using System;

class D
{
    public D() : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

class D
{
    static D()
    [|{|]
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;
class A : Attribute {}
class D
{
    [A]
    public D() : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;
class A : Attribute {}
class D
{
    [A]
    public D() 
        : [|base()|]
    {
    }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

class A : Attribute {}
class C { }
class D
{
    [A]
    [|public D()|]
    {
    }
}", TestOptions.DebugDll);
        }

        #endregion

        #region Destructor

        [Fact]
        public void Destructors()
        {
            var source = @"
using System;

public class Base
{
    ~Base()
    {
        Console.WriteLine();
    }
}

public class Derived : Base
{
    ~Derived()
    {
        Console.WriteLine();
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Base"" name=""Finalize"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""29"" document=""1"" />
        <entry offset=""0xa"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x12"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""Derived"" name=""Finalize"">
      <customDebugInfo>
        <forward declaringType=""Base"" methodName=""Finalize"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""29"" document=""1"" />
        <entry offset=""0xa"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Field and Property Initializers

        [Fact]
        [WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")]
        public void TestPartialClassFieldInitializers()
        {
            var text1 = WithWindowsLineBreaks(@"
public partial class C
{
    int x = 1;
}
");

            var text2 = WithWindowsLineBreaks(@"
public partial class C
{
    int y = 1;

    static void Main()
    {
        C c = new C();
    }
}
");
            // Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            // loads the assembly into the ReflectionOnlyLoadFrom context.
            // So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateCompilation(new SyntaxTree[] { Parse(text1, "a.cs"), Parse(text2, "b.cs") });

            compilation.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name=""b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""BB-7A-A6-D2-B2-32-59-43-8C-98-7F-E1-98-8D-F0-94-68-E9-EB-80"" />
    <file id=""2"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""B4-EA-18-73-D2-0E-7F-15-51-4C-68-86-40-DF-E3-C3-97-9D-F6-B7"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""2"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            compilation.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""B4-EA-18-73-D2-0E-7F-15-51-4C-68-86-40-DF-E3-C3-97-9D-F6-B7"" />
    <file id=""2"" name=""b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""BB-7A-A6-D2-B2-32-59-43-8C-98-7F-E1-98-8D-F0-94-68-E9-EB-80"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""1"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""15"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [Fact]
        [WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")]
        public void TestPartialClassFieldInitializersWithLineDirectives()
        {
            var text1 = WithWindowsLineBreaks(@"
using System;
public partial class C
{
    int x = 1;
#line 12 ""foo.cs""
    int z = Math.Abs(-3);
    int w = Math.Abs(4);
#line 17 ""bar.cs""
    double zed = Math.Sin(5);
}

#pragma checksum ""mah.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

");

            var text2 = WithWindowsLineBreaks(@"
using System;
public partial class C
{
    int y = 1;
    int x2 = 1;
#line 12 ""foo2.cs""
    int z2 = Math.Abs(-3);
    int w2 = Math.Abs(4);
}
");

            var text3 = WithWindowsLineBreaks(@"
using System;
public partial class C
{
#line 112 ""mah.cs""
    int y3 = 1;
    int x3 = 1;
    int z3 = Math.Abs(-3);
#line default
    int w3 = Math.Abs(4);
    double zed3 = Math.Sin(5);

    C() {
        Console.WriteLine(""hi"");
    } 

    static void Main()
    {
        C c = new C();
    }
}
");

            //Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            //loads the assembly into the ReflectionOnlyLoadFrom context.
            //So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateCompilation(new[] { Parse(text1, "a.cs"), Parse(text2, "b.cs"), Parse(text3, "a.cs") }, options: TestOptions.DebugDll);
            compilation.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""E2-3B-47-02-DC-E4-8D-B4-FF-00-67-90-31-68-74-C0-06-D7-39-0E"" />
    <file id=""2"" name=""b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-CE-E5-E9-CB-53-E5-EF-C1-7F-2C-53-EC-02-FE-5C-34-2C-EF-94"" />
    <file id=""3"" name=""foo.cs"" language=""C#"" />
    <file id=""4"" name=""bar.cs"" language=""C#"" />
    <file id=""5"" name=""foo2.cs"" language=""C#"" />
    <file id=""6"" name=""mah.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""26"" document=""3"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""25"" document=""3"" />
        <entry offset=""0x20"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""30"" document=""4"" />
        <entry offset=""0x34"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""2"" />
        <entry offset=""0x3b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""16"" document=""2"" />
        <entry offset=""0x42"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""27"" document=""5"" />
        <entry offset=""0x4f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""26"" document=""5"" />
        <entry offset=""0x5b"" startLine=""112"" startColumn=""5"" endLine=""112"" endColumn=""16"" document=""6"" />
        <entry offset=""0x62"" startLine=""113"" startColumn=""5"" endLine=""113"" endColumn=""16"" document=""6"" />
        <entry offset=""0x69"" startLine=""114"" startColumn=""5"" endLine=""114"" endColumn=""27"" document=""6"" />
        <entry offset=""0x76"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""26"" document=""1"" />
        <entry offset=""0x82"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""31"" document=""1"" />
        <entry offset=""0x96"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""8"" document=""1"" />
        <entry offset=""0x9d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x9e"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""33"" document=""1"" />
        <entry offset=""0xa9"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(543313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543313")]
        [Fact]
        public void TestFieldInitializerExpressionLambda()
        {
            var source = @"
class C
{
    int x = ((System.Func<int, int>)(z => z))(1);
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>1</methodOrdinal>
          <lambda offset=""-6"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""50"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__1_0"" parameterNames=""z"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""43"" endLine=""4"" endColumn=""44"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FieldInitializerSequencePointSpans()
        {
            var source = @"
class C
{
    int x = 1, y = 2;
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""14"" document=""1"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""16"" endLine=""4"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Auto-Property

        [WorkItem(820806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820806")]
        [Fact]
        public void BreakpointForAutoImplementedProperty()
        {
            var source = @"
public class C
{
    public static int AutoProp1 { get; private set; }
    internal string AutoProp2 { get; set; }
    internal protected C AutoProp3 { internal get; set;  }
}
";

            var comp = CreateCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_AutoProp1"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""35"" endLine=""4"" endColumn=""39"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp1"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""40"" endLine=""4"" endColumn=""52"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_AutoProp2"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""33"" endLine=""5"" endColumn=""37"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp2"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""38"" endLine=""5"" endColumn=""42"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_AutoProp3"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""38"" endLine=""6"" endColumn=""51"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp3"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""52"" endLine=""6"" endColumn=""56"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void PropertyDeclaration()
        {
            TestSequencePoints(
@"using System;

public class C
{
    int P { [|get;|] set; }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get; [|set;|] }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get [|{|] return 0; } }
}", TestOptions.DebugDll);

            TestSequencePoints(
@"using System;

public class C
{
    int P { get; } = [|int.Parse(""42"")|];
}", TestOptions.DebugDll, TestOptions.Regular);
        }

        #endregion

        #region ReturnStatement

        [Fact]
        public void Return_Implicit()
        {
            var source = @"class C
{
    static void Main()
    {
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Return_Explicit()
        {
            var source = @"class C
{
    static void Main()
    {
        return;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""16"" document=""1"" />
        <entry offset=""0x3"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(538298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538298")]
        [Fact]
        public void RegressSeqPtEndOfMethodAfterReturn()
        {
            var source = WithWindowsLineBreaks(
@"using System;

public class SeqPointAfterReturn
{
    public static int Main()
    {
        int ret = 0;
        ReturnVoid(100);
        if (field != ""Even"")
            ret = 1;

        ReturnVoid(99);
        if (field != ""Odd"")
            ret = ret + 1;

        string rets = ReturnValue(101);
        if (rets != ""Odd"")
            ret = ret + 1;

        rets = ReturnValue(102);
        if (rets != ""Even"")
            ret = ret + 1;

        return ret;
    }

    static string field;
    public static void ReturnVoid(int p)
    {
        int x = (int)(p % 2);
        if (x == 0)
        {
            field = ""Even"";
        }
        else
        {
            field = ""Odd"";
        }
    }

    public static string ReturnValue(int p)
    {
        int x = (int)(p % 2);
        if (x == 0)
        {
            return ""Even"";
        }
        else
        {
            return ""Odd"";
        }
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // Expected are current actual output plus Two extra expected SeqPt:
            //  <entry offset=""0x73"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""1"" />
            //  <entry offset=""0x22"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" document=""1"" />
            // 
            // Note: NOT include other differences between Roslyn and Dev10, as they are filed in separated bugs
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""SeqPointAfterReturn"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""204"" />
          <slot kind=""1"" offset=""59"" />
          <slot kind=""1"" offset=""138"" />
          <slot kind=""1"" offset=""238"" />
          <slot kind=""1"" offset=""330"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""25"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x1e"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x20"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""24"" document=""1"" />
        <entry offset=""0x28"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""28"" document=""1"" />
        <entry offset=""0x38"" hidden=""true"" document=""1"" />
        <entry offset=""0x3b"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""27"" document=""1"" />
        <entry offset=""0x3f"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""40"" document=""1"" />
        <entry offset=""0x47"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""27"" document=""1"" />
        <entry offset=""0x54"" hidden=""true"" document=""1"" />
        <entry offset=""0x58"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""27"" document=""1"" />
        <entry offset=""0x5c"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""33"" document=""1"" />
        <entry offset=""0x64"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""28"" document=""1"" />
        <entry offset=""0x71"" hidden=""true"" document=""1"" />
        <entry offset=""0x75"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""27"" document=""1"" />
        <entry offset=""0x79"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""20"" document=""1"" />
        <entry offset=""0x7e"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x81"">
        <namespace name=""System"" />
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
        <local name=""rets"" il_index=""1"" il_start=""0x0"" il_end=""0x81"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnVoid"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointAfterReturn"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""29"" startColumn=""5"" endLine=""29"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""30"" document=""1"" />
        <entry offset=""0x5"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""20"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""33"" startColumn=""13"" endLine=""33"" endColumn=""28"" document=""1"" />
        <entry offset=""0x18"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""10"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1c"" startLine=""37"" startColumn=""13"" endLine=""37"" endColumn=""27"" document=""1"" />
        <entry offset=""0x26"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""10"" document=""1"" />
        <entry offset=""0x27"" startLine=""39"" startColumn=""5"" endLine=""39"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""SeqPointAfterReturn"" name=""ReturnValue"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""SeqPointAfterReturn"" methodName=""Main"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""42"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""42"" startColumn=""5"" endLine=""42"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""9"" endLine=""43"" endColumn=""30"" document=""1"" />
        <entry offset=""0x5"" startLine=""44"" startColumn=""9"" endLine=""44"" endColumn=""20"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0xd"" startLine=""45"" startColumn=""9"" endLine=""45"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""46"" startColumn=""13"" endLine=""46"" endColumn=""27"" document=""1"" />
        <entry offset=""0x16"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""10"" document=""1"" />
        <entry offset=""0x17"" startLine=""50"" startColumn=""13"" endLine=""50"" endColumn=""26"" document=""1"" />
        <entry offset=""0x1f"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x21"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Exception Handling

        [WorkItem(542064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542064")]
        [Fact]
        public void ExceptionHandling()
        {
            var source = WithWindowsLineBreaks(@"
class Test
{
    static int Main()
    {
        int ret = 0; // stop 1
        try
        { // stop 2
            throw new System.Exception(); // stop 3
        }
        catch (System.Exception e) // stop 4
        { // stop 5
            ret = 1; // stop 6
        }

        try
        { // stop 7
            throw new System.Exception(); // stop 8
        }
        catch // stop 9
        { // stop 10
            return ret; // stop 11
        }

    }
}
");
            // Dev12 inserts an additional sequence point on catch clause, just before 
            // the exception object is assigned to the variable. We don't place that sequence point.
            // Also the scope of he exception variable is different.

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Test.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""147"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""42"" document=""1"" />
        <entry offset=""0xa"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""35"" document=""1"" />
        <entry offset=""0xb"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""21"" document=""1"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x13"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""42"" document=""1"" />
        <entry offset=""0x19"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" document=""1"" />
        <entry offset=""0x1a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1b"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""24"" document=""1"" />
        <entry offset=""0x1f"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x21"">
        <local name=""ret"" il_index=""0"" il_start=""0x0"" il_end=""0x21"" attributes=""0"" />
        <scope startOffset=""0xa"" endOffset=""0x11"">
          <local name=""e"" il_index=""1"" il_start=""0xa"" il_end=""0x11"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        [Fact]
        public void ExceptionHandling_Filter_Debug1()
        {
            var source = WithWindowsLineBreaks(@"
using System;
using System.IO;

class Test
{
    static string filter(Exception e)
    {
        return null;
    }

    static void Main()
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (IOException e) when (filter(e) != null)
        {
            Console.WriteLine();
        }
        catch (Exception e) when (filter(e) != null)
        {
            Console.WriteLine();
        }
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll));

            v.VerifyIL("Test.Main", @"
{
  // Code size       89 (0x59)
  .maxstack  2
  .locals init (System.IO.IOException V_0, //e
                bool V_1,
                System.Exception V_2, //e
                bool V_3)
 -IL_0000:  nop
  .try
  {
   -IL_0001:  nop
   -IL_0002:  newobj     ""System.InvalidOperationException..ctor()""
    IL_0007:  throw
  }
  filter
  {
   ~IL_0008:  isinst     ""System.IO.IOException""
    IL_000d:  dup
    IL_000e:  brtrue.s   IL_0014
    IL_0010:  pop
    IL_0011:  ldc.i4.0
    IL_0012:  br.s       IL_0023
    IL_0014:  stloc.0
   -IL_0015:  ldloc.0
    IL_0016:  call       ""string Test.filter(System.Exception)""
    IL_001b:  ldnull
    IL_001c:  cgt.un
    IL_001e:  stloc.1
   ~IL_001f:  ldloc.1
    IL_0020:  ldc.i4.0
    IL_0021:  cgt.un
    IL_0023:  endfilter
  }  // end filter
  {  // handler
   ~IL_0025:  pop
   -IL_0026:  nop
   -IL_0027:  call       ""void System.Console.WriteLine()""
    IL_002c:  nop
   -IL_002d:  nop
    IL_002e:  leave.s    IL_0058
  }
  filter
  {
   ~IL_0030:  isinst     ""System.Exception""
    IL_0035:  dup
    IL_0036:  brtrue.s   IL_003c
    IL_0038:  pop
    IL_0039:  ldc.i4.0
    IL_003a:  br.s       IL_004b
    IL_003c:  stloc.2
   -IL_003d:  ldloc.2
    IL_003e:  call       ""string Test.filter(System.Exception)""
    IL_0043:  ldnull
    IL_0044:  cgt.un
    IL_0046:  stloc.3
   ~IL_0047:  ldloc.3
    IL_0048:  ldc.i4.0
    IL_0049:  cgt.un
    IL_004b:  endfilter
  }  // end filter
  {  // handler
   ~IL_004d:  pop
   -IL_004e:  nop
   -IL_004f:  call       ""void System.Console.WriteLine()""
    IL_0054:  nop
   -IL_0055:  nop
    IL_0056:  leave.s    IL_0058
  }
 -IL_0058:  ret
}
", sequencePoints: "Test.Main");

            v.VerifyPdb("Test.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""Test"" methodName=""filter"" parameterNames=""e"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""104"" />
          <slot kind=""1"" offset=""120"" />
          <slot kind=""0"" offset=""216"" />
          <slot kind=""1"" offset=""230"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""51"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" startLine=""18"" startColumn=""31"" endLine=""18"" endColumn=""55"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x26"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x27"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""33"" document=""1"" />
        <entry offset=""0x2d"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x3d"" startLine=""22"" startColumn=""29"" endLine=""22"" endColumn=""53"" document=""1"" />
        <entry offset=""0x47"" hidden=""true"" document=""1"" />
        <entry offset=""0x4d"" hidden=""true"" document=""1"" />
        <entry offset=""0x4e"" startLine=""23"" startColumn=""9"" endLine=""23"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4f"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""33"" document=""1"" />
        <entry offset=""0x55"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" document=""1"" />
        <entry offset=""0x58"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x59"">
        <scope startOffset=""0x8"" endOffset=""0x30"">
          <local name=""e"" il_index=""0"" il_start=""0x8"" il_end=""0x30"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x30"" endOffset=""0x58"">
          <local name=""e"" il_index=""2"" il_start=""0x30"" il_end=""0x58"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        [Fact]
        public void ExceptionHandling_Filter_Debug2()
        {
            var source = WithWindowsLineBreaks(@"
class Test
{
    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch when (F())
        { 
            System.Console.WriteLine();
        }
    }

    private static bool F()
    {
        return true;
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll));
            v.VerifyIL("Test.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (bool V_0)
 -IL_0000:  nop
  .try
  {
   -IL_0001:  nop
   -IL_0002:  newobj     ""System.Exception..ctor()""
    IL_0007:  throw
  }
  filter
  {
   ~IL_0008:  pop
   -IL_0009:  call       ""bool Test.F()""
    IL_000e:  stloc.0
   ~IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  cgt.un
    IL_0013:  endfilter
  }  // end filter
  {  // handler
   ~IL_0015:  pop
   -IL_0016:  nop
   -IL_0017:  call       ""void System.Console.WriteLine()""
    IL_001c:  nop
   -IL_001d:  nop
    IL_001e:  leave.s    IL_0020
  }
 -IL_0020:  ret
}
", sequencePoints: "Test.Main");

            v.VerifyPdb("Test.Main", @"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""95"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""42"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0x9"" startLine=""10"" startColumn=""15"" endLine=""10"" endColumn=""25"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x16"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x17"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x20"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        [Fact]
        public void ExceptionHandling_Filter_Debug3()
        {
            var source = WithWindowsLineBreaks(@"
class Test
{
    static bool a = true;

    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch when (a)
        { 
            System.Console.WriteLine();
        }
    }
}
");
            var v = CompileAndVerify(CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll));
            v.VerifyIL("Test.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (bool V_0)
 -IL_0000:  nop
  .try
  {
   -IL_0001:  nop
   -IL_0002:  newobj     ""System.Exception..ctor()""
    IL_0007:  throw
  }
  filter
  {
   ~IL_0008:  pop
   -IL_0009:  ldsfld     ""bool Test.a""
    IL_000e:  stloc.0
   ~IL_000f:  ldloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  cgt.un
    IL_0013:  endfilter
  }  // end filter
  {  // handler
   ~IL_0015:  pop
   -IL_0016:  nop
   -IL_0017:  call       ""void System.Console.WriteLine()""
    IL_001c:  nop
   -IL_001d:  nop
    IL_001e:  leave.s    IL_0020
  }
 -IL_0020:  ret
}
", sequencePoints: "Test.Main");

            v.VerifyPdb("Test.Main", @"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""95"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""42"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0x9"" startLine=""12"" startColumn=""15"" endLine=""12"" endColumn=""23"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x16"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x17"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1d"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x20"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        [Fact]
        public void ExceptionHandling_Filter_Release3()
        {
            var source = @"
class Test
{
    static bool a = true;

    static void Main()
    {
        try
        {
            throw new System.Exception();
        }
        catch when (a)
        { 
            System.Console.WriteLine();
        }
    }
}
";
            var v = CompileAndVerify(CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseDll));
            v.VerifyIL("Test.Main", @"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .try
  {
   -IL_0000:  newobj     ""System.Exception..ctor()""
    IL_0005:  throw
  }
  filter
  {
   ~IL_0006:  pop
   -IL_0007:  ldsfld     ""bool Test.a""
    IL_000c:  ldc.i4.0
    IL_000d:  cgt.un
    IL_000f:  endfilter
  }  // end filter
  {  // handler
   ~IL_0011:  pop
   -IL_0012:  call       ""void System.Console.WriteLine()""
   -IL_0017:  leave.s    IL_0019
  }
 -IL_0019:  ret
}
", sequencePoints: "Test.Main");

            v.VerifyPdb("Test.Main", @"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""42"" document=""1"" />
        <entry offset=""0x6"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""15"" endLine=""12"" endColumn=""23"" document=""1"" />
        <entry offset=""0x11"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""40"" document=""1"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x19"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(778655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/778655")]
        [Fact]
        public void BranchToStartOfTry()
        {
            string source = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string str = null;
        bool isEmpty = string.IsNullOrEmpty(str);
        // isEmpty is always true here, so it should never go thru this if statement.
        if (!isEmpty)
        {
            throw new Exception();
        }
        try
        {
            Console.WriteLine();
        }
        catch
        {
        }
    }
}
");
            // Note the hidden sequence point @IL_0019.
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
          <slot kind=""0"" offset=""44"" />
          <slot kind=""1"" offset=""177"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""27"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""50"" document=""1"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""22"" document=""1"" />
        <entry offset=""0xf"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x13"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""35"" document=""1"" />
        <entry offset=""0x19"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1b"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""33"" document=""1"" />
        <entry offset=""0x21"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
        <entry offset=""0x24"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" document=""1"" />
        <entry offset=""0x25"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" document=""1"" />
        <entry offset=""0x26"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x29"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2a"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <local name=""str"" il_index=""0"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
        <local name=""isEmpty"" il_index=""1"" il_start=""0x0"" il_end=""0x2a"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region UsingStatement

        [Fact]
        public void UsingStatement_EmbeddedStatement()
        {
            var source = WithWindowsLineBreaks(@"
public class DisposableClass : System.IDisposable
{
    public DisposableClass(int a) { }
    public void Dispose() { }
}

class C
{
    static void Main()
    {
        using (DisposableClass a = new DisposableClass(1), b = new DisposableClass(2))
            System.Console.WriteLine(""First"");
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            v.VerifyIL("C.Main", sequencePoints: "C.Main", source: source, expectedIL: @"
 {
   // Code size       53 (0x35)
   .maxstack  1
   .locals init (DisposableClass V_0, //a
                 DisposableClass V_1) //b
   // sequence point: {
   IL_0000:  nop
   // sequence point: DisposableClass a = new DisposableClass(1)
   IL_0001:  ldc.i4.1
   IL_0002:  newobj     ""DisposableClass..ctor(int)""
   IL_0007:  stloc.0
   .try
   {
     // sequence point: b = new DisposableClass(2)
     IL_0008:  ldc.i4.2
     IL_0009:  newobj     ""DisposableClass..ctor(int)""
     IL_000e:  stloc.1
     .try
     {
       // sequence point: System.Console.WriteLine(""First"");
       IL_000f:  ldstr      ""First""
       IL_0014:  call       ""void System.Console.WriteLine(string)""
       IL_0019:  nop
       IL_001a:  leave.s    IL_0027
     }
     finally
     {
       // sequence point: <hidden>
       IL_001c:  ldloc.1
       IL_001d:  brfalse.s  IL_0026
       IL_001f:  ldloc.1
       IL_0020:  callvirt   ""void System.IDisposable.Dispose()""
       IL_0025:  nop
       // sequence point: <hidden>
       IL_0026:  endfinally
     }
     // sequence point: <hidden>
     IL_0027:  leave.s    IL_0034
   }
   finally
   {
     // sequence point: <hidden>
     IL_0029:  ldloc.0
     IL_002a:  brfalse.s  IL_0033
     IL_002c:  ldloc.0
     IL_002d:  callvirt   ""void System.IDisposable.Dispose()""
     IL_0032:  nop
     // sequence point: <hidden>
     IL_0033:  endfinally
   }
   // sequence point: }
   IL_0034:  ret
 }
");

            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""a"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""62"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""58"" document=""1"" />
        <entry offset=""0x8"" startLine=""12"" startColumn=""60"" endLine=""12"" endColumn=""86"" document=""1"" />
        <entry offset=""0xf"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""47"" document=""1"" />
        <entry offset=""0x1c"" hidden=""true"" document=""1"" />
        <entry offset=""0x26"" hidden=""true"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" hidden=""true"" document=""1"" />
        <entry offset=""0x33"" hidden=""true"" document=""1"" />
        <entry offset=""0x34"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x35"">
        <scope startOffset=""0x1"" endOffset=""0x34"">
          <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x34"" attributes=""0"" />
          <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x34"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void UsingStatement_Block()
        {
            var source = WithWindowsLineBreaks(@"
public class DisposableClass : System.IDisposable
{
    public DisposableClass(int a) { }
    public void Dispose() { }
}

class C
{
    static void Main()
    {
        using (DisposableClass c = new DisposableClass(3), d = new DisposableClass(4))
        {
            System.Console.WriteLine(""Second"");
        }
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            v.VerifyIL("C.Main", sequencePoints: "C.Main", source: source, expectedIL: @"
{
  // Code size       55 (0x37)
  .maxstack  1
  .locals init (DisposableClass V_0, //c
                DisposableClass V_1) //d
  // sequence point: {
  IL_0000:  nop
  // sequence point: DisposableClass c = new DisposableClass(3)
  IL_0001:  ldc.i4.3
  IL_0002:  newobj     ""DisposableClass..ctor(int)""
  IL_0007:  stloc.0
  .try
  {
    // sequence point: d = new DisposableClass(4)
    IL_0008:  ldc.i4.4
    IL_0009:  newobj     ""DisposableClass..ctor(int)""
    IL_000e:  stloc.1
    .try
    {
      // sequence point: {
      IL_000f:  nop
      // sequence point: System.Console.WriteLine(""Second"");
      IL_0010:  ldstr      ""Second""
      IL_0015:  call       ""void System.Console.WriteLine(string)""
      IL_001a:  nop
      // sequence point: }
      IL_001b:  nop
      IL_001c:  leave.s    IL_0029
    }
    finally
    {
      // sequence point: <hidden>
      IL_001e:  ldloc.1
      IL_001f:  brfalse.s  IL_0028
      IL_0021:  ldloc.1
      IL_0022:  callvirt   ""void System.IDisposable.Dispose()""
      IL_0027:  nop
      // sequence point: <hidden>
      IL_0028:  endfinally
    }
    // sequence point: <hidden>
    IL_0029:  leave.s    IL_0036
  }
  finally
  {
    // sequence point: <hidden>
    IL_002b:  ldloc.0
    IL_002c:  brfalse.s  IL_0035
    IL_002e:  ldloc.0
    IL_002f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0034:  nop
    // sequence point: <hidden>
    IL_0035:  endfinally
  }
  // sequence point: }
  IL_0036:  ret
}
"
);
            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""a"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""62"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""58"" document=""1"" />
        <entry offset=""0x8"" startLine=""12"" startColumn=""60"" endLine=""12"" endColumn=""86"" document=""1"" />
        <entry offset=""0xf"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x10"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""48"" document=""1"" />
        <entry offset=""0x1b"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1e"" hidden=""true"" document=""1"" />
        <entry offset=""0x28"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" hidden=""true"" document=""1"" />
        <entry offset=""0x2b"" hidden=""true"" document=""1"" />
        <entry offset=""0x35"" hidden=""true"" document=""1"" />
        <entry offset=""0x36"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x37"">
        <scope startOffset=""0x1"" endOffset=""0x36"">
          <local name=""c"" il_index=""0"" il_start=""0x1"" il_end=""0x36"" attributes=""0"" />
          <local name=""d"" il_index=""1"" il_start=""0x1"" il_end=""0x36"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
        [Fact]
        public void UsingStatement_EmbeddedConditional()
        {
            var source = @"
class C
{
    bool F()
    {
        bool x = true;
        bool value = false;
        using (var stream = new System.IO.MemoryStream())
            if (x)
            {
                value = true;
            }
            else
                value = false;

        return value;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (bool V_0, //x
                bool V_1, //value
                System.IO.MemoryStream V_2, //stream
                bool V_3,
                bool V_4)
  // sequence point: {
  IL_0000:  nop
  // sequence point: bool x = true;
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  // sequence point: bool value = false;
  IL_0003:  ldc.i4.0
  IL_0004:  stloc.1
  // sequence point: var stream = new System.IO.MemoryStream()
  IL_0005:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_000a:  stloc.2
  .try
  {
    // sequence point: if (x)
    IL_000b:  ldloc.0
    IL_000c:  stloc.3
    // sequence point: <hidden>
    IL_000d:  ldloc.3
    IL_000e:  brfalse.s  IL_0016
    // sequence point: {
    IL_0010:  nop
    // sequence point: value = true;
    IL_0011:  ldc.i4.1
    IL_0012:  stloc.1
    // sequence point: }
    IL_0013:  nop
    // sequence point: <hidden>
    IL_0014:  br.s       IL_0018
    // sequence point: value = false;
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.1
    // sequence point: <hidden>
    IL_0018:  leave.s    IL_0025
  }
  finally
  {
    // sequence point: <hidden>
    IL_001a:  ldloc.2
    IL_001b:  brfalse.s  IL_0024
    IL_001d:  ldloc.2
    IL_001e:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0023:  nop
    // sequence point: <hidden>
    IL_0024:  endfinally
  }
  // sequence point: return value;
  IL_0025:  ldloc.1
  IL_0026:  stloc.s    V_4
  IL_0028:  br.s       IL_002a
  // sequence point: }
  IL_002a:  ldloc.s    V_4
  IL_002c:  ret
}
", sequencePoints: "C.F", source: source);
        }

        [WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
        [Fact]
        public void UsingStatement_EmbeddedConditional2()
        {
            var source = @"
class C
{
    bool F()
    {
        bool x = true;
        bool value = false;
        using (var stream = new System.IO.MemoryStream())
            if (x)
            {
                value = true;
            }
            else
            {
                value = false;
            }

        return value;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       47 (0x2f)
  .maxstack  1
  .locals init (bool V_0, //x
                bool V_1, //value
                System.IO.MemoryStream V_2, //stream
                bool V_3,
                bool V_4)
  // sequence point: {
  IL_0000:  nop
  // sequence point: bool x = true;
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  // sequence point: bool value = false;
  IL_0003:  ldc.i4.0
  IL_0004:  stloc.1
  // sequence point: var stream = new System.IO.MemoryStream()
  IL_0005:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_000a:  stloc.2
  .try
  {
    // sequence point: if (x)
    IL_000b:  ldloc.0
    IL_000c:  stloc.3
    // sequence point: <hidden>
    IL_000d:  ldloc.3
    IL_000e:  brfalse.s  IL_0016
    // sequence point: {
    IL_0010:  nop
    // sequence point: value = true;
    IL_0011:  ldc.i4.1
    IL_0012:  stloc.1
    // sequence point: }
    IL_0013:  nop
    // sequence point: <hidden>
    IL_0014:  br.s       IL_001a
    // sequence point: {
    IL_0016:  nop
    // sequence point: value = false;
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.1
    // sequence point: }
    IL_0019:  nop
    // sequence point: <hidden>
    IL_001a:  leave.s    IL_0027
  }
  finally
  {
    // sequence point: <hidden>
    IL_001c:  ldloc.2
    IL_001d:  brfalse.s  IL_0026
    IL_001f:  ldloc.2
    IL_0020:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0025:  nop
    // sequence point: <hidden>
    IL_0026:  endfinally
  }
  // sequence point: return value;
  IL_0027:  ldloc.1
  IL_0028:  stloc.s    V_4
  IL_002a:  br.s       IL_002c
  // sequence point: }
  IL_002c:  ldloc.s    V_4
  IL_002e:  ret
}
", sequencePoints: "C.F", source: source);
        }

        [WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
        [Fact]
        public void UsingStatement_EmbeddedWhile()
        {
            var source = @"
class C
{
    void F(bool x)
    {
        using (var stream = new System.IO.MemoryStream())
            while (x)
                x = false;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (System.IO.MemoryStream V_0, //stream
                bool V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: var stream = new System.IO.MemoryStream()
  IL_0001:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  br.s       IL_000c
    // sequence point: x = false;
    IL_0009:  ldc.i4.0
    IL_000a:  starg.s    V_1
    // sequence point: while (x)
    IL_000c:  ldarg.1
    IL_000d:  stloc.1
    // sequence point: <hidden>
    IL_000e:  ldloc.1
    IL_000f:  brtrue.s   IL_0009
    IL_0011:  leave.s    IL_001e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0013:  ldloc.0
    IL_0014:  brfalse.s  IL_001d
    IL_0016:  ldloc.0
    IL_0017:  callvirt   ""void System.IDisposable.Dispose()""
    IL_001c:  nop
    // sequence point: <hidden>
    IL_001d:  endfinally
  }
  // sequence point: }
  IL_001e:  ret
}
", sequencePoints: "C.F", source: source);
        }

        [WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
        [Fact]
        public void UsingStatement_EmbeddedFor()
        {
            var source = @"
class C
{
    void F(bool x)
    {
        using (var stream = new System.IO.MemoryStream())
            for ( ; x == true; )
                x = false;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (System.IO.MemoryStream V_0, //stream
                bool V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: var stream = new System.IO.MemoryStream()
  IL_0001:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: <hidden>
    IL_0007:  br.s       IL_000c
    // sequence point: x = false;
    IL_0009:  ldc.i4.0
    IL_000a:  starg.s    V_1
    // sequence point: x == true
    IL_000c:  ldarg.1
    IL_000d:  stloc.1
    // sequence point: <hidden>
    IL_000e:  ldloc.1
    IL_000f:  brtrue.s   IL_0009
    IL_0011:  leave.s    IL_001e
  }
  finally
  {
    // sequence point: <hidden>
    IL_0013:  ldloc.0
    IL_0014:  brfalse.s  IL_001d
    IL_0016:  ldloc.0
    IL_0017:  callvirt   ""void System.IDisposable.Dispose()""
    IL_001c:  nop
    // sequence point: <hidden>
    IL_001d:  endfinally
  }
  // sequence point: }
  IL_001e:  ret
}
", sequencePoints: "C.F", source: source);
        }

        [WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
        [Fact]
        public void LockStatement_EmbeddedIf()
        {
            var source = @"
class C
{
    void F(bool x)
    {
        string y = """";
        lock (y)
            if (!x)
                System.Console.Write(1);
            else
                System.Console.Write(2);
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);
            v.VerifyIL("C.F", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (string V_0, //y
                string V_1,
                bool V_2,
                bool V_3)
  // sequence point: {
  IL_0000:  nop
  // sequence point: string y = """";
  IL_0001:  ldstr      """"
  IL_0006:  stloc.0
  // sequence point: lock (y)
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  ldc.i4.0
  IL_000a:  stloc.2
  .try
  {
    IL_000b:  ldloc.1
    IL_000c:  ldloca.s   V_2
    IL_000e:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0013:  nop
    // sequence point: if (!x)
    IL_0014:  ldarg.1
    IL_0015:  ldc.i4.0
    IL_0016:  ceq
    IL_0018:  stloc.3
    // sequence point: <hidden>
    IL_0019:  ldloc.3
    IL_001a:  brfalse.s  IL_0025
    // sequence point: System.Console.Write(1);
    IL_001c:  ldc.i4.1
    IL_001d:  call       ""void System.Console.Write(int)""
    IL_0022:  nop
    // sequence point: <hidden>
    IL_0023:  br.s       IL_002c
    // sequence point: System.Console.Write(2);
    IL_0025:  ldc.i4.2
    IL_0026:  call       ""void System.Console.Write(int)""
    IL_002b:  nop
    // sequence point: <hidden>
    IL_002c:  leave.s    IL_0039
  }
  finally
  {
    // sequence point: <hidden>
    IL_002e:  ldloc.2
    IL_002f:  brfalse.s  IL_0038
    IL_0031:  ldloc.1
    IL_0032:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0037:  nop
    // sequence point: <hidden>
    IL_0038:  endfinally
  }
  // sequence point: }
  IL_0039:  ret
}
", sequencePoints: "C.F", source: source);
        }

        #endregion

        #region Using Declaration

        [WorkItem(37417, "https://github.com/dotnet/roslyn/issues/37417")]
        [Fact]
        public void UsingDeclaration_BodyBlockScope()
        {
            var source = WithWindowsLineBreaks(@"
using System;
using System.IO;
class C
{
    static void Main()
    {
        using MemoryStream m = new MemoryStream(), n = new MemoryStream();
        Console.WriteLine(1);
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            // TODO: https://github.com/dotnet/roslyn/issues/37417
            // Duplicate sequence point at `}`

            v.VerifyIL("C.Main", sequencePoints: "C.Main", source: source, expectedIL: @"
{
  // Code size       45 (0x2d)
  .maxstack  1
  .locals init (System.IO.MemoryStream V_0, //m
                System.IO.MemoryStream V_1) //n
  // sequence point: {
  IL_0000:  nop
  // sequence point: using MemoryStream m = new MemoryStream()
  IL_0001:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: n = new MemoryStream()
    IL_0007:  newobj     ""System.IO.MemoryStream..ctor()""
    IL_000c:  stloc.1
    .try
    {
      // sequence point: Console.WriteLine(1);
      IL_000d:  ldc.i4.1
      IL_000e:  call       ""void System.Console.WriteLine(int)""
      IL_0013:  nop
      // sequence point: }
      IL_0014:  leave.s    IL_002c
    }
    finally
    {
      // sequence point: <hidden>
      IL_0016:  ldloc.1
      IL_0017:  brfalse.s  IL_0020
      IL_0019:  ldloc.1
      IL_001a:  callvirt   ""void System.IDisposable.Dispose()""
      IL_001f:  nop
      // sequence point: <hidden>
      IL_0020:  endfinally
    }
  }
  finally
  {
    // sequence point: <hidden>
    IL_0021:  ldloc.0
    IL_0022:  brfalse.s  IL_002b
    IL_0024:  ldloc.0
    IL_0025:  callvirt   ""void System.IDisposable.Dispose()""
    IL_002a:  nop
    // sequence point: <hidden>
    IL_002b:  endfinally
  }
  // sequence point: }
  IL_002c:  ret
}
");

            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""30"" />
          <slot kind=""0"" offset=""54"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""50"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""52"" endLine=""8"" endColumn=""74"" document=""1"" />
        <entry offset=""0xd"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x14"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x20"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" hidden=""true"" document=""1"" />
        <entry offset=""0x2b"" hidden=""true"" document=""1"" />
        <entry offset=""0x2c"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2d"">
        <namespace name=""System"" />
        <namespace name=""System.IO"" />
        <local name=""m"" il_index=""0"" il_start=""0x0"" il_end=""0x2d"" attributes=""0"" />
        <local name=""n"" il_index=""1"" il_start=""0x0"" il_end=""0x2d"" attributes=""0"" />
      </scope>
    </method>
  </methods>
 </symbols>");
        }

        [WorkItem(37417, "https://github.com/dotnet/roslyn/issues/37417")]
        [Fact]
        public void UsingDeclaration_BodyBlockScopeWithReturn()
        {
            var source = WithWindowsLineBreaks(@"
using System;
using System.IO;
class C
{
    static int Main()
    {
        using MemoryStream m = new MemoryStream();
        Console.WriteLine(1);
        return 1;
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            // TODO: https://github.com/dotnet/roslyn/issues/37417
            // Duplicate sequence point at `}`

            v.VerifyIL("C.Main", sequencePoints: "C.Main", source: source, expectedIL: @"
{
  // Code size       31 (0x1f)
  .maxstack  1
  .locals init (System.IO.MemoryStream V_0, //m
                int V_1)
  // sequence point: {
  IL_0000:  nop
  // sequence point: using MemoryStream m = new MemoryStream();
  IL_0001:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: Console.WriteLine(1);
    IL_0007:  ldc.i4.1
    IL_0008:  call       ""void System.Console.WriteLine(int)""
    IL_000d:  nop
    // sequence point: return 1;
    IL_000e:  ldc.i4.1
    IL_000f:  stloc.1
    IL_0010:  leave.s    IL_001d
  }
  finally
  {
    // sequence point: <hidden>
    IL_0012:  ldloc.0
    IL_0013:  brfalse.s  IL_001c
    IL_0015:  ldloc.0
    IL_0016:  callvirt   ""void System.IDisposable.Dispose()""
    IL_001b:  nop
    // sequence point: <hidden>
    IL_001c:  endfinally
  }
  // sequence point: }
  IL_001d:  ldloc.1
  IL_001e:  ret
}
");

            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""30"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""51"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0xe"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""18"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1f"">
        <namespace name=""System"" />
        <namespace name=""System.IO"" />
        <local name=""m"" il_index=""0"" il_start=""0x0"" il_end=""0x1f"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(37417, "https://github.com/dotnet/roslyn/issues/37417")]
        [Fact]
        public void UsingDeclaration_IfBodyScope()
        {
            var source = WithWindowsLineBreaks(@"
using System;
using System.IO;
class C
{
    public static bool G() => true;

    static void Main()
    {
        if (G()) 
        {
            using var m = new MemoryStream();
            Console.WriteLine(1);
        }
        Console.WriteLine(2);
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            // TODO: https://github.com/dotnet/roslyn/issues/37417
            // In this case the sequence point `}` is not emitted on the leave instruction,
            // but to a nop instruction following the disposal.

            v.VerifyIL("C.Main", sequencePoints: "C.Main", source: source, expectedIL: @"
{
  // Code size       46 (0x2e)
  .maxstack  1
  .locals init (bool V_0,
                System.IO.MemoryStream V_1) //m
  // sequence point: {
  IL_0000:  nop
  // sequence point: if (G())
  IL_0001:  call       ""bool C.G()""
  IL_0006:  stloc.0
  // sequence point: <hidden>
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0026
  // sequence point: {
  IL_000a:  nop
  // sequence point: using var m = new MemoryStream();
  IL_000b:  newobj     ""System.IO.MemoryStream..ctor()""
  IL_0010:  stloc.1
  .try
  {
    // sequence point: Console.WriteLine(1);
    IL_0011:  ldc.i4.1
    IL_0012:  call       ""void System.Console.WriteLine(int)""
    IL_0017:  nop
    IL_0018:  leave.s    IL_0025
  }
  finally
  {
    // sequence point: <hidden>
    IL_001a:  ldloc.1
    IL_001b:  brfalse.s  IL_0024
    IL_001d:  ldloc.1
    IL_001e:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0023:  nop
    // sequence point: <hidden>
    IL_0024:  endfinally
  }
  // sequence point: }
  IL_0025:  nop
  // sequence point: Console.WriteLine(2);
  IL_0026:  ldc.i4.2
  IL_0027:  call       ""void System.Console.WriteLine(int)""
  IL_002c:  nop
  // sequence point: }
  IL_002d:  ret
}
");

            c.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" />
        <encLocalSlotMap>
          <slot kind=""1"" offset=""11"" />
          <slot kind=""0"" offset=""55"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""17"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""46"" document=""1"" />
        <entry offset=""0x11"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""34"" document=""1"" />
        <entry offset=""0x1a"" hidden=""true"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x25"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x26"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""30"" document=""1"" />
        <entry offset=""0x2d"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2e"">
        <scope startOffset=""0xa"" endOffset=""0x26"">
          <local name=""m"" il_index=""1"" il_start=""0xa"" il_end=""0x26"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
 </symbols>");
        }

        #endregion

        // LockStatement tested in CodeGenLock

        #region Anonymous Type

        [Fact]
        public void AnonymousType_Empty()
        {
            var source = WithWindowsLineBreaks(@"
class Program
{
    static void Main(string[] args)
    {
        var o = new {};
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""24"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x8"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void AnonymousType_NonEmpty()
        {
            var source = WithWindowsLineBreaks(@"
class Program
{
    static void Main(string[] args)
    {
        var o = new { a = 1 };
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""31"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0x9"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region FixedStatement

        [Fact]
        public void FixedStatementSingleAddress()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    int x;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x)
        {
            *p = 1;
        }
        Console.WriteLine(c.x);
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
          <slot kind=""9"" offset=""47"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" document=""1"" />
        <entry offset=""0xe"" startLine=""11"" startColumn=""16"" endLine=""11"" endColumn=""29"" document=""1"" />
        <entry offset=""0x11"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""20"" document=""1"" />
        <entry offset=""0x15"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x19"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""32"" document=""1"" />
        <entry offset=""0x25"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x26"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x26"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x19"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x19"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementSingleString()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (char* p = ""hello"")
        {
            Console.WriteLine(*p);
        }
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""24"" />
          <slot kind=""9"" offset=""24"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" document=""1"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x16"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""35"" document=""1"" />
        <entry offset=""0x1e"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x22"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x21"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x21"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementSingleArray()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    int[] a = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        fixed (int* p = c.a)
        {
            (*p)++;
        }
        Console.Write(c.a[0]);
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""79"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" document=""1"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""31"" document=""1"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""28"" document=""1"" />
        <entry offset=""0x32"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x33"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" document=""1"" />
        <entry offset=""0x39"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""31"" document=""1"" />
        <entry offset=""0x4a"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4b"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x4b"" attributes=""0"" />
        <scope startOffset=""0x15"" endOffset=""0x3c"">
          <local name=""p"" il_index=""1"" il_start=""0x15"" il_end=""0x3c"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleAddresses()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    int x;
    int y;
    
    static void Main()
    {
        C c = new C();
        fixed (int* p = &c.x, q = &c.y)
        {
            *p = 1;
            *q = 2;
        }
        Console.WriteLine(c.x + c.y);
    }
}
");
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""47"" />
          <slot kind=""0"" offset=""57"" />
          <slot kind=""9"" offset=""47"" />
          <slot kind=""9"" offset=""57"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""1"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""29"" document=""1"" />
        <entry offset=""0x19"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""39"" document=""1"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1e"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" document=""1"" />
        <entry offset=""0x21"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""20"" document=""1"" />
        <entry offset=""0x24"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x2c"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""38"" document=""1"" />
        <entry offset=""0x3f"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x40"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x2c"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x2c"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x2c"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleStrings()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    static void Main()
    {
        fixed (char* p = ""hello"", q = ""goodbye"")
        {
            Console.Write(*p);
            Console.Write(*q);
        }
    }
}
");
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""24"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""9"" offset=""24"" />
          <slot kind=""9"" offset=""37"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" document=""1"" />
        <entry offset=""0x1b"" startLine=""8"" startColumn=""35"" endLine=""8"" endColumn=""48"" document=""1"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""31"" document=""1"" />
        <entry offset=""0x32"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""31"" document=""1"" />
        <entry offset=""0x3a"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3b"" hidden=""true"" document=""1"" />
        <entry offset=""0x3f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x3f"">
          <local name=""p"" il_index=""0"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
          <local name=""q"" il_index=""1"" il_start=""0x1"" il_end=""0x3f"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleArrays()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    int[] a = new int[1];
    int[] b = new int[1];

    static void Main()
    {
        C c = new C();
        Console.Write(c.a[0]);
        Console.Write(c.b[0]);
        fixed (int* p = c.a, q = c.b)
        {
            *p = 1;
            *q = 2;
        }
        Console.Write(c.a[0]);
        Console.Write(c.b[0]);
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""111"" />
          <slot kind=""0"" offset=""120"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""31"" document=""1"" />
        <entry offset=""0x15"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""31"" document=""1"" />
        <entry offset=""0x23"" startLine=""14"" startColumn=""16"" endLine=""14"" endColumn=""28"" document=""1"" />
        <entry offset=""0x40"" startLine=""14"" startColumn=""30"" endLine=""14"" endColumn=""37"" document=""1"" />
        <entry offset=""0x60"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
        <entry offset=""0x61"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""20"" document=""1"" />
        <entry offset=""0x64"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""20"" document=""1"" />
        <entry offset=""0x67"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" document=""1"" />
        <entry offset=""0x68"" hidden=""true"" document=""1"" />
        <entry offset=""0x6d"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""31"" document=""1"" />
        <entry offset=""0x7b"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""31"" document=""1"" />
        <entry offset=""0x89"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8a"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x8a"" attributes=""0"" />
        <scope startOffset=""0x23"" endOffset=""0x6d"">
          <local name=""p"" il_index=""1"" il_start=""0x23"" il_end=""0x6d"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x23"" il_end=""0x6d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" document=""1"" />
        <entry offset=""0xc"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""26"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleMixed()
        {
            var source = WithWindowsLineBreaks(@"
using System;

unsafe class C
{
    char c = 'a';
    char[] a = new char[1];

    static void Main()
    {
        C c = new C();
        fixed (char* p = &c.c, q = c.a, r = ""hello"")
        {
            Console.Write((int)*p);
            Console.Write((int)*q);
            Console.Write((int)*r);
        }
    }
}
");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""0"" offset=""48"" />
          <slot kind=""0"" offset=""58"" />
          <slot kind=""0"" offset=""67"" />
          <slot kind=""9"" offset=""48"" />
          <slot kind=""temp"" />
          <slot kind=""9"" offset=""67"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" document=""1"" />
        <entry offset=""0xf"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""30"" document=""1"" />
        <entry offset=""0x13"" startLine=""12"" startColumn=""32"" endLine=""12"" endColumn=""39"" document=""1"" />
        <entry offset=""0x3a"" startLine=""12"" startColumn=""41"" endLine=""12"" endColumn=""52"" document=""1"" />
        <entry offset=""0x49"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4a"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""36"" document=""1"" />
        <entry offset=""0x52"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""36"" document=""1"" />
        <entry offset=""0x5a"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""36"" document=""1"" />
        <entry offset=""0x62"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""1"" />
        <entry offset=""0x63"" hidden=""true"" document=""1"" />
        <entry offset=""0x6d"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6e"">
        <namespace name=""System"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x6e"" attributes=""0"" />
        <scope startOffset=""0x7"" endOffset=""0x6d"">
          <local name=""p"" il_index=""1"" il_start=""0x7"" il_end=""0x6d"" attributes=""0"" />
          <local name=""q"" il_index=""2"" il_start=""0x7"" il_end=""0x6d"" attributes=""0"" />
          <local name=""r"" il_index=""3"" il_start=""0x7"" il_end=""0x6d"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Main"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""18"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""28"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Line Directives

        [Fact]
        public void LineDirective()
        {
            var source = @"
#line 50 ""foo.cs""

using System;

unsafe class C
{
    static void Main()
    {
        Console.Write(1);
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""foo.cs"" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""56"" startColumn=""5"" endLine=""56"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""57"" startColumn=""9"" endLine=""57"" endColumn=""26"" document=""1"" />
        <entry offset=""0x8"" startLine=""58"" startColumn=""5"" endLine=""58"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(544917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544917")]
        [Fact]
        public void DisabledLineDirective()
        {
            var source = @"
#if false
#line 50 ""foo.cs""
#endif

using System;

unsafe class C
{
    static void Main()
    {
        Console.Write(1);
    }
}
";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""26"" document=""1"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestLineDirectivesHidden()
        {
            var text1 = WithWindowsLineBreaks(@"
using System;
public class C
{
    public void Foo()
    {
        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }

#line hidden
        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }
#line default

        foreach (var x in new int[] { 1, 2, 3, 4 })
        {
            Console.WriteLine(x);
        }
    }
}
");

            var compilation = CreateCompilation(text1, options: TestOptions.DebugDll);
            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Foo"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""6"" offset=""11"" />
          <slot kind=""8"" offset=""11"" />
          <slot kind=""0"" offset=""11"" />
          <slot kind=""6"" offset=""137"" />
          <slot kind=""8"" offset=""137"" />
          <slot kind=""0"" offset=""137"" />
          <slot kind=""6"" offset=""264"" />
          <slot kind=""8"" offset=""264"" />
          <slot kind=""0"" offset=""264"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""16"" document=""1"" />
        <entry offset=""0x2"" startLine=""7"" startColumn=""27"" endLine=""7"" endColumn=""51"" document=""1"" />
        <entry offset=""0x16"" hidden=""true"" document=""1"" />
        <entry offset=""0x18"" startLine=""7"" startColumn=""18"" endLine=""7"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1d"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""34"" document=""1"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x25"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" startLine=""7"" startColumn=""24"" endLine=""7"" endColumn=""26"" document=""1"" />
        <entry offset=""0x2f"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x45"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" hidden=""true"" document=""1"" />
        <entry offset=""0x4d"" hidden=""true"" document=""1"" />
        <entry offset=""0x4e"" hidden=""true"" document=""1"" />
        <entry offset=""0x56"" hidden=""true"" document=""1"" />
        <entry offset=""0x57"" hidden=""true"" document=""1"" />
        <entry offset=""0x5d"" hidden=""true"" document=""1"" />
        <entry offset=""0x64"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""16"" document=""1"" />
        <entry offset=""0x65"" startLine=""19"" startColumn=""27"" endLine=""19"" endColumn=""51"" document=""1"" />
        <entry offset=""0x7b"" hidden=""true"" document=""1"" />
        <entry offset=""0x7d"" startLine=""19"" startColumn=""18"" endLine=""19"" endColumn=""23"" document=""1"" />
        <entry offset=""0x84"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" document=""1"" />
        <entry offset=""0x85"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""34"" document=""1"" />
        <entry offset=""0x8d"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8e"" hidden=""true"" document=""1"" />
        <entry offset=""0x94"" startLine=""19"" startColumn=""24"" endLine=""19"" endColumn=""26"" document=""1"" />
        <entry offset=""0x9c"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x9d"">
        <namespace name=""System"" />
        <scope startOffset=""0x18"" endOffset=""0x25"">
          <local name=""x"" il_index=""2"" il_start=""0x18"" il_end=""0x25"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x47"" endOffset=""0x57"">
          <local name=""x"" il_index=""5"" il_start=""0x47"" il_end=""0x57"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x7d"" endOffset=""0x8e"">
          <local name=""x"" il_index=""8"" il_start=""0x7d"" il_end=""0x8e"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void HiddenMethods()
        {
            var src = WithWindowsLineBreaks(@"
using System;

class C
{
#line hidden
    public static void H()
    {
        F();
    }

#line default
    public static void G()
    {
        F();
    }

#line hidden
    public static void F()
    {
        {
            const int z = 1;
            var (x, y) = (1,2);
            Console.WriteLine(x + z);
        }
        {
            dynamic x = 1;
            Console.WriteLine(x);
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""13"" document=""1"" />
        <entry offset=""0x7"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""61"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""158"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void HiddenEntryPoint()
        {
            var src = @"
class C
{
#line hidden
    public static void Main()
    {
    }
}";
            var c = CreateCompilationWithMscorlib40AndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugExe);

            // Note: Dev10 emitted a hidden sequence point to #line hidden method, 
            // which enabled the debugger to locate the first user visible sequence point starting from the entry point.
            // Roslyn does not emit such sequence point. We could potentially synthesize one but that would defeat the purpose of 
            // #line hidden directive. 
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"" format=""windows"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
    </method>
  </methods>
</symbols>",
            // When converting from Portable to Windows the PDB writer doesn't create an entry for the Main method 
            // and thus there is no entry point record either.
            options: PdbValidationOptions.SkipConversionValidation);
        }

        [Fact]
        public void HiddenIterator()
        {
            var src = WithWindowsLineBreaks(@"
using System;
using System.Collections.Generic;

class C
{
    public static void Main()
    {
        F();
    }

#line hidden
    public static IEnumerable<int> F()
    {
        {
            const int z = 1;
            var (x, y) = (1,2);
            Console.WriteLine(x + z);
        }
        {
            dynamic x = 1;
            Console.WriteLine(x);
        }

        yield return 1;
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);

            // We don't really need the debug info for kickoff method when the entire iterator method is hidden, 
            // but it doesn't hurt and removing it would need extra effort that's unnecessary.
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""13"" document=""1"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forwardIterator name=""&lt;F&gt;d__1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""61"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""158"" />
        </encLocalSlotMap>
        <encStateMachineStateMap>
          <state number=""1"" offset=""222"" />
        </encStateMachineStateMap>
      </customDebugInfo>
    </method>
    <method containingType=""C+&lt;F&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Nested Types

        [Fact]
        public void NestedTypes()
        {
            string source = WithWindowsLineBreaks(@"
using System;

namespace N
{
	public class C
	{
		public class D<T>
		{
			public class E 
			{
				public static void f(int a) 
				{
					Console.WriteLine();
				}
			}
		}
	}
}
");
            var c = CreateCompilation(Parse(source, filename: "file.cs"));
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""file.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""F7-03-46-2C-11-16-DE-85-F9-DD-5C-76-F6-55-D9-13-E0-95-DE-14"" />
  </files>
  <methods>
    <method containingType=""N.C+D`1+E"" name=""f"" parameterNames=""a"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""6"" endLine=""14"" endColumn=""26"" document=""1"" />
        <entry offset=""0x5"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Expression Bodied Members

        [Fact]
        public void ExpressionBodiedProperty()
        {
            var source = WithWindowsLineBreaks(@"
class C
{
    public int P => M();
    public int M()
    {
        return 2;
    }
}");
            var comp = CreateCompilationWithMscorlib45(source);
            comp.VerifyDiagnostics();
            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""21"" endLine=""4"" endColumn=""24"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""18"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedIndexer()
        {
            var comp = CreateCompilationWithMscorlib45(@"
using System;

class C
{
    public int this[Int32 i] => M();
    public int M()
    {
        return 2;
    }
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_Item"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""33"" endLine=""6"" endColumn=""36"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_Item"" parameterNames=""i"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedMethod()
        {
            var comp = CreateCompilationWithMscorlib45(@"
using System;

class C
{
    public Int32 P => 2;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""23"" endLine=""6"" endColumn=""24"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedOperator()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public static C operator ++(C c) => c;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""op_Increment"" parameterNames=""c"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""41"" endLine=""4"" endColumn=""42"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ExpressionBodiedConversion()
        {
            var comp = CreateCompilationWithMscorlib45(@"
using System;

class C
{
    public static explicit operator C(Int32 i) => new C();
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""op_Explicit"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""51"" endLine=""6"" endColumn=""58"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        [Fact]
        public void ExpressionBodiedConstructor()
        {
            var comp = CreateCompilationWithMscorlib45(@"
using System;

class C
{
    public int X;
    public C(Int32 x) => X = x;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""22"" document=""1"" />
        <entry offset=""0x6"" startLine=""7"" startColumn=""26"" endLine=""7"" endColumn=""31"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        [Fact]
        public void ExpressionBodiedDestructor()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int X;
    ~C() => X = 0;
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Finalize"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""13"" endLine=""5"" endColumn=""18"" document=""1"" />
        <entry offset=""0x9"" hidden=""true"" document=""1"" />
        <entry offset=""0x10"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        [Fact]
        public void ExpressionBodiedAccessor()
        {
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int x;
    public int X
    {
        get => x;
        set => x = value;
    }
    public event System.Action E
    {
        add => x = 1;
        remove => x = 0;
    }
}");
            comp.VerifyDiagnostics();

            comp.VerifyPdb(@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""get_X"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""16"" endLine=""7"" endColumn=""17"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_X"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""25"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""add_E"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""remove_E"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""19"" endLine=""13"" endColumn=""24"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Synthesized Methods

        [Fact]
        public void ImportsInLambda()
        {
            var source = WithWindowsLineBreaks(
@"using System.Collections.Generic;
using System.Linq;
class C
{
    static void M()
    {
        System.Action f = () =>
        {
            var c = new[] { 1, 2, 3 };
            c.Select(i => i);
        };
        f();
    }
}");
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<>c.<M>b__0_0",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""63"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""39"" document=""1"" />
        <entry offset=""0x13"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""30"" document=""1"" />
        <entry offset=""0x39"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x3a"">
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x3a"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ImportsInIterator()
        {
            var source = WithWindowsLineBreaks(
@"using System.Collections.Generic;
using System.Linq;
class C
{
    static IEnumerable<object> F()
    {
        var c = new[] { 1, 2, 3 };
        foreach (var i in c.Select(i => i))
        {
            yield return i;
        }
    }
}");
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<F>d__0.MoveNext",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c"" methodName=""&lt;F&gt;b__0_0"" parameterNames=""i"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x27"" endOffset=""0xd5"" />
          <slot />
          <slot startOffset=""0x7f"" endOffset=""0xb6"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""temp"" />
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x27"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x28"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""35"" document=""1"" />
        <entry offset=""0x3f"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" document=""1"" />
        <entry offset=""0x40"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""43"" document=""1"" />
        <entry offset=""0x7d"" hidden=""true"" document=""1"" />
        <entry offset=""0x7f"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" document=""1"" />
        <entry offset=""0x90"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x91"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""28"" document=""1"" />
        <entry offset=""0xad"" hidden=""true"" document=""1"" />
        <entry offset=""0xb5"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb6"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0xd1"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
        <entry offset=""0xd5"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ImportsInAsync()
        {
            var source = WithWindowsLineBreaks(
@"using System.Linq;
using System.Threading.Tasks;
class C
{
    static async Task F()
    {
        var c = new[] { 1, 2, 3 };
        c.Select(i => i);
    }
}");
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<F>d__0.MoveNext",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c"" methodName=""&lt;F&gt;b__0_0"" parameterNames=""i"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x87"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""35"" document=""1"" />
        <entry offset=""0x1f"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""26"" document=""1"" />
        <entry offset=""0x4c"" hidden=""true"" document=""1"" />
        <entry offset=""0x6b"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x73"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""F"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(2501, "https://github.com/dotnet/roslyn/issues/2501")]
        [Fact]
        public void ImportsInAsyncLambda()
        {
            var source = WithWindowsLineBreaks(
@"using System.Linq;
class C
{
    static void M()
    {
        System.Action f = async () =>
        {
            var c = new[] { 1, 2, 3 };
            c.Select(i => i);
        };
    }
}");
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<>c.<M>b__0_0",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_0"">
      <customDebugInfo>
        <forwardIterator name=""&lt;&lt;M&gt;b__0_0&gt;d"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""69"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>");
            c.VerifyPdb("C+<>c+<<M>b__0_0>d.MoveNext",
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;&gt;c+&lt;&lt;M&gt;b__0_0&gt;d"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x87"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""50"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""39"" document=""1"" />
        <entry offset=""0x1f"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x4c"" hidden=""true"" document=""1"" />
        <entry offset=""0x6b"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x73"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x4c"" />
        <kickoffMethod declaringType=""C+&lt;&gt;c"" methodName=""&lt;M&gt;b__0_0"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Patterns

        [Fact]
        public void SyntaxOffset_IsPattern()
        {
            var source = @"class C { bool F(object o) => o is int i && o is 3 && o is bool; }";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""12"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void Patterns_SwitchStatement()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    public void Deconstruct() { }
    public void Deconstruct(out int x) { x = 1; }
    public void Deconstruct(out int x, out object y) { x = 2; y = new C(); }
}

class D
{
    public int P { get; set; }
    public D Q { get; set; }
    public C R { get; set; }
}

class Program
{
    static object F() => new C();
    static bool B() => true;
    static int G(int x) => x;

    static int Main()
    {
        switch (F())
        {
            // declaration pattern
            case int x when G(x) > 10: return 1;

            // discard pattern
            case bool _: return 2;

            // var pattern
            case var (y, z): return 3;

            // constant pattern
            case 4.0: return 4;

            // positional patterns
            case C() when B(): return 5;
            case (): return 6;
            case C(int p, C(int q)): return 7;
            case C(x: int p): return 8;

            // property pattern
            case D { P: 1, Q: D { P: 2 }, R: C(int z) }: return 9;

            default: return 10;
        };
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerify(c, verify: Verification.Skipped);

            verifier.VerifyIL("Program.Main", sequencePoints: "Program.Main", expectedIL: @"
{
  // Code size      448 (0x1c0)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //x
                object V_2, //y
                object V_3, //z
                int V_4, //p
                int V_5, //q
                int V_6, //p
                int V_7, //z
                object V_8,
                System.Runtime.CompilerServices.ITuple V_9,
                int V_10,
                double V_11,
                C V_12,
                object V_13,
                C V_14,
                D V_15,
                int V_16,
                D V_17,
                int V_18,
                C V_19,
                object V_20,
                int V_21)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (F())
  IL_0001:  call       ""object Program.F()""
  IL_0006:  stloc.s    V_20
  // sequence point: <hidden>
  IL_0008:  ldloc.s    V_20
  IL_000a:  stloc.s    V_8
  // sequence point: <hidden>
  IL_000c:  ldloc.s    V_8
  IL_000e:  isinst     ""int""
  IL_0013:  brfalse.s  IL_0022
  IL_0015:  ldloc.s    V_8
  IL_0017:  unbox.any  ""int""
  IL_001c:  stloc.1
  // sequence point: <hidden>
  IL_001d:  br         IL_0150
  IL_0022:  ldloc.s    V_8
  IL_0024:  isinst     ""bool""
  IL_0029:  brtrue     IL_0161
  IL_002e:  ldloc.s    V_8
  IL_0030:  isinst     ""System.Runtime.CompilerServices.ITuple""
  IL_0035:  stloc.s    V_9
  IL_0037:  ldloc.s    V_9
  IL_0039:  brfalse.s  IL_0080
  IL_003b:  ldloc.s    V_9
  IL_003d:  callvirt   ""int System.Runtime.CompilerServices.ITuple.Length.get""
  IL_0042:  stloc.s    V_10
  // sequence point: <hidden>
  IL_0044:  ldloc.s    V_10
  IL_0046:  ldc.i4.2
  IL_0047:  bne.un.s   IL_0060
  IL_0049:  ldloc.s    V_9
  IL_004b:  ldc.i4.0
  IL_004c:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_0051:  stloc.2
  // sequence point: <hidden>
  IL_0052:  ldloc.s    V_9
  IL_0054:  ldc.i4.1
  IL_0055:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_005a:  stloc.3
  // sequence point: <hidden>
  IL_005b:  br         IL_0166
  IL_0060:  ldloc.s    V_8
  IL_0062:  isinst     ""C""
  IL_0067:  brtrue     IL_0172
  IL_006c:  br.s       IL_0077
  IL_006e:  ldloc.s    V_10
  IL_0070:  brfalse    IL_019c
  IL_0075:  br.s       IL_00b5
  IL_0077:  ldloc.s    V_10
  IL_0079:  brfalse    IL_019c
  IL_007e:  br.s       IL_00f5
  IL_0080:  ldloc.s    V_8
  IL_0082:  isinst     ""double""
  IL_0087:  brfalse.s  IL_00a7
  IL_0089:  ldloc.s    V_8
  IL_008b:  unbox.any  ""double""
  IL_0090:  stloc.s    V_11
  // sequence point: <hidden>
  IL_0092:  ldloc.s    V_11
  IL_0094:  ldc.r8     4
  IL_009d:  beq        IL_016d
  IL_00a2:  br         IL_01b7
  IL_00a7:  ldloc.s    V_8
  IL_00a9:  isinst     ""C""
  IL_00ae:  brtrue     IL_0176
  IL_00b3:  br.s       IL_00f5
  IL_00b5:  ldloc.s    V_8
  IL_00b7:  castclass  ""C""
  IL_00bc:  stloc.s    V_12
  // sequence point: <hidden>
  IL_00be:  ldloc.s    V_12
  IL_00c0:  ldloca.s   V_4
  IL_00c2:  ldloca.s   V_13
  IL_00c4:  callvirt   ""void C.Deconstruct(out int, out object)""
  IL_00c9:  nop
  // sequence point: <hidden>
  IL_00ca:  ldloc.s    V_13
  IL_00cc:  isinst     ""C""
  IL_00d1:  stloc.s    V_14
  IL_00d3:  ldloc.s    V_14
  IL_00d5:  brfalse.s  IL_00e6
  IL_00d7:  ldloc.s    V_14
  IL_00d9:  ldloca.s   V_5
  IL_00db:  callvirt   ""void C.Deconstruct(out int)""
  IL_00e0:  nop
  // sequence point: <hidden>
  IL_00e1:  br         IL_01a1
  IL_00e6:  ldloc.s    V_12
  IL_00e8:  ldloca.s   V_6
  IL_00ea:  callvirt   ""void C.Deconstruct(out int)""
  IL_00ef:  nop
  // sequence point: <hidden>
  IL_00f0:  br         IL_01a8
  IL_00f5:  ldloc.s    V_8
  IL_00f7:  isinst     ""D""
  IL_00fc:  stloc.s    V_15
  IL_00fe:  ldloc.s    V_15
  IL_0100:  brfalse    IL_01b7
  IL_0105:  ldloc.s    V_15
  IL_0107:  callvirt   ""int D.P.get""
  IL_010c:  stloc.s    V_16
  // sequence point: <hidden>
  IL_010e:  ldloc.s    V_16
  IL_0110:  ldc.i4.1
  IL_0111:  bne.un     IL_01b7
  IL_0116:  ldloc.s    V_15
  IL_0118:  callvirt   ""D D.Q.get""
  IL_011d:  stloc.s    V_17
  // sequence point: <hidden>
  IL_011f:  ldloc.s    V_17
  IL_0121:  brfalse    IL_01b7
  IL_0126:  ldloc.s    V_17
  IL_0128:  callvirt   ""int D.P.get""
  IL_012d:  stloc.s    V_18
  // sequence point: <hidden>
  IL_012f:  ldloc.s    V_18
  IL_0131:  ldc.i4.2
  IL_0132:  bne.un     IL_01b7
  IL_0137:  ldloc.s    V_15
  IL_0139:  callvirt   ""C D.R.get""
  IL_013e:  stloc.s    V_19
  // sequence point: <hidden>
  IL_0140:  ldloc.s    V_19
  IL_0142:  brfalse.s  IL_01b7
  IL_0144:  ldloc.s    V_19
  IL_0146:  ldloca.s   V_7
  IL_0148:  callvirt   ""void C.Deconstruct(out int)""
  IL_014d:  nop
  // sequence point: <hidden>
  IL_014e:  br.s       IL_01af
  // sequence point: when G(x) > 10
  IL_0150:  ldloc.1
  IL_0151:  call       ""int Program.G(int)""
  IL_0156:  ldc.i4.s   10
  IL_0158:  bgt.s      IL_015c
  // sequence point: <hidden>
  IL_015a:  br.s       IL_01b7
  // sequence point: return 1;
  IL_015c:  ldc.i4.1
  IL_015d:  stloc.s    V_21
  IL_015f:  br.s       IL_01bd
  // sequence point: return 2;
  IL_0161:  ldc.i4.2
  IL_0162:  stloc.s    V_21
  IL_0164:  br.s       IL_01bd
  // sequence point: <hidden>
  IL_0166:  br.s       IL_0168
  // sequence point: return 3;
  IL_0168:  ldc.i4.3
  IL_0169:  stloc.s    V_21
  IL_016b:  br.s       IL_01bd
  // sequence point: return 4;
  IL_016d:  ldc.i4.4
  IL_016e:  stloc.s    V_21
  IL_0170:  br.s       IL_01bd
  // sequence point: <hidden>
  IL_0172:  ldc.i4.1
  IL_0173:  stloc.0
  IL_0174:  br.s       IL_017a
  IL_0176:  ldc.i4.2
  IL_0177:  stloc.0
  IL_0178:  br.s       IL_017a
  // sequence point: when B()
  IL_017a:  call       ""bool Program.B()""
  IL_017f:  brtrue.s   IL_0197
  // sequence point: <hidden>
  IL_0181:  ldloc.0
  IL_0182:  ldc.i4.1
  IL_0183:  beq.s      IL_018d
  IL_0185:  br.s       IL_0187
  IL_0187:  ldloc.0
  IL_0188:  ldc.i4.2
  IL_0189:  beq.s      IL_0192
  IL_018b:  br.s       IL_0197
  IL_018d:  br         IL_006e
  IL_0192:  br         IL_00b5
  // sequence point: return 5;
  IL_0197:  ldc.i4.5
  IL_0198:  stloc.s    V_21
  IL_019a:  br.s       IL_01bd
  // sequence point: return 6;
  IL_019c:  ldc.i4.6
  IL_019d:  stloc.s    V_21
  IL_019f:  br.s       IL_01bd
  // sequence point: <hidden>
  IL_01a1:  br.s       IL_01a3
  // sequence point: return 7;
  IL_01a3:  ldc.i4.7
  IL_01a4:  stloc.s    V_21
  IL_01a6:  br.s       IL_01bd
  // sequence point: <hidden>
  IL_01a8:  br.s       IL_01aa
  // sequence point: return 8;
  IL_01aa:  ldc.i4.8
  IL_01ab:  stloc.s    V_21
  IL_01ad:  br.s       IL_01bd
  // sequence point: <hidden>
  IL_01af:  br.s       IL_01b1
  // sequence point: return 9;
  IL_01b1:  ldc.i4.s   9
  IL_01b3:  stloc.s    V_21
  IL_01b5:  br.s       IL_01bd
  // sequence point: return 10;
  IL_01b7:  ldc.i4.s   10
  IL_01b9:  stloc.s    V_21
  IL_01bb:  br.s       IL_01bd
  // sequence point: }
  IL_01bd:  ldloc.s    V_21
  IL_01bf:  ret
}
", source: source);

            verifier.VerifyPdb("Program.Main", @"   
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Deconstruct"" />
        <encLocalSlotMap>
          <slot kind=""temp"" />
          <slot kind=""0"" offset=""93"" />
          <slot kind=""0"" offset=""244"" />
          <slot kind=""0"" offset=""247"" />
          <slot kind=""0"" offset=""465"" />
          <slot kind=""0"" offset=""474"" />
          <slot kind=""0"" offset=""516"" />
          <slot kind=""0"" offset=""617"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" ordinal=""1"" />
          <slot kind=""35"" offset=""11"" ordinal=""2"" />
          <slot kind=""35"" offset=""11"" ordinal=""3"" />
          <slot kind=""35"" offset=""11"" ordinal=""4"" />
          <slot kind=""35"" offset=""11"" ordinal=""5"" />
          <slot kind=""35"" offset=""11"" ordinal=""6"" />
          <slot kind=""35"" offset=""11"" ordinal=""7"" />
          <slot kind=""35"" offset=""11"" ordinal=""8"" />
          <slot kind=""35"" offset=""11"" ordinal=""9"" />
          <slot kind=""35"" offset=""11"" ordinal=""10"" />
          <slot kind=""35"" offset=""11"" ordinal=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""21"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0xc"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x44"" hidden=""true"" document=""1"" />
        <entry offset=""0x52"" hidden=""true"" document=""1"" />
        <entry offset=""0x5b"" hidden=""true"" document=""1"" />
        <entry offset=""0x92"" hidden=""true"" document=""1"" />
        <entry offset=""0xbe"" hidden=""true"" document=""1"" />
        <entry offset=""0xca"" hidden=""true"" document=""1"" />
        <entry offset=""0xe1"" hidden=""true"" document=""1"" />
        <entry offset=""0xf0"" hidden=""true"" document=""1"" />
        <entry offset=""0x10e"" hidden=""true"" document=""1"" />
        <entry offset=""0x11f"" hidden=""true"" document=""1"" />
        <entry offset=""0x12f"" hidden=""true"" document=""1"" />
        <entry offset=""0x140"" hidden=""true"" document=""1"" />
        <entry offset=""0x14e"" hidden=""true"" document=""1"" />
        <entry offset=""0x150"" startLine=""27"" startColumn=""24"" endLine=""27"" endColumn=""38"" document=""1"" />
        <entry offset=""0x15a"" hidden=""true"" document=""1"" />
        <entry offset=""0x15c"" startLine=""27"" startColumn=""40"" endLine=""27"" endColumn=""49"" document=""1"" />
        <entry offset=""0x161"" startLine=""30"" startColumn=""26"" endLine=""30"" endColumn=""35"" document=""1"" />
        <entry offset=""0x166"" hidden=""true"" document=""1"" />
        <entry offset=""0x168"" startLine=""33"" startColumn=""30"" endLine=""33"" endColumn=""39"" document=""1"" />
        <entry offset=""0x16d"" startLine=""36"" startColumn=""23"" endLine=""36"" endColumn=""32"" document=""1"" />
        <entry offset=""0x172"" hidden=""true"" document=""1"" />
        <entry offset=""0x17a"" startLine=""39"" startColumn=""22"" endLine=""39"" endColumn=""30"" document=""1"" />
        <entry offset=""0x181"" hidden=""true"" document=""1"" />
        <entry offset=""0x197"" startLine=""39"" startColumn=""32"" endLine=""39"" endColumn=""41"" document=""1"" />
        <entry offset=""0x19c"" startLine=""40"" startColumn=""22"" endLine=""40"" endColumn=""31"" document=""1"" />
        <entry offset=""0x1a1"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a3"" startLine=""41"" startColumn=""38"" endLine=""41"" endColumn=""47"" document=""1"" />
        <entry offset=""0x1a8"" hidden=""true"" document=""1"" />
        <entry offset=""0x1aa"" startLine=""42"" startColumn=""31"" endLine=""42"" endColumn=""40"" document=""1"" />
        <entry offset=""0x1af"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b1"" startLine=""45"" startColumn=""58"" endLine=""45"" endColumn=""67"" document=""1"" />
        <entry offset=""0x1b7"" startLine=""47"" startColumn=""22"" endLine=""47"" endColumn=""32"" document=""1"" />
        <entry offset=""0x1bd"" startLine=""49"" startColumn=""5"" endLine=""49"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c0"">
        <scope startOffset=""0x150"" endOffset=""0x161"">
          <local name=""x"" il_index=""1"" il_start=""0x150"" il_end=""0x161"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x166"" endOffset=""0x16d"">
          <local name=""y"" il_index=""2"" il_start=""0x166"" il_end=""0x16d"" attributes=""0"" />
          <local name=""z"" il_index=""3"" il_start=""0x166"" il_end=""0x16d"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1a1"" endOffset=""0x1a8"">
          <local name=""p"" il_index=""4"" il_start=""0x1a1"" il_end=""0x1a8"" attributes=""0"" />
          <local name=""q"" il_index=""5"" il_start=""0x1a1"" il_end=""0x1a8"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1a8"" endOffset=""0x1af"">
          <local name=""p"" il_index=""6"" il_start=""0x1a8"" il_end=""0x1af"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1af"" endOffset=""0x1b7"">
          <local name=""z"" il_index=""7"" il_start=""0x1af"" il_end=""0x1b7"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void Patterns_SwitchExpression()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    public void Deconstruct() { }
    public void Deconstruct(out int x) { x = 1; }
    public void Deconstruct(out int x, out object y) { x = 2; y = new C(); }
}

class D
{
    public int P { get; set; }
    public D Q { get; set; }
    public C R { get; set; }
}

class Program
{
    static object F() => new C();
    static bool B() => true;
    static int G(int x) => x;

    static void Main()
    {
        var a = F() switch
        {
            // declaration pattern
            int x when G(x) > 10 => 1,

            // discard pattern
            bool _ => 2,

            // var pattern
            var (y, z) => 3,

            // constant pattern
            4.0 => 4,

            // positional patterns
            C() when B() => 5,
            () => 6,
            C(int p, C(int q)) => 7,
            C(x: int p) => 8,

            // property pattern
            D { P: 1, Q: D { P: 2 }, R: C (int z) } => 9,

            _ => 10,
        };
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerify(c, verify: Verification.Skipped);

            // note no sequence points emitted within the switch expression

            verifier.VerifyIL("Program.Main", sequencePoints: "Program.Main", expectedIL: @"
{
  // Code size      454 (0x1c6)
  .maxstack  3
  .locals init (int V_0, //a
                int V_1,
                int V_2, //x
                object V_3, //y
                object V_4, //z
                int V_5, //p
                int V_6, //q
                int V_7, //p
                int V_8, //z
                int V_9,
                object V_10,
                System.Runtime.CompilerServices.ITuple V_11,
                int V_12,
                double V_13,
                C V_14,
                object V_15,
                C V_16,
                D V_17,
                int V_18,
                D V_19,
                int V_20,
                C V_21)
 -IL_0000:  nop
 -IL_0001:  call       ""object Program.F()""
  IL_0006:  stloc.s    V_10
  IL_0008:  ldc.i4.1
  IL_0009:  brtrue.s   IL_000c
 -IL_000b:  nop
 ~IL_000c:  ldloc.s    V_10
  IL_000e:  isinst     ""int""
  IL_0013:  brfalse.s  IL_0022
  IL_0015:  ldloc.s    V_10
  IL_0017:  unbox.any  ""int""
  IL_001c:  stloc.2
 ~IL_001d:  br         IL_0151
  IL_0022:  ldloc.s    V_10
  IL_0024:  isinst     ""bool""
  IL_0029:  brtrue     IL_0162
  IL_002e:  ldloc.s    V_10
  IL_0030:  isinst     ""System.Runtime.CompilerServices.ITuple""
  IL_0035:  stloc.s    V_11
  IL_0037:  ldloc.s    V_11
  IL_0039:  brfalse.s  IL_0081
  IL_003b:  ldloc.s    V_11
  IL_003d:  callvirt   ""int System.Runtime.CompilerServices.ITuple.Length.get""
  IL_0042:  stloc.s    V_12
 ~IL_0044:  ldloc.s    V_12
  IL_0046:  ldc.i4.2
  IL_0047:  bne.un.s   IL_0061
  IL_0049:  ldloc.s    V_11
  IL_004b:  ldc.i4.0
  IL_004c:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_0051:  stloc.3
 ~IL_0052:  ldloc.s    V_11
  IL_0054:  ldc.i4.1
  IL_0055:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_005a:  stloc.s    V_4
 ~IL_005c:  br         IL_0167
  IL_0061:  ldloc.s    V_10
  IL_0063:  isinst     ""C""
  IL_0068:  brtrue     IL_0173
  IL_006d:  br.s       IL_0078
  IL_006f:  ldloc.s    V_12
  IL_0071:  brfalse    IL_019d
  IL_0076:  br.s       IL_00b6
  IL_0078:  ldloc.s    V_12
  IL_007a:  brfalse    IL_019d
  IL_007f:  br.s       IL_00f6
  IL_0081:  ldloc.s    V_10
  IL_0083:  isinst     ""double""
  IL_0088:  brfalse.s  IL_00a8
  IL_008a:  ldloc.s    V_10
  IL_008c:  unbox.any  ""double""
  IL_0091:  stloc.s    V_13
 ~IL_0093:  ldloc.s    V_13
  IL_0095:  ldc.r8     4
  IL_009e:  beq        IL_016e
  IL_00a3:  br         IL_01b8
  IL_00a8:  ldloc.s    V_10
  IL_00aa:  isinst     ""C""
  IL_00af:  brtrue     IL_0177
  IL_00b4:  br.s       IL_00f6
  IL_00b6:  ldloc.s    V_10
  IL_00b8:  castclass  ""C""
  IL_00bd:  stloc.s    V_14
 ~IL_00bf:  ldloc.s    V_14
  IL_00c1:  ldloca.s   V_5
  IL_00c3:  ldloca.s   V_15
  IL_00c5:  callvirt   ""void C.Deconstruct(out int, out object)""
  IL_00ca:  nop
 ~IL_00cb:  ldloc.s    V_15
  IL_00cd:  isinst     ""C""
  IL_00d2:  stloc.s    V_16
  IL_00d4:  ldloc.s    V_16
  IL_00d6:  brfalse.s  IL_00e7
  IL_00d8:  ldloc.s    V_16
  IL_00da:  ldloca.s   V_6
  IL_00dc:  callvirt   ""void C.Deconstruct(out int)""
  IL_00e1:  nop
 ~IL_00e2:  br         IL_01a2
  IL_00e7:  ldloc.s    V_14
  IL_00e9:  ldloca.s   V_7
  IL_00eb:  callvirt   ""void C.Deconstruct(out int)""
  IL_00f0:  nop
 ~IL_00f1:  br         IL_01a9
  IL_00f6:  ldloc.s    V_10
  IL_00f8:  isinst     ""D""
  IL_00fd:  stloc.s    V_17
  IL_00ff:  ldloc.s    V_17
  IL_0101:  brfalse    IL_01b8
  IL_0106:  ldloc.s    V_17
  IL_0108:  callvirt   ""int D.P.get""
  IL_010d:  stloc.s    V_18
 ~IL_010f:  ldloc.s    V_18
  IL_0111:  ldc.i4.1
  IL_0112:  bne.un     IL_01b8
  IL_0117:  ldloc.s    V_17
  IL_0119:  callvirt   ""D D.Q.get""
  IL_011e:  stloc.s    V_19
 ~IL_0120:  ldloc.s    V_19
  IL_0122:  brfalse    IL_01b8
  IL_0127:  ldloc.s    V_19
  IL_0129:  callvirt   ""int D.P.get""
  IL_012e:  stloc.s    V_20
 ~IL_0130:  ldloc.s    V_20
  IL_0132:  ldc.i4.2
  IL_0133:  bne.un     IL_01b8
  IL_0138:  ldloc.s    V_17
  IL_013a:  callvirt   ""C D.R.get""
  IL_013f:  stloc.s    V_21
 ~IL_0141:  ldloc.s    V_21
  IL_0143:  brfalse.s  IL_01b8
  IL_0145:  ldloc.s    V_21
  IL_0147:  ldloca.s   V_8
  IL_0149:  callvirt   ""void C.Deconstruct(out int)""
  IL_014e:  nop
 ~IL_014f:  br.s       IL_01b0
 -IL_0151:  ldloc.2
  IL_0152:  call       ""int Program.G(int)""
  IL_0157:  ldc.i4.s   10
  IL_0159:  bgt.s      IL_015d
 ~IL_015b:  br.s       IL_01b8
 -IL_015d:  ldc.i4.1
  IL_015e:  stloc.s    V_9
  IL_0160:  br.s       IL_01be
 -IL_0162:  ldc.i4.2
  IL_0163:  stloc.s    V_9
  IL_0165:  br.s       IL_01be
 ~IL_0167:  br.s       IL_0169
 -IL_0169:  ldc.i4.3
  IL_016a:  stloc.s    V_9
  IL_016c:  br.s       IL_01be
 -IL_016e:  ldc.i4.4
  IL_016f:  stloc.s    V_9
  IL_0171:  br.s       IL_01be
 ~IL_0173:  ldc.i4.1
  IL_0174:  stloc.1
  IL_0175:  br.s       IL_017b
  IL_0177:  ldc.i4.2
  IL_0178:  stloc.1
  IL_0179:  br.s       IL_017b
 -IL_017b:  call       ""bool Program.B()""
  IL_0180:  brtrue.s   IL_0198
 ~IL_0182:  ldloc.1
  IL_0183:  ldc.i4.1
  IL_0184:  beq.s      IL_018e
  IL_0186:  br.s       IL_0188
  IL_0188:  ldloc.1
  IL_0189:  ldc.i4.2
  IL_018a:  beq.s      IL_0193
  IL_018c:  br.s       IL_0198
  IL_018e:  br         IL_006f
  IL_0193:  br         IL_00b6
 -IL_0198:  ldc.i4.5
  IL_0199:  stloc.s    V_9
  IL_019b:  br.s       IL_01be
 -IL_019d:  ldc.i4.6
  IL_019e:  stloc.s    V_9
  IL_01a0:  br.s       IL_01be
 ~IL_01a2:  br.s       IL_01a4
 -IL_01a4:  ldc.i4.7
  IL_01a5:  stloc.s    V_9
  IL_01a7:  br.s       IL_01be
 ~IL_01a9:  br.s       IL_01ab
 -IL_01ab:  ldc.i4.8
  IL_01ac:  stloc.s    V_9
  IL_01ae:  br.s       IL_01be
 ~IL_01b0:  br.s       IL_01b2
 -IL_01b2:  ldc.i4.s   9
  IL_01b4:  stloc.s    V_9
  IL_01b6:  br.s       IL_01be
 -IL_01b8:  ldc.i4.s   10
  IL_01ba:  stloc.s    V_9
  IL_01bc:  br.s       IL_01be
 ~IL_01be:  ldc.i4.1
  IL_01bf:  brtrue.s   IL_01c2
 -IL_01c1:  nop
 ~IL_01c2:  ldloc.s    V_9
  IL_01c4:  stloc.0
 -IL_01c5:  ret
}
");

            verifier.VerifyPdb("Program.Main", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Deconstruct"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""temp"" />
          <slot kind=""0"" offset=""94"" />
          <slot kind=""0"" offset=""225"" />
          <slot kind=""0"" offset=""228"" />
          <slot kind=""0"" offset=""406"" />
          <slot kind=""0"" offset=""415"" />
          <slot kind=""0"" offset=""447"" />
          <slot kind=""0"" offset=""539"" />
          <slot kind=""temp"" />
          <slot kind=""35"" offset=""23"" />
          <slot kind=""35"" offset=""23"" ordinal=""1"" />
          <slot kind=""35"" offset=""23"" ordinal=""2"" />
          <slot kind=""35"" offset=""23"" ordinal=""3"" />
          <slot kind=""35"" offset=""23"" ordinal=""4"" />
          <slot kind=""35"" offset=""23"" ordinal=""5"" />
          <slot kind=""35"" offset=""23"" ordinal=""6"" />
          <slot kind=""35"" offset=""23"" ordinal=""7"" />
          <slot kind=""35"" offset=""23"" ordinal=""8"" />
          <slot kind=""35"" offset=""23"" ordinal=""9"" />
          <slot kind=""35"" offset=""23"" ordinal=""10"" />
          <slot kind=""35"" offset=""23"" ordinal=""11"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""48"" endColumn=""11"" document=""1"" />
        <entry offset=""0xb"" startLine=""24"" startColumn=""21"" endLine=""48"" endColumn=""10"" document=""1"" />
        <entry offset=""0xc"" hidden=""true"" document=""1"" />
        <entry offset=""0x1d"" hidden=""true"" document=""1"" />
        <entry offset=""0x44"" hidden=""true"" document=""1"" />
        <entry offset=""0x52"" hidden=""true"" document=""1"" />
        <entry offset=""0x5c"" hidden=""true"" document=""1"" />
        <entry offset=""0x93"" hidden=""true"" document=""1"" />
        <entry offset=""0xbf"" hidden=""true"" document=""1"" />
        <entry offset=""0xcb"" hidden=""true"" document=""1"" />
        <entry offset=""0xe2"" hidden=""true"" document=""1"" />
        <entry offset=""0xf1"" hidden=""true"" document=""1"" />
        <entry offset=""0x10f"" hidden=""true"" document=""1"" />
        <entry offset=""0x120"" hidden=""true"" document=""1"" />
        <entry offset=""0x130"" hidden=""true"" document=""1"" />
        <entry offset=""0x141"" hidden=""true"" document=""1"" />
        <entry offset=""0x14f"" hidden=""true"" document=""1"" />
        <entry offset=""0x151"" startLine=""27"" startColumn=""19"" endLine=""27"" endColumn=""33"" document=""1"" />
        <entry offset=""0x15b"" hidden=""true"" document=""1"" />
        <entry offset=""0x15d"" startLine=""27"" startColumn=""37"" endLine=""27"" endColumn=""38"" document=""1"" />
        <entry offset=""0x162"" startLine=""30"" startColumn=""23"" endLine=""30"" endColumn=""24"" document=""1"" />
        <entry offset=""0x167"" hidden=""true"" document=""1"" />
        <entry offset=""0x169"" startLine=""33"" startColumn=""27"" endLine=""33"" endColumn=""28"" document=""1"" />
        <entry offset=""0x16e"" startLine=""36"" startColumn=""20"" endLine=""36"" endColumn=""21"" document=""1"" />
        <entry offset=""0x173"" hidden=""true"" document=""1"" />
        <entry offset=""0x17b"" startLine=""39"" startColumn=""17"" endLine=""39"" endColumn=""25"" document=""1"" />
        <entry offset=""0x182"" hidden=""true"" document=""1"" />
        <entry offset=""0x198"" startLine=""39"" startColumn=""29"" endLine=""39"" endColumn=""30"" document=""1"" />
        <entry offset=""0x19d"" startLine=""40"" startColumn=""19"" endLine=""40"" endColumn=""20"" document=""1"" />
        <entry offset=""0x1a2"" hidden=""true"" document=""1"" />
        <entry offset=""0x1a4"" startLine=""41"" startColumn=""35"" endLine=""41"" endColumn=""36"" document=""1"" />
        <entry offset=""0x1a9"" hidden=""true"" document=""1"" />
        <entry offset=""0x1ab"" startLine=""42"" startColumn=""28"" endLine=""42"" endColumn=""29"" document=""1"" />
        <entry offset=""0x1b0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b2"" startLine=""45"" startColumn=""56"" endLine=""45"" endColumn=""57"" document=""1"" />
        <entry offset=""0x1b8"" startLine=""47"" startColumn=""18"" endLine=""47"" endColumn=""20"" document=""1"" />
        <entry offset=""0x1be"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c1"" startLine=""24"" startColumn=""9"" endLine=""48"" endColumn=""11"" document=""1"" />
        <entry offset=""0x1c2"" hidden=""true"" document=""1"" />
        <entry offset=""0x1c5"" startLine=""49"" startColumn=""5"" endLine=""49"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c6"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x1c6"" attributes=""0"" />
        <scope startOffset=""0x151"" endOffset=""0x162"">
          <local name=""x"" il_index=""2"" il_start=""0x151"" il_end=""0x162"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x167"" endOffset=""0x16e"">
          <local name=""y"" il_index=""3"" il_start=""0x167"" il_end=""0x16e"" attributes=""0"" />
          <local name=""z"" il_index=""4"" il_start=""0x167"" il_end=""0x16e"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1a2"" endOffset=""0x1a9"">
          <local name=""p"" il_index=""5"" il_start=""0x1a2"" il_end=""0x1a9"" attributes=""0"" />
          <local name=""q"" il_index=""6"" il_start=""0x1a2"" il_end=""0x1a9"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1a9"" endOffset=""0x1b0"">
          <local name=""p"" il_index=""7"" il_start=""0x1a9"" il_end=""0x1b0"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x1b0"" endOffset=""0x1b8"">
          <local name=""z"" il_index=""8"" il_start=""0x1b0"" il_end=""0x1b8"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void Patterns_IsPattern()
        {
            string source = WithWindowsLineBreaks(@"
class C
{
    public void Deconstruct() { }
    public void Deconstruct(out int x) { x = 1; }
    public void Deconstruct(out int x, out object y) { x = 2; y = new C(); }
}

class D
{
    public int P { get; set; }
    public D Q { get; set; }
    public C R { get; set; }
}

class Program
{
    static object F() => new C();
    static bool B() => true;
    static int G(int x) => x;

    static bool M()
    {
        object obj = F();
        return 
            // declaration pattern
            obj is int x ||

            // discard pattern
            obj is bool _ ||

            // var pattern
            obj is var (y, z1) ||

            // constant pattern
            obj is 4.0 ||

            // positional patterns
            obj is C() ||
            obj is () ||
            obj is C(int p1, C(int q)) ||
            obj is C(x: int p2) ||

            // property pattern
            obj is D { P: 1, Q: D { P: 2 }, R: C(int z2) };
    }
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll, targetFramework: TargetFramework.NetCoreApp);
            var verifier = CompileAndVerify(c, verify: Verification.Skipped);

            verifier.VerifyIL("Program.M", sequencePoints: "Program.M", expectedIL: @"
{
  // Code size      301 (0x12d)
  .maxstack  3
  .locals init (object V_0, //obj
                int V_1, //x
                object V_2, //y
                object V_3, //z1
                int V_4, //p1
                int V_5, //q
                int V_6, //p2
                int V_7, //z2
                System.Runtime.CompilerServices.ITuple V_8,
                C V_9,
                object V_10,
                C V_11,
                D V_12,
                D V_13,
                bool V_14)
 -IL_0000:  nop
 -IL_0001:  call       ""object Program.F()""
  IL_0006:  stloc.0
 -IL_0007:  ldloc.0
  IL_0008:  isinst     ""int""
  IL_000d:  brfalse.s  IL_001b
  IL_000f:  ldloc.0
  IL_0010:  unbox.any  ""int""
  IL_0015:  stloc.1
  IL_0016:  br         IL_0125
  IL_001b:  ldloc.0
  IL_001c:  isinst     ""bool""
  IL_0021:  brtrue     IL_0125
  IL_0026:  ldloc.0
  IL_0027:  isinst     ""System.Runtime.CompilerServices.ITuple""
  IL_002c:  stloc.s    V_8
  IL_002e:  ldloc.s    V_8
  IL_0030:  brfalse.s  IL_0053
  IL_0032:  ldloc.s    V_8
  IL_0034:  callvirt   ""int System.Runtime.CompilerServices.ITuple.Length.get""
  IL_0039:  ldc.i4.2
  IL_003a:  bne.un.s   IL_0053
  IL_003c:  ldloc.s    V_8
  IL_003e:  ldc.i4.0
  IL_003f:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_0044:  stloc.2
  IL_0045:  ldloc.s    V_8
  IL_0047:  ldc.i4.1
  IL_0048:  callvirt   ""object System.Runtime.CompilerServices.ITuple.this[int].get""
  IL_004d:  stloc.3
  IL_004e:  br         IL_0125
  IL_0053:  ldloc.0
  IL_0054:  isinst     ""double""
  IL_0059:  brfalse.s  IL_006f
  IL_005b:  ldloc.0
  IL_005c:  unbox.any  ""double""
  IL_0061:  ldc.r8     4
  IL_006a:  beq        IL_0125
  IL_006f:  ldloc.0
  IL_0070:  isinst     ""C""
  IL_0075:  brtrue     IL_0125
  IL_007a:  ldloc.0
  IL_007b:  isinst     ""System.Runtime.CompilerServices.ITuple""
  IL_0080:  stloc.s    V_8
  IL_0082:  ldloc.s    V_8
  IL_0084:  brfalse.s  IL_0092
  IL_0086:  ldloc.s    V_8
  IL_0088:  callvirt   ""int System.Runtime.CompilerServices.ITuple.Length.get""
  IL_008d:  brfalse    IL_0125
  IL_0092:  ldloc.0
  IL_0093:  isinst     ""C""
  IL_0098:  stloc.s    V_9
  IL_009a:  ldloc.s    V_9
  IL_009c:  brfalse.s  IL_00c3
  IL_009e:  ldloc.s    V_9
  IL_00a0:  ldloca.s   V_4
  IL_00a2:  ldloca.s   V_10
  IL_00a4:  callvirt   ""void C.Deconstruct(out int, out object)""
  IL_00a9:  nop
  IL_00aa:  ldloc.s    V_10
  IL_00ac:  isinst     ""C""
  IL_00b1:  stloc.s    V_11
  IL_00b3:  ldloc.s    V_11
  IL_00b5:  brfalse.s  IL_00c3
  IL_00b7:  ldloc.s    V_11
  IL_00b9:  ldloca.s   V_5
  IL_00bb:  callvirt   ""void C.Deconstruct(out int)""
  IL_00c0:  nop
  IL_00c1:  br.s       IL_0125
  IL_00c3:  ldloc.0
  IL_00c4:  isinst     ""C""
  IL_00c9:  stloc.s    V_11
  IL_00cb:  ldloc.s    V_11
  IL_00cd:  brfalse.s  IL_00db
  IL_00cf:  ldloc.s    V_11
  IL_00d1:  ldloca.s   V_6
  IL_00d3:  callvirt   ""void C.Deconstruct(out int)""
  IL_00d8:  nop
  IL_00d9:  br.s       IL_0125
  IL_00db:  ldloc.0
  IL_00dc:  isinst     ""D""
  IL_00e1:  stloc.s    V_12
  IL_00e3:  ldloc.s    V_12
  IL_00e5:  brfalse.s  IL_0122
  IL_00e7:  ldloc.s    V_12
  IL_00e9:  callvirt   ""int D.P.get""
  IL_00ee:  ldc.i4.1
  IL_00ef:  bne.un.s   IL_0122
  IL_00f1:  ldloc.s    V_12
  IL_00f3:  callvirt   ""D D.Q.get""
  IL_00f8:  stloc.s    V_13
  IL_00fa:  ldloc.s    V_13
  IL_00fc:  brfalse.s  IL_0122
  IL_00fe:  ldloc.s    V_13
  IL_0100:  callvirt   ""int D.P.get""
  IL_0105:  ldc.i4.2
  IL_0106:  bne.un.s   IL_0122
  IL_0108:  ldloc.s    V_12
  IL_010a:  callvirt   ""C D.R.get""
  IL_010f:  stloc.s    V_11
  IL_0111:  ldloc.s    V_11
  IL_0113:  brfalse.s  IL_0122
  IL_0115:  ldloc.s    V_11
  IL_0117:  ldloca.s   V_7
  IL_0119:  callvirt   ""void C.Deconstruct(out int)""
  IL_011e:  nop
  IL_011f:  ldc.i4.1
  IL_0120:  br.s       IL_0123
  IL_0122:  ldc.i4.0
  IL_0123:  br.s       IL_0126
  IL_0125:  ldc.i4.1
  IL_0126:  stloc.s    V_14
  IL_0128:  br.s       IL_012a
 -IL_012a:  ldloc.s    V_14
  IL_012c:  ret
}
");

            verifier.VerifyPdb("Program.M", @"   
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""Deconstruct"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""18"" />
          <slot kind=""0"" offset=""106"" />
          <slot kind=""0"" offset=""230"" />
          <slot kind=""0"" offset=""233"" />
          <slot kind=""0"" offset=""419"" />
          <slot kind=""0"" offset=""429"" />
          <slot kind=""0"" offset=""465"" />
          <slot kind=""0"" offset=""561"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""26"" document=""1"" />
        <entry offset=""0x7"" startLine=""25"" startColumn=""9"" endLine=""45"" endColumn=""60"" document=""1"" />
        <entry offset=""0x12a"" startLine=""46"" startColumn=""5"" endLine=""46"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x12d"">
        <local name=""obj"" il_index=""0"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""x"" il_index=""1"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""y"" il_index=""2"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""z1"" il_index=""3"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""p1"" il_index=""4"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""q"" il_index=""5"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""p2"" il_index=""6"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
        <local name=""z2"" il_index=""7"" il_start=""0x0"" il_end=""0x12d"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [WorkItem(37232, "https://github.com/dotnet/roslyn/issues/37232")]
        [WorkItem(37237, "https://github.com/dotnet/roslyn/issues/37237")]
        [Fact]
        public void Patterns_SwitchExpression_Closures()
        {
            string source = WithWindowsLineBreaks(@"
using System;
public class C
{
    static int M() 
    {
        return F() switch 
        {
            1 => F() switch
                 {
                     C { P: int p, Q: C { P: int q } } => G(() => p + q),
                     _ => 10
                 },
            2 => F() switch
                 {
                     C { P: int r } => G(() => r),
                     _ => 20
                 },
            C { Q: int s } => G(() => s),
            _ => 0
        }
        switch 
        {
            var t when t > 0 => G(() => t),
            _ => 0
        };
    }

    object P { get; set; }
    object Q { get; set; }
    static object F() => null;
    static int G(Func<int> f) => 0;
}
");
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(c);
            verifier.VerifyIL("C.M", sequencePoints: "C.M", source: source, expectedIL: @"
    {
      // Code size      472 (0x1d8)
      .maxstack  2
      .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                    int V_1,
                    C.<>c__DisplayClass0_1 V_2, //CS$<>8__locals1
                    int V_3,
                    object V_4,
                    int V_5,
                    C V_6,
                    object V_7,
                    C.<>c__DisplayClass0_2 V_8, //CS$<>8__locals2
                    int V_9,
                    object V_10,
                    C V_11,
                    object V_12,
                    object V_13,
                    C V_14,
                    object V_15,
                    C.<>c__DisplayClass0_3 V_16, //CS$<>8__locals3
                    object V_17,
                    C V_18,
                    object V_19,
                    int V_20)
      // sequence point: {
      IL_0000:  nop
      // sequence point: <hidden>
      IL_0001:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
      IL_0006:  stloc.0
      // sequence point: <hidden>
      IL_0007:  newobj     ""C.<>c__DisplayClass0_1..ctor()""
      IL_000c:  stloc.2
      IL_000d:  call       ""object C.F()""
      IL_0012:  stloc.s    V_4
      IL_0014:  ldc.i4.1
      IL_0015:  brtrue.s   IL_0018
      // sequence point: switch  ...         }
      IL_0017:  nop
      // sequence point: <hidden>
      IL_0018:  ldloc.s    V_4
      IL_001a:  isinst     ""int""
      IL_001f:  brfalse.s  IL_003e
      IL_0021:  ldloc.s    V_4
      IL_0023:  unbox.any  ""int""
      IL_0028:  stloc.s    V_5
      // sequence point: <hidden>
      IL_002a:  ldloc.s    V_5
      IL_002c:  ldc.i4.1
      IL_002d:  beq.s      IL_0075
      IL_002f:  br.s       IL_0031
      IL_0031:  ldloc.s    V_5
      IL_0033:  ldc.i4.2
      IL_0034:  beq        IL_0116
      IL_0039:  br         IL_0194
      IL_003e:  ldloc.s    V_4
      IL_0040:  isinst     ""C""
      IL_0045:  stloc.s    V_6
      IL_0047:  ldloc.s    V_6
      IL_0049:  brfalse    IL_0194
      IL_004e:  ldloc.s    V_6
      IL_0050:  callvirt   ""object C.Q.get""
      IL_0055:  stloc.s    V_7
      // sequence point: <hidden>
      IL_0057:  ldloc.s    V_7
      IL_0059:  isinst     ""int""
      IL_005e:  brfalse    IL_0194
      IL_0063:  ldloc.2
      IL_0064:  ldloc.s    V_7
      IL_0066:  unbox.any  ""int""
      IL_006b:  stfld      ""int C.<>c__DisplayClass0_1.<s>5__3""
      // sequence point: <hidden>
      IL_0070:  br         IL_017e
      // sequence point: <hidden>
      IL_0075:  newobj     ""C.<>c__DisplayClass0_2..ctor()""
      IL_007a:  stloc.s    V_8
      IL_007c:  call       ""object C.F()""
      IL_0081:  stloc.s    V_10
      IL_0083:  ldc.i4.1
      IL_0084:  brtrue.s   IL_0087
      // sequence point: switch ...             
      IL_0086:  nop
      // sequence point: <hidden>
      IL_0087:  ldloc.s    V_10
      IL_0089:  isinst     ""C""
      IL_008e:  stloc.s    V_11
      IL_0090:  ldloc.s    V_11
      IL_0092:  brfalse.s  IL_0104
      IL_0094:  ldloc.s    V_11
      IL_0096:  callvirt   ""object C.P.get""
      IL_009b:  stloc.s    V_12
      // sequence point: <hidden>
      IL_009d:  ldloc.s    V_12
      IL_009f:  isinst     ""int""
      IL_00a4:  brfalse.s  IL_0104
      IL_00a6:  ldloc.s    V_8
      IL_00a8:  ldloc.s    V_12
      IL_00aa:  unbox.any  ""int""
      IL_00af:  stfld      ""int C.<>c__DisplayClass0_2.<p>5__4""
      // sequence point: <hidden>
      IL_00b4:  ldloc.s    V_11
      IL_00b6:  callvirt   ""object C.Q.get""
      IL_00bb:  stloc.s    V_13
      // sequence point: <hidden>
      IL_00bd:  ldloc.s    V_13
      IL_00bf:  isinst     ""C""
      IL_00c4:  stloc.s    V_14
      IL_00c6:  ldloc.s    V_14
      IL_00c8:  brfalse.s  IL_0104
      IL_00ca:  ldloc.s    V_14
      IL_00cc:  callvirt   ""object C.P.get""
      IL_00d1:  stloc.s    V_15
      // sequence point: <hidden>
      IL_00d3:  ldloc.s    V_15
      IL_00d5:  isinst     ""int""
      IL_00da:  brfalse.s  IL_0104
      IL_00dc:  ldloc.s    V_8
      IL_00de:  ldloc.s    V_15
      IL_00e0:  unbox.any  ""int""
      IL_00e5:  stfld      ""int C.<>c__DisplayClass0_2.<q>5__5""
      // sequence point: <hidden>
      IL_00ea:  br.s       IL_00ec
      // sequence point: <hidden>
      IL_00ec:  br.s       IL_00ee
      // sequence point: G(() => p + q)
      IL_00ee:  ldloc.s    V_8
      IL_00f0:  ldftn      ""int C.<>c__DisplayClass0_2.<M>b__2()""
      IL_00f6:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
      IL_00fb:  call       ""int C.G(System.Func<int>)""
      IL_0100:  stloc.s    V_9
      IL_0102:  br.s       IL_010a
      // sequence point: 10
      IL_0104:  ldc.i4.s   10
      IL_0106:  stloc.s    V_9
      IL_0108:  br.s       IL_010a
      // sequence point: <hidden>
      IL_010a:  ldc.i4.1
      IL_010b:  brtrue.s   IL_010e
      // sequence point: switch  ...         }
      IL_010d:  nop
      // sequence point: F() switch ...             
      IL_010e:  ldloc.s    V_9
      IL_0110:  stloc.3
      IL_0111:  br         IL_0198
      // sequence point: <hidden>
      IL_0116:  newobj     ""C.<>c__DisplayClass0_3..ctor()""
      IL_011b:  stloc.s    V_16
      IL_011d:  call       ""object C.F()""
      IL_0122:  stloc.s    V_17
      IL_0124:  ldc.i4.1
      IL_0125:  brtrue.s   IL_0128
      // sequence point: switch ...             
      IL_0127:  nop
      // sequence point: <hidden>
      IL_0128:  ldloc.s    V_17
      IL_012a:  isinst     ""C""
      IL_012f:  stloc.s    V_18
      IL_0131:  ldloc.s    V_18
      IL_0133:  brfalse.s  IL_016f
      IL_0135:  ldloc.s    V_18
      IL_0137:  callvirt   ""object C.P.get""
      IL_013c:  stloc.s    V_19
      // sequence point: <hidden>
      IL_013e:  ldloc.s    V_19
      IL_0140:  isinst     ""int""
      IL_0145:  brfalse.s  IL_016f
      IL_0147:  ldloc.s    V_16
      IL_0149:  ldloc.s    V_19
      IL_014b:  unbox.any  ""int""
      IL_0150:  stfld      ""int C.<>c__DisplayClass0_3.<r>5__6""
      // sequence point: <hidden>
      IL_0155:  br.s       IL_0157
      // sequence point: <hidden>
      IL_0157:  br.s       IL_0159
      // sequence point: G(() => r)
      IL_0159:  ldloc.s    V_16
      IL_015b:  ldftn      ""int C.<>c__DisplayClass0_3.<M>b__3()""
      IL_0161:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
      IL_0166:  call       ""int C.G(System.Func<int>)""
      IL_016b:  stloc.s    V_9
      IL_016d:  br.s       IL_0175
      // sequence point: 20
      IL_016f:  ldc.i4.s   20
      IL_0171:  stloc.s    V_9
      IL_0173:  br.s       IL_0175
      // sequence point: <hidden>
      IL_0175:  ldc.i4.1
      IL_0176:  brtrue.s   IL_0179
      // sequence point: F() switch ...             
      IL_0178:  nop
      // sequence point: F() switch ...             
      IL_0179:  ldloc.s    V_9
      IL_017b:  stloc.3
      IL_017c:  br.s       IL_0198
      // sequence point: <hidden>
      IL_017e:  br.s       IL_0180
      // sequence point: G(() => s)
      IL_0180:  ldloc.2
      IL_0181:  ldftn      ""int C.<>c__DisplayClass0_1.<M>b__1()""
      IL_0187:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
      IL_018c:  call       ""int C.G(System.Func<int>)""
      IL_0191:  stloc.3
      IL_0192:  br.s       IL_0198
      // sequence point: 0
      IL_0194:  ldc.i4.0
      IL_0195:  stloc.3
      IL_0196:  br.s       IL_0198
      // sequence point: <hidden>
      IL_0198:  ldc.i4.1
      IL_0199:  brtrue.s   IL_019c
      // sequence point: return F() s ...         };
      IL_019b:  nop
      // sequence point: <hidden>
      IL_019c:  ldloc.0
      IL_019d:  ldloc.3
      IL_019e:  stfld      ""int C.<>c__DisplayClass0_0.<t>5__2""
      IL_01a3:  ldc.i4.1
      IL_01a4:  brtrue.s   IL_01a7
      // sequence point: switch  ...         }
      IL_01a6:  nop
      // sequence point: <hidden>
      IL_01a7:  br.s       IL_01a9
      // sequence point: when t > 0
      IL_01a9:  ldloc.0
      IL_01aa:  ldfld      ""int C.<>c__DisplayClass0_0.<t>5__2""
      IL_01af:  ldc.i4.0
      IL_01b0:  bgt.s      IL_01b4
      // sequence point: <hidden>
      IL_01b2:  br.s       IL_01c8
      // sequence point: G(() => t)
      IL_01b4:  ldloc.0
      IL_01b5:  ldftn      ""int C.<>c__DisplayClass0_0.<M>b__0()""
      IL_01bb:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
      IL_01c0:  call       ""int C.G(System.Func<int>)""
      IL_01c5:  stloc.1
      IL_01c6:  br.s       IL_01cc
      // sequence point: 0
      IL_01c8:  ldc.i4.0
      IL_01c9:  stloc.1
      IL_01ca:  br.s       IL_01cc
      // sequence point: <hidden>
      IL_01cc:  ldc.i4.1
      IL_01cd:  brtrue.s   IL_01d0
      // sequence point: return F() s ...         };
      IL_01cf:  nop
      // sequence point: <hidden>
      IL_01d0:  ldloc.1
      IL_01d1:  stloc.s    V_20
      IL_01d3:  br.s       IL_01d5
      // sequence point: }
      IL_01d5:  ldloc.s    V_20
      IL_01d7:  ret
    }
");
            verifier.VerifyPdb("C.M", @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""11"" />
              <slot kind=""temp"" />
              <slot kind=""30"" offset=""22"" />
              <slot kind=""temp"" />
              <slot kind=""35"" offset=""22"" />
              <slot kind=""35"" offset=""22"" ordinal=""1"" />
              <slot kind=""35"" offset=""22"" ordinal=""2"" />
              <slot kind=""35"" offset=""22"" ordinal=""3"" />
              <slot kind=""30"" offset=""63"" />
              <slot kind=""temp"" />
              <slot kind=""35"" offset=""63"" />
              <slot kind=""35"" offset=""63"" ordinal=""1"" />
              <slot kind=""35"" offset=""63"" ordinal=""2"" />
              <slot kind=""35"" offset=""63"" ordinal=""3"" />
              <slot kind=""35"" offset=""63"" ordinal=""4"" />
              <slot kind=""35"" offset=""63"" ordinal=""5"" />
              <slot kind=""30"" offset=""238"" />
              <slot kind=""35"" offset=""238"" />
              <slot kind=""35"" offset=""238"" ordinal=""1"" />
              <slot kind=""35"" offset=""238"" ordinal=""2"" />
              <slot kind=""21"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""11"" />
              <closure offset=""22"" />
              <closure offset=""63"" />
              <closure offset=""238"" />
              <lambda offset=""511"" closure=""0"" />
              <lambda offset=""407"" closure=""1"" />
              <lambda offset=""157"" closure=""2"" />
              <lambda offset=""313"" closure=""3"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
            <entry offset=""0x1"" hidden=""true"" document=""1"" />
            <entry offset=""0x7"" hidden=""true"" document=""1"" />
            <entry offset=""0x17"" startLine=""7"" startColumn=""20"" endLine=""21"" endColumn=""10"" document=""1"" />
            <entry offset=""0x18"" hidden=""true"" document=""1"" />
            <entry offset=""0x2a"" hidden=""true"" document=""1"" />
            <entry offset=""0x57"" hidden=""true"" document=""1"" />
            <entry offset=""0x70"" hidden=""true"" document=""1"" />
            <entry offset=""0x75"" hidden=""true"" document=""1"" />
            <entry offset=""0x86"" startLine=""9"" startColumn=""22"" endLine=""13"" endColumn=""19"" document=""1"" />
            <entry offset=""0x87"" hidden=""true"" document=""1"" />
            <entry offset=""0x9d"" hidden=""true"" document=""1"" />
            <entry offset=""0xb4"" hidden=""true"" document=""1"" />
            <entry offset=""0xbd"" hidden=""true"" document=""1"" />
            <entry offset=""0xd3"" hidden=""true"" document=""1"" />
            <entry offset=""0xea"" hidden=""true"" document=""1"" />
            <entry offset=""0xec"" hidden=""true"" document=""1"" />
            <entry offset=""0xee"" startLine=""11"" startColumn=""59"" endLine=""11"" endColumn=""73"" document=""1"" />
            <entry offset=""0x104"" startLine=""12"" startColumn=""27"" endLine=""12"" endColumn=""29"" document=""1"" />
            <entry offset=""0x10a"" hidden=""true"" document=""1"" />
            <entry offset=""0x10d"" startLine=""7"" startColumn=""20"" endLine=""21"" endColumn=""10"" document=""1"" />
            <entry offset=""0x10e"" startLine=""9"" startColumn=""18"" endLine=""13"" endColumn=""19"" document=""1"" />
            <entry offset=""0x116"" hidden=""true"" document=""1"" />
            <entry offset=""0x127"" startLine=""14"" startColumn=""22"" endLine=""18"" endColumn=""19"" document=""1"" />
            <entry offset=""0x128"" hidden=""true"" document=""1"" />
            <entry offset=""0x13e"" hidden=""true"" document=""1"" />
            <entry offset=""0x155"" hidden=""true"" document=""1"" />
            <entry offset=""0x157"" hidden=""true"" document=""1"" />
            <entry offset=""0x159"" startLine=""16"" startColumn=""40"" endLine=""16"" endColumn=""50"" document=""1"" />
            <entry offset=""0x16f"" startLine=""17"" startColumn=""27"" endLine=""17"" endColumn=""29"" document=""1"" />
            <entry offset=""0x175"" hidden=""true"" document=""1"" />
            <entry offset=""0x178"" startLine=""9"" startColumn=""18"" endLine=""13"" endColumn=""19"" document=""1"" />
            <entry offset=""0x179"" startLine=""14"" startColumn=""18"" endLine=""18"" endColumn=""19"" document=""1"" />
            <entry offset=""0x17e"" hidden=""true"" document=""1"" />
            <entry offset=""0x180"" startLine=""19"" startColumn=""31"" endLine=""19"" endColumn=""41"" document=""1"" />
            <entry offset=""0x194"" startLine=""20"" startColumn=""18"" endLine=""20"" endColumn=""19"" document=""1"" />
            <entry offset=""0x198"" hidden=""true"" document=""1"" />
            <entry offset=""0x19b"" startLine=""7"" startColumn=""9"" endLine=""26"" endColumn=""11"" document=""1"" />
            <entry offset=""0x19c"" hidden=""true"" document=""1"" />
            <entry offset=""0x1a6"" startLine=""22"" startColumn=""9"" endLine=""26"" endColumn=""10"" document=""1"" />
            <entry offset=""0x1a7"" hidden=""true"" document=""1"" />
            <entry offset=""0x1a9"" startLine=""24"" startColumn=""19"" endLine=""24"" endColumn=""29"" document=""1"" />
            <entry offset=""0x1b2"" hidden=""true"" document=""1"" />
            <entry offset=""0x1b4"" startLine=""24"" startColumn=""33"" endLine=""24"" endColumn=""43"" document=""1"" />
            <entry offset=""0x1c8"" startLine=""25"" startColumn=""18"" endLine=""25"" endColumn=""19"" document=""1"" />
            <entry offset=""0x1cc"" hidden=""true"" document=""1"" />
            <entry offset=""0x1cf"" startLine=""7"" startColumn=""9"" endLine=""26"" endColumn=""11"" document=""1"" />
            <entry offset=""0x1d0"" hidden=""true"" document=""1"" />
            <entry offset=""0x1d5"" startLine=""27"" startColumn=""5"" endLine=""27"" endColumn=""6"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x1d8"">
            <namespace name=""System"" />
            <scope startOffset=""0x1"" endOffset=""0x1d5"">
              <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x1d5"" attributes=""0"" />
              <scope startOffset=""0x7"" endOffset=""0x1a3"">
                <local name=""CS$&lt;&gt;8__locals1"" il_index=""2"" il_start=""0x7"" il_end=""0x1a3"" attributes=""0"" />
                <scope startOffset=""0x75"" endOffset=""0x111"">
                  <local name=""CS$&lt;&gt;8__locals2"" il_index=""8"" il_start=""0x75"" il_end=""0x111"" attributes=""0"" />
                </scope>
                <scope startOffset=""0x116"" endOffset=""0x17c"">
                  <local name=""CS$&lt;&gt;8__locals3"" il_index=""16"" il_start=""0x116"" il_end=""0x17c"" attributes=""0"" />
                </scope>
              </scope>
            </scope>
          </scope>
        </method>
      </methods>
    </symbols>
");
        }

        [WorkItem(50321, "https://github.com/dotnet/roslyn/issues/50321")]
        [ConditionalFact(typeof(CoreClrOnly))]
        public void NestedSwitchExpressions_Closures_01()
        {
            string source = WithWindowsLineBreaks(
@"using System;
class C
{
    static int F(object o)
    {
        return o switch
        {
            int i => new Func<int>(() => i + i switch
            {
                1 => 2,
                _ => 3
            })(),
            _ => 4
        };
    }
}");
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            verifier.VerifyTypeIL("C",
@".class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 '<i>5__2'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a1
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance int32 '<F>b__0' () cil managed 
		{
			// Method begins at RVA 0x20ac
			// Code size 38 (0x26)
			.maxstack 2
			.locals init (
				[0] int32,
				[1] int32
			)
			IL_0000: ldarg.0
			IL_0001: ldfld int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
			IL_0006: stloc.0
			IL_0007: ldc.i4.1
			IL_0008: brtrue.s IL_000b
			IL_000a: nop
			IL_000b: ldarg.0
			IL_000c: ldfld int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
			IL_0011: ldc.i4.1
			IL_0012: beq.s IL_0016
			IL_0014: br.s IL_001a
			IL_0016: ldc.i4.2
			IL_0017: stloc.1
			IL_0018: br.s IL_001e
			IL_001a: ldc.i4.3
			IL_001b: stloc.1
			IL_001c: br.s IL_001e
			IL_001e: ldc.i4.1
			IL_001f: brtrue.s IL_0022
			IL_0021: nop
			IL_0022: ldloc.0
			IL_0023: ldloc.1
			IL_0024: add
			IL_0025: ret
		} // end of method '<>c__DisplayClass0_0'::'<F>b__0'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method private hidebysig static 
		int32 F (
			object o
		) cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 69 (0x45)
		.maxstack 2
		.locals init (
			[0] class C/'<>c__DisplayClass0_0',
			[1] int32,
			[2] int32
		)
		IL_0000: nop
		IL_0001: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0006: stloc.0
		IL_0007: ldc.i4.1
		IL_0008: brtrue.s IL_000b
		IL_000a: nop
		IL_000b: ldarg.0
		IL_000c: isinst [netstandard]System.Int32
		IL_0011: brfalse.s IL_0037
		IL_0013: ldloc.0
		IL_0014: ldarg.0
		IL_0015: unbox.any [netstandard]System.Int32
		IL_001a: stfld int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
		IL_001f: br.s IL_0021
		IL_0021: br.s IL_0023
		IL_0023: ldloc.0
		IL_0024: ldftn instance int32 C/'<>c__DisplayClass0_0'::'<F>b__0'()
		IL_002a: newobj instance void class [netstandard]System.Func`1<int32>::.ctor(object, native int)
		IL_002f: callvirt instance !0 class [netstandard]System.Func`1<int32>::Invoke()
		IL_0034: stloc.1
		IL_0035: br.s IL_003b
		IL_0037: ldc.i4.4
		IL_0038: stloc.1
		IL_0039: br.s IL_003b
		IL_003b: ldc.i4.1
		IL_003c: brtrue.s IL_003f
		IL_003e: nop
		IL_003f: ldloc.1
		IL_0040: stloc.2
		IL_0041: br.s IL_0043
		IL_0043: ldloc.2
		IL_0044: ret
	} // end of method C::F
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a1
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [netstandard]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method C::.ctor
} // end of class C
");
            verifier.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""11"" />
          <slot kind=""temp"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""11"" />
          <lambda offset=""80"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""18"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" hidden=""true"" document=""1"" />
        <entry offset=""0x23"" startLine=""8"" startColumn=""22"" endLine=""12"" endColumn=""17"" document=""1"" />
        <entry offset=""0x37"" startLine=""13"" startColumn=""18"" endLine=""13"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3b"" hidden=""true"" document=""1"" />
        <entry offset=""0x3e"" startLine=""6"" startColumn=""9"" endLine=""14"" endColumn=""11"" document=""1"" />
        <entry offset=""0x3f"" hidden=""true"" document=""1"" />
        <entry offset=""0x43"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x45"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x43"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x43"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;F&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" parameterNames=""o"" />
        <encLocalSlotMap>
          <slot kind=""28"" offset=""86"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""42"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0xa"" startLine=""8"" startColumn=""48"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0xb"" hidden=""true"" document=""1"" />
        <entry offset=""0x16"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1a"" startLine=""11"" startColumn=""22"" endLine=""11"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1e"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""8"" startColumn=""42"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x22"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(50321, "https://github.com/dotnet/roslyn/issues/50321")]
        [ConditionalFact(typeof(CoreClrOnly))]
        public void NestedSwitchExpressions_Closures_02()
        {
            string source = WithWindowsLineBreaks(
@"using System;
class C
{
    static string F(object o)
    {
        return o switch
        {
            int i => new Func<string>(() => ""1"" + i switch
            {
                1 => new Func<string>(() => ""2"" + i)(),
                _ => ""3""
            })(),
            _ => ""4""
        };
    }
}");
            var verifier = CompileAndVerify(source, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll);
            verifier.VerifyTypeIL("C",
@".class private auto ansi beforefieldinit C
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested private auto ansi sealed beforefieldinit '<>c__DisplayClass0_0'
		extends [netstandard]System.Object
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Fields
		.field public int32 '<i>5__2'
		.field public class [netstandard]System.Func`1<string> '<>9__1'
		// Methods
		.method public hidebysig specialname rtspecialname 
			instance void .ctor () cil managed 
		{
			// Method begins at RVA 0x20a5
			// Code size 8 (0x8)
			.maxstack 8
			IL_0000: ldarg.0
			IL_0001: call instance void [netstandard]System.Object::.ctor()
			IL_0006: nop
			IL_0007: ret
		} // end of method '<>c__DisplayClass0_0'::.ctor
		.method assembly hidebysig 
			instance string '<F>b__0' () cil managed 
		{
			// Method begins at RVA 0x20b0
			// Code size 78 (0x4e)
			.maxstack 3
			.locals init (
				[0] string,
				[1] class [netstandard]System.Func`1<string>
			)
			IL_0000: ldc.i4.1
			IL_0001: brtrue.s IL_0004
			IL_0003: nop
			IL_0004: ldarg.0
			IL_0005: ldfld int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
			IL_000a: ldc.i4.1
			IL_000b: beq.s IL_000f
			IL_000d: br.s IL_0036
			IL_000f: ldarg.0
			IL_0010: ldfld class [netstandard]System.Func`1<string> C/'<>c__DisplayClass0_0'::'<>9__1'
			IL_0015: dup
			IL_0016: brtrue.s IL_002e
			IL_0018: pop
			IL_0019: ldarg.0
			IL_001a: ldarg.0
			IL_001b: ldftn instance string C/'<>c__DisplayClass0_0'::'<F>b__1'()
			IL_0021: newobj instance void class [netstandard]System.Func`1<string>::.ctor(object, native int)
			IL_0026: dup
			IL_0027: stloc.1
			IL_0028: stfld class [netstandard]System.Func`1<string> C/'<>c__DisplayClass0_0'::'<>9__1'
			IL_002d: ldloc.1
			IL_002e: callvirt instance !0 class [netstandard]System.Func`1<string>::Invoke()
			IL_0033: stloc.0
			IL_0034: br.s IL_003e
			IL_0036: ldstr ""3""
			IL_003b: stloc.0
			IL_003c: br.s IL_003e
			IL_003e: ldc.i4.1
			IL_003f: brtrue.s IL_0042
			IL_0041: nop
			IL_0042: ldstr ""1""
			IL_0047: ldloc.0
			IL_0048: call string [netstandard]System.String::Concat(string, string)
			IL_004d: ret
		} // end of method '<>c__DisplayClass0_0'::'<F>b__0'
		.method assembly hidebysig 
			instance string '<F>b__1' () cil managed 
		{
			// Method begins at RVA 0x210a
			// Code size 22 (0x16)
			.maxstack 8
			IL_0000: ldstr ""2""
			IL_0005: ldarg.0
			IL_0006: ldflda int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
			IL_000b: call instance string [netstandard]System.Int32::ToString()
			IL_0010: call string [netstandard]System.String::Concat(string, string)
			IL_0015: ret
		} // end of method '<>c__DisplayClass0_0'::'<F>b__1'
	} // end of class <>c__DisplayClass0_0
	// Methods
	.method private hidebysig static 
		string F (
			object o
		) cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 73 (0x49)
		.maxstack 2
		.locals init (
			[0] class C/'<>c__DisplayClass0_0',
			[1] string,
			[2] string
		)
		IL_0000: nop
		IL_0001: newobj instance void C/'<>c__DisplayClass0_0'::.ctor()
		IL_0006: stloc.0
		IL_0007: ldc.i4.1
		IL_0008: brtrue.s IL_000b
		IL_000a: nop
		IL_000b: ldarg.0
		IL_000c: isinst [netstandard]System.Int32
		IL_0011: brfalse.s IL_0037
		IL_0013: ldloc.0
		IL_0014: ldarg.0
		IL_0015: unbox.any [netstandard]System.Int32
		IL_001a: stfld int32 C/'<>c__DisplayClass0_0'::'<i>5__2'
		IL_001f: br.s IL_0021
		IL_0021: br.s IL_0023
		IL_0023: ldloc.0
		IL_0024: ldftn instance string C/'<>c__DisplayClass0_0'::'<F>b__0'()
		IL_002a: newobj instance void class [netstandard]System.Func`1<string>::.ctor(object, native int)
		IL_002f: callvirt instance !0 class [netstandard]System.Func`1<string>::Invoke()
		IL_0034: stloc.1
		IL_0035: br.s IL_003f
		IL_0037: ldstr ""4""
		IL_003c: stloc.1
		IL_003d: br.s IL_003f
		IL_003f: ldc.i4.1
		IL_0040: brtrue.s IL_0043
		IL_0042: nop
		IL_0043: ldloc.1
		IL_0044: stloc.2
		IL_0045: br.s IL_0047
		IL_0047: ldloc.2
		IL_0048: ret
	} // end of method C::F
	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x20a5
		// Code size 8 (0x8)
		.maxstack 8
		IL_0000: ldarg.0
		IL_0001: call instance void [netstandard]System.Object::.ctor()
		IL_0006: nop
		IL_0007: ret
	} // end of method C::.ctor
} // end of class C
");
            verifier.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""11"" />
          <slot kind=""temp"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""11"" />
          <lambda offset=""83"" closure=""0"" />
          <lambda offset=""158"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" hidden=""true"" document=""1"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""18"" endLine=""14"" endColumn=""10"" document=""1"" />
        <entry offset=""0xb"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" hidden=""true"" document=""1"" />
        <entry offset=""0x23"" startLine=""8"" startColumn=""22"" endLine=""12"" endColumn=""17"" document=""1"" />
        <entry offset=""0x37"" startLine=""13"" startColumn=""18"" endLine=""13"" endColumn=""21"" document=""1"" />
        <entry offset=""0x3f"" hidden=""true"" document=""1"" />
        <entry offset=""0x42"" startLine=""6"" startColumn=""9"" endLine=""14"" endColumn=""11"" document=""1"" />
        <entry offset=""0x43"" hidden=""true"" document=""1"" />
        <entry offset=""0x47"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x49"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0x47"">
          <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x1"" il_end=""0x47"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;F&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" parameterNames=""o"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""45"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""53"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x4"" hidden=""true"" document=""1"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""55"" document=""1"" />
        <entry offset=""0x36"" startLine=""11"" startColumn=""22"" endLine=""11"" endColumn=""25"" document=""1"" />
        <entry offset=""0x3e"" hidden=""true"" document=""1"" />
        <entry offset=""0x41"" startLine=""8"" startColumn=""45"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x42"" hidden=""true"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;F&gt;b__1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""F"" parameterNames=""o"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""45"" endLine=""10"" endColumn=""52"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(37261, "https://github.com/dotnet/roslyn/issues/37261")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SwitchExpression_MethodBody()
        {
            string source = @"
using System;
public class C
{
    static int M() => F() switch
    {
        1 => 1,
        C { P: int p, Q: C { P: int q } } => G(() => p + q),
        _ => 0
    };

    object P { get; set; }
    object Q { get; set; }
    static object F() => null;
    static int G(Func<int> f) => 0;
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(c);

            verifier.VerifyIL("C.M", sequencePoints: "C.M", source: source, expectedIL: @"
    {
      // Code size      171 (0xab)
      .maxstack  2
      .locals init (C.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                    int V_1,
                    object V_2,
                    int V_3,
                    C V_4,
                    object V_5,
                    object V_6,
                    C V_7,
                    object V_8)
      // sequence point: <hidden>
      IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
      IL_0005:  stloc.0
      IL_0006:  call       ""object C.F()""
      IL_000b:  stloc.2
      IL_000c:  ldc.i4.1
      IL_000d:  brtrue.s   IL_0010
      // sequence point: switch ...     }
      IL_000f:  nop
      // sequence point: <hidden>
      IL_0010:  ldloc.2
      IL_0011:  isinst     ""int""
      IL_0016:  brfalse.s  IL_0025
      IL_0018:  ldloc.2
      IL_0019:  unbox.any  ""int""
      IL_001e:  stloc.3
      // sequence point: <hidden>
      IL_001f:  ldloc.3
      IL_0020:  ldc.i4.1
      IL_0021:  beq.s      IL_0087
      IL_0023:  br.s       IL_00a1
      IL_0025:  ldloc.2
      IL_0026:  isinst     ""C""
      IL_002b:  stloc.s    V_4
      IL_002d:  ldloc.s    V_4
      IL_002f:  brfalse.s  IL_00a1
      IL_0031:  ldloc.s    V_4
      IL_0033:  callvirt   ""object C.P.get""
      IL_0038:  stloc.s    V_5
      // sequence point: <hidden>
      IL_003a:  ldloc.s    V_5
      IL_003c:  isinst     ""int""
      IL_0041:  brfalse.s  IL_00a1
      IL_0043:  ldloc.0
      IL_0044:  ldloc.s    V_5
      IL_0046:  unbox.any  ""int""
      IL_004b:  stfld      ""int C.<>c__DisplayClass0_0.<p>5__2""
      // sequence point: <hidden>
      IL_0050:  ldloc.s    V_4
      IL_0052:  callvirt   ""object C.Q.get""
      IL_0057:  stloc.s    V_6
      // sequence point: <hidden>
      IL_0059:  ldloc.s    V_6
      IL_005b:  isinst     ""C""
      IL_0060:  stloc.s    V_7
      IL_0062:  ldloc.s    V_7
      IL_0064:  brfalse.s  IL_00a1
      IL_0066:  ldloc.s    V_7
      IL_0068:  callvirt   ""object C.P.get""
      IL_006d:  stloc.s    V_8
      // sequence point: <hidden>
      IL_006f:  ldloc.s    V_8
      IL_0071:  isinst     ""int""
      IL_0076:  brfalse.s  IL_00a1
      IL_0078:  ldloc.0
      IL_0079:  ldloc.s    V_8
      IL_007b:  unbox.any  ""int""
      IL_0080:  stfld      ""int C.<>c__DisplayClass0_0.<q>5__3""
      // sequence point: <hidden>
      IL_0085:  br.s       IL_008b
      // sequence point: 1
      IL_0087:  ldc.i4.1
      IL_0088:  stloc.1
      IL_0089:  br.s       IL_00a5
      // sequence point: <hidden>
      IL_008b:  br.s       IL_008d
      // sequence point: G(() => p + q)
      IL_008d:  ldloc.0
      IL_008e:  ldftn      ""int C.<>c__DisplayClass0_0.<M>b__0()""
      IL_0094:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
      IL_0099:  call       ""int C.G(System.Func<int>)""
      IL_009e:  stloc.1
      IL_009f:  br.s       IL_00a5
      // sequence point: 0
      IL_00a1:  ldc.i4.0
      IL_00a2:  stloc.1
      IL_00a3:  br.s       IL_00a5
      // sequence point: <hidden>
      IL_00a5:  ldc.i4.1
      IL_00a6:  brtrue.s   IL_00a9
      // sequence point: F() switch ...     }
      IL_00a8:  nop
      // sequence point: <hidden>
      IL_00a9:  ldloc.1
      IL_00aa:  ret
    }
");
            verifier.VerifyPdb("C.M", @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""7"" />
              <slot kind=""temp"" />
              <slot kind=""35"" offset=""7"" />
              <slot kind=""35"" offset=""7"" ordinal=""1"" />
              <slot kind=""35"" offset=""7"" ordinal=""2"" />
              <slot kind=""35"" offset=""7"" ordinal=""3"" />
              <slot kind=""35"" offset=""7"" ordinal=""4"" />
              <slot kind=""35"" offset=""7"" ordinal=""5"" />
              <slot kind=""35"" offset=""7"" ordinal=""6"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""7"" />
              <lambda offset=""92"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xf"" startLine=""5"" startColumn=""27"" endLine=""10"" endColumn=""6"" document=""1"" />
            <entry offset=""0x10"" hidden=""true"" document=""1"" />
            <entry offset=""0x1f"" hidden=""true"" document=""1"" />
            <entry offset=""0x3a"" hidden=""true"" document=""1"" />
            <entry offset=""0x50"" hidden=""true"" document=""1"" />
            <entry offset=""0x59"" hidden=""true"" document=""1"" />
            <entry offset=""0x6f"" hidden=""true"" document=""1"" />
            <entry offset=""0x85"" hidden=""true"" document=""1"" />
            <entry offset=""0x87"" startLine=""7"" startColumn=""14"" endLine=""7"" endColumn=""15"" document=""1"" />
            <entry offset=""0x8b"" hidden=""true"" document=""1"" />
            <entry offset=""0x8d"" startLine=""8"" startColumn=""46"" endLine=""8"" endColumn=""60"" document=""1"" />
            <entry offset=""0xa1"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""15"" document=""1"" />
            <entry offset=""0xa5"" hidden=""true"" document=""1"" />
            <entry offset=""0xa8"" startLine=""5"" startColumn=""23"" endLine=""10"" endColumn=""6"" document=""1"" />
            <entry offset=""0xa9"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0xab"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0xab"" attributes=""0"" />
          </scope>
        </method>
      </methods>
    </symbols>
");
        }

        [WorkItem(37261, "https://github.com/dotnet/roslyn/issues/37261")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SwitchExpression_MethodBody_02()
        {
            string source = @"
using System;
public class C
{
    static Action M1(int x) => () => { _ = x; };
    static Action M2(int x) => x switch { _ => () => { _ = x; } };
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(c);

            verifier.VerifyIL("C.M1", sequencePoints: "C.M1", source: source, expectedIL: @"
    {
      // Code size       26 (0x1a)
      .maxstack  2
      .locals init (C.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
      // sequence point: <hidden>
      IL_0000:  newobj     ""C.<>c__DisplayClass0_0..ctor()""
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldarg.0
      IL_0008:  stfld      ""int C.<>c__DisplayClass0_0.x""
      // sequence point: () => { _ = x; }
      IL_000d:  ldloc.0
      IL_000e:  ldftn      ""void C.<>c__DisplayClass0_0.<M1>b__0()""
      IL_0014:  newobj     ""System.Action..ctor(object, System.IntPtr)""
      IL_0019:  ret
    }
");
            verifier.VerifyIL("C.M2", sequencePoints: "C.M2", source: source, expectedIL: @"
    {
      // Code size       40 (0x28)
      .maxstack  2
      .locals init (C.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                    System.Action V_1)
      // sequence point: <hidden>
      IL_0000:  newobj     ""C.<>c__DisplayClass1_0..ctor()""
      IL_0005:  stloc.0
      IL_0006:  ldloc.0
      IL_0007:  ldarg.0
      IL_0008:  stfld      ""int C.<>c__DisplayClass1_0.x""
      // sequence point: x switch { _ => () => { _ = x; } }
      IL_000d:  ldc.i4.1
      IL_000e:  brtrue.s   IL_0011
      // sequence point: switch { _ => () => { _ = x; } }
      IL_0010:  nop
      // sequence point: <hidden>
      IL_0011:  br.s       IL_0013
      // sequence point: () => { _ = x; }
      IL_0013:  ldloc.0
      IL_0014:  ldftn      ""void C.<>c__DisplayClass1_0.<M2>b__0()""
      IL_001a:  newobj     ""System.Action..ctor(object, System.IntPtr)""
      IL_001f:  stloc.1
      IL_0020:  br.s       IL_0022
      // sequence point: <hidden>
      IL_0022:  ldc.i4.1
      IL_0023:  brtrue.s   IL_0026
      // sequence point: x switch { _ => () => { _ = x; } }
      IL_0025:  nop
      // sequence point: <hidden>
      IL_0026:  ldloc.1
      IL_0027:  ret
    }
");
            verifier.VerifyPdb("C.M1", @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M1"" parameterNames=""x"">
          <customDebugInfo>
            <using>
              <namespace usingCount=""1"" />
            </using>
            <encLocalSlotMap>
              <slot kind=""30"" offset=""0"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>0</methodOrdinal>
              <closure offset=""0"" />
              <lambda offset=""9"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" startLine=""5"" startColumn=""32"" endLine=""5"" endColumn=""48"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x1a"">
            <namespace name=""System"" />
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
          </scope>
        </method>
      </methods>
    </symbols>
");
            verifier.VerifyPdb("C.M2", @"
    <symbols>
      <files>
        <file id=""1"" name="""" language=""C#"" />
      </files>
      <methods>
        <method containingType=""C"" name=""M2"" parameterNames=""x"">
          <customDebugInfo>
            <forward declaringType=""C"" methodName=""M1"" parameterNames=""x"" />
            <encLocalSlotMap>
              <slot kind=""30"" offset=""0"" />
              <slot kind=""temp"" />
            </encLocalSlotMap>
            <encLambdaMap>
              <methodOrdinal>1</methodOrdinal>
              <closure offset=""0"" />
              <lambda offset=""25"" closure=""0"" />
            </encLambdaMap>
          </customDebugInfo>
          <sequencePoints>
            <entry offset=""0x0"" hidden=""true"" document=""1"" />
            <entry offset=""0xd"" startLine=""6"" startColumn=""32"" endLine=""6"" endColumn=""66"" document=""1"" />
            <entry offset=""0x10"" startLine=""6"" startColumn=""34"" endLine=""6"" endColumn=""66"" document=""1"" />
            <entry offset=""0x11"" hidden=""true"" document=""1"" />
            <entry offset=""0x13"" startLine=""6"" startColumn=""48"" endLine=""6"" endColumn=""64"" document=""1"" />
            <entry offset=""0x22"" hidden=""true"" document=""1"" />
            <entry offset=""0x25"" startLine=""6"" startColumn=""32"" endLine=""6"" endColumn=""66"" document=""1"" />
            <entry offset=""0x26"" hidden=""true"" document=""1"" />
          </sequencePoints>
          <scope startOffset=""0x0"" endOffset=""0x28"">
            <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x28"" attributes=""0"" />
          </scope>
        </method>
      </methods>
    </symbols>
");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SyntaxOffset_OutVarInInitializers_SwitchExpression()
        {
            var source =
@"class C
{ 
    static int G(out int x) => throw null;
    static int F(System.Func<int> x) => throw null;
    C() { }

    int y1 = G(out var z) switch { _ => F(() => z) }; // line 7
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-26"" />
          <slot kind=""temp"" />
          <slot kind=""35"" offset=""-26"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""-26"" />
          <lambda offset=""-4"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [WorkItem(43468, "https://github.com/dotnet/roslyn/issues/43468")]
        [Fact]
        public void HiddenSequencePointAtSwitchExpressionFinalMergePoint()
        {
            var source =
@"class C
{
    static int M(int x)
    {
        var y = x switch
        {
            1 => 2,
            _ => 3,
        };
        return y;
    }
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var verifier = CompileAndVerify(c);
            verifier.VerifyIL("C.M", sequencePoints: "C.M", source: source, expectedIL: @"
    {
      // Code size       31 (0x1f)
      .maxstack  2
      .locals init (int V_0, //y
                    int V_1,
                    int V_2)
      // sequence point: {
      IL_0000:  nop
      // sequence point: var y = x sw ...         };
      IL_0001:  ldc.i4.1
      IL_0002:  brtrue.s   IL_0005
      // sequence point: switch ...         }
      IL_0004:  nop
      // sequence point: <hidden>
      IL_0005:  ldarg.0
      IL_0006:  ldc.i4.1
      IL_0007:  beq.s      IL_000b
      IL_0009:  br.s       IL_000f
      // sequence point: 2
      IL_000b:  ldc.i4.2
      IL_000c:  stloc.1
      IL_000d:  br.s       IL_0013
      // sequence point: 3
      IL_000f:  ldc.i4.3
      IL_0010:  stloc.1
      IL_0011:  br.s       IL_0013
      // sequence point: <hidden>
      IL_0013:  ldc.i4.1
      IL_0014:  brtrue.s   IL_0017
      // sequence point: var y = x sw ...         };
      IL_0016:  nop
      // sequence point: <hidden>
      IL_0017:  ldloc.1
      IL_0018:  stloc.0
      // sequence point: return y;
      IL_0019:  ldloc.0
      IL_001a:  stloc.2
      IL_001b:  br.s       IL_001d
      // sequence point: }
      IL_001d:  ldloc.2
      IL_001e:  ret
    }
");
        }

        [WorkItem(12378, "https://github.com/dotnet/roslyn/issues/12378")]
        [WorkItem(13971, "https://github.com/dotnet/roslyn/issues/13971")]
        [Fact]
        public void Patterns_SwitchStatement_Constant()
        {
            string source = WithWindowsLineBreaks(
@"class Program
{
    static void M(object o)
    {
        switch (o)
        {
            case 1 when o == null:
            case 4:
            case 2 when o == null:
                break;
            case 1 when o != null:
            case 5:
            case 3 when o != null:
                break;
            default:
                break;
            case 1:
                break;
        }
        switch (o)
        {
            case 1:
                break;
            default:
                break;
        }
        switch (o)
        {
            default:
                break;
        }
    }
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            CompileAndVerify(c).VerifyIL(qualifiedMethodName: "Program.M", sequencePoints: "Program.M", source: source,
expectedIL: @"{
  // Code size      123 (0x7b)
  .maxstack  2
  .locals init (object V_0,
                int V_1,
                object V_2,
                object V_3,
                int V_4,
                object V_5,
                object V_6,
                object V_7)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (o)
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  // sequence point: <hidden>
  IL_0003:  ldloc.2
  IL_0004:  stloc.0
  // sequence point: <hidden>
  IL_0005:  ldloc.0
  IL_0006:  isinst     ""int""
  IL_000b:  brfalse.s  IL_004a
  IL_000d:  ldloc.0
  IL_000e:  unbox.any  ""int""
  IL_0013:  stloc.1
  // sequence point: <hidden>
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  sub
  IL_0017:  switch    (
        IL_0032,
        IL_0037,
        IL_0043,
        IL_003c,
        IL_0048)
  IL_0030:  br.s       IL_004a
  // sequence point: when o == null
  IL_0032:  ldarg.0
  IL_0033:  brfalse.s  IL_003c
  // sequence point: <hidden>
  IL_0035:  br.s       IL_003e
  // sequence point: when o == null
  IL_0037:  ldarg.0
  IL_0038:  brfalse.s  IL_003c
  // sequence point: <hidden>
  IL_003a:  br.s       IL_004a
  // sequence point: break;
  IL_003c:  br.s       IL_004e
  // sequence point: when o != null
  IL_003e:  ldarg.0
  IL_003f:  brtrue.s   IL_0048
  // sequence point: <hidden>
  IL_0041:  br.s       IL_004c
  // sequence point: when o != null
  IL_0043:  ldarg.0
  IL_0044:  brtrue.s   IL_0048
  // sequence point: <hidden>
  IL_0046:  br.s       IL_004a
  // sequence point: break;
  IL_0048:  br.s       IL_004e
  // sequence point: break;
  IL_004a:  br.s       IL_004e
  // sequence point: break;
  IL_004c:  br.s       IL_004e
  // sequence point: switch (o)
  IL_004e:  ldarg.0
  IL_004f:  stloc.s    V_5
  // sequence point: <hidden>
  IL_0051:  ldloc.s    V_5
  IL_0053:  stloc.3
  // sequence point: <hidden>
  IL_0054:  ldloc.3
  IL_0055:  isinst     ""int""
  IL_005a:  brfalse.s  IL_006d
  IL_005c:  ldloc.3
  IL_005d:  unbox.any  ""int""
  IL_0062:  stloc.s    V_4
  // sequence point: <hidden>
  IL_0064:  ldloc.s    V_4
  IL_0066:  ldc.i4.1
  IL_0067:  beq.s      IL_006b
  IL_0069:  br.s       IL_006d
  // sequence point: break;
  IL_006b:  br.s       IL_006f
  // sequence point: break;
  IL_006d:  br.s       IL_006f
  // sequence point: switch (o)
  IL_006f:  ldarg.0
  IL_0070:  stloc.s    V_7
  // sequence point: <hidden>
  IL_0072:  ldloc.s    V_7
  IL_0074:  stloc.s    V_6
  // sequence point: <hidden>
  IL_0076:  br.s       IL_0078
  // sequence point: break;
  IL_0078:  br.s       IL_007a
  // sequence point: }
  IL_007a:  ret
}");
            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" ordinal=""1"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""35"" offset=""378"" />
          <slot kind=""35"" offset=""378"" ordinal=""1"" />
          <slot kind=""1"" offset=""378"" />
          <slot kind=""35"" offset=""511"" />
          <slot kind=""1"" offset=""511"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" hidden=""true"" document=""1"" />
        <entry offset=""0x5"" hidden=""true"" document=""1"" />
        <entry offset=""0x14"" hidden=""true"" document=""1"" />
        <entry offset=""0x32"" startLine=""7"" startColumn=""20"" endLine=""7"" endColumn=""34"" document=""1"" />
        <entry offset=""0x35"" hidden=""true"" document=""1"" />
        <entry offset=""0x37"" startLine=""9"" startColumn=""20"" endLine=""9"" endColumn=""34"" document=""1"" />
        <entry offset=""0x3a"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" startLine=""10"" startColumn=""17"" endLine=""10"" endColumn=""23"" document=""1"" />
        <entry offset=""0x3e"" startLine=""11"" startColumn=""20"" endLine=""11"" endColumn=""34"" document=""1"" />
        <entry offset=""0x41"" hidden=""true"" document=""1"" />
        <entry offset=""0x43"" startLine=""13"" startColumn=""20"" endLine=""13"" endColumn=""34"" document=""1"" />
        <entry offset=""0x46"" hidden=""true"" document=""1"" />
        <entry offset=""0x48"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4a"" startLine=""16"" startColumn=""17"" endLine=""16"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4c"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4e"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""19"" document=""1"" />
        <entry offset=""0x51"" hidden=""true"" document=""1"" />
        <entry offset=""0x54"" hidden=""true"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0x6b"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""23"" document=""1"" />
        <entry offset=""0x6d"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""23"" document=""1"" />
        <entry offset=""0x6f"" startLine=""27"" startColumn=""9"" endLine=""27"" endColumn=""19"" document=""1"" />
        <entry offset=""0x72"" hidden=""true"" document=""1"" />
        <entry offset=""0x76"" hidden=""true"" document=""1"" />
        <entry offset=""0x78"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""23"" document=""1"" />
        <entry offset=""0x7a"" startLine=""32"" startColumn=""5"" endLine=""32"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(37172, "https://github.com/dotnet/roslyn/issues/37172")]
        [Fact]
        public void Patterns_SwitchStatement_Tuple()
        {
            string source = WithWindowsLineBreaks(@"
public class C
{
    static int F(int i)
    {
        switch (G())
        {
            case (1, 2): return 3;
            default: return 0;
        };
    }

    static (object, object) G() => (2, 3);
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);
            var cv = CompileAndVerify(c);

            cv.VerifyIL("C.F", @"
{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (System.ValueTuple<object, object> V_0,
                object V_1,
                int V_2,
                object V_3,
                int V_4,
                System.ValueTuple<object, object> V_5,
                int V_6)
  IL_0000:  nop
  IL_0001:  call       ""System.ValueTuple<object, object> C.G()""
  IL_0006:  stloc.s    V_5
  IL_0008:  ldloc.s    V_5
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldfld      ""object System.ValueTuple<object, object>.Item1""
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  isinst     ""int""
  IL_0018:  brfalse.s  IL_0048
  IL_001a:  ldloc.1
  IL_001b:  unbox.any  ""int""
  IL_0020:  stloc.2
  IL_0021:  ldloc.2
  IL_0022:  ldc.i4.1
  IL_0023:  bne.un.s   IL_0048
  IL_0025:  ldloc.0
  IL_0026:  ldfld      ""object System.ValueTuple<object, object>.Item2""
  IL_002b:  stloc.3
  IL_002c:  ldloc.3
  IL_002d:  isinst     ""int""
  IL_0032:  brfalse.s  IL_0048
  IL_0034:  ldloc.3
  IL_0035:  unbox.any  ""int""
  IL_003a:  stloc.s    V_4
  IL_003c:  ldloc.s    V_4
  IL_003e:  ldc.i4.2
  IL_003f:  beq.s      IL_0043
  IL_0041:  br.s       IL_0048
  IL_0043:  ldc.i4.3
  IL_0044:  stloc.s    V_6
  IL_0046:  br.s       IL_004d
  IL_0048:  ldc.i4.0
  IL_0049:  stloc.s    V_6
  IL_004b:  br.s       IL_004d
  IL_004d:  ldloc.s    V_6
  IL_004f:  ret
}
");

            c.VerifyPdb("C.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" ordinal=""1"" />
          <slot kind=""35"" offset=""11"" ordinal=""2"" />
          <slot kind=""35"" offset=""11"" ordinal=""3"" />
          <slot kind=""35"" offset=""11"" ordinal=""4"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" document=""1"" />
        <entry offset=""0x8"" hidden=""true"" document=""1"" />
        <entry offset=""0xb"" hidden=""true"" document=""1"" />
        <entry offset=""0x12"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" hidden=""true"" document=""1"" />
        <entry offset=""0x2c"" hidden=""true"" document=""1"" />
        <entry offset=""0x3c"" hidden=""true"" document=""1"" />
        <entry offset=""0x43"" startLine=""8"" startColumn=""26"" endLine=""8"" endColumn=""35"" document=""1"" />
        <entry offset=""0x48"" startLine=""9"" startColumn=""22"" endLine=""9"" endColumn=""31"" document=""1"" />
        <entry offset=""0x4d"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Tuples

        [Fact]
        public void SyntaxOffset_TupleDeconstruction()
        {
            var source = @"class C { int F() { (int a, (_, int c)) = (1, (2, 3)); return a + c; } }";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""7"" />
          <slot kind=""0"" offset=""18"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void TestDeconstruction()
        {
            var source = @"
public class C
{
    public static (int, int) F() => (1, 2);

    public static void Main()
    {
        int x, y;
        (x, y) = F();
        System.Console.WriteLine(x + y);
    }
}
";
            var c = CreateCompilation(source, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            v.VerifyIL("C.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0, //x
                int V_1) //y
  // sequence point: {
  IL_0000:  nop
  // sequence point: (x, y) = F();
  IL_0001:  call       ""System.ValueTuple<int, int> C.F()""
  IL_0006:  dup
  IL_0007:  ldfld      ""int System.ValueTuple<int, int>.Item1""
  IL_000c:  stloc.0
  IL_000d:  ldfld      ""int System.ValueTuple<int, int>.Item2""
  IL_0012:  stloc.1
  // sequence point: System.Console.WriteLine(x + y);
  IL_0013:  ldloc.0
  IL_0014:  ldloc.1
  IL_0015:  add
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  nop
  // sequence point: }
  IL_001c:  ret
}
", sequencePoints: "C.Main", source: source);
        }

        [Fact]
        public void SyntaxOffset_TupleParenthesized()
        {
            var source = @"class C { int F() { (int, (int, int)) x = (1, (2, 3)); return x.Item1 + x.Item2.Item1 + x.Item2.Item2; } }";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""20"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_TupleVarDefined()
        {
            var source = @"class C { int F() { var x = (1, 2); return x.Item1 + x.Item2; } }";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""6"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_TupleIgnoreDeconstructionIfVariableDeclared()
        {
            var source = @"class C { int F() { (int x, int y) a = (1, 2); return a.Item1 + a.Item2; } }";
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <tupleElementNames>
          <local elementNames=""|x|y"" slotIndex=""0"" localName=""a"" scopeStart=""0x0"" scopeEnd=""0x0"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""17"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>", options: SyntaxOffsetPdbValidationOptions);
        }

        #endregion

        #region OutVar

        [Fact]
        public void SyntaxOffset_OutVarInConstructor()
        {
            var source = @"
class B
{
    B(out int z) { z = 2; } 
}

class C
{
    int F = G(out var v1);    
    int P => G(out var v2);    

    C() 
    : base(out var v3)
    { 
        G(out var v4);
    }

    int G(out int x) 
    {
        x = 1;
        return 2;
    }
}
";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics(
                // (9,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.G(out int)'
                //     int F = G(out var v1);    
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "G").WithArguments("C.G(out int)").WithLocation(9, 13),
                // (13,7): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                //     : base(out var v3)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("object", "1").WithLocation(13, 7));
        }

        [Fact]
        public void SyntaxOffset_OutVarInMethod()
        {
            var source = @"class C { int G(out int x) { int z = 1; G(out var y); G(out var w); return x = y; } }";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.G", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""G"" parameterNames=""x"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""6"" />
          <slot kind=""0"" offset=""23"" />
          <slot kind=""0"" offset=""37"" />
          <slot kind=""temp"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_01()
        {
            var source = WithWindowsLineBreaks(
@"
class C : A
{ 
    int x = G(out var x);
    int y {get;} = G(out var y);

    C() : base(G(out var z))
    { 
    } 

    static int G(out int x) 
    {
        throw null;
    }
}

class A
{
    public A(int x) {}
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-36"" />
          <slot kind=""0"" offset=""-22"" />
          <slot kind=""0"" offset=""-3"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_02()
        {
            var source = WithWindowsLineBreaks(
@"
class C : A
{ 
    C() : base(G(out var x))
    { 
        int y = 1;
        y++;
    } 

    static int G(out int x) 
    {
        throw null;
    }
}

class A
{
    public A(int x) {}
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-3"" />
          <slot kind=""0"" offset=""16"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_03()
        {
            var source = WithWindowsLineBreaks(
@"
class C : A
{ 
    C() : base(G(out var x))
    => G(out var y);

    static int G(out int x) 
    {
        throw null;
    }
}

class A
{
    public A(int x) {}
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-3"" />
          <slot kind=""0"" offset=""13"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_04()
        {
            var source = WithWindowsLineBreaks(
@"
class C
{ 
    static int G(out int x) 
    {
        throw null;
    }
    static int F(System.Func<int> x) 
    {
        throw null;
    }

    C() 
    {
    }

#line 2000
    int y1 = G(out var z) + F(() => z);
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-25"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""-25"" />
          <lambda offset=""-2"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass2_0.<.ctor>b__0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass2_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_05()
        {
            var source = WithWindowsLineBreaks(
@"
class C
{ 
    static int G(out int x) 
    {
        throw null;
    }
    static int F(System.Func<int> x) 
    {
        throw null;
    }

#line 2000
    int y1 { get; } = G(out var z) + F(() => z);
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-25"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>5</methodOrdinal>
          <closure offset=""-25"" />
          <lambda offset=""-2"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass5_0.<.ctor>b__0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass5_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_06()
        {
            var source = WithWindowsLineBreaks(
@"
class C
{ 
    static int G(out int x) 
    {
        throw null;
    }
    static int F(System.Func<int> x) 
    {
        throw null;
    }

#line 2000
    int y1 = G(out var z) + F(() => z), y2 = G(out var u) + F(() => u);
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            var v = CompileAndVerify(c);
            v.VerifyIL("C..ctor", sequencePoints: "C..ctor", expectedIL: @"
{
  // Code size       90 (0x5a)
  .maxstack  4
  .locals init (C.<>c__DisplayClass4_0 V_0, //CS$<>8__locals0
                C.<>c__DisplayClass4_1 V_1) //CS$<>8__locals1
 ~IL_0000:  newobj     ""C.<>c__DisplayClass4_0..ctor()""
  IL_0005:  stloc.0
 -IL_0006:  ldarg.0
  IL_0007:  ldloc.0
  IL_0008:  ldflda     ""int C.<>c__DisplayClass4_0.z""
  IL_000d:  call       ""int C.G(out int)""
  IL_0012:  ldloc.0
  IL_0013:  ldftn      ""int C.<>c__DisplayClass4_0.<.ctor>b__0()""
  IL_0019:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_001e:  call       ""int C.F(System.Func<int>)""
  IL_0023:  add
  IL_0024:  stfld      ""int C.y1""
 ~IL_0029:  newobj     ""C.<>c__DisplayClass4_1..ctor()""
  IL_002e:  stloc.1
 -IL_002f:  ldarg.0
  IL_0030:  ldloc.1
  IL_0031:  ldflda     ""int C.<>c__DisplayClass4_1.u""
  IL_0036:  call       ""int C.G(out int)""
  IL_003b:  ldloc.1
  IL_003c:  ldftn      ""int C.<>c__DisplayClass4_1.<.ctor>b__1()""
  IL_0042:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0047:  call       ""int C.F(System.Func<int>)""
  IL_004c:  add
  IL_004d:  stfld      ""int C.y2""
  IL_0052:  ldarg.0
  IL_0053:  call       ""object..ctor()""
  IL_0058:  nop
  IL_0059:  ret
}
");

            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-52"" />
          <slot kind=""30"" offset=""-25"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>4</methodOrdinal>
          <closure offset=""-52"" />
          <closure offset=""-25"" />
          <lambda offset=""-29"" closure=""0"" />
          <lambda offset=""-2"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass4_0.<.ctor>b__0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass4_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass4_1.<.ctor>b__1", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass4_1"" name=""&lt;.ctor&gt;b__1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""G"" parameterNames=""x"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInInitializers_07()
        {
            var source = WithWindowsLineBreaks(
@"
class C : A
{ 
#line 2000
    C() : base(G(out var z)+ F(() => z))
    { 
    } 

    static int G(out int x) 
    {
        throw null;
    }
    static int F(System.Func<int> x) 
    {
        throw null;
    }
}

class A
{
    public A(int x) {}
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""30"" offset=""-1"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""-1"" />
          <lambda offset=""-3"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass0_0.<.ctor>b__0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;.ctor&gt;b__0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInQuery_01()
        {
            var source = WithWindowsLineBreaks(
@"
using System.Linq;

class C
{ 
    C()
    {
        var q = from a in new [] {1} 
                where 
                      G(out var x1) > a  
                select a;
    }

    static int G(out int x) 
    {
        throw null;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""88"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c.<.ctor>b__0_0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__0_0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""98"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInQuery_02()
        {
            var source = WithWindowsLineBreaks(
@"
using System.Linq;

class C
{ 
    C()
#line 2000
    {
        var q = from a in new [] {1} 
                where 
                      G(out var x1) > F(() => x1)  
                select a;
    }

    static int G(out int x) 
    {
        throw null;
    }
    static int F(System.Func<int> x) 
    {
        throw null;
    }
}
");

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor", @"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <closure offset=""88"" />
          <lambda offset=""88"" />
          <lambda offset=""112"" closure=""0"" />
        </encLambdaMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c.<.ctor>b__0_0", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__0_0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""88"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);

            c.VerifyPdb("C+<>c__DisplayClass0_0.<.ctor>b__1", @"
<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c__DisplayClass0_0"" name=""&lt;.ctor&gt;b__1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInSwitchExpression()
        {
            var source = @"class C { static object G() => N(out var x) switch { null => x switch {1 =>  1, _ => 2 }, _ => 1 }; static object N(out int x) { x = 1; return null; } }";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.G", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""13"" />
          <slot kind=""temp"" />
          <slot kind=""35"" offset=""16"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        [Fact]
        public void SyntaxOffset_OutVarInPrimaryConstructorInitializer()
        {
            var source = @"
class B(int x, int y)
{
}

class C(int x) : B(F(out var y), y)
{
    int Z = F(out var z);
    static int F(out int a) => a = 1;
}";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""B"" name="".ctor"" parameterNames=""x, y"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
    </method>
    <method containingType=""C"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""x, y"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""-6"" />
          <slot kind=""0"" offset=""-20"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""C"" name=""F"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""B"" methodName="".ctor"" parameterNames=""x, y"" />
      </customDebugInfo>
    </method>
  </methods>
</symbols>
", options: SyntaxOffsetPdbValidationOptions);
        }

        #endregion

        #region Primary Constructors

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/63299")]
        public void SequencePoints_PrimaryConstructor_ExplicitBaseInitializer()
        {
            var source = @"
class B() : object()
{
}

class C(int x) : B()
{
    int y = 1;
}";

            var c = CompileAndVerify(source);
            c.VerifyMethodBody("B..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  // sequence point: object()
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");
            c.VerifyMethodBody("C..ctor", @"
 {
  // Code size       14 (0xe)
  .maxstack  2
  // sequence point: int y = 1;
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  stfld      ""int C.y""
  // sequence point: B()
  IL_0007:  ldarg.0
  IL_0008:  call       ""B..ctor()""
  IL_000d:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/63299")]
        public void SequencePoints_PrimaryConstructor_ImplicitBaseInitializer()
        {
            var source = @"
class B()
{
}

class C(int x) : B
{
}";

            var c = CompileAndVerify(source);
            c.VerifyMethodBody("B..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  // sequence point: B()
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ret
}
");
            c.VerifyMethodBody("C..ctor", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  // sequence point: C(int x)
  IL_0000:  ldarg.0
  IL_0001:  call       ""B..ctor()""
  IL_0006:  ret
}
");
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/63299")]
        public void SequencePoints_RecordConstructors_ModifiersAndDefault()
        {
            var source = @"
record C<T>([A]in T P = default) where T : struct;

class A : System.Attribute {}
" + IsExternalInitTypeDefinition;

            var c = CompileAndVerify(source, verify: Verification.Skipped);

            // primary constructor
            c.VerifyMethodBody("C<T>..ctor(in T)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldobj      ""T""
  IL_0007:  stfld      ""T C<T>.<P>k__BackingField""
  // sequence point: C<T>([A]in T P = default)
  IL_000c:  ldarg.0
  IL_000d:  call       ""object..ctor()""
  IL_0012:  ret
}
");
            // copy constructor
            c.VerifyMethodBody("C<T>..ctor(C<T>)", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  call       ""object..ctor()""
  IL_0006:  ldarg.0
  IL_0007:  ldarg.1
  IL_0008:  ldfld      ""T C<T>.<P>k__BackingField""
  IL_000d:  stfld      ""T C<T>.<P>k__BackingField""
  // sequence point: C<T>
  IL_0012:  ret
}
");
            // primary auto-property getter
            c.VerifyMethodBody("C<T>.P.get", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  // sequence point: in T P
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.<P>k__BackingField""
  IL_0006:  ret
}
");
            // primary auto-property setter
            c.VerifyMethodBody("C<T>.P.init", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  // sequence point: in T P
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""T C<T>.<P>k__BackingField""
  IL_0007:  ret
}
");
        }

        [Theory]
        [InlineData("int[] P = default", "int[] P")]
        [InlineData("[A]int[] P", "int[] P")]
        [InlineData("params int[] P", "params int[] P")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/63299")]
        public void SequencePoints_RecordPropertyAccessors(string parameterSyntax, string expectedSpan)
        {
            var source =
                "record C(" + parameterSyntax + ");" +
                "class A : System.Attribute { }" +
                IsExternalInitTypeDefinition;

            var c = CompileAndVerify(source, verify: Verification.Skipped);

            // primary auto-property getter
            c.VerifyMethodBody("C.P.get", $@"
{{
  // Code size        7 (0x7)
  .maxstack  1
  // sequence point: {expectedSpan}
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int[] C.<P>k__BackingField""
  IL_0006:  ret
}}
");
            // primary auto-property setter
            c.VerifyMethodBody("C.P.init", $@"
{{
  // Code size        8 (0x8)
  .maxstack  2
  // sequence point: {expectedSpan}
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  stfld      ""int[] C.<P>k__BackingField""
  IL_0007:  ret
}}
");
        }

        #endregion

        [WorkItem(4370, "https://github.com/dotnet/roslyn/issues/4370")]
        [Fact]
        public void HeadingHiddenSequencePointsPickUpDocumentFromVisibleSequencePoint()
        {
            var source = WithWindowsLineBreaks(
@"#line 1 ""C:\Async.cs""
#pragma checksum ""C:\Async.cs"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""DBEB2A067B2F0E0D678A002C587A2806056C3DCE""

using System.Threading.Tasks;

public class C
{
    public async void M1()
    {
    }
}
");

            var tree = SyntaxFactory.ParseSyntaxTree(source, encoding: Encoding.UTF8, path: "HIDDEN.cs");
            var c = CSharpCompilation.Create("Compilation", new[] { tree }, new[] { MscorlibRef_v46 }, options: TestOptions.DebugDll.WithDebugPlusMode(true));

            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name=""C:\Async.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
    <file id=""2"" name=""HIDDEN.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8A-92-EE-2F-D6-6F-C0-69-F4-A8-54-CB-11-BE-A3-06-76-2C-9C-98"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M1"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M1&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""C+&lt;M1&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0xa"" hidden=""true"" document=""1"" />
        <entry offset=""0x22"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x2a"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x37"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <catchHandler offset=""0xa"" />
        <kickoffMethod declaringType=""C"" methodName=""M1"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
", format: DebugInformationFormat.Pdb);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""HIDDEN.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8A-92-EE-2F-D6-6F-C0-69-F4-A8-54-CB-11-BE-A3-06-76-2C-9C-98"" />
    <file id=""2"" name=""C:\Async.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M1&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""2"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""2"" />
        <entry offset=""0xa"" hidden=""true"" document=""2"" />
        <entry offset=""0x22"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""2"" />
        <entry offset=""0x2a"" hidden=""true"" document=""2"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0xa"" />
        <kickoffMethod declaringType=""C"" methodName=""M1"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(12923, "https://github.com/dotnet/roslyn/issues/12923")]
        [Fact]
        public void SequencePointsForConstructorWithHiddenInitializer()
        {
            string initializerSource = WithWindowsLineBreaks(@"
#line hidden
partial class C
{
    int i = 42;
}
");

            string constructorSource = WithWindowsLineBreaks(@"
partial class C
{
    C()
    {
    }
}
");

            var c = CreateCompilation(
                new[] { Parse(initializerSource, "initializer.cs"), Parse(constructorSource, "constructor.cs") },
                options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""constructor.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""EA-D6-0A-16-6C-6A-BC-C1-5D-98-0F-B7-4B-78-13-93-FB-C7-C2-5A"" />
    <file id=""2"" name=""initializer.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""84-32-24-D7-FE-32-63-BA-41-D5-17-A2-D5-90-23-B8-12-3C-AF-D5"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x8"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""8"" document=""1"" />
        <entry offset=""0xf"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x10"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
", format: DebugInformationFormat.Pdb);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""initializer.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""84-32-24-D7-FE-32-63-BA-41-D5-17-A2-D5-90-23-B8-12-3C-AF-D5"" />
    <file id=""2"" name=""constructor.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""EA-D6-0A-16-6C-6A-BC-C1-5D-98-0F-B7-4B-78-13-93-FB-C7-C2-5A"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""2"" />
        <entry offset=""0x8"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""8"" document=""2"" />
        <entry offset=""0xf"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""2"" />
        <entry offset=""0x10"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        [Fact]
        public void LocalFunctionSequencePoints()
        {
            string source = WithWindowsLineBreaks(
@"class Program
{
    static int Main(string[] args)
    {                                                // 4
        int Local1(string[] a)
            =>
            a.Length;                                // 7
        int Local2(string[] a)
        {                                            // 9
            return a.Length;                         // 10
        }                                            // 11
        return Local1(args) + Local2(args);          // 12
    }                                                // 13
}");
            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""115"" />
          <lambda offset=""202"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""44"" document=""1"" />
        <entry offset=""0x13"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Program"" name=""&lt;Main&gt;g__Local1|0_0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""13"" endLine=""7"" endColumn=""21"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Program"" name=""&lt;Main&gt;g__Local2|0_1"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""202"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""29"" document=""1"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void SwitchInAsyncMethod()
        {
            var source = @"
using System;

class Program
{
    public static async void Test()
    {
        int i = 0;

        switch (i)
        {
            case 1:
                break;
        }
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       89 (0x59)
  .maxstack  2
  .locals init (int V_0,
                int V_1,
                System.Exception V_2)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0;
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: switch (i)
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0016:  stloc.1
    // sequence point: <hidden>
    IL_0017:  ldloc.1
    IL_0018:  stfld      ""int Program.<Test>d__0.<>s__2""
    // sequence point: <hidden>
    IL_001d:  ldarg.0
    IL_001e:  ldfld      ""int Program.<Test>d__0.<>s__2""
    IL_0023:  ldc.i4.1
    IL_0024:  beq.s      IL_0028
    IL_0026:  br.s       IL_002a
    // sequence point: break;
    IL_0028:  br.s       IL_002a
    // sequence point: <hidden>
    IL_002a:  leave.s    IL_0044
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_002c:  stloc.2
    IL_002d:  ldarg.0
    IL_002e:  ldc.i4.s   -2
    IL_0030:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_0035:  ldarg.0
    IL_0036:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_003b:  ldloc.2
    IL_003c:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0041:  nop
    IL_0042:  leave.s    IL_0058
  }
  // sequence point: }
  IL_0044:  ldarg.0
  IL_0045:  ldc.i4.s   -2
  IL_0047:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_004c:  ldarg.0
  IL_004d:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_0052:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_0057:  nop
  IL_0058:  ret
}", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void WhileInAsyncMethod()
        {
            var source = @"
using System;

class Program
{
    public static async void Test()
    {
        int i = 0;
        while (i == 1)
            Console.WriteLine();
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       83 (0x53)
  .maxstack  2
  .locals init (int V_0,
                bool V_1,
                System.Exception V_2)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0;
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: <hidden>
    IL_000f:  br.s       IL_0017
    // sequence point: Console.WriteLine();
    IL_0011:  call       ""void System.Console.WriteLine()""
    IL_0016:  nop
    // sequence point: while (i == 1)
    IL_0017:  ldarg.0
    IL_0018:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_001d:  ldc.i4.1
    IL_001e:  ceq
    IL_0020:  stloc.1
    // sequence point: <hidden>
    IL_0021:  ldloc.1
    IL_0022:  brtrue.s   IL_0011
    IL_0024:  leave.s    IL_003e
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0026:  stloc.2
    IL_0027:  ldarg.0
    IL_0028:  ldc.i4.s   -2
    IL_002a:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_002f:  ldarg.0
    IL_0030:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_0035:  ldloc.2
    IL_0036:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_003b:  nop
    IL_003c:  leave.s    IL_0052
  }
  // sequence point: }
  IL_003e:  ldarg.0
  IL_003f:  ldc.i4.s   -2
  IL_0041:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0046:  ldarg.0
  IL_0047:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_0051:  nop
  IL_0052:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ForInAsyncMethod()
        {
            var source = @"
using System;

class Program
{
    public static async void Test()
    {
        for (int i = 0; i > 1; i--)
            Console.WriteLine();
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       99 (0x63)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                bool V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = 0
    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: <hidden>
    IL_000f:  br.s       IL_0027
    // sequence point: Console.WriteLine();
    IL_0011:  call       ""void System.Console.WriteLine()""
    IL_0016:  nop
    // sequence point: i--
    IL_0017:  ldarg.0
    IL_0018:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_001d:  stloc.1
    IL_001e:  ldarg.0
    IL_001f:  ldloc.1
    IL_0020:  ldc.i4.1
    IL_0021:  sub
    IL_0022:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: i > 1
    IL_0027:  ldarg.0
    IL_0028:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_002d:  ldc.i4.1
    IL_002e:  cgt
    IL_0030:  stloc.2
    // sequence point: <hidden>
    IL_0031:  ldloc.2
    IL_0032:  brtrue.s   IL_0011
    IL_0034:  leave.s    IL_004e
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0036:  stloc.3
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.s   -2
    IL_003a:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_003f:  ldarg.0
    IL_0040:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_0045:  ldloc.3
    IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_004b:  nop
    IL_004c:  leave.s    IL_0062
  }
  // sequence point: }
  IL_004e:  ldarg.0
  IL_004f:  ldc.i4.s   -2
  IL_0051:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0056:  ldarg.0
  IL_0057:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_005c:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_0061:  nop
  IL_0062:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
        }

        [Fact]
        [WorkItem(12564, "https://github.com/dotnet/roslyn/issues/12564")]
        public void ForWithInnerLocalsInAsyncMethod()
        {
            var source = @"
using System;

class Program
{
    public static async void Test()
    {
        for (int i = M(out var x); i > 1; i--)
            Console.WriteLine();
    }
    public static int M(out int x) { x = 0; return 0; }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size      109 (0x6d)
  .maxstack  3
  .locals init (int V_0,
                int V_1,
                bool V_2,
                System.Exception V_3)
  // sequence point: <hidden>
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Test>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    // sequence point: {
    IL_0007:  nop
    // sequence point: int i = M(out var x)
    IL_0008:  ldarg.0
    IL_0009:  ldarg.0
    IL_000a:  ldflda     ""int Program.<Test>d__0.<x>5__2""
    IL_000f:  call       ""int Program.M(out int)""
    IL_0014:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: <hidden>
    IL_0019:  br.s       IL_0031
    // sequence point: Console.WriteLine();
    IL_001b:  call       ""void System.Console.WriteLine()""
    IL_0020:  nop
    // sequence point: i--
    IL_0021:  ldarg.0
    IL_0022:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0027:  stloc.1
    IL_0028:  ldarg.0
    IL_0029:  ldloc.1
    IL_002a:  ldc.i4.1
    IL_002b:  sub
    IL_002c:  stfld      ""int Program.<Test>d__0.<i>5__1""
    // sequence point: i > 1
    IL_0031:  ldarg.0
    IL_0032:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0037:  ldc.i4.1
    IL_0038:  cgt
    IL_003a:  stloc.2
    // sequence point: <hidden>
    IL_003b:  ldloc.2
    IL_003c:  brtrue.s   IL_001b
    IL_003e:  leave.s    IL_0058
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0040:  stloc.3
    IL_0041:  ldarg.0
    IL_0042:  ldc.i4.s   -2
    IL_0044:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_0049:  ldarg.0
    IL_004a:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_004f:  ldloc.3
    IL_0050:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0055:  nop
    IL_0056:  leave.s    IL_006c
  }
  // sequence point: }
  IL_0058:  ldarg.0
  IL_0059:  ldc.i4.s   -2
  IL_005b:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0060:  ldarg.0
  IL_0061:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_0066:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_006b:  nop
  IL_006c:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(23525, "https://github.com/dotnet/roslyn/issues/23525")]
        public void InvalidCharacterInPdbPath()
        {
            using (var outStream = Temp.CreateFile().Open())
            {
                var compilation = CreateCompilation("");
                var result = compilation.Emit(outStream, options: new EmitOptions(pdbFilePath: "test\\?.pdb", debugInformationFormat: DebugInformationFormat.Embedded));

                // This is fine because EmitOptions just controls what is written into the PE file and it's 
                // valid for this to be an illegal file name (path map can easily create these).
                Assert.True(result.Success);
            }
        }

        [Fact]
        [WorkItem(38954, "https://github.com/dotnet/roslyn/issues/38954")]
        public void FilesOneWithNoMethodBody()
        {
            string source1 = WithWindowsLineBreaks(@"
using System;

class C
{
    public static void Main()
    {
        Console.WriteLine();
    }
}
");
            string source2 = WithWindowsLineBreaks(@"
// no code
");

            var tree1 = Parse(source1, "f:/build/goo.cs");
            var tree2 = Parse(source2, "f:/build/nocode.cs");
            var c = CreateCompilation(new[] { tree1, tree2 }, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""f:/build/goo.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""5D-7D-CF-1B-79-12-0E-0A-80-13-E0-98-7E-5C-AA-3B-63-D8-7E-4F"" />
    <file id=""2"" name=""f:/build/nocode.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8B-1D-3F-75-E0-A8-8F-90-B2-D3-52-CF-71-9B-17-29-3C-70-7A-42"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""29"" document=""1"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        [WorkItem(38954, "https://github.com/dotnet/roslyn/issues/38954")]
        public void SingleFileWithNoMethodBody()
        {
            string source = WithWindowsLineBreaks(@"
// no code
");

            var tree = Parse(source, "f:/build/nocode.cs");
            var c = CreateCompilation(new[] { tree }, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""f:/build/nocode.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8B-1D-3F-75-E0-A8-8F-90-B2-D3-52-CF-71-9B-17-29-3C-70-7A-42"" />
  </files>
  <methods />
</symbols>
");
        }

        [Fact]
        public void CompilerInfo_WindowsPdb()
        {
            var compilerAssembly = typeof(Compilation).Assembly;
            var fileVersion = Version.Parse(compilerAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version);
            var versionString = compilerAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            var source = "class C { void F() {} }";

            var c = CreateCompilation(
                new[] { Parse(source, "a.cs") },
                options: TestOptions.DebugDll);

            c.VerifyPdb($@"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""CB-D0-82-32-17-65-3C-22-44-D1-38-EA-BC-88-09-CF-A1-35-1D-09"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""20"" endLine=""1"" endColumn=""21"" document=""1"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""22"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
  <compilerInfo version=""{fileVersion}"" name=""C# - {versionString}"" />
</symbols>
", options: PdbValidationOptions.IncludeModuleDebugInfo, format: DebugInformationFormat.Pdb);
        }
    }
}
