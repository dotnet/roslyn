// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class ForeachTest : EmitMetadataTestBase
    {
        // The loop object must be an array or an object collection
        [Fact]
        public void SimpleLoop()
        {
            var text =
@"
using System;
public class Test
{
    static void Main(string[] args)
    {
        string[] arr = new string[4]; // Initialize
        arr[0] = ""one"";               // Element 1
        arr[1] = ""two"";               // Element 2
        arr[2] = ""three"";             // Element 3
        arr[3] = ""four"";              // Element 4
        foreach (string s in arr)
        {
            System.Console.WriteLine(s);
        }
    }
}
";
            string expectedOutput = @"one
two
three
four";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestIteration()
        {
            CompileAndVerify(@"
using System;
public class Test
{
    public static void Main(string[] args)
    {
        unsafe
        {
            int* y = null;
            foreach (var x in new int*[] { y }) { }
        }
    }
}", options: TestOptions.UnsafeReleaseDll).VerifyIL("Test.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  4
  .locals init (int* V_0, //y
  int*[] V_1,
  int V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  conv.u
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  newarr     ""int*""
  IL_0009:  dup
  IL_000a:  ldc.i4.0
  IL_000b:  ldloc.0
  IL_000c:  stelem.i
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.2
  IL_0010:  br.s       IL_001a
  IL_0012:  ldloc.1
  IL_0013:  ldloc.2
  IL_0014:  ldelem.i
  IL_0015:  pop
  IL_0016:  ldloc.2
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  stloc.2
  IL_001a:  ldloc.2
  IL_001b:  ldloc.1
  IL_001c:  ldlen
  IL_001d:  conv.i4
  IL_001e:  blt.s      IL_0012
  IL_0020:  ret
}");
        }

        // Using the Linq as iteration variable
        [Fact]
        public void TestLinqInForeach()
        {
            var text =
@"using System;
using System.Linq;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int x in from char c in ""abc"" select c)
        {
            Console.WriteLine(x);
        }
    }
}";
            string expectedOutput = @"97
98
99";
            CompileAndVerify(text, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: expectedOutput);
        }

        // Empty foreach statement
        [Fact]
        public void TestEmptyStatementForeach()
        {
            var text =
@"class C
{
    static void Main()
    {
        foreach (char C in ""abc"");
    }
}";
            string expectedIL = @"{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldstr      ""abc""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0016
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  pop
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  stloc.1
  IL_0016:  ldloc.1
  IL_0017:  ldloc.0
  IL_0018:  callvirt   ""int string.Length.get""
  IL_001d:  blt.s      IL_000a
  IL_001f:  ret
}";
            CompileAndVerify(text).VerifyIL("C.Main", expectedIL);
        }

        // Foreach value can't be deleted in a loop
        [Fact]
        public void TestRemoveValueInForeach()
        {
            var text =
@"using System.Collections;
using System.Collections.Generic;

class C
{
    static public void Main()
    {
        List<int> arrInt = new List<int>();
        arrInt.Add(1);
        foreach (int i in arrInt)
        {
            arrInt.Remove(i);//It will generate error in run-time
        }
    }
}
";
            string expectedIL = @"{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (System.Collections.Generic.List<int> V_0, //arrInt
           System.Collections.Generic.List<int>.Enumerator V_1,
           int V_2) //i
  IL_0000:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_0005:  stloc.0   
  IL_0006:  ldloc.0   
  IL_0007:  ldc.i4.1  
  IL_0008:  callvirt   ""void System.Collections.Generic.List<int>.Add(int)""
  IL_000d:  ldloc.0   
  IL_000e:  callvirt   ""System.Collections.Generic.List<int>.Enumerator System.Collections.Generic.List<int>.GetEnumerator()""
  IL_0013:  stloc.1   
  .try
  {
    IL_0014:  br.s       IL_0026
    IL_0016:  ldloca.s   V_1
    IL_0018:  call       ""int System.Collections.Generic.List<int>.Enumerator.Current.get""
    IL_001d:  stloc.2   
    IL_001e:  ldloc.0   
    IL_001f:  ldloc.2   
    IL_0020:  callvirt   ""bool System.Collections.Generic.List<int>.Remove(int)""
    IL_0025:  pop       
    IL_0026:  ldloca.s   V_1
    IL_0028:  call       ""bool System.Collections.Generic.List<int>.Enumerator.MoveNext()""
    IL_002d:  brtrue.s   IL_0016
    IL_002f:  leave.s    IL_003f
  }
  finally
  {
    IL_0031:  ldloca.s   V_1
    IL_0033:  constrained. ""System.Collections.Generic.List<int>.Enumerator""
    IL_0039:  callvirt   ""void System.IDisposable.Dispose()""
    IL_003e:  endfinally
  }
  IL_003f:  ret       
}";
            CompileAndVerify(text).VerifyIL("C.Main", expectedIL);
        }

        // With multidimensional arrays, you can use one loop to iterate through the elements
        [Fact]
        public void TestMultiDimensionalArray()
        {
            var text =
@"class T
{
    static public void Main()
    {
        int[,] numbers2D = new int[3, 2] { { 9, 99 }, { 3, 33 }, { 5, 55 } };
        foreach (int i in numbers2D)
        {
            System.Console.WriteLine(i);
        }
    }
}
";
            string expectedOutput = @"9
99
3
33
5
55";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [WorkItem(540917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540917")]
        [Fact]
        public void TestArray()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        string[] arr = new string[4]; // Initialize
        arr[0] = ""one"";               // Element 1
        arr[1] = ""two"";               // Element 2
        foreach (string s in arr)
        {
            System.Console.WriteLine(s);
        }
    }
}
";
            string expectedIL = @"
{
  // Code size       46 (0x2e)
  .maxstack  4
  .locals init (string[] V_0,
  int V_1)
  IL_0000:  ldc.i4.4
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""one""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""two""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_0027
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  call       ""void System.Console.WriteLine(string)""
  IL_0023:  ldloc.1
  IL_0024:  ldc.i4.1
  IL_0025:  add
  IL_0026:  stloc.1
  IL_0027:  ldloc.1
  IL_0028:  ldloc.0
  IL_0029:  ldlen
  IL_002a:  conv.i4
  IL_002b:  blt.s      IL_001b
  IL_002d:  ret
}";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        // Traveled Multi-dimensional jagged arrays 
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestJaggedArray()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        int[][] arr = new int[][] { new int[] { 1, 2 }, new int[] { 4, 5, 6 } };
        foreach (int[] outer in arr)
        {
            foreach (int i in outer)
            {
                Console.WriteLine(i);
            }
        }
    }
}
";
            string expectedOutput = @"1
