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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
        return ref local;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReturnRefAssign()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        ref int local;
        return ref (local = ref i);
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t)
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
    ref int M(T t)
    {
        return ref t.P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t)
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
    ref int M(T t)
    {
        return ref t[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefReturnStructInstanceField()
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

class Program2
{
    Program program = default(Program);

    ref int M()
    {
        return ref program.i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t)
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
    ref int M(T t)
    {
        return ref t.M();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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
    ref int M(T t, ref int i, ref int j, object o)
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
    ref int M(T t, ref int i, ref int j, object o)
    {
        return ref t.M(ref i, ref j, o);
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text).VerifyDiagnostics();
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

            CreateCompilationWithMscorlib(text, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics();
        }

        [Fact]
        public void BadRefReturnParameter()
        {
            var text = @"
class Program
{
    static ref int M(int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,20): error CS8911: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnLocal()
        {
            var text = @"
class Program
{
    static ref int M()
    {
        int i = 0;
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,20): error CS8913: Cannot return or assign a reference to local 'i' because it is not a ref local
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("i").WithLocation(7, 20));
        }

        [Fact]
        public void BadRefReturnByValueProperty()
        {
            var text = @"
class Program
{
    static int P { get; set; }

    static ref int M()
    {
        return ref P;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref P;
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "P").WithArguments("Program.P").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnByValueIndexer()
        {
            var text = @"
class Program
{
    int this[int i] { get { return 0; } }

    ref int M()
    {
        return ref this[0];
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref this[0];
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "this[0]").WithArguments("Program.this[int]").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnNonFieldEvent()
        {
            var text = @"
delegate void D();

class Program
{
    event D d { add { } remove { } }

    ref int M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,20): error CS0079: The event 'Program.d' can only appear on the left hand side of += or -=
                //         return ref d;
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "d").WithArguments("Program.d").WithLocation(10, 20));
        }

        [Fact]
        public void BadRefReturnEventReceiver()
        {
            var text = @"
delegate void D();

struct Program
{
    event D d;

    ref D M()
    {
        return ref d;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (10,20): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         return ref d;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "d").WithArguments("this").WithLocation(10, 20),
                // (10,20): error CS8916: Cannot return or assign a reference to 'Program.d' because its receiver may not be returned or assigned by reference
                //         return ref d;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "d").WithArguments("Program.d").WithLocation(10, 20));
        }

        [Fact]
        public void BadRefReturnReadonlyField()
        {
            var text = @"
class Program
{
    readonly int i = 0;

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,20): error CS8905: A readonly field cannot be returned by reference
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReadonly, "i").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnFieldReceiver()
        {
            var text = @"
struct Program
{
    int i;

    Program(int i)
    {
        this.i = i;
    }

    ref int M()
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (13,20): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "i").WithArguments("this").WithLocation(13, 20),
                // (13,20): error CS8916: Cannot return or assign a reference to 'Program.i' because its receiver may not be returned or assigned by reference
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnReceiver, "i").WithArguments("Program.i").WithLocation(13, 20));
        }

        [Fact]
        public void BadRefReturnByValueCall()
        {
            var text = @"
class Program
{
    static int L()
    {
        return 0;
    }

    static ref int M()
    {
        return ref L();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (11,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref L();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "L()").WithLocation(11, 20));
        }

        [Fact]
        public void BadRefReturnByValueDelegateInvocation()
        {
            var text = @"
delegate int D();

class Program
{
    static ref int M(D d)
    {
        return ref d();
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,20): error CS8900: The argument to a by reference return or assignment must be an assignable variable or a property or call that returns by reference
                //         return ref d();
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "d()").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnDelegateInvocationWithArguments()
        {
            var text = @"
delegate ref int D(ref int i, ref int j, object o);

class Program
{
    static ref int M(D d, int i, int j, object o)
    {
        return ref d(ref i, ref j, o);
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (8,26): error CS8912: Cannot return or assign a reference to parameter 'i' because it is not a ref or out parameter
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnParameter, "i").WithArguments("i").WithLocation(8, 26),
                // (8,20): error CS8910: Cannot return or assign a reference to the result of 'D.Invoke(ref int, ref int, object)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         return ref d(ref i, ref j, o);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "d(ref i, ref j, o)").WithArguments("D.Invoke(ref int, ref int, object)", "i").WithLocation(8, 20));
        }

        [Fact]
        public void BadRefReturnCallArgument()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        int j = 0;
        return ref M(ref j);
    }
}
";


            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,26): error CS8914: Cannot return or assign a reference to local 'j' because it is not a ref local
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "j").WithArguments("j").WithLocation(7, 26),
                // (7,20): error CS8910: Cannot return or assign a reference to the result of 'Program.M(ref int)' because the argument passed to parameter 'i' cannot be returned or assigned by reference
                //         return ref M(ref j);
                Diagnostic(ErrorCode.ERR_RefReturnCall, "M(ref j)").WithArguments("Program.M(ref int)", "i").WithLocation(7, 20));
        }

        [Fact]
        public void BadRefReturnStructThis()
        {
            var text = @"
struct Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
               // (6,20): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
               //         return ref this;
               Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnThisReference()
        {
            var text = @"
class Program
{
    ref Program M()
    {
        return ref this;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,20): error CS8914: Cannot return or assign a reference to local 'this' because it is not a ref local
                //         return ref this;
                Diagnostic(ErrorCode.ERR_RefReturnLocal, "this").WithArguments("this").WithLocation(6, 20));
        }

        [Fact]
        public void BadRefReturnWrongType()
        {
            var text = @"
class Program
{
    ref int M(ref long i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,20): error CS8085: The return expression must be of type 'int' because this method returns by reference.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_RefReturnMustHaveIdentityConversion, "i").WithArguments("int").WithLocation(6, 20));
        }

        [Fact]
        public void BadByRefReturnInByValueReturningMethod()
        {
            var text = @"
class Program
{
    static int M(ref int i)
    {
        return ref i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,9): error CS8083: By-reference returns may only be used in by-reference returning methods.
                //         return ref i;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadByValueReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return i;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,9): error CS8084: By-value returns may only be used in by-value returning methods.
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadEmptyReturnInByRefReturningMethod()
        {
            var text = @"
class Program
{
    static ref int M(ref int i)
    {
        return;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (6,9): error CS8084: By-value returns may only be used in by-value returning methods.
                //         return;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(6, 9));
        }

        [Fact]
        public void BadIteratorReturnInRefReturningMethod()
        {
            var text = @"
using System.Collections;
using System.Collections.Generic;

class C
{
    public ref IEnumerator ObjEnumerator()
    {
        yield return new object();
    }

    public ref IEnumerable ObjEnumerable()
    {
        yield return new object();
    }

    public ref IEnumerator<int> GenEnumerator()
    {
        yield return 0;
    }

    public ref IEnumerable<int> GenEnumerable()
    {
        yield return 0;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
                // (7,28): error CS8089: The body of 'C.ObjEnumerator()' cannot be an iterator block because 'C.ObjEnumerator()' returns by reference
                //     public ref IEnumerator ObjEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerator").WithArguments("C.ObjEnumerator()").WithLocation(7, 28),
                // (12,28): error CS8089: The body of 'C.ObjEnumerable()' cannot be an iterator block because 'C.ObjEnumerable()' returns by reference
                //     public ref IEnumerable ObjEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "ObjEnumerable").WithArguments("C.ObjEnumerable()").WithLocation(12, 28),
                // (17,33): error CS8089: The body of 'C.GenEnumerator()' cannot be an iterator block because 'C.GenEnumerator()' returns by reference
                //     public ref IEnumerator<int> GenEnumerator()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerator").WithArguments("C.GenEnumerator()").WithLocation(17, 33),
                // (22,33): error CS8089: The body of 'C.GenEnumerable()' cannot be an iterator block because 'C.GenEnumerable()' returns by reference
                //     public ref IEnumerable<int> GenEnumerable()
                Diagnostic(ErrorCode.ERR_BadIteratorReturnRef, "GenEnumerable").WithArguments("C.GenEnumerable()").WithLocation(22, 33));
        }

        [Fact]
        public void BadRefReturnInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate ref int D();
delegate ref int E(int i);

class C
{
    static int field = 0;

    static void M()
    {
        Expression<D> d = () => ref field;
        Expression<E> e = (int i) => ref field;
    }
}
";

                CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                    // (13,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                    //         Expression<D> d = () => ref field;
                    Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "() => ref field").WithLocation(13, 27),
                    // (14,27): error CS8090: Lambda expressions that return by reference cannot be converted to expression trees
                    //         Expression<E> e = (int i) => ref field;
                    Diagnostic(ErrorCode.ERR_BadRefReturnExpressionTree, "(int i) => ref field").WithLocation(14, 27));
        }

        [Fact]
        public void BadRefReturningCallInExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

delegate int D(C c);

class C
{
    int field = 0;

    ref int P { get { return ref field; } }
    ref int this[int i] { get { return ref field; } }
    ref int M() { return ref field; }

    static void M1()
    {
        Expression<D> e = c => c.P;
        e = c => c[0];
        e = c => c.M();
    }
}
";

                CreateCompilationWithMscorlibAndSystemCore(text).VerifyDiagnostics(
                    // (16,32): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                    //         Expression<D> e = c => c.P;
                    Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.P").WithLocation(16, 32),
                    // (17,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                    //         e = c => c[0];
                    Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c[0]").WithLocation(17, 18),
                    // (18,18): error CS8091: An expression tree lambda may not contain a call to a method, property, or indexer that returns by reference
                    //         e = c => c.M();
                    Diagnostic(ErrorCode.ERR_RefReturningCallInExpressionTree, "c.M()").WithLocation(18, 18));
        }

        [Fact]
        public void BadRefReturningCallWithAwait()
        {
            var text = @"
using System.Threading.Tasks;

struct S
{
    static S s = new S();

    public static ref S Instance { get { return ref s; } }

    public int Echo(int i)
    {
        return i;
    }
}

class C
{
    ref int Assign(ref int loc, int val)
    {
        loc = val;
        return ref loc;
    }

    public async Task<int> Do(int i)
    {
        if (i == 0)
        {
            return 0;
        }

        int temp = 0;
        var a = S.Instance.Echo(await Do(i - 1));
        var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
        return a + b;
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyEmitDiagnostics(
                // (32,33): error CS8933: 'await' cannot be used in an expression containing a call to 'S.Instance.get' because it returns by reference
                //         var a = S.Instance.Echo(await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Do(i - 1)").WithArguments("S.Instance.get").WithLocation(32, 33),
                // (33,49): error CS8933: 'await' cannot be used in an expression containing a call to 'C.Assign(ref int, int)' because it returns by reference
                //         var b = Assign(ref Assign(ref temp, 0), await Do(i - 1));
                Diagnostic(ErrorCode.ERR_RefReturningCallAndAwait, "await Do(i - 1)").WithArguments("C.Assign(ref int, int)").WithLocation(33, 49));
        }
    }
}
