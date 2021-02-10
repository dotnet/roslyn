using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class PatternMatchingTests5 : PatternMatchingTestBase
    {
        [Fact(Skip = "PROTOTYPE")]
        public void ListPattern_Array_MDArray()
        {
            var source = @"
using System;
public class X
{
    static int[,] array = {{1, 2, 3},{4, 5, 6}};

    public static void Main()
    {
        Console.Write(array is {{1, 2, 3},{4, 5, 6}});
    }
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True").VerifyIL("X.Main",
@"{
  // Code size      104 (0x68)
  .maxstack  3
  .locals init (int[,] V_0)
  IL_0000:  ldsfld     ""int[,] X.array""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0061
  IL_0009:  ldloc.0
  IL_000a:  ldc.i4.0
  IL_000b:  callvirt   ""int System.Array.GetLength(int)""
  IL_0010:  ldc.i4.2
  IL_0011:  bne.un.s   IL_0061
  IL_0013:  ldloc.0
  IL_0014:  ldc.i4.1
  IL_0015:  callvirt   ""int System.Array.GetLength(int)""
  IL_001a:  ldc.i4.3
  IL_001b:  bne.un.s   IL_0061
  IL_001d:  ldloc.0
  IL_001e:  ldc.i4.0
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""int[*,*].Get""
  IL_0025:  ldc.i4.1
  IL_0026:  bne.un.s   IL_0061
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.0
  IL_002a:  ldc.i4.1
  IL_002b:  call       ""int[*,*].Get""
  IL_0030:  ldc.i4.2
  IL_0031:  bne.un.s   IL_0061
  IL_0033:  ldloc.0
  IL_0034:  ldc.i4.0
  IL_0035:  ldc.i4.2
  IL_0036:  call       ""int[*,*].Get""
  IL_003b:  ldc.i4.3
  IL_003c:  bne.un.s   IL_0061
  IL_003e:  ldloc.0
  IL_003f:  ldc.i4.1
  IL_0040:  ldc.i4.0
  IL_0041:  call       ""int[*,*].Get""
  IL_0046:  ldc.i4.4
  IL_0047:  bne.un.s   IL_0061
  IL_0049:  ldloc.0
  IL_004a:  ldc.i4.1
  IL_004b:  ldc.i4.1
  IL_004c:  call       ""int[*,*].Get""
  IL_0051:  ldc.i4.5
  IL_0052:  bne.un.s   IL_0061
  IL_0054:  ldloc.0
  IL_0055:  ldc.i4.1
  IL_0056:  ldc.i4.2
  IL_0057:  call       ""int[*,*].Get""
  IL_005c:  ldc.i4.6
  IL_005d:  ceq
  IL_005f:  br.s       IL_0062
  IL_0061:  ldc.i4.0
  IL_0062:  call       ""void System.Console.Write(bool)""
  IL_0067:  ret
}");
        }

        [Fact]
        public void ListPattern_Array_SZArray()
        {
            var source = @"
using System;
public class X
{
    static int[] array = {1, 2, 3};
    public static void Main()
    {
        Console.Write(array is {1, 2, 3});
    }
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            CompileAndVerify(compilation, expectedOutput: "True").VerifyIL("X.Main",
@"{
  // Code size       45 (0x2d)
  .maxstack  2
  .locals init (int[] V_0)
  IL_0000:  ldsfld     ""int[] X.array""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  brfalse.s  IL_0026
  IL_0009:  ldloc.0
  IL_000a:  callvirt   ""int System.Array.Length.get""
  IL_000f:  ldc.i4.3
  IL_0010:  bne.un.s   IL_0026
  IL_0012:  ldloc.0
  IL_0013:  ldc.i4.0
  IL_0014:  ldelem.i4
  IL_0015:  ldc.i4.1
  IL_0016:  bne.un.s   IL_0026
  IL_0018:  ldloc.0
  IL_0019:  ldc.i4.1
  IL_001a:  ldelem.i4
  IL_001b:  ldc.i4.2
  IL_001c:  bne.un.s   IL_0026
  IL_001e:  ldloc.0
  IL_001f:  ldc.i4.2
  IL_0020:  ldelem.i4
  IL_0021:  ldc.i4.3
  IL_0022:  ceq
  IL_0024:  br.s       IL_0027
  IL_0026:  ldc.i4.0
  IL_0027:  call       ""void System.Console.Write(bool)""
  IL_002c:  ret
}");
        }

        [Fact]
        public void ListPattern_Span()
        {
            var source =
                @"
using System;
public class X
{
    static bool IsSymmetric(Span<char> span)
    {
        switch (span)
        {
            case {Length:0}:
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
            CompileAndVerify(compilation, expectedOutput: "1100110").VerifyIL("X.IsSymmetric",
@"{
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

        [Fact]
        public void ListPattern_Enumerable_Trailing()
        {
            var source = @"
using System;

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

class X 
{
    public static void Main()
    {
        Print(new[]{1,2,1,2,3});
        Print(new[]{2,1,2,3});
        Print(new[]{1,2,3});
        Print(new[]{2,3});
        Print(new[]{3});
    }
    static void Print(Array array)
    {
        Console.Write(array is {..,1,2,3} ? 1 : 0);
    }
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            string expectedOutput = @"11100";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            return;
            verifier.VerifyIL("X.Print",
@"{
  // Code size      137 (0x89)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                int V_1,
                System.Collections.Generic.Deque<object> V_2,
                object V_3,
                object V_4,
                bool V_5,
                System.IDisposable V_6)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_007e
  IL_0003:  ldc.i4.0
  IL_0004:  stloc.1
  IL_0005:  ldarg.0
  IL_0006:  callvirt   ""System.Collections.IEnumerator System.Array.GetEnumerator()""
  IL_000b:  stloc.0
  .try
  {
    IL_000c:  ldloc.0
    IL_000d:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0012:  brfalse.s  IL_0061
    IL_0014:  newobj     ""System.Collections.Generic.Deque<object>..ctor()""
    IL_0019:  pop
    IL_001a:  ldloc.0
    IL_001b:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0020:  brfalse.s  IL_002e
    IL_0022:  ldloc.1
    IL_0023:  ldc.i4.3
    IL_0024:  blt.s      IL_001a
    IL_0026:  ldloc.0
    IL_0027:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_002c:  brtrue.s   IL_0026
    IL_002e:  ldloc.2
    IL_002f:  callvirt   ""object System.Collections.Generic.Deque<object>.Pop()""
    IL_0034:  stloc.3
    IL_0035:  ldloc.3
    IL_0036:  isinst     ""int""
    IL_003b:  brfalse.s  IL_0061
    IL_003d:  ldloc.3
    IL_003e:  unbox.any  ""int""
    IL_0043:  ldc.i4.3
    IL_0044:  bne.un.s   IL_0061
    IL_0046:  ldloc.2
    IL_0047:  callvirt   ""object System.Collections.Generic.Deque<object>.Pop()""
    IL_004c:  stloc.s    V_4
    IL_004e:  ldloc.s    V_4
    IL_0050:  isinst     ""int""
    IL_0055:  brfalse.s  IL_0061
    IL_0057:  ldloc.s    V_4
    IL_0059:  unbox.any  ""int""
    IL_005e:  ldc.i4.2
    IL_005f:  beq.s      IL_0063
    IL_0061:  leave.s    IL_007e
    IL_0063:  leave.s    IL_0079
  }
  finally
  {
    IL_0065:  ldloc.0
    IL_0066:  isinst     ""System.IDisposable""
    IL_006b:  stloc.s    V_6
    IL_006d:  ldloc.s    V_6
    IL_006f:  brfalse.s  IL_0078
    IL_0071:  ldloc.s    V_6
    IL_0073:  callvirt   ""void System.IDisposable.Dispose()""
    IL_0078:  endfinally
  }
  IL_0079:  ldc.i4.1
  IL_007a:  stloc.s    V_5
  IL_007c:  br.s       IL_0081
  IL_007e:  ldc.i4.0
  IL_007f:  stloc.s    V_5
  IL_0081:  ldloc.s    V_5
  IL_0083:  call       ""void System.Console.Write(bool)""
  IL_0088:  ret
}");
        }

        [Fact]
        public void ListPattern_Enumerable()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        var arrays = new int[][]
        {
            new int[] { }
          , new int[] { 1 }
          , new int[] { 1, 2 }
          , new int[] { 1, 2, 3 }
          , new int[] { 1, 2, 3, 4 }
        };
        foreach (var a in arrays)
        { M1(a); M2(a); }
    }
    static void M1(Array a) => Console.WriteLine(a is {1,2,3});
    static void M2(Array a) => Console.WriteLine(a switch {{Length:0} => 0, {1} => 1, {1,2} => 2, {1,2,3} => 3, _ => 4});
}
";
            var compilation = CreateCompilationWithIndexAndRangeAndSpan(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics();
            var expectedOutput = @"
False
0
False
1
False
2
True
3
False
4
";
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            verifier.VerifyIL("X.M1",
@"{
  // Code size      157 (0x9d)
  .maxstack  2
  .locals init (System.Collections.IEnumerator V_0,
                object V_1,
                object V_2,
                object V_3,
                bool V_4,
                System.IDisposable V_5)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_0092
  IL_0006:  ldarg.0
  IL_0007:  callvirt   ""System.Collections.IEnumerator System.Array.GetEnumerator()""
  IL_000c:  stloc.0
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0013:  brfalse.s  IL_0075
    IL_0015:  ldloc.0
    IL_0016:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_001b:  stloc.1
    IL_001c:  ldloc.1
    IL_001d:  isinst     ""int""
    IL_0022:  brfalse.s  IL_0075
    IL_0024:  ldloc.1
    IL_0025:  unbox.any  ""int""
    IL_002a:  ldc.i4.1
    IL_002b:  bne.un.s   IL_0075
    IL_002d:  ldloc.0
    IL_002e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0033:  brfalse.s  IL_0075
    IL_0035:  ldloc.0
    IL_0036:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_003b:  stloc.2
    IL_003c:  ldloc.2
    IL_003d:  isinst     ""int""
    IL_0042:  brfalse.s  IL_0075
    IL_0044:  ldloc.2
    IL_0045:  unbox.any  ""int""
    IL_004a:  ldc.i4.2
    IL_004b:  bne.un.s   IL_0075
    IL_004d:  ldloc.0
    IL_004e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0053:  brfalse.s  IL_0075
    IL_0055:  ldloc.0
    IL_0056:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_005b:  stloc.3
    IL_005c:  ldloc.3
    IL_005d:  isinst     ""int""
    IL_0062:  brfalse.s  IL_0075
    IL_0064:  ldloc.3
    IL_0065:  unbox.any  ""int""
    IL_006a:  ldc.i4.3
    IL_006b:  bne.un.s   IL_0075
    IL_006d:  ldloc.0
    IL_006e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0073:  brfalse.s  IL_0077
    IL_0075:  leave.s    IL_0092
    IL_0077:  leave.s    IL_008d
  }
  finally
  {
    IL_0079:  ldloc.0
    IL_007a:  isinst     ""System.IDisposable""
    IL_007f:  stloc.s    V_5
    IL_0081:  ldloc.s    V_5
    IL_0083:  brfalse.s  IL_008c
    IL_0085:  ldloc.s    V_5
    IL_0087:  callvirt   ""void System.IDisposable.Dispose()""
    IL_008c:  endfinally
  }
  IL_008d:  ldc.i4.1
  IL_008e:  stloc.s    V_4
  IL_0090:  br.s       IL_0095
  IL_0092:  ldc.i4.0
  IL_0093:  stloc.s    V_4
  IL_0095:  ldloc.s    V_4
  IL_0097:  call       ""void System.Console.WriteLine(bool)""
  IL_009c:  ret
}");

            verifier.VerifyIL("X.M2",
@"{
  // Code size      184 (0xb8)
  .maxstack  2
  .locals init (int V_0,
                System.Collections.IEnumerator V_1,
                object V_2,
                object V_3,
                object V_4,
                System.IDisposable V_5)
  IL_0000:  ldarg.0
  IL_0001:  brfalse    IL_00af
  IL_0006:  ldarg.0
  IL_0007:  callvirt   ""int System.Array.Length.get""
  IL_000c:  brfalse    IL_009f
  IL_0011:  ldarg.0
  IL_0012:  callvirt   ""System.Collections.IEnumerator System.Array.GetEnumerator()""
  IL_0017:  stloc.1
  .try
  {
    IL_0018:  ldloc.1
    IL_0019:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_001e:  brfalse.s  IL_0083
    IL_0020:  ldloc.1
    IL_0021:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0026:  stloc.2
    IL_0027:  ldloc.2
    IL_0028:  isinst     ""int""
    IL_002d:  brfalse.s  IL_0083
    IL_002f:  ldloc.2
    IL_0030:  unbox.any  ""int""
    IL_0035:  ldc.i4.1
    IL_0036:  bne.un.s   IL_0083
    IL_0038:  ldloc.1
    IL_0039:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_003e:  brfalse.s  IL_0085
    IL_0040:  ldloc.1
    IL_0041:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0046:  stloc.3
    IL_0047:  ldloc.3
    IL_0048:  isinst     ""int""
    IL_004d:  brfalse.s  IL_0083
    IL_004f:  ldloc.3
    IL_0050:  unbox.any  ""int""
    IL_0055:  ldc.i4.2
    IL_0056:  bne.un.s   IL_0083
    IL_0058:  ldloc.1
    IL_0059:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_005e:  brfalse.s  IL_0087
    IL_0060:  ldloc.1
    IL_0061:  callvirt   ""object System.Collections.IEnumerator.Current.get""
    IL_0066:  stloc.s    V_4
    IL_0068:  ldloc.s    V_4
    IL_006a:  isinst     ""int""
    IL_006f:  brfalse.s  IL_0083
    IL_0071:  ldloc.s    V_4
    IL_0073:  unbox.any  ""int""
    IL_0078:  ldc.i4.3
    IL_0079:  bne.un.s   IL_0083
    IL_007b:  ldloc.1
    IL_007c:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0081:  brfalse.s  IL_0089
    IL_0083:  leave.s    IL_00af
    IL_0085:  leave.s    IL_00a3
    IL_0087:  leave.s    IL_00a7
    IL_0089:  leave.s    IL_00ab
  }
  finally
  {
    IL_008b:  ldloc.1
    IL_008c:  isinst     ""System.IDisposable""
    IL_0091:  stloc.s    V_5
    IL_0093:  ldloc.s    V_5
    IL_0095:  brfalse.s  IL_009e
    IL_0097:  ldloc.s    V_5
    IL_0099:  callvirt   ""void System.IDisposable.Dispose()""
    IL_009e:  endfinally
  }
  IL_009f:  ldc.i4.0
  IL_00a0:  stloc.0
  IL_00a1:  br.s       IL_00b1
  IL_00a3:  ldc.i4.1
  IL_00a4:  stloc.0
  IL_00a5:  br.s       IL_00b1
  IL_00a7:  ldc.i4.2
  IL_00a8:  stloc.0
  IL_00a9:  br.s       IL_00b1
  IL_00ab:  ldc.i4.3
  IL_00ac:  stloc.0
  IL_00ad:  br.s       IL_00b1
  IL_00af:  ldc.i4.4
  IL_00b0:  stloc.0
  IL_00b1:  ldloc.0
  IL_00b2:  call       ""void System.Console.WriteLine(int)""
  IL_00b7:  ret
}");
        }
    }
}
