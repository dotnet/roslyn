// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
    const char constantchar = 'd';

    static void Main()
    {
        Console.WriteLine($"""");
        Console.WriteLine($""ABC"");
        Console.WriteLine($""{constantabc}"");
        Console.WriteLine($""{constantnull}"");
        Console.WriteLine($""{constantabc}{constantnull}"");
        Console.WriteLine($""({constantabc})({constantnull})"");
        Console.WriteLine($""{constantabc}{constantchar}"");
        Console.WriteLine($""{constantabc}({constantchar})"");
        Console.WriteLine($""{constantchar}"");
        Console.WriteLine($""{constantnull}"");
        Console.WriteLine($""{constantnull}{constantabc}{default}{constantnull}{null}"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
ABC
abc

abc
(abc)()
abcd
abc(d)
d

abc
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      111 (0x6f)
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
  IL_003c:  ldstr      ""abcd""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldstr      ""abc(d)""
  IL_004b:  call       ""void System.Console.WriteLine(string)""
  IL_0050:  ldstr      ""d""
  IL_0055:  call       ""void System.Console.WriteLine(string)""
  IL_005a:  ldstr      """"
  IL_005f:  call       ""void System.Console.WriteLine(string)""
  IL_0064:  ldstr      ""abc""
  IL_0069:  call       ""void System.Console.WriteLine(string)""
  IL_006e:  ret
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
        const string c = ""c"";
        const char d = 'd';
        const string n = null;

        Console.WriteLine($""a: {a}"");
        Console.WriteLine($""{a + b}"");
        Console.WriteLine($""{a}{b}"");
        Console.WriteLine($""a: {a}, b: {b}"");
        Console.WriteLine($""{{{a}}}"");
        Console.WriteLine(""a:"" + $"" {a}"");
        Console.WriteLine($""a: {$""{a}, b: {b}""}"");
        Console.WriteLine($""acd: {a}{c}{d}, b: {b}"");
        Console.WriteLine($""{{{'{'}{""{""}{a}{""}""}{'}'}}}"");
        Console.WriteLine($""{a}{n}{b}{null}{a}{null}{c}"");
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
acd: acd, b: b
{{{a}}}
abac
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size      195 (0xc3)
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
  IL_0085:  ldstr      ""acd: ""
  IL_008a:  ldloc.0
  IL_008b:  ldstr      ""cd, b: ""
  IL_0090:  ldloc.1
  IL_0091:  call       ""string string.Concat(string, string, string, string)""
  IL_0096:  call       ""void System.Console.WriteLine(string)""
  IL_009b:  ldstr      ""{{{""
  IL_00a0:  ldloc.0
  IL_00a1:  ldstr      ""}}}""
  IL_00a6:  call       ""string string.Concat(string, string, string)""
  IL_00ab:  call       ""void System.Console.WriteLine(string)""
  IL_00b0:  ldloc.0
  IL_00b1:  ldloc.1
  IL_00b2:  ldloc.0
  IL_00b3:  ldstr      ""c""
  IL_00b8:  call       ""string string.Concat(string, string, string, string)""
  IL_00bd:  call       ""void System.Console.WriteLine(string)""
  IL_00c2:  ret
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
        const string b = ""b"";
        const char c = 'c';

        Console.WriteLine($""{a}"");
        Console.WriteLine($""a: {a}"");
        Console.WriteLine($""a: {a}, b: {b}, c: {c}"");
        Console.WriteLine($""{{{'{'}{""{""}{a}{""}""}{'}'}}}"");
        Console.WriteLine($""{null}{b}{a}{null}{a}{null}"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a
a: a
a: a, b: b, c: c
{{{a}}}
baa
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       88 (0x58)
  .maxstack  3
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
  IL_0026:  ldstr      ""a: {0}, b: b, c: c""
  IL_002b:  ldloc.0
  IL_002c:  call       ""string string.Format(string, object)""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ldstr      ""{{{{{{{0}}}}}}}""
  IL_003b:  ldloc.0
  IL_003c:  call       ""string string.Format(string, object)""
  IL_0041:  call       ""void System.Console.WriteLine(string)""
  IL_0046:  ldstr      ""b{0}{1}""
  IL_004b:  ldloc.0
  IL_004c:  ldloc.0
  IL_004d:  call       ""string string.Format(string, object, object)""
  IL_0052:  call       ""void System.Console.WriteLine(string)""
  IL_0057:  ret
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

        [Fact]
        public void ExpressionsAreNotOptimized2()
        {
            string toObject = Environment.Version.Major > 4 ? ", Object" : "";
            var source = @"
using System;
using System.Linq.Expressions;

public class Test
{
    static void Main()
    {
        const char c = 'c';
        Expression<Func<string, string>> f = a => $""a: {a} c: {c} f: {nameof(f)}"";

        Console.Write(f);
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"a => Format(""a: {0} c: {1} f: {2}"", a, Convert(c" + toObject + @"), ""f"")");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void CombinationWithNonConcatenationAndStringConstants()
        {
            var source = @"
using System;

public class Test
{
    static void Main()
    {
        object a = ""a"";
        const string cd = ""cd"";
        const char f = 'f';

        Console.WriteLine($""{a}b{cd}e{f}g"");
    }
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"abcdefg
");

            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       23 (0x17)
  .maxstack  2
  .locals init (object V_0) //a
  IL_0000:  ldstr      ""a""
  IL_0005:  stloc.0
  IL_0006:  ldstr      ""{0}bcdefg""
  IL_000b:  ldloc.0
  IL_000c:  call       ""string string.Format(string, object)""
  IL_0011:  call       ""void System.Console.WriteLine(string)""
  IL_0016:  ret
}
");
        }
    }
}
