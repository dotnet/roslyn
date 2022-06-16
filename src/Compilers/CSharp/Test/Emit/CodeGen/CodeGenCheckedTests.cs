// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenCheckedTests : CSharpTestBase
    {
        [Fact]
        public void CheckedExpression_Signed()
        {
            var source = @"
class C 
{
    public static int Add(int a, int b) 
    {
        return checked(a+b);
    } 

    public static int Sub(int a, int b) 
    {
        return checked(a-b);
    } 

    public static int Mul(int a, int b) 
    {
        return checked(a*b);
    } 

    public static int Minus(int a) 
    {
        return checked(-a);
    } 
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
            verifier.VerifyIL("C.Sub", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  ret
}
");

            verifier.VerifyIL("C.Mul", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf
  IL_0003:  ret
}
");

            verifier.VerifyIL("C.Minus", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  ldarg.0
  IL_0002:  sub.ovf
  IL_0003:  ret
}

");
        }

        [Fact]
        public void CheckedExpression_Unsigned()
        {
            var source = @"
class C 
{
    public static long Add(uint a, uint b) 
    {
        return checked(a+b);
    } 

    public static long Sub(uint a, uint b) 
    {
        return checked(a-b);
    } 

    public static long Mul(uint a, uint b) 
    {
        return checked(a*b);
    } 

    public static long Minus(uint a) 
    {
        return checked(-a);
    } 
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf.un
  IL_0003:  conv.u8
  IL_0004:  ret
}
");
            verifier.VerifyIL("C.Sub", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf.un
  IL_0003:  conv.u8
  IL_0004:  ret
}
");

            verifier.VerifyIL("C.Mul", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf.un
  IL_0003:  conv.u8
  IL_0004:  ret
}
");

            verifier.VerifyIL("C.Minus", @"
{
  // Code size        6 (0x6)
  .maxstack  2
  IL_0000:  ldc.i4.0
  IL_0001:  conv.i8
  IL_0002:  ldarg.0
  IL_0003:  conv.u8
  IL_0004:  sub.ovf
  IL_0005:  ret
}
");
        }

        [Fact]
        public void CheckedExpression_Enums()
        {
            var source = @"
enum E
{
    A
}

class C 
{
    public static E Add1(E a, int b) 
    {
        return checked(a+b);
    }
    
    public static E Add2(int a, E b) 
    {
        return checked(a+b);
    }

    public static E Sub(E a, E b) 
    {
        return (E)checked(a-b);
    }  
    
    public static E PostInc(E e) 
    {
        return checked(e++);
    }  
    
    public static E PreInc(E e) 
    {
        return checked(--e);
    }  
    
    public static E PostDec(E e) 
    {
        return checked(e--);
    }
    
    public static E PreDec(E e) 
    {
        return checked(--e);
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add1", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
            verifier.VerifyIL("C.Add2", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");

            verifier.VerifyIL("C.Sub", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  ret
}
");

            verifier.VerifyIL("C.PostInc", @"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  add.ovf
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.PostDec", @"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  sub.ovf
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.PreInc", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.PreDec", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
        }

        [Fact]
        public void CheckedExpression_Pointers()
        {
            var source = @"
enum E
{
    A
}

unsafe struct C 
{
    public static C* Add_Int1(C* a, int b) 
    {
        return checked(a+b);
    }
    
    public static C* Add_Int2(C* a, int b) 
    {
        return checked(b+a);
    }
    
    public static C* Add_UInt1(C* a, uint b) 
    {
        return checked(a+b);
    }
    
    public static C* Add_UInt2(C* a, uint b) 
    {
        return checked(b+a);
    }
    
    public static C* Add_Long1(C* a, long b) 
    {
        return checked(a+b);
    }
    
    public static C* Add_Long2(C* a, long b) 
    {
        return checked(b+a);
    }    
    
    public static C* Add_ULong1(C* a, ulong b) 
    {
        return checked(a+b);
    }       
    
    public static C* Add_ULong2(C* a, ulong b) 
    {
        return checked(b+a);
    }     
    
    public static C* Sub_Int(C* a, int b) 
    {
        return checked(a-b);
    }    
    
    public static C* Sub_UInt(C* a, uint b) 
    {
        return checked(a-b);
    }    
    
    public static C* Sub_Long(C* a, long b) 
    {
        return checked(a-b);
    }
    
    public static C* Sub_ULong(C* a, ulong b) 
    {
        return checked(a-b);
    }
    
    public static long Sub_Ptr(C* a, C* b) 
    {
        return checked(a-b);
    }
    
    public static C* PostInc(C* a) 
    {
        return checked(a++);
    }      
    
    public static C* PostDec(C* a) 
    {
        return checked(a--);
    }
}
";
            var verifier = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll, verify: Verification.Fails);

            // NOTE: unsigned addition
            verifier.VerifyIL("C.Add_Int1", @"
{
  // Code size       12 (0xc)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  conv.i
  IL_0003:  sizeof     ""C""
  IL_0009:  mul.ovf
  IL_000a:  add.ovf.un
  IL_000b:  ret
}");
            // NOTE: signed addition
            verifier.VerifyIL("C.Add_Int2", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  conv.i
  IL_0002:  sizeof     ""C""
  IL_0008:  mul.ovf
  IL_0009:  ldarg.0
  IL_000a:  add.ovf
  IL_000b:  ret
}");
            // NOTE: unsigned addition
            verifier.VerifyIL("C.Add_UInt1", @"{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  conv.u8
  IL_0003:  sizeof     ""C""
  IL_0009:  conv.i8
  IL_000a:  mul.ovf
  IL_000b:  conv.i
  IL_000c:  add.ovf.un
  IL_000d:  ret
}");
            // NOTE: signed addition
            verifier.VerifyIL("C.Add_UInt2", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  conv.u8
  IL_0002:  sizeof     ""C""
  IL_0008:  conv.i8
  IL_0009:  mul.ovf
  IL_000a:  conv.i
  IL_000b:  ldarg.0
  IL_000c:  add.ovf
  IL_000d:  ret
}");
            // NOTE: unsigned addition
            verifier.VerifyIL("C.Add_Long1", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""C""
  IL_0008:  conv.i8
  IL_0009:  mul.ovf
  IL_000a:  conv.i
  IL_000b:  add.ovf.un
  IL_000c:  ret
}");
            // NOTE: signed addition
            verifier.VerifyIL("C.Add_Long2", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  sizeof     ""C""
  IL_0007:  conv.i8
  IL_0008:  mul.ovf
  IL_0009:  conv.i
  IL_000a:  ldarg.0
  IL_000b:  add.ovf
  IL_000c:  ret
}");
            // NOTE: unsigned addition
            verifier.VerifyIL("C.Add_ULong1", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""C""
  IL_0008:  conv.ovf.u8
  IL_0009:  mul.ovf.un
  IL_000a:  conv.u
  IL_000b:  add.ovf.un
  IL_000c:  ret
}");
            // NOTE: unsigned addition (differs from previous Add_*2's)
            verifier.VerifyIL("C.Add_ULong2", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  sizeof     ""C""
  IL_0007:  conv.ovf.u8
  IL_0008:  mul.ovf.un
  IL_0009:  conv.u
  IL_000a:  ldarg.0
  IL_000b:  add.ovf.un
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Sub_Int", @"
{
  // Code size       12 (0xc)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  conv.i
  IL_0003:  sizeof     ""C""
  IL_0009:  mul.ovf
  IL_000a:  sub.ovf.un
  IL_000b:  ret
}");
            verifier.VerifyIL("C.Sub_UInt", @"
{
  // Code size       14 (0xe)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  conv.u8
  IL_0003:  sizeof     ""C""
  IL_0009:  conv.i8
  IL_000a:  mul.ovf
  IL_000b:  conv.i
  IL_000c:  sub.ovf.un
  IL_000d:  ret
}");
            verifier.VerifyIL("C.Sub_Long", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""C""
  IL_0008:  conv.i8
  IL_0009:  mul.ovf
  IL_000a:  conv.i
  IL_000b:  sub.ovf.un
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Sub_ULong", @"
{
  // Code size       13 (0xd)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sizeof     ""C""
  IL_0008:  conv.ovf.u8
  IL_0009:  mul.ovf.un
  IL_000a:  conv.u
  IL_000b:  sub.ovf.un
  IL_000c:  ret
}");
            verifier.VerifyIL("C.Sub_Ptr", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  sizeof     ""C""
  IL_0009:  div
  IL_000a:  conv.i8
  IL_000b:  ret
}");
            verifier.VerifyIL("C.PostInc", @"
{
  // Code size       12 (0xc)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  sizeof     ""C""
  IL_0008:  add.ovf.un
  IL_0009:  starg.s    V_0
  IL_000b:  ret
}");
            verifier.VerifyIL("C.PostDec", @"
{
  // Code size       12 (0xc)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  sizeof     ""C""
  IL_0008:  sub.ovf.un
  IL_0009:  starg.s    V_0
  IL_000b:  ret
}");
        }

        [Fact]
        public void CheckedExpression_Optimizer()
        {
            var source = @"
class C
{
    static bool Local()
    {
        int a = 1;
        return checked(-a) != -1;
    }

    static bool LocalInc()
    {
        int a = 1;
        return checked(-(a++)) != -1;
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Local", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  ldloc.0
  IL_0004:  sub.ovf
  IL_0005:  ldc.i4.m1
  IL_0006:  ceq
  IL_0008:  ldc.i4.0
  IL_0009:  ceq
  IL_000b:  ret
}
");
            verifier.VerifyIL("C.LocalInc", @"
{
  // Code size       16 (0x10)
  .maxstack  4
  .locals init (int V_0) //a
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldc.i4.0
  IL_0003:  ldloc.0
  IL_0004:  dup
  IL_0005:  ldc.i4.1
  IL_0006:  add.ovf
  IL_0007:  stloc.0
  IL_0008:  sub.ovf
  IL_0009:  ldc.i4.m1
  IL_000a:  ceq
  IL_000c:  ldc.i4.0
  IL_000d:  ceq
  IL_000f:  ret
}
");
        }

        [Fact]
        public void CheckedExpression_IncDec()
        {
            var source = @"
class C 
{
    public static int PostInc(int a) 
    {
        return checked(a++);
    } 

    public static int PreInc(int a) 
    {
        return checked(++a);
    } 

    public static int PostDec(int a) 
    {
        return checked(a--);
    } 

    public static int PreDec(int a) 
    {
        return checked(--a);
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.PostInc", @"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  add.ovf
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.PreInc", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.PostDec", @"
{
  // Code size        7 (0x7)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  sub.ovf
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.PreDec", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  sub.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
        }

        [Fact]
        public void CheckedExpression_CompoundAssignment()
        {
            var source = @"
class C 
{
    public static int Add(int a, int b) 
    {
        return checked(a+=b);
    } 

    public static int Sub(int a, int b) 
    {
        return checked(a-=b);
    } 

    public static int Mul(int a, int b) 
    {
        return checked(a*=b);
    } 

    public static int Div(int a, int b) 
    {
        return checked(a/=b);
    } 

    public static int Rem(int a, int b) 
    {
        return checked(a%=b);
    } 
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.Sub", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Mul", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul.ovf
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            // both checked and unchecked generate the same instruction that checks overflow

            verifier.VerifyIL("C.Div", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Rem", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
        }

        [Fact]
        public void Checked_ImplicitConversions_CompoundAssignment()
        {
            var source = @"
class C 
{
    public static int Add(short a) 
    {
        return checked(a+=1000);
    }
}";

            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4     0x3e8
  IL_0006:  add.ovf
  IL_0007:  conv.ovf.i2
  IL_0008:  dup
  IL_0009:  starg.s    V_0
  IL_000b:  ret
}
");
        }

        [Fact]
        public void Checked_ImplicitConversions_IncDec()
        {
            var source = @"
class C 
{
    class X
    {
        public static implicit operator short(X a) 
        {
            return 1;
        }

        public static implicit operator X(short a)
        {
            return new X();
        }
    }
    
    static void PostIncUserDefined(X x) 
    {
        checked { x++; }
    }

    public static int PostInc(short a) 
    {
        return checked(a++);
    }
    
    public static int PreInc(short a) 
    {
        return checked(++a);
    }

    static short? s = 0;
    static short? PostIncNullable() 
    {
        checked { return s++; }
    }
}";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.PostIncUserDefined", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  call       ""short C.X.op_Implicit(C.X)""
  IL_0006:  ldc.i4.1
  IL_0007:  add.ovf
  IL_0008:  conv.ovf.i2
  IL_0009:  call       ""C.X C.X.op_Implicit(short)""
  IL_000e:  starg.s    V_0
  IL_0010:  ret
}
");

            verifier.VerifyIL("C.PostInc", @"
{
  // Code size        8 (0x8)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.1
  IL_0003:  add.ovf
  IL_0004:  conv.ovf.i2
  IL_0005:  starg.s    V_0
  IL_0007:  ret
}
");

            verifier.VerifyIL("C.PreInc", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  conv.ovf.i2
  IL_0004:  dup
  IL_0005:  starg.s    V_0
  IL_0007:  ret
}
");

            verifier.VerifyIL("C.PostIncNullable", @"{
  // Code size       48 (0x30)
  .maxstack  3
  .locals init (short? V_0,
  short? V_1)
  IL_0000:  ldsfld     ""short? C.s""
  IL_0005:  dup
  IL_0006:  stloc.0
  IL_0007:  ldloca.s   V_0
  IL_0009:  call       ""bool short?.HasValue.get""
  IL_000e:  brtrue.s   IL_001b
  IL_0010:  ldloca.s   V_1
  IL_0012:  initobj    ""short?""
  IL_0018:  ldloc.1
  IL_0019:  br.s       IL_002a
  IL_001b:  ldloca.s   V_0
  IL_001d:  call       ""short short?.GetValueOrDefault()""
  IL_0022:  ldc.i4.1
  IL_0023:  add.ovf
  IL_0024:  conv.ovf.i2
  IL_0025:  newobj     ""short?..ctor(short)""
  IL_002a:  stsfld     ""short? C.s""
  IL_002f:  ret
}
");
        }

        [Fact]
        public void Checked_ImplicitConversions_ArraySize()
        {
            var source = @"
class C 
{
    public static int[] ArraySize(long size) 
    {
        checked
        {
            return new int[size];
        }
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.ArraySize", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i
  IL_0002:  newarr     ""int""
  IL_0007:  ret
}
");
        }

        [Fact]
        public void Checked_ImplicitConversions_ForEach()
        {
            var source = @"
class C 
{
    static void ForEachString() 
    {
        checked 
        {
            foreach (byte b in ""hello"") 
            {
            
            }
        }
    }
    
    static void ForEachVector() 
    {
        checked 
        {
            foreach (short b in new int[] {1}) 
            {
            
            }
        }
    }
    
    static void ForEachMultiDimArray() 
    {
        checked 
        {
            foreach (short b in new int[,] {{1}}) 
            {
            
            }
        }
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.ForEachString", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  .locals init (string V_0,
  int V_1)
  IL_0000:  ldstr      ""hello""
  IL_0005:  stloc.0
  IL_0006:  ldc.i4.0
  IL_0007:  stloc.1
  IL_0008:  br.s       IL_0017
  IL_000a:  ldloc.0
  IL_000b:  ldloc.1
  IL_000c:  callvirt   ""char string.this[int].get""
  IL_0011:  conv.ovf.u1.un
  IL_0012:  pop
  IL_0013:  ldloc.1
  IL_0014:  ldc.i4.1
  IL_0015:  add
  IL_0016:  stloc.1
  IL_0017:  ldloc.1
  IL_0018:  ldloc.0
  IL_0019:  callvirt   ""int string.Length.get""
  IL_001e:  blt.s      IL_000a
  IL_0020:  ret
}
");

            verifier.VerifyIL("C.ForEachVector", @"
{
  // Code size       31 (0x1f)
  .maxstack  4
  .locals init (int[] V_0,
  int V_1)
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  dup
  IL_0007:  ldc.i4.0
  IL_0008:  ldc.i4.1
  IL_0009:  stelem.i4
  IL_000a:  stloc.0
  IL_000b:  ldc.i4.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_0018
  IL_000f:  ldloc.0
  IL_0010:  ldloc.1
  IL_0011:  ldelem.i4
  IL_0012:  conv.ovf.i2
  IL_0013:  pop
  IL_0014:  ldloc.1
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stloc.1
  IL_0018:  ldloc.1
  IL_0019:  ldloc.0
  IL_001a:  ldlen
  IL_001b:  conv.i4
  IL_001c:  blt.s      IL_000f
  IL_001e:  ret
}
");

            verifier.VerifyIL("C.ForEachMultiDimArray", @"
{
  // Code size       85 (0x55)
  .maxstack  5
  .locals init (int[,] V_0,
  int V_1,
  int V_2,
  int V_3,
  int V_4)
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.1
  IL_0002:  newobj     ""int[*,*]..ctor""
  IL_0007:  dup
  IL_0008:  ldc.i4.0
  IL_0009:  ldc.i4.0
  IL_000a:  ldc.i4.1
  IL_000b:  call       ""int[*,*].Set""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldc.i4.0
  IL_0013:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0018:  stloc.1
  IL_0019:  ldloc.0
  IL_001a:  ldc.i4.1
  IL_001b:  callvirt   ""int System.Array.GetUpperBound(int)""
  IL_0020:  stloc.2
  IL_0021:  ldloc.0
  IL_0022:  ldc.i4.0
  IL_0023:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0028:  stloc.3
  IL_0029:  br.s       IL_0050
  IL_002b:  ldloc.0
  IL_002c:  ldc.i4.1
  IL_002d:  callvirt   ""int System.Array.GetLowerBound(int)""
  IL_0032:  stloc.s    V_4
  IL_0034:  br.s       IL_0047
  IL_0036:  ldloc.0
  IL_0037:  ldloc.3
  IL_0038:  ldloc.s    V_4
  IL_003a:  call       ""int[*,*].Get""
  IL_003f:  conv.ovf.i2
  IL_0040:  pop
  IL_0041:  ldloc.s    V_4
  IL_0043:  ldc.i4.1
  IL_0044:  add
  IL_0045:  stloc.s    V_4
  IL_0047:  ldloc.s    V_4
  IL_0049:  ldloc.2
  IL_004a:  ble.s      IL_0036
  IL_004c:  ldloc.3
  IL_004d:  ldc.i4.1
  IL_004e:  add
  IL_004f:  stloc.3
  IL_0050:  ldloc.3
  IL_0051:  ldloc.1
  IL_0052:  ble.s      IL_002b
  IL_0054:  ret
}");
        }

        [Fact]
        public void UncheckedExpression_CompoundAssignment()
        {
            var source = @"
class C 
{
    public static int Add(int a, int b) 
    {
        return unchecked(a+=b);
    } 

    public static int Sub(int a, int b) 
    {
        return unchecked(a-=b);
    } 

    public static int Mul(int a, int b) 
    {
        return unchecked(a*=b);
    } 

    public static int Div(int a, int b) 
    {
        return unchecked(a/=b);
    } 

    public static int Rem(int a, int b) 
    {
        return unchecked(a%=b);
    } 
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.Add", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
            verifier.VerifyIL("C.Sub", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  sub
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Mul", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  mul
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            // both checked and unchecked generate the same instruction that checks overflow

            verifier.VerifyIL("C.Div", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  div
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");

            verifier.VerifyIL("C.Rem", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  rem
  IL_0003:  dup
  IL_0004:  starg.s    V_0
  IL_0006:  ret
}
");
        }

        [Fact]
        public void CheckedBlock_Conversions()
        {
            var source = @"
enum E
{
   A = 1
}

class C
{
    public static uint SByte_UInt(sbyte a)
    {
        checked { return (uint)a; }
    }

    public static int UInt_Int(uint a)
    {
        checked { return (int)a; }
    }

    public static short Enum_Short(E a)
    {
        checked { return (short)a; }
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("C.SByte_UInt", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4
  IL_0002:  ret
}
");

            verifier.VerifyIL("C.UInt_Int", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i4.un
  IL_0002:  ret
}
");

            verifier.VerifyIL("C.Enum_Short", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.i2
  IL_0002:  ret
}
");
        }

        // Although C# 4.0 specification says that checked context never flows in a lambda, 
        // the Dev10 compiler implementation always flows the context in, except for 
        // when the lambda is directly a "parameter" of the checked/unchecked expression.
        // For Roslyn we decided to change the spec and always flow the context in.

        [Fact]
        public void Lambda_Statement()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        checked
        {
            System.Func<int, int> d1 = delegate(int i)
            {
                return i + 1;    // add_ovf
            };
        }
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_QueryStmt()
        {
            var verifier = CompileAndVerify(@"
using System.Linq;

class C
{
    static void F()
    {
        checked
        {
            var a = from x in new[] { 1 } select x * 2;  // mul_ovf
        }
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  mul.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_QueryExpr()
        {
            var verifier = CompileAndVerify(@"
using System.Linq;

class C
{
    static void F()
    {
        var a = checked(from x in new[] { 1 } select x * 2);  // mul_ovf
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.2
  IL_0002:  mul.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfAssignment()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        System.Func<int, int> d2;
        System.Func<int, int> d1 = checked(d2 = delegate(int i)
        {
            return i + 1;    // add_ovf
        });
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_Add()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        System.Func<int, int> d1 = checked(delegate (int i)
        {
            return i + 1;  // Dev10: add, Roslyn: add_ovf
        });
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_Cast()
        {
            var verifier = CompileAndVerify(@"
class C
{    
    static void F()
    {
        var d1 = checked((System.Func<int, int>)delegate (int i)
        {
            return i + 1;  // add_ovf
        });
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfCompoundAssignment()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        System.Func<int, int> d2 = null;
        System.Func<int, int> d1 = checked(d2 += delegate(int i)
        {
            return i + 1;    // add_ovf
        });
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfArgument()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }
    
    static void F()
    {
        System.Func<int, int> d1 = checked(Id(delegate(int i)
        {
            return i + 1;    // add_ovf
        }));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfArgument2()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = checked(Id(checked(delegate(int i)
        {
            return i + 1;    // add_ovf
        })));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddArgument3()
        {
            var verifier = CompileAndVerify(@"
class C
{   
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = unchecked(Id(checked(delegate(int i)
        {
            return i + 1;    // add
        })));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfArgument4()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d2;
        System.Func<int, int> d1 = unchecked(Id(checked(d2 = checked(delegate(int i)
        {
            return i + 1;    // add_ovf
        }))));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfArgument5()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = checked(Id(unchecked(delegate(int i)
        {
            return i + 1;    // add_ovf
        })));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddArgument6()
        {
            var verifier = CompileAndVerify(@"
class C
{   
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = checked(unchecked(Id(delegate(int i)
        {
            return i + 1;    // add
        })));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddArgument7()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = checked(Id(unchecked(Id(delegate(int i)
        {
            return i + 1;    // add
        }))));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_AddOvfArgument8()
        {
            var verifier = CompileAndVerify(@"
class C
{    
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        System.Func<int, int> d1 = checked(Id(unchecked(Id(delegate(int i)
        {
            return i + 1;    // add
        }))));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_LambdaVsDelegate1()
        {
            var verifier = CompileAndVerify(@"
class C
{    
    static void F()
    {
        System.Func<int, int> d1 = checked(delegate(int i) { return i + 1; });  // Dev10: add, Roslyn: add_ovf
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}

");
        }

        [Fact]
        public void Lambda_LambdaVsDelegate2()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        System.Func<int, int> d1 = checked(i => { return i + 1; }); // Dev10: add, Roslyn: add_ovf
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_LambdaVsDelegate3()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static void F()
    {
        System.Func<int, int> d1 = checked(i => i + 1); // Dev10: add, Roslyn: add_ovf
    }
}
").VerifyIL("C.<>c.<F>b__0_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_NewDelegate1()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        Id(new System.Func<int, int>(i => i + 1));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add
  IL_0003:  ret
}
");
        }

        [Fact]
        public void Lambda_NewDelegate2()
        {
            var verifier = CompileAndVerify(@"
class C
{
    static System.Func<int, int> Id(System.Func<int, int> x) { return x; }

    static void F()
    {
        Id(checked(new System.Func<int, int>(i => i + 1)));
    }
}
").VerifyIL("C.<>c.<F>b__1_0(int)", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  ldc.i4.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact]
        public void CheckedOption1()
        {
            var source = @"
class C
{
    public static uint ULong_UInt(ulong a)
    {
        return (uint)a;
    }
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithOverflowChecks(true)).VerifyIL("C.ULong_UInt", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4.un
  IL_0002:  ret
}
");
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithOverflowChecks(false)).VerifyIL("C.ULong_UInt", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
");
        }

        [Fact]
        public void CheckedOption2()
        {
            var source = @"
class C
{
    public static uint ULong_UInt(ulong a)
    {
        return unchecked((uint)a);
    }
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithOverflowChecks(true)).VerifyIL("C.ULong_UInt", @"
{
  // Code size        3 (0x3)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  conv.u4
  IL_0002:  ret
}
");
        }

        [Fact]
        public void CheckedUncheckedNesting()
        {
            var source = @"
class C
{
    public static uint ULong_UInt(ulong a)
    {
        uint b = (uint)a;

        unchecked 
        {
            return b + checked((uint)a - unchecked((uint)a));
        }
    }
}
";
            CompileAndVerify(source, options: TestOptions.ReleaseDll.WithOverflowChecks(true)).VerifyIL("C.ULong_UInt", @"
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  conv.ovf.u4.un
  IL_0002:  ldarg.0
  IL_0003:  conv.ovf.u4.un
  IL_0004:  ldarg.0
  IL_0005:  conv.u4
  IL_0006:  sub.ovf.un
  IL_0007:  add
  IL_0008:  ret
}
");
        }

        [Fact]
        public void UncheckedOperatorWithConstantsOfTheSignedIntegralTypes()
        {
            var source = @"
class Test
{
    const int a = unchecked((int)0xFFFFFFFF);
    const int b = unchecked((int)0x80000000);
}
";
            CompileAndVerify(source);
        }

        [Fact]
        public void UncheckedOperatorInCheckedStatement()
        {
            // Overflow checking context with use unchecked operator in checked statement

            var source = @"
class Program
{
    static void Main()
    {
        int r = 0;
        r = int.MaxValue + 1;
        checked
        {
            r = int.MaxValue + 1;
            r = unchecked(int.MaxValue + 1);
        };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): error CS0220: The operation overflows at compile time in checked mode
                //         r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (10,17): error CS0220: The operation overflows at compile time in checked mode
                //             r = int.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [Fact]
        public void ExpressionsInUncheckedStatementAreInExplicitlyUncheckedContext()
        {
            // Expressions which are in unchecked statement are in explicitly unchecked context.

            var source = @"
using System;
class Program
{
    static void Main()
    {
        int s2 = int.MaxValue;
        int r = 0;
        unchecked { r = s2 + 1; };
        if (r != int.MinValue)
        {
            Console.Write(""FAIL"");
        }
        unchecked { r = int.MaxValue + 1; };
        if (r != int.MinValue)
        {
            Console.Write(""FAIL"");
        }
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("Program.Main", @"
{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldc.i4     0x7fffffff
  IL_0005:  ldc.i4.1
  IL_0006:  add
  IL_0007:  ldc.i4     0x80000000
  IL_000c:  beq.s      IL_0018
  IL_000e:  ldstr      ""FAIL""
  IL_0013:  call       ""void System.Console.Write(string)""
  IL_0018:  ldc.i4     0x80000000
  IL_001d:  ldc.i4     0x80000000
  IL_0022:  beq.s      IL_002e
  IL_0024:  ldstr      ""FAIL""
  IL_0029:  call       ""void System.Console.Write(string)""
  IL_002e:  ret
}
");
        }

        [Fact]
        public void CheckOnUnaryOperator()
        {
            var source = @"
class Program
{
    static void Main()
    {
        long test1 = long.MinValue;
        long test2 = 0;
        try
        {
            checked
            {
                test2 = -test1;
            }
        }
        catch (System.OverflowException)
        {
            System.Console.Write(test2);
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "0");
        }

        [Fact]
        public void Test_024_16BitSignedInteger()
        {
            var source = @"
using System;
public class MyClass
{
    public static void Main()
    {
        Int16 f = 0;
        var r = checked(f += 32000);
        Console.Write(r);
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "32000");
        }

        [Fact]
        public void AnonymousFunctionExpressionInUncheckedOperator()
        {
            // Overflow checking context with use anonymous function expression in unchecked operator

            var source = @"
class Program
{
    delegate int D1(int i);
    static void Main()
    {
        D1 d1;
        int r1 = 0;
        d1 = unchecked(delegate (int i) { r1 = int.MaxValue + 1; r1++; return checked(int.MaxValue + 1); });
        d1 = unchecked(i => int.MaxValue + 1 + checked(0 + 0));
        d1 = unchecked(i => 0 + 0 + checked(int.MaxValue + 1));
        d1 = unchecked(d1 = delegate (int i) { r1 = int.MaxValue + 1; return checked(int.MaxValue + 1); });
        d1 = unchecked(d1 = i => int.MaxValue + 1 + checked(0 + 0));
        d1 = unchecked(d1 = i => 0 + 0 + checked(int.MaxValue + 1));
        d1 = unchecked(new D1(delegate (int i) { r1 = int.MaxValue + 1; return checked(int.MaxValue + 1); }));
        d1 = unchecked(new D1(i => int.MaxValue + 1 + checked(0 + 0)));
        d1 = unchecked(new D1(i => 0 + 0 + checked(int.MaxValue + 1)));
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,81): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(delegate (int i) { r1 = int.MaxValue + 1; return checked(int.MaxValue + 1); });
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (11,45): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(i => 0 + 0 + checked(int.MaxValue + 1));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (12,86): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(d1 = delegate (int i) { r1 = int.MaxValue + 1; return checked(int.MaxValue + 1); });
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (14,50): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(d1 = i => 0 + 0 + checked(int.MaxValue + 1));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (15,88): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(new D1(delegate (int i) { r1 = int.MaxValue + 1; return checked(int.MaxValue + 1); }));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"),
                // (17,52): error CS0220: The operation overflows at compile time in checked mode
                //         d1 = unchecked(new D1(i => 0 + 0 + checked(int.MaxValue + 1)));
                Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1"));
        }

        [WorkItem(648109, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/648109")]
        [Fact]
        public void CheckedExpressionWithDecimal()
        {
            var source = @"
class M
{
    void F()
    {   
        var r = checked(decimal.MaxValue + 1);
        
        // error should be reported regardless checked or not
        // decimal math has not concept of ""checked""
        var r1 = decimal.MaxValue + 1;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,25): error CS0463: Evaluation of the decimal constant expression failed
                //         var r = checked(decimal.MaxValue + 1);
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MaxValue + 1").WithLocation(6, 25),
                // (10,18): error CS0463: Evaluation of the decimal constant expression failed
                //         var r1 = decimal.MaxValue + 1;
                Diagnostic(ErrorCode.ERR_DecConstError, "decimal.MaxValue + 1").WithLocation(10, 18)
                );
        }

        [WorkItem(543894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543894"), WorkItem(543924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543924")]
        [Fact]
        public void CheckedOperatorOnEnumOverflow()
        {
            var source = @"
using System;

class Test
{
    enum Esb { max = int.MaxValue }
    static void Main()
    {
        Esb e = Esb.max;
        try
        {
            var i = checked(e++);
            Console.WriteLine(""FAIL"");
        }
        catch (OverflowException)
        {
            Console.WriteLine(""PASS"");
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "PASS");
        }

        [WorkItem(529263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529263")]
        [Fact]
        public void CheckedOperatorOnLambdaExpr()
        {
            var source = @"
using System;

class Program
{
    static void Main()
    {
        int max = int.MaxValue;

        try
        {
            Func<int> f1 = checked(() => max + 1);
            Console.WriteLine(f1());
            Console.WriteLine(""FAIL"");
        }
        catch (OverflowException)
        {
            Console.WriteLine(""PASS"");
        }
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "PASS");
        }

        [Fact, WorkItem(543981, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543981")]
        public void CheckedOperatorOnUnaryExpression()
        {
            var source = @"
using System;

class Program
{
    static int Main()
    {
        int result = 0;

        int normi = 10;
        if (checked(-normi) != -10)
        {
            result += 1;
        }

        int maxi = int.MaxValue;
        if (checked(-maxi) != int.MinValue + 1)
        {
            result += 1;
        }

        try
        {
            int mini = int.MinValue;
            var x = checked(-mini);
            result += 1;
        }
        catch (OverflowException)
        {
            Console.Write(""OV-"");
        }

        Console.WriteLine(result);
        return result;
    }
}
";

            CompileAndVerify(
                source,
                expectedOutput: "OV-0");
        }

        [Fact, WorkItem(543983, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543983")]
        public void CheckedStatementWithCompoundAssignment()
        {
            var source = @"
using System;

public class MyClass
{
    public static void Main()
    {
        short goo = 0;
        try
        {
            for (int i = 0; i < 2; i++)
            {
                checked { goo += 32000; }
                Console.Write(goo);
            }
        }
        catch (OverflowException)
        {
            Console.WriteLine(""OV"");
        }
    }
}
";
            CompileAndVerify(
                source,
                expectedOutput: "32000OV");
        }

        [Fact, WorkItem(546872, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546872")]
        public void CheckPostIncrementOnBaseProtectedClassMember()
        {
            var source = @"
using System;
class Base1
{
    protected int memberee=0;
    protected int xyz {
        get;
        set;
    }
    protected int this[int number]
    {
        get { return 0; }
        set { }
    }
}

class Derived2 : Base1
{
    public void inc()
    {
        base.memberee++;
        base.xyz++;
        base[0]++;
    }
}
";
            var verifier = CompileAndVerify(source);

            verifier.VerifyIL("Derived2.inc", @"
{
  // Code size       49 (0x31)
  .maxstack  4
  .locals init (int V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldfld      ""int Base1.memberee""
  IL_0007:  ldc.i4.1
  IL_0008:  add
  IL_0009:  stfld      ""int Base1.memberee""
  IL_000e:  ldarg.0
  IL_000f:  call       ""int Base1.xyz.get""
  IL_0014:  stloc.0
  IL_0015:  ldarg.0
  IL_0016:  ldloc.0
  IL_0017:  ldc.i4.1
  IL_0018:  add
  IL_0019:  call       ""void Base1.xyz.set""
  IL_001e:  ldarg.0
  IL_001f:  ldc.i4.0
  IL_0020:  call       ""int Base1.this[int].get""
  IL_0025:  stloc.0
  IL_0026:  ldarg.0
  IL_0027:  ldc.i4.0
  IL_0028:  ldloc.0
  IL_0029:  ldc.i4.1
  IL_002a:  add
  IL_002b:  call       ""void Base1.this[int].set""
  IL_0030:  ret
}
");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        public void CheckedConversionsInExpressionTrees_Implicit()
        {
            // char
            CheckedConversionInExpressionTree_Implicit("char", "char", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("char", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("char", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("char", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("char", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("char", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("char", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("char", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("char", "double", ConvertMethod.Convert);

            // sbyte
            CheckedConversionInExpressionTree_Implicit("sbyte", "sbyte", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("sbyte", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("sbyte", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("sbyte", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("sbyte", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("sbyte", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("sbyte", "double", ConvertMethod.Convert);

            // byte
            CheckedConversionInExpressionTree_Implicit("byte", "byte", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("byte", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("byte", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("byte", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("byte", "double", ConvertMethod.Convert);

            // short
            CheckedConversionInExpressionTree_Implicit("short", "short", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("short", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("short", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("short", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("short", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("short", "double", ConvertMethod.Convert);

            // ushort
            CheckedConversionInExpressionTree_Implicit("ushort", "ushort", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("ushort", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("ushort", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("ushort", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("ushort", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("ushort", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("ushort", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("ushort", "double", ConvertMethod.Convert);

            // int
            CheckedConversionInExpressionTree_Implicit("int", "int", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("int", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("int", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("int", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("int", "double", ConvertMethod.Convert);

            // uint
            CheckedConversionInExpressionTree_Implicit("uint", "uint", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("uint", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("uint", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Implicit("uint", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("uint", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("uint", "double", ConvertMethod.Convert);

            // long
            CheckedConversionInExpressionTree_Implicit("long", "long", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("long", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("long", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("long", "double", ConvertMethod.Convert);

            // ulong
            CheckedConversionInExpressionTree_Implicit("ulong", "ulong", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("ulong", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("ulong", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("ulong", "double", ConvertMethod.Convert);

            // decimal
            CheckedConversionInExpressionTree_Implicit("decimal", "decimal", ConvertMethod.None);

            // float
            CheckedConversionInExpressionTree_Implicit("float", "float", ConvertMethod.None);
            CheckedConversionInExpressionTree_Implicit("float", "double", ConvertMethod.Convert);

            // double
            CheckedConversionInExpressionTree_Implicit("double", "double", ConvertMethod.None);

            // object
            CheckedConversionInExpressionTree_Implicit("int", "object", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Implicit("string", "object", ConvertMethod.None);

            // Nullable<>
            CheckedConversionInExpressionTree_Implicit("int", "int?", "arg => F(Convert(arg))");
            CheckedConversionInExpressionTree_Implicit("int", "long?", "arg => F(ConvertChecked(ConvertChecked(arg)))");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        [WorkItem(18459, "https://github.com/dotnet/roslyn/issues/18459")]
        public void CheckedConversionsInExpressionTrees_ImplicitTuple()
        {
            CheckedConversionInExpressionTree_Implicit("(int, int)", "(int, int)?", ConvertMethod.Convert);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        public void CheckedConversionsInExpressionTrees_Explicit()
        {
            // char
            CheckedConversionInExpressionTree_Explicit("char", "char", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("char", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("char", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("char", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("char", "double", ConvertMethod.Convert);

            // sbyte
            CheckedConversionInExpressionTree_Explicit("sbyte", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "sbyte", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("sbyte", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("sbyte", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("sbyte", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("sbyte", "double", ConvertMethod.Convert);

            // byte
            CheckedConversionInExpressionTree_Explicit("byte", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "byte", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("byte", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("byte", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("byte", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("byte", "double", ConvertMethod.Convert);

            // short
            CheckedConversionInExpressionTree_Explicit("short", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "short", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("short", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("short", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("short", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("short", "double", ConvertMethod.Convert);

            // ushort
            CheckedConversionInExpressionTree_Explicit("ushort", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "ushort", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ushort", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ushort", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ushort", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ushort", "double", ConvertMethod.Convert);

            // int
            CheckedConversionInExpressionTree_Explicit("int", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "int", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("int", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("int", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("int", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("int", "double", ConvertMethod.Convert);

            // uint
            CheckedConversionInExpressionTree_Explicit("uint", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "uint", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("uint", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("uint", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("uint", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("uint", "double", ConvertMethod.Convert);

            // long
            CheckedConversionInExpressionTree_Explicit("long", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "long", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("long", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("long", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("long", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("long", "double", ConvertMethod.Convert);

            // ulong
            CheckedConversionInExpressionTree_Explicit("ulong", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("ulong", "ulong", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ulong", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ulong", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("ulong", "double", ConvertMethod.Convert);

            // decimal
            CheckedConversionInExpressionTree_Explicit("decimal", "char", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "sbyte", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "byte", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "short", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "ushort", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "int", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "uint", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "long", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "ulong", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("decimal", "double", ConvertMethod.Convert);

            // float
            CheckedConversionInExpressionTree_Explicit("float", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("float", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("float", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("float", "double", ConvertMethod.Convert);

            // double
            CheckedConversionInExpressionTree_Explicit("double", "char", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "sbyte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "byte", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "short", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "ushort", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "int", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "uint", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "long", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "ulong", ConvertMethod.ConvertChecked);
            CheckedConversionInExpressionTree_Explicit("double", "decimal", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("double", "float", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("double", "double", ConvertMethod.Convert);

            // enum
            CheckedConversionInExpressionTree_Explicit("E", "int", ConvertMethod.ConvertChecked, "enum E { }");
            CheckedConversionInExpressionTree_Explicit("int", "E", ConvertMethod.ConvertChecked, "enum E { }");
            CheckedConversionInExpressionTree_Explicit("E", "int", ConvertMethod.ConvertChecked, "enum E : short { }");
            CheckedConversionInExpressionTree_Explicit("int", "E", ConvertMethod.ConvertChecked, "enum E : short { }");

            // object
            CheckedConversionInExpressionTree_Explicit("int", "object", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("object", "int", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("string", "object", ConvertMethod.Convert);
            CheckedConversionInExpressionTree_Explicit("object", "string", ConvertMethod.Convert);

            // Nullable<>
            CheckedConversionInExpressionTree_Explicit("int", "byte?", "arg => ConvertChecked(arg)");
            CheckedConversionInExpressionTree_Explicit("int", "int?", "arg => ConvertChecked(arg)");
            CheckedConversionInExpressionTree_Explicit("int", "long?", "arg => ConvertChecked(ConvertChecked(arg))");
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30160")]
        public void CheckedConversionsInExpressionTrees_ExplicitTuple()
        {
            CheckedConversionInExpressionTree_Explicit("(int, int)", "(int, int)?", ConvertMethod.Convert);
        }

        private enum ConvertMethod
        {
            None,
            Convert,
            ConvertChecked,
        }

        private void CheckedConversionInExpressionTree_Implicit(string fromType, string toType, ConvertMethod expectedMethod, string additionalTypes = "")
        {
            var source = CheckedConversionInExpressionTree_ImplicitSource(fromType, toType, additionalTypes);
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            string expectedOutput;
            switch (expectedMethod)
            {
                default:
                    expectedOutput = "arg => F(arg)";
                    break;
                case ConvertMethod.Convert:
                    expectedOutput = "arg => F(Convert(arg))";
                    break;
                case ConvertMethod.ConvertChecked:
                    expectedOutput = "arg => F(ConvertChecked(arg))";
                    break;
            }
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            // Since Expression.ConvertChecked can generate a Checked result
            // (rather than ConvertChecked), verify the correct method was called.
            VerifyConversionInExpressionTreeIL(verifier.TestData.GetMethodData("C.Main").GetMethodIL(), expectedMethod);
        }

        private void CheckedConversionInExpressionTree_Implicit(string fromType, string toType, string expectedOutput)
        {
            var source = CheckedConversionInExpressionTree_ImplicitSource(fromType, toType, additionalTypes: "");
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        private static string CheckedConversionInExpressionTree_ImplicitSource(string fromType, string toType, string additionalTypes)
        {
            return
$@"using System;
using System.Linq.Expressions;
{additionalTypes}
class C
{{
    static {toType} F({toType} arg) => arg;
    static void Main()
    {{
        Expression<Func<{fromType}, {toType}>> e = arg => checked(F(arg));
        Console.WriteLine(e);
    }}
}}";
        }

        private void CheckedConversionInExpressionTree_Explicit(string fromType, string toType, ConvertMethod expectedMethod, string additionalTypes = "")
        {
            var source = CheckedConversionInExpressionTree_ExplicitSource(fromType, toType, additionalTypes);
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef });
            string expectedOutput;
            switch (expectedMethod)
            {
                default:
                    expectedOutput = "arg => arg";
                    break;
                case ConvertMethod.Convert:
                    expectedOutput = "arg => Convert(arg)";
                    break;
                case ConvertMethod.ConvertChecked:
                    expectedOutput = "arg => ConvertChecked(arg)";
                    break;
            }
            var verifier = CompileAndVerify(compilation, expectedOutput: expectedOutput);
            // Since Expression.ConvertChecked can generate a Checked result
            // (rather than ConvertChecked), verify the correct method was called.
            VerifyConversionInExpressionTreeIL(verifier.TestData.GetMethodData("C.Main").GetMethodIL(), expectedMethod);
        }

        private void CheckedConversionInExpressionTree_Explicit(string fromType, string toType, string expectedOutput)
        {
            var source = CheckedConversionInExpressionTree_ExplicitSource(fromType, toType, additionalTypes: "");
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: expectedOutput);
        }

        private static string CheckedConversionInExpressionTree_ExplicitSource(string fromType, string toType, string additionalTypes)
        {
            return
$@"using System;
using System.Linq.Expressions;
{additionalTypes}
class C
{{
    static void Main()
    {{
        Expression<Func<{fromType}, {toType}>> e = arg => checked(({toType})arg);
        Console.WriteLine(e);
    }}
}}";
        }

        private static void VerifyConversionInExpressionTreeIL(string actualIL, ConvertMethod expectedMethod)
        {
            Assert.Equal(
                actualIL.Contains($"System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression, "),
                expectedMethod == ConvertMethod.Convert);
            Assert.Equal(
                actualIL.Contains($"System.Linq.Expressions.Expression.ConvertChecked(System.Linq.Expressions.Expression, "),
                expectedMethod == ConvertMethod.ConvertChecked);
        }

        [Fact, WorkItem(61843, "https://github.com/dotnet/roslyn/issues/61843")]
        public void SwitchExpressionInCheckedExpression()
        {
            var source = """
using System;
int x = int.MaxValue;
int y = int.MaxValue;

try
{
    C.AddInSwitchExpression(x, y, true);
}
catch (OverflowException)
{
    Console.Write("RAN1 ");
}

try
{
    C.AddInSwitchExpression2(x, y, true);
}
catch (OverflowException)
{
    Console.Write("RAN2 ");
}

try
{
    C.Add(x, y);
}
catch (OverflowException)
{
    Console.Write("RAN3 ");
}

public static class C
{
    public static int AddInSwitchExpression(int x, int y, bool condition) =>
        checked (
            condition switch
            {
                true => x + y,
                _ => throw null
            }
        );

    public static int AddInSwitchExpression2(int x, int y, bool condition)
    {
        checked
        {
            return condition switch
            {
                true => x + y,
                _ => throw null
            };
        }
    }

    public static int Add(int x, int y) => checked(x + y);
}
""";

            var comp = CreateCompilation(source);
            var verifier = CompileAndVerify(comp, expectedOutput: "RAN1 RAN2 RAN3");

            verifier.VerifyIL("C.AddInSwitchExpression", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.2
  IL_0001:  brfalse.s  IL_0009
  IL_0003:  ldarg.0
  IL_0004:  ldarg.1
  IL_0005:  add.ovf
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_000b
  IL_0009:  ldnull
  IL_000a:  throw
  IL_000b:  ldloc.0
  IL_000c:  ret
}
");

            verifier.VerifyIL("C.AddInSwitchExpression2", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  .locals init (int V_0)
  IL_0000:  ldarg.2
  IL_0001:  brfalse.s  IL_0009
  IL_0003:  ldarg.0
  IL_0004:  ldarg.1
  IL_0005:  add.ovf
  IL_0006:  stloc.0
  IL_0007:  br.s       IL_000b
  IL_0009:  ldnull
  IL_000a:  throw
  IL_000b:  ldloc.0
  IL_000c:  ret
}
");

            verifier.VerifyIL("C.Add", @"
{
  // Code size        4 (0x4)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  add.ovf
  IL_0003:  ret
}
");
        }

        [Fact, WorkItem(61843, "https://github.com/dotnet/roslyn/issues/61843")]
        public void SpeculationInCheckedExpression()
        {
            var source = """
class C
{
    int M(int x, int y)
        => checked(x);
}
""";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var xNode = tree.GetRoot().DescendantNodes().OfType<CheckedExpressionSyntax>().Single().Expression;
            Assert.Equal("x", xNode.ToString());

            var nodeToSpeculate = SyntaxFactory.ParseExpression("x + y");
            Assert.Equal("System.Int32 System.Int32.op_CheckedAddition(System.Int32 left, System.Int32 right)",
                model.GetSpeculativeSymbolInfo(xNode.Position, nodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());

            var checkedNode = tree.GetRoot().DescendantNodes().OfType<CheckedExpressionSyntax>().Single();
            Assert.Equal("checked(x)", checkedNode.ToString());

            Assert.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)",
                model.GetSpeculativeSymbolInfo(checkedNode.Position + 2, nodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(61843, "https://github.com/dotnet/roslyn/issues/61843")]
        public void SpeculationInUncheckedExpression()
        {
            var source = """
class C
{
    int M(int x, int y)
    {
        checked
        {
            return unchecked(x);
        }
    }
}
""";
            var comp = CreateCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var xNode = tree.GetRoot().DescendantNodes().OfType<CheckedExpressionSyntax>().Single().Expression;
            Assert.Equal("x", xNode.ToString());

            var nodeToSpeculate = SyntaxFactory.ParseExpression("x + y");
            Assert.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)",
                model.GetSpeculativeSymbolInfo(xNode.Position, nodeToSpeculate, SpeculativeBindingOption.BindAsExpression).Symbol.ToTestDisplayString());
        }
    }
}
