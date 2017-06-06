using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.Tuples)]
    public class TuplesTests : CSharpTestBase
    {
        private static readonly MetadataReference[] s_valueTupleRefs = new[] { SystemRuntimeFacadeRef, ValueTupleRef };

        [Fact]
        public void TupleFieldNameAliasing()
        {
            var comp = CreateStandardCompilation(@"
using System;

class C
{
    void M()
    {
        (int x, int y) t;
        t.x = 0;
        Console.Write(t.x);
        Console.Write(t.Item1);
    }
    void M2()
    {
        (int x, int y) t;
        Console.Write(t.y);
        // No error, error is reported once per field
        // and t.Item2 is alias for the same field
        Console.Write(t.Item2);
    }
    void M3()
    {
        (int x, int y) t;
        Console.Write(t.Item2);
        // No error, error is reported once per field
        // and t.y is alias for the same field
        Console.Write(t.y);
    }
}", references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            comp.VerifyDiagnostics(
                // (16,23): error CS0170: Use of possibly unassigned field 'y'
                //         Console.Write(t.y);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "t.y").WithArguments("y").WithLocation(16, 23),
                // (24,23): error CS0170: Use of possibly unassigned field 'Item2'
                //         Console.Write(t.Item2);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "t.Item2").WithArguments("Item2").WithLocation(24, 23));
        }

        [Fact]
        public void DefiniteAssignment001()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string A, string B) ss;

            ss.A = ""q"";
            ss.Item2 = ""w"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, w)
");
        }

        [Fact]
        public void DefiniteAssignment002()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string A, string B) ss;

            ss.A = ""q"";
            ss.B = ""q"";
            ss.Item1 = ""w"";
            ss.Item2 = ""w"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"(w, w)");
        }

        [Fact]
        public void DefiniteAssignment003()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string A, (string B, string C) D) ss;

            ss.A = ""q"";
            ss.D.B = ""w"";
            ss.D.C = ""e"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, (w, e))
");
        }

        [Fact]
        public void DefiniteAssignment004()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            ss.I8 = ""q"";
            ss.I9 = ""q"";
            ss.I10 = ""q"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q, q, q, q, q, q, q, q)
");
        }

        [Fact]
        public void DefiniteAssignment005()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.Item1 = ""q"";
            ss.Item2 = ""q"";
            ss.Item3 = ""q"";
            ss.Item4 = ""q"";
            ss.Item5 = ""q"";
            ss.Item6 = ""q"";
            ss.Item7 = ""q"";
            ss.Item8 = ""q"";
            ss.Item9 = ""q"";
            ss.Item10 = ""q"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q, q, q, q, q, q, q, q)
");
        }

        [Fact]
        public void DefiniteAssignment006()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.Item1 = ""q"";
            ss.I2 = ""q"";
            ss.Item3 = ""q"";
            ss.I4 = ""q"";
            ss.Item5 = ""q"";
            ss.I6 = ""q"";
            ss.Item7 = ""q"";
            ss.I8 = ""q"";
            ss.Item9 = ""q"";
            ss.I10 = ""q"";

            System.Console.WriteLine(ss);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q, q, q, q, q, q, q, q)
");
        }

        [Fact]
        public void DefiniteAssignment007()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.Item1 = ""q"";
            ss.I2 = ""q"";
            ss.Item3 = ""q"";
            ss.I4 = ""q"";
            ss.Item5 = ""q"";
            ss.I6 = ""q"";
            ss.Item7 = ""q"";
            ss.I8 = ""q"";
            ss.Item9 = ""q"";
            ss.I10 = ""q"";

            System.Console.WriteLine(ss.Rest);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q)
");
        }

        [Fact]
        public void DefiniteAssignment008()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.I8 = ""q"";
            ss.Item9 = ""q"";
            ss.I10 = ""q"";

            System.Console.WriteLine(ss.Rest);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q)
");
        }

        [Fact]
        public void DefiniteAssignment009()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            Assign(out ss.Rest);

            System.Console.WriteLine(ss);
        }

        static void Assign<T>(out T v)
        {
            v = default(T);
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"
(q, q, q, q, q, q, q, , , )
");
        }

        [Fact]
        public void DefiniteAssignment010()
        {
            var source = @"
using System;
class C
{
        static void Main(string[] args)
        {
            (string I1, 
             string I2, 
             string I3, 
             string I4, 
             string I5, 
             string I6, 
             string I7, 
             string I8, 
             string I9, 
             string I10) ss;

            Assign(out ss.Rest, (""q"", ""w"", ""e""));

            System.Console.WriteLine(ss.I9);
        }

        static void Assign<T>(out T r, T v)
        {
            r = v;
        }
    }
";

            var comp = CompileAndVerify(source,
                additionalRefs: s_valueTupleRefs,
                parseOptions: TestOptions.Regular, expectedOutput: @"w");
        }

        [Fact]
        public void DefiniteAssignment011()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        if (1.ToString() == 2.ToString())
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            ss.I8 = ""q"";

            System.Console.WriteLine(ss);
        }
        else if (1.ToString() == 3.ToString())
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            // ss.I8 = ""q"";

            System.Console.WriteLine(ss);
        }
        else 
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            // ss.I7 = ""q"";
            ss.I8 = ""q"";

            System.Console.WriteLine(ss); // should fail
        }
    }
}

namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            this.Item1 = item1;
        }
    }

    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            this.Item1 = item1;
            this.Item2 = item2;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        // public TRest Rest;     oops, Rest is missing

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
            Item5 = item5;
            Item6 = item6;
            Item7 = item7;
            // Rest = rest;   
        }
    }
}
" + TestResources.NetFX.ValueTuple.tupleattributes_cs;

            var comp = CreateStandardCompilation(source,
                parseOptions: TestOptions.Regular);

            comp.VerifyDiagnostics(
                // (70,38): error CS0165: Use of unassigned local variable 'ss'
                //             System.Console.WriteLine(ss); // should fail
                Diagnostic(ErrorCode.ERR_UseDefViolation, "ss").WithArguments("ss").WithLocation(70, 38)
            );
        }

        [Fact]
        public void DefiniteAssignment012()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        if (1.ToString() == 2.ToString())
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            ss.I8 = ""q"";

            System.Console.WriteLine(ss);
        }
        else if (1.ToString() == 3.ToString())
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            ss.I7 = ""q"";
            // ss.I8 = ""q"";

            System.Console.WriteLine(ss); // should fail1
        }
        else 
        {
            (string I1, 
                string I2, 
                string I3, 
                string I4, 
                string I5, 
                string I6, 
                string I7, 
                string I8) ss;

            ss.I1 = ""q"";
            ss.I2 = ""q"";
            ss.I3 = ""q"";
            ss.I4 = ""q"";
            ss.I5 = ""q"";
            ss.I6 = ""q"";
            // ss.I7 = ""q"";
            ss.I8 = ""q"";

            System.Console.WriteLine(ss); // should fail2
        }
    }
}

";

            var comp = CreateStandardCompilation(source,
                references: s_valueTupleRefs,
                parseOptions: TestOptions.Regular);

            comp.VerifyDiagnostics(
                // (48,38): error CS0165: Use of unassigned local variable 'ss'
                //             System.Console.WriteLine(ss); // should fail1
                Diagnostic(ErrorCode.ERR_UseDefViolation, "ss").WithArguments("ss").WithLocation(48, 38),
                // (70,38): error CS0165: Use of unassigned local variable 'ss'
                //             System.Console.WriteLine(ss); // should fail2
                Diagnostic(ErrorCode.ERR_UseDefViolation, "ss").WithArguments("ss").WithLocation(70, 38)
            );
        }

        [Fact]
        public void DefiniteAssignment013()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        (string I1, 
            string I2, 
            string I3, 
            string I4, 
            string I5, 
            string I6, 
            string I7, 
            string I8) ss;

        ss.I1 = ""q"";
        ss.I2 = ""q"";
        ss.I3 = ""q"";
        ss.I4 = ""q"";
        ss.I5 = ""q"";
        ss.I6 = ""q"";
        ss.I7 = ""q"";

        ss.Item1 = ""q"";
        ss.Item2 = ""q"";
        ss.Item3 = ""q"";
        ss.Item4 = ""q"";
        ss.Item5 = ""q"";
        ss.Item6 = ""q"";
        ss.Item7 = ""q"";

        System.Console.WriteLine(ss.Rest);
    }
}

";

            var comp = CreateStandardCompilation(source,
                references: s_valueTupleRefs,
                parseOptions: TestOptions.Regular);

            comp.VerifyDiagnostics(
                // (31,34): error CS0170: Use of possibly unassigned field 'Rest'
                //         System.Console.WriteLine(ss.Rest);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "ss.Rest").WithArguments("Rest").WithLocation(31, 34)
            );
        }

        [Fact]
        public void DefiniteAssignment014()
        {
            var source = @"
class C
{
    static void Main(string[] args)
    {
        (string I1, 
            string I2, 
            string I3, 
            string I4, 
            string I5, 
            string I6, 
            string I7, 
            string I8) ss;

        ss.I1 = ""q"";
        ss.Item2 = ""aa"";

        System.Console.WriteLine(ss.Item1);
        System.Console.WriteLine(ss.I2);

        System.Console.WriteLine(ss.I3);
    }
}

";

            var comp = CreateStandardCompilation(source,
                references: s_valueTupleRefs,
                parseOptions: TestOptions.Regular);

            comp.VerifyDiagnostics(
                // (21,34): error CS0170: Use of possibly unassigned field 'I3'
                //         System.Console.WriteLine(ss.I3);
                Diagnostic(ErrorCode.ERR_UseDefViolationField, "ss.I3").WithArguments("I3").WithLocation(21, 34)
            );
        }
    }
}
