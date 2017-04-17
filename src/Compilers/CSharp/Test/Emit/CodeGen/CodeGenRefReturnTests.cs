// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private CompilationVerifier CompileAndVerifyRef(
            string source,
            string expectedOutput = null,
            CSharpCompilationOptions options = null,
            bool verify = true)
        {
            return CompileAndVerify(
                source,
                expectedOutput: expectedOutput,
                options: options,
                verify: verify);
        }

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

            CompileAndVerifyRef(text).VerifyIL("Program.M()", @"
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

            CompileAndVerifyRef(text, verify: false).VerifyIL("Program.M(ref int)", @"
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

            CompileAndVerifyRef(text, verify: false).VerifyIL("Program.M(out int)", @"
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

            CompileAndVerifyRef(text, verify: false).VerifyIL("Program.M(ref int)", @"
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

    public static void Main()
    {
        var local = 42;   // must be real local
        P = local;
        P = local;  // assign again, should not use stack local
        System.Console.WriteLine(P);
    }
}
";

            var v = CompileAndVerifyRef(text, expectedOutput: "42");

            v.VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""ref int Program.P.get""
  IL_0005:  ret
}");

            v.VerifyIL("Program.Main()", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  .locals init (int V_0) //local
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  call       ""ref int Program.P.get""
  IL_0008:  ldloc.0
  IL_0009:  stind.i4
  IL_000a:  call       ""ref int Program.P.get""
  IL_000f:  ldloc.0
  IL_0010:  stind.i4
  IL_0011:  call       ""ref int Program.P.get""
  IL_0016:  ldind.i4
  IL_0017:  call       ""void System.Console.WriteLine(int)""
  IL_001c:  ret
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text, verify: false);
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text, verify: false);
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

            var compilation = CompileAndVerifyRef(text);
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

            CompileAndVerifyRef(text).VerifyIL("Program.M()", @"
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

            var compilation = CompileAndVerifyRef(text);
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

            CompileAndVerifyRef(text).VerifyIL("Program.M()", @"
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text, verify: false);
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

            CompileAndVerifyRef(text).VerifyIL("Program.M()", @"
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text);
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

            CompileAndVerifyRef(text).VerifyIL("Program.M(ref int, ref int, object)", @"
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text);
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

            var compilation = CompileAndVerifyRef(text);
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

            CompileAndVerifyRef(text).VerifyIL("Program.M(D)", @"
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

            CompileAndVerifyRef(text).VerifyIL("Program.M(D, ref int, ref int, object)", @"
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

            CompileAndVerifyRef(text, options: TestOptions.UnsafeReleaseDll).VerifyIL("Program.Main()", @"
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

            CompileAndVerifyRef(text, options: TestOptions.UnsafeReleaseDll).VerifyIL("Program.Main()", @"
{
  // Code size      168 (0xa8)
  .maxstack  4
  .locals init (Program V_0, //program
                long V_1) //long
  IL_0000:  newobj     ""Program..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""ref int Program.P.get""
  IL_000c:  ldind.i4
  IL_000d:  ldloc.0
  IL_000e:  callvirt   ""ref int Program.P.get""
  IL_0013:  call       ""string int.ToString()""
  IL_0018:  pop
  IL_0019:  ldloc.0
  IL_001a:  callvirt   ""ref int Program.P.get""
  IL_001f:  ldind.i4
  IL_0020:  conv.i8
  IL_0021:  stloc.1
  IL_0022:  ldloc.0
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""ref int Program.P.get""
  IL_0029:  ldind.i4
  IL_002a:  callvirt   ""void Program.N(int)""
  IL_002f:  ldloc.0
  IL_0030:  ldc.i4.0
  IL_0031:  callvirt   ""ref int Program.this[int].get""
  IL_0036:  ldind.i4
  IL_0037:  add
  IL_0038:  ldloc.0
  IL_0039:  ldc.i4.0
  IL_003a:  callvirt   ""ref int Program.this[int].get""
  IL_003f:  call       ""string int.ToString()""
  IL_0044:  pop
  IL_0045:  ldloc.1
  IL_0046:  ldloc.0
  IL_0047:  ldc.i4.0
  IL_0048:  callvirt   ""ref int Program.this[int].get""
  IL_004d:  ldind.i4
  IL_004e:  conv.i8
  IL_004f:  add
  IL_0050:  stloc.1
  IL_0051:  ldloc.0
  IL_0052:  ldloc.0
  IL_0053:  ldc.i4.0
  IL_0054:  callvirt   ""ref int Program.this[int].get""
  IL_0059:  ldind.i4
  IL_005a:  callvirt   ""void Program.N(int)""
  IL_005f:  ldloc.0
  IL_0060:  ldloc.0
  IL_0061:  ldflda     ""int Program.field""
  IL_0066:  callvirt   ""ref int Program.M(ref int)""
  IL_006b:  ldind.i4
  IL_006c:  add
  IL_006d:  ldloc.0
  IL_006e:  ldloc.0
  IL_006f:  ldflda     ""int Program.field""
  IL_0074:  callvirt   ""ref int Program.M(ref int)""
  IL_0079:  call       ""string int.ToString()""
  IL_007e:  pop
  IL_007f:  ldloc.1
  IL_0080:  ldloc.0
  IL_0081:  ldloc.0
  IL_0082:  ldflda     ""int Program.field""
  IL_0087:  callvirt   ""ref int Program.M(ref int)""
  IL_008c:  ldind.i4
  IL_008d:  conv.i8
  IL_008e:  add
  IL_008f:  stloc.1
  IL_0090:  ldloc.0
  IL_0091:  ldloc.0
  IL_0092:  ldloc.0
  IL_0093:  ldflda     ""int Program.field""
  IL_0098:  callvirt   ""ref int Program.M(ref int)""
  IL_009d:  ldind.i4
  IL_009e:  callvirt   ""void Program.N(int)""
  IL_00a3:  conv.i8
  IL_00a4:  ldloc.1
  IL_00a5:  add
  IL_00a6:  conv.i4
  IL_00a7:  ret
}");
        }

        [Fact]
        public void RefReturnArrayAccessNested()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        ref int N()
        {
            return ref (new int[1])[0];
        }

        return ref N();
    }
}
";

            CompileAndVerify(text, parseOptions: TestOptions.Regular).VerifyIL("Program.M()", @"
{
  // Code size        6 (0x6)
  .maxstack  1
  IL_0000:  call       ""ref int Program.<M>g__N0_0()""
  IL_0005:  ret
}").VerifyIL("Program.<M>g__N0_0", @"
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
        public void RefReturnArrayAccessNested1()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        var arr = new int[1]{40};

        ref int N()
        {
            ref int NN(ref int arg) => ref arg;

            ref var r = ref NN(ref arr[0]); 
            r += 2;

            return ref r;
        }

        return ref N();
    }

    static void Main()
    {
        System.Console.WriteLine(M());
    }
}
";

            CompileAndVerify(text, parseOptions: TestOptions.Regular, expectedOutput: "42", verify: false).VerifyIL("Program.M()", @"
{
  // Code size       34 (0x22)
  .maxstack  5
  .locals init (Program.<>c__DisplayClass0_0 V_0) //CS$<>8__locals0
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""Program.<>c__DisplayClass0_0""
  IL_0008:  ldloca.s   V_0
  IL_000a:  ldc.i4.1
  IL_000b:  newarr     ""int""
  IL_0010:  dup
  IL_0011:  ldc.i4.0
  IL_0012:  ldc.i4.s   40
  IL_0014:  stelem.i4
  IL_0015:  stfld      ""int[] Program.<>c__DisplayClass0_0.arr""
  IL_001a:  ldloca.s   V_0
  IL_001c:  call       ""ref int Program.<M>g__N0_0(ref Program.<>c__DisplayClass0_0)""
  IL_0021:  ret
}").VerifyIL("Program.<M>g__N0_0", @"
{
  // Code size       24 (0x18)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int[] Program.<>c__DisplayClass0_0.arr""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  call       ""ref int Program.<M>g__NN0_1(ref int)""
  IL_0011:  dup
  IL_0012:  dup
  IL_0013:  ldind.i4
  IL_0014:  ldc.i4.2
  IL_0015:  add
  IL_0016:  stind.i4
  IL_0017:  ret
}").VerifyIL("Program.<M>g__NN0_1", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RefReturnArrayAccessNested2()
        {
            var text = @"
class Program
{
    delegate ref int D();    

    static D M()
    {
        var arr = new int[1]{40};

        ref int N()
        {
            ref int NN(ref int arg) => ref arg;

            ref var r = ref NN(ref arr[0]); 
            r += 2;

            return ref r;
        }

        return N;
    }

    static void Main()
    {
        System.Console.WriteLine(M()());
    }
}
";

            CompileAndVerify(text, parseOptions: TestOptions.Regular, expectedOutput: "42", verify: false).VerifyIL("Program.M()", @"
{
  // Code size       36 (0x24)
  .maxstack  5
  .locals init (Program.<>c__DisplayClass1_0 V_0) //CS$<>8__locals0
  IL_0000:  newobj     ""Program.<>c__DisplayClass1_0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  ldc.i4.1
  IL_0008:  newarr     ""int""
  IL_000d:  dup
  IL_000e:  ldc.i4.0
  IL_000f:  ldc.i4.s   40
  IL_0011:  stelem.i4
  IL_0012:  stfld      ""int[] Program.<>c__DisplayClass1_0.arr""
  IL_0017:  ldloc.0
  IL_0018:  ldftn      ""ref int Program.<>c__DisplayClass1_0.<M>g__N0()""
  IL_001e:  newobj     ""Program.D..ctor(object, System.IntPtr)""
  IL_0023:  ret
}").VerifyIL("Program.<>c__DisplayClass1_0.<M>g__N0()", @"
{
  // Code size       24 (0x18)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""int[] Program.<>c__DisplayClass1_0.arr""
  IL_0006:  ldc.i4.0
  IL_0007:  ldelema    ""int""
  IL_000c:  call       ""ref int Program.<M>g__NN1_1(ref int)""
  IL_0011:  dup
  IL_0012:  dup
  IL_0013:  ldind.i4
  IL_0014:  ldc.i4.2
  IL_0015:  add
  IL_0016:  stind.i4
  IL_0017:  ret
}").VerifyIL("Program.<M>g__NN1_1(ref int)", @"
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}");
        }

        [Fact]
        public void RefReturnConditionalAccess01()
        {
            var text = @"
    using System;
 
    class Program
    {
        class C1<T> where T : IDisposable
        {
            T inst = default(T);

            public ref T GetDisposable()
            {
                return ref inst;
            }

            public void Test()
            {
                GetDisposable().Dispose();
                System.Console.Write(inst.ToString());

                GetDisposable()?.Dispose();
                System.Console.Write(inst.ToString());
            }
        }

        static void Main(string[] args)
        {
            var v = new C1<Mutable>();
            v.Test();
        }
    }

    struct Mutable : IDisposable
    {
        public int disposed;
         
        public void Dispose()
        {
            disposed += 1;
        }

        public override string ToString()
        {
            return disposed.ToString();
        }
    }
";

            CompileAndVerifyRef(text, expectedOutput: "12")
                .VerifyIL("Program.C1<T>.Test()", @"
{
  // Code size      114 (0x72)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  call       ""ref T Program.C1<T>.GetDisposable()""
  IL_0006:  constrained. ""T""
  IL_000c:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0011:  ldarg.0
  IL_0012:  ldflda     ""T Program.C1<T>.inst""
  IL_0017:  constrained. ""T""
  IL_001d:  callvirt   ""string object.ToString()""
  IL_0022:  call       ""void System.Console.Write(string)""
  IL_0027:  ldarg.0
  IL_0028:  call       ""ref T Program.C1<T>.GetDisposable()""
  IL_002d:  ldloca.s   V_0
  IL_002f:  initobj    ""T""
  IL_0035:  ldloc.0
  IL_0036:  box        ""T""
  IL_003b:  brtrue.s   IL_0050
  IL_003d:  ldobj      ""T""
  IL_0042:  stloc.0
  IL_0043:  ldloca.s   V_0
  IL_0045:  ldloc.0
  IL_0046:  box        ""T""
  IL_004b:  brtrue.s   IL_0050
  IL_004d:  pop
  IL_004e:  br.s       IL_005b
  IL_0050:  constrained. ""T""
  IL_0056:  callvirt   ""void System.IDisposable.Dispose()""
  IL_005b:  ldarg.0
  IL_005c:  ldflda     ""T Program.C1<T>.inst""
  IL_0061:  constrained. ""T""
  IL_0067:  callvirt   ""string object.ToString()""
  IL_006c:  call       ""void System.Console.Write(string)""
  IL_0071:  ret
}");
        }

        [Fact]
        public void RefReturnConditionalAccess02()
        {
            var text = @"
using System;

class Program
{
    class C1<T> where T : IDisposable
    {
        T inst = default(T);

        public ref T GetDisposable(ref T arg)
        {
            return ref arg;
        }

        public void Test()
        {
            ref T temp = ref GetDisposable(ref inst);
            temp.Dispose();
            System.Console.Write(inst.ToString());

            temp?.Dispose();
            System.Console.Write(inst.ToString());
        }
    }

    static void Main(string[] args)
    {
        var v = new C1<Mutable>();
        v.Test();
    }
}

struct Mutable : IDisposable
{
    public int disposed;

    public void Dispose()
    {
        disposed += 1;
    }

    public override string ToString()
    {
        return disposed.ToString();
    }
}
";

            CompileAndVerifyRef(text, expectedOutput: "12", verify: false)
                .VerifyIL("Program.C1<T>.Test()", @"
{
  // Code size      115 (0x73)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program.C1<T>.inst""
  IL_0007:  call       ""ref T Program.C1<T>.GetDisposable(ref T)""
  IL_000c:  dup
  IL_000d:  constrained. ""T""
  IL_0013:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0018:  ldarg.0
  IL_0019:  ldflda     ""T Program.C1<T>.inst""
  IL_001e:  constrained. ""T""
  IL_0024:  callvirt   ""string object.ToString()""
  IL_0029:  call       ""void System.Console.Write(string)""
  IL_002e:  ldloca.s   V_0
  IL_0030:  initobj    ""T""
  IL_0036:  ldloc.0
  IL_0037:  box        ""T""
  IL_003c:  brtrue.s   IL_0051
  IL_003e:  ldobj      ""T""
  IL_0043:  stloc.0
  IL_0044:  ldloca.s   V_0
  IL_0046:  ldloc.0
  IL_0047:  box        ""T""
  IL_004c:  brtrue.s   IL_0051
  IL_004e:  pop
  IL_004f:  br.s       IL_005c
  IL_0051:  constrained. ""T""
  IL_0057:  callvirt   ""void System.IDisposable.Dispose()""
  IL_005c:  ldarg.0
  IL_005d:  ldflda     ""T Program.C1<T>.inst""
  IL_0062:  constrained. ""T""
  IL_0068:  callvirt   ""string object.ToString()""
  IL_006d:  call       ""void System.Console.Write(string)""
  IL_0072:  ret
}");
        }

        [Fact]
        public void RefReturnConditionalAccess03()
        {
            var text = @"
using System;

class Program
{
    class C1<T> where T : IDisposable
    {
        T inst = default(T);

        public ref T GetDisposable(ref T arg)
        {
            return ref arg;
        }

        public void Test()
        {
            ref T temp = ref GetDisposable(ref inst);

            // prevent eliding of temp 
            for(int i = 0; i < 2; i++)
            {
                temp.Dispose();
                System.Console.Write(inst.ToString());

                temp?.Dispose();
                System.Console.Write(inst.ToString());
            }
        }
    }

    static void Main(string[] args)
    {
        var v = new C1<Mutable>();
        v.Test();
    }
}

struct Mutable : IDisposable
{
    public int disposed;

    public void Dispose()
    {
        disposed += 1;
    }

    public override string ToString()
    {
        return disposed.ToString();
    }
}
";

            CompileAndVerifyRef(text, expectedOutput: "1234", verify: false)
                .VerifyIL("Program.C1<T>.Test()", @"
{
  // Code size      129 (0x81)
  .maxstack  2
  .locals init (T& V_0, //temp
                int V_1, //i
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program.C1<T>.inst""
  IL_0007:  call       ""ref T Program.C1<T>.GetDisposable(ref T)""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  IL_000f:  br.s       IL_007c
  IL_0011:  ldloc.0
  IL_0012:  constrained. ""T""
  IL_0018:  callvirt   ""void System.IDisposable.Dispose()""
  IL_001d:  ldarg.0
  IL_001e:  ldflda     ""T Program.C1<T>.inst""
  IL_0023:  constrained. ""T""
  IL_0029:  callvirt   ""string object.ToString()""
  IL_002e:  call       ""void System.Console.Write(string)""
  IL_0033:  ldloc.0
  IL_0034:  ldloca.s   V_2
  IL_0036:  initobj    ""T""
  IL_003c:  ldloc.2
  IL_003d:  box        ""T""
  IL_0042:  brtrue.s   IL_0057
  IL_0044:  ldobj      ""T""
  IL_0049:  stloc.2
  IL_004a:  ldloca.s   V_2
  IL_004c:  ldloc.2
  IL_004d:  box        ""T""
  IL_0052:  brtrue.s   IL_0057
  IL_0054:  pop
  IL_0055:  br.s       IL_0062
  IL_0057:  constrained. ""T""
  IL_005d:  callvirt   ""void System.IDisposable.Dispose()""
  IL_0062:  ldarg.0
  IL_0063:  ldflda     ""T Program.C1<T>.inst""
  IL_0068:  constrained. ""T""
  IL_006e:  callvirt   ""string object.ToString()""
  IL_0073:  call       ""void System.Console.Write(string)""
  IL_0078:  ldloc.1
  IL_0079:  ldc.i4.1
  IL_007a:  add
  IL_007b:  stloc.1
  IL_007c:  ldloc.1
  IL_007d:  ldc.i4.2
  IL_007e:  blt.s      IL_0011
  IL_0080:  ret
}");
        }

        [Fact]
        public void RefReturnConditionalAccess04()
        {
            var text = @"
using System;

class Program
{
    class C1<T> where T : IFoo<T>, new()
    {
        T inst = new T();

        public ref T GetDisposable(ref T arg)
        {
            return ref arg;
        }

        public void Test()
        {
            GetDisposable(ref inst)?.Blah(ref inst);
            System.Console.Write(inst == null);
        }
    }

    static void Main(string[] args)
    {
        var v = new C1<Foo>();
        v.Test();
    }
}

interface IFoo<T>
{
    void Blah(ref T arg);
}

class Foo : IFoo<Foo>
{
    public int disposed;

    public void Blah(ref Foo arg)
    {
        arg = null;
        disposed++;
        System.Console.Write(disposed);
    }
}
";

            CompileAndVerifyRef(text, expectedOutput: "1True", verify: false)
                .VerifyIL("Program.C1<T>.Test()", @"
{
  // Code size       84 (0x54)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program.C1<T>.inst""
  IL_0007:  call       ""ref T Program.C1<T>.GetDisposable(ref T)""
  IL_000c:  ldloca.s   V_0
  IL_000e:  initobj    ""T""
  IL_0014:  ldloc.0
  IL_0015:  box        ""T""
  IL_001a:  brtrue.s   IL_002f
  IL_001c:  ldobj      ""T""
  IL_0021:  stloc.0
  IL_0022:  ldloca.s   V_0
  IL_0024:  ldloc.0
  IL_0025:  box        ""T""
  IL_002a:  brtrue.s   IL_002f
  IL_002c:  pop
  IL_002d:  br.s       IL_0040
  IL_002f:  ldarg.0
  IL_0030:  ldflda     ""T Program.C1<T>.inst""
  IL_0035:  constrained. ""T""
  IL_003b:  callvirt   ""void IFoo<T>.Blah(ref T)""
  IL_0040:  ldarg.0
  IL_0041:  ldfld      ""T Program.C1<T>.inst""
  IL_0046:  box        ""T""
  IL_004b:  ldnull
  IL_004c:  ceq
  IL_004e:  call       ""void System.Console.Write(bool)""
  IL_0053:  ret
}");
        }

        [Fact]
        public void RefReturnConditionalAccess05()
        {
            var text = @"
using System;

class Program
{
    class C1<T> where T : IFoo<T>, new()
    {
        T inst = new T();

        public ref T GetDisposable(ref T arg)
        {
            return ref arg;
        }

        public void Test()
        {
            ref T temp = ref GetDisposable(ref inst);

            // prevent eliding of temp 
            for(int i = 0; i < 2; i++)
            {
                temp?.Blah(ref temp);
                System.Console.Write(temp == null);
                System.Console.Write(inst == null);

                inst = new T();
                temp?.Blah(ref temp);
                System.Console.Write(temp == null);
                System.Console.Write(inst == null);
            }
        }
    }

    static void Main(string[] args)
    {
        var v = new C1<Foo>();
        v.Test();
    }
}

interface IFoo<T>
{
    void Blah(ref T arg);
}

class Foo : IFoo<Foo>
{
    public int disposed;

    public void Blah(ref Foo arg)
    {
        arg = null;
        disposed++;
        System.Console.Write(disposed);
    }
}
";

            CompileAndVerifyRef(text, expectedOutput: "1TrueTrue1TrueTrueTrueTrue1TrueTrue", verify: false)
                .VerifyIL("Program.C1<T>.Test()", @"
{
  // Code size      215 (0xd7)
  .maxstack  2
  .locals init (T& V_0, //temp
                int V_1, //i
                T V_2)
  IL_0000:  ldarg.0
  IL_0001:  ldarg.0
  IL_0002:  ldflda     ""T Program.C1<T>.inst""
  IL_0007:  call       ""ref T Program.C1<T>.GetDisposable(ref T)""
  IL_000c:  stloc.0
  IL_000d:  ldc.i4.0
  IL_000e:  stloc.1
  IL_000f:  br         IL_00cf
  IL_0014:  ldloc.0
  IL_0015:  ldloca.s   V_2
  IL_0017:  initobj    ""T""
  IL_001d:  ldloc.2
  IL_001e:  box        ""T""
  IL_0023:  brtrue.s   IL_0038
  IL_0025:  ldobj      ""T""
  IL_002a:  stloc.2
  IL_002b:  ldloca.s   V_2
  IL_002d:  ldloc.2
  IL_002e:  box        ""T""
  IL_0033:  brtrue.s   IL_0038
  IL_0035:  pop
  IL_0036:  br.s       IL_0044
  IL_0038:  ldloc.0
  IL_0039:  constrained. ""T""
  IL_003f:  callvirt   ""void IFoo<T>.Blah(ref T)""
  IL_0044:  ldloc.0
  IL_0045:  ldobj      ""T""
  IL_004a:  box        ""T""
  IL_004f:  ldnull
  IL_0050:  ceq
  IL_0052:  call       ""void System.Console.Write(bool)""
  IL_0057:  ldarg.0
  IL_0058:  ldfld      ""T Program.C1<T>.inst""
  IL_005d:  box        ""T""
  IL_0062:  ldnull
  IL_0063:  ceq
  IL_0065:  call       ""void System.Console.Write(bool)""
  IL_006a:  ldarg.0
  IL_006b:  call       ""T System.Activator.CreateInstance<T>()""
  IL_0070:  stfld      ""T Program.C1<T>.inst""
  IL_0075:  ldloc.0
  IL_0076:  ldloca.s   V_2
  IL_0078:  initobj    ""T""
  IL_007e:  ldloc.2
  IL_007f:  box        ""T""
  IL_0084:  brtrue.s   IL_0099
  IL_0086:  ldobj      ""T""
  IL_008b:  stloc.2
  IL_008c:  ldloca.s   V_2
  IL_008e:  ldloc.2
  IL_008f:  box        ""T""
  IL_0094:  brtrue.s   IL_0099
  IL_0096:  pop
  IL_0097:  br.s       IL_00a5
  IL_0099:  ldloc.0
  IL_009a:  constrained. ""T""
  IL_00a0:  callvirt   ""void IFoo<T>.Blah(ref T)""
  IL_00a5:  ldloc.0
  IL_00a6:  ldobj      ""T""
  IL_00ab:  box        ""T""
  IL_00b0:  ldnull
  IL_00b1:  ceq
  IL_00b3:  call       ""void System.Console.Write(bool)""
  IL_00b8:  ldarg.0
  IL_00b9:  ldfld      ""T Program.C1<T>.inst""
  IL_00be:  box        ""T""
  IL_00c3:  ldnull
  IL_00c4:  ceq
  IL_00c6:  call       ""void System.Console.Write(bool)""
  IL_00cb:  ldloc.1
  IL_00cc:  ldc.i4.1
  IL_00cd:  add
  IL_00ce:  stloc.1
  IL_00cf:  ldloc.1
  IL_00d0:  ldc.i4.2
  IL_00d1:  blt        IL_0014
  IL_00d6:  ret
}");
        }

        [Fact]
        public void RefReturn_CSharp6()
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

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (4,12): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //     static ref int M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(4, 12),
                // (6,16): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         return ref (new int[1])[0];
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(6, 16)
                );
        }

        [Fact]
        public void RefInLambda_CSharp6()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        var arr = new int[1]{40};

        ref int N()
        {
            ref int NN(ref int arg) => ref arg;

            ref var r = ref NN(ref arr[0]);
            r += 2;

            return ref r;
        }

        return ref N();
    }
}
";
            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (4,12): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //     static ref int M()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(4, 12),
                // (8,9): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         ref int N()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(8, 9),
                // (8,17): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7 or greater.
                //         ref int N()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "N").WithArguments("local functions", "7").WithLocation(8, 17),
                // (10,13): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //             ref int NN(ref int arg) => ref arg;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(10, 13),
                // (10,21): error CS8059: Feature 'local functions' is not available in C# 6. Please use language version 7 or greater.
                //             ref int NN(ref int arg) => ref arg;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "NN").WithArguments("local functions", "7").WithLocation(10, 21),
                // (10,40): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //             ref int NN(ref int arg) => ref arg;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(10, 40),
                // (12,13): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //             ref var r = ref NN(ref arr[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref var").WithArguments("byref locals and returns", "7").WithLocation(12, 13),
                // (12,25): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //             ref var r = ref NN(ref arr[0]);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(12, 25),
                // (15,20): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //             return ref r;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(15, 20),
                // (18,16): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         return ref N();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(18, 16)
                );
        }

        [Fact]
        public void RefDelegate_CSharp6()
        {
            var text = @"
delegate ref int D();
";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (2,10): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                // delegate ref int D();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(2, 10)
                );
        }

        [Fact]
        public void RefInForStatement_CSharp6()
        {
            var text = @"
class Program
{
    static int M(ref int d)
    {
        for (ref int a = ref d; ;) { }
    }
}
";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (6,14): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         for (ref int a = ref d; ;) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(6, 14),
                // (6,26): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         for (ref int a = ref d; ;) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(6, 26)
                );
        }

        [Fact]
        public void RefLambdaInferenceMethodArgument()
        {
            var text = @"
delegate ref int D(int x);

class C
{
    static void MD(D d) { }

    static int i = 0;
    static void M()
    {
        MD((x) => ref i);
        MD(x => ref i);
    }
}
";

            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            comp.VerifyDiagnostics(
                // (2,10): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                // delegate ref int D(int x);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref int").WithArguments("byref locals and returns", "7").WithLocation(2, 10),
                // (11,19): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         MD((x) => ref i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(11, 19),
                // (12,17): error CS8059: Feature 'byref locals and returns' is not available in C# 6. Please use language version 7 or greater.
                //         MD(x => ref i);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "ref").WithArguments("byref locals and returns", "7").WithLocation(12, 17)
                );
        }

        [Fact]
        public void Override_Metadata()
        {
            var ilSource =
@".class public abstract A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object F() { ldnull throw }
  .method public virtual instance object& get_P() { ldnull throw }
  .property instance object& P()
  {
    .get instance object& A::get_P()
  }
}
.class public abstract B1 extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object F() { ldnull throw }
  .method public virtual instance object get_P() { ldnull throw }
  .property instance object P()
  {
    .get instance object B1::get_P()
  }
}
.class public abstract B2 extends A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public virtual instance object& F() { ldnull throw }
  .method public virtual instance object& get_P() { ldnull throw }
  .property instance object& P()
  {
    .get instance object& B2::get_P()
  }
}";
            var ref1 = CompileIL(ilSource);
            var compilation = CreateStandardCompilation("", options: TestOptions.DebugDll, references: new[] { ref1 });

            var method = compilation.GetMember<MethodSymbol>("B1.F");
            Assert.Equal("System.Object B1.F()", method.ToTestDisplayString());
            Assert.Equal("System.Object A.F()", method.OverriddenMethod.ToTestDisplayString());

            var property = compilation.GetMember<PropertySymbol>("B1.P");
            Assert.Equal("System.Object B1.P { get; }", property.ToTestDisplayString());
            Assert.Null(property.OverriddenProperty);

            method = compilation.GetMember<MethodSymbol>("B2.F");
            Assert.Equal("ref System.Object B2.F()", method.ToTestDisplayString());
            Assert.Null(method.OverriddenMethod);

            property = compilation.GetMember<PropertySymbol>("B2.P");
            Assert.Equal("ref System.Object B2.P { get; }", property.ToTestDisplayString());
            Assert.Equal("ref System.Object A.P { get; }", property.OverriddenProperty.ToTestDisplayString());
        }

        [WorkItem(12763, "https://github.com/dotnet/roslyn/issues/12763")]
        [Fact]
        public void RefReturnFieldUse001()
        {
            var text = @"
public class A<T>
{
    private T _f;
    public ref T F()
    {
        return ref _f;
    }

    private T _p;
    public ref T P
    {
        get { return ref _p; }
    }
}
";

            var comp = CreateStandardCompilation(text, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                 // no diagnostics expected
                );
        }

        [WorkItem(12763, "https://github.com/dotnet/roslyn/issues/12763")]
        [Fact]
        public void RefAssignFieldUse001()
        {
            var text = @"
public class A<T>
{
    private T _f;
    public ref T F()
    {
        ref var r = ref _f;
        return ref r;
    }

    private T _p;
    public ref T P
    {
        get 
        { 
            ref var r = ref _p;
            return ref r;
        }
    }
}
";

            var comp = CreateStandardCompilation(text, options: TestOptions.ReleaseDll);

            comp.VerifyDiagnostics(
                // no diagnostics expected
                );
        }

        [Fact]
        public void ThrowRefReturn()
        {
            var text = @"using System;
class Program
{
    static ref int P1 { get => throw new E(1); }
    static ref int P2 => throw new E(2);
    static ref int M() => throw new E(3);

    public static void Main()
    {
        ref int L() => throw new E(4);
        D d = () => throw new E(5);

        try { ref int x = ref P1; }  catch (E e) { Console.Write(e.Value); }
        try { ref int x = ref P2; }  catch (E e) { Console.Write(e.Value); }
        try { ref int x = ref M(); } catch (E e) { Console.Write(e.Value); }
        try { ref int x = ref L(); } catch (E e) { Console.Write(e.Value); }
        try { ref int x = ref d(); } catch (E e) { Console.Write(e.Value); }
    }
}
delegate ref int D();
class E : Exception
{
    public int Value;
    public E(int value) { this.Value = value; }
}
";
            var v = CompileAndVerify(text, expectedOutput: "12345");
        }

        [Fact]
        public void NoRefThrow()
        {
            var text = @"using System;
class Program
{
    static ref int P1 { get => ref throw new E(1); }
    static ref int P2 => ref throw new E(2);
    static ref int M() => ref throw new E(3);

    public static void Main()
    {
        ref int L() => ref throw new E(4);
        D d = () => ref throw new E(5);
        L();
        d();
    }
}
delegate ref int D();
class E : Exception
{
    public int Value;
    public E(int value) { this.Value = value; }
}
";
            CreateStandardCompilation(text).VerifyDiagnostics(
                // (4,36): error CS8115: A throw expression is not allowed in this context.
                //     static ref int P1 { get => ref throw new E(1); }
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(4, 36),
                // (5,30): error CS8115: A throw expression is not allowed in this context.
                //     static ref int P2 => ref throw new E(2);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(5, 30),
                // (6,31): error CS8115: A throw expression is not allowed in this context.
                //     static ref int M() => ref throw new E(3);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(6, 31),
                // (10,28): error CS8115: A throw expression is not allowed in this context.
                //         ref int L() => ref throw new E(4);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(10, 28),
                // (11,25): error CS8115: A throw expression is not allowed in this context.
                //         D d = () => ref throw new E(5);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(11, 25)
                );
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Lambda_01()
        { 
            var source =
@"public delegate ref T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(() => o.F(), 2);
        System.Console.WriteLine(o.F());
    }
}";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (24,19): error CS8150: By-value returns may only be used in methods that return by value
                //         B.F(() => o.F(), 2);
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "o.F()").WithLocation(24, 19)
                );
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Lambda_02()
        {
            var source =
@"public delegate ref T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(() => ref o.F(), 2);
        System.Console.WriteLine(o.F());
    }
}";

            var v = CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Lambda_03()
        {
            var source =
@"public delegate T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d();
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(() => ref o.F(), 2);
        System.Console.WriteLine(o.F());
    }
}";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (24,23): error CS8149: By-reference returns may only be used in methods that return by reference
                //         B.F(() => ref o.F(), 2);
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "o.F()").WithLocation(24, 23)
                );
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Delegate_01()
        {
            var source =
@"public delegate ref T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(o.F, 2);
        System.Console.Write(o.F());
        B.F(new D<int>(o.F), 3);
        System.Console.Write(o.F());
    }
}";

            var v = CompileAndVerify(source, expectedOutput: "23");
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Delegate_02()
        {
            var source =
@"public delegate T D<T>();
public class A<T>
{
#pragma warning disable 0649
    private T _t;
    public ref T F()
    {
        return ref _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d();
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(o.F, 2);
        System.Console.Write(o.F());
        B.F(new D<int>(o.F), 3);
        System.Console.Write(o.F());
    }
}";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (24,13): error CS8189: Ref mismatch between 'A<int>.F()' and delegate 'D<int>'
                //         B.F(o.F, 2);
                Diagnostic(ErrorCode.ERR_DelegateRefMismatch, "o.F").WithArguments("A<int>.F()", "D<int>").WithLocation(24, 13),
                // (26,24): error CS8189: Ref mismatch between 'A<int>.F()' and delegate 'D<int>'
                //         B.F(new D<int>(o.F), 3);
                Diagnostic(ErrorCode.ERR_DelegateRefMismatch, "o.F").WithArguments("A<int>.F()", "D<int>").WithLocation(26, 24)
                );
        }

        [Fact]
        [WorkItem(13206, "https://github.com/dotnet/roslyn/issues/13206")]
        public void Delegate_03()
        {
            var source =
@"public delegate ref T D<T>();
public class A<T>
{
    private T _t = default(T);
    public T F()
    {
        return _t;
    }
}
public class B
{
    public static void F<T>(D<T> d, T t)
    {
        d() = t;
    }
}

class Program
{
    static void Main()
    {
        var o = new A<int>();
        B.F(o.F, 2);
        System.Console.Write(o.F());
        B.F(new D<int>(o.F), 3);
        System.Console.Write(o.F());
    }
}";

            CreateStandardCompilation(source).VerifyDiagnostics(
                // (23,13): error CS8189: Ref mismatch between 'A<int>.F()' and delegate 'D<int>'
                //         B.F(o.F, 2);
                Diagnostic(ErrorCode.ERR_DelegateRefMismatch, "o.F").WithArguments("A<int>.F()", "D<int>").WithLocation(23, 13),
                // (25,24): error CS8189: Ref mismatch between 'A<int>.F()' and delegate 'D<int>'
                //         B.F(new D<int>(o.F), 3);
                Diagnostic(ErrorCode.ERR_DelegateRefMismatch, "o.F").WithArguments("A<int>.F()", "D<int>").WithLocation(25, 24)
                );
        }

        [WorkItem(16947, "https://github.com/dotnet/roslyn/issues/16947")]
        public void Dynamic001()
        {
            var source =
@"

public class C 
{
    public void M() 
    {
        dynamic d = ""qq"";

        F(ref d);
    }

    public static ref dynamic F(ref dynamic d)
    {
        return ref d.Length;
    }
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (14,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref d.Length;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d.Length").WithLocation(14, 20)
            );
        }
        [WorkItem(16947, "https://github.com/dotnet/roslyn/issues/16947")]
        public void Dynamic002()
        {
            var source =
@"

public class C 
{
    public void M() 
    {
        dynamic d = ""qq"";

        F(ref d);
    }

    public static ref dynamic F(ref dynamic d)
    {
        return ref d[0];
    }
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (14,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref d[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d[0]").WithLocation(14, 20)
            );
        }

        [WorkItem(16947, "https://github.com/dotnet/roslyn/issues/16947")]
        public void Dynamic003()
        {
            var source =
@"

public class C 
{
    public void M() 
    {
        dynamic d = ""qq"";

        F(ref d);
    }

    public static ref dynamic F(ref dynamic d)
    {
        return ref G(ref d.Length);
    }

    public static ref dynamic G(ref dynamic d)
    {
        return ref d;
    }
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (14,28): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref G(ref d.Length);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d.Length").WithLocation(14, 28),
                // (14,20): error CS8164: Cannot return by reference a result of 'C.G(ref dynamic)' because the argument passed to parameter 'd' cannot be returned by reference
                //         return ref G(ref d.Length);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "G(ref d.Length)").WithArguments("C.G(ref dynamic)", "d").WithLocation(14, 20)

            );
        }

        [Fact]
        public void RefReturnVarianceDelegate()
        {
            var source = @"
using System;

delegate ref T RefFunc1<T>();
delegate ref T RefFunc2<in T>();
delegate ref T RefFunc3<out T>();

delegate ref Action<T> RefFunc1a<T>();
delegate ref Action<T> RefFunc2a<in T>();
delegate ref Action<T> RefFunc3a<out T>();
         
delegate ref Func<T> RefFunc1f<T>();
delegate ref Func<T> RefFunc2f<in T>();
delegate ref Func<T> RefFunc3f<out T>();

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (6,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc3<T>.Invoke()'. 'T' is covariant.
                // delegate ref T RefFunc3<out T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("RefFunc3<T>.Invoke()", "T", "covariant", "invariantly").WithLocation(6, 10),
                // (5,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc2<T>.Invoke()'. 'T' is contravariant.
                // delegate ref T RefFunc2<in T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("RefFunc2<T>.Invoke()", "T", "contravariant", "invariantly").WithLocation(5, 10),
                // (14,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc3f<T>.Invoke()'. 'T' is covariant.
                // delegate ref Func<T> RefFunc3f<out T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("RefFunc3f<T>.Invoke()", "T", "covariant", "invariantly").WithLocation(14, 10),
                // (13,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc2f<T>.Invoke()'. 'T' is contravariant.
                // delegate ref Func<T> RefFunc2f<in T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("RefFunc2f<T>.Invoke()", "T", "contravariant", "invariantly").WithLocation(13, 10),
                // (10,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc3a<T>.Invoke()'. 'T' is covariant.
                // delegate ref Action<T> RefFunc3a<out T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("RefFunc3a<T>.Invoke()", "T", "covariant", "invariantly").WithLocation(10, 10),
                // (9,10): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'RefFunc2a<T>.Invoke()'. 'T' is contravariant.
                // delegate ref Action<T> RefFunc2a<in T>();
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("RefFunc2a<T>.Invoke()", "T", "contravariant", "invariantly").WithLocation(9, 10)

            );
        }

        [Fact]
        public void RefReturnVarianceMethod()
        {
            var source = @"
using System;

interface IM1<T> { ref T RefMethod(); }
interface IM2<in T> { ref T RefMethod(); }
interface IM3<out T> { ref T RefMethod(); }

interface IM1a<T> { ref Action<T> RefMethod(); }
interface IM2a<in T> { ref Action<T> RefMethod(); }
interface IM3a<out T> { ref Action<T> RefMethod(); }

interface IM1f<T> { ref Func<T> RefMethod(); }
interface IM2f<in T> { ref Func<T> RefMethod(); }
interface IM3f<out T> { ref Func<T> RefMethod(); }

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (6,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM3<T>.RefMethod()'. 'T' is covariant.
                // interface IM3<out T> { ref T RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IM3<T>.RefMethod()", "T", "covariant", "invariantly").WithLocation(6, 24),
                // (10,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM3a<T>.RefMethod()'. 'T' is covariant.
                // interface IM3a<out T> { ref Action<T> RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IM3a<T>.RefMethod()", "T", "covariant", "invariantly").WithLocation(10, 25),
                // (9,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM2a<T>.RefMethod()'. 'T' is contravariant.
                // interface IM2a<in T> { ref Action<T> RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IM2a<T>.RefMethod()", "T", "contravariant", "invariantly").WithLocation(9, 24),
                // (13,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM2f<T>.RefMethod()'. 'T' is contravariant.
                // interface IM2f<in T> { ref Func<T> RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IM2f<T>.RefMethod()", "T", "contravariant", "invariantly").WithLocation(13, 24),
                // (14,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM3f<T>.RefMethod()'. 'T' is covariant.
                // interface IM3f<out T> { ref Func<T> RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IM3f<T>.RefMethod()", "T", "covariant", "invariantly").WithLocation(14, 25),
                // (5,23): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IM2<T>.RefMethod()'. 'T' is contravariant.
                // interface IM2<in T> { ref T RefMethod(); }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IM2<T>.RefMethod()", "T", "contravariant", "invariantly").WithLocation(5, 23)

            );
        }

        [Fact]
        public void RefReturnVarianceProperty()
        {
            var source = @"
using System;

interface IP1<T> { ref T RefProp{get;} }
interface IP2<in T> { ref T RefProp{get;} }
interface IP3<out T> { ref T RefProp{get;} }

interface IP1a<T> { ref Action<T> RefProp{get;} }
interface IP2a<in T> { ref Action<T> RefProp{get;} }
interface IP3a<out T> { ref Action<T> RefProp{get;} }

interface IP1f<T> { ref Func<T> RefProp{get;} }
interface IP2f<in T> { ref Func<T> RefProp{get;} }
interface IP3f<out T> { ref Func<T> RefProp{get;} }

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (5,23): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2<T>.RefProp'. 'T' is contravariant.
                // interface IP2<in T> { ref T RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IP2<T>.RefProp", "T", "contravariant", "invariantly").WithLocation(5, 23),
                // (13,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2f<T>.RefProp'. 'T' is contravariant.
                // interface IP2f<in T> { ref Func<T> RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IP2f<T>.RefProp", "T", "contravariant", "invariantly").WithLocation(13, 24),
                // (9,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2a<T>.RefProp'. 'T' is contravariant.
                // interface IP2a<in T> { ref Action<T> RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IP2a<T>.RefProp", "T", "contravariant", "invariantly").WithLocation(9, 24),
                // (10,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3a<T>.RefProp'. 'T' is covariant.
                // interface IP3a<out T> { ref Action<T> RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IP3a<T>.RefProp", "T", "covariant", "invariantly").WithLocation(10, 25),
                // (14,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3f<T>.RefProp'. 'T' is covariant.
                // interface IP3f<out T> { ref Func<T> RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IP3f<T>.RefProp", "T", "covariant", "invariantly").WithLocation(14, 25),
                // (6,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3<T>.RefProp'. 'T' is covariant.
                // interface IP3<out T> { ref T RefProp{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IP3<T>.RefProp", "T", "covariant", "invariantly").WithLocation(6, 24)

            );
        }

        [Fact]
        public void RefReturnVarianceIndexer()
        {
            var source = @"
using System;

interface IP1<T> { ref T this[int i]{get;} }
interface IP2<in T> { ref T this[int i]{get;} }
interface IP3<out T> { ref T this[int i]{get;} }

interface IP1a<T> { ref Action<T> this[int i]{get;} }
interface IP2a<in T> { ref Action<T> this[int i]{get;} }
interface IP3a<out T> { ref Action<T> this[int i]{get;} }

interface IP1f<T> { ref Func<T> this[int i]{get;} }
interface IP2f<in T> { ref Func<T> this[int i]{get;} }
interface IP3f<out T> { ref Func<T> this[int i]{get;} }

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (6,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3<T>.this[int]'. 'T' is covariant.
                // interface IP3<out T> { ref T this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IP3<T>.this[int]", "T", "covariant", "invariantly").WithLocation(6, 24),
                // (5,23): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2<T>.this[int]'. 'T' is contravariant.
                // interface IP2<in T> { ref T this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref T").WithArguments("IP2<T>.this[int]", "T", "contravariant", "invariantly").WithLocation(5, 23),
                // (9,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2a<T>.this[int]'. 'T' is contravariant.
                // interface IP2a<in T> { ref Action<T> this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IP2a<T>.this[int]", "T", "contravariant", "invariantly").WithLocation(9, 24),
                // (10,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3a<T>.this[int]'. 'T' is covariant.
                // interface IP3a<out T> { ref Action<T> this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Action<T>").WithArguments("IP3a<T>.this[int]", "T", "covariant", "invariantly").WithLocation(10, 25),
                // (13,24): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP2f<T>.this[int]'. 'T' is contravariant.
                // interface IP2f<in T> { ref Func<T> this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IP2f<T>.this[int]", "T", "contravariant", "invariantly").WithLocation(13, 24),
                // (14,25): error CS1961: Invalid variance: The type parameter 'T' must be invariantly valid on 'IP3f<T>.this[int]'. 'T' is covariant.
                // interface IP3f<out T> { ref Func<T> this[int i]{get;} }
                Diagnostic(ErrorCode.ERR_UnexpectedVariance, "ref Func<T>").WithArguments("IP3f<T>.this[int]", "T", "covariant", "invariantly").WithLocation(14, 25)

            );
        }

        [Fact]
        public void RefMethodGroupConversionError()
        {
            var source = @"
using System;

class Program
{
    delegate ref T RefFunc1<T>();

    static void Main()
    {
        RefFunc1<object> f = M1;
        f() = 1;

        f = new RefFunc1<object>(M1);
        f() = 1;
    }

    static ref string M1() => ref new string[]{""qq""}[0];
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (10,30): error CS0407: 'string Program.M1()' has the wrong return type
                //         RefFunc1<object> f = M1;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "string"),
                // (13,34): error CS0407: 'string Program.M1()' has the wrong return type
                //         f = new RefFunc1<object>(M1);
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1()", "string").WithLocation(13, 34)
            );
        }

        [Fact]
        public void RefMethodGroupConversionError_WithResolution()
        {
            var source = @"
using System;

class Base
{
    public static Base Instance = new Base();
}

class Derived1: Base
{
    public static new Derived1 Instance = new Derived1();
}

class Derived2: Derived1
{
}

class Program
{
    delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

    static void Main()
    {
        RefFunc1<Derived2, Base> f = M1;
        System.Console.WriteLine(f(null));
    }

    static ref Base M1(Base arg) => ref Base.Instance;
    static ref Derived1 M1(Derived1 arg) => ref Derived1.Instance;
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (24,38): error CS0407: 'Derived1 Program.M1(Derived1)' has the wrong return type
                //         RefFunc1<Derived2, Base> f = M1;
                Diagnostic(ErrorCode.ERR_BadRetType, "M1").WithArguments("Program.M1(Derived1)", "Derived1").WithLocation(24, 38)
            );
        }

        [Fact]
        public void RefMethodGroupConversionNoError_WithResolution()
        {
            var source = @"
using System;

class Base
{
    public static Base Instance = new Base();
}

class Derived1 : Base
{
    public static new Derived1 Instance = new Derived1();
}

class Derived2 : Derived1
{
    public static new Derived2 Instance = new Derived2();
}

class Program
{
    delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

    static void Main()
    {
        RefFunc1<Derived2, Base> f = M1;
        System.Console.WriteLine(f(null));
    }

    static ref Base M1(Base arg) => throw null;
    static ref Base M1(Derived1 arg) => ref Base.Instance;
}
";

            CompileAndVerify(source, parseOptions: TestOptions.Regular, expectedOutput: "Base", verify: true);
        }

        [Fact]
        public void RefMethodGroupOverloadResolutionErr()
        {
            var source = @"
using System;

class Base
{
    public static Base Instance = new Base();
}

class Derived1: Base
{
    public static new Derived1 Instance = new Derived1();
}

class Derived2: Derived1
{
    public static new Derived2 Instance = new Derived2();
}

class Program
{
    delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

    static void Main()
    {
        Test(M1);
        Test(M3);
    }

    static ref Base M1(Derived1 arg) => ref Base.Instance;
    static ref Base M3(Derived2 arg) => ref Base.Instance;

    static void Test(RefFunc1<Derived2, Base> arg) => Console.WriteLine(arg);
    static void Test(RefFunc1<Derived2, Derived1> arg) => Console.WriteLine(arg);
}

";

            CreateCompilationWithMscorlib45AndCSruntime(source).VerifyEmitDiagnostics(
                // (25,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(Program.RefFunc1<Derived2, Base>)' and 'Program.Test(Program.RefFunc1<Derived2, Derived1>)'
                //         Test(M1);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(Program.RefFunc1<Derived2, Base>)", "Program.Test(Program.RefFunc1<Derived2, Derived1>)").WithLocation(25, 9),
                // (26,9): error CS0121: The call is ambiguous between the following methods or properties: 'Program.Test(Program.RefFunc1<Derived2, Base>)' and 'Program.Test(Program.RefFunc1<Derived2, Derived1>)'
                //         Test(M3);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Program.Test(Program.RefFunc1<Derived2, Base>)", "Program.Test(Program.RefFunc1<Derived2, Derived1>)").WithLocation(26, 9)
            );
        }

        [Fact]
        public void RefMethodGroupOverloadResolution()
        {
            var source = @"
using System;

class Base
{
    public static Base Instance = new Base();
}

class Derived1: Base
{
    public static new Derived1 Instance = new Derived1();
}

class Derived2: Derived1
{
    public static new Derived2 Instance = new Derived2();
}

class Program
{
    delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

    static void Main()
    {
        Test(M2);
    }

    static ref Derived1 M2(Base arg) => ref Derived1.Instance;

    static void Test(RefFunc1<Derived2, Base> arg) => Console.WriteLine(arg);
    static void Test(RefFunc1<Derived2, Derived1> arg) => Console.WriteLine(arg);
}

";

            CompileAndVerify(source, parseOptions: TestOptions.Regular, expectedOutput: "Program+RefFunc1`2[Derived2,Derived1]", verify: true);
        }

        [Fact]
        public void RefLambdaOverloadResolution()
        {
            var source = @"
using System;

class Base
{
    public static Base Instance = new Base();
}

class Derived1: Base
{
    public static new Derived1 Instance = new Derived1();
}

class Derived2: Derived1
{
    public static new Derived2 Instance = new Derived2();
}

class Program
{
    delegate ref TResult RefFunc1<TArg, TResult>(TArg arg);

    static void Main()
    {
        Test((t)=> Base.Instance);
        Test((t)=> ref Base.Instance);
    }

    static void Test(RefFunc1<Derived1, Base> arg) => Console.WriteLine(arg);
    static void Test(Func<Derived1, Base> arg) => Console.WriteLine(arg);
}

";

            CompileAndVerify(source, parseOptions: TestOptions.Regular, expectedOutput: @"System.Func`2[Derived1,Base]
Program+RefFunc1`2[Derived1,Base]", verify: true);
        }

    }
}
