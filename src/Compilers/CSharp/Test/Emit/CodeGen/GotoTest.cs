// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class GotoTests : EmitMetadataTestBase
    {
        [Fact]
        public void Goto()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""goo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
        return;
    }
}
";
            string expectedOutput = @"goo
bar
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        // Identical to last test, but without "return" statement.  (This was failing once.)
        [Fact]
        public void GotoWithoutReturn()
        {
            string source = @"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""goo"");
        goto bar;
        Console.Write(""you won't see me"");
        bar: Console.WriteLine(""bar"");
    }
}
";
            string expectedOutput = @"goo
bar
";

            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        // The goto can also be used to jump to a case or default statement in a switch
        [Fact]
        public void GotoInSwitch()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        string Fruit = ""Apple"";
        switch (Fruit)
        {
            case ""Banana"":
                break;
            case ""Chair"":
                break;
            case ""Apple"":
                goto case ""Banana"";
            case ""Table"":
                goto default;
            default:
                break;
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       58 (0x3a)
  .maxstack  2
  .locals init (string V_0) //Fruit
  IL_0000:  ldstr      ""Apple""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldstr      ""Banana""
  IL_000c:  call       ""bool string.op_Equality(string, string)""
  IL_0011:  brtrue.s   IL_0039
  IL_0013:  ldloc.0
  IL_0014:  ldstr      ""Chair""
  IL_0019:  call       ""bool string.op_Equality(string, string)""
  IL_001e:  brtrue.s   IL_0039
  IL_0020:  ldloc.0
  IL_0021:  ldstr      ""Apple""
  IL_0026:  call       ""bool string.op_Equality(string, string)""
  IL_002b:  brtrue.s   IL_0039
  IL_002d:  ldloc.0
  IL_002e:  ldstr      ""Table""
  IL_0033:  call       ""bool string.op_Equality(string, string)""
  IL_0038:  pop
  IL_0039:  ret
}");
        }

        // Goto location outside enclosing block 
        [WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        [Fact]
        public void LocationOfGotoOutofClosure()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        if (i == 1)
            goto Lab1;
        else
            goto Lab2;
    Lab1:
        i = 2;
    Lab2:
        i = 3;

        System.Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  pop
  IL_0003:  pop
  IL_0004:  ldc.i4.3
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ret
}");
        }

        // Goto location in enclosing block  
        [WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        [Fact]
        public void LocationOfGotoInClosure()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        if (i == 1)
            goto Lab1;
        else
            goto Lab2;
    Lab1:
        i = 2;
    Lab2:
        i = 3;

        System.Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldc.i4.1
  IL_0002:  pop
  IL_0003:  pop
  IL_0004:  ldc.i4.3
  IL_0005:  call       ""void System.Console.WriteLine(int)""
  IL_000a:  ret
}");
        }

        // Same label in different scope  
        [WorkItem(539876, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539876")]
        [Fact]
        public void SameLabelInDiffScope()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        {
        Lab1:
            goto Lab1;
        }
        {
        Lab1:
            return;
        }
    }
}
";
            var c = CompileAndVerify(text);

            c.VerifyDiagnostics(
                // (11,9): warning CS0162: Unreachable code detected
                //         Lab1:
                Diagnostic(ErrorCode.WRN_UnreachableCode, "Lab1"),
                // (11,9): warning CS0164: This label has not been referenced
                //         Lab1:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "Lab1"));

            c.VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
");
        }

        // Label Next to Label  
        [WorkItem(539877, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539877")]
        [Fact]
        public void LabelNexttoLabel()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
    Lab3:
    // Lab1:
        goto Lab2;
    Lab2:
        goto Lab3;
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
");
        }

        // Infinite loop
        [WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        [Fact]
        public void Infiniteloop()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
    A:
        goto B;
    B:
        goto A;
    }
}
";
            CompileAndVerify(text).VerifyDiagnostics().VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  0
  IL_0000:  br.s       IL_0000
}
");
        }

        // unreachable code
        [WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        [Fact]
        public void CS0162WRN_UnreachableCode()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5;)
        {
            i = 2;
            goto Lab2;
            i = 1;
            break;
        Lab2:
            return ;
        }
    }
}
";
            var c = CompileAndVerify(text);

            c.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "i"));

            c.VerifyIL("C.Main", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0  
  IL_0001:  stloc.0   
  IL_0002:  br.s       IL_0007
  IL_0004:  ldc.i4.2  
  IL_0005:  stloc.0   
  IL_0006:  ret       
  IL_0007:  ldloc.0   
  IL_0008:  ldc.i4.5  
  IL_0009:  blt.s      IL_0004
  IL_000b:  ret       
}
");
        }

        // Declare variable after goto
        [WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        [Fact]
        public void DeclareVariableAfterGoto()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        goto label1;
        string s = ""A""; // unreachable
    label1:
        s = ""B"";
        System.Console.WriteLine(s);
    }
}
";
            var c = CompileAndVerify(text);
            c.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_UnreachableCode, "string"));

            c.VerifyIL("C.Main", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldstr      ""B""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ret
}
");
        }

        // Finally is executed while use 'goto' to exit try block
        [WorkItem(540721, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540721")]
        [Fact]
        public void GotoInTry()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        try { i = 1; goto lab1; }
        catch { i = 2; }
        finally { System.Console.WriteLine(""a""); }
    lab1:
        System.Console.WriteLine(i);
        return;
    }
}
";
            var c = CompileAndVerify(text, expectedOutput: @"a
1");

            c.VerifyIL("C.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_0016
}
  catch object
{
  IL_0006:  pop
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  leave.s    IL_0016
}
}
  finally
{
  IL_000b:  ldstr      ""a""
  IL_0010:  call       ""void System.Console.WriteLine(string)""
  IL_0015:  endfinally
}
  IL_0016:  ldloc.0
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  ret
}");
        }

        [WorkItem(540716, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540716")]
        [Fact]
        public void GotoInFinallyBlock()
        {
            var text = @"
class C
{
    static void Main(string[] args)
    {
        int i = 0;
        try { i = 1; }
        catch { i = 2; }
        finally { lab1: i = 3; goto lab1; }

        System.Console.WriteLine(i);
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       17 (0x11)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  .try
{
  .try
{
  IL_0002:  ldc.i4.1
  IL_0003:  stloc.0
  IL_0004:  leave.s    IL_000f
}
  catch object
{
  IL_0006:  pop
  IL_0007:  ldc.i4.2
  IL_0008:  stloc.0
  IL_0009:  leave.s    IL_000f
}
}
  finally
{
  IL_000b:  ldc.i4.3
  IL_000c:  stloc.0
  IL_000d:  br.s       IL_000b
}
  IL_000f:  br.s       IL_000f
}");
        }

        // Optimization redundant branch for code generate
        [Fact, WorkItem(527952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527952")]
        public void OptimizationForGoto()
        {
            var source = @"
class C
{
    static int Main(string[] args)
    {
        goto Lab1;
    Lab1:
        goto Lab2;
    Lab2:
        goto Lab3;
    Lab3:
        goto Lab4;
    Lab4:
        return 0;
    }
}
";
            var c = CompileAndVerify(source, options: TestOptions.ReleaseDll);

            c.VerifyIL("C.Main", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  ret
}
");
        }

        [Fact, WorkItem(528010, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528010")]
        public void GotoInLambda()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del q = x =>
        {
        label2: goto label1;
        label1: goto label2;
        };

        System.Console.WriteLine(q);
    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  IL_0000:  ldsfld     ""del C.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000e:  ldftn      ""int C.<>c.<Main>b__0_0(int)""
  IL_0014:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""del C.<>c.<>9__0_0""
  IL_001f:  call       ""void System.Console.WriteLine(object)""
  IL_0024:  ret
}
");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73068")]
        public void GotoInLambda_OutOfScope_Backward()
        {
            var code = """
                x:
                System.Action a = () =>
                {
                    using System.IDisposable d = null;
                    goto x;
                };
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (1,1): warning CS0164: This label has not been referenced
                // x:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "x").WithLocation(1, 1),
                // (5,5): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("x").WithLocation(5, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73068")]
        public void GotoInLambda_OutOfScope_Forward()
        {
            var code = """
                System.Action a = () =>
                {
                    using System.IDisposable d = null;
                    goto x;
                };
                x:;
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (4,5): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("x").WithLocation(4, 5),
                // (6,1): warning CS0164: This label has not been referenced
                // x:;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "x").WithLocation(6, 1));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73068")]
        public void GotoInLambda_NonExistent()
        {
            var code = """
                System.Action a = () =>
                {
                    using System.IDisposable d = null;
                    goto x;
                };
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (4,10): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "x").WithArguments("x").WithLocation(4, 10));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73397")]
        public void GotoInLocalFunc_OutOfScope_Backward()
        {
            var code = """
                #pragma warning disable CS8321 // local function unused
                x:
                void localFunc()
                {
                    using System.IDisposable d = null;
                    goto x;
                }
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (6,5): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("x").WithLocation(6, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73397")]
        public void GotoInLocalFunc_OutOfScope_Forward()
        {
            var code = """
                #pragma warning disable CS8321 // local function unused
                void localFunc()
                {
                    using System.IDisposable d = null;
                    goto x;
                }
                x:;
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (5,5): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("x").WithLocation(5, 5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73397")]
        public void GotoInLocalFunc_NonExistent()
        {
            var code = """
                #pragma warning disable CS8321 // local function unused
                void localFunc()
                {
                    using System.IDisposable d = null;
                    goto x;
                }
                """;
            CreateCompilation(code).VerifyEmitDiagnostics(
                // (5,10): error CS0159: No such label 'x' within the scope of the goto statement
                //     goto x;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "x").WithArguments("x").WithLocation(5, 10));
        }

        // Definition same label in different lambdas
        [WorkItem(5991, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SameLabelInDiffLambda()
        {
            var text = @"
delegate int del(int i);
class C
{
    static void Main(string[] args)
    {
        del q = x =>
        {
            goto label1;
                label1:
                    return x * x;
        };
        System.Console.WriteLine(q);

        del p = x =>
        {
            goto label1;
                label1:
                    return x * x;
        };
        System.Console.WriteLine(p);

    }
}
";
            CompileAndVerify(text).VerifyIL("C.Main", @"
{
  // Code size       73 (0x49)
  .maxstack  2
  IL_0000:  ldsfld     ""del C.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000e:  ldftn      ""int C.<>c.<Main>b__0_0(int)""
  IL_0014:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""del C.<>c.<>9__0_0""
  IL_001f:  call       ""void System.Console.WriteLine(object)""
  IL_0024:  ldsfld     ""del C.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0032:  ldftn      ""int C.<>c.<Main>b__0_1(int)""
  IL_0038:  newobj     ""del..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""del C.<>c.<>9__0_1""
  IL_0043:  call       ""void System.Console.WriteLine(object)""
  IL_0048:  ret
}
");
        }

        // Control is transferred to the target of the goto statement after finally
        [WorkItem(540720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540720")]
        [Fact]
        public void ControlTransferred()
        {
            var text = @"
class C
{
    public static void Main()
    {
        try { goto Label1; }
        catch { throw; }
        finally { Finally(); }
    Label1: Label();
    }
    private static void Finally()
    { System.Console.WriteLine(""Finally""); }
    private static void Label()
    { System.Console.WriteLine(""Label""); }
}
";
            CompileAndVerify(text, expectedOutput: @"
Finally
Label
");
        }

        // Control is transferred to the target of the goto statement in nested try
        [WorkItem(540720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540720")]
        [Fact]
        public void ControlTransferred_02()
        {
            var text = @"
class C
{
    public static void Main()
    {
        int i = 0;
        try
        {
            i = 1;
            try { goto lab1; }
            catch { throw; }
            finally { System.Console.WriteLine(""inner finally""); }
        }
        catch { i = 2; }
        finally { System.Console.WriteLine(""outer finally""); }
    lab1: System.Console.WriteLine(""label"");
    }
}
";
            CompileAndVerify(text, expectedOutput: @"
inner finally
outer finally
label
");
        }

        [Fact]
        public void ControlTransferred_03()
        {
            var text = @"
using System.Collections;
class C
{
    public static void Main()
    {
        foreach (int i in Power(2, 3))
        {
            System.Console.WriteLine(i);
        }
    }
    public static IEnumerable Power(int number, int exponent)
    {
        int counter = 0;
        int result = 1;
        try
        {
            while (counter++ < exponent)
            {
                result = result * number;
                yield return result;
            }
            goto Label1;
        }
        finally { System.Console.WriteLine(""finally""); }
    Label1: System.Console.WriteLine(""label"");
    }
}
";
            string expectedOutput = @"2
4
8
finally
label
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [WorkItem(540719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540719")]
        [Fact]
        public void LabelBetweenLocalAndInitialize()
        {
            var text = @"
class C
{
    static void M(int x)
    {
    NoInitializers:
        int w, y;
    Const1:
    Const2:
        const int z = 0;
        w = z;
        y = x + w;
        x = y;
    }
}";
            CompileAndVerify(text);
        }

        [WorkItem(540719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540719")]
        [Fact]
        public void LabelBetweenLocalAndInitialize02()
        {
            var text = @"
public class A
{
    public static int Main()
    {
        int i = 0;
        int retVal = 1;
        try
        {
        L:
            int k;
            try
            {
                i++; goto L;
            }
            finally
            {
                if (i == 10) throw new System.Exception();
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""Catch"");
            retVal = 0;
        }
        return retVal;
    }
}
";
            CompileAndVerify(text, expectedOutput: "Catch");
        }

        [WorkItem(540719, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540719")]
        [Fact]
        public void LabelBetweenLocalAndInitialize03()
        {
            var text = @"
public class A
{
    public static int Main()
    {
        int i = 0;
        int retVal = 1;
        try
        {
        L:
            { int k; }
            try
            {
                i++; goto L;
            }
            finally
            {
                if (i == 10) throw new System.Exception();
            }
        }
        catch (System.Exception)
        {
            System.Console.Write(""Catch"");
            retVal = 0;
        }
        return retVal;
    }
}
";
            CompileAndVerify(text, expectedOutput: "Catch");
        }

        [Fact]
        public void OutOfScriptBlock()
        {
            string source =
@"bool b = true;
L0: ;
{
    {
        System.Console.WriteLine(b);
        if (b) b = !b;
        else goto L1;
        goto L0;
    }
    L1: ;
}";
            string expectedOutput =
@"True
False";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Passes);
        }

        [Fact]
        public void IntoScriptBlock()
        {
            string source =
@"goto L0;
{
    L0: goto L1;
}
{
    L1: ;
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (1,6): error CS0159: No such label 'L0' within the scope of the goto statement
                // goto L0;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "L0").WithArguments("L0").WithLocation(1, 6),
                // (3,14): error CS0159: No such label 'L1' within the scope of the goto statement
                //     L0: goto L1;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "L1").WithArguments("L1").WithLocation(3, 14),
                // (3,5): warning CS0164: This label has not been referenced
                //     L0: goto L1;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L0").WithLocation(3, 5),
                // (6,5): warning CS0164: This label has not been referenced
                //     L1: ;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L1").WithLocation(6, 5));
        }

        [ConditionalFact(typeof(IsRelease), Reason = "https://github.com/dotnet/roslyn/issues/25702")]
        public void AcrossScriptDeclarations()
        {
            string source =
@"int P { get; } = G(""P"");
L:
int F = G(""F"");
int Q { get; } = G(""Q"");
static int x = 2;
static int G(string s)
{
    System.Console.WriteLine(""{0}: {1}"", x, s);
    x++;
    return x;
}
if (Q < 4) goto L;";
            string expectedOutput =
@"2: P
3: F
4: Q";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Fails);
        }

        [Fact]
        public void AcrossSubmissions()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source0 =
@"bool b = false;
L: ;
if (b)
{
    goto L;
}";
            var source1 =
@"goto L;";
            var s0 = CSharpCompilation.CreateScriptCompilation("s0.dll", SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script), references);
            s0.VerifyDiagnostics();
            var s1 = CSharpCompilation.CreateScriptCompilation("s1.dll", SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Script), references, previousScriptCompilation: s0);
            s1.VerifyDiagnostics(
                // (1,6): error CS0159: No such label 'L' within the scope of the goto statement
                // goto L;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "L").WithArguments("L").WithLocation(1, 6));
        }

        [Fact]
        public void OutOfScriptMethod()
        {
            string source =
@"static void F(bool b)
{
    if (b) goto L;
}
L:
F(true);";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (5,1): warning CS0164: This label has not been referenced
                // L:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(5, 1),
                // (3,17): error CS0159: No such label 'L' within the scope of the goto statement
                //     if (b) goto L;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "L").WithArguments("L").WithLocation(3, 17));
        }

        [Fact]
        public void IntoScriptMethod()
        {
            string source =
@"static void F()
{
L:
    return;
}
goto L;";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics(
                // (6,6): error CS0159: No such label 'L' within the scope of the goto statement
                // goto L;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "L").WithArguments("L").WithLocation(6, 6),
                // (3,1): warning CS0164: This label has not been referenced
                // L:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L").WithLocation(3, 1));
        }

        [Fact]
        public void InScriptSwitch()
        {
            string source =
@"int x = 3;
switch (x)
{
case 1:
    break;
case 2:
    System.Console.WriteLine(x);
    break;
default:
    goto case 2;
}";
            string expectedOutput =
@"3";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput, verify: Verification.Passes);
        }

        [Fact]
        public void DuplicateLabelInScript()
        {
            string source =
@"bool b = false;
L: ;
if (b)
{
    goto L;
}
else
{
    b = !b;
    if (b) goto L;
L: ;
}";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { SystemCoreRef }, parseOptions: TestOptions.Script, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,1): error CS0158: The label 'L' shadows another label by the same name in a contained scope
                // L: ;
                Diagnostic(ErrorCode.ERR_LabelShadow, "L").WithArguments("L").WithLocation(11, 1));
        }

        [Fact]
        public void DuplicateLabelInSeparateSubmissions()
        {
            var references = new[] { MscorlibRef_v4_0_30316_17626, SystemCoreRef };
            var source0 =
@"bool b = false;
L: ;
if (b)
{
    goto L;
}";
            var source1 =
@"if (!b)
{
    b = !b;
    if (b) goto L;
L: ;
}";
            var s0 = CSharpCompilation.CreateScriptCompilation("s0.dll", SyntaxFactory.ParseSyntaxTree(source0, options: TestOptions.Script), references);
            s0.VerifyDiagnostics();
            var s1 = CSharpCompilation.CreateScriptCompilation("s1.dll", SyntaxFactory.ParseSyntaxTree(source1, options: TestOptions.Script), references, previousScriptCompilation: s0);
            s1.VerifyDiagnostics();
        }

        [Fact]
        public void LoadedFile()
        {
            var sourceA =
@"goto A;
A: goto B;";
            var sourceB =
@"#load ""a.csx""
goto B;
B: goto A;";
            var resolver = TestSourceReferenceResolver.Create(KeyValuePairUtil.Create("a.csx", sourceA));
            var options = TestOptions.DebugDll.WithSourceReferenceResolver(resolver);
            var compilation = CreateCompilationWithMscorlib45(sourceB, options: options, parseOptions: TestOptions.Script);
            compilation.GetDiagnostics().Verify(
                // a.csx(2,9): error CS0159: No such label 'B' within the scope of the goto statement
                // A: goto B;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "B").WithArguments("B").WithLocation(2, 9),
                // (3,9): error CS0159: No such label 'A' within the scope of the goto statement
                // B: goto A;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "A").WithArguments("A").WithLocation(3, 9));
        }

        [Fact, WorkItem(3712, "https://github.com/dotnet/roslyn/pull/3172")]
        public void Label_GetDeclaredSymbol_Script()
        {
            string source =
@"L0: goto L1;
static void F() { }
L1: goto L0;";
            var tree = Parse(source, options: TestOptions.Script);
            var model = CreateCompilationWithMscorlib45(new[] { tree }).GetSemanticModel(tree, ignoreAccessibility: false);
            var label = (LabeledStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.LabeledStatement);
            var symbol = model.GetDeclaredSymbol(label);
            Assert.Equal("L0", symbol.Name);
        }

        [Fact, WorkItem(3712, "https://github.com/dotnet/roslyn/pull/3172")]
        public void Label_GetDeclaredSymbol_Error_Script()
        {
            string source = @"
C: \a\b\
";
            var tree = Parse(source, options: TestOptions.Script);
            var model = CreateCompilationWithMscorlib45(new[] { tree }).GetSemanticModel(tree, ignoreAccessibility: false);
            var label = (LabeledStatementSyntax)tree.FindNodeOrTokenByKind(SyntaxKind.LabeledStatement);
            var symbol = model.GetDeclaredSymbol(label);
            Assert.Equal("C", symbol.Name);
        }

        [Fact]
        public void TrailingExpression()
        {
            var source = @"
goto EOF;
EOF:";

            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script);
            compilation.GetDiagnostics().Verify(
                // (3,5): error CS1733: Expected expression
                // EOF:
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(3, 5));

            compilation = CreateSubmission(source);
            compilation.GetDiagnostics().Verify(
                // (3,5): error CS1733: Expected expression
                // EOF:
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(3, 5));

            source = @"
goto EOF;
EOF: 42";
            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script);
            compilation.GetDiagnostics().Verify(
                // (3,8): error CS1002: ; expected
                // EOF: 42
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(3, 8));

            source = @"
var obj = new object();
goto L1;
L1:
L2:
EOF: obj.ToString()";

            compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.Script);
            compilation.GetDiagnostics().Verify(
                // (6,20): error CS1002: ; expected
                // EOF: obj.ToString()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 20),
                // (5,1): warning CS0164: This label has not been referenced
                // L2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L2").WithLocation(5, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // EOF: obj.ToString()
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "EOF").WithLocation(6, 1));

            compilation = CreateSubmission(source);
            compilation.GetDiagnostics().Verify(
                // (6,20): error CS1002: ; expected
                // EOF: obj.ToString()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 20),
                // (5,1): warning CS0164: This label has not been referenced
                // L2:
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "L2").WithLocation(5, 1),
                // (6,1): warning CS0164: This label has not been referenced
                // EOF: obj.ToString()
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "EOF").WithLocation(6, 1));
        }
    }
}
