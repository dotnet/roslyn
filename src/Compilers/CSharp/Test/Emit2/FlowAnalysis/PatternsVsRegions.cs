// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("o, i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
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
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args, i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
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
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("args, o", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args, o, i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
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
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }

        [Fact]
        public void RegionInIsPattern05()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(
@"class Program
{
    public static void Main(string[] args)
    {
        if (/*<bind>*/args is [] i/*</bind>*/) {}
    }
}");
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("args", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.Captured));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.CapturedOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.UnsafeAddressTaken));
        }
    }
}
