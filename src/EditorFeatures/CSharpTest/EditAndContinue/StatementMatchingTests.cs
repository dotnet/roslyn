// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class StatementMatchingTests : EditingTestBase
    {
        #region Known Matches

        [Fact]
        public void KnownMatches()
        {
            var src1 = @"
Console.WriteLine(1)/*1*/;
Console.WriteLine(1)/*2*/;
";

            var src2 = @"
Console.WriteLine(1)/*3*/;
Console.WriteLine(1)/*4*/;
";

            var m1 = MakeMethodBody(src1);
            var m2 = MakeMethodBody(src2);

            var knownMatches = new KeyValuePair<SyntaxNode, SyntaxNode>[]
            {
                new KeyValuePair<SyntaxNode, SyntaxNode>(((BlockSyntax)m1.RootNodes.First()).Statements[1], ((BlockSyntax)m2.RootNodes.First()).Statements[0])
            };

            // pre-matched:

            var match = m1.ComputeSingleRootMatch(m2, knownMatches);

            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "Console.WriteLine(1)/*1*/;", "Console.WriteLine(1)/*4*/;" },
                { "Console.WriteLine(1)/*2*/;", "Console.WriteLine(1)/*3*/;" }
            };

            expected.AssertEqual(actual);

            // not pre-matched:

            match = m1.ComputeSingleRootMatch(m2, knownMatches: null);

            actual = ToMatchingPairs(match);

            expected = new MatchingPairs
            {
                { "Console.WriteLine(1)/*1*/;", "Console.WriteLine(1)/*3*/;" },
                { "Console.WriteLine(1)/*2*/;", "Console.WriteLine(1)/*4*/;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void KnownMatches_Root()
        {
            var src1 = @"
Console.WriteLine(1);
";

            var src2 = @"
Console.WriteLine(2);
";

            var m1 = MakeMethodBody(src1);
            var m2 = MakeMethodBody(src2);

            var knownMatches = new[] { new KeyValuePair<SyntaxNode, SyntaxNode>(m1.RootNodes.First(), m2.RootNodes.First()) };
            var match = m1.ComputeSingleRootMatch(m2, knownMatches);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "Console.WriteLine(1);", "Console.WriteLine(2);" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Statements

        [Fact]
        public void MiscStatements()
        {
            var src1 = @"
int x = 1; 
Console.WriteLine(1);
x++/*1A*/;
Console.WriteLine(2);

while (true)
{
    x++/*2A*/;
}

Console.WriteLine(1);
";
            var src2 = @"
int x = 1;
x++/*1B*/;
for (int i = 0; i < 10; i++) {}
y++;
if (x > 1)
{
    while (true)
    {
        x++/*2B*/;
    }

    Console.WriteLine(1);
}";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "int x = 1;", "int x = 1;" },
                { "int x = 1", "int x = 1" },
                { "x = 1", "x = 1" },
                { "Console.WriteLine(1);", "Console.WriteLine(1);" },
                { "x++/*1A*/;", "x++/*1B*/;" },
                { "Console.WriteLine(2);", "y++;" },
                { "while (true) {     x++/*2A*/; }", "while (true)     {         x++/*2B*/;     }" },
                { "{     x++/*2A*/; }", "{         x++/*2B*/;     }" },
                { "x++/*2A*/;", "x++/*2B*/;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ThrowException_UpdateInsert()
        {
            var src1 = @"
return a > 3 ? a : throw new Exception();
return c > 7 ? c : 7;
";

            var src2 = @"
return a > 3 ? a : throw new ArgumentException();
return c > 7 ? c : throw new IndexOutOfRangeException();
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "return a > 3 ? a : throw new Exception();", "return a > 3 ? a : throw new ArgumentException();" },
                { "return c > 7 ? c : 7;", "return c > 7 ? c : throw new IndexOutOfRangeException();" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ThrowException_UpdateDelete()
        {
            var src1 = @"
return a > 3 ? a : throw new Exception();
return b > 5 ? b : throw new OperationCanceledException();
";

            var src2 = @"
return a > 3 ? a : throw new ArgumentException();
return b > 5 ? b : 5;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "return a > 3 ? a : throw new Exception();", "return a > 3 ? a : throw new ArgumentException();" },
                { "return b > 5 ? b : throw new OperationCanceledException();", "return b > 5 ? b : 5;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Tuple()
        {
            var src1 = @"
return (1, 2);
return (d, 6);
return (10, e, 22);
return (2, () => { 
    int a = 6;
    return 1;
});";

            var src2 = @"
return (1, 2, 3);
return (d, 5);
return (10, e);
return (2, () => {
    int a = 6;
    return 5;
});";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "return (1, 2);", "return (1, 2, 3);" },
                { "return (d, 6);", "return (d, 5);" },
                { "return (10, e, 22);", "return (10, e);" },
                { "return (2, () => {      int a = 6;     return 1; });", "return (2, () => {     int a = 6;     return 5; });" },
                { "() => {      int a = 6;     return 1; }", "() => {     int a = 6;     return 5; }" },
                { "()", "()" },
                { "{      int a = 6;     return 1; }", "{     int a = 6;     return 5; }" },
                { "int a = 6;", "int a = 6;" },
                { "int a = 6", "int a = 6" },
                { "a = 6", "a = 6" },
                { "return 1;", "return 5;" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Local Variables

        [Fact]
        public void Locals_Rename()
        {
            var src1 = @"
int x = 1;
";
            var src2 = @"
int y = 1;
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "int x = 1;", "int y = 1;" },
                { "int x = 1", "int y = 1" },
                { "x = 1", "y = 1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Locals_TypeChange()
        {
            var src1 = @"
int x = 1;
";
            var src2 = @"
byte x = 1;
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "int x = 1;", "byte x = 1;" },
                { "int x = 1", "byte x = 1" },
                { "x = 1", "x = 1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void BlocksWithLocals1()
        {
            var src1 = @"
{
    int a = 1;
}
{
    int b = 2;
}
";
            var src2 = @"
{
    int a = 3;
    int b = 4;
}
{
    int b = 5;
}
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "{     int a = 1; }", "{     int a = 3;     int b = 4; }" },
                { "int a = 1;", "int a = 3;" },
                { "int a = 1", "int a = 3" },
                { "a = 1", "a = 3" },
                { "{     int b = 2; }", "{     int b = 5; }" },
                { "int b = 2;", "int b = 5;" },
                { "int b = 2", "int b = 5" },
                { "b = 2", "b = 5" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void IfBlocksWithLocals1()
        {
            var src1 = @"
if (X)
{
    int a = 1;
}
if (Y)
{
    int b = 2;
}
";
            var src2 = @"
if (Y)
{
    int a = 3;
    int b = 4;
}
if (X)
{
    int b = 5;
}
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "if (X) {     int a = 1; }", "if (Y) {     int a = 3;     int b = 4; }" },
                { "{     int a = 1; }", "{     int a = 3;     int b = 4; }" },
                { "int a = 1;", "int a = 3;" },
                { "int a = 1", "int a = 3" },
                { "a = 1", "a = 3" },
                { "if (Y) {     int b = 2; }", "if (X) {     int b = 5; }" },
                { "{     int b = 2; }", "{     int b = 5; }" },
                { "int b = 2;", "int b = 5;" },
                { "int b = 2", "int b = 5" },
                { "b = 2", "b = 5" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void BlocksWithLocals2()
        {
            var src1 = @"
{
    int a = 1;
}
{
    {
        int b = 2;
    }
}
";
            var src2 = @"
{
    int b = 1;
}
{
    {
        int a = 2;
    }
}
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "{     int a = 1; }", "{         int a = 2;     }" },
                { "int a = 1;", "int a = 2;" },
                { "int a = 1", "int a = 2" },
                { "a = 1", "a = 2" },
                { "{     {         int b = 2;     } }", "{     {         int a = 2;     } }" },
                { "{         int b = 2;     }", "{     int b = 1; }" },
                { "int b = 2;", "int b = 1;" },
                { "int b = 2", "int b = 1" },
                { "b = 2", "b = 1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void BlocksWithLocals3()
        {
            var src1 = @"
{
    int a = 1, b = 2, c = 3;
    Console.WriteLine(a + b + c);
}
{
    int c = 4, b = 5, a = 6;
    Console.WriteLine(a + b + c);
}
{
    int a = 7, b = 8;
    Console.WriteLine(a + b);
}
";
            var src2 = @"
{
    int a = 9, b = 10;
    Console.WriteLine(a + b);
}
{
    int c = 11, b = 12, a = 13;
    Console.WriteLine(a + b + c);
}
{
    int a = 14, b = 15, c = 16;
    Console.WriteLine(a + b + c);
}
";
            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "{     int a = 1, b = 2, c = 3;     Console.WriteLine(a + b + c); }", "{     int a = 14, b = 15, c = 16;     Console.WriteLine(a + b + c); }" },
                { "int a = 1, b = 2, c = 3;", "int a = 14, b = 15, c = 16;" },
                { "int a = 1, b = 2, c = 3", "int a = 14, b = 15, c = 16" },
                { "a = 1", "a = 14" },
                { "b = 2", "b = 15" },
                { "c = 3", "c = 16" },
                { "Console.WriteLine(a + b + c);", "Console.WriteLine(a + b + c);" },
                { "{     int c = 4, b = 5, a = 6;     Console.WriteLine(a + b + c); }", "{     int c = 11, b = 12, a = 13;     Console.WriteLine(a + b + c); }" },
                { "int c = 4, b = 5, a = 6;", "int c = 11, b = 12, a = 13;" },
                { "int c = 4, b = 5, a = 6", "int c = 11, b = 12, a = 13" },
                { "c = 4", "c = 11" },
                { "b = 5", "b = 12" },
                { "a = 6", "a = 13" },
                { "Console.WriteLine(a + b + c);", "Console.WriteLine(a + b + c);" },
                { "{     int a = 7, b = 8;     Console.WriteLine(a + b); }", "{     int a = 9, b = 10;     Console.WriteLine(a + b); }" },
                { "int a = 7, b = 8;", "int a = 9, b = 10;" },
                { "int a = 7, b = 8", "int a = 9, b = 10" },
                { "a = 7", "a = 9" },
                { "b = 8", "b = 10" },
                { "Console.WriteLine(a + b);", "Console.WriteLine(a + b);" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void VariableDesignations()
        {
            var src1 = @"
M(out int z);
N(out var a);
";

            var src2 = @"
M(out var z);
N(out var b);
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "M(out int z);", "M(out var z);" },
                { "z", "z" },
                { "N(out var a);", "N(out var b);" },
                { "a", "b" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ParenthesizedVariable_Update()
        {
            var src1 = @"
var (x1, (x2, x3, _)) = (1, (2, true, 3));
var (a1, a2) = (1, () => { return 7; });
";

            var src2 = @"
var (x1, (x3, x4)) = (1, (2, true));
var (a1, a3) = (1, () => { return 8; });
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var (x1, (x2, x3, _)) = (1, (2, true, 3));", "var (x1, (x3, x4)) = (1, (2, true));" },
                { "x1", "x1" },
                { "x2", "x4" },
                { "x3", "x3" },
                { "var (a1, a2) = (1, () => { return 7; });", "var (a1, a3) = (1, () => { return 8; });" },
                { "a1", "a1" },
                { "a2", "a3" },
                { "() => { return 7; }", "() => { return 8; }" },
                { "()", "()" },
                { "{ return 7; }", "{ return 8; }" },
                { "return 7;", "return 8;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ParenthesizedVariable_Insert()
        {
            var src1 = @"var (z1, z2) = (1, 2);";
            var src2 = @"var (z1, z2, z3) = (1, 2, 5);";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var (z1, z2) = (1, 2);", "var (z1, z2, z3) = (1, 2, 5);" },
                { "z1", "z1" },
                { "z2", "z2" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ParenthesizedVariable_Delete()
        {
            var src1 = @"var (y1, y2, y3) = (1, 2, 7);";
            var src2 = @"var (y1, y2) = (1, 4);";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var (y1, y2, y3) = (1, 2, 7);", "var (y1, y2) = (1, 4);" },
                { "y1", "y1" },
                { "y2", "y2" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void RefVariable()
        {
            var src1 = @"
ref int a = ref G(new int[] { 1, 2 });
    ref int G(int[] p)
    {
        return ref p[1];
    }
";

            var src2 = @"
ref int32 a = ref G1(new int[] { 1, 2 });
    ref int G1(int[] p)
    {
        return ref p[2];
    }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "ref int a = ref G(new int[] { 1, 2 });", "ref int32 a = ref G1(new int[] { 1, 2 });" },
                { "ref int a = ref G(new int[] { 1, 2 })", "ref int32 a = ref G1(new int[] { 1, 2 })" },
                { "a = ref G(new int[] { 1, 2 })", "a = ref G1(new int[] { 1, 2 })" },
                { "ref int G(int[] p)     {         return ref p[1];     }", "ref int G1(int[] p)     {         return ref p[2];     }" },
                { "(int[] p)", "(int[] p)" },
                { "int[] p", "int[] p" },
                { "{         return ref p[1];     }", "{         return ref p[2];     }" },
                { "return ref p[1];", "return ref p[2];" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Lambdas

        [Fact]
        public void Lambdas1()
        {
            var src1 = "Action x = a => a;";
            var src2 = "Action x = (a) => a;";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "Action x = a => a;", "Action x = (a) => a;" },
                { "Action x = a => a", "Action x = (a) => a" },
                { "x = a => a", "x = (a) => a" },
                { "a => a", "(a) => a" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas2a()
        {
            var src1 = @"
F(x => x + 1, 1, y => y + 1, delegate(int x) { return x; }, async u => u);
";
            var src2 = @"
F(y => y + 1, G(), x => x + 1, (int x) => x, u => u, async (u, v) => u + v);
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(x => x + 1, 1, y => y + 1, delegate(int x) { return x; }, async u => u);", "F(y => y + 1, G(), x => x + 1, (int x) => x, u => u, async (u, v) => u + v);" },
                { "x => x + 1", "x => x + 1" },
                { "x", "x" },
                { "y => y + 1", "y => y + 1" },
                { "y", "y" },
                { "delegate(int x) { return x; }", "(int x) => x" },
                { "(int x)", "(int x)" },
                { "int x", "int x" },
                { "async u => u", "async (u, v) => u + v" }
            };

            expected.AssertEqual(actual);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/830419")]
        public void Lambdas2b()
        {
            var src1 = @"
F(delegate { return x; });
";
            var src2 = @"
F((a) => x, () => x);
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(delegate { return x; });", "F((a) => x, () => x);" },
                { "delegate { return x; }", "() => x" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas3()
        {
            var src1 = @"
a += async u => u;
";
            var src2 = @"
a += u => u;
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "a += async u => u;", "a += u => u;" },
                { "async u => u", "u => u" },
                { "u", "u" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas4()
        {
            var src1 = @"
foreach (var a in z)
{
    var e = from q in a.Where(l => l > 10) select q + 1;
}
";
            var src2 = @"
foreach (var a in z)
{
    var e = from q in a.Where(l => l < 0) select q + 1;
}
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "foreach (var a in z) {     var e = from q in a.Where(l => l > 10) select q + 1; }", "foreach (var a in z) {     var e = from q in a.Where(l => l < 0) select q + 1; }" },
                { "{     var e = from q in a.Where(l => l > 10) select q + 1; }", "{     var e = from q in a.Where(l => l < 0) select q + 1; }" },
                { "var e = from q in a.Where(l => l > 10) select q + 1;", "var e = from q in a.Where(l => l < 0) select q + 1;" },
                { "var e = from q in a.Where(l => l > 10) select q + 1", "var e = from q in a.Where(l => l < 0) select q + 1" },
                { "e = from q in a.Where(l => l > 10) select q + 1", "e = from q in a.Where(l => l < 0) select q + 1" },
                { "from q in a.Where(l => l > 10)", "from q in a.Where(l => l < 0)" },
                { "l => l > 10", "l => l < 0" },
                { "l", "l" },
                { "select q + 1", "select q + 1" },  // select clause
                { "select q + 1", "select q + 1" }   // query body
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas5()
        {
            var src1 = @"
F(a => b => c => d);
";
            var src2 = @"
F(a => b => c => d);
";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "F(a => b => c => d);", "F(a => b => c => d);" },
                { "a => b => c => d", "a => b => c => d" },
                { "a", "a" },
                { "b => c => d", "b => c => d" },
                { "b", "b" },
                { "c => d", "c => d" },
                { "c", "c" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas6()
        {
            var src1 = @"
F(a => b => c => d);
";
            var src2 = @"
F(a => G(b => H(c => I(d))));
";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "F(a => b => c => d);", "F(a => G(b => H(c => I(d))));" },
                { "a => b => c => d", "a => G(b => H(c => I(d)))" },
                { "a", "a" },
                { "b => c => d", "b => H(c => I(d))" },
                { "b", "b" },
                { "c => d", "c => I(d)" },
                { "c", "c" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas7()
        {
            var src1 = @"
F(a => 
{ 
    F(c => /*1*/d);
    F((u, v) => 
    {
        F((w) => c => /*2*/d);
        F(p => p);
    });
});
";
            var src2 = @"
F(a => 
{ 
    F(c => /*1*/d + 1);
    F((u, v) => 
    {
        F((w) => c => /*2*/d + 1);
        F(p => p*2);
    });
});
";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "F(a =>  {      F(c => /*1*/d);     F((u, v) =>      {         F((w) => c => /*2*/d);         F(p => p);     }); });",
                  "F(a =>  {      F(c => /*1*/d + 1);     F((u, v) =>      {         F((w) => c => /*2*/d + 1);         F(p => p*2);     }); });" },
                { "a =>  {      F(c => /*1*/d);     F((u, v) =>      {         F((w) => c => /*2*/d);         F(p => p);     }); }",
                  "a =>  {      F(c => /*1*/d + 1);     F((u, v) =>      {         F((w) => c => /*2*/d + 1);         F(p => p*2);     }); }" },
                { "a", "a" },
                { "{      F(c => /*1*/d);     F((u, v) =>      {         F((w) => c => /*2*/d);         F(p => p);     }); }",
                  "{      F(c => /*1*/d + 1);     F((u, v) =>      {         F((w) => c => /*2*/d + 1);         F(p => p*2);     }); }" },
                { "F(c => /*1*/d);", "F(c => /*1*/d + 1);" },
                { "c => /*1*/d", "c => /*1*/d + 1" },
                { "c", "c" },
                { "F((u, v) =>      {         F((w) => c => /*2*/d);         F(p => p);     });", "F((u, v) =>      {         F((w) => c => /*2*/d + 1);         F(p => p*2);     });" },
                { "(u, v) =>      {         F((w) => c => /*2*/d);         F(p => p);     }", "(u, v) =>      {         F((w) => c => /*2*/d + 1);         F(p => p*2);     }" },
                { "(u, v)", "(u, v)" },
                { "u", "u" },
                { "v", "v" },
                { "{         F((w) => c => /*2*/d);         F(p => p);     }", "{         F((w) => c => /*2*/d + 1);         F(p => p*2);     }" },
                { "F((w) => c => /*2*/d);", "F((w) => c => /*2*/d + 1);" },
                { "(w) => c => /*2*/d", "(w) => c => /*2*/d + 1" },
                { "(w)", "(w)" },
                { "w", "w" },
                { "c => /*2*/d", "c => /*2*/d + 1" },
                { "c", "c" },
                { "F(p => p);", "F(p => p*2);" },
                { "p => p", "p => p*2" },
                { "p", "p" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LambdasInArrayType()
        {
            var src1 = "var x = new int[F(a => 1)];";
            var src2 = "var x = new int[F(a => 2)];";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var x = new int[F(a => 1)];", "var x = new int[F(a => 2)];" },
                { "var x = new int[F(a => 1)]", "var x = new int[F(a => 2)]" },
                { "x = new int[F(a => 1)]", "x = new int[F(a => 2)]" },
                { "a => 1", "a => 2" },
                { "a", "a" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LambdasInArrayInitializer()
        {
            var src1 = "var x = new int[] { F(a => 1) };";
            var src2 = "var x = new int[] { F(a => 2) };";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var x = new int[] { F(a => 1) };", "var x = new int[] { F(a => 2) };" },
                { "var x = new int[] { F(a => 1) }", "var x = new int[] { F(a => 2) }" },
                { "x = new int[] { F(a => 1) }", "x = new int[] { F(a => 2) }" },
                { "a => 1", "a => 2" },
                { "a", "a" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LambdasInStackalloc()
        {
            var src1 = "var x = stackalloc int[F(a => 1)];";
            var src2 = "var x = stackalloc int[F(a => 2)];";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var x = stackalloc int[F(a => 1)];", "var x = stackalloc int[F(a => 2)];" },
                { "var x = stackalloc int[F(a => 1)]", "var x = stackalloc int[F(a => 2)]" },
                { "x = stackalloc int[F(a => 1)]", "x = stackalloc int[F(a => 2)]" },
                { "a => 1", "a => 2" },
                { "a", "a" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LambdasInStackalloc_Initializer()
        {
            var src1 = "var x = stackalloc[] { F(a => 1) };";
            var src2 = "var x = stackalloc[] { F(a => 2) };";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var x = stackalloc[] { F(a => 1) };", "var x = stackalloc[] { F(a => 2) };" },
                { "var x = stackalloc[] { F(a => 1) }", "var x = stackalloc[] { F(a => 2) }" },
                { "x = stackalloc[] { F(a => 1) }", "x = stackalloc[] { F(a => 2) }" },
                { "a => 1", "a => 2" },
                { "a", "a" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Lambdas_ParameterToDiscard()
        {
            var src1 = "var x = F((a, b) => 1);";
            var src2 = "var x = F((_, _) => 2);";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var x = F((a, b) => 1);", "var x = F((_, _) => 2);" },
                { "var x = F((a, b) => 1)", "var x = F((_, _) => 2)" },
                { "x = F((a, b) => 1)", "x = F((_, _) => 2)" },
                { "(a, b) => 1", "(_, _) => 2" },
                { "(a, b)", "(_, _)" },
                { "a", "_" },
                { "b", "_" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Local Functions

        [Fact]
        public void LocalFunctionDefinitions()
        {
            var src1 = @"
(int a, string c) F1(int i) { return null; }
(int a, int b) F2(int i) { return null; }
(int a, int b, int c) F3(int i) { return null; }
";

            var src2 = @"
(int a, int b) F1(int i) { return null; }
(int a, int b, string c) F2(int i) { return null; }
(int a, int b) F3(int i) { return null; }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "(int a, string c) F1(int i) { return null; }", "(int a, int b) F1(int i) { return null; }" },
                { "(int i)", "(int i)" },
                { "int i", "int i" },
                { "{ return null; }", "{ return null; }" },
                { "return null;", "return null;" },
                { "(int a, int b) F2(int i) { return null; }", "(int a, int b, string c) F2(int i) { return null; }" },
                { "(int i)", "(int i)" },
                { "int i", "int i" },
                { "{ return null; }", "{ return null; }" },
                { "return null;", "return null;" },
                { "(int a, int b, int c) F3(int i) { return null; }", "(int a, int b) F3(int i) { return null; }" },
                { "(int i)", "(int i)" },
                { "int i", "int i" },
                { "{ return null; }", "{ return null; }" },
                { "return null;", "return null;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions1()
        {
            var src1 = "object x(object a) => a; F(x);";
            var src2 = "F(a => a);";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "object x(object a) => a;", "a => a" },
                { "F(x);", "F(a => a);" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions2a()
        {
            var src1 = @"
F(x => x + 1, 1, y => y + 1, delegate(int x) { return x; }, async u => u);
";
            var src2 = @"
int localF1(int y) => y + 1;
int localF2(int x) => x + 1;
int localF3(int x) => x;
int localF4(int u) => u;
async int localF5(int u, int v) =>  u + v;
F(localF1, localF2, G(), localF2, localF3, localF4, localF5);
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(x => x + 1, 1, y => y + 1, delegate(int x) { return x; }, async u => u);", "F(localF1, localF2, G(), localF2, localF3, localF4, localF5);" },
                { "x => x + 1", "int localF2(int x) => x + 1;" },
                { "y => y + 1", "int localF1(int y) => y + 1;" },
                { "delegate(int x) { return x; }", "int localF3(int x) => x;" },
                { "(int x)", "(int x)" },
                { "int x", "int x" },
                { "async u => u", "int localF4(int u) => u;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions2b()
        {
            var src1 = @"
F(delegate { return x; });
";
            var src2 = @"
int localF() => x;
F(localF);
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(delegate { return x; });", "F(localF);" },
                { "delegate { return x; }", "int localF() => x;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions3()
        {
            var src1 = @"
a += async u => u;
";
            var src2 = @"
object localF(object u) => u;
a += localF;
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "a += async u => u;", "a += localF;" },
                { "async u => u", "object localF(object u) => u;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions4()
        {
            var src1 = @"int a() { int b() { int c() { int d() { return 0; } } return c(); } return b(); }";
            var src2 = @"int a() { int b() { int c() { int d() { return 0; } } return c(); } return b(); }";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "int a() { int b() { int c() { int d() { return 0; } } return c(); } return b(); }",
                    "int a() { int b() { int c() { int d() { return 0; } } return c(); } return b(); }" },
                { "()", "()" },
                { "{ int b() { int c() { int d() { return 0; } } return c(); } return b(); }",
                    "{ int b() { int c() { int d() { return 0; } } return c(); } return b(); }" },
                { "int b() { int c() { int d() { return 0; } } return c(); }",
                    "int b() { int c() { int d() { return 0; } } return c(); }" },
                { "()", "()" },
                { "{ int c() { int d() { return 0; } } return c(); }",
                    "{ int c() { int d() { return 0; } } return c(); }" },
                { "int c() { int d() { return 0; } }", "int c() { int d() { return 0; } }" },
                { "()", "()" },
                { "{ int d() { return 0; } }", "{ int d() { return 0; } }" },
                { "int d() { return 0; }", "int d() { return 0; }" },
                { "()", "()" },
                { "{ return 0; }", "{ return 0; }" },
                { "return 0;", "return 0;" },
                { "return c();", "return c();" },
                { "return b();", "return b();" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions5()
        {
            var src1 = @"
void G6(int a)
{ 
    int G5(int c) => /*1*/d;
    F(G5);

    void G4()
    {
        void G1(int x) => x;
        int G3(int w)
        { 
            int G2(int c) => /*2*/d;
            return G2(w);
        }
        F(G3);
        F(G1);
    };
    F(G4);
}
";

            var src2 = @"
void G6(int a)
{ 
    int G5(int c) => /*1*/d + 1;F(G5);

    void G4()
    {
        int G3(int w)
        { 
            int G2(int c) => /*2*/d + 1; return G2(w);
        }
        F(G3); F(G1); int G6(int p) => p *2; F(G6);
    }
    F(G4);
}
";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "void G6(int a) {      int G5(int c) => /*1*/d;     F(G5);      void G4()     {         void G1(int x) => x;         int G3(int w)         {              int G2(int c) => /*2*/d;             return G2(w);         }         F(G3);         F(G1);     };     F(G4); }",
                    "void G6(int a) {      int G5(int c) => /*1*/d + 1;F(G5);      void G4()     {         int G3(int w)         {              int G2(int c) => /*2*/d + 1; return G2(w);         }         F(G3); F(G1); int G6(int p) => p *2; F(G6);     }     F(G4); }" },
                { "(int a)", "(int a)" },
                { "int a", "int a" },
                { "{      int G5(int c) => /*1*/d;     F(G5);      void G4()     {         void G1(int x) => x;         int G3(int w)         {              int G2(int c) => /*2*/d;             return G2(w);         }         F(G3);         F(G1);     };     F(G4); }",
                    "{      int G5(int c) => /*1*/d + 1;F(G5);      void G4()     {         int G3(int w)         {              int G2(int c) => /*2*/d + 1; return G2(w);         }         F(G3); F(G1); int G6(int p) => p *2; F(G6);     }     F(G4); }" },
                { "int G5(int c) => /*1*/d;", "int G5(int c) => /*1*/d + 1;" },
                { "(int c)", "(int c)" },
                { "int c", "int c" },
                { "F(G5);", "F(G5);" },
                { "void G4()     {         void G1(int x) => x;         int G3(int w)         {              int G2(int c) => /*2*/d;             return G2(w);         }         F(G3);         F(G1);     }",
                    "void G4()     {         int G3(int w)         {              int G2(int c) => /*2*/d + 1; return G2(w);         }         F(G3); F(G1); int G6(int p) => p *2; F(G6);     }" },
                { "()", "()" },
                { "{         void G1(int x) => x;         int G3(int w)         {              int G2(int c) => /*2*/d;             return G2(w);         }         F(G3);         F(G1);     }",
                    "{         int G3(int w)         {              int G2(int c) => /*2*/d + 1; return G2(w);         }         F(G3); F(G1); int G6(int p) => p *2; F(G6);     }" },
                { "void G1(int x) => x;", "int G6(int p) => p *2;" },
                { "(int x)", "(int p)" },
                { "int x", "int p" },
                { "int G3(int w)         {              int G2(int c) => /*2*/d;             return G2(w);         }",
                    "int G3(int w)         {              int G2(int c) => /*2*/d + 1; return G2(w);         }" },
                { "(int w)", "(int w)" },
                { "int w", "int w" },
                { "{              int G2(int c) => /*2*/d;             return G2(w);         }", "{              int G2(int c) => /*2*/d + 1; return G2(w);         }" },
                { "int G2(int c) => /*2*/d;", "int G2(int c) => /*2*/d + 1;" },
                { "(int c)", "(int c)" },
                { "int c", "int c" },
                { "return G2(w);", "return G2(w);" },
                { "F(G3);", "F(G3);" },
                { "F(G1);", "F(G1);" },
                { "F(G4);", "F(G4);" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions6()
        {
            var src1 = @"int f() { return local(); int local() { return 1; }}";
            var src2 = @"int f() { return local(); int local() => 2; }";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "int f() { return local(); int local() { return 1; }}", "int f() { return local(); int local() => 2; }" },
                { "()", "()" },
                { "{ return local(); int local() { return 1; }}", "{ return local(); int local() => 2; }" },
                { "return local();", "return local();" },
                { "int local() { return 1; }", "int local() => 2;" },
                { "()", "()" },
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void LocalFunctions6Reverse()
        {
            var src1 = @"int f() { return local(); int local() => 2; }";
            var src2 = @"int f() { return local(); int local() { return 1; }}";

            var matches = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(matches);

            var expected = new MatchingPairs
            {
                { "int f() { return local(); int local() => 2; }", "int f() { return local(); int local() { return 1; }}" },
                { "()", "()" },
                { "{ return local(); int local() => 2; }", "{ return local(); int local() { return 1; }}" },
                { "return local();", "return local();" },
                { "int local() => 2;", "int local() { return 1; }" },
                { "()", "()" },
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region LINQ

        [Fact]
        public void Queries1()
        {
            var src1 = @"
var q = from c in cars
        from ud in users_details
        from bd in bids
        select 1;
";
            var src2 = @"
var q = from c in cars
        from bd in bids
        from ud in users_details
        select 2;
";

            var match = GetMethodMatch(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var q = from c in cars         from ud in users_details         from bd in bids         select 1;", "var q = from c in cars         from bd in bids         from ud in users_details         select 2;" },
                { "var q = from c in cars         from ud in users_details         from bd in bids         select 1", "var q = from c in cars         from bd in bids         from ud in users_details         select 2" },
                { "q = from c in cars         from ud in users_details         from bd in bids         select 1", "q = from c in cars         from bd in bids         from ud in users_details         select 2" },
                { "from c in cars", "from c in cars" },
                { "from ud in users_details         from bd in bids         select 1", "from bd in bids         from ud in users_details         select 2" },
                { "from ud in users_details", "from ud in users_details" },
                { "from bd in bids", "from bd in bids" },
                { "select 1", "select 2" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Queries2()
        {
            var src1 = @"
var q = from c in cars
        from ud in users_details
        from bd in bids
        orderby c.listingOption descending
        where a.userID == ud.userid
        let images = from ai in auction_images
                     where ai.belongs_to == c.id
                     select ai
        let bid = (from b in bids
                    orderby b.id descending
                    where b.carID == c.id
                    select b.bidamount).FirstOrDefault()
        select bid;
";
            var src2 = @"
var q = from c in cars
        from ud in users_details
        from bd in bids
        orderby c.listingOption descending
        where a.userID == ud.userid
        let images = from ai in auction_images
                     where ai.belongs_to == c.id2
                     select ai + 1
        let bid = (from b in bids
                    orderby b.id ascending
                    where b.carID == c.id2
                    select b.bidamount).FirstOrDefault()
        select bid;
";

            var match = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id                      select ai         let bid = (from b in bids                     orderby b.id descending                     where b.carID == c.id                     select b.bidamount).FirstOrDefault()         select bid;",
                  "var q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id2                      select ai + 1         let bid = (from b in bids                     orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount).FirstOrDefault()         select bid;" },
                { "var q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id                      select ai         let bid = (from b in bids                     orderby b.id descending                     where b.carID == c.id                     select b.bidamount).FirstOrDefault()         select bid",
                  "var q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id2                      select ai + 1         let bid = (from b in bids                     orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount).FirstOrDefault()         select bid" },
                { "q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id                      select ai         let bid = (from b in bids                     orderby b.id descending                     where b.carID == c.id                     select b.bidamount).FirstOrDefault()         select bid",
                  "q = from c in cars         from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id2                      select ai + 1         let bid = (from b in bids                     orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount).FirstOrDefault()         select bid" },
                { "from c in cars", "from c in cars" },
                { "from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id                      select ai         let bid = (from b in bids                     orderby b.id descending                     where b.carID == c.id                     select b.bidamount).FirstOrDefault()         select bid", "from ud in users_details         from bd in bids         orderby c.listingOption descending         where a.userID == ud.userid         let images = from ai in auction_images                      where ai.belongs_to == c.id2                      select ai + 1         let bid = (from b in bids                     orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount).FirstOrDefault()         select bid" },
                { "from ud in users_details", "from ud in users_details" },
                { "from bd in bids", "from bd in bids" },
                { "orderby c.listingOption descending", "orderby c.listingOption descending" },
                { "c.listingOption descending", "c.listingOption descending" },
                { "where a.userID == ud.userid", "where a.userID == ud.userid" },
                { "let images = from ai in auction_images                      where ai.belongs_to == c.id                      select ai",
                  "let images = from ai in auction_images                      where ai.belongs_to == c.id2                      select ai + 1" },
                { "from ai in auction_images", "from ai in auction_images" },
                { "where ai.belongs_to == c.id                      select ai", "where ai.belongs_to == c.id2                      select ai + 1" },
                { "where ai.belongs_to == c.id", "where ai.belongs_to == c.id2" },
                { "select ai", "select ai + 1" },
                { "let bid = (from b in bids                     orderby b.id descending                     where b.carID == c.id                     select b.bidamount).FirstOrDefault()",
                  "let bid = (from b in bids                     orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount).FirstOrDefault()" },
                { "from b in bids", "from b in bids" },
                { "orderby b.id descending                     where b.carID == c.id                     select b.bidamount", "orderby b.id ascending                     where b.carID == c.id2                     select b.bidamount" },
                { "orderby b.id descending", "orderby b.id ascending" },
                { "b.id descending", "b.id ascending" },
                { "where b.carID == c.id", "where b.carID == c.id2" },
                { "select b.bidamount", "select b.bidamount" },
                { "select bid", "select bid" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Queries3()
        {
            var src1 = @"
var q = from a in await seq1
        join c in await seq2 on F(u => u) equals G(s => s) into g1
        join l in await seq3 on F(v => v) equals G(t => t) into g2
        select a;

";
            var src2 = @"
var q = from a in await seq1
        join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1
        join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2
        select a + 1;
";

            var match = GetMethodMatches(src1, src2, MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var q = from a in await seq1         join c in await seq2 on F(u => u) equals G(s => s) into g1         join l in await seq3 on F(v => v) equals G(t => t) into g2         select a;", "var q = from a in await seq1         join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1         join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2         select a + 1;" },
                { "var q = from a in await seq1         join c in await seq2 on F(u => u) equals G(s => s) into g1         join l in await seq3 on F(v => v) equals G(t => t) into g2         select a", "var q = from a in await seq1         join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1         join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2         select a + 1" },
                { "q = from a in await seq1         join c in await seq2 on F(u => u) equals G(s => s) into g1         join l in await seq3 on F(v => v) equals G(t => t) into g2         select a", "q = from a in await seq1         join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1         join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2         select a + 1" },
                { "from a in await seq1", "from a in await seq1" },
                { "await seq1", "await seq1" },
                { "join c in await seq2 on F(u => u) equals G(s => s) into g1         join l in await seq3 on F(v => v) equals G(t => t) into g2         select a", "join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1         join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2         select a + 1" },
                { "join c in await seq2 on F(u => u) equals G(s => s) into g1", "join c in await seq2 on F(u => u + 1) equals G(s => s + 3) into g1" },
                { "await seq2", "await seq2" },
                { "u => u", "u => u + 1" },
                { "u", "u" },
                { "s => s", "s => s + 3" },
                { "s", "s" },
                { "into g1", "into g1" },
                { "join l in await seq3 on F(v => v) equals G(t => t) into g2", "join c in await seq3 on F(vv => vv + 2) equals G(tt => tt + 4) into g2" },
                { "await seq3", "await seq3" },
                { "v => v", "vv => vv + 2" },
                { "v", "vv" },
                { "t => t", "tt => tt + 4" },
                { "t", "tt" },
                { "into g2", "into g2" },
                { "select a", "select a + 1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Queries4()
        {
            var src1 = "F(from a in await b from x in y select c);";
            var src2 = "F(from a in await c from x in y select c);";

            var match = GetMethodMatches(src1, src2, MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(from a in await b from x in y select c);", "F(from a in await c from x in y select c);" },
                { "from a in await b", "from a in await c" },
                { "await b", "await c" },
                { "from x in y select c", "from x in y select c" },
                { "from x in y", "from x in y" },
                { "select c", "select c" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Queries5()
        {
            var src1 = "F(from a in b  group a by a.x into g  select g);";
            var src2 = "F(from a in b  group z by z.y into h  select h);";

            var match = GetMethodMatches(src1, src2);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(from a in b  group a by a.x into g  select g);", "F(from a in b  group z by z.y into h  select h);" },
                { "from a in b", "from a in b" },
                { "group a by a.x into g  select g", "group z by z.y into h  select h" },
                { "group a by a.x", "group z by z.y" },
                { "into g  select g", "into h  select h" },
                { "select g", "select h" },
                { "select g", "select h" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Iterators

        [Fact]
        public void Yields()
        {
            var src1 = @"
yield return 0;
yield return 1;
";
            var src2 = @"
yield break;
yield return 1;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Iterator);
            var actual = ToMatchingPairs(match);

            // yield return should not match yield break
            var expected = new MatchingPairs()
            {
                { "yield return 1;", "yield return 1;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void YieldReturn_Add()
        {
            var src1 = @"
yield return /*1*/ 1;
yield return /*2*/ 2;
";
            var src2 = @"
yield return /*3*/ 3;
yield return /*1*/ 1;
yield return /*2*/ 2;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Iterator);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "yield return /*1*/ 1;", "yield return /*1*/ 1;" },
                { "yield return /*2*/ 2;", "yield return /*2*/ 2;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void YieldReturn_Swap1()
        {
            var src1 = @"
A();
yield return /*1*/ 1;
B();
yield return /*2*/ 2;
C();
";
            var src2 = @"
B();
yield return /*2*/ 2;
A();
yield return /*1*/ 1;
C();
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Iterator);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "A();", "A();" },
                { "yield return /*1*/ 1;", "yield return /*1*/ 1;" },
                { "B();", "B();" },
                { "yield return /*2*/ 2;", "yield return /*2*/ 2;" },
                { "C();", "C();" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void YieldReturn_Swap2()
        {
            var src1 = @"
yield return /*1*/ 1;

{
    yield return /*2*/ 2;
}

foreach (var x in y) { yield return /*3*/ 3; }
";
            var src2 = @"
yield return /*1*/ 1;
yield return /*2*/ 3;
foreach (var x in y) { yield return /*3*/ 2; }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Iterator);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "yield return /*1*/ 1;", "yield return /*1*/ 1;" },
                { "{     yield return /*2*/ 2; }", "{ yield return /*3*/ 2; }" },
                { "yield return /*2*/ 2;", "yield return /*3*/ 2;" },
                { "foreach (var x in y) { yield return /*3*/ 3; }", "foreach (var x in y) { yield return /*3*/ 2; }" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Async

        [Fact]
        public void AwaitExpressions()
        {
            var src1 = "F(await x, await y);";
            var src2 = "F(await y, await x);";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "F(await x, await y);", "F(await y, await x);" },
                { "await x", "await x" },
                { "await y", "await y" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Awaits()
        {
            var src1 = @"
await x;
await using (expr) {}
await using (D y = new D()) {}
await using D y = new D();
await foreach (var z in w) {} 
await foreach (var (u, v) in w) {}
";
            var src2 = @"
await foreach (var (u, v) in w) {}
await foreach (var z in w) {} 
await using D y = new D();
await using (D y = new D()) {}
await using (expr) {}
await x;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "await x;", "await x;" },
                { "await x", "await x" },
                { "await using (expr) {}", "await using (expr) {}" },
                { "{}", "{}" },
                { "await using (D y = new D()) {}", "await using (D y = new D()) {}" },
                { "D y = new D()", "D y = new D()" },
                { "y = new D()", "y = new D()" },
                { "{}", "{}" },
                { "await using D y = new D();", "await using D y = new D();" },
                { "D y = new D()", "D y = new D()" },
                { "y = new D()", "y = new D()" },
                { "await foreach (var z in w) {}", "await foreach (var z in w) {}" },
                { "{}", "{}" },
                { "await foreach (var (u, v) in w) {}", "await foreach (var (u, v) in w) {}" },
                { "u", "u" },
                { "v", "v" },
                { "{}", "{}" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Await_To_AwaitUsingExpression()
        {
            var src1 = @"
await x;
";
            var src2 = @"
await using (expr) {}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs();

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Await_To_AwaitUsingDecl()
        {
            var src1 = @"
await x;
";
            var src2 = @"
await using D y = new D();
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // empty - treated as different awaits as the await using awaits at the end of the scope
            var expected = new MatchingPairs();

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitUsingDecl_To_AwaitUsingStatement()
        {
            var src1 = @"
await using D y = new D();
";
            var src2 = @"
await using (D y = new D()) { }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs();

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitUsingExpression_To_AwaitUsingStatementWithSingleVariable()
        {
            var src1 = @"
await using (y = new D()) { }
";
            var src2 = @"
await using (D y = new D()) { }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // Using with a single variable could match using with an expression because they both generate a single try-finally block,
            // but to simplify logic we do not match them currently.
            var expected = new MatchingPairs
            {
                { "{ }", "{ }" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitUsingExpression_To_AwaitUsingStatementWithMultipleVariables()
        {
            var src1 = @"
await using (y = new D()) { }
";
            var src2 = @"
await using (D y = new D(), z = new D()) { }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // Using with multiple variables should not match using with an expression because they generate different number of try-finally blocks.
            var expected = new MatchingPairs
            {
                { "{ }", "{ }" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitUsingMatchesUSing()
        {
            var src1 = "await x;foreach (T x in y) {}";
            var src2 = "await x;await foreach (T x in y) {}";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs()
            {
                { "await x;", "await x;" },
                { "await x", "await x" },
                { "foreach (T x in y) {}", "await foreach (T x in y) {}" },
                { "{}", "{}" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Await_To_AwaitForeach()
        {
            var src1 = @"
await x;
";
            var src2 = @"
await foreach (var x in y) {}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // empty - treated as different awaits as the foreach awaits in each loop iteration
            var expected = new MatchingPairs();

            expected.AssertEqual(actual);
        }

        [Fact]
        public void Await_To_AwaitForeachVar()
        {
            var src1 = @"
await x;
";
            var src2 = @"
await foreach (var (x, y) in z) {}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // empty - treated as different awaits as the foreach awaits in each loop iteration
            var expected = new MatchingPairs();

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitForeach_To_AwaitForeachVar()
        {
            var src1 = @"
await foreach (var x in y) {}
";
            var src2 = @"
await foreach (var (u, v) in y) {}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "await foreach (var x in y) {}", "await foreach (var (u, v) in y) {}" },
                { "{}", "{}" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void AwaitForeachMatchesForeach()
        {
            var src1 = "await x;foreach (T x in y) {}";
            var src2 = "await x;await foreach (T x in y) {}";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            // We do match await foreach to foreach, even though the latter does not represent state machine.
            // This is ok since the previous version of the method won't have state machine state associated with the 
            // foreach statement and thus matching state machine state for the await foreach won't succeed even though 
            // the syntax nodes match.
            var expected = new MatchingPairs()
            {
                { "await x;", "await x;" },
                { "await x", "await x" },
                { "foreach (T x in y) {}", "await foreach (T x in y) {}" },
                { "{}", "{}" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Constructors

        [Fact]
        public void ConstructorWithInitializer1()
        {
            var src1 = @"
(int x = 1) : base(a => a + 1) { Console.WriteLine(1); }
";
            var src2 = @"
(int x = 1) : base(a => a + 1) { Console.WriteLine(1); }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.ConstructorWithParameters);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "(int x = 1)", "(int x = 1)" },
                { "int x = 1", "int x = 1" },
                { "a => a + 1", "a => a + 1" },
                { "a", "a" },
                { "{ Console.WriteLine(1); }", "{ Console.WriteLine(1); }" },
                { "Console.WriteLine(1);", "Console.WriteLine(1);" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ConstructorWithInitializer2()
        {
            var src1 = @"
() : base(a => a + 1) { Console.WriteLine(1); }
";
            var src2 = @"
() { Console.WriteLine(1); }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.ConstructorWithParameters);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "()", "()" },
                { "{ Console.WriteLine(1); }", "{ Console.WriteLine(1); }" },
                { "Console.WriteLine(1);", "Console.WriteLine(1);" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Exception Handlers

        [Fact]
        public void ExceptionHandlers()
        {
            var src1 = @"
try { throw new InvalidOperationException(1); }
catch (IOException e) when (filter(e)) { Console.WriteLine(2); }
catch (Exception e) when (filter(e)) { Console.WriteLine(3); }
";
            var src2 = @"
try { throw new InvalidOperationException(10); }
catch (IOException e) when (filter(e)) { Console.WriteLine(20); }
catch (Exception e) when (filter(e)) { Console.WriteLine(30); }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "try { throw new InvalidOperationException(1); } catch (IOException e) when (filter(e)) { Console.WriteLine(2); } catch (Exception e) when (filter(e)) { Console.WriteLine(3); }", "try { throw new InvalidOperationException(10); } catch (IOException e) when (filter(e)) { Console.WriteLine(20); } catch (Exception e) when (filter(e)) { Console.WriteLine(30); }" },
                { "{ throw new InvalidOperationException(1); }", "{ throw new InvalidOperationException(10); }" },
                { "throw new InvalidOperationException(1);", "throw new InvalidOperationException(10);" },
                { "catch (IOException e) when (filter(e)) { Console.WriteLine(2); }", "catch (IOException e) when (filter(e)) { Console.WriteLine(20); }" },
                { "(IOException e)", "(IOException e)" },
                { "when (filter(e))", "when (filter(e))" },
                { "{ Console.WriteLine(2); }", "{ Console.WriteLine(20); }" },
                { "Console.WriteLine(2);", "Console.WriteLine(20);" },
                { "catch (Exception e) when (filter(e)) { Console.WriteLine(3); }", "catch (Exception e) when (filter(e)) { Console.WriteLine(30); }" },
                { "(Exception e)", "(Exception e)" },
                { "when (filter(e))", "when (filter(e))" },
                { "{ Console.WriteLine(3); }", "{ Console.WriteLine(30); }" },
                { "Console.WriteLine(3);", "Console.WriteLine(30);" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Foreach

        [Fact]
        public void ForeachVariable_Update1()
        {
            var src1 = @"
foreach (var (a1, a2) in e) { A1(); }
foreach ((var b1, var b2) in e) { A2(); }
";

            var src2 = @"
foreach (var (a1, a3) in e) { A1(); }
foreach ((var b3, int b2) in e) { A2(); }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "foreach (var (a1, a2) in e) { A1(); }", "foreach (var (a1, a3) in e) { A1(); }" },
                { "a1", "a1" },
                { "a2", "a3" },
                { "{ A1(); }", "{ A1(); }" },
                { "A1();", "A1();" },
                { "foreach ((var b1, var b2) in e) { A2(); }", "foreach ((var b3, int b2) in e) { A2(); }" },
                { "b1", "b3" },
                { "b2", "b2" },
                { "{ A2(); }", "{ A2(); }" },
                { "A2();", "A2();" },
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ForeachVariable_Update2()
        {
            var src1 = @"
foreach (_ in e2) { }
foreach (_ in e3) { A(); }
";

            var src2 = @"
foreach (_ in e4) { A(); }
foreach (var b in e2) { }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "foreach (_ in e2) { }", "foreach (var b in e2) { }" },
                { "{ }", "{ }" },
                { "foreach (_ in e3) { A(); }", "foreach (_ in e4) { A(); }" },
                { "{ A(); }", "{ A(); }" },
                { "A();", "A();" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ForeachVariable_Insert()
        {
            var src1 = @"
foreach (var ((a3, a4), _) in e) { }
foreach ((var b4, var b5) in e) { }
";

            var src2 = @"
foreach (var ((a3, a5, a4), _) in e) { }
foreach ((var b6, var b4, var b5) in e) { }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "foreach (var ((a3, a4), _) in e) { }", "foreach (var ((a3, a5, a4), _) in e) { }" },
                { "a3", "a3" },
                { "a4", "a4" },
                { "{ }", "{ }" },
                { "foreach ((var b4, var b5) in e) { }", "foreach ((var b6, var b4, var b5) in e) { }" },
                { "b4", "b4" },
                { "b5", "b5" },
                { "{ }", "{ }" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void ForeachVariable_Delete()
        {
            var src1 = @"
foreach (var (a11, a12, a13) in e) { A1(); }
foreach ((var b7, var b8, var b9) in e) { A2(); }
";

            var src2 = @"
foreach (var (a12, a13) in e1) { A1(); }
foreach ((var b7, var b9) in e) { A2(); }
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "foreach (var (a11, a12, a13) in e) { A1(); }", "foreach (var (a12, a13) in e1) { A1(); }" },
                { "a12", "a12" },
                { "a13", "a13" },
                { "{ A1(); }", "{ A1(); }" },
                { "A1();", "A1();" },
                { "foreach ((var b7, var b8, var b9) in e) { A2(); }", "foreach ((var b7, var b9) in e) { A2(); }" },
                { "b7", "b7" },
                { "b9", "b9" },
                { "{ A2(); }", "{ A2(); }" },
                { "A2();", "A2();" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Patterns

        [Fact]
        public void ConstantPattern()
        {
            var src1 = @"
if ((o is null) && (y == 7)) return 3;
if (a is 7) return 5;
";

            var src2 = @"
if ((o1 is null) && (y == 7)) return 3;
if (a is 77) return 5;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "if ((o is null) && (y == 7)) return 3;", "if ((o1 is null) && (y == 7)) return 3;" },
                { "return 3;", "return 3;" },
                { "if (a is 7) return 5;", "if (a is 77) return 5;" },
                { "return 5;", "return 5;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void DeclarationPattern()
        {
            var src1 = @"
if (!(o is int i) && (y == 7)) return;
if (!(a is string s)) return;
if (!(b is string t)) return;
if (!(c is int j)) return;
";

            var src2 = @"
if (!(b is string t1)) return;
if (!(o1 is int i) && (y == 7)) return;
if (!(c is int)) return;
if (!(a is int s)) return;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "if (!(o is int i) && (y == 7)) return;", "if (!(o1 is int i) && (y == 7)) return;" },
                { "i", "i" },
                { "return;", "return;" },
                { "if (!(a is string s)) return;", "if (!(a is int s)) return;" },
                { "s", "s" },
                { "return;", "return;" },
                { "if (!(b is string t)) return;", "if (!(b is string t1)) return;" },
                { "t", "t1" },
                { "return;", "return;" },
                { "if (!(c is int j)) return;", "if (!(c is int)) return;" },
                { "return;", "return;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void VarPattern()
        {
            var src1 = @"
if (!(o is (var x, var y))) return;
if (!(o4 is (string a, var (b, c)))) return;
if (!(o2 is var (e, f, g))) return;
if (!(o3 is var (k, l, m))) return;
";

            var src2 = @"
if (!(o is (int x, int y1)))  return;
if (!(o1 is (var a, (var b, string c1)))) return;
if (!(o7 is var (g, e, f))) return;
if (!(o3 is (string k, int l2, int m))) return;
";
            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "if (!(o is (var x, var y))) return;", "if (!(o is (int x, int y1)))  return;" },
                { "x", "x" },
                { "y", "y1" },
                { "return;", "return;" },
                { "if (!(o4 is (string a, var (b, c)))) return;", "if (!(o1 is (var a, (var b, string c1)))) return;" },
                { "a", "a" },
                { "b", "b" },
                { "c", "c1" },
                { "return;", "return;" },
                { "if (!(o2 is var (e, f, g))) return;", "if (!(o7 is var (g, e, f))) return;" },
                { "e", "e" },
                { "f", "f" },
                { "g", "g" },
                { "return;", "return;" },
                { "if (!(o3 is var (k, l, m))) return;", "if (!(o3 is (string k, int l2, int m))) return;" },
                { "k", "k" },
                { "l", "l2" },
                { "m", "m" },
                { "return;", "return;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void PositionalPattern()
        {
            var src1 = @"var r = (x, y, z) switch {
(1, 2, 3) => 0,
(var a, 3, 4) => a,
(0, var b, int c) when c > 1 => 2,
(1, 1, Point { X: 0 } p) => 3,
_ => 4
};
";

            var src2 = @"var r = ((x, y, z)) switch {
(1, 2, 3) => 0,
(var a1, 3, 4) => a1 * 2,
(_, int b1, double c1) when c1 > 2 => c1,
(1, 1, Point { Y: 0 } p1) => 3,
_ => 4
};
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "var r = (x, y, z) switch { (1, 2, 3) => 0, (var a, 3, 4) => a, (0, var b, int c) when c > 1 => 2, (1, 1, Point { X: 0 } p) => 3, _ => 4 };", "var r = ((x, y, z)) switch { (1, 2, 3) => 0, (var a1, 3, 4) => a1 * 2, (_, int b1, double c1) when c1 > 2 => c1, (1, 1, Point { Y: 0 } p1) => 3, _ => 4 };" },
                { "var r = (x, y, z) switch { (1, 2, 3) => 0, (var a, 3, 4) => a, (0, var b, int c) when c > 1 => 2, (1, 1, Point { X: 0 } p) => 3, _ => 4 }", "var r = ((x, y, z)) switch { (1, 2, 3) => 0, (var a1, 3, 4) => a1 * 2, (_, int b1, double c1) when c1 > 2 => c1, (1, 1, Point { Y: 0 } p1) => 3, _ => 4 }" },
                { "r = (x, y, z) switch { (1, 2, 3) => 0, (var a, 3, 4) => a, (0, var b, int c) when c > 1 => 2, (1, 1, Point { X: 0 } p) => 3, _ => 4 }", "r = ((x, y, z)) switch { (1, 2, 3) => 0, (var a1, 3, 4) => a1 * 2, (_, int b1, double c1) when c1 > 2 => c1, (1, 1, Point { Y: 0 } p1) => 3, _ => 4 }" },
                { "(x, y, z) switch { (1, 2, 3) => 0, (var a, 3, 4) => a, (0, var b, int c) when c > 1 => 2, (1, 1, Point { X: 0 } p) => 3, _ => 4 }", "((x, y, z)) switch { (1, 2, 3) => 0, (var a1, 3, 4) => a1 * 2, (_, int b1, double c1) when c1 > 2 => c1, (1, 1, Point { Y: 0 } p1) => 3, _ => 4 }" },
                { "(1, 2, 3) => 0", "(1, 2, 3) => 0" },
                { "(var a, 3, 4) => a", "(var a1, 3, 4) => a1 * 2" },
                { "a", "a1" },
                { "(0, var b, int c) when c > 1 => 2", "(_, int b1, double c1) when c1 > 2 => c1" },
                { "b", "c1" },
                { "c", "b1" },
                { "when c > 1", "when c1 > 2" },
                { "(1, 1, Point { X: 0 } p) => 3", "(1, 1, Point { Y: 0 } p1) => 3" },
                { "p", "p1" },
                { "_ => 4", "_ => 4" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void PropertyPattern()
        {
            var src1 = @"
if (address is { State: ""WA"" }) return 1;
if (obj is { Color: Color.Purple }) return 2;
if (o is string { Length: 5 } s) return 3;
";

            var src2 = @"
if (address is { ZipCode: 98052 }) return 4;
if (obj is { Size: Size.M }) return 2;
if (o is string { Length: 7 } s7) return 5;
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "if (address is { State: \"WA\" }) return 1;", "if (address is { ZipCode: 98052 }) return 4;" },
                { "return 1;", "return 4;" },
                { "if (obj is { Color: Color.Purple }) return 2;", "if (obj is { Size: Size.M }) return 2;" },
                { "return 2;", "return 2;" },
                { "if (o is string { Length: 5 } s) return 3;", "if (o is string { Length: 7 } s7) return 5;" },
                { "s", "s7" },
                { "return 3;", "return 5;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void RecursivePatterns()
        {
            var src1 = @"var r = obj switch
{
    string s when s.Length > 0 => (s, obj1) switch
    {
        (""a"", int i) => i,
        ("""", Task<int> t) => await t,
        _ => 0
    },
    int i => i * i,
    _ => -1
};
";

            var src2 = @"var r = obj switch
{
    string s when s.Length > 0 => (s, obj1) switch
    {
        (""b"", decimal i1) => i1,
        ("""", Task<object> obj2) => await obj2,
        _ => 0
    },
    double i => i * i,
    _ => -1
};
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Async);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "var r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     },     int i => i * i,     _ => -1 };", "var r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     },     double i => i * i,     _ => -1 };" },
                { "var r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     },     int i => i * i,     _ => -1 }", "var r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     },     double i => i * i,     _ => -1 }" },
                { "r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     },     int i => i * i,     _ => -1 }", "r = obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     },     double i => i * i,     _ => -1 }" },
                { "obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     },     int i => i * i,     _ => -1 }", "obj switch {     string s when s.Length > 0 => (s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     },     double i => i * i,     _ => -1 }" },
                { "string s when s.Length > 0 => (s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     }", "string s when s.Length > 0 => (s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     }" },
                { "s", "s" },
                { "when s.Length > 0", "when s.Length > 0" },
                { "(s, obj1) switch     {         (\"a\", int i) => i,         (\"\", Task<int> t) => await t,         _ => 0     }", "(s, obj1) switch     {         (\"b\", decimal i1) => i1,         (\"\", Task<object> obj2) => await obj2,         _ => 0     }" },
                { "(\"a\", int i) => i", "(\"b\", decimal i1) => i1" },
                { "i", "i" },
                { "(\"\", Task<int> t) => await t", "(\"\", Task<object> obj2) => await obj2" },
                { "t", "obj2" },
                { "await t", "await obj2" },
                { "_ => 0", "_ => 0" },
                { "int i => i * i", "double i => i * i" },
                { "i", "i1" },
                { "_ => -1", "_ => -1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void CasePattern_UpdateInsert()
        {
            var src1 = @"
switch(shape)
{
    case Circle c: return 1;
    default: return 4;
}
";

            var src2 = @"
switch(shape)
{
    case Circle c1: return 1;
    case Point p: return 0;
    default: return 4;
}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "switch(shape) {     case Circle c: return 1;     default: return 4; }", "switch(shape) {     case Circle c1: return 1;     case Point p: return 0;     default: return 4; }" },
                { "case Circle c: return 1;", "case Circle c1: return 1;" },
                { "case Circle c:", "case Circle c1:" },
                { "c", "c1" },
                { "return 1;", "return 1;" },
                { "default: return 4;", "default: return 4;" },
                { "return 4;", "return 4;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void CasePattern_UpdateDelete()
        {
            var src1 = @"
switch(shape)
{
    case Point p: return 0;
    case Circle c: return 1;
}
";

            var src2 = @"
switch(shape)
{
    case Circle circle: return 1;
}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "switch(shape) {     case Point p: return 0;     case Circle c: return 1; }", "switch(shape) {     case Circle circle: return 1; }" },
                { "case Circle c: return 1;", "case Circle circle: return 1;" },
                { "case Circle c:", "case Circle circle:" },
                { "c", "circle" },
                { "return 1;", "return 1;" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void WhenCondition()
        {
            var src1 = @"
switch(shape)
{
    case Circle c when (c < 10): return 1;
    case Circle c when (c > 100): return 2;
}
";

            var src2 = @"
switch(shape)
{
    case Circle c when (c < 5): return 1;
    case Circle c2 when (c2 > 100): return 2;
}
";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "switch(shape) {     case Circle c when (c < 10): return 1;     case Circle c when (c > 100): return 2; }", "switch(shape) {     case Circle c when (c < 5): return 1;     case Circle c2 when (c2 > 100): return 2; }" },
                { "case Circle c when (c < 10): return 1;", "case Circle c when (c < 5): return 1;" },
                { "case Circle c when (c < 10):", "case Circle c when (c < 5):" },
                { "c", "c" },
                { "when (c < 10)", "when (c < 5)" },
                { "return 1;", "return 1;" },
                { "case Circle c when (c > 100): return 2;", "case Circle c2 when (c2 > 100): return 2;" },
                { "case Circle c when (c > 100):", "case Circle c2 when (c2 > 100):" },
                { "c", "c2" },
                { "when (c > 100)", "when (c2 > 100)" },
                { "return 2;", "return 2;" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Switch Expression

        [Fact]
        public void SwitchExpressionArms_Lambda()
        {
            var src1 = @"F1() switch { 1 => new Func<int>(() => 1)(), _ => 2 };";
            var src2 = @"F1() switch { 1 => new Func<int>(() => 3)(), _ => 2 };";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "F1() switch { 1 => new Func<int>(() => 1)(), _ => 2 };", "F1() switch { 1 => new Func<int>(() => 3)(), _ => 2 };" },
                { "F1() switch { 1 => new Func<int>(() => 1)(), _ => 2 }", "F1() switch { 1 => new Func<int>(() => 3)(), _ => 2 }" },
                { "1 => new Func<int>(() => 1)()", "1 => new Func<int>(() => 3)()" },
                { "() => 1", "() => 3" },
                { "()", "()" },
                { "_ => 2", "_ => 2" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void SwitchExpressionArms_NestedSimilar()
        {
            // The inner switch is mapped to the outer one, which is assumed to be removed.
            var src1 = @"F1() switch { 1 => 0, _ => F2() switch { 1 => 0, _ => 2 } };";
            var src2 = @"F1() switch { 1 => 0, _ => 1 };";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "F1() switch { 1 => 0, _ => F2() switch { 1 => 0, _ => 2 } };", "F1() switch { 1 => 0, _ => 1 };" },
                { "F2() switch { 1 => 0, _ => 2 }", "F1() switch { 1 => 0, _ => 1 }" },
                { "1 => 0", "1 => 0" },
                { "_ => 2", "_ => 1" }
            };

            expected.AssertEqual(actual);
        }

        [Fact]
        public void SwitchExpressionArms_NestedDissimilar()
        {
            // The inner switch is mapped to the outer one, which is assumed to be removed.
            var src1 = @"Method() switch { true => G(), _ => F2() switch { 1 => 0, _ => 2 } };";
            var src2 = @"Method() switch { true => G(), _ => 1 };";

            var match = GetMethodMatches(src1, src2, kind: MethodKind.Regular);
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs {
                { "Method() switch { true => G(), _ => F2() switch { 1 => 0, _ => 2 } };", "Method() switch { true => G(), _ => 1 };" },
                { "Method() switch { true => G(), _ => F2() switch { 1 => 0, _ => 2 } }", "Method() switch { true => G(), _ => 1 }" },
                { "true => G()", "true => G()" },
                { "_ => F2() switch { 1 => 0, _ => 2 }", "_ => 1" }
            };

            expected.AssertEqual(actual);
        }

        #endregion

        #region Top Level Statements

        [Fact]
        public void TopLevelStatements()
        {
            var src1 = @"
Console.WriteLine(1);
Console.WriteLine(2);

var x = 0;
while (true)
{
    x++;
}

Console.WriteLine(3);
";
            var src2 = @"
Console.WriteLine(4);
Console.WriteLine(5);

var x = 1;
while (true)
{
    x--;
}

Console.WriteLine(6);
";
            var match = GetTopEdits(src1, src2).Match;
            var actual = ToMatchingPairs(match);

            var expected = new MatchingPairs
            {
                { "Console.WriteLine(1);", "Console.WriteLine(4);" },
                { "Console.WriteLine(2);", "Console.WriteLine(5);" },
                { "var x = 0;", "var x = 1;" },
                { "while (true) {     x++; }", "while (true) {     x--; }" },
                { "Console.WriteLine(3);", "Console.WriteLine(6);" }
            };

            expected.AssertEqual(actual);
        }

        #endregion
    }
}
