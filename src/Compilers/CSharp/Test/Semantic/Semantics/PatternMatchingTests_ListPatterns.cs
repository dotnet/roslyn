using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests_ListPatterns : PatternMatchingTestBase
    {
        [Fact]
        public void ListPattern_Span()
        {
            var source = @"
using System;
public class X
{
    static bool IsSymmetric(Span<char> span)
    {
        switch (span)
        {
            case [0]:
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
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
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
        // Index
        [InlineData("[Index i]")]
        [InlineData("[Index i, int ignored = 0]")]
        [InlineData("[Index i, params int[] ignored]")]
        [InlineData("[params Index[] i]")]
        // int
        [InlineData("[int i]")]
        [InlineData("[int i, int ignored = 0]")]
        [InlineData("[int i, params int[] ignored]")]
        [InlineData("[params int[] i]")]
        // long
        [InlineData("[long i]")]
        [InlineData("[long i, int ignored = 0]")]
        [InlineData("[long i, params int[] ignored]")]
        [InlineData("[params long[] i]")]
        public void ListPattern_IndexIndexerPattern(string indexer)
        {
            var source = @"
using System;
class X
{
    public int this" + indexer + @"
    {
        get
        {
            i.ToString(); // verify argument is usable
            Console.Write(""this[] "");
            return 1;
        }
    }
    public int Length => 1;
    public static void Main()
    {
        Console.Write(new X() is { 1 });
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "this[] True");
        }

        [Theory]
        // Range
        [InlineData("[Range i]")]
        [InlineData("[Range i, int ignored = 0]")]
        [InlineData("[Range i, params int[] ignored]")]
        [InlineData("[params Range[] i]")]
        public void ListPattern_ExplicitRangeIndexerPattern(string indexer)
        {
            var source = @"
using System;
class X
{
    public int this" + indexer + @"
    {
        get
        {
            i.ToString(); // verify argument is usable
            Console.Write(""this[] "");
            return 1;
        }
    }
    public int this[int i] => throw new();
    public int Count => 1;
    public static void Main()
    {
        Console.Write(new X() is { .. 1 });
    } 
}
";
            var compilation = CreateCompilationWithIndexAndRange(source, parseOptions: TestOptions.RegularWithListPatterns, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "this[] True");
        }
    }
}
