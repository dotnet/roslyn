// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class LocalFunctions : FlowTestBase
    {
        [Fact]
        [WorkItem(17829, "https://github.com/dotnet/roslyn/issues/17829")]
        public void UncalledLambdaInLocalFunction()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        void L()
        {
            Action a = () =>
            {
                int x;
                x++;
            };
        }
    }
}");
            comp.VerifyDiagnostics(
                // (12,17): error CS0165: Use of unassigned local variable 'x'
                //                 x++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(12, 17),
                // (7,14): warning CS8321: The local function 'L' is declared but never used
                //         void L()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L").WithArguments("L").WithLocation(7, 14));
        }

        [Fact]
        [WorkItem(17829, "https://github.com/dotnet/roslyn/issues/17829")]
        public void LambdaInNestedUncalledLocalFunctions()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        void L1()
        {
            void L2()
            {
                Action a = () =>
                {
                    int x;
                    x++;
                };
            }
            L2();
        }
    }
}");
            comp.VerifyDiagnostics(
                // (14,21): error CS0165: Use of unassigned local variable 'x'
                //                     x++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(14, 21),
                // (7,14): warning CS8321: The local function 'L1' is declared but never used
                //         void L1()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L1").WithArguments("L1").WithLocation(7, 14));
        }

        [Fact]
        [WorkItem(17829, "https://github.com/dotnet/roslyn/issues/17829")]
        public void CapturedInLambdaInUncalledLocalFunction()
        {
            var comp = CreateCompilation(@"
using System;
class C
{
    void M()
    {
        void L()
        {
            int x;
            Action f = () => x++;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,30): error CS0165: Use of unassigned local variable 'x'
                //             Action f = () => x++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(10, 30),
                // (7,14): warning CS8321: The local function 'L' is declared but never used
                //         void L()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L").WithArguments("L").WithLocation(7, 14));
        }

        [Fact]
        [WorkItem(17829, "https://github.com/dotnet/roslyn/issues/17829")]
        public void CapturedInNestedUncalledLocalFunctions()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        void L1()
        {
            int x;
            void L2() => x++;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): warning CS8321: The local function 'L2' is declared but never used
                //             void L2() => x++;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L2").WithArguments("L2").WithLocation(9, 18),
                // (6,14): warning CS8321: The local function 'L1' is declared but never used
                //         void L1()
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "L1").WithArguments("L1").WithLocation(6, 14));
        }

        [Fact]
        public void ConstUnassigned()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        L();
        int L()
        {
            int y = x + 1;
            const int x = 0;
            return y;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,21): error CS0841: Cannot use local variable 'x' before it is declared
                //             int y = x + 1;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(9, 21));
        }

        [Fact]
        public void ConstUnassigned2()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        L();
        int L() => x;
        const int x = 0;
    }
}");
            comp.VerifyDiagnostics(
                // (7,20): error CS0841: Cannot use local variable 'x' before it is declared
                //         int L() => x;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(7, 20));
        }

        [Fact]
        public void ConstUnassigned3()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        L();
        const int x;
        int L() => x;
    }
}");
            comp.VerifyDiagnostics(
                // (7,19): error CS0145: A const field requires a value to be provided
                //         const int x;
                Diagnostic(ErrorCode.ERR_ConstValueRequired, "x").WithLocation(7, 19));
        }

        [Fact]
        [WorkItem(14243, "https://github.com/dotnet/roslyn/issues/14243")]
        public void AssignInsideCallToLocalFunc()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M()
    {
        int x;
        int Local(int p1) => x++;

        Local(x = 0);

        int z;
        int Local2(int p1, int p2) => z++;
        Local2(z = 0, z++);
    }

    public void M2()
    {
        int x;
        int Local(int p1) => x++;
        int Local2(int p1) => Local(p1);
        int Local3(int p1) => x + Local2(p1);

        Local3(x = 0);
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(14046, "https://github.com/dotnet/roslyn/issues/14046")]
        public void UnreachableAfterThrow()
        {
            var comp = CreateCompilation(@"
class C
{
  public void M3()
  {
    int x, y;
    void Local()
    {
      throw null;
      x = 5; // unreachable code
      System.Console.WriteLine(y);
    }

    Local();
    System.Console.WriteLine(x);
    System.Console.WriteLine(y);
  }
}");
            comp.VerifyDiagnostics(
                // (10,7): warning CS0162: Unreachable code detected
                //       x = 5; // unreachable code
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(10, 7));
        }

        [Fact]
        [WorkItem(13739, "https://github.com/dotnet/roslyn/issues/13739")]
        public void UnreachableRecursion()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M()
    {
        int x, y;
        void Local()
        {
            Local();
            x = 0;
        }
        Local();
        x++;
        y++;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ReadBeforeUnreachable()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M()
    {
        int x;
        void Local()
        {
            x++;
            Local();
        }
        Local();
        x++;
    }
}");
            comp.VerifyDiagnostics(
                // (12,9): error CS0165: Use of unassigned local variable 'x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("x").WithLocation(12, 9));
        }

        [Fact]
        [WorkItem(13739, "https://github.com/dotnet/roslyn/issues/13739")]
        public void MutualRecursiveUnreachable()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void M()
    {
        int x, y, z;
        
        void L1()
        {
            L2();
            x++; // Unreachable
        } 
        void L2()
        {
            L1();
            y++; // Unreachable
        }

        L1();
        // Unreachable code, so everything should be definitely assigned
        x++;
        y++;
        z++;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(13739, "https://github.com/dotnet/roslyn/issues/13739")]
        public void AssignedInDeadBranch()
        {
            var comp = CreateCompilation(@"
using System;

class Program
{
    public static void Main(string[] args)
    {
        int u;
        M();
    https://github.com/dotnet/roslyn/issues/13739
        Console.WriteLine(u); // error: use of unassigned local variable 'u'
        return;

        void M()
        {
            goto La;
        Lb: return;
        La: u = 3;
            goto Lb;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (10,5): warning CS0164: This label has not been referenced
                //     https://github.com/dotnet/roslyn/issues/13739
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "https").WithLocation(10, 5));
        }

        [Fact]
        public void InvalidBranchOutOfLocalFunc()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void M()
    {
        label1:

        L();
        L2();
        L3();

        void L()
        {
            goto label1;
        }

        void L2()
        {
            break;
        }

        void L3()
        {
            continue;
        }
    }
}");
            comp.VerifyDiagnostics(
                    // (19,13): error CS0139: No enclosing loop out of which to break or continue
                    //             break;
                    Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(19, 13),
                    // (24,13): error CS0139: No enclosing loop out of which to break or continue
                    //             continue;
                    Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue;").WithLocation(24, 13),
                    // (14,13): error CS0159: No such label 'label1' within the scope of the goto statement
                    //             goto label1;
                    Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("label1").WithLocation(14, 13));
        }

        [Fact]
        [WorkItem(13762, "https://github.com/dotnet/roslyn/issues/13762")]
        public void AssignsInAsync()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System.Threading.Tasks;

class C
{
    public static void M2()
    {
        int a=0, x, y, z;
        L1();
        a++;
        x++;
        y++;
        z++;

        async void L1()
        {
            x = 0;
            await Task.Delay(0);
            y = 0;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (12,9): error CS0165: Use of unassigned local variable 'y'
                //         y++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(12, 9),
                // (13,9): error CS0165: Use of unassigned local variable 'z'
                //         z++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(13, 9));
        }

        [Fact]
        [WorkItem(13762, "https://github.com/dotnet/roslyn/issues/13762")]
        public void AssignsInIterator()
        {
            var comp = CreateCompilationWithMscorlib46(@"
using System.Collections.Generic;

class C
{
    public static void M2()
    {
        int a=0, w, x, y, z;

        L1();

        a++;
        w++;
        x++;
        y++;
        z++;

        IEnumerable<bool> L1()
        {
            w = 0;
            yield return false;
            x = 0;
            yield break;
            y = 0;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (24,13): warning CS0162: Unreachable code detected
                //             y = 0;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "y").WithLocation(24, 13),
                // (13,9): error CS0165: Use of unassigned local variable 'w'
                //         w++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "w").WithArguments("w").WithLocation(13, 9),
                // (14,9): error CS0165: Use of unassigned local variable 'x'
                //         x++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(14, 9),
                // (15,9): error CS0165: Use of unassigned local variable 'y'
                //         y++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(15, 9),
                // (16,9): error CS0165: Use of unassigned local variable 'z'
                //         z++;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "z").WithArguments("z").WithLocation(16, 9));
        }

        [Fact]
        public void SimpleForwardCall()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        var x = Local();
        int Local() => 2;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DefinedWhenCalled()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool Local() => x == 0;
        x = 0;
        Local();
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotDefinedWhenCalled()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool Local() => x == 0;
        Local();
    }
}");
            comp.VerifyDiagnostics(
                // (8,9): error CS0165: Use of unassigned local variable 'x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("x").WithLocation(8, 9)
                );
        }

        [Fact]
        public void ChainedDef()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool Local2() => Local1();
        bool Local1() => x == 0;
        Local2();
    }
}");
            comp.VerifyDiagnostics(
                // (9,9): error CS0165: Use of unassigned local variable 'x'
                //         Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local2()").WithArguments("x").WithLocation(9, 9)
                );
        }

        [Fact]
        public void SetInLocalFunc()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        void L1()
        {
            x = 0;
        }
        bool L2() => x == 0;
        L1();
        L2();
    }
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SetInLocalFuncMutual()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool L1()
        {
            L2();
            return x == 0;
        }
        void L2()
        {
            x = 0;
            L1();
        }
        L1();
    }
}
");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LongWriteChain()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M()
    {
        int x;
        bool L1()
        {
            L2();
            return x == 0;
        }
        bool L2()
        {
            L3();
            return x == 0;
        }
        bool L3()
        {
            L4();
            return x == 0;
        }
        void L4()
        {
            x = 0;
        }
        L1();
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ConvertBeforeDefined()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool L1() => x == 0;
        System.Func<bool> f = L1;
        x = 0;
        f();
    }
}");
            comp.VerifyDiagnostics(
                // (8,31): error CS0165: Use of unassigned local variable 'x'
                //         System.Func<bool> f = L1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "L1").WithArguments("x").WithLocation(8, 31));
        }

        [Fact]
        public void NestedCapture()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        void Local1()
        {
            int x;
            bool Local2() => x == 0;
            Local2();
        }
        Local1();
    }
}");
            comp.VerifyDiagnostics(
                // (10,13): error CS0165: Use of unassigned local variable 'x'
                //             Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local2()").WithArguments("x").WithLocation(10, 13));
        }

        [Fact]
        public void UnusedLocalFunc()
        {
            var comp = CreateCompilation(@"
class C
{
    public static void Main()
    {
        int x;
        bool Local() => x == 0;
    }
}");
            comp.VerifyDiagnostics(
                // (7,14): warning CS8321: The local function 'Local' is declared but never used
                //         bool Local() => x == 0;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(7, 14));
        }

        [Fact]
        public void UnassignedInStruct()
        {
            var comp = CreateCompilation(@"
struct S
{
    int _x;
    public S(int x)
    {
        var s = this;
        void Local()
        {
            s._x = _x;
        }
        Local();
    }
}");
            comp.VerifyDiagnostics(
                // (10,20): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //             s._x = _x;
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "_x").WithLocation(10, 20),
                // (7,17): error CS0188: The 'this' object cannot be used before all of its fields are assigned to
                //         var s = this;
                Diagnostic(ErrorCode.ERR_UseDefViolationThis, "this").WithArguments("this").WithLocation(7, 17),
                // (12,9): error CS0170: Use of possibly unassigned field '_x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "Local()").WithArguments("_x").WithLocation(12, 9),
                // (5,12): error CS0171: Field 'S._x' must be fully assigned before control is returned to the caller
                //     public S(int x)
                Diagnostic(ErrorCode.ERR_UnassignedThis, "S").WithArguments("S._x").WithLocation(5, 12));
        }

        [Fact]
        public void AssignWithStruct()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int x;
    public int y;
}

class C
{
    public void M1()
    {
        S s1;
        Local();
        S s2 = s1; // unassigned
        void Local()
        {
            s1.x = 0;
        }
        s1.y = 0;
    }

    public void M2()
    {
        S s1;
        Local();
        S s2 = s1; // success
        void Local()
        {
            s1.x = 0;
            s1.y = 0;
        }
    }

    public void M3()
    {
        S s1;
        S s2 = s1; // unassigned
        Local();
        void Local()
        {
            s1.x = 0;
            s1.y = 0;
        }
    }
    void M4()
    {
        S s1;
        Local();
        void Local()
        {
            s1.x = 0;
            s1.x += s1.y;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (14,16): error CS0165: Use of unassigned local variable 's1'
                //         S s2 = s1; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(14, 16),
                // (37,16): error CS0165: Use of unassigned local variable 's1'
                //         S s2 = s1; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(37, 16),
                // (48,9): error CS0170: Use of possibly unassigned field 'y'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "Local()").WithArguments("y").WithLocation(48, 9));
        }

        [Fact]
        public void NestedStructProperty()
        {
            var comp = CreateCompilation(@"
struct A
{
    public int x;
    public int y { get; set; }
}

struct B
{
    public A a;
    public int z;
}

class C
{
    void AssignInLocalFunc()
    {
        A a1;
        Local1(); // unassigned
        A a2 = a1;
        void Local1()
        {
            a1.x = 0;
            a1.y = 0; 
        }

        B b1;
        Local2();
        B b2 = b1;
        void Local2()
        {
            b1.a.x = 0;
            b1.a.y = 0;
            b1.z = 0;
        }
    }
    void SkipNestedField()
    {
        B b1;
        Local();
        B b2 = b1; // unassigned
        void Local()
        {
            b1.a.x = 0;
            b1.z = 0;
        }
    }
    void SkipNestedStruct()
    {
        B b1;
        Local();
        B b2 = b1; // unassigned
        void Local()
        {
            b1.z = 0;
        }
    }
    void SkipField()
    {
        B b1;
        Local();
        B b2 = b1; // unassigned
        void Local()
        {
            b1.a.x = 0;
            b1.a.y = 0;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (19,9): error CS8079: Use of possibly unassigned auto-implemented property 'y'
                //         Local1(); // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Local1()").WithArguments("y").WithLocation(19, 9),
                // (28,9): error CS8079: Use of possibly unassigned auto-implemented property 'y'
                //         Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Local2()").WithArguments("y").WithLocation(28, 9),
                // (41,16): error CS0165: Use of unassigned local variable 'b1'
                //         B b2 = b1; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b1").WithArguments("b1").WithLocation(41, 16),
                // (52,16): error CS0165: Use of unassigned local variable 'b1'
                //         B b2 = b1; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b1").WithArguments("b1").WithLocation(52, 16),
                // (61,9): error CS8079: Use of possibly unassigned auto-implemented property 'y'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolationProperty, "Local()").WithArguments("y").WithLocation(61, 9),
                // (62,16): error CS0165: Use of unassigned local variable 'b1'
                //         B b2 = b1; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "b1").WithArguments("b1").WithLocation(62, 16));
        }

        [Fact]
        public void WriteAndReadInLocalFunc()
        {
            var comp = CreateCompilation(@"
class C
{
    public void M()
    {
        int x;
        bool b;
        Local();
        void Local()
        {
            x = x + 1;
            x = 0;
        }
        b = x == 0;
    }
}");
            comp.VerifyDiagnostics(
                // (8,9): error CS0165: Use of unassigned local variable 'x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("x").WithLocation(8, 9));
        }

        [Fact]
        public void EventReadAndWrite()
        {
            var comp = CreateCompilation(@"
using System;

struct S
{
    public int x;
    public event EventHandler Event;

    public void Fire() => Event(null, EventArgs.Empty);
}

class C
{
    void PartialAssign()
    {
        S s1;
        Local1();
        S s2 = s1;
        void Local1()
        {
            s1.x = 0;
        }
    }
    void FullAssign()
    {
        S s1;
        Local1();
        S s2 = s1;
        void Local1()
        {
            s1 = new S();
            s1.x = 0;
            s1.Event += Handler1;
            s1.Fire();

            void Handler1(object sender, EventArgs args)
            {
                s1.x++;
            }
        }

        S s3;
        void Local2()
        {
            s3.x = 0;
            s3.Event += Handler2;

            void Handler2(object sender, EventArgs args)
            {
                s1.x++;
                s3.x++;
            }
        }
        S s4 = s3;
        Local2();
    }
}");
            comp.VerifyDiagnostics(
                // (18,16): error CS0165: Use of unassigned local variable 's1'
                //         S s2 = s1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s1").WithArguments("s1").WithLocation(18, 16),
                // (54,16): error CS0165: Use of unassigned local variable 's3'
                //         S s4 = s3;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s3").WithArguments("s3").WithLocation(54, 16),
                // (55,9): error CS0170: Use of possibly unassigned field 'Event'
                //         Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "Local2()").WithArguments("Event").WithLocation(55, 9));
        }

        [Fact]
        public void CaptureForeachVar()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var items = new[] { 0, 1, 2, 3};
        foreach (var i in items)
        {
            void Local()
            {
                i = 4;
            }
            Local();
        }
    }
}");
            comp.VerifyDiagnostics(
                // (11,17): error CS1656: Cannot assign to 'i' because it is a 'foreach iteration variable'
                //                 i = 4;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "foreach iteration variable").WithLocation(11, 17));
        }

        [Fact]
        public void CapturePattern()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        object o = 2;
        if (o is int x1 && Local(x1) == 0)
        {
        }

        if (o is int x2 || Local(x2) == 0)
        {
        }

        if (!(o is int x3))
        {
            void Local2()
            {
                x3++;
            }
            Local2();
        }
        
        int Local(int i) => i;
    }
}");
            comp.VerifyDiagnostics(
                // (11,34): error CS0165: Use of unassigned local variable 'x2'
                //         if (o is int x2 || Local(x2) == 0)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(11, 34),
                // (21,13): error CS0165: Use of unassigned local variable 'x3'
                //             Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local2()").WithArguments("x3").WithLocation(21, 13));
        }

        [Fact]
        public void NotAssignedControlFlow()
        {
            var comp = CreateCompilation(@"
class C
{
    void FullyAssigned()
    {
        int x;
        int y = 0;
        void Local()
        {
            if (y == 0)
                x = 0;
            else
                Local2();
        }
        void Local2()
        {
            x = 0;
        }
        Local();
        y = x;
    }
    void PartiallyAssigned()
    {
        int x;
        int y = 0;
        void Local()
        {
            if (y == 0)
                x = 0;
            else
                Local2();
        }
        void Local2()
        {
            //x = 0;
        }
        Local();
        y = x; // unassigned
    }
}");
            comp.VerifyDiagnostics(
                // (38,13): error CS0165: Use of unassigned local variable 'x'
                //         y = x; // unassigned
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(38, 13));
        }

        [Fact]
        public void UseConsts()
        {
            var comp = CreateCompilation(@"
struct S
{
    public const int z = 0;
}

class C
{
    const int x = 0;
    void M()
    {
        const int y = 0;
        Local();

        int Local()
        {
            const int a = 1;
            return a + x + y + S.z;
        }
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void NotAssignedAtAllReturns()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x;
        void L1()
        {
            if ("""".Length == 1)
            {
                x = 1;
            }
            else
            {
                return;
            }
        }
        L1();
        var z = x;
    }
}");
            comp.VerifyDiagnostics(
                // (19,17): error CS0165: Use of unassigned local variable 'x'
                //         var z = x;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(19, 17));
        }

        [Fact]
        public void NotAssignedAtThrow()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x1, x2;
        void L1()
        {
            if ("""".Length == 1)
                x1 = x2 = 0;
            else
                throw new System.Exception();
        }
        try
        {
            L1();
            var y = x1;
        }
        catch
        {
            var z = x1;
        }
        var zz = x2;
    }
}");

            comp.VerifyDiagnostics(
                // (21,21): error CS0165: Use of unassigned local variable 'x1'
                //             var z = x1;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(21, 21),
                // (23,18): error CS0165: Use of unassigned local variable 'x2'
                //         var zz = x2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(23, 18));
        }

        [Fact]
        public void DeadCode()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x;
        goto live;
        void L1() => x = 0;
        live:
        L1();
        var z = x;
    }
    void M2()
    {
        int x;
        goto live;
        void L1()
        {
            if ("""".Length == 1)
                x = 0;
            else
                return;
        }
        live:
        L1();
        var z = x;
    }
}");
            comp.VerifyDiagnostics(
                // (26,17): error CS0165: Use of unassigned local variable 'x'
                //         var z = x;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(26, 17));
        }

        [Fact]
        public void LocalFunctionFromOtherSwitch()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        int x;
        int y;
        switch("""".Length)
        {
            case 0:
                void L1()
                {
                    y = 0;
                    L2();
                }
                break;
            case 1:
                L1();
                y = x;
                break;
            case 2:
                void L2()
                {
                    x = y;
                }
                break;
         }
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(15298, "https://github.com/dotnet/roslyn/issues/15298")]
        public void UnassignedUndefinedVariable()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        {
            Goo();
            int x = 0;
            void Goo() => x++;
        }
        {
            System.Action a = Goo;
            int x = 0;
            void Goo() => x++;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (7,13): error CS0165: Use of unassigned local variable 'x'
                //             Goo();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Goo()").WithArguments("x").WithLocation(7, 13),
                // (12,31): error CS0165: Use of unassigned local variable 'x'
                //             System.Action a = Goo;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Goo").WithArguments("x").WithLocation(12, 31));
        }

        [Fact]
        [WorkItem(15322, "https://github.com/dotnet/roslyn/issues/15322")]
        public void UseBeforeDeclarationInSwitch()
        {
            var comp = CreateCompilation(@"
class Program
{
    static void Main(object[] args)
    {
        switch(args[0])
        {
            case string x:
                Goo(); 
                break;
            case int x:
                void Goo() => System.Console.WriteLine(x);
                break;
        }
    }
}");
            comp.VerifyDiagnostics(
                // (9,17): error CS0165: Use of unassigned local variable 'x'
                //                 Goo();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Goo()").WithArguments("x").WithLocation(9, 17));
        }

        [Fact]
        [WorkItem(14097, "https://github.com/dotnet/roslyn/issues/14097")]
        public void PiecewiseStructAssign()
        {
            var comp = CreateCompilation(@"
struct S { public int X, Y; }

class C
{
    public static void Main()
    {
        S s;
        s.X = 5;
        void Local()
        {
          s.Y = 10;
          System.Console.WriteLine(s);
        }
        Local();
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(14097, "https://github.com/dotnet/roslyn/issues/14097")]
        public void PiecewiseStructAssign2()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int X;
    public int Y { get; set; }
    
    public S(int x, int y)
    {
        this.X = x;
        this.Y = y;

        Local(this);
        void Local(S s)
        {
            s.X++;
            s.Y++;
        }
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(14097, "https://github.com/dotnet/roslyn/issues/14097")]
        public void PiecewiseStructAssign3()
        {
            var comp = CreateCompilation(@"
struct S { }
struct S2
{ 
    public int x;
    public S s;
}
class C
{
    public void M()
    {
        S2 s2;
        void Local1()
        {
            s2.x = 0;
            S2 s4 = s2;
        }
        Local1();
        S2 s3 = s2;
    }

    public void M2()
    {
        S2 s3;
        void Local2()
        {
            s3.s = new S();
            S2 s4 = s3;
        }
        Local2();
    }

    public void M3()
    {
        S2 s5;
        void Local3()
        {
            s5.s = new S();
        }
        Local3();
        S2 s6 = s5;
    }
}");
            comp.VerifyDiagnostics(
                // (30,9): error CS0170: Use of possibly unassigned field 'x'
                //         Local2();
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "Local2()").WithArguments("x").WithLocation(30, 9),
                // (41,17): error CS0165: Use of unassigned local variable 's5'
                //         S2 s6 = s5;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "s5").WithArguments("s5").WithLocation(41, 17));
        }

        [Fact]
        [WorkItem(14097, "https://github.com/dotnet/roslyn/issues/14097")]
        public void PiecewiseStructAssignmentInConstructor()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int _x;
    public int _y;

    public S(int x, int y)
    {
        _y = 0;
        void Local()
        {
            _x = 0;
            S s2 = this;
        }
        Local();
    }
}");
            // Note that definite assignment is still validated in this
            // compilation
            comp.VerifyDiagnostics(
                // (12,13): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //             _x = 0;
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "_x").WithLocation(12, 13),
                // (13,20): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //             S s2 = this;
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(13, 20));
        }

        [Fact]
        [WorkItem(14097, "https://github.com/dotnet/roslyn/issues/14097")]
        public void PiecewiseStructAssignmentInConstructor2()
        {
            var comp = CreateCompilation(@"
struct S
{
    public int _x;
    public int _y;

    public S(int x, int y)
    {
        _y = 0;
        void Local()
        {
            S s2 = this;
        }
        Local();
        _x = 0;
    }
}");
            comp.VerifyDiagnostics(
                // (12,20): error CS1673: Anonymous methods, lambda expressions, query expressions, and local functions inside structs cannot access instance members of 'this'. Consider copying 'this' to a local variable outside the anonymous method, lambda expression, query expression, or local function and using the local instead.
                //             S s2 = this;
                Diagnostic(ErrorCode.ERR_ThisStructNotInAnonMeth, "this").WithLocation(12, 20),
                // (14,9): error CS0170: Use of possibly unassigned field '_x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "Local()").WithArguments("_x").WithLocation(14, 9));
        }

        [Fact]
        [WorkItem(18813, "https://github.com/dotnet/roslyn/issues/18813")]
        public void LocalIEnumerableFunctionWithOutParameter1()
        {
            var comp = CreateCompilation(@"
class @c
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool output)
        {
            output = true;
            yield return ""goo"";
        }

        getGoo(1, out bool myBool);
    }
}");

            comp.VerifyDiagnostics(
                // (6,83): error CS1623: Iterators cannot have ref, in or out parameters
                //         System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool goobar)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "output"),
                // (6,56): error CS0177: The out parameter 'goobar' must be assigned to before control leaves the current method
                //         System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool goobar)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "getGoo").WithArguments("output").WithLocation(6, 56));
        }

        [Fact]
        [WorkItem(18813, "https://github.com/dotnet/roslyn/issues/18813")]
        public void LocalIEnumerableFunctionWithOutParameter2()
        {
            var comp = CreateCompilation(@"
class @c
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool output)
        {
            output = false;
            for (int i = 0; i < count; i++)
            {
                foreach (var val in getGoo(3, out var bar))
                yield return ""goo"";
            }
            yield return ""goo"";
        }

        getGoo(1, out bool myBool);
    }
}");

            comp.VerifyDiagnostics(
                // (6,83): error CS1623: Iterators cannot have ref, in or out parameters
                //         System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool goobar)
                Diagnostic(ErrorCode.ERR_BadIteratorArgType, "output"),
                // (6,56): error CS0177: The out parameter 'goobar' must be assigned to before control leaves the current method
                //         System.Collections.Generic.IEnumerable<string> getGoo(int count, out bool goobar)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "getGoo").WithArguments("output").WithLocation(6, 56));
        }

        [Fact]
        [WorkItem(41631, "https://github.com/dotnet/roslyn/issues/41631")]
        public void UseOfCapturedVariableAssignedByOutParameter()
        {
            CreateCompilation(@"
public class C
{
    void M()
    {
        string s0;
        local1(out s0); // 1
    
        void local1(out string s1)
        {
            s0.ToString();
            s1 = ""hello"";
        }
    }
}").VerifyDiagnostics(
                    // (7,9): error CS0165: Use of unassigned local variable 's0'
                    //         local1(out s0);
                    Diagnostic(ErrorCode.ERR_UseDefViolation, "local1(out s0)").WithArguments("s0").WithLocation(7, 9));
        }

        [Fact]
        public void OutParameterIsAssignedByLocalFunction()
        {
            CreateCompilation(@"
public class C
{
    void M()
    {
        string s0;
        local1(out s0);
        s0.ToString();
    
        void local1(out string s1)
        {
            s1 = ""hello"";
        }
    }
}").VerifyDiagnostics();
        }

        [Fact]
        public void UseOfCapturedVariableAssignedInArgument()
        {
            CreateCompilation(@"
public class C
{
    void M()
    {
        string s0;
        local1(s0 = ""hello"", out s0);
    
        void local1(string s1, out string s2)
        {
               s0.ToString();
               s2 = ""bye"";
        }
    }
}").VerifyDiagnostics();
        }
    }
}
