// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class CheckSumTest : CSharpTestBase
    {
        private static CSharpCompilation CreateCompilationWithChecksums(string source, string filePath, string baseDirectory)
        {
            return CSharpCompilation.Create(
                GetUniqueName(),
                new[] { Parse(source, filePath) },
                new[] { MscorlibRef },
                TestOptions.DebugDll.WithSourceReferenceResolver(new SourceFileResolver(ImmutableArray.Create<string>(), baseDirectory)));
        }

        [Fact]
        public void ChecksumAlgorithms()
        {
            var source1 = "public class C1 { public C1() { } }";
            var source256 = "public class C256 { public C256() { } }";
            var source384 = "public class C384 { public C384() { } }";
            var source512 = "public class C512 { public C512() { } }";
            var tree1 = SyntaxFactory.ParseSyntaxTree(StringText.From(source1, Encoding.UTF8, SourceHashAlgorithm.Sha1), path: "sha1.cs");
            var tree256 = SyntaxFactory.ParseSyntaxTree(StringText.From(source256, Encoding.UTF8, SourceHashAlgorithm.Sha256), path: "sha256.cs");
            var tree384 = SyntaxFactory.ParseSyntaxTree(StringText.From(source384, Encoding.UTF8, SourceHashAlgorithm.Sha384), path: "sha384.cs");
            var tree512 = SyntaxFactory.ParseSyntaxTree(StringText.From(source512, Encoding.UTF8, SourceHashAlgorithm.Sha512), path: "sha512.cs");

            var compilation = CreateCompilation(new[] { tree1, tree256, tree384, tree512 });

            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""sha1.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8E-37-F3-94-ED-18-24-3F-35-EC-1B-70-25-29-42-1C-B0-84-9B-C8"" />
    <file id=""2"" name=""sha256.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""83-31-5B-52-08-2D-68-54-14-88-0E-E3-3A-5E-B7-83-86-53-83-B4-5A-3F-36-9E-5F-1B-60-33-27-0A-8A-EC"" />
    <file id=""3"" name=""sha384.cs"" language=""C#"" checksumAlgorithm=""SHA384"" checksum=""DC-4F-64-F5-55-33-D6-A0-CF-B6-80-26-E6-CA-EC-3A-F4-64-A8-14-08-63-2D-66-5D-85-70-FF-59-8D-76-09-C7-9A-7D-80-0B-E5-71-34-99-3B-B8-B2-47-9F-91-F7"" />
    <file id=""4"" name=""sha512.cs"" language=""C#"" checksumAlgorithm=""SHA512"" checksum=""07-7E-FF-0C-1E-84-35-85-D8-FE-84-A5-13-A8-79-C8-A4-15-C8-A1-EF-6F-3B-04-A8-B2-D2-12-B4-8B-F3-E2-7A-6A-C3-3F-0C-2C-97-B6-16-38-A6-F8-C8-E5-94-E7-23-21-F1-20-9C-4E-BE-3A-A7-53-E4-32-87-EA-D7-3C"" />
  </files>
  <methods>
    <method containingType=""C1"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""19"" endLine=""1"" endColumn=""30"" document=""1"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""33"" endLine=""1"" endColumn=""34"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C256"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""34"" document=""2"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""37"" endLine=""1"" endColumn=""38"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C384"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""34"" document=""3"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""37"" endLine=""1"" endColumn=""38"" document=""3"" />
      </sequencePoints>
    </method>
    <method containingType=""C512"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C1"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""21"" endLine=""1"" endColumn=""34"" document=""4"" />
        <entry offset=""0x6"" startLine=""1"" startColumn=""37"" endLine=""1"" endColumn=""38"" document=""4"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void CheckSumPragmaClashesSameTree()
        {
            var text =
@"
class C
{

#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// same
#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// different case in Hex numerics, but otherwise same
#pragma checksum ""bogus.cs"" ""{406ea660-64cf-4C82-B6F0-42D48172A799}"" ""AB007f1d23d9""

// different case in path, so not a clash
#pragma checksum ""bogUs.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A788}"" ""ab007f1d23d9""

// whitespace in path, so not a clash
#pragma checksum ""bogUs.cs "" ""{406EA660-64CF-4C82-B6F0-42D48172A788}"" ""ab007f1d23d9""

#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""
// and now a clash in Guid
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A798}"" ""ab007f1d23d9""
// and now a clash in CheckSum
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d8""

    static void Main(string[] args)
    {
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugExe).
                VerifyDiagnostics(
                // (20,1): warning CS1697: Different checksum values given for 'bogus1.cs'
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A798}" "ab007f1d23d9"
                Diagnostic(ErrorCode.WRN_ConflictingChecksum, @"#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A798}"" ""ab007f1d23d9""").WithArguments("bogus1.cs"),
                // (22,1): warning CS1697: Different checksum values given for 'bogus1.cs'
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A799}" "ab007f1d23d8"
                Diagnostic(ErrorCode.WRN_ConflictingChecksum, @"#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d8""").WithArguments("bogus1.cs"));
        }

        [Fact]
        public void CheckSumPragmaClashesDifferentLength()
        {
            var text =
@"
class C
{

#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23""
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" """"
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// odd length, parsing warning, ignored by emit.
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d""

// bad Guid, parsing warning, ignored by emit.
#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A79}"" ""ab007f1d23d9""

    static void Main(string[] args)
    {
    }
}
";

            CompileAndVerify(text).
                VerifyDiagnostics(
                // (11,71): warning CS1695: Invalid #pragma checksum syntax; should be #pragma checksum "filename" "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}" "XXXX..."
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A799}" "ab007f1d23d"
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, @"""ab007f1d23d"""),
                // (14,30): warning CS1695: Invalid #pragma checksum syntax; should be #pragma checksum "filename" "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}" "XXXX..."
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A79}" "ab007f1d23d9"
                Diagnostic(ErrorCode.WRN_IllegalPPChecksum, @"""{406EA660-64CF-4C82-B6F0-42D48172A79}"""),
                // (6,1): warning CS1697: Different checksum values given for 'bogus1.cs'
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A799}" "ab007f1d23"
                Diagnostic(ErrorCode.WRN_ConflictingChecksum, @"#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23""").WithArguments("bogus1.cs"),
                // (7,1): warning CS1697: Different checksum values given for 'bogus1.cs'
                // #pragma checksum "bogus1.cs" "{406EA660-64CF-4C82-B6F0-42D48172A799}" ""
                Diagnostic(ErrorCode.WRN_ConflictingChecksum, @"#pragma checksum ""bogus1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" """"").WithArguments("bogus1.cs"));
        }

        [Fact]
        public void CheckSumPragmaClashesDifferentTrees()
        {
            var text1 =
@"
class C
{

#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// same
#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

    static void Main(string[] args)
    {
    }
}
";

            var text2 =
@"
class C1
{

#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// same
#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

// different
#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23""

}
";

            CompileAndVerify(new string[] { text1, text2 }).
                VerifyDiagnostics(
                // (11,1): warning CS1697: Different checksum values given for 'bogus.cs'
                // #pragma checksum "bogus.cs" "{406EA660-64CF-4C82-B6F0-42D48172A799}" "ab007f1d23"
                Diagnostic(ErrorCode.WRN_ConflictingChecksum, @"#pragma checksum ""bogus.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23""").WithArguments("bogus.cs"));
        }

        [Fact]
        [WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")]
        public void TestPartialClassFieldInitializers()
        {
            var text1 = WithWindowsLineBreaks(@"
public partial class C
{
    int x = 1;
}

#pragma checksum ""UNUSED.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

#pragma checksum ""USED1.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

");

            var text2 = WithWindowsLineBreaks(@"
public partial class C
{
#pragma checksum ""USED2.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""

int y = 1;

    static void Main()
    {

#line 112 ""USED1.cs""
        C c = new C();

#line 112 ""USED2.cs""
        C c1 = new C();

#line default

    }
}
");
            var compilation = CreateCompilation(new[] { Parse(text1, "a.cs"), Parse(text2, "b.cs") });
            compilation.VerifyPdb("C.Main", @"
<symbols>
  <files>
    <file id=""1"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""F0-C4-23-63-A5-89-B9-29-AF-94-07-85-2F-3A-40-D3-70-14-8F-9B"" />
    <file id=""2"" name=""b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""C0-51-F0-6F-D3-ED-44-A2-11-4D-03-70-89-20-A6-05-11-62-14-BE"" />
    <file id=""3"" name=""USED1.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
    <file id=""4"" name=""USED2.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
    <file id=""5"" name=""UNUSED.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""112"" startColumn=""9"" endLine=""112"" endColumn=""23"" document=""3"" />
        <entry offset=""0x6"" startLine=""112"" startColumn=""9"" endLine=""112"" endColumn=""24"" document=""4"" />
        <entry offset=""0xc"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void NormalizedPath_Tree()
        {
            var source = @"
class C
{
    void M()
    {
    }
}
";
            var comp = CreateCompilationWithChecksums(source, "b.cs", @"b:\base");

            // Verify the value of name attribute in file element.
            comp.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name=""b:\base\b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""05-25-26-AE-53-A0-54-46-AC-A6-1D-8A-3B-1E-3F-C3-43-39-FB-59"" />
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
        <entry offset=""0x1"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(50611, "https://github.com/dotnet/roslyn/issues/50611")]
        public void NoResolver()
        {
            var comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { Parse(@"
#pragma checksum ""a\..\a.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d5""
#line 10 ""a\..\a.cs""
class C { void M() { } }

", @"C:\a\..\b.cs") },
                new[] { MscorlibRef },
                TestOptions.DebugDll.WithSourceReferenceResolver(null));

            // Verify the value of name attribute in file element.
            comp.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""C:\a\..\b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""36-39-3C-83-56-97-F2-F0-60-95-A4-A0-32-C6-32-C7-B2-4B-16-92"" />
    <file id=""2"" name=""a\..\a.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D5"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""20"" endLine=""10"" endColumn=""21"" document=""2"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""23"" document=""2"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void NormalizedPath_LineDirective()
        {
            var source = @"
class C
{
    void M()
    {
        M();
#line 1 ""line.cs""
        M();
#line 2 ""./line.cs""
        M();
#line 3 "".\line.cs""
        M();
#line 4 ""q\..\line.cs""
        M();
#line 5 ""q:\absolute\file.cs""
        M();
    }
}
";

            var comp = CreateCompilationWithChecksums(source, "b.cs", @"b:\base");

            // Verify the fact that there's a single file element for "line.cs" and it has an absolute path.
            // Verify the fact that the path that was already absolute wasn't affected by the base directory.
            comp.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name=""b:\base\b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""B6-C3-C8-D1-2D-F4-BD-FA-F7-25-AC-F8-17-E1-83-BE-CC-9B-40-84"" />
    <file id=""2"" name=""b:\base\line.cs"" language=""C#"" />
    <file id=""3"" name=""q:\absolute\file.cs"" language=""C#"" />
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
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""6"" endColumn=""13"" document=""1"" />
        <entry offset=""0x8"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""2"" />
        <entry offset=""0xf"" startLine=""2"" startColumn=""9"" endLine=""2"" endColumn=""13"" document=""2"" />
        <entry offset=""0x16"" startLine=""3"" startColumn=""9"" endLine=""3"" endColumn=""13"" document=""2"" />
        <entry offset=""0x1d"" startLine=""4"" startColumn=""9"" endLine=""4"" endColumn=""13"" document=""2"" />
        <entry offset=""0x24"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""13"" document=""3"" />
        <entry offset=""0x2b"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""3"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void NormalizedPath_ChecksumDirective()
        {
            var source = @"
class C
{
#pragma checksum ""a.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d5""
#pragma checksum ""./b.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d6""
#pragma checksum "".\c.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d7""
#pragma checksum ""q\..\d.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d8""
#pragma checksum ""b:\base\e.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d9""
    void M()
    {
        M();
#line 1 ""a.cs""
        M();
#line 1 ""b.cs""
        M();
#line 1 ""c.cs""
        M();
#line 1 ""d.cs""
        M();
#line 1 ""e.cs""
        M();
    }
}
";

            var comp = CreateCompilationWithChecksums(source, "file.cs", @"b:\base");
            comp.VerifyDiagnostics();

            // Verify the fact that all pragmas are referenced, even though the paths differ before normalization.
            comp.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name=""b:\base\file.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""2B-34-42-7D-32-E5-0A-24-3D-01-43-BF-42-FB-38-57-62-60-8B-14"" />
    <file id=""2"" name=""b:\base\a.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D5"" />
    <file id=""3"" name=""b:\base\b.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D6"" />
    <file id=""4"" name=""b:\base\c.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D7"" />
    <file id=""5"" name=""b:\base\d.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D8"" />
    <file id=""6"" name=""b:\base\e.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""13"" document=""1"" />
        <entry offset=""0x8"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""2"" />
        <entry offset=""0xf"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""3"" />
        <entry offset=""0x16"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""4"" />
        <entry offset=""0x1d"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""5"" />
        <entry offset=""0x24"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""6"" />
        <entry offset=""0x2b"" startLine=""2"" startColumn=""5"" endLine=""2"" endColumn=""6"" document=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(729235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/729235")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void NormalizedPath_NoBaseDirectory()
        {
            var source = @"
class C
{
#pragma checksum ""a.cs"" ""{406EA660-64CF-4C82-B6F0-42D48172A799}"" ""ab007f1d23d5""
    void M()
    {
        M();
#line 1 ""a.cs""
        M();
#line 1 ""./a.cs""
        M();
#line 1 ""b.cs""
        M();
    }
}
";

            var comp = CreateCompilationWithChecksums(source, "file.cs", null);
            comp.VerifyDiagnostics();

            // Verify nothing blew up.
            comp.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name=""file.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""9B-81-4F-A7-E1-1F-D2-45-8B-00-F3-82-65-DF-E4-BF-A1-3A-3B-29"" />
    <file id=""2"" name=""a.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D5"" />
    <file id=""3"" name=""./a.cs"" language=""C#"" />
    <file id=""4"" name=""b.cs"" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""13"" document=""1"" />
        <entry offset=""0x8"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""2"" />
        <entry offset=""0xf"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""3"" />
        <entry offset=""0x16"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""4"" />
        <entry offset=""0x1d"" startLine=""2"" startColumn=""5"" endLine=""2"" endColumn=""6"" document=""4"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void CheckSumPragma_Sha384AndSha512Guids()
        {
            // #pragma checksum with well-known SHA-384/SHA-512 GUIDs and correct-length checksums
            var text = """
class C
{
#pragma checksum "sha384.cs" "{d99cfeb1-8c43-444a-8a6c-b61269d2a0bf}" "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f"
#pragma checksum "sha512.cs" "{ef2d1afc-6550-46d6-b14b-d70afe9a5566}" "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"

    static void Main()
    {
#line 1 "sha384.cs"
        System.Console.Write(1);
#line 1 "sha512.cs"
        System.Console.Write(2);
    }
}
""";

            var compilation = CreateCompilation(text, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            compilation.VerifyPdb("C.Main", """
<symbols>
  <files>
    <file id="1" name="" language="C#" />
    <file id="2" name="sha384.cs" language="C#" checksumAlgorithm="SHA384" checksum="00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10-11-12-13-14-15-16-17-18-19-1A-1B-1C-1D-1E-1F-20-21-22-23-24-25-26-27-28-29-2A-2B-2C-2D-2E-2F" />
    <file id="3" name="sha512.cs" language="C#" checksumAlgorithm="SHA512" checksum="00-01-02-03-04-05-06-07-08-09-0A-0B-0C-0D-0E-0F-10-11-12-13-14-15-16-17-18-19-1A-1B-1C-1D-1E-1F-20-21-22-23-24-25-26-27-28-29-2A-2B-2C-2D-2E-2F-30-31-32-33-34-35-36-37-38-39-3A-3B-3C-3D-3E-3F" />
  </files>
  <entryPoint declaringType="C" methodName="Main" />
  <methods>
    <method containingType="C" name="Main">
      <customDebugInfo>
        <using>
          <namespace usingCount="0" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset="0x0" startLine="7" startColumn="5" endLine="7" endColumn="6" document="1" />
        <entry offset="0x1" startLine="1" startColumn="9" endLine="1" endColumn="33" document="2" />
        <entry offset="0x8" startLine="1" startColumn="9" endLine="1" endColumn="33" document="3" />
        <entry offset="0xf" startLine="2" startColumn="5" endLine="2" endColumn="6" document="3" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
""");
        }

        [Fact]
        public void CheckSumPragma_UnrecognizedGuid()
        {
            // #pragma checksum treats GUIDs as opaque values
            var text = """
class C
{
#pragma checksum "random.cs" "{aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa}" "ef00112233"

    static void Main()
    {
#line 1 "random.cs"
        System.Console.Write(1);
    }
}
""";

            var compilation = CreateCompilation(text, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics();
            compilation.VerifyPdb("C.Main", """
<symbols>
  <files>
    <file id="1" name="" language="C#" />
    <file id="2" name="random.cs" language="C#" checksumAlgorithm="aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa" checksum="EF-00-11-22-33" />
  </files>
  <entryPoint declaringType="C" methodName="Main" />
  <methods>
    <method containingType="C" name="Main">
      <customDebugInfo>
        <using>
          <namespace usingCount="0" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset="0x0" startLine="6" startColumn="5" endLine="6" endColumn="6" document="1" />
        <entry offset="0x1" startLine="1" startColumn="9" endLine="1" endColumn="33" document="2" />
        <entry offset="0x8" startLine="2" startColumn="5" endLine="2" endColumn="6" document="2" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
""");
        }
    }
}
