// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RefReturnTests : CompilingTestBase
    {
        [Fact]
        public void RefReturnArrayAccess()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        return ref (new int[1])[0];
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldc.i4.1
  IL_0001:  newarr     ""int""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefReturnRefParameter()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return ref i;
    }
}
";

            CompileAndVerify(text, verify: false).VerifyIL("Program.M(ref int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RefReturnOutParameter()
        {
            var text = @"
class Program
{
    static ref int M(out int i)
    {
        i = 0;
        return ref i;
    }
}
";

            CompileAndVerify(text, verify: false).VerifyIL("Program.M(out int)", @"
{
  // Code size        5 (0x5)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  stind.i4
  IL_0003:  ldarg.0
  IL_0004:  ret
}");
        }

        [Fact]
        public void RefReturnRefLocal()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        ref int local = ref i;
        local = 0;
        return ref local;
    }
}
";

            CompileAndVerify(text, verify: false).VerifyIL("Program.M(ref int)", @"
{
  // Code size        5 (0x5)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  dup
  IL_0002:  ldc.i4.0
  IL_0003:  stind.i4
  IL_0004:  ret
}");
        }

        [Fact]
        public void RefReturnStaticProperty()
        {
            var text = @"
class Program
{
    static int field = 0;
    static ref int P { get { return ref field; } }

    static ref int M()
    {
        return ref P;
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""ref int Program.P.get""
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceProperty()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int P { get { return ref field; } }

    ref int M()
    {
        return ref P;
    }

    ref int M1()
    {
        return ref new Program().P;
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program.M1()", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""ref int Program.P.get""
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReturnStructInstanceProperty()
        {
            var text = @"
struct Program
{
    public ref int P { get { return ref (new int[1])[0]; } }

    ref int M()
    {
        return ref P;
    }

    ref int M1(ref Program program)
    {
        return ref program.P;
    }
}

struct Program2
{
    Program program;

    Program2(Program program)
    {
        this.program = program;
    }

    ref int M()
    {
        return ref program.P;
    }
}

class Program3
{
    Program program = default(Program);

    ref int M()
    {
        return ref program.P;
    }
}
";

            var compilation = CompileAndVerify(text, verify: false);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program.M1(ref Program)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  call       ""ref int Program.P.get""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program2.M()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program2.program""
  IL_0006:  call       ""ref int Program.P.get""
  IL_000b:  ret
}");
            compilation.VerifyIL("Program3.M()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program3.program""
  IL_0006:  call       ""ref int Program.P.get""
  IL_000b:  ret
}");
        }

        [Fact]
        public void RefReturnConstrainedInstanceProperty()
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

    ref int M()
    {
        return ref t.P;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        return ref t.P;
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M()
    {
        return ref t.P;
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program<T>.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program<T>.t""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""ref int I.P.get""
  IL_0011:  ret
}");
            compilation.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""ref int I.P.get""
  IL_000b:  ret
}");
            compilation.VerifyIL("Program3<T>.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program3<T>.t""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""ref int I.P.get""
  IL_0011:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceIndexer()
        {
            var text = @"
class Program
{
    int field = 0;
    ref int this[int i] { get { return ref field; } }

    ref int M()
    {
        return ref this[0];
    }

    ref int M1()
    {
        return ref new Program()[0];
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       ""ref int Program.this[int].get""
  IL_0007:  ret
}");
            compilation.VerifyIL("Program.M1()", @"
{
  // Code size       12 (0xc)
  .maxstack  2
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  ldc.i4.0
  IL_0006:  call       ""ref int Program.this[int].get""
  IL_000b:  ret
}");
        }

        [Fact]
        public void RefReturnStructInstanceIndexer()
        {
            var text = @"
struct Program
{
    public ref int this[int i] { get { return ref (new int[1])[0]; } }

    ref int M()
    {
        return ref this[0];
    }
}

struct Program2
{
    Program program;

    Program2(Program program)
    {
        this.program = program;
    }

    ref int M()
    {
        return ref program[0];
    }
}

class Program3
{
    Program program = default(Program);

    ref int M()
    {
        return ref program[0];
    }
}
";

            var compilation = CompileAndVerify(text, verify: false);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        8 (0x8)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  call       ""ref int Program.this[int].get""
  IL_0007:  ret
}");
            compilation.VerifyIL("Program2.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program2.program""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref int Program.this[int].get""
  IL_000c:  ret
}");
            compilation.VerifyIL("Program3.M()", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program3.program""
  IL_0006:  ldc.i4.0
  IL_0007:  call       ""ref int Program.this[int].get""
  IL_000c:  ret
}");
        }

        [Fact]
        public void RefReturnConstrainedInstanceIndexer()
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

    ref int M()
    {
        return ref t[0];
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        return ref t[0];
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M()
    {
        return ref t[0];
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program<T>.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program<T>.t""
  IL_0006:  ldc.i4.0
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.this[int].get""
  IL_0012:  ret
}");
            compilation.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  ldc.i4.0
  IL_0007:  callvirt   ""ref int I.this[int].get""
  IL_000c:  ret
}");
            compilation.VerifyIL("Program3<T>.M()", @"
{
  // Code size       19 (0x13)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program3<T>.t""
  IL_0006:  ldc.i4.0
  IL_0007:  constrained. ""T""
  IL_000d:  callvirt   ""ref int I.this[int].get""
  IL_0012:  ret
}");
        }

        [Fact]
        public void RefReturnStaticFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    static event D d;

    static ref D M()
    {
        return ref d;
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsflda    ""D Program.d""
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceFieldLikeEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d;

    ref D M()
    {
        return ref d;
    }

    ref D M1()
    {
        return ref new Program().d;
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""D Program.d""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program.M1()", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  ldflda     ""D Program.d""
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReturnStaticField()
        {
            var text = @"
class Program
{
    static int i = 0;

    static ref int M()
    {
        return ref i;
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  ldsflda    ""int Program.i""
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceField()
        {
            var text = @"
class Program
{
    int i = 0;

    ref int M()
    {
        return ref i;
    }

    ref int M1()
    {
        return ref new Program().i;
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""int Program.i""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program.M1()", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  ldflda     ""int Program.i""
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReturnStructInstanceField()
        {
            var text = @"
struct Program
{
    public int i;
}

class Program2
{
    Program program = default(Program);

    ref int M(ref Program program)
    {
        return ref program.i;
    }

    ref int M()
    {
        return ref program.i;
    }
}
";

            var compilation = CompileAndVerify(text, verify: false);
            compilation.VerifyIL("Program2.M(ref Program)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  ldflda     ""int Program.i""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program2.M()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program2.program""
  IL_0006:  ldflda     ""int Program.i""
  IL_000b:  ret
}");
        }

        [Fact]
        public void RefReturnStaticCallWithoutArguments()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        return ref M();
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""ref int Program.M()""
  IL_0005:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceCallWithoutArguments()
        {
            var text = @"
class Program
{
    ref int M()
    {
        return ref M();
    }

    ref int M1()
    {
        return ref new Program().M();
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int Program.M()""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program.M1()", @"
{
  // Code size       11 (0xb)
  .maxstack  1
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  call       ""ref int Program.M()""
  IL_000a:  ret
}");
        }

        [Fact]
        public void RefReturnStructInstanceCallWithoutArguments()
        {
            var text = @"
struct Program
{
    public ref int M()
    {
        return ref M();
    }
}

struct Program2
{
    Program program;

    ref int M()
    {
        return ref program.M();
    }
}

class Program3
{
    Program program;

    ref int M()
    {
        return ref program.M();
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M()", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref int Program.M()""
  IL_0006:  ret
}");
            compilation.VerifyIL("Program2.M()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program2.program""
  IL_0006:  call       ""ref int Program.M()""
  IL_000b:  ret
}");
            compilation.VerifyIL("Program3.M()", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program3.program""
  IL_0006:  call       ""ref int Program.M()""
  IL_000b:  ret
}");
        }

        [Fact]
        public void RefReturnConstrainedInstanceCallWithoutArguments()
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
        return ref t.M();
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        return ref t.M();
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M()
    {
        return ref t.M();
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program<T>.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program<T>.t""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""ref int I.M()""
  IL_0011:  ret
}");
            compilation.VerifyIL("Program2<T>.M(T)", @"
{
  // Code size       12 (0xc)
  .maxstack  1
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  callvirt   ""ref int I.M()""
  IL_000b:  ret
}");
            compilation.VerifyIL("Program3<T>.M()", @"
{
  // Code size       18 (0x12)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program3<T>.t""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""ref int I.M()""
  IL_0011:  ret
}");
        }

        [Fact]
        public void RefReturnStaticCallWithArguments()
        {
            var text = @"
class Program
{
    static ref int M(ref int i, ref int j, object o)
    {
        return ref M(ref i, ref j, o);
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size        9 (0x9)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0008:  ret
}");
        }

        [Fact]
        public void RefReturnClassInstanceCallWithArguments()
        {
            var text = @"
class Program
{
    ref int M(ref int i, ref int j, object o)
    {
        return ref M(ref i, ref j, o);
    }

    ref int M1(ref int i, ref int j, object o)
    {
        return ref new Program().M(ref i, ref j, o);
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0009:  ret
}");
            compilation.VerifyIL("Program.M1(ref int, ref int, object)", @"
{
  // Code size       14 (0xe)
  .maxstack  4
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  ldarg.1
  IL_0006:  ldarg.2
  IL_0007:  ldarg.3
  IL_0008:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000d:  ret
}");
        }

        [Fact]
        public void RefReturnStructInstanceCallWithArguments()
        {
            var text = @"
struct Program
{
    public ref int M(ref int i, ref int j, object o)
    {
        return ref M(ref i, ref j, o);
    }
}

struct Program2
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        return ref program.M(ref i, ref j, o);
    }
}

class Program3
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        return ref program.M(ref i, ref j, o);
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program.M(ref int, ref int, object)", @"
{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_0009:  ret
}");
            compilation.VerifyIL("Program2.M(ref int, ref int, object)", @"
{
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program2.program""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000e:  ret
}");
            compilation.VerifyIL("Program3.M(ref int, ref int, object)", @"
{
  // Code size       15 (0xf)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""Program Program3.program""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  call       ""ref int Program.M(ref int, ref int, object)""
  IL_000e:  ret
}");
        }

        [Fact]
        public void RefReturnConstrainedInstanceCallWithArguments()
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
        return ref t.M(ref i, ref j, o);
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        return ref t.M(ref i, ref j, o);
    }
}

class Program3<T>
    where T : struct, I
{
    T t = default(T);

    ref int M(ref int i, ref int j, object o)
    {
        return ref t.M(ref i, ref j, o);
    }
}
";

            var compilation = CompileAndVerify(text);
            compilation.VerifyIL("Program<T>.M(ref int, ref int, object)", @"
{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program<T>.t""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0014:  ret
}");
            compilation.VerifyIL("Program2<T>.M(T, ref int, ref int, object)", @"
{
  // Code size       16 (0x10)
  .maxstack  4
  IL_0000:  ldarg.1
  IL_0001:  box        ""T""
  IL_0006:  ldarg.2
  IL_0007:  ldarg.3
  IL_0008:  ldarg.s    V_4
  IL_000a:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_000f:  ret
}");
            compilation.VerifyIL("Program3<T>.M(ref int, ref int, object)", @"
{
  // Code size       21 (0x15)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldflda     ""T Program3<T>.t""
  IL_0006:  ldarg.1
  IL_0007:  ldarg.2
  IL_0008:  ldarg.3
  IL_0009:  constrained. ""T""
  IL_000f:  callvirt   ""ref int I.M(ref int, ref int, object)""
  IL_0014:  ret
}");
        }

        [Fact]
        public void RefReturnDelegateInvocationWithNoArguments()
        {
            var text = @"
delegate ref int D();

class Program
{
    static ref int M(D d)
    {
        return ref d();
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M(D)", @"
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  callvirt   ""ref int D.Invoke()""
  IL_0006:  ret
}");
        }

        [Fact]
        public void RefReturnDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, ref int i, ref int j, object o)
    {
        return ref d(ref i, ref j, o);
    }
}
";

            CompileAndVerify(text).VerifyIL("Program.M(D, ref int, ref int, object)", @"
{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldarg.1
  IL_0002:  ldarg.2
  IL_0003:  ldarg.3
  IL_0004:  callvirt   ""ref int D.Invoke(ref int, ref int, object)""
  IL_0009:  ret
}");
        }

        [Fact]
        public void RefReturnsAreVariables()
        {
            var text = @"
class Program
{
    int field = 0;

    ref int P { get { return ref field; } }

    ref int this[int i] { get { return ref field; } }

    ref int M(ref int i)
    {
        return ref i;
    }

    void N(out int i)
    {
        i = 0;
    }

    static unsafe void Main()
    {
        var program = new Program();
        program.P = 0;
        program.P += 1;
        program.P++;
        program.M(ref program.P);
        program.N(out program.P);
        fixed (int* i = &program.P) { }
        var tr = __makeref(program.P);
        program[0] = 0;
        program[0] += 1;
        program[0]++;
        program.M(ref program[0]);
        program.N(out program[0]);
        fixed (int* i = &program[0]) { }
        tr = __makeref(program[0]);
        program.M(ref program.field) = 0;
        program.M(ref program.field) += 1;
        program.M(ref program.field)++;
        program.M(ref program.M(ref program.field));
        program.N(out program.M(ref program.field));
        fixed (int* i = &program.M(ref program.field)) { }
        tr = __makeref(program.M(ref program.field));
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll).VerifyIL("Program.Main()", @"
{
  // Code size      285 (0x11d)
  .maxstack  4
  .locals init (pinned int& V_0, //i
                pinned int& V_1, //i
                pinned int& V_2) //i
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""ref int Program.P.get""
  IL_000b:  ldc.i4.0
  IL_000c:  stind.i4
  IL_000d:  dup
  IL_000e:  callvirt   ""ref int Program.P.get""
  IL_0013:  dup
  IL_0014:  ldind.i4
  IL_0015:  ldc.i4.1
  IL_0016:  add
  IL_0017:  stind.i4
  IL_0018:  dup
  IL_0019:  callvirt   ""ref int Program.P.get""
  IL_001e:  dup
  IL_001f:  ldind.i4
  IL_0020:  ldc.i4.1
  IL_0021:  add
  IL_0022:  stind.i4
  IL_0023:  dup
  IL_0024:  dup
  IL_0025:  callvirt   ""ref int Program.P.get""
  IL_002a:  callvirt   ""ref int Program.M(ref int)""
  IL_002f:  pop
  IL_0030:  dup
  IL_0031:  dup
  IL_0032:  callvirt   ""ref int Program.P.get""
  IL_0037:  callvirt   ""void Program.N(out int)""
  IL_003c:  dup
  IL_003d:  callvirt   ""ref int Program.P.get""
  IL_0042:  stloc.0
  IL_0043:  ldc.i4.0
  IL_0044:  conv.u
  IL_0045:  stloc.0
  IL_0046:  dup
  IL_0047:  callvirt   ""ref int Program.P.get""
  IL_004c:  mkrefany   ""int""
  IL_0051:  pop
  IL_0052:  dup
  IL_0053:  ldc.i4.0
  IL_0054:  callvirt   ""ref int Program.this[int].get""
  IL_0059:  ldc.i4.0
  IL_005a:  stind.i4
  IL_005b:  dup
  IL_005c:  ldc.i4.0
  IL_005d:  callvirt   ""ref int Program.this[int].get""
  IL_0062:  dup
  IL_0063:  ldind.i4
  IL_0064:  ldc.i4.1
  IL_0065:  add
  IL_0066:  stind.i4
  IL_0067:  dup
  IL_0068:  ldc.i4.0
  IL_0069:  callvirt   ""ref int Program.this[int].get""
  IL_006e:  dup
  IL_006f:  ldind.i4
  IL_0070:  ldc.i4.1
  IL_0071:  add
  IL_0072:  stind.i4
  IL_0073:  dup
  IL_0074:  dup
  IL_0075:  ldc.i4.0
  IL_0076:  callvirt   ""ref int Program.this[int].get""
  IL_007b:  callvirt   ""ref int Program.M(ref int)""
  IL_0080:  pop
  IL_0081:  dup
  IL_0082:  dup
  IL_0083:  ldc.i4.0
  IL_0084:  callvirt   ""ref int Program.this[int].get""
  IL_0089:  callvirt   ""void Program.N(out int)""
  IL_008e:  dup
  IL_008f:  ldc.i4.0
  IL_0090:  callvirt   ""ref int Program.this[int].get""
  IL_0095:  stloc.1
  IL_0096:  ldc.i4.0
  IL_0097:  conv.u
  IL_0098:  stloc.1
  IL_0099:  dup
  IL_009a:  ldc.i4.0
  IL_009b:  callvirt   ""ref int Program.this[int].get""
  IL_00a0:  mkrefany   ""int""
  IL_00a5:  pop
  IL_00a6:  dup
  IL_00a7:  dup
  IL_00a8:  ldflda     ""int Program.field""
  IL_00ad:  callvirt   ""ref int Program.M(ref int)""
  IL_00b2:  ldc.i4.0
  IL_00b3:  stind.i4
  IL_00b4:  dup
  IL_00b5:  dup
  IL_00b6:  ldflda     ""int Program.field""
  IL_00bb:  callvirt   ""ref int Program.M(ref int)""
  IL_00c0:  dup
  IL_00c1:  ldind.i4
  IL_00c2:  ldc.i4.1
  IL_00c3:  add
  IL_00c4:  stind.i4
  IL_00c5:  dup
  IL_00c6:  dup
  IL_00c7:  ldflda     ""int Program.field""
  IL_00cc:  callvirt   ""ref int Program.M(ref int)""
  IL_00d1:  dup
  IL_00d2:  ldind.i4
  IL_00d3:  ldc.i4.1
  IL_00d4:  add
  IL_00d5:  stind.i4
  IL_00d6:  dup
  IL_00d7:  dup
  IL_00d8:  dup
  IL_00d9:  ldflda     ""int Program.field""
  IL_00de:  callvirt   ""ref int Program.M(ref int)""
  IL_00e3:  callvirt   ""ref int Program.M(ref int)""
  IL_00e8:  pop
  IL_00e9:  dup
  IL_00ea:  dup
  IL_00eb:  dup
  IL_00ec:  ldflda     ""int Program.field""
  IL_00f1:  callvirt   ""ref int Program.M(ref int)""
  IL_00f6:  callvirt   ""void Program.N(out int)""
  IL_00fb:  dup
  IL_00fc:  dup
  IL_00fd:  ldflda     ""int Program.field""
  IL_0102:  callvirt   ""ref int Program.M(ref int)""
  IL_0107:  stloc.2
  IL_0108:  ldc.i4.0
  IL_0109:  conv.u
  IL_010a:  stloc.2
  IL_010b:  dup
  IL_010c:  ldflda     ""int Program.field""
  IL_0111:  callvirt   ""ref int Program.M(ref int)""
  IL_0116:  mkrefany   ""int""
  IL_011b:  pop
  IL_011c:  ret
}");
        }

        [Fact]
        private void RefReturnsAreValues()
        {
            var text = @"
class Program
{
    int field = 0;

    ref int P { get { return ref field; } }

    ref int this[int i] { get { return ref field; } }

    ref int M(ref int i)
    {
        return ref i;
    }

    void N(int i)
    {
        i = 0;
    }

    static unsafe int Main()
    {
        var program = new Program();
        var @int = program.P + 0;
        var @string = program.P.ToString();
        var @long = (long)program.P;
        program.N(program.P);
        @int += program[0] + 0;
        @string = program[0].ToString();
        @long += (long)program[0];
        program.N(program[0]);
        @int += program.M(ref program.field) + 0;
        @string = program.M(ref program.field).ToString();
        @long += (long)program.M(ref program.field);
        program.N(program.M(ref program.field));
        return unchecked((int)((long)@int + @long));
    }
}
";

            CompileAndVerify(text, options: TestOptions.UnsafeReleaseDll).VerifyIL("Program.Main()", @"
{
  // Code size      174 (0xae)
  .maxstack  4
  .locals init (Program V_0, //program
                long V_1) //long
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""ref int Program.P.get""
  IL_000c:  ldind.i4
  IL_000d:  ldc.i4.0
  IL_000e:  add
  IL_000f:  ldloc.0
  IL_0010:  callvirt   ""ref int Program.P.get""
  IL_0015:  call       ""string int.ToString()""
  IL_001a:  pop
  IL_001b:  ldloc.0
  IL_001c:  callvirt   ""ref int Program.P.get""
  IL_0021:  ldind.i4
  IL_0022:  conv.i8
  IL_0023:  stloc.1
  IL_0024:  ldloc.0
  IL_0025:  ldloc.0
  IL_0026:  callvirt   ""ref int Program.P.get""
  IL_002b:  ldind.i4
  IL_002c:  callvirt   ""void Program.N(int)""
  IL_0031:  ldloc.0
  IL_0032:  ldc.i4.0
  IL_0033:  callvirt   ""ref int Program.this[int].get""
  IL_0038:  ldind.i4
  IL_0039:  ldc.i4.0
  IL_003a:  add
  IL_003b:  add
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.0
  IL_003e:  callvirt   ""ref int Program.this[int].get""
  IL_0043:  call       ""string int.ToString()""
  IL_0048:  pop
  IL_0049:  ldloc.1
  IL_004a:  ldloc.0
  IL_004b:  ldc.i4.0
  IL_004c:  callvirt   ""ref int Program.this[int].get""
  IL_0051:  ldind.i4
  IL_0052:  conv.i8
  IL_0053:  add
  IL_0054:  stloc.1
  IL_0055:  ldloc.0
  IL_0056:  ldloc.0
  IL_0057:  ldc.i4.0
  IL_0058:  callvirt   ""ref int Program.this[int].get""
  IL_005d:  ldind.i4
  IL_005e:  callvirt   ""void Program.N(int)""
  IL_0063:  ldloc.0
  IL_0064:  ldloc.0
  IL_0065:  ldflda     ""int Program.field""
  IL_006a:  callvirt   ""ref int Program.M(ref int)""
  IL_006f:  ldind.i4
  IL_0070:  ldc.i4.0
  IL_0071:  add
  IL_0072:  add
  IL_0073:  ldloc.0
  IL_0074:  ldloc.0
  IL_0075:  ldflda     ""int Program.field""
  IL_007a:  callvirt   ""ref int Program.M(ref int)""
  IL_007f:  call       ""string int.ToString()""
  IL_0084:  pop
  IL_0085:  ldloc.1
  IL_0086:  ldloc.0
  IL_0087:  ldloc.0
  IL_0088:  ldflda     ""int Program.field""
  IL_008d:  callvirt   ""ref int Program.M(ref int)""
  IL_0092:  ldind.i4
  IL_0093:  conv.i8
  IL_0094:  add
  IL_0095:  stloc.1
  IL_0096:  ldloc.0
  IL_0097:  ldloc.0
  IL_0098:  ldloc.0
  IL_0099:  ldflda     ""int Program.field""
  IL_009e:  callvirt   ""ref int Program.M(ref int)""
  IL_00a3:  ldind.i4
  IL_00a4:  callvirt   ""void Program.N(int)""
  IL_00a9:  conv.i8
  IL_00aa:  ldloc.1
  IL_00ab:  add
  IL_00ac:  conv.i4
  IL_00ad:  ret
}");
        }
    }
}
