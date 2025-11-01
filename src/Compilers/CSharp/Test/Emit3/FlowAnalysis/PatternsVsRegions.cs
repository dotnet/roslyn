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

        [Fact]
        public void RegionWithinSubpattern01()
        {
            var comp = CreateCompilation("""
                int x = 1;
                if (x is var y and 2) { }
                """);

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var constantPattern = tree.GetRoot().DescendantNodes().OfType<ConstantPatternSyntax>().Single();
            Assert.Equal("2", constantPattern.ToString());

            Assert.Throws<ArgumentException>(() => model.AnalyzeDataFlow(constantPattern));
            var dataFlow = model.AnalyzeDataFlow(constantPattern.Expression);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: 
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: 
                DataFlowsOut: 
                DefinitelyAssignedOnEntry: x, y, args
                DefinitelyAssignedOnExit: x, y, args
                ReadInside: 
                ReadOutside: x
                WrittenInside: 
                WrittenOutside: x, y, args
                """,
                dataFlow);
        }

        [Fact]
        public void RegionWithinSubpattern02()
        {
            var comp = CreateCompilation("""
                static class Program
                {
                    static void Main()
                    {
                        int x = 1;
                        if (x is var y and x.P and > 2) { }
                    }

                    extension (ref int i)
                    {
                        public int P => i;
                    }
                }
                """);

            comp.VerifyDiagnostics(
                // (6,28): error CS9135: A constant value of type 'int' is expected
                //         if (x is var y and x.P and > 2) { }
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "x.P").WithArguments("int").WithLocation(6, 28));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var constantPattern = tree.GetRoot().DescendantNodes().OfType<ConstantPatternSyntax>().Single();
            Assert.Equal("x.P", constantPattern.ToString());

            Assert.Throws<ArgumentException>(() => model.AnalyzeDataFlow(constantPattern));
            var dataFlow = model.AnalyzeDataFlow(constantPattern.Expression);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: 
                AlwaysAssigned: 
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: x
                DataFlowsOut: 
                DefinitelyAssignedOnEntry: x, y
                DefinitelyAssignedOnExit: x, y
                ReadInside: x
                ReadOutside: x
                WrittenInside: 
                WrittenOutside: x, y
                """,
                dataFlow);
        }

        [Fact]
        public void RegionWithinSubpattern03()
        {
            var comp = CreateCompilation("""
                bool x = false;
                if (x is var y and (x is var z)) { }
                """);

            comp.VerifyDiagnostics(
                // (2,21): error CS9135: A constant value of type 'bool' is expected
                // if (x is var y and (x is var z)) { }
                Diagnostic(ErrorCode.ERR_ConstantValueOfTypeExpected, "x is var z").WithArguments("bool").WithLocation(2, 21));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var constantPattern = tree.GetRoot().DescendantNodes().OfType<ConstantPatternSyntax>().Single();
            Assert.Equal("(x is var z)", constantPattern.ToString());

            Assert.Throws<ArgumentException>(() => model.AnalyzeDataFlow(constantPattern));
            var dataFlow = model.AnalyzeDataFlow(constantPattern.Expression);
            VerifyDataFlowAnalysis("""
                VariablesDeclared: z
                AlwaysAssigned: z
                Captured: 
                CapturedInside: 
                CapturedOutside: 
                DataFlowsIn: x
                DataFlowsOut: 
                DefinitelyAssignedOnEntry: x, y, args
                DefinitelyAssignedOnExit: x, y, z, args
                ReadInside: x
                ReadOutside: x
                WrittenInside: z
                WrittenOutside: x, y, args
                """,
                dataFlow);
        }

        // TODO2: more tests for different pattern kinds
    }
}
