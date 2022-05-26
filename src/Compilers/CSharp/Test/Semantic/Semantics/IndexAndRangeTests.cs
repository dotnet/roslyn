// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IndexAndRangeTests : CompilingTestBase
    {
        private const string RangeCtorSignature = "System.Range..ctor(System.Index start, System.Index end)";
        private const string RangeStartAtSignature = "System.Range System.Range.StartAt(System.Index start)";
        private const string RangeEndAtSignature = "System.Range System.Range.EndAt(System.Index end)";
        private const string RangeAllSignature = "System.Range System.Range.All.get";

        [Fact]
        public void RangeBadIndexerTypes()
        {
            var src = @"
using System;

public static class Program {
    public static void Main() {
        var a = new Span<byte>();
        var b = a[""str2""];
        var c = a[null];
        var d = a[Main()];
        var e = a[new object()];
        Console.WriteLine(zzz[0]);
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src);
            comp.VerifyDiagnostics(
                // (7,19): error CS1503: Argument 1: cannot convert from 'string' to 'int'
                //         var b = a["str2"];
                Diagnostic(ErrorCode.ERR_BadArgType, @"""str2""").WithArguments("1", "string", "int").WithLocation(7, 19),
                // (8,19): error CS1503: Argument 1: cannot convert from '<null>' to 'int'
                //         var c = a[null];
                Diagnostic(ErrorCode.ERR_BadArgType, "null").WithArguments("1", "<null>", "int").WithLocation(8, 19),
                // (9,19): error CS1503: Argument 1: cannot convert from 'void' to 'int'
                //         var d = a[Main()];
                Diagnostic(ErrorCode.ERR_BadArgType, "Main()").WithArguments("1", "void", "int").WithLocation(9, 19),
                // (10,19): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                //         var e = a[new object()];
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "int").WithLocation(10, 19),
                // (11,27): error CS0103: The name 'zzz' does not exist in the current context
                //         Console.WriteLine(zzz[0]);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "zzz").WithArguments("zzz").WithLocation(11, 27));
        }

        [Fact]
        public void PatternIndexRangeLangVer()
        {
            var src = @"
using System;
struct S
{
    public int Length => 0;
    public int Slice(int x, int y) => 0;
}
class C
{
    void M(string s, Index i, Range r)
    {
        _ = s[i];
        _ = s[r];
        _ = new S()[r];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics();
            comp = CreateCompilationWithIndexAndRange(src, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (12,13): error CS8652: The feature 'index operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         _ = s[i];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "s[i]").WithArguments("index operator", "8.0").WithLocation(12, 13),
                // (13,13): error CS8652: The feature 'index operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         _ = s[r];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "s[r]").WithArguments("index operator", "8.0").WithLocation(13, 13),
                // (14,13): error CS8652: The feature 'index operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         _ = new S()[r];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "new S()[r]").WithArguments("index operator", "8.0").WithLocation(14, 13));
        }

        [Fact]
        public void PatternIndexRangeReadOnly_01()
        {
            var src = @"
using System;
struct S
{
    public int this[int i] => 0;
    public int Length => 0;
    public int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i]; // 1, 2
        _ = this[r]; // 3, 4
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[i]; // 1, 2
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(11, 13),
                // (11,13): warning CS8656: Call to non-readonly member 'S.this[int].get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[i]; // 1, 2
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.this[int].get", "this").WithLocation(11, 13),
                // (12,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[r]; // 3, 4
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(12, 13),
                // (12,13): warning CS8656: Call to non-readonly member 'S.Slice(int, int)' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[r]; // 3, 4
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Slice(int, int)", "this").WithLocation(12, 13));
        }

        [Fact]
        public void PatternIndexRangeReadOnly_02()
        {
            var src = @"
using System;
struct S
{
    public int this[int i] => 0;
    public readonly int Length => 0;
    public int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i]; // 1
        _ = this[r]; // 2
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8656: Call to non-readonly member 'S.this[int].get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[i]; // 1
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.this[int].get", "this").WithLocation(11, 13),
                // (12,13): warning CS8656: Call to non-readonly member 'S.Slice(int, int)' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[r]; // 2
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Slice(int, int)", "this").WithLocation(12, 13));
        }

        [Fact]
        public void PatternIndexRangeReadOnly_03()
        {
            var src = @"
using System;
struct S
{
    public readonly int this[int i] => 0;
    public int Length => 0;
    public readonly int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i]; // 1
        _ = this[r]; // 2
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[i]; // 1
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(11, 13),
                // (12,13): warning CS8656: Call to non-readonly member 'S.Length.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[r]; // 2
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Length.get", "this").WithLocation(12, 13));
        }

        [Fact]
        public void PatternIndexRangeReadOnly_04()
        {
            var src = @"
using System;
struct S
{
    public readonly int this[int i] => 0;
    public int Count => 0;
    public readonly int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i]; // 1
        _ = this[r]; // 2
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,13): warning CS8656: Call to non-readonly member 'S.Count.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[i]; // 1
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Count.get", "this").WithLocation(11, 13),
                // (12,13): warning CS8656: Call to non-readonly member 'S.Count.get' from a 'readonly' member results in an implicit copy of 'this'.
                //         _ = this[r]; // 2
                Diagnostic(ErrorCode.WRN_ImplicitCopyInReadOnlyMember, "this").WithArguments("S.Count.get", "this").WithLocation(12, 13));
        }

        [Fact]
        public void PatternIndexRangeReadOnly_05()
        {
            var src = @"
using System;
struct S
{
    public readonly int this[int i] => 0;
    public readonly int Length => 0;
    public readonly int Slice(int x, int y) => 0;

    readonly void M(Index i, Range r)
    {
        _ = this[i];
        _ = this[r];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void SpanPatternRangeDelegate()
        {
            var src = @"
using System;
class C
{
    void Throws<T>(Func<T> f) { }
    public static void Main()
    {
        string s = ""abcd"";
        Throws(() => new Span<char>(s.ToCharArray())[0..1]);
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src, TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (9,9): error CS0306: The type 'Span<char>' may not be used as a type argument
                //         Throws(() => new Span<char>(s.ToCharArray())[0..1]);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "Throws").WithArguments("System.Span<char>").WithLocation(9, 9));
        }

        [Fact]
        public void PatternIndexNoRefIndexer()
        {
            var src = @"
struct S
{
    public int Length => 0;
    public int this[int i] => 0;
}
class C
{
    void M(S s)
    {
        ref readonly int x = ref s[^2];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,34): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         ref readonly int x = ref s[^2];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "s[^2]").WithLocation(11, 34));
        }

        [Fact]
        public void PatternRangeSpanNoReturn()
        {
            var src = @"
using System;
class C
{
    Span<int> M()
    {
        Span<int> s1 = stackalloc int[10];
        Span<int> s2 = s1[0..2];
        return s2;
    }
}";
            var comp = CreateCompilationWithIndexAndRangeAndSpan(src);
            comp.VerifyDiagnostics(
                // (9,16): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         return s2;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(9, 16));
        }

        [Fact]
        public void PatternIndexAndRangeLengthInaccessible()
        {
            var src = @"
class B
{
    private int Length => 0;
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[^0];
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (12,15): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = b[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(12, 15),
                // (13,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(13, 15)
                );
        }

        [Fact]
        public void PatternIndexAndRangeLengthNoGetter()
        {
            var src = @"
class B
{
    public int Length { set { } }
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[^0];
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (12,15): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = b[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(12, 15),
                // (13,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(13, 15)
                );
        }

        [Fact]
        public void PatternIndexAndRangeGetLengthInaccessible()
        {
            var src = @"
class B
{
    public int Length { private get => 0; set { } }
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[^0];
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (12,15): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = b[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(12, 15),
                // (13,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(13, 15)
                );
        }

        [Fact]
        public void PatternIndexAndRangePatternMethodsInaccessible()
        {
            var src = @"
class B
{
    public int Length => 0;
    public int this[int i] { private get => 0; set { } }
    private int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[^0];
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (12,13): error CS0271: The property or indexer 'B.this[int]' cannot be used in this context because the get accessor is inaccessible
                //         _ = b[^0];
                Diagnostic(ErrorCode.ERR_InaccessibleGetter, "b[^0]").WithArguments("B.this[int]").WithLocation(12, 13),
                // (13,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(13, 15)
                );
        }

        [Fact]
        public void PatternIndexAndRangeStaticLength()
        {
            var src = @"
class B
{
    public static int Length => 0;
    public int this[int i] => 0;
    private int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[^0];
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (12,15): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = b[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(12, 15),
                // (13,15): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(13, 15)
                );
        }

        [Fact]
        public void PatternIndexAndRangeStaticSlice()
        {
            var src = @"
class B
{
    public int Length => 0;
    private static int Slice(int i, int j) => 0;
}
class C
{
    void M(B b)
    {
        _ = b[0..];
    }
}";
            var comp = CreateCompilationWithIndexAndRange(src);
            comp.VerifyDiagnostics(
                // (11,13): error CS0021: Cannot apply indexing with [] to an expression of type 'B'
                //         _ = b[0..];
                Diagnostic(ErrorCode.ERR_BadIndexLHS, "b[0..]").WithArguments("B").WithLocation(11, 13)
                );
        }

        [Fact]
        public void PatternIndexAndRangeNoGetOffset()
        {
            var src = @"
namespace System
{
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
        public static implicit operator Index(int value) => default;
    }
    public struct Range
    {
        public Range(Index start, Index end) { }
        public Index Start => default;
        public Index End => default;
    }
}
class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
    void M()
    {
        _ = this[^0];
        _ = this[0..];
    }
}";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                    // (23,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                    //         _ = this[^0];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "this[^0]").WithArguments("System.Index", "GetOffset").WithLocation(23, 13),
                    // (24,13): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                    //         _ = this[0..];
                    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "this[0..]").WithArguments("System.Index", "GetOffset").WithLocation(24, 13)
                );
        }

        [Theory]
        [InlineData("Start")]
        [InlineData("End")]
        public void PatternIndexAndRangeNoStartAndEnd(string propertyName)
        {
            var src = @"
namespace System
{
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
        public static implicit operator Index(int value) => default;
        public int GetOffset(int length) => 0;
    }
    public struct Range
    {
        public Range(Index start, Index end) { }
        public Index " + propertyName + @" => default;
    }
}
class C
{
    public int Length => 0;
    public int this[int i] => 0;
    public int Slice(int i, int j) => 0;
    void M()
    {
        _ = this[^0];
        _ = this[0..];
    }
}";
            var comp = CreateCompilation(src);

            comp.VerifyDiagnostics(
                // (24,13): error CS0656: Missing compiler required member 'System.Range.get_...'
                //         _ = this[0..];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "this[0..]").WithArguments("System.Range", "get_" + (propertyName == "Start" ? "End" : "Start")).WithLocation(24, 13)
                );
        }

        [Fact]
        public void PatternIndexAndRangeNoOptionalParams()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
class C
{
    public int Length => 0;
    public int this[int i, int j = 0] => i;
    public int Slice(int i, int j, int k = 0) => i;
    public void M()
    {
        _ = this[^0];
        _ = this[0..];
    }
}");
            comp.VerifyDiagnostics(
                // (9,18): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = this[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(9, 18),
                // (10,18): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = this[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(10, 18));
        }

        [Fact]
        public void PatternIndexAndRangeUseOriginalDefition()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
struct S1<T>
{
    public int Length => 0;
    public int this[T t] => 0;
    public int Slice(T t, int j) => 0;
}
struct S2<T>
{
    public T Length => default;
    public int this[int t] => 0;
    public int Slice(int t, int j) => 0;
}
class C
{
    void M()
    {
        var s1 = new S1<int>();
        _ = s1[^0];
        _ = s1[0..];

        var s2 = new S2<int>();
        _ = s2[^0];
        _ = s2[0..];
    }
}");
            comp.VerifyDiagnostics(
                // (19,16): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = s1[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(19, 16),
                // (20,16): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = s1[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(20, 16),
                // (23,16): error CS1503: Argument 1: cannot convert from 'System.Index' to 'int'
                //         _ = s2[^0];
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "int").WithLocation(23, 16),
                // (24,16): error CS1503: Argument 1: cannot convert from 'System.Range' to 'int'
                //         _ = s2[0..];
                Diagnostic(ErrorCode.ERR_BadArgType, "0..").WithArguments("1", "System.Range", "int").WithLocation(24, 16));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void ArrayRangeIllegalRef()
        {
            var comp = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public struct Boolean { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
    }
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
        public static implicit operator Index(int value) => default;
    }
    public struct Range
    {
        public Range(Index start, Index end) { }
    }
}
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static T[] GetSubArray<T>(T[] array, Range range) => null;
    }
}
public class C {
    public ref int[] M(int[] arr) {
        ref int[] x = ref arr[0..2];
        M(in arr[0..2]);
        M(arr[0..2]);
        return ref arr[0..2];
    }
    void M(in int[] arr) { }
}");
            comp.VerifyDiagnostics(
                // (32,27): error CS1510: A ref or out value must be an assignable variable
                //         ref int[] x = ref arr[0..2];
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "arr[0..2]").WithLocation(32, 27),
                // (33,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         M(in arr[0..2]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "arr[0..2]").WithLocation(33, 14),
                // (35,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref arr[0..2];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "arr[0..2]").WithLocation(35, 20));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void ArrayRangeIllegalRefNoRange()
        {
            var comp = CreateCompilationWithIndex(@"
public class C {
    public void M(int[] arr) {
        ref int[] x = ref arr[0..2];
    }
}");
            comp.VerifyDiagnostics(
                // (4,31): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         ref int[] x = ref arr[0..2];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "0..2").WithArguments("System.Range").WithLocation(4, 31));
        }

        [Fact]
        public void ArrayRangeIndexerNoHelper()
        {
            var comp = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
    }
}
public class C {
    public void M(int[] arr) {
        var x = arr[0..2];
    }
}");
            comp.VerifyDiagnostics(
                // (11,17): error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.RuntimeHelpers.GetSubArray'
                //         var x = arr[0..2];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "0..2").WithArguments("System.Runtime.CompilerServices.RuntimeHelpers", "GetSubArray").WithLocation(11, 21));
        }

        [Fact]
        public void ArrayIndexIndexerNoHelper()
        {
            const string source = @"
class C
{
    public void M(int[] arr)
    {
        var x = arr[^2];
    }
}";
            var comp = CreateCompilation(source + @"
namespace System
{
    public readonly struct Index
    {
        public Index(int value, bool fromEnd) { }
    }
}");
            comp.VerifyDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Index.GetOffset'
                //         var x = arr[^2];
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^2").WithArguments("System.Index", "GetOffset").WithLocation(6, 21));

            comp = CreateCompilation(source + @"
namespace System
{
    public readonly struct Index
    {
        public Index(int value, bool fromEnd) { }
        public int GetOffset(int length) => 0;
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StringIndexers()
        {
            // The string type in our standard references don't have indexers for string or range
            var comp = CreateCompilationWithIndexAndRange(@"
class C
{
    public void M(string s)
    {
        var x = s[^0];
        var y = s[1..];
    }
}");
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void FromEndIllegalRef()
        {
            var comp = CreateCompilationWithIndex(@"
using System;
public class C {
    public ref Index M() {
        ref Index x = ref ^0;
        M(in ^0);
        M(^0);
        return ref ^0;
    }
    void M(in int[] arr) { }
}");
            comp.VerifyDiagnostics(
                // (5,27): error CS1510: A ref or out value must be an assignable variable
                //         ref Index x = ref ^0;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "^0").WithLocation(5, 27),
                // (6,14): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         M(in ^0);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "^0").WithLocation(6, 14),
                // (7,11): error CS1503: Argument 1: cannot convert from 'System.Index' to 'in int[]'
                //         M(^0);
                Diagnostic(ErrorCode.ERR_BadArgType, "^0").WithArguments("1", "System.Index", "in int[]").WithLocation(7, 11),
                // (8,20): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //         return ref ^0;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "^0").WithLocation(8, 20));
        }

        [Fact]
        [WorkItem(31889, "https://github.com/dotnet/roslyn/issues/31889")]
        public void FromEndIllegalRefNoIndex()
        {
            var comp = CreateCompilationWithIndex(@"
public class C {
    public void M() {
        ref var x = ref ^0;
    }
}");
            comp.VerifyDiagnostics(
                // (4,25): error CS1510: A ref or out value must be an assignable variable
                //         ref var x = ref ^0;
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "^0").WithLocation(4, 25));
        }

        [Fact]
        public void IndexExpression_TypeNotFound()
        {
            var compilation = CreateCompilation(@"
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Index", ".ctor").WithLocation(6, 17),
                // (6,17): error CS0518: Predefined type 'System.Index' is not defined or imported
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "^arg").WithArguments("System.Index").WithLocation(6, 17));
        }

        [Fact]
        public void IndexExpression_LiftedTypeIsNotNullable()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public class Index
    {
        public Index(int value, bool fromEnd) { }
    }
}
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (13,17): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "^arg").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(13, 17));
        }

        [Fact]
        public void IndexExpression_NullableConstructorNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public struct Boolean { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
    }
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
    }
}
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (22,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(22, 17));
        }

        [Fact]
        public void IndexExpression_ConstructorNotFound()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public readonly struct Index
    {
    }
}
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics(
                // (12,17): error CS0656: Missing compiler required member 'System.Index..ctor'
                //         var x = ^arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "^arg").WithArguments("System.Index", ".ctor").WithLocation(12, 17));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("System.Index", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expression).Symbol);
        }

        [Fact]
        public void IndexExpression_SemanticModel()
        {
            var compilation = CreateCompilationWithIndex(@"
class Test
{
    void M(int arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("^", expression.OperatorToken.ToFullString());
            Assert.Equal("System.Index", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal("System.Index..ctor(System.Int32 value, [System.Boolean fromEnd = false])", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void IndexExpression_Nullable_SemanticModel()
        {
            var compilation = CreateCompilationWithIndex(@"
class Test
{
    void M(int? arg)
    {
        var x = ^arg;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<PrefixUnaryExpressionSyntax>().Single();
            Assert.Equal("^", expression.OperatorToken.ToFullString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal("System.Index..ctor(System.Int32 value, [System.Boolean fromEnd = false])", model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void IndexExpression_InvalidTypes()
        {
            var compilation = CreateCompilationWithIndex(@"
class Test
{
    void M()
    {
        var x = ^""string"";
        var y = ^1.5;
        var z = ^true;
    }
}").VerifyDiagnostics(
                //(6,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         var x = ^"string";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"^""string""").WithArguments("string", "int").WithLocation(6, 17),
                //(7,17): error CS0029: Cannot implicitly convert type 'double' to 'int'
                //         var y = ^1.5;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "^1.5").WithArguments("double", "int").WithLocation(7, 17),
                //(8,17): error CS0029: Cannot implicitly convert type 'bool' to 'int'
                //         var z = ^true;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "^true").WithArguments("bool", "int").WithLocation(8, 17));
        }

        [Fact]
        public void IndexExpression_NoOperatorOverloading()
        {
            var compilation = CreateCompilationWithIndex(@"
public class Test
{
    public static Test operator ^(Test value) => default;  
}").VerifyDiagnostics(
                // (4,33): error CS1019: Overloadable unary operator expected
                //     public static Test operator ^(Test value) => default;  
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "^").WithLocation(4, 33));
        }

        [Fact]
        public void IndexExpression_OlderLanguageVersion()
        {
            var expected = new[]
            {
                // (6,17): error CS8652: The feature 'index operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var x = ^1;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "^1").WithArguments("index operator", "8.0").WithLocation(6, 17)
            };
            const string source = @"
class Test
{
    void M()
    {
        var x = ^1;
    }
}";
            var compilation = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);
            compilation = CreateCompilationWithIndex(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        public void RangeExpression_RangeNotFound()
        {
            var compilation = CreateCompilationWithIndex(@"
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..2").WithArguments("System.Range").WithLocation(6, 17),
                // (7,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "1..").WithArguments("System.Range").WithLocation(7, 17),
                // (8,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..2").WithArguments("System.Range").WithLocation(8, 17),
                // (9,17): error CS0518: Predefined type 'System.Range' is not defined or imported
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "..").WithArguments("System.Range").WithLocation(9, 17));
        }

        [Fact]
        public void RangeExpression_LiftedRangeNotNullable()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public class Range
    {
        public Range(Index start, Index end) { }
    }
}
class Test
{
    void M(System.Index? index)
    {
        var a = index..index;
    }
}").VerifyDiagnostics(
                // (13,17): error CS0453: The type 'Range' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index..index").WithArguments("System.Nullable<T>", "T", "System.Range").WithLocation(13, 17));
        }

        [Fact]
        public void RangeExpression_LiftedIndexNotNullable()
        {
            var compilation = CreateCompilation(@"
namespace System
{
    public class Index
    {
        public Index(int value, bool fromEnd) { }
        public static implicit operator Index(int value) => new Index(value, fromEnd: false);
    }
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
    }
}
class Test
{
    void M(int? index)
    {
        var a = index..index;
    }
}").VerifyDiagnostics(
                // (18,17): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(18, 17),
                // (18,17): error CS0029: Cannot implicitly convert type 'int?' to 'System.Index?'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "index").WithArguments("int?", "System.Index?").WithLocation(18, 17),
                // (18,24): error CS0453: The type 'Index' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Nullable<T>'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "index").WithArguments("System.Nullable<T>", "T", "System.Index").WithLocation(18, 24),
                // (18,24): error CS0029: Cannot implicitly convert type 'int?' to 'System.Index?'
                //         var a = index..index;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "index").WithArguments("int?", "System.Index?").WithLocation(18, 24));
        }

        [Fact]
        public void RangeExpression_WithoutRangeCtor()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        // public Range(Index start, Index end) => default;
        public static Range StartAt(Index start) => default;
        public static Range EndAt(Index end) => default;
        public static Range All => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics(
                // (16,17): error CS0656: Missing compiler required member 'System.Range..ctor'
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "1..2").WithArguments("System.Range", ".ctor").WithLocation(16, 17));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expressions = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ToArray();
            Assert.Equal(4, expressions.Length);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[0]).Type.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(expressions[0]).Symbol);
            Assert.Equal("System.Int32", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal(RangeStartAtSignature, model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Int32", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal(RangeEndAtSignature, model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal(RangeAllSignature, model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_NullableConstructorNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public struct Boolean { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
    }
    public struct Index
    {
        public Index(int value, bool fromEnd) { }
    }
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
    }
}
class Test
{
    void M(System.Index? arg)
    {
        var x = arg..arg;
    }
}").VerifyDiagnostics(
                // (26,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 17),
                // (26,22): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 22),
                // (26,17): error CS0656: Missing compiler required member 'System.Nullable`1..ctor'
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "arg..arg").WithArguments("System.Nullable`1", ".ctor").WithLocation(26, 17));
        }

        [Fact]
        public void RangeExpression_BooleanNotFound()
        {
            var compilation = CreateEmptyCompilation(@"
namespace System
{
    public struct Int32 { }
    public class ValueType { }
    public class String { }
    public class Object { }
    public class Void { }
    public struct Nullable<T> where T : struct
    {
        public Nullable(T value) { }
    }
    public struct Index
    {
    }
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
    }
}
class Test
{
    void M(System.Index? arg)
    {
        var x = arg..arg;
    }
}").VerifyDiagnostics(
                // (25,17): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //         var x = arg..arg;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "arg..arg").WithArguments("System.Boolean").WithLocation(25, 17));
        }

        [Fact]
        public void RangeExpression_WithoutRangeStartAt()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
        // public static Range StartAt(Index start) => default;
        public static Range EndAt(Index end) => default;
        public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(1);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void RangeExpression_WithoutRangeEndAt()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
        public static Range StartAt(Index start) => default;
        // public static Range EndAt(Index end) => default;
        public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(2);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void RangeExpression_WithoutRangeAll()
        {
            var compilation = CreateCompilationWithIndex(@"
namespace System
{
    public readonly struct Range
    {
        public Range(Index start, Index end) { }
        public static Range StartAt(Index start) => default;
        public static Range EndAt(Index end) => default;
        // public static Range All() => default;
    }
}
class Test
{
    void M(int arg)
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expression = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ElementAt(3);
            Assert.Equal("System.Range", model.GetTypeInfo(expression).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expression).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void RangeExpression_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
class Test
{
    void M(Index start, Index end)
    {
        var a = start..end;
        var b = start..;
        var c = ..end;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expressions = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ToArray();
            Assert.Equal(4, expressions.Length);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[0]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal(RangeStartAtSignature, model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal(RangeEndAtSignature, model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal(RangeAllSignature, model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_Nullable_SemanticModel()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
using System;
class Test
{
    void M(Index? start, Index? end)
    {
        var a = start..end;
        var b = start..;
        var c = ..end;
        var d = ..;
    }
}").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var expressions = tree.GetRoot().DescendantNodes().OfType<RangeExpressionSyntax>().ToArray();
            Assert.Equal(4, expressions.Length);

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[0]).Type.ToTestDisplayString());
            Assert.Equal(RangeCtorSignature, model.GetSymbolInfo(expressions[0]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].RightOperand).Type.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[0].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[1]).Type.ToTestDisplayString());
            Assert.Equal(RangeStartAtSignature, model.GetSymbolInfo(expressions[1]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[1].RightOperand);
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[1].LeftOperand).Type.ToTestDisplayString());

            Assert.Equal("System.Range?", model.GetTypeInfo(expressions[2]).Type.ToTestDisplayString());
            Assert.Equal(RangeEndAtSignature, model.GetSymbolInfo(expressions[2]).Symbol.ToTestDisplayString());
            Assert.Equal("System.Index?", model.GetTypeInfo(expressions[2].RightOperand).Type.ToTestDisplayString());
            Assert.Null(expressions[2].LeftOperand);

            Assert.Equal("System.Range", model.GetTypeInfo(expressions[3]).Type.ToTestDisplayString());
            Assert.Equal(RangeAllSignature, model.GetSymbolInfo(expressions[3]).Symbol.ToTestDisplayString());
            Assert.Null(expressions[3].RightOperand);
            Assert.Null(expressions[3].LeftOperand);
        }

        [Fact]
        public void RangeExpression_InvalidTypes()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var a = 1..""string"";
        var b = 1.5..;
        var c = ..true;
        var d = ..M();
    }
}").VerifyDiagnostics(
                // (6,20): error CS0029: Cannot implicitly convert type 'string' to 'System.Index'
                //         var a = 1.."string";
                Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""string""").WithArguments("string", "System.Index").WithLocation(6, 20),
                // (7,17): error CS0029: Cannot implicitly convert type 'double' to 'System.Index'
                //         var b = 1.5..;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1.5").WithArguments("double", "System.Index").WithLocation(7, 17),
                // (8,19): error CS0029: Cannot implicitly convert type 'bool' to 'System.Index'
                //         var c = ..true;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "true").WithArguments("bool", "System.Index").WithLocation(8, 19),
                // (9,19): error CS0029: Cannot implicitly convert type 'void' to 'System.Index'
                //         var d = ..M();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "M()").WithArguments("void", "System.Index").WithLocation(9, 19));
        }

        [Fact]
        public void RangeExpression_NoOperatorOverloading()
        {
            var compilation = CreateCompilationWithIndexAndRange(@"
public class Test
{
    public static Test operator ..(Test value) => default;
    public static Test operator ..(Test value1, Test value2) => default;
}").VerifyDiagnostics(
                // (4,33): error CS1019: Overloadable unary operator expected
                //     public static Test operator ..(Test value) => default;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "..").WithLocation(4, 33),
                // (5,33): error CS1020: Overloadable binary operator expected
                //     public static Test operator ..(Test value1, Test value2) => default;
                Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "..").WithLocation(5, 33));
        }

        [Fact]
        public void RangeExpression_OlderLanguageVersion()
        {
            const string source = @"
class Test
{
    void M()
    {
        var a = 1..2;
        var b = 1..;
        var c = ..2;
        var d = ..;
    }
}";
            var expected = new[]
            {
                // (6,17): error CS8652: The feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var a = 1..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..2").WithArguments("range operator", "8.0").WithLocation(6, 17),
                // (7,17): error CS8652: The feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var b = 1..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "1..").WithArguments("range operator", "8.0").WithLocation(7, 17),
                // (8,17): error CS8652: The feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var c = ..2;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..2").WithArguments("range operator", "8.0").WithLocation(8, 17),
                // (9,17): error CS8652: The feature 'range operator' is not available in C# 7.3. Please use language version 8.0 or greater.
                //         var d = ..;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "..").WithArguments("range operator", "8.0").WithLocation(9, 17)
            };
            CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected);
            CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.Regular8).VerifyDiagnostics();
        }

        [Fact]
        public void IndexOnNonTypedNodes()
        {
            CreateCompilationWithIndex(@"
class Test
{
    void M()
    {
        var a = ^M;
        var b = ^null;
        var c = ^default;
    }
}").VerifyDiagnostics(
                // (6,17): error CS0428: Cannot convert method group 'M' to non-delegate type 'int'. Did you intend to invoke the method?
                //         var a = ^M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "^M").WithArguments("M", "int").WithLocation(6, 17),
                // (7,17): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         var b = ^null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "^null").WithArguments("int").WithLocation(7, 17));
        }

        [Fact]
        public void RangeOnNonTypedNodes()
        {
            CreateCompilationWithIndexAndRange(@"
class Test
{
    void M()
    {
        var a = 0..M;
        var b = 0..null;
        var c = 0..default;

        var d = M..0;
        var e = null..0;
        var f = default..0;
    }
}").VerifyDiagnostics(
                // (6,20): error CS0428: Cannot convert method group 'M' to non-delegate type 'Index'. Did you intend to invoke the method?
                //         var a = 0..M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "System.Index").WithLocation(6, 20),
                // (7,20): error CS0037: Cannot convert null to 'Index' because it is a non-nullable value type
                //         var b = 0..null;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Index").WithLocation(7, 20),
                // (10,17): error CS0428: Cannot convert method group 'M' to non-delegate type 'Index'. Did you intend to invoke the method?
                //         var d = M..0;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "System.Index").WithLocation(10, 17),
                // (11,17): error CS0037: Cannot convert null to 'Index' because it is a non-nullable value type
                //         var e = null..0;
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("System.Index").WithLocation(11, 17));
        }

        [Fact]
        public void Range_OnVarOut_Error()
        {
            CreateCompilationWithIndexAndRange(@"
using System;
partial class Program
{
    static void Main()
    {
        var result = y..Create(out Index y);
    }
    static Index Create(out Index y)
    {
        y = ^2;
        return ^1;
    }
}").VerifyDiagnostics(
                // (7,22): error CS0841: Cannot use local variable 'y' before it is declared
                //         var result = y..Create(out Index y);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 22));
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void DontAllowNamedArgumentsForImplicitRangeIndexer()
        {
            CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public class C 
{
    public static void M(string text) 
    {
        _ = text[startIndex: 1..^1];
        _ = text[range: 1..^1];
        _ = text[notEvenTheCorrectName: 1..^1];
        _ = new Span<char>(text.ToCharArray())[start: 1..^1];
        _ = new Span<char>(text.ToCharArray())[range: 1..^1];
        _ = new Span<char>(text.ToCharArray())[notEvenTheCorrectName: 1..^1];
    }
}").VerifyDiagnostics(
                    // (7,18): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = text[startIndex: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "startIndex").WithLocation(7, 18),
                    // (8,18): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = text[range: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "range").WithLocation(8, 18),
                    // (9,18): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = text[notEvenTheCorrectName: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "notEvenTheCorrectName").WithLocation(9, 18),
                    // (10,48): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = new Span<char>(text.ToCharArray())[start: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "start").WithLocation(10, 48),
                    // (11,48): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = new Span<char>(text.ToCharArray())[range: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "range").WithLocation(11, 48),
                    // (12,48): error CS8429: Invocation of implicit Range Indexer cannot name the argument.
                    //         _ = new Span<char>(text.ToCharArray())[notEvenTheCorrectName: 1..^1];
                    Diagnostic(ErrorCode.ERR_ImplicitRangeIndexerWithName, "notEvenTheCorrectName").WithLocation(12, 48));
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void DontAllowNamedArgumentsForImplicitIndexIndexer()
        {
            CreateCompilationWithIndexAndRangeAndSpan(@"
using System;
public class C 
{
    public static void M(string text) 
    {
        _ = text[index: ^1];
        _ = text[notEvenTheCorrectName: ^1];
        _ = new Span<char>(text.ToCharArray())[index: ^1];
        _ = new Span<char>(text.ToCharArray())[notEvenTheCorrectName: ^1];
    }
}").VerifyDiagnostics(
                    // (7,18): error CS8428: Invocation of implicit Index Indexer cannot name the argument.
                    //         _ = text[index: ^1];
                    Diagnostic(ErrorCode.ERR_ImplicitIndexIndexerWithName, "index").WithLocation(7, 18),
                    // (8,18): error CS8428: Invocation of implicit Index Indexer cannot name the argument.
                    //         _ = text[notEvenTheCorrectName: ^1];
                    Diagnostic(ErrorCode.ERR_ImplicitIndexIndexerWithName, "notEvenTheCorrectName").WithLocation(8, 18),
                    // (9,48): error CS8428: Invocation of implicit Index Indexer cannot name the argument.
                    //         _ = new Span<char>(text.ToCharArray())[index: ^1];
                    Diagnostic(ErrorCode.ERR_ImplicitIndexIndexerWithName, "index").WithLocation(9, 48),
                    // (10,48): error CS8428: Invocation of implicit Index Indexer cannot name the argument.
                    //         _ = new Span<char>(text.ToCharArray())[notEvenTheCorrectName: ^1];
                    Diagnostic(ErrorCode.ERR_ImplicitIndexIndexerWithName, "notEvenTheCorrectName").WithLocation(10, 48));
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void AllowNamedArgumentsForRealRangeIndexer1()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Range range] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[range: 1..^1]);
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void AllowNamedArgumentsForRealRangeIndexer2()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Range param] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[param: 1..^1]);
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void DontAllowIncorrectNamedArgumentsForRealRangeIndexer()
        {
            CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Range range] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[param: 1..^1]);
    }
}").VerifyDiagnostics(
                    // (11,31): error CS1739: The best overload for 'this' does not have a parameter named 'param'
                    //         Console.Write(new A()[param: 1..^1]);
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "param").WithArguments("this", "param").WithLocation(11, 31));
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void AllowNamedArgumentsForRealIndexIndexer1()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Index index] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[index: ^1]);
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void AllowNamedArgumentsForRealIndexIndexer2()
        {
            var comp = CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Index param] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[param: ^1]);
    }
}", options: TestOptions.ReleaseExe).VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [Fact, WorkItem(39852, "https://github.com/dotnet/roslyn/issues/39852")]
        public void DontAllowIncorrectNamedArgumentsForRealIndexIndexer()
        {
            CreateCompilationWithIndexAndRange(@"
using System;
public class A
{
     public int this[Index index] => 42;
}
public class C 
{
    public static void Main() 
    {
        Console.Write(new A()[param: ^1]);
    }
}").VerifyDiagnostics(
                    // (11,31): error CS1739: The best overload for 'this' does not have a parameter named 'param'
                    //         Console.Write(new A()[param: ^1]);
                    Diagnostic(ErrorCode.ERR_BadNamedArgument, "param").WithArguments("this", "param").WithLocation(11, 31));
        }

        [Fact]
        [WorkItem(52724, "https://github.com/dotnet/roslyn/issues/52724")]
        public void ValEscape_01()
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS0436
using System;
namespace System {
    public readonly ref struct Index {
        public Index(int value, bool fromEnd = false) {
        }
    }
}
namespace ConsoleApp1 {
    interface I<T> {
        ref readonly T this[Index index] { get; }
    }
    class Program {
        static ref readonly T f<T>(I<T> i) {
            return ref i[new Index(1, true)];
        }
        static ref readonly T h<T>(I<T> i) {
            return ref i[^1];
        }
    }
}
", options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(52724, "https://github.com/dotnet/roslyn/issues/52724")]
        public void ValEscape_02()
        {
            var comp = CreateCompilation(@"
#pragma warning disable CS0436
using System;
namespace System {
    public readonly ref struct Index {
        public Index(int value, bool fromEnd = false) {
        }
    }
}
namespace ConsoleApp1 {
    interface I<T> {
        ref readonly T this[Index index] { get; }
    }
    class Program {
        static ref readonly T f<T>(I<T> i) {
            var x = new Index(1, true);
            return ref i[x];
        }
        static ref readonly T h<T>(I<T> i) {
            var x = ^1;
            return ref i[x];
        }
    }
}
", options: TestOptions.ReleaseDll);

            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        public void ValEscape_03()
        {
            var src = @"
using System;
namespace System
{
    public readonly ref struct Index
    {
        public Index(Span<int> x)
        {
        }
    }
    
    public readonly ref struct Range
    {
        public Range(System.Index start, System.Index end) => throw null;
        public static Range StartAt(System.Index start) => throw null;
        public static Range EndAt(System.Index end) => throw null;
        public static Range All => throw null;
    }
}

class Program
{
    static void Main() {}
    
    Range Test1()
    {
        Span<int> s1 = stackalloc int[10];
        return new Index(s1)..;
    }

    Range Test2()
    {
        Span<int> s2 = stackalloc int[10];
        return Range.StartAt(new Index(s2));
    }

    Range Test3()
    {
        Span<int> s3 = stackalloc int[10];
        return ..new Index(s3);
    }

    Range Test4()
    {
        Span<int> s4 = stackalloc int[10];
        return Range.EndAt(new Index(s4));
    }

    Range Test5(Span<int> s51)
    {
        Span<int> s52 = stackalloc int[10];
        return new Index(s51) .. new Index(s52);
    }

    Range Test6(Span<int> s61)
    {
        Span<int> s62 = stackalloc int[10];
        return new Range(new Index(s61), new Index(s62));
    }

    Range Test7(Span<int> s72)
    {
        Span<int> s71 = stackalloc int[10];
        return new Index(s71) .. new Index(s72);
    }

    Range Test8(Span<int> s82)
    {
        Span<int> s81 = stackalloc int[10];
        return new Range(new Index(s81), new Index(s82));
    }

    Range Test9()
    {
        Span<int> s91 = stackalloc int[10];
        Span<int> s92 = stackalloc int[10];
        return new Index(s91) .. new Index(s92);
    }

    Range Test10()
    {
        Span<int> s101 = stackalloc int[10];
        Span<int> s102 = stackalloc int[10];
        return new Range(new Index(s101), new Index(s102));
    }

    Range Test11()
    {
        return Range.All;
    }
}
";
            var comp = CreateCompilationWithSpan(src);
            comp.VerifyDiagnostics(
                // (28,16): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Index(s1)..;
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s1)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(28, 16),
                // (28,26): error CS8352: Cannot use local 's1' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Index(s1)..;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s1").WithArguments("s1").WithLocation(28, 26),
                // (34,16): error CS8347: Cannot use a result of 'Range.StartAt(Index)' in this context because it may expose variables referenced by parameter 'start' outside of their declaration scope
                //         return Range.StartAt(new Index(s2));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Range.StartAt(new Index(s2))").WithArguments("System.Range.StartAt(System.Index)", "start").WithLocation(34, 16),
                // (34,30): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return Range.StartAt(new Index(s2));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s2)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(34, 30),
                // (34,40): error CS8352: Cannot use local 's2' in this context because it may expose referenced variables outside of their declaration scope
                //         return Range.StartAt(new Index(s2));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s2").WithArguments("s2").WithLocation(34, 40),
                // (40,18): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return ..new Index(s3);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s3)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(40, 18),
                // (40,28): error CS8352: Cannot use local 's3' in this context because it may expose referenced variables outside of their declaration scope
                //         return ..new Index(s3);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s3").WithArguments("s3").WithLocation(40, 28),
                // (46,16): error CS8347: Cannot use a result of 'Range.EndAt(Index)' in this context because it may expose variables referenced by parameter 'end' outside of their declaration scope
                //         return Range.EndAt(new Index(s4));
                Diagnostic(ErrorCode.ERR_EscapeCall, "Range.EndAt(new Index(s4))").WithArguments("System.Range.EndAt(System.Index)", "end").WithLocation(46, 16),
                // (46,28): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return Range.EndAt(new Index(s4));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s4)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(46, 28),
                // (46,38): error CS8352: Cannot use local 's4' in this context because it may expose referenced variables outside of their declaration scope
                //         return Range.EndAt(new Index(s4));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s4").WithArguments("s4").WithLocation(46, 38),
                // (52,34): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Index(s51) .. new Index(s52);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s52)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(52, 34),
                // (52,44): error CS8352: Cannot use local 's52' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Index(s51) .. new Index(s52);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s52").WithArguments("s52").WithLocation(52, 44),
                // (58,16): error CS8347: Cannot use a result of 'Range.Range(Index, Index)' in this context because it may expose variables referenced by parameter 'end' outside of their declaration scope
                //         return new Range(new Index(s61), new Index(s62));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Range(new Index(s61), new Index(s62))").WithArguments("System.Range.Range(System.Index, System.Index)", "end").WithLocation(58, 16),
                // (58,42): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Range(new Index(s61), new Index(s62));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s62)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(58, 42),
                // (58,52): error CS8352: Cannot use local 's62' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Range(new Index(s61), new Index(s62));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s62").WithArguments("s62").WithLocation(58, 52),
                // (64,16): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Index(s71) .. new Index(s72);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s71)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(64, 16),
                // (64,26): error CS8352: Cannot use local 's71' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Index(s71) .. new Index(s72);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s71").WithArguments("s71").WithLocation(64, 26),
                // (70,16): error CS8347: Cannot use a result of 'Range.Range(Index, Index)' in this context because it may expose variables referenced by parameter 'start' outside of their declaration scope
                //         return new Range(new Index(s81), new Index(s82));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Range(new Index(s81), new Index(s82))").WithArguments("System.Range.Range(System.Index, System.Index)", "start").WithLocation(70, 16),
                // (70,26): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Range(new Index(s81), new Index(s82));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s81)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(70, 26),
                // (70,36): error CS8352: Cannot use local 's81' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Range(new Index(s81), new Index(s82));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s81").WithArguments("s81").WithLocation(70, 36),
                // (77,16): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Index(s91) .. new Index(s92);
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s91)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(77, 16),
                // (77,26): error CS8352: Cannot use local 's91' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Index(s91) .. new Index(s92);
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s91").WithArguments("s91").WithLocation(77, 26),
                // (84,16): error CS8347: Cannot use a result of 'Range.Range(Index, Index)' in this context because it may expose variables referenced by parameter 'start' outside of their declaration scope
                //         return new Range(new Index(s101), new Index(s102));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Range(new Index(s101), new Index(s102))").WithArguments("System.Range.Range(System.Index, System.Index)", "start").WithLocation(84, 16),
                // (84,26): error CS8347: Cannot use a result of 'Index.Index(Span<int>)' in this context because it may expose variables referenced by parameter 'x' outside of their declaration scope
                //         return new Range(new Index(s101), new Index(s102));
                Diagnostic(ErrorCode.ERR_EscapeCall, "new Index(s101)").WithArguments("System.Index.Index(System.Span<int>)", "x").WithLocation(84, 26),
                // (84,36): error CS8352: Cannot use local 's101' in this context because it may expose referenced variables outside of their declaration scope
                //         return new Range(new Index(s101), new Index(s102));
                Diagnostic(ErrorCode.ERR_EscapeLocal, "s101").WithArguments("s101").WithLocation(84, 36)
                );
        }

        [Fact]
        public void ValEscape_04()
        {
            var src = @"
using System;
namespace System
{
    public readonly ref struct Index
    {
        public Index(Span<int> x)
        {
        }
    }
    
    public readonly ref struct Range
    {
        public Range(System.Index start, System.Index end) => throw null;
        public static Range StartAt(System.Index start) => throw null;
        public static Range EndAt(System.Index end) => throw null;
        public static Range All => throw null;
    }
}

class Program
{
    static void Main() {}
    
    Range Test1()
    {
        Span<int> s1 = stackalloc int[10];
        var r1 = new Index(s1)..;
        return r1;
    }

    Range Test2()
    {
        Span<int> s2 = stackalloc int[10];
        var r2 = Range.StartAt(new Index(s2));
        return r2;
    }

    Range Test3()
    {
        Span<int> s3 = stackalloc int[10];
        var r3 = ..new Index(s3);
        return r3;
    }

    Range Test4()
    {
        Span<int> s4 = stackalloc int[10];
        var r4 = Range.EndAt(new Index(s4));
        return r4;
    }

    Range Test5(Span<int> s51)
    {
        Span<int> s52 = stackalloc int[10];
        var r5 = new Index(s51) .. new Index(s52);
        return r5;
    }

    Range Test6(Span<int> s61)
    {
        Span<int> s62 = stackalloc int[10];
        var r6 = new Range(new Index(s61), new Index(s62));
        return r6;
    }

    Range Test7(Span<int> s72)
    {
        Span<int> s71 = stackalloc int[10];
        var r7 = new Index(s71) .. new Index(s72);
        return r7;
    }

    Range Test8(Span<int> s82)
    {
        Span<int> s81 = stackalloc int[10];
        var r8 = new Range(new Index(s81), new Index(s82));
        return r8;
    }

    Range Test9()
    {
        Span<int> s91 = stackalloc int[10];
        Span<int> s92 = stackalloc int[10];
        var r9 = new Index(s91) .. new Index(s92);
        return r9;
    }

    Range Test10()
    {
        Span<int> s101 = stackalloc int[10];
        Span<int> s102 = stackalloc int[10];
        var r10 = new Range(new Index(s101), new Index(s102));
        return r10;
    }

    Range Test11()
    {
        var r11 = Range.All;
        return r11;
    }
}
";
            var comp = CreateCompilationWithSpan(src);
            comp.VerifyDiagnostics(
                // (29,16): error CS8352: Cannot use local 'r1' in this context because it may expose referenced variables outside of their declaration scope
                //         return r1;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r1").WithArguments("r1").WithLocation(29, 16),
                // (36,16): error CS8352: Cannot use local 'r2' in this context because it may expose referenced variables outside of their declaration scope
                //         return r2;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r2").WithArguments("r2").WithLocation(36, 16),
                // (43,16): error CS8352: Cannot use local 'r3' in this context because it may expose referenced variables outside of their declaration scope
                //         return r3;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r3").WithArguments("r3").WithLocation(43, 16),
                // (50,16): error CS8352: Cannot use local 'r4' in this context because it may expose referenced variables outside of their declaration scope
                //         return r4;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r4").WithArguments("r4").WithLocation(50, 16),
                // (57,16): error CS8352: Cannot use local 'r5' in this context because it may expose referenced variables outside of their declaration scope
                //         return r5;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r5").WithArguments("r5").WithLocation(57, 16),
                // (64,16): error CS8352: Cannot use local 'r6' in this context because it may expose referenced variables outside of their declaration scope
                //         return r6;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r6").WithArguments("r6").WithLocation(64, 16),
                // (71,16): error CS8352: Cannot use local 'r7' in this context because it may expose referenced variables outside of their declaration scope
                //         return r7;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r7").WithArguments("r7").WithLocation(71, 16),
                // (78,16): error CS8352: Cannot use local 'r8' in this context because it may expose referenced variables outside of their declaration scope
                //         return r8;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r8").WithArguments("r8").WithLocation(78, 16),
                // (86,16): error CS8352: Cannot use local 'r9' in this context because it may expose referenced variables outside of their declaration scope
                //         return r9;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r9").WithArguments("r9").WithLocation(86, 16),
                // (94,16): error CS8352: Cannot use local 'r10' in this context because it may expose referenced variables outside of their declaration scope
                //         return r10;
                Diagnostic(ErrorCode.ERR_EscapeLocal, "r10").WithArguments("r10").WithLocation(94, 16)
                );
        }
    }
}
