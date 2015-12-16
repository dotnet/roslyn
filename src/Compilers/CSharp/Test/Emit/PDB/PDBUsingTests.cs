// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.MetadataUtilities;
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
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""30"" endLine=""16"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System.Threading"" />
        <namespace name=""System.IO"" />
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""24"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System.IO"" />
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""22"" endLine=""4"" endColumn=""23"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""30"" endLine=""16"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""R"" target=""System.Threading"" kind=""namespace"" />
        <alias name=""Q"" target=""System.IO"" kind=""namespace"" />
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""24"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.IO"" kind=""namespace"" />
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""22"" endLine=""4"" endColumn=""23"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System"" kind=""namespace"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""30"" endLine=""16"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""R"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""Q"" target=""System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""24"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""22"" endLine=""4"" endColumn=""23"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""30"" endLine=""16"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""Q"" target=""System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""24"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""Q"" target=""System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""22"" endLine=""4"" endColumn=""23"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""P"" target=""System.Collections.Generic.List`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void TestExternAliases1()
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

            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""22"" endLine=""4"" endColumn=""23"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""P"" />
        <externinfo alias=""P"" assembly=""a, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""Q"" assembly=""b, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""R"" assembly=""c, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
        <forwardToModule declaringType=""A"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""24"" endLine=""10"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""26"" endLine=""10"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""Q"" />
        <extern alias=""P"" />
      </scope>
    </method>
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
          <namespace usingCount=""1"" />
        </using>
        <forwardToModule declaringType=""A"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""30"" endLine=""16"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""R"" />
        <extern alias=""Q"" />
        <extern alias=""P"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact, WorkItem(1120579)]
        public void TestExternAliases2()
        {
            string source1 = @"
namespace U.V.W {}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll, assemblyName: "TestExternAliases2");

            string source2 = @"
using U.V.W;
 
class A { void M() {  } }
";
            var compilation2 = CreateCompilationWithMscorlib(
                source2,
                options: TestOptions.DebugDll,
                references: new[]
                {
                // first unaliased reference 
                compilation1.ToMetadataReference(),
                // second aliased reference
                compilation1.ToMetadataReference(ImmutableArray.Create("X"))
                });

            compilation2.VerifyPdb("A.M", @"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""23"" endLine=""4"" endColumn=""24"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""U.V.W"" />
        <externinfo alias=""X"" assembly=""TestExternAliases2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact, WorkItem(1120579)]
        public void TestExternAliases3()
        {
            string source1 = @"
namespace U.V.W {}
";

            var compilation1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll, assemblyName: "TestExternAliases3");

            string source2 = @"
using U.V.W;
 
