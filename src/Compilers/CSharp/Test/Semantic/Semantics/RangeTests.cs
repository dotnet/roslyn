// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RangeTests : CompilingTestBase
    {
        private readonly string RangeStruct = @"
using System.Collections;
using System.Collections.Generic;
namespace System
{
    public struct Range : IEnumerable<int>
    {
        public int Start { get; }
        public int End { get; }
        private Range(int start, int end)
        {
            Start = start;
            End = end;
        }
        public static Range Create(int start, int end) => new Range(start, end);
        public RangeEnumerable GetEnumerator() => new RangeEnumerable(Start, End);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        public struct RangeEnumerable : IEnumerator<int>
        {
            public int Current { get; private set; }
            public int End { get; }
            public RangeEnumerable(int start, int end)
            {
                Current = start - 1;
                End = end;
            }
            public bool MoveNext() => ++Current < End;
            int IEnumerator<int>.Current => Current;
            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
            bool IEnumerator.MoveNext() => MoveNext();
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
";

        private readonly string SlicableArray = @"
using System;
using System.Collections;
using System.Collections.Generic;
public struct SlicableArray<T> : IEnumerable<T>
{
    private readonly T[] _array;
    public SlicableArray(params T[] array) => _array = array;
    public T this[int index] => _array[index];
    public SlicableArray<T> this[Range range]
    {
        get
        {
            var length = range.End - range.Start;
            var result = new T[length];
            Array.Copy(_array, range.Start, result, 0, length);
            return new SlicableArray<T>(result);
        }
    }
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
";

        [Fact]
        public void RangeHelloWorld()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in 0..4)
        {
            System.Console.Write(x);
        }
    }
}
";

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "0123");
        }

        [Fact]
        public void Lexing()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var a = 0 .. 1;
        var b =
0
..
1
;
        var c =
// comment
 0 // comment
// comment
 .. // comment
// comment
 1 // comment
// comment
;
        var d = /* comment */ 0 /* comment */ .. /* comment */ 1 /* comment */;
        var e = /* comment */0/* comment */../* comment */1/* comment */;
        Console.Write(string.Join("";"",
            string.Join("","", a),
            string.Join("","", b),
            string.Join("","", c),
            string.Join("","", d),
            string.Join("","", e)
        ));
    }
}
";
            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "0;0;0;0;0");
        }

        [Fact]
        public void LexingBad()
        {
            var source = @"
class C
{
    static void Main()
    {
        var a = 0. .1;
        var b = 0...1;
        var c = 0..=1;
        var d = 0..<1;
    }
}
";
            CreateCSharpCompilation(source).VerifyDiagnostics(
                // (6,20): error CS1001: Identifier expected
                //         var a = 0. .1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ".1").WithLocation(6, 20),
                // (6,20): error CS1002: ; expected
                //         var a = 0. .1;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ".1").WithLocation(6, 20),
                // (7,18): error CS1056: Unexpected character '.'
                //         var b = 0...1;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(".").WithLocation(7, 18),
                // (7,19): error CS1002: ; expected
                //         var b = 0...1;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "..").WithLocation(7, 19),
                // (7,19): error CS1525: Invalid expression term '..'
                //         var b = 0...1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "..").WithArguments("..").WithLocation(7, 19),
                // (8,20): error CS1525: Invalid expression term '='
                //         var c = 0..=1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "=").WithArguments("=").WithLocation(8, 20),
                // (9,20): error CS1525: Invalid expression term '<'
                //         var d = 0..<1;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(9, 20),
                // (6,20): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         var a = 0. .1;
                Diagnostic(ErrorCode.ERR_IllegalStatement, ".1").WithLocation(6, 20)
            );
        }

        [Fact]
        public void Typed()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var a = 0..4;
        System.Range b = 0..4;
        Console.Write(string.Join("","", a) + "","" + string.Join("","", b));
    }
}
";

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "0,1,2,3,0,1,2,3");
        }

        [Fact]
        public void Slice()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        var arr = new SlicableArray<int>(2, 4, 6, 8, 10);
        Console.Write(""["" + string.Join("","", arr[2..4]) + ""]"");
        Console.Write(""["" + string.Join("","", arr[2..2]) + ""]"");
        Console.Write(""["" + string.Join("","", arr[1..3][1]) + ""]"");
    }
}
";

            CompileAndVerify(new[] { RangeStruct, SlicableArray, source }, expectedOutput: "[6,8][][6]");
        }

        [Fact]
        public void RangeTypeMissing()
        {
            var source = @"
class C
{
    static void Main()
    {
        foreach (var x in 0..4)
        {
            System.Console.Write(x);
        }
    }
}
";

            CreateCSharpCompilation(source).VerifyDiagnostics(
                // (6,27): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         foreach (var x in 0..4)
                Diagnostic(ErrorCode.ERR_RangeNotFound, "0..4").WithArguments("System.Range").WithLocation(6, 27)
            );
        }

        [Fact]
        public void LiftedOperatorDotDot()
        {
            var source = @"
using System;
class C
{
    static void Main()
    {
        int? nullX = 2;
        int? nullY = 4;
        var a = nullX..4;
        var b = 2..nullY;
        var c = nullX..nullY;
        Console.Write(string.Join("";"",
            string.Join("","", a.Value),
            string.Join("","", b.Value),
            string.Join("","", c.Value)
        ));
        Console.Write(""|"");
        nullX = null;
        nullY = null;
        a = nullX..4;
        b = 2..nullY;
        c = nullX..nullY;
        Console.Write(string.Join("","",
            a.HasValue,
            b.HasValue,
            c.HasValue
        ));
    }
}
";

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "2,3;2,3;2,3|False,False,False");
        }

        [Fact]
        public void CustomOperatorDotDot()
        {
            var source = @"
class C
{
    public static object operator..(C left, C right)
    {
        System.Console.Write(""ok"");
        return null;
    }
    static void Main()
    {
        var x = new C()..new C();
    }
}
";

            CompileAndVerify(source, expectedOutput: "ok");
        }

        [Fact]
        public void LiftedCustomOperatorDotDot()
        {
            var source = @"
using System;
struct C
{
    public static bool operator..(C left, C right)
    {
        Console.Write(""ok"");
        return true;
    }
    static void Main()
    {
        C? nullable = new C();
        var res = nullable..nullable;
        Console.Write("","");
        // Compare to null to make sure the type is bool?, not bool
        Console.Write(res == null);
    }
}
";

            CompileAndVerify(source, expectedOutput: "ok,False");
        }
    }
}
