// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class LambdaTests : CompilingTestBase
    {
        [Fact, WorkItem(608181, "DevDiv")]
        public void BadInvocationInLambda()
        {
            var src = @"
using System;
using System.Linq.Expressions;

class C
{
    Expression<Action<dynamic>> e = x => new object[](x);
}";
            var comp = CreateCompilationWithMscorlibAndSystemCore(src);
            comp.VerifyDiagnostics(
                // (7,52): error CS1586: Array creation must have array size or array initializer
                //     Expression<Action<dynamic>> e = x => new object[](x);
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]"));
        }

        [Fact]
        public void TestLambdaErrors01()
        {
            var comp = CreateCompilationWithMscorlibAndSystemCore(@"
using System;
using System.Linq.Expressions;

namespace System.Linq.Expressions
{
    public class Expression<T> {}
}

class C 
{ 
    delegate void D1(ref int x, out int y, int z);
    delegate void D2(out int x);
    void M() 
    { 
        int q1 = ()=>1;
        int q2 = delegate { return 1; };
        Func<int> q3 = x3=>1;
        Func<int, int> q4 = (System.Itn23 x4)=>1; // type mismatch error should be suppressed on error type
        Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
        D1 q6 = (double x6, ref int y6, ref int z6)=>1; 

        // COMPATIBILITY: The C# 4 compiler produces two errors:
        //
        // error CS1676: Parameter 2 must be declared with the 'out' keyword
        // error CS1688: Cannot convert anonymous method block without a parameter list 
        // to delegate type 'D1' because it has one or more out parameters
        //
        // This seems redundant (because there is no 'parameter 2' in the source code)
        // I propose that we eliminate the first error.

        D1 q7 = delegate {};

        Frob q8 = ()=>{};

        D2 q9 = x9=>{};

        D1 q10 = (x10,y10,z10)=>{}; 

        // COMPATIBILITY: THe C# 4 compiler produces two errors:
        //
        // error CS0127: Since 'System.Action' returns void, a return keyword must 
        // not be followed by an object expression
        //
        // error CS1662: Cannot convert lambda expression to delegate type 'System.Action' 
        // because some of the return types in the block are not implicitly convertible to 
        // the delegate return type
        //
        // The problem is adequately characterized by the first message; I propose we 
        // eliminate the second, which seems both redundant and wrong.

        Action q11 = ()=>{ return 1; };

        Action q12 = ()=>1;

        Func<int> q13 = ()=>{ if (false) return 1; };

        Func<int> q14 = ()=>123.456;

        // Note that the type error is still an error even if the offending 
        // return is unreachable.
        Func<double> q15 = ()=>{if (false) return 1m; else return 0; };
        // In the native compiler these errors were caught at parse time. In Roslyn, these are now semantic
        // analysis errors. See changeset 1674 for details.

        Action<int[]> q16 = delegate (params int[] p) { };
        Action<string[]> q17 = (params string[] s)=>{};
        Action<int, double[]> q18 = (int x, params double[] s)=>{};

        object q19 = new Action( (int x)=>{} );
 
        Expression<int> ex1 = ()=>1;  

    }
}");

            comp.VerifyDiagnostics(
    // (16,18): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
    //         int q1 = ()=>1;
    Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "()=>1").WithArguments("lambda expression", "int").WithLocation(16, 18),
    // (17,18): error CS1660: Cannot convert anonymous method to type 'int' because it is not a delegate type
    //         int q2 = delegate { return 1; };
    Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate { return 1; }").WithArguments("anonymous method", "int").WithLocation(17, 18),
    // (18,24): error CS1593: Delegate 'System.Func<int>' does not take 1 arguments
    //         Func<int> q3 = x3=>1;
    Diagnostic(ErrorCode.ERR_BadDelArgCount, "x3=>1").WithArguments("System.Func<int>", "1").WithLocation(18, 24),
    // (19,37): error CS0234: The type or namespace name 'Itn23' does not exist in the namespace 'System' (are you missing an assembly reference?)
    //         Func<int, int> q4 = (System.Itn23 x4)=>1; // type mismatch error should be suppressed on error type
    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Itn23").WithArguments("Itn23", "System").WithLocation(19, 37),
    // (20,35): error CS0234: The type or namespace name 'Duobel' does not exist in the namespace 'System' (are you missing an assembly reference?)
    //         Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Duobel").WithArguments("Duobel", "System").WithLocation(20, 35),
    // (20,27): error CS1593: Delegate 'System.Func<double>' does not take 1 arguments
    //         Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
    Diagnostic(ErrorCode.ERR_BadDelArgCount, "(System.Duobel x5)=>1").WithArguments("System.Func<double>", "1").WithLocation(20, 27),
    // (21,17): error CS1661: Cannot convert lambda expression to delegate type 'C.D1' because the parameter types do not match the delegate parameter types
    //         D1 q6 = (double x6, ref int y6, ref int z6)=>1; 
    Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "(double x6, ref int y6, ref int z6)=>1").WithArguments("lambda expression", "C.D1").WithLocation(21, 17),
    // (21,25): error CS1678: Parameter 1 is declared as type 'double' but should be 'ref int'
    //         D1 q6 = (double x6, ref int y6, ref int z6)=>1; 
    Diagnostic(ErrorCode.ERR_BadParamType, "x6").WithArguments("1", "", "double", "ref ", "int").WithLocation(21, 25),
    // (21,37): error CS1676: Parameter 2 must be declared with the 'out' keyword
    //         D1 q6 = (double x6, ref int y6, ref int z6)=>1; 
    Diagnostic(ErrorCode.ERR_BadParamRef, "y6").WithArguments("2", "out").WithLocation(21, 37),
    // (21,49): error CS1677: Parameter 3 should not be declared with the 'ref' keyword
    //         D1 q6 = (double x6, ref int y6, ref int z6)=>1; 
    Diagnostic(ErrorCode.ERR_BadParamExtraRef, "z6").WithArguments("3", "ref").WithLocation(21, 49),
    // (32,17): error CS1688: Cannot convert anonymous method block without a parameter list to delegate type 'C.D1' because it has one or more out parameters
    //         D1 q7 = delegate {};
    Diagnostic(ErrorCode.ERR_CantConvAnonMethNoParams, "delegate {}").WithArguments("C.D1").WithLocation(32, 17),
    // (34,9): error CS0246: The type or namespace name 'Frob' could not be found (are you missing a using directive or an assembly reference?)
    //         Frob q8 = ()=>{};
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Frob").WithArguments("Frob").WithLocation(34, 9),
    // (36,17): error CS1676: Parameter 1 must be declared with the 'out' keyword
    //         D2 q9 = x9=>{};
    Diagnostic(ErrorCode.ERR_BadParamRef, "x9").WithArguments("1", "out").WithLocation(36, 17),
    // (38,19): error CS1676: Parameter 1 must be declared with the 'ref' keyword
    //         D1 q10 = (x10,y10,z10)=>{}; 
    Diagnostic(ErrorCode.ERR_BadParamRef, "x10").WithArguments("1", "ref").WithLocation(38, 19),
    // (38,23): error CS1676: Parameter 2 must be declared with the 'out' keyword
    //         D1 q10 = (x10,y10,z10)=>{}; 
    Diagnostic(ErrorCode.ERR_BadParamRef, "y10").WithArguments("2", "out").WithLocation(38, 23),
    // (52,28): error CS8030: Anonymous function converted to a void returning delegate cannot return a value
    //         Action q11 = ()=>{ return 1; };
    Diagnostic(ErrorCode.ERR_RetNoObjectRequiredLambda, "return").WithLocation(52, 28),
    // (54,26): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         Action q12 = ()=>1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(54, 26),
    // (56,42): warning CS0162: Unreachable code detected
    //         Func<int> q13 = ()=>{ if (false) return 1; };
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(56, 42),
    // (56,25): error CS1643: Not all code paths return a value in lambda expression of type 'System.Func<int>'
    //         Func<int> q13 = ()=>{ if (false) return 1; };
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "()=>{ if (false) return 1; }").WithArguments("lambda expression", "System.Func<int>").WithLocation(56, 25),
    // (58,29): error CS0266: Cannot implicitly convert type 'double' to 'int'. An explicit conversion exists (are you missing a cast?)
    //         Func<int> q14 = ()=>123.456;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "123.456").WithArguments("double", "int").WithLocation(58, 29),
    // (58,29): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         Func<int> q14 = ()=>123.456;
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "123.456").WithArguments("lambda expression").WithLocation(58, 29),
    // (62,51): error CS0266: Cannot implicitly convert type 'decimal' to 'double'. An explicit conversion exists (are you missing a cast?)
    //         Func<double> q15 = ()=>{if (false) return 1m; else return 0; };
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1m").WithArguments("decimal", "double").WithLocation(62, 51),
    // (62,51): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         Func<double> q15 = ()=>{if (false) return 1m; else return 0; };
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "1m").WithArguments("lambda expression").WithLocation(62, 51),
    // (62,44): warning CS0162: Unreachable code detected
    //         Func<double> q15 = ()=>{if (false) return 1m; else return 0; };
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(62, 44),
    // (66,39): error CS1670: params is not valid in this context
    //         Action<int[]> q16 = delegate (params int[] p) { };
    Diagnostic(ErrorCode.ERR_IllegalParams, "params int[] p").WithLocation(66, 39),
    // (67,33): error CS1670: params is not valid in this context
    //         Action<string[]> q17 = (params string[] s)=>{};
    Diagnostic(ErrorCode.ERR_IllegalParams, "params string[] s").WithLocation(67, 33),
    // (68,45): error CS1670: params is not valid in this context
    //         Action<int, double[]> q18 = (int x, params double[] s)=>{};
    Diagnostic(ErrorCode.ERR_IllegalParams, "params double[] s").WithLocation(68, 45),
    // (70,34): error CS1593: Delegate 'System.Action' does not take 1 arguments
    //         object q19 = new Action( (int x)=>{} );
    Diagnostic(ErrorCode.ERR_BadDelArgCount, "(int x)=>{}").WithArguments("System.Action", "1").WithLocation(70, 34),
    // (72,9): warning CS0436: The type 'System.Linq.Expressions.Expression<T>' in '' conflicts with the imported type 'System.Linq.Expressions.Expression<TDelegate>' in 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
    //         Expression<int> ex1 = ()=>1;  
    Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Expression<int>").WithArguments("", "System.Linq.Expressions.Expression<T>", "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Linq.Expressions.Expression<TDelegate>").WithLocation(72, 9),
    // (72,31): error CS0835: Cannot convert lambda to an expression tree whose type argument 'int' is not a delegate type
    //         Expression<int> ex1 = ()=>1;  
    Diagnostic(ErrorCode.ERR_ExpressionTreeMustHaveDelegate, "()=>1").WithArguments("int").WithLocation(72, 31)
                );
        }

        [Fact] // 5368
        public void TestLambdaErrors02()
        {
            string code = @"
class C
{
    void M()
    {
        System.Func<int, int> del = x => x + 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(code);
            compilation.VerifyDiagnostics(); // no errors expected
        }

        [WorkItem(539538, "DevDiv")]
        [Fact]
        public void TestLambdaErrors03()
        {
            string source = @"
using System;

interface I : IComparable<IComparable<I>> { }

class C
{
    static void Foo(Func<IComparable<I>> x) { }
    static void Foo(Func<I> x) {}
    static void M()
    {
        Foo(() => null);
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Foo(Func<IComparable<I>>)' and 'C.Foo(Func<I>)'
                //         Foo(() => null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Foo").WithArguments("C.Foo(System.Func<System.IComparable<I>>)", "C.Foo(System.Func<I>)").WithLocation(12, 9));
        }

        [WorkItem(539976, "DevDiv")]
        [Fact]
        public void LambdaArgumentToOverloadedDelegate()
        {
            var text = @"class W
{
    delegate T Func<A0, T>(A0 a0);

    static int F(Func<short, int> f) { return 0; }
    static int F(Func<short, double> f) { return 1; }

    static int Main()
    {
        return F(c => c);
    }
}
";
            var comp = CreateCompilationWithMscorlib(Parse(text));
            comp.VerifyDiagnostics();
        }

        [WorkItem(528044, "DevDiv")]
        [Fact]
        public void MissingReferenceInOverloadResolution()
        {
            var text1 = @"
using System;
public static class A
{
    public static void Foo(Func<B, object> func) { }
    public static void Foo(Func<C, object> func) { }
}

public class B
{
    public Uri GetUrl()
    {
        return null;
    }
}

public class C
{
    public string GetUrl()
    {
        return null;
    }
}";

            var comp1 = CreateCompilationWithMscorlib(
                Parse(text1),
                new[] { TestReferences.NetFx.v4_0_30319.System });

            var text2 = @"
class Program
{
    static void Main()
    {
        A.Foo(x => x.GetUrl());
    }
}
";

            var comp2 = CreateCompilationWithMscorlib(
                Parse(text2),
                new[] { new CSharpCompilationReference(comp1) });

            Assert.Equal(0, comp2.GetDiagnostics().Count());
        }

        [WorkItem(528047, "DevDiv")]
        [Fact()]
        public void OverloadResolutionWithEmbeddedInteropType()
        {
            var text1 = @"
using System;
using System.Collections.Generic;
using stdole;

public static class A
{
    public static void Foo(Func<X> func) 
    { 
        System.Console.WriteLine(""X"");
}
    public static void Foo(Func<Y> func) 
    { 
        System.Console.WriteLine(""Y"");
    }
}

public delegate void X(List<IDispatch> addin);
public delegate void Y(List<string> addin);
";

            var comp1 = CreateCompilationWithMscorlib(
                Parse(text1),
                new[] { TestReferences.SymbolsTests.NoPia.StdOle.WithEmbedInteropTypes(true) },
                options: TestOptions.ReleaseDll);

            var text2 = @"
public class Program
{
    public static void Main()
    {
        A.Foo(() => delegate { });
    }
}
";

            var comp2 = CreateCompilationWithMscorlib(
                Parse(text2),
                new MetadataReference[]
                    {
                        new CSharpCompilationReference(comp1),
                        TestReferences.SymbolsTests.NoPia.StdOle.WithEmbedInteropTypes(true)
                    },
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp2, expectedOutput: "Y").Diagnostics.Verify();

            var comp3 = CreateCompilationWithMscorlib(
                Parse(text2),
                new MetadataReference[]
                    {
                        comp1.EmitToImageReference(),
                        TestReferences.SymbolsTests.NoPia.StdOle.WithEmbedInteropTypes(true)
                    },
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp3, expectedOutput: "Y").Diagnostics.Verify();
        }

        [WorkItem(6358, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InvalidExpressionInvolveLambdaOperator()
        {
            var text1 = @"
class C
{
    static void X() 
    {
        int x=0; int y=0;
        if(x-=>*y)  // CS1525
            return;

        return; 
    }
}
";

            var comp = CreateCompilationWithMscorlib(Parse(text1));
            var errs = comp.GetDiagnostics();
            Assert.True(0 < errs.Count(), "Diagnostics not empty");
            Assert.True(0 < errs.Where(e => e.Code == 1525).Select(e => e).Count(), "Diagnostics contains CS1525");
        }

        [WorkItem(540219, "DevDiv")]
        [Fact]
        public void OverloadResolutionWithStaticType()
        {
            var vbSource = @"
Imports System

Namespace Microsoft.VisualBasic.CompilerServices

    <System.AttributeUsage(System.AttributeTargets.Class, Inherited:=False, AllowMultiple:=False)>
    Public NotInheritable Class StandardModuleAttribute
      Inherits System.Attribute

      Public Sub New()
        MyBase.New()
      End Sub

    End Class

End Namespace


Public Module M
  Sub Foo(x as Action(Of String))
  End Sub
  Sub Foo(x as Action(Of GC))
  End Sub
End Module
";

            var vbProject = VisualBasic.VisualBasicCompilation.Create(
                "VBProject",
                references: new[] { MscorlibRef },
                syntaxTrees: new[] { VisualBasic.VisualBasicSyntaxTree.ParseText(vbSource) });

            var csSource = @"
class Program
{
    static void Main()
    {
        M.Foo(x => { });
    }
}
";
            var metadataStream = new MemoryStream();
            var emitResult = vbProject.Emit(metadataStream, options: new EmitOptions(metadataOnly: true));
            Assert.True(emitResult.Success);

            var csProject = CreateCompilationWithMscorlib(
                Parse(csSource),
                new[] { MetadataReference.CreateFromImage(metadataStream.ToImmutable()) });

            Assert.Equal(0, csProject.GetDiagnostics().Count());
        }

        [Fact]
        public void OverloadResolutionWithStaticTypeError()
        {
            var vbSource = @"
Imports System

Namespace Microsoft.VisualBasic.CompilerServices

    <System.AttributeUsage(System.AttributeTargets.Class, Inherited:=False, AllowMultiple:=False)>
    Public NotInheritable Class StandardModuleAttribute
      Inherits System.Attribute

      Public Sub New()
        MyBase.New()
      End Sub

    End Class

End Namespace

Public Module M
  Public Dim F As Action(Of GC)
End Module
";

            var vbProject = VisualBasic.VisualBasicCompilation.Create(
                "VBProject",
                references: new[] { MscorlibRef },
                syntaxTrees: new[] { VisualBasic.VisualBasicSyntaxTree.ParseText(vbSource) });

            var csSource = @"
class Program
{
    static void Main()
    {
        M.F = x=>{};
    }
}
";
            var vbMetadata = vbProject.EmitToArray(options: new EmitOptions(metadataOnly: true));
            var csProject = CreateCompilationWithMscorlib(Parse(csSource), new[] { MetadataReference.CreateFromImage(vbMetadata) });

            var diagnostics = csProject.GetDiagnostics().Select(DumpDiagnostic);
            Assert.Equal(1, diagnostics.Count());
            Assert.Equal("'x' error CS0721: 'GC': static types cannot be used as parameters", diagnostics.First());
        }

        [WorkItem(540251, "DevDiv")]
        [Fact]
        public void AttributesCannotBeUsedInAnonymousMethods()
        {
            var csSource = @"
using System;

class Program
{
    static void Main()
    {
        const string message = ""The parameter is obsolete"";
        Action<int> a = delegate ([ObsoleteAttribute(message)] int x) { };
    }
}
";

            var csProject = CreateCompilationWithMscorlib(csSource);

            var emitResult = csProject.Emit(Stream.Null);
            Assert.False(emitResult.Success);
            Assert.True(emitResult.Diagnostics.Any());
            // TODO: check error code
        }

        [WorkItem(540263, "DevDiv")]
        [Fact]
        public void ErrorsInUnboundLambdas()
        {
            var csSource = @"using System;

class Program
{
    static void Main()
    {
        ((Func<int>)delegate { return """"; })();
        ((Func<int>)delegate { })();
        ((Func<int>)delegate { 1 / 0; })();
    }
}
";

            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
    // (7,39): error CS0029: Cannot implicitly convert type 'string' to 'int'
    //         ((Func<int>)delegate { return ""; })();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(7, 39),
    // (7,39): error CS1662: Cannot convert anonymous method to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         ((Func<int>)delegate { return ""; })();
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, @"""""").WithArguments("anonymous method").WithLocation(7, 39),
    // (8,21): error CS1643: Not all code paths return a value in anonymous method of type 'System.Func<int>'
    //         ((Func<int>)delegate { })();
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate { }").WithArguments("anonymous method", "System.Func<int>").WithLocation(8, 21),
    // (9,32): error CS0020: Division by constant zero
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1 / 0").WithLocation(9, 32),
    // (9,32): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_IllegalStatement, "1 / 0").WithLocation(9, 32),
    // (9,21): error CS1643: Not all code paths return a value in anonymous method of type 'System.Func<int>'
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate { 1 / 0; }").WithArguments("anonymous method", "System.Func<int>").WithLocation(9, 21)
        );
        }

        [WorkItem(540181, "DevDiv")]
        [Fact]
        public void ErrorInLambdaArgumentList()
        {
            var csSource = @"using System;

class Program
{
    public Program(string x) : this(() => x) { }
    static void Main(string[] args)
    {
        ((Action<string>)(f => Console.WriteLine(f)))(nulF);
    }
}";

            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
            // (5,37): error CS1660: Cannot convert lambda expression to type 'string' because it is not a delegate type
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, @"() => x").WithArguments("lambda expression", "string"),
            // (8,55): error CS0103: The name 'nulF' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, @"nulF").WithArguments("nulF"));
        }

        [WorkItem(541725, "DevDiv")]
        [Fact]
        public void DelegateCreationIsNotStatement()
        {
            var csSource = @"
delegate void D();
class Program
{
    public static void Main(string[] args)
    {
        D d = () => new D(() => { });
        new D(()=>{});
    }
}";

            // Though it is legal to have an object-creation-expression, because it might be useful
            // for its side effects, a delegate-creation-expression is not allowed as a
            // statement expression.

            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
                // (7,21): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         D d = () => new D(() => { });
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new D(() => { })"),
                // (8,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         new D(()=>{});
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new D(()=>{})"));
        }

        [WorkItem(542336, "DevDiv")]
        [Fact]
        public void ThisInStaticContext()
        {
            var csSource = @"
delegate void D();
class Program
{
    public static void Main(string[] args)
    {
        D d = () => {
            object o = this;
        };
    }
}";
            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
                // (8,24): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //             object o = this;
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this")
                );
        }

        [WorkItem(542431, "DevDiv")]
        [Fact]
        public void LambdaHasMoreParametersThanDelegate()
        {
            var csSource = @"
class C
{
    static void Main()
    {
        System.Func<int> f = new System.Func<int>(r => 0);
    }
}";
            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
                // (6,51): error CS1593: Delegate 'System.Func<int>' does not take 1 arguments
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "r => 0").WithArguments("System.Func<int>", "1"));
        }

        [Fact, WorkItem(529054, "DevDiv")]
        public void LambdaInDynamicCall()
        {
            var source = @"
public class Program
{
    static void Main()
    {
        dynamic b = new string[] { ""AA"" };
        bool exists = System.Array.Exists(b, o => o != ""BB"");
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (7,46): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
    //         bool exists = System.Array.Exists(b, o => o != "BB");
    Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, @"o => o != ""BB""")
                );
        }

        [Fact, WorkItem(529389, "DevDiv")]
        public void ParenthesizedLambdaInCastExpression()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        int x = 1;
        byte y = (byte) (x + x);
        Func<int> f1 = (() => { return 1; });
        Func<int> f2 = (Func<int>) (() => { return 2; });
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            ExpressionSyntax expr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().
                Where(e => e.Kind() == SyntaxKind.AddExpression).Single();

            var tinfo = model.GetTypeInfo(expr);
            var conv = model.GetConversion(expr);
            // Not byte
            Assert.Equal("int", tinfo.Type.ToDisplayString());

            var exprs = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>();
            expr = exprs.First();
            tinfo = model.GetTypeInfo(expr);
            conv = model.GetConversion(expr);
            Assert.True(conv.IsAnonymousFunction, "LambdaConversion");
            Assert.Null(tinfo.Type);
            var sym = model.GetSymbolInfo(expr).Symbol;
            Assert.NotNull(sym);
            Assert.Equal(SymbolKind.Method, sym.Kind);
            Assert.Equal(MethodKind.AnonymousFunction, (sym as MethodSymbol).MethodKind);

            expr = exprs.Last();
            tinfo = model.GetTypeInfo(expr);
            conv = model.GetConversion(expr);
            Assert.True(conv.IsAnonymousFunction, "LambdaConversion");
            Assert.Null(tinfo.Type);
            sym = model.GetSymbolInfo(expr).Symbol;
            Assert.NotNull(sym);
            Assert.Equal(SymbolKind.Method, sym.Kind);
            Assert.Equal(MethodKind.AnonymousFunction, (sym as MethodSymbol).MethodKind);
        }

        [WorkItem(544594, "DevDiv")]
        [Fact]
        public void LambdaInEnumMemberDecl()
        {
            var csSource = @"
public class TestClass
{
    public enum Test { aa = ((System.Func<int>)(() => 1))() }
    Test MyTest = Test.aa;
    public static void Main()
    {
    }
}
";
            CreateCompilationWithMscorlib(csSource).VerifyDiagnostics(
                // (4,29): error CS0133: The expression being assigned to 'TestClass.Test.aa' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "((System.Func<int>)(() => 1))()").WithArguments("TestClass.Test.aa"),
                // (5,10): warning CS0414: The field 'TestClass.MyTest' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "MyTest").WithArguments("TestClass.MyTest"));
        }

        [WorkItem(544932, "DevDiv")]
        [Fact]
        public void AnonymousLambdaInEnumSubtraction()
        {
            string source = @"
class Test
{
    enum E1 : byte
    {
        A = byte.MinValue,
        C = 1
    }

    static void Main()
    {
        int j = ((System.Func<Test.E1>)(() => E1.A))() - E1.C;
        System.Console.WriteLine(j);
    }
}
";
            string expectedOutput = @"255";

            CompileAndVerify(new[] { source }, expectedOutput: expectedOutput);
        }

        [WorkItem(545156, "DevDiv")]
        [Fact]
        public void SpeculativelyBindOverloadResolution()
        {
            string source = @"
using System;
using System.Collections;
using System.Collections.Generic;
 
class Program
{
    static void Main()
    {
        Foo(() => () => { var x = (IEnumerable<int>)null; return x; });
    }
 
    static void Foo(Func<Func<IEnumerable>> x) { }
    static void Foo(Func<Func<IFormattable>> x) { }
}
";
            var compilation = CreateCompilationWithMscorlib(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var invocation = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            // Used to throw a NRE because of the ExpressionSyntax's null SyntaxTree.
            model.GetSpeculativeSymbolInfo(
                invocation.SpanStart,
                SyntaxFactory.ParseExpression("Foo(() => () => { var x = null; return x; })"), // cast removed
                SpeculativeBindingOption.BindAsExpression);
        }

        [WorkItem(545343, "DevDiv")]
        [Fact]
        public void LambdaUsingFieldInConstructor()
        {
            string source = @"
using System;

public class Derived
{
    int field = 1;

    Derived()
    {
        int local = 2;

        // A lambda that captures a local and refers to an instance field.
        Action a = () => Console.WriteLine(""Local = {0}, Field = {1}"", local, field); 

        // NullReferenceException if the ""this"" field of the display class hasn't been set.
        a();
    }

    public static void Main()
    {
        Derived d = new Derived();
    }
}";
            CompileAndVerify(source, expectedOutput: "Local = 2, Field = 1");
        }

        [WorkItem(642222, "DevDiv")]
        [Fact]
        public void SpeculativelyBindOverloadResolutionAndInferenceWithError()
        {
            string source = @"
using System;using System.Linq.Expressions;
namespace IntellisenseBug
{
    public class Program
    {
        void M(Mapper<FromData, ToData> mapper)
        {
            // Intellisense is broken here when you type . after the x:
            mapper.Map(x => x/* */.
        }
    }
    public class Mapper<TTypeFrom, TTypeTo>
    {
        public void Map<TPropertyFrom, TPropertyTo>(
            Expression<Func<TTypeFrom, TPropertyFrom>> from,
            Expression<Func<TTypeTo, TPropertyTo>> to)
        { }
    }
    public class FromData
    {
        public int Int { get; set; }
        public string String { get; set; }
    }
    public class ToData
    {
        public int Id { get; set; }
        public string Name
        {
            get; set;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            // We don't actually require any particular diagnostics, but these are what we get.
            compilation.VerifyDiagnostics(
                // (10,36): error CS1001: Identifier expected
                //             mapper.Map(x => x/* */.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
                // (10,36): error CS1026: ) expected
                //             mapper.Map(x => x/* */.
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ""),
                // (10,36): error CS1002: ; expected
                //             mapper.Map(x => x/* */.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "")
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var xReference =
                tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ExpressionSyntax>()
                .Where(e => e.ToFullString() == "x/* */")
                .Last();
            var typeInfo = model.GetTypeInfo(xReference);
            Assert.NotNull(((TypeSymbol)typeInfo.Type).GetMember("String"));
        }

        [WorkItem(722288, "DevDiv")]
        [Fact]
        public void CompletionInLambdaInIncompleteInvocation()
        {
            string source = @"
using System;
using System.Linq.Expressions;
 
public class SomeType
{
    public string SomeProperty { get; set; }
}
public class IntelliSenseError
{
    public static void Test1<T>(Expression<Func<T, object>> expr)
    {
        Console.WriteLine(((MemberExpression)expr.Body).Member.Name);
    }
    public static void Test2<T>(Expression<Func<T, object>> expr, bool additionalParameter)
    {
        Test1(expr);
    }
    public static void Main()
    {
        Test2<SomeType>(o => o/* */.
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            // We don't actually require any particular diagnostics, but these are what we get.
            compilation.VerifyDiagnostics(
                // (21,37): error CS1001: Identifier expected
                //         Test2<SomeType>(o => o/* */.
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
                // (21,37): error CS1026: ) expected
                //         Test2<SomeType>(o => o/* */.
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ""),
                // (21,37): error CS1002: ; expected
                //         Test2<SomeType>(o => o/* */.
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "")
                );
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var oReference =
                tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<NameSyntax>()
                .Where(e => e.ToFullString() == "o/* */")
                .Last();
            var typeInfo = model.GetTypeInfo(oReference);
            Assert.NotNull(((TypeSymbol)typeInfo.Type).GetMember("SomeProperty"));
        }

        [WorkItem(871896, "DevDiv")]
        [Fact]
        public void Bug871896()
        {
            string source = @"
using System.Threading;
using System.Threading.Tasks;
class TestDataPointBase
{
    private readonly IVisualStudioIntegrationService integrationService;
    protected void TryGetDocumentId(CancellationToken token)
    {
        DocumentId documentId = null;
        if (!await Task.Run(() => this.integrationService.TryGetDocumentId(null, out documentId), token).ConfigureAwait(false))
        {
        }
    }
}

";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var oReference =
                tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<ExpressionSyntax>()
                .OrderByDescending(s => s.SpanStart);

            foreach (var name in oReference)
            {
                CSharpExtensions.GetSymbolInfo(model, name);
            }

            // We should get a bunch of errors, but no asserts.
            compilation.VerifyDiagnostics(
    // (6,22): error CS0246: The type or namespace name 'IVisualStudioIntegrationService' could not be found (are you missing a using directive or an assembly reference?)
    //     private readonly IVisualStudioIntegrationService integrationService;
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "IVisualStudioIntegrationService").WithArguments("IVisualStudioIntegrationService").WithLocation(6, 22),
    // (9,9): error CS0246: The type or namespace name 'DocumentId' could not be found (are you missing a using directive or an assembly reference?)
    //         DocumentId documentId = null;
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "DocumentId").WithArguments("DocumentId").WithLocation(9, 9),
    // (10,25): error CS0117: 'System.Threading.Tasks.Task' does not contain a definition for 'Run'
    //         if (!await Task.Run(() => this.integrationService.TryGetDocumentId(null, out documentId), token).ConfigureAwait(false))
    Diagnostic(ErrorCode.ERR_NoSuchMember, "Run").WithArguments("System.Threading.Tasks.Task", "Run").WithLocation(10, 25),
    // (10,14): error CS4033: The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
    //         if (!await Task.Run(() => this.integrationService.TryGetDocumentId(null, out documentId), token).ConfigureAwait(false))
    Diagnostic(ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod, "await Task.Run(() => this.integrationService.TryGetDocumentId(null, out documentId), token).ConfigureAwait(false)").WithLocation(10, 14),
    // (6,54): warning CS0649: Field 'TestDataPointBase.integrationService' is never assigned to, and will always have its default value null
    //     private readonly IVisualStudioIntegrationService integrationService;
    Diagnostic(ErrorCode.WRN_UnassignedInternalField, "integrationService").WithArguments("TestDataPointBase.integrationService", "null").WithLocation(6, 54)
                );
        }

        [Fact, WorkItem(960755, "DevDiv")]
        public void Bug960755_01()
        {
            var source = @"
using System.Collections.Generic;
 
class C
{
    static void M(IList<C> c)
    {
        var tmp = new C();
        tmp.M((a, b) => c.Add);
    }
}

";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single().Body;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(960755, "DevDiv")]
        public void Bug960755_02()
        {
            var source = @"
using System.Collections.Generic;
 
class C
{
    static void M(IList<C> c)
    {
        int tmp = c.Add;
    }
}

";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(960755, "DevDiv")]
        public void Bug960755_03()
        {
            var source = @"
using System.Collections.Generic;
 
class C
{
    static void M(IList<C> c)
    {
        var tmp = new C();
        tmp.M((a, b) => c.Add);
    }

    static void M(System.Func<int, int, System.Action<C>> x)
    {}
}

";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilationWithMscorlib(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single().Body;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [WorkItem(1112875, "DevDiv")]
        [Fact]
        public void Bug1112875_1()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System;
 
class Program
{
    static void Main()
    {
        ICloneable c = """";
        Foo(() => (c.Clone()), null);
    }
 
    static void Foo(Action x, string y) { }
    static void Foo(Func<object> x, object y) { Console.WriteLine(42); }
}", options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [WorkItem(1112875, "DevDiv")]
        [Fact]
        public void Bug1112875_2()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Program
{
    void M()
    {
        var d = new System.Action(() => (new object()));
    }
}
");
            comp.VerifyDiagnostics(
                // (6,41): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         var d = new System.Action(() => (new object()));
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(new object())").WithLocation(6, 41));
        }

        [WorkItem(1830, "https://github.com/dotnet/roslyn/issues/1830")]
        [Fact]
        public void FuncOfVoid()
        {
            var comp = CreateCompilationWithMscorlib(@"
using System;
class Program
{
    void M1<T>(Func<T> f) {}
    void Main(string[] args)
    {
        M1(() => { return System.Console.Beep(); });
    }
}
");
            comp.VerifyDiagnostics(
                // (8,27): error CS4029: Cannot return an expression of type 'void'
                //         M1(() => { return System.Console.Beep(); });
                Diagnostic(ErrorCode.ERR_CantReturnVoid, "System.Console.Beep()").WithLocation(8, 27)
                );
        }

        [Fact, WorkItem(1179899, "DevDiv")]
        public void ParameterReference_01()
        {
            var src = @"
using System;

class Program
{
    static Func<Program, string> stuff()
    {
        return a => a.
    }
}
";
            var compilation = CreateCompilationWithMscorlib(src);
            compilation.VerifyDiagnostics(
    // (8,23): error CS1001: Identifier expected
    //         return a => a.
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(8, 23),
    // (8,23): error CS1002: ; expected
    //         return a => a.
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 23)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();

            Assert.Equal("a.", node.Parent.ToString().Trim());

            var semanticModel = compilation.GetSemanticModel(tree);
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            Assert.Equal("Program a", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1179899, "DevDiv")]
        public void ParameterReference_02()
        {
            var src = @"
using System;

class Program
{
    static void stuff()
    {
        Func<Program, string> l = a => a.
    }
}
";
            var compilation = CreateCompilationWithMscorlib(src);
            compilation.VerifyDiagnostics(
    // (8,42): error CS1001: Identifier expected
    //         Func<Program, string> l = a => a.
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(8, 42),
    // (8,42): error CS1002: ; expected
    //         Func<Program, string> l = a => a.
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(8, 42)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();

            Assert.Equal("a.", node.Parent.ToString().Trim());

            var semanticModel = compilation.GetSemanticModel(tree);
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            Assert.Equal("Program a", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1179899, "DevDiv")]
        public void ParameterReference_03()
        {
            var src = @"
using System;

class Program
{
    static void stuff()
    {
         M1(a => a.);
    }

    static void M1(Func<Program, string> l){}
}
";
            var compilation = CreateCompilationWithMscorlib(src);
            compilation.VerifyDiagnostics(
    // (8,20): error CS1001: Identifier expected
    //          M1(a => a.);
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(8, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();

            Assert.Equal("a.", node.Parent.ToString().Trim());

            var semanticModel = compilation.GetSemanticModel(tree);
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            Assert.Equal("Program a", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(1179899, "DevDiv")]
        public void ParameterReference_04()
        {
            var src = @"
using System;

class Program
{
    static void stuff()
    {
        var l = (Func<Program, string>) (a => a.);
    }
}
";
            var compilation = CreateCompilationWithMscorlib(src);
            compilation.VerifyDiagnostics(
    // (8,49): error CS1001: Identifier expected
    //         var l = (Func<Program, string>) (a => a.);
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(8, 49)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "a").Single();

            Assert.Equal("a.", node.Parent.ToString().Trim());

            var semanticModel = compilation.GetSemanticModel(tree);
            var symbolInfo = semanticModel.GetSymbolInfo(node);

            Assert.Equal("Program a", symbolInfo.Symbol.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(3826, "https://github.com/dotnet/roslyn/issues/3826")]
        public void ExpressionTreeSelfAssignmentShouldError()
        {
            var source = @"
using System;
using System.Linq.Expressions;

class Program
{
    static void Main()
    {
        Expression<Func<int, int>> x = y => y = y;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (9,45): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         Expression<Func<int, int>> x = y => y = y;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "y = y").WithLocation(9, 45),
                // (9,45): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<int, int>> x = y => y = y;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "y = y").WithLocation(9, 45));
        }

        [Fact, WorkItem(5363, "https://github.com/dotnet/roslyn/issues/5363")]
        public void ReturnInferenceCache_Dynamic_vs_Object_01()
        {
            var source =
@"
using System;
using System.Collections;
using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
        IEnumerable<dynamic> dynX = null;

        // CS1061 'object' does not contain a definition for 'Text'...
        // tooltip on 'var' shows IColumn instead of IEnumerable<dynamic>
        var result = dynX.Select(_ => _.Text);
    }

    public static IColumn Select<TResult>(this IColumn source, Func<object, TResult> selector)
    {
        throw new NotImplementedException();
    }

    public static IEnumerable<S> Select<T, S>(this IEnumerable<T> source, Func<T, S> selector)
    {
        System.Console.WriteLine(""Select<T, S>"");
        return null;
    }
}

public interface IColumn { }
";
            var compilation = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "Select<T, S>");
        }

        [Fact, WorkItem(5363, "https://github.com/dotnet/roslyn/issues/5363")]
        public void ReturnInferenceCache_Dynamic_vs_Object_02()
        {
            var source =
@"
using System;
using System.Collections;
using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
        IEnumerable<dynamic> dynX = null;

        // CS1061 'object' does not contain a definition for 'Text'...
        // tooltip on 'var' shows IColumn instead of IEnumerable<dynamic>
        var result = dynX.Select(_ => _.Text);
    }

    public static IEnumerable<S> Select<T, S>(this IEnumerable<T> source, Func<T, S> selector)
    {
        System.Console.WriteLine(""Select<T, S>"");
        return null;
    }

    public static IColumn Select<TResult>(this IColumn source, Func<object, TResult> selector)
    {
        throw new NotImplementedException();
    }
}

public interface IColumn { }
";
            var compilation = CreateCompilationWithMscorlib(source, new[] { SystemCoreRef, CSharpRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "Select<T, S>");
        }

        [Fact, WorkItem(4527, "https://github.com/dotnet/roslyn/issues/4527")]
        public void AnonymousMethodExpressionWithoutParameterList()
        {
            var source =
@"
using System;
using System.Threading.Tasks;

namespace RoslynAsyncDelegate
{
    class Program
    {
        static EventHandler MyEvent;

        static void Main(string[] args)
        {
           MyEvent += async delegate { await Task.Delay(0); };
        }
    }
}

";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.AnonymousMethodExpression)).Single();

            Assert.Equal("async delegate { await Task.Delay(0); }", node1.ToString());

            Assert.Equal("void System.EventHandler.Invoke(System.Object sender, System.EventArgs e)", model.GetTypeInfo(node1).ConvertedType.GetMembers("Invoke").Single().ToTestDisplayString());

            var lambdaParameters = ((MethodSymbol)(model.GetSymbolInfo(node1)).Symbol).Parameters;

            Assert.Equal("System.Object <sender>", lambdaParameters[0].ToTestDisplayString());
            Assert.Equal("System.EventArgs <e>", lambdaParameters[1].ToTestDisplayString());

            CompileAndVerify(compilation);
        }
    }
}
