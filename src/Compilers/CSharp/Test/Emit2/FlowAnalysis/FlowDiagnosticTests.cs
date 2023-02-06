// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class FlowDiagnosticTests : FlowTestBase
    {
        [Fact]
        public void TestBug12350()
        {
            // We suppress the "local variable is only written" warning if the
            // variable is assigned a non-constant value. 

            string program = @"
class Program
{
    static int X() { return 1; }
    static void M()
    {
        int i1 = 123;               // 0219
        int i2 = X();               // no warning
        int? i3 = 123;              // 0219
        int? i4 = null;             // 0219
        int? i5 = X();              // no warning
        int i6 = default(int);      // 0219
        int? i7 = default(int?);    // 0219
        int? i8 = new int?();       // 0219
        int i9 = new int();         // 0219
    }
}";
            CreateCompilation(program).VerifyDiagnostics(
                // (7,13): warning CS0219: The variable 'i1' is assigned but its value is never used
                //         int i1 = 123;               // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i1").WithArguments("i1"),
                // (9,14): warning CS0219: The variable 'i3' is assigned but its value is never used
                //         int? i3 = 123;              // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i3").WithArguments("i3"),
                // (10,14): warning CS0219: The variable 'i4' is assigned but its value is never used
                //         int? i4 = null;             // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i4").WithArguments("i4"),
                // (12,13): warning CS0219: The variable 'i6' is assigned but its value is never used
                //         int i6 = default(int);      // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i6").WithArguments("i6"),
                // (13,14): warning CS0219: The variable 'i7' is assigned but its value is never used
                //         int? i7 = default(int?);    // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i7").WithArguments("i7"),
                // (14,14): warning CS0219: The variable 'i8' is assigned but its value is never used
                //         int? i8 = new int?();       // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i8").WithArguments("i8"),
                // (15,13): warning CS0219: The variable 'i9' is assigned but its value is never used
                //         int i9 = new int();         // 0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i9").WithArguments("i9"));
        }

        [Fact]
        public void Test1()
        {
            string program = @"
namespace ConsoleApplication1
{
    class Program
    {
        public static void F(int z)
        {
            int x;
            if (z == 2)
            {
                int y = x; x = y; // use of unassigned local variable 'x'
            }
            else
            {
                int y = x; x = y; // diagnostic suppressed here
            }
        }
    }
}";
            CreateCompilation(program).VerifyDiagnostics(
                // (11,25): error CS0165: Use of unassigned local variable 'x'
                //                 int y = x; x = y; // use of unassigned local variable 'x'
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x")
                );
        }

        [Fact]
        public void Test2()
        {
            //x is "assigned when true" after "false"
            //Therefore x is "assigned" before "z == 1" (5.3.3.24)
            //Therefore x is "assigned" after "z == 1" (5.3.3.20)
            //Therefore x is "assigned when true" after "(false && z == 1)" (5.3.3.24)
            //Since the condition of the ?: expression is the constant true, the state of x after the ?: expression is the same as the state of x after the consequence (5.3.3.28)
            //Since the state of x after the consequence is "assigned when true", the state of x after the ?: expression is "assigned when true" (5.3.3.28)
            //Since the state of x after the if's condition is "assigned when true", x is assigned in the then block (5.3.3.5)
            //Therefore, there should be no error.
            string program = @"
namespace ConsoleApplication1
{
    class Program
    {
        void F(int z)
        {
            int x;
            if (true ? (false && z == 1) : true)
                x = x + 1; // Dev10 complains x not assigned.
        }
    }
}";
            var comp = CreateCompilation(program);
            var errs = this.FlowDiagnostics(comp);
            Assert.Equal(0, errs.Count());
        }

        [Fact]
        public void Test3()
        {
            string program = @"
class Program
{
    int F(int z)
    {
    }
}";
            var comp = CreateCompilation(program);
            var errs = this.FlowDiagnostics(comp);
            Assert.Equal(1, errs.Count());
        }

        [Fact]
        public void Test4()
        {
            // v is definitely assigned at the beginning of any unreachable statement.
            string program = @"
                class Program
                {
                    void F()
                    {
                        if (false)
                        {
                            int x;      // warning: unreachable code
                            x = x + 1;  // no error: x assigned when unreachable
                        }
                    }
                }";
            var comp = CreateCompilation(program);
            int[] count = new int[4];
            foreach (var e in this.FlowDiagnostics(comp))
                count[(int)e.Severity]++;

            Assert.Equal(0, count[(int)DiagnosticSeverity.Error]);
            Assert.Equal(1, count[(int)DiagnosticSeverity.Warning]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Info]);
        }

        [Fact]
        public void Test5()
        {
            // v is definitely assigned at the beginning of any unreachable statement.
            string program = @"
                class A
                {
                    static void F()
                    {
                        goto L2;
                        goto L1; // unreachable code detected
                        int x;
                    L1: ;       // Roslyn: extrs warning CS0162 -unreachable code
                        x = x + 1; // no definite assignment problem in unreachable code
                    L2: ;
                    }
                }
";
            var comp = CreateCompilation(program);
            int[] count = new int[4];
            foreach (var e in this.FlowDiagnostics(comp))
                count[(int)e.Severity]++;

            Assert.Equal(0, count[(int)DiagnosticSeverity.Error]);
            Assert.Equal(2, count[(int)DiagnosticSeverity.Warning]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Info]);
        }

        [WorkItem(537918, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537918")]
        [Fact]
        public void AssertForInvalidBreak()
        {
            // v is definitely assigned at the beginning of any unreachable statement.
            string program = @"
public class Test
{
    public static int Main()
    {
        int ret = 1;
        break; // Assert here

        return (ret);
    }
}
";
            var comp = CreateCompilation(program);

            comp.GetMethodBodyDiagnostics().Verify(
                // (7,9): error CS0139: No enclosing loop out of which to break or continue
                //         break; // Assert here
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;"));
        }

        [WorkItem(538064, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538064")]
        [Fact]
        public void IfFalse()
        {
            string program = @"
using System;
class Program
{
    static void Main()
    {
        if (false)
        {
        }
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(538067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538067")]
        [Fact]
        public void WhileConstEqualsConst()
        {
            string program = @"
using System;
class Program
{
    bool goo()
    {
        const bool b = true;
        while (b == b)
        {
            return b;
        }
    }
    static void Main()
    {
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(538175, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538175")]
        [Fact]
        public void BreakWithoutTarget()
        {
            string program = @"public class Test
{
    public static void Main()
    {
        if (true)
            break;
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [Fact]
        public void OutCausesAssignment()
        {
            string program = @"class Program
{
    void F(out int x)
    {
        G(out x);
    }
    void G(out int x)
    {
        x = 1;
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [Fact]
        public void OutNotAssigned01()
        {
            string program = @"class Program
{
    bool b;
    void F(out int x)
    {
        if (b) return;
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(2, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(539374, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539374")]
        [Fact]
        public void OutAssignedAfterCall01()
        {
            string program = @"class Program
{
    void F(out int x, int y)
    {
        x = y;
    }
    void G()
    {
        int x;
        F(out x, x);
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(1, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(538067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538067")]
        [Fact]
        public void WhileConstEqualsConst2()
        {
            string program = @"
using System;
class Program
{
    bool goo()
    {
        const bool b = true;
        while (b == b)
        {
            return b;
        }
        return b; // Should detect this line as unreachable code.
    }
    static void Main()
    {
    }
}";
            var comp = CreateCompilation(program);

            int[] count = new int[4];
            foreach (var e in this.FlowDiagnostics(comp))
                count[(int)e.Severity]++;

            Assert.Equal(0, count[(int)DiagnosticSeverity.Error]);
            Assert.Equal(1, count[(int)DiagnosticSeverity.Warning]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Info]);
        }

        [WorkItem(538072, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538072")]
        [Fact]
        public void UnusedLocal()
        {
            string program = @"
using System;
class Program
{
    int x; int y; int z;
    static void Main()
    {
        int a;
        const bool b = true;
    }
    void goo()
    {
        y = 2;
        Console.WriteLine(z);
    }
}";
            var comp = CreateCompilation(program);

            int[] count = new int[4];
            Dictionary<int, int> warnings = new Dictionary<int, int>();
            foreach (var e in this.FlowDiagnostics(comp))
            {
                count[(int)e.Severity]++;
                if (!warnings.ContainsKey(e.Code)) warnings[e.Code] = 0;
                warnings[e.Code] += 1;
            }

            Assert.Equal(0, count[(int)DiagnosticSeverity.Error]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Info]);

            // See bug 3562 - field level flow analysis warnings CS0169, CS0414 and CS0649 are out of scope for M2.
            // TODO: Fix this test once CS0169, CS0414 and CS0649 are implemented.
            // Assert.Equal(5, count[(int)DiagnosticSeverity.Warning]);
            Assert.Equal(2, count[(int)DiagnosticSeverity.Warning]);

            Assert.Equal(1, warnings[168]);
            Assert.Equal(1, warnings[219]);

            // See bug 3562 - field level flow analysis warnings CS0169, CS0414 and CS0649 are out of scope for M2.
            // TODO: Fix this test once CS0169, CS0414 and CS0649 are implemented.
            // Assert.Equal(1, warnings[169]);
            // Assert.Equal(1, warnings[414]);
            // Assert.Equal(1, warnings[649]);
        }

        [WorkItem(538384, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538384")]
        [Fact]
        public void UnusedLocalConstants()
        {
            string program = @"
using System;
class Program
{
    static void Main()
    {
            const string CONST1 = ""hello""; // Should not report CS0219
            Console.WriteLine(CONST1 != ""hello"");
 
            int i = 1;
            const long CONST2 = 1;
            const uint CONST3 = 1; // Should not report CS0219
            while (CONST2 < CONST3 - i)
            {
                if (CONST3 < CONST3 - i) continue;
            }
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(538385, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538385")]
        [Fact]
        public void UnusedLocalReferenceTypedVariables()
        {
            string program = @"
using System;
class Program
{
    static void Main()
    {
            object o = 1; // Should not report CS0219

            Test c = new Test(); // Should not report CS0219
            c = new Program();

            string s = string.Empty; // Should not report CS0219
            s = null;
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [WorkItem(538386, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538386")]
        [Fact]
        public void UnusedLocalValueTypedVariables()
        {
            string program = @"
using System;
class Program
{
    static void Main()
    {
    }

    public void Repro2(params int[] x)
    {
        int i = x[0]; // Should not report CS0219
 
        byte b1 = 1;
        byte b11 = b1; // Should not report CS0219
    }
}";
            var comp = CreateCompilation(program);
            Assert.Equal(0, this.FlowDiagnostics(comp).Count());
        }

        [Fact]
        public void RefParameter01()
        {
            string program = @"
class Program
{
    public static void Main(string[] args)
    {
        int i;
        F(ref i); // use of unassigned local variable 'i'
    }
    static void F(ref int i) { }
}";
            var comp = CreateCompilation(program);
            Assert.NotEmpty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact]
        public void OutParameter01()
        {
            string program = @"
class Program
{
    public static void Main(string[] args)
    {
        int i;
        F(out i);
        int j = i;
    }
    static void F(out int i) { i = 1; }
}";
            var comp = CreateCompilation(program);
            Assert.Empty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact]
        public void Goto01()
        {
            string program = @"
using System;
class Program
{
    public void M(bool b)
    {
        if (b) goto label;
        int i;
        i = 3;
        i = i + 1;
    label:
        int j = i; // i not definitely assigned
    }
}";
            var comp = CreateCompilation(program);
            Assert.NotEmpty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact]
        public void LambdaParameters()
        {
            string program = @"
using System;
class Program
{
    delegate void Func(ref int i, int r);
    static void Main(string[] args)
    {
        Func fnc = (ref int arg, int arg2) => { arg = arg; };
    }
}";
            var comp = CreateCompilation(program);
            Assert.Empty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact]
        public void LambdaMightNotBeInvoked()
        {
            string program = @"
class Program
{
    delegate void Func();
    static void Main(string[] args)
    {
        int i;
        Func query = () =>
        {
            i = 12;
        };
        int j = i;
    }
}";
            var comp = CreateCompilation(program);
            Assert.NotEmpty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact]
        public void LambdaMustAssignOutParameters()
        {
            string program = @"
class Program
{
    delegate void Func(out int x);
    static void Main(string[] args)
    {
        Func query = (out int x) =>
        {
        };
    }
}";
            var comp = CreateCompilation(program);
            Assert.NotEmpty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [Fact, WorkItem(528052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528052")]
        public void InnerVariablesAreNotDefinitelyAssignedInBeginningOfLambdaBody()
        {
            string program = @"
using System;

class Program
{
    static void Main()
    {
        return;
        Action f = () => { int y = y; };
    }
}";

            CreateCompilation(program).VerifyDiagnostics(
    // (9,9): warning CS0162: Unreachable code detected
    //         Action f = () => { int y = y; };
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Action"),
    // (9,36): error CS0165: Use of unassigned local variable 'y'
    //         Action f = () => { int y = y; };
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y")
                );
        }

        [WorkItem(540139, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540139")]
        [Fact]
        public void DelegateCreationReceiverIsRead()
        {
            string program = @"
using System;
 
class Program
{
    static void Main()
    {
        Action a;
        Func<string> b = new Func<string>(a.ToString);
    }
}
";
            var comp = CreateCompilation(program);
            Assert.NotEmpty(this.FlowDiagnostics(comp).Where(e => e.Severity >= DiagnosticSeverity.Error));
        }

        [WorkItem(540405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540405")]
        [Fact]
        public void ErrorInFieldInitializerLambda()
        {
            string program = @"
using System;
 
class Program
{
    static Func<string> x = () => { string s; return s; };
 
    static void Main()
    {
    }
}

";
            CreateCompilation(program).VerifyDiagnostics(
                // (6,54): error CS0165: Use of unassigned local variable 's'
                //     static Func<string> x = () => { string s; return s; };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s")
                );
        }

        [WorkItem(541389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541389")]
        [Fact]
        public void IterationWithEmptyBody()
        {
            string program = @"
public class A
{
    public static void Main(string[] args)
    {
        for (int i = 0; i < 10; i++) ;
        foreach (var v in args);

        int len = args.Length;
        while (len-- > 0);
    }
}";
            CreateCompilation(program).VerifyDiagnostics();
        }

        [WorkItem(541389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541389")]
        [Fact]
        public void SelectionWithEmptyBody()
        {
            string program = @"
public class A
{
    public static void Main(string[] args)
    {
        int len = args.Length;
        if (len++ < 9) ; else ;
    }
}";
            CreateCompilation(program).VerifyDiagnostics(
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"),
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"));
        }

        [WorkItem(542146, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542146")]
        [Fact]
        public void FieldlikeEvent()
        {
            string program = @"public delegate void D();
public struct S
{
    public event D Ev;
    public S(D d)
    {
        Ev = null;
        Ev += d;
    }
}";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,20): warning CS0414: The field 'S.Ev' is assigned but its value is never used
                //     public event D Ev;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "Ev").WithArguments("S.Ev"));
        }

        [WorkItem(542187, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542187")]
        [Fact]
        public void GotoFromTry()
        {
            string program =
@"class Test
{
    static void F(int x) { }
    static void Main()
    {
        int a;
        try
        {
            a = 1;
            goto L1;
        }
        finally
        {
        }
    L1:
        F(a);
    }
}";
            CreateCompilation(program).VerifyDiagnostics();
        }

        [WorkItem(542154, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542154")]
        [Fact]
        public void UnreachableThrow()
        {
            string program =
@"public class C
{
    static void Main()
    {
        return;
        throw Goo();
    }
    static System.Exception Goo()
    {
        System.Console.WriteLine(""Hello"");
        return null;
    }
}
";
            CreateCompilation(program).VerifyDiagnostics();
        }

        [WorkItem(542585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542585")]
        [Fact]
        public void Bug9870()
        {
            string program =
@"
struct S<T>
{
    T x;

    static void Goo()
    {
        x.x = 1;
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // (8,9): error CS0120: An object reference is required for the non-static field, method, or property 'S<T>.x'
                //         x.x = 1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("S<T>.x")
                );
        }

        [WorkItem(542597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542597")]
        [Fact]
        public void LambdaEntryPointIsReachable()
        {
            string program =
@"class Program
{
    static void Main(string[] args)
    {
        int i;
        return;
        System.Action a = () =>
        {
            int j = i + j;
        };
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // unreachable statement
                // (7,23): warning CS0162: Unreachable code detected
                //         System.Action a = () =>
                Diagnostic(ErrorCode.WRN_UnreachableCode, "System"),
                // (9,25): error CS0165: Use of unassigned local variable 'j'
                //             int j = i + j;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j")
                );
        }

        [WorkItem(542597, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542597")]
        [Fact]
        public void LambdaInUnimplementedPartial()
        {
            string program =
@"using System;

partial class C
{
    static partial void Goo(Action a);

    static void Main()
    {
        Goo(() => { int x, y = x; });
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // (9,32): error CS0165: Use of unassigned local variable 'x'
                //         Goo(() => { int x, y = x; });
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x")
                );
        }

        [WorkItem(541887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541887")]
        [Fact]
        public void CascadedDiagnostics01()
        {
            string program =
@"
class Program
{
    static void Main(string[] args)
    {
        var s = goo<,int>(123);
    }
    public static int goo<T>(int i)
    {
        return 1;
    }
}";
            var comp = CreateCompilation(program);
            var parseErrors = comp.SyntaxTrees[0].GetDiagnostics();
            var errors = comp.GetDiagnostics();
            Assert.Equal(parseErrors.Count(), errors.Count());
        }

        [Fact]
        public void UnassignedInInitializer()
        {
            string program =
@"class C
{
    System.Action a = () => { int i; int j = i; };
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // (3,46): error CS0165: Use of unassigned local variable 'i'
                //     System.Action a = () => { int i; int j = i; };
                Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i")
                );
        }

        [WorkItem(543343, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543343")]
        [Fact]
        public void ConstInSwitch()
        {
            string program =
@"class Program
{
    static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 0:
                const int N = 3;
                break;
            case 1:
                int M = N;
                break;
        }
    }
}";
            var comp = CreateCompilation(program);
            comp.VerifyDiagnostics(
                // (11,21): warning CS0219: The variable 'M' is assigned but its value is never used
                //                 int M = N;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "M").WithArguments("M")
                );
        }

        #region "Struct"

        [Fact]
        public void CycleWithInitialization()
        {
            string program = @"
public struct A
{
    A a = new A(); // CS8036
    public static void Main()
    {
        A a = new A();
    }
}
";
            CreateCompilation(program, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (4,7): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     A a = new A(); // CS8036
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "a").WithArguments("struct field initializers", "10.0").WithLocation(4, 7),
                // (2,15): error CS8983: A 'struct' with field initializers must include an explicitly declared constructor.
                // public struct A
                Diagnostic(ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor, "A").WithLocation(2, 15),
                // (4,7): error CS0523: Struct member 'A.a' of type 'A' causes a cycle in the struct layout
                //     A a = new A(); // CS8036
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "a").WithArguments("A.a", "A").WithLocation(4, 7),
                // (7,11): warning CS0219: The variable 'a' is assigned but its value is never used
                //         A a = new A();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(7, 11),
                // (4,7): warning CS0169: The field 'A.a' is never used
                //     A a = new A(); // CS8036
                Diagnostic(ErrorCode.WRN_UnreferencedField, "a").WithArguments("A.a").WithLocation(4, 7));
        }

        [WorkItem(542356, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542356")]
        [Fact]
        public void StaticMemberExplosion()
        {
            string program = @"
struct A<T>
{
    static A<A<T>> x;
}

struct B<T>
{
    static A<B<T>> x;
}

struct C<T>
{
    static D<T> x;
}
struct D<T>
{
    static C<D<T>> x;
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (14,17): error CS0523: Struct member 'C<T>.x' of type 'D<T>' causes a cycle in the struct layout
                //     static D<T> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("C<T>.x", "D<T>").WithLocation(14, 17),
                // (18,20): error CS0523: Struct member 'D<T>.x' of type 'C<D<T>>' causes a cycle in the struct layout
                //     static C<D<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("D<T>.x", "C<D<T>>").WithLocation(18, 20),
                // (4,20): error CS0523: Struct member 'A<T>.x' of type 'A<A<T>>' causes a cycle in the struct layout
                //     static A<A<T>> x;
                Diagnostic(ErrorCode.ERR_StructLayoutCycle, "x").WithArguments("A<T>.x", "A<A<T>>").WithLocation(4, 20),
                // (9,20): warning CS0169: The field 'B<T>.x' is never used
                //     static A<B<T>> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("B<T>.x").WithLocation(9, 20),
                // (4,20): warning CS0169: The field 'A<T>.x' is never used
                //     static A<A<T>> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("A<T>.x").WithLocation(4, 20),
                // (18,20): warning CS0169: The field 'D<T>.x' is never used
                //     static C<D<T>> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("D<T>.x").WithLocation(18, 20),
                // (14,17): warning CS0169: The field 'C<T>.x' is never used
                //     static D<T> x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C<T>.x").WithLocation(14, 17)
                );
        }

        [Fact]
        public void StaticSequential()
        {
            string program = @"
partial struct S
{
    public static int x;
}

partial struct S
{
    public static int y;
}";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (4,23): warning CS0649: Field 'S.x' is never assigned to, and will always have its default value 0
                //     public static int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("S.x", "0"),
                // (9,23): warning CS0649: Field 'S.y' is never assigned to, and will always have its default value 0
                //     public static int y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "y").WithArguments("S.y", "0")
                );
        }

        [WorkItem(542567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542567")]
        [Fact]
        public void ImplicitFieldSequential()
        {
            string program =
@"partial struct S1
{
    public int x;
}

partial struct S1
{
    public int y { get; set; }
}

partial struct S2
{
    public int x;
}

delegate void D();
partial struct S2
{
    public event D y;
}";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (1,16): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'S1'. To specify an ordering, all instance fields must be in the same declaration.
                // partial struct S1
                Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "S1").WithArguments("S1"),
                // (11,16): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'S2'. To specify an ordering, all instance fields must be in the same declaration.
                // partial struct S2
                Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "S2").WithArguments("S2"),
                // (3,16): warning CS0649: Field 'S1.x' is never assigned to, and will always have its default value 0
                //     public int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("S1.x", "0"),
                // (13,16): warning CS0649: Field 'S2.x' is never assigned to, and will always have its default value 0
                //     public int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("S2.x", "0"),
                // (19,20): warning CS0067: The event 'S2.y' is never used
                //     public event D y;
                Diagnostic(ErrorCode.WRN_UnreferencedEvent, "y").WithArguments("S2.y")
                );
        }

        [Fact]
        public void StaticInitializer()
        {
            string program = @"
public struct A
{
    static System.Func<int> d = () => { int j; return j * 9000; };

    public static void Main()
    {
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j")
                );
        }

        [Fact]
        public void AllPiecesAssigned()
        {
            string program = @"
struct S
{
    public int x, y;
}
class Program
{
    public static void Main(string[] args)
    {
        S s;
        s.x = args.Length;
        s.y = args.Length;
        S t = s;
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [Fact]
        public void OnePieceMissing()
        {
            string program = @"
struct S
{
    public int x, y;
}
class Program
{
    public static void Main(string[] args)
    {
        S s;
        s.x = args.Length;
        S t = s;
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (12,15): error CS0165: Use of unassigned local variable 's'
                //         S t = s;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s").WithArguments("s"),
                // (4,19): warning CS0649: Field 'S.y' is never assigned to, and will always have its default value 0
                //     public int x, y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "y").WithArguments("S.y", "0")
                );
        }

        [Fact]
        public void OnePieceOnOnePath()
        {
            string program = @"
struct S
{
    public int x, y;
}
class Program
{
    public void F(S s)
    {
        S s2;
        if (s.x == 3)
            s2 = s;
        else
            s2.x = s.x;
        int x = s2.x;
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (4,19): warning CS0649: Field 'S.y' is never assigned to, and will always have its default value 0
                //     public int x, y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "y").WithArguments("S.y", "0")
                );
        }

        [Fact]
        public void DefaultConstructor()
        {
            string program = @"
struct S
{
    public int x, y;
}
class Program
{
    public static void Main(string[] args)
    {
        S s = new S();
        s.x = s.y = s.x;
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [Fact]
        public void FieldAssignedInConstructor()
        {
            string program = @"
struct S
{
    int x, y;
    S(int x, int y) { this.x = x; this.y = y; }
}";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [Fact]
        public void FieldUnassignedInConstructor()
        {
            string program = @"
struct S
{
    int x, y;
    S(int x) { this.x = x; }
}";
            CreateCompilation(program, parseOptions: TestOptions.Regular10)
                .VerifyDiagnostics(
                // (5,5): error CS0171: Field 'S.y' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     S(int x) { this.x = x; }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.y", "11.0").WithLocation(5, 5),
                // (4,12): warning CS0169: The field 'S.y' is never used
                //     int x, y;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "y").WithArguments("S.y").WithLocation(4, 12)
                );

            var verifier = CompileAndVerify(program, parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics(
                // (4,12): warning CS0169: The field 'S.y' is never used
                //     int x, y;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "y").WithArguments("S.y").WithLocation(4, 12));
            verifier.VerifyIL("S..ctor", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int S.y""
  IL_0007:  ldarg.0
  IL_0008:  ldarg.1
  IL_0009:  stfld      ""int S.x""
  IL_000e:  ret
}
");
        }

        [WorkItem(543429, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543429")]
        [Fact]
        public void ConstructorCannotComplete()
        {
            string program = @"using System;
public struct S
{
    int value;
    public S(int value)
    {
        throw new NotImplementedException();
    }
}";
            CreateCompilation(program).VerifyDiagnostics(
                // (4,9): warning CS0169: The field 'S.value' is never used
                //     int value;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "value").WithArguments("S.value")
                );
        }

        [Fact]
        public void AutoPropInitialization1()
        {
            string program = @"
struct Program
{
    public int X { get; private set; }
    public Program(int x)
    {
    }
    public static void Main(string[] args)
    {
    }
}";
            CreateCompilation(program, parseOptions: TestOptions.Regular10)
                .VerifyDiagnostics(
                // (5,12): error CS0843: Auto-implemented property 'Program.X' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
                //     public Program(int x)
                Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.X", "11.0").WithLocation(5, 12));

            var verifier = CompileAndVerify(program, parseOptions: TestOptions.Regular11);
            verifier.VerifyDiagnostics();
            verifier.VerifyIL("Program..ctor", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stfld      ""int Program.<X>k__BackingField""
  IL_0007:  ret
}");
        }

        [Fact]
        public void AutoPropInitialization2()
        {
            var text = @"struct S
{
    public int P { get; set; } = 1;
    internal static long Q { get; } = 10;
    public decimal R { get; } = 300;

    public S(int i) {}
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (3,16): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public int P { get; set; } = 1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "P").WithArguments("struct field initializers", "10.0").WithLocation(3, 16),
                // (5,20): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public decimal R { get; } = 300;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "R").WithArguments("struct field initializers", "10.0").WithLocation(5, 20));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AutoPropInitialization3()
        {
            var text = @"struct S
{
    public int P { get; private set; }
    internal static long Q { get; } = 10;
    public decimal R { get; } = 300;

    public S(int p)
    {
        P = p;
    }
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,20): error CS8773: Feature 'struct field initializers' is not available in C# 9.0. Please use language version 10.0 or greater.
                //     public decimal R { get; } = 300;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "R").WithArguments("struct field initializers", "10.0").WithLocation(5, 20));

            comp = CreateCompilation(text);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AutoPropInitialization4()
        {
            var text = @"
struct Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; }
    S1 x2 { get; }


    public Program(int dummy)
    {
        x.i = 1;
        System.Console.WriteLine(x2.ii);
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
    // (16,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
    //         x.i = 1;
    Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(16, 9),
    // (16,9): error CS9015: Use of possibly unassigned field 'i'. Consider updating to language version '11.0' to auto-default the field.
    //         x.i = 1;
    Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "x.i").WithArguments("i", "11.0").WithLocation(16, 9),
    // (17,34): error CS9014: Use of possibly unassigned auto-implemented property 'x2'. Consider updating to language version '11.0' to auto-default the property.
    //         System.Console.WriteLine(x2.ii);
    Diagnostic(ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion, "x2").WithArguments("x2", "11.0").WithLocation(17, 34),
    // (14,12): error CS0843: Auto-implemented property 'Program.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
    //     public Program(int dummy)
    Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.x", "11.0").WithLocation(14, 12),
    // (14,12): error CS0843: Auto-implemented property 'Program.x2' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
    //     public Program(int dummy)
    Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.x2", "11.0").WithLocation(14, 12));

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (16,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
                //         x.i = 1;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(16, 9));
        }

        [Fact]
        public void AutoPropInitialization5()
        {
            var text = @"
struct Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; set;}
    S1 x2 { get; set;}


    public Program(int dummy)
    {
        x.i = 1;
        System.Console.WriteLine(x2.ii);
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
    // (16,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
    //         x.i = 1;
    Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(16, 9),
    // (16,9): error CS9015: Use of possibly unassigned field 'i'. Consider updating to language version '11.0' to auto-default the field.
    //         x.i = 1;
    Diagnostic(ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion, "x.i").WithArguments("i", "11.0").WithLocation(16, 9),
    // (17,34): error CS9014: Use of possibly unassigned auto-implemented property 'x2'. Consider updating to language version '11.0' to auto-default the property.
    //         System.Console.WriteLine(x2.ii);
    Diagnostic(ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion, "x2").WithArguments("x2", "11.0").WithLocation(17, 34),
    // (14,12): error CS0843: Auto-implemented property 'Program.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
    //     public Program(int dummy)
    Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.x", "11.0").WithLocation(14, 12),
    // (14,12): error CS0843: Auto-implemented property 'Program.x2' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the property.
    //     public Program(int dummy)
    Diagnostic(ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion, "Program").WithArguments("Program.x2", "11.0").WithLocation(14, 12));

            comp = CreateCompilation(text, options: TestOptions.DebugDll.WithSpecificDiagnosticOptions(ReportStructInitializationWarnings), parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (14,12): warning CS9020: Control is returned to caller before auto-implemented property 'Program.x' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "Program").WithArguments("Program.x").WithLocation(14, 12),
                // (14,12): warning CS9020: Control is returned to caller before auto-implemented property 'Program.x2' is explicitly assigned, causing a preceding implicit assignment of 'default'.
                //     public Program(int dummy)
                Diagnostic(ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion, "Program").WithArguments("Program.x2").WithLocation(14, 12),
                // (16,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
                //         x.i = 1;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(16, 9),
                // (16,9): warning CS9018: Field 'i' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         x.i = 1;
                Diagnostic(ErrorCode.WRN_UseDefViolationFieldSupportedVersion, "x.i").WithArguments("i").WithLocation(16, 9),
                // (17,34): warning CS9017: Auto-implemented property 'x2' is read before being explicitly assigned, causing a preceding implicit assignment of 'default'.
                //         System.Console.WriteLine(x2.ii);
                Diagnostic(ErrorCode.WRN_UseDefViolationPropertySupportedVersion, "x2").WithArguments("x2").WithLocation(17, 34));

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (16,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
                //         x.i = 1;
                Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(16, 9));
        }

        [Fact]
        public void AutoPropInitialization6()
        {
            var text = @"
struct Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; set;}
    S1 x2 { get;}


    public Program(int dummy)
    {
        x = new S1();
        x.i += 1;

        x2 = new S1();
        x2.i += 1;
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (17,9): error CS1612: Cannot modify the return value of 'Program.x' because it is not a variable
    //         x.i += 1;
    Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x").WithArguments("Program.x").WithLocation(17, 9),
    // (20,9): error CS1612: Cannot modify the return value of 'Program.x2' because it is not a variable
    //         x2.i += 1;
    Diagnostic(ErrorCode.ERR_ReturnNotLValue, "x2").WithArguments("Program.x2").WithLocation(20, 9)
                );
        }

        [Fact]
        public void AutoPropInitialization7()
        {
            var text = @"
struct Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; set;}
    S1 x2 { get;}


    public Program(int dummy)
    {
        this = default(Program);

        System.Action a = () =>
        {
            this.x = new S1();
        };

        System.Action a2 = () =>
        {
            this.x2 = new S1();
        };
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (20,13): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
    //             this.x = new S1();
    Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(20, 13),
    // (25,13): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
    //             this.x2 = new S1();
    Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(25, 13),
    // (6,20): warning CS0649: Field 'Program.S1.i' is never assigned to, and will always have its default value 0
    //         public int i;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("Program.S1.i", "0").WithLocation(6, 20)
                );
        }

        [Fact]
        public void AutoPropInitialization7c()
        {
            var text = @"
class Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; set;}
    S1 x2 { get;}


    public Program()
    {
        System.Action a = () =>
        {
            this.x = new S1();
        };

        System.Action a2 = () =>
        {
            this.x2 = new S1();
        };
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (23,13): error CS0200: Property or indexer 'Program.x2' cannot be assigned to -- it is read only
    //             this.x2 = new S1();
    Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "this.x2").WithArguments("Program.x2").WithLocation(23, 13),
    // (6,20): warning CS0649: Field 'Program.S1.i' is never assigned to, and will always have its default value 0
    //         public int i;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("Program.S1.i", "0").WithLocation(6, 20)
                );
        }

        [Fact]
        public void AutoPropInitialization8()
        {
            var text = @"
struct Program
{
    struct S1
    {
        public int i;
        public int ii { get; }
    }

    S1 x { get; set;}
    S1 x2 { get;}

    public Program(int arg)
        : this()
    {
        x2 = x;
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (6,20): warning CS0649: Field 'Program.S1.i' is never assigned to, and will always have its default value 0
    //         public int i;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "i").WithArguments("Program.S1.i", "0").WithLocation(6, 20)
                );
        }

        [Fact]
        public void AutoPropInitialization9()
        {
            var text = @"
struct Program
{
    struct S1
    {
    }

    S1 x { get; set;}
    S1 x2 { get;}

    public Program(int arg)
    {
        x2 = x;
    }

    public void Goo()
    {
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text);
            // no errors since S1 is empty
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void AutoPropInitialization10()
        {
            var text = @"
struct Program
{
    public struct S1
    {
        public int x;
    }

    S1 x1 { get; set;}
    S1 x2 { get;}
    S1 x3;

    public Program(int arg)
    {
        Goo(out x1);
        Goo(ref x1);
        Goo(out x2);
        Goo(ref x2);
        Goo(out x3);
        Goo(ref x3);
    }

    public static void Goo(out S1 s)
    {
        s = default(S1);
    }

    public static void Goo1(ref S1 s)
    {
        s = default(S1);
    }

    static void Main(string[] args)
    {

    }
}";

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (15,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(out x1);
                Diagnostic(ErrorCode.ERR_RefProperty, "x1").WithLocation(15, 17),
                // (16,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(ref x1);
                Diagnostic(ErrorCode.ERR_RefProperty, "x1").WithLocation(16, 17),
                // (17,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(out x2);
                Diagnostic(ErrorCode.ERR_RefProperty, "x2").WithLocation(17, 17),
                // (18,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(ref x2);
                Diagnostic(ErrorCode.ERR_RefProperty, "x2").WithLocation(18, 17),
                // (20,17): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         Goo(ref x3);
                Diagnostic(ErrorCode.ERR_BadArgRef, "x3").WithArguments("1", "out").WithLocation(20, 17),
                // (15,17): error CS9014: Use of possibly unassigned auto-implemented property 'x1'. Consider updating to language version '11.0' to auto-default the property.
                //         Goo(out x1);
                Diagnostic(ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion, "x1").WithArguments("x1", "11.0").WithLocation(15, 17),
                // (16,9): error CS0188: The 'this' object cannot be used before all of its fields have been assigned. Consider updating to language version '11.0' to auto-default the unassigned fields.
                //         Goo(ref x1);
                Diagnostic(ErrorCode.ERR_UseDefViolationThisUnsupportedVersion, "Goo").WithArguments("11.0").WithLocation(16, 9),
                // (17,17): error CS9014: Use of possibly unassigned auto-implemented property 'x2'. Consider updating to language version '11.0' to auto-default the property.
                //         Goo(out x2);
                Diagnostic(ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion, "x2").WithArguments("x2", "11.0").WithLocation(17, 17),
                // (6,20): warning CS0649: Field 'Program.S1.x' is never assigned to, and will always have its default value 0
                //         public int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("Program.S1.x", "0").WithLocation(6, 20)
                );

            comp = CreateCompilation(text, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (15,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(out x1);
                Diagnostic(ErrorCode.ERR_RefProperty, "x1").WithLocation(15, 17),
                // (16,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(ref x1);
                Diagnostic(ErrorCode.ERR_RefProperty, "x1").WithLocation(16, 17),
                // (17,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(out x2);
                Diagnostic(ErrorCode.ERR_RefProperty, "x2").WithLocation(17, 17),
                // (18,17): error CS0206: A property or indexer may not be passed as an out or ref parameter
                //         Goo(ref x2);
                Diagnostic(ErrorCode.ERR_RefProperty, "x2").WithLocation(18, 17),
                // (20,17): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         Goo(ref x3);
                Diagnostic(ErrorCode.ERR_BadArgRef, "x3").WithArguments("1", "out").WithLocation(20, 17),
                // (6,20): warning CS0649: Field 'Program.S1.x' is never assigned to, and will always have its default value 0
                //         public int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("Program.S1.x", "0").WithLocation(6, 20)
                );
        }

        [Fact]
        public void EmptyStructAlwaysAssigned()
        {
            string program = @"
struct S
{
    static S M()
    {
        S s;
        return s;
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [Fact]
        public void DeeplyEmptyStructAlwaysAssigned()
        {
            string program = @"
struct S
{
    static S M()
    {
        S s;
        return s;
    }
}

struct T
{
    S s1, s2, s3;
    static T M()
    {
        T t;
        return t;
    }
}";
            CreateCompilation(program)
                .VerifyDiagnostics(
                // (13,15): warning CS0169: The field 'T.s3' is never used
                //     S s1, s2, s3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s3").WithArguments("T.s3").WithLocation(13, 15),
                // (13,11): warning CS0169: The field 'T.s2' is never used
                //     S s1, s2, s3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s2").WithArguments("T.s2").WithLocation(13, 11),
                // (13,7): warning CS0169: The field 'T.s1' is never used
                //     S s1, s2, s3;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "s1").WithArguments("T.s1").WithLocation(13, 7)
                );
        }

        [WorkItem(543466, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543466")]
        [Fact]
        public void UnreferencedFieldWarningsMissingInEmit()
        {
            var comp = CreateCompilation(@"
public class Class1
{
    int field1;
}");
            var bindingDiags = comp.GetDiagnostics().ToArray();
            Assert.Equal(1, bindingDiags.Length);
            Assert.Equal(ErrorCode.WRN_UnreferencedField, (ErrorCode)bindingDiags[0].Code);

            var emitDiags = comp.Emit(new System.IO.MemoryStream()).Diagnostics.ToArray();
            Assert.Equal(bindingDiags.Length, emitDiags.Length);
            Assert.Equal(bindingDiags[0], emitDiags[0]);
        }

        [Fact]
        public void DefiniteAssignGenericStruct()
        {
            string program = @"
using System;
struct C<T>
{
    public int num;
    public int Goo1()
    {
        return this.num;
    }
}
class Test
{
    static void Main(string[] args)
    {
        C<object> c;
        c.num = 1;
        bool verify = c.Goo1() == 1;
        Console.WriteLine(verify);
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [WorkItem(540896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540896")]
        [WorkItem(541268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541268")]
        [Fact]
        public void ChainToStructDefaultConstructor()
        {
            string program = @"
using System;
 
namespace Roslyn.Compilers.CSharp
{
    class DecimalRewriter
    {
        private DecimalRewriter() { }
 
        private struct DecimalParts
        {
            public DecimalParts(decimal value)
                : this()
            {
                int[] bits = Decimal.GetBits(value);
                Low = bits[0];
            }
 
            public int Low { get; private set; }
        }
    }
}
";
            CreateCompilation(program)
                .VerifyDiagnostics(
                );
        }

        [WorkItem(541298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541298")]
        [WorkItem(541298, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541298")]
        [Fact]
        public void SetStaticPropertyOnStruct()
        {
            string source = @"
struct S
{
    public static int p { get; internal set; }
}

class C
{
    public static void Main()
    {
        S.p = 10;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void UsingPart()
        {
            string program = @"
struct S
{
    public int x;
}

class Program
{
    public static void Main(string[] args)
    {
        S s = new S();
        s.x = 12;
    }
}";
            CreateCompilation(program).VerifyDiagnostics();
        }

        [Fact]
        public void ReferencingCycledStructures()
        {
            string program = @"
public struct A
{
    public static void Main()
    {
        S1 s1 = new S1();
        S2 s2 = new S2();
        s2.fld = new S3();
        s2.fld.fld.fld.fld = new S2();
    }
}
";
            var c = CreateCompilation(program, new[] { TestReferences.SymbolsTests.CycledStructs });

            c.VerifyDiagnostics(
                // (6,12): warning CS0219: The variable 's1' is assigned but its value is never used
                //         S1 s1 = new S1();
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s1").WithArguments("s1"));
        }

        [Fact]
        public void BigStruct()
        {
            string source = @"
struct S<T>
{
    T a, b, c, d, e, f, g, h;
    S(T t)
    {
        a = b = c = d = e = f = g = h = t;
    }
    static void M()
    {
        S<S<S<S<S<S<S<S<int>>>>>>>> x;
        x.a.a.a.a.a.a.a.a = 12;
        x.a.a.a.a.a.a.a.b = x.a.a.a.a.a.a.a.a;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(542901, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542901")]
        public void DataFlowForStructFieldAssignment()
        {
            string program = @"struct S
{
    public float X;
    public float Y;
    public float Z;
 
    void M()
    {
        if (3 < 3.4)
        {
            S s;
            if (s.X < 3)
            {
                s = GetS();
                s.Z = 10f;
            }
            else
            {
            }
        }
        else
        {
        }
    }
 
    private static S GetS()
    {
        return new S();
    }
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (12,17): error CS0170: Use of possibly unassigned field 'X'
                //             if (s.X < 3)
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "s.X").WithArguments("X"),
                // (3,18): warning CS0649: Field 'S.X' is never assigned to, and will always have its default value 0
                //     public float X;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "X").WithArguments("S.X", "0"),
                // (4,18): warning CS0649: Field 'S.Y' is never assigned to, and will always have its default value 0
                //     public float Y;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Y").WithArguments("S.Y", "0")
                );
        }

        [Fact]
        [WorkItem(2470, "https://github.com/dotnet/roslyn/issues/2470")]
        public void NoFieldNeverAssignedWarning()
        {
            string program = @"
using System.Threading.Tasks;

internal struct TaskEvent<T>
{
    private TaskCompletionSource<T> _tcs;

    public Task<T> Task
    {
        get
        {
            if (_tcs == null)
                _tcs = new TaskCompletionSource<T>();
            return _tcs.Task;
        }
    }

    public void Invoke(T result)
    {
        if (_tcs != null)
        {
            TaskCompletionSource<T> localTcs = _tcs;
            _tcs = null;
            localTcs.SetResult(result);
        }
    }
}

public class OperationExecutor
{
    private TaskEvent<float?> _nextValueEvent; // Field is never assigned warning

    // Start some async operation
    public Task<bool> StartOperation()
    {
        return null;
    }

    // Get progress or data during async operation
    public Task<float?> WaitNextValue()
    {
        return _nextValueEvent.Task;
    }

    // Called externally
    internal void OnNextValue(float? value)
    {
        _nextValueEvent.Invoke(value);
    }
}
";
            CreateCompilationWithMscorlib45(program).VerifyEmitDiagnostics();
        }

        #endregion

        [Fact, WorkItem(545347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545347")]
        public void FieldInAbstractClass_UnusedField()
        {
            var text = @"
abstract class AbstractType
{
    public int Kind;
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (4,16): warning CS0649: Field 'AbstractType.Kind' is never assigned to, and will always have its default value 0
                //     public int Kind;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Kind").WithArguments("AbstractType.Kind", "0").WithLocation(4, 16));
        }

        [Fact, WorkItem(545347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545347")]
        public void FieldInAbstractClass_FieldUsedInChildType()
        {
            var text = @"
abstract class AbstractType
{
    public int Kind;
}
class ChildType : AbstractType
{
    public ChildType()
    {
        this.Kind = 1;
    }
}
";

            CreateCompilation(text).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(545642, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545642")]
        public void InitializerAndConstructorWithOutParameter()
        {
            string program =
@"class Program
{
    private int field = Goo();
    static int Goo() { return 12; }
    public Program(out int x)
    {
        x = 13;
    }
}";
            CreateCompilation(program).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(545875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545875")]
        public void TestSuppressUnreferencedVarAssgOnIntPtr()
        {
            var source = @"
using System;

public class Test
{
    public static void Main()
    {
        IntPtr i1 = (IntPtr)0;
        IntPtr i2 = (IntPtr)10L;
        UIntPtr ui1 = (UIntPtr)0;
        UIntPtr ui2 = (UIntPtr)10L;

        IntPtr z = IntPtr.Zero;
        int ip1 = (int)z;
        long lp1 = (long)z;
        UIntPtr uz = UIntPtr.Zero;
        int ip2 = (int)uz;
        long lp2 = (long)uz;
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(546183, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546183")]
        public void TestUnassignedStructFieldsInPInvokePassByRefCase()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;
namespace ManagedDebuggingAssistants
{
    internal class DebugMonitor
    {
        internal DebugMonitor()
        {
            SECURITY_ATTRIBUTES attributes = new SECURITY_ATTRIBUTES();
            SECURITY_DESCRIPTOR descriptor = new SECURITY_DESCRIPTOR();
 
            IntPtr pDescriptor = IntPtr.Zero;
            IntPtr pAttributes = IntPtr.Zero;
            attributes.nLength = Marshal.SizeOf(attributes);
            attributes.bInheritHandle = true;
            attributes.lpSecurityDescriptor = pDescriptor;
 
            if (!InitializeSecurityDescriptor(ref descriptor, 1 /*SECURITY_DESCRIPTOR_REVISION*/))
                throw new ApplicationException(""InitializeSecurityDescriptor failed: "" + Marshal.GetLastWin32Error());
 
            if (!SetSecurityDescriptorDacl(ref descriptor, true, IntPtr.Zero, false))
                throw new ApplicationException(""SetSecurityDescriptorDacl failed: "" + Marshal.GetLastWin32Error());
 
            Marshal.StructureToPtr(descriptor, pDescriptor, false);
            Marshal.StructureToPtr(attributes, pAttributes, false);
        }
        
        #region Interop definitions
        private struct SECURITY_DESCRIPTOR
        {
            internal byte Revision;
            internal byte Sbz1;
            internal short Control;
            internal IntPtr Owner;
            internal IntPtr Group;
            internal IntPtr Sacl;
            internal IntPtr Dacl;
        }
 
        private struct SECURITY_ATTRIBUTES
        {
            internal int nLength;
            internal IntPtr lpSecurityDescriptor;
            // disable csharp compiler warning #0414: field assigned unused value
#pragma warning disable 0414
            internal bool bInheritHandle;
#pragma warning restore 0414
        }
 
        [DllImport(""advapi32.dll"", SetLastError = true)]
        private static extern bool InitializeSecurityDescriptor([In] ref SECURITY_DESCRIPTOR pSecurityDescriptor, int dwRevision);
 
        [DllImport(""advapi32.dll"", SetLastError = true)]
        private static extern bool SetSecurityDescriptorDacl([In] ref SECURITY_DESCRIPTOR pSecurityDescriptor, bool bDaclPresent, IntPtr pDacl, bool bDaclDefaulted);
        #endregion
    }
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [WorkItem(546673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546673")]
        [Fact]
        public void TestBreakInsideNonLocalScopeBinder()
        {
            var source = @"
public class C
{
    public static void Main()
    {
        while (true)
        {
            unchecked
            {
                break;
            }
        }

        switch(0)
        {
            case 0:
                unchecked
                {
                    break;
                }
        }

        while (true)
        {
            unsafe
            {
                break;
            } 
        }

        switch(0)
        {
            case 0:
                unsafe
                {
                    break;
                }
        }

        bool flag = false;
        while (!flag)
        {
            flag = true;
            unchecked
            {
                continue;
            }
        }

        flag = false;
        while (!flag)
        {
            flag = true;
            unsafe
            {
                continue;
            } 
        }
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "");
        }

        [WorkItem(611904, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/611904")]
        [Fact]
        public void LabelAtTopLevelInsideLambda()
        {
            var source = @"
class Program
{
    delegate T SomeDelegate<T>(out bool f);

    static void Main(string[] args)
    {
        Test((out bool f) =>
        {
            if (1.ToString() != null)
                goto l2;

            f = true;

        l1:
            if (1.ToString() != null)
                return 123;                 // <==== ERROR EXPECTED HERE

            f = true;

            if (1.ToString() != null)
                return 456;

        l2:
            goto l1;

        });
    }

    static void Test<T>(SomeDelegate<T> f)
    {
    }
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (17,17): error CS0177: The out parameter 'f' must be assigned to before control leaves the current method
                //                 return 123;                 // <==== ERROR EXPECTED HERE
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 123;").WithArguments("f")
                );
        }

        [WorkItem(633927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633927")]
        [Fact]
        public void Xyzzy()
        {
            var source =
@"class C
{
    struct S
    {
        int x;
        S(dynamic y)
        {
            Goo(y, null);
        }
    }
    static void Goo(int y)
    {
    }
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (8,13): error CS1501: No overload for method 'Goo' takes 2 arguments
                //             Goo(y, null);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Goo").WithArguments("Goo", "2").WithLocation(8, 13),
                // (6,9): error CS0171: Field 'C.S.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //         S(dynamic y)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("C.S.x", "11.0").WithLocation(6, 9),
                // (5,13): warning CS0169: The field 'C.S.x' is never used
                //         int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C.S.x").WithLocation(5, 13)
                );

            comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (8,13): error CS1501: No overload for method 'Goo' takes 2 arguments
                //             Goo(y, null);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Goo").WithArguments("Goo", "2").WithLocation(8, 13),
                // (5,13): warning CS0169: The field 'C.S.x' is never used
                //         int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("C.S.x").WithLocation(5, 13)
                );
        }

        [WorkItem(667368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667368")]
        [Fact]
        public void RegressionTest667368()
        {
            var source =
@"using System.Collections.Generic;

namespace ConsoleApplication1
{
    internal class Class1
    {
        Dictionary<string, int> _dict = new Dictionary<string, int>();

        public Class1()
        {
        }

        public int? GetCode(dynamic value)
        {
            int val;
            if (value != null && _dict.TryGetValue(value, out val))
                return val;
            return null;
        }
    }
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (17,24): error CS0165: Use of unassigned local variable 'val'
                //                 return val;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "val").WithArguments("val")
                );
        }

        [WorkItem(690921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/690921")]
        [Fact]
        public void RegressionTest690921()
        {
            var source =
@"using System.Collections.Generic;
namespace ConsoleApplication1
{
    internal class Class1
    {
        Dictionary<string, int> _dict = new Dictionary<string, int>();
        public Class1()
        {
        }

        public static string GetOutBoxItemId(string itemName, string uniqueIdKey, string listUrl, Dictionary<string, dynamic> myList = null, bool useDefaultCredentials = false)
        {
            string uniqueId = null;
            dynamic myItemName;
            if (myList != null && myList.TryGetValue(""DisplayName"", out myItemName) && myItemName == itemName)
            {
            }
            return uniqueId;
        }

        public static void Main() { }
    }
}
";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [WorkItem(715338, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715338")]
        [Fact]
        public void RegressionTest715338()
        {
            var source =
@"using System;
using System.Collections.Generic;
 
static class Program
{
    static void Add(this IList<int> source, string key, int value) { }
    static void View(Action<string, int> adder) { }
    static readonly IList<int> myInts = null;
    static void Main()
    {
        View(myInts.Add);
    }
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [WorkItem(808567, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808567")]
        [Fact]
        public void RegressionTest808567()
        {
            var source =
@"class Base
{
    public Base(out int x, System.Func<int> u)
    {
        x = 0;
    }
}
class Derived2 : Base
{
    Derived2(out int p1)
        : base(out p1, ()=>p1)
    {
    }
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (11,28): error CS1628: Cannot use ref or out parameter 'p1' inside an anonymous method, lambda expression, or query expression
                //         : base(out p1, ()=>p1)
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "p1").WithArguments("p1"),
                // (11,20): error CS0269: Use of unassigned out parameter 'p1'
                //         : base(out p1, ()=>p1)
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "p1").WithArguments("p1")
                );
        }

        [WorkItem(949324, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949324")]
        [Fact]
        public void RegressionTest949324()
        {
            var source =
@"struct Derived
{
    Derived(int x) { }
    Derived(long x) : this(p2) // error CS0188: The 'this' object cannot be used before all of its fields are assigned to
    {
        this = new Derived();
    }
    private int x;
}";
            CSharpCompilation comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (3,5): error CS0171: Field 'Derived.x' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     Derived(int x) { }
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "Derived").WithArguments("Derived.x", "11.0").WithLocation(3, 5),
                // (4,28): error CS0103: The name 'p2' does not exist in the current context
                //     Derived(long x) : this(p2) // error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(4, 28),
                // (8,17): warning CS0169: The field 'Derived.x' is never used
                //     private int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("Derived.x").WithLocation(8, 17)
                );

            comp = CreateCompilationWithMscorlib40AndSystemCore(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (4,28): error CS0103: The name 'p2' does not exist in the current context
                //     Derived(long x) : this(p2) // error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                Diagnostic(ErrorCode.ERR_NameNotInContext, "p2").WithArguments("p2").WithLocation(4, 28),
                // (8,17): warning CS0169: The field 'Derived.x' is never used
                //     private int x;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "x").WithArguments("Derived.x").WithLocation(8, 17)
                );
        }

        [WorkItem(612, "https://github.com/dotnet/roslyn/issues/612")]
        [Fact]
        public void CascadedUnreachableCode()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        string k;
        switch (1)
        {
        case 1:
        }
        string s = k;
    }
}";
            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,9): error CS8070: Control cannot fall out of switch from final case label ('case 1:')
                //         case 1:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 1:").WithArguments("case 1:").WithLocation(8, 9),
                // (10,20): error CS0165: Use of unassigned local variable 'k'
                //         string s = k;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "k").WithArguments("k").WithLocation(10, 20)
                );
        }

        [WorkItem(9581, "https://github.com/dotnet/roslyn/issues/9581")]
        [Fact]
        public void UsingSelfAssignment()
        {
            var source =
@"class Program
{
    static void Main()
    {
        using (System.IDisposable x = x)
        {
        }
    }
}";
            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,39): error CS0165: Use of unassigned local variable 'x'
                //         using (System.IDisposable x = x)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(5, 39)
                );
        }

        [Fact]
        public void UsingAssignment()
        {
            var source =
@"class Program
{
    static void Main()
    {
        using (System.IDisposable x = null)
        {
            System.Console.WriteLine(x.ToString());
        }
        using (System.IDisposable x = null, y = x)
        {
            System.Console.WriteLine(y.ToString());
        }
    }
}";
            CSharpCompilation comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                );
        }

        [Fact]
        public void RangeDefiniteAssignmentOrder()
        {
            CreateCompilationWithIndexAndRange(@"
class C
{
    void M()
    {
        int x;
        var r = (x=0)..(x+1);
        int y;
        r = (y+1)..(y=0);
    }
}").VerifyDiagnostics(
                // (9,14): error CS0165: Use of unassigned local variable 'y'
                //         r = (y+1)..(y=0);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(9, 14));
        }

        [Fact]
        public void FieldAssignedInLambdaOnly()
        {
            var source =
@"using System;
struct S
{
    private object F;
    private object G;
    public S(object x, object y)
    {
        Action a = () => { F = x; };
        G = y;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (6,12): error CS0171: Field 'S.F' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S(object x, object y)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.F", "11.0").WithLocation(6, 12),
                // (8,28): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //         Action a = () => { F = x; };
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "F").WithLocation(8, 28));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (8,28): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //         Action a = () => { F = x; };
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "F").WithLocation(8, 28));
        }

        [Fact]
        public void FieldAssignedInLocalFunctionOnly()
        {
            var source =
@"struct S
{
    private object F;
    private object G;
    public S(object x, object y)
    {
        void f() { F = x; }
        G = y;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(
                // (5,12): error CS0171: Field 'S.F' must be fully assigned before control is returned to the caller. Consider updating to language version '11.0' to auto-default the field.
                //     public S(object x, object y)
                Diagnostic(ErrorCode.ERR_UnassignedThisUnsupportedVersion, "S").WithArguments("S.F", "11.0").WithLocation(5, 12),
                // (7,14): warning CS8321: The local function 'f' is declared but never used
                //         void f() { F = x; }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(7, 14),
                // (7,20): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //         void f() { F = x; }
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "F").WithLocation(7, 20));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular11);
            comp.VerifyDiagnostics(
                // (7,14): warning CS8321: The local function 'f' is declared but never used
                //         void f() { F = x; }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "f").WithArguments("f").WithLocation(7, 14),
                // (7,20): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //         void f() { F = x; }
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "F").WithLocation(7, 20));
        }

        [Fact]
        [WorkItem(1243877, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1243877")]
        public void WorkItem1243877()
        {
            string program = @"
static class C
{
    static void Main()
    {
        Test(out var x, x);
    }
    
    static void Test(out Empty x, object y) => throw null;
}


struct Empty
{
}
";
            CreateCompilation(program).VerifyDiagnostics(
                // (6,25): error CS8196: Reference to an implicitly-typed out variable 'x' is not permitted in the same argument list.
                //         Test(out var x, x);
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedOutVariableUsedInTheSameArgumentList, "x").WithArguments("x").WithLocation(6, 25)
                );
        }
    }
}
