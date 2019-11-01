// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaTests : CompilingTestBase
    {
        [Fact, WorkItem(37456, "https://github.com/dotnet/roslyn/issues/37456")]
        public void Verify37456()
        {
            var comp = CreateCompilation(@"
using System;
using System.Collections.Generic;
using System.Linq;

public static partial class EnumerableEx
{
    public static void Join1<TA, TKey, T>(this IEnumerable<TA> a, Func<TA, TKey> aKey, Func<TA, T> aSel, Func<TA, TA, T> sel)
    {
        KeyValuePair<TK, TV> Pair<TK, TV>(TK k, TV v) => new KeyValuePair<TK, TV>(k, v);

        _ = a.GroupJoin(a, aKey, aKey, (f, ss) => Pair(f, ss.Select(s => Pair(true, s)))); // simplified repro
    }

    public static IEnumerable<T> Join2<TA, TB, TKey, T>(this IEnumerable<TA> a, IEnumerable<TB> b, Func<TA, TKey> aKey, Func<TB, TKey> bKey, Func<TA, T> aSel, Func<TA, TB, T> sel, IEqualityComparer<TKey> comp) 
    {
        KeyValuePair<TK, TV> Pair<TK, TV>(TK k, TV v) => new KeyValuePair<TK, TV>(k, v);

        return
            from j in a.GroupJoin(b, aKey, bKey, (f, ss) => Pair(f, from s in ss select Pair(true, s)), comp)
            from s in j.Value.DefaultIfEmpty()
            select s.Key ? sel(j.Key, s.Value) : aSel(j.Key);
    }
}");

            comp.VerifyDiagnostics();
            CompileAndVerify(comp);
            // emitting should not hang
        }

        [Fact, WorkItem(608181, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/608181")]
        public void BadInvocationInLambda()
        {
            var src = @"
using System;
using System.Linq.Expressions;

class C
{
    Expression<Action<dynamic>> e = x => new object[](x);
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(src);
            comp.VerifyDiagnostics(
                // (7,52): error CS1586: Array creation must have array size or array initializer
                //     Expression<Action<dynamic>> e = x => new object[](x);
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(7, 52)
                );
        }

        [Fact]
        public void TestLambdaErrors01()
        {
            var comp = CreateCompilationWithMscorlib40AndSystemCore(@"
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

        // COMPATIBILITY: The C# 4 compiler produces two errors:
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
    // (18,24): error CS1593: Delegate 'Func<int>' does not take 1 arguments
    //         Func<int> q3 = x3=>1;
    Diagnostic(ErrorCode.ERR_BadDelArgCount, "x3=>1").WithArguments("System.Func<int>", "1").WithLocation(18, 24),
    // (19,37): error CS0234: The type or namespace name 'Itn23' does not exist in the namespace 'System' (are you missing an assembly reference?)
    //         Func<int, int> q4 = (System.Itn23 x4)=>1; // type mismatch error should be suppressed on error type
    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Itn23").WithArguments("Itn23", "System").WithLocation(19, 37),
    // (20,35): error CS0234: The type or namespace name 'Duobel' does not exist in the namespace 'System' (are you missing an assembly reference?)
    //         Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
    Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Duobel").WithArguments("Duobel", "System").WithLocation(20, 35),
    // (20,27): error CS1593: Delegate 'Func<double>' does not take 1 arguments
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
    // (54,26): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         Action q12 = ()=>1;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "1").WithLocation(54, 26),
    // (56,42): warning CS0162: Unreachable code detected
    //         Func<int> q13 = ()=>{ if (false) return 1; };
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(56, 42),
    // (56,27): error CS1643: Not all code paths return a value in lambda expression of type 'Func<int>'
    //         Func<int> q13 = ()=>{ if (false) return 1; };
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "=>").WithArguments("lambda expression", "System.Func<int>").WithLocation(56, 27),
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
    // (70,34): error CS1593: Delegate 'Action' does not take 1 arguments
    //         object q19 = new Action( (int x)=>{} );
    Diagnostic(ErrorCode.ERR_BadDelArgCount, "(int x)=>{}").WithArguments("System.Action", "1").WithLocation(70, 34),
    // (72,9): warning CS0436: The type 'Expression<T>' in '' conflicts with the imported type 'Expression<TDelegate>' in 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
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
            var compilation = CreateCompilation(code);
            compilation.VerifyDiagnostics(); // no errors expected
        }

        [WorkItem(539538, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539538")]
        [Fact]
        public void TestLambdaErrors03()
        {
            string source = @"
using System;

interface I : IComparable<IComparable<I>> { }

class C
{
    static void Goo(Func<IComparable<I>> x) { }
    static void Goo(Func<I> x) {}
    static void M()
    {
        Goo(() => null);
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (12,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.Goo(Func<IComparable<I>>)' and 'C.Goo(Func<I>)'
                //         Goo(() => null);
                Diagnostic(ErrorCode.ERR_AmbigCall, "Goo").WithArguments("C.Goo(System.Func<System.IComparable<I>>)", "C.Goo(System.Func<I>)").WithLocation(12, 9));
        }

        [WorkItem(18645, "https://github.com/dotnet/roslyn/issues/18645")]
        [Fact]
        public void LambdaExpressionTreesErrors()
        {
            string source = @"
using System;
using System.Linq.Expressions;

class C
{
    void M()
    {
        Expression<Func<int,int>> ex1 = () => 1;
        Expression<Func<int,int>> ex2 = (double d) => 1;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (9,41): error CS1593: Delegate 'Func<int, int>' does not take 0 arguments
                //         Expression<Func<int,int>> ex1 = () => 1;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "() => 1").WithArguments("System.Func<int, int>", "0").WithLocation(9, 41),
                // (10,41): error CS1661: Cannot convert lambda expression to type 'Expression<Func<int, int>>' because the parameter types do not match the delegate parameter types
                //         Expression<Func<int,int>> ex2 = (double d) => 1;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "(double d) => 1").WithArguments("lambda expression", "System.Linq.Expressions.Expression<System.Func<int, int>>").WithLocation(10, 41),
                // (10,49): error CS1678: Parameter 1 is declared as type 'double' but should be 'int'
                //         Expression<Func<int,int>> ex2 = (double d) => 1;
                Diagnostic(ErrorCode.ERR_BadParamType, "d").WithArguments("1", "", "double", "", "int").WithLocation(10, 49));
        }

        [WorkItem(539976, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539976")]
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
            var comp = CreateCompilation(Parse(text));
            comp.VerifyDiagnostics();
        }

        [WorkItem(528044, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528044")]
        [Fact]
        public void MissingReferenceInOverloadResolution()
        {
            var text1 = @"
using System;
public static class A
{
    public static void Goo(Func<B, object> func) { }
    public static void Goo(Func<C, object> func) { }
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

            var comp1 = CreateCompilationWithMscorlib40(
                new[] { Parse(text1) },
                new[] { TestReferences.NetFx.v4_0_30319.System });

            var text2 = @"
class Program
{
    static void Main()
    {
        A.Goo(x => x.GetUrl());
    }
}
";

            var comp2 = CreateCompilationWithMscorlib40(
                new[] { Parse(text2) },
                new[] { new CSharpCompilationReference(comp1) });

            Assert.Equal(0, comp2.GetDiagnostics().Count());
        }

        [WorkItem(528047, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528047")]
        [Fact()]
        public void OverloadResolutionWithEmbeddedInteropType()
        {
            var text1 = @"
using System;
using System.Collections.Generic;
using stdole;

public static class A
{
    public static void Goo(Func<X> func) 
    { 
        System.Console.WriteLine(""X"");
}
    public static void Goo(Func<Y> func) 
    { 
        System.Console.WriteLine(""Y"");
    }
}

public delegate void X(List<IDispatch> addin);
public delegate void Y(List<string> addin);
";

            var comp1 = CreateCompilation(
                Parse(text1),
                new[] { TestReferences.SymbolsTests.NoPia.StdOle.WithEmbedInteropTypes(true) },
                options: TestOptions.ReleaseDll);

            var text2 = @"
public class Program
{
    public static void Main()
    {
        A.Goo(() => delegate { });
    }
}
";

            var comp2 = CreateCompilation(
                Parse(text2),
                new MetadataReference[]
                    {
                        new CSharpCompilationReference(comp1),
                        TestReferences.SymbolsTests.NoPia.StdOle.WithEmbedInteropTypes(true)
                    },
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp2, expectedOutput: "Y").Diagnostics.Verify();

            var comp3 = CreateCompilation(
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

            var comp = CreateCompilation(Parse(text1));
            var errs = comp.GetDiagnostics();
            Assert.True(0 < errs.Count(), "Diagnostics not empty");
            Assert.True(0 < errs.Where(e => e.Code == 1525).Select(e => e).Count(), "Diagnostics contains CS1525");
        }

        [WorkItem(540219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540219")]
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
  Sub Goo(x as Action(Of String))
  End Sub
  Sub Goo(x as Action(Of GC))
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
        M.Goo(x => { });
    }
}
";
            var metadataStream = new MemoryStream();
            var emitResult = vbProject.Emit(metadataStream, options: new EmitOptions(metadataOnly: true));
            Assert.True(emitResult.Success);

            var csProject = CreateCompilation(
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
            var csProject = CreateCompilation(Parse(csSource), new[] { MetadataReference.CreateFromImage(vbMetadata) });

            var diagnostics = csProject.GetDiagnostics().Select(DumpDiagnostic);
            Assert.Equal(1, diagnostics.Count());
            Assert.Equal("'x' error CS0721: 'GC': static types cannot be used as parameters", diagnostics.First());
        }

        [WorkItem(540251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540251")]
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

            var csProject = CreateCompilation(csSource);

            var emitResult = csProject.Emit(Stream.Null);
            Assert.False(emitResult.Success);
            Assert.True(emitResult.Diagnostics.Any());
            // TODO: check error code
        }

        [WorkItem(540263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540263")]
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

            CreateCompilation(csSource).VerifyDiagnostics(
    // (7,39): error CS0029: Cannot implicitly convert type 'string' to 'int'
    //         ((Func<int>)delegate { return ""; })();
    Diagnostic(ErrorCode.ERR_NoImplicitConv, @"""""").WithArguments("string", "int").WithLocation(7, 39),
    // (7,39): error CS1662: Cannot convert anonymous method to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
    //         ((Func<int>)delegate { return ""; })();
    Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, @"""""").WithArguments("anonymous method").WithLocation(7, 39),
    // (8,21): error CS1643: Not all code paths return a value in anonymous method of type 'Func<int>'
    //         ((Func<int>)delegate { })();
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "System.Func<int>").WithLocation(8, 21),
    // (9,32): error CS0020: Division by constant zero
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_IntDivByZero, "1 / 0").WithLocation(9, 32),
    // (9,32): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_IllegalStatement, "1 / 0").WithLocation(9, 32),
    // (9,21): error CS1643: Not all code paths return a value in anonymous method of type 'Func<int>'
    //         ((Func<int>)delegate { 1 / 0; })();
    Diagnostic(ErrorCode.ERR_AnonymousReturnExpected, "delegate").WithArguments("anonymous method", "System.Func<int>").WithLocation(9, 21)
            );
        }

        [WorkItem(540181, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540181")]
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

            CreateCompilation(csSource).VerifyDiagnostics(
            // (5,37): error CS1660: Cannot convert lambda expression to type 'string' because it is not a delegate type
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, @"() => x").WithArguments("lambda expression", "string"),
            // (8,55): error CS0103: The name 'nulF' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, @"nulF").WithArguments("nulF"));
        }

        [WorkItem(541725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541725")]
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

            CreateCompilation(csSource).VerifyDiagnostics(
                // (7,21): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         D d = () => new D(() => { });
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new D(() => { })"),
                // (8,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         new D(()=>{});
                Diagnostic(ErrorCode.ERR_IllegalStatement, "new D(()=>{})"));
        }

        [WorkItem(542336, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542336")]
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
            CreateCompilation(csSource).VerifyDiagnostics(
                // (8,24): error CS0026: Keyword 'this' is not valid in a static property, static method, or static field initializer
                //             object o = this;
                Diagnostic(ErrorCode.ERR_ThisInStaticMeth, "this")
                );
        }

        [WorkItem(542431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542431")]
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
            CreateCompilation(csSource).VerifyDiagnostics(
                // (6,51): error CS1593: Delegate 'System.Func<int>' does not take 1 arguments
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "r => 0").WithArguments("System.Func<int>", "1"));
        }

        [Fact, WorkItem(529054, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529054")]
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
            CreateCompilation(source).VerifyDiagnostics(
    // (7,46): error CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
    //         bool exists = System.Array.Exists(b, o => o != "BB");
    Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, @"o => o != ""BB""")
                );
        }

        [Fact, WorkItem(529389, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529389")]
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
            var comp = CreateCompilation(tree);
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
            Assert.Equal(MethodKind.AnonymousFunction, (sym as IMethodSymbol).MethodKind);

            expr = exprs.Last();
            tinfo = model.GetTypeInfo(expr);
            conv = model.GetConversion(expr);
            Assert.True(conv.IsAnonymousFunction, "LambdaConversion");
            Assert.Null(tinfo.Type);
            sym = model.GetSymbolInfo(expr).Symbol;
            Assert.NotNull(sym);
            Assert.Equal(SymbolKind.Method, sym.Kind);
            Assert.Equal(MethodKind.AnonymousFunction, (sym as IMethodSymbol).MethodKind);
        }

        [WorkItem(544594, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544594")]
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
            CreateCompilation(csSource).VerifyDiagnostics(
                // (4,29): error CS0133: The expression being assigned to 'TestClass.Test.aa' must be constant
                Diagnostic(ErrorCode.ERR_NotConstantExpression, "((System.Func<int>)(() => 1))()").WithArguments("TestClass.Test.aa"),
                // (5,10): warning CS0414: The field 'TestClass.MyTest' is assigned but its value is never used
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "MyTest").WithArguments("TestClass.MyTest"));
        }

        [WorkItem(544932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544932")]
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

        [WorkItem(545156, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545156")]
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
        Goo(() => () => { var x = (IEnumerable<int>)null; return x; });
    }
 
    static void Goo(Func<Func<IEnumerable>> x) { }
    static void Goo(Func<Func<IFormattable>> x) { }
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var invocation = tree.GetCompilationUnitRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            // Used to throw a NRE because of the ExpressionSyntax's null SyntaxTree.
            model.GetSpeculativeSymbolInfo(
                invocation.SpanStart,
                SyntaxFactory.ParseExpression("Goo(() => () => { var x = null; return x; })"), // cast removed
                SpeculativeBindingOption.BindAsExpression);
        }

        [WorkItem(545343, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545343")]
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

        [WorkItem(642222, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/642222")]
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
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
            Assert.NotNull(((ITypeSymbol)typeInfo.Type).GetMember("String"));
        }

        [WorkItem(722288, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/722288")]
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
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
            Assert.NotNull(((ITypeSymbol)typeInfo.Type).GetMember("SomeProperty"));
        }

        [WorkItem(871896, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/871896")]
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);

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

        [Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")]
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
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single().Body;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")]
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
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single().Initializer.Value;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Null(symbolInfo.Symbol);
            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact, WorkItem(960755, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/960755")]
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
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);

            var expr = (ExpressionSyntax)tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single().Body;

            var symbolInfo = model.GetSymbolInfo(expr);

            Assert.Equal("void System.Collections.Generic.ICollection<C>.Add(C item)", symbolInfo.Symbol.ToTestDisplayString());
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
        }

        [Fact]
        public void RefLambdaInferenceMethodArgument()
        {
            var text = @"
delegate ref int D();

class C
{
    static void MD(D d) { }

    static int i = 0;
    static void M()
    {
        MD(() => ref i);
        MD(() => { return ref i; });
        MD(delegate { return ref i; });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics();
        }


        [Fact]
        public void RefLambdaInferenceDelegateCreation()
        {
            var text = @"
delegate ref int D();

class C
{
    static int i = 0;
    static void M()
    {
        var d = new D(() => ref i);
        d = new D(() => { return ref i; });
        d = new D(delegate { return ref i; });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefLambdaInferenceOverloadedDelegateType()
        {
            var text = @"
delegate ref int D();
delegate int E();

class C
{
    static void M(D d) { }
    static void M(E e) { }

    static int i = 0;
    static void M()
    {
        M(() => ref i);
        M(() => { return ref i; });
        M(delegate { return ref i; });
        M(() => i);
        M(() => { return i; });
        M(delegate { return i; });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics();
        }

        [Fact]
        public void RefLambdaInferenceArgumentBadRefReturn()
        {
            var text = @"
delegate int E();

class C
{
    static void ME(E e) { }

    static int i = 0;
    static void M()
    {
        ME(() => ref i);
        ME(() => { return ref i; });
        ME(delegate { return ref i; });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (11,22): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         ME(() => ref i);
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "i").WithLocation(11, 22),
                // (12,20): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         ME(() => { return ref i; });
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(12, 20),
                // (13,23): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         ME(delegate { return ref i; });
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(13, 23));
        }

        [Fact]
        public void RefLambdaInferenceDelegateCreationBadRefReturn()
        {
            var text = @"
delegate int E();

class C
{
    static int i = 0;
    static void M()
    {
        var e = new E(() => ref i);
        e = new E(() => { return ref i; });
        e = new E(delegate { return ref i; });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (9,33): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         var e = new E(() => ref i);
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "i").WithLocation(9, 33),
                // (10,27): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         e = new E(() => { return ref i; });
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(10, 27),
                // (11,30): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //         e = new E(delegate { return ref i; });
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(11, 30));
        }

        [Fact]
        public void RefLambdaInferenceMixedByValueAndByRefReturns()
        {
            var text = @"
delegate ref int D();
delegate int E();

class C
{
    static void MD(D e) { }
    static void ME(E e) { }

    static int i = 0;
    static void M()
    {
        MD(() => {
            if (i == 0)
            {
                return ref i;
            }
            return i;
        });
        ME(() => {
            if (i == 0)
            {
                return ref i;
            }
            return i;
        });
    }
}
";

            CreateCompilationWithMscorlib45(text).VerifyDiagnostics(
                // (18,13): error CS8150: By-value returns may only be used in by-value returning methods.
                //             return i;
                Diagnostic(ErrorCode.ERR_MustHaveRefReturn, "return").WithLocation(18, 13),
                // (23,17): error CS8149: By-reference returns may only be used in by-reference returning methods.
                //                 return ref i;
                Diagnostic(ErrorCode.ERR_MustNotHaveRefReturn, "return").WithLocation(23, 17));
        }

        [WorkItem(1112875, "DevDiv")]
        [WorkItem(1112875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112875")]
        [Fact]
        public void Bug1112875_1()
        {
            var comp = CreateCompilation(@"
using System;
 
class Program
{
    static void Main()
    {
        ICloneable c = """";
        Goo(() => (c.Clone()), null);
    }
 
    static void Goo(Action x, string y) { }
    static void Goo(Func<object> x, object y) { Console.WriteLine(42); }
}", options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics();

            CompileAndVerify(comp, expectedOutput: "42");
        }

        [WorkItem(1112875, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1112875")]
        [Fact]
        public void Bug1112875_2()
        {
            var comp = CreateCompilation(@"
class Program
{
    void M()
    {
        var d = new System.Action(() => (new object()));
    }
}
");
            comp.VerifyDiagnostics(
                // (6,41): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         var d = new System.Action(() => (new object()));
                Diagnostic(ErrorCode.ERR_IllegalStatement, "(new object())").WithLocation(6, 41));
        }

        [WorkItem(1830, "https://github.com/dotnet/roslyn/issues/1830")]
        [Fact]
        public void FuncOfVoid()
        {
            var comp = CreateCompilation(@"
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

        [Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")]
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
            var compilation = CreateCompilation(src);
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

        [Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")]
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
            var compilation = CreateCompilation(src);
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

        [Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")]
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
            var compilation = CreateCompilation(src);
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

        [Fact, WorkItem(1179899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1179899")]
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
            var compilation = CreateCompilation(src);
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
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (9,45): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //         Expression<Func<int, int>> x = y => y = y;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "y = y").WithLocation(9, 45),
                // (9,45): error CS0832: An expression tree may not contain an assignment operator
                //         Expression<Func<int, int>> x = y => y = y;
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsAssignment, "y = y").WithLocation(9, 45));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public class Class1
{
    public void Method1()
    {
        Method((Class1 c) => c.Method2(default(Struct1)));
    }

    public void Method2(Struct1 s1) { }

    public static void Method<T>(Expression<Action<T>> expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,40): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Struct1'.
                //         Method((Class1 c) => c.Method2(default(Struct1)));
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default(Struct1)").WithArguments("Struct1").WithLocation(8, 40));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructDefaultExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public class Class1
{
    public void Method1()
    {
        Method((Class1 c) => c.Method2(default));
    }

    public void Method2(Struct1 s1) { }

    public static void Method<T>(Expression<Action<T>> expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,40): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Struct1'.
                //         Method((Class1 c) => c.Method2(default));
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default").WithArguments("Struct1").WithLocation(8, 40));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructDefaultCastExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public class Class1
{
    public void Method1()
    {
        Method((Class1 c) => c.Method2((Struct1) default));
    }

    public void Method2(Struct1 s1) { }

    public static void Method<T>(Expression<Action<T>> expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,50): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Struct1'.
                //         Method((Class1 c) => c.Method2((Struct1) default));
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default").WithArguments("Struct1").WithLocation(8, 50));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructNewExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public class Class1
{
    public void Method1()
    {
        Method((Class1 c) => c.Method2(new Struct1()));
    }

    public void Method2(Struct1 s1) { }

    public static void Method<T>(Expression<Action<T>> expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,40): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Struct1'.
                //         Method((Class1 c) => c.Method2(new Struct1()));
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "new Struct1()").WithArguments("Struct1").WithLocation(8, 40));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructParamExpressionTree()
        {
            var text = @"
using System.Linq.Expressions;

public delegate void Delegate1(Struct1 s);
public class Class1
{
    public void Method1()
    {
        Method((Struct1 s) => Method2());
    }

    public void Method2() { }

    public static void Method(Expression<Delegate1> expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,25): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'Struct1'.
                //         Method((Struct1 s) => Method2());
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "s").WithArguments("Struct1").WithLocation(9, 25));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void RefStructParamLambda()
        {
            var text = @"
public delegate void Delegate1(Struct1 s);
public class Class1
{
    public void Method1()
    {
        Method((Struct1 s) => Method2());
    }

    public void Method2() { }

    public static void Method(Delegate1 expression) { }
}

public ref struct Struct1 { }
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics();
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void TypedReferenceExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public class Class1
{
    public void Method1()
    {
        Method(() => Method2(default));
    }

    public void Method2(TypedReference tr) { }

    public static void Method(Expression<Action> expression) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (8,30): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'TypedReference'.
                //         Method(() => Method2(default));
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "default").WithArguments("TypedReference").WithLocation(8, 30));
        }

        [Fact, WorkItem(30776, "https://github.com/dotnet/roslyn/issues/30776")]
        public void TypedReferenceParamExpressionTree()
        {
            var text = @"
using System;
using System.Linq.Expressions;
public delegate void Delegate1(TypedReference tr);
public class Class1
{
    public void Method1()
    {
        Method((TypedReference tr) => Method2());
    }

    public void Method2() { }

    public static void Method(Expression<Delegate1> expression) { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(text).VerifyDiagnostics(
                // (9,32): error CS8640: Expression tree cannot contain value of ref struct or restricted type 'TypedReference'.
                //         Method((TypedReference tr) => Method2());
                Diagnostic(ErrorCode.ERR_ExpressionTreeCantContainRefStruct, "tr").WithArguments("TypedReference").WithLocation(9, 32));
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
            var compilation = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.ReleaseExe);
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
            var compilation = CreateCompilation(source, new[] { CSharpRef }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: "Select<T, S>");
        }

        [Fact, WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void SyntaxAndSemanticErrorInLambda()
        {
            var source =
@"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action a = () => { new X().ToString() };
        a();
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,47): error CS1002: ; expected
                //         Action a = () => { new X().ToString() };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(7, 47),
                // (7,32): error CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         Action a = () => { new X().ToString() };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(7, 32)
                );
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

            var lambdaParameters = ((IMethodSymbol)(model.GetSymbolInfo(node1)).Symbol).Parameters;

            Assert.Equal("System.Object <p0>", lambdaParameters[0].ToTestDisplayString());
            Assert.Equal("System.EventArgs <p1>", lambdaParameters[1].ToTestDisplayString());

            CompileAndVerify(compilation);
        }

        [Fact]
        [WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void TestLambdaWithError01()
        {
            var source =
@"using System.Linq;
class C { C() { string.Empty.Select(() => { new Unbound1 }); } }";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
    // (2,58): error CS1526: A new expression requires (), [], or {} after type
    // class C { C() { string.Empty.Select(() => { new Unbound1 }); } }
    Diagnostic(ErrorCode.ERR_BadNewExpr, "}").WithLocation(2, 58),
    // (2,58): error CS1002: ; expected
    // class C { C() { string.Empty.Select(() => { new Unbound1 }); } }
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 58),
    // (2,49): error CS0246: The type or namespace name 'Unbound1' could not be found (are you missing a using directive or an assembly reference?)
    // class C { C() { string.Empty.Select(() => { new Unbound1 }); } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unbound1").WithArguments("Unbound1").WithLocation(2, 49)
                );
        }

        [Fact]
        [WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void TestLambdaWithError02()
        {
            var source =
@"using System.Linq;
class C { C() { string.Empty.Select(() => { new Unbound1 ( ) }); } }";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
    // (2,62): error CS1002: ; expected
    // class C { C() { string.Empty.Select(() => { new Unbound1 ( ) }); } }
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(2, 62),
    // (2,49): error CS0246: The type or namespace name 'Unbound1' could not be found (are you missing a using directive or an assembly reference?)
    // class C { C() { string.Empty.Select(() => { new Unbound1 ( ) }); } }
    Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Unbound1").WithArguments("Unbound1").WithLocation(2, 49)
                );
        }

        [Fact]
        [WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void TestLambdaWithError03()
        {
            var source =
@"using System.Linq;
class C { C() { string.Empty.Select(x => Unbound1, Unbound2 Unbound2); } }";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
    // (2,61): error CS1003: Syntax error, ',' expected
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2 Unbound2); } }
    Diagnostic(ErrorCode.ERR_SyntaxError, "Unbound2").WithArguments(",", "").WithLocation(2, 61),
    // (2,52): error CS0103: The name 'Unbound2' does not exist in the current context
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2 Unbound2); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound2").WithArguments("Unbound2").WithLocation(2, 52),
    // (2,61): error CS0103: The name 'Unbound2' does not exist in the current context
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2 Unbound2); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound2").WithArguments("Unbound2").WithLocation(2, 61),
    // (2,42): error CS0103: The name 'Unbound1' does not exist in the current context
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2 Unbound2); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound1").WithArguments("Unbound1").WithLocation(2, 42)
                );
        }

        [Fact]
        [WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void TestLambdaWithError04()
        {
            var source =
@"using System.Linq;
class C { C() { string.Empty.Select(x => Unbound1, Unbound2); } }";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
    // (2,52): error CS0103: The name 'Unbound2' does not exist in the current context
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound2").WithArguments("Unbound2").WithLocation(2, 52),
    // (2,42): error CS0103: The name 'Unbound1' does not exist in the current context
    // class C { C() { string.Empty.Select(x => Unbound1, Unbound2); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound1").WithArguments("Unbound1").WithLocation(2, 42)
                );
        }

        [Fact]
        [WorkItem(1867, "https://github.com/dotnet/roslyn/issues/1867")]
        public void TestLambdaWithError05()
        {
            var source =
@"using System.Linq;
class C { C() { Unbound2.Select(x => Unbound1); } }";
            CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
    // (2,17): error CS0103: The name 'Unbound2' does not exist in the current context
    // class C { C() { Unbound2.Select(x => Unbound1); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound2").WithArguments("Unbound2").WithLocation(2, 17),
    // (2,38): error CS0103: The name 'Unbound1' does not exist in the current context
    // class C { C() { Unbound2.Select(x => Unbound1); } }
    Diagnostic(ErrorCode.ERR_NameNotInContext, "Unbound1").WithArguments("Unbound1").WithLocation(2, 38)
                );
        }

        [Fact]
        [WorkItem(4480, "https://github.com/dotnet/roslyn/issues/4480")]
        public void TestLambdaWithError06()
        {
            var source =
@"class Program
{
    static void Main(string[] args)
    {
        // completion should work even in a syntactically invalid lambda
        var handler = new MyDelegateType((s, e) => { e. });
    }
}

public delegate void MyDelegateType(
    object sender,
    MyArgumentType e
);

public class MyArgumentType
{
    public int SomePublicMember;
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source)
                .VerifyDiagnostics(
                //         var handler = new MyDelegateType((s, e) => { e. });
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "}").WithLocation(6, 57),
                // (6,57): error CS1002: ; expected
                //         var handler = new MyDelegateType((s, e) => { e. });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(6, 57)
                );
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("e", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal("MyArgumentType", typeInfo.Type.Name);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.NotEmpty(typeInfo.Type.GetMembers("SomePublicMember"));
        }

        [Fact]
        [WorkItem(11053, "https://github.com/dotnet/roslyn/issues/11053")]
        [WorkItem(11358, "https://github.com/dotnet/roslyn/issues/11358")]
        public void TestLambdaWithError07()
        {
            var source =
@"using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main()
    {
        var parameter = new List<string>();
        var result = parameter.FirstOrDefault(x => x. );
    }
}

public static class Enumerable
{
    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, TSource defaultValue)
    {
        return default(TSource);
    }

    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
    {
        return default(TSource);
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (9,55): error CS1001: Identifier expected
                //         var result = parameter.FirstOrDefault(x => x. );
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(9, 55)
                );
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("x", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
        }

        [Fact]
        [WorkItem(11053, "https://github.com/dotnet/roslyn/issues/11053")]
        [WorkItem(11358, "https://github.com/dotnet/roslyn/issues/11358")]
        public void TestLambdaWithError08()
        {
            var source =
@"using System;
using System.Collections.Generic;

public static class Program
{
    public static void Main()
    {
        var parameter = new List<string>();
        var result = parameter.FirstOrDefault(x => x. );
    }
}

public static class Enumerable
{
    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, params TSource[] defaultValue)
    {
        return default(TSource);
    }

    public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, params TSource[] defaultValue)
    {
        return default(TSource);
}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (9,55): error CS1001: Identifier expected
                //         var result = parameter.FirstOrDefault(x => x. );
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(9, 55)
                );
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("x", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
        }

        [Fact]
        [WorkItem(11053, "https://github.com/dotnet/roslyn/issues/11053")]
        [WorkItem(11358, "https://github.com/dotnet/roslyn/issues/11358")]
        public void TestLambdaWithError09()
        {
            var source =
@"using System;

public static class Program
{
    public static void Main()
    {
        var parameter = new MyList<string>();
        var result = parameter.FirstOrDefault(x => x. );
    }
}

public class MyList<TSource>
{
    public TSource FirstOrDefault(TSource defaultValue)
    {
        return default(TSource);
    }

    public TSource FirstOrDefault(Func<TSource, bool> predicate, TSource defaultValue)
    {
        return default(TSource);
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,55): error CS1001: Identifier expected
                //         var result = parameter.FirstOrDefault(x => x. );
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(8, 55)
                );
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("x", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
        }

        [Fact]
        [WorkItem(11053, "https://github.com/dotnet/roslyn/issues/11053")]
        [WorkItem(11358, "https://github.com/dotnet/roslyn/issues/11358")]
        public void TestLambdaWithError10()
        {
            var source =
@"using System;

public static class Program
{
    public static void Main()
    {
        var parameter = new MyList<string>();
        var result = parameter.FirstOrDefault(x => x. );
    }
}

public class MyList<TSource>
{
    public TSource FirstOrDefault(params TSource[] defaultValue)
    {
        return default(TSource);
    }

    public TSource FirstOrDefault(Func<TSource, bool> predicate, params TSource[] defaultValue)
    {
        return default(TSource);
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source).VerifyDiagnostics(
                // (8,55): error CS1001: Identifier expected
                //         var result = parameter.FirstOrDefault(x => x. );
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(8, 55)
                );
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("x", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
        }

        [Fact]
        [WorkItem(557, "https://github.com/dotnet/roslyn/issues/557")]
        public void TestLambdaWithError11()
        {
            var source =
@"using System.Linq;

public static class Program
{
    public static void Main()
    {
        var x = new {
            X = """".Select(c => c.
            Y = 0,
        };
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("c", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Struct, typeInfo.Type.TypeKind);
            Assert.Equal("Char", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("IsHighSurrogate")); // check it is the char we know and love
        }

        [Fact]
        [WorkItem(5498, "https://github.com/dotnet/roslyn/issues/5498")]
        public void TestLambdaWithError12()
        {
            var source =
@"using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var z = args.Select(a => a.
        var goo = 
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambda = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var eReference = lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>().First();
            Assert.Equal("a", eReference.ToString());
            var typeInfo = sm.GetTypeInfo(eReference);
            Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
            Assert.Equal("String", typeInfo.Type.Name);
            Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
        }

        [WorkItem(5498, "https://github.com/dotnet/roslyn/issues/5498")]
        [WorkItem(11358, "https://github.com/dotnet/roslyn/issues/11358")]
        [Fact]
        public void TestLambdaWithError13()
        {
            // These tests ensure we attempt to perform type inference and bind a lambda expression
            // argument even when there are too many or too few arguments to an invocation, in the
            // case when there is more than one method in the method group.
            // See https://github.com/dotnet/roslyn/issues/11901 for the case of one method in the group
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Thing<string> t = null;
        t.X1(x => x, 1); // too many args
        t.X2(x => x);    // too few args
        t.M2(string.Empty, x => x, 1); // too many args
        t.M3(string.Empty, x => x); // too few args
    }
}
public class Thing<T>
{
    public void M2<T>(T x, Func<T, T> func) {}
    public void M3<T>(T x, Func<T, T> func, T y) {}

    // Ensure we have more than one method in the method group
    public void M2() {}
    public void M3() {}
}
public static class XThing
{
    public static Thing<T> X1<T>(this Thing<T> self, Func<T, T> func) => null;
    public static Thing<T> X2<T>(this Thing<T> self, Func<T, T> func, int i) => null;

    // Ensure we have more than one method in the method group
    public static void X1(this object self) {}
    public static void X2(this object self) {}
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        [Fact]
        [WorkItem(11901, "https://github.com/dotnet/roslyn/issues/11901")]
        public void TestLambdaWithError15()
        {
            // These tests ensure we attempt to perform type inference and bind a lambda expression
            // argument even when there are too many or too few arguments to an invocation, in the
            // case when there is exactly one method in the method group.
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Thing<string> t = null;
        t.X1(x => x, 1); // too many args
        t.X2(x => x);    // too few args
        t.M2(string.Empty, x => x, 1); // too many args
        t.M3(string.Empty, x => x); // too few args
    }
}
public class Thing<T>
{
    public void M2<T>(T x, Func<T, T> func) {}
    public void M3<T>(T x, Func<T, T> func, T y) {}
}
public static class XThing
{
    public static Thing<T> X1<T>(this Thing<T> self, Func<T, T> func) => null;
    public static Thing<T> X2<T>(this Thing<T> self, Func<T, T> func, int i) => null;
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        [Fact]
        [WorkItem(11901, "https://github.com/dotnet/roslyn/issues/11901")]
        public void TestLambdaWithError16()
        {
            // These tests ensure we use the substituted method to bind a lambda expression
            // argument even when there are too many or too few arguments to an invocation, in the
            // case when there is exactly one method in the method group.
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Thing<string> t = null;
        t.X1<string>(x => x, 1); // too many args
        t.X2<string>(x => x);    // too few args
        t.M2<string>(string.Empty, x => x, 1); // too many args
        t.M3<string>(string.Empty, x => x); // too few args
    }
}
public class Thing<T>
{
    public void M2<T>(T x, Func<T, T> func) {}
    public void M3<T>(T x, Func<T, T> func, T y) {}
}
public static class XThing
{
    public static Thing<T> X1<T>(this Thing<T> self, Func<T, T> func) => null;
    public static Thing<T> X2<T>(this Thing<T> self, Func<T, T> func, int i) => null;
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        [Fact]
        [WorkItem(12063, "https://github.com/dotnet/roslyn/issues/12063")]
        public void TestLambdaWithError17()
        {
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Ma(action: (x, y) => x.ToString(), t: string.Empty);
        Mb(action: (x, y) => x.ToString(), t: string.Empty);
    }
    static void Ma<T>(T t, Action<T, T, int> action) { }
    static void Mb<T>(T t, Action<T, T, int> action) { }
    static void Mb() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        [Fact]
        [WorkItem(12063, "https://github.com/dotnet/roslyn/issues/12063")]
        public void TestLambdaWithError18()
        {
            var source =
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Ma(string.Empty, (x, y) => x.ToString());
        Mb(string.Empty, (x, y) => x.ToString());
    }
    static void Ma<T>(T t, Action<T, T, int> action) { }
    static void Mb<T>(T t, Action<T, T, int> action) { }
    static void Mb() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        [Fact]
        [WorkItem(12063, "https://github.com/dotnet/roslyn/issues/12063")]
        public void TestLambdaWithError19()
        {
            var source =
@"using System;
using System.Linq.Expressions;

class Program
{
    static void Main(string[] args)
    {
        Ma(string.Empty, (x, y) => x.ToString());
        Mb(string.Empty, (x, y) => x.ToString());
        Mc(string.Empty, (x, y) => x.ToString());
    }
    static void Ma<T>(T t, Expression<Action<T, T, int>> action) { }
    static void Mb<T>(T t, Expression<Action<T, T, int>> action) { }
    static void Mb<T>(T t, Action<T, T, int> action) { }
    static void Mc<T>(T t, Expression<Action<T, T, int>> action) { }
    static void Mc() { }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            foreach (var lambda in tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>())
            {
                var reference = lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().First();
                Assert.Equal("x", reference.ToString());
                var typeInfo = sm.GetTypeInfo(reference);
                Assert.Equal(TypeKind.Class, typeInfo.Type.TypeKind);
                Assert.Equal("String", typeInfo.Type.Name);
                Assert.NotEmpty(typeInfo.Type.GetMembers("Replace"));
            }
        }

        // See MaxParameterListsForErrorRecovery.
        [Fact]
        public void BuildArgumentsForErrorRecovery_ManyOverloads()
        {
            BuildArgumentsForErrorRecovery_ManyOverloads_Internal(Binder.MaxParameterListsForErrorRecovery - 1, tooMany: false);
            BuildArgumentsForErrorRecovery_ManyOverloads_Internal(Binder.MaxParameterListsForErrorRecovery, tooMany: true);
        }

        private void BuildArgumentsForErrorRecovery_ManyOverloads_Internal(int n, bool tooMany)
        {
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.Append(
@"class A { }
class B { }
class C
{
    void M()
    {
        F(1, (t, a, b, c) => { });
        var o = this[(a, b, c) => { }];
    }
");
            // Too few parameters.
            AppendLines(builder, n, i => $"    void F<T>(T t, Action<T, A, C{i}> a) {{ }}");
            AppendLines(builder, n, i => $"    object this[Action<A, C{i}> a] => {i}");
            // Type inference failure.
            AppendLines(builder, n, i => $"    void F<T, U>(T t, Action<T, U, C{i}> a) where U : T {{ }}");
            // Too many parameters.
            AppendLines(builder, n, i => $"    void F<T>(T t, Action<T, A, B, C, C{i}> a) {{ }}");
            AppendLines(builder, n, i => $"    object this[Action<A, B, C, C{i}> a] => {i}");
            builder.AppendLine("}");

            var source = builder.ToString();
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var sm = compilation.GetSemanticModel(tree);
            var lambdas = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().ToArray();

            // F(1, (t, a, b, c) => { });
            var lambda = lambdas[0];
            var parameters = lambda.ParameterList.Parameters;
            var parameter = (IParameterSymbol)sm.GetDeclaredSymbol(parameters[0]);
            Assert.False(parameter.Type.IsErrorType());
            Assert.Equal("System.Int32 t", parameter.ToTestDisplayString());
            parameter = (IParameterSymbol)sm.GetDeclaredSymbol(parameters[1]);
            Assert.False(parameter.Type.IsErrorType());
            Assert.Equal("A a", parameter.ToTestDisplayString());
            parameter = (IParameterSymbol)sm.GetDeclaredSymbol(parameters[3]);
            Assert.Equal(tooMany, parameter.Type.IsErrorType());
            Assert.Equal(tooMany ? "? c" : "C c", parameter.ToTestDisplayString());

            // var o = this[(a, b, c) => { }];
            lambda = lambdas[1];
            parameters = lambda.ParameterList.Parameters;
            parameter = (IParameterSymbol)sm.GetDeclaredSymbol(parameters[0]);
            Assert.False(parameter.Type.IsErrorType());
            Assert.Equal("A a", parameter.ToTestDisplayString());
            parameter = (IParameterSymbol)sm.GetDeclaredSymbol(parameters[2]);
            Assert.Equal(tooMany, parameter.Type.IsErrorType());
            Assert.Equal(tooMany ? "? c" : "C c", parameter.ToTestDisplayString());
        }

        private static void AppendLines(StringBuilder builder, int n, Func<int, string> getLine)
        {
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine(getLine(i));
            }
        }

        [Fact]
        [WorkItem(13797, "https://github.com/dotnet/roslyn/issues/13797")]
        public void DelegateAsAction()
        {
            var source = @"
using System;

public static class C
{
    public static void M() => Dispatch(delegate { });

    public static T Dispatch<T>(Func<T> func) => default(T);

    public static void Dispatch(Action func) { }
}";
            var comp = CreateCompilation(source);
            CompileAndVerify(comp);
        }

        [Fact, WorkItem(278481, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=278481")]
        public void LambdaReturningNull_1()
        {
            var src = @"
public static class ExtensionMethods
{
    public static System.Linq.IQueryable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(
        this System.Linq.IQueryable<TOuter> outerValues,
        System.Linq.IQueryable<TInner> innerValues,
        System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector,
        System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector,
        System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> fullResultSelector,
        System.Linq.Expressions.Expression<System.Func<TOuter, TResult>> partialResultSelector,
        System.Collections.Generic.IEqualityComparer<TKey> comparer)
    { return null; }

    public static System.Linq.IQueryable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(
        this System.Linq.IQueryable<TOuter> outerValues, 
        System.Linq.IQueryable<TInner> innerValues, 
        System.Linq.Expressions.Expression<System.Func<TOuter, TKey>> outerKeySelector, 
        System.Linq.Expressions.Expression<System.Func<TInner, TKey>> innerKeySelector, 
        System.Linq.Expressions.Expression<System.Func<TOuter, TInner, TResult>> fullResultSelector, 
        System.Linq.Expressions.Expression<System.Func<TOuter, TResult>> partialResultSelector)
    {
        System.Console.WriteLine(""1""); 
        return null; 
    }

    public static System.Collections.Generic.IEnumerable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(
        this System.Collections.Generic.IEnumerable<TOuter> outerValues, 
        System.Linq.IQueryable<TInner> innerValues, 
        System.Func<TOuter, TKey> outerKeySelector, 
        System.Func<TInner, TKey> innerKeySelector, 
        System.Func<TOuter, TInner, TResult> fullResultSelector, 
        System.Func<TOuter, TResult> partialResultSelector)
    {
        System.Console.WriteLine(""2""); 
        return null; 
    }

    public static System.Collections.Generic.IEnumerable<TResult> LeftOuterJoin<TOuter, TInner, TKey, TResult>(
        this System.Collections.Generic.IEnumerable<TOuter> outerQueryable,
        System.Collections.Generic.IEnumerable<TInner> innerQueryable,
        System.Func<TOuter, TKey> outerKeySelector,
        System.Func<TInner, TKey> innerKeySelector,
        System.Func<TOuter, TInner, TResult> resultSelector)
    { return null; }
}

partial class C
{
    public static void Main()
    {
        System.Linq.IQueryable<A> outerValue = null;
        System.Linq.IQueryable<B> innerValues = null;

        outerValue.LeftOuterJoin(innerValues,
                    co => co.id,
                    coa => coa.id,
                    (co, coa) => null,
                    co => co);
    }
}

class A
{
    public int id=2;
}

class B
{
    public int id = 2;
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "1");
        }

        [Fact, WorkItem(296550, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=296550")]
        public void LambdaReturningNull_2()
        {
            var src = @"
class Test1<T>
    {
        public void M1(System.Func<T> x) {}
        public void M1<S>(System.Func<S> x) {}
        public void M2<S>(System.Func<S> x) {}
        public void M2(System.Func<T> x) {}
    }

    class Test2 : Test1<System.>
    {
        void Main()
        {
            M1(()=> null);
            M2(()=> null);
        }
    }
";
            var comp = CreateCompilation(src, options: TestOptions.DebugDll);

            comp.VerifyDiagnostics(
                // (10,32): error CS1001: Identifier expected
                //     class Test2 : Test1<System.>
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">").WithLocation(10, 32)
                );
        }

        [Fact, WorkItem(22662, "https://github.com/dotnet/roslyn/issues/22662")]
        public void LambdaSquigglesArea()
        {
            var src = @"
class C
{
    void M()
    {
        System.Func<bool, System.Action<bool>> x = x1 => x2 =>
        {
            error();
        };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS0103: The name 'error' does not exist in the current context
                //             error();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error").WithArguments("error").WithLocation(8, 13),
                // (6,58): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         System.Func<bool, System.Action<bool>> x = x1 => x2 =>
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "x2 =>").WithArguments("lambda expression").WithLocation(6, 58)
                );
        }

        [Fact, WorkItem(22662, "https://github.com/dotnet/roslyn/issues/22662")]
        public void LambdaSquigglesAreaInAsync()
        {
            var src = @"
class C
{
    void M()
    {
        System.Func<bool, System.Threading.Tasks.Task<System.Action<bool>>> x = async x1 => x2 =>
        {
            error();
        };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS0103: The name 'error' does not exist in the current context
                //             error();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error").WithArguments("error").WithLocation(8, 13),
                // (6,93): error CS4010: Cannot convert async lambda expression to delegate type 'Task<Action<bool>>'. An async lambda expression may return void, Task or Task<T>, none of which are convertible to 'Task<Action<bool>>'.
                //         System.Func<bool, System.Threading.Tasks.Task<System.Action<bool>>> x = async x1 => x2 =>
                Diagnostic(ErrorCode.ERR_CantConvAsyncAnonFuncReturns, "x2 =>").WithArguments("lambda expression", "System.Threading.Tasks.Task<System.Action<bool>>").WithLocation(6, 93),
                // (6,90): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         System.Func<bool, System.Threading.Tasks.Task<System.Action<bool>>> x = async x1 => x2 =>
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 90)
                );
        }

        [Fact, WorkItem(22662, "https://github.com/dotnet/roslyn/issues/22662")]
        public void DelegateSquigglesArea()
        {
            var src = @"
class C
{
    void M()
    {
        System.Func<bool, System.Action<bool>> x = x1 => delegate(bool x2)
        {
            error();
        };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS0103: The name 'error' does not exist in the current context
                //             error();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error").WithArguments("error").WithLocation(8, 13),
                // (6,58): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         System.Func<bool, System.Action<bool>> x = x1 => delegate(bool x2)
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "delegate(bool x2)").WithArguments("lambda expression").WithLocation(6, 58)
                );
        }

        [Fact, WorkItem(22662, "https://github.com/dotnet/roslyn/issues/22662")]
        public void DelegateWithoutArgumentsSquigglesArea()
        {
            var src = @"
class C
{
    void M()
    {
        System.Func<bool, System.Action> x = x1 => delegate
        {
            error();
        };
    }
}
";
            var comp = CreateCompilation(src);
            comp.VerifyDiagnostics(
                // (8,13): error CS0103: The name 'error' does not exist in the current context
                //             error();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "error").WithArguments("error").WithLocation(8, 13),
                // (6,52): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         System.Func<bool, System.Action> x = x1 => delegate
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "delegate").WithArguments("lambda expression").WithLocation(6, 52)
                );
        }

        [Fact]
        public void ThrowExpression_Lambda()
        {
            var src = @"using System;
class C
{
    public static void Main()
    {
        Action a = () => throw new Exception(""1"");
        try
        {
            a();
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
        Func<int, int> b = x => throw new Exception(""2"");
        try
        {
            b(0);
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
        b = (int x) => throw new Exception(""3"");
        try
        {
            b(0);
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
        b = (x) => throw new Exception(""4"");
        try
        {
            b(0);
        }
        catch (Exception ex)
        {
            Console.Write(ex.Message);
        }
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(src, options: TestOptions.DebugExe);
            CompileAndVerify(comp, expectedOutput: "1234");
        }

        [Fact, WorkItem(23883, "https://github.com/dotnet/roslyn/issues/23883")]
        public void InMalformedEmbeddedStatement_01()
        {
            var source = @"
class Program
{
    void method1()
    {
        if (method2())
            .Any(b => b.ContentType, out var chars)
        {
        }
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilation(tree);

            ExpressionSyntax contentType = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "ContentType").Single();

            var model = comp.GetSemanticModel(tree);
            Assert.Equal("ContentType", contentType.ToString());
            Assert.Null(model.GetSymbolInfo(contentType).Symbol);
            Assert.Equal(TypeKind.Error, model.GetTypeInfo(contentType).Type.TypeKind);

            ExpressionSyntax b = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "b").Single();

            model = comp.GetSemanticModel(tree);
            Assert.Equal("b", b.ToString());
            ISymbol symbol = model.GetSymbolInfo(b).Symbol;
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("? b", symbol.ToTestDisplayString());
            Assert.Equal(TypeKind.Error, model.GetTypeInfo(b).Type.TypeKind);

            ParameterSyntax parameterSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Single();

            model = comp.GetSemanticModel(tree);
            symbol = model.GetDeclaredSymbol(parameterSyntax);
            Assert.Equal(SymbolKind.Parameter, symbol.Kind);
            Assert.Equal("? b", symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(23883, "https://github.com/dotnet/roslyn/issues/23883")]
        public void InMalformedEmbeddedStatement_02()
        {
            var source = @"
class Program
{
    void method1()
    {
        if (method2())
            .Any(b => b.ContentType, out var chars)
        {
        }
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var comp = CreateCompilation(tree);

            ExpressionSyntax contentType = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "ContentType").Single();

            var model = comp.GetSemanticModel(tree);
            Assert.Equal("ContentType", contentType.ToString());
            var lambda = (IMethodSymbol)model.GetEnclosingSymbol(contentType.SpanStart);
            Assert.Equal(MethodKind.AnonymousFunction, lambda.MethodKind);

            ExpressionSyntax b = tree.GetCompilationUnitRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "b").Single();

            model = comp.GetSemanticModel(tree);
            Assert.Equal("b", b.ToString());
            lambda = (IMethodSymbol)model.GetEnclosingSymbol(b.SpanStart);
            Assert.Equal(MethodKind.AnonymousFunction, lambda.MethodKind);

            model = comp.GetSemanticModel(tree);
            ParameterSyntax parameterSyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<ParameterSyntax>().Single();
            Assert.Equal("void Program.method1()", model.GetEnclosingSymbol(parameterSyntax.SpanStart).ToTestDisplayString());
        }

        [Fact]
        public void ShadowNames_Local()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static void M()
    {
        Action a1 = () => { object x = 0; }; // local
        Action<string> a2 = x => { }; // parameter
        Action<string> a3 = (string x) => { }; // parameter
        object x = null;
        Action a4 = () => { void x() { } }; // method
        Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,36): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a1 = () => { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 36),
                // (10,29): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a2 = x => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 29),
                // (11,37): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a3 = (string x) => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 37),
                // (13,34): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a4 = () => { void x() { } }; // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(13, 34),
                // (14,38): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(14, 38));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Parameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static Action<object> F = (object x) =>
    {
        Action a1 = () => { object x = 0; }; // local
        Action<string> a2 = x => { }; // parameter
        Action<string> a3 = (string x) => { }; // parameter
        Action a4 = () => { void x() { } }; // method
        Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    };
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,36): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a1 = () => { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 36),
                // (10,29): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a2 = x => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 29),
                // (11,37): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a3 = (string x) => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 37),
                // (13,38): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(13, 38));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_TypeParameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static void M<x>()
    {
        Action a1 = () => { object x = 0; }; // local
        Action<string> a2 = x => { }; // parameter
        Action<string> a3 = (string x) => { }; // parameter
        Action a4 = () => { void x() { } }; // method
        Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,36): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action a1 = () => { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(9, 36),
                // (10,29): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action<string> a2 = x => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(10, 29),
                // (11,37): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action<string> a3 = (string x) => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(11, 37),
                // (12,34): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action a4 = () => { void x() { } }; // method
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(12, 34),
                // (13,38): error CS1948: The range variable 'x' cannot have the same name as a method type parameter
                //         Action a5 = () => { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "x").WithArguments("x").WithLocation(13, 38));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_QueryParameter()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static void Main(string[] args)
    {
        _ = from x in args select (Action)(() => { object x = 0; }); // local
        _ = from x in args select (Action<string>)(x => { }); // parameter
        _ = from x in args select (Action<string>)((string x) => { }); // parameter
        _ = from x in args select (Action)(() => { void x() { } }); // method
        _ = from x in args select (Action)(() => { _ = from x in new[] { 1, 2, 3 } select x; }); // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,59): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         _ = from x in args select (Action)(() => { object x = 0; }); // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 59),
                // (10,52): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         _ = from x in args select (Action<string>)(x => { }); // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 52),
                // (11,60): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         _ = from x in args select (Action<string>)((string x) => { }); // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 60),
                // (13,61): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         _ = from x in args select (Action)(() => { _ = from x in new[] { 1, 2, 3 } select x; }); // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(13, 61));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Local_Delegate()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static void M()
    {
        object x = null;
        Action a1 = delegate() { object x = 0; }; // local
        Action<string> a2 = delegate(string x) { }; // parameter
        Action a3 = delegate() { void x() { } }; // method
        Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (10,41): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a1 = delegate() { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 41),
                // (11,45): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a2 = delegate(string x) { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 45),
                // (12,39): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a3 = delegate() { void x() { } }; // method
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(12, 39),
                // (13,43): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(13, 43));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);
            comp.VerifyDiagnostics();

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Parameter_Delegate()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static Action<object> F = (object x) =>
    {
        Action a1 = delegate() { object x = 0; }; // local
        Action<string> a2 = delegate(string x) { }; // parameter
        Action a3 = delegate() { void x() { } }; // method
        Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    };
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,41): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action a1 = delegate() { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(9, 41),
                // (10,45): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Action<string> a2 = delegate(string x) { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 45),
                // (12,43): error CS1931: The range variable 'x' conflicts with a previous declaration of 'x'
                //         Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "x").WithArguments("x").WithLocation(12, 43));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_TypeParameter_Delegate()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
using System.Linq;
class Program
{
    static void M<x>()
    {
        Action a1 = delegate() { object x = 0; }; // local
        Action<string> a2 = delegate(string x) { }; // parameter
        Action a3 = delegate() { void x() { } }; // method
        Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (9,41): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action a1 = delegate() { object x = 0; }; // local
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(9, 41),
                // (10,45): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action<string> a2 = delegate(string x) { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(10, 45),
                // (11,39): error CS0412: 'x': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //         Action a3 = delegate() { void x() { } }; // method
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "x").WithArguments("x").WithLocation(11, 39),
                // (12,43): error CS1948: The range variable 'x' cannot have the same name as a method type parameter
                //         Action a4 = delegate() { _ = from x in new[] { 1, 2, 3 } select x; }; // range variable
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "x").WithArguments("x").WithLocation(12, 43));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LambdaInsideLambda()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M<T>(object x)
    {
        Action a1 = () =>
        {
            Action b1 = () => { object x = 1; }; // local
            Action<string> b2 = (string x) => { }; // parameter
        };
        Action a2 = () =>
        {
            Action b3 = () => { object T = 3; }; // local
            Action<string> b4 = T => { }; // parameter
        };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (10,40): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Action b1 = () => { object x = 1; }; // local
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(10, 40),
                // (11,41): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Action<string> b2 = (string x) => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 41),
                // (15,40): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             Action b3 = () => { object T = 3; }; // local
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(15, 40),
                // (16,33): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             Action<string> b4 = T => { }; // parameter
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(16, 33));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Underscore_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Func<int, Func<int, int>> f = _ => _ => _;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,44): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, Func<int, int>> f = _ => _ => _;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(8, 44));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Underscore_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Func<int, int, int> f = (_, _) => 0;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            verifyDiagnostics();

            comp = CreateCompilation(source);
            verifyDiagnostics();

            void verifyDiagnostics()
            {
                comp.VerifyDiagnostics(
                    // (8,37): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                    //         Func<int, int, int> f = (_, _) => 0;
                    Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(8, 37));
            }
        }

        [Fact]
        public void ShadowNames_Nested_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Func<int, Func<int, Func<int, int>>> f = x => x => x => x;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,55): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, Func<int, Func<int, int>>> f = x => x => x => x;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(8, 55),
                // (8,60): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, Func<int, Func<int, int>>> f = x => x => x => x;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(8, 60));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_Nested_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        Func<int, int, int, Func<int, int, Func<int, int, int>>> f = (x, y, z) => (_, x) => (y, _) => x + y + z + _;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (8,87): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, int, int, Func<int, int, Func<int, int, int>>> f = (x, y, z) => (_, x) => (y, _) => x + y + z + _;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(8, 87),
                // (8,94): error CS0136: A local or parameter named 'y' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, int, int, Func<int, int, Func<int, int, int>>> f = (x, y, z) => (_, x) => (y, _) => x + y + z + _;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y").WithArguments("y").WithLocation(8, 94),
                // (8,97): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         Func<int, int, int, Func<int, int, Func<int, int, int>>> f = (x, y, z) => (_, x) => (y, _) => x + y + z + _;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(8, 97));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LambdaInsideLocalFunction_01()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M()
    {
        void F1()
        {
            object x = null;
            Action a1 = () => { int x = 0; };
        }
        void F2<T>()
        {
            Action a2 = () => { int T = 0; };
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (11,37): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Action a1 = () => { int x = 0; };
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 37),
                // (15,37): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //             Action a2 = () => { int T = 0; };
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(15, 37));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ShadowNames_LambdaInsideLocalFunction_02()
        {
            var source =
@"#pragma warning disable 0219
#pragma warning disable 8321
using System;
class Program
{
    static void M<T>()
    {
        object x = null;
        void F()
        {
            Action<int> a1 = (int x) =>
            {
                Action b1 = () => { int T = 0; };
            };
            Action a2 = () =>
            {
                int x = 0;
                Action<int> b2 = (int T) => { };
            };
        }
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (11,35): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             Action<int> a1 = (int x) =>
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(11, 35),
                // (13,41): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //                 Action b1 = () => { int T = 0; };
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(13, 41),
                // (17,21): error CS0136: A local or parameter named 'x' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 int x = 0;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x").WithArguments("x").WithLocation(17, 21),
                // (18,39): error CS0412: 'T': a parameter, local variable, or local function cannot have the same name as a method type parameter
                //                 Action<int> b2 = (int T) => { };
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "T").WithArguments("T").WithLocation(18, 39));

            comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }
    }
}
