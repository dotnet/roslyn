// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.Iterator)]
    public class CodeGenIterators : CSharpTestBase
    {
        [Fact]
        public void TestSimpleIterator01()
        {
            var source =
@"using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static IEnumerator Ints0()
    {
        yield return 1;
        yield return 2;
    }
    static IEnumerator<int> Ints1()
    {
        yield return 3;
        yield return 4;
    }
    static IEnumerable Ints2()
    {
        yield return 5;
        yield return 6;
    }
    static IEnumerable<int> Ints3()
    {
        yield return 7;
        yield return 8;
    }
    static IEnumerable<int> Ints4()
    {
        yield return 9;
        throw new Exception();
    }

    static void Main()
    {
        var e0 = Ints0();
        {
            while (e0.MoveNext())
            {
                Console.Write(e0.Current);
            }
        }
        using (var e1 = Ints1())
        {
            while (e1.MoveNext())
            {
                Console.Write(e1.Current);
            }
        }
        foreach (var i in Ints2())
        {
            Console.Write(i);
        }
        foreach (var i in Ints3())
        {
            Console.Write(i);
        }
        try {
            foreach (var i in Ints4())
            {
                Console.Write(i);
            }
        } catch (Exception) {
            Console.Write('X');
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "123456789X");
        }

        [Fact]
        public void TestSimpleIterator02()
        {
            var source =
@"using System.Collections.Generic;
using System;

class C
{
    static IEnumerable<int> IE()
    {
        {
            int x = 0;
            yield return x++;
            yield return x++;
        }
        {
            int x = 2;
            yield return x++;
            yield return x++;
            {
                int y = 4;
                yield return y++;
                yield return y++;
            }
        }
        for (int i = 6; i < 10; i++) yield return i;
    }

    static void Main(string[] args)
    {
        foreach (var i in IE()) Console.Write(i);
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "0123456789");
        }

        [Fact]
        public void TestSimpleIterator03()
        {
            var source =
@"using System.Collections.Generic;
using System;

struct S
{
    int x;
    public S(int x) { this.x = x; }

    public IEnumerable<T> IE<T>(T i0, T i1)
    {
        Console.Write(x);
        yield return i0;
        yield return i1;
        yield return i0;
    }
}

class C
{
    int x;
    public C(int x) { this.x = x; }

    public IEnumerable<T> IE<T>(T i0, T i1)
    {
        Console.Write(x);
        yield return i0;
        yield return i1;
        yield return i0;
    }
}

class Program
{
    static void Main(string[] args)
    {
        foreach (var i in new S(1).IE(2, 3)) Console.Write(i);
        foreach (var i in new C(4).IE(5, 6)) Console.Write(i);
    }
}";
            var compilation = CompileAndVerifyWithMscorlib40(source, expectedOutput: "12324565");

            compilation.VerifyIL("C.<IE>d__2<T>.System.Collections.Generic.IEnumerable<T>.GetEnumerator()", @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (C.<IE>d__2<T> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int C.<IE>d__2<T>.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0027
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int C.<IE>d__2<T>.<>l__initialThreadId""
  IL_0010:  call       ""System.Threading.Thread System.Threading.Thread.CurrentThread.get""
  IL_0015:  callvirt   ""int System.Threading.Thread.ManagedThreadId.get""
  IL_001a:  bne.un.s   IL_0027
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.0
  IL_001e:  stfld      ""int C.<IE>d__2<T>.<>1__state""
  IL_0023:  ldarg.0
  IL_0024:  stloc.0
  IL_0025:  br.s       IL_003a
  IL_0027:  ldc.i4.0
  IL_0028:  newobj     ""C.<IE>d__2<T>..ctor(int)""
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  ldarg.0
  IL_0030:  ldfld      ""C C.<IE>d__2<T>.<>4__this""
  IL_0035:  stfld      ""C C.<IE>d__2<T>.<>4__this""
  IL_003a:  ldloc.0
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""T C.<IE>d__2<T>.<>3__i0""
  IL_0041:  stfld      ""T C.<IE>d__2<T>.i0""
  IL_0046:  ldloc.0
  IL_0047:  ldarg.0
  IL_0048:  ldfld      ""T C.<IE>d__2<T>.<>3__i1""
  IL_004d:  stfld      ""T C.<IE>d__2<T>.i1""
  IL_0052:  ldloc.0
  IL_0053:  ret
}");
            compilation.VerifyIL("S.<IE>d__2<T>.System.Collections.Generic.IEnumerable<T>.GetEnumerator()", @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (S.<IE>d__2<T> V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int S.<IE>d__2<T>.<>1__state""
  IL_0006:  ldc.i4.s   -2
  IL_0008:  bne.un.s   IL_0027
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""int S.<IE>d__2<T>.<>l__initialThreadId""
  IL_0010:  call       ""System.Threading.Thread System.Threading.Thread.CurrentThread.get""
  IL_0015:  callvirt   ""int System.Threading.Thread.ManagedThreadId.get""
  IL_001a:  bne.un.s   IL_0027
  IL_001c:  ldarg.0
  IL_001d:  ldc.i4.0
  IL_001e:  stfld      ""int S.<IE>d__2<T>.<>1__state""
  IL_0023:  ldarg.0
  IL_0024:  stloc.0
  IL_0025:  br.s       IL_002e
  IL_0027:  ldc.i4.0
  IL_0028:  newobj     ""S.<IE>d__2<T>..ctor(int)""
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  ldarg.0
  IL_0030:  ldfld      ""S S.<IE>d__2<T>.<>3__<>4__this""
  IL_0035:  stfld      ""S S.<IE>d__2<T>.<>4__this""
  IL_003a:  ldloc.0
  IL_003b:  ldarg.0
  IL_003c:  ldfld      ""T S.<IE>d__2<T>.<>3__i0""
  IL_0041:  stfld      ""T S.<IE>d__2<T>.i0""
  IL_0046:  ldloc.0
  IL_0047:  ldarg.0
  IL_0048:  ldfld      ""T S.<IE>d__2<T>.<>3__i1""
  IL_004d:  stfld      ""T S.<IE>d__2<T>.i1""
  IL_0052:  ldloc.0
  IL_0053:  ret
}");
        }

        [Fact]
        public void TestSimpleIterator04()
        {
            var source =
@"using System;
using System.Collections.Generic;

class Program
{
    static IEnumerable<int> Int0()
    {
        yield return 0;
        try
        {
            yield return 1;
            try
            {
                yield return 2;
            }
            finally
            {
                Console.Write('X');
            }
            yield return 3;
            try
            {
                yield return 4;
            }
            finally
            {
                Console.Write('Y');
            }
            yield return 5;
        }
        finally
        {
            Console.Write('Z');
        }
        yield return 6;
    }

    public static void Main(string[] args)
    {
        for (int i = 0; i < 7; i++)
        {
            if (i != 0) Console.Write('|');
            foreach (var j in Int0())
            {
                Console.Write(j);
                if (i == j) break;
            }
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "0|01Z|012XZ|012X3Z|012X34YZ|012X34Y5Z|012X34Y5Z6");

            compilation.VerifyIL("Program.<Int0>d__0.System.Collections.IEnumerator.MoveNext()", @"
{
  // Code size      303 (0x12f)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  switch    (
        IL_0034,
        IL_0050,
        IL_0074,
        IL_0099,
        IL_00b9,
        IL_00db,
        IL_00fb,
        IL_011b)
    IL_002d:  ldc.i4.0
    IL_002e:  stloc.0
    IL_002f:  leave      IL_012d
    IL_0034:  ldarg.0
    IL_0035:  ldc.i4.m1
    IL_0036:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_003b:  ldarg.0
    IL_003c:  ldc.i4.0
    IL_003d:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.1
    IL_0044:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0049:  ldc.i4.1
    IL_004a:  stloc.0
    IL_004b:  leave      IL_012d
    IL_0050:  ldarg.0
    IL_0051:  ldc.i4.m1
    IL_0052:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.s   -4
    IL_005a:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_005f:  ldarg.0
    IL_0060:  ldc.i4.1
    IL_0061:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_0066:  ldarg.0
    IL_0067:  ldc.i4.2
    IL_0068:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_006d:  ldc.i4.1
    IL_006e:  stloc.0
    IL_006f:  leave      IL_012d
    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.s   -4
    IL_0077:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_007c:  ldarg.0
    IL_007d:  ldc.i4.s   -5
    IL_007f:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0084:  ldarg.0
    IL_0085:  ldc.i4.2
    IL_0086:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_008b:  ldarg.0
    IL_008c:  ldc.i4.3
    IL_008d:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0092:  ldc.i4.1
    IL_0093:  stloc.0
    IL_0094:  leave      IL_012d
    IL_0099:  ldarg.0
    IL_009a:  ldc.i4.s   -5
    IL_009c:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00a1:  ldarg.0
    IL_00a2:  call       ""void Program.<Int0>d__0.<>m__Finally2()""
    IL_00a7:  ldarg.0
    IL_00a8:  ldc.i4.3
    IL_00a9:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_00ae:  ldarg.0
    IL_00af:  ldc.i4.4
    IL_00b0:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00b5:  ldc.i4.1
    IL_00b6:  stloc.0
    IL_00b7:  leave.s    IL_012d
    IL_00b9:  ldarg.0
    IL_00ba:  ldc.i4.s   -4
    IL_00bc:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00c1:  ldarg.0
    IL_00c2:  ldc.i4.s   -6
    IL_00c4:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00c9:  ldarg.0
    IL_00ca:  ldc.i4.4
    IL_00cb:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_00d0:  ldarg.0
    IL_00d1:  ldc.i4.5
    IL_00d2:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00d7:  ldc.i4.1
    IL_00d8:  stloc.0
    IL_00d9:  leave.s    IL_012d
    IL_00db:  ldarg.0
    IL_00dc:  ldc.i4.s   -6
    IL_00de:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00e3:  ldarg.0
    IL_00e4:  call       ""void Program.<Int0>d__0.<>m__Finally3()""
    IL_00e9:  ldarg.0
    IL_00ea:  ldc.i4.5
    IL_00eb:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_00f0:  ldarg.0
    IL_00f1:  ldc.i4.6
    IL_00f2:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_00f7:  ldc.i4.1
    IL_00f8:  stloc.0
    IL_00f9:  leave.s    IL_012d
    IL_00fb:  ldarg.0
    IL_00fc:  ldc.i4.s   -4
    IL_00fe:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0103:  ldarg.0
    IL_0104:  call       ""void Program.<Int0>d__0.<>m__Finally1()""
    IL_0109:  ldarg.0
    IL_010a:  ldc.i4.6
    IL_010b:  stfld      ""int Program.<Int0>d__0.<>2__current""
    IL_0110:  ldarg.0
    IL_0111:  ldc.i4.7
    IL_0112:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0117:  ldc.i4.1
    IL_0118:  stloc.0
    IL_0119:  leave.s    IL_012d
    IL_011b:  ldarg.0
    IL_011c:  ldc.i4.m1
    IL_011d:  stfld      ""int Program.<Int0>d__0.<>1__state""
    IL_0122:  ldc.i4.0
    IL_0123:  stloc.0
    IL_0124:  leave.s    IL_012d
  }
  fault
  {
    IL_0126:  ldarg.0
    IL_0127:  call       ""void Program.<Int0>d__0.Dispose()""
    IL_012c:  endfinally
  }
  IL_012d:  ldloc.0
  IL_012e:  ret
}
");
            compilation.VerifyIL("Program.<Int0>d__0.System.IDisposable.Dispose()", @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Int0>d__0.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -6
  IL_000a:  sub
  IL_000b:  ldc.i4.2
  IL_000c:  ble.un.s   IL_0014
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.2
  IL_0010:  sub
  IL_0011:  ldc.i4.4
  IL_0012:  bgt.un.s   IL_004b
  IL_0014:  nop
  .try
  {
    IL_0015:  ldloc.0
    IL_0016:  ldc.i4.s   -5
    IL_0018:  bgt.s      IL_0026
    IL_001a:  ldloc.0
    IL_001b:  ldc.i4.s   -6
    IL_001d:  beq.s      IL_003a
    IL_001f:  ldloc.0
    IL_0020:  ldc.i4.s   -5
    IL_0022:  beq.s      IL_0030
    IL_0024:  leave.s    IL_004b
    IL_0026:  ldloc.0
    IL_0027:  ldc.i4.3
    IL_0028:  beq.s      IL_0030
    IL_002a:  ldloc.0
    IL_002b:  ldc.i4.5
    IL_002c:  beq.s      IL_003a
    IL_002e:  leave.s    IL_004b
    IL_0030:  nop
    .try
    {
      IL_0031:  leave.s    IL_004b
    }
    finally
    {
      IL_0033:  ldarg.0
      IL_0034:  call       ""void Program.<Int0>d__0.<>m__Finally2()""
      IL_0039:  endfinally
    }
    IL_003a:  nop
    .try
    {
      IL_003b:  leave.s    IL_004b
    }
    finally
    {
      IL_003d:  ldarg.0
      IL_003e:  call       ""void Program.<Int0>d__0.<>m__Finally3()""
      IL_0043:  endfinally
    }
  }
  finally
  {
    IL_0044:  ldarg.0
    IL_0045:  call       ""void Program.<Int0>d__0.<>m__Finally1()""
    IL_004a:  endfinally
  }
  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.s   -3
  IL_004e:  stfld      ""int Program.<Int0>d__0.<>1__state""
  IL_0053:  ret
}
");
        }

        [Fact]
        public void TestIteratorWithBaseAccess()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        foreach (string i in new Derived().Iter())
        {
            Console.Write(i);
        }
    }
}

class Base
{
    public virtual string Func()
    {
        return ""Base.Func;"";
    }
}

class Derived: Base
{
    public override string Func()
    {
        return ""Derived.Func;"";
    }

    public IEnumerable<string> Iter()
    {
        yield return base.Func();
        yield return this.Func();
    }
}";
            CompileAndVerify(source, expectedOutput: "Base.Func;Derived.Func;");
        }

        [Fact]
        public void TestIteratorWithBaseAccessInLambda()
        {
            var source = @"
using System;
using System.Collections.Generic;

static class M1
{
    class B1<T>
    {
        public virtual string F<U>(T t, U u)
        {
            return ""B1::F;""; 
        }
    }

    class Outer<V>
    {
        public class B2 : B1<V>
        {
            public override string F<U>(V t, U u)
            {
                return ""B2::F;"";
            }

            public void Test()
            {
                Action m = () =>
                    {
                        foreach (string i in this.Iter())
                        {
                            Console.Write(i);
                        }
                    };
                m();
            }

            public IEnumerable<string> Iter()
            {
                V v = default(V);
                int i = 0;
                string s = null;

                Func<string> f = () => { Func<Func<V, int, string>> ff = () => base.F<int>; return ff()(v, i); };
                yield return f();

                f = () => { Func<Func<V, string, string>> ff = () => this.F<string>; return ff()(v, s); };
                yield return f();

                f = () => { Func<Func<V, int, string>> ff = () => { i++; return base.F<int>; }; return ff()(v, i); };
                yield return f();
            }
        }
    }

    class D<X> : Outer<X>.B2
    {
        public override string F<U>(X t, U u)
        {
            return ""D::F;"";
        }
    }

    static void Main()
    {
        (new D<int>()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F;D::F;B1::F;");
        }

        [Fact]
        public void TestIteratorWithBaseAccessInLambda2()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        foreach (int i in new C().Iter())
        {
            Console.Write(i);
        }
    }
}

class Base
{
    public virtual int Func(int p)
    {
        return p * 444;
    }
}

class C: Base
{
    public override int Func(int p)
    {
        return p * 222;
    }

    public IEnumerable<int> Iter()
    {
        int local = 0;
        Action<int> d = delegate(int jj2)
        {
            local = base.Func(jj2);
        };
        d(1);
        yield return local;

        Func<int> dd = () => {    local++; return base.Func(2);    };
        yield return dd();
    }
}";
            CompileAndVerify(source, expectedOutput: "444888");
        }

        [WorkItem(543165, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543165")]
        [Fact]
        public void TestIteratorWithLambda()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        foreach (int i in new C().Iter())
        {
            Console.Write(i);
        }
    }
}

class C
{
    public IEnumerable<int> Iter()
    {
        int local = 0;
        Action<int> d = delegate(int jj2)
        {
            local = jj2;
        };
        d(3);
        yield return local;

        Func<int> dd = () => {    return 6;    };
        yield return dd();
    }
}
";
            CompileAndVerify(source, expectedOutput: "36");
        }

        [Fact]
        public void Legacy_basic_itr_block010()
        {
            var source = @"using System.Collections.Generic;
using System.Collections;
using System;

class Ien<T> : IEnumerable<T>
{
    List<T> items;

    public Ien(T t0, T t1)
    {
        items = new List<T>();
        items.Add(t0);
        items.Add(t1);
    }

    public IEnumerator<T> GetEnumerator()
    {
        IEnumerator<T> ie = items.GetEnumerator();
        foreach (T t in items) yield return t;
    }

    IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
}

class Program
{
    public static void Main(string[] args)
    {
        var x = new Ien<char>('a', 'b');
        foreach (var c in x) Console.Write(c);
        var y = new Ien<int>(0, 1);
        foreach (var i in y) Console.Write(i);
    }
}";
            CompileAndVerify(source, expectedOutput: "ab01");
        }

        [WorkItem(543178, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543178")]
        [Fact]
        public void TestIteratorWithLambda02()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

public class A
{
    static IEnumerable<Func<U>> Iter<T, U>(T t) where T : IEnumerable
    {
        foreach (U c in t)
        {
            yield return (Func<U>)delegate
            {
                return c;
            };
        }
    }

    static void Main()
    {
        foreach (var item in Iter<string, char>(""abc""))
        {
            Console.Write(item());
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "abc");
        }

        [WorkItem(543373, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543373")]
        [Fact]
        public void TestIteratorWithNestedForEachAndThrow()
        {
            var source = @"
using System.Collections.Generic;
using System;

public class Test
{
    static IEnumerable<int> GetInts()
    {
        foreach (int i in new MyEnumerable(""Outer""))
        {
            foreach (int j in new MyEnumerable(""Inner""))
            {
                yield return 1;
            }
            throw new Exception(""ExInner"");
        }
    }

    public static string result;
    public static void Main()
    {
        try
        {
            foreach (int i in GetInts()) { }
        }
        catch (Exception e)
        {
            result += e.Message;
        }
        // Expect: inner, outer, ExInner
        Console.WriteLine(result);
    }
}

public class MyEnumerable : IEnumerable<int>
{
    string name;
    public MyEnumerable(string name)  {    this.name = name;    }
    public IEnumerator<int> GetEnumerator()  {    return new MyEnumerator(name);    }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()  {    return new MyEnumerator(name);    }
}

public class MyEnumerator : IEnumerator<int>
{
    int[] collection = new int[] { 1 };
    int index = -1;
    string name;

    public MyEnumerator(string name)  {    this.name = name;    }
    public void Dispose()  {    Test.result += name;    }

    bool System.Collections.IEnumerator.MoveNext()
    {
        if (index == 0)
        {
            index = -1;
        }
        else
        {
            index++;
        }
        return index != -1;
    }

    int IEnumerator<int>.Current { get { return collection[index]; } }
    object System.Collections.IEnumerator.Current { get { return collection[index]; } }
    void System.Collections.IEnumerator.Reset() { index = -1; }
}
";
            CompileAndVerify(source, expectedOutput: "InnerOuterExInner");
        }

        [WorkItem(543542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543542")]
        [Fact]
        public void TestIteratorWithSwitchBreak()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Test
{
    static void Main()
    {
        foreach (int i in Iter2(1))
        {
            Console.Write(i);
        }
    }

    static public IEnumerable<int> Iter2(int x)
    {
        foreach (int i in new int[] { 1, 2, 3 })
        {
            switch (x)
            {
                default:
                    {
                        yield return x + i;
                    }
                    break;
            }
        }
    } // Iter2
}
";
            CompileAndVerify(source, expectedOutput: "234");
        }

        [WorkItem(546128, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546128")]
        [Fact]
        public void TestIteratorWithCapturedStruct()
        {
            var source =
@"using System;
using System.Collections.Generic;

class Program
{
    public static void Main(string[] args)
    {
        foreach (var b in Blend(3))
        {
            Console.Write(b.N);
        }
    }

    static private IEnumerable<B> Blend(int limit)
    {
        int n = limit;
        B result;
        do
        {
            result = new B(n);
            yield return result;
            n = n - 1;
        }
        while (result.N > 0);
    }
}

struct B
{
    public int N;
    public B(int n) { this.N = n; }
}";
            CompileAndVerify(source, expectedOutput: "3210");
        }

        [Fact, WorkItem(544908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544908")]
        public void TestIteratorWithNullableAsCollectionVariable_NonNull()
        {
            var source = @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        S? s = new S();
        int i = 0;
        foreach (object x in s)
        {
            i++;
        }
        Console.Write(i);
    }
}

struct S : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return """";
    }
}
";
            CompileAndVerify(source, expectedOutput: "1").VerifyIL("Program.Main", @"
{
  // Code size       90 (0x5a)
  .maxstack  2
  .locals init (S? V_0, //s
  int V_1, //i
  S V_2,
  System.Collections.IEnumerator V_3,
  System.IDisposable V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldloca.s   V_2
  IL_0004:  initobj    ""S""
  IL_000a:  ldloc.2
  IL_000b:  call       ""S?..ctor(S)""
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.1
  IL_0012:  ldloca.s   V_0
  IL_0014:  call       ""S S?.Value.get""
  IL_0019:  stloc.2
  IL_001a:  ldloca.s   V_2
  IL_001c:  constrained. ""S""
  IL_0022:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_0027:  stloc.3
  .try
{
  IL_0028:  br.s       IL_0035
  IL_002a:  ldloc.3
  IL_002b:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0030:  pop
  IL_0031:  ldloc.1
  IL_0032:  ldc.i4.1
  IL_0033:  add
  IL_0034:  stloc.1
  IL_0035:  ldloc.3
  IL_0036:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_003b:  brtrue.s   IL_002a
  IL_003d:  leave.s    IL_0053
}
  finally
{
  IL_003f:  ldloc.3
  IL_0040:  isinst     ""System.IDisposable""
  IL_0045:  stloc.s    V_4
  IL_0047:  ldloc.s    V_4
  IL_0049:  brfalse.s  IL_0052
  IL_004b:  ldloc.s    V_4
  IL_004d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0052:  endfinally
}
  IL_0053:  ldloc.1
  IL_0054:  call       ""void System.Console.Write(int)""
  IL_0059:  ret
}");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(544908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544908")]
        public void TestIteratorWithNullableAsCollectionVariable_Null()
        {
            var source = @"
using System;
using System.Collections;

class Program
{
    static void Main()
    {
        System.Globalization.CultureInfo saveUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            Test();
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = saveUICulture;
        }
    }

    static void Test()
    {
        S? s = null;
        int i = 0;
        foreach (object x in s)
        {
            i++;
        }
        Console.Write(i);
    }
}

struct S : IEnumerable
{
    IEnumerator IEnumerable.GetEnumerator()
    {
        yield return """";
    }
}
";
            CompileAndVerifyException<InvalidOperationException>(source, expectedMessage: "Nullable object must have a value.").
                VerifyIL("Program.Test", @"
{
  // Code size       82 (0x52)
  .maxstack  2
  .locals init (S? V_0, //s
  int V_1, //i
  System.Collections.IEnumerator V_2,
  S V_3,
  System.IDisposable V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""S?""
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  IL_000a:  ldloca.s   V_0
  IL_000c:  call       ""S S?.Value.get""
  IL_0011:  stloc.3
  IL_0012:  ldloca.s   V_3
  IL_0014:  constrained. ""S""
  IL_001a:  callvirt   ""System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()""
  IL_001f:  stloc.2
  .try
{
  IL_0020:  br.s       IL_002d
  IL_0022:  ldloc.2
  IL_0023:  callvirt   ""object System.Collections.IEnumerator.Current.get""
  IL_0028:  pop
  IL_0029:  ldloc.1
  IL_002a:  ldc.i4.1
  IL_002b:  add
  IL_002c:  stloc.1
  IL_002d:  ldloc.2
  IL_002e:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
  IL_0033:  brtrue.s   IL_0022
  IL_0035:  leave.s    IL_004b
}
  finally
{
  IL_0037:  ldloc.2
  IL_0038:  isinst     ""System.IDisposable""
  IL_003d:  stloc.s    V_4
  IL_003f:  ldloc.s    V_4
  IL_0041:  brfalse.s  IL_004a
  IL_0043:  ldloc.s    V_4
  IL_0045:  callvirt   ""void System.IDisposable.Dispose()""
  IL_004a:  endfinally
}
  IL_004b:  ldloc.1
  IL_004c:  call       ""void System.Console.Write(int)""
  IL_0051:  ret
}");
        }

        [Fact]
        [WorkItem(545650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545650")]
        public void TestIteratorWithUsing()
        {
            var source =
@"using System;
using System.Collections.Generic;
 
class Test<T>
{
    public static IEnumerator<T> M<U>(IEnumerable<T> items) where U : IDisposable, new()
    {
        T val = default(T);
        U u = default(U);
        using (u = new U()) { }
        yield return val;
    }
}
 
class T
{
    static void Main()
    {
    }
}";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact, WorkItem(545767, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545767")]
        public void DoNotCaptureUnusedParameters_Release()
        {
            var source = @"
using System;
using System.Collections;
using System.Collections.Generic;

class Program
{
    static void Main() { }

    public static IEnumerator<int> M(IEnumerable<int> items)
    {
        int x = 0;
        yield return x;
    }
}";
            var rel = CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current"
                }, module.GetFieldNames("Program.<M>d__1"));
            });

            rel.VerifyIL("Program.M", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""Program.<M>d__1..ctor(int)""
  IL_0006:  ret
}");
            var dbg = CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "items",
                    "<x>5__1",
                }, module.GetFieldNames("Program.<M>d__1"));
            });

            dbg.VerifyIL("Program.M", @"
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""Program.<M>d__1..ctor(int)""
  IL_0006:  dup
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""System.Collections.Generic.IEnumerable<int> Program.<M>d__1.items""
  IL_000d:  ret
}");
        }

        [Fact]
        public void HoistedParameters_Enumerable()
        {
            var source = @"
using System.Collections.Generic;

struct Test
{
    public static IEnumerable<int> F(int x, int y, int z)
    {
        x = z;
        yield return 1;
        y = 1;
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                // consider: we don't really need to hoist "x" and "z", we could store the values of "<>3__x" and "<>3__z" to locals at the beginning of MoveNext.
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "x",
                    "<>3__x",
                    "z",
                    "<>3__z",
                    "y",
                    "<>3__y",
                }, module.GetFieldNames("Test.<F>d__0"));
            });

            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "<>l__initialThreadId",
                    "x",
                    "<>3__x",
                    "y",
                    "<>3__y",
                    "z",
                    "<>3__z",
                }, module.GetFieldNames("Test.<F>d__0"));
            });
        }

        [Fact]
        public void HoistedParameters_Enumerator()
        {
            var source = @"
using System.Collections.Generic;

struct Test
{
    public static IEnumerator<int> F(int x, int y, int z)
    {
        x = z;
        yield return 1;
        y = 1;
    }
}";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "x",
                    "z",
                    "y",
                }, module.GetFieldNames("Test.<F>d__0"));
            });

            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: module =>
            {
                AssertEx.Equal(new[]
                {
                    "<>1__state",
                    "<>2__current",
                    "x",
                    "y",
                    "z",
                }, module.GetFieldNames("Test.<F>d__0"));
            });
        }

        [Fact]
        public void IteratorForEach()
        {
            var source =
@"
using System;
using System.Collections.Generic;

class Test
{
    public static IEnumerable<T> M<T>(IEnumerable<T> col)
    {
        foreach (var r in col)
        {
            yield return r;
        }
    }

    static void Main()
    {
        foreach (var rr in M(""abcdef""))
        {
            System.Console.Write(rr);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "abcdef").
                VerifyIL("Test.<M>d__0<T>.System.Collections.IEnumerator.MoveNext()",
@"{
  // Code size      129 (0x81)
  .maxstack  2
  .locals init (bool V_0,
                int V_1,
                T V_2) //r
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int Test.<M>d__0<T>.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  ldloc.1
    IL_000b:  ldc.i4.1
    IL_000c:  beq.s      IL_0052
    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  leave.s    IL_007f
    IL_0012:  ldarg.0
    IL_0013:  ldc.i4.m1
    IL_0014:  stfld      ""int Test.<M>d__0<T>.<>1__state""
    IL_0019:  ldarg.0
    IL_001a:  ldarg.0
    IL_001b:  ldfld      ""System.Collections.Generic.IEnumerable<T> Test.<M>d__0<T>.col""
    IL_0020:  callvirt   ""System.Collections.Generic.IEnumerator<T> System.Collections.Generic.IEnumerable<T>.GetEnumerator()""
    IL_0025:  stfld      ""System.Collections.Generic.IEnumerator<T> Test.<M>d__0<T>.<>7__wrap1""
    IL_002a:  ldarg.0
    IL_002b:  ldc.i4.s   -4
    IL_002d:  stfld      ""int Test.<M>d__0<T>.<>1__state""
    IL_0032:  br.s       IL_005a
    IL_0034:  ldarg.0
    IL_0035:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test.<M>d__0<T>.<>7__wrap1""
    IL_003a:  callvirt   ""T System.Collections.Generic.IEnumerator<T>.Current.get""
    IL_003f:  stloc.2
    IL_0040:  ldarg.0
    IL_0041:  ldloc.2
    IL_0042:  stfld      ""T Test.<M>d__0<T>.<>2__current""
    IL_0047:  ldarg.0
    IL_0048:  ldc.i4.1
    IL_0049:  stfld      ""int Test.<M>d__0<T>.<>1__state""
    IL_004e:  ldc.i4.1
    IL_004f:  stloc.0
    IL_0050:  leave.s    IL_007f
    IL_0052:  ldarg.0
    IL_0053:  ldc.i4.s   -4
    IL_0055:  stfld      ""int Test.<M>d__0<T>.<>1__state""
    IL_005a:  ldarg.0
    IL_005b:  ldfld      ""System.Collections.Generic.IEnumerator<T> Test.<M>d__0<T>.<>7__wrap1""
    IL_0060:  callvirt   ""bool System.Collections.IEnumerator.MoveNext()""
    IL_0065:  brtrue.s   IL_0034
    IL_0067:  ldarg.0
    IL_0068:  call       ""void Test.<M>d__0<T>.<>m__Finally1()""
    IL_006d:  ldarg.0
    IL_006e:  ldnull
    IL_006f:  stfld      ""System.Collections.Generic.IEnumerator<T> Test.<M>d__0<T>.<>7__wrap1""
    IL_0074:  ldc.i4.0
    IL_0075:  stloc.0
    IL_0076:  leave.s    IL_007f
  }
  fault
  {
    IL_0078:  ldarg.0
    IL_0079:  call       ""void Test.<M>d__0<T>.Dispose()""
    IL_007e:  endfinally
  }
  IL_007f:  ldloc.0
  IL_0080:  ret
}");
        }

        [Fact]
        [WorkItem(563925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/563925")]
        public void CaptureRefLocalNoParts()
        {
            var source =
@"using System;
using System.Collections.Generic;

struct S
{
    public int X;
}

class Program
{
    public static IEnumerable<int> M()
    {
        S s = new S();
        yield return s.X;
        s.X += 12;
        yield return s.X;
    }

    public static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "012");
        }

        [Fact]
        [WorkItem(590712, "DevDiv")]
        public void MultipassAnalysisWithRefLocal()
        {
            var source =
@"using System;
using System.Collections.Generic;

struct S
{
    public int I;
}
class Program
{
    public static IEnumerable<int> M()
    {
        S s = new S();
        for (int i = 0; i < 3; i++)
        {
            yield return i;
            s.I += 1;
        }
    }

    public static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "012");
        }

        [Fact]
        [WorkItem(620862, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/620862")]
        public void DelegateCreationInIterator()
        {
            var source =
@"using System;
using System.Collections.Generic;

delegate int Delegate(int x);

class Program1
{
    static int F(int x) { return x; }
    int F(double y) { return (int)y; }
    static IEnumerable<int> M()
    {
        Delegate d = new Delegate(F);
        yield return d(12);
        yield break;
    }
    static void Main()
    {
        foreach (int i in M())
        {
            Console.WriteLine(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void ForwardBranchInFinally()
        {
            var source =
@"
using System;
using System.Collections.Generic;


class Program
{
    private static bool flag = true;

    public static IEnumerable<int> M()
    {
        try
        {
            yield return 1;

            while (true)
            {

                if (flag)
                {
                    Console.Write(""Try"");
                }

                try
                {
                    if (flag)
                    {
                        Console.Write(""NestedTry"");
                    }
                    break;
                }
                catch (Exception)
                {
                    if (flag)
                    {
                        Console.Write(""NestedCatch"");
                    }
                    break;
                }
                finally
                {
                    if (flag)
                    {
                        Console.Write(""NestedFinally"");
                    }
                }
            }

            yield return 2;
        }
        finally
        {
            if (flag)
            {
                Console.Write(""Finally"");
            }
        }
    }

    public static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}


";
            CompileAndVerify(source, expectedOutput: "1TryNestedTryNestedFinally2Finally");
        }

        [Fact]
        public void MultiLevelGoto()
        {
            var source =
@"
using System;
using System.Collections.Generic;


class Program
{
    private static bool flag = true;

    public static IEnumerable<int> M()
    {
        try
        {
            yield return 1;
            try
            {
                yield return 2;
                try
                {
                    yield return 3;
                    try
                    {
                        yield return 4;
                        try
                        {
                            yield return 5;
                            goto L1;
                        }
                        finally
                        {
                            if (flag)
                            {
                                Console.Write(""Finally5"");
                            }
                        }
                    }
                    finally
                    {
                        if (flag)
                        {
                            Console.Write(""Finally4"");
                        }
                    }
                }
                finally
                {
                    if (flag)
                    {
                        Console.Write(""Finally3"");
                    }
                }
            }
            finally
            {
                if (flag)
                {
                    Console.Write(""Finally2"");
                }
            }

            L1:
            Console.Write(""L1"");
            
        }
        finally
        {
            if (flag)
            {
                Console.Write(""Finally1"");
            }
        }
    }

    public static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}



";
            CompileAndVerify(source, expectedOutput: "12345Finally5Finally4Finally3Finally2L1Finally1");
        }

        [Fact]
        public void MultiLevelGoto001()
        {
            var source =
@"
using System;
using System.Collections.Generic;


class Program
{
    private static int i = 0;

    public static IEnumerable<int> M()
    {
        tryAgain:

        try
        {
            if (i == 0)
                goto L1;
            
            yield return 0;

            try
            {
                if (i == 1)
                    goto L1;

                yield return 1;

                try
                {
                    if (i == 2)
                        goto L1;

                    yield return 2;

                    try
                    {
                        if (i == 3)
                            goto L1;

                        yield return 3;

                        try
                        {
                            if (i == 4)
                                goto L1;

                            yield return 4;

                        }
                        finally
                        {
                            Console.Write(""Finally5"");
                        }
                    }
                    finally
                    {
                        Console.Write(""Finally4"");
                    }
                }
                finally
                {
                    Console.Write(""Finally3"");
                }
            }
            finally
            {
                Console.Write(""Finally2"");
            }

            L1:
            Console.WriteLine(""L1"");

            if (i++ < 5)
                goto tryAgain;

        }
        finally
        {
            Console.Write(""Finally1"");
        }
    }

    public static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}

";
            CompileAndVerify(source, expectedOutput: @"
L1
Finally10Finally2L1
Finally101Finally3Finally2L1
Finally1012Finally4Finally3Finally2L1
Finally10123Finally5Finally4Finally3Finally2L1
Finally101234Finally5Finally4Finally3Finally2L1
Finally1");
        }

        [Fact]
        public void Switch001()
        {
            var source =
@"
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

delegate T Del<T, R>(int a, string s, T t, R r);

class Test
{
    static void Main()
    {
        Console.WriteLine(""M"");
    }
}

class S<T1, T2>
{
    T1 t1_field = default(T1);


    public IEnumerable<int> Iter2<M1, M2>(int x, string str)
    {
        try
        {
            using (DisposeImpl<M2> d = new DisposeImpl<M2>())
            {
                foreach (T1 t in new T1[] { })
                {
                    Console.WriteLine();
                }
                foreach (int ii in new int[] { 1, 2, 3, 4, 3, 3, 4, 5 })
                {
                    switch (str)
                    {
                        case ""blah"":
                            yield return 1;
                            break;
                        case ""hoge"":
                            yield break;
                            break;
                        case ""age"":
                            yield return 1;
                            break;
                        case ""hogehoge"":
                            yield return 1;
                            break;
                    }

                }
            }
        }
        finally
        {
            using (DisposeImpl<M2> d = new DisposeImpl<M2>())
            {
                foreach (T1 t in new T1[] { })
                {

                    Del<M2, T1> del;


                    foreach (int ii in new int[] { 1, 2, 3, 4, 3, 3, 4, 5 })
                    {
                        switch (str)
                        {
                            case ""abcdef"":
                                goto case ""blah"";

                            case ""blah"":
                                del = delegate { Console.WriteLine(d.ToString() + t.ToString() + ii + x + str + t1_field); return default(M2); };
                                break;

                        }

                    }
                }
            }

        }

    }

    class DisposeImpl<T> : IDisposable
    {
        public void Dispose()
        {
            Console.WriteLine(""D"");
        }
    }
}


";
            CompileAndVerify(source, expectedOutput: @"M");
        }

        [Fact]
        public void AssignedInFinally()
        {
            var source =
@"
using System;
using System.Collections.Generic;

class A
{
    public static IEnumerable<int> Iterator()
    {
        int aa;
        int dd;
        try
        {
            yield return 5;
        }
        finally
        {
            aa = 42;
            dd = 42;
        }
        yield return dd;
    }
    
    static void Main()
    {
        foreach(int i in Iterator())
        {
            Console.WriteLine(i);
        }
    }
}

";
            CompileAndVerify(source, expectedOutput: @"5
42");
        }

        [Fact]
        [WorkItem(703361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703361")]
        public void VerifyHelpers()
        {
            var source =
                @"
using System.Collections.Generic;

class Program
{
    public static IEnumerable<int> Goo()
    {
        yield return 1;
    }
}
";
            //EDMAURER ensure that we use System.Environment.CurrentManagedThreadId when compiling against 4.5
            var parsed = new[] { Parse(source) };
            var comp = CreateCompilationWithMscorlib461(parsed);
            var verifier = this.CompileAndVerify(comp);
            var il = verifier.VisualizeIL("Program.<Goo>d__0.System.Collections.Generic.IEnumerable<int>.GetEnumerator()");
            Assert.Contains("System.Environment.CurrentManagedThreadId.get", il, StringComparison.Ordinal);
        }

        [Fact]
        [WorkItem(703361, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/703361")]
        public void VerifyHelpers001()
        {
            var source =
                @"
using System.Collections.Generic;

class Program
{
    public static IEnumerable<int> Goo()
    {
        yield return 1;
    }
}

namespace System
{
    class Environment
    {
        public static int CurrentManagedThreadId{get{return 1;}}
    }
}

";
            var parsed = new[] { Parse(source) };
            var comp = CreateCompilation(parsed);
            comp.MakeMemberMissing(WellKnownMember.System_Threading_Thread__ManagedThreadId);
            var verifier = this.CompileAndVerify(comp);
            var il = verifier.VisualizeIL("Program.<Goo>d__0.System.Collections.Generic.IEnumerable<int>.GetEnumerator()");
            Assert.Contains("System.Environment.CurrentManagedThreadId.get", il, StringComparison.Ordinal);
        }

        [Fact]
        public void UnreachableExit()
        {
            var source =
@"
using System.Collections.Generic;
using System;

class Program
{
    public static IEnumerable<T> GetOnce<T>(T item)
    {
        yield break;
        yield return item;
    }

    static void Main(string[] args)
    {
        int i = 0;
        foreach (int ii in GetOnce(1))
        {
            i++;
        }
        Console.WriteLine(""DONE"");
    }
}

";
            CompileAndVerify(source, expectedOutput: @"DONE");
        }

        [Fact]
        public void Regress709127()
        {
            var source =
@"
    using System.Collections;
    using System.Collections.Generic;
    using System;

    // Enumerator is an empty struct - why not?
    struct Enumerator : IEnumerator<int>
    {

        public int Current
        {
            get { throw new NotImplementedException(); }
        }

        public void Dispose()
        {
        }

        object IEnumerator.Current
        {
            get { throw new NotImplementedException(); }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }

    class C : IEnumerable<int>
    {
        public Enumerator GetEnumerator()
        {
            return new Enumerator();
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    class Program 
    {
        private C moduleCatalog = new C();

        static void Main(string[] args)
        {
            var x = new Program();

            foreach (var v in x.GetModules())
            {
            }
        }

        public IEnumerable<int> GetModules()
        {
            foreach (var catalog in moduleCatalog)
                yield return catalog;
        }

    }";
            CompileAndVerify(source, expectedOutput: @"");
        }

        [WorkItem(718498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718498")]
        [Fact]
        public void Regress718498a()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
using System;

struct Empty
{
}

class Program
{
    private static void M(Empty s) { }
    public static IEnumerable<int> M()
    {
        yield return 1;
        yield return 2;
        try
        {
            Empty s;  // a variable of an empty struct type
            M(s); // is used before being assigned
        }
        finally   // in a try with a finally
        {
        }
    }

    static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"12");
        }

        [WorkItem(718498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718498")]
        [Fact]
        public void Regress718498b()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
using System;

class Program
{
    static void M(int x) { }
    public static IEnumerable<int> M()
    {
        try                 // a try-finally statement
        {
            yield return 1; // with a yield in its try block
        }
        finally
        {
            try             // and a try block in its finally block
            {
                // containing a local variable declared and definitely assigned
                int x = 12;
                M(x);
            }
            finally
            {
            }
        }

        yield return 2;
    }

    static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"12");
        }

        [WorkItem(718498, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/718498")]
        [Fact]
        public void Regress718498c()
        {
            var source =
@"using System.Collections;
using System.Collections.Generic;
using System;

class Program
{
    public static IEnumerable<int> M()
    {
        int y;
        try
        {
            yield return 1;
            y = 1;
        }
        finally // since the finally is moved to a separate method, any outside variable it uses must be stored in the frame
        {
            if (1.ToString() == 1.ToString()) y = 2;
        }

        yield return 2;
    }

    static void Main(string[] args)
    {
        foreach (var i in M())
        {
            Console.Write(i);
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"12");
        }

        /// <summary>
        /// Name of public fields for spill temps must start with
        /// "&lt;&gt;[c]__" so the fields are hidden in the debugger.
        /// </summary>
        [WorkItem(808600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/808600")]
        [Fact]
        public void SpillFieldName()
        {
            var source =
@"class C<T>
{
    static System.Collections.Generic.IEnumerable<T> F(System.IDisposable x, T[] y)
    {
        using (x)
        {
            foreach (var o in y)
            {
                yield return o;
            }
        }
    }
}";
            string expectedIL;
            CompileAndVerify(source).VerifyIL("C<T>.<F>d__0.System.Collections.IEnumerator.MoveNext()", expectedIL =
@"{
  // Code size      176 (0xb0)
  .maxstack  3
  .locals init (bool V_0,
                int V_1,
                T V_2) //o
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      ""int C<T>.<F>d__0.<>1__state""
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0015
    IL_000a:  ldloc.1
    IL_000b:  ldc.i4.1
    IL_000c:  beq.s      IL_0069
    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  leave      IL_00ae
    IL_0015:  ldarg.0
    IL_0016:  ldc.i4.m1
    IL_0017:  stfld      ""int C<T>.<F>d__0.<>1__state""
    IL_001c:  ldarg.0
    IL_001d:  ldarg.0
    IL_001e:  ldfld      ""System.IDisposable C<T>.<F>d__0.x""
    IL_0023:  stfld      ""System.IDisposable C<T>.<F>d__0.<>7__wrap1""
    IL_0028:  ldarg.0
    IL_0029:  ldc.i4.s   -4
    IL_002b:  stfld      ""int C<T>.<F>d__0.<>1__state""
    IL_0030:  ldarg.0
    IL_0031:  ldarg.0
    IL_0032:  ldfld      ""T[] C<T>.<F>d__0.y""
    IL_0037:  stfld      ""T[] C<T>.<F>d__0.<>7__wrap2""
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.0
    IL_003e:  stfld      ""int C<T>.<F>d__0.<>7__wrap3""
    IL_0043:  br.s       IL_007f
    IL_0045:  ldarg.0
    IL_0046:  ldfld      ""T[] C<T>.<F>d__0.<>7__wrap2""
    IL_004b:  ldarg.0
    IL_004c:  ldfld      ""int C<T>.<F>d__0.<>7__wrap3""
    IL_0051:  ldelem     ""T""
    IL_0056:  stloc.2
    IL_0057:  ldarg.0
    IL_0058:  ldloc.2
    IL_0059:  stfld      ""T C<T>.<F>d__0.<>2__current""
    IL_005e:  ldarg.0
    IL_005f:  ldc.i4.1
    IL_0060:  stfld      ""int C<T>.<F>d__0.<>1__state""
    IL_0065:  ldc.i4.1
    IL_0066:  stloc.0
    IL_0067:  leave.s    IL_00ae
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.s   -4
    IL_006c:  stfld      ""int C<T>.<F>d__0.<>1__state""
    IL_0071:  ldarg.0
    IL_0072:  ldarg.0
    IL_0073:  ldfld      ""int C<T>.<F>d__0.<>7__wrap3""
    IL_0078:  ldc.i4.1
    IL_0079:  add
    IL_007a:  stfld      ""int C<T>.<F>d__0.<>7__wrap3""
    IL_007f:  ldarg.0
    IL_0080:  ldfld      ""int C<T>.<F>d__0.<>7__wrap3""
    IL_0085:  ldarg.0
    IL_0086:  ldfld      ""T[] C<T>.<F>d__0.<>7__wrap2""
    IL_008b:  ldlen
    IL_008c:  conv.i4
    IL_008d:  blt.s      IL_0045
    IL_008f:  ldarg.0
    IL_0090:  ldnull
    IL_0091:  stfld      ""T[] C<T>.<F>d__0.<>7__wrap2""
    IL_0096:  ldarg.0
    IL_0097:  call       ""void C<T>.<F>d__0.<>m__Finally1()""
    IL_009c:  ldarg.0
    IL_009d:  ldnull
    IL_009e:  stfld      ""System.IDisposable C<T>.<F>d__0.<>7__wrap1""
    IL_00a3:  ldc.i4.0
    IL_00a4:  stloc.0
    IL_00a5:  leave.s    IL_00ae
  }
  fault
  {
    IL_00a7:  ldarg.0
    IL_00a8:  call       ""void C<T>.<F>d__0.Dispose()""
    IL_00ad:  endfinally
  }
  IL_00ae:  ldloc.0
  IL_00af:  ret
}");
            Assert.True(expectedIL.IndexOf("<>_", StringComparison.Ordinal) < 0);
        }

        [Fact, WorkItem(9167, "https://github.com/dotnet/roslyn/issues/9167")]
        public void IteratorShouldCompileWithoutOptionalAttributes()
        {
            #region IL for corlib without CompilerGeneratedAttribute or DebuggerNonUserCodeAttribute
            var corlib = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class Exception { }
    public class NotSupportedException : Exception { }
    public class ValueType { }
    public class Enum { }
    public struct Void { }
    public interface IDisposable
    {
        void Dispose();
    }
}

namespace System.Collections
{
    public interface IEnumerable
    {
        IEnumerator GetEnumerator();
    }

    public interface IEnumerator
    {
        bool MoveNext();
        object Current { get; }
        void Reset();
    }

    namespace Generic
    {
        public interface IEnumerable<T> : IEnumerable
        {
            new IEnumerator<T> GetEnumerator();
        }

        public interface IEnumerator<T> : IEnumerator
        {
            new T Current { get; }
        }
    }
}";
            #endregion

            var source = @"
public class C
{
    public System.Collections.IEnumerable SomeNumbers()
    {
        yield return 42;
    }
}";
            // The compilation succeeds even though CompilerGeneratedAttribute and DebuggerNonUserCodeAttribute are not available.
            var parseOptions = TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            var compilation = CreateEmptyCompilation(new[] { Parse(source, options: parseOptions), Parse(corlib, options: parseOptions) });
            // PEVerify: System.Enum must extend System.ValueType.
            var verifier = CompileAndVerify(compilation, verify: Verification.FailsPEVerify);
            verifier.VerifyDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1));
        }

        [Fact, WorkItem(9463, "https://github.com/dotnet/roslyn/issues/9463")]
        public void IEnumerableIteratorReportsDiagnosticsWhenCoreTypesAreMissing()
        {
            // Note that IDisposable.Dispose, IEnumerator.Current and other types are missing
            // Also, IEnumerator<T> doesn't have a get accessor
            var source = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class Exception { }
    public class ValueType { }
    public class Enum { }
    public struct Void { }
    public interface IDisposable { }
}

namespace System.Collections
{
    public interface IEnumerable { }
    public interface IEnumerator { }
}

namespace System.Collections.Generic
{
    public interface IEnumerator<T>
    {
        T Current { set; }
    }
}

public class C
{
    public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
}";
            var compilation = CreateEmptyCompilation(new[] { Parse(source, options: TestOptions.Regular.WithNoRefSafetyRulesAttribute()) });

            compilation.VerifyEmitDiagnostics(
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1),
                // (31,57): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.IDisposable", "Dispose").WithLocation(31, 57),
                // (31,57): error CS0154: The property or indexer 'IEnumerator<T>.Current' cannot be used in this context because it lacks the get accessor
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "{ yield return 42; }").WithArguments("System.Collections.Generic.IEnumerator<T>.Current").WithLocation(31, 57),
                // (31,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.Current'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator", "Current").WithLocation(31, 57),
                // (31,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator", "MoveNext").WithLocation(31, 57),
                // (31,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.Reset'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator", "Reset").WithLocation(31, 57),
                // (31,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerable.GetEnumerator'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerable", "GetEnumerator").WithLocation(31, 57),
                // (31,57): error CS0518: Predefined type 'System.Collections.Generic.IEnumerable`1' is not defined or imported
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "{ yield return 42; }").WithArguments("System.Collections.Generic.IEnumerable`1").WithLocation(31, 57),
                // (31,57): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerable`1.GetEnumerator'
                //     public System.Collections.IEnumerable SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.Generic.IEnumerable`1", "GetEnumerator").WithLocation(31, 57));
        }

        [Fact, WorkItem(9463, "https://github.com/dotnet/roslyn/issues/9463")]
        public void IEnumeratorIteratorReportsDiagnosticsWhenCoreTypesAreMissing()
        {
            // Note that IDisposable.Dispose and other types are missing
            // Also IEnumerator.Current lacks a get accessor
            var source = @"
namespace System
{
    public class Object { }
    public struct Int32 { }
    public struct Boolean { }
    public class String { }
    public class Exception { }
    public class ValueType { }
    public class Enum { }
    public struct Void { }

    public interface IDisposable { }
}

namespace System.Collections
{
    public interface IEnumerable { }
    public interface IEnumerator
    {
        Object Current { set; }
    }
}

public class C
{
    public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
}";
            var compilation = CreateEmptyCompilation(new[] { Parse(source, options: TestOptions.Regular.WithNoRefSafetyRulesAttribute()) });

            // No error about IEnumerable
            compilation.VerifyEmitDiagnostics(
                // (27,57): error CS0656: Missing compiler required member 'System.IDisposable.Dispose'
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.IDisposable", "Dispose").WithLocation(27, 57),
                // (27,57): error CS0518: Predefined type 'System.Collections.Generic.IEnumerator`1' is not defined or imported
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "{ yield return 42; }").WithArguments("System.Collections.Generic.IEnumerator`1").WithLocation(27, 57),
                // (27,57): error CS0656: Missing compiler required member 'System.Collections.Generic.IEnumerator`1.Current'
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.Generic.IEnumerator`1", "Current").WithLocation(27, 57),
                // (27,57): error CS0154: The property or indexer 'IEnumerator.Current' cannot be used in this context because it lacks the get accessor
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator.Current").WithLocation(27, 57),
                // (27,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.MoveNext'
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator", "MoveNext").WithLocation(27, 57),
                // (27,57): error CS0656: Missing compiler required member 'System.Collections.IEnumerator.Reset'
                //     public System.Collections.IEnumerator SomeNumbers() { yield return 42; }
                Diagnostic(ErrorCode.ERR_MissingPredefinedMember, "{ yield return 42; }").WithArguments("System.Collections.IEnumerator", "Reset").WithLocation(27, 57),
                // warning CS8021: No value for RuntimeMetadataVersion found. No assembly containing System.Object was found nor was a value for RuntimeMetadataVersion specified through options.
                Diagnostic(ErrorCode.WRN_NoRuntimeMetadataVersion).WithLocation(1, 1));
        }

        [Fact, WorkItem(21077, "https://github.com/dotnet/roslyn/issues/21077")]
        public void NoExtraCapturing_01()
        {
            // Note that the variable i is not captured in any of these three async methods.
            var source =
@"using System.Threading.Tasks;

class Program
{
    static async Task Method1(bool value)
    {
        int i = 42;
        int j = i;
        await Task.Yield();
    }

    static async Task Method2(bool value)
    {
        int i = 42;
        if (value)
        {
            int j = i;
            await Task.Yield();
        }
        else
        {
            await Task.Yield();
        }
    }

    static async Task Method3(bool value)
    {
        int i = 42;
        if (value)
        {
            await Task.Yield();
        }
        else
        {
            int j = i;
            await Task.Yield();
        }
    }
}";
            var v = CompileAndVerify(source, options: TestOptions.ReleaseDll);
            v.VerifyIL("Program.<Method1>d__0.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      148 (0x94)
  .maxstack  3
  .locals init (int V_0,
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_1,
                System.Runtime.CompilerServices.YieldAwaitable V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Method1>d__0.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0044
    IL_000a:  ldc.i4.s   42
    IL_000c:  pop
    IL_000d:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0012:  stloc.2
    IL_0013:  ldloca.s   V_2
    IL_0015:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_001a:  stloc.1
    IL_001b:  ldloca.s   V_1
    IL_001d:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0022:  brtrue.s   IL_0060
    IL_0024:  ldarg.0
    IL_0025:  ldc.i4.0
    IL_0026:  dup
    IL_0027:  stloc.0
    IL_0028:  stfld      ""int Program.<Method1>d__0.<>1__state""
    IL_002d:  ldarg.0
    IL_002e:  ldloc.1
    IL_002f:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method1>d__0.<>u__1""
    IL_0034:  ldarg.0
    IL_0035:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method1>d__0.<>t__builder""
    IL_003a:  ldloca.s   V_1
    IL_003c:  ldarg.0
    IL_003d:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Method1>d__0>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Method1>d__0)""
    IL_0042:  leave.s    IL_0093
    IL_0044:  ldarg.0
    IL_0045:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method1>d__0.<>u__1""
    IL_004a:  stloc.1
    IL_004b:  ldarg.0
    IL_004c:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method1>d__0.<>u__1""
    IL_0051:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0057:  ldarg.0
    IL_0058:  ldc.i4.m1
    IL_0059:  dup
    IL_005a:  stloc.0
    IL_005b:  stfld      ""int Program.<Method1>d__0.<>1__state""
    IL_0060:  ldloca.s   V_1
    IL_0062:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0067:  leave.s    IL_0080
  }
  catch System.Exception
  {
    IL_0069:  stloc.3
    IL_006a:  ldarg.0
    IL_006b:  ldc.i4.s   -2
    IL_006d:  stfld      ""int Program.<Method1>d__0.<>1__state""
    IL_0072:  ldarg.0
    IL_0073:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method1>d__0.<>t__builder""
    IL_0078:  ldloc.3
    IL_0079:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_007e:  leave.s    IL_0093
  }
  IL_0080:  ldarg.0
  IL_0081:  ldc.i4.s   -2
  IL_0083:  stfld      ""int Program.<Method1>d__0.<>1__state""
  IL_0088:  ldarg.0
  IL_0089:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method1>d__0.<>t__builder""
  IL_008e:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0093:  ret
}");
            v.VerifyIL("Program.<Method2>d__1.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      260 (0x104)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //i
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Method2>d__1.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0056
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b2
    IL_0011:  ldc.i4.s   42
    IL_0013:  stloc.1
    IL_0014:  ldarg.0
    IL_0015:  ldfld      ""bool Program.<Method2>d__1.value""
    IL_001a:  brfalse.s  IL_007b
    IL_001c:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0029:  stloc.2
    IL_002a:  ldloca.s   V_2
    IL_002c:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0031:  brtrue.s   IL_0072
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""int Program.<Method2>d__1.<>1__state""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.2
    IL_003e:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method2>d__1.<>t__builder""
    IL_0049:  ldloca.s   V_2
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Method2>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Method2>d__1)""
    IL_0051:  leave      IL_0103
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_005c:  stloc.2
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int Program.<Method2>d__1.<>1__state""
    IL_0072:  ldloca.s   V_2
    IL_0074:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0079:  br.s       IL_00d5
    IL_007b:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0080:  stloc.3
    IL_0081:  ldloca.s   V_3
    IL_0083:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0088:  stloc.2
    IL_0089:  ldloca.s   V_2
    IL_008b:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0090:  brtrue.s   IL_00ce
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      ""int Program.<Method2>d__1.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldloc.2
    IL_009d:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method2>d__1.<>t__builder""
    IL_00a8:  ldloca.s   V_2
    IL_00aa:  ldarg.0
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Method2>d__1>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Method2>d__1)""
    IL_00b0:  leave.s    IL_0103
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_00b8:  stloc.2
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method2>d__1.<>u__1""
    IL_00bf:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00c5:  ldarg.0
    IL_00c6:  ldc.i4.m1
    IL_00c7:  dup
    IL_00c8:  stloc.0
    IL_00c9:  stfld      ""int Program.<Method2>d__1.<>1__state""
    IL_00ce:  ldloca.s   V_2
    IL_00d0:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00d5:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00d7:  stloc.s    V_4
    IL_00d9:  ldarg.0
    IL_00da:  ldc.i4.s   -2
    IL_00dc:  stfld      ""int Program.<Method2>d__1.<>1__state""
    IL_00e1:  ldarg.0
    IL_00e2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method2>d__1.<>t__builder""
    IL_00e7:  ldloc.s    V_4
    IL_00e9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ee:  leave.s    IL_0103
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  stfld      ""int Program.<Method2>d__1.<>1__state""
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method2>d__1.<>t__builder""
  IL_00fe:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0103:  ret
}");
            v.VerifyIL("Program.<Method3>d__2.System.Runtime.CompilerServices.IAsyncStateMachine.MoveNext()",
@"{
  // Code size      260 (0x104)
  .maxstack  3
  .locals init (int V_0,
                int V_1, //i
                System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter V_2,
                System.Runtime.CompilerServices.YieldAwaitable V_3,
                System.Exception V_4)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Method3>d__2.<>1__state""
  IL_0006:  stloc.0
  .try
  {
    IL_0007:  ldloc.0
    IL_0008:  brfalse.s  IL_0056
    IL_000a:  ldloc.0
    IL_000b:  ldc.i4.1
    IL_000c:  beq        IL_00b2
    IL_0011:  ldc.i4.s   42
    IL_0013:  stloc.1
    IL_0014:  ldarg.0
    IL_0015:  ldfld      ""bool Program.<Method3>d__2.value""
    IL_001a:  brfalse.s  IL_007b
    IL_001c:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0021:  stloc.3
    IL_0022:  ldloca.s   V_3
    IL_0024:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0029:  stloc.2
    IL_002a:  ldloca.s   V_2
    IL_002c:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0031:  brtrue.s   IL_0072
    IL_0033:  ldarg.0
    IL_0034:  ldc.i4.0
    IL_0035:  dup
    IL_0036:  stloc.0
    IL_0037:  stfld      ""int Program.<Method3>d__2.<>1__state""
    IL_003c:  ldarg.0
    IL_003d:  ldloc.2
    IL_003e:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_0043:  ldarg.0
    IL_0044:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method3>d__2.<>t__builder""
    IL_0049:  ldloca.s   V_2
    IL_004b:  ldarg.0
    IL_004c:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Method3>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Method3>d__2)""
    IL_0051:  leave      IL_0103
    IL_0056:  ldarg.0
    IL_0057:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_005c:  stloc.2
    IL_005d:  ldarg.0
    IL_005e:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_0063:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_0069:  ldarg.0
    IL_006a:  ldc.i4.m1
    IL_006b:  dup
    IL_006c:  stloc.0
    IL_006d:  stfld      ""int Program.<Method3>d__2.<>1__state""
    IL_0072:  ldloca.s   V_2
    IL_0074:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_0079:  br.s       IL_00d5
    IL_007b:  call       ""System.Runtime.CompilerServices.YieldAwaitable System.Threading.Tasks.Task.Yield()""
    IL_0080:  stloc.3
    IL_0081:  ldloca.s   V_3
    IL_0083:  call       ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter System.Runtime.CompilerServices.YieldAwaitable.GetAwaiter()""
    IL_0088:  stloc.2
    IL_0089:  ldloca.s   V_2
    IL_008b:  call       ""bool System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.IsCompleted.get""
    IL_0090:  brtrue.s   IL_00ce
    IL_0092:  ldarg.0
    IL_0093:  ldc.i4.1
    IL_0094:  dup
    IL_0095:  stloc.0
    IL_0096:  stfld      ""int Program.<Method3>d__2.<>1__state""
    IL_009b:  ldarg.0
    IL_009c:  ldloc.2
    IL_009d:  stfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_00a2:  ldarg.0
    IL_00a3:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method3>d__2.<>t__builder""
    IL_00a8:  ldloca.s   V_2
    IL_00aa:  ldarg.0
    IL_00ab:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted<System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, Program.<Method3>d__2>(ref System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter, ref Program.<Method3>d__2)""
    IL_00b0:  leave.s    IL_0103
    IL_00b2:  ldarg.0
    IL_00b3:  ldfld      ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_00b8:  stloc.2
    IL_00b9:  ldarg.0
    IL_00ba:  ldflda     ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter Program.<Method3>d__2.<>u__1""
    IL_00bf:  initobj    ""System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter""
    IL_00c5:  ldarg.0
    IL_00c6:  ldc.i4.m1
    IL_00c7:  dup
    IL_00c8:  stloc.0
    IL_00c9:  stfld      ""int Program.<Method3>d__2.<>1__state""
    IL_00ce:  ldloca.s   V_2
    IL_00d0:  call       ""void System.Runtime.CompilerServices.YieldAwaitable.YieldAwaiter.GetResult()""
    IL_00d5:  leave.s    IL_00f0
  }
  catch System.Exception
  {
    IL_00d7:  stloc.s    V_4
    IL_00d9:  ldarg.0
    IL_00da:  ldc.i4.s   -2
    IL_00dc:  stfld      ""int Program.<Method3>d__2.<>1__state""
    IL_00e1:  ldarg.0
    IL_00e2:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method3>d__2.<>t__builder""
    IL_00e7:  ldloc.s    V_4
    IL_00e9:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetException(System.Exception)""
    IL_00ee:  leave.s    IL_0103
  }
  IL_00f0:  ldarg.0
  IL_00f1:  ldc.i4.s   -2
  IL_00f3:  stfld      ""int Program.<Method3>d__2.<>1__state""
  IL_00f8:  ldarg.0
  IL_00f9:  ldflda     ""System.Runtime.CompilerServices.AsyncTaskMethodBuilder Program.<Method3>d__2.<>t__builder""
  IL_00fe:  call       ""void System.Runtime.CompilerServices.AsyncTaskMethodBuilder.SetResult()""
  IL_0103:  ret
}");
        }

        [Fact, WorkItem(5062, "https://github.com/dotnet/roslyn/issues/5062")]
        public void LocalLiftingVsSwitch()
        {
            var source =
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        foreach (var s in Iter1(0)) Console.Write(s);
        foreach (var s in Iter1(1)) Console.Write(s);
        foreach (var s in Iter2(0)) Console.Write(s);
        foreach (var s in Iter2(1)) Console.Write(s);
    }

    static IEnumerable<string> Iter1(int i)
    {
        bool result;
        switch (i)
        {
            case 1: result = true; break;
            default: result = false; break;
        }
        yield return result.ToString();
    }

    static IEnumerable<string> Iter2(int i)
    {
        bool result;
        if (i == 1)
            result = true;
        else
            result = false;
        yield return result.ToString();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "FalseTrueFalseTrue", options: TestOptions.ReleaseExe);
            compilation.VerifyIL("Program.<Iter1>d__1.System.Collections.IEnumerator.MoveNext()", @"{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (int V_0,
                bool V_1) //result
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iter1>d__1.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_003c
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int Program.<Iter1>d__1.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  ldfld      ""int Program.<Iter1>d__1.i""
  IL_001d:  ldc.i4.1
  IL_001e:  bne.un.s   IL_0024
  IL_0020:  ldc.i4.1
  IL_0021:  stloc.1
  IL_0022:  br.s       IL_0026
  IL_0024:  ldc.i4.0
  IL_0025:  stloc.1
  IL_0026:  ldarg.0
  IL_0027:  ldloca.s   V_1
  IL_0029:  call       ""string bool.ToString()""
  IL_002e:  stfld      ""string Program.<Iter1>d__1.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int Program.<Iter1>d__1.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int Program.<Iter1>d__1.<>1__state""
  IL_0043:  ldc.i4.0
  IL_0044:  ret
}");
            compilation.VerifyIL("Program.<Iter2>d__2.System.Collections.IEnumerator.MoveNext()", @"{
  // Code size       69 (0x45)
  .maxstack  2
  .locals init (int V_0,
                bool V_1) //result
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int Program.<Iter2>d__2.<>1__state""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  brfalse.s  IL_0010
  IL_000a:  ldloc.0
  IL_000b:  ldc.i4.1
  IL_000c:  beq.s      IL_003c
  IL_000e:  ldc.i4.0
  IL_000f:  ret
  IL_0010:  ldarg.0
  IL_0011:  ldc.i4.m1
  IL_0012:  stfld      ""int Program.<Iter2>d__2.<>1__state""
  IL_0017:  ldarg.0
  IL_0018:  ldfld      ""int Program.<Iter2>d__2.i""
  IL_001d:  ldc.i4.1
  IL_001e:  bne.un.s   IL_0024
  IL_0020:  ldc.i4.1
  IL_0021:  stloc.1
  IL_0022:  br.s       IL_0026
  IL_0024:  ldc.i4.0
  IL_0025:  stloc.1
  IL_0026:  ldarg.0
  IL_0027:  ldloca.s   V_1
  IL_0029:  call       ""string bool.ToString()""
  IL_002e:  stfld      ""string Program.<Iter2>d__2.<>2__current""
  IL_0033:  ldarg.0
  IL_0034:  ldc.i4.1
  IL_0035:  stfld      ""int Program.<Iter2>d__2.<>1__state""
  IL_003a:  ldc.i4.1
  IL_003b:  ret
  IL_003c:  ldarg.0
  IL_003d:  ldc.i4.m1
  IL_003e:  stfld      ""int Program.<Iter2>d__2.<>1__state""
  IL_0043:  ldc.i4.0
  IL_0044:  ret
}");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_IntLocal()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values) { }
System.Console.Write(((int)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        int values2 = 42;
        yield return values2;
        System.Console.Write($"{values2} ");
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42 42").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        string values2 = "ran ";
        yield return 42;
        System.Console.Write(values2);
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "ran ran True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<Produce>d__0.<values2>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_YieldBreak()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        string s = "ran ";
        yield return 42;
        System.Console.Write(s);
        yield break;
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "ran ran True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<Produce>d__0.<s>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_IntArrayLocal()
        {
            string source = """
using System.Linq;
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((int[])values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)).Length);
}
System.Console.Write(((int[])values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        int[] values2 = Enumerable.Range(0, 100).ToArray();
        yield return 42;
        System.Console.Write($" {values2.Length} ");
    }
}
""";
            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "100 100 True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "int[] C.<Produce>d__0.<values2>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal()
        {
            var src = """
using System.Reflection;

var enumerable = C.M(true);
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
    System.Console.Write(((string)enumerator.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
    assert(!enumerator.MoveNext());
    System.Console.Write(((string)enumerator.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)) is null);
}
finally
{
    enumerator.Dispose();
}

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 42;
            b = false;
            System.Console.Write(s);
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42 value value True").VerifyDiagnostics();
            verifier.VerifyIL("C.<M>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<M>d__0.<s>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<M>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_YieldBreak()
        {
            var src = """
using System.Reflection;

var enumerable = C.M(true);
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
    assert(!enumerator.MoveNext());
    System.Console.Write(((string)enumerator.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
}
finally
{
    enumerator.Dispose();
}
System.Console.Write(((string)enumerator.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)) is null);

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 42;
            System.Console.Write(s);
            yield break;
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42 value value True").VerifyDiagnostics();
            verifier.VerifyIL("C.<M>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<M>d__0.<s>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<M>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_ThrownException()
        {
            var src = """
using System.Reflection;

var enumerable = C.M(true);
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
}
finally
{
    System.Console.Write(((string)enumerable.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerable)));
    enumerator.Dispose();
}
System.Console.Write(((string)enumerable.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerable)) is null);

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M(bool b)
    {
        while (b)
        {
            string s = "value ";
            yield return 42;
            System.Console.Write(s);
            throw new System.Exception();
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "42 value True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_IntParameter()
        {
            string src = """
using System.Reflection;

var values = C.Produce(42);
foreach (int value in values) { }
System.Console.Write(((int)values.GetType().GetField("s", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
System.Console.Write(((int)values.GetType().GetField("<>3__s", BindingFlags.Public | BindingFlags.Instance).GetValue(values)));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(int s)
    {
        yield return 0;
        System.Console.Write($"{s} ");
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42 4242").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringParameter()
        {
            string src = """
using System.Reflection;

var values = C.Produce("value ");
foreach (int value in values) { }
System.Console.Write(((string)values.GetType().GetField("s", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
System.Console.Write(((string)values.GetType().GetField("<>3__s", BindingFlags.Public | BindingFlags.Instance).GetValue(values)));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(string s)
    {
        yield return 0;
        System.Console.Write(s);
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "value value value").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_ClosureOverLocal()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values) { }
var closure = values.GetType().GetField("<>8__1", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values);
System.Console.Write((int)(closure.GetType().GetField("s", BindingFlags.Public | BindingFlags.Instance).GetValue(closure)));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        int s = 41;
        local();
        yield return 0;
        System.Console.Write($"{s} ");

        void local()
        {
            s++;
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42 42").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_ClosureOverThis()
        {
            string src = """
using System.Reflection;

class C
{
    int field = 41;
    public static void Main()
    {
        var values = new C().Produce();
        foreach (int value in values) { }
        System.Console.Write(((C)values.GetType().GetField("<>4__this", BindingFlags.Public | BindingFlags.Instance).GetValue(values)).field);
    }

    private System.Collections.Generic.IEnumerable<int> Produce()
    {
        local();
        yield return this.field;

        void local()
        {
            this.field++;
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "42").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__2.System.IDisposable.Dispose()", """
 {
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__2.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_InTryFinally()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            var s = "value ";
            yield return 0;
            System.Console.Write(s);
        }
        finally
        {
            System.Console.Write("ran ");
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "value value ran True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_0010
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  bne.un.s   IL_001a
  IL_0010:  nop
  .try
  {
    IL_0011:  leave.s    IL_001a
  }
  finally
  {
    IL_0013:  ldarg.0
    IL_0014:  call       "void C.<Produce>d__0.<>m__Finally1()"
    IL_0019:  endfinally
  }
  IL_001a:  ldarg.0
  IL_001b:  ldnull
  IL_001c:  stfld      "string C.<Produce>d__0.<s>5__2"
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.s   -3
  IL_0024:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0029:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_InTryFinally_WithThrow()
        {
            string src = """
using System.Reflection;

var values = C.Produce();

try
{
    foreach (int value in values) 
    {
        System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    }
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            string s = "value ";
            yield return 0;
            System.Console.Write(s);
        }
        finally
        {
            throw new System.Exception("exception ");
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "value value exception True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_0010
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  bne.un.s   IL_001a
  IL_0010:  nop
  .try
  {
    IL_0011:  leave.s    IL_001a
  }
  finally
  {
    IL_0013:  ldarg.0
    IL_0014:  call       "void C.<Produce>d__0.<>m__Finally1()"
    IL_0019:  endfinally
  }
  IL_001a:  ldarg.0
  IL_001b:  ldnull
  IL_001c:  stfld      "string C.<Produce>d__0.<s>5__2"
  IL_0021:  ldarg.0
  IL_0022:  ldc.i4.s   -3
  IL_0024:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0029:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_InTryFinally_WithThrow_EarlyIterationExit()
        {
            string src = """
using System.Reflection;

var values = C.Produce();

try
{
    foreach (int value in values) { break; } // we interrupt the iteration early
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            string s = "value";
            yield return 0;
            s.ToString();
            throw null;
        }
        finally
        {
            throw new System.Exception("exception ");
        }
    }
}
""";
            // Note: nested hoisted local does not get cleared when an exception is thrown during disposal
            CompileAndVerify(src, expectedOutput: "exception value").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_WithThrownException()
        {
            string src = """
using System.Reflection;

var values = C.Produce(true);

try
{
    foreach (int value in values) { }
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(bool b)
    {
        string s = "value ";
        if (b) throw new System.Exception("exception ");
        yield return 0;
        System.Console.Write(s);
    }
}
""";
            CompileAndVerify(src, expectedOutput: "exception True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedUnmanagedTypeParameterLocal()
        {
            var src = """
using System.Reflection;

var enumerable = C.M(true, 42);
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
    assert(!enumerator.MoveNext());
    System.Console.Write(" ");
    System.Console.Write(((int)enumerator.GetType().GetField("<local>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
}
finally
{
    enumerator.Dispose();
}

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M<T>(bool b, T t) where T : unmanaged
    {
        while (b)
        {
            T local = t;
            yield return 10;
            b = false;
            System.Console.Write(local);
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "10 42 42").VerifyDiagnostics();
            verifier.VerifyIL("C.<M>d__0<T>.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<M>d__0<T>.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedLocalWithStructFromAnotherCompilation()
        {
            var libSrc = """
public struct S
{
    public int field;
    public override string ToString() => field.ToString();
}
""";
            var libComp = CreateCompilation(libSrc);
            var src = """
using System.Reflection;

var enumerable = C.M(true, new S { field = 42 });
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
    assert(!enumerator.MoveNext());
    System.Console.Write(" ");
    System.Console.Write(((S)enumerator.GetType().GetField("<local>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
}
finally
{
    enumerator.Dispose();
}

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M(bool b, S s)
    {
        while (b)
        {
            S local = s;
            yield return 10;
            b = false;
            System.Console.Write(local.ToString());
        }
    }
}
""";

            var verifier = CompileAndVerify(src, expectedOutput: "10 42 42", references: [libComp.EmitToImageReference()]).VerifyDiagnostics();
            verifier.VerifyIL("C.<M>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<M>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedUnmanagedWithGenericsLocal()
        {
            var src = """
using System.Reflection;

var enumerable = C.M(true, 42);
var enumerator = enumerable.GetEnumerator();
try
{
    assert(enumerator.MoveNext());
    System.Console.Write($"{enumerator.Current} ");
    assert(!enumerator.MoveNext());
    System.Console.Write(((S<int>)enumerator.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)).field);
}
finally
{
    enumerator.Dispose();
}

void assert(bool b)
{
    if (!b) throw new System.Exception();
}

public struct S<T> where T : unmanaged // UnmanagedWithGenerics
{
    public T field;
}

public class C
{
    public static System.Collections.Generic.IEnumerable<int> M<T>(bool b, T t) where T : unmanaged
    {
        while (b)
        {
            S<T> s = new S<T> { field = t };
            yield return 42;
            b = false;
            System.Console.Write(s.field);
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "42 4242").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_EarlyIterationExit()
        {
            string src = """
using System.Reflection;

var values = C.Produce();

foreach (int value in values) 
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    break;
}

System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        string s = "value ";
        yield return 0;
        s.ToString();
        throw null;
    }
}
""";
            CompileAndVerify(src, expectedOutput: "value True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NestedStringLocal_Reused()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
}
System.Console.Write(((string)values.GetType().GetField("<values2>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        {
            string values2 = "values2 ";
            yield return 42;
            System.Console.Write(values2);
        }
        {
            string values3 = "values3 ";
            yield return 43;
            System.Console.Write(values3);
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "values2 values2 values3 values3 True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<Produce>d__0.<values2>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_Parameters()
        {
            string src = """
string s = "ran ";
var values = C.Produce(s);
foreach (int value in values) { }
foreach (int value in values) { }

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(string s)
    {
        yield return 42;
        System.Console.Write(s);
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "ran ran").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_0008:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_NotCleanedTooSoon()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
foreach (int value in values)
{
    System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
    break;
}
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            string s = "value ";
            try
            {
                yield return 42;
            }
            finally
            {
                System.Console.Write(s);
            }
        }
        finally
        {
            System.Console.Write("outer ");
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "value value outer True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       63 (0x3f)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<Produce>d__0.<>1__state"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -5
  IL_000a:  sub
  IL_000b:  ldc.i4.1
  IL_000c:  ble.un.s   IL_0012
  IL_000e:  ldloc.0
  IL_000f:  ldc.i4.1
  IL_0010:  bne.un.s   IL_002f
  IL_0012:  nop
  .try
  {
    IL_0013:  ldloc.0
    IL_0014:  ldc.i4.s   -5
    IL_0016:  beq.s      IL_001e
    IL_0018:  ldloc.0
    IL_0019:  ldc.i4.1
    IL_001a:  beq.s      IL_001e
    IL_001c:  leave.s    IL_002f
    IL_001e:  nop
    .try
    {
      IL_001f:  leave.s    IL_002f
    }
    finally
    {
      IL_0021:  ldarg.0
      IL_0022:  call       "void C.<Produce>d__0.<>m__Finally2()"
      IL_0027:  endfinally
    }
  }
  finally
  {
    IL_0028:  ldarg.0
    IL_0029:  call       "void C.<Produce>d__0.<>m__Finally1()"
    IL_002e:  endfinally
  }
  IL_002f:  ldarg.0
  IL_0030:  ldnull
  IL_0031:  stfld      "string C.<Produce>d__0.<s>5__2"
  IL_0036:  ldarg.0
  IL_0037:  ldc.i4.s   -3
  IL_0039:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_003e:  ret
}
""");
        }

        [ConditionalFact(typeof(CoreClrOnly))]
        public void AddVariableCleanup_HoistedFromRefExpression()
        {
            var src = """
using System.Reflection;

C c = new C();
var coll = Test(c);
var enumerator = coll.GetEnumerator();
try
{
    enumerator.MoveNext();
    System.Console.Write(((C)coll.GetType().GetField("<>7__wrap2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coll)) is null);
}
finally
{
    enumerator.Dispose();
}

System.Console.Write(((C)coll.GetType().GetField("<>7__wrap2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coll)) is null);

class C
{
    public Buffer4<int> F = default;
}

partial class Program
{
    static System.Collections.Generic.IEnumerable<int> Test(C x)
    {
        foreach (var y in x.F)
        {
            yield return -1;
        }
    }
}

[System.Runtime.CompilerServices.InlineArray(4)]
public struct Buffer4<T>
{
    private T _element0;
}
""";
            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net80);
            var verifier = CompileAndVerify(comp, expectedOutput: "FalseTrue", verify: Verification.Skipped).VerifyDiagnostics();
            verifier.VerifyIL("Program.<Test>d__1.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "C Program.<Test>d__1.<>7__wrap2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int Program.<Test>d__1.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_IEnumerator()
        {
            string src = """
using System.Reflection;

var values = C.Produce();
assert(values.MoveNext());
assert(values.Current == 42);
assert(!values.MoveNext());
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)));
values.Dispose();
System.Console.Write(((string)values.GetType().GetField("<s>5__2", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(values)) is null);

static void assert(bool b) { if (!b) throw new System.Exception(); }

class C
{
    public static System.Collections.Generic.IEnumerator<int> Produce()
    {
        string s = "ran ";
        yield return 42;
        System.Console.Write(s);
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "ran ran True").VerifyDiagnostics();
            verifier.VerifyIL("C.<Produce>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<Produce>d__0.<s>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<Produce>d__0.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact]
        public void AddVariableCleanup_Unmanaged_UseSiteError()
        {
            var missingLibS1 = CreateCompilation(@"
public struct S1
{
    public int i;
}
", assemblyName: "libS1").ToMetadataReference();

            var libS2 = CreateCompilation(@"
public struct S2
{
    public S1 s1;
}
", references: [missingLibS1], assemblyName: "libS2").ToMetadataReference();

            var source = @"
class C
{
    System.Collections.Generic.IEnumerable<int> M1()
    {
        S2 s2 = M2();
        yield return 42;
        System.Console.Write(s2);
    }

    S2 M2() => default;
}
";
            var comp = CreateCompilation(source, references: [libS2]);
            comp.VerifyEmitDiagnostics(
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1),
                // error CS0012: The type 'S1' is defined in an assembly that is not referenced. You must add a reference to assembly 'libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'.
                Diagnostic(ErrorCode.ERR_NoTypeDef).WithArguments("S1", "libS1, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null").WithLocation(1, 1));

            comp = CreateCompilation(source, options: TestOptions.UnsafeDebugDll, references: [libS2, missingLibS1]);
            comp.VerifyEmitDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75666")]
        public void AddVariableCleanup_StringLocal_MoveNextAfterDispose()
        {
            string src = """
var values = new C<string>([" one ", " two "]);
var enumerator = values.GetEnumerator();
System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write("disposing ");
enumerator.Dispose();
System.Console.Write("disposed ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C<T>
{
    private sealed class Node
    {
        internal readonly T _value;
        internal Node _next;

        internal Node(T value)
        {
            _value = value;
            _next = null;
        }
    }

    private Node _head; 

    public C(System.Collections.Generic.IEnumerable<T> collection)
    {
        Node lastNode = null;
        foreach (T element in collection)
        {
            Node newNode = new Node(element);
            newNode._next = lastNode;
            lastNode = newNode;
        }

        _head = lastNode;
    }

    public System.Collections.Generic.IEnumerator<T> GetEnumerator()
    {
        return GetEnumerator(_head);
    }

    private static System.Collections.Generic.IEnumerator<T> GetEnumerator(Node head)
    {
        Node current = head;
        while (current != null)
        {
            yield return current._value;
            current = current._next;
            System.Console.Write("AFTER");
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "True two disposing disposed False two").VerifyDiagnostics();
            verifier.VerifyIL("C<T>.<GetEnumerator>d__4.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "C<T>.Node C<T>.<GetEnumerator>d__4.<current>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C<T>.<GetEnumerator>d__4.<>1__state"
  IL_000f:  ret
}
""");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldReturn()
        {
            // When a yield return statement is encountered ... The state of the enumerator object is changed to suspended.
            // If the state of the enumerator object is suspended, invoking Dispose: ... Changes the state to after.
            string src = """
var enumerator = C.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();
System.Console.Write("disposed ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        yield return " one ";
        yield return " two ";
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "True one disposed False one").VerifyDiagnostics();
            verifier.VerifyIL("C.<GetEnumerator>d__0.System.IDisposable.Dispose()", """
{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.s   -3
  IL_0003:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0008:  ret
}
""");

            // Verify GetEnumerator
            var src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();
System.Console.Write(object.ReferenceEquals(enumerable, enumerator));

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();
System.Console.Write("disposed ");

System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<string> Produce()
    {
        yield return " one ";
        yield return " two ";
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "TrueTrue one disposed -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_DisposeBeforeIteration()
        {
            string src = """
var enumerator = C.GetEnumerator();

enumerator.Dispose();
System.Console.Write("disposed ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current is null);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        yield return " one ";
        yield return " two ";
    }
}
""";
            CompileAndVerify(src, expectedOutput: "disposed FalseTrue").VerifyDiagnostics();

            // Verify GetEnumerator
            var src2 = """
using System.Reflection;

var enumerable = C.Produce();
System.Console.Write(((int)enumerable.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerable)));
System.Console.Write(" ");

var enumerator = enumerable.GetEnumerator();
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));

enumerator.Dispose();
System.Console.Write(" disposed ");

System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<string> Produce()
    {
        yield return " one ";
        yield return " two ";
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "-2 0 disposed -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldReturn_IEnumerable()
        {
            // When a yield return statement is encountered ... The state of the enumerator object is changed to suspended.
            // If the state of the enumerator object is suspended, invoking Dispose: ... Changes the state to after.
            string src = """
var enumerator = C.Produce().GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();
System.Console.Write("disposed ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerable<string> Produce()
    {
        yield return " one ";
        yield return " two ";
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one disposed False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(object.ReferenceEquals(enumerable, enumerator));
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

System.Console.Write(enumerator.MoveNext());
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

enumerator.Dispose();

System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

enumerator.Dispose();
enumerator.Dispose();

System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        yield return 42;
        yield return 43;
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "TrueTrueTrueTrue -3 True -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_DisposeTwice()
        {
            // If the state of the enumerator object is after, invoking Dispose has no affect.
            string src = """
var enumerator = C.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();
System.Console.Write("disposed ");

enumerator.Dispose();
System.Console.Write("disposed2 ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        string local = "";
        yield return " one ";
        local.ToString();
    }
}
""";
            // Note: we actually set the state to "after"/"finished" and cleanup hoisted locals again
            var verifier = CompileAndVerify(src, expectedOutput: "True one disposed disposed2 False one").VerifyDiagnostics();
            verifier.VerifyIL("C.<GetEnumerator>d__0.System.IDisposable.Dispose()", """
{
  // Code size       16 (0x10)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldnull
  IL_0002:  stfld      "string C.<GetEnumerator>d__0.<local>5__2"
  IL_0007:  ldarg.0
  IL_0008:  ldc.i4.s   -3
  IL_000a:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_000f:  ret
}
""");

            // Verify GetEnumerator
            var src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();
System.Console.Write("disposed ");

enumerator.Dispose();
System.Console.Write("disposed2 ");

System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<string> Produce()
    {
        string local = "";
        yield return " one ";
        local.ToString();
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "True one disposed disposed2 -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldBreak()
        {
            // When a yield break statement is encountered ... The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        yield return " one ";
        if (b) yield break;
        yield return " two ";
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one False one False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce(true);
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

System.Console.Write(!enumerator.MoveNext());
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(bool b)
    {
        yield return 42;
        if (b) yield break;
        yield return 43;
    }
}
""";
            // We're not setting the state to "after"/"finished"
            // Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(src2, expectedOutput: "TrueTrueTrue -1 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_EndOfBody()
        {
            // When the end of the iterator body is encountered ... The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        yield return " one ";
        System.Console.Write("done ");
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one done False one False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce(true);
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

System.Console.Write(!enumerator.MoveNext());
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(bool b)
    {
        yield return 42;
    }
}
""";
            // We're not setting the state to "after"/"finished"
            // Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(src2, expectedOutput: "True 1 TrueTrue -1 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_ThrowException()
        {
            // When an exception is thrown and propagated out of the iterator block ... The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

try
{
    System.Console.Write(enumerator.MoveNext());
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        yield return " one ";
        throw new System.Exception("exception");
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one exception one False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

try
{
    _ = enumerator.MoveNext();
}
catch (System.Exception)
{
    System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));
}

System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");

enumerator.Dispose();
System.Console.Write("disposed ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        yield return 42;
        throw new System.Exception("exception");
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "TrueTrueTrue -1 disposed -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldReturn_InTryFinally()
        {
            // When a yield return statement is encountered ... The state of the enumerator object is changed to suspended.

            // If the state of the enumerator object is suspended, invoking Dispose:
            // ...
            // Executes any finally blocks as if the last executed yield return statement were a yield break statement.
            //   If this causes an exception to be thrown and propagated out of the iterator body,
            //   the state of the enumerator object is set to after and the exception is propagated to the caller of the Dispose method.
            string src = """
var enumerator = C.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write("disposing ");
try
{
    enumerator.Dispose();
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}
System.Console.Write(" disposed ");

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator()
    {
        try
        {
            yield return " one ";
        }
        finally
        {
            throw new System.Exception("exception");
        }
    }
}
""";
            var verifier = CompileAndVerify(src, expectedOutput: "True one disposing exception disposed False one").VerifyDiagnostics();
            verifier.VerifyIL("C.<GetEnumerator>d__0.System.IDisposable.Dispose()", """
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_0010
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  bne.un.s   IL_001a
  IL_0010:  nop
  .try
  {
    IL_0011:  leave.s    IL_001a
  }
  finally
  {
    IL_0013:  ldarg.0
    IL_0014:  call       "void C.<GetEnumerator>d__0.<>m__Finally1()"
    IL_0019:  endfinally
  }
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.s   -3
  IL_001d:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0022:  ret
}
""");
            verifier.VerifyIL("C.<GetEnumerator>d__0.<>m__Finally1()", """
{
  // Code size       18 (0x12)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.m1
  IL_0002:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0007:  ldstr      "exception"
  IL_000c:  newobj     "System.Exception..ctor(string)"
  IL_0011:  throw
}
""");

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

try
{
    enumerator.Dispose();
}
catch (System.Exception)
{
    System.Console.Write(" ");
    System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
    System.Console.Write(" ");
    System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));
}

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            yield return 42;
        }
        finally
        {
            throw new System.Exception("exception");
        }
    }
}
""";
            // We're not setting the state to "after"/"finished"
            // Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(src2, expectedOutput: "TrueTrue -1 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldBreak_InTryFinally()
        {
            // When a yield break statement is encountered:
            // If the yield break statement is within one or more try blocks, the associated finally blocks are executed.
            // The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        yield return " one ";
        try
        {
            if (b) yield break;
        }
        finally
        {
            System.Console.Write("finally ");
        }
        yield return " two ";
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one finally False one False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce(true);
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

System.Console.Write(!enumerator.MoveNext());
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce(bool b)
    {
        yield return 42;
        try
        {
            if (b) yield break;
        }
        finally
        {
            System.Console.Write(" finally ");
        }
        yield return 43;
    }
}
""";
            // We're not setting the state to "after"/"finished"
            // Tracked by https://github.com/dotnet/roslyn/issues/76089
            CompileAndVerify(src2, expectedOutput: "True 1 True finally True -1 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_ThrowException_InTryFinally()
        {
            // When an exception is thrown and propagated out of the iterator block: ... The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

try
{
    System.Console.Write(enumerator.MoveNext());
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        yield return " one ";
        try
        {
            throw new System.Exception("exception");
        }
        finally
        {
            System.Console.Write("finally ");
        }
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one finally exception one False one False one").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());

try
{
    _ = enumerator.MoveNext();
}
catch (System.Exception)
{
    System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
    System.Console.Write(" ");
    System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));
}

enumerator.Dispose();
System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        yield return 42;
        try
        {
            throw new System.Exception("exception");
        }
        finally
        {
            System.Console.Write(" finally ");
        }
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "True finally -1 True -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_ThrowException_InTryFinally_WithYieldInTry()
        {
            // When an exception is thrown and propagated out of the iterator block: ... The state of the enumerator object is changed to after.
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

try
{
    System.Console.Write(enumerator.MoveNext());
}
catch (System.Exception e)
{
    System.Console.Write(e.Message);
}

System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        try
        {
            yield return " one ";
            if (b) throw new System.Exception("exception");
        }
        finally
        {
            System.Console.Write("finally ");
        }
    }
}
""";
            // Note: we generate a `fault { Dispose(); }`, but only if there is a `yield` in a `try`, which is surprising
            var verifier = CompileAndVerify(src, expectedOutput: "True one finally exception one False one").VerifyDiagnostics();
            verifier.VerifyIL("C.<GetEnumerator>d__0.System.Collections.IEnumerator.MoveNext()", """
{
  // Code size      101 (0x65)
  .maxstack  2
  .locals init (bool V_0,
                int V_1)
  .try
  {
    IL_0000:  ldarg.0
    IL_0001:  ldfld      "int C.<GetEnumerator>d__0.<>1__state"
    IL_0006:  stloc.1
    IL_0007:  ldloc.1
    IL_0008:  brfalse.s  IL_0012
    IL_000a:  ldloc.1
    IL_000b:  ldc.i4.1
    IL_000c:  beq.s      IL_0037
    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  leave.s    IL_0063
    IL_0012:  ldarg.0
    IL_0013:  ldc.i4.m1
    IL_0014:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
    IL_0019:  ldarg.0
    IL_001a:  ldc.i4.s   -4
    IL_001c:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
    IL_0021:  ldarg.0
    IL_0022:  ldstr      " one "
    IL_0027:  stfld      "string C.<GetEnumerator>d__0.<>2__current"
    IL_002c:  ldarg.0
    IL_002d:  ldc.i4.1
    IL_002e:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
    IL_0033:  ldc.i4.1
    IL_0034:  stloc.0
    IL_0035:  leave.s    IL_0063
    IL_0037:  ldarg.0
    IL_0038:  ldc.i4.s   -4
    IL_003a:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
    IL_003f:  ldarg.0
    IL_0040:  ldfld      "bool C.<GetEnumerator>d__0.b"
    IL_0045:  brfalse.s  IL_0052
    IL_0047:  ldstr      "exception"
    IL_004c:  newobj     "System.Exception..ctor(string)"
    IL_0051:  throw
    IL_0052:  ldarg.0
    IL_0053:  call       "void C.<GetEnumerator>d__0.<>m__Finally1()"
    IL_0058:  ldc.i4.0
    IL_0059:  stloc.0
    IL_005a:  leave.s    IL_0063
  }
  fault
  {
    IL_005c:  ldarg.0
    IL_005d:  call       "void C.<GetEnumerator>d__0.Dispose()"
    IL_0062:  endfinally
  }
  IL_0063:  ldloc.0
  IL_0064:  ret
}
""");

            verifier.VerifyIL("C.<GetEnumerator>d__0.System.IDisposable.Dispose()", """
{
  // Code size       35 (0x23)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.s   -4
  IL_000a:  beq.s      IL_0010
  IL_000c:  ldloc.0
  IL_000d:  ldc.i4.1
  IL_000e:  bne.un.s   IL_001a
  IL_0010:  nop
  .try
  {
    IL_0011:  leave.s    IL_001a
  }
  finally
  {
    IL_0013:  ldarg.0
    IL_0014:  call       "void C.<GetEnumerator>d__0.<>m__Finally1()"
    IL_0019:  endfinally
  }
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.s   -3
  IL_001d:  stfld      "int C.<GetEnumerator>d__0.<>1__state"
  IL_0022:  ret
}
""");

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());

try
{
    _ = enumerator.MoveNext();
}
catch (System.Exception)
{
    System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
    System.Console.Write(" ");
    System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));
}

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            yield return 42;
            throw new System.Exception("exception");
        }
        finally
        {
            System.Console.Write(" finally ");
        }
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "True finally -3 True").VerifyDiagnostics();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76078")]
        public void StateAfterMoveNext_YieldReturn_AfterTryFinally()
        {
            string src = """
var enumerator = C.GetEnumerator(true);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

enumerator.Dispose();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.Current);

class C
{
    public static System.Collections.Generic.IEnumerator<string> GetEnumerator(bool b)
    {
        try
        {
            yield return " one ";
        }
        finally
        {
            System.Console.Write("finally ");
        }

        yield return " two ";
        System.Console.Write("not executed after disposal");
    }
}
""";
            CompileAndVerify(src, expectedOutput: "True one finally True two False two").VerifyDiagnostics();

            // Verify GetEnumerator
            string src2 = """
using System.Reflection;

var enumerable = C.Produce();
var enumerator = enumerable.GetEnumerator();

System.Console.Write(enumerator.MoveNext());
System.Console.Write(enumerator.MoveNext());

System.Console.Write(" ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));

enumerator.Dispose();
System.Console.Write(" disposed ");
System.Console.Write(((int)enumerator.GetType().GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enumerator)));
System.Console.Write(" ");
System.Console.Write(!object.ReferenceEquals(enumerable, enumerable.GetEnumerator()));

class C
{
    public static System.Collections.Generic.IEnumerable<int> Produce()
    {
        try
        {
            yield return 42;
        }
        finally
        {
            System.Console.Write(" finally ");
        }

        yield return 43;
        System.Console.Write("not executed after disposal");
    }
}
""";
            CompileAndVerify(src2, expectedOutput: "True finally True 2 disposed -3 True").VerifyDiagnostics();
        }
        // TODO2 add a test that observes state numbers including those for finalize frames
    }
}
