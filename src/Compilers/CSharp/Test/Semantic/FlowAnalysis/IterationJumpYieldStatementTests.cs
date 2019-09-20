// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IterationJumpYieldStatementTests : FlowTestBase
    {
        #region "While, Do, Break, Continue"

        [Fact]
        public void TestBreakStatement()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
        while (true) { 
/*<bind>*/
            break;
            while (true) break;
            int y;
/*</bind>*/
        }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.StartPointIsReachable);
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestContinueStatement()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
        while (true) { 
/*<bind>*/
            continue;
            while (true) continue;
            int? y;
/*</bind>*/
        }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantBooleanFalse()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int? x;
/*<bind>*/
        while (false) { 
        }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantBooleanTrue()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (true) { 
        }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantBooleanXor()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (true ^ false) { 
        }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantBooleanNew()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (!new bool()) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(3850, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestLoopWithConstantBooleanChecked()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (checked(true)) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithAssignmentInCondition()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        bool x;
/*<bind>*/
        while (x = true) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantEnumEquality()
        {
            var analysisResults = CompileAndAnalyzeControlFlowStatements(@"
using System;
class C {
    static void Goo()
    {
/*<bind>*/
        while (DayOfWeek.Sunday == 0) { }
/*</bind>*/
    }
}
");
            Assert.False(analysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithConstantNaNComparison()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (!(0 > double.NaN)) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantNaNComparison2()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (double.NaN != double.NaN) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantStringEquality()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while ("""" == """" + null) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantStringEqualityWithUnicodeEscapes()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (""\u0065"" == ""e"" + null) { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithConstantVerbatimStringEquality()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (@""\u0065"" == ""e"") { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithEmptyBlockAfterIt()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Goo()
    {
        int x;
/*<bind>*/
        while (true) { } { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestLoopWithUnreachableBreakStatement()
        {
            var controlFlowAnalysisResults = CompileAndAnalyzeControlFlowStatements(@"
class C {
    static void Main()
    {
/*<bind>*/
        while (true) { if(false) break; }
/*</bind>*/
    }
}
");
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithReachableBreakStatement()
        {
            var controlFlowAnalysisResults = CompileAndAnalyzeControlFlowStatements(@"
class C {
    static void Main()
    {
/*<bind>*/
        while (true) { if(true) break; }
/*</bind>*/
    }
}
");
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithContinueStatement()
        {
            var controlFlowAnalysisResults = CompileAndAnalyzeControlFlowStatements(@"
class C {
    static void Main()
    {
/*<bind>*/
        while (true) { continue; }
/*</bind>*/
    }
}
");
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithContinueAndUnreachableBreakStatement()
        {
            var controlFlowAnalysisResults = CompileAndAnalyzeControlFlowStatements(@"
class C {
    static void Main()
    {
/*<bind>*/
        while (true) { continue; break; }
/*</bind>*/
    }
}
");
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithConstantTernaryOperator()
        {
            var controlFlowAnalysisResults = CompileAndAnalyzeControlFlowStatements(@"
class C {
    static void Main()
    {
/*<bind>*/
        while (true ? true : true) {  }
/*</bind>*/
    }
}
");
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [Fact]
        public void TestLoopWithShortCircuitingOr()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Main()
    {
        bool x;
/*<bind>*/
        while (true || x) {  }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(540183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540183")]
        [Fact]
        public void ControlledStatement01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C
{
    static void Main()
    {
        while (true) /*<bind>*/Main();/*</bind>*/
    }
}");
            var controlFlowAnalysisResults = analysisResults.Item1;
            //var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [WorkItem(540183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540183")]
        [Fact]
        public void ControlledStatement02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C
{
    static void Main()
    {
        /*<bind>*/while (true) Main();/*</bind>*/
    }
}");
            var controlFlowAnalysisResults = analysisResults.Item1;
            //var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
        }

        #endregion

        #region "For, Foreach, Break, Continue"

        [Fact]
        public void TestVariablesDeclaredInForLoop()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
/*<bind>*/
        for(int i = 1, j = 5; i < 10; i++) {  }
/*</bind>*/
        int c;
    }
}");
            Assert.Equal("i, j", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Equal("i, j", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Equal("i", GetSymbolNamesJoined(analysis.ReadInside));
        }

        [WorkItem(539603, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539603")]
        [Fact]
        public void TestForIncrement()
        {
            var dataFlowAnalysisResults = CompileAndAnalyzeDataFlowExpression(@"
class C
{
    static void M()
    {
        for (int i = 0; i < 10; /*<bind>*/i = i + 1/*</bind>*/)
        {
        }
    }
}
");
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("i", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestNestedForLoops()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C
{
    static void M()
    {
        for (int i = 0; i < 2; i = i + 1)
        {
            for (int? j = 0; j < 2; j = j + 1)
            {
                /*<bind>*/
                for (int k = 0; k < 2; k = k + 1)
                {
                }
                /*</bind>*/;
            }
        }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("k", GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("k", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i, j", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("k", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("i, j", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(539701, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539701")]
        [Fact]
        public void ContinueInForStatement()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
using System;

class Program
{
    static void Main(string[] args)
    {
        int i;
        /*<bind>*/for (i = 0; i < 10; i++)
        {
            continue;
        }/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            //var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
        }

        [WorkItem(528498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528498")]
        [Fact]
        public void TestVariablesDeclaredInForeachLoop01()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
/*<bind>*/
        foreach(var c in """") {  }
/*</bind>*/
        int b;
    }
}");
            Assert.Equal("c", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Equal("c", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(analysis.ReadInside));
        }

        [WorkItem(528498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528498")]
        [Fact]
        public void TestVariablesDeclaredInForeachLoop02()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
/*<bind>*/
        foreach (short? v in new int?[] { 1, null, 2, x }) { }
/*</bind>*/
        int b;
    }
}");
            Assert.Equal("v", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Equal("v", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Equal("x", GetSymbolNamesJoined(analysis.ReadInside));
        }

        [WorkItem(528498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528498")]
        [WorkItem(541438, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541438")]
        [Fact]
        public void TestLocalsInForeachLoop()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
using System;
using System.Collections.Generic;

class Test
{
    public void F(IEnumerable<byte> ary)
    {
        ushort? a = 123;
/*<bind>*/
        foreach (var v in ary)
        {
            int x = v + a.Value;
            Console.WriteLine(x);
        }
/*</bind>*/
    }
}
");
            Assert.Equal("v, x", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(analysis.AlwaysAssigned));
            Assert.Equal("ary, a", GetSymbolNamesJoined(analysis.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(analysis.DataFlowsOut));
            Assert.Equal("ary, a, v, x", GetSymbolNamesJoined(analysis.ReadInside));
            Assert.Equal("v, x", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(analysis.ReadOutside));
            Assert.Equal("this, ary, a", GetSymbolNamesJoined(analysis.WrittenOutside));
        }

        [Fact]
        public void TestLocalsInForeachLoop02()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
using System;
using System.Collections.Generic;

class Test
{
    public void F(IEnumerable<byte> ary)
    {
        ushort a = 123;
        foreach (var v in ary)
/*<bind>*/
        {
            int x = v + a;
            Console.WriteLine(x);
        }
/*</bind>*/
    }
}
");
            Assert.Equal("x", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned));
            Assert.Equal("a, v", GetSymbolNamesJoined(analysis.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(analysis.DataFlowsOut));
            Assert.Equal("a, v, x", GetSymbolNamesJoined(analysis.ReadInside));
            Assert.Equal("x", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Equal("ary", GetSymbolNamesJoined(analysis.ReadOutside));
            Assert.Equal("this, ary, a, v", GetSymbolNamesJoined(analysis.WrittenOutside));
        }

        [Fact]
        public void TestLocalsInForeachLoop03()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
using System;
using System.Collections.Generic;

class Test
{
    public void F(IEnumerable<byte?> ary)
    {
        ushort a = 123;
        foreach (var v in ary)
        {
/*<bind>*/
            int x = (v.HasValue ? v.Value : 0) + a;
            Console.WriteLine(x);
/*</bind>*/
        }
    }
}
");
            Assert.Equal("x", GetSymbolNamesJoined(analysis.VariablesDeclared));
            Assert.Equal("x", GetSymbolNamesJoined(analysis.AlwaysAssigned));
            Assert.Equal("a, v", GetSymbolNamesJoined(analysis.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(analysis.DataFlowsOut));
            Assert.Equal("a, v, x", GetSymbolNamesJoined(analysis.ReadInside));
            Assert.Equal("x", GetSymbolNamesJoined(analysis.WrittenInside));
            Assert.Equal("ary", GetSymbolNamesJoined(analysis.ReadOutside));
            Assert.Equal("this, ary, a, v", GetSymbolNamesJoined(analysis.WrittenOutside));
        }

        [WorkItem(541711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541711")]
        [Fact]
        public void ForEachVariableShouldNotInVariableDeclaredTest()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Main()
    {
        int i = 20;
        foreach (var i100 in new int[] { 4, 5 })
        {
        }
        /*<bind>*/return;/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("i, i100", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void ForEachVariablesDeclared()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
public class Program
{
    public static void Main()
    {
        var args = new string[] { ""hi"" };
        foreach (var s in args)
        {
            integer[] b = new integer[]{args.Length};
        }
    }
}");
            var comp = CreateCompilation(new[] { tree });
            var semanticModel = comp.GetSemanticModel(tree);
            var foreachNode = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ForEachStatementSyntax>().Single();
            var flow = semanticModel.AnalyzeDataFlow(foreachNode);
            Assert.Equal(2, flow.VariablesDeclared.Count());
            Assert.Equal(true, flow.VariablesDeclared.Any((s) => s.Name == "b"));
            Assert.Equal(true, flow.VariablesDeclared.Any((s) => s.Name == "s"));
        }

        #endregion

        #region "Return"

        [Fact]
        public void TestReturnStatements01()
        {
            var analysis = CompileAndAnalyzeControlFlowStatements(@"
class C {
    public void F(int x)
    {
/*<bind>*/
        return;
/*</bind>*/
    }
}");
            Assert.False(analysis.EndPointIsReachable);
            Assert.Equal(1, analysis.ExitPoints.Count());
        }

        [Fact]
        public void TestReturnStatements02()
        {
            var analysis = CompileAndAnalyzeControlFlowStatements(@"
class C {
    public void F(int x)
    {
        if (x == 0) return;
/*<bind>*/
        if (x == 1) return;
/*</bind>*/
        if (x == 2) return;
    }
}");
            Assert.Equal(1, analysis.ExitPoints.Count());
        }

        [Fact]
        public void TestReturnStatement()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Main()
    {
        int x = 1, y = x;
/*<bind>*/
        return;
/*</bind>*/
        int z = (y) + 1;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("x, y, z", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestReturnStatementWithExpression()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static int Goo()
    {
        int x = 1, y = x;
/*<bind>*/
        return (y) + 1;
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("x, y", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestReturnStatementWithAssignmentExpressions()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static int? Goo()
    {
        int x = 0;
/*<bind>*/
        return x = x = 1;
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestReturnStatementWithAssignmentExpressions2()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static int Goo()
    {
        int? x;
/*<bind>*/
        return x = x = 1;
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(528583, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528583")]
        [Fact]
        public void InaccessibleVariables()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C {
    static void Main()
    {
        for (int i1 = 1; i1 < 100; i1++)
        {
            int i2 = i1 + 1;
        }

        while (true)
        {
            int i3 = 99;
        }

        foreach (var i4 in new int[] { 4, 5 })
        {
            int i5 = 99;
        }

        System.Func<int?, int?> f1 = (x) => x;

        /*<bind>*/ return; /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Empty(controlFlowAnalysisResults.EntryPoints);
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("i1, x", GetSymbolNamesJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Null(GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("i1, i2, i3, i4, i5, f1, x", GetSymbolNamesJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        #endregion

        #region "Yield Return, Break"

        [WorkItem(543070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543070")]
        [Fact]
        public void TestYieldStatements01()
        {
            var analysis = CompileAndAnalyzeControlAndDataFlowMultipleStatements(@"
using System;
using System.Collections;
using System.Collections.Generic;

class Test
{
    public IEnumerator Iterator01()
    /*<bind0>*/{
        yield return 1;
        yield break;
    }/*</bind0>*/

    public IEnumerator<int> Iterator02()
    {
/*<bind1>*/
        yield return 2;
/*</bind1>*/
    }

    public IEnumerable Iterator11()
    /*<bind2>*/{
        yield return 3;
    }/*</bind2>*/

    public IEnumerable<int> Iterator12()
    {
        yield return 4;
       /*<bind3>*/  yield break; /*</bind3>*/
    }
}
");
            var ctrlFlowAnalysis = analysis.Item1;

            var reachable = new bool[] { false, true, true, false };
            var bkcount = new int[] { 1, 0, 0, 1 };
            int idx = 0;

            foreach (var ctrlFlow in ctrlFlowAnalysis)
            {
                if (reachable[idx])
                    Assert.True(ctrlFlow.EndPointIsReachable);
                else
                    Assert.False(ctrlFlow.EndPointIsReachable);

                Assert.Equal(bkcount[idx], ctrlFlow.ExitPoints.Count());

                idx++;
            }
        }

        [WorkItem(543070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543070")]
        [Fact]
        public void TestYieldStatements02()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class Test
{
    public IEnumerator Iterator01()
    {
        byte local = 0;
        /*<bind0>*/
        do
        {
            var ret = local % 3;
            /*<bind1>*/
            if (ret == 0)
            {
                yield return local++;
                yield break;
            }
            else if (ret == 1)
            {
                local++;
                yield return local++;
                yield break;
            }
            /*</bind1>*/
            yield return local++;

            if (local >= 11)
                break;
        } while (true);
        /*</bind0>*/
    }

    public IEnumerator<Test> Iterator02(IList<Test> list)
    {
        /*<bind2>*/
        foreach (var t in list)
        {
            if (t == null)
                yield break;

            yield return t;
        }
        /*</bind2>*/
    }
}
";

            var analysis = CompileAndAnalyzeControlAndDataFlowMultipleStatements(source);
            var ctrlFlowAnalysis = analysis.Item1.ToList();

            var ctrlFlow = ctrlFlowAnalysis[0];
            Assert.True(ctrlFlow.EndPointIsReachable);
            Assert.Equal(2, ctrlFlow.ExitPoints.Count());

            ctrlFlow = ctrlFlowAnalysis[1];
            Assert.True(ctrlFlow.EndPointIsReachable);
            Assert.Equal(2, ctrlFlow.ExitPoints.Count());

            ctrlFlow = ctrlFlowAnalysis[2];
            Assert.True(ctrlFlow.EndPointIsReachable);
            Assert.Equal(1, ctrlFlow.ExitPoints.Count());
        }

        [WorkItem(543070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543070")]
        [Fact]
        public void TestYieldStatements03()
        {
            #region "source"
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class Test
{
    Test lockObject = new Test();
    IEnumerable Iterator11<T, V>(T t, V v) where T : V where V: class
    {
        /*<bind0>*/
        lock (lockObject)
        {
            if (t != null)
            {
                yield return t;
                goto L1;
            }
            yield return default(T);
        L1:
            if (t == null && v != null)
            {
                yield return v;
                yield break;
            }
            /*<bind1>*/
            {
                yield return default(V);
                yield return null;
                yield break;
            }
            /*</bind1>*/
        }
        /*</bind0>*/
    }

    [Flags]
    internal enum E { None, One, Two, Four = 4, Eight = Four * 2, Sixteen = Eight << 1 }
    internal IEnumerable<T> Iterator12<T>(E e, List<T> list) where T: struct
    {
        /*<bind2>*/
        for (int i = 0; i < list.Count -1; i += 2)
        {
            /*<bind3>*/
            switch (e)
            {
                case E.One:
                case E.Two:
                    yield return list[i];
                    break;
                case E.Eight:
                    yield return list[i+1];
                    break;
                case E.Sixteen:
                    yield return default(T);
                    break;
                default:
                    yield break;
            }
            /*</bind3>*/
            if (i> 100)
                yield break;
        }
        /*</bind2>*/
    }
}
";
            #endregion

            var analysis = CompileAndAnalyzeControlAndDataFlowMultipleStatements(source);
            var ctrlFlowAnalysis = analysis.Item1.ToList();

            var ctrlFlow = ctrlFlowAnalysis[0];
            Assert.False(ctrlFlow.EndPointIsReachable);
            Assert.Equal(2, ctrlFlow.ExitPoints.Count());

            ctrlFlow = ctrlFlowAnalysis[1];
            Assert.False(ctrlFlow.EndPointIsReachable);
            Assert.Equal(1, ctrlFlow.ExitPoints.Count());

            ctrlFlow = ctrlFlowAnalysis[2];
            Assert.True(ctrlFlow.EndPointIsReachable);
            Assert.Equal(2, ctrlFlow.ExitPoints.Count());

            ctrlFlow = ctrlFlowAnalysis[3];
            Assert.True(ctrlFlow.EndPointIsReachable);
            Assert.Equal(1, ctrlFlow.ExitPoints.Count());
        }

        [WorkItem(543564, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543564")]
        [Fact]
        public void YieldReturnStatement()
        {
            var source = @"
using System.Collections.Generic;

public class Test 
{
    public IEnumerator<int?> M1() { /*<bind>*/yield return 0;/*</bind>*/ }
}
";
            var ctrlFlowAnalysis = CompileAndAnalyzeControlFlowStatements(source);
            Assert.Empty(ctrlFlowAnalysis.ExitPoints);
        }

        #endregion
    }
}
