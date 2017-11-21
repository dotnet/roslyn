// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RangeTests : CompilingTestBase
    {
        private static readonly string RangeStruct = @"
using System.Collections;
using System.Collections.Generic;
namespace System
{
    public struct Range : IEnumerable<int>
    {
        public readonly int Start;
        public readonly int Last;
        private Range(int start, int last)
        {
            Start = start;
            Last = last;
        }
        public static Range Create(int start, int last) => new Range(start, last);
        public RangeEnumerable GetEnumerator() => new RangeEnumerable(Start, Last);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        public struct RangeEnumerable : IEnumerator<int>
        {
            private int _current;
            private readonly int _last;
            public RangeEnumerable(int start, int last)
            {
                _current = start - 1;
                _last = last;
            }
            public int Current => _current;
            public bool MoveNext() => ++_current <= _last;
            int IEnumerator<int>.Current => Current;
            object IEnumerator.Current => Current;
            void IDisposable.Dispose() { }
            bool IEnumerator.MoveNext() => MoveNext();
            void IEnumerator.Reset() => throw new NotSupportedException();
        }
    }
}
";

        private static readonly string LongRangeStruct = RangeStruct.Replace("int", "long").Replace("Range", "LongRange");

        private static readonly string SlicableArray = @"
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
            var length = range.Last - range.Start + 1;
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

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "01234");
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
            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "0,1;0,1;0,1;0,1;0,1");
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
        Console.Write(string.Join("";"",
            string.Join("","", a),
            string.Join("","", b)
        ));
    }
}
";

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "0,1,2,3,4;0,1,2,3,4");
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
        Console.Write(string.Join("";"",
            string.Join("","", arr[2..4]),
            string.Join("","", arr[2..1]),
            string.Join("","", arr[1..3][1])
        ));
    }
}
";

            CompileAndVerify(new[] { RangeStruct, SlicableArray, source }, expectedOutput: "6,8,10;;6");
        }


        [Fact]
        public void RangeTypeMissing()
        {
            var source = @"
class C
{
    static void Main()
    {
        var a = false..true;
        var b = ((sbyte)2)..((sbyte)4);
        var c = ((byte)2)..((byte)4);
        var d = ((short)2)..((short)4);
        var e = ((ushort)2)..((ushort)4);
        var f = ((int)2)..((int)4);
        var g = ((uint)2)..((uint)4);
        var h = ((long)2)..((long)4);
        var i = ((ulong)2)..((ulong)4);
        var j = ((float)2)..((float)4);
        var k = ((double)2)..((double)4);
    }
}
";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (6,17): error CS0019: Operator '..' cannot be applied to operands of type 'bool' and 'bool'
                //         var a = false..true;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "false..true").WithArguments("..", "bool", "bool").WithLocation(6, 17),
                // (7,17): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         var b = ((sbyte)2)..((sbyte)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((sbyte)2)..((sbyte)4)").WithArguments("System.Range").WithLocation(7, 17),
                // (8,17): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         var c = ((byte)2)..((byte)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((byte)2)..((byte)4)").WithArguments("System.Range").WithLocation(8, 17),
                // (9,17): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         var d = ((short)2)..((short)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((short)2)..((short)4)").WithArguments("System.Range").WithLocation(9, 17),
                // (10,17): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         var e = ((ushort)2)..((ushort)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((ushort)2)..((ushort)4)").WithArguments("System.Range").WithLocation(10, 17),
                // (11,17): error CS8380: Cannot create a range value because the compiler required type 'System.Range' cannot be found. Are you missing a reference?
                //         var f = ((int)2)..((int)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((int)2)..((int)4)").WithArguments("System.Range").WithLocation(11, 17),
                // (12,17): error CS8380: Cannot create a range value because the compiler required type 'System.LongRange' cannot be found. Are you missing a reference?
                //         var g = ((uint)2)..((uint)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((uint)2)..((uint)4)").WithArguments("System.LongRange").WithLocation(12, 17),
                // (13,17): error CS8380: Cannot create a range value because the compiler required type 'System.LongRange' cannot be found. Are you missing a reference?
                //         var h = ((long)2)..((long)4);
                Diagnostic(ErrorCode.ERR_RangeNotFound, "((long)2)..((long)4)").WithArguments("System.LongRange").WithLocation(13, 17),
                // (14,17): error CS0019: Operator '..' cannot be applied to operands of type 'ulong' and 'ulong'
                //         var i = ((ulong)2)..((ulong)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((ulong)2)..((ulong)4)").WithArguments("..", "ulong", "ulong").WithLocation(14, 17),
                // (15,17): error CS0019: Operator '..' cannot be applied to operands of type 'float' and 'float'
                //         var j = ((float)2)..((float)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((float)2)..((float)4)").WithArguments("..", "float", "float").WithLocation(15, 17),
                // (16,17): error CS0019: Operator '..' cannot be applied to operands of type 'double' and 'double'
                //         var k = ((double)2)..((double)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((double)2)..((double)4)").WithArguments("..", "double", "double").WithLocation(16, 17)
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

            CompileAndVerify(new[] { RangeStruct, source }, expectedOutput: "2,3,4;2,3,4;2,3,4|False,False,False");
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

        [Fact]
        public void LongRange()
        {
            var source = @"
using System;
struct C
{
    static void Main()
    {
        var a = 10_000_000_000L..10_000_000_001L;
        var b = 10_000_000_000..10_000_000_001;
        var c = 10..10_000_000_002;
        var d = 10..12L;
        var e = 2..((long?)4L);
        Console.Write(string.Join("";"",
            string.Join("","", a),
            string.Join("","", b),
            string.Join("","", c.Start, c.Last),
            string.Join("","", d),
            string.Join("","", e.Value)
        ));
    }
}
";

            var big = 10_000_000_000;
            CompileAndVerify(new[] { RangeStruct, LongRangeStruct, source }, expectedOutput: $"{big},{big+1};{big},{big+1};10,{big+2};10,11,12;2,3,4");
        }

        [Fact]
        public void BuiltInRangesWithConversions()
        {
            var source = @"
using System;
struct C
{
    static void Main()
    {
        var a = ((sbyte)2)..((sbyte)4);
        var b = ((byte)2)..((byte)4);
        var c = ((short)2)..((short)4);
        var d = ((ushort)2)..((ushort)4);
        var e = ((uint)2)..((uint)4);
        Console.Write(string.Join("","",
            a.GetType().Name,
            b.GetType().Name,
            c.GetType().Name,
            d.GetType().Name,
            e.GetType().Name
        ));
    }
}
";

            CompileAndVerify(new[] { RangeStruct, LongRangeStruct, source }, expectedOutput: $"Range,Range,Range,Range,LongRange");
        }

        [Fact]
        public void BadlyTypedBuiltInRanges()
        {
            var source = @"
struct C
{
    static void Main()
    {
        var a = false..true;
        var b = ((ulong)2)..((ulong)4);
        var c = ((float)2)..((float)4);
        var d = ((double)2)..((double)4);
    }
}
";

            CreateStandardCompilation(new[] { RangeStruct, LongRangeStruct, source }).VerifyDiagnostics(
                // (6,17): error CS0019: Operator '..' cannot be applied to operands of type 'bool' and 'bool'
                //         var a = false..true;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "false..true").WithArguments("..", "bool", "bool").WithLocation(6, 17),
                // (7,17): error CS0019: Operator '..' cannot be applied to operands of type 'ulong' and 'ulong'
                //         var b = ((ulong)2)..((ulong)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((ulong)2)..((ulong)4)").WithArguments("..", "ulong", "ulong").WithLocation(7, 17),
                // (8,17): error CS0019: Operator '..' cannot be applied to operands of type 'float' and 'float'
                //         var c = ((float)2)..((float)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((float)2)..((float)4)").WithArguments("..", "float", "float").WithLocation(8, 17),
                // (9,17): error CS0019: Operator '..' cannot be applied to operands of type 'double' and 'double'
                //         var d = ((double)2)..((double)4);
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "((double)2)..((double)4)").WithArguments("..", "double", "double").WithLocation(9, 17)
            );
        }
    }
}
