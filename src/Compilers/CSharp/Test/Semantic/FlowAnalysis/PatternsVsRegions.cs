// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Region analysis tests involving pattern-matching constructs.
    /// </summary>
    public partial class PatternsVsRegions : FlowTestBase
    {
        [Fact]
        public void RegionInIsPattern01()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(
@"class Program
{
    public static void Main(string[] args)
    {
        object o = args;
        if (/*<bind>*/o is int i && i > 10/*</bind>*/) {}
    }
}");
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("o, i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }

        [Fact]
        public void RegionInIsPattern02()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(
@"class Program
{
    public static void Main(string[] args)
    {
        object o = args;
        if (/*<bind>*/o is int i/*</bind>*/ && i > 10) {}
    }
}");
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args, i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }

        [Fact]
        public void RegionInIsPattern03()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(
@"class Program
{
    public static void Main(string[] args)
    {
        object o = args;
        if (o is int i && /*<bind>*/i > 10/*</bind>*/) {}
    }
}");
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o, i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }

        [Fact]
        public void RegionInIsPattern04()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(
@"class Program
{
    public static void Main(string[] args)
    {
        int o = args.Length;
        if (/*<bind>*/o is int i/*</bind>*/ && i > 10) {}
    }
}");
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args, i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Equal(null, GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }

    }
}
