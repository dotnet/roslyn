﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.RefLocalsReturns)]
    public class RefLocalTests : CompilingTestBase
    {
        [Fact]
        public void RefAssignArrayAccess()
        {
            var text = @"
class Program
{
    static void M()
    {
        ref int rl = ref (new int[1])[0];
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldc.i4.1
  IL_0002:  newarr     ""int""
  IL_0007:  ldc.i4.0
  IL_0008:  ldelema    ""int""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignRefParameter()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        ref int rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        4 (0x4)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ret
}");
        }

        [Fact]
        public void RefAssignOutParameter()
        {
            var text = @"
class Program
{
    static void M(out int i)
    {
        i = 0;
        ref int rl = ref i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(out int)", @"
{
  // Code size        7 (0x7)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  stind.i4
  IL_0004:  ldarg.0
  IL_0005:  stloc.0
  IL_0006:  ret
}");
        }

        [Fact]
        public void RefAssignRefLocal()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        ref int local = ref i;
        ref int rl = ref local;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(ref int)", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  .locals init (int& V_0, //local
                int& V_1) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  stloc.1
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefAssignStaticProperty()
        {
            var text = @"
class Program
{
    static int field = 0;
    static ref int P { get { return ref field; } }

    static void M()
    {
        ref int rl = ref P;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  stloc.0
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceProperty()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int P { get { return ref field; } }

    void M()
    {
        ref int rl = ref P;
    }

    void M1()
    {
        ref int rl = ref new Program().P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.P.get""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceProperty()
        {
            var text = @"
struct Program
{
    public ref int P { get { return ref (new int[1])[0]; } }

    void M()
    {
        ref int rl = ref P;
    }

    void M1(ref Program program)
    {
        ref int rl = ref program.P;
    }
}

struct Program2
{
    Program program;


    Program2(Program program)
    {
        this.program = program;
    }

    void M()
    {
        ref int rl = ref program.P;
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1(ref Program)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  call       ""ref int Program.P.get""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceProperty()
        {
            var text = @"
interface I
{
    ref int P { get; }
}

class Program<T>
    where T : I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t.P;
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t.P;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t.P;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.P.get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.P.get""
  IL_0012:  stloc.0
  IL_0013:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceIndexer()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int this[int i] { get { return ref field; } }

    void M()
    {
        ref int rl = ref this[0];
    }

    void M1()
    {
        ref int rl = ref new Program()[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       14 (0xe)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref int Program.this[int].get""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceIndexer()
        {
            var text = @"
struct Program
{
    public ref int this[int i] { get { return ref (new int[1])[0]; } }

    void M()
    {
        ref int rl = ref this[0];
    }
}

struct Program2
{
    Program program;

    Program2(Program program)
    {
        this.program = program;
    }

    void M()
    {
        ref int rl = ref program[0];
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       10 (0xa)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldc.i4.0
  IL_0003:  call       ""ref int Program.this[int].get""
  IL_0008:  stloc.0
  IL_0009:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  ldc.i4.0
  IL_0008:  call       ""ref int Program.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceIndexer()
        {
            var text = @"
interface I
{
    ref int this[int i] { get; }
}

class Program<T>
    where T : I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t[0];
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t[0];
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    void M()
    {
        ref int rl = ref t[0];
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  ldc.i4.0
  IL_0008:  callvirt   ""ref int I.this[int].get""
  IL_000d:  stloc.0
  IL_000e:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       21 (0x15)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  ldc.i4.0
  IL_0008:  constrained. ""T""
  IL_000e:  callvirt   ""ref int I.this[int].get""
  IL_0013:  stloc.0
  IL_0014:  ret
}");
        }

        [Fact]
        public void RefAssignStaticFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    static event D d;

    static void M()
    {
        ref D rl = ref d;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""D Program.d""
  IL_0006:  stloc.0
  IL_0007:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d;

    void M()
    {
        ref D rl = ref d;
    }

    void M1()
    {
        ref D rl = ref new Program().d;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""D Program.d""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (D& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""D Program.d""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStaticField()
        {
            var text = @"
class Program
{
    static int i = 0;

    static void M()
    {
        ref int rl = ref i;
        rl = i;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M()", @"
{
  // Code size       15 (0xf)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.i""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldsfld     ""int Program.i""
  IL_000d:  stind.i4
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceField()
        {
            var text = @"
class Program
{
    int i = 0;

    void M()
    {
        ref int rl = ref i;
    }

    void M1()
    {
        ref int rl = ref new Program().i;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program.M()", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""int Program.i""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldflda     ""int Program.i""
  IL_000b:  stloc.0
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceField()
        {
            var text = @"
struct Program
{
    public int i;
}

class Program2
{
    Program program = default(Program);

    void M(ref Program program)
    {
        ref int rl = ref program.i;
        rl = program.i;
    }

    void M()
    {
        ref int rl = ref program.i;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll);
            comp.VerifyIL("Program2.M(ref Program)", @"
{
  // Code size       17 (0x11)
  .maxstack  2
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  ldflda     ""int Program.i""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  ldarg.1
  IL_000a:  ldfld      ""int Program.i""
  IL_000f:  stind.i4
  IL_0010:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldflda     ""int Program.i""
  IL_000c:  stloc.0
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefAssignStaticCallWithoutArguments()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  call       ""ref int Program.M()""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  stloc.1
  IL_0009:  br.s       IL_000b
  IL_000b:  ldloc.1
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceCallWithoutArguments()
        {
            var text = @"
class Program
{
    ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }

    ref int M1()
    {
        ref int rl = ref new Program().M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ret
}");
            comp.VerifyIL("Program.M1()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  call       ""ref int Program.M()""
  IL_000b:  stloc.0
  IL_000c:  ldloc.0
  IL_000d:  stloc.1
  IL_000e:  br.s       IL_0010
  IL_0010:  ldloc.1
  IL_0011:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceCallWithoutArguments()
        {
            var text = @"
struct Program
{
    public ref int M()
    {
        ref int rl = ref M();
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M()", @"
{
  // Code size       14 (0xe)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  call       ""ref int Program.M()""
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  br.s       IL_000c
  IL_000c:  ldloc.1
  IL_000d:  ret
}");
            comp.VerifyIL("Program2.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
            comp.VerifyIL("Program3.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  call       ""ref int Program.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceCallWithoutArguments()
        {
            var text = @"
interface I
{
    ref int M();
}

class Program<T>
    where T : I
{
    T t = default(T);

    ref int M()
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M()
    {
        ref int rl = ref t.M();
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program<T>.M()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.1
  IL_0018:  ret
}");
            comp.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       19 (0x13)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  callvirt   ""ref int I.M()""
  IL_000c:  stloc.0
  IL_000d:  ldloc.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_0011
  IL_0011:  ldloc.1
  IL_0012:  ret
}");
            comp.VerifyIL("Program3<T>.M()", @"
{
  // Code size       25 (0x19)
  .maxstack  1
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.M()""
  IL_0012:  stloc.0
  IL_0013:  ldloc.0
  IL_0014:  stloc.1
  IL_0015:  br.s       IL_0017
  IL_0017:  ldloc.1
  IL_0018:  ret
}");
        }

        [Fact]
        public void RefAssignStaticCallWithArguments()
        {
            var text = @"
class Program
{
    static ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       16 (0x10)
  .maxstack  3
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0009:  stloc.0
  IL_000a:  ldloc.0
  IL_000b:  stloc.1
  IL_000c:  br.s       IL_000e
  IL_000e:  ldloc.1
  IL_000f:  ret
}");
        }

        [Fact]
        public void RefAssignClassInstanceCallWithArguments()
        {
            var text = @"
class Program
{
    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }

    ref int M1(ref int i, ref int j, object o)
    {
        ref int rl = ref new Program().M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
            comp.VerifyIL("Program.M1(ref int, ref int, object)", @"
{
  // Code size       21 (0x15)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  newobj     ""Program..ctor()""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000e:  stloc.0
  IL_000f:  ldloc.0
  IL_0010:  stloc.1
  IL_0011:  br.s       IL_0013
  IL_0013:  ldloc.1
  IL_0014:  ret
}");
        }

        [Fact]
        public void RefAssignStructInstanceCallWithArguments()
        {
            var text = @"
struct Program
{
    public ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
            comp.VerifyIL("Program2.M(ref int, ref int, object)", @"
{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program2.program""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.1
  IL_0015:  ret
}");
            comp.VerifyIL("Program3.M(ref int, ref int, object)", @"
{
  // Code size       22 (0x16)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""Program Program3.program""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000f:  stloc.0
  IL_0010:  ldloc.0
  IL_0011:  stloc.1
  IL_0012:  br.s       IL_0014
  IL_0014:  ldloc.1
  IL_0015:  ret
}");
        }

        [Fact]
        public void RefAssignConstrainedInstanceCallWithArguments()
        {
            var text = @"
interface I
{
    ref int M(ref int i, ref int j, object o);
}

class Program<T>
    where T : I
{
    T t = default(T);

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            var comp = CompileAndVerify(text, options: TestOptions.DebugDll, verify: false);
            comp.VerifyIL("Program<T>.M(ref int, ref int, object)", @"
{
  // Code size       28 (0x1c)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program<T>.t""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_001a
  IL_001a:  ldloc.1
  IL_001b:  ret
}");
            comp.VerifyIL("Program2<T>.M(T, ref int, ref int, object)", @"
{
  // Code size       23 (0x17)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.1
  IL_0002:  box        ""T""
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  ldarg.s    V_4
  IL_000b:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0010:  stloc.0
  IL_0011:  ldloc.0
  IL_0012:  stloc.1
  IL_0013:  br.s       IL_0015
  IL_0015:  ldloc.1
  IL_0016:  ret
}");
            comp.VerifyIL("Program3<T>.M(ref int, ref int, object)", @"
{
  // Code size       28 (0x1c)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program3<T>.t""
  IL_0007:  ldarg.1
  IL_0008:  ldarg.2
  IL_0009:  ldarg.3
  IL_000a:  constrained. ""T""
  IL_0010:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0015:  stloc.0
  IL_0016:  ldloc.0
  IL_0017:  stloc.1
  IL_0018:  br.s       IL_001a
  IL_001a:  ldloc.1
  IL_001b:  ret
}");
        }

        [Fact]
        public void RefAssignDelegateInvocationWithNoArguments()
        {
            var text = @"
delegate ref int D();

class Program
{
    static void M(D d)
    {
        ref int rl = ref d();
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll).VerifyIL("Program.M(D)", @"
{
  // Code size        9 (0x9)
  .maxstack  1
  .locals init (int& V_0) //rl
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  callvirt   ""ref int D.Invoke()""
  IL_0007:  stloc.0
  IL_0008:  ret
}");
        }

        [Fact]
        public void RefAssignDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, ref int i, ref int j, object o)
    {
        ref int rl = ref d(ref i, ref j, o);
        return ref rl;
    }
}
";

            CompileAndVerify(text, options: TestOptions.DebugDll, verify: false).VerifyIL("Program.M(D, ref int, ref int, object)", @"
{
  // Code size       17 (0x11)
  .maxstack  4
  .locals init (int& V_0, //rl
                int& V_1)
  IL_0000:  nop
  IL_0001:  ldarg.0
  IL_0002:  ldarg.1
  IL_0003:  ldarg.2
  IL_0004:  ldarg.3
  IL_0005:  callvirt   ""ref int D.Invoke(ref int, ref int, object)""
  IL_000a:  stloc.0
  IL_000b:  ldloc.0
  IL_000c:  stloc.1
  IL_000d:  br.s       IL_000f
  IL_000f:  ldloc.1
  IL_0010:  ret
}");
        }

        [Fact]
        public void RefLocalsAreVariables()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void M(ref int i)
    {
    }

    static void N(out int i)
    {
        i = 0;
    }

    static unsafe void Main()
    {
        ref int rl = ref field;
        rl = 0;
        rl += 1;
        rl++;
        M(ref rl);
        N(out rl);
        fixed (int* i = &rl) { }
        var tr = __makeref(rl);
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size       54 (0x36)
  .maxstack  3
  .locals init (int& V_0, //rl
                System.TypedReference V_1, //tr
                int* V_2, //i
                pinned int& V_3)
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldc.i4.0
  IL_0009:  stind.i4
  IL_000a:  ldloc.0
  IL_000b:  ldloc.0
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.1
  IL_000e:  add
  IL_000f:  stind.i4
  IL_0010:  ldloc.0
  IL_0011:  ldloc.0
  IL_0012:  ldind.i4
  IL_0013:  ldc.i4.1
  IL_0014:  add
  IL_0015:  stind.i4
  IL_0016:  ldloc.0
  IL_0017:  call       ""void Program.M(ref int)""
  IL_001c:  nop
  IL_001d:  ldloc.0
  IL_001e:  call       ""void Program.N(out int)""
  IL_0023:  nop
  IL_0024:  ldloc.0
  IL_0025:  stloc.3
  IL_0026:  ldloc.3
  IL_0027:  conv.u
  IL_0028:  stloc.2
  IL_0029:  nop
  IL_002a:  nop
  IL_002b:  ldc.i4.0
  IL_002c:  conv.u
  IL_002d:  stloc.3
  IL_002e:  ldloc.0
  IL_002f:  mkrefany   ""int""
  IL_0034:  stloc.1
  IL_0035:  ret
}");
        }

        [Fact]
        private void RefLocalsAreValues()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void N(int i)
    {
    }

    static unsafe int Main()
    {
        ref int rl = ref field;
        var @int = rl + 0;
        var @string = rl.ToString();
        var @long = (long)rl;
        N(rl);
        return unchecked((int)((long)@int + @long));
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeDebugDll).VerifyIL("Program.Main()", @"
{
  // Code size       41 (0x29)
  .maxstack  2
  .locals init (int& V_0, //rl
                int V_1, //int
                string V_2, //string
                long V_3, //long
                int V_4)
  IL_0000:  nop
  IL_0001:  ldsflda    ""int Program.field""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldind.i4
  IL_0009:  stloc.1
  IL_000a:  ldloc.0
  IL_000b:  call       ""string int.ToString()""
  IL_0010:  stloc.2
  IL_0011:  ldloc.0
  IL_0012:  ldind.i4
  IL_0013:  conv.i8
  IL_0014:  stloc.3
  IL_0015:  ldloc.0
  IL_0016:  ldind.i4
  IL_0017:  call       ""void Program.N(int)""
  IL_001c:  nop
  IL_001d:  ldloc.1
  IL_001e:  conv.i8
  IL_001f:  ldloc.3
  IL_0020:  add
  IL_0021:  conv.i4
  IL_0022:  stloc.s    V_4
  IL_0024:  br.s       IL_0026
  IL_0026:  ldloc.s    V_4
  IL_0028:  ret
}
");
        }

        [Fact]
        public void RefLocal_CSharp6()
        {
            var text = @"
class Program
{
    static void M()
    {
        ref int rl = ref (new int[1])[0];
    }
}
";
            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (6,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int rl = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7.0").WithLocation(6, 9),
                // (6,22): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7.0 or greater.
                //         ref int rl = ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7.0").WithLocation(6, 22)
                );
        }

        [Fact]
        public void RefVarSemanticModel()
        {
            var text = @"
class Program
{
    static void M()
    {
        int i = 0;
        ref var x = ref i;
    }
}
";
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("System.Int32 x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refVar = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refVar.Type;
            Assert.Equal("System.Int32", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(type));

            Assert.Null(model.GetSymbolInfo(refVar).Symbol);
            Assert.Null(model.GetTypeInfo(refVar).Type);
        }

        [Fact]
        public void RefAliasVarSemanticModel()
        {
            var text = @"
using var = C;
class C
{
    static void M()
    {
        C i = null;
        ref var x = ref i;
    }
}
";
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("C x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refVar = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refVar.Type;
            Assert.Equal("C", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("C", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            var alias = model.GetAliasInfo(type);
            Assert.Equal(SymbolKind.NamedType, alias.Target.Kind);
            Assert.Equal("C", alias.Target.ToDisplayString());

            Assert.Null(model.GetSymbolInfo(refVar).Symbol);
            Assert.Null(model.GetTypeInfo(refVar).Type);
        }

        [Fact]
        public void RefIntSemanticModel()
        {
            var text = @"
class Program
{
    static void M()
    {
        int i = 0;
        ref System.Int32 x = ref i;
    }
}
";
            var comp = CreateStandardCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var xDecl = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ElementAt(1);
            Assert.Equal("System.Int32 x", model.GetDeclaredSymbol(xDecl).ToTestDisplayString());

            var refInt = tree.GetRoot().DescendantNodes().OfType<RefTypeSyntax>().Single();
            var type = refInt.Type;
            Assert.Equal("System.Int32", model.GetTypeInfo(type).Type.ToTestDisplayString());
            Assert.Equal("System.Int32", model.GetSymbolInfo(type).Symbol.ToTestDisplayString());
            Assert.Null(model.GetAliasInfo(type));

            Assert.Null(model.GetSymbolInfo(refInt).Symbol);
            Assert.Null(model.GetTypeInfo(refInt).Type);
        }

        [WorkItem(17395, "https://github.com/dotnet/roslyn/issues/17453")]
        [Fact]
        public void Regression17395()
        {
            var source = @"
using System;

public class C 
{            
    public void F()
    {
        ref int[] a = ref {1,2,3};
        Console.WriteLine(a[0]);

        ref var b = ref {4, 5, 6};
        Console.WriteLine(b[0]);

        ref object c = ref {7,8,9};
        Console.WriteLine(c);
    }        
}
";

            var c = CreateStandardCompilation(source);

            c.VerifyDiagnostics(
                // (8,27): error CS1510: A ref or out value must be an assignable variable
                //         ref int[] a = ref {1,2,3};
                Diagnostic(ErrorCode.ERR_RefLvalueExpected, "{1,2,3}"),
                // (11,17): error CS0820: Cannot initialize an implicitly-typed variable with an array initializer
                //         ref var b = ref {4, 5, 6};
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer, "b = ref {4, 5, 6}").WithLocation(11, 17),
                // (14,28): error CS0622: Can only use array initializer expressions to assign to array types. Try using a new expression instead.
                //         ref object c = ref {7,8,9};
                Diagnostic(ErrorCode.ERR_ArrayInitToNonArrayType, "{7,8,9}").WithLocation(14, 28)
            );
        }
    }
}