2
4
5
6";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Optimization to foreach (char c in String) by treating String as a char array 
        [Fact]
        public void TestString01()
        {
            var text =
@"using System;
public class Test
{
    static void Main(string[] args)
    {
        System.String Str = new System.String('\0', 1024);
        foreach (char C in Str) { }
    }
}
";
            string expectedOutput = @"";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestString02()
        {
            var text =
@"using System;
public class Test
{
    static public int Main(string[] args)
    {
        foreach (var var in ""foo"")
        {
            if (!var.GetType().Equals(typeof(char)))
            {
                System.Console.WriteLine(-1);
                return -1;
            }
        }
        return 0;
    }
}
";
            string expectedOutput = @"";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestString03()
        {
            var text =
@"using System;
public class Test
{
    static public void Main(string[] args)
    {
        String Str = null;
        foreach (char C in Str) { }
    }
}
";
            string expectedIL = @"{
  // Code size       28 (0x1c)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  br.s       IL_0012
  IL_0006:  ldloc.0
  IL_0007:  ldloc.1
  IL_0008:  callvirt   ""char string.this[int].get""
  IL_000d:  pop
  IL_000e:  ldloc.1
  IL_000f:  ldc.i4.1
  IL_0010:  add
  IL_0011:  stloc.1
  IL_0012:  ldloc.1
  IL_0013:  ldloc.0
  IL_0014:  callvirt   ""int string.Length.get""
  IL_0019:  blt.s      IL_0006
  IL_001b:  ret
}";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        // Traversing items in 'Dictionary'
        [Fact]
        public void TestDictionary()
        {
            var text =
@"using System;
using System.Collections.Generic;
public class Test
{
    static public void Main(string[] args)
    {
        Dictionary<int, int> s = new Dictionary<int, int>();
        s.Add(1, 2);
        s.Add(2, 3);
        s.Add(3, 4);
        foreach (var pair in s) { Console.WriteLine( pair.Key );}
        foreach (KeyValuePair<int, int> pair in s) {Console.WriteLine( pair.Value ); }
    }
}
";
            string expectedOutput = @"1
2
3
2
3
4";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Inner foreach loop referencing the outer foreach loop iteration variable
        [Fact]
        public void TestNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
            }
        }
    }
}
";
            string expectedOutput = @"A
