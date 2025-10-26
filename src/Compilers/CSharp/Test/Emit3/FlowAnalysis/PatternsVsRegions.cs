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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79489")]
        public void RegionInIsPattern06()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression("""
                C.Use(doLogic: true);

                class C
                {
                    public static (bool, string? errorMessage) M() => (false, "Something went wrong");

                    public static void Use(bool doLogic)
                    {
                        if (doLogic)
                        {
                            if (/*<bind>*/M() is (false, var errorMessage)/*</bind>*/)
                            {
                                Console.Error.WriteLine(errorMessage);
                            }
                        }
                    }
                }
                """);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: errorMessage
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: errorMessage
                DefinitelyAssignedOnEntry: doLogic
                DefinitelyAssignedOnExit: doLogic
                ReadInside: 
                ReadOutside: doLogic, errorMessage
                WrittenInside: errorMessage
                WrittenOutside: doLogic
                """, dataFlowAnalysisResults);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79489")]
        public void RegionInSwitch01()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression("""
                static void PatternMatchingUsage()
                {
                  var t = (1, 2);
                  // selection start
                  switch (t)
                  {
                    case /*<bind>*/(var x, 2)/*</bind>*/:
                      Console.WriteLine(x);
                      break;
                  }
                  // selection end
                }
                """);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: 
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: 
                DefinitelyAssignedOnEntry: t, x, args
                DefinitelyAssignedOnExit: t, x, args
                ReadInside: 
                ReadOutside: t, x
                WrittenInside: 
                WrittenOutside: t, x, args
                """, dataFlowAnalysisResults);
        }

        [Fact]
        public void RegionInIsPattern07()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression("""
                C.Use(doLogic: true);

                class C
                {
                    public static (bool, (string result, string? errorMessage)) M() => (false, "Something went wrong");

                    public static void Use(bool doLogic)
                    {
                        if (doLogic)
                        {
                            if (/*<bind>*/M() is (false, (var inner, var errorMessage))/*</bind>*/)
                            {
                                Console.Error.WriteLine(errorMessage);
                            }
                        }
                    }
                }

                """);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: inner, errorMessage
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: errorMessage
                DefinitelyAssignedOnEntry: doLogic
                DefinitelyAssignedOnExit: doLogic
                ReadInside: 
                ReadOutside: doLogic, errorMessage
                WrittenInside: inner, errorMessage
                WrittenOutside: doLogic
                """, dataFlowAnalysisResults);
        }

        [Fact]
        public void RegionInIsPattern08()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression("""
                C.Use(doLogic: true);

                class C
                {
                    public static (string result, (bool, string? errorMessage)) M() => (false, "Something went wrong");

                    public static void Use(bool doLogic)
                    {
                        if (doLogic)
                        {
                            if (/*<bind>*/M() is (var outer, (true, var errorMessage))/*</bind>*/)
                            {
                                Console.Error.WriteLine(errorMessage);
                            }
                        }
                    }
                }

                """);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: outer, errorMessage
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: errorMessage
                DefinitelyAssignedOnEntry: doLogic
                DefinitelyAssignedOnExit: doLogic
                ReadInside: 
                ReadOutside: doLogic, errorMessage
                WrittenInside: outer, errorMessage
                WrittenOutside: doLogic
                """, dataFlowAnalysisResults);
        }

        [Fact]
        public void RegionInIsPattern09()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression("""
                C.Use(doLogic: true);

                class C
                {
                    public static (string result, (bool, string? errorMessage)) M() => (false, "Something went wrong");

                    public static void Use(bool doLogic)
                    {
                        if (doLogic)
                        {
                            if (/*<bind>*/M() is { result: var outer, Item2: { errorMessage: var errorMessage } })/*</bind>*/)
                            {
                                Console.Error.WriteLine(errorMessage);
                            }
                        }
                    }
                }

                """);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: outer, errorMessage
                AlwaysAssigned: outer, errorMessage
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: errorMessage
                DefinitelyAssignedOnEntry: doLogic
                DefinitelyAssignedOnExit: doLogic, outer, errorMessage
                ReadInside: 
                ReadOutside: doLogic, errorMessage
                WrittenInside: outer, errorMessage
                WrittenOutside: doLogic
                """, dataFlowAnalysisResults);
        }
    }
}
