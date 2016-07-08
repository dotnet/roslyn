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
        rl = ref  (new int[1])[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref local;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref P;
    }

    void M1()
    {
        ref int rl = ref new Program().P;
        rl = ref new Program().P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref P;
    }

    void M1(ref Program program)
    {
        ref int rl = ref program.P;
        rl = ref program.P;
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
        rl = ref program.P;
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program.P;
        rl = ref program.P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    void M(T t)
    {
        ref int rl = ref t.P;
        rl = ref t.P;
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t.P;
        rl = ref t.P;
    }
}

class Program3<T>
    where T : struct, I
{
    void M(T t)
    {
        ref int rl = ref t.P;
        rl = ref t.P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref this[0];
    }

    void M1()
    {
        ref int rl = ref new Program()[0];
        rl = ref new Program()[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref this[0];
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
        rl = ref program[0];
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program[0];
        rl = ref program[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    void M(T t)
    {
        ref int rl = ref t[0];
        rl = ref t[0];
    }
}

class Program2<T>
    where T : class, I
{
    void M(T t)
    {
        ref int rl = ref t[0];
        rl = ref t[0];
    }
}

class Program3<T>
    where T : struct, I
{
    void M(T t)
    {
        ref int rl = ref t[0];
        rl = ref t[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref d;
    }

    void M1()
    {
        ref D rl = ref new Program().d;
        rl = ref new Program().d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref i;
    }

    void M1()
    {
        ref int rl = ref new Program().i;
        rl = ref new Program().i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefAssignStructInstanceField()
        {
            var text = @"
struct Program
{
    public int i;

    public Program(int i)
    {
        this.i = i;
    }
}

class Program3
{
    Program program = default(Program);

    void M()
    {
        ref int rl = ref program.i;
        rl = ref program.i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M();
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M();
        return ref rl;
    }

    ref int M1()
    {
        ref int rl = ref new Program().M();
        rl = ref new Program().M();
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M();
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        rl = ref program.M();
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M()
    {
        ref int rl = ref program.M();
        rl = ref program.M();
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t)
    {
        ref int rl = ref t.M();
        rl = ref t.M();
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t)
    {
        ref int rl = ref t.M();
        rl = ref t.M();
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    ref int M(T t)
    {
        ref int rl = ref t.M();
        rl = ref t.M();
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }

    ref int M1(ref int i, ref int j, object o)
    {
        ref int rl = ref new Program().M(ref i, ref j, o);
        rl = ref new Program().M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref M(ref i, ref j, o);
        return ref rl;
    }
}

struct Program2
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3
{
    Program program;

    ref int M(ref int i, ref int j, object o)
    {
        ref int rl = ref program.M(ref i, ref j, o);
        rl = ref program.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program2<T>
    where T : class, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}

class Program3<T>
    where T : struct, I
{
    ref int M(T t, ref int i, ref int j, object o)
    {
        ref int rl = ref t.M(ref i, ref j, o);
        rl = ref t.M(ref i, ref j, o);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref d();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        rl = ref d(ref i, ref j, o);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefAssignRefAssign()
        {
            var text = @"
class Program
{
    static int field = 0;

    static void M()
    {
        ref int a, b, c, d, e, f;
        a = ref b = ref c = ref d = ref e = ref f = ref field;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefAssignsAreVariables()
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

    static unsafe ref int Main()
    {
        ref int rl;
        (rl = ref field) = 0;
        (rl = ref field) += 1;
        (rl = ref field)++;
        M(ref (rl = ref field));
        N(out (rl = ref field));
        fixed (int* i = &(rl = ref field)) { }
        var tr = __makeref((rl = ref field));
        return ref (rl = ref field);
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();           
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

    static unsafe ref int Main()
    {
        ref int rl = ref field;
        rl = 0;
        rl += 1;
        rl++;
        M(ref rl);
        N(out rl);
        fixed (int* i = &rl) { }
        var tr = __makeref(rl);
        return ref rl;
    }
}
";

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void BadRefAssignParameter()
        {
            var text = @"
class Program
{
    static void M(int i)
    {
        ref int rl = ref i;
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,26): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(6, 26),
                // (7,18): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(7, 18));
        }

        [Fact]
        public void BadRefAssignLocal()
        {
            var text = @"
class Program
{
    static void M()
    {
        int i = 0;
        ref int rl = ref i;
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,26): error CS8914: Cannot return or assign a reference to local 'i' because it is not a ref local
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(7, 26),
                // (8,18): error CS8914: Cannot return or assign a reference to local 'i' because it is not a ref local
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(8, 18));
        }

        [Fact]
        public void BadRefAssignByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static void M()
    {
        ref int rl = ref P;
        rl = ref P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,26): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         ref int rl = ref P;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P").WithArguments("Program.P").WithLocation(8, 26),
                // (9,18): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         rl = ref P;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P").WithArguments("Program.P").WithLocation(9, 18));
        }

        [Fact]
        public void BadRefAssignByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    void M()
    {
        ref int rl = ref this[0];
        rl = ref this[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,26): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         ref int rl = ref this[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "this[0]").WithArguments("Program.this[int]").WithLocation(8, 26),
                // (9,18): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         rl = ref this[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "this[0]").WithArguments("Program.this[int]").WithLocation(9, 18));
        }

        [Fact]
        public void BadRefAssignNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    void M()
    {
        ref int rl = ref d;
        rl = ref d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,26): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         ref int rl = ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 26),
                // (11,18): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         rl = ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(11, 18));
        }

        [Fact]
        public void BadRefAssignEventReceiver()
        {
            var text = @"
delegate void D();

struct Program
{
    event D d;

    void M()
    {
        ref D rl = ref d;
        rl = ref d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,24): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         ref D rl = ref d;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "d").WithArguments("this").WithLocation(10, 24),
                // (10,24): error CS8916: Cannot return or assign a reference to 'Program.d' because its receiver may not be returned or assigned by reference
                //         ref D rl = ref d;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "d").WithArguments("Program.d").WithLocation(10, 24),
                // (11,18): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         rl = ref d;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "d").WithArguments("this").WithLocation(11, 18),
                // (11,18): error CS8916: Cannot return or assign a reference to 'Program.d' because its receiver may not be returned or assigned by reference
                //         rl = ref d;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "d").WithArguments("Program.d").WithLocation(11, 18));
        }

        [Fact]
        public void BadRefAssignReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    void M()
    {
        ref int rl = ref i;
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,26): error CS8906: A readonly field cannot be returned by reference
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i").WithLocation(8, 26),
                // (9,18): error CS8906: A readonly field cannot be returned by reference
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i").WithLocation(9, 18));
        }

        [Fact]
        public void BadRefAssignFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    void M()
    {
        ref int rl = ref i;
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (13,26): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("this").WithLocation(13, 26),
                // (13,26): error CS8916: Cannot return or assign a reference to 'Program.i' because its receiver may not be returned or assigned by reference
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "i").WithArguments("Program.i").WithLocation(13, 26),
                // (14,18): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("this").WithLocation(14, 18),
                // (14,18): error CS8916: Cannot return or assign a reference to 'Program.i' because its receiver may not be returned or assigned by reference
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "i").WithArguments("Program.i").WithLocation(14, 18));
        }

        [Fact]
        public void BadRefAssignByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static void M()
    {
        ref int rl = ref L();
        rl = ref L();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (11,26): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         ref int rl = ref L();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "L()").WithLocation(11, 26),
                // (12,18): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         rl = ref L();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "L()").WithLocation(12, 18));
        }

        [Fact]
        public void BadRefAssignByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static void M(D d)
    {
        ref int rl = ref d();
        rl = ref d();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,26): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         ref int rl = ref d();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d()").WithLocation(8, 26),
                // (9,18): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         rl = ref d();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d()").WithLocation(9, 18));
        }

        [Fact]
        public void BadRefAssignDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static void M(D d, int i, int j, object o)
    {
        ref int rl = ref d(ref i, ref j, o);
        rl = ref d(ref i, ref j, o);
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,32): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         ref int rl = ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(8, 32),
                // (8,26): error CS8910: Cannot return or assign a reference to the result of 'D.Invoke(ref int, ref int, object)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         ref int rl = ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "d(ref i, ref j, o)").WithArguments("D.Invoke(ref int, ref int, object)", "i").WithLocation(8, 26),
                // (9,24): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         rl = ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(9, 24),
                // (9,18): error CS8910: Cannot return or assign a reference to the result of 'D.Invoke(ref int, ref int, object)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         rl = ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "d(ref i, ref j, o)").WithArguments("D.Invoke(ref int, ref int, object)", "i").WithLocation(9, 18));
        }

        [Fact]
        public void BadRefAssignCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        ref int rl = ref M(ref j);
        rl = ref M(ref j);
        return ref rl;
    }
}
";


            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,32): error CS8914: Cannot return or assign a reference to local 'j' because it is not a ref local
                //         ref int rl = ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "j").WithArguments("j").WithLocation(7, 32),
                // (7,26): error CS8910: Cannot return or assign a reference to the result of 'Program.M(ref int)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         ref int rl = ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref j)").WithArguments("Program.M(ref int)", "i").WithLocation(7, 26),
                // (8,24): error CS8914: Cannot return or assign a reference to local 'j' because it is not a ref local
                //         rl = ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "j").WithArguments("j").WithLocation(8, 24),
                // (8,18): error CS8910: Cannot return or assign a reference to the result of 'Program.M(ref int)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         rl = ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref j)").WithArguments("Program.M(ref int)", "i").WithLocation(8, 18));
        }

        [Fact]
        public void BadRefAssignStructThis()
        {
            var text = @"
struct Program
{
    void M()
    {
        ref Program rl = ref this;
        rl = ref this;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,30): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         ref Program rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(6, 30),
                // (7,18): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(7, 18));
        }

        [Fact]
        public void BadRefAssignThisReference()
        {
            var text = @"
class Program
{
    void M()
    {
        ref int rl = ref this;
        rl = ref this;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,26): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         ref int rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(6, 26),
                // (7,18): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         rl = ref this;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(7, 18));
        }

        [Fact]
        public void BadRefAssignWrongType()
        {
            var text = @"
class Program
{
    void M(ref long i)
    {
        ref int rl = ref i;
        rl = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,26): error CS8922: The expression must be of type 'int' because it is being assigned by reference
                //         ref int rl = ref i;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 26),
                // (7,18): error CS8922: The expression must be of type 'int' because it is being assigned by reference
                //         rl = ref i;
                Diagnostic(ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(7, 18));
        }

        [Fact]
        public void BadRefLocalCapturedInAnonymousMethod()
        {
            var text = @"
using System.Linq;

delegate int D();

class Program
{
    static int field = 0;

    static void M()
    {
        ref int rl = ref field;
        var d = new D(delegate { return rl; });
        d = new D(() => rl);
        rl = (from v in new int[10] where v > rl select r1).Single();
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                // (13,41): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         var d = new D(delegate { return rl; });
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(13, 41),
                // (14,25): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         d = new D(() => rl);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(14, 25),
                // (15,47): error CS8930: Cannot use ref local 'rl' inside an anonymous method, lambda expression, or query expression
                //         rl = (from v in new int[10] where v > rl select r1).Single();
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUseLocal, "rl").WithArguments("rl").WithLocation(15, 47));
        }

        [Fact]
        public void BadRefLocalInAsyncMethod()
        {
            var text = @"
class Program
{
    static int field = 0;

    static async void Foo()
    {
        ref int i = ref field;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,17): error CS8932: Async methods cannot have by reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadAsyncLocalType, "i = ref field").WithLocation(8, 17),
                // (6,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     static async void Foo()
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "Foo").WithLocation(6, 23));
        }

        [Fact]
        public void BadRefLocalInIteratorMethod()
        {
            var text = @"
using System.Collections;

class Program
{
    static int field = 0;

    static IEnumerable ObjEnumerable()
    {
        ref int i = ref field;
        yield return new object();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,17): error CS8931: Iterators cannot have by reference locals
                //         ref int i = ref field;
                Diagnostic(ErrorCode.ERR_BadIteratorLocalType, "i").WithLocation(10, 17));
        }

        [Fact]
        public void BadRefAssignByValueLocal()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        int l = ref i;
        l = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,13): error CS8922: Cannot initialize a by-value variable with a reference
                //         int l = ref i;
                Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "l = ref i").WithLocation(6, 13),
                // (7,9): error CS8920: 'l' cannot be assigned a reference because it is not a by-reference local
                //         l = ref i;
                Diagnostic(ErrorCode.ERR_MustBeRefAssignable, "l").WithArguments("l").WithLocation(7, 9));
        }

        [Fact]
        public void BadRefAssignOther()
        {
            var text = @"
class Program
{
    static void M(ref int i)
    {
        int[] arr = new int[1];
        arr[0] = ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,9): error CS8921: Expected a by-reference local
                //         arr[0] = ref i;
                Diagnostic(ErrorCode.ERR_MustBeRefAssignableLocal, "arr[0]").WithLocation(7, 9));
        }

        [Fact]
        public void BadByValueInitRefLocal()
        {
            var text = @"
class Program
{
    static void M(int i)
    {
        ref int rl = i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,17): error CS8921: Cannot initialize a by-reference variable with a value
                //         ref int rl = i;
                Diagnostic(ErrorCode.ERR_InitializeByReferenceVariableWithValue, "rl = i").WithLocation(6, 17));
        }

        [Fact]
        public void BadRefLocalUseBeforeDef()
        {
            var text = @"
class Program
{
    static void M(int i, ref int j, out int k, bool b)
    {
        ref int rl, rm;
        if (b)
        {
            rl = ref j;
        }
        rl = i;
        rl = ref rm;
        rl = ref k;
        k = 1;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (11,9): error CS0165: Use of unassigned local variable 'rl'
                //         rl = i;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "rl").WithArguments("rl").WithLocation(11, 9),
                // (12,18): error CS0165: Use of unassigned local variable 'rm'
                //         rl = ref rm;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "rm").WithArguments("rm").WithLocation(12, 18),
                // (13,18): error CS0269: Use of unassigned out parameter 'k'
                //         rl = ref k;
                Diagnostic(ErrorCode.ERR_UseDefViolationOut, "k").WithArguments("k").WithLocation(13, 18));
        }
    }
}