class A { void M() {  } }
";
            var compilation2 = CreateCompilationWithMscorlib(
                source2,
                options: TestOptions.DebugDll,
                references: new[]
                {
                // first aliased reference
                compilation1.ToMetadataReference(ImmutableArray.Create("X")),
                // second unaliased reference 
                compilation1.ToMetadataReference(),
                });

            compilation2.VerifyPdb("A.M", @"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""23"" endLine=""4"" endColumn=""24"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""U.V.W"" />
        <externinfo alias=""X"" assembly=""TestExternAliases3, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void ExternAliases4()
        {
            var src1 = @"
namespace N
{
    public class C { }
}";
            var dummyCompilation = CreateCompilationWithMscorlib(src1, assemblyName: "A", options: TestOptions.DebugDll);

            var src2 = @"
namespace M
{
    extern alias A;
    using A::N;
    
    public class D
    {
        public C P
        {
            get { return new C(); }
            set { }
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(src2,
                assemblyName: GetUniqueName(),
                options: TestOptions.DebugDll,
                references: new[]
                {
                    new CSharpCompilationReference(dummyCompilation, ImmutableArray.Create("A", "A")),
                });

            compilation.VerifyDiagnostics();
            compilation.VerifyEmitDiagnostics();
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

            compilation.VerifyPdb(@"
<symbols>
    <methods>
        <method containingType=""C"" name=""M"">
            <customDebugInfo>
                <using>
                    <namespace usingCount=""5""/>
                </using>
            </customDebugInfo>
            <sequencePoints>
                <entry offset=""0x0"" startLine=""8"" startColumn=""20"" endLine=""8"" endColumn=""21""/>
                <entry offset=""0x1"" startLine=""8"" startColumn=""22"" endLine=""8"" endColumn=""23""/>
            </sequencePoints>
            <scope startOffset=""0x0"" endOffset=""0x2"">
                <extern alias=""A""/>
                <extern alias=""B""/>
                <alias name=""X"" target=""N"" kind=""namespace""/>
                <alias name=""Y"" target=""N"" kind=""namespace""/>
                <alias name=""Z"" target=""N"" kind=""namespace""/>
                <externinfo alias=""A"" assembly=""A, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""/>
                <externinfo alias=""B"" assembly=""B, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""/>
            </scope>
        </method>
    </methods>
</symbols>");
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

            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""N.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""7"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""24"" endLine=""12"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""26"" endLine=""12"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <extern alias=""P"" />
        <namespace qualifier=""P"" name=""N"" />
        <namespace name=""N"" />
        <alias name=""Q"" target=""N.A, Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""R"" qualifier=""P"" target=""N"" kind=""namespace"" />
        <alias name=""S"" target=""N.B"" kind=""type"" />
        <alias name=""T"" target=""N"" kind=""namespace"" />
        <externinfo alias=""P"" assembly=""Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>");
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

            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""A"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""20"" endLine=""7"" endColumn=""21"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""22"" endLine=""7"" endColumn=""23"" />
      </sequencePoints>
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
    <method containingType=""X.B"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""A"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""24"" endLine=""16"" endColumn=""25"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""26"" endLine=""16"" endColumn=""27"" />
      </sequencePoints>
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
    <method containingType=""X.Y.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""A"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""25"" startColumn=""28"" endLine=""25"" endColumn=""29"" />
        <entry offset=""0x1"" startLine=""25"" startColumn=""30"" endLine=""25"" endColumn=""31"" />
      </sequencePoints>
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
</symbols>");
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
            compilation2.VerifyPdb(@"
<symbols>
    <methods>
        <method containingType=""C"" name=""Main"">
            <customDebugInfo>
                <using>
                    <namespace usingCount=""6""/>
                </using>
            </customDebugInfo>
            <sequencePoints>
                <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6""/>
                <entry offset=""0x1"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""43""/>
                <entry offset=""0xc"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""43""/>
                <entry offset=""0x17"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""45""/>
                <entry offset=""0x22"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""45""/>
                <entry offset=""0x2d"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6""/>
            </sequencePoints>
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
");
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
            compilation2.VerifyPdb(@"
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
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""45"" />
        <entry offset=""0xc"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""48"" />
        <entry offset=""0x17"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""48"" />
        <entry offset=""0x22"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <extern alias=""A"" />
        <extern alias=""B"" />
        <externinfo alias=""B"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
        <externinfo alias=""A"" assembly=""" + compilation1.AssemblyName + @", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" />
      </scope>
    </method>
  </methods>
</symbols>
");
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

            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""30"" startColumn=""26"" endLine=""30"" endColumn=""27"" />
        <entry offset=""0x1"" startLine=""30"" startColumn=""28"" endLine=""30"" endColumn=""29"" />
      </sequencePoints>
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
    <method containingType=""X.C"" name=""N1"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""X.C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""19"" endLine=""17"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""21"" endLine=""17"" endColumn=""22"" />
      </sequencePoints>
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
    <method containingType=""X.C"" name=""N2"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""X.C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""31"" startColumn=""19"" endLine=""31"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""31"" startColumn=""21"" endLine=""31"" endColumn=""22"" />
      </sequencePoints>
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
</symbols>");
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

            compilation.VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""X.C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""26"" endLine=""16"" endColumn=""27"" />
        <entry offset=""0x1"" startLine=""16"" startColumn=""28"" endLine=""16"" endColumn=""29"" />
      </sequencePoints>
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
    <method containingType=""X.C"" name=""N1"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""X.C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""19"" endLine=""17"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""21"" endLine=""17"" endColumn=""22"" />
      </sequencePoints>
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
    <method containingType=""X.C"" name=""N2"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""4"" />
          <namespace usingCount=""4"" />
        </using>
        <forwardToModule declaringType=""X.C"" methodName=""M"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""19"" endLine=""17"" endColumn=""20"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""21"" endLine=""17"" endColumn=""22"" />
      </sequencePoints>
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
</symbols>");
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

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
    <methods>
        <method containingType=""X.C"" name="".ctor"">
            <customDebugInfo>
                <using>
                    <namespace usingCount=""1""/>
                    <namespace usingCount=""0""/>
                </using>
            </customDebugInfo>
            <sequencePoints>
                <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19""/>
                <entry offset=""0x7"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""19""/>
            </sequencePoints>
            <scope startOffset=""0x0"" endOffset=""0x16"">
                <namespace name=""System""/>
            </scope>
        </method>
        <method containingType=""X.C"" name="".cctor"">
            <customDebugInfo>
                <forward declaringType=""X.C"" methodName="".ctor""/>
            </customDebugInfo>
            <sequencePoints>
                <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""27""/>
                <entry offset=""0x6"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""27""/>
            </sequencePoints>
        </method>
    </methods>
</symbols>");
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
            CompileAndVerify(text, new[] { SystemCoreRef }, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLambdaMap>
          <methodOrdinal>2</methodOrdinal>
          <lambda offset=""-23"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""59"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x38"">
        <namespace name=""System.Linq"" />
      </scope>
    </method>
    <method containingType=""C"" name="".cctor"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLambdaMap>
          <methodOrdinal>3</methodOrdinal>
          <lambda offset=""-38"" />
        </encLambdaMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""10"" endColumn=""8"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.ctor&gt;b__2_0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""-23"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""35"" endLine=""6"" endColumn=""36"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""37"" endLine=""6"" endColumn=""55"" />
        <entry offset=""0xa"" startLine=""6"" startColumn=""56"" endLine=""6"" endColumn=""57"" />
      </sequencePoints>
    </method>
    <method containingType=""C+&lt;&gt;c"" name=""&lt;.cctor&gt;b__3_0"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""-38"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""27"" />
        <entry offset=""0xa"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
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

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_P1"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""14"" endLine=""6"" endColumn=""18"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_P1"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""19"" endLine=""6"" endColumn=""23"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_P2"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""18"" endLine=""7"" endColumn=""19"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""20"" endLine=""7"" endColumn=""29"" />
        <entry offset=""0x5"" startLine=""7"" startColumn=""30"" endLine=""7"" endColumn=""31"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method containingType=""C"" name=""set_P2"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P2"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""36"" endLine=""7"" endColumn=""37"" />
        <entry offset=""0x1"" startLine=""7"" startColumn=""38"" endLine=""7"" endColumn=""39"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""add_E2"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P2"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""34"" endLine=""10"" endColumn=""35"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""36"" endLine=""10"" endColumn=""37"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""remove_E2"" parameterNames=""value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P2"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""45"" endLine=""10"" endColumn=""46"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""47"" endLine=""10"" endColumn=""48"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""get_Item"" parameterNames=""x"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P2"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""27"" endLine=""8"" endColumn=""28"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""29"" endLine=""8"" endColumn=""38"" />
        <entry offset=""0x5"" startLine=""8"" startColumn=""39"" endLine=""8"" endColumn=""40"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""set_Item"" parameterNames=""x, value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_P2"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""45"" endLine=""8"" endColumn=""46"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""47"" endLine=""8"" endColumn=""48"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
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

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""Base"" name=""get_P"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""28"" endLine=""6"" endColumn=""32"" />
      </sequencePoints>
    </method>
    <method containingType=""Base"" name=""set_P"" parameterNames=""value"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""33"" endLine=""6"" endColumn=""37"" />
      </sequencePoints>
    </method>
    <method containingType=""Derived"" name=""set_P"" parameterNames=""value"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""40"" endLine=""11"" endColumn=""41"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""42"" endLine=""11"" endColumn=""43"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
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

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyPdb(@"
<symbols>
  <methods>
    <method containingType=""C"" name=""get_Item"" parameterNames=""x"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""34"" endLine=""18"" endColumn=""35"" />
        <entry offset=""0x1"" startLine=""18"" startColumn=""36"" endLine=""18"" endColumn=""45"" />
        <entry offset=""0x5"" startLine=""18"" startColumn=""46"" endLine=""18"" endColumn=""47"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x7"">
        <namespace name=""System.Runtime.CompilerServices"" />
      </scope>
    </method>
    <method containingType=""C"" name=""set_Item"" parameterNames=""x, value"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName=""get_Item"" parameterNames=""x"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""52"" endLine=""18"" endColumn=""53"" />
        <entry offset=""0x1"" startLine=""18"" startColumn=""54"" endLine=""18"" endColumn=""55"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
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
            CompileAndVerify(source, new[] { libRef }, options: TestOptions.DebugExe).VerifyPdb("Test.Main", @"
<symbols>
  <entryPoint declaringType=""Test"" methodName=""Main"" />
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""I"" target=""Outer+Inner, Lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            var comp = CreateCompilationWithMscorlib(source);

            // As in dev12, we drop all '@'s.
            comp.VerifyPdb("Test.Main", @"
<symbols>
  <methods>
    <method containingType=""Test"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""20"" startColumn=""35"" endLine=""20"" endColumn=""36"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <namespace name=""namespace"" />
        <alias name=""object"" target=""namespace"" kind=""namespace"" />
        <alias name=""string"" target=""namespace.class`1+struct[namespace.interface]"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            comp.VerifyPdb("N.D.Main", @"
<symbols>
  <methods>
    <method containingType=""N.D"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
          <namespace usingCount=""3"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""30"" endLine=""13"" endColumn=""31"" />
      </sequencePoints>
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
</symbols>");
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
            comp.VerifyPdb("D.Main", @"
<symbols>
  <methods>
    <method containingType=""D"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""26"" endLine=""6"" endColumn=""27"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1"">
        <alias name=""AD"" target=""System.Action`1[[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            var comp1 = CreateCompilationWithMscorlib(source1, options: TestOptions.DebugDll, assemblyName: "Comp1");

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
            var comp2 = CreateCompilationWithMscorlib(source2, new[] { comp1.ToMetadataReference() }, options: TestOptions.DebugExe);

            comp2.VerifyPdb(@"
<symbols>
  <entryPoint declaringType=""C1"" methodName=""Main"" />
  <methods>
    <method containingType=""C1"" name=""Main"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""6"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" />
        <entry offset=""0x1"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""t1"" target=""Y`1[[W[], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t2"" target=""Y`1[[W[,], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t3"" target=""Y`1[[W[][,], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t4"" target=""Y`1[[Y`1[[W, Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][,][], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t5"" target=""Y`1+Z`1[[W[][,], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[W[,,][], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
        <alias name=""t6"" target=""Y`1[[Y`1+Z`1[[Y`1+F[[System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]][][,], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null],[Y`1+F[[W[][,], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]][][], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]], Comp1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
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
            comp.VerifyPdb("D.Main", @"
<symbols>
    <methods>
        <method containingType=""D"" name=""Main"">
            <customDebugInfo>
                <using>
                    <namespace usingCount=""1""/>
                </using>
            </customDebugInfo>
            <sequencePoints>
                <entry offset=""0x0"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""19""/>
                <entry offset=""0x8"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6""/>
            </sequencePoints>
            <scope startOffset=""0x0"" endOffset=""0x9"">
                <type name=""System.Math, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""/>
            </scope>
        </method>
    </methods>
</symbols>");
        }

        [Fact]
        public void UnusedImports()
        {
            var source = @"
extern alias A;
using System;
using X = A::System.Linq.Enumerable;
using Y = A::System.Linq;
using Z = System.Data.DataColumn;
using F = System.Func<int>;

class C
{
    static void Main() 
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef.WithAliases(new[] { "A" }), SystemDataRef });
            var v = CompileAndVerify(comp, validator: (peAssembly) =>
            {
                var reader = peAssembly.ManifestModule.MetadataReader;

                Assert.Equal(new[]
                {
                    "mscorlib",
                    "System.Core",
                    "System.Data"
                }, peAssembly.AssemblyReferences.Select(ai => ai.Name));

                Assert.Equal(new[]
                {
                    "CompilationRelaxationsAttribute",
                    "RuntimeCompatibilityAttribute",
                    "DebuggableAttribute",
                    "DebuggingModes",
                    "Object",
                    "Func`1",
                    "Enumerable",
                    "DataColumn"
                }, reader.TypeReferences.Select(h => reader.GetString(reader.GetTypeReference(h).Name)));

                Assert.Equal(1, reader.GetTableRowCount(TableIndex.TypeSpec));
            });
        }

        [Fact]
        public void UnusedImports_Nonexisting()
        {
            var source = @"
extern alias A;
using B;
using X = C.D;
using Y = A::E;
using Z = F<int>;

class C
{
    static void Main() 
    {
    }
}
";
            var comp = CreateCompilationWithMscorlib(source);

            comp.VerifyDiagnostics(
                // (6,11): error CS0246: The type or namespace name 'F<>' could not be found (are you missing a using directive or an assembly reference?)
                // using Z = F<int>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "F<int>").WithArguments("F<>").WithLocation(6, 11),
                // (5,14): error CS0234: The type or namespace name 'E' does not exist in the namespace 'A' (are you missing an assembly reference?)
                // using Y = A::E;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "E").WithArguments("E", "A").WithLocation(5, 14),
                // (4,13): error CS0426: The type name 'D' does not exist in the type 'C'
                // using X = C.D;
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "D").WithArguments("D", "C").WithLocation(4, 13),
                // (2,14): error CS0430: The extern alias 'A' was not specified in a /reference option
                // extern alias A;
                Diagnostic(ErrorCode.ERR_BadExternAlias, "A").WithArguments("A").WithLocation(2, 14),
                // (3,7): error CS0246: The type or namespace name 'B' could not be found (are you missing a using directive or an assembly reference?)
                // using B;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "B").WithArguments("B").WithLocation(3, 7),
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using Y = A::E;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Y = A::E;").WithLocation(5, 1),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using B;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using B;").WithLocation(3, 1),
                // (4,1): hidden CS8019: Unnecessary using directive.
                // using X = C.D;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = C.D;").WithLocation(4, 1),
                // (6,1): hidden CS8019: Unnecessary using directive.
                // using Z = F<int>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using Z = F<int>;").WithLocation(6, 1)
                );
        }

        [Fact]
        public void EmittingPdbVsNot()
        {
            string source = @"
using System;
using X = System.IO.FileStream;

class C
{
    int x = 1;
    static int y = 1;

    C()
    {
        Console.WriteLine();
    }
}
";

            var c = CreateCompilationWithMscorlib(source, assemblyName: "EmittingPdbVsNot", options: TestOptions.ReleaseDll);

            var peStream1 = new MemoryStream();
            var peStream2 = new MemoryStream();
            var pdbStream = new MemoryStream();

            var emitResult1 = c.Emit(peStream: peStream1, pdbStream: pdbStream);
            var emitResult2 = c.Emit(peStream: peStream2);

            PdbValidation.VerifyMetadataEqualModuloMvid(peStream1, peStream2);
        }

        [Fact]
        public void ImportedNoPiaTypes()
        {
            var sourceLib = @"
using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: Guid(""11111111-1111-1111-1111-111111111111"")]
[assembly: ImportedFromTypeLib(""Foo"")]
[assembly: TypeLibVersion(1, 0)]

namespace N
{
    public enum E
    {
        Value1 = 1
    }

    public struct S1
    {
        public int A1;
        public int A2;
    }

    public struct S2
    {
        public const int Value2 = 2;
    }

    public struct SBad
    {
        public int A3;
        public const int Value3 = 3;
    }

    [ComImport, Guid(""22222222-2222-2222-2222-222222222222"")]
    public interface I
    {
        void F();
    }

    public interface IBad
    {
        void F();
    }
}
";
            var source = @"
using System;

using static N.E;
using static N.SBad;
using Z1 = N.S1;
using Z2 = N.S2;
using ZBad = N.SBad;
using NI = N.I;
using NIBad = N.IBad;

class C
{
    NI i;

    void M()
    {
        Console.WriteLine(Value1);
        Console.WriteLine(Z2.Value2);
        Console.WriteLine(new Z1());
    }
}
";
            var libRef = CreateCompilationWithMscorlib(sourceLib, assemblyName: "ImportedNoPiaTypesAssemblyName").EmitToImageReference(embedInteropTypes: true);
            var compilation = CreateCompilationWithMscorlib(source, new[] { libRef });
            var v = CompileAndVerify(compilation);

            v.Diagnostics.Verify(
                // (14,8): warning CS0169: The field 'C.i' is never used
                //     NI i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("C.i"),
                // (5,1): hidden CS8019: Unnecessary using directive.
                // using static N.SBad;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static N.SBad;"),
                // (10,1): hidden CS8019: Unnecessary using directive.
                // using NIBad = N.IBad;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using NIBad = N.IBad;"),
                // (8,1): hidden CS8019: Unnecessary using directive.
                // using ZBad = N.SBad;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using ZBad = N.SBad;"));

            // Usings of embedded types are currently omitted:
            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""9"" endLine=""18"" endColumn=""35"" />
        <entry offset=""0xb"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""38"" />
        <entry offset=""0x11"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""37"" />
        <entry offset=""0x24"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x25"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void ImportedTypeWithUnknownBase()
        {
            var sourceLib1 = @"
namespace N
{
    public class A { }
}
";
            var sourceLib2 = @"
namespace N
{
    public class B : A { }
}
";
            var source = @"
using System;
using X = N.B;

class C
{
    void M()
    {
        Console.WriteLine();
    }
}
";
            var libRef1 = CreateCompilationWithMscorlib(sourceLib1).EmitToImageReference();
            var libRef2 = CreateCompilationWithMscorlib(sourceLib2, new[] { libRef1 }, assemblyName: "LibRef2").EmitToImageReference();
            var compilation = CreateCompilationWithMscorlib(source, new[] { libRef2 });
            var v = CompileAndVerify(compilation);

            v.Diagnostics.Verify(
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using X = N.B;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using X = N.B;"));

            v.VerifyPdb("C.M", @"
<symbols>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""29"" />
        <entry offset=""0x5"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x6"">
        <namespace name=""System"" />
        <alias name=""X"" target=""N.B, LibRef2, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>");
        }
    }
}