B
C
X
Y
Z";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Breaking from nested Loops
        [Fact]
        public void TestBreakInNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                if (y == 'A')
                    break;
                else 
                    System.Console.WriteLine(y);
            }
            System.Console.WriteLine(x);
        }
    }
}
";
            string expectedOutput = @"ABC
X
Y
Z
XYZ";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Continuing  from nested Loops
        [Fact]
        public void TestContinueInNestedLoop()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                if (y == 'C')
                    continue;
                System.Console.WriteLine(y);
            }
        }
    }
}
";
            string expectedOutput = @"A
B
X
Y
Z";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // Goto in foreach loops
        [Fact]
        public void TestGoto01()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
                goto stop;
            }
        }
    stop:
        return;
    }
}
";
            string expectedOutput = @"A";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestGoto02()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            foreach (char y in x)
            {
                System.Console.WriteLine(y);
                goto outerLoop;
            }
        outerLoop:
            return;
        }
    }
}
";
            string expectedOutput = @"A";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // 'Return' in foreach
        [Fact]
        public void TestReturn()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (string x in S)
        {
            return;
        }
    }
}
";
            string expectedIL = @"{
  // Code size       39 (0x27)
  .maxstack  4
  .locals init (string[] V_0,
  int V_1)
  IL_0000:  ldc.i4.2
  IL_0001:  newarr     ""string""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldstr      ""ABC""
  IL_000d:  stelem.ref
  IL_000e:  dup
  IL_000f:  ldc.i4.1
  IL_0010:  ldstr      ""XYZ""
  IL_0015:  stelem.ref
  IL_0016:  stloc.0
  IL_0017:  ldc.i4.0
  IL_0018:  stloc.1
  IL_0019:  br.s       IL_0020
  IL_001b:  ldloc.0
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  pop
  IL_001f:  ret
  IL_0020:  ldloc.1
  IL_0021:  ldloc.0
  IL_0022:  ldlen
  IL_0023:  conv.i4
  IL_0024:  blt.s      IL_001b
  IL_0026:  ret
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", expectedIL);
        }

        // Dynamic works in foreach 
        [Fact]
        public void TestDynamic()
        {
            var text =
@"public class Test
{
    static public void Main(string[] args)
    {
        string[] S = new string[] { ""ABC"", ""XYZ"" };
        foreach (dynamic x in S)
        {
            System.Console.WriteLine(x.ToLower());
        }
    }
}
";
            string expectedOutput = @"abc
xyz";
            CompileAndVerify(text, additionalRefs: new[] { SystemCoreRef, CSharpRef }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar01()
        {
            var text =
@"using System.Collections.Generic;
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new List<double> { 1.0, 2.0, 3.0 })
        {
            if (var.GetType().Equals(typeof(double)))
            {
                System.Console.WriteLine(true);
            }
        }
    }
}
";
            string expectedOutput = @"True
True
True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar02()
        {
            var text =
@"
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new string[] { ""one"", ""two"", ""three"" })
        {
            if (!var.GetType().Equals(typeof(double)))
            {
                System.Console.WriteLine(false);
            }
        }
    }
}
";
            string expectedOutput = @"False
False
False";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestVar03()
        {
            var text =
@"
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var var in new MyClass())
        {
            if (var.GetType().Equals(typeof(int)))
            {
                System.Console.WriteLine(true);
            }
        }
    }
}
class MyClass
{
    public MyEnumerator GetEnumerator()
    {
        return new MyEnumerator();
    }
}
class MyEnumerator
{
    int count = 4;
    public int Current
    {
        get
        {
            return count;
        }
    }
    public bool MoveNext()
    {
        count--;
        return count != 0;
    }
}
";
            string expectedOutput = @"True
