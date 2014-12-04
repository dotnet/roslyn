// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBUsingTests : CSharpPDBTestBase
    {
        #region Helpers

        private static CSharpCompilation CreateDummyCompilation(string assemblyName)
        {
            return CreateCompilationWithMscorlib(
                "public class C { }",
                assemblyName: assemblyName,
                options: TestOptions.DebugDll);
        }

        #endregion

        [Fact]
        public void TestUsings()
        {
            var text = @"
using System;

class A { void M() { } }

namespace X
{
    using System.IO;

    class B { void M() { } }

    namespace Y
    {
        using System.Threading;

        class C { void M() { } }
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""30"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System.Threading"" />
        <namespace name=""System.IO"" />
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""24"" end_row=""10"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System.IO"" />
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""20"" end_row=""4"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""22"" end_row=""4"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestNamespaceAliases()
        {
            var text = @"
using P = System;

class A { void M() { } }

namespace X
{
    using Q = System.IO;

    class B { void M() { } }

    namespace Y
    {
        using R = System.Threading;

        class C { void M() { } }
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""30"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""R"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""Q"" target=""System.IO"" kind=""namespace"" />
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""24"" end_row=""10"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.IO"" kind=""namespace"" />
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""20"" end_row=""4"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""22"" end_row=""4"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestTypeAliases1()
        {
            var text = @"
using P = System.String;

class A { void M() { } }

namespace X
{
    using Q = System.Int32;

    class B { void M() { } }

    namespace Y
    {
        using R = System.Char;

        class C { void M() { } }
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""30"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""R"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""Q"" target=""System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""24"" end_row=""10"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""20"" end_row=""4"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""22"" end_row=""4"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestTypeAliases2()
        {
            var text = @"
using P = System.Collections.Generic.List<int>;

class A { void M() { } }

namespace X
{
    using Q = System.Collections.Generic.List<System.Collections.Generic.List<char>>;

    class B { void M() { } }

    namespace Y
    {
        using P = System.Char; //hides previous P

        class C { void M() { } }
    }
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""30"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""Q"" target=""System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""24"" end_row=""10"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""20"" end_row=""4"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""22"" end_row=""4"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExternAliases()
        {
            CSharpCompilation dummyCompilation1 = CreateDummyCompilation("a");
            CSharpCompilation dummyCompilation2 = CreateDummyCompilation("b");
            CSharpCompilation dummyCompilation3 = CreateDummyCompilation("c");

            var text = @"
extern alias P;

class A { void M() { } }

namespace X
{
    extern alias Q;

    class B { void M() { } }

    namespace Y
    {
        extern alias R;

        class C { void M() { } }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text,
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation1, ImmutableArray.Create("P")) ,
                    new CSharpCompilationReference(dummyCompilation2, ImmutableArray.Create("Q")),
                    new CSharpCompilationReference(dummyCompilation3, ImmutableArray.Create("R"))
                });
            compilation.VerifyDiagnostics(
                // (2,1): info CS8020: Unused extern alias.
                // extern alias P;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias P;"),
                // (8,5): info CS8020: Unused extern alias.
                //     extern alias Q;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Q;"),
                // (14,9): info CS8020: Unused extern alias.
                //         extern alias R;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias R;"));

            //CONSIDER: Dev10 puts the <externinfo>s on A.M
            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""4"" start_column=""20"" end_row=""4"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""4"" start_column=""22"" end_row=""4"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""P"" />
        <externinfo alias=""P"" assembly=""a, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""Q"" assembly=""b, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""R"" assembly=""c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""A"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""24"" end_row=""10"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""26"" end_row=""10"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""Q"" />
        <extern alias=""P"" />
      </scope>
    </method>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""A"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""30"" end_row=""16"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""R"" />
        <extern alias=""Q"" />
        <extern alias=""P"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExternAliases_ExplicitAndGlobal()
        {
            var dummySource = @"
namespace N
{
    public class C { }
}
";

            CSharpCompilation dummyCompilation1 = CreateCompilationWithMscorlib(dummySource, assemblyName: "A", options: TestOptions.DebugDll);
            CSharpCompilation dummyCompilation2 = CreateCompilationWithMscorlib(dummySource, assemblyName: "B", options: TestOptions.DebugDll);

            var text = @"
extern alias A;
extern alias B;
using X = A::N;
using Y = B::N;
using Z = global::N;

class C { void M() { } }
";
            var compilation = CreateCompilationWithMscorlib(text,
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation1, ImmutableArray.Create("global", "A")),
                    new CSharpCompilationReference(dummyCompilation2, ImmutableArray.Create("B", "global"))
                });
            compilation.VerifyDiagnostics(
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using Y = B::N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = B::N;").WithLocation(5, 1),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using X = A::N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = A::N;").WithLocation(4, 1),
                // (6,1): hidden CS8019: Unnecessary using directive.
                // using Z = global::N;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Z = global::N;").WithLocation(6, 1));

            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
    <methods>
        <method containingType=""C"" name=""M"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
                    <namespace usingCount=""5""/>
                </using>
            </customDebugInfo>
            <sequencepoints total=""2"">
                <entry il_offset=""0x0"" start_row=""8"" start_column=""20"" end_row=""8"" end_column=""21"" file_ref=""0""/>
                <entry il_offset=""0x1"" start_row=""8"" start_column=""22"" end_row=""8"" end_column=""23"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
            <scope startOffset=""0x0"" endOffset=""0x2"">
                <extern alias=""A""/>
                <extern alias=""B""/>
                <alias name=""X"" target=""N"" kind=""namespace""/>
                <alias name=""Y"" qualifier=""B"" target=""N"" kind=""namespace""/>
                <alias name=""Z"" target=""N"" kind=""namespace""/>
                <externinfo alias=""A"" assembly=""A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""/>
                <externinfo alias=""B"" assembly=""B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""/>
            </scope>
        </method>
    </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestExternAliasesInUsing()
        {
            CSharpCompilation libComp = CreateCompilationWithMscorlib(@"
namespace N
{
    public class A { }
}", assemblyName: "Lib");

            var text = @"
extern alias P;
using P::N;
using Q = P::N.A;
using R = P::N;
using global::N;
using S = global::N.B;
using T = global::N;

namespace N
{
    class B { void M() { } }
}
";
            var compilation = CreateCompilationWithMscorlib(text,
                assemblyName: "Test",
                options: TestOptions.DebugDll,
                references: new[] { new CSharpCompilationReference(libComp, ImmutableArray.Create("P")) });
            compilation.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Verify();

            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""N.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""0"" />
          <namespace usingCount=""7"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""24"" end_row=""12"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""12"" start_column=""26"" end_row=""12"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""P"" />
        <namespace qualifier=""P"" name=""N"" />
        <namespace name=""N"" />
        <alias name=""Q"" target=""N.A, Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""R"" qualifier=""P"" target=""N"" kind=""namespace"" />
        <alias name=""S"" target=""N.B, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""T"" target=""N"" kind=""namespace"" />
        <externinfo alias=""P"" assembly=""Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestNamespacesAndAliases()
        {
            CSharpCompilation dummyCompilation1 = CreateDummyCompilation("a");
            CSharpCompilation dummyCompilation2 = CreateDummyCompilation("b");
            CSharpCompilation dummyCompilation3 = CreateDummyCompilation("c");

            var text = @"
extern alias P;
using System;
using AU1 = System;
using AT1 = System.Char;

class A { void M() { } }

namespace X
{
    extern alias Q;
    using AU2 = System.IO;
    using AT2 = System.IO.Directory;
    using System.IO;

    class B { void M() { } }

    namespace Y
    {
        extern alias R;
        using AT3 = System.Text.StringBuilder;
        using System.Text;
        using AU3 = System.Text;

        class C { void M() { } }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text,
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation1, ImmutableArray.Create("P")) ,
                    new CSharpCompilationReference(dummyCompilation2, ImmutableArray.Create("Q")),
                    new CSharpCompilationReference(dummyCompilation3, ImmutableArray.Create("R"))
                });
            compilation.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using AU1 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU1 = System;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using AT1 = System.Char;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT1 = System.Char;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias P;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias P;"),
                // (12,5): info CS8019: Unnecessary using directive.
                //     using AU2 = System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU2 = System.IO;"),
                // (13,5): info CS8019: Unnecessary using directive.
                //     using AT2 = System.IO.Directory;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT2 = System.IO.Directory;"),
                // (14,5): info CS8019: Unnecessary using directive.
                //     using System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.IO;"),
                // (11,5): info CS8020: Unused extern alias.
                //     extern alias Q;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Q;"),
                // (21,9): info CS8019: Unnecessary using directive.
                //         using AT3 = System.Text.StringBuilder;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT3 = System.Text.StringBuilder;"),
                // (22,9): info CS8019: Unnecessary using directive.
                //         using System.Text;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Text;"),
                // (23,9): info CS8019: Unnecessary using directive.
                //         using AU3 = System.Text;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU3 = System.Text;"),
                // (20,9): info CS8020: Unused extern alias.
                //         extern alias R;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias R;"));

            //CONSIDER: Dev10 puts the <externinfo>s on A.M
            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""20"" end_row=""7"" end_column=""21"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""22"" end_row=""7"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <externinfo alias=""P"" assembly=""a, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""Q"" assembly=""b, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""R"" assembly=""c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""A"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""24"" end_row=""16"" end_column=""25"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""26"" end_row=""16"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""Q"" />
        <namespace name=""System.IO"" />
        <alias name=""AU2"" target=""System.IO"" kind=""namespace"" />
        <alias name=""AT2"" target=""System.IO.Directory, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.Y.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""3"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""A"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""25"" start_column=""28"" end_row=""25"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""25"" start_column=""30"" end_row=""25"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""R"" />
        <namespace name=""System.Text"" />
        <alias name=""AT3"" target=""System.Text.StringBuilder, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""AU3"" target=""System.Text"" kind=""namespace"" />
        <extern alias=""Q"" />
        <namespace name=""System.IO"" />
        <alias name=""AU2"" target=""System.IO"" kind=""namespace"" />
        <alias name=""AT2"" target=""System.IO.Directory, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(913022, "DevDiv")]
        public void ReferenceWithMultipleAliases()
        {
            var source1 = @"
namespace N { public class D { } }
namespace M { public class E { } }
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var source2 = @"
extern alias A;
extern alias B;

using A::N;
using B::M;
using X = A::N;
using Y = B::N;

public class C
{
	public static void Main() 
    {
        System.Console.WriteLine(new D()); 
        System.Console.WriteLine(new E()); 
        System.Console.WriteLine(new X.D()); 
        System.Console.WriteLine(new Y.D()); 
    }
}";

            var compilation2 = CreateCompilationWithMscorlib(source2,
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(compilation1, ImmutableArray.Create("A", "B"))
                });

            compilation2.VerifyDiagnostics();

            string actual = GetPdbXml(compilation2);
            string expected = @"
<symbols>
    <methods>
        <method containingType=""C"" name=""Main"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
                    <namespace usingCount=""6""/>
                </using>
            </customDebugInfo>
            <sequencepoints total=""6"">
                <entry il_offset=""0x0"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0""/>
                <entry il_offset=""0x1"" start_row=""14"" start_column=""9"" end_row=""14"" end_column=""43"" file_ref=""0""/>
                <entry il_offset=""0xc"" start_row=""15"" start_column=""9"" end_row=""15"" end_column=""43"" file_ref=""0""/>
                <entry il_offset=""0x17"" start_row=""16"" start_column=""9"" end_row=""16"" end_column=""45"" file_ref=""0""/>
                <entry il_offset=""0x22"" start_row=""17"" start_column=""9"" end_row=""17"" end_column=""45"" file_ref=""0""/>
                <entry il_offset=""0x2d"" start_row=""18"" start_column=""5"" end_row=""18"" end_column=""6"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
            <scope startOffset=""0x0"" endOffset=""0x2e"">
                <extern alias=""A""/>
                <extern alias=""B""/>
                <namespace qualifier=""A"" name=""N""/>
                <namespace qualifier=""A"" name=""M""/>
                <alias name=""X"" qualifier=""A"" target=""N"" kind=""namespace""/>
                <alias name=""Y"" qualifier=""A"" target=""N"" kind=""namespace""/>
                <externinfo alias = ""A"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
                <externinfo alias = ""B"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
            </scope>
        </method>
    </methods>
</symbols>
";
            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(913022, "DevDiv")]
        public void ReferenceWithGlobalAndDuplicateAliases()
        {
            var source1 = @"
namespace N { public class D { } }
";
            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll);

            var source2 = @"
extern alias A;
extern alias B;

public class C
{
	public static void Main() 
    {
        System.Console.WriteLine(new N.D()); 
        System.Console.WriteLine(new A::N.D()); 
        System.Console.WriteLine(new B::N.D()); 
    }
}";

            var compilation2 = CreateCompilationWithMscorlib(source2,
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(compilation1, ImmutableArray.Create("global", "B", "A", "A", "global"))
                });

            compilation2.VerifyDiagnostics();

            string actual = GetPdbXml(compilation2);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""5"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0xc"" start_row=""10"" start_column=""9"" end_row=""10"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x17"" start_row=""11"" start_column=""9"" end_row=""11"" end_column=""48"" file_ref=""0"" />
        <entry il_offset=""0x22"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <extern alias=""A"" />
        <extern alias=""B"" />
        <externinfo alias=""B"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""A"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestPartialTypeInOneFile()
        {
            CSharpCompilation dummyCompilation1 = CreateDummyCompilation("a");
            CSharpCompilation dummyCompilation2 = CreateDummyCompilation("b");
            CSharpCompilation dummyCompilation3 = CreateDummyCompilation("c");

            var text1 = @"
extern alias P;
using System;
using AU1 = System;
using AT1 = System.Char;

namespace X
{
    extern alias Q;
    using AU2 = System.IO;
    using AT2 = System.IO.Directory;
    using System.IO;

    partial class C
    {
        partial void M();
        void N1() { }
    }
}

namespace X
{
    extern alias R;
    using AU3 = System.Threading;
    using AT3 = System.Threading.Thread;
    using System.Threading;

    partial class C
    {
        partial void M() { }
        void N2() { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(
                text1,
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation1, ImmutableArray.Create("P")),
                    new CSharpCompilationReference(dummyCompilation2, ImmutableArray.Create("Q")),
                    new CSharpCompilationReference(dummyCompilation3, ImmutableArray.Create("R")),
                });
            compilation.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using AU1 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU1 = System;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using AT1 = System.Char;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT1 = System.Char;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias P;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias P;"),
                // (24,5): info CS8019: Unnecessary using directive.
                //     using AU3 = System.Threading;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU3 = System.Threading;"),
                // (25,5): info CS8019: Unnecessary using directive.
                //     using AT3 = System.Threading.Thread;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT3 = System.Threading.Thread;"),
                // (26,5): info CS8019: Unnecessary using directive.
                //     using System.Threading;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading;"),
                // (23,5): info CS8020: Unused extern alias.
                //     extern alias R;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias R;"),
                // (10,5): info CS8019: Unnecessary using directive.
                //     using AU2 = System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU2 = System.IO;"),
                // (11,5): info CS8019: Unnecessary using directive.
                //     using AT2 = System.IO.Directory;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT2 = System.IO.Directory;"),
                // (12,5): info CS8019: Unnecessary using directive.
                //     using System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.IO;"),
                // (9,5): info CS8020: Unused extern alias.
                //     extern alias Q;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Q;"));

            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""30"" start_column=""26"" end_row=""30"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""30"" start_column=""28"" end_row=""30"" end_column=""29"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""R"" />
        <namespace name=""System.Threading"" />
        <alias name=""AU3"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""AT3"" target=""System.Threading.Thread, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <externinfo alias=""P"" assembly=""a, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""Q"" assembly=""b, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""R"" assembly=""c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
    <method containingType=""X.C"" name=""N1"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""X.C"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""19"" end_row=""17"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""21"" end_row=""17"" end_column=""22"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""Q"" />
        <namespace name=""System.IO"" />
        <alias name=""AU2"" target=""System.IO"" kind=""namespace"" />
        <alias name=""AT2"" target=""System.IO.Directory, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.C"" name=""N2"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""X.C"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""31"" start_column=""19"" end_row=""31"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""31"" start_column=""21"" end_row=""31"" end_column=""22"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""R"" />
        <namespace name=""System.Threading"" />
        <alias name=""AU3"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""AT3"" target=""System.Threading.Thread, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestPartialTypeInTwoFiles()
        {
            CSharpCompilation dummyCompilation1 = CreateDummyCompilation("a");
            CSharpCompilation dummyCompilation2 = CreateDummyCompilation("b");
            CSharpCompilation dummyCompilation3 = CreateDummyCompilation("c");
            CSharpCompilation dummyCompilation4 = CreateDummyCompilation("d");

            var text1 = @"
extern alias P;
using System;
using AU1 = System;
using AT1 = System.Char;

namespace X
{
    extern alias Q;
    using AU2 = System.IO;
    using AT2 = System.IO.Directory;
    using System.IO;

    partial class C
    {
        partial void M();
        void N1() { }
    }
}
";

            var text2 = @"
extern alias R;
using System.Text;
using AU3 = System.Text;
using AT3 = System.Text.StringBuilder;

namespace X
{
    extern alias S;
    using AU4 = System.Threading;
    using AT4 = System.Threading.Thread;
    using System.Threading;

    partial class C
    {
        partial void M() { }
        void N2() { }
    }
}
";
            var compilation = CreateCompilationWithMscorlib(
                new string[] { text1, text2 },
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation1, ImmutableArray.Create("P")),
                    new CSharpCompilationReference(dummyCompilation2, ImmutableArray.Create("Q")),
                    new CSharpCompilationReference(dummyCompilation3, ImmutableArray.Create("R")),
                    new CSharpCompilationReference(dummyCompilation4, ImmutableArray.Create("S")),
                });
            compilation.VerifyDiagnostics(
                // (3,1): info CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using AU1 = System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU1 = System;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using AT1 = System.Char;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT1 = System.Char;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias P;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias P;"),
                // (3,1): info CS8019: Unnecessary using directive.
                // using System.Text;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Text;"),
                // (4,1): info CS8019: Unnecessary using directive.
                // using AU3 = System.Text;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU3 = System.Text;"),
                // (5,1): info CS8019: Unnecessary using directive.
                // using AT3 = System.Text.StringBuilder;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT3 = System.Text.StringBuilder;"),
                // (10,5): info CS8019: Unnecessary using directive.
                //     using AU2 = System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU2 = System.IO;"),
                // (2,1): info CS8020: Unused extern alias.
                // extern alias R;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias R;"),
                // (11,5): info CS8019: Unnecessary using directive.
                //     using AT2 = System.IO.Directory;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT2 = System.IO.Directory;"),
                // (12,5): info CS8019: Unnecessary using directive.
                //     using System.IO;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.IO;"),
                // (10,5): info CS8019: Unnecessary using directive.
                //     using AU4 = System.Threading;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AU4 = System.Threading;"),
                // (9,5): info CS8020: Unused extern alias.
                //     extern alias Q;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias Q;"),
                // (11,5): info CS8019: Unnecessary using directive.
                //     using AT4 = System.Threading.Thread;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using AT4 = System.Threading.Thread;"),
                // (12,5): info CS8019: Unnecessary using directive.
                //     using System.Threading;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Threading;"),
                // (9,5): info CS8020: Unused extern alias.
                //     extern alias S;
                Diagnostic(ErrorCode.HDN_UnusedExternAlias, "extern alias S;"));

            string actual = GetPdbXml(compilation);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""X.C"" name=""M"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""16"" start_column=""26"" end_row=""16"" end_column=""27"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""16"" start_column=""28"" end_row=""16"" end_column=""29"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""S"" />
        <namespace name=""System.Threading"" />
        <alias name=""AU4"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""AT4"" target=""System.Threading.Thread, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""R"" />
        <namespace name=""System.Text"" />
        <alias name=""AU3"" target=""System.Text"" kind=""namespace"" />
        <alias name=""AT3"" target=""System.Text.StringBuilder, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <externinfo alias=""P"" assembly=""a, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""Q"" assembly=""b, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""R"" assembly=""c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""S"" assembly=""d, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
    <method containingType=""X.C"" name=""N1"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""X.C"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""19"" end_row=""17"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""21"" end_row=""17"" end_column=""22"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""Q"" />
        <namespace name=""System.IO"" />
        <alias name=""AU2"" target=""System.IO"" kind=""namespace"" />
        <alias name=""AT2"" target=""System.IO.Directory, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""P"" />
        <namespace name=""System"" />
        <alias name=""AU1"" target=""System"" kind=""namespace"" />
        <alias name=""AT1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.C"" name=""N2"" parameterNames="""">
      <customDebugInfo version=""4"" count=""2"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule version=""4"" kind=""ForwardToModuleInfo"" size=""12"" declaringType=""X.C"" methodName=""M"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""17"" start_column=""19"" end_row=""17"" end_column=""20"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""17"" start_column=""21"" end_row=""17"" end_column=""22"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""S"" />
        <namespace name=""System.Threading"" />
        <alias name=""AU4"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""AT4"" target=""System.Threading.Thread, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""R"" />
        <namespace name=""System.Text"" />
        <alias name=""AU3"" target=""System.Text"" kind=""namespace"" />
        <alias name=""AT3"" target=""System.Text.StringBuilder, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestSynthesizedConstructors()
        {
            var text = @"
namespace X
{
    using System;

    partial class C
    {
        int x = 1;
        static int sx = 1;
    }
}

namespace X
{
    using System.IO;

    partial class C
    {
        int y = 1;
        static int sy = 1;
    }
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
    <methods>
        <method containingType=""X.C"" name="".ctor"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
                    <namespace usingCount=""1""/>
                    <namespace usingCount=""0""/>
                </using>
            </customDebugInfo>
            <sequencepoints total=""2"">
                <entry il_offset=""0x0"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""19"" file_ref=""0""/>
                <entry il_offset=""0x7"" start_row=""19"" start_column=""9"" end_row=""19"" end_column=""19"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
            <scope startOffset=""0x0"" endOffset=""0x16"">
                <namespace name=""System""/>
            </scope>
        </method>
        <method containingType=""X.C"" name="".cctor"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""X.C"" methodName="".ctor"" parameterNames=""""/>
            </customDebugInfo>
            <sequencepoints total=""2"">
                <entry il_offset=""0x0"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""27"" file_ref=""0""/>
                <entry il_offset=""0x6"" start_row=""20"" start_column=""9"" end_row=""20"" end_column=""27"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
        </method>
    </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestFieldInitializerLambdas()
        {
            var text = @"
using System.Linq;

class C
{
    int x = new int[2].Count(x => { return x % 3 == 0; });
    static bool sx = new int[2].Any(x => 
    {
        return x % 2 == 0; 
    });
}
";
            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
    <methods>
        <method containingType=""C"" name="".ctor"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
                    <namespace usingCount=""1""/>
                </using>
            </customDebugInfo>
            <sequencepoints total=""1"">
                <entry il_offset=""0x0"" start_row=""6"" start_column=""5"" end_row=""6"" end_column=""59"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
            <scope startOffset=""0x0"" endOffset=""0x38"">
                <namespace name=""System.Linq""/>
            </scope>
        </method>
        <method containingType=""C"" name="".cctor"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".ctor"" parameterNames=""""/>
            </customDebugInfo>
            <sequencepoints total=""1"">
                <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""10"" end_column=""8"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
        </method>
        <method containingType=""C+&lt;&gt;c__DisplayClass0"" name=""&lt;_ctor&gt;b__1"" parameterNames=""x"">
            <customDebugInfo version=""4"" count=""1"">
                <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".ctor"" parameterNames=""""/>
            </customDebugInfo>
            <sequencepoints total=""3"">
                <entry il_offset=""0x0"" start_row=""6"" start_column=""35"" end_row=""6"" end_column=""36"" file_ref=""0""/>
                <entry il_offset=""0x1"" start_row=""6"" start_column=""37"" end_row=""6"" end_column=""55"" file_ref=""0""/>
                <entry il_offset=""0xa"" start_row=""6"" start_column=""56"" end_row=""6"" end_column=""57"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
        </method>
        <method containingType=""C+&lt;&gt;c__DisplayClass3"" name=""&lt;_cctor&gt;b__4"" parameterNames=""x"">
            <customDebugInfo version=""4"" count=""1"">
                <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName="".ctor"" parameterNames=""""/>
            </customDebugInfo>
            <sequencepoints total=""3"">
                <entry il_offset=""0x0"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0""/>
                <entry il_offset=""0x1"" start_row=""9"" start_column=""9"" end_row=""9"" end_column=""27"" file_ref=""0""/>
                <entry il_offset=""0xa"" start_row=""10"" start_column=""5"" end_row=""10"" end_column=""6"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
        </method>
    </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestAccessors()
        {
            var text = @"
using System;

class C
{
    int P1 { get; set; }
    int P2 { get { return 0; } set { } }
    int this[int x] { get { return 0; } set { } }
    event System.Action E1;
    event System.Action E2 { add { } remove { } }
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_P1"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""14"" end_row=""6"" end_column=""18"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_P1"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""19"" end_row=""6"" end_column=""23"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_P2"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""18"" end_row=""7"" end_column=""19"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""20"" end_row=""7"" end_column=""29"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""7"" start_column=""30"" end_row=""7"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""set_P2"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_P2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""36"" end_row=""7"" end_column=""37"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""7"" start_column=""38"" end_row=""7"" end_column=""39"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""add_E2"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_P2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""34"" end_row=""10"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""36"" end_row=""10"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""remove_E2"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_P2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""10"" start_column=""45"" end_row=""10"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""10"" start_column=""47"" end_row=""10"" end_column=""48"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""get_Item"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_P2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""27"" end_row=""8"" end_column=""28"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""29"" end_row=""8"" end_column=""38"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""8"" start_column=""39"" end_row=""8"" end_column=""40"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""C"" name=""set_Item"" parameterNames=""x, value"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_P2"" parameterNames="""" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""8"" start_column=""45"" end_row=""8"" end_column=""46"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""47"" end_row=""8"" end_column=""48"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestSynthesizedSealedAccessors()
        {
            var text = @"
using System;

class Base
{
    public virtual int P { get; set; }
}

class Derived : Base
{
    public sealed override int P { set { } } //have to synthesize a sealed getter
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""Base"" name=""get_P"" parameterNames="""">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""28"" end_row=""6"" end_column=""32"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Base"" name=""set_P"" parameterNames=""value"">
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""33"" end_row=""6"" end_column=""37"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
    <method containingType=""Derived"" name=""set_P"" parameterNames=""value"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""11"" start_column=""40"" end_row=""11"" end_column=""41"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""11"" start_column=""42"" end_row=""11"" end_column=""43"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>";
            AssertXmlEqual(expected, actual);
        }

        [Fact]
        public void TestSynthesizedExplicitImplementation()
        {
            var text = @"
using System.Runtime.CompilerServices;

interface I1
{
    [IndexerName(""A"")]
    int this[int x] { get; set; }
}

interface I2
{
    [IndexerName(""B"")]
    int this[int x] { get; set; }
}

class C : I1, I2
{
    public int this[int x] { get { return 0; } set { } }
}
";

            string actual = GetPdbXml(text, TestOptions.DebugDll);
            string expected = @"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_Item"" parameterNames=""x"">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""3"">
        <entry il_offset=""0x0"" start_row=""18"" start_column=""34"" end_row=""18"" end_column=""35"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""18"" start_column=""36"" end_row=""18"" end_column=""45"" file_ref=""0"" />
        <entry il_offset=""0x5"" start_row=""18"" start_column=""46"" end_row=""18"" end_column=""47"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System.Runtime.CompilerServices"" />
      </scope>
    </method>
    <method containingType=""C"" name=""set_Item"" parameterNames=""x, value"">
      <customDebugInfo version=""4"" count=""1"">
        <forward version=""4"" kind=""ForwardInfo"" size=""12"" declaringType=""C"" methodName=""get_Item"" parameterNames=""x"" />
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""18"" start_column=""52"" end_row=""18"" end_column=""53"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""18"" start_column=""54"" end_row=""18"" end_column=""55"" file_ref=""0"" />
      </sequencepoints>
      <locals />
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expected, actual);
        }

        [WorkItem(692496, "DevDiv")]
        [Fact]
        public void SequencePointOnUsingExpression()
        {
            var source = @"
using System;

public class Test : IDisposable
{
    static void Main()
    {
        using (new Test())
        {
        }
    }

    public void Dispose() { }
}
";
            var v = CompileAndVerify(source, options: TestOptions.DebugDll);

            v.VerifyIL("Test.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  1
  .locals init (Test V_0)
 -IL_0000:  nop
 -IL_0001:  newobj     ""Test..ctor()""
  IL_0006:  stloc.0
  .try
  {
   -IL_0007:  nop
   -IL_0008:  nop
    IL_0009:  leave.s    IL_0016
  }
  finally
  {
   ~IL_000b:  ldloc.0
    IL_000c:  brfalse.s  IL_0015
    IL_000e:  ldloc.0
    IL_000f:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0014:  nop
    IL_0015:  endfinally
  }
 -IL_0016:  ret
}", sequencePoints: "Test.Main");
        }

        [Fact]
        public void TestNestedType()
        {
            var libSource = @"
public class Outer
{
    public class Inner
    {
    }
}
";

            var libRef = CreateCompilationWithMscorlib(libSource, assemblyName: "Lib").EmitToImageReference();

            var source = @"
using I = Outer.Inner;

public class Test
{
    static void Main()
    {
    }
}
";
            var expectedXml = @"
<symbols>
  <entryPoint declaringType=""Test"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""7"" start_column=""5"" end_row=""7"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""8"" start_column=""5"" end_row=""8"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""I"" target=""Outer+Inner, Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expectedXml, GetPdbXml(source, TestOptions.DebugExe, "Test.Main", references: new[] { libRef }));
        }

        [Fact]
        public void TestVerbatimIdentifiers()
        {
            var source = @"
using @namespace;
using @object = @namespace;
using @string = @namespace.@class<@namespace.@interface>.@struct;

namespace @namespace
{
    public class @class<T>
    {
        public struct @struct
        {
        }
    }

    public interface @interface
    {
    }
}

class Test { static void Main() { } }
";
            // As in dev12, we drop all '@'s.
            var expectedXml = @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""20"" start_column=""35"" end_row=""20"" end_column=""36"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <namespace name=""namespace"" />
        <alias name=""object"" target=""namespace"" kind=""namespace"" />
        <alias name=""string"" target=""namespace.class`1+struct[[namespace.interface, Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Test, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";

            var comp = CreateCompilationWithMscorlib(source, assemblyName: "Test");
            AssertXmlEqual(expectedXml, GetPdbXml(comp, "Test.Main"));
        }

        [WorkItem(842479, "DevDiv")]
        [Fact]
        public void UsingExternAlias()
        {
            var libSource = "public class C { }";
            var lib = CreateCompilationWithMscorlib(libSource, assemblyName: "Lib");
            var libRef = lib.EmitToImageReference(aliases: ImmutableArray.Create("Q"));

            var source = @"
extern alias Q;
using R = Q;
using Q;

namespace N
{
    using S = R;
    using R;

    class D
    {
        static void Main() { }
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { libRef });

            var expectedXml = @"
<symbols>
  <methods>
    <method containingType=""N.D"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""16"" namespaceCount=""2"">
          <namespace usingCount=""2"" />
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""13"" start_column=""30"" end_row=""13"" end_column=""31"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <namespace qualifier=""Q"" name="""" />
        <alias name=""S"" qualifier=""Q"" target="""" kind=""namespace"" />
        <extern alias=""Q"" />
        <namespace qualifier=""Q"" name="""" />
        <alias name=""R"" qualifier=""Q"" target="""" kind=""namespace"" />
        <externinfo alias=""Q"" assembly=""Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expectedXml, GetPdbXml(comp, "N.D.Main"));
        }

        [WorkItem(842478, "DevDiv")]
        [Fact]
        public void AliasIncludingDynamic()
        {
            var source = @"
using AD = System.Action<dynamic>;

class D
{
    static void Main() { }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            var expectedXml = @"
<symbols>
  <methods>
    <method containingType=""D"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""1"">
        <entry il_offset=""0x0"" start_row=""6"" start_column=""26"" end_row=""6"" end_column=""27"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <alias name=""AD"" target=""System.Action`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>";

            AssertXmlEqual(expectedXml, GetPdbXml(comp, "D.Main"));
        }

        [Fact]
        public void UsingExpression()
        {
            TestSequencePoints(
@"using System;

public class Test : IDisposable
{
    static void Main()
    {
        [|using (new Test())|]
        {
        }
    }

    public void Dispose() { }

}", TestOptions.ReleaseExe, methodName: "Test.Main");
        }

        [Fact]
        public void UsingVariable()
        {
            TestSequencePoints(
@"using System;

public class Test : IDisposable
{
    static void Main()
    {
        var x = new Test();
        [|using (x)|]
        {
        }
    }

    public void Dispose() { }

}", TestOptions.ReleaseExe, methodName: "Test.Main");
        }

        [WorkItem(546754, "DevDiv")]
        [Fact]
        public void ArrayType()
        {
            var source1 = @"
using System;

public class W {}

public class Y<T>
{
  public class F {}
  public class Z<U> {}
}
";
            string compName = GetUniqueName();
            var comp = CreateCompilationWithMscorlib(source1, assemblyName: compName, options: TestOptions.DebugDll);

            var source2 = @"
using t1 = Y<W[]>;
using t2 = Y<W[,]>;
using t3 = Y<W[,][]>;
using t4 = Y<Y<W>[][,]>;
using t5 = Y<W[,][]>.Z<W[][,,]>;
using t6 = Y<Y<Y<int[]>.F[,][]>.Z<Y<W[,][]>.F[]>[][]>;

public class C1
{
    public static void Main()
    {
    }
}
";
            string actual = GetPdbXml(source2, TestOptions.DebugExe, references: new[] { comp.ToMetadataReference() });
            string expected = string.Format(@"
<symbols>
  <entryPoint declaringType=""C1"" methodName=""Main"" parameterNames="""" />
  <methods>
    <method containingType=""C1"" name=""Main"" parameterNames="""">
      <customDebugInfo version=""4"" count=""1"">
        <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
          <namespace usingCount=""6"" />
        </using>
      </customDebugInfo>
      <sequencepoints total=""2"">
        <entry il_offset=""0x0"" start_row=""12"" start_column=""5"" end_row=""12"" end_column=""6"" file_ref=""0"" />
        <entry il_offset=""0x1"" start_row=""13"" start_column=""5"" end_row=""13"" end_column=""6"" file_ref=""0"" />
      </sequencepoints>
      <locals />
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""t1"" target=""Y`1[[W[], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t2"" target=""Y`1[[W[,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t3"" target=""Y`1[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t4"" target=""Y`1[[Y`1[[W, {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][,][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t5"" target=""Y`1+Z`1[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[W[,,][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t6"" target=""Y`1[[Y`1+Z`1[[Y`1+F[[System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[Y`1+F[[W[][,], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][][], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], {0}, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>", compName);

            AssertXmlEqual(expected, actual);
        }

        [Fact, WorkItem(543615, "DevDiv")]
        public void WRN_DebugFullNameTooLong()
        {
            var text = @"
using System;

using DICT1 = System.Collections.Generic.Dictionary<int, int>;

namespace foo
{
    using ACT = System.Action<DICT1, DICT1, DICT1, DICT1, DICT1, DICT1, DICT1>;
    
    class C
    {
        static void Main()
        {
            ACT ac = null;
            Console.Write(ac);
        }
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.DebugExe);

            var exebits = new MemoryStream();
            var pdbbits = new MemoryStream();
            var result = compilation.Emit(exebits, pdbbits);

            result.Diagnostics.Verify(
                Diagnostic(ErrorCode.WRN_DebugFullNameTooLong, "Main").WithArguments("AACT TSystem.Action`7[[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Collections.Generic.Dictionary`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
        }

        [WorkItem(1084059, "DevDiv")]
        [Fact]
        public void StaticType()
        {
            var source = @"
using static System.Math;

class D
{
    static void Main() 
    {
        Max(1, 2);
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            var expectedXml = @"
<symbols>
    <methods>
        <method containingType=""D"" name=""Main"" parameterNames="""">
            <customDebugInfo version=""4"" count=""1"">
                <using version=""4"" kind=""UsingInfo"" size=""12"" namespaceCount=""1"">
                    <namespace usingCount=""1""/>
                </using>
            </customDebugInfo>
            <sequencepoints total=""2"">
                <entry il_offset=""0x0"" start_row=""8"" start_column=""9"" end_row=""8"" end_column=""19"" file_ref=""0""/>
                <entry il_offset=""0x8"" start_row=""9"" start_column=""5"" end_row=""9"" end_column=""6"" file_ref=""0""/>
            </sequencepoints>
            <locals/>
            <scope startOffset=""0x0"" endOffset=""0x9"">
                <type name=""System.Math, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""/>
            </scope>
        </method>
    </methods>
</symbols>";

            AssertXmlEqual(expectedXml, GetPdbXml(comp, "D.Main"));
        }
    }
}