﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader.Tools;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBTests : CSharpPDBTestBase
    {
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

            var hash1 = CryptographicHashProvider.ComputeSha1(Encoding.Unicode.GetBytesWithPreamble(tree1.ToString()));
            var hash3 = CryptographicHashProvider.ComputeSha1(new UTF8Encoding(true, false).GetBytesWithPreamble(tree3.ToString()));
            var hash4 = CryptographicHashProvider.ComputeSha1(new UTF8Encoding(false, false).GetBytesWithPreamble(tree4.ToString()));

            var checksum1 = string.Concat(hash1.Select(b => string.Format("{0,2:X}", b) + ", "));
            var checksum3 = string.Concat(hash3.Select(b => string.Format("{0,2:X}", b) + ", "));
            var checksum4 = string.Concat(hash4.Select(b => string.Format("{0,2:X}", b) + ", "));

            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""Foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""" + checksum1 + @""" />
    <file id=""2"" name=""Bar.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""" + checksum3 + @""" />
    <file id=""3"" name=""Baz.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""" + checksum4 + @""" />
  </files>
</symbols>", options: PdbToXmlOptions.ExcludeMethods);
        }

        [Fact, WorkItem(846584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/846584")]
        public void RelativePathForExternalSource_Sha1()
        {
            var text1 = @"
#pragma checksum ""..\Test2.cs"" ""{406ea660-64cf-4c82-b6f0-42d48172a799}"" ""BA8CBEA9C2EFABD90D53B616FB80A081""

public class C
{
    public void InitializeComponent() {
        #line 4 ""..\Test2.cs""
        InitializeComponent();
        #line default
    }
}
";

            var compilation = CreateStandardCompilation(
                new[] { Parse(text1, @"C:\Folder1\Folder2\Test1.cs") },
                options: TestOptions.DebugDll.WithSourceReferenceResolver(SourceFileResolver.Default));

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""C:\Folder1\Folder2\Test1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""40, A6, 20,  2, 2E, 60, 7D, 4F, 2D, A8, F4, A6, ED, 2E,  E, 49, 8D, 9F, D7, EB, "" />
    <file id=""2"" name=""C:\Folder1\Test2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""406ea660-64cf-4c82-b6f0-42d48172a799"" checkSum=""BA, 8C, BE, A9, C2, EF, AB, D9,  D, 53, B6, 16, FB, 80, A0, 81, "" />
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

        [Fact]
        public void SymWriterErrors()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateStandardCompilation(source0, options: TestOptions.DebugDll);

            // Verify full metadata contains expected rows.
            using (MemoryStream peStream = new MemoryStream(), pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    peStream: peStream,
                    metadataPEStream: null,
                    pdbStream: pdbStream,
                    xmlDocumentationStream: null,
                    cancellationToken: default(CancellationToken),
                    win32Resources: null,
                    manifestResources: null,
                    options: null,
                    debugEntryPoint: null,
                    sourceLinkStream: null,
                    embeddedTexts: null,
                    testData: new CompilationTestData() { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The method or operation is not implemented.'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(new NotImplementedException().Message));

                Assert.False(result.Success);
            }
        }

        [Fact]
        public void SymWriterErrors2()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateStandardCompilation(source0, options: TestOptions.DebugDll);

            // Verify full metadata contains expected rows.
            using (MemoryStream peStream = new MemoryStream(), pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    peStream: peStream,
                    metadataPEStream: null,
                    pdbStream: pdbStream,
                    xmlDocumentationStream: null,
                    cancellationToken: default(CancellationToken),
                    win32Resources: null,
                    manifestResources: null,
                    options: null,
                    debugEntryPoint: null,
                    sourceLinkStream: null,
                    embeddedTexts: null,
                    testData: new CompilationTestData() { SymWriterFactory = () => new object() });

                var libName = $"Microsoft.DiaSymReader.Native.{(IntPtr.Size == 4 ? "x86" : "amd64")}.dll";

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'The version of Windows PDB writer is older than required: 'Microsoft.DiaSymReader.Native.x86.dll''
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(string.Format(CodeAnalysisResources.SymWriterOlderVersionThanRequired, libName)));

                Assert.False(result.Success);
            }
        }

        [Fact]
        public void SymWriterErrors3()
        {
            var source0 =
@"class C
{
}";
            var compilation = CreateStandardCompilation(source0, options: TestOptions.DebugDll.WithDeterministic(true));

            // Verify full metadata contains expected rows.
            using (MemoryStream peStream = new MemoryStream(), pdbStream = new MemoryStream())
            {
                var result = compilation.Emit(
                    peStream: peStream,
                    metadataPEStream: null,
                    pdbStream: pdbStream,
                    xmlDocumentationStream: null,
                    cancellationToken: default(CancellationToken),
                    win32Resources: null,
                    manifestResources: null,
                    options: null,
                    debugEntryPoint: null,
                    sourceLinkStream: null,
                    embeddedTexts: null,
                    testData: new CompilationTestData() { SymWriterFactory = () => new MockSymUnmanagedWriter() });

                var libName = $"Microsoft.DiaSymReader.Native.{(IntPtr.Size == 4 ? "x86" : "amd64")}.dll";

                result.Diagnostics.Verify(
                    // error CS0041: Unexpected error writing debug information -- 'Windows PDB writer doesn't support deterministic compilation -- could not find {0}'
                    Diagnostic(ErrorCode.FTL_DebugEmitFailure).WithArguments(string.Format(CodeAnalysisResources.SymWriterNotDeterministic, libName)));

                Assert.False(result.Success);
            }
        }

        [Fact, WorkItem(1067635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067635")]
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

            var debug = CreateStandardCompilation(source, new[] { CSharpRef, SystemCoreRef }, options: TestOptions.DebugWinMD);
            debug.VerifyPdb(@"
<symbols>
    <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" />
        <entry offset=""0xa"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" />
        <entry offset=""0xb"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" />
        <entry offset=""0x1f"" hidden=""true"" />
        <entry offset=""0x24"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" />
        <entry offset=""0xe6"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0xe7"" hidden=""true"" />
        <entry offset=""0xeb"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" />
        <entry offset=""0xf4"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
</symbols>", format: DebugInformationFormat.Pdb);

            var release = CreateStandardCompilation(source, new[] { CSharpRef, SystemCoreRef }, options: TestOptions.ReleaseWinMD);
            release.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""19"" />
        <entry offset=""0x9"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""42"" />
        <entry offset=""0x1d"" hidden=""true"" />
        <entry offset=""0x22"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" />
        <entry offset=""0x26"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""45"" />
        <entry offset=""0xdd"" hidden=""true"" />
        <entry offset=""0xe1"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" />
        <entry offset=""0xea"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xeb"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
        <local name=""b"" il_index=""1"" il_start=""0x0"" il_end=""0xeb"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);
        }

        [Fact, WorkItem(1067635, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067635")]
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

            var debug = CreateStandardCompilation(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugWinMD);
            debug.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""35"" />
        <entry offset=""0x9"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xa"">
        <local name=""o"" il_index=""0"" il_start=""0x0"" il_end=""0xa"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            var release = CreateStandardCompilation(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.ReleaseWinMD);
            release.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);
        }

        [Fact]
        public void DuplicateDocuments()
        {
            var source1 = @"class C { static void F() { } }";
            var source2 = @"class D { static void F() { } }";

            var tree1 = Parse(source1, @"foo.cs");
            var tree2 = Parse(source2, @"foo.cs");

            var comp = CreateStandardCompilation(new[] { tree1, tree2 });

            // the first file wins (checksum CB 22 ...)
            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""CB, 22, D8,  3, D3, 27, 32, 64, 2C, BC, 7D, 67, 5D, E3, CB, AC, D1, 64, 25, 83, "" />
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

            var c = CreateStandardCompilation(source, options: TestOptions.DebugDll);
            var f = c.GetMember<MethodSymbol>("C.F");

            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""F"" />
  <methods/>
</symbols>", debugEntryPoint: f, options: PdbToXmlOptions.ExcludeScopes | PdbToXmlOptions.ExcludeSequencePoints | PdbToXmlOptions.ExcludeCustomDebugInformation);

            var peReader = new PEReader(c.EmitToArray(debugEntryPoint: f));
            int peEntryPointToken = peReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress;

            Assert.Equal(0, peEntryPointToken);
        }

        [Fact]
        public void CustomDebugEntryPoint_EXE()
        {
            var source = @"class M { static void Main() { } } class C { static void F<S>() { } }";

            var c = CreateStandardCompilation(source, options: TestOptions.DebugExe);
            var f = c.GetMember<MethodSymbol>("C.F");

            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""F"" />
  <methods/>
</symbols>", debugEntryPoint: f, options: PdbToXmlOptions.ExcludeScopes | PdbToXmlOptions.ExcludeSequencePoints | PdbToXmlOptions.ExcludeCustomDebugInformation);

            var peReader = new PEReader(c.EmitToArray(debugEntryPoint: f));
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

            var c1 = CreateStandardCompilation(source1, options: TestOptions.DebugDll);
            var c2 = CreateStandardCompilation(source2, options: TestOptions.DebugDll);

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

            var result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: f2);
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_t_g_int);
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_int_g);
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));

            result = c1.Emit(new MemoryStream(), new MemoryStream(), debugEntryPoint: d_int_g_int);
            result.Diagnostics.Verify(
                // error CS8096: Debug entry point must be a definition of a source method in the current compilation.
                Diagnostic(ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition));
        }

        #endregion

        #region Method Bodies

        [Fact]
        public void TestBasic()
        {
            var source = @"
class Program
{
    Program() { }

    static void Main(string[] args)
    {
        Program p = new Program();
    }
}
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"
<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""35"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""28"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""58"" />
        <entry offset=""0x14"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x15"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""14"" />
        <entry offset=""0x16"" startLine=""12"" startColumn=""17"" endLine=""12"" endColumn=""33"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""60"" />
        <entry offset=""0x29"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" />
        <entry offset=""0x2a"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""14"" />
        <entry offset=""0x2b"" startLine=""17"" startColumn=""17"" endLine=""17"" endColumn=""31"" />
        <entry offset=""0x2d"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""62"" />
        <entry offset=""0x3e"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" />
        <entry offset=""0x3f"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""45"" />
        <entry offset=""0x4a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" />
        <entry offset=""0x4b"" startLine=""22"" startColumn=""5"" endLine=""22"" endColumn=""6"" />
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

        [WorkItem(7244, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/7244")]
        [Fact]
        public void ConstructorsWithoutInitializers()
        {
            var source =
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor",
@"<symbols>
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
        <entry offset=""0x0"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""8"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x8"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""16"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x8"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""22"" />
        <entry offset=""0xa"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
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
            var source =
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C..ctor",
@"<symbols>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""18"" />
        <entry offset=""0xb"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""8"" />
        <entry offset=""0x12"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x13"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""18"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""16"" />
        <entry offset=""0x12"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
        <entry offset=""0x13"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""22"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""19"" />
        <entry offset=""0x3"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""30"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""19"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""30"" />
        <entry offset=""0xa"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""19"" />
        <entry offset=""0x3"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""30"" />
        <entry offset=""0xa"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" />
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
            string source =
@"using System;
class C
{
    static Func<object, int> F = x =>
    {
        Func<object, int> f = o => 1;
        Func<Func<object, int>, Func<object, int>> g = h => y => h(y);
        return g(f)(null);
    };
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""9"" endColumn=""7"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x16"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c__DisplayClass2_0"" name=""&lt;.cctor&gt;b__3"" parameterNames=""y"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".cctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""66"" endLine=""7"" endColumn=""70"" />
      </sequencePoints>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""38"" />
        <entry offset=""0x21"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""71"" />
        <entry offset=""0x41"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""27"" />
        <entry offset=""0x51"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""36"" endLine=""6"" endColumn=""37"" />
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
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0xd"" startLine=""7"" startColumn=""61"" endLine=""7"" endColumn=""70"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1a"">
        <local name=""CS$&lt;&gt;8__locals0"" il_index=""0"" il_start=""0x0"" il_end=""0x1a"" attributes=""0"" />
      </scope>
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // verify that both syntax offsets are the same
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""44"" endLine=""4"" endColumn=""45"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""57"" endLine=""4"" endColumn=""67"" />
        <entry offset=""0x3"" startLine=""4"" startColumn=""79"" endLine=""4"" endColumn=""80"" />
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""32"" endLine=""5"" endColumn=""33"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""45"" endLine=""5"" endColumn=""55"" />
        <entry offset=""0x3"" startLine=""5"" startColumn=""67"" endLine=""5"" endColumn=""68"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // verify that syntax offsets of both .cctor's are the same
            c.VerifyPdb("C1..cctor", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""39"" />
        <entry offset=""0x15"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""63"" />
        <entry offset=""0x2a"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""39"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x40"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");

            c.VerifyPdb("C2..cctor", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""39"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""51"" />
        <entry offset=""0x2a"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""39"" />
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
            var source = @"
class Program
{
    static int Main()
    {
        return 1;
    }
}
";

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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""18"" />
        <entry offset=""0x5"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Return_Property1()
        {
            var source = @"
class C
{
    static int P
    {
        get { return 1; }
    }
}
";

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
        <entry offset=""0x0"" startLine=""6"" startColumn=""13"" endLine=""6"" endColumn=""14"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""15"" endLine=""6"" endColumn=""24"" />
        <entry offset=""0x5"" startLine=""6"" startColumn=""25"" endLine=""6"" endColumn=""26"" />
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
            var source = @"
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
";
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""33"" />
        <entry offset=""0x8"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""22"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""26"" />
        <entry offset=""0xd"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
        <entry offset=""0xe"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""22"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Method", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""23"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""15"" />
        <entry offset=""0x5"" hidden=""true"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x9"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""31"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" />
        <entry offset=""0x16"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x19"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x1a"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""32"" />
        <entry offset=""0x20"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""23"" />
        <entry offset=""0x23"" hidden=""true"" />
        <entry offset=""0x25"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""14"" />
        <entry offset=""0x26"" startLine=""19"" startColumn=""17"" endLine=""19"" endColumn=""26"" />
        <entry offset=""0x2a"" startLine=""19"" startColumn=""28"" endLine=""19"" endColumn=""33"" />
        <entry offset=""0x2d"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""45"" />
        <entry offset=""0x35"" startLine=""21"" startColumn=""17"" endLine=""21"" endColumn=""27"" />
        <entry offset=""0x3c"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""14"" />
        <entry offset=""0x3d"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""28"" />
        <entry offset=""0x45"" hidden=""true"" />
        <entry offset=""0x49"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""23"" />
        <entry offset=""0x4f"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" />
        <entry offset=""0x50"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" />
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);

            // Offset 0x01 should be:
            //  <entry offset=""0x1"" hidden=""true"" />
            // Move original offset 0x01 to 0x33
            //  <entry offset=""0x33"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" />
            // 
            // Note: 16707566 == 0x00FEEFEE
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""SeqPointForWhile"" methodName=""Main"" />
  <methods>
    <method containingType=""SeqPointForWhile"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" />
        <entry offset=""0xf"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
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
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" />
        <entry offset=""0xc"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" />
        <entry offset=""0x11"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" />
        <entry offset=""0x13"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""27"" />
        <entry offset=""0x1a"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" />
        <entry offset=""0x1c"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""27"" />
        <entry offset=""0x1d"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""38"" />
        <entry offset=""0x22"" startLine=""31"" startColumn=""17"" endLine=""31"" endColumn=""23"" />
        <entry offset=""0x24"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""22"" />
        <entry offset=""0x28"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""20"" />
        <entry offset=""0x2f"" startLine=""35"" startColumn=""5"" endLine=""35"" endColumn=""6"" />
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
            var source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" />
        <entry offset=""0x3"" hidden=""true"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0x6"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""41"" />
        <entry offset=""0xd"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0xe"" startLine=""9"" startColumn=""31"" endLine=""9"" endColumn=""35"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""29"" />
        <entry offset=""0x1c"" hidden=""true"" />
        <entry offset=""0x1f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" hidden=""true"" />
        <entry offset=""0x3"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x4"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0xc"" hidden=""true"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ForStatement3()
        {
            var source = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.M", @"<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""19"" />
        <entry offset=""0x3"" hidden=""true"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x6"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""41"" />
        <entry offset=""0xd"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0xe"" startLine=""7"" startColumn=""16"" endLine=""7"" endColumn=""19"" />
        <entry offset=""0x12"" hidden=""true"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);

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
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""34"" />
        <entry offset=""0x8"" hidden=""true"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" />
        <entry offset=""0x11"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" />
        <entry offset=""0x16"" hidden=""true"" />
        <entry offset=""0x1a"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ForEachStatement_Array()
        {
            var source = @"
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""16"" />
        <entry offset=""0x2"" startLine=""6"" startColumn=""27"" endLine=""6"" endColumn=""37"" />
        <entry offset=""0xb"" hidden=""true"" />
        <entry offset=""0xd"" startLine=""6"" startColumn=""18"" endLine=""6"" endColumn=""23"" />
        <entry offset=""0x11"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x12"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""41"" />
        <entry offset=""0x19"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x1a"" hidden=""true"" />
        <entry offset=""0x1e"" startLine=""6"" startColumn=""24"" endLine=""6"" endColumn=""26"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
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
    }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[]{ MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" />
        <entry offset=""0x19"" hidden=""true"" />
        <entry offset=""0x1c"" startLine=""11"" startColumn=""13"" endLine=""12"" endColumn=""30"" />
        <entry offset=""0x22"" hidden=""true"" />
        <entry offset=""0x24"" hidden=""true"" />
        <entry offset=""0x3c"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
        <entry offset=""0x44"" hidden=""true"" />
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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
            var source = @"
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
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19"" />
        <entry offset=""0xf"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""20"" />
        <entry offset=""0x19"" hidden=""true"" />
        <entry offset=""0x1c"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""38"" />
        <entry offset=""0x29"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""40"" />
        <entry offset=""0x34"" hidden=""true"" />
        <entry offset=""0x36"" hidden=""true"" />
        <entry offset=""0x4e"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" />
        <entry offset=""0x56"" hidden=""true"" />
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
            var source = @"
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
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""23"" />
        <entry offset=""0x4"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""24"" />
        <entry offset=""0x9"" hidden=""true"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""37"" />
        <entry offset=""0x12"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
        <entry offset=""0x15"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""14"" />
        <entry offset=""0x16"" startLine=""15"" startColumn=""15"" endLine=""15"" endColumn=""16"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""17"" endLine=""15"" endColumn=""18"" />
        <entry offset=""0x1a"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
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
  IL_000a:  ldtoken    ""<PrivateImplementationDetails>.__StaticArrayInitTypeSize=32 <PrivateImplementationDetails>.EB196F988F4F427D318CA25B68671CF3A4510012""
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
            string source = @"
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
";
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Main", @"<symbols>
  <methods>
    <method containingType=""Program"" name=""Main"" parameterNames=""args"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""5"" offset=""15"" />
          <slot kind=""0"" offset=""15"" />
          <slot kind=""1"" offset=""83"" />
          <slot kind=""1"" offset=""237"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""20"" />
        <entry offset=""0x2"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""47"" />
        <entry offset=""0x12"" hidden=""true"" />
        <entry offset=""0x14"" startLine=""12"" startColumn=""22"" endLine=""12"" endColumn=""27"" />
        <entry offset=""0x1b"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""14"" />
        <entry offset=""0x1c"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""33"" />
        <entry offset=""0x23"" hidden=""true"" />
        <entry offset=""0x29"" startLine=""17"" startColumn=""25"" endLine=""17"" endColumn=""31"" />
        <entry offset=""0x2b"" startLine=""20"" startColumn=""25"" endLine=""20"" endColumn=""42"" />
        <entry offset=""0x35"" hidden=""true"" />
        <entry offset=""0x38"" startLine=""21"" startColumn=""25"" endLine=""21"" endColumn=""26"" />
        <entry offset=""0x39"" startLine=""22"" startColumn=""25"" endLine=""22"" endColumn=""26"" />
        <entry offset=""0x3a"" startLine=""24"" startColumn=""25"" endLine=""24"" endColumn=""31"" />
        <entry offset=""0x3c"" startLine=""26"" startColumn=""13"" endLine=""26"" endColumn=""14"" />
        <entry offset=""0x3d"" startLine=""12"" startColumn=""28"" endLine=""12"" endColumn=""30"" />
        <entry offset=""0x47"" hidden=""true"" />
        <entry offset=""0x51"" hidden=""true"" />
        <entry offset=""0x52"" startLine=""27"" startColumn=""9"" endLine=""27"" endColumn=""10"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x53"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <namespace name=""System.Linq"" />
        <scope startOffset=""0x14"" endOffset=""0x3d"">
          <local name=""i"" il_index=""1"" il_start=""0x14"" il_end=""0x3d"" attributes=""0"" />
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
            var source = @"
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
";
            var c = CreateStandardCompilation(source, new[] { ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            var v = CompileAndVerify(c);

            v.VerifyIL("C.Main", @"
{
  // Code size       70 (0x46)
  .maxstack  2
  .locals init ((int, (bool, double))[] V_0,
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
  IL_0002:  call       ""(int, (bool, double))[] C.F()""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  // sequence point: <hidden>
  IL_000a:  br.s       IL_003f
  // sequence point: var (c, (d, e))
  IL_000c:  ldloc.0
  IL_000d:  ldloc.1
  IL_000e:  ldelem     ""System.ValueTuple<int, (bool, double)>""
  IL_0013:  dup
  IL_0014:  ldfld      ""(bool, double) System.ValueTuple<int, (bool, double)>.Item2""
  IL_0019:  stloc.s    V_5
  IL_001b:  ldfld      ""int System.ValueTuple<int, (bool, double)>.Item1""
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
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""50"" endLine=""4"" endColumn=""76"" />
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""37"" endLine=""8"" endColumn=""40"" />
        <entry offset=""0xa"" hidden=""true"" />
        <entry offset=""0xc"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""33"" />
        <entry offset=""0x32"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x33"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""41"" />
        <entry offset=""0x3a"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x3b"" hidden=""true"" />
        <entry offset=""0x3f"" startLine=""8"" startColumn=""34"" endLine=""8"" endColumn=""36"" />
        <entry offset=""0x45"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
            string source = @"
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
";
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Operate",
@"<symbols>
  <methods>
    <method containingType=""Program"" name=""Operate"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""0"" offset=""59"" />
          <slot kind=""0"" offset=""163"" />
          <slot kind=""0"" offset=""250"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""19"" />
        <entry offset=""0x4"" hidden=""true"" />
        <entry offset=""0x28"" hidden=""true"" />
        <entry offset=""0x2a"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""44"" />
        <entry offset=""0x3d"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""57"" />
        <entry offset=""0x5c"" hidden=""true"" />
        <entry offset=""0x61"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""57"" />
        <entry offset=""0x82"" hidden=""true"" />
        <entry offset=""0x87"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""59"" />
        <entry offset=""0xa3"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""43"" />
        <entry offset=""0xb7"" startLine=""31"" startColumn=""5"" endLine=""31"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xba"">
        <scope startOffset=""0x28"" endOffset=""0x5c"">
          <local name=""s"" il_index=""3"" il_start=""0x28"" il_end=""0x5c"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x5c"" endOffset=""0x82"">
          <local name=""s"" il_index=""4"" il_start=""0x5c"" il_end=""0x82"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x82"" endOffset=""0xa3"">
          <local name=""t"" il_index=""5"" il_start=""0x82"" il_end=""0xa3"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SwitchWithPattern_02()
        {
            string source = @"
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
";
            // we just want this to compile without crashing/asserting
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Program.Operate",
@"<symbols>
  <methods>
    <method containingType=""Program"" name=""Operate"" parameterNames=""p"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""30"" offset=""0"" />
          <slot kind=""30"" offset=""383"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <closure offset=""383"" />
          <closure offset=""0"" />
          <lambda offset=""109"" closure=""0"" />
          <lambda offset=""202"" closure=""0"" />
          <lambda offset=""295"" closure=""0"" />
          <lambda offset=""383"" closure=""1"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0xd"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" />
        <entry offset=""0xe"" hidden=""true"" />
        <entry offset=""0x1c"" hidden=""true"" />
        <entry offset=""0x41"" hidden=""true"" />
        <entry offset=""0x48"" startLine=""22"" startColumn=""28"" endLine=""22"" endColumn=""44"" />
        <entry offset=""0x60"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""63"" />
        <entry offset=""0x70"" hidden=""true"" />
        <entry offset=""0x79"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""63"" />
        <entry offset=""0x89"" hidden=""true"" />
        <entry offset=""0x93"" startLine=""27"" startColumn=""17"" endLine=""27"" endColumn=""65"" />
        <entry offset=""0xa3"" startLine=""29"" startColumn=""17"" endLine=""29"" endColumn=""49"" />
        <entry offset=""0xb3"" startLine=""31"" startColumn=""5"" endLine=""31"" endColumn=""6"" />
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

        [Fact, WorkItem(17090, "https://github.com/dotnet/roslyn/issues/17090"), WorkItem(19731, "https://github.com/dotnet/roslyn/issues/19731")]
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "1M2");

            verifier.VerifyIL(qualifiedMethodName: "Program.M1", sequencePoints: "Program.M1", source: source,
expectedIL: @"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (int V_0)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch ...           (1
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.0
  IL_0003:  br.s       IL_0005
  // sequence point: Console.Write(1);
  IL_0005:  ldc.i4.1
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  nop
  // sequence point: break;
  IL_000c:  br.s       IL_000e
  // sequence point: }
  IL_000e:  ret
}");
            verifier.VerifyIL(qualifiedMethodName: "Program.M2", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (string V_0)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch ...  (nameof(M2)
  IL_0001:  ldstr      ""M2""
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_0009
  // sequence point: Console.Write(nameof(M2));
  IL_0009:  ldstr      ""M2""
  IL_000e:  call       ""void System.Console.Write(string)""
  IL_0013:  nop
  // sequence point: break;
  IL_0014:  br.s       IL_0016
  // sequence point: }
  IL_0016:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe);
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

        [Fact, WorkItem(19734, "https://github.com/dotnet/roslyn/issues/19734")]
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "1234");

            verifier.VerifyIL(qualifiedMethodName: "Program.M1<T>", sequencePoints: "Program.M1", source: source,
expectedIL: @"{
  // Code size       80 (0x50)
  .maxstack  2
  .locals init (T V_0,
                int V_1,
                T V_2, //t
                int V_3, //i
                int V_4,
                object V_5,
                T V_6)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (1)
  IL_0001:  ldc.i4.1
  IL_0002:  stloc.s    V_4
  IL_0004:  ldc.i4.1
  IL_0005:  box        ""int""
  IL_000a:  stloc.s    V_5
  IL_000c:  ldloc.s    V_5
  IL_000e:  isinst     ""T""
  IL_0013:  ldnull
  IL_0014:  cgt.un
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_0025
  IL_0019:  ldloca.s   V_6
  IL_001b:  initobj    ""T""
  IL_0021:  ldloc.s    V_6
  IL_0023:  br.s       IL_002c
  IL_0025:  ldloc.s    V_5
  IL_0027:  unbox.any  ""T""
  IL_002c:  stloc.0
  IL_002d:  brfalse.s  IL_0031
  IL_002f:  br.s       IL_0035
  IL_0031:  ldc.i4.1
  IL_0032:  stloc.1
  IL_0033:  br.s       IL_0042
  // sequence point: <hidden>
  IL_0035:  ldloc.0
  IL_0036:  stloc.2
  IL_0037:  br.s       IL_0039
  // sequence point: Console.Write(1);
  IL_0039:  ldc.i4.1
  IL_003a:  call       ""void System.Console.Write(int)""
  IL_003f:  nop
  // sequence point: break;
  IL_0040:  br.s       IL_004f
  // sequence point: <hidden>
  IL_0042:  ldloc.1
  IL_0043:  stloc.3
  IL_0044:  br.s       IL_0046
  // sequence point: Console.Write(2);
  IL_0046:  ldc.i4.2
  IL_0047:  call       ""void System.Console.Write(int)""
  IL_004c:  nop
  // sequence point: break;
  IL_004d:  br.s       IL_004f
  // sequence point: }
  IL_004f:  ret
}");
            verifier.VerifyIL(qualifiedMethodName: "Program.M2<T>", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       87 (0x57)
  .maxstack  2
  .locals init (T V_0,
                string V_1,
                T V_2, //t
                string V_3, //s
                string V_4,
                object V_5,
                T V_6)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (nameof(M2))
  IL_0001:  ldstr      ""M2""
  IL_0006:  stloc.s    V_4
  IL_0008:  ldstr      ""M2""
  IL_000d:  stloc.s    V_5
  IL_000f:  ldloc.s    V_5
  IL_0011:  isinst     ""T""
  IL_0016:  ldnull
  IL_0017:  cgt.un
  IL_0019:  dup
  IL_001a:  brtrue.s   IL_0028
  IL_001c:  ldloca.s   V_6
  IL_001e:  initobj    ""T""
  IL_0024:  ldloc.s    V_6
  IL_0026:  br.s       IL_002f
  IL_0028:  ldloc.s    V_5
  IL_002a:  unbox.any  ""T""
  IL_002f:  stloc.0
  IL_0030:  brfalse.s  IL_0034
  IL_0032:  br.s       IL_003c
  IL_0034:  ldstr      ""M2""
  IL_0039:  stloc.1
  IL_003a:  br.s       IL_0049
  // sequence point: <hidden>
  IL_003c:  ldloc.0
  IL_003d:  stloc.2
  IL_003e:  br.s       IL_0040
  // sequence point: Console.Write(3);
  IL_0040:  ldc.i4.3
  IL_0041:  call       ""void System.Console.Write(int)""
  IL_0046:  nop
  // sequence point: break;
  IL_0047:  br.s       IL_0056
  // sequence point: <hidden>
  IL_0049:  ldloc.1
  IL_004a:  stloc.3
  IL_004b:  br.s       IL_004d
  // sequence point: Console.Write(4);
  IL_004d:  ldc.i4.4
  IL_004e:  call       ""void System.Console.Write(int)""
  IL_0053:  nop
  // sequence point: break;
  IL_0054:  br.s       IL_0056
  // sequence point: }
  IL_0056:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            verifier = CompileAndVerify(c, expectedOutput: "1234");

            verifier.VerifyIL("Program.M1<T>",
@"{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (T V_0,
                int V_1,
                object V_2,
                T V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  box        ""int""
  IL_0006:  stloc.2
  IL_0007:  ldloc.2
  IL_0008:  isinst     ""T""
  IL_000d:  ldnull
  IL_000e:  cgt.un
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_001e
  IL_0013:  ldloca.s   V_3
  IL_0015:  initobj    ""T""
  IL_001b:  ldloc.3
  IL_001c:  br.s       IL_0024
  IL_001e:  ldloc.2
  IL_001f:  unbox.any  ""T""
  IL_0024:  stloc.0
  IL_0025:  brtrue.s   IL_002b
  IL_0027:  ldc.i4.1
  IL_0028:  stloc.1
  IL_0029:  br.s       IL_0032
  IL_002b:  ldc.i4.1
  IL_002c:  call       ""void System.Console.Write(int)""
  IL_0031:  ret
  IL_0032:  ldc.i4.2
  IL_0033:  call       ""void System.Console.Write(int)""
  IL_0038:  ret
}");
            verifier.VerifyIL("Program.M2<T>",
@"{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (T V_0,
                string V_1,
                object V_2,
                T V_3)
  IL_0000:  ldstr      ""M2""
  IL_0005:  stloc.2
  IL_0006:  ldloc.2
  IL_0007:  isinst     ""T""
  IL_000c:  ldnull
  IL_000d:  cgt.un
  IL_000f:  dup
  IL_0010:  brtrue.s   IL_001d
  IL_0012:  ldloca.s   V_3
  IL_0014:  initobj    ""T""
  IL_001a:  ldloc.3
  IL_001b:  br.s       IL_0023
  IL_001d:  ldloc.2
  IL_001e:  unbox.any  ""T""
  IL_0023:  stloc.0
  IL_0024:  brtrue.s   IL_002e
  IL_0026:  ldstr      ""M2""
  IL_002b:  stloc.1
  IL_002c:  br.s       IL_0035
  IL_002e:  ldc.i4.3
  IL_002f:  call       ""void System.Console.Write(int)""
  IL_0034:  ret
  IL_0035:  ldc.i4.4
  IL_0036:  call       ""void System.Console.Write(int)""
  IL_003b:  ret
}");
        }

        [Fact, WorkItem(19734, "https://github.com/dotnet/roslyn/issues/19734")]
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular7_1);
            c.VerifyDiagnostics();
            var verifier = CompileAndVerify(c, expectedOutput: "66");

            verifier.VerifyIL(qualifiedMethodName: "Program.M2<T>", sequencePoints: "Program.M2", source: source,
expectedIL: @"{
  // Code size       15 (0xf)
  .maxstack  1
  .locals init (T V_0, //t
                string V_1, //s
                string V_2)
  // sequence point: {
  IL_0000:  nop
  // sequence point: switch (x)
  IL_0001:  ldnull
  IL_0002:  stloc.2
  IL_0003:  br.s       IL_0005
  // sequence point: Console.Write(6);
  IL_0005:  ldc.i4.6
  IL_0006:  call       ""void System.Console.Write(int)""
  IL_000b:  nop
  // sequence point: break;
  IL_000c:  br.s       IL_000e
  // sequence point: }
  IL_000e:  ret
}");

            // Check the release code generation too.
            c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular7_1);
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

        #endregion

        #region DoStatement

        [Fact]
        public void DoStatement()
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""55"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""24"" />
        <entry offset=""0x13"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""30"" />
        <entry offset=""0x7"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" />
        <entry offset=""0xd"" hidden=""true"" />
        <entry offset=""0x10"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""14"" />
        <entry offset=""0x11"" startLine=""20"" startColumn=""17"" endLine=""20"" endColumn=""26"" />
        <entry offset=""0x13"" startLine=""22"" startColumn=""18"" endLine=""22"" endColumn=""29"" />
        <entry offset=""0x19"" hidden=""true"" />
        <entry offset=""0x1c"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""14"" />
        <entry offset=""0x1d"" startLine=""24"" startColumn=""17"" endLine=""24"" endColumn=""27"" />
        <entry offset=""0x24"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""14"" />
        <entry offset=""0x27"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""14"" />
        <entry offset=""0x28"" startLine=""28"" startColumn=""17"" endLine=""28"" endColumn=""23"" />
        <entry offset=""0x2a"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""10"" />
        <entry offset=""0x2b"" startLine=""30"" startColumn=""11"" endLine=""30"" endColumn=""25"" />
        <entry offset=""0x30"" hidden=""true"" />
        <entry offset=""0x33"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""20"" />
        <entry offset=""0x3a"" startLine=""33"" startColumn=""5"" endLine=""33"" endColumn=""6"" />
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
            var source = @"namespace NS
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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

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
  <methods>
    <method containingType=""NS.MyClass"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""25"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""27"" />
        <entry offset=""0x10"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
      </sequencePoints>
    </method>
    <method containingType=""NS.MyClass"" name="".ctor"" parameterNames=""values"">
      <customDebugInfo>
        <forward declaringType=""NS.MyClass"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""44"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""57"" />
        <entry offset=""0x19"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
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
        <entry offset=""0x0"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" />
        <entry offset=""0x1"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""25"" />
        <entry offset=""0x3"" startLine=""18"" startColumn=""27"" endLine=""18"" endColumn=""35"" />
        <entry offset=""0x5"" startLine=""19"" startColumn=""13"" endLine=""19"" endColumn=""26"" />
        <entry offset=""0x7"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""40"" />
        <entry offset=""0xd"" startLine=""25"" startColumn=""13"" endLine=""25"" endColumn=""48"" />
        <entry offset=""0x25"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""36"" />
        <entry offset=""0x32"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Base"" name=""Finalize"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""29"" />
        <entry offset=""0xa"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
        <entry offset=""0x12"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0xa"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Field and Property Initializers

        [Fact]
        public void TestPartialClassFieldInitializers()
        {
            var text1 = @"
public partial class C
{
    int x = 1;
}
";

            var text2 = @"
public partial class C
{
    int y = 1;

    static void Main()
    {
        C c = new C();
    }
}
";
            //Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            //loads the assembly into the ReflectionOnlyLoadFrom context.
            //So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateStandardCompilation(new SyntaxTree[] { Parse(text1, "a.cs"), Parse(text2, "b.cs") });

            compilation.VerifyPdb("C..ctor", @"
<symbols>
  <files>
    <file id=""1"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""BB, 7A, A6, D2, B2, 32, 59, 43, 8C, 98, 7F, E1, 98, 8D, F0, 94, 68, E9, EB, 80, "" />
    <file id=""2"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""B4, EA, 18, 73, D2,  E, 7F, 15, 51, 4C, 68, 86, 40, DF, E3, C3, 97, 9D, F6, B7, "" />
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
</symbols>");
        }

        [Fact]
        public void TestPartialClassFieldInitializersWithLineDirectives()
        {
            var text1 = @"
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

";

            var text2 = @"
using System;
public partial class C
{
    int y = 1;
    int x2 = 1;
#line 12 ""foo2.cs""
    int z2 = Math.Abs(-3);
    int w2 = Math.Abs(4);
}
";

            var text3 = @"
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
";

            //Having a unique name here may be important. The infrastructure of the pdb to xml conversion
            //loads the assembly into the ReflectionOnlyLoadFrom context.
            //So it's probably a good idea to have a new name for each assembly.
            var compilation = CreateStandardCompilation(new[] { Parse(text1, "a.cs"), Parse(text2, "b.cs"), Parse(text3, "a.cs") }, options: TestOptions.DebugDll);

            compilation.VerifyPdb("C..ctor", @"
<symbols>
<files>
  <file id=""1"" name=""a.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""E2, 3B, 47,  2, DC, E4, 8D, B4, FF,  0, 67, 90, 31, 68, 74, C0,  6, D7, 39,  E, "" />
  <file id=""2"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""3"" name=""bar.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""4"" name=""b.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, CE, E5, E9, CB, 53, E5, EF, C1, 7F, 2C, 53, EC,  2, FE, 5C, 34, 2C, EF, 94, "" />
  <file id=""5"" name=""foo2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  <file id=""6"" name=""mah.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""406ea660-64cf-4c82-b6f0-42d48172a799"" checkSum=""AB,  0, 7F, 1D, 23, D9, "" />
</files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""1"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""26"" document=""2"" />
        <entry offset=""0x14"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""25"" document=""2"" />
        <entry offset=""0x20"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""30"" document=""3"" />
        <entry offset=""0x34"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""15"" document=""4"" />
        <entry offset=""0x3b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""16"" document=""4"" />
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
      <scope startOffset=""0x0"" endOffset=""0xaa"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""50"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__1_0"" parameterNames=""z"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""43"" endLine=""4"" endColumn=""44"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""14"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""16"" endLine=""4"" endColumn=""21"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        #endregion

        #region Auto-Property

        [Fact, WorkItem(820806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820806")]
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

            var comp = CreateStandardCompilation(source, options: TestOptions.DebugDll);

            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_AutoProp1"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""35"" endLine=""4"" endColumn=""39"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp1"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""40"" endLine=""4"" endColumn=""52"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_AutoProp2"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""33"" endLine=""5"" endColumn=""37"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp2"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""38"" endLine=""5"" endColumn=""42"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_AutoProp3"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""38"" endLine=""6"" endColumn=""51"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_AutoProp3"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""52"" endLine=""6"" endColumn=""56"" />
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""16"" />
        <entry offset=""0x3"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(538298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538298")]
        [Fact]
        public void RegressSeqPtEndOfMethodAfterReturn()
        {
            var source = @"using System;

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
";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);

            // Expected are current actual output plus Two extra expected SeqPt:
            //  <entry offset=""0x73"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" />
            //  <entry offset=""0x22"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" />
            // 
            // Note: NOT include other differences between Roslyn and Dev10, as they are filed in separated bugs
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""21"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""25"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" />
        <entry offset=""0x1b"" hidden=""true"" />
        <entry offset=""0x1e"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""21"" />
        <entry offset=""0x20"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""24"" />
        <entry offset=""0x28"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""28"" />
        <entry offset=""0x38"" hidden=""true"" />
        <entry offset=""0x3b"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""27"" />
        <entry offset=""0x3f"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""40"" />
        <entry offset=""0x47"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""27"" />
        <entry offset=""0x54"" hidden=""true"" />
        <entry offset=""0x58"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""27"" />
        <entry offset=""0x5c"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""33"" />
        <entry offset=""0x64"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""28"" />
        <entry offset=""0x71"" hidden=""true"" />
        <entry offset=""0x75"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""27"" />
        <entry offset=""0x79"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""20"" />
        <entry offset=""0x7e"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""29"" startColumn=""5"" endLine=""29"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""30"" />
        <entry offset=""0x5"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""20"" />
        <entry offset=""0xa"" hidden=""true"" />
        <entry offset=""0xd"" startLine=""32"" startColumn=""9"" endLine=""32"" endColumn=""10"" />
        <entry offset=""0xe"" startLine=""33"" startColumn=""13"" endLine=""33"" endColumn=""28"" />
        <entry offset=""0x18"" startLine=""34"" startColumn=""9"" endLine=""34"" endColumn=""10"" />
        <entry offset=""0x1b"" startLine=""36"" startColumn=""9"" endLine=""36"" endColumn=""10"" />
        <entry offset=""0x1c"" startLine=""37"" startColumn=""13"" endLine=""37"" endColumn=""27"" />
        <entry offset=""0x26"" startLine=""38"" startColumn=""9"" endLine=""38"" endColumn=""10"" />
        <entry offset=""0x27"" startLine=""39"" startColumn=""5"" endLine=""39"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""42"" startColumn=""5"" endLine=""42"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""9"" endLine=""43"" endColumn=""30"" />
        <entry offset=""0x5"" startLine=""44"" startColumn=""9"" endLine=""44"" endColumn=""20"" />
        <entry offset=""0xa"" hidden=""true"" />
        <entry offset=""0xd"" startLine=""45"" startColumn=""9"" endLine=""45"" endColumn=""10"" />
        <entry offset=""0xe"" startLine=""46"" startColumn=""13"" endLine=""46"" endColumn=""27"" />
        <entry offset=""0x16"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""10"" />
        <entry offset=""0x17"" startLine=""50"" startColumn=""13"" endLine=""50"" endColumn=""26"" />
        <entry offset=""0x1f"" startLine=""52"" startColumn=""5"" endLine=""52"" endColumn=""6"" />
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
            var source = @"
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
";
            // Dev12 inserts an additional sequence point on catch clause, just before 
            // the exception object is assigned to the variable. We don't place that sequence point.
            // Also the scope of he exception variable is different.

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("Test.Main", @"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""21"" />
        <entry offset=""0x3"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x4"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""42"" />
        <entry offset=""0xa"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""35"" />
        <entry offset=""0xb"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0xc"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""21"" />
        <entry offset=""0xe"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
        <entry offset=""0x11"" hidden=""true"" />
        <entry offset=""0x12"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" />
        <entry offset=""0x13"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""42"" />
        <entry offset=""0x19"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" />
        <entry offset=""0x1a"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" />
        <entry offset=""0x1b"" startLine=""22"" startColumn=""13"" endLine=""22"" endColumn=""24"" />
        <entry offset=""0x1f"" startLine=""25"" startColumn=""5"" endLine=""25"" endColumn=""6"" />
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

        [Fact, WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        public void ExceptionHandling_Filter_Debug1()
        {
            var source = @"
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
";
            var v = CompileAndVerify(CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll));

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
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""51"" />
        <entry offset=""0x8"" hidden=""true"" />
        <entry offset=""0x15"" startLine=""18"" startColumn=""31"" endLine=""18"" endColumn=""55"" />
        <entry offset=""0x1f"" hidden=""true"" />
        <entry offset=""0x25"" hidden=""true"" />
        <entry offset=""0x26"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" />
        <entry offset=""0x27"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""33"" />
        <entry offset=""0x2d"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" />
        <entry offset=""0x30"" hidden=""true"" />
        <entry offset=""0x3d"" startLine=""22"" startColumn=""29"" endLine=""22"" endColumn=""53"" />
        <entry offset=""0x47"" hidden=""true"" />
        <entry offset=""0x4d"" hidden=""true"" />
        <entry offset=""0x4e"" startLine=""23"" startColumn=""9"" endLine=""23"" endColumn=""10"" />
        <entry offset=""0x4f"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""33"" />
        <entry offset=""0x55"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" />
        <entry offset=""0x58"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" />
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

        [Fact, WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        public void ExceptionHandling_Filter_Debug2()
        {
            var source = @"
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
";
            var v = CompileAndVerify(CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll));
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""42"" />
        <entry offset=""0x8"" hidden=""true"" />
        <entry offset=""0x9"" startLine=""10"" startColumn=""15"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0xf"" hidden=""true"" />
        <entry offset=""0x15"" hidden=""true"" />
        <entry offset=""0x16"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x17"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""40"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x20"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
        public void ExceptionHandling_Filter_Debug3()
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
            var v = CompileAndVerify(CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll));
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""42"" />
        <entry offset=""0x8"" hidden=""true"" />
        <entry offset=""0x9"" startLine=""12"" startColumn=""15"" endLine=""12"" endColumn=""23"" />
        <entry offset=""0xf"" hidden=""true"" />
        <entry offset=""0x15"" hidden=""true"" />
        <entry offset=""0x16"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x17"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""40"" />
        <entry offset=""0x1d"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x20"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(2911, "https://github.com/dotnet/roslyn/issues/2911")]
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
            var v = CompileAndVerify(CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll));
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
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""42"" />
        <entry offset=""0x6"" hidden=""true"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""15"" endLine=""12"" endColumn=""23"" />
        <entry offset=""0x11"" hidden=""true"" />
        <entry offset=""0x12"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""40"" />
        <entry offset=""0x17"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x19"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(778655, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/778655")]
        [Fact]
        public void BranchToStartOfTry()
        {
            string source = @"
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
";
            // Note the hidden sequence point @IL_0019.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""27"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""50"" />
        <entry offset=""0xa"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""22"" />
        <entry offset=""0xf"" hidden=""true"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x13"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""35"" />
        <entry offset=""0x19"" hidden=""true"" />
        <entry offset=""0x1a"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" />
        <entry offset=""0x1b"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""33"" />
        <entry offset=""0x21"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" />
        <entry offset=""0x24"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""14"" />
        <entry offset=""0x25"" startLine=""21"" startColumn=""9"" endLine=""21"" endColumn=""10"" />
        <entry offset=""0x26"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" />
        <entry offset=""0x29"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" />
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
        public void UsingStatement()
        {
            var source = @"
public class DisposableClass : System.IDisposable
{
    private readonly string name;

    public DisposableClass(string name) 
    {
        this.name = name;
        System.Console.WriteLine(""Creating "" + name);
    }

    public void Dispose()
    {
        System.Console.WriteLine(""Disposing "" + name);
    }
}

class C
{
    static void Main()
    {
        using (DisposableClass a = new DisposableClass(""A""), b = new DisposableClass(""B""))
            System.Console.WriteLine(""First"");

        using (DisposableClass c = new DisposableClass(""C""), d = new DisposableClass(""D""))
        {
            System.Console.WriteLine(""Second"");
        }

        using (null)
        {

        }
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.Main", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <forward declaringType=""DisposableClass"" methodName="".ctor"" parameterNames=""name"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""34"" />
          <slot kind=""0"" offset=""64"" />
          <slot kind=""0"" offset=""176"" />
          <slot kind=""0"" offset=""206"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""22"" startColumn=""16"" endLine=""22"" endColumn=""60"" />
        <entry offset=""0xc"" startLine=""22"" startColumn=""62"" endLine=""22"" endColumn=""90"" />
        <entry offset=""0x17"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""47"" />
        <entry offset=""0x24"" hidden=""true"" />
        <entry offset=""0x2e"" hidden=""true"" />
        <entry offset=""0x2f"" hidden=""true"" />
        <entry offset=""0x31"" hidden=""true"" />
        <entry offset=""0x3b"" hidden=""true"" />
        <entry offset=""0x3c"" startLine=""25"" startColumn=""16"" endLine=""25"" endColumn=""60"" />
        <entry offset=""0x47"" startLine=""25"" startColumn=""62"" endLine=""25"" endColumn=""90"" />
        <entry offset=""0x52"" startLine=""26"" startColumn=""9"" endLine=""26"" endColumn=""10"" />
        <entry offset=""0x53"" startLine=""27"" startColumn=""13"" endLine=""27"" endColumn=""48"" />
        <entry offset=""0x5e"" startLine=""28"" startColumn=""9"" endLine=""28"" endColumn=""10"" />
        <entry offset=""0x61"" hidden=""true"" />
        <entry offset=""0x6b"" hidden=""true"" />
        <entry offset=""0x6c"" hidden=""true"" />
        <entry offset=""0x6e"" hidden=""true"" />
        <entry offset=""0x78"" hidden=""true"" />
        <entry offset=""0x79"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""10"" />
        <entry offset=""0x7a"" startLine=""33"" startColumn=""9"" endLine=""33"" endColumn=""10"" />
        <entry offset=""0x7b"" startLine=""34"" startColumn=""5"" endLine=""34"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7c"">
        <scope startOffset=""0x1"" endOffset=""0x3c"">
          <local name=""a"" il_index=""0"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
          <local name=""b"" il_index=""1"" il_start=""0x1"" il_end=""0x3c"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x3c"" endOffset=""0x79"">
          <local name=""c"" il_index=""2"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
          <local name=""d"" il_index=""3"" il_start=""0x3c"" il_end=""0x79"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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

        [Fact, WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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

        [Fact, WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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

        [Fact, WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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

        [Fact, WorkItem(18844, "https://github.com/dotnet/roslyn/issues/18844")]
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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

        // LockStatement tested in CodeGenLock

        #region Anonymous Type

        [Fact]
        public void AnonymousType_Empty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new {};
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""24"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
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
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        var o = new { a = 1 };
    }
}
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""31"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" />
        <entry offset=""0xe"" startLine=""11"" startColumn=""16"" endLine=""11"" endColumn=""29"" />
        <entry offset=""0x11"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0x12"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""20"" />
        <entry offset=""0x15"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""10"" />
        <entry offset=""0x16"" hidden=""true"" />
        <entry offset=""0x19"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""32"" />
        <entry offset=""0x25"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" />
        <entry offset=""0x15"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x16"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""35"" />
        <entry offset=""0x1e"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0x1f"" hidden=""true"" />
        <entry offset=""0x21"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""23"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""31"" />
        <entry offset=""0x15"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""28"" />
        <entry offset=""0x32"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x33"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" />
        <entry offset=""0x39"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x3a"" hidden=""true"" />
        <entry offset=""0x3c"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""31"" />
        <entry offset=""0x4a"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleAddresses()
        {
            var source = @"
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
";
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" />
        <entry offset=""0xe"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""29"" />
        <entry offset=""0x19"" startLine=""12"" startColumn=""31"" endLine=""12"" endColumn=""39"" />
        <entry offset=""0x1d"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x1e"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""20"" />
        <entry offset=""0x21"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""20"" />
        <entry offset=""0x24"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""10"" />
        <entry offset=""0x25"" hidden=""true"" />
        <entry offset=""0x2c"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""38"" />
        <entry offset=""0x3f"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" />
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
            var source = @"
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
";
            // NOTE: stop on each declarator.
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""33"" />
        <entry offset=""0x1b"" startLine=""8"" startColumn=""35"" endLine=""8"" endColumn=""48"" />
        <entry offset=""0x29"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x2a"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""31"" />
        <entry offset=""0x32"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""31"" />
        <entry offset=""0x3a"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""10"" />
        <entry offset=""0x3b"" hidden=""true"" />
        <entry offset=""0x3f"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
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
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""31"" />
        <entry offset=""0x15"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""31"" />
        <entry offset=""0x23"" startLine=""14"" startColumn=""16"" endLine=""14"" endColumn=""28"" />
        <entry offset=""0x40"" startLine=""14"" startColumn=""30"" endLine=""14"" endColumn=""37"" />
        <entry offset=""0x60"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" />
        <entry offset=""0x61"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""20"" />
        <entry offset=""0x64"" startLine=""17"" startColumn=""13"" endLine=""17"" endColumn=""20"" />
        <entry offset=""0x67"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""10"" />
        <entry offset=""0x68"" hidden=""true"" />
        <entry offset=""0x6d"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""31"" />
        <entry offset=""0x7b"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""31"" />
        <entry offset=""0x89"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""26"" />
        <entry offset=""0xc"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""26"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void FixedStatementMultipleMixed()
        {
            var source = @"
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
";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugDll);
            c.VerifyPdb(@"<symbols>
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
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""23"" />
        <entry offset=""0xf"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""30"" />
        <entry offset=""0x13"" startLine=""12"" startColumn=""32"" endLine=""12"" endColumn=""39"" />
        <entry offset=""0x3a"" startLine=""12"" startColumn=""41"" endLine=""12"" endColumn=""52"" />
        <entry offset=""0x49"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" />
        <entry offset=""0x4a"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""36"" />
        <entry offset=""0x52"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""36"" />
        <entry offset=""0x5a"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""36"" />
        <entry offset=""0x62"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" />
        <entry offset=""0x63"" hidden=""true"" />
        <entry offset=""0x6d"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" />
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""18"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""28"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""foo.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.UnsafeDebugExe);
            c.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C"" methodName=""Main"" />
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""26"" />
        <entry offset=""0x8"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
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
            var text1 = @"
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
";

            var compilation = CreateStandardCompilation(text1, options: TestOptions.DebugDll);
            compilation.VerifyPdb(@"
<symbols>
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
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""16"" />
        <entry offset=""0x2"" startLine=""7"" startColumn=""27"" endLine=""7"" endColumn=""51"" />
        <entry offset=""0x16"" hidden=""true"" />
        <entry offset=""0x18"" startLine=""7"" startColumn=""18"" endLine=""7"" endColumn=""23"" />
        <entry offset=""0x1c"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x1d"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""34"" />
        <entry offset=""0x24"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0x25"" hidden=""true"" />
        <entry offset=""0x29"" startLine=""7"" startColumn=""24"" endLine=""7"" endColumn=""26"" />
        <entry offset=""0x2f"" hidden=""true"" />
        <entry offset=""0x30"" hidden=""true"" />
        <entry offset=""0x45"" hidden=""true"" />
        <entry offset=""0x47"" hidden=""true"" />
        <entry offset=""0x4d"" hidden=""true"" />
        <entry offset=""0x4e"" hidden=""true"" />
        <entry offset=""0x56"" hidden=""true"" />
        <entry offset=""0x57"" hidden=""true"" />
        <entry offset=""0x5d"" hidden=""true"" />
        <entry offset=""0x64"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""16"" />
        <entry offset=""0x65"" startLine=""19"" startColumn=""27"" endLine=""19"" endColumn=""51"" />
        <entry offset=""0x7b"" hidden=""true"" />
        <entry offset=""0x7d"" startLine=""19"" startColumn=""18"" endLine=""19"" endColumn=""23"" />
        <entry offset=""0x84"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""10"" />
        <entry offset=""0x85"" startLine=""21"" startColumn=""13"" endLine=""21"" endColumn=""34"" />
        <entry offset=""0x8d"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" />
        <entry offset=""0x8e"" hidden=""true"" />
        <entry offset=""0x94"" startLine=""19"" startColumn=""24"" endLine=""19"" endColumn=""26"" />
        <entry offset=""0x9c"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" />
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
            var src = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""13"" />
        <entry offset=""0x7"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8"">
        <namespace name=""System"" />
      </scope>
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
            var c = CreateCompilationWithMscorlibAndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugExe);

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
</symbols>");

        }

        [Fact]
        public void HiddenIterator()
        {
            var src = @"
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(src, references: new[] { CSharpRef, ValueTupleRef, SystemRuntimeFacadeRef }, options: TestOptions.DebugDll);
            
            // We don't really need the debug info for kickoff method when the entire iterator method is hidden, 
            // but it doesn't hurt and removing it would need extra effort that's unnecessary.
            c.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""13"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
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
            string source = @"
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
";
            var c = CreateStandardCompilation(Parse(source, filename: "file.cs"));
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""file.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""F7,  3, 46, 2C, 11, 16, DE, 85, F9, DD, 5C, 76, F6, 55, D9, 13, E0, 95, DE, 14, "" />
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
            var comp = CreateCompilationWithMscorlib45(@"
class C
{
    public int P => M();
    public int M()
    {
        return 2;
    }
}");
            comp.VerifyDiagnostics();
            comp.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""21"" endLine=""4"" endColumn=""24"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""18"" />
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
  <methods>
    <method containingType=""C"" name=""get_Item"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""33"" endLine=""6"" endColumn=""36"" />
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
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" />
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
  <methods>
    <method containingType=""C"" name=""get_P"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""23"" endLine=""6"" endColumn=""24"" />
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
  <methods>
    <method containingType=""C"" name=""op_Increment"" parameterNames=""c"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""41"" endLine=""4"" endColumn=""42"" />
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
  <methods>
    <method containingType=""C"" name=""op_Explicit"" parameterNames=""i"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""51"" endLine=""6"" endColumn=""58"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
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
  <methods>
    <method containingType=""C"" name="".ctor"" parameterNames=""x"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""22"" />
        <entry offset=""0x6"" startLine=""7"" startColumn=""26"" endLine=""7"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
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
  <methods>
    <method containingType=""C"" name=""Finalize"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""13"" endLine=""5"" endColumn=""18"" />
        <entry offset=""0x9"" hidden=""true"" />
        <entry offset=""0x10"" hidden=""true"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
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
  <methods>
    <method containingType=""C"" name=""get_X"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""16"" endLine=""7"" endColumn=""17"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_X"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""16"" endLine=""8"" endColumn=""25"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""add_E"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""16"" endLine=""12"" endColumn=""21"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""remove_E"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_X"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""19"" endLine=""13"" endColumn=""24"" />
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
            var source =
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
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<>c.<M>b__0_0",
@"<symbols>
  <methods>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""63"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""10"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""39"" />
        <entry offset=""0x13"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""30"" />
        <entry offset=""0x39"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
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
            var source =
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
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<F>d__0.MoveNext",
@"<symbols>
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
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x27"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x28"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""35"" />
        <entry offset=""0x3f"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""16"" />
        <entry offset=""0x40"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""43"" />
        <entry offset=""0x7d"" hidden=""true"" />
        <entry offset=""0x7f"" startLine=""8"" startColumn=""18"" endLine=""8"" endColumn=""23"" />
        <entry offset=""0x90"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x91"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""28"" />
        <entry offset=""0xad"" hidden=""true"" />
        <entry offset=""0xb5"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
        <entry offset=""0xb6"" startLine=""8"" startColumn=""24"" endLine=""8"" endColumn=""26"" />
        <entry offset=""0xd1"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
        <entry offset=""0xd5"" hidden=""true"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ImportsInAsync()
        {
            var source =
@"using System.Linq;
using System.Threading.Tasks;
class C
{
    static async Task F()
    {
        var c = new[] { 1, 2, 3 };
        c.Select(i => i);
    }
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<F>d__0.MoveNext",
@"<symbols>
  <methods>
    <method containingType=""C+&lt;F&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;&gt;c"" methodName=""&lt;F&gt;b__0_0"" parameterNames=""i"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x79"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x7"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" />
        <entry offset=""0x8"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""35"" />
        <entry offset=""0x1f"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""26"" />
        <entry offset=""0x4c"" hidden=""true"" />
        <entry offset=""0x64"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" />
        <entry offset=""0x6c"" hidden=""true"" />
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
            var source =
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
}";
            var c = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll, references: new[] { SystemCoreRef });
            c.VerifyPdb("C+<>c.<M>b__0_0",
@"<symbols>
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
  <methods>
    <method containingType=""C+&lt;&gt;c+&lt;&lt;M&gt;b__0_0&gt;d"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" />
        <hoistedLocalScopes>
          <slot startOffset=""0x0"" endOffset=""0x79"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""50"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" />
        <entry offset=""0x7"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" />
        <entry offset=""0x8"" startLine=""8"" startColumn=""13"" endLine=""8"" endColumn=""39"" />
        <entry offset=""0x1f"" startLine=""9"" startColumn=""13"" endLine=""9"" endColumn=""30"" />
        <entry offset=""0x4c"" hidden=""true"" />
        <entry offset=""0x64"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" />
        <entry offset=""0x6c"" hidden=""true"" />
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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21079")]
        public void SyntaxOffset_Pattern()
        {
            var source = @"class C { bool F(object o) => o is int i && o is 3 && o is bool; }";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb("C.F", @"<symbols>
	  <methods>
	    <method containingType=""C"" name=""F"" parameterNames=""o"">
	      <customDebugInfo>
	        <using>
	          <namespace usingCount=""0"" />
	        </using>
	        <encLocalSlotMap>
	          <slot kind=""0"" offset=""12"" />
	          <slot kind=""temp"" />
	        </encLocalSlotMap>
	      </customDebugInfo>
	      <sequencePoints>
	        <entry offset=""0x0"" startLine=""1"" startColumn=""31"" endLine=""1"" endColumn=""64"" />
	      </sequencePoints>
	      <scope startOffset=""0x0"" endOffset=""0x38"">
	        <local name=""i"" il_index=""0"" il_start=""0x0"" il_end=""0x38"" attributes=""0"" />
	      </scope>
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
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"<symbols>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""55"" />
        <entry offset=""0x5"" startLine=""1"" startColumn=""56"" endLine=""1"" endColumn=""69"" />
        <entry offset=""0xb"" startLine=""1"" startColumn=""70"" endLine=""1"" endColumn=""71"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xd"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0xd"" attributes=""0"" />
        <local name=""c"" il_index=""1"" il_start=""0x0"" il_end=""0xd"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SyntaxOffset_TupleParenthesized()
        {
            var source = @"class C { int F() { (int, (int, int)) x = (1, (2, 3)); return x.Item1 + x.Item2.Item1 + x.Item2.Item2; } }";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"<symbols>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""55"" />
        <entry offset=""0x10"" startLine=""1"" startColumn=""56"" endLine=""1"" endColumn=""103"" />
        <entry offset=""0x31"" startLine=""1"" startColumn=""104"" endLine=""1"" endColumn=""105"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x33"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x33"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>"
);
        }

        [Fact]
        public void SyntaxOffset_TupleVarDefined()
        {
            var source = @"class C { int F() { var x = (1, 2); return x.Item1 + x.Item2; } }";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"<symbols>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""36"" />
        <entry offset=""0xa"" startLine=""1"" startColumn=""37"" endLine=""1"" endColumn=""62"" />
        <entry offset=""0x1a"" startLine=""1"" startColumn=""63"" endLine=""1"" endColumn=""64"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1c"">
        <local name=""x"" il_index=""0"" il_start=""0x0"" il_end=""0x1c"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void SyntaxOffset_TupleIgnoreDeconstructionIfVariableDeclared()
        {
            var source = @"class C { int F() { (int x, int y) a = (1, 2); return a.Item1 + a.Item2; } }";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll, references: s_valueTupleRefs);

            c.VerifyPdb("C.F", @"<symbols>
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""47"" />
        <entry offset=""0x9"" startLine=""1"" startColumn=""48"" endLine=""1"" endColumn=""73"" />
        <entry offset=""0x19"" startLine=""1"" startColumn=""74"" endLine=""1"" endColumn=""75"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1b"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x1b"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>");
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

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyDiagnostics(
                // (9,19): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     int F = G(out var v1);    
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var v1").WithLocation(9, 19),
                // (9,13): error CS0236: A field initializer cannot reference the non-static field, method, or property 'C.G(out int)'
                //     int F = G(out var v1);    
                Diagnostic(ErrorCode.ERR_FieldInitRefNonstatic, "G(out var v1)").WithArguments("C.G(out int)").WithLocation(9, 13),
                // (13,16): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
                //     : base(out var v3)
                Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "var v3").WithLocation(13, 16),
                // (13,7): error CS1729: 'object' does not contain a constructor that takes 1 arguments
                //     : base(out var v3)
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "base").WithArguments("object", "1").WithLocation(13, 7));
        }

        [Fact]
        public void SyntaxOffset_OutVarInMethod()
        {
            var source = @"class C { int G(out int x) { int z = 1; G(out var y); G(out var w); return x = y; } }";

            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
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
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""28"" endLine=""1"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""1"" startColumn=""30"" endLine=""1"" endColumn=""40"" />
        <entry offset=""0x3"" startLine=""1"" startColumn=""41"" endLine=""1"" endColumn=""54"" />
        <entry offset=""0xc"" startLine=""1"" startColumn=""55"" endLine=""1"" endColumn=""68"" />
        <entry offset=""0x15"" startLine=""1"" startColumn=""69"" endLine=""1"" endColumn=""82"" />
        <entry offset=""0x1f"" startLine=""1"" startColumn=""83"" endLine=""1"" endColumn=""84"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x22"">
        <local name=""z"" il_index=""0"" il_start=""0x0"" il_end=""0x22"" attributes=""0"" />
        <local name=""y"" il_index=""1"" il_start=""0x0"" il_end=""0x22"" attributes=""0"" />
        <local name=""w"" il_index=""2"" il_start=""0x0"" il_end=""0x22"" attributes=""0"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        #endregion

        [Fact, WorkItem(4370, "https://github.com/dotnet/roslyn/issues/4370")]
        public void HeadingHiddenSequencePointsPickUpDocumentFromVisibleSequencePoint()
        {
            var source = 
@"#line 1 ""C:\Async.cs""
#pragma checksum ""C:\Async.cs"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""DBEB2A067B2F0E0D678A002C587A2806056C3DCE""

using System.Threading.Tasks;

public class C
{
    public async void M1()
    {
    }
}
";
            
            var tree = SyntaxFactory.ParseSyntaxTree(source, encoding: Encoding.UTF8, path: "HIDDEN.cs");
            var c = CSharpCompilation.Create("Compilation", new[] { tree }, new[] { MscorlibRef_v46 }, options: TestOptions.DebugDll.WithDebugPlusMode(true));

            c.VerifyPdb(
@"<symbols>
  <files>
    <file id=""1"" name=""C:\Async.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, EB, 2A,  6, 7B, 2F,  E,  D, 67, 8A,  0, 2C, 58, 7A, 28,  6,  5, 6C, 3D, CE, "" />
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
");
        }

        [Fact, WorkItem(12923, "https://github.com/dotnet/roslyn/issues/12923")]
        public void SequencePointsForConstructorWithHiddenInitializer()
        {
            string initializerSource = @"
#line hidden
partial class C
{
    int i = 42;
}
";

            string constructorSource = @"
partial class C
{
    C()
    {
    }
}
";

            var c = CreateStandardCompilation(
                new[] { Parse(initializerSource, "initializer.cs"), Parse(constructorSource, "constructor.cs") },
                options: TestOptions.DebugDll);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""constructor.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""EA, D6,  A, 16, 6C, 6A, BC, C1, 5D, 98,  F, B7, 4B, 78, 13, 93, FB, C7, C2, 5A, "" />
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
");
        }

        [WorkItem(12378, "https://github.com/dotnet/roslyn/issues/12378")]
        [WorkItem(13971, "https://github.com/dotnet/roslyn/issues/13971")]
        [Fact]
        public void PatternSwitchSequencePoints()
        {
            string source =
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            CompileAndVerify(c).VerifyIL("Program.M",
@"{
  // Code size      188 (0xbc)
  .maxstack  2
  .locals init (object V_0,
                int V_1,
                object V_2,
                object V_3,
                object V_4,
                int V_5,
                object V_6,
                object V_7,
                object V_8)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.2
  IL_0003:  ldloc.2
  IL_0004:  stloc.0
  IL_0005:  ldloc.0
  IL_0006:  brtrue.s   IL_000a
  IL_0008:  br.s       IL_0054
  IL_000a:  ldloc.0
  IL_000b:  stloc.3
  IL_000c:  ldloc.3
  IL_000d:  isinst     ""int""
  IL_0012:  ldnull
  IL_0013:  cgt.un
  IL_0015:  dup
  IL_0016:  brtrue.s   IL_001b
  IL_0018:  ldc.i4.0
  IL_0019:  br.s       IL_0021
  IL_001b:  ldloc.3
  IL_001c:  unbox.any  ""int""
  IL_0021:  stloc.1
  IL_0022:  brfalse.s  IL_0054
  IL_0024:  ldloc.1
  IL_0025:  ldc.i4.1
  IL_0026:  sub
  IL_0027:  switch    (
        IL_0042,
        IL_004a,
        IL_0050,
        IL_0048,
        IL_004e)
  IL_0040:  br.s       IL_0054
  IL_0042:  br.s       IL_0056
  IL_0044:  br.s       IL_0062
  IL_0046:  br.s       IL_0070
  IL_0048:  br.s       IL_0060
  IL_004a:  br.s       IL_005b
  IL_004c:  br.s       IL_0054
  IL_004e:  br.s       IL_006c
  IL_0050:  br.s       IL_0067
  IL_0052:  br.s       IL_0054
  IL_0054:  br.s       IL_006e
  IL_0056:  ldarg.0
  IL_0057:  brfalse.s  IL_0060
  IL_0059:  br.s       IL_0044
  IL_005b:  ldarg.0
  IL_005c:  brfalse.s  IL_0060
  IL_005e:  br.s       IL_004c
  IL_0060:  br.s       IL_0072
  IL_0062:  ldarg.0
  IL_0063:  brtrue.s   IL_006c
  IL_0065:  br.s       IL_0046
  IL_0067:  ldarg.0
  IL_0068:  brtrue.s   IL_006c
  IL_006a:  br.s       IL_0052
  IL_006c:  br.s       IL_0072
  IL_006e:  br.s       IL_0072
  IL_0070:  br.s       IL_0072
  IL_0072:  ldarg.0
  IL_0073:  stloc.s    V_6
  IL_0075:  ldloc.s    V_6
  IL_0077:  stloc.s    V_4
  IL_0079:  ldloc.s    V_4
  IL_007b:  brtrue.s   IL_007f
  IL_007d:  br.s       IL_00a4
  IL_007f:  ldloc.s    V_4
  IL_0081:  stloc.3
  IL_0082:  ldloc.3
  IL_0083:  isinst     ""int""
  IL_0088:  ldnull
  IL_0089:  cgt.un
  IL_008b:  dup
  IL_008c:  brtrue.s   IL_0091
  IL_008e:  ldc.i4.0
  IL_008f:  br.s       IL_0097
  IL_0091:  ldloc.3
  IL_0092:  unbox.any  ""int""
  IL_0097:  stloc.s    V_5
  IL_0099:  brfalse.s  IL_00a4
  IL_009b:  ldloc.s    V_5
  IL_009d:  ldc.i4.1
  IL_009e:  beq.s      IL_00a2
  IL_00a0:  br.s       IL_00a4
  IL_00a2:  br.s       IL_00a6
  IL_00a4:  br.s       IL_00a8
  IL_00a6:  br.s       IL_00aa
  IL_00a8:  br.s       IL_00aa
  IL_00aa:  ldarg.0
  IL_00ab:  stloc.s    V_8
  IL_00ad:  ldloc.s    V_8
  IL_00af:  stloc.s    V_7
  IL_00b1:  ldloc.s    V_7
  IL_00b3:  brtrue.s   IL_00b7
  IL_00b5:  br.s       IL_00b7
  IL_00b7:  br.s       IL_00b9
  IL_00b9:  br.s       IL_00bb
  IL_00bb:  ret
}");
            c.VerifyPdb(
@"<symbols>
  <methods>
    <method containingType=""Program"" name=""M"" parameterNames=""o"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""35"" offset=""11"" />
          <slot kind=""35"" offset=""11"" />
          <slot kind=""1"" offset=""11"" />
          <slot kind=""temp"" />
          <slot kind=""35"" offset=""378"" />
          <slot kind=""35"" offset=""378"" />
          <slot kind=""1"" offset=""378"" />
          <slot kind=""35"" offset=""511"" />
          <slot kind=""1"" offset=""511"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""19"" />
        <entry offset=""0x3"" hidden=""true"" />
        <entry offset=""0x56"" startLine=""7"" startColumn=""20"" endLine=""7"" endColumn=""34"" />
        <entry offset=""0x5b"" startLine=""9"" startColumn=""20"" endLine=""9"" endColumn=""34"" />
        <entry offset=""0x60"" startLine=""10"" startColumn=""17"" endLine=""10"" endColumn=""23"" />
        <entry offset=""0x62"" startLine=""11"" startColumn=""20"" endLine=""11"" endColumn=""34"" />
        <entry offset=""0x67"" startLine=""13"" startColumn=""20"" endLine=""13"" endColumn=""34"" />
        <entry offset=""0x6c"" startLine=""14"" startColumn=""17"" endLine=""14"" endColumn=""23"" />
        <entry offset=""0x6e"" startLine=""16"" startColumn=""17"" endLine=""16"" endColumn=""23"" />
        <entry offset=""0x70"" startLine=""18"" startColumn=""17"" endLine=""18"" endColumn=""23"" />
        <entry offset=""0x72"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""19"" />
        <entry offset=""0x75"" hidden=""true"" />
        <entry offset=""0xa6"" startLine=""23"" startColumn=""17"" endLine=""23"" endColumn=""23"" />
        <entry offset=""0xa8"" startLine=""25"" startColumn=""17"" endLine=""25"" endColumn=""23"" />
        <entry offset=""0xaa"" startLine=""27"" startColumn=""9"" endLine=""27"" endColumn=""19"" />
        <entry offset=""0xad"" hidden=""true"" />
        <entry offset=""0xb9"" startLine=""30"" startColumn=""17"" endLine=""30"" endColumn=""23"" />
        <entry offset=""0xbb"" startLine=""32"" startColumn=""5"" endLine=""32"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        [Fact]
        public void LocalFunctionSequencePoints()
        {
            string source =
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
}";
            var c = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.DebugDll);
            c.VerifyPdb(
@"<symbols>
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
          <lambda offset=""99"" />
          <lambda offset=""202"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" />
        <entry offset=""0x3"" startLine=""12"" startColumn=""9"" endLine=""12"" endColumn=""44"" />
        <entry offset=""0x13"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
      </sequencePoints>
    </method>
    <method containingType=""Program"" name=""&lt;Main&gt;g__Local10_0"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""13"" endLine=""7"" endColumn=""21"" />
      </sequencePoints>
    </method>
    <method containingType=""Program"" name=""&lt;Main&gt;g__Local20_1"" parameterNames=""a"">
      <customDebugInfo>
        <forward declaringType=""Program"" methodName=""Main"" parameterNames=""args"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""202"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""13"" endLine=""10"" endColumn=""29"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""10"" />
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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

            v.VerifyIL("Program.<Test>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()", @"
{
  // Code size       77 (0x4d)
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
    IL_0010:  ldfld      ""int Program.<Test>d__0.<i>5__1""
    IL_0015:  stloc.1
    // sequence point: <hidden>
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.1
    IL_0018:  beq.s      IL_001c
    IL_001a:  br.s       IL_001e
    // sequence point: break;
    IL_001c:  br.s       IL_001e
    IL_001e:  leave.s    IL_0038
  }
  catch System.Exception
  {
    // async: catch handler, sequence point: <hidden>
    IL_0020:  stloc.2
    IL_0021:  ldarg.0
    IL_0022:  ldc.i4.s   -2
    IL_0024:  stfld      ""int Program.<Test>d__0.<>1__state""
    IL_0029:  ldarg.0
    IL_002a:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
    IL_002f:  ldloc.2
    IL_0030:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetException(System.Exception)""
    IL_0035:  nop
    IL_0036:  leave.s    IL_004c
  }
  // sequence point: }
  IL_0038:  ldarg.0
  IL_0039:  ldc.i4.s   -2
  IL_003b:  stfld      ""int Program.<Test>d__0.<>1__state""
  // sequence point: <hidden>
  IL_0040:  ldarg.0
  IL_0041:  ldflda     ""System.Runtime.CompilerServices.AsyncVoidMethodBuilder Program.<Test>d__0.<>t__builder""
  IL_0046:  call       ""void System.Runtime.CompilerServices.AsyncVoidMethodBuilder.SetResult()""
  IL_004b:  nop
  IL_004c:  ret
}
", sequencePoints: "Program+<Test>d__0.MoveNext", source: source);
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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
            var v = CompileAndVerify(source, options: TestOptions.DebugDll, additionalRefs: new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef, CSharpRef });

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
    }
}