True
True";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestQuery()
        {
            var text =
@"
using System.Linq;
public class Test
{
    static public void Main(string[] args)
    {
        foreach (var x in from x in new[] { 'A', 'B', 'C' }
                          let z = x.ToString()
                          select z into w
                          select w)
        {
            System.Console.WriteLine(x.ToLower());
        }
    }
}
";
            string expectedOutput = @"a
b
c
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(text, options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield01()
        {
            var text =
@"
using System.Collections;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int i in myClass.Power(2, 8))
        {
            System.Console.WriteLine(""{0}"", i);
        }
    }
}

public class myClass
{
    public static IEnumerable Power(int number, int exponent)
    {
        int counter = 0;
        int result = 1;
        while (counter++ < exponent)
        {
            result = result * number;
            yield return result;
        }
    }
}
";
            string expectedOutput = @"2
4
8
16
32
64
128
256
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield02()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int i in FromTo(2,4))
        {
            System.Console.WriteLine(""{0}"", i);
        }
    }
    public static IEnumerable<int> FromTo(int from, int to)
    {
        for (int i = from; i <= to; i++) yield return i;
    }
}
";
            string expectedOutput = @"2
3
4
";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield03()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (var i in EnumerateIt<string>(new List<string>() { ""abc"" }))
        {
            System.Console.WriteLine(i);
        }
    }
    public static IEnumerable<T> EnumerateIt<T>(IEnumerable<T> xs)
    {
        foreach (T x in xs) yield return x;
    }
}
";
            string expectedOutput = @"abc";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield04()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (int p in EnumerateIt(FromTo(3, 5)))
        {
            System.Console.WriteLine(""{0}"", p);
        }
    }
    public static IEnumerable<int> FromTo(int from, int to)
    {
        for (int i = from; i <= to; i++) yield return i;
    }

    public static IEnumerable<T> EnumerateIt<T>(IEnumerable<T> xs)
    {
        foreach (T x in xs) yield return x;
    }
}
";
            string expectedOutput = @"3
4
5";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestYield05()
        {
            var text =
@"
using System.Collections.Generic;
public class Test
{
    public static void Main(string[] args)
    {
        foreach (var j in new Gen<double>()) { System.Console.WriteLine(j); }
    }
}
public class Gen<T> where T : new()
{
    public IEnumerator<T> GetEnumerator()
    {
        yield return new T();
        yield return new T();
        yield return new T();
    }
}
";
            string expectedOutput = @"0
0
0";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestValueTypeIterationVariableCanBeMutatedByInstanceMethods()
        {
            const string source = @"
struct A
{
    int field;

    void Set(A a)
    {
        this = a;
    }

    static void Main()
    {
        foreach (var a in new A[1])
        {
            a.Set(new A { field = 5 });
            System.Console.Write(a.field);
        }
    }  
}";

            CompileAndVerify(source, expectedOutput: "5");
        }


        [Fact, WorkItem(1077204, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077204")]
        public void TestValueTypeIterationVariableFieldsAreReadonly()
        {
            const string source = @"
using System;

struct A
{
    public B B;

    static void Main()
    {
        A[] array = { default(A) };

        foreach (A a in array)
        {
            a.B.SetField(5);
            Console.Write(a.B.Field);
        }
    }
}

struct B
{
    public int Field;

    public void SetField(int value)
    {
        this.Field = value;
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact]
        public void TestValueTypeIterationVariableFieldsAreReadonly2()
        {
            const string source = @"
struct C
{
    public int field;

    public void SetField(int value)
    {
        field = value;
    }
}

struct B
{
    public C c;
}

struct A
{
    B b;

    static void Main()
    {
        foreach (var a in new A[1])
        {
            a.b.c.SetField(5);
            System.Console.Write(a.b.c.field);
        }
    }
}";

            CompileAndVerify(source, expectedOutput: "0");
        }
    }
}
