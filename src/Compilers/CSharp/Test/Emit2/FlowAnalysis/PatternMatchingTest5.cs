// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.FlowAnalysis
{
    public class PatternMatchingTest5 : CSharpTestBase
    {
        [Fact]
        public void SwitchExpressionInCheckedExpression()
        {
            var source = """
using System;
int x = int.MaxValue;
int y = int.MaxValue;

try
{
    C.AddInSwitchExpression(x, y, true);
}
catch (OverflowException)
{
    Console.Write("RAN1 ");
}

try
{
    C.AddInSwitchExpression2(x, y, true);
}
catch (OverflowException)
{
    Console.Write("RAN2 ");
}

try
{
    C.Add(x, y);
}
catch (OverflowException)
{
    Console.Write("RAN3 ");
}

public static class C
{
    public static int AddInSwitchExpression(int x, int y, bool condition) =>
        checked (
            condition switch
            {
                true => x + y,
                _ => throw null
            }
        );

    public static int AddInSwitchExpression2(int x, int y, bool condition)
    {
        checked
        {
            return condition switch
            {
                true => x + y,
                _ => throw null
            };
        }
    }

    public static int Add(int x, int y) => checked(x + y);
}
""";

            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN1 RAN2 RAN3");

            verifier.VerifyIL("C.AddInSwitchExpression", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.2
  IL_0001:  brfalse.s  IL_0009
  IL_0003:  ldarg.0
  IL_0004:  ldarg.1
  IL_0005:  add.ovf
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_000b
  IL_0009:  ldnull
  IL_000a:  throw
  IL_000b:  ldloc.0
  IL_000c:  ret
}
");

            verifier.VerifyIL("C.AddInSwitchExpression2", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.2
  IL_0001:  brfalse.s  IL_0009
  IL_0003:  ldarg.0
  IL_0004:  ldarg.1
  IL_0005:  add.ovf
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_000b
  IL_0009:  ldnull
  IL_000a:  throw
  IL_000b:  ldloc.0
  IL_000c:  ret
}
");

            verifier.VerifyIL("C.Add", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        public class FlowAnalysisTests : FlowTestBase
        {
            [Fact]
            public void RegionInIsPattern01()
            {
                var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(@"
class C
{
    static void M(object o)
    {
        _ = o switch
        {
            string { Length: 0 } s => /*<bind>*/s.ToString()/*</bind>*/,
            _ = throw null
        };
    }
}");
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
                Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
                Assert.Equal("s", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
                Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
                Assert.Equal("o, s", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
                Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
            }
        }
    }
}
