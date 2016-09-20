// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LocalFunctionsTestBase : CSharpTestBase
    {
        internal static readonly CSharpParseOptions DefaultParseOptions = TestOptions.Regular;

        internal void VerifyDiagnostics(string source, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: TestOptions.ReleaseDll, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(expected);
        }

        internal void VerifyDiagnostics(string source, CSharpCompilationOptions options, params DiagnosticDescription[] expected)
        {
            var comp = CreateCompilationWithMscorlib45AndCSruntime(source, options: options, parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics(expected);
        }
    }

    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public class LocalFunctionTests : LocalFunctionsTestBase
    {

        [Fact]
        public void ForgotSemicolonLocalFunctionsMistake()
        {
            var src = @"
class C
{
    public void M1()
    {
    // forget closing brace

    public void BadLocal1()
    {
        this.BadLocal2();
    }

    public void BadLocal2()
    {
    }

    public int P => 0;
}";
            VerifyDiagnostics(src,
                // (8,5): error CS0106: The modifier 'public' is not valid for this item
                //     public void BadLocal1()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(8, 5),
                // (13,5): error CS0106: The modifier 'public' is not valid for this item
                //     public void BadLocal2()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(13, 5),
                // (15,6): error CS1513: } expected
                //     }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(15, 6),
                // (10,14): error CS1061: 'C' does not contain a definition for 'BadLocal2' and no extension method 'BadLocal2' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                //         this.BadLocal2();
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "BadLocal2").WithArguments("C", "BadLocal2").WithLocation(10, 14),
                // (8,17): warning CS0168: The variable 'BadLocal1' is declared but never used
                //     public void BadLocal1()
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "BadLocal1").WithArguments("BadLocal1").WithLocation(8, 17),
                // (13,17): warning CS0168: The variable 'BadLocal2' is declared but never used
                //     public void BadLocal2()
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "BadLocal2").WithArguments("BadLocal2").WithLocation(13, 17));

        }

        [Fact]
        public void VarLocalFunction()
        {
            var src = @"
class C
{
    void M()
    {
        var local() => 0;
        int x = local();
   } 
}";
            VerifyDiagnostics(src,
                // (6,9): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         var local() => 0;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(6, 9));
        }

        [Fact]
        public void VarLocalFunction2()
        {
            var comp = CreateCompilationWithMscorlib(@"
class C
{
    private class var
    {
    }

    void M()
    {
        var local() => new var();
        var x = local();
   } 
}", parseOptions: DefaultParseOptions);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Params)]
        public void BadParams()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void Params(params int x)
        {
            Console.WriteLine(x);
        }
        Params(2);
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): error CS0225: The params parameter must be a single dimensional array
    //         void Params(params int x)
    Diagnostic(ErrorCode.ERR_ParamsMustBeArray, "params").WithLocation(8, 21)
    );
        }

        [Fact]
        public void BadRefWithDefault()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void RefOut(ref int x = 2)
        {
            x++;
        }
        int y = 2;
        RefOut(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (6,21): error CS1741: A ref or out parameter cannot have a default value
    //         void RefOut(ref int x = 2)
    Diagnostic(ErrorCode.ERR_RefOutDefaultValue, "ref").WithLocation(6, 21)
    );
        }

        [Fact]
        public void BadDefaultValueType()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        void NamedOptional(string x = 2)
        {
            Console.WriteLine(x);
        }
        NamedOptional(""2"");
    }
}
";
            VerifyDiagnostics(source,
    // (8,35): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
    //         void NamedOptional(string x = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("int", "string").WithLocation(8, 35)
    );
        }

        [Fact]
        public void BadCallerMemberName()
        {
            var source = @"
using System;
using System.Runtime.CompilerServices;

class Program
{
    static void Main(string[] args)
    {
        void CallerMemberName([CallerMemberName] int s = 2)
        {
            Console.WriteLine(s);
        }
        CallerMemberName();
    }
}
";
            VerifyDiagnostics(source,
    // (9,32): error CS4019: CallerMemberNameAttribute cannot be applied because there are no standard conversions from type 'string' to type 'int'
    //         void CallerMemberName([CallerMemberName] int s = 2)
    Diagnostic(ErrorCode.ERR_NoConversionForCallerMemberNameParam, "CallerMemberName").WithArguments("string", "int").WithLocation(9, 32)
    );
        }
        [WorkItem(10708, "https://github.com/dotnet/roslyn/issues/10708")]
        [CompilerTrait(CompilerFeature.Dynamic, CompilerFeature.Params)]
        [Fact]
        public void DynamicArgumentToParams()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void L1(int x = 0, params int[] ys) => Console.Write(x);

        dynamic val = 2;
        L1(val, val);
        L1(ys: val, x: val);
        L1(ys: val);
    }
}";
            VerifyDiagnostics(src,
                // (10,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(val, val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(val, val)").WithArguments("ys", "L1").WithLocation(10, 9),
                // (11,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(ys: val, x: val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(ys: val, x: val)").WithArguments("ys", "L1").WithLocation(11, 9),
                // (12,9): error CS8106: Cannot pass argument with dynamic type to params parameter 'ys' of local function 'L1'.
                //         L1(ys: val);
                Diagnostic(ErrorCode.ERR_DynamicLocalFunctionParamsParameter, "L1(ys: val)").WithArguments("ys", "L1").WithLocation(12, 9));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgOverload()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void Overload(int i) => Console.Write(i);
        void Overload(string s) => Console.Write(s);

        dynamic val = 2;
        Overload(val);
    }
}";
            VerifyDiagnostics(src,
                // (8,14): error CS0128: A local variable named 'Overload' is already defined in this scope
                //         void Overload(string s) => Console.Write(s);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "Overload").WithArguments("Overload").WithLocation(8, 14),
                // (8,14): warning CS0168: The variable 'Overload' is declared but never used
                //         void Overload(string s) => Console.Write(s);
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Overload").WithArguments("Overload").WithLocation(8, 14));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Dynamic)]
        public void DynamicArgWrongArity()
        {
            var src = @"
using System;
class C
{
    static void Main()
    {
        void Local(int i) => Console.Write(i);

        dynamic val = 2;
        Local(val, val);
    }
}";
            VerifyDiagnostics(src,
                // (10,9): error CS1501: No overload for method 'Local' takes 2 arguments
                //         Local(val, val);
                Diagnostic(ErrorCode.ERR_BadArgCount, "Local").WithArguments("Local", "2").WithLocation(10, 9));
        }

        [WorkItem(3923, "https://github.com/dotnet/roslyn/issues/3923")]
        [Fact]
        public void ExpressionTreeLocfuncUsage()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        T Id<T>(T x)
        {
            return x;
        }
        Expression<Func<T>> Local<T>(Expression<Func<T>> f)
        {
            return f;
        }
        Console.Write(Local(() => Id(2)));
        Console.Write(Local<Func<int, int>>(() => Id));
        Console.Write(Local(() => new Func<int, int>(Id)));
        Console.Write(Local(() => nameof(Id)));
    }
}
";
            VerifyDiagnostics(source,
    // (16,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => Id(2)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id(2)").WithLocation(16, 35),
    // (17,51): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local<Func<int, int>>(() => Id));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Id").WithLocation(17, 51),
    // (18,35): error CS8096: An expression tree may not contain a reference to a local function
    //         Console.Write(Local(() => new Func<int, int>(Id)));
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "new Func<int, int>(Id)").WithLocation(18, 35)
    );
        }

        [Fact]
        public void ExpressionTreeLocfuncInside()
        {
            var source = @"
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<int, int>> f = x =>
        {
            int Local(int y) => y;
            return Local(x);
        };
        Console.Write(f);
    }
}
";
            VerifyDiagnostics(source,
    // (8,40): error CS0834: A lambda expression with a statement body cannot be converted to an expression tree
    //         Expression<Func<int, int>> f = x =>
    Diagnostic(ErrorCode.ERR_StatementLambdaToExpressionTree, @"x =>
        {
            int Local(int y) => y;
            return Local(x);
        }").WithLocation(8, 40),
    // (11,20): error CS8096: An expression tree may not contain a local function or a reference to a local function
    //             return Local(x);
    Diagnostic(ErrorCode.ERR_ExpressionTreeContainsLocalFunction, "Local(x)").WithLocation(11, 20)
    );
        }
        [Fact]
        public void BadScoping()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        if (true)
        {
            void Local()
            {
                Console.WriteLine(2);
            }
            Local();
        }
        Local();

        Local2();
        void Local2()
        {
            Console.WriteLine(2);
        }
    }
}
";
            VerifyDiagnostics(source,
    // (16,9): error CS0103: The name 'Local' does not exist in the current context
    //         Local();
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Local").WithArguments("Local").WithLocation(16, 9)
    );
        }

        [Fact]
        public void NameConflictDuplicate()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Duplicate() { }
        void Duplicate() { }
        Duplicate();
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Duplicate' is already defined in this scope
    //         void Duplicate() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14),
    // (7,14): warning CS0168: The variable 'Duplicate' is declared but never used
    //         void Duplicate() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Duplicate").WithArguments("Duplicate").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        void Param(int x) { }
        Param(x);
    }
}
";
            VerifyDiagnostics(source,
    // (7,24): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Param(int x) { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(7, 24)
    );
        }

        [Fact]
        public void NameConflictTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int T;
        void Generic<T>() { }
        Generic<int>();
    }
}
";
            VerifyDiagnostics(source,
    // (7,22): error CS0136: A local or parameter named 'T' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Generic<T>() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "T").WithArguments("T").WithLocation(7, 22),
    // (6,13): warning CS0168: The variable 'T' is declared but never used
    //         int T;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "T").WithArguments("T").WithLocation(6, 13)
    );
        }

        [Fact]
        public void NameConflictNestedTypeParameter()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        T Outer<T>()
        {
            T Inner<T>()
            {
                return default(T);
            }
            return Inner<T>();
        }
        System.Console.Write(Outer<int>());
    }
}
";
            VerifyDiagnostics(source,
    // (8,21): warning CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'Outer<T>()'
    //             T Inner<T>()
    Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "T").WithArguments("T", "Outer<T>()").WithLocation(8, 21)
    );
        }

        [Fact]
        public void NameConflictLocalVarFirst()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Conflict;
        void Conflict() { }
    }
}
";
            VerifyDiagnostics(source,
    // (7,14): error CS0128: A local variable named 'Conflict' is already defined in this scope
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "Conflict").WithArguments("Conflict").WithLocation(7, 14),
    // (6,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(6, 13),
    // (7,14): warning CS0168: The variable 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(7, 14)
    );
        }

        [Fact]
        public void NameConflictLocalVarLast()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Conflict() { }
        int Conflict;
    }
}
";
            // TODO: This is strange. Probably has to do with the fact that local variables are preferred over functions.
            VerifyDiagnostics(source,
    // (6,14): error CS0136: A local or parameter named 'Conflict' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         void Conflict() { }
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "Conflict").WithArguments("Conflict").WithLocation(6, 14),
    // (7,13): warning CS0168: The variable 'Conflict' is declared but never used
    //         int Conflict;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(7, 13),
    // (6,14): warning CS0168: The variable 'Conflict' is declared but never used
    //         void Conflict() { }
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Conflict").WithArguments("Conflict").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadUnsafeNoKeyword()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}
";
            VerifyDiagnostics(source,
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32)
    );
        }

        [Fact]
        public void BadUnsafeKeywordDoesntApply()
        {
            var source = @"
using System;

class Program
{
    static unsafe void B()
    {
        void Local()
        {
            int x = 2;
            Console.WriteLine(*&x);
        }
        Local();
    }
    static void Main(string[] args)
    {
        B();
    }
}
";
            VerifyDiagnostics(source, TestOptions.ReleaseExe.WithAllowUnsafe(true),
    // (11,32): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
    //             Console.WriteLine(*&x);
    Diagnostic(ErrorCode.ERR_UnsafeNeeded, "&x").WithLocation(11, 32)
    );
        }

        [Fact]
        public void BadEmptyBody()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local(int x);
        Local(2);
    }
}";
            VerifyDiagnostics(source,
    // (6,14): error CS0501: 'Local(int)' must declare a body because it is not marked abstract, extern, or partial
    //         void Local(int x);
    Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "Local").WithArguments("Local(int)").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadGotoInto()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        goto A;
        void Local()
        {
        A:  Console.Write(2);
        }
        Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,14): error CS0159: No such label 'A' within the scope of the goto statement
    //         goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "A").WithArguments("A").WithLocation(8, 14),
    // (11,9): warning CS0164: This label has not been referenced
    //         A:  Console.Write(2);
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(11, 9)
    );
        }

        [Fact]
        public void BadGotoOutOf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local()
        {
            goto A;
        }
    A:  Local();
    }
}";
            VerifyDiagnostics(source,
    // (8,13): error CS0159: No such label 'A' within the scope of the goto statement
    //             goto A;
    Diagnostic(ErrorCode.ERR_LabelNotFound, "goto").WithArguments("A").WithLocation(8, 13),
    // (10,5): warning CS0164: This label has not been referenced
    //     A:  Local();
    Diagnostic(ErrorCode.WRN_UnreferencedLabel, "A").WithLocation(10, 5)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentCall()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        Local();
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,9): error CS0165: Use of unassigned local variable 'x'
                //         Local();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local()").WithArguments("x").WithLocation(15, 9)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConversion()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        Action foo = Local;
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,22): error CS0165: Use of unassigned local variable 'x'
                //         Action foo = Local;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Local").WithArguments("x").WithLocation(15, 22)
    );
        }

        [Fact]
        public void BadDefiniteAssignmentDelegateConstruction()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        goto Label;
        int x = 2;
        void Local()
        {
            Console.Write(x);
        }
        Label:
        var bar = new Action(Local);
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
                // (9,9): warning CS0162: Unreachable code detected
                //         int x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(9, 9),
                // (15,19): error CS0165: Use of unassigned local variable 'x'
                //         var bar = new Action(Local);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "new Action(Local)").WithArguments("x").WithLocation(15, 19)
                );
        }

        [Fact]
        public void BadNotUsed()
        {
            var source = @"
class Program
{
    static void A()
    {
        void Local()
        {
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (6,14): warning CS0168: The variable 'Local' is declared but never used
    //         void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Local").WithArguments("Local").WithLocation(6, 14)
    );
        }

        [Fact]
        public void BadNotUsedSwitch()
        {
            var source = @"
class Program
{
    static void A()
    {
        switch (0)
        {
        case 0:
            void Local()
            {
            }
            break;
        }
    }
    static void Main(string[] args)
    {
        A();
    }
}";
            VerifyDiagnostics(source,
    // (9,18): warning CS0168: The variable 'Local' is declared but never used
    //             void Local()
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Local").WithArguments("Local").WithLocation(9, 18)
    );
        }

        [Fact]
        public void BadByRefClosure()
        {
            var source = @"
using System;

class Program
{
    static void A(ref int x)
    {
        void Local()
        {
            Console.WriteLine(x);
        }
        Local();
    }
    static void Main()
    {
    }
}";
            VerifyDiagnostics(source,
    // (10,31): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
    //             Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(10, 31)
    );
        }

        [Fact]
        public void BadArglistUse()
        {
            var source = @"
using System;

class Program
{
    static void A()
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void B(__arglist)
    {
        void Local()
        {
            Console.WriteLine(__arglist);
        }
        Local();
    }
    static void C() // C and D produce different errors
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void D(__arglist)
    {
        void Local(__arglist)
        {
            Console.WriteLine(__arglist);
        }
        Local(__arglist());
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
    // (10,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(10, 31),
    // (18,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(18, 31),
    // (26,31): error CS0190: The __arglist construct is valid only within a variable argument method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_ArgsInvalid, "__arglist").WithLocation(26, 31),
    // (34,31): error CS4013: Instance of type 'RuntimeArgumentHandle' cannot be used inside an anonymous function, query expression, iterator block or async method
    //             Console.WriteLine(__arglist);
    Diagnostic(ErrorCode.ERR_SpecialByRefInLambda, "__arglist").WithArguments("System.RuntimeArgumentHandle").WithLocation(34, 31)
    );
        }

        [Fact]
        public void BadClosureStaticRefInstance()
        {
            var source = @"
using System;

class Program
{
    int _a = 0;
    static void A()
    {
        void Local()
        {
            Console.WriteLine(_a);
        }
        Local();
    }
    static void Main()
    {
    }
}
";
            VerifyDiagnostics(source,
    // (11,31): error CS0120: An object reference is required for the non-static field, method, or property 'Program._a'
    //             Console.WriteLine(_a);
    Diagnostic(ErrorCode.ERR_ObjectRequired, "_a").WithArguments("Program._a").WithLocation(11, 31)
    );
        }

        [Fact]
        public void BadRefIterator()
        {
            var source = @"
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> RefEnumerable(ref int x)
        {
            yield return x;
        }
        int y = 0;
        RefEnumerable(ref y);
    }
}
";
            VerifyDiagnostics(source,
    // (8,48): error CS1623: Iterators cannot have ref or out parameters
    //         IEnumerable<int> RefEnumerable(ref int x)
    Diagnostic(ErrorCode.ERR_BadIteratorArgType, "x").WithLocation(8, 48)
    );
        }

        [Fact]
        public void BadRefAsync()
        {
            var source = @"
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        async Task<int> RefAsync(ref int x)
        {
            return await Task.FromResult(x);
        }
        int y = 2;
        Console.Write(RefAsync(ref y).Result);
    }
}
";
            VerifyDiagnostics(source,
    // (9,42): error CS1988: Async methods cannot have ref or out parameters
    //         async Task<int> RefAsync(ref int x)
    Diagnostic(ErrorCode.ERR_BadAsyncArgType, "x").WithLocation(9, 42)
    );
        }

        [Fact]
        public void Extension()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int Local(this int x)
        {
            return x;
        }
        Console.WriteLine(Local(2));
    }
}
";
            VerifyDiagnostics(source,
    // (8,13): error CS1106: Extension method must be defined in a non-generic static class
    //         int Local(this int x)
    Diagnostic(ErrorCode.ERR_BadExtensionAgg, "Local").WithLocation(8, 13)
                );
        }

        [Fact]
        public void BadModifiers()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        const void LocalConst()
        {
        }
        static void LocalStatic()
        {
        }
        readonly void LocalReadonly()
        {
        }
        volatile void LocalVolatile()
        {
        }
        LocalConst();
        LocalStatic();
        LocalReadonly();
        LocalVolatile();
    }
}
";
            VerifyDiagnostics(source,
    // (6,9): error CS0106: The modifier 'const' is not valid for this item
    //         const void LocalConst()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "const").WithArguments("const").WithLocation(6, 9),
    // (9,9): error CS0106: The modifier 'static' is not valid for this item
    //         static void LocalStatic()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(9, 9),
    // (12,9): error CS0106: The modifier 'readonly' is not valid for this item
    //         readonly void LocalReadonly()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(12, 9),
    // (15,9): error CS0106: The modifier 'volatile' is not valid for this item
    //         volatile void LocalVolatile()
    Diagnostic(ErrorCode.ERR_BadMemberFlag, "volatile").WithArguments("volatile").WithLocation(15, 9)
                );
        }

        [Fact]
        public void ArglistIterator()
        {
            var source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        IEnumerable<int> Local(__arglist)
        {
            yield return 2;
        }
        Console.WriteLine(string.Join("","", Local(__arglist())));
    }
}
";
            VerifyDiagnostics(source,
    // (9,26): error CS1636: __arglist is not allowed in the parameter list of iterators
    //         IEnumerable<int> Local(__arglist)
    Diagnostic(ErrorCode.ERR_VarargsIterator, "Local").WithLocation(9, 26)
    );
        }

        [Fact]
        public void ForwardReference()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(Local());
        int Local() => 2;
    }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void ForwardReferenceCapture()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(Local());
        int Local() => x;
    }
}
";
            CompileAndVerify(source, expectedOutput: "2");
        }

        [Fact]
        public void ForwardRefInLocalFunc()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 2;
        Console.WriteLine(Local());
        int Local()
        {
            x = 3;
            return Local2();
        }
        int Local2() => x;
    }
}
";
            CompileAndVerify(source, expectedOutput: "3");
        }

        [Fact]
        public void LocalFuncMutualRecursion()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        int x = 5;
        int y = 0;
        Console.WriteLine(Local1());
        int Local1()
        {
            x -= 1;
            return Local2(y++);
        }
        int Local2(int z)
        {
            if (x == 0)
            {
                return z;
            }
            else
            {
                return Local1();
            }
        }
    }
}
";
            CompileAndVerify(source, expectedOutput: "4");
        }

        [Fact]
        public void OtherSwitchBlock()
        {
            var source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = int.Parse(Console.ReadLine());
        switch (x)
        {
        case 0:
            void Local()
            {
            }
            break;
        default:
            Local();
            break;
        }
    }
}
";
            VerifyDiagnostics(source);
        }

        [Fact]
        public void NoOperator()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        Program operator +(Program left, Program right)
        {
            return left;
        }
    }
}
";
            VerifyDiagnostics(source,
    // (6,17): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "operator").WithLocation(6, 17),
    // (6,17): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, "operator").WithLocation(6, 17),
    // (6,36): error CS1026: ) expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "left").WithLocation(6, 36),
    // (6,36): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "left").WithLocation(6, 36),
    // (6,40): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 40),
    // (6,40): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 40),
    // (6,55): error CS1002: ; expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 55),
    // (6,55): error CS1513: } expected
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 55),
    // (6,9): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 9),
    // (6,28): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 28),
    // (6,28): error CS0119: 'Program' is a type, which is not valid in the given context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(6, 28),
    // (6,36): error CS0103: The name 'left' does not exist in the current context
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "left").WithArguments("left").WithLocation(6, 36),
    // (8,20): error CS0103: The name 'left' does not exist in the current context
    //             return left;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "left").WithArguments("left").WithLocation(8, 20),
    // (6,50): warning CS0168: The variable 'right' is declared but never used
    //         Program operator +(Program left, Program right)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "right").WithArguments("right").WithLocation(6, 50)
    );
        }

        [Fact]
        public void NoProperty()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        int Foo
        {
            get
            {
                return 2;
            }
        }
        int Bar => 2;
    }
}
";
            VerifyDiagnostics(source,
    // (6,16): error CS1002: ; expected
    //         int Foo
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 16),
    // (8,16): error CS1002: ; expected
    //             get
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 16),
    // (13,17): error CS1003: Syntax error, ',' expected
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",", "=>").WithLocation(13, 17),
    // (13,20): error CS1002: ; expected
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "2").WithLocation(13, 20),
    // (8,13): error CS0103: The name 'get' does not exist in the current context
    //             get
    Diagnostic(ErrorCode.ERR_NameNotInContext, "get").WithArguments("get").WithLocation(8, 13),
    // (10,17): error CS0127: Since 'Program.Main(string[])' returns void, a return keyword must not be followed by an object expression
    //                 return 2;
    Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("Program.Main(string[])").WithLocation(10, 17),
    // (13,20): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         int Bar => 2;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "2").WithLocation(13, 20),
    // (13,9): warning CS0162: Unreachable code detected
    //         int Bar => 2;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "int").WithLocation(13, 9),
    // (6,13): warning CS0168: The variable 'Foo' is declared but never used
    //         int Foo
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Foo").WithArguments("Foo").WithLocation(6, 13),
    // (13,13): warning CS0168: The variable 'Bar' is declared but never used
    //         int Bar => 2;
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "Bar").WithArguments("Bar").WithLocation(13, 13)
    );
        }

        [Fact]
        public void NoFeatureSwitch()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        void Local() { }
        Local();
    }
}
";
            var option = TestOptions.ReleaseExe;
            CreateCompilationWithMscorlib(source, options: option, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (6,14): error CS8059: Feature 'local functions' is not available in C# 6.  Please use language version 7 or greater.
                //         void Local() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "Local").WithArguments("local functions", "7").WithLocation(6, 14)
                );
        }

        [Fact, WorkItem(10521, "https://github.com/dotnet/roslyn/issues/10521")]
        public void LocalFunctionInIf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        if () // typing at this point
        int Add(int x, int y) => x + y;
    }
}
";
            VerifyDiagnostics(source,
                // (6,13): error CS1525: Invalid expression term ')'
                //         if () // typing at this point
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 13),
                // (7,9): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "int Add(int x, int y) => x + y;").WithLocation(7, 9),
                // (7,13): warning CS0168: The variable 'Add' is declared but never used
                //         int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Add").WithArguments("Add").WithLocation(7, 13)
                );
        }

        [Fact, WorkItem(10521, "https://github.com/dotnet/roslyn/issues/10521")]
        public void LabeledLocalFunctionInIf()
        {
            var source = @"
class Program
{
    static void Main(string[] args)
    {
        if () // typing at this point
a:      int Add(int x, int y) => x + y;
    }
}
";
            VerifyDiagnostics(source,
                // (6,13): error CS1525: Invalid expression term ')'
                //         if () // typing at this point
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 13),
                // (7,1): error CS1023: Embedded statement cannot be a declaration or labeled statement
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "a:      int Add(int x, int y) => x + y;").WithLocation(7, 1),
                // (7,1): warning CS0164: This label has not been referenced
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(7, 1),
                // (7,13): warning CS0168: The variable 'Add' is declared but never used
                // a:      int Add(int x, int y) => x + y;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "Add").WithArguments("Add").WithLocation(7, 13)
                );
        }

        [CompilerTrait(CompilerFeature.LocalFunctions, CompilerFeature.Var)]
        public sealed class VarTests : LocalFunctionsTestBase
        {
            [Fact]
            public void IllegalAsReturn()
            {
                var source = @"
using System;
class Program
{
    static void Main()
    {
        var f() => 42;
        Console.WriteLine(f());
    }
}";
                var comp = CreateCompilationWithMscorlib45(source, parseOptions: DefaultParseOptions);
                comp.VerifyDiagnostics(
                    // (7,9): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                    //         var f() => 42;
                    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(7, 9));
            }

            [Fact]
            public void RealTypeAsReturn()
            {
                var source = @"
using System;
class var 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        var f() => new var();
        Console.WriteLine(f());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void RealTypeParameterAsReturn()
            {
                var source = @"
using System;
class test 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Test<var>(var x)
    {
        var f() => x;
        Console.WriteLine(f());
    }

    static void Main()
    {
        Test(new test());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void IdentifierAndTypeNamedVar()
            {
                var source = @"
using System;
class var 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        int var = 42;
        var f() => new var();
        Console.WriteLine($""{f()}-{var}"");
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog-42");
            }
        }

        [CompilerTrait(CompilerFeature.LocalFunctions, CompilerFeature.Async)]
        public sealed class AsyncTests : LocalFunctionsTestBase
        {
            [Fact]
            public void RealTypeAsReturn()
            {
                var source = @"
using System;
class async 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Main()
    {
        async f() => new async();
        Console.WriteLine(f());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void RealTypeParameterAsReturn()
            {
                var source = @"
using System;
class test 
{
    public override string ToString() => ""dog"";
}

class Program
{
    static void Test<async>(async x)
    {
        async f() => x;
        Console.WriteLine(f());
    }

    static void Main()
    {
        Test(new test());
    }
}";

                CompileAndVerify(
                    source,
                    parseOptions: DefaultParseOptions,
                    expectedOutput: "dog");
            }

            [Fact]
            public void ManyMeaningsType()
            {
                var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;

class async 
{
    public override string ToString() => ""async"";
}

class Program
{
    static void Main()
    {
        async Task<async> Test(Task<async> t)
        {
            async local = await t;
            Console.WriteLine(local);
            return local;
        }

        Test(Task.FromResult<async>(new async())).Wait();
    }
}";

                var comp = CreateCompilationWithMscorlib46(source, parseOptions: DefaultParseOptions, options: TestOptions.DebugExe);
                CompileAndVerify(
                    comp,
                    expectedOutput: "async");
            }
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_01()
        {
            var src = @"
class C
{
    public void M1()
    {
        void TakeOutParam1(out int x)
        {
        }

        int y;
        TakeOutParam1(out y);
    }

        void TakeOutParam2(out int x)
        {
        }
}";
            VerifyDiagnostics(src,
                // (6,14): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         void TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam1").WithArguments("x").WithLocation(6, 14),
                // (14,14): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         void TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam2").WithArguments("x").WithLocation(14, 14)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_02()
        {
            var src = @"
class C
{
    public void M1()
    {
        void TakeOutParam1(out int x)
        {
            return; // 1 
        }

        int y;
        TakeOutParam1(out y);
    }

        void TakeOutParam2(out int x)
        {
            return; // 2 
        }
}";
            VerifyDiagnostics(src,
                // (8,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return; // 1 
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return;").WithArguments("x").WithLocation(8, 13),
                // (17,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return; // 2 
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return;").WithArguments("x").WithLocation(17, 13)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_03()
        {
            var src = @"
class C
{
    public void M1()
    {
        int TakeOutParam1(out int x)
        {
        }

        int y;
        TakeOutParam1(out y);
    }

        int TakeOutParam2(out int x)
        {
        }
}";
            VerifyDiagnostics(src,
                // (6,13): error CS0161: 'TakeOutParam1(out int)': not all code paths return a value
                //         int TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "TakeOutParam1").WithArguments("TakeOutParam1(out int)").WithLocation(6, 13),
                // (6,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         int TakeOutParam1(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam1").WithArguments("x").WithLocation(6, 13),
                // (14,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //         int TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "TakeOutParam2").WithArguments("x").WithLocation(14, 13),
                // (14,13): error CS0161: 'C.TakeOutParam2(out int)': not all code paths return a value
                //         int TakeOutParam2(out int x)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "TakeOutParam2").WithArguments("C.TakeOutParam2(out int)").WithLocation(14, 13)
                );
        }

        [Fact]
        [WorkItem(12467, "https://github.com/dotnet/roslyn/issues/12467")]
        public void ParamUnassigned_04()
        {
            var src = @"
class C
{
    public void M1()
    {
        int TakeOutParam1(out int x)
        {
            return 1;
        }

        int y;
        TakeOutParam1(out y);
    }

        int TakeOutParam2(out int x)
        {
            return 2;
        }
}";
            VerifyDiagnostics(src,
                // (8,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return 1;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 1;").WithArguments("x").WithLocation(8, 13),
                // (17,13): error CS0177: The out parameter 'x' must be assigned to before control leaves the current method
                //             return 2;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 2;").WithArguments("x").WithLocation(17, 13)
                );
        }
    }
}
