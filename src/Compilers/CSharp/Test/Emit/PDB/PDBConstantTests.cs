// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBConstantTests : CSharpTestBase
    {
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void StringsWithSurrogateChar()
        {
            var source = @"
using System;
public class T
{
    public static void Main()
    {
        const string HighSurrogateCharacter = ""\uD800"";
        const string LowSurrogateCharacter = ""\uDC00"";
        const string MatchedSurrogateCharacters = ""\uD800\uDC00"";
    }
}";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unpaired surrogates.
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""T"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <constant name=""HighSurrogateCharacter"" value=""\uFFFD"" type=""String"" />
        <constant name=""LowSurrogateCharacter"" value=""\uFFFD"" type=""String"" />
        <constant name=""MatchedSurrogateCharacters"" value=""\uD800\uDC00"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""T"" name=""Main"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""HighSurrogateCharacter"" value=""\uD800"" type=""String"" />
        <constant name=""LowSurrogateCharacter"" value=""\uDC00"" type=""String"" />
        <constant name=""MatchedSurrogateCharacters"" value=""\uD800\uDC00"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(546862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546862")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void InvalidUnicodeString()
        {
            var source = @"
using System;
public class T
{
    public static void Main()
    {
        const string invalidUnicodeString = ""\uD800\0\uDC00"";
    }
}";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            // Note:  U+FFFD is the Unicode 'replacement character' point and is used to replace an incoming character
            //        whose value is unknown or unrepresentable in Unicode.  This is what our pdb writer does with
            //        unpaired surrogates.
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""T"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <constant name=""invalidUnicodeString"" value=""\uFFFD\u0000\uFFFD"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""T"" name=""Main"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""invalidUnicodeString"" value=""\uD800\u0000\uDC00"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void AllTypes()
        {
            var source = @"
using System;
using System.Collections.Generic;

class X {}

public class C<S>
{
    enum EnumI1 : sbyte  { A }
    enum EnumU1 : byte   { A }
    enum EnumI2 : short  { A }
    enum EnumU2 : ushort { A }
    enum EnumI4 : int    { A }
    enum EnumU4 : uint   { A }
    enum EnumI8 : long   { A }
    enum EnumU8 : ulong  { A }

    public static void F<T>()
    {
        const bool B = false;
        const char C = '\0';
        const sbyte I1 = 0;
        const byte U1 = 0;
        const short I2 = 0;
        const ushort U2 = 0;
        const int I4 = 0;
        const uint U4 = 0;
        const long I8 = 0;
        const ulong U8 = 0;
        const float R4 = 0;
        const double R8 = 0;

        const C<int>.EnumI1 EI1 = 0;
        const C<int>.EnumU1 EU1 = 0;
        const C<int>.EnumI2 EI2 = 0;
        const C<int>.EnumU2 EU2 = 0;
        const C<int>.EnumI4 EI4 = 0;
        const C<int>.EnumU4 EU4 = 0;
        const C<int>.EnumI8 EI8 = 0;
        const C<int>.EnumU8 EU8 = 0;

        const string StrWithNul = ""\0"";
        const string EmptyStr = """";
        const string NullStr = null;
        const object NullObject = null;
        const dynamic NullDynamic = null;
        const X NullTypeDef = null;
        const Action NullTypeRef = null;
        const Func<Dictionary<int, C<int>>, dynamic, T, List<S>> NullTypeSpec = null;
        const object[] Array1 = null;
        const object[,] Array2 = null;
        const object[][] Array3 = null;

        const decimal D = 0M;
        // DateTime const not expressible in C#
    }
}";

            var c = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.DebugDll);

            c.VerifyPdb("C`1.F", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C`1"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""1"" slotId=""0"" localName=""NullDynamic"" />
          <bucket flags=""000001000"" slotId=""0"" localName=""NullTypeSpec"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""56"" startColumn=""5"" endLine=""56"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <constant name=""B"" value=""0"" type=""Boolean"" />
        <constant name=""C"" value=""0"" type=""Char"" />
        <constant name=""I1"" value=""0"" type=""SByte"" />
        <constant name=""U1"" value=""0"" type=""Byte"" />
        <constant name=""I2"" value=""0"" type=""Int16"" />
        <constant name=""U2"" value=""0"" type=""UInt16"" />
        <constant name=""I4"" value=""0"" type=""Int32"" />
        <constant name=""U4"" value=""0"" type=""UInt32"" />
        <constant name=""I8"" value=""0"" type=""Int64"" />
        <constant name=""U8"" value=""0"" type=""UInt64"" />
        <constant name=""R4"" value=""0"" type=""Single"" />
        <constant name=""R8"" value=""0"" type=""Double"" />
        <constant name=""EI1"" value=""0"" signature=""EnumI1{Int32}"" />
        <constant name=""EU1"" value=""0"" signature=""EnumU1{Int32}"" />
        <constant name=""EI2"" value=""0"" signature=""EnumI2{Int32}"" />
        <constant name=""EU2"" value=""0"" signature=""EnumU2{Int32}"" />
        <constant name=""EI4"" value=""0"" signature=""EnumI4{Int32}"" />
        <constant name=""EU4"" value=""0"" signature=""EnumU4{Int32}"" />
        <constant name=""EI8"" value=""0"" signature=""EnumI8{Int32}"" />
        <constant name=""EU8"" value=""0"" signature=""EnumU8{Int32}"" />
        <constant name=""StrWithNul"" value=""\u0000"" type=""String"" />
        <constant name=""EmptyStr"" value="""" type=""String"" />
        <constant name=""NullStr"" value=""null"" type=""String"" />
        <constant name=""NullObject"" value=""null"" type=""Object"" />
        <constant name=""NullDynamic"" value=""null"" type=""Object"" />
        <constant name=""NullTypeDef"" value=""null"" signature=""X"" />
        <constant name=""NullTypeRef"" value=""null"" signature=""System.Action"" />
        <constant name=""NullTypeSpec"" value=""null"" signature=""System.Func`4{System.Collections.Generic.Dictionary`2{Int32, C`1{Int32}}, Object, !!0, System.Collections.Generic.List`1{!0}}"" />
        <constant name=""Array1"" value=""null"" signature=""Object[]"" />
        <constant name=""Array2"" value=""null"" signature=""Object[,,]"" />
        <constant name=""Array3"" value=""null"" signature=""Object[][]"" />
        <constant name=""D"" value=""0"" type=""Decimal"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void SimpleLocalConstant()
        {
            var text = @"
class C
{
    void M()
    {
        const int x = 1;
        {
            const int y = 2;
        }
    }
}
";
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
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
        <entry offset=""0x1"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x3"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <constant name=""x"" value=""1"" type=""Int32"" />
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <constant name=""y"" value=""2"" type=""Int32"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void LambdaLocalConstants()
        {
            var text = @"
using System;

class C
{
    void M(Action a)
    {
        const int x = 1;
        M(() =>
        {
            const int y = 2;
            {
                const int z = 3;
            }
        });
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);
            c.VerifyPdb(@"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"" parameterNames=""a"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>0</methodOrdinal>
          <lambda offset=""54"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""15"" endColumn=""12"" document=""1"" />
        <entry offset=""0x27"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x28"">
        <namespace name=""System"" />
        <constant name=""x"" value=""1"" type=""Int32"" />
      </scope>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;M&gt;b__0_0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""M"" parameterNames=""a"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" document=""1"" />
        <entry offset=""0x3"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x4"">
        <constant name=""y"" value=""2"" type=""Int32"" />
        <scope startOffset=""0x1"" endOffset=""0x3"">
          <constant name=""z"" value=""3"" type=""Int32"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [WorkItem(543342, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543342")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void IteratorLocalConstants()
        {
            var source = @"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        const int x = 1;
        for (int i = 0; i < 10; i++)
        {
            const int y = 2;
            yield return x + y + i;
        }
    }
}
";
            // NOTE: Roslyn's output is somewhat different than Dev10's in this case, but
            // all of the changes look reasonable.  The main thing for this test is that 
            // Dev10 creates fields for the locals in the iterator class.  Roslyn doesn't
            // do that - the <constant> in the <scope> is sufficient.
            var v = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                Assert.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "<>4__this",
                    "<i>5__1"
                }, module.GetFieldNames("C.<M>d__0"));
            });

            v.VerifyPdb("C+<M>d__0.MoveNext", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C+&lt;M&gt;d__0"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x20"" endOffset=""0x67"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""37"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x20"" startLine=""9"" startColumn=""14"" endLine=""9"" endColumn=""23"" document=""1"" />
        <entry offset=""0x27"" hidden=""true"" document=""1"" />
        <entry offset=""0x29"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2a"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""36"" document=""1"" />
        <entry offset=""0x41"" hidden=""true"" document=""1"" />
        <entry offset=""0x48"" startLine=""13"" startColumn=""9"" endLine=""13"" endColumn=""10"" document=""1"" />
        <entry offset=""0x49"" startLine=""9"" startColumn=""33"" endLine=""9"" endColumn=""36"" document=""1"" />
        <entry offset=""0x59"" startLine=""9"" startColumn=""25"" endLine=""9"" endColumn=""31"" document=""1"" />
        <entry offset=""0x64"" hidden=""true"" document=""1"" />
        <entry offset=""0x67"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x69"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x1f"" endOffset=""0x69"">
          <constant name=""x"" value=""1"" type=""Int32"" />
          <scope startOffset=""0x29"" endOffset=""0x49"">
            <constant name=""y"" value=""2"" type=""Int32"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/33564")]
        // https://github.com/dotnet/roslyn/issues/33564: Was [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        public void LocalConstantsTypes()
        {
            var text = @"
class C
{
    void M()
    {
        const object o = null;
        const string s = ""hello"";
        const float f = float.MinValue;
        const double d = double.MaxValue;
    }
}
";
            using (new CultureContext(new CultureInfo("en-US", useUserOverride: false)))
            {
                CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
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
        <entry offset=""0x1"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""o"" value=""null"" type=""Object"" />
        <constant name=""s"" value=""hello"" type=""String"" />
        <constant name=""f"" value=""-3.402823E+38"" type=""Single"" />
        <constant name=""d"" value=""1.79769313486232E+308"" type=""Double"" />
      </scope>
    </method>
  </methods>
</symbols>");
            }
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void WRN_PDBConstantStringValueTooLong()
        {
            var longStringValue = new string('a', 2049);
            var source = @"
using System;

class C
{
    static void Main()
    {
        const string goo = """ + longStringValue + @""";
        Console.Write(goo);
    }
}
";

            var compilation = CreateCompilation(source, options: TestOptions.DebugExe);

            var exebits = new MemoryStream();
            var pdbbits = new MemoryStream();
            var result = compilation.Emit(exebits, pdbbits);
            result.Diagnostics.Verify();

            /*
             * old behavior. This new warning was abandoned
            result.Diagnostics.Verify(// warning CS7063: Constant string value of 'goo' is too long to be used in a PDB file. Only the debug experience may be affected.
                                      Diagnostic(ErrorCode.WRN_PDBConstantStringValueTooLong).WithArguments("goo", longStringValue.Substring(0, 20) + "..."));

            //make sure that this warning is suppressable
            compilation = CreateCompilationWithMscorlib(text, compOptions: Options.Exe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(false).
                WithSpecificDiagnosticOptions(new Dictionary<int, ReportWarning>(){ {(int)ErrorCode.WRN_PDBConstantStringValueTooLong, ReportWarning.Suppress} }));

            result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);
            result.Diagnostics.Verify();

            //make sure that this warning can be turned into an error.
            compilation = CreateCompilationWithMscorlib(text, compOptions: Options.Exe.WithDebugInformationKind(Common.DebugInformationKind.Full).WithOptimizations(false).
                WithSpecificDiagnosticOptions(new Dictionary<int, ReportWarning>() { { (int)ErrorCode.WRN_PDBConstantStringValueTooLong, ReportWarning.Error } }));

            result = compilation.Emit(exebits, null, "DontCare", pdbbits, null);
            Assert.False(result.Success);
            result.Diagnostics.Verify(
                                      Diagnostic(ErrorCode.WRN_PDBConstantStringValueTooLong).WithArguments("goo", longStringValue.Substring(0, 20) + "...").WithWarningAsError(true));
             * */
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void StringConstantTooLong()
        {
            var text = @"
class C
{
    void M()
    {
        const string text = @""
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB
this is a string constant that is too long to fit into the PDB"";
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);

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
        <entry offset=""0x1"" startLine=""43"" startColumn=""5"" endLine=""43"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""5"" endLine=""43"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""text"" value=""\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB\u000D\u000Athis is a string constant that is too long to fit into the PDB"" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(178988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/178988")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void StringWithNulCharacter_MaxSupportedLength()
        {
            const int length = 2031;
            string str = new string('x', 9) + "\0" + new string('x', length - 10);

            string text = @"
class C
{
    void M()
    {
        const string x = """ + str + @""";
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);

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
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""x"" value=""" + str.Replace("\0", @"\u0000") + @""" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""x"" value=""" + str.Replace("\0", @"\u0000") + @""" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [WorkItem(178988, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/178988")]
        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void StringWithNulCharacter_OverSupportedLength()
        {
            const int length = 2032;
            string str = new string('x', 9) + "\0" + new string('x', length - 10);

            string text = @"
class C
{
    void M()
    {
        const string x = """ + str + @""";
    }
}
";
            var c = CompileAndVerify(text, options: TestOptions.DebugDll);

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
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.Pdb);

            c.VerifyPdb("C.M", @"
<symbols>
  <files>
    <file id=""1"" name="""" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""x"" value=""" + str.Replace("\0", @"\u0000") + @""" type=""String"" />
      </scope>
    </method>
  </methods>
</symbols>", format: DebugInformationFormat.PortablePdb);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void DecimalLocalConstants()
        {
            var text = @"
class C
{
    void M()
    {
        const decimal d = (decimal)1.5;
    }
}
";
            using (new CultureContext(new CultureInfo("en-US", useUserOverride: false)))
            {
                CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb("C.M", @"
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
        <entry offset=""0x1"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <constant name=""d"" value=""1.5"" type=""Decimal"" />
      </scope>
    </method>
  </methods>
</symbols>");
            }
        }
    }
}
