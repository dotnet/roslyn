// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenConditionalOperatorTests : CSharpTestBase
    {
        [Fact, WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void ConditionalDelegateInterfaceUnification1()
        {
            var src =
@"using System;
using System.Security;

[assembly: SecurityTransparent()]

interface I {}

class A : I {}

class C : I
{
    static Func<I> Tester(bool a)
    {
        return a 
            ? (Func<I>)(() => new A())
            : () => new C();
    }
    static void Main()
    {
        System.Console.Write(Tester(false)().GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       73 (0x49)
  .maxstack  2
  .locals init (System.Func<I> V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  brtrue.s   IL_0025
  IL_0004:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_0023
  IL_000c:  pop
  IL_000d:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0012:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_0018:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_001d:  dup
  IL_001e:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0023:  br.s       IL_0044
  IL_0025:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_002a:  dup
  IL_002b:  brtrue.s   IL_0044
  IL_002d:  pop
  IL_002e:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0033:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_0039:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0044:  stloc.0
  IL_0045:  br.s       IL_0047
  IL_0047:  ldloc.0
  IL_0048:  ret
}
");

            verify = CompileAndVerify(src,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       67 (0x43)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_0023
  IL_0003:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0008:  dup
  IL_0009:  brtrue.s   IL_0042
  IL_000b:  pop
  IL_000c:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0011:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_0017:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0022:  ret
  IL_0023:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0028:  dup
  IL_0029:  brtrue.s   IL_0042
  IL_002b:  pop
  IL_002c:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0031:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_0037:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_003c:  dup
  IL_003d:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0042:  ret
}
");
        }

        [Fact, WorkItem(20266, "https://github.com/dotnet/roslyn/issues/20266")]
        public void ConditionalAccessInMethodGroupConversion()
        {
            var source = @"
class C
{
    void M(object o)
    {
        System.Func<int, string> filter = new C(o?.ToString()).Method;
        filter(0);
    }
    string Method(int x)
    {
        throw null;
    }
    C(string x)
    {
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp);
        }

        [Fact, WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void ConditionalDelegateInterfaceUnification2()
        {
            var src =
@"using System;
using System.Security;

[assembly: SecurityTransparent()]

interface I {}

class A : I {}

class B : I {}

class C : I
{
    static Func<I> Tester(int a)
    {
        return a > 0 
              ? a == 1 
                ? (Func<I>)(() => new A())
                : () => new B()
              : (() => new C());
    }
    static void Main()
    {
        System.Console.Write(Tester(1)().GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "A");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      111 (0x6f)
  .maxstack  2
  .locals init (System.Func<I> V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_0026
  IL_0005:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0024
  IL_000d:  pop
  IL_000e:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0013:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_0019:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_001e:  dup
  IL_001f:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0024:  br.s       IL_006a
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.1
  IL_0028:  beq.s      IL_004b
  IL_002a:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0049
  IL_0032:  pop
  IL_0033:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0038:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_003e:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0043:  dup
  IL_0044:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0049:  br.s       IL_006a
  IL_004b:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_006a
  IL_0053:  pop
  IL_0054:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0059:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_005f:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0064:  dup
  IL_0065:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_006a:  stloc.0
  IL_006b:  br.s       IL_006d
  IL_006d:  ldloc.0
  IL_006e:  ret
}
");
            verify = CompileAndVerify(src,
                expectedOutput: "A");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      104 (0x68)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_0024
  IL_0004:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_0067
  IL_000c:  pop
  IL_000d:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0012:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_0018:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_001d:  dup
  IL_001e:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0023:  ret
  IL_0024:  ldarg.0
  IL_0025:  ldc.i4.1
  IL_0026:  beq.s      IL_0048
  IL_0028:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_002d:  dup
  IL_002e:  brtrue.s   IL_0067
  IL_0030:  pop
  IL_0031:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0036:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_003c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0041:  dup
  IL_0042:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0047:  ret
  IL_0048:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0056:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_005c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0067:  ret
}
");
        }

        [Fact, WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void ConditionalDelegateInterfaceUnification3()
        {
            var src =
                @"using System;
using System.Security;

[assembly: SecurityTransparent()]

interface I {}

class A : I {}

class B : I {}

class C : I
{
    static Func<I> Tester(int a)
    {
        return a > 0 
                ? (() => new A())
                : a == -1 
                    ? (Func<I>)(() => new B())
                    : (() => new C());
    }
    static void Main()
    {
        System.Console.Write(Tester(-1)().GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "B");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      111 (0x6f)
  .maxstack  2
  .locals init (System.Func<I> V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_004b
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.m1
  IL_0007:  beq.s      IL_002a
  IL_0009:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0028
  IL_0011:  pop
  IL_0012:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0017:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_001d:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0022:  dup
  IL_0023:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0028:  br.s       IL_0049
  IL_002a:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0049
  IL_0032:  pop
  IL_0033:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0038:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_003e:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0043:  dup
  IL_0044:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0049:  br.s       IL_006a
  IL_004b:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_006a
  IL_0053:  pop
  IL_0054:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0059:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_005f:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0064:  dup
  IL_0065:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_006a:  stloc.0
  IL_006b:  br.s       IL_006d
  IL_006d:  ldloc.0
  IL_006e:  ret
}
");
            verify = CompileAndVerify(src,
                expectedOutput: "B");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      104 (0x68)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_0048
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.m1
  IL_0006:  beq.s      IL_0028
  IL_0008:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_0067
  IL_0010:  pop
  IL_0011:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0016:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_001c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0027:  ret
  IL_0028:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_002d:  dup
  IL_002e:  brtrue.s   IL_0067
  IL_0030:  pop
  IL_0031:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0036:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_003c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0041:  dup
  IL_0042:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0047:  ret
  IL_0048:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_004d:  dup
  IL_004e:  brtrue.s   IL_0067
  IL_0050:  pop
  IL_0051:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0056:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_005c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0061:  dup
  IL_0062:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0067:  ret
}");
        }

        [Fact, WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void ConditionalDelegateInterfaceUnification4()
        {
            var src =
                @"using System;
using System.Security;

[assembly: SecurityTransparent()]

interface I {}

class A : I {}

class B : I {}

class D : I {}

class C : I
{
    static Func<I> Tester(int a)
    {
        return a > 0 
                ? a == 1 
                    ? (Func<I>)(() => new A())
                    : () => new B()
                : a == -1 
                    ? () => new C()
                    : (Func<I>)(() => new D());
    }
    static void Main()
    {
        System.Console.Write(Tester(-2)().GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "D");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      148 (0x94)
  .maxstack  2
  .locals init (System.Func<I> V_0)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_004b
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.m1
  IL_0007:  beq.s      IL_002a
  IL_0009:  ldsfld     ""System.Func<I> C.<>c.<>9__0_3""
  IL_000e:  dup
  IL_000f:  brtrue.s   IL_0028
  IL_0011:  pop
  IL_0012:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0017:  ldftn      ""I C.<>c.<Tester>b__0_3()""
  IL_001d:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0022:  dup
  IL_0023:  stsfld     ""System.Func<I> C.<>c.<>9__0_3""
  IL_0028:  br.s       IL_0049
  IL_002a:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_002f:  dup
  IL_0030:  brtrue.s   IL_0049
  IL_0032:  pop
  IL_0033:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0038:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_003e:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0043:  dup
  IL_0044:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0049:  br.s       IL_008f
  IL_004b:  ldarg.0
  IL_004c:  ldc.i4.1
  IL_004d:  beq.s      IL_0070
  IL_004f:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0054:  dup
  IL_0055:  brtrue.s   IL_006e
  IL_0057:  pop
  IL_0058:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_005d:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_0063:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0068:  dup
  IL_0069:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_006e:  br.s       IL_008f
  IL_0070:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0075:  dup
  IL_0076:  brtrue.s   IL_008f
  IL_0078:  pop
  IL_0079:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_007e:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_0084:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0089:  dup
  IL_008a:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_008f:  stloc.0
  IL_0090:  br.s       IL_0092
  IL_0092:  ldloc.0
  IL_0093:  ret
}
");
            verify = CompileAndVerify(src,
                expectedOutput: "D");
            verify.VerifyIL("C.Tester", @"
{
  // Code size      140 (0x8c)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_0048
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.m1
  IL_0006:  beq.s      IL_0028
  IL_0008:  ldsfld     ""System.Func<I> C.<>c.<>9__0_3""
  IL_000d:  dup
  IL_000e:  brtrue.s   IL_008b
  IL_0010:  pop
  IL_0011:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0016:  ldftn      ""I C.<>c.<Tester>b__0_3()""
  IL_001c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Func<I> C.<>c.<>9__0_3""
  IL_0027:  ret
  IL_0028:  ldsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_002d:  dup
  IL_002e:  brtrue.s   IL_008b
  IL_0030:  pop
  IL_0031:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0036:  ldftn      ""I C.<>c.<Tester>b__0_2()""
  IL_003c:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0041:  dup
  IL_0042:  stsfld     ""System.Func<I> C.<>c.<>9__0_2""
  IL_0047:  ret
  IL_0048:  ldarg.0
  IL_0049:  ldc.i4.1
  IL_004a:  beq.s      IL_006c
  IL_004c:  ldsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_0051:  dup
  IL_0052:  brtrue.s   IL_008b
  IL_0054:  pop
  IL_0055:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_005a:  ldftn      ""I C.<>c.<Tester>b__0_1()""
  IL_0060:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0065:  dup
  IL_0066:  stsfld     ""System.Func<I> C.<>c.<>9__0_1""
  IL_006b:  ret
  IL_006c:  ldsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_0071:  dup
  IL_0072:  brtrue.s   IL_008b
  IL_0074:  pop
  IL_0075:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_007a:  ldftn      ""I C.<>c.<Tester>b__0_0()""
  IL_0080:  newobj     ""System.Func<I>..ctor(object, System.IntPtr)""
  IL_0085:  dup
  IL_0086:  stsfld     ""System.Func<I> C.<>c.<>9__0_0""
  IL_008b:  ret
}
");
        }

        [Fact(), WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void NestedConditional1()
        {
            var src =
@"using System.Security;

[assembly: SecurityTransparent()]

public interface I {}

class A : I {}

class B : I {}

public class C : I
{
    static I Tester(int x, int y)
    {
        return x == 0 
            ? (y == 0 ? (I)new A() : new B())
            : new C();
    }
    static void Main()
    {
        System.Console.Write(Tester(1, 0).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (I V_0,
                I V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  brfalse.s  IL_000d
  IL_0004:  newobj     ""C..ctor()""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  br.s       IL_0020
  IL_000d:  ldarg.1
  IL_000e:  brfalse.s  IL_0019
  IL_0010:  newobj     ""B..ctor()""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  br.s       IL_0020
  IL_0019:  newobj     ""A..ctor()""
  IL_001e:  stloc.0
  IL_001f:  ldloc.0
  IL_0020:  stloc.1
  IL_0021:  br.s       IL_0023
  IL_0023:  ldloc.1
  IL_0024:  ret
}");
            // Optimized
            verify = CompileAndVerify(src,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  brfalse.s  IL_000b
  IL_0003:  newobj     ""C..ctor()""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  ret
  IL_000b:  ldarg.1
  IL_000c:  brfalse.s  IL_0016
  IL_000e:  newobj     ""B..ctor()""
  IL_0013:  stloc.0
  IL_0014:  ldloc.0
  IL_0015:  ret
  IL_0016:  newobj     ""A..ctor()""
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  ret
}");
        }

        [Fact(), WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void NestedConditional2()
        {
            var src =
@"using System.Security;

[assembly: SecurityTransparent()]

public interface I {}

class A : I {}

class B : I {}

public class C : I
{
    static I Tester(int x)
    {
        return x > 0 
            ? new C()
            : (x < 0 ? (I)new A() : new B());
    }
    static void Main()
    {
        System.Console.Write(Tester(0).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "B");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       39 (0x27)
  .maxstack  2
  .locals init (I V_0,
                I V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_001b
  IL_0005:  ldarg.0
  IL_0006:  ldc.i4.0
  IL_0007:  blt.s      IL_0012
  IL_0009:  newobj     ""B..ctor()""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  br.s       IL_0019
  IL_0012:  newobj     ""A..ctor()""
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  br.s       IL_0022
  IL_001b:  newobj     ""C..ctor()""
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  stloc.1
  IL_0023:  br.s       IL_0025
  IL_0025:  ldloc.1
  IL_0026:  ret
}");
            // Optimized
            verify = CompileAndVerify(src,
                expectedOutput: "B");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_0018
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.0
  IL_0006:  blt.s      IL_0010
  IL_0008:  newobj     ""B..ctor()""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  ret
  IL_0010:  newobj     ""A..ctor()""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ret
  IL_0018:  newobj     ""C..ctor()""
  IL_001d:  stloc.0
  IL_001e:  ldloc.0
  IL_001f:  ret
}");
        }

        [Fact(), WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void NestedConditional3()
        {
            var src =
@"using System.Security;

[assembly: SecurityTransparent()]

public interface I {}

class A : I {}

class B : I {}

class D : I {}

public class C : I
{
    static I Tester(int x)
    {
        return x > 0 
            ? x == 1 ? (I)new A() : new B()
            : x == 0 ? (I)new C() : new D();
    }
    static void Main()
    {
        System.Console.Write(Tester(1).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "A");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       51 (0x33)
  .maxstack  2
  .locals init (I V_0,
                I V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_001a
  IL_0005:  ldarg.0
  IL_0006:  brfalse.s  IL_0011
  IL_0008:  newobj     ""D..ctor()""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  br.s       IL_0018
  IL_0011:  newobj     ""C..ctor()""
  IL_0016:  stloc.0
  IL_0017:  ldloc.0
  IL_0018:  br.s       IL_002e
  IL_001a:  ldarg.0
  IL_001b:  ldc.i4.1
  IL_001c:  beq.s      IL_0027
  IL_001e:  newobj     ""B..ctor()""
  IL_0023:  stloc.0
  IL_0024:  ldloc.0
  IL_0025:  br.s       IL_002e
  IL_0027:  newobj     ""A..ctor()""
  IL_002c:  stloc.0
  IL_002d:  ldloc.0
  IL_002e:  stloc.1
  IL_002f:  br.s       IL_0031
  IL_0031:  ldloc.1
  IL_0032:  ret
}");
            // Optimized
            verify = CompileAndVerify(src,
                expectedOutput: "A");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_0017
  IL_0004:  ldarg.0
  IL_0005:  brfalse.s  IL_000f
  IL_0007:  newobj     ""D..ctor()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  ret
  IL_000f:  newobj     ""C..ctor()""
  IL_0014:  stloc.0
  IL_0015:  ldloc.0
  IL_0016:  ret
  IL_0017:  ldarg.0
  IL_0018:  ldc.i4.1
  IL_0019:  beq.s      IL_0023
  IL_001b:  newobj     ""B..ctor()""
  IL_0020:  stloc.0
  IL_0021:  ldloc.0
  IL_0022:  ret
  IL_0023:  newobj     ""A..ctor()""
  IL_0028:  stloc.0
  IL_0029:  ldloc.0
  IL_002a:  ret
}");
        }

        [Fact(), WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void NestedConditional4()
        {
            var src =
@"using System.Security;

[assembly: SecurityTransparent()]

public interface I {}

class A : I {}

class B : I {}

class D : I {}

public class C : I
{
    static I Tester(int x)
    {
        return x > 0 
            ? x > 1
                ? x > 2 
                    ? (I)new A() 
                    : new B() 
                : new C()
            : new D();
    }
    static void Main()
    {
        System.Console.Write(Tester(0).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "D");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       52 (0x34)
  .maxstack  2
  .locals init (I V_0,
                I V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  bgt.s      IL_000e
  IL_0005:  newobj     ""D..ctor()""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  br.s       IL_002f
  IL_000e:  ldarg.0
  IL_000f:  ldc.i4.1
  IL_0010:  bgt.s      IL_001b
  IL_0012:  newobj     ""C..ctor()""
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  br.s       IL_002f
  IL_001b:  ldarg.0
  IL_001c:  ldc.i4.2
  IL_001d:  bgt.s      IL_0028
  IL_001f:  newobj     ""B..ctor()""
  IL_0024:  stloc.0
  IL_0025:  ldloc.0
  IL_0026:  br.s       IL_002f
  IL_0028:  newobj     ""A..ctor()""
  IL_002d:  stloc.0
  IL_002e:  ldloc.0
  IL_002f:  stloc.1
  IL_0030:  br.s       IL_0032
  IL_0032:  ldloc.1
  IL_0033:  ret
}");
            // Optimized
            verify = CompileAndVerify(src,
                expectedOutput: "D");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       44 (0x2c)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  bgt.s      IL_000c
  IL_0004:  newobj     ""D..ctor()""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  ret
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  bgt.s      IL_0018
  IL_0010:  newobj     ""C..ctor()""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  ret
  IL_0018:  ldarg.0
  IL_0019:  ldc.i4.2
  IL_001a:  bgt.s      IL_0024
  IL_001c:  newobj     ""B..ctor()""
  IL_0021:  stloc.0
  IL_0022:  ldloc.0
  IL_0023:  ret
  IL_0024:  newobj     ""A..ctor()""
  IL_0029:  stloc.0
  IL_002a:  ldloc.0
  IL_002b:  ret
}");
        }

        [Fact]
        public void TestConditionalOperatorSimple()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Console.WriteLine(b ? 1 : 2);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: "1");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"{
  // Code size       13 (0xd)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  brtrue.s   IL_0006
  IL_0003:  ldc.i4.2
  IL_0004:  br.s       IL_0007
  IL_0006:  ldc.i4.1
  IL_0007:  call       ""void System.Console.WriteLine(int)""
  IL_000c:  ret
}");
        }

        [Fact]
        public void TestConditionalOperatorConstantCondition()
        {
            var source = @"
class C
{
    static void Main()
    {
        int x = 1;
        int y = 2;
        System.Console.WriteLine(true ? x : y);
        System.Console.WriteLine(false ? x : y);
    }
}";
            var comp = CompileAndVerify(source, expectedOutput: @"
1
2");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int V_0) //y
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  stloc.0
  IL_0003:  call       ""void System.Console.WriteLine(int)""
  IL_0008:  ldloc.0
  IL_0009:  call       ""void System.Console.WriteLine(int)""
  IL_000e:  ret
}");
        }

        [Fact]
        public void TestConditionalOperatorNullLiteral()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        string s1 = b ? ""hello"" : null;
        System.Console.WriteLine(s1);
        string s2 = b ? null : ""goodbye"";
        System.Console.WriteLine(s2);
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0007
  IL_0004:  ldnull
  IL_0005:  br.s       IL_000c
  IL_0007:  ldstr      ""hello""
  IL_000c:  call       ""void System.Console.WriteLine(string)""
  IL_0011:  brtrue.s   IL_001a
  IL_0013:  ldstr      ""goodbye""
  IL_0018:  br.s       IL_001b
  IL_001a:  ldnull
  IL_001b:  call       ""void System.Console.WriteLine(string)""
  IL_0020:  ret
}");
        }

        [Fact]
        public void TestConditionalOperatorLambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Func<int, int> f = null;
        System.Console.WriteLine(f);
        System.Func<int, int> g1 = b ? f : x => x;
        System.Console.WriteLine(g1);
        System.Func<int, int> g2 = b ? x => x : f;
        System.Console.WriteLine(g2);
    }
}";
            // NOTE: this is slightly different from the Dev10 IL, which caches the lambdas in static fields
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       93 (0x5d)
  .maxstack  3
  .locals init (System.Func<int, int> V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void System.Console.WriteLine(object)""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_002d
  IL_000c:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_002e
  IL_0014:  pop
  IL_0015:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_001a:  ldftn      ""int C.<>c.<Main>b__0_0(int)""
  IL_0020:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0025:  dup
  IL_0026:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_0""
  IL_002b:  br.s       IL_002e
  IL_002d:  ldloc.0
  IL_002e:  call       ""void System.Console.WriteLine(object)""
  IL_0033:  brtrue.s   IL_0038
  IL_0035:  ldloc.0
  IL_0036:  br.s       IL_0057
  IL_0038:  ldsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_003d:  dup
  IL_003e:  brtrue.s   IL_0057
  IL_0040:  pop
  IL_0041:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_0046:  ldftn      ""int C.<>c.<Main>b__0_1(int)""
  IL_004c:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0051:  dup
  IL_0052:  stsfld     ""System.Func<int, int> C.<>c.<>9__0_1""
  IL_0057:  call       ""void System.Console.WriteLine(object)""
  IL_005c:  ret
}
");
        }

        [Fact]
        public void TestConditionalOperatorMethodGroup()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Func<int> f = null;
        System.Console.WriteLine(f);
        System.Func<int> g1 = b ? f : M;
        System.Console.WriteLine(g1);
        System.Func<int> g2 = b ? M : f;
        System.Console.WriteLine(g2);
    }

    static int M()
    {
        return 0;
    }
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       55 (0x37)
  .maxstack  3
  .locals init (System.Func<int> V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void System.Console.WriteLine(object)""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_001a
  IL_000c:  ldnull
  IL_000d:  ldftn      ""int C.M()""
  IL_0013:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0018:  br.s       IL_001b
  IL_001a:  ldloc.0
  IL_001b:  call       ""void System.Console.WriteLine(object)""
  IL_0020:  brtrue.s   IL_0025
  IL_0022:  ldloc.0
  IL_0023:  br.s       IL_0031
  IL_0025:  ldnull
  IL_0026:  ldftn      ""int C.M()""
  IL_002c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0031:  call       ""void System.Console.WriteLine(object)""
  IL_0036:  ret
}
");
        }

        [Fact]
        public void TestConditionalOperatorPreferWider()
        {
            var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Console.Write(b ? 0 : (short)1);
        System.Console.Write(b ? 0 : 1u);
    }
}";
            // NOTE: second call is to Write(uint)
            var comp = CompileAndVerify(source, expectedOutput: "00");
            comp.VerifyDiagnostics();
            comp.VerifyIL("C.Main", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  dup
  IL_0002:  brtrue.s   IL_0007
  IL_0004:  ldc.i4.1
  IL_0005:  br.s       IL_0008
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""void System.Console.Write(int)""
  IL_000d:  brtrue.s   IL_0012
  IL_000f:  ldc.i4.1
  IL_0010:  br.s       IL_0013
  IL_0012:  ldc.i4.0
  IL_0013:  call       ""void System.Console.Write(uint)""
  IL_0018:  ret
}");
        }

        /// <summary>
        /// This specific code has caused problems in the past.
        /// System.Security.VerificationException on the second attempt.
        /// </summary>
        /// <remarks>
        /// No special handling seems to have been required to make this work
        /// in Roslyn.
        /// </remarks>
        [Fact]
        public void TestConditionalOperatorInterfaceRegression1()
        {
            var source = @"
using System;
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        IList<Type> knownTypeList = null;

        Console.Write(""first attempt: "");
        object o1 = (IList<Type>)(knownTypeList == null ? new Type[0] : knownTypeList);
        Console.WriteLine(o1);

        Console.Write(""second attempt: "");
        IList<Type> o2 = (IList<Type>)(knownTypeList == null ? new Type[0] : knownTypeList);
        Console.WriteLine(o2);
    }
}";
            // NOTE: second call is to Write(uint)
            var comp = CompileAndVerify(source, expectedOutput: @"
first attempt: System.Type[]
second attempt: System.Type[]");
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       61 (0x3d)
  .maxstack  1
  .locals init (System.Collections.Generic.IList<System.Type> V_0, //knownTypeList
  System.Collections.Generic.IList<System.Type> V_1)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldstr      ""first attempt: ""
  IL_0007:  call       ""void System.Console.Write(string)""
  IL_000c:  ldloc.0
  IL_000d:  brfalse.s  IL_0012
  IL_000f:  ldloc.0
  IL_0010:  br.s       IL_001a
  IL_0012:  ldc.i4.0
  IL_0013:  newarr     ""System.Type""
  IL_0018:  stloc.1
  IL_0019:  ldloc.1
  IL_001a:  call       ""void System.Console.WriteLine(object)""
  IL_001f:  ldstr      ""second attempt: ""
  IL_0024:  call       ""void System.Console.Write(string)""
  IL_0029:  ldloc.0
  IL_002a:  brfalse.s  IL_002f
  IL_002c:  ldloc.0
  IL_002d:  br.s       IL_0037
  IL_002f:  ldc.i4.0
  IL_0030:  newarr     ""System.Type""
  IL_0035:  stloc.1
  IL_0036:  ldloc.1
  IL_0037:  call       ""void System.Console.WriteLine(object)""
  IL_003c:  ret
}");
        }

        /// <summary>
        /// From orcas bug #42645.  PEVerify fails.
        /// </summary>
        [Fact]
        public void TestConditionalOperatorInterfaceRegression2()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        List<int> b = new List<int>();

        IEnumerable<int> c = a != null ? a : (IEnumerable<int>)b;
        System.Console.WriteLine(c);
    }
}";
            // Note the explicit casts, even though the conversions are
            // implicit reference conversions.
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       30 (0x1e)
  .maxstack  1
  .locals init (int[] V_0, //a
  System.Collections.Generic.List<int> V_1, //b
  System.Collections.Generic.IEnumerable<int> V_2)
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  brtrue.s   IL_0015
  IL_0010:  ldloc.1
  IL_0011:  stloc.2
  IL_0012:  ldloc.2
  IL_0013:  br.s       IL_0018
  IL_0015:  ldloc.0
  IL_0016:  stloc.2
  IL_0017:  ldloc.2
  IL_0018:  call       ""void System.Console.WriteLine(object)""
  IL_001d:  ret
}");
        }

        [Fact]
        public void TestConditionalOperatorInterfaceRegression2a()
        {
            var source = @"
using System.Collections.Generic;

public class Test
{
    static void Main()
    {
        int[] a = new int[] { };
        List<int> b = new List<int>();

        IEnumerable<int> c = a != null ? a : (IEnumerable<int>)b;
    }
}";
            // Note the explicit casts, even though the conversions are
            // implicit reference conversions.
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test.Main", @"
{
  // Code size       16 (0x10)
  .maxstack  1
  .locals init (int[] V_0, //a
  System.Collections.Generic.List<int> V_1) //b
  IL_0000:  ldc.i4.0
  IL_0001:  newarr     ""int""
  IL_0006:  stloc.0
  IL_0007:  newobj     ""System.Collections.Generic.List<int>..ctor()""
  IL_000c:  stloc.1
  IL_000d:  ldloc.0
  IL_000e:  pop
  IL_000f:  ret
}
");
        }
        /// <summary>
        /// From whidbey bug #108643.  Extraneous class casts in Test1.m2 (vs Test1.m1).
        /// </summary>
        [Fact]
        public void TestConditionalOperatorInterfaceRegression3()
        {
            var source = @"
interface Base { Base m1();}
class Driver { public static int mask = 1;}
class Test1 : Base
{
    Base next;
    Base same;
    int cnt = 0;
    public Test1(Base link)
    {
        same = this;
        next = link;
        if (next == null) next = this;
    }
    public Base m1() { return ((++cnt) & Driver.mask) != 0 ? same : next; } //version1 (explicit impl in original repro)
    public Base m2() { return ((++cnt) & Driver.mask) != 0 ? this : next; } //version2 
}";
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("Test1.m1", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int Test1.cnt""
  IL_0007:  ldc.i4.1
  IL_0008:  add
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  stfld      ""int Test1.cnt""
  IL_0010:  ldloc.0
  IL_0011:  ldsfld     ""int Driver.mask""
  IL_0016:  and
  IL_0017:  brtrue.s   IL_0020
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""Base Test1.next""
  IL_001f:  ret
  IL_0020:  ldarg.0
  IL_0021:  ldfld      ""Base Test1.same""
  IL_0026:  ret
}");
            // NOTE: no castclass
            comp.VerifyIL("Test1.m2", @"
{
  // Code size       36 (0x24)
  .maxstack  3
  .locals init (int V_0,
  Base V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int Test1.cnt""
  IL_0007:  ldc.i4.1
  IL_0008:  add
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  stfld      ""int Test1.cnt""
  IL_0010:  ldloc.0
  IL_0011:  ldsfld     ""int Driver.mask""
  IL_0016:  and
  IL_0017:  brtrue.s   IL_0020
  IL_0019:  ldarg.0
  IL_001a:  ldfld      ""Base Test1.next""
  IL_001f:  ret
  IL_0020:  ldarg.0
  IL_0021:  stloc.1
  IL_0022:  ldloc.1
  IL_0023:  ret
}");
        }

        /// <summary>
        /// From whidbey bug #49619.  PEVerify fails.
        /// </summary>
        [Fact]
        public void TestConditionalOperatorInterfaceRegression4()
        {
            var source = @"
public interface IA { }
public interface IB { int f(); }
public class AB1 : IA, IB { public int f() { return 42; } }
public class AB2 : IA, IB { public int f() { return 1; } }

class MainClass
{
    public static void g(bool p)
    {
        (p ? (IB)new AB1() : (IB)new AB2()).f();
    }
}";
            // Note the explicit casts, even though the conversions are
            // implicit reference conversions.
            // CONSIDER: dev10 writes to/reads from a temp to simulate
            // a static cast (instead of using castclass).
            var comp = CompileAndVerify(source);
            comp.VerifyDiagnostics();
            comp.VerifyIL("MainClass.g", @"
{
  // Code size       26 (0x1a)
  .maxstack  1
  .locals init (IB V_0)
  IL_0000:  ldarg.0
  IL_0001:  brtrue.s   IL_000c
  IL_0003:  newobj     ""AB2..ctor()""
  IL_0008:  stloc.0
  IL_0009:  ldloc.0
  IL_000a:  br.s       IL_0013
  IL_000c:  newobj     ""AB1..ctor()""
  IL_0011:  stloc.0
  IL_0012:  ldloc.0
  IL_0013:  callvirt   ""int IB.f()""
  IL_0018:  pop
  IL_0019:  ret
}");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestConditionalOperatorForExpression()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
class Program
{
    static void Main(string[] args)
    {
        string[] arr = new string[] { ""aaa"", ""bbb"", ""ccc"" };
        int[] arr_int = new int[] { 111, 222, 333 };
        IEnumerable<string> s = true ? from x in arr select x : from x in arr_int select x.ToString();
        foreach (var item in s)
        {
            System.Console.WriteLine(item);
        }
    }
}";
            string expectedOutput = @"aaa
bbb
ccc";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForObjInit()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        Goo f1 = new Goo(), f2 = new Goo(), f3 = new Goo();
        bool b = true;
        f3 = b ? f1 = new Goo { i = 1 } : f2 = new Goo { i = 2 };
        Console.WriteLine(f1.i);
        Console.WriteLine(f2.i);
        Console.WriteLine(f3.i);
        b = false;
        f3 = b ? f1 = new Goo { i = 3 } : f2 = new Goo { i = 4 };
        Console.WriteLine(f1.i);
        Console.WriteLine(f2.i);
        Console.WriteLine(f3.i);
    }
}
class Goo
{
    public int i;
}
";
            string expectedOutput = @"1
0
1
1
4
4";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForObjInit_2()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        bool b1 = true;
        bool b2 = false;
        Goo f = new Goo
        {
            i = b1 ? 10 : -10
        };
        Console.WriteLine(f.i);
        f = new Goo
        {
            i = b2 ? 10 : -10
        };
        Console.WriteLine(f.i);
    }
}
class Goo
{
    public int i;
}
";
            string expectedOutput = @"10
-10";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForExpressionTree()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main(string[] args)
    {
        Expression<Func<bool, long, int, long>> testExpr = (x, y, z) => x ? y : z;
        var testFunc = testExpr.Compile();
        Console.WriteLine(testFunc(false, (long)3, 100)); //100
    }
}
";
            string expectedOutput = @"100";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForCustomOperator()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main(string[] args)
    {
        Expression<Func<TestStruct, int?, int, int?>> testExpr = (x, y, z) => x ? y : z;
        var testFunc = testExpr.Compile();
        Console.WriteLine(testFunc(new TestStruct(), (int?)null, 10)); //10
    }
}
public struct TestStruct
{
    public static bool operator true(TestStruct ts)
    {
        return false;
    }
    public static bool operator false(TestStruct ts)
    {
        return true;
    }
}
";
            string expectedOutput = @"10";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForMultiCondition()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(false ? 1 : true ? 2 : 3);
    }
}
";
            string expectedOutput = @"2";
            string expectedIL = @"{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldc.i4.2  
  IL_0001:  call       ""void System.Console.WriteLine(int)""
  IL_0006:  ret       
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL);
        }

        [WorkItem(528275, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528275")]
        [Fact]
        public void TestConditionalOperatorForImplicitConv()
        {
            var source = @"
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        System.Console.WriteLine(false ? Enumerable.Empty<int>() : new int[] { 1 });
    }
}
";
            string expectedOutput = @"System.Int32[]";
            string expectedIL = @"{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldc.i4.1  
  IL_0001:  newarr     ""int""
  IL_0006:  dup       
  IL_0007:  ldc.i4.0  
  IL_0008:  ldc.i4.1  
  IL_0009:  stelem.i4 
  IL_000a:  call       ""void System.Console.WriteLine(object)""
  IL_000f:  ret       
}";
            // Dev11 compiler reports WRN_UnreachableExpr, but reachability is defined for statements not for expressions.
            // We don't report the warning.
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL).VerifyDiagnostics();
        }

        [Fact]
        public void TestConditionalOperatorForConversion()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        object valueFromDatabase;
        decimal result;
        valueFromDatabase = DBNull.Value;
        result = (valueFromDatabase != DBNull.Value ? (decimal)valueFromDatabase : (decimal)0);
        System.Console.WriteLine(result);
        result = (decimal)(valueFromDatabase != DBNull.Value ? valueFromDatabase : 0); //Runtime exception
        System.Console.WriteLine(result);
    }
}
";
            string expectedIL = @"
{
  // Code size       60 (0x3c)
  .maxstack  2
  .locals init (object V_0) //valueFromDatabase
  IL_0000:  ldsfld     ""System.DBNull System.DBNull.Value""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldsfld     ""System.DBNull System.DBNull.Value""
  IL_000c:  bne.un.s   IL_0015
  IL_000e:  ldsfld     ""decimal decimal.Zero""
  IL_0013:  br.s       IL_001b
  IL_0015:  ldloc.0
  IL_0016:  unbox.any  ""decimal""
  IL_001b:  call       ""void System.Console.WriteLine(decimal)""
  IL_0020:  ldloc.0
  IL_0021:  ldsfld     ""System.DBNull System.DBNull.Value""
  IL_0026:  bne.un.s   IL_0030
  IL_0028:  ldc.i4.0
  IL_0029:  box        ""int""
  IL_002e:  br.s       IL_0031
  IL_0030:  ldloc.0
  IL_0031:  unbox.any  ""decimal""
  IL_0036:  call       ""void System.Console.WriteLine(decimal)""
  IL_003b:  ret
}";
            CompileAndVerify(source).VerifyIL("Program.Main", expectedIL);
        }

        [Fact, WorkItem(530071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530071")]
        public void TestConditionalOperatorForImplicitlyTypedArrays()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        int[] a, a1, a2;
        a = a1 = a2 = null;
        a = true ? a1 = new[] { 1, 2, 3 } : a2 = new[] { 4, 5, 6 };
        foreach (int item in a1)
        { Console.WriteLine(item); }
    }
}
";
            string expectedOutput = @"1
