using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests_ListPatterns : PatternMatchingTestBase
    {
        private const string TryGetCountSource_ReturnsTrue = @"
namespace System.Linq
{
    public static class Enumerable
    {
        public static bool TryGetNonEnumeratedCount(System.Collections.IEnumerable list, out int count)
        {   
            count = ((Array)list).Length;
            return true;
        }
    }
}
";

        private const string TryGetCountSource_ReturnsFalse = @"
namespace System.Linq
{
    public static class Enumerable
    {
        public static bool TryGetNonEnumeratedCount(System.Collections.IEnumerable list, out int count)
        {   
            count = default;
            return false;
        }
    }
}
";

        private const string BufferSource = @"
namespace System.Collections.Generic
{
    public class Deque<T>
    {
        private readonly List<T> list;
        private readonly int size;
        public Deque(int size) // fixed size
        {
            this.list = new List<T>(size);
            this.size = size;
        }
        public void Enqueue(T value) // EnqueueHead
        {
            if (list.Count == size)
                list.RemoveAt(0);
            list.Add(value);
        }
        public T Pop() // DequeueTail
        {
            var lastIndex = list.Count - 1;
            var last = list[lastIndex];
            list.RemoveAt(lastIndex);
            return last;
        }
    }
}
";

        [Theory]
        [CombinatorialData]
        public void ListPattern_TryGetCount(
            bool tryGetCountReturns,
            [CombinatorialValues(
                "",
                "{1,..}",
                "{1,2,..}",
                "{1,2,3,..}",
                "{1,2,3}",
                "{..,1,2,3}",
                "{..,2,3}",
                "{..,3}",
                "{..}",
                "{1,..,3}"
            )]
            string list,
            [CombinatorialValues(
                "",
                "[_]",
                "[3]",
                "[<=3]",
                "[>=3]",
                "[>=1 and <=3]",
                "[1 or 2 or 3]"
            )]
            string length)
        {
            if (length == "" && list == "")
                return;
            const string type = "System.Collections.Generic.IEnumerable<int>";
            var source = $@"
System.Console.Write(({type})(new[] {{1,2,3}}) is {length}{list});
" + BufferSource + (tryGetCountReturns ? TryGetCountSource_ReturnsTrue : TryGetCountSource_ReturnsFalse);
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"True";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_Matches(
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<int>",
                "int[]"
            )]
            string type,
            [CombinatorialValues(
                "",
                "{1,..}",
                "{1,2,..}",
                "{1,2,3,..}",
                "{1,2,3}",
                "{..,1,2,3}",
                "{..,2,3}",
                "{..,3}",
                "{..}",
                "{1,..,3}"
            )]
            string list,
            [CombinatorialValues(
                "",
                "[_]",
                "[3]",
                "[<=3]",
                "[>=3]",
                "[>=1 and <=3]",
                "[1 or 2 or 3]"
            )]
            string length)
        {
            if (length == "" && list == "")
                return;
            var source = $@"
System.Console.Write(({type})(new[] {{1,2,3}}) is {length}{list});
" + BufferSource;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"True";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Fact]
        public void ListPattern_Span()
        {
            var source = @"
using System;
public class X
{
    void M(System.Collections.Generic.IEnumerable<int> a) {
        foreach (var item in a){}
    }
    static bool IsSymmetric(Span<char> span)
    {
        switch (span)
        {
            case []{}:
            case {_}:
              return true;
            case {var first, ..var others, var last} when first == last:
              return IsSymmetric(others);
            default:
              return false;
        }
    }
    static void Check(int num)
    {
        Console.Write(IsSymmetric(num.ToString().ToCharArray()) ? 1 : 0);
    }
    public static void Main()
    {
        Check(1);
        Check(11);
        Check(12);
        Check(123);
        Check(121);
        Check(1221);
        Check(1222);
    }
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "1100110").VerifyIL("X.IsSymmetric", @"
{
  // Code size       76 (0x4c)
  .maxstack  4
  .locals init (char V_0, //first
              System.Span<char> V_1, //others
              char V_2, //last
              System.Span<char> V_3,
              int V_4)
  IL_0000:  ldarg.0
  IL_0001:  stloc.3
  IL_0002:  ldloca.s   V_3
  IL_0004:  call       ""int System.Span<char>.Length.get""
  IL_0009:  stloc.s    V_4
  IL_000b:  ldloc.s    V_4
  IL_000d:  ldc.i4.2
  IL_000e:  bge.s      IL_0017
  IL_0010:  ldloc.s    V_4
  IL_0012:  ldc.i4.1
  IL_0013:  ble.un.s   IL_003d
  IL_0015:  br.s       IL_004a
  IL_0017:  ldloca.s   V_3
  IL_0019:  ldc.i4.0
  IL_001a:  call       ""ref char System.Span<char>.this[int].get""
  IL_001f:  ldind.u2
  IL_0020:  stloc.0
  IL_0021:  ldloca.s   V_3
  IL_0023:  ldc.i4.1
  IL_0024:  ldloc.s    V_4
  IL_0026:  ldc.i4.2
  IL_0027:  sub
  IL_0028:  call       ""System.Span<char> System.Span<char>.Slice(int, int)""
  IL_002d:  stloc.1
  IL_002e:  ldloca.s   V_3
  IL_0030:  ldloc.s    V_4
  IL_0032:  ldc.i4.1
  IL_0033:  sub
  IL_0034:  call       ""ref char System.Span<char>.this[int].get""
  IL_0039:  ldind.u2
  IL_003a:  stloc.2
  IL_003b:  br.s       IL_003f
  IL_003d:  ldc.i4.1
  IL_003e:  ret
  IL_003f:  ldloc.0
  IL_0040:  ldloc.2
  IL_0041:  bne.un.s   IL_004a
  IL_0043:  ldloc.1
  IL_0044:  call       ""bool X.IsSymmetric(System.Span<char>)""
  IL_0049:  ret
  IL_004a:  ldc.i4.0
  IL_004b:  ret
}");
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_Trailing_01(
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<int>",
                "int[]"
            )]
            string type)
        {
            var source = @"
using System;

Console.Write(Match(new[]{0,1,2,3,1,2,3,4}));
Console.Write(Match(new[]{0,1,2,3,4,1,2,3}));
Console.Write(Match(new[]{1,2,3,1,2,3}));
Console.Write(Match(new[]{1,2,3}));
Console.Write(Match(new[]{1}));

static int Match(TYPE array) => array switch
{
    { 0       ,.., 1, 2, 3, 4 } => 1,
    { 0       ,..,    1, 2, 3 } => 2,
    { 1, 2, 3 ,..,    1, 2, 3 } => 3,
    {          ..,    1, 2, 3 } => 4,
    _ => 0
};
".Replace("TYPE", type) + BufferSource;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"12340";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_Trailing_02(
            [CombinatorialValues(-1,1,0)] int tryCountReturns,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<int>",
                "int[]"
            )]
            string type)
        {
            var source = @"
using System;

Console.Write(Match1(new int[]{}));    
Console.Write(Match1(new[]{0}));
Console.Write(Match1(new[]{1}));
Console.Write(Match1(new[]{0,3}));
Console.Write(Match1(new[]{0,4}));
Console.Write(Match1(new[]{1,3}));
Console.Write(Match1(new[]{1,4}));
Console.Write(Match1(new[]{1,4,3}));
Console.Write(Match1(new[]{0,3,4}));

Console.Write(Match2(new int[]{}));    
Console.Write(Match2(new[]{0}));
Console.Write(Match2(new[]{1}));
Console.Write(Match2(new[]{0,3}));
Console.Write(Match2(new[]{0,4}));
Console.Write(Match2(new[]{1,3}));
Console.Write(Match2(new[]{1,4}));
Console.Write(Match2(new[]{1,4,3}));
Console.Write(Match2(new[]{0,3,4}));

static int Match1(TYPE array) => array switch
{
    { 1 ,.., 4 } => 2,
    { 0 ,.., 3 } => 1,
    _ => 0
};
static int Match2(TYPE array) => array switch
{
    { 0 ,.., 3 } => 1,
    { 1 ,.., 4 } => 2,
    _ => 0
};
".Replace("TYPE", type) + BufferSource + (tryCountReturns == -1 ? null : tryCountReturns == 1 ? TryGetCountSource_ReturnsTrue : TryGetCountSource_ReturnsFalse);
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"000100200000100200";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_Trailing_03(
            [CombinatorialValues(-1,1,0)] int tryCountReturns,
            [CombinatorialValues(
                "System.Collections.Generic.IEnumerable<int>",
                "int[]"
            )]
            string type)
        {
            var source = @"
using System;

Console.Write(Match1(new[]{0,1,2,3}));    
Console.Write(Match1(new[]{0,1,2,3,1,2,3}));
Console.Write(Match2(new[]{0,1,2,3}));    
Console.Write(Match2(new[]{0,1,2,3,1,2,3}));

Console.Write(Match1(new[]{0,1,2,3,4}));    
Console.Write(Match1(new[]{0,1,2,3,1,2,3,4}));
Console.Write(Match2(new[]{0,1,2,3,4}));    
Console.Write(Match2(new[]{0,1,2,3,1,2,3,4}));
static int Match1(TYPE array) => array switch
{
    { 0,.., 1, 2, 3, 4 } => 2,
    { 0,..,    1, 2, 3 } => 1,
    
    _ => 0
};
static int Match2(TYPE array) => array switch
{
    { 0,.., 1, 2, 3 } => 1, 
    { 0,.., 1, 2, 3, 4 } => 2,
    _ => 0
};
".Replace("TYPE", type) + BufferSource + (tryCountReturns == -1 ? null : tryCountReturns == 1 ? TryGetCountSource_ReturnsTrue : TryGetCountSource_ReturnsFalse);
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"11112222";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        [Theory]
        [CombinatorialData]
        public void ListPattern_Trailing_04(
            [CombinatorialValues(-1,1,0)] int tryCountReturns)
        {
            var source = @"
_ = Match1(null);
static int Match1(System.Collections.Generic.IEnumerable<int> array) => array switch
{
    { .., 1, 2 } => 2,
    { 1, 2, .. } => 1,
    _ => 0
};
" + BufferSource + (tryCountReturns == -1 ? null : tryCountReturns == 1 ? TryGetCountSource_ReturnsTrue : TryGetCountSource_ReturnsFalse);;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                // (6,5): error CS8510: The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.
                //     { 1, 2, .. } => 1,
                Diagnostic(ErrorCode.ERR_SwitchArmSubsumed, "{ 1, 2, .. }").WithLocation(6, 5)
                );
        }

        [Fact]
        public void ListPattern_Enumerable()
        {
            var source = @"
using System;

Console.Write(Match(new int[] { }));
Console.Write(Match(new int[] { 1 }));
Console.Write(Match(new int[] { 1, 2 }));
Console.Write(Match(new int[] { 1, 2, 3 }));
Console.Write(Match(new int[] { 1, 2, 3, 4 }));

static int Match(Array array) => array switch
{
    []{} => 0,
    {1} => 1,
    {1,2} => 2,
    {1,2,3} => 3,
    _ => 4
};
" + BufferSource;
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"01234";
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }
    }
}
