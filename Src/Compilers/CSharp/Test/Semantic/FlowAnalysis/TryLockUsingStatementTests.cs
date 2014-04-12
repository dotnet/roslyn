// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TryLockUsingStatementTests : FlowTestBase
    {
        #region "try-catch-finally"

        [Fact]
        public void TestAssignmentInCatch()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C
{
    static void F()
    {
    }
    static void M(out int x, out int y)
    {
/*<bind>*/
        try
        {
            F();
            x = 1;
            y = 0;
        }
        catch (System.Exception)
        {
            x = 0;
        }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Empty(controlFlowAnalysisResults.EntryPoints);
            Assert.Empty(controlFlowAnalysisResults.ExitPoints);
            Assert.Empty(controlFlowAnalysisResults.ReturnStatements);
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestAssignmentInFinally()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
class C
{
    static void M(out int x, out int y)
    {
/*<bind>*/
        try
        {
            y = 0;
            return;
        }
        finally
        {
            x = 0;
        }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(1, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestBreakContinueInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
public class TryCatchFinally
{
    public byte TryMethod(byte para)
    {
        byte by = 9;
        while (by > 0)
        {
            by--;
/*<bind>*/
            try
            {
            if (by % 7 == 0)
                continue;
            }
            catch (System.Exception)
            {
                if (by % 11 == 0)
                {
                    break;
                }
            }
            finally
            {
                try
                {
                    break; // CS0157
                }
                catch
                {
                }
            }
/*</bind>*/
        }
        return by;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(3, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("by, para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(528296, "DevDiv")]
        [Fact]
        public void TestReturnInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
public class TryCatchFinally
{
    public byte TryMethod(ref byte para)
    {
        byte by = 10;
        /*<bind>*/
        try
        {
            if (by % 13 == 0)
            {
                return 13;
            }
            else if (by % 17 == 0)
            {
                return 17;
            }
        }
        catch (System.Exception)
        {
            try
            {
                return 123;
            }
            finally
            {
                if (by % 11 == 0)
                {
                    return 11; // CS0157
                }
            }
            return 7;
        }
        /*</bind>*/
        return by;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(5, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("by, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("by, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("by, para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestGotoInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
public class TryCatchFinally
{
    public void TryMethod(out byte para)
    {
        byte by = 10;
    L1:
        para = by;
        /*<bind>*/
        try
        {
            by = (byte)(by + by);
            L2:
            by = (byte)(by / by);
            if (by / 13 == 0)
            {
                goto L1; // ok
            }
            else if (by / 17 == 0)
            {
                goto L2; // ok
            }
        }
        finally
        {
            by = by--;
            try
            {
        L3:     ;
            }
            catch (System.Exception)
            {
                goto L3; // CS0159
            }
            goto L1; // CS0157
        }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(2, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("by, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("by", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("by, para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestThrowInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(uint para)
    {
        try
        {
            /*<bind>*/
            throw new DivideByZeroException();
            /*</bind>*/
        }
        catch (IndexOutOfRangeException)
        {
            para++;
        }
        finally { para--;}
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(541724, "DevDiv")]
        [Fact]
        public void TestThrowInTry02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ref uint para)
    {
        try
        {
            throw new DivideByZeroException();
        }
        catch (IndexOutOfRangeException)
        {
            para++;
        }
        catch (DivideByZeroException)
        {
            /*<bind>*/
            para--;
            /*</bind>*/
            // rethrow
            throw;
        }
        finally { }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal(0, controlFlowAnalysisResults.EntryPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ExitPoints.Count());
            Assert.Equal(0, controlFlowAnalysisResults.ReturnStatements.Count());
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestThrowInTry03()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte sb;
        /*<bind>*/
        try
        {
            sb = 0;
            try
            {
                // int x = (100 / sb);
                throw new DivideByZeroException();
            }
            catch (IndexOutOfRangeException)
            {
                sb++;
            }
            catch (DivideByZeroException)
            {
                // rethrow
                throw;
            }
            finally {  sb--;  }
        }
        catch (DivideByZeroException)
        {
            throw new NullReferenceException();
        }
        catch (NullReferenceException)
        {
            sb = -128;
        }
        finally 
        {  
            throw; // CS0156
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
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("sb", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("sb", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestAlwaysAssignedInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(out ulong para)
    {
        string local;
/*<bind>*/
        try
        {
            local = ""try"";
            throw new DivideByZeroException();
        }
        catch (IndexOutOfRangeException)
        {
            local = ""ex"";
            para = 12345;
        }
        catch (DivideByZeroException)
        {
         throw;
        }
        finally { }
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
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("local, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("local, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestAlwaysAssignedInTry02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ref long? para)
    {
        long? local;
/*<bind>*/
        try
        {
            if (para > 0)
                local = 12345;
        }
        catch (IndexOutOfRangeException)
        {
            local = -1;
            throw;
            para = local;
        }
        finally { }
/*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("local, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("local, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(528567, "DevDiv")]
        [WorkItem(541723, "DevDiv")]
        [Fact]
        public void TestAlwaysAssignedInTry03()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ref string para)
    {
        string local;

        try
        {
            if (!String.IsNullOrEmpty(para))
                local = ""try"";
            else
                para = local;
        }
        catch (ArgumentException ax)
        {
/*<bind>*/
            para = -0;
            Console.WriteLine(ax);
/*</bind>*/
            // throw;
        }
        finally { }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("ax", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ax", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("local, para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("para", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("ax, local, para, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestDataFlowsInOut01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    protected void TryMethod(long p)
    {
        long x = 0, y = 1, z;
        /*<bind>*/
        try
        {
            if (p > 0)
                z = x;
        }
        catch (Exception)
        {
            throw;
            z = y;
        }
        finally
        {
            if (false)
                x = y*y;
        }
        /*</bind>*/
        x = z * y;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("p, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("p, x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("p, this, x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(540797, "DevDiv")]
        [Fact]
        public void TestDataFlowsInOut02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(long p)
    {
        long? x = null, y = 1, z;
        /*<bind>*/
        try
        {
            x = p;
        }
        catch (Exception)
        {
        }
        finally
        {
            z = x;
        }
        /*</bind>*/
        p = x.Value * y.Value;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("p, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("p, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("p, this, x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(540798, "DevDiv")]
        [Fact]
        public void TestDataFlowsInOut03()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ref long p)
    {
        long x, y, z = 111;
L1:
        x = z;
        /*<bind>*/
        try
        {
            L2:  y = x + x;
            goto L2;
        }
        catch (ArgumentException ax)
        {
            p = y * y;
            goto L1;
        }
        finally
        {
            z = x;
        }
        /*</bind>*/
        p = y;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("ax", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("ax, p, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            // y flows out as follows: the assignment in the try block, followed by an exception taking it into the catch
            // block, then goto L1, (now the value is outside the region), followed by flowing back in to the try block,
            // then another exception arising before executing the assignment to y, then reading y in the catch block.
            Assert.Equal("p, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("p, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("ax, p, y, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("p, this, x, z", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(541655, "DevDiv")]
        [WorkItem(541723, "DevDiv")]
        [Fact]
        public void TestVariablesDeclaredInTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        /*<bind>*/
        try
        {
            if (false)
            {
                string s = ""SOS"";
            }
        }
        catch (ArgumentException ax)
        {
            throw;
            ushort s = 123;
        }
        finally
        {
            short s = 456;
        }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("ax, s, s, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("ax, s, s, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestFlowsOutTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
public class TryCatchFinally
{
    public static void Main()
    {
        int x = 12;
        try
        {
            /*<bind>*/
            x = 12;
            return;
            /*</bind>*/
        }
        finally
        {
            int z = x;
        }
    }
}
");
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact]
        public void TestFlowsOutTry02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"
public class TryCatchFinally
{
    public static void M(int n)
    {
    L1:
        try
        {
            try
            {
            L2:
                /*<bind>*/
                n++;
                if (n < 99)
                    goto L1;
                /*</bind>*/
                if (n < 999)
                    goto L2;
            }
            catch (Exception x)
            {
            }
        }
        finally
        {
            n = 0;
        }
    }
}
");
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.Equal("n", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("n", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("n", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact]
        public void TestVariableInCatch01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte x = 111, y = 222;
        try
        {
            sbyte s = x;
        }
        catch (ArgumentException ax)
        {
            /*<bind>*/
            Console.Write(ax);
            goto L;
            // unreachable is ALWAYS assigned
            sbyte s = y;
           /*</bind>*/
        }
        finally
        {
            x= y;
        }
        L: y = x;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            // unreachable is ALWAYS assigned
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("ax", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ax, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("ax, s, this, x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestVariableInCatch02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte x = 111, y = 222;
        /*<bind>*/
        try
        {
            sbyte s = x;
        }
        catch (ArgumentException ax)
        {
            Console.Write(ax);
            goto L;
        }
        catch (Exception ex)
        {
            Console.Write(ex);
        }
        finally
        {
            x = s;
        }
        /*</bind>*/
        L: y = x;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("ax, ex, s", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            // s?
            Assert.Equal("ax, ex, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("ax, ex, s, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this, x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact, WorkItem(528297, "DevDiv")]
        public void TestTryInWhile()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte x = 111, y;
        /*<bind>*/
        while (x-- > 0)
        {
            try
            {
                y = (sbyte)(x / 2);
            }
            finally
            {
                throw new Exception(); // this makes while end-ptr unreachable
            }
        }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable); // possible if while (false)...
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact, WorkItem(528298, "DevDiv")]
        public void TestTryInDoWhile()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte x = 111, y;
        /*<bind>*/
        do 
        {
            try
            {
                y = x;
                break;
            }
            catch (Exception)
            {
                continue;
            }
            finally
            {
                // return;
                throw new Exception();
            } // unreachable
        } while (x++ < 121)
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("x, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestTryInFor()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(short p)
    {
        sbyte y;
        /*<bind>*/
        for (short i = 0; i < p; i++)
        {
            try
            {
                y = GetVal(i);
                continue;
            }
            catch (Exception)
            {
                break;
            }
            finally
            {
                throw new Exception();
            } // unreachable
        }
        /*</bind>*/
    }

    sbyte GetVal(sbyte n)
    {
        return n++;
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            // Bug#7263 (BD) - if the whole 'try' somehow unreachable, the end of 'for' is reachable
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("i", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("p, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("i, p, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("i, y", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("p, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(540835, "DevDiv")]
        [Fact]
        public void TestBracketRegionsInTry()
        {
            var analysisResults01 = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ushort p)
    {
        ulong x= 0, y;
        try
        /*<bind>*/
        {
            y = x + p;
        }
        /*</bind>*/
        catch (Exception ex)
        {
            Console.Write(ex);
        }
    }
}
");

            var analysisResults02 = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod(ushort p)
    {
        ulong x= 0, y;
        try
        {
        /*<bind>*/
            y = x + p;
        /*</bind>*/
        }
        catch (Exception ex)
        {
            Console.Write(ex);
        }
    }
}
");

            var dataFlowResults01 = analysisResults01.Item2;
            var dataFlowResults02 = analysisResults02.Item2;

            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.VariablesDeclared), GetSymbolNamesSortedAndJoined(dataFlowResults01.VariablesDeclared));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.AlwaysAssigned), GetSymbolNamesSortedAndJoined(dataFlowResults01.AlwaysAssigned));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.DataFlowsIn), GetSymbolNamesSortedAndJoined(dataFlowResults01.DataFlowsIn));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.DataFlowsOut), GetSymbolNamesSortedAndJoined(dataFlowResults01.DataFlowsOut));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.ReadInside), GetSymbolNamesSortedAndJoined(dataFlowResults01.ReadInside));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.ReadOutside), GetSymbolNamesSortedAndJoined(dataFlowResults01.ReadOutside));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.WrittenInside), GetSymbolNamesSortedAndJoined(dataFlowResults01.WrittenInside));
            Assert.Equal(GetSymbolNamesSortedAndJoined(dataFlowResults02.WrittenOutside), GetSymbolNamesSortedAndJoined(dataFlowResults01.WrittenOutside));
        }

        [Fact]
        public void TestTryWithLambda01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    delegate long D01(long dp);
    void M(ref long refp, out long outp)
    {
        /*<bind>*/
        try
        {
            outp = refp++;
        }
        catch (Exception e)
        {
            D01 d = (ap) =>
            {
                e = new ArgumentException(ap.ToString());
                return e.Message.Length;
            };
            outp = d(refp);

        }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);

            Assert.Equal("ap, d, e", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("outp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("e", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal("refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("outp, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ap, d, e, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("ap, d, e, outp, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("outp, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("refp, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [WorkItem(541723, "DevDiv")]
        [Fact]
        public void TestTryWithLambda02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    delegate long D01(long dp);
    static void M(ref long refp, out long outp)
    {
        try
        {
            outp = refp++;
        }
        catch (Exception e)
        {
        /*<bind>*/
            D01 d = delegate (long ap)
            {
                e = new ArgumentException(ap.ToString());
                return e.Message.Length;
            };
            outp = d(refp);
        /*</bind>*/
        }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);

            Assert.Equal("ap, d", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("d, outp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("e", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal("refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal("outp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ap, d, e, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("ap, d, e, outp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("outp, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("e, outp, refp", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestTryWithLambda03()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
   delegate string D02(byte dp);
    static string M(ushort p)
    {
        byte local = (byte)(p % byte.MaxValue);
        /*<bind>*/
        try
        {
            if (local == p)
            {
                return null;
            }

            return local.ToString();
        }
        catch (Exception e)
        {
            D02 d = delegate (byte ap)
            {
                return (ap + local + p).ToString() + e.Message;
            };
            return d(local);
        }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);

            Assert.Equal("ap, d, e", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("e, local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ap, d, e, local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("ap, d, e", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestTryWithLambda04()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    delegate string D02(byte? dp);
    internal string M(ushort p)
    {
        byte? local = (byte)(p % byte.MaxValue);
        try
        {
            if (local.Value == p)
            {
                return null;
            }

            return local.Value.ToString();
        }
        catch (Exception e)
        {
        /*<bind>*/
            D02 d = (ap) =>
            {
                return (ap.Value + local.Value + p).ToString() + e.Message;
            };
            return d(local);
        /*</bind>*/
        }
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.False(controlFlowAnalysisResults.EndPointIsReachable);

            Assert.Equal("ap, d", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.VariablesDeclared));
            Assert.Equal("d", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
            Assert.Equal("e, local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.Captured));
            Assert.Equal("e, local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
            Assert.Equal("ap, d, e, local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadInside));
            Assert.Equal("ap, d", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenInside));
            Assert.Equal("local, p", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.ReadOutside));
            Assert.Equal("e, local, p, this", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.WrittenOutside));
        }

        [Fact]
        public void TestIncompleteCatch()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class Program
{
    public static void Main(string[] args)
    {
        int x = 12;
        try
        {
            M1();
        }
        catch (Exception ex)
        {
            /*<bind>*/
            if (args.Length == 2)
            {
                x = 14;
                throw;
            }
            /*</bind>*/
        }
        M2(x);
    }
    public static void M1() {}
    public static void M2(int x) {}
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact]
        public void TestNestedTry01()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class Program
{
    public static void Main(string[] args)
    {
        int x = 12;
            /*<bind>*/
        try
        {
            M1();
        }
        catch (Exception ex)
        {
        }
        finally
        {
            try
            {
                x = 14;
            }
            catch (Exception ex)
            {
            }
        }
            /*</bind>*/
        M2(x);
    }
    public static void M1() {}
    public static void M2(int x) {}
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact]
        public void TestNestedTry02()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class Program
{
    public static void Main(string[] args)
    {
        int x = 12;
            /*<bind>*/
        try
        {
            M1();
        }
        catch (Exception ex)
        {
            try
            {
                x = 14;
            }
            catch (Exception ex)
            {
            }
        }
        finally
        {
        }
            /*</bind>*/
        M2(x);
    }
    public static void M1() {}
    public static void M2(int x) {}
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact]
        public void TestNestedTry03()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class Program
{
    public static void Main(string[] args)
    {
        int x = 12;
            /*<bind>*/
        try
        {
            M1();
            try
            {
                x = 14;
            }
            catch (Exception ex)
            {
            }
        }
        catch (Exception ex)
        {
        }
        finally
        {
        }
            /*</bind>*/
        M2(x);
    }
    public static void M1() {}
    public static void M2(int x) {}
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.DataFlowsOut));
        }

        [Fact, WorkItem(529180, "DevDiv")]
        public void AlwaysAssignedInTry()
        {
            var analysisResults = CompileAndAnalyzeControlAndDataFlowStatements(@"using System;
public class TryCatchFinally
{
    public void TryMethod()
    {
        int x, y;
        /*<bind>*/
        try  {    x = 123;    }
        finally  {    }

        try  {    y = 123;    }
        catch(Exception)  {    }
        /*</bind>*/
    }
}
");
            var controlFlowAnalysisResults = analysisResults.Item1;
            var dataFlowAnalysisResults = analysisResults.Item2;
            Assert.True(controlFlowAnalysisResults.EndPointIsReachable);
            Assert.Equal("x", GetSymbolNamesSortedAndJoined(dataFlowAnalysisResults.AlwaysAssigned));
        }

        #endregion

        [Fact]
        public void TestVariablesDeclaredInUsingStatement1()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
/*<bind>*/
        using (var c = new System.IO.StreamWriter())
        {
            F(x);
        }
/*</bind>*/
        int b;
    }
}");
            Assert.Equal("c", GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal("c", GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal("c", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact]
        public void TestVariablesDeclaredInUsingStatement2()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
        using (var c = new System.IO.StreamWriter())
        {
/*<bind>*/
            F(x);
/*</bind>*/
        }
        int b;
    }
}");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal("c", GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        #region "lock statement"

        [Fact]
        public void TestLockStatement1()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
        C c = new C();
/*<bind>*/
        lock (c)
        {
            F(x);
        }
/*</bind>*/
        int b;
    }
}");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact]
        public void TestLockStatement2()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(@"
class C {
    public void F(int x)
    {
        int a;
        C c = new C();
        lock (c)
        {
/*<bind>*/
            F(x);
/*</bind>*/
        }
        int b;
    }
}");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("c, this, x", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal("c", GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("this, x", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact()]
        public void NestedLock()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(
@"
class Test
{
    public void Main()
    {
/*<bind>*/
        lock (typeof(Test))
        {
            lock (new Test())
            {
            }
        }
/*</bind>*/
    }
}
");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("this", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact()]
        public void LockAnonymousTypes()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(
@"
class Test
{
    public static void Main()
    {
        string name = "";
        object obj = new object();
/*<bind>*/
        lock (new { p1 = name, p2 = foo(obj) })
        {
        }
/*</bind>*/
    }
    static int foo(object  x)
    { return 1; }
}
");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("name, obj", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("name, obj", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("name, obj", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact()]
        public void AssignmentInLock()
        {
            var analysis = CompileAndAnalyzeDataFlowStatements(
@"
class Test
{
    public static void Main()
    {
        string str = string.Empty;
        object obj;
/*<bind>*/
        lock (obj = new string[][] { new string[] { """" }, new string[] { str } })
        {
        }
/*</bind>*/
        System.Console.Write(obj);
    }
}
");
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysis.VariablesDeclared));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysis.WrittenInside));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysis.WrittenOutside));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysis.ReadInside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysis.ReadOutside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysis.AlwaysAssigned));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysis.DataFlowsIn));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysis.DataFlowsOut));
        }

        [Fact()]
        public void BranchOutFromLock()
        {
            var analysis = CompileAndAnalyzeControlAndDataFlowStatements(
@"
class Test
{
    public static void Main()
    {
        string str = string.Empty;
        object obj;
/*<bind>*/
        lock (new string[][] { new string[] { """" }, new string[] { str } })
        {
            obj = new object();
            return;
        }
/*</bind>*/
        System.Console.Write(obj);
    }
}
");
            var analysisControlFlow = analysis.Item1;
            var analysisDataflow = analysis.Item2;
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.VariablesDeclared));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.WrittenInside));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysisDataflow.WrittenOutside));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysisDataflow.ReadInside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.ReadOutside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.AlwaysAssigned));
            Assert.Equal("str", GetSymbolNamesSortedAndJoined(analysisDataflow.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.DataFlowsOut));

            Assert.Equal(1, analysisControlFlow.ExitPoints.Count());
            Assert.Equal(0, analysisControlFlow.EntryPoints.Count());
        }

        [Fact()]
        public void BranchInLock()
        {
            var analysis = CompileAndAnalyzeControlAndDataFlowStatements(
@"
class Test
{
    static int x;
    public static void Main()
    {
        object obj;
/*<bind>*/
        lock (obj = new object())
        {
            if (x > 1)
            { goto lab1; }
            else
            { System.Console.Write(obj);}
        lab1: System.Console.WriteLine();
        }
/*</bind>*/
    }
}
");
            var analysisControlFlow = analysis.Item1;
            var analysisDataflow = analysis.Item2;
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.VariablesDeclared));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.WrittenInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.WrittenOutside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.ReadInside));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.ReadOutside));
            Assert.Equal("obj", GetSymbolNamesSortedAndJoined(analysisDataflow.AlwaysAssigned));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.DataFlowsIn));
            Assert.Equal(null, GetSymbolNamesSortedAndJoined(analysisDataflow.DataFlowsOut));

            Assert.Equal(0, analysisControlFlow.ExitPoints.Count());
            Assert.Equal(0, analysisControlFlow.EntryPoints.Count());
            Assert.Equal(0, analysisControlFlow.ReturnStatements.Count());

        }
        #endregion
    }
}
