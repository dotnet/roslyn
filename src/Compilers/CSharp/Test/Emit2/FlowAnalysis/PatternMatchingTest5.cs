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
    public class PatternMatchingTest5
    {
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
