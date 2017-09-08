using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.JumpExpressions)]
    public class JumpExpressionTests : CSharpTestBase
    {
        [Fact]
        public void JumpExpressions_Good()
        {
            var source =
@"class Program
{
    static void Test(object o, bool b)
    {
        while (true)
        {
            o = o ?? return;
            o = o ?? continue;
            o = o ?? break;
            o = b ? o : return;
            o = b ? o : continue;
            o = b ? o : break;
            o = b ? return : o;
            o = b ? continue : o;
            o = b ? break : o;
        }
    }

    static object TestReturn(object o, bool b)
    {
        o = o ?? return o;
        o = b ? return o : o;
        o = b ? o : return o;
        throw null;
    }

    static ref object TestRefReturn(object o, ref object r, bool b)
    {
        o = o ?? return ref r;
        o = b ? return ref r : o;
        o = b ? o : return ref r;
        throw null;
    }
}
";

            CreateStandardCompilation(source).VerifyDiagnostics();

            // TODO(jmp-expr): test feature flag
            // CreateStandardCompilation(source, parseOptions: TestOptions.Regular7).VerifyDiagnostics();
        }

        [Fact]
        public void JumpExpressions_Bad()
        {
            var source =
@"class Program
{
    static void Test(string s, bool b)
    {
        while (true)
        {
            s = s + return;
            if (b || return) { }
            s = s + break;
            if (b || break) { }
            s = s + continue;
            if (b || continue) { }

            var q1 = from x in return select x;
            var q2 = from x in break select x;
            var q3 = from x in continue select x;

            M(return);
            M(break);
            M(continue);

            object c1 = s ?? return ?? return;
            object c2 = b ? break : continue;

            switch(s)
            { 
                case return: break; 
                case break: break;
                case continue: break;
            }

            (int, int) w = (1, return);
        }

        foreach (var x in s ?? break) {}
        foreach (var x in s ?? continue) {}
    }

    static void Nested1() { return return; }
    static void Nested2() { return break; }
    static void Nested3() { return continue; }

    static void M() => return;
    static object M(object o) => return o;
    static object M(ref object o) => return ref o;

    static void M(string s) {}
}
";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (7,21): error CS1525: Invalid expression term 'return'
                //             s = s + return;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return").WithLocation(7, 21),
                // (8,22): error CS1525: Invalid expression term 'return'
                //             if (b || return) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return").WithLocation(8, 22),
                // (9,21): error CS1525: Invalid expression term 'break'
                //             s = s + break;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "break").WithArguments("break").WithLocation(9, 21),
                // (10,22): error CS1525: Invalid expression term 'break'
                //             if (b || break) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "break").WithArguments("break").WithLocation(10, 22),
                // (11,21): error CS1525: Invalid expression term 'continue'
                //             s = s + continue;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "continue").WithArguments("continue").WithLocation(11, 21),
                // (12,22): error CS1525: Invalid expression term 'continue'
                //             if (b || continue) { }
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "continue").WithArguments("continue").WithLocation(12, 22),
                // (22,37): error CS1525: Invalid expression term '??'
                //             object c1 = s ?? return ?? return;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "??").WithArguments("??").WithLocation(22, 37),
                // (14,32): error CS8115: A throw expression is not allowed in this context.
                //             var q1 = from x in return select x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(14, 32),
                // (15,32): error CS8115: A throw expression is not allowed in this context.
                //             var q2 = from x in break select x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "break").WithLocation(15, 32),
                // (16,32): error CS8115: A throw expression is not allowed in this context.
                //             var q3 = from x in continue select x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "continue").WithLocation(16, 32),
                // (18,15): error CS8115: A throw expression is not allowed in this context.
                //             M(return);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(18, 15),
                // (19,15): error CS8115: A throw expression is not allowed in this context.
                //             M(break);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "break").WithLocation(19, 15),
                // (20,15): error CS8115: A throw expression is not allowed in this context.
                //             M(continue);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "continue").WithLocation(20, 15),
                // (23,25): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'break' and 'continue'
                //             object c2 = b ? break : continue;
                Diagnostic(ErrorCode.ERR_InvalidQM, "b ? break : continue").WithArguments("break", "continue").WithLocation(23, 25),
                // (27,22): error CS8115: A throw expression is not allowed in this context.
                //                 case return: break; 
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(27, 22),
                // (28,22): error CS8115: A throw expression is not allowed in this context.
                //                 case break: break;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "break").WithLocation(28, 22),
                // (29,22): error CS8115: A throw expression is not allowed in this context.
                //                 case continue: break;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "continue").WithLocation(29, 22),
                // (32,13): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported, or is declared in multiple referenced assemblies
                //             (int, int) w = (1, return);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(int, int)").WithArguments("System.ValueTuple`2").WithLocation(32, 13),
                // (32,32): error CS8115: A throw expression is not allowed in this context.
                //             (int, int) w = (1, return);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(32, 32),
                // (32,28): error CS8179: Predefined type 'System.ValueTuple`2' is not defined or imported, or is declared in multiple referenced assemblies
                //             (int, int) w = (1, return);
                Diagnostic(ErrorCode.ERR_PredefinedValueTupleTypeNotFound, "(1, return)").WithArguments("System.ValueTuple`2").WithLocation(32, 28),
                // (35,32): error CS0139: No enclosing loop out of which to break or continue
                //         foreach (var x in s ?? break) {}
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break").WithLocation(35, 32),
                // (36,32): error CS0139: No enclosing loop out of which to break or continue
                //         foreach (var x in s ?? continue) {}
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue").WithLocation(36, 32),
                // (35,9): warning CS0162: Unreachable code detected
                //         foreach (var x in s ?? break) {}
                Diagnostic(ErrorCode.WRN_UnreachableCode, "foreach").WithLocation(35, 9),
                // (39,36): error CS8115: A throw expression is not allowed in this context.
                //     static void Nested1() { return return; }
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(39, 36),
                // (40,36): error CS8115: A throw expression is not allowed in this context.
                //     static void Nested2() { return break; }
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "break").WithLocation(40, 36),
                // (40,36): error CS0139: No enclosing loop out of which to break or continue
                //     static void Nested2() { return break; }
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break").WithLocation(40, 36),
                // (41,36): error CS8115: A throw expression is not allowed in this context.
                //     static void Nested3() { return continue; }
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "continue").WithLocation(41, 36),
                // (41,36): error CS0139: No enclosing loop out of which to break or continue
                //     static void Nested3() { return continue; }
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue").WithLocation(41, 36),
                // (43,24): error CS8115: A throw expression is not allowed in this context.
                //     static void M() => return;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(43, 24),
                // (43,24): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //     static void M() => return;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "return").WithLocation(43, 24),
                // (44,34): error CS8115: A throw expression is not allowed in this context.
                //     static object M(object o) => return o;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(44, 34),
                // (45,38): error CS8115: A throw expression is not allowed in this context.
                //     static object M(ref object o) => return ref o;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "return").WithLocation(45, 38),
                // (45,38): error CS8149: By-reference returns may only be used in methods that return by reference
                //     static object M(ref object o) => return ref o;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(45, 38)
                );
        }

        [Fact]
        public void JumpExpressions_Loops()
        {
            var source =
@"using static System.Console;
class Program
{
    static int?[] list = {1, null, 2, null, 3};
    static object nil = null;

    static void @foreach()
    {
        foreach(var item in list)
        {
            Write(item ?? continue);
        }

        foreach (var item in list)
        {
            Write(nil ?? break);
            Write(""unreachable"");
        }

        Write(4);
    }

    static void @while()
    {
        int i = 0;
        while (i < list.Length)
        {
            Write(list[i++] ?? continue);
        }

        while (nil == null)
        {
            Write(nil ?? break);
            Write(""unreachable"");
        }

        Write(4);
    }

    static void @do()
    {
        int i = 0;
        do
        {
            Write(list[i++] ?? continue);
        }
        while (i < list.Length);

        do
        {
            Write(nil ?? break);
            Write(""unreachable"");
        }
        while(nil == null);

        Write(4);
    }

    static void @for()
    {
        for (int i = 0; i < list.Length; i++)
        {
            Write(list[i++] ?? continue);
        }

        for (int i = 0; i < list.Length; i++)
        {
            Write(nil ?? break);
            Write(""unreachable"");
        }

        Write(4);
    }

    public static void Main()
    {
        @foreach();
        @while();
        @do();
        @for();
    }
}
";

            CompileAndVerify(source, expectedOutput: "1234123412341234");
        }
    }
}
