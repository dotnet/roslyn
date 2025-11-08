// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenLockTests : EmitMetadataTestBase
    {
        #region 4.0 codegen

        [Fact]
        public void LockNull()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (null)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
 -IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
 -IL_000a:  ldnull
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  ldloca.s   V_1
    IL_0011:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
   -IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
   -IL_0020:  leave.s    IL_002c
  }
  finally
  {
   ~IL_0022:  ldloc.1
    IL_0023:  brfalse.s  IL_002b
    IL_0025:  ldloc.0
    IL_0026:  call       ""void System.Threading.Monitor.Exit(object)""
   ~IL_002b:  endfinally
  }
 -IL_002c:  ldstr      ""After""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
 -IL_0036:  ret
}", displaySequencePoints: true);
        }

        [Fact]
        public void LockLocal()
        {
            var text =
@"
public class Test
{
    void M()
    {
        object o = new object();
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
 -IL_0000:  newobj     ""object..ctor()""
 -IL_0005:  ldstr      ""Before""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
 -IL_000f:  stloc.0
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.1
  .try
  {
    IL_0012:  ldloc.0
    IL_0013:  ldloca.s   V_1
    IL_0015:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
   -IL_001a:  ldstr      ""In""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
   -IL_0024:  leave.s    IL_0030
  }
  finally
  {
   ~IL_0026:  ldloc.1
    IL_0027:  brfalse.s  IL_002f
    IL_0029:  ldloc.0
    IL_002a:  call       ""void System.Threading.Monitor.Exit(object)""
   ~IL_002f:  endfinally
  }
 -IL_0030:  ldstr      ""After""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
 -IL_003a:  ret
}", displaySequencePoints: true);
        }

        [Fact]
        public void LockParameter()
        {
            var text =
@"
public class Test
{
    void M(object o)
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (object V_0,
  bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  ldloca.s   V_1
    IL_0011:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_002c
  }
  finally
  {
    IL_0022:  ldloc.1
    IL_0023:  brfalse.s  IL_002b
    IL_0025:  ldloc.0
    IL_0026:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_002b:  endfinally
  }
  IL_002c:  ldstr      ""After""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void LockField()
        {
            var text =
@"
public class Test
{
    object o;

    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""object Test.o""
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  .try
  {
    IL_0013:  ldloc.0
    IL_0014:  ldloca.s   V_1
    IL_0016:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_001b:  ldstr      ""In""
    IL_0020:  call       ""void System.Console.WriteLine(string)""
    IL_0025:  leave.s    IL_0031
  }
  finally
  {
    IL_0027:  ldloc.1
    IL_0028:  brfalse.s  IL_0030
    IL_002a:  ldloc.0
    IL_002b:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0030:  endfinally
  }
  IL_0031:  ldstr      ""After""
  IL_0036:  call       ""void System.Console.WriteLine(string)""
  IL_003b:  ret
}");
        }

        [Fact]
        public void LockStaticField()
        {
            var text =
@"
public class Test
{
    static object o;

    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldsfld     ""object Test.o""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.1
  .try
  {
    IL_0012:  ldloc.0
    IL_0013:  ldloca.s   V_1
    IL_0015:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_001a:  ldstr      ""In""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
    IL_0024:  leave.s    IL_0030
  }
  finally
  {
    IL_0026:  ldloc.1
    IL_0027:  brfalse.s  IL_002f
    IL_0029:  ldloc.0
    IL_002a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_002f:  endfinally
  }
  IL_0030:  ldstr      ""After""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
  IL_003a:  ret
}");
        }

        [Fact]
        public void LockThis()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (this)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       55 (0x37)
  .maxstack  2
  .locals init (Test V_0,
                bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  IL_000c:  ldc.i4.0
  IL_000d:  stloc.1
  .try
  {
    IL_000e:  ldloc.0
    IL_000f:  ldloca.s   V_1
    IL_0011:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_002c
  }
  finally
  {
    IL_0022:  ldloc.1
    IL_0023:  brfalse.s  IL_002b
    IL_0025:  ldloc.0
    IL_0026:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_002b:  endfinally
  }
  IL_002c:  ldstr      ""After""
  IL_0031:  call       ""void System.Console.WriteLine(string)""
  IL_0036:  ret
}");
        }

        [Fact]
        public void LockExpression()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (new object())
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       59 (0x3b)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
 -IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
 -IL_000a:  newobj     ""object..ctor()""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.1
  .try
  {
    IL_0012:  ldloc.0
    IL_0013:  ldloca.s   V_1
    IL_0015:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
   -IL_001a:  ldstr      ""In""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
   -IL_0024:  leave.s    IL_0030
  }
  finally
  {
   ~IL_0026:  ldloc.1
    IL_0027:  brfalse.s  IL_002f
    IL_0029:  ldloc.0
    IL_002a:  call       ""void System.Threading.Monitor.Exit(object)""
   ~IL_002f:  endfinally
  }
 -IL_0030:  ldstr      ""After""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
 -IL_003a:  ret
}", displaySequencePoints: true);
        }

        [Fact]
        public void LockTypeParameterExpression()
        {
            var text =
@"
public class Test
{
    void M<T>(T t) where T : class
    {
        System.Console.WriteLine(""Before"");
        lock (t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var verifier = CompileAndVerify(text);
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (object V_0,
                bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  box        ""T""
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  .try
  {
    IL_0013:  ldloc.0
    IL_0014:  ldloca.s   V_1
    IL_0016:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_001b:  ldstr      ""In""
    IL_0020:  call       ""void System.Console.WriteLine(string)""
    IL_0025:  leave.s    IL_0031
  }
  finally
  {
    IL_0027:  ldloc.1
    IL_0028:  brfalse.s  IL_0030
    IL_002a:  ldloc.0
    IL_002b:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0030:  endfinally
  }
  IL_0031:  ldstr      ""After""
  IL_0036:  call       ""void System.Console.WriteLine(string)""
  IL_003b:  ret
}");
        }

        [Fact]
        public void LockQuery()
        {
            var text =
@"
using System.Linq;
class Test
{
    public static void Main()
    {
        System.Console.WriteLine(""Before"");
        lock (from x in ""ABC""
              select x)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text);
            CompileAndVerify(compilation).VerifyIL("Test.Main", @"
{
  // Code size       95 (0x5f)
  .maxstack  3
  .locals init (System.Collections.Generic.IEnumerable<char> V_0,
                bool V_1)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldstr      ""ABC""
  IL_000f:  ldsfld     ""System.Func<char, char> Test.<>c.<>9__0_0""
  IL_0014:  dup
  IL_0015:  brtrue.s   IL_002e
  IL_0017:  pop
  IL_0018:  ldsfld     ""Test.<>c Test.<>c.<>9""
  IL_001d:  ldftn      ""char Test.<>c.<Main>b__0_0(char)""
  IL_0023:  newobj     ""System.Func<char, char>..ctor(object, System.IntPtr)""
  IL_0028:  dup
  IL_0029:  stsfld     ""System.Func<char, char> Test.<>c.<>9__0_0""
  IL_002e:  call       ""System.Collections.Generic.IEnumerable<char> System.Linq.Enumerable.Select<char, char>(System.Collections.Generic.IEnumerable<char>, System.Func<char, char>)""
  IL_0033:  stloc.0
  IL_0034:  ldc.i4.0
  IL_0035:  stloc.1
  .try
  {
    IL_0036:  ldloc.0
    IL_0037:  ldloca.s   V_1
    IL_0039:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_003e:  ldstr      ""In""
    IL_0043:  call       ""void System.Console.WriteLine(string)""
    IL_0048:  leave.s    IL_0054
  }
  finally
  {
    IL_004a:  ldloc.1
    IL_004b:  brfalse.s  IL_0053
    IL_004d:  ldloc.0
    IL_004e:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0053:  endfinally
  }
  IL_0054:  ldstr      ""After""
  IL_0059:  call       ""void System.Console.WriteLine(string)""
  IL_005e:  ret
}
");
        }

        [Fact]
        public void LockDelegate()
        {
            var text =
@"
delegate void D(int p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1= PM)
        {
        }
    }
    static partial void PM(int p2);
    static partial void PM(int p2)
    {
    }
}
";

            CompileAndVerify(text, parseOptions: TestOptions.Regular10).VerifyIL("Test.Main", @"
{
  // Code size       36 (0x24)
  .maxstack  2
  .locals init (D V_0,
                bool V_1)
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void Test.PM(int)""
  IL_0007:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  .try
  {
    IL_000f:  ldloc.0
    IL_0010:  ldloca.s   V_1
    IL_0012:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0017:  leave.s    IL_0023
  }
  finally
  {
    IL_0019:  ldloc.1
    IL_001a:  brfalse.s  IL_0022
    IL_001c:  ldloc.0
    IL_001d:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0022:  endfinally
  }
  IL_0023:  ret
}");
        }

        [Fact]
        public void InitInLock()
        {
            CompileAndVerify(@"
class Test
{
    public static void Main()
    {
        Res d;
        lock (d = new Res { x = 10, y = ""abc"" })
        {
            Test.Eval(d.x, 10);
            Test.Eval(d.y, ""abc"");
        }
    }
    static void Eval(object  x, object y)
    { }
}
class Res
{
    public int x;
    public object y;
}");
        }

        [Fact]
        public void ImplicitArraysInLockStatement()
        {
            CompileAndVerify(@"
class Test
{
    public static void Main()
    {
        int[] a = null;
        lock (a = new[] { 1, 2, 3 })// OK
        {
        }

        lock (new[] { 1, 2, 3 }) // OK
        {
        }
    }
}");
        }

        // Extension method call in a lock() or lock block
        [Fact()]
        public void ExtensionMethodCalledInLock()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        var myLock = new Test();
        lock (myLock.Test())
        {
        }
        lock (myLock)
        {
            Eval(myLock, myLock.Test());
        }
    }
    public static void Eval(object obj1, object obj2)
    {
    }
}
public static partial class Extensions
{
    public static object Test(this object o) { return o; }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text);
            CompileAndVerify(compilation).VerifyIL("Test.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  2
  .locals init (Test V_0, //myLock
                object V_1,
                bool V_2,
                Test V_3)
  IL_0000:  newobj     ""Test..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  call       ""object Extensions.Test(object)""
  IL_000c:  stloc.1
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.2
  .try
  {
    IL_000f:  ldloc.1
    IL_0010:  ldloca.s   V_2
    IL_0012:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0017:  leave.s    IL_0023
  }
  finally
  {
    IL_0019:  ldloc.2
    IL_001a:  brfalse.s  IL_0022
    IL_001c:  ldloc.1
    IL_001d:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0022:  endfinally
  }
  IL_0023:  ldloc.0
  IL_0024:  stloc.3
  IL_0025:  ldc.i4.0
  IL_0026:  stloc.2
  .try
  {
    IL_0027:  ldloc.3
    IL_0028:  ldloca.s   V_2
    IL_002a:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_002f:  ldloc.0
    IL_0030:  ldloc.0
    IL_0031:  call       ""object Extensions.Test(object)""
    IL_0036:  call       ""void Test.Eval(object, object)""
    IL_003b:  leave.s    IL_0047
  }
  finally
  {
    IL_003d:  ldloc.2
    IL_003e:  brfalse.s  IL_0046
    IL_0040:  ldloc.3
    IL_0041:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0046:  endfinally
  }
  IL_0047:  ret
}");
        }

        // Anonymous types can appear in lock statements
        [Fact()]
        public void LockAnonymousTypes_1()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        var a = new { };
        lock (a)
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (<>f__AnonymousType0 V_0,
                bool V_1)
  IL_0000:  newobj     ""<>f__AnonymousType0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
  {
    IL_0008:  ldloc.0
    IL_0009:  ldloca.s   V_1
    IL_000b:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0010:  leave.s    IL_001c
  }
  finally
  {
    IL_0012:  ldloc.1
    IL_0013:  brfalse.s  IL_001b
    IL_0015:  ldloc.0
    IL_0016:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001b:  endfinally
  }
  IL_001c:  ret
}");
        }

        [Fact()]
        public void LockAnonymousTypes_2()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        var b = new { p1 = 10 };
        lock (b)
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       31 (0x1f)
  .maxstack  2
  .locals init (<>f__AnonymousType0<int> V_0,
                bool V_1)
  IL_0000:  ldc.i4.s   10
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  stloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.1
  .try
  {
    IL_000a:  ldloc.0
    IL_000b:  ldloca.s   V_1
    IL_000d:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0012:  leave.s    IL_001e
  }
  finally
  {
    IL_0014:  ldloc.1
    IL_0015:  brfalse.s  IL_001d
    IL_0017:  ldloc.0
    IL_0018:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001d:  endfinally
  }
  IL_001e:  ret
}");
        }

        [Fact()]
        public void LockAnonymousTypes_3()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        var c = new { p1 = 10.0, p2 = 'a' };
        lock (c)
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  .locals init (<>f__AnonymousType0<double, char> V_0,
                bool V_1)
  IL_0000:  ldc.r8     10
  IL_0009:  ldc.i4.s   97
  IL_000b:  newobj     ""<>f__AnonymousType0<double, char>..ctor(double, char)""
  IL_0010:  stloc.0
  IL_0011:  ldc.i4.0
  IL_0012:  stloc.1
  .try
  {
    IL_0013:  ldloc.0
    IL_0014:  ldloca.s   V_1
    IL_0016:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_001b:  leave.s    IL_0027
  }
  finally
  {
    IL_001d:  ldloc.1
    IL_001e:  brfalse.s  IL_0026
    IL_0020:  ldloc.0
    IL_0021:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0026:  endfinally
  }
  IL_0027:  ret
}");
        }

        [Fact()]
        public void LockTypeOf()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        lock (typeof(decimal))
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (System.Type V_0,
                bool V_1)
  IL_0000:  ldtoken    ""decimal""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  ldloca.s   V_1
    IL_0010:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0015:  leave.s    IL_0021
  }
  finally
  {
    IL_0017:  ldloc.1
    IL_0018:  brfalse.s  IL_0020
    IL_001a:  ldloc.0
    IL_001b:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0020:  endfinally
  }
  IL_0021:  ret
}");
        }

        [Fact()]
        public void LockGetType()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        var obj = new object();
        lock (obj.GetType())
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  .locals init (System.Type V_0,
                bool V_1)
  IL_0000:  newobj     ""object..ctor()""
  IL_0005:  callvirt   ""System.Type object.GetType()""
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  ldloca.s   V_1
    IL_0010:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0015:  leave.s    IL_0021
  }
  finally
  {
    IL_0017:  ldloc.1
    IL_0018:  brfalse.s  IL_0020
    IL_001a:  ldloc.0
    IL_001b:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0020:  endfinally
  }
  IL_0021:  ret
}");
        }

        [Fact()]
        public void LockGetType_Struct()
        {
            var text =
@"
public class Test
{
    static S x = new S();
    public static  void Main()
    {
        lock (x.GetType())
        {
        }
    }
}
struct S
{ }
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (System.Type V_0,
                bool V_1)
  IL_0000:  ldsfld     ""S Test.x""
  IL_0005:  box        ""S""
  IL_000a:  call       ""System.Type object.GetType()""
  IL_000f:  stloc.0
  IL_0010:  ldc.i4.0
  IL_0011:  stloc.1
  .try
  {
    IL_0012:  ldloc.0
    IL_0013:  ldloca.s   V_1
    IL_0015:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_001a:  leave.s    IL_0026
  }
  finally
  {
    IL_001c:  ldloc.1
    IL_001d:  brfalse.s  IL_0025
    IL_001f:  ldloc.0
    IL_0020:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0025:  endfinally
  }
  IL_0026:  ret
}");
        }

        [Fact()]
        public void LockString()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        lock (""abc"")
        {
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (string V_0,
                bool V_1)
  IL_0000:  ldstr      ""abc""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  .try
  {
    IL_0008:  ldloc.0
    IL_0009:  ldloca.s   V_1
    IL_000b:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0010:  leave.s    IL_001c
  }
  finally
  {
    IL_0012:  ldloc.1
    IL_0013:  brfalse.s  IL_001b
    IL_0015:  ldloc.0
    IL_0016:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_001b:  endfinally
  }
  IL_001c:  ret
}");
        }

        [Fact()]
        public void NestedLock()
        {
            var text =
@"
class Test
{
    public void Main()
    {
        lock (typeof(Test))
        {
            Test C = new Test();
            lock (C)
            {
            }
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (System.Type V_0,
                bool V_1,
                Test V_2,
                bool V_3)
  IL_0000:  ldtoken    ""Test""
  IL_0005:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  .try
  {
    IL_000d:  ldloc.0
    IL_000e:  ldloca.s   V_1
    IL_0010:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0015:  newobj     ""Test..ctor()""
    IL_001a:  stloc.2
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.3
    .try
    {
      IL_001d:  ldloc.2
      IL_001e:  ldloca.s   V_3
      IL_0020:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
      IL_0025:  leave.s    IL_003b
    }
    finally
    {
      IL_0027:  ldloc.3
      IL_0028:  brfalse.s  IL_0030
      IL_002a:  ldloc.2
      IL_002b:  call       ""void System.Threading.Monitor.Exit(object)""
      IL_0030:  endfinally
    }
  }
  finally
  {
    IL_0031:  ldloc.1
    IL_0032:  brfalse.s  IL_003a
    IL_0034:  ldloc.0
    IL_0035:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_003a:  endfinally
  }
  IL_003b:  ret
}");
        }

        [Fact()]
        public void NestedLock_2()
        {
            var text =
@"
class Test
{
    private object syncroot = new object();
    public void goo()
    {
        lock (syncroot)
        {
            lock (syncroot)
            {
            }
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.goo", @"
{
  // Code size       57 (0x39)
  .maxstack  2
  .locals init (object V_0,
                bool V_1,
                object V_2,
                bool V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""object Test.syncroot""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.0
  IL_0008:  stloc.1
  .try
  {
    IL_0009:  ldloc.0
    IL_000a:  ldloca.s   V_1
    IL_000c:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0011:  ldarg.0
    IL_0012:  ldfld      ""object Test.syncroot""
    IL_0017:  stloc.2
    IL_0018:  ldc.i4.0
    IL_0019:  stloc.3
    .try
    {
      IL_001a:  ldloc.2
      IL_001b:  ldloca.s   V_3
      IL_001d:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
      IL_0022:  leave.s    IL_0038
    }
    finally
    {
      IL_0024:  ldloc.3
      IL_0025:  brfalse.s  IL_002d
      IL_0027:  ldloc.2
      IL_0028:  call       ""void System.Threading.Monitor.Exit(object)""
      IL_002d:  endfinally
    }
  }
  finally
  {
    IL_002e:  ldloc.1
    IL_002f:  brfalse.s  IL_0037
    IL_0031:  ldloc.0
    IL_0032:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0037:  endfinally
  }
  IL_0038:  ret
}");
        }

        //	Yield return inside a lock statement
        [WorkItem(10765, "DevDiv_Projects/Roslyn")]
        [Fact()]
        public void YieldInLock()
        {
            var text =
@"
using System.Collections.Generic;
class Test
{
    int[] values = new int[] { 1, 2, 3, 4 };
    object lockObj = new object();

    public IEnumerable<int> Values()
    {
        lock (lockObj)
        {
            foreach (var i in values)
                yield return i;
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Values", @"
{
  // Code size       15 (0xf)
  .maxstack  3
  IL_0000:  ldc.i4.s   -2
  IL_0002:  newobj     ""Test.<Values>d__2..ctor(int)""
  IL_0007:  dup
  IL_0008:  ldarg.0
  IL_0009:  stfld      ""Test Test.<Values>d__2.<>4__this""
  IL_000e:  ret
}");
        }

        [Fact()]
        public void YieldAfterLock()
        {
            var text =
@"
using System.Collections.Generic;
class Test
{
    public static void Main()
    { }
    public IEnumerable<int> Goo()
    {
        lock (new object())
        {
        }
        yield return 0;
    }
}
";
            CompileAndVerify(text);
        }

        // The definite assignment state of v at the beginning of expr is the same as the state of v at the beginning of stmt
        [Fact()]
        public void AssignmentInLock()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        object myLock = null;
        lock ((myLock == null).ToString())
        {
            System.Console.WriteLine(myLock.ToString());
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       49 (0x31)
  .maxstack  2
  .locals init (object V_0, //myLock
                string V_1,
                bool V_2,
                bool V_3)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  ldnull
  IL_0004:  ceq
  IL_0006:  stloc.3
  IL_0007:  ldloca.s   V_3
  IL_0009:  call       ""string bool.ToString()""
  IL_000e:  stloc.1
  IL_000f:  ldc.i4.0
  IL_0010:  stloc.2
  .try
  {
    IL_0011:  ldloc.1
    IL_0012:  ldloca.s   V_2
    IL_0014:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0019:  ldloc.0
    IL_001a:  callvirt   ""string object.ToString()""
    IL_001f:  call       ""void System.Console.WriteLine(string)""
    IL_0024:  leave.s    IL_0030
  }
  finally
  {
    IL_0026:  ldloc.2
    IL_0027:  brfalse.s  IL_002f
    IL_0029:  ldloc.1
    IL_002a:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_002f:  endfinally
  }
  IL_0030:  ret
}");
        }

        // The definite assignment state of v on the control flow transfer to embedded-statement is the same as the state of v at the end of expr
        [Fact()]
        public void AssignmentInLock_1()
        {
            var text =
@"
class Test
{
    public static void Main()
    {
        object myLock;
        lock (myLock = new object())
        {
            System.Console.WriteLine(myLock);
        }
    }
}
";
            CompileAndVerify(text).VerifyIL("Test.Main", @"
{
  // Code size       37 (0x25)
  .maxstack  2
  .locals init (object V_0, //myLock
                object V_1,
                bool V_2)
  IL_0000:  newobj     ""object..ctor()""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  stloc.1
  IL_0008:  ldc.i4.0
  IL_0009:  stloc.2
  .try
  {
    IL_000a:  ldloc.1
    IL_000b:  ldloca.s   V_2
    IL_000d:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0012:  ldloc.0
    IL_0013:  call       ""void System.Console.WriteLine(object)""
    IL_0018:  leave.s    IL_0024
  }
  finally
  {
    IL_001a:  ldloc.2
    IL_001b:  brfalse.s  IL_0023
    IL_001d:  ldloc.1
    IL_001e:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0023:  endfinally
  }
  IL_0024:  ret
}");
        }

        #endregion 4.0 codegen

        #region Pre-4.0 codegen

        [Fact]
        public void FallbackLockNull()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (null)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (object V_0)
 -IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
 -IL_000a:  ldnull
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
   -IL_0012:  ldstr      ""In""
    IL_0017:  call       ""void System.Console.WriteLine(string)""
   -IL_001c:  leave.s    IL_0025
  }
  finally
  {
   ~IL_001e:  ldloc.0
    IL_001f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0024:  endfinally
  }
 -IL_0025:  ldstr      ""After""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
 -IL_002f:  ret
}", displaySequencePoints: true);
        }

        [Fact]
        public void FallbackLockLocal()
        {
            var text =
@"
public class Test
{
    void M()
    {
        object o = new object();
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (object V_0)
  IL_0000:  newobj     ""object..ctor()""
  IL_0005:  ldstr      ""Before""
  IL_000a:  call       ""void System.Console.WriteLine(string)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_0029
  }
  finally
  {
    IL_0022:  ldloc.0
    IL_0023:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0028:  endfinally
  }
  IL_0029:  ldstr      ""After""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}");
        }

        [Fact]
        public void FallbackLockParameter()
        {
            var text =
@"
public class Test
{
    void M(object o)
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0012:  ldstr      ""In""
    IL_0017:  call       ""void System.Console.WriteLine(string)""
    IL_001c:  leave.s    IL_0025
  }
  finally
  {
    IL_001e:  ldloc.0
    IL_001f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0024:  endfinally
  }
  IL_0025:  ldstr      ""After""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
  IL_002f:  ret
}");
        }

        [Fact]
        public void FallbackLockField()
        {
            var text =
@"
public class Test
{
    object o;

    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  ldfld      ""object Test.o""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0017:  ldstr      ""In""
    IL_001c:  call       ""void System.Console.WriteLine(string)""
    IL_0021:  leave.s    IL_002a
  }
  finally
  {
    IL_0023:  ldloc.0
    IL_0024:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0029:  endfinally
  }
  IL_002a:  ldstr      ""After""
  IL_002f:  call       ""void System.Console.WriteLine(string)""
  IL_0034:  ret
}");
        }

        [Fact]
        public void FallbackLockStaticField()
        {
            var text =
@"
public class Test
{
    static object o;

    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (o)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       52 (0x34)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldsfld     ""object Test.o""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_0029
  }
  finally
  {
    IL_0022:  ldloc.0
    IL_0023:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0028:  endfinally
  }
  IL_0029:  ldstr      ""After""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}");
        }

        [Fact]
        public void FallbackLockThis()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (this)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (Test V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.0
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0012:  ldstr      ""In""
    IL_0017:  call       ""void System.Console.WriteLine(string)""
    IL_001c:  leave.s    IL_0025
  }
  finally
  {
    IL_001e:  ldloc.0
    IL_001f:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0024:  endfinally
  }
  IL_0025:  ldstr      ""After""
  IL_002a:  call       ""void System.Console.WriteLine(string)""
  IL_002f:  ret
}");
        }

        [Fact]
        public void FallbackLockExpression()
        {
            var text =
@"
public class Test
{
    void M()
    {
        System.Console.WriteLine(""Before"");
        lock (new object())
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M", @"
{
  // Code size       52 (0x34)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  newobj     ""object..ctor()""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0016:  ldstr      ""In""
    IL_001b:  call       ""void System.Console.WriteLine(string)""
    IL_0020:  leave.s    IL_0029
  }
  finally
  {
    IL_0022:  ldloc.0
    IL_0023:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0028:  endfinally
  }
  IL_0029:  ldstr      ""After""
  IL_002e:  call       ""void System.Console.WriteLine(string)""
  IL_0033:  ret
}");
        }

        [Fact]
        public void FallbackLockTypeParameterExpression()
        {
            var text =
@"
public class Test
{
    void M<T>(T t) where T : class
    {
        System.Console.WriteLine(""Before"");
        lock (t)
        {
            System.Console.WriteLine(""In"");
        }
        System.Console.WriteLine(""After"");
    }
}
";
            var compilation = CreateCompilationWithCorlib20(text);
            var verifier = CompileAndVerify(compilation);
            verifier.VerifyIL("Test.M<T>", @"
{
  // Code size       53 (0x35)
  .maxstack  1
  .locals init (object V_0)
  IL_0000:  ldstr      ""Before""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ldarg.1
  IL_000b:  box        ""T""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Threading.Monitor.Enter(object)""
  .try
  {
    IL_0017:  ldstr      ""In""
    IL_001c:  call       ""void System.Console.WriteLine(string)""
    IL_0021:  leave.s    IL_002a
  }
  finally
  {
    IL_0023:  ldloc.0
    IL_0024:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_0029:  endfinally
  }
  IL_002a:  ldstr      ""After""
  IL_002f:  call       ""void System.Console.WriteLine(string)""
  IL_0034:  ret
}");
        }

        private static CSharpCompilation CreateCompilationWithCorlib20(string text)
        {
            return CreateEmptyCompilation(new string[] { text }, new[] { Net20.References.mscorlib });
        }

        #endregion Pre-4.0 codegen

        #region Execution

        [Fact]
        public void ProducerConsumer()
        {
            var text =
@"
using System;
using System.Threading;

public class Buffer<T>
{
    private readonly object bufferLock = new object();
    private bool empty = true;
    private T item;

    public void Write(T newItem)
    {
        lock (bufferLock)
        {
            while (!empty) Monitor.Wait(bufferLock);

            item = newItem;
            empty = false;
            Console.WriteLine(""{0} wrote {1}"", Thread.CurrentThread.Name, newItem);
            Monitor.PulseAll(bufferLock);
        }
    }

    public T Read()
    {
        lock (bufferLock)
        {
            while (empty) Monitor.Wait(bufferLock);

            empty = true;
            T result = item;
            Console.WriteLine(""{0} read {1}"", Thread.CurrentThread.Name, result);
            Monitor.PulseAll(bufferLock);
            return result;
        }
    }
}

public class Program
{
    static void Main()
    {
        Buffer<int> buffer = new Buffer<int>();

        Thread writer = new Thread(() =>
        {
            for (int i = 0; i < 10; i++) buffer.Write(i);
        });
        writer.Name = ""Writer"";

        Thread reader = new Thread(() =>
        {
            for (int i = 0; i < 10; i++) buffer.Read();
        });
        reader.Name = ""Reader"";

        writer.Start();
        reader.Start();
        reader.Join();
        writer.Join();
    }
}
";
            CompileAndVerify(text, expectedOutput: @"Writer wrote 0
Reader read 0
Writer wrote 1
Reader read 1
Writer wrote 2
Reader read 2
Writer wrote 3
Reader read 3
Writer wrote 4
Reader read 4
Writer wrote 5
Reader read 5
Writer wrote 6
Reader read 6
Writer wrote 7
Reader read 7
Writer wrote 8
Reader read 8
Writer wrote 9
Reader read 9");
        }

        [Fact]
        public void ProducerConsumer_1()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        D p = new D();
        System.Threading.Thread[] t = new System.Threading.Thread[20];
        for (int i = 0; i < 20; i++)
        {
            t[i] = new System.Threading.Thread(p.goo);
            t[i].Start();
        }
        for (int i = 0; i < 20; i++)
        {
            t[i].Join();
        }
        System.Console.WriteLine(p.s);
    }
}

class D
{
    private object syncroot = new object();
    public int s;
    public void goo()
    {
        lock (syncroot)
        {
            for (int i = 0; i < 50000; i++)
            {
                s += 1;
            }
        }
        return;
    }
}
";
            CompileAndVerify(text, expectedOutput: @"1000000");
        }

        #endregion Execution

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_01()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter);

            CompileAndVerify(compilation, expectedOutput: "Inside lock.");
        }

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_02()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2);

            CompileAndVerify(compilation, expectedOutput: "Inside lock.");
        }

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_03()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2);

            compilation.VerifyEmitDiagnostics(
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Enter'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Enter").WithLocation(6, 9)
                );
        }

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_04()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Exit);

            compilation.VerifyEmitDiagnostics(
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Exit'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Exit").WithLocation(6, 9)
                );
        }

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_05()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Enter2);
            compilation.MakeMemberMissing(WellKnownMember.System_Threading_Monitor__Exit);

            compilation.VerifyEmitDiagnostics(
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Exit'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Exit").WithLocation(6, 9),
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Enter'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Enter").WithLocation(6, 9)
                );
        }

        [Fact(), WorkItem(1106943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1106943")]
        public void Bug1106943_06()
        {
            var source = @"
class C1
{
    public static void Main()
    {
        lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }
    }
}";

            var compilation = CreateCompilation(source, options: TestOptions.ReleaseExe);
            compilation.MakeTypeMissing(WellKnownType.System_Threading_Monitor);

            compilation.VerifyEmitDiagnostics(
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Exit'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Exit").WithLocation(6, 9),
    // (6,9): error CS0656: Missing compiler required member 'System.Threading.Monitor.Enter'
    //         lock (typeof(C1))
    Diagnostic(ErrorCode.ERR_MissingPredefinedMember, @"lock (typeof(C1))
        {
            System.Console.WriteLine(""Inside lock."");
        }").WithArguments("System.Threading.Monitor", "Enter").WithLocation(6, 9)
                );
        }
    }
}
