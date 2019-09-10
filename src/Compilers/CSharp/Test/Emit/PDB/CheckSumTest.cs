// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
            var tree1 = SyntaxFactory.ParseSyntaxTree(StringText.From(source1, Encoding.UTF8, SourceHashAlgorithm.Sha1), path: "sha1.cs");
            var tree256 = SyntaxFactory.ParseSyntaxTree(StringText.From(source256, Encoding.UTF8, SourceHashAlgorithm.Sha256), path: "sha256.cs");

            var compilation = CreateCompilation(new[] { tree1, tree256 });
            compilation.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name=""sha1.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8E-37-F3-94-ED-18-24-3F-35-EC-1B-70-25-29-42-1C-B0-84-9B-C8"" />
    <file id=""2"" name=""sha256.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""83-31-5B-52-08-2D-68-54-14-88-0E-E3-3A-5E-B7-83-86-53-83-B4-5A-3F-36-9E-5F-1B-60-33-27-0A-8A-EC"" />
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
    <file id=""1"" name=""USED1.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
    <file id=""2"" name=""USED2.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D9"" />
    <file id=""3"" name=""b.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""C0-51-F0-6F-D3-ED-44-A2-11-4D-03-70-89-20-A6-05-11-62-14-BE"" />
    <file id=""4"" name=""a.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""F0-C4-23-63-A5-89-B9-29-AF-94-07-85-2F-3A-40-D3-70-14-8F-9B"" />
  </files>
  <methods>
    <method containingType=""C"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""112"" startColumn=""9"" endLine=""112"" endColumn=""23"" document=""1"" />
        <entry offset=""0x6"" startLine=""112"" startColumn=""9"" endLine=""112"" endColumn=""24"" document=""2"" />
        <entry offset=""0xc"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""3"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
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

        [Fact]
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
    <file id=""1"" name=""a\..\a.cs"" language=""C#"" checksumAlgorithm=""406ea660-64cf-4c82-b6f0-42d48172a799"" checksum=""AB-00-7F-1D-23-D5"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""20"" endLine=""10"" endColumn=""21"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""22"" endLine=""10"" endColumn=""23"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
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
    }
}