2
3";
            // Dev11 compiler reports WRN_UnreachableExpr, but reachability is defined for statements not for expressions.
            // We don't report the warning.
            CompileAndVerify(source, expectedOutput: expectedOutput).
                VerifyDiagnostics();
        }

        [Fact]
        public void TestConditionalOperatorForConst()
        {
            var source = @"
class Program
{
    const bool con = true;
    static int Main(string[] args)
    {
        int s1 = con != true ? 1 : 2;
        return s1;
    }
}
";
            string expectedIL = @"{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldc.i4.2
  IL_0001:  ret
}";
            CompileAndVerify(source).VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForStatic()
        {
            var source = @"
class Program
{
    static bool con = true;
    static int Main(string[] args)
    {
        int s1 = con != true ? 1 : 2;
        return s1;
    }
}
";
            string expectedIL = @"{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  ldsfld     ""bool Program.con""
  IL_0005:  brfalse.s  IL_0009
  IL_0007:  ldc.i4.2
  IL_0008:  ret
  IL_0009:  ldc.i4.1
  IL_000a:  ret
}";
            CompileAndVerify(source).VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForGenericMethod()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        object y = 0;
        System.Console.WriteLine(true ? fun(x) : fun(y));
    }
    private static T fun<T>(T t)
    {
        return t;
    }
}
";
            string expectedOutput = @"1";
            string expectedIL = @"{
  // Code size       17 (0x11)
  .maxstack  1
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""int Program.fun<int>(int)""
  IL_0006:  box        ""int""
  IL_000b:  call       ""void System.Console.WriteLine(object)""
  IL_0010:  ret
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForGenericMethod2()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        object y = 0;
        System.Console.WriteLine(y != null ? fun(x) : fun(y));
    }
    private static T fun<T>(T t)
    {
        return t;
    }
}
";
            string expectedOutput = @"1";
            string expectedIL = @"
{
  // Code size       37 (0x25)
  .maxstack  1
  .locals init (int V_0, //x
  object V_1) //y
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  box        ""int""
  IL_0008:  stloc.1
  IL_0009:  ldloc.1
  IL_000a:  brtrue.s   IL_0014
  IL_000c:  ldloc.1
  IL_000d:  call       ""object Program.fun<object>(object)""
  IL_0012:  br.s       IL_001f
  IL_0014:  ldloc.0
  IL_0015:  call       ""int Program.fun<int>(int)""
  IL_001a:  box        ""int""
  IL_001f:  call       ""void System.Console.WriteLine(object)""
  IL_0024:  ret
}
";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForParenthesizedExpression()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        int y = 1;
        System.Console.WriteLine(((x == y)) ? (++x) : ((((++y))))); 	// OK
    }
}
";
            string expectedOutput = @"2";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void TestConditionalOperatorForParameter()
        {
            var source = @"
class Program
{
    static bool b = true;
    static void Main(string[] args)
    {
        fun(b);
    }
    static void fun(bool b)
    {
        System.Console.WriteLine(b ? ""true"" : ""false"");
    }
}
";
            string expectedOutput = @"true";
            string expectedIL = @"{
  // Code size       21 (0x15)
  .maxstack  1
  IL_0000:  ldarg.0   
  IL_0001:  brtrue.s   IL_000a
  IL_0003:  ldstr      ""false""
  IL_0008:  br.s       IL_000f
  IL_000a:  ldstr      ""true""
  IL_000f:  call       ""void System.Console.WriteLine(string)""
  IL_0014:  ret       
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.fun", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForParameterRef()
        {
            var source = @"
class Program
{
    static bool b = true;
    static void Main(string[] args)
    {
        fun(ref b);
    }
    static void fun(ref bool b)
    {
        System.Console.WriteLine(b != true ? b = true : b = false);
    }
}
";
            string expectedOutput = @"False";
            string expectedIL = @"
{
  // Code size       24 (0x18)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldind.u1
  IL_0002:  brfalse.s  IL_000c
  IL_0004:  ldarg.0
  IL_0005:  ldc.i4.0
  IL_0006:  dup
  IL_0007:  stloc.0
  IL_0008:  stind.i1
  IL_0009:  ldloc.0
  IL_000a:  br.s       IL_0012
  IL_000c:  ldarg.0
  IL_000d:  ldc.i4.1
  IL_000e:  dup
  IL_000f:  stloc.0
  IL_0010:  stind.i1
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Console.WriteLine(bool)""
  IL_0017:  ret
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.fun", expectedIL);
        }

        [Fact]
        public void TestConditionalOperatorForParameterOut()
        {
            var source = @"
class Program
{
    static bool b = true;
    static void Main(string[] args)
    {
        fun(out b);
    }
    static void fun(out bool b)
    {
        System.Console.WriteLine(string.IsNullOrEmpty(""s"")  ? b = true : b = false);
    }
}
";
            string expectedOutput = @"False";
            string expectedIL = @"{
  // Code size       32 (0x20)
  .maxstack  3
  .locals init (bool V_0)
  IL_0000:  ldstr      ""s""
  IL_0005:  call       ""bool string.IsNullOrEmpty(string)""
  IL_000a:  brtrue.s   IL_0014
  IL_000c:  ldarg.0   
  IL_000d:  ldc.i4.0  
  IL_000e:  dup       
  IL_000f:  stloc.0   
  IL_0010:  stind.i1  
  IL_0011:  ldloc.0   
  IL_0012:  br.s       IL_001a
  IL_0014:  ldarg.0   
  IL_0015:  ldc.i4.1  
  IL_0016:  dup       
  IL_0017:  stloc.0   
  IL_0018:  stind.i1  
  IL_0019:  ldloc.0   
  IL_001a:  call       ""void System.Console.WriteLine(bool)""
  IL_001f:  ret       
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.fun", expectedIL);
        }

        [Fact]
        public void TestConditionalRequiringBox()
        {
            var source = @"
public static class Program
{
    public static void Main()
    {
        int a = 1;
        IGoo<string> i = null;
        GooVal e = new GooVal();

        i = a > 1 ? i : e;
        System.Console.Write(i.Goo());
    }

    interface IGoo<T> { T Goo(); }

    struct GooVal : IGoo<string>
    {
        public string Goo() { return ""Val ""; }
    }
}";
            string expectedOutput = @"Val ";
            string expectedIL = @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (Program.IGoo<string> V_0, //i
  Program.GooVal V_1, //e
  Program.IGoo<string> V_2)
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloca.s   V_1
  IL_0005:  initobj    ""Program.GooVal""
  IL_000b:  ldc.i4.1
  IL_000c:  bgt.s      IL_0018
  IL_000e:  ldloc.1
  IL_000f:  box        ""Program.GooVal""
  IL_0014:  stloc.2
  IL_0015:  ldloc.2
  IL_0016:  br.s       IL_0019
  IL_0018:  ldloc.0
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  callvirt   ""string Program.IGoo<string>.Goo()""
  IL_0020:  call       ""void System.Console.Write(string)""
  IL_0025:  ret
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL);
        }

        [Fact]
        public void TestCoalesceRequiringBox()
        {
            var source = @"
public static class Program
{
    public static void Main()
    {
        IGoo<string> i = null;
        GooVal e = new GooVal();
        GooVal? n = e;

        i = i ?? e;
        System.Console.Write(i.Goo());

        i = null;
        i = n ?? i;
        System.Console.Write(i.Goo());
    }

    interface IGoo<T> { T Goo(); }

    struct GooVal : IGoo<string>
    {
        public string Goo() { return ""Val ""; }
    }
}";
            string expectedOutput = @"Val Val ";
            string expectedIL = @"
{
  // Code size       81 (0x51)
  .maxstack  3
  .locals init (Program.IGoo<string> V_0, //i
  Program.GooVal V_1, //e
  Program.GooVal? V_2,
  Program.IGoo<string> V_3)
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_1
  IL_0004:  initobj    ""Program.GooVal""
  IL_000a:  ldloc.1
  IL_000b:  newobj     ""Program.GooVal?..ctor(Program.GooVal)""
  IL_0010:  ldloc.0
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_001b
  IL_0014:  pop
  IL_0015:  ldloc.1
  IL_0016:  box        ""Program.GooVal""
  IL_001b:  stloc.0
  IL_001c:  ldloc.0
  IL_001d:  callvirt   ""string Program.IGoo<string>.Goo()""
  IL_0022:  call       ""void System.Console.Write(string)""
  IL_0027:  ldnull
  IL_0028:  stloc.0
  IL_0029:  stloc.2
  IL_002a:  ldloca.s   V_2
  IL_002c:  call       ""bool Program.GooVal?.HasValue.get""
  IL_0031:  brtrue.s   IL_0036
  IL_0033:  ldloc.0
  IL_0034:  br.s       IL_0044
  IL_0036:  ldloca.s   V_2
  IL_0038:  call       ""Program.GooVal Program.GooVal?.GetValueOrDefault()""
  IL_003d:  box        ""Program.GooVal""
  IL_0042:  stloc.3
  IL_0043:  ldloc.3
  IL_0044:  stloc.0
  IL_0045:  ldloc.0
  IL_0046:  callvirt   ""string Program.IGoo<string>.Goo()""
  IL_004b:  call       ""void System.Console.Write(string)""
  IL_0050:  ret
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Program.Main", expectedIL);
        }

        [Fact(), WorkItem(543609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543609")]
        public void SeveralAdjacentIfsWithConditionalExpressions()
        {
            var source = @"
class Class1
{
    static void Main()
    {
        int x=0;
        int y=0;
        bool local_bool=true;
        if(true?true:local_bool) x++; else y++;
        if(true?false:local_bool) x++; else y++;
        if(true?local_bool:true) x++; else y++;
    }
}
";

            string expectedOutput = @"";
            string expectedIL = @"
{
  // Code size       25 (0x19)
  .maxstack  3
  .locals init (int V_0, //x
  int V_1) //y
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  stloc.1
  IL_0004:  ldc.i4.1
  IL_0005:  ldloc.0
  IL_0006:  ldc.i4.1
  IL_0007:  add
  IL_0008:  stloc.0
  IL_0009:  ldloc.1
  IL_000a:  ldc.i4.1
  IL_000b:  add
  IL_000c:  stloc.1
  IL_000d:  brfalse.s  IL_0014
  IL_000f:  ldloc.0
  IL_0010:  ldc.i4.1
  IL_0011:  add
  IL_0012:  stloc.0
  IL_0013:  ret
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stloc.1
  IL_0018:  ret
}";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Class1.Main", expectedIL);
        }

        [Fact(), WorkItem(638289, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638289")]
        public void TestNestedConditionalAndNullOperators()
        {
            var src =
@"using System.Security;

[assembly: SecurityTransparent()]

public interface I {}

class A : I {}

class B : I {}

class D : I {}

public class C : I
{
    static I Tester(A a)
    {
        return a ?? (a != null ? (I)new B() : new C());
    }    
    static void Main()
    {
        System.Console.Write(Tester(null).GetType());
    }
}";
            var verify = CompileAndVerify(src,
                options: TestOptions.DebugExe,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester", @"
{
  // Code size       32 (0x20)
  .maxstack  2
  .locals init (I V_0,
                I V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  dup
  IL_0005:  brtrue.s   IL_001b
  IL_0007:  pop
  IL_0008:  ldarg.0
  IL_0009:  brtrue.s   IL_0014
  IL_000b:  newobj     ""C..ctor()""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  br.s       IL_001b
  IL_0014:  newobj     ""B..ctor()""
  IL_0019:  stloc.0
  IL_001a:  ldloc.0
  IL_001b:  stloc.1
  IL_001c:  br.s       IL_001e
  IL_001e:  ldloc.1
  IL_001f:  ret
}");
            // Optimized
            verify = CompileAndVerify(src,
                expectedOutput: "C");
            verify.VerifyIL("C.Tester",
@"
{
  // Code size       26 (0x1a)
  .maxstack  2
  .locals init (I V_0)
  IL_0000:  ldarg.0
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0019
  IL_0006:  pop
  IL_0007:  ldarg.0
  IL_0008:  brtrue.s   IL_0012
  IL_000a:  newobj     ""C..ctor()""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  ret
  IL_0012:  newobj     ""B..ctor()""
  IL_0017:  stloc.0
  IL_0018:  ldloc.0
  IL_0019:  ret
}");
        }

        [Fact(), WorkItem(543609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543609")]
        public void UnreachableLabelInUnreachableCode()
        {
            var source = @"
class Class1
{
    static void Main()
    {
        var local = ""hi"";

        if (true)
        {       
        } 
        else
        {        
            goto unreachable;    // this is not emitted
        }

        goto reachable;

        // stack for this block == 1 because of stacklocal
        System.Console.WriteLine(""unreachable"");

        // label to which we have not seen branches yet
        unreachable:    

            System.Console.WriteLine(""hello"");

        reachable:
        System.Console.WriteLine(local);
    }
}
    ";

            string expectedOutput = @"hi";
            string expectedIL = @"
{
  // Code size       11 (0xb)
  .maxstack  2
  IL_0000:  ldstr      ""hi""
  IL_0005:  call       ""void System.Console.WriteLine(string)""
  IL_000a:  ret
}
";
            CompileAndVerify(source, expectedOutput: expectedOutput).VerifyIL("Class1.Main", expectedIL);
        }

        [Fact, WorkItem(12439, "https://github.com/dotnet/roslyn/issues/12439")]
        public void GenericStructInLambda()
        {
            var source = @"
using System;

static class LiveList
{
    struct WhereInfo<TSource>
    {
        public int Key { get; set; }
    }

    static void Where<TSource>()
    {
        Action subscribe = () =>
        {
            WhereInfo<TSource>? previous = null;

            var previousKey = previous?.Key;
        };
    }
}";

            CompileAndVerify(source);
        }

        [Fact, WorkItem(12439, "https://github.com/dotnet/roslyn/issues/12439")]
        public void GenericStructInIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

static class LiveList
{
    struct WhereInfo<TSource>
    {
        public int Key { get; set; }
    }

    static IEnumerable<int> Where<TSource>()
    {
        WhereInfo<TSource>? previous = null;

        var previousKey = previous?.Key;

        yield break;
    }
}";

            CompileAndVerify(source);
        }

        [Fact, WorkItem(17756, "https://github.com/dotnet/roslyn/issues/17756")]
        public void TestConditionalOperatorNotLvalue()
        {
            var source = @"
    class Program
    {

        interface IIncrementable
        {
            int Increment();
        }

        struct S1: IIncrementable
        {
            public int field;
            public int Increment() => field++;
        }

        static void Main()
        {
            S1 v = default(S1);
            v.Increment();

            (true ? v : default(S1)).Increment();

            System.Console.WriteLine(v.field);

            System.Console.WriteLine((false ? default(S1): v).Increment());

            System.Console.WriteLine(v.field);

            Test(ref v);
            System.Console.WriteLine(v.field);
        }

        static void Test<T>(ref T arg) where T:IIncrementable
        {
            (true ? arg : default(T)).Increment();
        }
    }
";
            string expectedOutput = @"1
1
1
1";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact, WorkItem(17756, "https://github.com/dotnet/roslyn/issues/17756")]
        public void TestConditionalOperatorNotLvaluePointerElementAccess()
        {
            var source = @"
class Program
{

    struct S1 
    {
        public int field;
        public int Increment() => field++;

    }

    unsafe static void Main()
    {
        S1 v = default(S1);
        v.Increment();

        System.Console.WriteLine(v.field);

        S1* pv = &v;

        (true ? pv[0] : default(S1)).Increment();

        System.Console.WriteLine(v.field);
    }
}
";
            string expectedOutput = @"1
1";
            CompileAndVerify(source, expectedOutput: expectedOutput, options: TestOptions.UnsafeReleaseExe, verify: Verification.Fails);
        }

        [Fact, WorkItem(17756, "https://github.com/dotnet/roslyn/issues/17756")]
        public void TestConditionalOperatorNotLvalueRefCall()
        {
            var source = @"
class Program
{

    struct S1 
    {
        public int field;
        public int Increment() => field++;
    }

    private static S1 sv;

    private static ref S1 M1()
    {
        return ref sv;
    }

    static void Main()
    {
        sv.Increment();

        System.Console.WriteLine(sv.field);

        (true ? M1() : default(S1)).Increment();

        System.Console.WriteLine(sv.field);
    }
}
";
            string expectedOutput = @"1
1";
            CompileAndVerify(source, expectedOutput: expectedOutput);
        }

        [Fact]
        public void RefReadonlyConditional()
        {
            var comp = CompileAndVerify(@"
using System;

struct S
{
    public int X;
    public S(int x) => X = x;

    public void Mutate() => X++;
}

class C
{
    static void Main()
    {
        S local1 = new S(0);
        S local2 = new S(0);
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        bool condition = false;

        (condition ? ref local1 : ref local2).Mutate();
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        (condition ? ref local1 : ref local2).X = 0;
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        ref readonly S ro1 = ref local1;
        ref readonly S ro2 = ref local2;

        (condition ? ref local1 : ref ro2).Mutate();
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        (condition ? ref ro1 : ref local2).Mutate();
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        (!condition ? ref local1 : ref ro2).Mutate();
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);

        (!condition ? ref ro1 : ref local2).Mutate();
        Console.WriteLine(local1.X);
        Console.WriteLine(local2.X);
    }
}", expectedOutput: @"0
0
0
1
0
0
0
0
0
0
0
0
0
0");
            comp.VerifyIL("C.Main", @"
{
  // Code size      290 (0x122)
  .maxstack  3
  .locals init (S V_0, //local1
                S V_1, //local2
                S& V_2, //ro1
                S& V_3, //ro2
                S V_4)
  IL_0000:  ldloca.s   V_0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""S..ctor(int)""
  IL_0008:  ldloca.s   V_1
  IL_000a:  ldc.i4.0
  IL_000b:  call       ""S..ctor(int)""
  IL_0010:  ldloc.0
  IL_0011:  ldfld      ""int S.X""
  IL_0016:  call       ""void System.Console.WriteLine(int)""
  IL_001b:  ldloc.1
  IL_001c:  ldfld      ""int S.X""
  IL_0021:  call       ""void System.Console.WriteLine(int)""
  IL_0026:  ldc.i4.0
  IL_0027:  dup
  IL_0028:  brtrue.s   IL_002e
  IL_002a:  ldloca.s   V_1
  IL_002c:  br.s       IL_0030
  IL_002e:  ldloca.s   V_0
  IL_0030:  call       ""void S.Mutate()""
  IL_0035:  ldloc.0
  IL_0036:  ldfld      ""int S.X""
  IL_003b:  call       ""void System.Console.WriteLine(int)""
  IL_0040:  ldloc.1
  IL_0041:  ldfld      ""int S.X""
  IL_0046:  call       ""void System.Console.WriteLine(int)""
  IL_004b:  dup
  IL_004c:  brtrue.s   IL_0052
  IL_004e:  ldloca.s   V_1
  IL_0050:  br.s       IL_0054
  IL_0052:  ldloca.s   V_0
  IL_0054:  ldc.i4.0
  IL_0055:  stfld      ""int S.X""
  IL_005a:  ldloc.0
  IL_005b:  ldfld      ""int S.X""
  IL_0060:  call       ""void System.Console.WriteLine(int)""
  IL_0065:  ldloc.1
  IL_0066:  ldfld      ""int S.X""
  IL_006b:  call       ""void System.Console.WriteLine(int)""
  IL_0070:  ldloca.s   V_0
  IL_0072:  stloc.2
  IL_0073:  ldloca.s   V_1
  IL_0075:  stloc.3
  IL_0076:  dup
  IL_0077:  brtrue.s   IL_0081
  IL_0079:  ldloc.3
  IL_007a:  ldobj      ""S""
  IL_007f:  br.s       IL_0082
  IL_0081:  ldloc.0
  IL_0082:  stloc.s    V_4
  IL_0084:  ldloca.s   V_4
  IL_0086:  call       ""void S.Mutate()""
  IL_008b:  ldloc.0
  IL_008c:  ldfld      ""int S.X""
  IL_0091:  call       ""void System.Console.WriteLine(int)""
  IL_0096:  ldloc.1
  IL_0097:  ldfld      ""int S.X""
  IL_009c:  call       ""void System.Console.WriteLine(int)""
  IL_00a1:  dup
  IL_00a2:  brtrue.s   IL_00a7
  IL_00a4:  ldloc.1
  IL_00a5:  br.s       IL_00ad
  IL_00a7:  ldloc.2
  IL_00a8:  ldobj      ""S""
  IL_00ad:  stloc.s    V_4
  IL_00af:  ldloca.s   V_4
  IL_00b1:  call       ""void S.Mutate()""
  IL_00b6:  ldloc.0
  IL_00b7:  ldfld      ""int S.X""
  IL_00bc:  call       ""void System.Console.WriteLine(int)""
  IL_00c1:  ldloc.1
  IL_00c2:  ldfld      ""int S.X""
  IL_00c7:  call       ""void System.Console.WriteLine(int)""
  IL_00cc:  dup
  IL_00cd:  brfalse.s  IL_00d7
  IL_00cf:  ldloc.3
  IL_00d0:  ldobj      ""S""
  IL_00d5:  br.s       IL_00d8
  IL_00d7:  ldloc.0
  IL_00d8:  stloc.s    V_4
  IL_00da:  ldloca.s   V_4
  IL_00dc:  call       ""void S.Mutate()""
  IL_00e1:  ldloc.0
  IL_00e2:  ldfld      ""int S.X""
  IL_00e7:  call       ""void System.Console.WriteLine(int)""
  IL_00ec:  ldloc.1
  IL_00ed:  ldfld      ""int S.X""
  IL_00f2:  call       ""void System.Console.WriteLine(int)""
  IL_00f7:  brfalse.s  IL_00fc
  IL_00f9:  ldloc.1
  IL_00fa:  br.s       IL_0102
  IL_00fc:  ldloc.2
  IL_00fd:  ldobj      ""S""
  IL_0102:  stloc.s    V_4
  IL_0104:  ldloca.s   V_4
  IL_0106:  call       ""void S.Mutate()""
  IL_010b:  ldloc.0
  IL_010c:  ldfld      ""int S.X""
  IL_0111:  call       ""void System.Console.WriteLine(int)""
  IL_0116:  ldloc.1
  IL_0117:  ldfld      ""int S.X""
  IL_011c:  call       ""void System.Console.WriteLine(int)""
  IL_0121:  ret
}");
        }

        [Fact]
        public void RefConditionalOperatorInValConditional()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        S1 local1 = 1;
        S1 local2 = 2;

        bool condition = false;

        (condition ? 
            condition ? ref local1 : ref local2 : 
            condition ? ref local1 : ref local2).Mutate();

        // must print '2', the above mutation is applied to an rvalue.
        System.Console.WriteLine(local2.field);

        (false ? 
            condition ? ref local1 : ref local2 : 
            condition ? ref local1 : ref local2).Mutate();

        // must print '2', the above mutation is applied to an rvalue.
        System.Console.WriteLine(local2.field);

        (false ? 
            false ? ref local1 : ref local2 : 
            false ? ref local1 : ref local2).Mutate();

        // must print '2', the above mutation is applied to an rvalue.
        System.Console.WriteLine(local2.field);
    }

    struct S1
    {
        public int field;

        public static implicit operator S1(int arg) => new S1 {field = arg };
        
        public void Mutate()
        {
            field = 42;
        }
    }

}
";
            string expectedOutput = @"2
2
2";
            var verify = CompileAndVerify(source, expectedOutput: expectedOutput);

            verify.VerifyIL("Program.Main",
@"
{
  // Code size      101 (0x65)
  .maxstack  1
  .locals init (Program.S1 V_0, //local1
                Program.S1 V_1, //local2
                bool V_2, //condition
                Program.S1 V_3)
  IL_0000:  ldc.i4.1
  IL_0001:  call       ""Program.S1 Program.S1.op_Implicit(int)""
  IL_0006:  stloc.0
  IL_0007:  ldc.i4.2
  IL_0008:  call       ""Program.S1 Program.S1.op_Implicit(int)""
  IL_000d:  stloc.1
  IL_000e:  ldc.i4.0
  IL_000f:  stloc.2
  IL_0010:  ldloc.2
  IL_0011:  brtrue.s   IL_001c
  IL_0013:  ldloc.2
  IL_0014:  brtrue.s   IL_0019
  IL_0016:  ldloc.1
  IL_0017:  br.s       IL_0023
  IL_0019:  ldloc.0
  IL_001a:  br.s       IL_0023
  IL_001c:  ldloc.2
  IL_001d:  brtrue.s   IL_0022
  IL_001f:  ldloc.1
  IL_0020:  br.s       IL_0023
  IL_0022:  ldloc.0
  IL_0023:  stloc.3
  IL_0024:  ldloca.s   V_3
  IL_0026:  call       ""void Program.S1.Mutate()""
  IL_002b:  ldloc.1
  IL_002c:  ldfld      ""int Program.S1.field""
  IL_0031:  call       ""void System.Console.WriteLine(int)""
  IL_0036:  ldloc.2
  IL_0037:  brtrue.s   IL_003c
  IL_0039:  ldloc.1
  IL_003a:  br.s       IL_003d
  IL_003c:  ldloc.0
  IL_003d:  stloc.3
  IL_003e:  ldloca.s   V_3
  IL_0040:  call       ""void Program.S1.Mutate()""
  IL_0045:  ldloc.1
  IL_0046:  ldfld      ""int Program.S1.field""
  IL_004b:  call       ""void System.Console.WriteLine(int)""
  IL_0050:  ldloc.1
  IL_0051:  stloc.3
  IL_0052:  ldloca.s   V_3
  IL_0054:  call       ""void Program.S1.Mutate()""
  IL_0059:  ldloc.1
  IL_005a:  ldfld      ""int Program.S1.field""
  IL_005f:  call       ""void System.Console.WriteLine(int)""
  IL_0064:  ret
}");
        }

        [Fact]
        [WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")]
        public void ConditionalAccessUnconstrainedTField()
        {
            var source = @"using System;
public class C<T>
{
    public C(T t) => this.t = t;
    public C(){}

    private T t;
    
    public void Print()
    {
        Console.WriteLine(t?.ToString());
        Console.WriteLine(t);
    }
    
}

public struct S
{
    int a;
    public override string ToString() => a++.ToString();
}

public static class Program
{
    public static void Main()
    {
        new C<S>().Print();
        new C<S?>().Print();
        new C<S?>(new S()).Print();
        new C<string>(""hello"").Print();
        new C<string>().Print();
    }
}";
            var verify = CompileAndVerify(source, expectedOutput: @"0
1


0
0
hello
hello");

            verify.VerifyIL("C<T>.Print()", @"
{
  // Code size       75 (0x4b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T C<T>.t""
  IL_0006:  ldloca.s   V_0
  IL_0008:  initobj    ""T""
  IL_000e:  ldloc.0
  IL_000f:  box        ""T""
  IL_0014:  brtrue.s   IL_002a
  IL_0016:  ldobj      ""T""
  IL_001b:  stloc.0
  IL_001c:  ldloca.s   V_0
  IL_001e:  ldloc.0
  IL_001f:  box        ""T""
  IL_0024:  brtrue.s   IL_002a
  IL_0026:  pop
  IL_0027:  ldnull
  IL_0028:  br.s       IL_0035
  IL_002a:  constrained. ""T""
  IL_0030:  callvirt   ""string object.ToString()""
  IL_0035:  call       ""void System.Console.WriteLine(string)""
  IL_003a:  ldarg.0
  IL_003b:  ldfld      ""T C<T>.t""
  IL_0040:  box        ""T""
  IL_0045:  call       ""void System.Console.WriteLine(object)""
  IL_004a:  ret
}");

        }

        [Fact]
        [WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")]
        public void ConditionalAccessReadonlyUnconstrainedTField()
        {
            var source = @"using System;
public class C<T> 
{
    public C(T t) => this.t = t;
    public C(){}

    readonly T t;
    
    public void Print()
    {
        Console.WriteLine(t?.ToString());
        Console.WriteLine(t);
    }
}

public struct S
{
    int a;
    public override string ToString() => a++.ToString();
}

public static class Program
{
    public static void Main()
    {
        new C<S>().Print();
        new C<S?>().Print();
        new C<S?>(new S()).Print();
        new C<string>(""hello"").Print();
        new C<string>().Print();
    }
}
";
            var verify = CompileAndVerify(source, expectedOutput: @"0
0


0
0
hello
hello");

            verify.VerifyIL("C<T>.Print()", @"
{
  // Code size       54 (0x36)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.t""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_0020
  IL_0015:  constrained. ""T""
  IL_001b:  callvirt   ""string object.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ldarg.0
  IL_0026:  ldfld      ""T C<T>.t""
  IL_002b:  box        ""T""
  IL_0030:  call       ""void System.Console.WriteLine(object)""
  IL_0035:  ret
}");

        }

        [Fact]
        [WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")]
        public void ConditionalAccessUnconstrainedTLocal()
        {
            var source = @"using System;
public class C<T>
{
    public C(T t) => this.t = t;
    public C(){}

    private T t;
    
    public void Print() 
    {
        var temp = t;
        Console.WriteLine(temp?.ToString());
        Console.WriteLine(temp);
    }
    
}

public struct S
{
    int a;
    public override string ToString() => a++.ToString();
}

public static class Program
{
    public static void Main()
    {
        new C<S>().Print();
        new C<S?>().Print();
        new C<S?>(new S()).Print();
        new C<string>(""hello"").Print();
        new C<string>().Print();
    }
}";
            var verify = CompileAndVerify(source, expectedOutput: @"0
1


0
1
hello
hello");

            verify.VerifyIL("C<T>.Print()", @"
{
  // Code size       48 (0x30)
  .maxstack  1
  .locals init (T V_0) //temp
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""T C<T>.t""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  box        ""T""
  IL_000d:  brtrue.s   IL_0012
  IL_000f:  ldnull
  IL_0010:  br.s       IL_001f
  IL_0012:  ldloca.s   V_0
  IL_0014:  constrained. ""T""
  IL_001a:  callvirt   ""string object.ToString()""
  IL_001f:  call       ""void System.Console.WriteLine(string)""
  IL_0024:  ldloc.0
  IL_0025:  box        ""T""
  IL_002a:  call       ""void System.Console.WriteLine(object)""
  IL_002f:  ret
}");

        }

        [Fact]
        [WorkItem(3519, "https://github.com/dotnet/roslyn/issues/35319")]
        public void ConditionalAccessUnconstrainedTTemp()
        {
            var source = @"using System;
public class C<T>
{
    public C(T t) => this.t = t;
    public C(){}

    T t;

    T M() => t;
    
    public void Print() => Console.WriteLine(M()?.ToString());
}

public static class Program
{
    public static void Main()
    {
        new C<int>().Print();
        new C<int?>().Print();
        new C<int?>(0).Print();
        new C<string>(""hello"").Print();
        new C<string>().Print();
    }
}
";
            var verify = CompileAndVerify(source, expectedOutput: @"0

0
hello
");

            verify.VerifyIL("C<T>.Print()", @"
{
  // Code size       38 (0x26)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""T C<T>.M()""
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  ldloc.0
  IL_000a:  box        ""T""
  IL_000f:  brtrue.s   IL_0015
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  br.s       IL_0020
  IL_0015:  constrained. ""T""
  IL_001b:  callvirt   ""string object.ToString()""
  IL_0020:  call       ""void System.Console.WriteLine(string)""
  IL_0025:  ret
}");

        }
    }
}
