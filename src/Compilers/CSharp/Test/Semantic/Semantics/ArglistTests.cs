// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

// "__arglist" is an undocumented keyword of the C# language. 
//
// There are three places where __arglist may legally appear in C#. It may appear:
//
// 1) as the final "parameter" of a method declaration:
//
// static void M(int x, int y, __arglist) {}
//
// 2) As an expression in a method whose declaration includes an __arglist parameter:
//
// static void M(int x, int y, __arglist) { var ai = new ArgIterator(__arglist); }
//
// 3) As the "receiver" of a "call" syntax in the last position of a call to an __arglist method:
//
// C.M(1, 2, __arglist(3, 4, 5));
//
// THE FIRST FORM
// ---------------
//
// In its first form it may not appear:
//
// * In a generic method
// * In a generic type
// * In an iterator method declaration
// * In a delegate declaration
// * In a user-defined operator or conversion declaration
//
// UNDONE: We should ensure that __arglist methods may not be async.
//
// In metadata, such a method is referred to as a "varargs" method and is identified
// by the calling convention of the method.
//
// THE SECOND FORM
// ---------------
//
// The second form is a legal expression (almost) anywhere inside a method that includes 
// an __arglist parameter. It is an expression of type System.RuntimeArgumentHandle and 
// classified as a value. It is usually passed to the ctor of the ArgIterator type. 
//
// Speaking of which, we should talk about some special types.
//
// RuntimeArgumentHandle, ArgIterator and TypedReference are "restricted" types:
//
// * A restricted type may not be converted to object.
// * It is illegal to declare a field or property of a restricted type.
// * A restricted type may not be used as a generic type argument.
// * A method or delegate may not return a restricted type.
// * Since a field may not be of a restricted type, a restricted type may not be used 
//   in an anonymous method, lambda or query expression if it would have to be hoisted 
//   to a field.
//
// The native compiler does not consistently enforce these rules. For example,
// it allows:
//
// delegate void D(RuntimeArgumentHandle r);
// static int M(__arglist)
// {
//     D f = null;
//     f = x=>f(__arglist);
// }
//
// Sure enough, C# 5 generates a display class with method:
//
// static int Anonymous(RuntimeArgumentHandle x) { return this.f(__arglist); }
//
// Which doesn't make any sense; the anonymous method is not an __arglist method.
//
// This should simply be illegal; Roslyn disallows __arglist used in the second
// form inside any lambda or anonymous method. (Even if the lambda in question is
// from a query transformation.)
//
// THE THIRD FORM
// --------------
//
// The third form may only appear as the last argument of a call to a varargs method.
//
// UNDONE: The third form may not appear in a method call inside an expression tree lambda.
//
//
// "__reftype" is also an undocumented keyword of C#. It is treated as an operator which
// takes as its sole operand an expression convertible to System.TypedReference. The result is
// the System.Type associated with the type of the typed reference.
//
// "__makeref" is also an undocumented keyword of C#. It is treated as an operator which takes
// as its sole operand an expression classified as a variable. The result is a TypedReference
// to the variable. It is analogous to the "&" address-of operator.
//
// "__refvalue" is also an undocumented keyword of C#. It is badly named, as it has the semantics
// of *dereference to produce a variable*. It is the opposite of the __makeref operator and is
// analogous to the "*" dereference operator. The operator takes a TypedReference and a 
// type, and produces a variable of that type.
//
// 

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ArglistTests : CompilingTestBase
    {
        [Fact]
        public void ExpressionTreeTest()
        {
            var text = @"
using System; 
using System.Linq.Expressions;
public struct C
{
    static void Main()
    {
        Expression<Func<bool>> ex1 = ()=>M(__makeref(S)); // CS7053
        Expression<Func<Type>> ex2 = ()=>__reftype(default(TypedReference));
        Expression<Func<int>> ex3 = ()=>__refvalue(default(TypedReference), int);
        Expression<Func<bool>> ex4 = ()=>N(__arglist());
    }
    static int S = 678;
    public static bool M(TypedReference tr) { return true; }
    public static bool N(__arglist) { return true;}
}";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(text);
            comp.VerifyDiagnostics(
// (8,44): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'TypedReference'.
//         Expression<Func<bool>> ex1 = ()=>M(__makeref(S)); // CS7053
Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "__makeref(S)").WithArguments("TypedReference").WithLocation(8, 44),
// (8,44): error CS7053: An expression tree may not contain '__makeref'
//         Expression<Func<bool>> ex1 = ()=>M(__makeref(S)); // CS7053
Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "__makeref(S)").WithArguments("__makeref").WithLocation(8, 44),
// (9,42): error CS7053: An expression tree may not contain '__reftype'
//         Expression<Func<Type>> ex2 = ()=>__reftype(default(TypedReference));
Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "__reftype(default(TypedReference))").WithArguments("__reftype").WithLocation(9, 42),
// (9,52): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'TypedReference'.
//         Expression<Func<Type>> ex2 = ()=>__reftype(default(TypedReference));
Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default(TypedReference)").WithArguments("TypedReference").WithLocation(9, 52),
// (10,41): error CS7053: An expression tree may not contain '__refvalue'
//         Expression<Func<int>> ex3 = ()=>__refvalue(default(TypedReference), int);
Diagnostic(ErrorCode.ERR_FeatureNotValidInExpressionTree, "__refvalue(default(TypedReference), int)").WithArguments("__refvalue").WithLocation(10, 41),
// (10,52): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'TypedReference'.
//         Expression<Func<int>> ex3 = ()=>__refvalue(default(TypedReference), int);
Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default(TypedReference)").WithArguments("TypedReference").WithLocation(10, 52),
// (11,44): error CS1952: An expression tree lambda may not contain a method with variable arguments
//         Expression<Func<bool>> ex4 = ()=>N(__arglist());
Diagnostic(ErrorCode.ERR_VarArgsInExpressionTree, "__arglist()").WithLocation(11, 44)
                );
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void MakeRefTest01()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        int i = 1;
        Console.WriteLine(M(__makeref(i)));
    }
    static Type M(TypedReference tr)
    {
        return __reftype(tr);
    }
}";

            string expectedIL = @"{
  // Code size       20 (0x14)
  .maxstack  1
  .locals init (int V_0) //i
  IL_0000:  ldc.i4.1
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  mkrefany   ""int""
  IL_0009:  call       ""System.Type C.M(System.TypedReference)""
  IL_000e:  call       ""void System.Console.WriteLine(object)""
  IL_0013:  ret
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: "System.Int32", verify: Verification.FailsILVerify);
            verifier.VerifyIL("C.Main", expectedIL);
        }

        [Fact]
        public void MakeRefTest02()
        {
            // A makeref is logically the same as passing a variable to a method that takes a ref/out parameter,
            // so we produce the same error messages. This differs from the native compiler, which either fails
            // to produce errors at all, or produces the error messages for a bad assignment. We should not produce
            // errors for bad assignments; first of all, making a ref does not do an assignment, and second, the
            // user might assume that it is the assignment to the local that is bad.

            var text = @"
using System;
public struct C
{
    static void Main()
    {
        TypedReference tr1 = default(TypedReference);
        TypedReference tr2 = __makeref(tr1); // CS1601
        TypedReference tr3 = __makeref(123); // CS1510
        TypedReference tr4 = __makeref(P); // CS0206
        TypedReference tr5 = __makeref(R); // CS0199
    }
    static int P { get; set; }
    static readonly int R = 345;
}";

            // UNDONE: Test what happens when __makereffing a volatile field, readonly field, etc.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
    // (8,30): error CS1601: Cannot make reference to variable of type 'TypedReference'
    //         TypedReference tr2 = __makeref(tr1); // CS1601
    Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "__makeref(tr1)").WithArguments("System.TypedReference").WithLocation(8, 30),
    // (9,40): error CS1510: A ref or out value must be an assignable variable
    //         TypedReference tr3 = __makeref(123); // CS1510
    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "123").WithLocation(9, 40),
    // (10,40): error CS0206: A property or indexer may not be passed as an out or ref parameter
    //         TypedReference tr4 = __makeref(P); // CS0206
    Diagnostic(ErrorCode.ERR_RefProperty, "P").WithArguments("C.P").WithLocation(10, 40),
    // (11,40): error CS0199: A static readonly field cannot be used as a ref or out value (except in a static constructor)
    //         TypedReference tr5 = __makeref(R); // CS0199
    Diagnostic(ErrorCode.ERR_RefReadonlyStatic, "R").WithLocation(11, 40)

                );
        }

        [Fact]
        [WorkItem(23369, "https://github.com/dotnet/roslyn/issues/23369")]
        public void ArglistWithVoidMethod()
        {
            var text = @"
public class C
{
    void M()
    {
        M2(__arglist(1, M()));
    }
    void M2(__arglist)
    {
    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (6,25): error CS8361: __arglist cannot have an argument of void type
                //         M2(__arglist(1, M()));
                Diagnostic(ErrorCode.ERR_CantUseVoidInArglist, "M()").WithLocation(6, 25)
                );
        }

        [Fact]
        public void RefValueUnsafeToReturn()
        {
            var text = @"
using System;

class C
{
    private static ref int Test()
    {
        int aa = 42;
        var tr = __makeref(aa);

        ref var r = ref Test2(ref __refvalue(tr, int));

        return ref r;
    }

    private static ref int Test2(ref int r)
    {
        return ref r;
    }

    private static ref int Test3(TypedReference tr)
    {
        return ref __refvalue(tr, int);
    }
}";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (13,20): error CS8157: Cannot return 'r' by reference because it was initialized to a value that cannot be returned by reference
                //         return ref r;
                Diagnostic(ErrorCode.ERR_RefReturnNonreturnableLocal, "r").WithArguments("r").WithLocation(13, 20),
                // (23,20): error CS8156: An expression cannot be used in this context because it may not be returned by reference
                //         return ref __refvalue(tr, int);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "__refvalue(tr, int)").WithLocation(23, 20)

                );
        }

        [Fact]
        public void MakeRefTest03_Dynamic_Bind()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        dynamic i = 1;
        Console.WriteLine(M(__makeref(i)));
    }
    static Type M(TypedReference tr)
    {
        return __reftype(tr);
    }
}";

            CreateCompilation(text).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void RefTypeTest01()
        {
            var text = @"
using System;
using System.Reflection;
public struct C
{
    public string f;
    static void Main()
    {
        Type ctype = typeof(C);
        FieldInfo[] ffield = new FieldInfo[] {ctype.GetFields()[0] };
        TypedReference tr = TypedReference.MakeTypedReference(new C(), ffield);
        Type type = M(tr);
        Console.WriteLine(type.ToString());
    }
    static Type M(TypedReference tr)
    {
        return __reftype(tr);
    }

}";

            string expectedIL = @"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  refanytype
  IL_0003:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_0008:  ret
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: "System.String", verify: Verification.FailsILVerify);
            verifier.VerifyIL("C.M", expectedIL);
        }

        [Fact]
        public void RefTypeTest02()
        {
            var text = @"
public struct C
{
    static void Main()
    {
        System.Type t = __reftype(null);
    }
}";


            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (6,25): error CS0037: Cannot convert null to 'System.TypedReference' because it is a non-nullable value type
//         System.Type t = __reftype(null);
Diagnostic(ErrorCode.ERR_ValueCantBeNull, "__reftype(null)").WithArguments("System.TypedReference")
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArglistTest01()
        {
            var text = @"
using System;
public class C
{
    static void Main()
    {
    }
    
    static void M(__arglist)
    {    
        new ArgIterator(__arglist);
    }
}";

            string expectedIL = @"{
  // Code size        9 (0x9)
  .maxstack  1
  IL_0000:  arglist
  IL_0002:  newobj     ""System.ArgIterator..ctor(System.RuntimeArgumentHandle)""
  IL_0007:  pop
  IL_0008:  ret
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: "");
            verifier.VerifyIL("C.M(__arglist)", expectedIL);
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArglistTest02()
        {
            var text = @"
using System;
public class C
{
    static void Main()
    {
        M(1, __arglist(2, 3, true));
    }
    
    static void M(int x, __arglist)
    {    
        Console.Write(x);
        DumpArgs(new ArgIterator(__arglist));
        new B(4);
        new D(6);
    }

    static void DumpArgs(ArgIterator args)
    {
        while(args.GetRemainingCount() > 0)
        {
            TypedReference tr = args.GetNextArg();
            object arg = TypedReference.ToObject(tr);
            Console.Write(arg);
        }
    }

    static void M(uint x, __arglist)
    {    
    }

    class B
    {
        public B(__arglist)
        {    
            DumpArgs(new ArgIterator(__arglist)); 
        }
        public B(int x) : this(__arglist(x, 5)) {}
    }
    class D : B
    {
        public D(int x) : base(__arglist(x, 7)) {}
    }

}";

            // Note that this IL is not quite right; here we are displaying the call as "void C.M(int, __arglist)".
            // The actual IL for this program should show the method ref as "void C.M(int, ..., int, int, bool)",
            // because that is the information that is actually encoded in the method ref. If we want to display
            // that then we'll need to add special code to the symbol display visitor that knows how to emit
            // the desired format.

            string expectedIL = @"{
  // Code size       10 (0xa)
  .maxstack  4
  IL_0000:  ldc.i4.1
  IL_0001:  ldc.i4.2
  IL_0002:  ldc.i4.3
  IL_0003:  ldc.i4.1
  IL_0004:  call       ""void C.M(int, __arglist) with __arglist( int, int, bool)""
  IL_0009:  ret
}
";
            string expectedOutput = @"123True4567";
            var verifier = CompileAndVerify(source: text, expectedOutput: expectedOutput);
            verifier.VerifyIL("C.Main", expectedIL);
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArglistTest03()
        {
            // The native parser produces "type expected" when __arglist is preceded by an illegal
            // modifier. The Roslyn compiler produces the more informative "__arglist not valid" error.

            var text = @"
static class C
{
    static void M(int x, __arglist) {}
    static void N(params __arglist) {}
    static void O(ref __arglist) {}
    static void P(out __arglist) {}
    static void Q(this __arglist) {}
    static void Main()
    {
        M(1);
        M(2, 3);
        M(4, 5, 6);
        M(1, __arglist()); // no error
        M(1, __arglist(__arglist()));
        var x = __arglist(123);
    }
    static object R()
    {
        return __arglist(456);
    }
    static void S(int x)
    {
        S(__arglist(1));
    }
    
    [MyAttribute(__arglist(2))]
    static void T() 
    {
        object obj1 = new System.TypedReference();
        object obj2 = (object)new System.ArgIterator();
        // The native compiler produces:
        //'TypedReference' may not be used as a type argument
        // which is not a very descriptive error! There is no type argument here;
        // the fact that anonymous types are actually generic is an implementation detail.
        // Roslyn produces the far more sensible error:
        // cannot assign TypedReference to anonymous type property
        object obj3 = new { X = new System.TypedReference() };
    }
}

public class MyAttribute : System.Attribute
{    
  public MyAttribute(__arglist) { }
}
";

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
                // (5,26): error CS1669: __arglist is not valid in this context
                //     static void N(params __arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(5, 26),
                // (6,23): error CS1669: __arglist is not valid in this context
                //     static void O(ref __arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(6, 23),
                // (7,23): error CS1669: __arglist is not valid in this context
                //     static void P(out __arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(7, 23),
                // (8,24): error CS1669: __arglist is not valid in this context
                //     static void Q(this __arglist) {}
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist").WithLocation(8, 24),
                // (11,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.M(int, __arglist)'
                //         M(1);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "C.M(int, __arglist)").WithLocation(11, 9),
                // (12,14): error CS1503: Argument 2: cannot convert from 'int' to '__arglist'
                //         M(2, 3);
                Diagnostic(ErrorCode.ERR_BadArgType, "3").WithArguments("2", "int", "__arglist").WithLocation(12, 14),
                // (13,9): error CS1501: No overload for method 'M' takes 3 arguments
                //         M(4, 5, 6);
                Diagnostic(ErrorCode.ERR_BadArgCount, "M").WithArguments("M", "3").WithLocation(13, 9),
                // (15,24): error CS0226: An __arglist expression may only appear inside of a call or new expression
                //         M(1, __arglist(__arglist()));
                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist()").WithLocation(15, 24),
                // (16,17): error CS0226: An __arglist expression may only appear inside of a call or new expression
                //         var x = __arglist(123);
                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist(123)").WithLocation(16, 17),
                // (20,16): error CS0226: An __arglist expression may only appear inside of a call or new expression
                //         return __arglist(456);
                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist(456)").WithLocation(20, 16),
                // (24,11): error CS1503: Argument 1: cannot convert from '__arglist' to 'int'
                //         S(__arglist(1));
                Diagnostic(ErrorCode.ERR_BadArgType, "__arglist(1)").WithArguments("1", "__arglist", "int").WithLocation(24, 11),
                // (27,18): error CS0226: An __arglist expression may only appear inside of a call or new expression
                //     [MyAttribute(__arglist(2))]
                Diagnostic(ErrorCode.ERR_IllegalArglist, "__arglist(2)").WithLocation(27, 18),
                // (30,23): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'object'
                //         object obj1 = new System.TypedReference();
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "new System.TypedReference()").WithArguments("System.TypedReference", "object").WithLocation(30, 23),
                // (31,23): error CS0030: Cannot convert type 'System.ArgIterator' to 'object'
                //         object obj2 = (object)new System.ArgIterator();
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "(object)new System.ArgIterator()").WithArguments("System.ArgIterator", "object").WithLocation(31, 23),
                // (38,29): error CS0828: Cannot assign System.TypedReference to anonymous type property
                //         object obj3 = new { X = new System.TypedReference() };
                Diagnostic(ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue, "X = new System.TypedReference()").WithArguments("System.TypedReference").WithLocation(38, 29));
        }

        [Fact]
        public void ArglistTest04()
        {
            var text = @"
using System;

class @error
{
    static void Main() {
		Action a = delegate (__arglist) { };
	}
}
";

            CreateCompilation(text).VerifyDiagnostics(
                // (7,24): error CS1669: __arglist is not valid in this context
                // 		Action a = delegate (__arglist) { };
                Diagnostic(ErrorCode.ERR_IllegalVarArgs, "__arglist"));
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void RefValueTest01()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        int i = 1;
        TypedReference tr = __makeref(i);
        Console.Write(i);
        Get(tr);
        Console.Write(i);
        Set(tr, 2);
        Console.Write(i);
        Ref(tr, 3);
        Console.Write(i);
    }
    static int Get(TypedReference tr)
    {
        return __refvalue(tr, int);
    }
    static void Set(TypedReference tr, int i)
    {
        __refvalue(tr, int) = i;
    }
    static void Ref(TypedReference tr, int i)
    {
        // The native compiler generates bad code for this; Roslyn gets it right.
        M(ref __refvalue(tr, int), i);
    }
    static void M(ref int x, int y)
    {
        x = y;
    }
}";

            string expectedGetIL = @"{
  // Code size        8 (0x8)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  refanyval  ""int""
  IL_0006:  ldind.i4
  IL_0007:  ret
}";

            string expectedSetIL = @"{
  // Code size        9 (0x9)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  refanyval  ""int""
  IL_0006:  ldarg.1
  IL_0007:  stind.i4
  IL_0008:  ret
}";

            string expectedRefIL = @"{
  // Code size       13 (0xd)
  .maxstack  2
  IL_0000:  ldarg.0
  IL_0001:  refanyval  ""int""
  IL_0006:  ldarg.1
  IL_0007:  call       ""void C.M(ref int, int)""
  IL_000c:  ret
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: "1123", verify: Verification.FailsILVerify);
            verifier.VerifyIL("C.Get", expectedGetIL);
            verifier.VerifyIL("C.Set", expectedSetIL);
            verifier.VerifyIL("C.Ref", expectedRefIL);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void RefValueTest01a()
        {
            var text = @"
using System;
    class Program
    {
        struct S1<T>
        {
            public T x;

            public void Assign(T i)
            {
                x = i;
            }
        }

        static void Main(string[] args)
        {
            int x = 0;
            var _ref = __makeref(x);
            __refvalue(_ref, int) = 42;
            System.Console.WriteLine(x);

            S1<int> s = new S1<int>();
            _ref = __makeref(s);
            __refvalue(_ref, S1<int>).Assign(333);
            System.Console.WriteLine(s.x);

            __refvalue(_ref, S1<int>).x = 42;
            System.Console.WriteLine(s.x);

            S1<S1<int>> s1 = new S1<S1<int>>();
            _ref = __makeref(s1);
            __refvalue(_ref, S1<S1<int>>).x.Assign(333);
            System.Console.WriteLine(s1.x.x);

            __refvalue(_ref, S1<S1<int>>) = default(S1<S1<int>>);
            System.Console.WriteLine(s1.x.x);

            __refvalue(_ref, S1<S1<int>>).x.x = 42;
            System.Console.WriteLine(s1.x.x);

        }
    }
";

            string expectedGetIL = @"
{
  // Code size      202 (0xca)
  .maxstack  3
  .locals init (int V_0, //x
  Program.S1<int> V_1, //s
  Program.S1<Program.S1<int>> V_2) //s1
  IL_0000:  ldc.i4.0
  IL_0001:  stloc.0
  IL_0002:  ldloca.s   V_0
  IL_0004:  mkrefany   ""int""
  IL_0009:  refanyval  ""int""
  IL_000e:  ldc.i4.s   42
  IL_0010:  stind.i4
  IL_0011:  ldloc.0
  IL_0012:  call       ""void System.Console.WriteLine(int)""
  IL_0017:  ldloca.s   V_1
  IL_0019:  initobj    ""Program.S1<int>""
  IL_001f:  ldloca.s   V_1
  IL_0021:  mkrefany   ""Program.S1<int>""
  IL_0026:  dup
  IL_0027:  refanyval  ""Program.S1<int>""
  IL_002c:  ldc.i4     0x14d
  IL_0031:  call       ""void Program.S1<int>.Assign(int)""
  IL_0036:  ldloc.1
  IL_0037:  ldfld      ""int Program.S1<int>.x""
  IL_003c:  call       ""void System.Console.WriteLine(int)""
  IL_0041:  refanyval  ""Program.S1<int>""
  IL_0046:  ldc.i4.s   42
  IL_0048:  stfld      ""int Program.S1<int>.x""
  IL_004d:  ldloc.1
  IL_004e:  ldfld      ""int Program.S1<int>.x""
  IL_0053:  call       ""void System.Console.WriteLine(int)""
  IL_0058:  ldloca.s   V_2
  IL_005a:  initobj    ""Program.S1<Program.S1<int>>""
  IL_0060:  ldloca.s   V_2
  IL_0062:  mkrefany   ""Program.S1<Program.S1<int>>""
  IL_0067:  dup
  IL_0068:  refanyval  ""Program.S1<Program.S1<int>>""
  IL_006d:  ldflda     ""Program.S1<int> Program.S1<Program.S1<int>>.x""
  IL_0072:  ldc.i4     0x14d
  IL_0077:  call       ""void Program.S1<int>.Assign(int)""
  IL_007c:  ldloc.2
  IL_007d:  ldfld      ""Program.S1<int> Program.S1<Program.S1<int>>.x""
  IL_0082:  ldfld      ""int Program.S1<int>.x""
  IL_0087:  call       ""void System.Console.WriteLine(int)""
  IL_008c:  dup
  IL_008d:  refanyval  ""Program.S1<Program.S1<int>>""
  IL_0092:  initobj    ""Program.S1<Program.S1<int>>""
  IL_0098:  ldloc.2
  IL_0099:  ldfld      ""Program.S1<int> Program.S1<Program.S1<int>>.x""
  IL_009e:  ldfld      ""int Program.S1<int>.x""
  IL_00a3:  call       ""void System.Console.WriteLine(int)""
  IL_00a8:  refanyval  ""Program.S1<Program.S1<int>>""
  IL_00ad:  ldflda     ""Program.S1<int> Program.S1<Program.S1<int>>.x""
  IL_00b2:  ldc.i4.s   42
  IL_00b4:  stfld      ""int Program.S1<int>.x""
  IL_00b9:  ldloc.2
  IL_00ba:  ldfld      ""Program.S1<int> Program.S1<Program.S1<int>>.x""
  IL_00bf:  ldfld      ""int Program.S1<int>.x""
  IL_00c4:  call       ""void System.Console.WriteLine(int)""
  IL_00c9:  ret
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: @"42
333
42
333
0
42", verify: Verification.FailsILVerify);
            verifier.VerifyIL("Program.Main", expectedGetIL);
        }

        [Fact]
        public void RefValueTest02()
        {
            var text = @"
using System;
static class C
{
    static void Main()
    {
        int a = 1;
        TypedReference tr = __makeref(a);
        int b = __refvalue(123, int);
        int c = __refvalue(tr, Main);
        int d = __refvalue(tr, double);
        __refvalue(tr, int) = null;
    }
}";

            // The native compiler produces 
            // CS0118: 'C.Main()' is a 'method' but is used like a 'type'
            // instead of
            // CS0246: The type or namespace name 'Main' could not be found
            // The native compiler behavior seems better here; we might consider fixing Roslyn to match.

            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (9,17): error CS0029: Cannot implicitly convert type 'int' to 'System.TypedReference'
//         int b = __refvalue(123, int);
Diagnostic(ErrorCode.ERR_NoImplicitConv, "__refvalue(123, int)").WithArguments("int", "System.TypedReference"),

// (10,32): error CS0246: The type or namespace name 'Main' could not be found (are you missing a using directive or an assembly reference?)
//         int c = __refvalue(tr, Main);
Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Main").WithArguments("Main"),

// (11,17): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
//         int d = __refvalue(tr, double);
Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "__refvalue(tr, double)").WithArguments("double", "int"),

// (12,31): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
//         __refvalue(tr, int) = null;
Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int")
);
        }

        [Fact]
        public void RefValueTest03_Dynamic_Bind()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        dynamic i = 1;
        TypedReference tr = __makeref(i);
        Console.Write(i);
        Get(tr);
        Console.Write(i);
        Set(tr, 2);
        Console.Write(i);
    }
    static dynamic Get(TypedReference tr)
    {
        return __refvalue(tr, dynamic);
    }
    static void Set(TypedReference tr, dynamic i)
    {
        __refvalue(tr, dynamic) = i;
    }
}";

            CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics();
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void RefValueTest04_optimizer()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        int k = 42;

        int i = 1;
        TypedReference tr1 = __makeref(i);

        __refvalue(tr1, int) = k;
        __refvalue(tr1, int) = k;

        int j = 1;
        TypedReference tr2 = __makeref(j);

        int l = 42;

        __refvalue(tr1, int) = l;
        __refvalue(tr2, int) = l;

        Console.Write(i);
        Console.Write(j);
    }
}";

            var verifier = CompileAndVerify(source: text, expectedOutput: "4242", verify: Verification.FailsILVerify);
            verifier.VerifyIL("C.Main", @"
{
  // Code size       72 (0x48)
  .maxstack  3
  .locals init (int V_0, //k
                int V_1, //i
                System.TypedReference V_2, //tr1
                int V_3, //j
                int V_4) //l
  IL_0000:  ldc.i4.s   42
  IL_0002:  stloc.0
  IL_0003:  ldc.i4.1
  IL_0004:  stloc.1
  IL_0005:  ldloca.s   V_1
  IL_0007:  mkrefany   ""int""
  IL_000c:  stloc.2
  IL_000d:  ldloc.2
  IL_000e:  refanyval  ""int""
  IL_0013:  ldloc.0
  IL_0014:  stind.i4
  IL_0015:  ldloc.2
  IL_0016:  refanyval  ""int""
  IL_001b:  ldloc.0
  IL_001c:  stind.i4
  IL_001d:  ldc.i4.1
  IL_001e:  stloc.3
  IL_001f:  ldloca.s   V_3
  IL_0021:  mkrefany   ""int""
  IL_0026:  ldc.i4.s   42
  IL_0028:  stloc.s    V_4
  IL_002a:  ldloc.2
  IL_002b:  refanyval  ""int""
  IL_0030:  ldloc.s    V_4
  IL_0032:  stind.i4
  IL_0033:  refanyval  ""int""
  IL_0038:  ldloc.s    V_4
  IL_003a:  stind.i4
  IL_003b:  ldloc.1
  IL_003c:  call       ""void System.Console.Write(int)""
  IL_0041:  ldloc.3
  IL_0042:  call       ""void System.Console.Write(int)""
  IL_0047:  ret
}
");
        }

        [Fact]
        public void TestBug13263()
        {
            var text = @"public class C { public void M() { var t = __makeref(delegate); } }";
            var tree = Parse(text);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var root = tree.GetCompilationUnitRoot();
            var clss = root.Members[0] as ClassDeclarationSyntax;
            var meth = clss.Members[0] as MethodDeclarationSyntax;
            var stmt = meth.Body.Statements[0] as LocalDeclarationStatementSyntax;
            var type = stmt.Declaration.Type;
            var info = model.GetSymbolInfo(type);
            Assert.Equal("TypedReference", info.Symbol.Name);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void MethodArgListParameterCount()
        {
            var text = @"
class A
{
    public void M1(__arglist) { }
    public void M2(int x, __arglist) { }
    public void M3(__arglist, int x) { } //illegal, but shouldn't break
    public void M4(__arglist, int x, __arglist) { } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var m1 = type.GetMember<MethodSymbol>("M1");
            Assert.Equal(0, m1.ParameterCount);
            Assert.Equal(0, m1.Parameters.Length);

            var m2 = type.GetMember<MethodSymbol>("M2");
            Assert.Equal(1, m2.ParameterCount);
            Assert.Equal(1, m2.Parameters.Length);

            var m3 = type.GetMember<MethodSymbol>("M3");
            Assert.Equal(1, m3.ParameterCount);
            Assert.Equal(1, m3.Parameters.Length);

            var m4 = type.GetMember<MethodSymbol>("M4");
            Assert.Equal(1, m4.ParameterCount);
            Assert.Equal(1, m4.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ILMethodArgListParameterCount()
        {
            var csharp = @"
class Unused
{
}
";

            var il = @"
.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig instance vararg void 
          M1() cil managed
  {
    ret
  }

  .method public hidebysig instance vararg void 
          M2(int32 x) cil managed
  {
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

} // end of class A
";

            var comp = CreateCompilationWithILAndMscorlib40(csharp, il);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var m1 = type.GetMember<MethodSymbol>("M1");
            Assert.Equal(0, m1.ParameterCount);
            Assert.Equal(0, m1.Parameters.Length);

            var m2 = type.GetMember<MethodSymbol>("M2");
            Assert.Equal(1, m2.ParameterCount);
            Assert.Equal(1, m2.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void OperatorArgListParameterCount()
        {
            var text = @"
class A
{
    public int operator +(__arglist) { return 0; } //illegal, but shouldn't break
    public int operator -(A a, __arglist) { return 0; } //illegal, but shouldn't break
    public int operator *(__arglist, A a) { return 0; } //illegal, but shouldn't break
    public int operator /(__arglist, A a, __arglist) { return 0; } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var m1 = type.GetMember<MethodSymbol>(WellKnownMemberNames.UnaryPlusOperatorName);
            Assert.Equal(0, m1.ParameterCount);
            Assert.Equal(0, m1.Parameters.Length);

            var m2 = type.GetMember<MethodSymbol>(WellKnownMemberNames.SubtractionOperatorName);
            Assert.Equal(1, m2.ParameterCount);
            Assert.Equal(1, m2.Parameters.Length);

            var m3 = type.GetMember<MethodSymbol>(WellKnownMemberNames.MultiplyOperatorName);
            Assert.Equal(1, m3.ParameterCount);
            Assert.Equal(1, m3.Parameters.Length);

            var m4 = type.GetMember<MethodSymbol>(WellKnownMemberNames.DivisionOperatorName);
            Assert.Equal(1, m4.ParameterCount);
            Assert.Equal(1, m4.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConversionArgListParameterCount1()
        {
            var text = @"
class A
{
    public explicit operator A(__arglist) { return null; } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var conversion = type.GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(0, conversion.ParameterCount);
            Assert.Equal(0, conversion.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConversionArgListParameterCount2()
        {
            var text = @"
class A
{
    public explicit operator A(int x, __arglist) { return null; } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var conversion = type.GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(1, conversion.ParameterCount);
            Assert.Equal(1, conversion.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConversionArgListParameterCount3()
        {
            var text = @"
class A
{
    public explicit operator A(__arglist, A a) { return null; } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var conversion = type.GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(1, conversion.ParameterCount);
            Assert.Equal(1, conversion.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConversionArgListParameterCount4()
        {
            var text = @"
class A
{
    public explicit operator A(__arglist, A a, __arglist) { return null; } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var type = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A");

            var conversion = type.GetMember<MethodSymbol>(WellKnownMemberNames.ExplicitConversionName);
            Assert.Equal(1, conversion.ParameterCount);
            Assert.Equal(1, conversion.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConstructorArgListParameterCount1()
        {
            var text = @"
class A
{
    public A(__arglist) { }
}
";
            var comp = CreateCompilation(text);

            var constructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(0, constructor.ParameterCount); //doesn't use syntax
            Assert.Equal(0, constructor.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConstructorArgListParameterCount2()
        {
            var text = @"
class A
{
    public A(int x, __arglist) { }
}
";
            var comp = CreateCompilation(text);

            var constructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(1, constructor.ParameterCount); //doesn't use syntax
            Assert.Equal(1, constructor.Parameters.Length);
        }


        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConstructorArgListParameterCount3()
        {
            var text = @"
class A
{
    public A(__arglist, int x) { } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var constructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(1, constructor.ParameterCount); //doesn't use syntax
            Assert.Equal(1, constructor.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void ConstructorArgListParameterCount4()
        {
            var text = @"
class A
{
    public A(__arglist, int x, __arglist) { } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var constructor = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<MethodSymbol>(WellKnownMemberNames.InstanceConstructorName);
            Assert.Equal(1, constructor.ParameterCount); //doesn't use syntax
            Assert.Equal(1, constructor.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void IndexerArgListParameterCount1()
        {
            var text = @"
class A
{
    public int this[__arglist] { get { return 0; } set { } } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var indexer = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.Equal(0, indexer.ParameterCount); //doesn't use syntax
            Assert.Equal(0, indexer.Parameters.Length);

            var getter = indexer.GetMethod;
            Assert.Equal(0, getter.ParameterCount);
            Assert.Equal(0, getter.Parameters.Length);

            var setter = indexer.SetMethod;
            Assert.Equal(1, setter.ParameterCount);
            Assert.Equal(1, setter.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void IndexerArgListParameterCount2()
        {
            var text = @"
class A
{
    public int this[int x, __arglist] { get { return 0; } set { } } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var indexer = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.Equal(1, indexer.ParameterCount); //doesn't use syntax
            Assert.Equal(1, indexer.Parameters.Length);

            var getter = indexer.GetMethod;
            Assert.Equal(1, getter.ParameterCount);
            Assert.Equal(1, getter.Parameters.Length);

            var setter = indexer.SetMethod;
            Assert.Equal(2, setter.ParameterCount);
            Assert.Equal(2, setter.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void IndexerArgListParameterCount3()
        {
            var text = @"
class A
{
    public int this[__arglist, int x] { get { return 0; } set { } } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var indexer = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.Equal(1, indexer.ParameterCount); //doesn't use syntax
            Assert.Equal(1, indexer.Parameters.Length);

            var getter = indexer.GetMethod;
            Assert.Equal(1, getter.ParameterCount);
            Assert.Equal(1, getter.Parameters.Length);

            var setter = indexer.SetMethod;
            Assert.Equal(2, setter.ParameterCount);
            Assert.Equal(2, setter.Parameters.Length);
        }

        [WorkItem(545055, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545055")]
        [WorkItem(545056, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545056")]
        [Fact]
        public void IndexerArgListParameterCount4()
        {
            var text = @"
class A
{
    public int this[__arglist, int x, __arglist] { get { return 0; } set { } } //illegal, but shouldn't break
}
";
            var comp = CreateCompilation(text);

            var indexer = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("A").GetMember<PropertySymbol>(WellKnownMemberNames.Indexer);
            Assert.Equal(1, indexer.ParameterCount); //doesn't use syntax
            Assert.Equal(1, indexer.Parameters.Length);

            var getter = indexer.GetMethod;
            Assert.Equal(1, getter.ParameterCount);
            Assert.Equal(1, getter.Parameters.Length);

            var setter = indexer.SetMethod;
            Assert.Equal(2, setter.ParameterCount);
            Assert.Equal(2, setter.Parameters.Length);
        }

        [WorkItem(545086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545086")]
        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void BoxReceiverTest()
        {
            var text = @"
using System;
class C
{
    static void Goo()
    {
        RuntimeArgumentHandle rah = default(RuntimeArgumentHandle);
        ArgIterator ai = default(ArgIterator);
        TypedReference tr = default(TypedReference);

        rah.GetType(); // not virtual
        ai.GetType();  // not virtual
        tr.GetType();  // not virtual
        rah.ToString(); // virtual, overridden on ValueType
        ai.ToString();  // virtual, overridden on ValueType
        tr.ToString();  // virtual, overridden on ValueType
        rah.GetHashCode();  // virtual, overridden on ValueType
        ai.GetHashCode();   // no error: virtual, overridden on ArgIterator
        tr.GetHashCode();   // no error: virtual, overridden on TypedReference
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics(
// (11,9): error CS0029: Cannot implicitly convert type 'System.RuntimeArgumentHandle' to 'object'
//         rah.GetType(); // not virtual
Diagnostic(ErrorCode.ERR_NoImplicitConv, "rah").WithArguments("System.RuntimeArgumentHandle", "object"),
// (12,9): error CS0029: Cannot implicitly convert type 'System.ArgIterator' to 'object'
//         ai.GetType();  // not virtual
Diagnostic(ErrorCode.ERR_NoImplicitConv, "ai").WithArguments("System.ArgIterator", "object"),
// (13,9): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'object'
//         tr.GetType();  // not virtual
Diagnostic(ErrorCode.ERR_NoImplicitConv, "tr").WithArguments("System.TypedReference", "object"),
// (14,9): error CS0029: Cannot implicitly convert type 'System.RuntimeArgumentHandle' to 'System.ValueType'
//         rah.ToString(); // virtual, overridden on ValueType
Diagnostic(ErrorCode.ERR_NoImplicitConv, "rah").WithArguments("System.RuntimeArgumentHandle", "System.ValueType"),
// (15,9): error CS0029: Cannot implicitly convert type 'System.ArgIterator' to 'System.ValueType'
//         ai.ToString();  // virtual, overridden on ValueType
Diagnostic(ErrorCode.ERR_NoImplicitConv, "ai").WithArguments("System.ArgIterator", "System.ValueType"),
// (16,9): error CS0029: Cannot implicitly convert type 'System.TypedReference' to 'System.ValueType'
//         tr.ToString();  // virtual, overridden on ValueType
Diagnostic(ErrorCode.ERR_NoImplicitConv, "tr").WithArguments("System.TypedReference", "System.ValueType"),
// (17,9): error CS0029: Cannot implicitly convert type 'System.RuntimeArgumentHandle' to 'System.ValueType'
//         rah.GetHashCode();  // virtual, overridden on ValueType
Diagnostic(ErrorCode.ERR_NoImplicitConv, "rah").WithArguments("System.RuntimeArgumentHandle", "System.ValueType")
                );
        }

        [WorkItem(649808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649808")]
        [Fact]
        public void MissingArgumentsAndOptionalParameters_1()
        {
            var source =
@"class A
{
    internal A(object x, __arglist) { }
    internal static void M(object x, __arglist) { }
}
class B
{
    internal B(object x = null, __arglist) { }
    internal static void M(object x = null, __arglist) { }
}
class C
{
    internal C(object x, object y = null, __arglist) { }
    internal static void M(object x, object y = null, __arglist) { }
}
class D
{
    internal D(object x = null, object y = null, __arglist) { }
    internal static void M(object x = null, object y = null, __arglist) { }
}
class E
{
    static void M()
    {
        // No optional arguments.
        new A(__arglist());
        new A(null, __arglist());
        A.M(__arglist());
        A.M(null, __arglist());
        // One optional argument.
        new B(__arglist());
        new B(null, __arglist());
        B.M(__arglist());
        B.M(null, __arglist());
        // One required, one optional argument.
        new C(__arglist());
        new C(null, __arglist());
        new C(null, null, __arglist());
        C.M(__arglist());
        C.M(null, __arglist());
        C.M(null, null, __arglist());
        // Two optional arguments.
        new D(__arglist());
        new D(null, __arglist());
        new D(null, null, __arglist());
        D.M(__arglist());
        D.M(null, __arglist());
        D.M(null, null, __arglist());
    }
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (26,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'A.A(object, __arglist)'
                //         new A(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "A").WithArguments("__arglist", "A.A(object, __arglist)").WithLocation(26, 13),
                // (28,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'A.M(object, __arglist)'
                //         A.M(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "A.M(object, __arglist)").WithLocation(28, 11),
                // (31,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'B.B(object, __arglist)'
                //         new B(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "B").WithArguments("__arglist", "B.B(object, __arglist)").WithLocation(31, 13),
                // (33,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'B.M(object, __arglist)'
                //         B.M(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "B.M(object, __arglist)").WithLocation(33, 11),
                // (36,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.C(object, object, __arglist)'
                //         new C(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("__arglist", "C.C(object, object, __arglist)").WithLocation(36, 13),
                // (37,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.C(object, object, __arglist)'
                //         new C(null, __arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "C").WithArguments("__arglist", "C.C(object, object, __arglist)").WithLocation(37, 13),
                // (39,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.M(object, object, __arglist)'
                //         C.M(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "C.M(object, object, __arglist)").WithLocation(39, 11),
                // (40,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'C.M(object, object, __arglist)'
                //         C.M(null, __arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "C.M(object, object, __arglist)").WithLocation(40, 11),
                // (43,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'D.D(object, object, __arglist)'
                //         new D(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "D").WithArguments("__arglist", "D.D(object, object, __arglist)").WithLocation(43, 13),
                // (44,13): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'D.D(object, object, __arglist)'
                //         new D(null, __arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "D").WithArguments("__arglist", "D.D(object, object, __arglist)").WithLocation(44, 13),
                // (46,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'D.M(object, object, __arglist)'
                //         D.M(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "D.M(object, object, __arglist)").WithLocation(46, 11),
                // (47,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'D.M(object, object, __arglist)'
                //         D.M(null, __arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M").WithArguments("__arglist", "D.M(object, object, __arglist)").WithLocation(47, 11));
        }

        [WorkItem(649808, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/649808")]
        [Fact]
        public void MissingArgumentsAndOptionalParameters_2()
        {
            var ilSource =
@".class public sealed D extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname instance void .ctor(object o, native int m) runtime { }
  .method public hidebysig instance vararg void Invoke([opt] object o) runtime { }
  .method public hidebysig instance class [mscorlib]System.IAsyncResult BeginInvoke(class [mscorlib]System.AsyncCallback c, object o) runtime { }
  .method public hidebysig instance void EndInvoke(class [mscorlib]System.IAsyncResult r) runtime { }
}";
            var source =
@"class C
{
    static void M(D d)
    {
        d(null, __arglist());
        d(__arglist());
    }
}";
            var compilation = CreateCompilationWithILAndMscorlib40(source, ilSource);
            compilation.VerifyDiagnostics(
                // (6,9): error CS7036: There is no argument given that corresponds to the required formal parameter '__arglist' of 'D'
                //         d(__arglist());
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "d").WithArguments("__arglist", "D").WithLocation(6, 9));
        }

        [Fact, WorkItem(1253, "https://github.com/dotnet/roslyn/issues/1253")]
        public void LambdaWithUnsafeParameter()
        {
            var source =
@"

using System;
using System.Threading;

namespace ConsoleApplication21
{
    public unsafe class GooBar : IDisposable
    {
        public void Dispose()
        {
            NativeOverlapped* overlapped = AllocateNativeOverlapped(() => { });
        }

        private unsafe static NativeOverlapped* AllocateNativeOverlapped(IOCompletionCallback callback, object context, byte[] pinData)
        {
            return null;
        }
    }
}
";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
    // (12,44): error CS7036: There is no argument given that corresponds to the required formal parameter 'context' of 'GooBar.AllocateNativeOverlapped(IOCompletionCallback, object, byte[])'
    //             NativeOverlapped* overlapped = AllocateNativeOverlapped(() => { });
    Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "AllocateNativeOverlapped").WithArguments("context", "ConsoleApplication21.GooBar.AllocateNativeOverlapped(System.Threading.IOCompletionCallback, object, byte[])").WithLocation(12, 44)
);
        }

        [Fact, WorkItem(8152, "https://github.com/dotnet/roslyn/issues/8152")]
        public void DuplicateDeclaration()
        {
            var source =
@"
public class SpecialCases
{
    public void ArgListMethod(__arglist)
    {
        ArgListMethod(__arglist(""""));
    }
    public void ArgListMethod(__arglist)
    {
        ArgListMethod(__arglist(""""));
    }
}
";
            CreateCompilation(source, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
    // (8,17): error CS0111: Type 'SpecialCases' already defines a member called 'ArgListMethod' with the same parameter types
    //     public void ArgListMethod(__arglist)
    Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "ArgListMethod").WithArguments("ArgListMethod", "SpecialCases").WithLocation(8, 17),
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'SpecialCases.ArgListMethod(__arglist)' and 'SpecialCases.ArgListMethod(__arglist)'
    //         ArgListMethod(__arglist(""));
    Diagnostic(ErrorCode.ERR_AmbigCall, "ArgListMethod").WithArguments("SpecialCases.ArgListMethod(__arglist)", "SpecialCases.ArgListMethod(__arglist)").WithLocation(6, 9),
    // (10,9): error CS0121: The call is ambiguous between the following methods or properties: 'SpecialCases.ArgListMethod(__arglist)' and 'SpecialCases.ArgListMethod(__arglist)'
    //         ArgListMethod(__arglist(""));
    Diagnostic(ErrorCode.ERR_AmbigCall, "ArgListMethod").WithArguments("SpecialCases.ArgListMethod(__arglist)", "SpecialCases.ArgListMethod(__arglist)").WithLocation(10, 9)
                );
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArgListMayNotHaveAnOutArgument()
        {
            CreateCompilation(@"
class Program
{
    static void Test(__arglist)
    {
        var a = 1;
    	Test(__arglist(out a));
    }
}
").VerifyDiagnostics(
                // (7,25): error CS8378: __arglist cannot have an argument passed by 'in' or 'out'
                //     	Test(__arglist(out a));
                Diagnostic(ErrorCode.ERR_CantUseInOrOutInArglist, "a").WithLocation(7, 25));
        }

        [Fact]
        public void ArgListMayNotHaveAnInArgument()
        {
            CreateCompilation(@"
class Program
{
    static void Test(__arglist)
    {
        var a = 1;
    	Test(__arglist(in a));
    }
}
").VerifyDiagnostics(
                // (7,24): error CS8378: __arglist cannot have an argument passed by 'in' or 'out'
                //     	Test(__arglist(in a));
                Diagnostic(ErrorCode.ERR_CantUseInOrOutInArglist, "a").WithLocation(7, 24));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArgListMayHaveARefArgument()
        {
            CompileAndVerify(@"
using System;
class Program
{
    static void Test(__arglist)
    {
        var args = new ArgIterator(__arglist);
        ref int a = ref __refvalue(args.GetNextArg(), int);
        a = 5;
    }
    static void Main()
    {
        int a = 0;
        Test(__arglist(ref a));
        Console.WriteLine(a);
    }
}",
                options: TestOptions.DebugExe,
                expectedOutput: "5");
        }

        [ConditionalFact(typeof(DesktopOnly), Reason = ConditionalSkipReason.RestrictedTypesNeedDesktop)]
        public void ArgListMayHaveAByValArgument()
        {
            CompileAndVerify(@"
using System;
class Program
{
    static void Test(__arglist)
    {
        var args = new ArgIterator(__arglist);
        int a = __refvalue(args.GetNextArg(), int);
        Console.WriteLine(a);
    }
    static void Main()
    {
        int a = 5;
        Test(__arglist(a));
    }
}",
                options: TestOptions.DebugExe,
                expectedOutput: "5");
        }
    }
}
