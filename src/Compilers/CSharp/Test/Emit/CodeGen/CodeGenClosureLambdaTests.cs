// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenClosureLambdaTests : CSharpTestBase
    {
        [Fact]
        public void StaticClosure01()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        D d1 = new D(() => Console.Write(1));
        d1();
        D d2 = () => Console.WriteLine(2);
        d2();
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");

            compilation.VerifyIL("C.Main",
@"
{
  // Code size       73 (0x49)
  .maxstack  2
  IL_0000:  ldsfld     ""D C.<>c.<>9__0_0""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001f
  IL_0008:  pop
  IL_0009:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000e:  ldftn      ""void C.<>c.<Main>b__0_0()""
  IL_0014:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0019:  dup
  IL_001a:  stsfld     ""D C.<>c.<>9__0_0""
  IL_001f:  callvirt   ""void D.Invoke()""
  IL_0024:  ldsfld     ""D C.<>c.<>9__0_1""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0032:  ldftn      ""void C.<>c.<Main>b__0_1()""
  IL_0038:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""D C.<>c.<>9__0_1""
  IL_0043:  callvirt   ""void D.Invoke()""
  IL_0048:  ret
}
");
        }

        [Fact]
        public void ThisOnlyClosure()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        new C().M();
    }

    int n = 1;
    int m = 2;
    public void M()
    {
        for (int i = 0; i < 2; i++)
        {
            D d1 = new D(() => Console.Write(n));
            d1();
            D d2 = () => Console.WriteLine(m);
            d2();
        }
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: @"12
12");

            compilation.VerifyIL("C.M",
@"{
  // Code size       47 (0x2f)
  .maxstack  2
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  br.s       IL_002a
  IL_0004:  ldarg.0
  IL_0005:  ldftn      ""void C.<M>b__3_0()""
  IL_000b:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0010:  callvirt   ""void D.Invoke()""
  IL_0015:  ldarg.0
  IL_0016:  ldftn      ""void C.<M>b__3_1()""
  IL_001c:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0021:  callvirt   ""void D.Invoke()""
  IL_0026:  ldloc.0
  IL_0027:  ldc.i4.1
  IL_0028:  add
  IL_0029:  stloc.0
  IL_002a:  ldloc.0
  IL_002b:  ldc.i4.2
  IL_002c:  blt.s      IL_0004
  IL_002e:  ret
}");
        }

        [Fact]
        public void StaticClosure02()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        new C().F();
    }

    void F()
    {
        D d1 = () => Console.WriteLine(1);
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: "1");
        }

        [Fact]
        public void StaticClosure03()
        {
            string source = @"using System;
delegate int D(int x);
class Program
{
    static void Main(string[] args)
    {
        int @string = 10;
        D d = delegate (int @class)
        {
            return @class + @string;
        };
        Console.WriteLine(d(2));
    }
} ";
            var compilation = CompileAndVerify(source, expectedOutput: @"12");
        }

        [Fact]
        public void InstanceClosure01()
        {
            string source = @"using System;
delegate void D();
class C
{
    int X;
    C(int x)
    {
        this.X = x;
    }

    public static void Main(string[] args)
    {
        new C(12).F();
    }

    void F()
    {
        D d1 = () => Console.WriteLine(X);
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void InstanceClosure02()
        {
            string source = @"using System;
delegate void D();
class C
{
    int X;
    C(int x)
    {
        this.X = x;
    }

    public static void Main(string[] args)
    {
        new C(12).F();
    }

    void F()
    {
        D d1 = () => {
            C c = this;
            Console.WriteLine(c.X);
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void InstanceClosure03()
        {
            string source = @"using System;
delegate void D();
class C
{
    int X;
    C(int x)
    {
        this.X = x;
    }

    public static void Main(string[] args)
    {
        new C(12).F();
    }

    void F()
    {
        C c = this;
        D d1 = () => {
            Console.WriteLine(c.X);
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void InstanceClosure04()
        {
            string source = @"using System;
delegate void D();
class C
{
    int X;
    C(int x)
    {
        this.X = x;
    }

    public static void Main(string[] args)
    {
        F(new C(12));
    }

    static void F(C c)
    {
        D d1 = () =>
        {
            Console.WriteLine(c.X);
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void InstanceClosure05()
        {
            string source = @"using System;
delegate void D();
class C
{
    int X;
    C(int x)
    {
        this.X = x;
    }

    public static void Main(string[] args)
    {
        {
            C c = new C(12);
            D d = () => {
                Console.WriteLine(c.X);
            };
            d();
        }
        {
            C c = new C(13);
            D d = () => {
                Console.WriteLine(c.X);
            };
            d();
        }
    }
}";
            CompileAndVerify(source, expectedOutput: @"
12
13
");
        }

        [Fact]
        public void InstanceClosure06()
        {
            string source = @"using System;
delegate void D();
class C
{
    int K = 11;
    public static void Main(string[] args)
    {
        new C().F();
    }
    void F()
    {
        int i = 12;
        D d1 = () =>
        {
            Console.WriteLine(K);
            Console.WriteLine(i);
        };
        d1();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
11
12
");
        }

        [Fact]
        public void LoopClosure01()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        D d1 = null, d2 = null;
        int i = 0;
        while (i < 10)
        {
            int j = i;
            if (i == 5)
            {
                d1 = () => Console.WriteLine(j);
                d2 = () => Console.WriteLine(i);
            }
            i = i + 1;
        }
        d1();
        d2();
    }
}";
            CompileAndVerify(source, expectedOutput: @"
5
10
");
        }

        [Fact]
        public void NestedClosure01()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        int i = 12;
        D d1 = () =>
        {
            D d2 = () =>
            {
                Console.WriteLine(i);
            };
            d2();
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: @"12");
        }

        [Fact]
        public void NestedClosure02()
        {
            string source = @"using System;
delegate void D();
class C
{
    int K = 11;
    public static void Main(string[] args)
    {
        new C().F();
    }
    void F()
    {
        int i = 12;
        D d1 = () =>
        {
            D d2 = () =>
            {
                Console.WriteLine(K);
                Console.WriteLine(i);
            };
            d2();
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: @"
11
12
");
        }

        [Fact]
        public void NestedClosure10()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        int i = 12;
        D d1 = () =>
        {
            int j = 13;
            D d2 = () =>
            {
                Console.WriteLine(i);
                Console.WriteLine(j);
            };
            d2();
        };
        d1();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
12
13
");
        }

        [Fact]
        public void NestedClosure11()
        {
            string source = @"using System;
delegate void D();
class C
{
    int K = 11;
    public static void Main(string[] args)
    {
        new C().F();
    }
    void F()
    {
        int i = 12;
        D d1 = () =>
        {
            int j = 13;
            D d2 = () =>
            {
                Console.WriteLine(K);
                Console.WriteLine(i);
                Console.WriteLine(j);
            };
            d2();
        };
        d1();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
11
12
13
");
        }

        [Fact]
        public void NestedClosure20()
        {
            string source = @"using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        int i = 12;
        D d1 = () =>
        {
            int j = i + 1;
            D d2 = () =>
            {
                Console.WriteLine(i);
                Console.WriteLine(j);
            };
            d2();
        };
        d1();
    }
}";
            CompileAndVerify(source, expectedOutput: @"
12
13
");
        }

        [Fact]
        public void NestedClosure21()
        {
            string source = @"using System;
delegate void D();
class C
{
    int K = 11;
    public static void Main(string[] args)
    {
        new C().F();
    }
    void F()
    {
        int i = 12;
        D d1 = () =>
        {
            int j = i + 1;
            D d2 = () =>
            {
                Console.WriteLine(K);
                Console.WriteLine(i);
                Console.WriteLine(j);
            };
            d2();
        };
        d1();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
11
12
13
");
        }

        [WorkItem(540146, "DevDiv")]
        [Fact]
        public void NestedClosureThisConstructorInitializer()
        {
            string source = @"using System;
delegate void D();
delegate void D1(int i);
class C
{
public int l = 15;
public C():this((int i) =>
                {
                    int k = 14;
                    D d15 = () =>
                    {
                        int j = 13;
                        D d2 = () =>
                        {
                            Console.WriteLine(i);
                            Console.WriteLine(j);
                            Console.WriteLine(k);                            
                        };
                        d2();
                    };
                    d15();
                })
        {func((int i) =>
            {
                int k = 14;
                D d15 = () =>
                {
                    int j = 13;
                    D d2 = () =>
                    {
                        Console.WriteLine(i);
                        Console.WriteLine(j);
                        Console.WriteLine(k);
                        Console.WriteLine(l);
                    };
                    d2();
                };
                d15();
            });
        }

public C(D1 d1){int i=12; d1(i);}

public void func(D1 d1)
{
    int i=12;d1(i);
}
public static void Main(string[] args)
    {
        new C();
    }
}";

            CompileAndVerify(source, expectedOutput: @"
12
13
14
12
13
14
15
");
        }

        [Fact]
        public void FilterParameterClosure01()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        string s = ""xxx"";

        try
        {
            throw new Exception(""xxx"");
        }
        catch (Exception e) when (new Func<Exception, bool>(x => x.Message == s)(e))
        {
            Console.Write(""pass"");
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "pass");
        }

        [Fact]
        public void FilterParameterClosure02()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        string s = ""xxx"";

        try
        {
            throw new Exception(""xxx"");
        }
        catch (Exception e) when (new Func<Exception, bool>(x => x.Message == s)(e))
        {
            Console.Write(s + ""pass"");
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "xxxpass");
        }

        [WorkItem(541258, "DevDiv")]
        [Fact]
        public void CatchVarLifted1()
        {
            string source = @"using System;
class Program
{
    static void Main()
    {
        Action a = null;
        try
        {
            throw new Exception(""pass"");
        }
        catch (Exception e)
        {
            a = () => Console.Write(e.Message);
        }
        a();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "pass");

            verifier.VerifyIL("Program.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (System.Action V_0, //a
                Program.<>c__DisplayClass0_0 V_1, //CS$<>8__locals0
                System.Exception V_2)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  .try
  {
    IL_0002:  ldstr      ""pass""
    IL_0007:  newobj     ""System.Exception..ctor(string)""
    IL_000c:  throw
  }
  catch System.Exception
  {
    IL_000d:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_0012:  stloc.1
    IL_0013:  stloc.2
    IL_0014:  ldloc.1
    IL_0015:  ldloc.2
    IL_0016:  stfld      ""System.Exception Program.<>c__DisplayClass0_0.e""
    IL_001b:  ldloc.1
    IL_001c:  ldftn      ""void Program.<>c__DisplayClass0_0.<Main>b__0()""
    IL_0022:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0027:  stloc.0
    IL_0028:  leave.s    IL_002a
  }
  IL_002a:  ldloc.0
  IL_002b:  callvirt   ""void System.Action.Invoke()""
  IL_0030:  ret
}
");
        }

        [Fact]
        public void CatchVarLifted2()
        {
            var source = @"
using System;

class Program
{
    static bool Foo(Action x)
    {
        x();
        return true;
    }

    static void Main()
    {
        try
        {
            throw new Exception(""fail"");
        }
        catch (Exception ex) when (Foo(() => { ex = new Exception(""pass""); }))
        {
            Console.Write(ex.Message);
        }
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: "pass");

            verifier.VerifyIL("Program.Main", @"
{
  // Code size       79 (0x4f)
  .maxstack  2
  .locals init (Program.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                System.Exception V_1)
  .try
  {
    IL_0000:  ldstr      ""fail""
    IL_0005:  newobj     ""System.Exception..ctor(string)""
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     ""System.Exception""
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_0039
    IL_0017:  newobj     ""Program.<>c__DisplayClass1_0..ctor()""
    IL_001c:  stloc.0
    IL_001d:  stloc.1
    IL_001e:  ldloc.0
    IL_001f:  ldloc.1
    IL_0020:  stfld      ""System.Exception Program.<>c__DisplayClass1_0.ex""
    IL_0025:  ldloc.0
    IL_0026:  ldftn      ""void Program.<>c__DisplayClass1_0.<Main>b__0()""
    IL_002c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
    IL_0031:  call       ""bool Program.Foo(System.Action)""
    IL_0036:  ldc.i4.0
    IL_0037:  cgt.un
    IL_0039:  endfilter
  }  // end filter
  {  // handler
    IL_003b:  pop
    IL_003c:  ldloc.0
    IL_003d:  ldfld      ""System.Exception Program.<>c__DisplayClass1_0.ex""
    IL_0042:  callvirt   ""string System.Exception.Message.get""
    IL_0047:  call       ""void System.Console.Write(string)""
    IL_004c:  leave.s    IL_004e
  }
  IL_004e:  ret
}
");
        }

        [Fact]
        public void CatchVarLifted2a()
        {
            var source = @"
using System;

class Program
{
    static bool Foo(Action x)
    {
        x();
        return true;
    }

    static void Main()
    {
        try
        {
            throw new Exception(""fail"");
        }
        catch (ArgumentException ex) when (Foo(() => { ex = new ArgumentException(""fail""); }))
        {
            Console.Write(ex.Message);
        }
        catch (Exception ex) when (Foo(() => { ex = new Exception(""pass""); }))
        {
            Console.Write(ex.Message);
        }
    }
}";

            var verifier = CompileAndVerify(source, expectedOutput: "pass");
        }

        [Fact]
        public void CatchVarLifted3()
        {
            string source = @"
using System;
class Program
{
    static void Main()
    {
        try
        {
            throw new Exception(""xxx"");
        }
        catch (Exception e) when (new Func<bool>(() => e.Message == ""xxx"")())
        {
            Console.Write(""pass"");
        }
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "pass");
            verifier.VerifyIL("Program.Main", @"
{
  // Code size       73 (0x49)
  .maxstack  2
  .locals init (Program.<>c__DisplayClass0_0 V_0, //CS$<>8__locals0
                System.Exception V_1)
  .try
  {
    IL_0000:  ldstr      ""xxx""
    IL_0005:  newobj     ""System.Exception..ctor(string)""
    IL_000a:  throw
  }
  filter
  {
    IL_000b:  isinst     ""System.Exception""
    IL_0010:  dup
    IL_0011:  brtrue.s   IL_0017
    IL_0013:  pop
    IL_0014:  ldc.i4.0
    IL_0015:  br.s       IL_0039
    IL_0017:  newobj     ""Program.<>c__DisplayClass0_0..ctor()""
    IL_001c:  stloc.0
    IL_001d:  stloc.1
    IL_001e:  ldloc.0
    IL_001f:  ldloc.1
    IL_0020:  stfld      ""System.Exception Program.<>c__DisplayClass0_0.e""
    IL_0025:  ldloc.0
    IL_0026:  ldftn      ""bool Program.<>c__DisplayClass0_0.<Main>b__0()""
    IL_002c:  newobj     ""System.Func<bool>..ctor(object, System.IntPtr)""
    IL_0031:  callvirt   ""bool System.Func<bool>.Invoke()""
    IL_0036:  ldc.i4.0
    IL_0037:  cgt.un
    IL_0039:  endfilter
  }  // end filter
  {  // handler
    IL_003b:  pop
    IL_003c:  ldstr      ""pass""
    IL_0041:  call       ""void System.Console.Write(string)""
    IL_0046:  leave.s    IL_0048
  }
  IL_0048:  ret
}
");
        }

        [Fact]
        public void CatchVarLifted_Generic1()
        {
            string source = @"
using System;
using System.IO;

class Program
{
    static void Main()
    {
        F<IOException>();
    }

    static void F<T>() where T : Exception
    {
        new Action(() => 
        {
            try
            {
                throw new IOException(""xxx"");
            }
            catch (T e) when (e.Message == ""xxx"")
            {
                Console.Write(""pass"");
            }
        })();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "pass");
            verifier.VerifyIL("Program.<>c__1<T>.<F>b__1_0", @"
{
  // Code size       67 (0x43)
  .maxstack  2
  .try
{
  IL_0000:  ldstr      ""xxx""
  IL_0005:  newobj     ""System.IO.IOException..ctor(string)""
  IL_000a:  throw
}
  filter
{
  IL_000b:  isinst     ""T""
  IL_0010:  dup
  IL_0011:  brtrue.s   IL_0017
  IL_0013:  pop
  IL_0014:  ldc.i4.0
  IL_0015:  br.s       IL_0033
  IL_0017:  unbox.any  ""T""
  IL_001c:  box        ""T""
  IL_0021:  callvirt   ""string System.Exception.Message.get""
  IL_0026:  ldstr      ""xxx""
  IL_002b:  call       ""bool string.op_Equality(string, string)""
  IL_0030:  ldc.i4.0
  IL_0031:  cgt.un
  IL_0033:  endfilter
}  // end filter
{  // handler
  IL_0035:  pop
  IL_0036:  ldstr      ""pass""
  IL_003b:  call       ""void System.Console.Write(string)""
  IL_0040:  leave.s    IL_0042
}
  IL_0042:  ret
}
");
        }

        [Fact]
        public void CatchVarLifted_Generic2()
        {
            string source = @"
using System;
using System.IO;

class Program
{
    static void Main()
    {
        F<IOException>();
    }

    static void F<T>() where T : Exception
    {
        string x = ""x"";

        new Action(() => 
        {
            string y = ""y"";

            try
            {
                throw new IOException(""xy"");
            }
            catch (T e) when (new Func<bool>(() => e.Message == x + y)())
            {
                Console.Write(""pass_"" + x + y);
            }
        })();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "pass_xy");
            verifier.VerifyIL("Program.<>c__DisplayClass1_1<T>.<F>b__0", @"
{
  // Code size      131 (0x83)
  .maxstack  3
  .locals init (Program.<>c__DisplayClass1_2<T> V_0, //CS$<>8__locals0
                Program.<>c__DisplayClass1_0<T> V_1, //CS$<>8__locals1
                T V_2)
  IL_0000:  newobj     ""Program.<>c__DisplayClass1_2<T>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Program.<>c__DisplayClass1_1<T> Program.<>c__DisplayClass1_2<T>.CS$<>8__locals1""
  IL_000d:  ldloc.0
  IL_000e:  ldstr      ""y""
  IL_0013:  stfld      ""string Program.<>c__DisplayClass1_2<T>.y""
  .try
  {
    IL_0018:  ldstr      ""xy""
    IL_001d:  newobj     ""System.IO.IOException..ctor(string)""
    IL_0022:  throw
  }
  filter
  {
    IL_0023:  isinst     ""T""
    IL_0028:  dup
    IL_0029:  brtrue.s   IL_002f
    IL_002b:  pop
    IL_002c:  ldc.i4.0
    IL_002d:  br.s       IL_005d
    IL_002f:  unbox.any  ""T""
    IL_0034:  newobj     ""Program.<>c__DisplayClass1_0<T>..ctor()""
    IL_0039:  stloc.1
    IL_003a:  ldloc.1
    IL_003b:  ldloc.0
    IL_003c:  stfld      ""Program.<>c__DisplayClass1_2<T> Program.<>c__DisplayClass1_0<T>.CS$<>8__locals2""
    IL_0041:  stloc.2
    IL_0042:  ldloc.1
    IL_0043:  ldloc.2
    IL_0044:  stfld      ""T Program.<>c__DisplayClass1_0<T>.e""
    IL_0049:  ldloc.1
    IL_004a:  ldftn      ""bool Program.<>c__DisplayClass1_0<T>.<F>b__1()""
    IL_0050:  newobj     ""System.Func<bool>..ctor(object, System.IntPtr)""
    IL_0055:  callvirt   ""bool System.Func<bool>.Invoke()""
    IL_005a:  ldc.i4.0
    IL_005b:  cgt.un
    IL_005d:  endfilter
  }  // end filter
  {  // handler
    IL_005f:  pop
    IL_0060:  ldstr      ""pass_""
    IL_0065:  ldarg.0
    IL_0066:  ldfld      ""string Program.<>c__DisplayClass1_1<T>.x""
    IL_006b:  ldloc.1
    IL_006c:  ldfld      ""Program.<>c__DisplayClass1_2<T> Program.<>c__DisplayClass1_0<T>.CS$<>8__locals2""
    IL_0071:  ldfld      ""string Program.<>c__DisplayClass1_2<T>.y""
    IL_0076:  call       ""string string.Concat(string, string, string)""
    IL_007b:  call       ""void System.Console.Write(string)""
    IL_0080:  leave.s    IL_0082
  }
  IL_0082:  ret
}
");
        }

        [Fact]
        public void CatchVarLifted_Generic3()
        {
            string source = @"
using System;
using System.IO;

class Program
{
    static void Main()
    {
        F<IOException>();
    }

    static void F<T>() where T : Exception
    {
        string x = ""x"";

        new Action(() => 
        {
            string y = ""y"";

            try
            {
                throw new IOException(""a"");
            }
            catch (T e1) when (new Func<bool>(() => 
                {
                    string z = ""z"";

                    try 
                    {
                        throw new IOException(""xyz"");
                    }
                    catch (T e2) when (e2.Message == x + y + z)
                    {
                        return true;
                    }

                })())
            {
                Console.Write(""pass_"" + x + y);
            }
        })();
    }
}";
            var verifier = CompileAndVerify(source, expectedOutput: "pass_xy");
        }

        [Fact]
        public void ForeachParameterClosure01()
        {
            string source = @"using System;
using System.Collections.Generic;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        List<string> list = new List<string>(new string[] {""A"", ""B""});
        D d = null;
        foreach (string s in list)
        {
            if (s == ""A"")
            {
                d = () =>
                {
                    Console.WriteLine(s);
                };
            }
        }
        d();
    }
}";
            // Dev10 prints B, but we have intentionally changed the scope
            // of the loop variable.
            CompileAndVerify(source, expectedOutput: "A");
        }

        [Fact]
        public void LambdaParameterClosure01()
        {
            string source = @"using System;
delegate void D0();
delegate void D1(string s);
class C
{
    public static void Main(string[] args)
    {
        D0 d0 = null;
        D1 d1 = (string s) =>
        {
            d0 = () =>
            {
                Console.WriteLine(s);
            };
        };
        d1(""foo"");
        d0();
    }
}";
            CompileAndVerify(source, expectedOutput: "foo");
        }

        [Fact]
        public void BaseInvocation01()
        {
            string source = @"using System;
class B
{
    public virtual void F()
    {
        Console.WriteLine(""base"");
    }
}
class C : B
{
    public override void F()
    {
        Console.WriteLine(""derived"");
    }
    void Main()
    {
        base.F();
    }
    public static void Main(string[] args)
    {
        new C().Main();
    }
}";
            CompileAndVerify(source, expectedOutput: "base");
        }

        [Fact]
        public void BaseInvocationClosure01()
        {
            string source = @"using System;
delegate void D();

class B
{
    public virtual void F()
    {
        Console.WriteLine(""base"");
    }
}
class C : B
{
    public override void F()
    {
        Console.WriteLine(""derived"");
    }
    void Main()
    {
        D d = () =>
        {
            base.F();
        };
        d();
    }
    public static void Main(string[] args)
    {
        new C().Main();
    }
}";
            CompileAndVerify(source, expectedOutput: "base");
        }

        [Fact]
        public void BaseInvocationClosure02()
        {
            string source = @"using System;
delegate void D();

class B
{
    public virtual void F()
    {
        Console.WriteLine(""base"");
    }
}
class C : B
{
    public override void F()
    {
        Console.WriteLine(""derived"");
    }
    void Main(int x)
    {
        D d = () =>
        {
            x = x + 1;
            base.F();
        };
        d();
    }
    public static void Main(string[] args)
    {
        new C().Main(3);
    }
}";
            CompileAndVerify(source, expectedOutput: "base");
        }

        [Fact]
        public void BaseInvocationClosure03()
        {
            string source = @"using System;
delegate void D();

class B
{
    public virtual void F()
    {
        Console.WriteLine(""base"");
    }
}
class C : B
{
    public override void F()
    {
        Console.WriteLine(""derived"");
    }
    void Main(int x)
    {
        D d = base.F;
        d();
    }
    public static void Main(string[] args)
    {
        new C().Main(3);
    }
}";
            CompileAndVerify(source, expectedOutput: "base");
        }

        [Fact]
        public void BaseAccessInClosure_01()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B2 : B1
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestThis()
        {
            var s = ""this: "";
            Func<string> f = () => s + this.F();
            Console.WriteLine(f());
        }

        public void TestBase()
        {
            var s = ""base: "";
            Func<string> f = () => s + base.F();
            Console.WriteLine(f());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestThis();
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "this: D::F\r\nbase: B1::F");
        }

        [Fact]
        public void BaseAccessInClosure_02()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B1a : B1
    {
    }

    class B2 : B1a
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestThis()
        {
            var s = ""this: "";
            Func<string> f = () => s + this.F();
            Console.WriteLine(f());
        }

        public void TestBase()
        {
            var s = ""base: "";
            Func<string> f = () => s + base.F();
            Console.WriteLine(f());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestThis();
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "this: D::F\r\nbase: B1::F");
        }

        [Fact]
        public void BaseAccessInClosure_03_WithILCheck()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B1a : B1
    {
    }

    class B2 : B1a
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestBase()
        {
            Func<string> f = () => base.F();
            Console.WriteLine(f());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "B1::F").
                VerifyIL("M1.B2.<TestBase>b__1_0",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""string M1.B1.F()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_03a_WithILCheck()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B1a : B1
    {
        public override string F()
        {
            return ""B1a::F"";
        }

    }

    class B2 : B1a
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestBase()
        {
            Func<string> f = () => base.F();
            Console.WriteLine(f());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "B1a::F").
                VerifyIL("M1.B2.<TestBase>b__1_0",
@"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""string M1.B1a.F()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_04()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B2 : B1
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestThis()
        {
            Func<string> f = this.F;
            Console.WriteLine(f());
        }

        public void TestBase()
        {
            Func<string> f = base.F;
            Console.WriteLine(f());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestThis();
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "D::F\r\nB1::F");
        }

        [Fact]
        public void BaseAccessInClosure_05()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B2 : B1
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestThis()
        {
            Func<Func<string>> f = () => this.F;
            Console.WriteLine(f()());
        }

        public void TestBase()
        {
            Func<Func<string>> f = () => base.F;
            Console.WriteLine(f()());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestThis();
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "D::F\r\nB1::F");
        }

        [Fact]
        public void BaseAccessInClosure_06()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F()
        {
            return ""B1::F"";
        }
    }

    class B2 : B1
    {
        public override string F()
        {
            return ""B2::F"";
        }

        public void TestThis()
        {
            int s = 0;
            Func<Func<string>> f = () => { s++; return this.F; };
            Console.WriteLine(f()());
        }

        public void TestBase()
        {
            int s = 0;
            Func<Func<string>> f = () => { s++; return base.F; };
            Console.WriteLine(f()());
        }
    }

    class D : B2
    {
        public override string F()
        {
            return ""D::F"";
        }
    }

    static void Main()
    {
        (new D()).TestThis();
        (new D()).TestBase();
    }
}
";
            CompileAndVerify(source, expectedOutput: "D::F\r\nB1::F");
        }

        [Fact]
        public void BaseAccessInClosure_07()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual string F
        {
            get { Console.Write(""B1::F.Get;""); return null; }
            set { Console.Write(""B1::F.Set;""); }
        }
    }

    class B2 : B1
    {
        public override string F
        {
            get { Console.Write(""B2::F.Get;""); return null; }
            set { Console.Write(""B2::F.Set;""); }
        }

        public void Test()
        {
            int s = 0;
            Action f = () => { s++; this.F = base.F; base.F = this.F; };
            f();
        }
    }

    class D : B2
    {
        public override string F
        {
            get { Console.Write(""D::F.Get;""); return null; }
            set { Console.Write(""F::F.Set;""); }
        }
    }

    static void Main()
    {
        (new D()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F.Get;F::F.Set;D::F.Get;B1::F.Set;");
        }

        [Fact]
        public void BaseAccessInClosure_08()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual void F<T>(T t)
        {
            Console.Write(""B1::F;""); 
        }
    }

    class B2 : B1
    {
        public override void F<T>(T t)
        {
            Console.Write(""B2::F;""); 
        }

        public void Test()
        {
            int s = 0;
            Action f = () => { s++; this.F<int>(0); base.F<string>(""""); };
            f();
        }
    }

    class D : B2
    {
        public override void F<T>(T t)
        {
            Console.Write(""D::F;"");
        }
    }

    static void Main()
    {
        (new D()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "D::F;B1::F;");
        }

        [Fact]
        public void BaseAccessInClosure_09()
        {
            string source = @"using System;
static class M1
{

    class B1
    {
        public virtual void F<T>(T t)
        {
            Console.Write(""B1::F;""); 
        }
    }

    class B2 : B1
    {
        public override void F<T>(T t)
        {
            Console.Write(""B2::F;""); 
        }

        public void Test()
        {
            int s = 0;
            Func<Action<int>> f1 = () => { s++; return base.F<int>; };
            f1()(0);
            Func<Action<string>> f2 = () => { s++; return this.F<string>; };
            f2()(null);
        }
    }

    class D : B2
    {
        public override void F<T>(T t)
        {
            Console.Write(""D::F;"");
        }
    }

    static void Main()
    {
        (new D()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F;D::F;");
        }

        [Fact]
        public void BaseAccessInClosure_10()
        {
            string source = @"using System;
static class M1
{
    class B1<T>
    {
        public virtual void F<U>(T t, U u)
        {
            Console.Write(""B1::F;""); 
        }
    }

    class B2<T> : B1<T>
    {
        public override void F<U>(T t, U u)
        {
            Console.Write(""B2::F;""); 
        }

        public void Test()
        {
            int s = 0;
            Func<Action<T, int>> f1 = () => { s++; return base.F<int>; };
            f1()(default(T), 0);
            Func<Action<T, string>> f2 = () => { s++; return this.F<string>; };
            f2()(default(T), null);
        }
    }

    class D<T> : B2<T>
    {
        public override void F<U>(T t, U u)
        {
            Console.Write(""D::F;"");
        }
    }

    static void Main()
    {
        (new D<int>()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F;D::F;");
        }

        [Fact]
        public void BaseAccessInClosure_11()
        {
            string source = @"using System;
static class M1
{
    class B1<T>
    {
        public virtual void F<U>(T t, U u)
        {
            Console.Write(""B1::F;""); 
        }
    }

    class Outer<V>
    {
        public class B2 : B1<V>
        {
            public override void F<U>(V t, U u)
            {
                Console.Write(""B2::F;"");
            }

            public void Test()
            {
                int s = 0;
                Func<Action<V, int>> f1 = () => { s++; return base.F<int>; };
                f1()(default(V), 0);
                Func<Action<V, string>> f2 = () => { s++; return this.F<string>; };
                f2()(default(V), null);
            }
        }
    }

    class D<X> : Outer<X>.B2
    {
        public override void F<U>(X t, U u)
        {
            Console.Write(""D::F;"");
        }
    }

    static void Main()
    {
        (new D<int>()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F;D::F;");
        }

        [Fact]
        public void BaseAccessInClosure_12()
        {
            string source = @"using System;
static class M1
{
    public class Outer<T>
    {
        public class B1
        {
            public virtual string F1(T t)
            {
                return ""B1::F1;"";
            }
            public virtual string F2(T t)
            {
                return ""B1::F2;"";
            }
        }
    }

    public class B2 : Outer<int>.B1
    {
        public override string F1(int t)
        {
            return ""B2::F2;"";
        }

        public void Test()
        {
            int s = 0;
            Func<string> f1 = () => new Func<int, string>(this.F1)(s) + new Func<int, string>(base.F1)(s);
            Func<string> f2 = () => new Func<int, string>(this.F2)(s) + new Func<int, string>(base.F2)(s);
            Console.WriteLine(f1() + f2());
        }
    }

    class D : B2
    {
        public override string F1(int t)
        {
            return ""D::F2;"";
        }
        public override string F2(int t)
        {
            return ""D::F2;"";
        }
    }

    static void Main()
    {
        (new D()).Test();
    }
}
";
            CompileAndVerify(source, expectedOutput: "D::F2;B1::F1;D::F2;B1::F2;");
        }

        [Fact]
        public void BaseAccessInClosure_13()
        {
            string source = @"using System;
static class M1
{
    interface I{}

    class B1<T>
        where T : I
    {
        public virtual void F<U>(T t, U u) where U : struct, T
        {
            Console.Write(""B1::F;""); 
        }
    }

    class Outer<V>
        where V : struct, I
    {
        public class B2 : B1<V>
        {
            public override void F<U>(V t, U u)
            {
                Console.Write(""B2::F;"");
            }

            public void Test()
            {
                int s = 0;
                Func<Action<V, V>> f1 = () => { s++; return base.F<V>; };
                f1()(default(V), default(V));
                Func<Action<V, V>> f2 = () => { s++; return this.F<V>; };
                f2()(default(V), default(V));
            }
        }
    }

    class D<X> : Outer<X>.B2
        where X : struct, I
    {
        public override void F<U>(X t, U u)
        {
            Console.Write(""D::F;"");
        }
    }

    struct C : I { }

    static void Main()
    {
        (new D<C>()).Test();
    }
}";
            CompileAndVerify(source, expectedOutput: "B1::F;D::F;");
        }

        [Fact]
        public void BaseAccessInClosure_14_WithILCheck()
        {
            string source = @"using System;

static class M1
{
    public class Outer<T>
    {
        public class B1
        {
            public virtual string F<U>(T t, U u)
            {
                return ""B1:F"";
            }
        }
    }

    public class B2 : Outer<int>.B1
    {
        public override string F<U>(int t, U u)
        {
            return ""B2:F"";
        }

        public void Test()
        {
            int s = 0;
            Func<string> f1 = () => new Func<int, int, string>(base.F<int>)(s, s);
            Console.WriteLine(f1());
        }
    }

    static void Main()
    {
        (new B2()).Test();
    }
}";
            CompileAndVerify(source,
                expectedOutput: @"B1:F"
            ).
            VerifyIL("M1.B2.<>n__0<U>",
@"{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""string M1.Outer<int>.B1.F<U>(int, U)""
  IL_0008:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_15_WithILCheck()
        {
            string source = @"using System;
static class M1
{
    public interface I { }
    public interface II : I { }
    public class C : II { public C() { } }

    public class Outer<T>
        where T : I, new()
    {
        public class B1
        {
            public virtual string F<U>(T t, U u)
                where U : T, new()
            {
                return ""B1::F;"";
            }
        }
    }

    public class B2 : Outer<C>.B1
    {
        public override string F<U>(C t, U u)
        {
            return ""B2::F;"";
        }

        public void Test()
        {
            C s = new C();
            Func<string> f1 = () => new Func<C, C, string>(base.F)(s, s);
            Console.WriteLine(f1());
        }
    }

    static void Main()
    {
        (new B2()).Test();
    }
}";
            CompileAndVerify(source,
                expectedOutput: @"B1::F;"
            ).
            VerifyIL("M1.B2.<>c__DisplayClass1_0.<Test>b__0",
@"{
  // Code size       35 (0x23)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""M1.B2 M1.B2.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ldftn      ""string M1.B2.<>n__0<M1.C>(M1.C, M1.C)""
  IL_000c:  newobj     ""System.Func<M1.C, M1.C, string>..ctor(object, System.IntPtr)""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""M1.C M1.B2.<>c__DisplayClass1_0.s""
  IL_0017:  ldarg.0
  IL_0018:  ldfld      ""M1.C M1.B2.<>c__DisplayClass1_0.s""
  IL_001d:  callvirt   ""string System.Func<M1.C, M1.C, string>.Invoke(M1.C, M1.C)""
  IL_0022:  ret
}").
            VerifyIL("M1.B2.<>n__0<U>",
@"{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""string M1.Outer<M1.C>.B1.F<U>(M1.C, U)""
  IL_0008:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_16()
        {
            string source = @"using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.CompilerServices;

static class M1
{
    public interface I { }
    public interface II : I { }
    public class C : II { public C() { } }

    public class Outer<T>
        where T : I, new()
    {
        public class B1
        {
            public virtual string F<U>(T t, U u)
                where U : I, T, new()
            {
                return ""B1::F;"";
            }
        }
    }

    public class B2 : Outer<C>.B1
    {
        public override string F<U>(C t, U u)
        {
            return ""B2::F;"";
        }

        public void Test()
        {
            C s = new C();
            Func<string> f1 = () => new Func<C, C, string>(base.F)(s, s);
            Console.WriteLine(f1());
        }
    }

    static void Main()
    {
        var type = (new B2()).GetType();
        var method = type.GetMethod(""<>n__0"", BindingFlags.NonPublic | BindingFlags.Instance);
        Console.WriteLine(Attribute.IsDefined(method, typeof(CompilerGeneratedAttribute)));
        Console.WriteLine(method.IsPrivate);
        Console.WriteLine(method.IsVirtual);
        Console.WriteLine(method.IsGenericMethod);
        var genericDef = method.GetGenericMethodDefinition();
        var arguments = genericDef.GetGenericArguments();
        Console.WriteLine(arguments.Length);
        var arg0 = arguments[0];
        GenericParameterAttributes attributes = arg0.GenericParameterAttributes;
        Console.WriteLine(attributes.ToString());
        var arg0constraints = arg0.GetGenericParameterConstraints();
        Console.WriteLine(arg0constraints.Length);
        Console.WriteLine(arg0constraints[0]);
        Console.WriteLine(arg0constraints[1]);
    }
}";
            CompileAndVerify(source,
                expectedOutput: @"
True
True
False
True
1
DefaultConstructorConstraint
2
M1+I
M1+C"
            );
        }

        [Fact]
        public void BaseAccessInClosure_17_WithILCheck()
        {
            string source = @"using System;

class Base<T>
{
    public virtual void Func<U>(T t, U u)
    {
        Console.Write(typeof (T));
        Console.Write("" "");
        Console.Write(typeof (U));
    }

    public class Derived : Base<int>
    {
        public void Test()
        {
            int i = 0;
            T t = default(T);
            Action d = () => { base.Func<T>(i, t); };
            d();
        }
    }
}

class Program
{
    static void Main()
    {
        new Base<string>.Derived().Test();
    }
}
";
            CompileAndVerify(source,
                expectedOutput: "System.Int32 System.String"
            ).
            VerifyIL("Base<T>.Derived.<>n__0<U>",
@"{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""void Base<int>.Func<U>(int, U)""
  IL_0008:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_18_WithILCheck()
        {
            string source = @"using System;

class Base
{
    public void F(int i) { Console.Write(""Base::F;""); }
}

class Derived : Base
{
    public new void F(int i) { Console.Write(""Derived::F;""); }
    public void Test()
    {
        int j = 0;
        Action a = () => { base.F(j); this.F(j); };
        a();
    }
    static void Main(string[] args)
    {
        new Derived().Test();
    }
}
";
            CompileAndVerify(source,
                expectedOutput: "Base::F;Derived::F;"
            ).
            VerifyIL("Derived.<>c__DisplayClass1_0.<Test>b__0",
@"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived Derived.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ldarg.0
  IL_0007:  ldfld      ""int Derived.<>c__DisplayClass1_0.j""
  IL_000c:  call       ""void Base.F(int)""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""Derived Derived.<>c__DisplayClass1_0.<>4__this""
  IL_0017:  ldarg.0
  IL_0018:  ldfld      ""int Derived.<>c__DisplayClass1_0.j""
  IL_001d:  call       ""void Derived.F(int)""
  IL_0022:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_19_WithILCheck()
        {
            string source = @"using System;

class Base
{
    public void F(int i) { Console.Write(""Base::F;""); }
}

class Derived : Base
{
    public new void F(int i) { Console.Write(""Derived::F;""); }
    public void Test()
    {
        int j = 0;
        Action a = () => { Action<int> b = base.F; b(j); };
        a();
    }
    static void Main(string[] args)
    {
        new Derived().Test();
    }
}
";
            CompileAndVerify(source,
                expectedOutput: "Base::F;"
            ).
            VerifyIL("Derived.<>c__DisplayClass1_0.<Test>b__0",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived Derived.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ldftn      ""void Base.F(int)""
  IL_000c:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int Derived.<>c__DisplayClass1_0.j""
  IL_0017:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void BaseAccessInClosure_20_WithILCheck()
        {
            string source = @"using System;

class Base
{
    public void F(int i) { Console.Write(""Base::F;""); }
}

class Derived : Base
{
    public new void F(int i) { Console.Write(""Derived::F;""); }
    public void Test()
    {
        int j = 0;
        Action a = () => { Action<int> b = new Action<int>(base.F); b(j); };
        a();
    }
    static void Main(string[] args)
    {
        new Derived().Test();
    }
}
";
            CompileAndVerify(source,
                expectedOutput: "Base::F;"
            ).
            VerifyIL("Derived.<>c__DisplayClass1_0.<Test>b__0",
@"{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived Derived.<>c__DisplayClass1_0.<>4__this""
  IL_0006:  ldftn      ""void Base.F(int)""
  IL_000c:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0011:  ldarg.0
  IL_0012:  ldfld      ""int Derived.<>c__DisplayClass1_0.j""
  IL_0017:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_001c:  ret
}");
        }

        [Fact]
        public void UnsafeInvocationClosure01()
        {
            string source = @"using System;
delegate void D();

class C
{
    static unsafe void F()
    {
        D d = () =>
        {
            Console.WriteLine(""F"");
        };
        d();
    }
    public static void Main(string[] args)
    {
        D d = () =>
        {
            F();
        };
        d();
    }
}";
            CompileAndVerify(source, options: TestOptions.UnsafeReleaseExe, expectedOutput: "F");
        }

        [Fact]
        public void LambdaWithParameters01()
        {
            var source = @"
delegate void D(int x);
class LWP
{
    public static void Main(string[] args)
    {
        D d1 = x => {
            System.Console.Write(x);
        };
        d1(123);
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void LambdaWithParameters02()
        {
            var source = @"
delegate void D(int x);
class LWP
{
    public static void Main(string[] args)
    {
        int local;
        D d1 = x => {
            local = 12;
            System.Console.Write(x);
        };
        d1(123);
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void LambdaWithParameters03()
        {
            var source = @"
delegate void D(int x);
class LWP
{
    void M()
    {
        D d1 = x => {
            System.Console.Write(x);
        };
        d1(123);
    }
    public static void Main(string[] args)
    {
        new LWP().M();
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void LambdaWithParameters04()
        {
            var source = @"
delegate void D(int x);
class LWP
{
    void M()
    {
        int local;
        D d1 = x => {
            local = 2;
            System.Console.Write(x);
        };
        d1(123);
    }
    public static void Main(string[] args)
    {
        new LWP().M();
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void CapturedLambdaParameter01()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    public static void Main(string[] args)
    {
        D d1 = x => y => z => {
            System.Console.Write(x + y + z);
            return null;
        };
        d1(100)(20)(3);
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void CapturedLambdaParameter02()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    void M()
    {
        D d1 = x => y => z => {
            System.Console.Write(x + y + z);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        new CLP().M();
    }
}
";
            CompileAndVerify(source, expectedOutput: "123");
        }

        [Fact]
        public void CapturedLambdaParameter03()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    public static void Main(string[] args)
    {
        int K = 4000;
        D d1 = x => y => z => {
            System.Console.Write(x + y + z + K);
            return null;
        };
        d1(100)(20)(3);
    }
}
";
            CompileAndVerify(source, expectedOutput: "4123");
        }

        [Fact]
        public void CapturedLambdaParameter04()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    void M()
    {
        int K = 4000;
        D d1 = x => y => z => {
            System.Console.Write(x + y + z + K);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        new CLP().M();
    }
}
";
            CompileAndVerify(source, expectedOutput: "4123");
        }

        [Fact]
        public void GenericClosure01()
        {
            string source = @"using System;
delegate void D();

class G<T>
{
    public static void F(T t)
    {
        D d = () =>
        {
            Console.WriteLine(t);
        };
        d();
    }
}

class C
{
    public static void Main(string[] args)
    {
        G<int>.F(12);
        G<string>.F(""foo"");
    }
}";

            CompileAndVerify(source, expectedOutput: @"
12
foo
");
        }

        [Fact]
        public void GenericClosure02()
        {
            string source = @"using System;
delegate void D();

class G
{
    public static void F<T>(T t)
    {
        D d = () =>
        {
            Console.WriteLine(t);
        };
        d();
    }
}

class C
{
    public static void Main(string[] args)
    {
        G.F<int>(12);
        G.F<string>(""foo"");
    }
}";

            CompileAndVerify(source, expectedOutput: @"
12
foo
");
        }

        [Fact]
        public void GenericClosure03()
        {
            var source = @"using System;

delegate U D<U>();
class GenericClosure
{
    static D<T> Default<T>(T value)
    {
        return () => value;
    }

    public static void Main(string[] args)
    {
        D<string> dHello = Default(""Hello"");
        Console.WriteLine(dHello());
        D<int> d1234 = Default(1234);
        Console.WriteLine(d1234());
    }
}
";
            var expectedOutput = @"Hello
1234";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void GenericClosure04()
        {
            var source = @"using System;

delegate T D<T>();
class GenericClosure
{
    static D<T> Default<T>(T value)
    {
        return () => default(T);
    }

    public static void Main(string[] args)
    {
        D<string> dHello = Default(""Hello"");
        Console.WriteLine(dHello());
        D<int> d1234 = Default(1234);
        Console.WriteLine(d1234());
    }
}
";
            var expectedOutput = @"
0";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void GenericCapturedLambdaParameter01()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    void M<T>()
    {
        D d1 = x => y => z => {
            System.Console.Write(x + y + z);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        new CLP().M<int>();
        new CLP().M<string>();
    }
}
";
            CompileAndVerify(source, expectedOutput: "123123");
        }

        [Fact]
        public void GenericCapturedLambdaParameter02()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    static void M<T>()
    {
        D d1 = x => y => z => {
            System.Console.Write(x + y + z);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        CLP.M<int>();
        CLP.M<string>();
    }
}
";
            CompileAndVerify(source, expectedOutput: "123123");
        }

        [Fact]
        public void GenericCapturedLambdaParameter03()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    void M<T>()
    {
        int K = 4000;
        D d1 = x => y => z => {
            System.Console.Write(x + y + z + K);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        new CLP().M<int>();
        new CLP().M<string>();
    }
}
";
            CompileAndVerify(source, expectedOutput: "41234123");
        }

        [Fact]
        public void GenericCapturedLambdaParameter04()
        {
            var source = @"
delegate D D(int x);
class CLP
{
    static void M<T>()
    {
        int K = 4000;
        D d1 = x => y => z => {
            System.Console.Write(x + y + z + K);
            return null;
        };
        d1(100)(20)(3);
    }
    public static void Main(string[] args)
    {
        CLP.M<int>();
        CLP.M<string>();
    }
}
";
            CompileAndVerify(source, expectedOutput: "41234123");
        }

        [Fact]
        public void GenericCapturedTypeParameterLocal()
        {
            var source = @"
delegate void D();
class CLP
{
    static void M<T>(T t0)
    {
        T t1 = t0;
        D d = () => {
            T t2 = t1;
            System.Console.Write("""" + t0 + t1 + t2);
        };
        d();
    }
    public static void Main(string[] args)
    {
        CLP.M<int>(0);
        CLP.M<string>(""h"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "000hhh");
        }

        [Fact]
        public void CaptureConstructedMethodInvolvingTypeParameter()
        {
            var source = @"
delegate void D();
class CLP
{
    static void P<T>(T t0, T t1, T t2)
    {
        System.Console.Write(string.Concat(t0, t1, t2));
    }
    static void M<T>(T t0)
    {
        T t1 = t0;
        D d = () => {
            T t2 = t1;
            P(t0, t1, t2);
        };
        d();
    }
    public static void Main(string[] args)
    {
        CLP.M<int>(0);
        CLP.M<string>(""h"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "000hhh");
        }

        [Fact]
        public void CaptureConstructionInvolvingTypeParameter()
        {
            var source = @"
delegate void D();
class CLP
{
    class P<T>
    {
        public P(T t0, T t1, T t2)
        {
            System.Console.Write(string.Concat(t0, t1, t2));
        }
    }
    static void M<T>(T t0)
    {
        T t1 = t0;
        D d = () =>
        {
            T t2 = t1;
            new P<T>(t0, t1, t2);
        };
        d();
    }
    public static void Main(string[] args)
    {
        CLP.M<int>(0);
        CLP.M<string>(""h"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "000hhh");
        }

        [Fact]
        public void CaptureDelegateConversionInvolvingTypeParameter()
        {
            var source = @"
delegate void D();
class CLP
{
    delegate void D3<T>(T t0, T t1, T t2);

    public static void P<T>(T t0, T t1, T t2)
    {
        System.Console.Write(string.Concat(t0, t1, t2));
    }
    static void M<T>(T t0)
    {
        T t1 = t0;
        D d = () =>
        {
            T t2 = t1;
            D3<T> d3 = P;
            d3(t0, t1, t2);
        };
        d();
    }
    public static void Main(string[] args)
    {
        CLP.M<int>(0);
        CLP.M<string>(""h"");
    }
}
";
            CompileAndVerify(source, expectedOutput: "000hhh");
        }

        [Fact]
        public void CaptureFieldInvolvingTypeParameter()
        {
            var source = @"
delegate void D();
class CLP
{
    class HolderClass<T>
    {
        public static T t;
    }

    delegate void D3<T>(T t0, T t1, T t2);

    static void M<T>()
    {
        D d = () =>
        {
            System.Console.Write(string.Concat(HolderClass<T>.t));
        };
        d();
    }
    public static void Main(string[] args)
    {
        HolderClass<int>.t = 12;
        HolderClass<string>.t = ""he"";
        CLP.M<int>();
        CLP.M<string>();
    }
}
";
            CompileAndVerify(source, expectedOutput: "12he");
        }

        [Fact]
        public void CaptureReadonly01()
        {
            var source = @"
using System;
 
class Program
{
    private static readonly int v = 5;
    delegate int del(int i);
    static void Main(string[] args)
    {
        del myDelegate = (int x) => x * v;
        Console.Write(string.Concat(myDelegate(3), ""he""));
    }
}";
            CompileAndVerify(source, expectedOutput: "15he");
        }

        [Fact]
        public void CaptureReadonly02()
        {
            var source = @"
using System;
 
class Program
{
    private readonly int v = 5;
    delegate int del(int i);
    void M()
    {
        del myDelegate = (int x) => x * v;
        Console.Write(string.Concat(myDelegate(3), ""he""));
    }
    static void Main(string[] args)
    {
        new Program().M();
    }
}";
            CompileAndVerify(source, expectedOutput: "15he");
        }

        [Fact]
        public void CaptureLoopIndex()
        {
            var source = @"
using System;
delegate void D();
class Program
{
    static void Main(string[] args)
    {
        D d0 = null;
        for (int i = 0; i < 4; i++)
        {
            D d = d0 = () =>
            {
                Console.Write(i);
            };
            d();
        }
        d0();
    }
}";
            CompileAndVerify(source, expectedOutput: "01234");
        }

        // see Roslyn bug 5956
        [Fact]
        public void CapturedIncrement()
        {
            var source = @"
class Program
{
    delegate int Func();
    static void Main(string[] args)
    {
        int i = 0;
        Func query;
        if (true)
        {
            query = () =>
            {
                i = 6;
                Foo(i++);
                return i;
            };
        }

        i = 3;
        System.Console.WriteLine(query.Invoke());
    }

    public static int Foo(int i)
    {
        i = 4;
        return i;
    }
}
";
            CompileAndVerify(source, expectedOutput: "7");
        }

        [Fact]
        public void StructDelegate()
        {
            var source = @"
using System;
class Program
{
    static void Main()
    {
        int x = 42;
        Func<string> f = x.ToString;
        Console.Write(f.Invoke());
    }
}"
;
            CompileAndVerify(source, expectedOutput: "42");
        }

        [Fact]
        public void StructDelegate1()
        {
            var source = @"
using System;
class Program
{
    public static void Foo<T>(T x)
    {
        Func<string> f = x.ToString;
        Console.Write(f.Invoke());       
    }

    static void Main()
    {
        string s = ""Hi"";
        Foo(s);

        int x = 42;
        Foo(x);
    }
}"
;
            CompileAndVerify(source, expectedOutput: "Hi42");
        }

        [Fact]
        public void StaticDelegateFromMethodGroupInLambda()
        {
            var source = @"using System;
class Program
{
    static void M()
    {
        Console.WriteLine(12);
    }
    public static void Main(string[] args)
    {
        Action a = () => {
            Action b = new Action(M);
            b();
        };
        a();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [Fact]
        public void StaticDelegateFromMethodGroupInLambda2()
        {
            var source = @"using System;
class Program
{
    static void M()
    {
        Console.WriteLine(12);
    }
    void G()
    {
        Action a = () => {
            Action b = new Action(M);
            b();
        };
        a();
    }
    public static void Main(string[] args)
    {
        new Program().G();
    }
}";
            CompileAndVerify(source, expectedOutput: "12");
        }

        [WorkItem(539346, "DevDiv")]
        [Fact]
        public void CachedLambdas()
        {
            var source = @"using System;

public class Program
{
    private static void Assert(bool b)
    {
        if (!b) throw new Exception();
    }

    public static void Test1()
    {
        Action a0 = null;
        for (int i = 0; i < 2; i++)
        {
            Action a = () => { };
            if (i == 0)
            {
                a0 = a;
            }
            else
            {
                Assert(ReferenceEquals(a, a0));
            }
        }
    }

    public void Test2()
    {
        Action a0 = null;
        for (int i = 0; i < 2; i++)
        {
            Action a = () => { };
            if (i == 0)
            {
                a0 = a;
            }
            else
            {
                Assert(ReferenceEquals(a, a0));
            }
        }
    }

    public static void Test3()
    {
        int inEnclosing = 12;
        Func<int> a0 = null;
        for (int i = 0; i < 2; i++)
        {
            Func<int> a = () => inEnclosing;
            if (i == 0)
            {
                a0 = a;
            }
            else
            {
                Assert(ReferenceEquals(a, a0));
            }
        }
    }

    public void Test4()
    {
        Func<Program> a0 = null;
        for (int i = 0; i < 2; i++)
        {
            Func<Program> a = () => this;
            if (i == 0)
            {
                a0 = a;
            }
            else
            {
                Assert(ReferenceEquals(a, a0)); // Roslyn misses this
            }
        }
    }

    public static void Test5()
    {
        int i = 12;
        Func<Action> D = () => () => Console.WriteLine(i);
        Action a1 = D();
        Action a2 = D();
        Assert(ReferenceEquals(a1, a2)); // native compiler misses this
    }

    public static void Test6()
    {
        Func<int> a1 = Foo<int>.Bar<int>();
        Func<int> a2 = Foo<int>.Bar<int>();
        Assert(ReferenceEquals(a1, a2)); // both native compiler and Roslyn miss this
    }

    public static void Main(string[] args)
    {
        Test1();
        new Program().Test2();
        Test3();
//        new Program().Test4();
        Test5();
//        Test6();
    }
}

class Foo<T>
{
    static T t;
    public static Func<U> Bar<U>()
    {
        return () => Foo<U>.t;
    }
}";
            CompileAndVerify(source, expectedOutput: "");
        }

        [Fact]
        public void ParentFrame01()
        {
            //IMPORTANT: the parent frame field in Program.c1.<>c__DisplayClass1 should be named CS$<>8__locals, not <>4__this.

            string source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        var t = new c1();
        t.Test();
    }

    class c1
    {
        int x = 1;

        public void Test()
        {
            int y = 2;
            Func<Func<Func<int, Func<int, int>>>> ff = null;

            if (2.ToString() != null)
            {
                int a = 4;

                ff = () => () => (z) => (zz) => x + y + z + a + zz;
            }

            Console.WriteLine(ff()()(3)(3));
        }
    }
}";
            CompileAndVerify(source, expectedOutput: "13").
            VerifyIL("Program.c1.<>c__DisplayClass1_2.<Test>b__2",
@"{
  // Code size       31 (0x1f)
  .maxstack  3
  IL_0000:  newobj     ""Program.c1.<>c__DisplayClass1_1..ctor()""
  IL_0005:  dup
  IL_0006:  ldarg.0
  IL_0007:  stfld      ""Program.c1.<>c__DisplayClass1_2 Program.c1.<>c__DisplayClass1_1.CS$<>8__locals2""
  IL_000c:  dup
  IL_000d:  ldarg.1
  IL_000e:  stfld      ""int Program.c1.<>c__DisplayClass1_1.z""
  IL_0013:  ldftn      ""int Program.c1.<>c__DisplayClass1_1.<Test>b__3(int)""
  IL_0019:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_001e:  ret
}");
        }

        [Fact]
        public void ParentFrame02()
        {
            string source = @"
using System;
    class Program
    {
        static void Main(string[] args)
        {
            var t = new c1();
            t.Test();
        }

        class c1
        {
            int x = 1;

            public void Test()
            {
                int y = 2;
                Func<Func<Func<int, Func<int>>>> ff = null;

                if (2.ToString() != null)
                {
                    int a = 4;
                    ff = () => () => (z) => () => x + y + z + a;
                }

                if (2.ToString() != null)
                {
                    int a = 4;
                    ff = () => () => (z) => () => x + y + z + a;
                }

                Console.WriteLine(ff()()(3)());
            }
        }
    }
";
            CompileAndVerify(source, expectedOutput: "10").
            VerifyIL("Program.c1.Test",
@"{
  // Code size      134 (0x86)
  .maxstack  3
  .locals init (Program.c1.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                System.Func<System.Func<System.Func<int, System.Func<int>>>> V_1, //ff
                int V_2)
  IL_0000:  newobj     ""Program.c1.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Program.c1 Program.c1.<>c__DisplayClass1_0.<>4__this""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.2
  IL_000f:  stfld      ""int Program.c1.<>c__DisplayClass1_0.y""
  IL_0014:  ldnull
  IL_0015:  stloc.1
  IL_0016:  ldc.i4.2
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  call       ""string int.ToString()""
  IL_001f:  brfalse.s  IL_0040
  IL_0021:  newobj     ""Program.c1.<>c__DisplayClass1_2..ctor()""
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      ""Program.c1.<>c__DisplayClass1_0 Program.c1.<>c__DisplayClass1_2.CS$<>8__locals1""
  IL_002d:  dup
  IL_002e:  ldc.i4.4
  IL_002f:  stfld      ""int Program.c1.<>c__DisplayClass1_2.a""
  IL_0034:  ldftn      ""System.Func<System.Func<int, System.Func<int>>> Program.c1.<>c__DisplayClass1_2.<Test>b__0()""
  IL_003a:  newobj     ""System.Func<System.Func<System.Func<int, System.Func<int>>>>..ctor(object, System.IntPtr)""
  IL_003f:  stloc.1
  IL_0040:  ldc.i4.2
  IL_0041:  stloc.2
  IL_0042:  ldloca.s   V_2
  IL_0044:  call       ""string int.ToString()""
  IL_0049:  brfalse.s  IL_006a
  IL_004b:  newobj     ""Program.c1.<>c__DisplayClass1_4..ctor()""
  IL_0050:  dup
  IL_0051:  ldloc.0
  IL_0052:  stfld      ""Program.c1.<>c__DisplayClass1_0 Program.c1.<>c__DisplayClass1_4.CS$<>8__locals3""
  IL_0057:  dup
  IL_0058:  ldc.i4.4
  IL_0059:  stfld      ""int Program.c1.<>c__DisplayClass1_4.a""
  IL_005e:  ldftn      ""System.Func<System.Func<int, System.Func<int>>> Program.c1.<>c__DisplayClass1_4.<Test>b__4()""
  IL_0064:  newobj     ""System.Func<System.Func<System.Func<int, System.Func<int>>>>..ctor(object, System.IntPtr)""
  IL_0069:  stloc.1
  IL_006a:  ldloc.1
  IL_006b:  callvirt   ""System.Func<System.Func<int, System.Func<int>>> System.Func<System.Func<System.Func<int, System.Func<int>>>>.Invoke()""
  IL_0070:  callvirt   ""System.Func<int, System.Func<int>> System.Func<System.Func<int, System.Func<int>>>.Invoke()""
  IL_0075:  ldc.i4.3
  IL_0076:  callvirt   ""System.Func<int> System.Func<int, System.Func<int>>.Invoke(int)""
  IL_007b:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0080:  call       ""void System.Console.WriteLine(int)""
  IL_0085:  ret
}");
        }

        [Fact]
        public void ParentFrame03()
        {
            string source = @"
using System;
    class Program
    {
        static void Main(string[] args)
        {
            var t = new c1();
            t.Test();
        }

        class c1
        {
            int x = 1;

            public void Test()
            {
                int y = 2;
                Func<Func<Func<Func<int>>>> ff = null;

                if (2.ToString() != null)
                {
                    int z = 3;
                    ff = () => () => () => () => x + y + z;
                }

                if (2.ToString() != null)
                {
                    int z = 3;
                    ff = () => () => () => () => x + y + z;
                }

                Console.WriteLine(ff()()()());
            }
        }
    }
";
            CompileAndVerify(source, expectedOutput: "6").
            VerifyIL("Program.c1.Test",
@"{
  // Code size      133 (0x85)
  .maxstack  3
  .locals init (Program.c1.<>c__DisplayClass1_0 V_0, //CS$<>8__locals0
                System.Func<System.Func<System.Func<System.Func<int>>>> V_1, //ff
                int V_2)
  IL_0000:  newobj     ""Program.c1.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Program.c1 Program.c1.<>c__DisplayClass1_0.<>4__this""
  IL_000d:  ldloc.0
  IL_000e:  ldc.i4.2
  IL_000f:  stfld      ""int Program.c1.<>c__DisplayClass1_0.y""
  IL_0014:  ldnull
  IL_0015:  stloc.1
  IL_0016:  ldc.i4.2
  IL_0017:  stloc.2
  IL_0018:  ldloca.s   V_2
  IL_001a:  call       ""string int.ToString()""
  IL_001f:  brfalse.s  IL_0040
  IL_0021:  newobj     ""Program.c1.<>c__DisplayClass1_1..ctor()""
  IL_0026:  dup
  IL_0027:  ldloc.0
  IL_0028:  stfld      ""Program.c1.<>c__DisplayClass1_0 Program.c1.<>c__DisplayClass1_1.CS$<>8__locals1""
  IL_002d:  dup
  IL_002e:  ldc.i4.3
  IL_002f:  stfld      ""int Program.c1.<>c__DisplayClass1_1.z""
  IL_0034:  ldftn      ""System.Func<System.Func<System.Func<int>>> Program.c1.<>c__DisplayClass1_1.<Test>b__0()""
  IL_003a:  newobj     ""System.Func<System.Func<System.Func<System.Func<int>>>>..ctor(object, System.IntPtr)""
  IL_003f:  stloc.1
  IL_0040:  ldc.i4.2
  IL_0041:  stloc.2
  IL_0042:  ldloca.s   V_2
  IL_0044:  call       ""string int.ToString()""
  IL_0049:  brfalse.s  IL_006a
  IL_004b:  newobj     ""Program.c1.<>c__DisplayClass1_2..ctor()""
  IL_0050:  dup
  IL_0051:  ldloc.0
  IL_0052:  stfld      ""Program.c1.<>c__DisplayClass1_0 Program.c1.<>c__DisplayClass1_2.CS$<>8__locals2""
  IL_0057:  dup
  IL_0058:  ldc.i4.3
  IL_0059:  stfld      ""int Program.c1.<>c__DisplayClass1_2.z""
  IL_005e:  ldftn      ""System.Func<System.Func<System.Func<int>>> Program.c1.<>c__DisplayClass1_2.<Test>b__4()""
  IL_0064:  newobj     ""System.Func<System.Func<System.Func<System.Func<int>>>>..ctor(object, System.IntPtr)""
  IL_0069:  stloc.1
  IL_006a:  ldloc.1
  IL_006b:  callvirt   ""System.Func<System.Func<System.Func<int>>> System.Func<System.Func<System.Func<System.Func<int>>>>.Invoke()""
  IL_0070:  callvirt   ""System.Func<System.Func<int>> System.Func<System.Func<System.Func<int>>>.Invoke()""
  IL_0075:  callvirt   ""System.Func<int> System.Func<System.Func<int>>.Invoke()""
  IL_007a:  callvirt   ""int System.Func<int>.Invoke()""
  IL_007f:  call       ""void System.Console.WriteLine(int)""
  IL_0084:  ret
}
");
        }

        [Fact]
        public void ParentFrame04()
        {
            string source = @"
using System;
public static class Program
{
    public static void Main()
    {
        var c = new c1();
        System.Console.WriteLine(c.Test());
    }

    class c1
    {
        public int foo = 42;

        public object Test()
        {
            if (T())
            {
                Func<int, Boolean> a = (s) => s == foo && ((Func<bool>)(() => s == foo)).Invoke();

                return a.Invoke(42);
            }

            int aaa = 42;

            if (T())
            {
                Func<int, bool> a = (s) => aaa == foo;
                return a.Invoke(42);
            }

            return null;
        }

        private bool T()
        {
            return true;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "True").
            VerifyIL("Program.c1.Test",
@"
{
  // Code size       89 (0x59)
  .maxstack  2
  .locals init (Program.c1.<>c__DisplayClass1_1 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.c1.<>c__DisplayClass1_1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.0
  IL_0008:  stfld      ""Program.c1 Program.c1.<>c__DisplayClass1_1.<>4__this""
  IL_000d:  ldarg.0
  IL_000e:  call       ""bool Program.c1.T()""
  IL_0013:  brfalse.s  IL_002e
  IL_0015:  ldarg.0
  IL_0016:  ldftn      ""bool Program.c1.<Test>b__1_0(int)""
  IL_001c:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_0021:  ldc.i4.s   42
  IL_0023:  callvirt   ""bool System.Func<int, bool>.Invoke(int)""
  IL_0028:  box        ""bool""
  IL_002d:  ret
  IL_002e:  ldloc.0
  IL_002f:  ldc.i4.s   42
  IL_0031:  stfld      ""int Program.c1.<>c__DisplayClass1_1.aaa""
  IL_0036:  ldarg.0
  IL_0037:  call       ""bool Program.c1.T()""
  IL_003c:  brfalse.s  IL_0057
  IL_003e:  ldloc.0
  IL_003f:  ldftn      ""bool Program.c1.<>c__DisplayClass1_1.<Test>b__2(int)""
  IL_0045:  newobj     ""System.Func<int, bool>..ctor(object, System.IntPtr)""
  IL_004a:  ldc.i4.s   42
  IL_004c:  callvirt   ""bool System.Func<int, bool>.Invoke(int)""
  IL_0051:  box        ""bool""
  IL_0056:  ret
  IL_0057:  ldnull
  IL_0058:  ret
}
");
        }

        [Fact]
        public void ParentFrame05()
        {
            // IMPORTANT: this code should not initialize any fields in Program.c1.<>c__DisplayClass0 except "a"
            //            Program.c1.<>c__DisplayClass0 should not capture any frame pointers.

            string source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        var t = new c1();
        t.Test();
    }

    class c1
    {
        int x = 1;

        public void Test()
        {
            Func<int> ff = null;
            Func<int> aa = null;

            if (T())
            {
                int a = 1;
                if (T())
                {                            
                    int b = 4;

                    ff = () => a + b;
                    aa = () => x;
                }
            }

            Console.WriteLine(ff() + aa());
        }

        private bool T()
        {
            return true;
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "6").
            VerifyIL("Program.c1.Test",
@"{
  // Code size       96 (0x60)
  .maxstack  3
  .locals init (System.Func<int> V_0, //ff
                System.Func<int> V_1, //aa
                Program.c1.<>c__DisplayClass1_0 V_2) //CS$<>8__locals0
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldnull
  IL_0003:  stloc.1
  IL_0004:  ldarg.0
  IL_0005:  call       ""bool Program.c1.T()""
  IL_000a:  brfalse.s  IL_004d
  IL_000c:  newobj     ""Program.c1.<>c__DisplayClass1_0..ctor()""
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  ldc.i4.1
  IL_0014:  stfld      ""int Program.c1.<>c__DisplayClass1_0.a""
  IL_0019:  ldarg.0
  IL_001a:  call       ""bool Program.c1.T()""
  IL_001f:  brfalse.s  IL_004d
  IL_0021:  newobj     ""Program.c1.<>c__DisplayClass1_1..ctor()""
  IL_0026:  dup
  IL_0027:  ldloc.2
  IL_0028:  stfld      ""Program.c1.<>c__DisplayClass1_0 Program.c1.<>c__DisplayClass1_1.CS$<>8__locals1""
  IL_002d:  dup
  IL_002e:  ldc.i4.4
  IL_002f:  stfld      ""int Program.c1.<>c__DisplayClass1_1.b""
  IL_0034:  ldftn      ""int Program.c1.<>c__DisplayClass1_1.<Test>b__0()""
  IL_003a:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_003f:  stloc.0
  IL_0040:  ldarg.0
  IL_0041:  ldftn      ""int Program.c1.<Test>b__1_1()""
  IL_0047:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_004c:  stloc.1
  IL_004d:  ldloc.0
  IL_004e:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0053:  ldloc.1
  IL_0054:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0059:  add
  IL_005a:  call       ""void System.Console.WriteLine(int)""
  IL_005f:  ret
}");
        }

        [Fact]
        public void ClosuresInConstructorAndInitializers1()
        {
            string source = @"
using System;

class C
{
    int f1 = new Func<int, int>(x => 1)(1);
    int f2 = new Func<int, int>(x => 2)(1);

    C()
    {
        int l = new Func<int, int>(x => 3)(1);
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void ClosuresInConstructorAndInitializers2()
        {
            string source = @"
using System;

class C
{
    int f1 = ((Func<int, int>)(x => ((Func<int>)(() => x + 2))() + x))(1);
    int f2 = ((Func<int, int>)(x => ((Func<int>)(() => x + 2))() + x))(1);

    C()
    {
        int l = ((Func<int, int>)(x => ((Func<int>)(() => x + 4))() + x))(1);
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void ClosuresInConstructorAndInitializers3()
        {
            string source = @"
using System;

class C
{
    static int f1 = ((Func<int, int>)(x => ((Func<int>)(() => x + 2))() + x))(1);
    static int f2 = ((Func<int, int>)(x => ((Func<int>)(() => x + 2))() + x))(1);

    static C()
    {
        int l = ((Func<int, int>)(x => ((Func<int>)(() => x + 4))() + x))(1);
    }
}";
            CompileAndVerify(source);
        }

        [Fact]
        public void GenericStaticFrames()
        {
            string source = @"
using System;

public class C
{
	public static void F<TF>()
	{
	    var f = new Func<TF>(() => default(TF));
	}
	
	public static void G<TG>()
	{
		var f = new Func<TG>(() => default(TG));
	}
	
	public static void F<TF1, TF2>()
	{
		var f = new Func<TF1, TF2>(a => default(TF2));
	}
	
	public static void G<TG1, TG2>()
	{
		var f = new Func<TG1, TG2>(a => default(TG2));
	}
}";
            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: m =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                AssertEx.Equal(new[]
                {
                    "C.<>c__0<TF>",
                    "C.<>c__1<TG>",
                    "C.<>c__2<TF1, TF2>",
                    "C.<>c__3<TG1, TG2>"
                }, c.GetMembers().Where(member => member.Kind == SymbolKind.NamedType).Select(member => member.ToString()));

                var c0 = c.GetMember<NamedTypeSymbol>("<>c__0");
                AssertEx.SetEqual(new[]
                {
                    "C.<>c__0<TF>.<>9",
                    "C.<>c__0<TF>.<>9__0_0",
                    "C.<>c__0<TF>.<>c__0()",
                    "C.<>c__0<TF>.<>c__0()",
                    "C.<>c__0<TF>.<F>b__0_0()",
                }, c0.GetMembers().Select(member => member.ToString()));

                var c1 = c.GetMember<NamedTypeSymbol>("<>c__1");
                AssertEx.SetEqual(new[]
                {
                    "C.<>c__1<TG>.<>9",
                    "C.<>c__1<TG>.<>9__1_0",
                    "C.<>c__1<TG>.<>c__1()",
                    "C.<>c__1<TG>.<>c__1()",
                    "C.<>c__1<TG>.<G>b__1_0()",
                }, c1.GetMembers().Select(member => member.ToString()));

                var c2 = c.GetMember<NamedTypeSymbol>("<>c__2");
                AssertEx.SetEqual(new[]
                {
                    "C.<>c__2<TF1, TF2>.<>9",
                    "C.<>c__2<TF1, TF2>.<>9__2_0",
                    "C.<>c__2<TF1, TF2>.<>c__2()",
                    "C.<>c__2<TF1, TF2>.<>c__2()",
                    "C.<>c__2<TF1, TF2>.<F>b__2_0(TF1)",
                }, c2.GetMembers().Select(member => member.ToString()));

                var c3 = c.GetMember<NamedTypeSymbol>("<>c__3");
                AssertEx.SetEqual(new[]
                {
                    "C.<>c__3<TG1, TG2>.<>9",
                    "C.<>c__3<TG1, TG2>.<>9__3_0",
                    "C.<>c__3<TG1, TG2>.<>c__3()",
                    "C.<>c__3<TG1, TG2>.<>c__3()",
                    "C.<>c__3<TG1, TG2>.<G>b__3_0(TG1)",
                }, c3.GetMembers().Select(member => member.ToString()));
            });
        }

        [Fact]
        public void GenericStaticFramesWithConstraints()
        {
            string source = @"
using System;

public class C
{
	public static void F<TF>() where TF : class
	{
	    var f = new Func<TF>(() => default(TF));
	}
	
	public static void G<TG>() where TG : struct
	{
		var f = new Func<TG>(() => default(TG));
	}
}";
            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: m =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                AssertEx.Equal(new[]
                {
                    "C.<>c__0<TF>",
                    "C.<>c__1<TG>",
                }, c.GetMembers().Where(member => member.Kind == SymbolKind.NamedType).Select(member => member.ToString()));
            });
        }

        [Fact]
        public void GenericInstance()
        {
            string source = @"
using System;

public class C
{
	public void F<TF>()
	{
	    var f = new Func<TF>(() => { this.F(); return default(TF); });
	}
	
	public void G<TG>()
	{
		var f = new Func<TG>(() => { this.F(); return default(TG); });
	}
	
	public void F<TF1, TF2>()
	{
		var f = new Func<TF1, TF2>(a => { this.F(); return default(TF2); });
	}
	
	public void G<TG1, TG2>()
	{
		var f = new Func<TG1, TG2>(a => { this.F(); return default(TG2); });
	}

    private void F() {}
}";
            CompileAndVerify(source, options: TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All), symbolValidator: m =>
            {
                var c = m.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
                AssertEx.SetEqual(new[]
                {
                    "C.F<TF>()",
                    "C.G<TG>()",
                    "C.F<TF1, TF2>()",
                    "C.G<TG1, TG2>()",
                    "C.F()",
                    "C.C()",
                    "C.<F>b__0_0<TF>()",
                    "C.<G>b__1_0<TG>()",
                    "C.<F>b__2_0<TF1, TF2>(TF1)",
                    "C.<G>b__3_0<TG1, TG2>(TG1)",
                }, c.GetMembers().Select(member => member.ToString()));
            });
        }

        #region "Regressions"

        [WorkItem(539439, "DevDiv")]
        [Fact]
        public void LambdaWithReturn()
        {
            string source = @"
using System;
class Program
{
    delegate int Func(int i, int r);
    static void Main(string[] args)
    {
        Func fnc = (arg, arg2) => { return 1 + 2; };
        Console.Write(fnc(3, 4));
    }
}";

            CompileAndVerify(source, expectedOutput: @"3");
        }

        /// <remarks>
        /// Based on MadsT blog post:
        /// http://blogs.msdn.com/b/madst/archive/2007/05/11/recursive-lambda-expressions.aspx
        /// </remarks>
        [WorkItem(540034, "DevDiv")]
        [Fact]
        public void YCombinatorTest()
        {
            var source = @"
using System;
using System.Linq;

public class Program
{
    delegate T SelfApplicable<T>(SelfApplicable<T> self);

    static void Main()
    {
        // The Y combinator
        SelfApplicable<
          Func<Func<Func<int, int>, Func<int, int>>, Func<int, int>>
        > Y = y => f => x => f(y(y)(f))(x);

        // The fixed point generator
        Func<Func<Func<int, int>, Func<int, int>>, Func<int, int>> Fix =
          Y(Y);

        // The higher order function describing factorial
        Func<Func<int, int>, Func<int, int>> F =
          fac => x => { if (x == 0) { return 1; } else { return x * fac(x - 1); }
        };

        // The factorial function itself
        Func<int, int> factorial = Fix(F);

        Console.WriteLine(string.Join(
            Environment.NewLine, 
            Enumerable.Select(Enumerable.Range(0, 12), factorial)));
    }
}
";

            CompileAndVerify(
                source,
                new[] { LinqAssemblyRef },
                expectedOutput:
@"1
1
2
6
24
120
720
5040
40320
362880
3628800
39916800"
);
        }

        [WorkItem(540035, "DevDiv")]
        [Fact]
        public void LongNameTest()
        {
            var source = @"
using System;

namespace Lambda.Bugs
{
    public interface I<T>
    {
        void Foo(int x);
    }

    public class OuterGenericClass<T, S>
    {
        public class NestedClass : OuterGenericClass<NestedClass, NestedClass> { }

        public class C : I<NestedClass.NestedClass.NestedClass.NestedClass.NestedClass>
        {
            void I<NestedClass.NestedClass.NestedClass.NestedClass.NestedClass>.Foo(int x)
            {
                Func<int> f = () => x;
                Console.WriteLine(f());
            }
        }
    }

    public class Program
    {
        public static void Main()
        {
            I<OuterGenericClass<int, int>.NestedClass.NestedClass.NestedClass.NestedClass.NestedClass> x =
                new OuterGenericClass<int, int>.C();

            x.Foo(1);
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyEmitDiagnostics(
                // error CS7013: Name '<Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo>b__0' exceeds the maximum length allowed in metadata.
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong).WithArguments("<Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo>b__0").WithLocation(1, 1),
                // (17,81): error CS7013: Name 'Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo' exceeds the maximum length allowed in metadata.
                //             void I<NestedClass.NestedClass.NestedClass.NestedClass.NestedClass>.Foo(int x)
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "Foo").WithArguments("Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo").WithLocation(17, 81),
                // (19,31): error CS7013: Name '<Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo>b__0' exceeds the maximum length allowed in metadata.
                //                 Func<int> f = () => x;
                Diagnostic(ErrorCode.ERR_MetadataNameTooLong, "() => x").WithArguments("<Lambda.Bugs.I<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass,Lambda.Bugs.OuterGenericClass<Lambda.Bugs.OuterGenericClass<T,S>.NestedClass,Lambda.Bugs.OuterGenericClass<T,S>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.NestedClass>.Foo>b__0").WithLocation(19, 31));
        }

        [WorkItem(540049, "DevDiv")]
        [Fact]
        public void LambdaWithUnreachableCode()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        int x = 7;
        Action f = () => { int y; Console.Write(x); return; int z = y; };
        f();
    }
}
";

            CompileAndVerify(source, expectedOutput: "7");
        }

        [WorkItem(1019237, "DevDiv")]
        [Fact]
        public void OrderOfDelegateMembers()
        {
            var source = @"
using System.Linq;

class Program
{
    delegate int D1();

    static void Main()
    {
        foreach (var member in typeof(D1).GetMembers(
            System.Reflection.BindingFlags.DeclaredOnly |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance).OrderBy(m=>m.MetadataToken))
        {
            System.Console.WriteLine(member.ToString());
        }
    }
}
";
            // ref emit would just have different metadata tokens
            // we are not interested in testing that
            CompileAndVerify(source,
                additionalRefs: new[] { LinqAssemblyRef },
                expectedOutput: @"
Void .ctor(System.Object, IntPtr)
Int32 Invoke()
System.IAsyncResult BeginInvoke(System.AsyncCallback, System.Object)
Int32 EndInvoke(System.IAsyncResult)
");
        }

        [WorkItem(540092, "DevDiv")]
        [Fact]
        public void NestedAnonymousMethodsusingLocalAndField()
        {
            string source = @"
using System;

delegate void MyDel(int i);
class Test
{
    int j = 1;

    static int Main()
    {
        Test t = new Test();
        t.foo();

        return 0;
    }

    void foo()
    {
        int l = 0;
        MyDel d = delegate
        {
            Console.WriteLine(l++);
        };

        d = delegate(int i)
        {
            MyDel dd = delegate(int k)
            {
                Console.WriteLine(i + j + k);
            };

            dd(10);
        };

        d(100);
    }
}
";

            CompileAndVerify(source, expectedOutput: "111");
        }

        [WorkItem(540129, "DevDiv")]
        [Fact]
        public void CacheStaticAnonymousMethodInField()
        {
            string source = @"
using System;

delegate void D();
public delegate void E<T, U>(T t, U u);

public class Gen<T>
{
    public static void Foo<U>(T t, U u)
    {
        ((D)delegate
        {
            ((E<T, U>)delegate(T t2, U u2)
            {
                // do nothing in order to allow this anonymous method to be cached in a static field
            })(t, u);
        })();
    }
}

public class Test
{
    public static void Main()
    {
        Gen<int>.Foo<string>(1, ""2"");
        Console.WriteLine(""PASS"");
    }
}
";

            CompileAndVerify(source, expectedOutput: "PASS");
        }

        [WorkItem(540147, "DevDiv")]
        [Fact]
        public void CapturedVariableNamedThis()
        {
            var source =
                @"
using System;
 
class A
{
    public int N;
    public A(int n)
    {
        this.N = n;
    }
    public void Foo(A @this)
    {
        Action a = () => Bar(@this);
        a.Invoke();
    }
    public void Bar(A other)
    {
        Console.Write(this.N);
        Console.Write(other.N);
    }
 
    static void Main()
    {
        A a = new A(1);
        A b = new A(2);
        a.Foo(b);
    }
}
";
            var verifier = CompileAndVerify(
                source,
                expectedOutput: "12");
        }

        [Fact]
        public void StaticClosureSerialize()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int> x = () => 42;
        System.Console.WriteLine(x.Target.GetType().IsSerializable);

        Func<int> y = () => x();
        System.Console.WriteLine(y.Target.GetType().IsSerializable);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.ReleaseExe, expectedOutput: @"True
False");
        }

        [Fact]
        public void StaticClosureSerializeD()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Func<int> x = () => 42;
        System.Console.WriteLine(x.Target.GetType().IsSerializable);

        Func<int> y = () => x();
        System.Console.WriteLine(y.Target.GetType().IsSerializable);
    }
}
";
            var compilation = CompileAndVerify(source, options: TestOptions.DebugExe, expectedOutput: @"True
False");
        }


        [WorkItem(540178, "DevDiv")]
        [Fact]
        public void NestedGenericLambda()
        {
            var source =
                @"
using System;
 
class Program
{
    static void Main()
    {
        Foo<int>()()();
    }
 
    static Func<Func<T>> Foo<T>()
    {
        T[] x = new T[1];
        return () => () => x[0];
    }
}
";
            var verifier = CompileAndVerify(
                source,
                expectedOutput: "");
        }

        [WorkItem(540768, "DevDiv")]
        [Fact]
        public void TestClosureMethodAccessibility()
        {
            var source = @"
using System;
class Test
{
    static void Main()
    {
    }
    Func<int, int> f = (x) => 0;
    Func<string, string> Foo()
    {
        string s = """"; Console.WriteLine(s);
        return (a) => s;
    }
}";
            // Dev11 emits "public", we emit "internal" visibility for <Foo>b__1:
            CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Test+<>c__DisplayClass2_0", "<Foo>b__0",
                          ".method assembly hidebysig instance System.String <Foo>b__0(System.String a) cil managed"),
            });
        }

        [WorkItem(541008, "DevDiv")]
        [Fact]
        public void TooEarlyForThis()
        {
            // this tests for the C# analogue of VB bug 7520
            var source =
                @"using System;

class Program
{
    int x;
    Program(int x) : this(z => x + 10)
    {
        this.x = x + 100;
        F(z => z + x + this.x + 1000);
    }
    Program(Func<int, int> func)
    {}
    void F(Func<int, int> func)
    {
        Console.Write(func(10000));
    }
    public static void Main(string[] args)
    {
        new Program(1);
    }
}";
            var verifier = CompileAndVerify(
                source,
                expectedOutput: "11102");
        }

        [WorkItem(542062, "DevDiv")]
        [Fact]
        public void TestLambdaNoClosureClass()
        {
            var source = @"
using System;

delegate int D();
class Test
{
    static D field = () => field2;
    static short field2 = -1;

    public static void Main()
    {
        D myd = delegate() { return 1; };
        Console.WriteLine(""({0},{1})"", myd(), field());
    }
}
";
            //IMPORTANT!!! we should not be caching static lambda in static initializer.
            CompileAndVerify(source, expectedOutput: "(1,-1)").VerifyIL("Test..cctor", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""Test.<>c Test.<>c.<>9""
  IL_0005:  ldftn      ""int Test.<>c.<.cctor>b__4_0()""
  IL_000b:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0010:  stsfld     ""D Test.field""
  IL_0015:  ldc.i4.m1
  IL_0016:  stsfld     ""short Test.field2""
  IL_001b:  ret
}
");
        }

        [WorkItem(543087, "DevDiv")]
        [Fact]
        public void LambdaInGenericMethod()
        {
            var source = @"
using System;

delegate bool D();

class G<T> where T : class
{
    public T Fld = default(T);

    public static bool RunTest<U>(U u) where U : class
    {
        G<U> g = new G<U>();
        g.Fld = u;
        return ((D)(() => Test.Eval(g.Fld == u, true)))();
    }
}

class Test
{
    public static bool Eval(object obj1, object obj2) 
    { 
        return obj1.Equals(obj2);
    }
    static void Main()
    {
        var ret = G<string>.RunTest((string)null);
        Console.Write(ret);
    }
}
";
            var verifier = CompileAndVerify(
                source,
                expectedOutput: "True");
        }

        [WorkItem(543344, "DevDiv")]
        [Fact]
        public void AnonymousMethodOmitParameterList()
        {
            var source = @"
using System;

class C
{
    public int M()
    {
        Func<C, int> f = delegate { return 9; } ;
        return f(new C());
    }

    static void Main()
    {
        int r = new C().M();
        Console.WriteLine((r == 9) ? 0 : 1);
    }
}
";
            var verifier = CompileAndVerify(
                source,
                expectedOutput: "0");
        }

        [WorkItem(543345, "DevDiv")]
        [Fact()]
        public void ExtraCompilerGeneratedAttribute()
        {
            string source = @"using System;
using System.Reflection;
using System.Runtime.CompilerServices;

class Test
{
    static public System.Collections.IEnumerable myIterator(int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            yield return (Func<int,int>)((x) => { return i + x; });
        }
        yield break;
    }

    static void Main()
    {
        var type = typeof(Test);
        var nested = type.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance);
        var total = 0;
        if (nested.Length > 0 && nested[0].Name.StartsWith(""<>c__DisplayClass"", StringComparison.Ordinal))
        {
            foreach (MemberInfo mi in nested[0].GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
            {
                var ca = mi.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false);
                foreach (var a in ca) Console.WriteLine(mi + "": "" + a);
                total += ca.Length;
            }
        }
        Console.WriteLine(total);
    }
}
";
            var compilation = CompileAndVerify(source, expectedOutput: "0");
        }

        [WorkItem(545430, "DevDiv")]
        [Fact]
        public void CacheNonStaticLambdaInGenericMethod()
        {
            var source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    static void M<T>(List<T> dd, int p) where T : D
    {
        do{
            if (dd != null)
            {
                var last = dd.LastOrDefault(m => m.P <= p);
                if (dd.Count() > 1)
                {
                    dd.Reverse();
                }
            }
        } while(false);  
    }
}

class D
{
    public int P { get; set; }
}
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp, expectedSignatures: new[]
            {
                Signature("C+<>c__DisplayClass0_0`1", "<>9__0",
                    ".field public instance System.Func`2[T,System.Boolean] <>9__0")
            });

            verifier.VerifyIL("C.M<T>", @"
{
  // Code size       70 (0x46)
  .maxstack  4
  .locals init (C.<>c__DisplayClass0_0<T> V_0, //CS$<>8__locals0
                System.Func<T, bool> V_1)
  IL_0000:  newobj     ""C.<>c__DisplayClass0_0<T>..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""int C.<>c__DisplayClass0_0<T>.p""
  IL_000d:  ldarg.0
  IL_000e:  brfalse.s  IL_0045
  IL_0010:  ldarg.0
  IL_0011:  ldloc.0
  IL_0012:  ldfld      ""System.Func<T, bool> C.<>c__DisplayClass0_0<T>.<>9__0""
  IL_0017:  dup
  IL_0018:  brtrue.s   IL_0030
  IL_001a:  pop
  IL_001b:  ldloc.0
  IL_001c:  ldloc.0
  IL_001d:  ldftn      ""bool C.<>c__DisplayClass0_0<T>.<M>b__0(T)""
  IL_0023:  newobj     ""System.Func<T, bool>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stloc.1
  IL_002a:  stfld      ""System.Func<T, bool> C.<>c__DisplayClass0_0<T>.<>9__0""
  IL_002f:  ldloc.1
  IL_0030:  call       ""T System.Linq.Enumerable.LastOrDefault<T>(System.Collections.Generic.IEnumerable<T>, System.Func<T, bool>)""
  IL_0035:  pop
  IL_0036:  ldarg.0
  IL_0037:  call       ""int System.Linq.Enumerable.Count<T>(System.Collections.Generic.IEnumerable<T>)""
  IL_003c:  ldc.i4.1
  IL_003d:  ble.s      IL_0045
  IL_003f:  ldarg.0
  IL_0040:  callvirt   ""void System.Collections.Generic.List<T>.Reverse()""
  IL_0045:  ret
}
");
        }

        [Fact]
        public void CacheNonStaticLambda001()
        {
            var source = @"
    using System;

    class Program
    {
        static void Main(string[] args)
        {
        }

        class Executor
        {
            public void Execute(Func<int, int> f)
            {
                f(42);
            }
        }

        Executor[] arr = new Executor[] { new Executor() };
        void Test()
        {
            int x = 123;
            for (int i = 1; i < 10; i++)
            {
                if (i < 2)
                {
                    arr[i].Execute(arg => arg + x);  // delegate should be cached
                }
            }

            for (int i = 1; i < 10; i++)
            {
                var val = i;
                if (i < 2)
                {
                    int y = i + i;
                    System.Console.WriteLine(y);
                    arr[i].Execute(arg => arg + val);  // delegate should NOT be cached (closure created inside the loop)
                }
            }
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp);

            verifier.VerifyIL("Program.Test", @"
{
  // Code size      142 (0x8e)
  .maxstack  4
  .locals init (Program.<>c__DisplayClass3_0 V_0, //CS$<>8__locals0
                int V_1, //i
                System.Func<int, int> V_2,
                int V_3, //i
                Program.<>c__DisplayClass3_1 V_4) //CS$<>8__locals1
  IL_0000:  newobj     ""Program.<>c__DisplayClass3_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.s   123
  IL_0009:  stfld      ""int Program.<>c__DisplayClass3_0.x""
  IL_000e:  ldc.i4.1
  IL_000f:  stloc.1
  IL_0010:  br.s       IL_0046
  IL_0012:  ldloc.1
  IL_0013:  ldc.i4.2
  IL_0014:  bge.s      IL_0042
  IL_0016:  ldarg.0
  IL_0017:  ldfld      ""Program.Executor[] Program.arr""
  IL_001c:  ldloc.1
  IL_001d:  ldelem.ref
  IL_001e:  ldloc.0
  IL_001f:  ldfld      ""System.Func<int, int> Program.<>c__DisplayClass3_0.<>9__0""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003d
  IL_0027:  pop
  IL_0028:  ldloc.0
  IL_0029:  ldloc.0
  IL_002a:  ldftn      ""int Program.<>c__DisplayClass3_0.<Test>b__0(int)""
  IL_0030:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stloc.2
  IL_0037:  stfld      ""System.Func<int, int> Program.<>c__DisplayClass3_0.<>9__0""
  IL_003c:  ldloc.2
  IL_003d:  callvirt   ""void Program.Executor.Execute(System.Func<int, int>)""
  IL_0042:  ldloc.1
  IL_0043:  ldc.i4.1
  IL_0044:  add
  IL_0045:  stloc.1
  IL_0046:  ldloc.1
  IL_0047:  ldc.i4.s   10
  IL_0049:  blt.s      IL_0012
  IL_004b:  ldc.i4.1
  IL_004c:  stloc.3
  IL_004d:  br.s       IL_0088
  IL_004f:  newobj     ""Program.<>c__DisplayClass3_1..ctor()""
  IL_0054:  stloc.s    V_4
  IL_0056:  ldloc.s    V_4
  IL_0058:  ldloc.3
  IL_0059:  stfld      ""int Program.<>c__DisplayClass3_1.val""
  IL_005e:  ldloc.3
  IL_005f:  ldc.i4.2
  IL_0060:  bge.s      IL_0084
  IL_0062:  ldloc.3
  IL_0063:  ldloc.3
  IL_0064:  add
  IL_0065:  call       ""void System.Console.WriteLine(int)""
  IL_006a:  ldarg.0
  IL_006b:  ldfld      ""Program.Executor[] Program.arr""
  IL_0070:  ldloc.3
  IL_0071:  ldelem.ref
  IL_0072:  ldloc.s    V_4
  IL_0074:  ldftn      ""int Program.<>c__DisplayClass3_1.<Test>b__1(int)""
  IL_007a:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_007f:  callvirt   ""void Program.Executor.Execute(System.Func<int, int>)""
  IL_0084:  ldloc.3
  IL_0085:  ldc.i4.1
  IL_0086:  add
  IL_0087:  stloc.3
  IL_0088:  ldloc.3
  IL_0089:  ldc.i4.s   10
  IL_008b:  blt.s      IL_004f
  IL_008d:  ret
}
");
        }

        [Fact]
        public void CacheNonStaticLambda002()
        {
            var source = @"
    using System;

    class Program
    {
        static void Main(string[] args)
        {
        }

        void Test()
        {
            int y = 123;

            Func<int, Func<int>> f1 = 
                // should be cached
                (x) =>
            {
                if (x > 0)
                {
                    int z = 123;
                    System.Console.WriteLine(z);
                    return () => y;
                }
                return null;
            };
            f1(1);

            Func<int, Func<int>> f2 =
                // should NOT be cached
                (x) =>
            {
                if (x > 0)
                {
                    int z = 123;
                    System.Console.WriteLine(z);
                    return () => x;
                }
                return null;
            };
            f2(1);
        }
    }
";
            var comp = CreateCompilationWithMscorlibAndSystemCore(source, options: TestOptions.ReleaseDll);
            var verifier = CompileAndVerify(comp);

            verifier.VerifyIL("Program.Test", @"
{
  // Code size       70 (0x46)
  .maxstack  3
  IL_0000:  newobj     ""Program.<>c__DisplayClass1_0..ctor()""
  IL_0005:  dup
  IL_0006:  ldc.i4.s   123
  IL_0008:  stfld      ""int Program.<>c__DisplayClass1_0.y""
  IL_000d:  ldftn      ""System.Func<int> Program.<>c__DisplayClass1_0.<Test>b__0(int)""
  IL_0013:  newobj     ""System.Func<int, System.Func<int>>..ctor(object, System.IntPtr)""
  IL_0018:  ldc.i4.1
  IL_0019:  callvirt   ""System.Func<int> System.Func<int, System.Func<int>>.Invoke(int)""
  IL_001e:  pop
  IL_001f:  ldsfld     ""System.Func<int, System.Func<int>> Program.<>c.<>9__1_2""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003e
  IL_0027:  pop
  IL_0028:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_002d:  ldftn      ""System.Func<int> Program.<>c.<Test>b__1_2(int)""
  IL_0033:  newobj     ""System.Func<int, System.Func<int>>..ctor(object, System.IntPtr)""
  IL_0038:  dup
  IL_0039:  stsfld     ""System.Func<int, System.Func<int>> Program.<>c.<>9__1_2""
  IL_003e:  ldc.i4.1
  IL_003f:  callvirt   ""System.Func<int> System.Func<int, System.Func<int>>.Invoke(int)""
  IL_0044:  pop
  IL_0045:  ret
}
");

            verifier.VerifyIL("Program.<>c.<Test>b__1_2(int)",
@"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (Program.<>c__DisplayClass1_1 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass1_1..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldarg.1
  IL_0008:  stfld      ""int Program.<>c__DisplayClass1_1.x""
  IL_000d:  ldloc.0
  IL_000e:  ldfld      ""int Program.<>c__DisplayClass1_1.x""
  IL_0013:  ldc.i4.0
  IL_0014:  ble.s      IL_002a
  IL_0016:  ldc.i4.s   123
  IL_0018:  call       ""void System.Console.WriteLine(int)""
  IL_001d:  ldloc.0
  IL_001e:  ldftn      ""int Program.<>c__DisplayClass1_1.<Test>b__3()""
  IL_0024:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0029:  ret
  IL_002a:  ldnull
  IL_002b:  ret
}
"
);
            verifier.VerifyIL("Program.<>c__DisplayClass1_0.<Test>b__0(int)",
@"
{
  // Code size       45 (0x2d)
  .maxstack  3
  .locals init (System.Func<int> V_0)
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.0
  IL_0002:  ble.s      IL_002b
  IL_0004:  ldc.i4.s   123
  IL_0006:  call       ""void System.Console.WriteLine(int)""
  IL_000b:  ldarg.0
  IL_000c:  ldfld      ""System.Func<int> Program.<>c__DisplayClass1_0.<>9__1""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_002a
  IL_0014:  pop
  IL_0015:  ldarg.0
  IL_0016:  ldarg.0
  IL_0017:  ldftn      ""int Program.<>c__DisplayClass1_0.<Test>b__1()""
  IL_001d:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0022:  dup
  IL_0023:  stloc.0
  IL_0024:  stfld      ""System.Func<int> Program.<>c__DisplayClass1_0.<>9__1""
  IL_0029:  ldloc.0
  IL_002a:  ret
  IL_002b:  ldnull
  IL_002c:  ret
}
"
);
        }


        [WorkItem(546211, "DevDiv")]
        [Fact]
        public void LambdaInCatchInLambdaInInstance()
        {
            var source =
@"using System;

static class Utilities
{
    internal static void ReportException(object _componentModelHost, Exception e, string p) { }
    internal static void BeginInvoke(Action a) { }
}

class VsCatalogProvider
{
    private object _componentModelHost = null;

    static void Main()
    {
        Console.WriteLine(""success"");
    }

    private void TryIsolatedOperation()
    {
        Action action = new Action(() =>
        {
            try
            {
            }
            catch (Exception ex)
            {
                Utilities.BeginInvoke(new Action(() =>
                {
                    Utilities.ReportException(_componentModelHost, ex, string.Empty);
                }));
            }
        });
    }
}";
            var compilation = CompileAndVerify(source, expectedOutput: "success");
        }

        [WorkItem(546748, "DevDiv")]
        [Fact]
        public void LambdaWithCatchTypeParameter()
        {
            var source =
@"using System; 
class Program
{
    static void Main() { }
    public static void Sleep<TException>(Func<bool> sleepDelegate) where TException : Exception
    {
        Func<bool> x = delegate
        {
            try
            {
                return sleepDelegate();
            }
            catch (TException e)
            {
            }
            return false;
        };
    }
}";
            CompileAndVerify(source);
        }

        [WorkItem(546748, "DevDiv")]
        [Fact]
        public void LambdaWithCapturedCatchTypeParameter()
        {
            var source =
@"using System; 
class Program
{
    static void Main() { }
    public static void Sleep<TException>(Func<bool> sleepDelegate) where TException : Exception
    {
        Func<bool> x = delegate
        {
            try
            {
                return sleepDelegate();
            }
            catch (TException e)
            {
                Func<bool> x2 = delegate {
                    return e == null;
                };
            }
            return false;
        };
    }
}";
            var compilation = CompileAndVerify(source);
        }

        [WorkItem(530911, "DevDiv")]
        [Fact]
        public void LambdaWithOutParameter()
        {
            var source =
@"

using System;
 
delegate D D(out D d);
class Program
{
    static void Main()
    {
        D tmpD = delegate (out D d)
        {
            throw new System.Exception();
        };
 
        D d01 = delegate (out D d)
        {
            tmpD(out d);
            d(out d);
            return d;
        };
    }
}
";
            var compilation = CompileAndVerify(source);
        }

        [WorkItem(691006, "DevDiv")]
        [Fact]
        public void LambdaWithSwitch()
        {
            var source =
@"

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApplication16
{
    class Program
    {

        public static Task<IEnumerable<TResult>> Iterate<TResult>(IEnumerable<Task> asyncIterator, CancellationToken cancellationToken = default(CancellationToken))
        {
            var results = new List<TResult>();
            var tcs = new TaskCompletionSource<IEnumerable<TResult>>();

            IEnumerator<Task> enumerator = asyncIterator.GetEnumerator();
            Action recursiveBody = null;

            recursiveBody = () =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                    }
                    else if (enumerator.MoveNext())
                    {
                        enumerator.Current.ContinueWith(previous =>
                        {
                            switch (previous.Status)
                            {
                                case TaskStatus.Faulted:
                                case TaskStatus.Canceled:
                                    tcs.SetResult((previous as Task<IEnumerable<TResult>>).Result);
                                    break;

                                default:
                                    var previousWithResult = previous as Task<TResult>;
                                    if (previousWithResult != null)
                                    {
                                        results.Add(previousWithResult.Result);
                                    }
                                    else
                                    {
                                        results.Add(default(TResult));
                                    }

                                    recursiveBody();
                                    break;
                            }
                        });
                    }
                    else
                    {
                        tcs.TrySetResult(results);
                    }
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            };

            recursiveBody();
            return tcs.Task;
        }
        static void Main(string[] args)
        {
        }
    }
}

";
            CompileAndVerify(source);
        }

        #endregion

        [Fact]
        public void LambdaInQuery_Let()
        {
            var source = @"
using System;
using System.Linq;

class C
{
    public void F(int[] array)
    {
        var f = from item in array
                let a = new Func<int>(() => item)
                select a() + new Func<int>(() => item)();
    }
}";

            CompileAndVerify(source, new[] { SystemCoreRef });
        }

        [Fact]
        public void LambdaInQuery_From()
        {
            var source = @"
using System;
using System.Linq;

class C
{
    public void F(int[] array)
    {
        var f = from item1 in new Func<int[]>(() => array)()
                from item2 in new Func<int[]>(() => array)()
                select item1 + item2;
    }
}";

            CompileAndVerify(source, new[] { SystemCoreRef });
        }

        [Fact]
        public void EmbeddedStatementClosures1()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class C
{
    public void G<T>(Func<T> f) {}

    public void F()
    {
        for (int x = 1, y = 2; x < 10; x++) G(() => x + y);
        for (int x = 1, y = 2; x < 10; x++) { G(() => x + y); }
        foreach (var x in new[] { 1, 2, 3 }) G(() => x);
        foreach (var x in new[] { 1, 2, 3 }) { G(() => x); }
        foreach (var x in new[,] { {1}, {2}, {3} }) G(() => x);
        foreach (var x in new[,] { {1}, {2}, {3} }) { G(() => x); }
        foreach (var x in ""123"") G(() => x);
        foreach (var x in ""123"") { G(() => x); }
        foreach (var x in new List<string>()) G(() => x);
        foreach (var x in new List<string>()) { G(() => x); }
        using (var x = new MemoryStream()) G(() => x);
        using (var x = new MemoryStream()) G(() => x);
    }
}";

            CompileAndVerify(source, new[] { SystemCoreRef });
        }

        [Fact, WorkItem(2549, "https://github.com/dotnet/roslyn/issues/2549")]
        public void NestedLambdaWithExtensionMethodsInGeneric()
        {
            var source =
@"using System;
using System.Collections.Generic;
using System.Linq;

public class BadBaby
{
    IEnumerable<object> Children;
    public object Foo<T>()
    {
        return from child in Children select from T ch in Children select false;
    }
}";
            CompileAndVerify(source, new[] { SystemCoreRef });
        }
    }
}
