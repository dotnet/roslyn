// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class InterpolatedStringTests : CSharpTestBase
    {
        [Fact, WorkItem(33713, "https://github.com/dotnet/roslyn/issues/33713")]
        public void AlternateVerbatimString()
        {
            var source = @"
class C
{
    static void Main()
    {
        int i = 42;
        var s = @$""{i}
{i}"";
        System.Console.Write(s);
        var s2 = $@"""";
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: @"42
42");

            var tree = comp.SyntaxTrees.Single();
            var interpolatedStrings = tree.GetRoot().DescendantNodes().OfType<InterpolatedStringExpressionSyntax>().ToArray();
            var token1 = interpolatedStrings[0].StringStartToken;
            Assert.Equal("@$\"", token1.Text);
            Assert.Equal("@$\"", token1.ValueText);

            var token2 = interpolatedStrings[1].StringStartToken;
            Assert.Equal("$@\"", token2.Text);
            Assert.Equal("$@\"", token2.ValueText);

            foreach (var token in tree.GetRoot().DescendantTokens().Where(t => t.Kind() != SyntaxKind.EndOfFileToken))
            {
                Assert.False(string.IsNullOrEmpty(token.Text));
                Assert.False(string.IsNullOrEmpty(token.ValueText));
            }
        }

        [Fact]
        public void ConstInterpolations()
        {
            var source = @"
using System;

public class Test
{
    const string constantabc = ""abc"";
    const string constantnull = null;

    static void Main()
    {
        Console.WriteLine($"""");
        Console.WriteLine($""ABC"");
        Console.WriteLine($""{constantabc}"");
        Console.WriteLine($""{constantnull}"");
        Console.WriteLine($""{constantabc}{constantnull}"");
        Console.WriteLine($""({constantabc})({constantnull})"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
ABC
abc

abc
(abc)()
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  1
  IL_0000:  ldstr      """"
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""ABC""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ldstr      ""abc""
  IL_0019:  call       ""void System.Console.WriteLine(string)""
  IL_001e:  ldstr      """"
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ldstr      ""abc""
  IL_002d:  call       ""void System.Console.WriteLine(string)""
  IL_0032:  ldstr      ""(abc)()""
  IL_0037:  call       ""void System.Console.WriteLine(string)""
  IL_003c:  ret
}
");
        }

        [Fact]
        public void InterpolatedStringIsNeverNull()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        Console.WriteLine(M6(null) == """");
        Console.WriteLine(M6(""abc""));
    }

    static string M6(string a) => $""{a}"";

}
";
            var comp = CompileAndVerify(source, expectedOutput: @"True
abc
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.M6", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_000a
  IL_0004:  pop
  IL_0005:  ldstr      """"
  IL_000a:  ret
}
");
        }

        [Fact]
        public void ConcatenatedStringInterpolations()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        string a = ""a"";
        string b = ""b"";

        Console.WriteLine($""a: {a}"");
        Console.WriteLine($""{a + b}"");
        Console.WriteLine($""{a}{b}"");
        Console.WriteLine($""a: {a}, b: {b}"");
        Console.WriteLine($""{{{a}}}"");
        Console.WriteLine(""a:"" + $"" {a}"");
        Console.WriteLine($""a: {$""{a}, b: {b}""}"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a: a
ab
ab
a: a, b: b
{a}
a: a
a: a, b: b
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      134 (0x86)
  .maxstack  4
  .locals init (string V_0, //a
                string V_1) //b
  IL_0000:  ldstr      ""a""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""b""
  IL_000b:  stloc.1
  IL_000c:  ldstr      ""a: ""
  IL_0011:  ldloc.0
  IL_0012:  call       ""string string.Concat(string, string)""
  IL_0017:  call       ""void System.Console.WriteLine(string)""
  IL_001c:  ldloc.0
  IL_001d:  ldloc.1
  IL_001e:  call       ""string string.Concat(string, string)""
  IL_0023:  call       ""void System.Console.WriteLine(string)""
  IL_0028:  ldloc.0
  IL_0029:  ldloc.1
  IL_002a:  call       ""string string.Concat(string, string)""
  IL_002f:  call       ""void System.Console.WriteLine(string)""
  IL_0034:  ldstr      ""a: ""
  IL_0039:  ldloc.0
  IL_003a:  ldstr      "", b: ""
  IL_003f:  ldloc.1
  IL_0040:  call       ""string string.Concat(string, string, string, string)""
  IL_0045:  call       ""void System.Console.WriteLine(string)""
  IL_004a:  ldstr      ""{""
  IL_004f:  ldloc.0
  IL_0050:  ldstr      ""}""
  IL_0055:  call       ""string string.Concat(string, string, string)""
  IL_005a:  call       ""void System.Console.WriteLine(string)""
  IL_005f:  ldstr      ""a: ""
  IL_0064:  ldloc.0
  IL_0065:  call       ""string string.Concat(string, string)""
  IL_006a:  call       ""void System.Console.WriteLine(string)""
  IL_006f:  ldstr      ""a: ""
  IL_0074:  ldloc.0
  IL_0075:  ldstr      "", b: ""
  IL_007a:  ldloc.1
  IL_007b:  call       ""string string.Concat(string, string, string, string)""
  IL_0080:  call       ""void System.Console.WriteLine(string)""
  IL_0085:  ret
}
");
        }

        [Fact]
        public void NonConcatenatedInterpolations()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        object a = ""a"";

        Console.WriteLine($""{a}"");
        Console.WriteLine($""a: {a}"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a
a: a
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (object V_0) //a
  IL_0000:  ldstr      ""a""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""{0}""
  IL_000b:  ldloc.0
  IL_000c:  call       ""string string.Format(string, object)""
  IL_0011:  call       ""void System.Console.WriteLine(string)""
  IL_0016:  ldstr      ""a: {0}""
  IL_001b:  ldloc.0
  IL_001c:  call       ""string string.Format(string, object)""
  IL_0021:  call       ""void System.Console.WriteLine(string)""
  IL_0026:  ret
}
");
        }

        [Fact]
        public void ExpressionsAreNotOptimized()
        {
            var source = @"
using System;
using System.Linq.Expressions;

public class Test
{
    static void Main()
    {
        Expression<Func<string, string>> f = a => $""a: {a}"";

        Console.Write(f);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a => Format(""a: {0}"", a)");

            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/runtime/issues/44678")]
        public void AlignmentWithNegativeValue()
        {
            // This test verifies that negative alignment values are formatted using invariant culture.
            // The issue occurs when compiling under non-US cultures (e.g., Swedish sv-SE) where the
            // minus sign is formatted as '−' (U+2212) instead of '-' (U+002D), causing runtime errors.
            var source = """
                using System;

                public class Test
                {
                    static void Main()
                    {
                        var st = "1";
                        var st2 = $"{st,100}";  // Positive alignment
                        var st3 = $"{st,-100}"; // Negative alignment - this is the problematic case
                        Console.WriteLine(st2);
                        Console.WriteLine(st3);
                    }
                }
                """;

            // Test under Swedish culture (sv-SE) where minus sign is represented differently
            using (new CultureContext(new System.Globalization.CultureInfo("sv-SE", useUserOverride: false)))
            {
                var comp = CompileAndVerify(source, expectedOutput: """
                                                                                                       1
                    1                                                                                                   
                    """);

                comp.VerifyDiagnostics();

                // Verify that the emitted IL contains the correct format string with a proper minus sign
                comp.VerifyIL("Test.Main", """
                    {
                      // Code size       41 (0x29)
                      .maxstack  2
                      .locals init (string V_0, //st
                                    string V_1) //st2
                      IL_0000:  ldstr      "1"
                      IL_0005:  stloc.0
                      IL_0006:  ldstr      "{0,100}"
                      IL_000b:  ldloc.0
                      IL_000c:  call       "string string.Format(string, object)"
                      IL_0011:  stloc.1
                      IL_0012:  ldstr      "{0,-100}"
                      IL_0017:  ldloc.0
                      IL_0018:  call       "string string.Format(string, object)"
                      IL_001d:  ldloc.1
                      IL_001e:  call       "void System.Console.WriteLine(string)"
                      IL_0023:  call       "void System.Console.WriteLine(string)"
                      IL_0028:  ret
                    }
                    """);
            }
        }
    }
}
