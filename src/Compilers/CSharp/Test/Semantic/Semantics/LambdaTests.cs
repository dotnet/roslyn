// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaTests : SemanticModelTestBase
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
                // (16,20): error CS1660: Cannot convert lambda expression to type 'int' because it is not a delegate type
                //         int q1 = ()=>1;
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "int").WithLocation(16, 20),
                // (17,18): error CS1660: Cannot convert anonymous method to type 'int' because it is not a delegate type
                //         int q2 = delegate { return 1; };
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "delegate").WithArguments("anonymous method", "int").WithLocation(17, 18),
                // (18,26): error CS1593: Delegate 'Func<int>' does not take 1 arguments
                //         Func<int> q3 = x3=>1;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Func<int>", "1").WithLocation(18, 26),
                // (19,37): error CS0234: The type or namespace name 'Itn23' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //         Func<int, int> q4 = (System.Itn23 x4)=>1; // type mismatch error should be suppressed on error type
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Itn23").WithArguments("Itn23", "System").WithLocation(19, 37),
                // (20,35): error CS0234: The type or namespace name 'Duobel' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //         Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Duobel").WithArguments("Duobel", "System").WithLocation(20, 35),
                // (20,45): error CS1593: Delegate 'Func<double>' does not take 1 arguments
                //         Func<double> q5 = (System.Duobel x5)=>1;  // but arity error should not be suppressed on error type
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Func<double>", "1").WithLocation(20, 45),
                // (21,52): error CS1661: Cannot convert lambda expression to type 'C.D1' because the parameter types do not match the delegate parameter types
                //         D1 q6 = (double x6, ref int y6, ref int z6)=>1; 
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "C.D1").WithLocation(21, 52),
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
                Diagnostic(ErrorCode.ERR_CantConvAnonMethNoParams, "delegate").WithArguments("C.D1").WithLocation(32, 17),
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
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(66, 39),
                // (67,49): warning CS9100: Parameter 1 has params modifier in lambda but not in target delegate type.
                //         Action<string[]> q17 = (params string[] s)=>{};
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "s").WithArguments("1").WithLocation(67, 49),
                // (68,61): warning CS9100: Parameter 2 has params modifier in lambda but not in target delegate type.
                //         Action<int, double[]> q18 = (int x, params double[] s)=>{};
                Diagnostic(ErrorCode.WRN_ParamsArrayInLambdaOnly, "s").WithArguments("2").WithLocation(68, 61),
                // (70,41): error CS1593: Delegate 'Action' does not take 1 arguments
                //         object q19 = new Action( (int x)=>{} );
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Action", "1").WithLocation(70, 41),
                // (72,9): warning CS0436: The type 'Expression<T>' in '' conflicts with the imported type 'Expression<TDelegate>' in 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'. Using the type defined in ''.
                //         Expression<int> ex1 = ()=>1;
                Diagnostic(ErrorCode.WRN_SameFullNameThisAggAgg, "Expression<int>").WithArguments("", "System.Linq.Expressions.Expression<T>", "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089", "System.Linq.Expressions.Expression<TDelegate>").WithLocation(72, 9),
                // (72,33): error CS0835: Cannot convert lambda to an expression tree whose type argument 'int' is not a delegate type
                //         Expression<int> ex1 = ()=>1;
                Diagnostic(ErrorCode.ERR_ExpressionTreeMustHaveDelegate, "=>").WithArguments("int").WithLocation(72, 33));
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
                // (9,44): error CS1593: Delegate 'Func<int, int>' does not take 0 arguments
                //         Expression<Func<int,int>> ex1 = () => 1;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Func<int, int>", "0").WithLocation(9, 44),
                // (10,49): error CS1678: Parameter 1 is declared as type 'double' but should be 'int'
                //         Expression<Func<int,int>> ex2 = (double d) => 1;
                Diagnostic(ErrorCode.ERR_BadParamType, "d").WithArguments("1", "", "double", "", "int").WithLocation(10, 49),
                // (10,52): error CS1661: Cannot convert lambda expression to type 'Expression<Func<int, int>>' because the parameter types do not match the delegate parameter types
                //         Expression<Func<int,int>> ex2 = (double d) => 1;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression<System.Func<int, int>>").WithLocation(10, 52));
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
                new[] { TestMetadata.Net451.System });

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
            csProject.VerifyDiagnostics(
                // (6,15): error CS0721: 'GC': static types cannot be used as parameters
                //         M.F = x=>{};
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "x").WithArguments("System.GC").WithLocation(6, 15));
        }

        [Fact]
        public void StaticTypeLambdaParameter()
        {
            var source = """
static class StaticClass {}

class Program
{
    public void M()
    {
        var lam = (StaticClass sc) => sc;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,20): error CS0721: 'StaticClass': static types cannot be used as parameters
                //         var lam = (StaticClass sc) => sc;
                Diagnostic(ErrorCode.ERR_ParameterIsStaticClass, "StaticClass").WithArguments("StaticClass").WithLocation(7, 20));
        }

        [Fact]
        public void ScopedNonRefValueRefStructLambdaParam()
        {
            var source = """
public class Program
{
    public void M()
    {
        var lam = (scoped int n) => n;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,20): error CS9048: The 'scoped' modifier can be used for refs and ref struct values only.
                //         var lam = (scoped int n) => n;
                Diagnostic(ErrorCode.ERR_ScopedRefAndRefStructOnly, "scoped int n").WithLocation(5, 20));
        }

        [WorkItem(62960, "https://github.com/dotnet/roslyn/issues/62960")]
        [Fact]
        public void ReferenceToRestrictedTypeLambdaParameter()
        {
            var source = """
using System;
public class Program
{
    public void M()
    {
        var lam = (ref TypedReference r) => {};
    }   
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,20): error CS1601: Cannot make reference to variable of type 'TypedReference'
                //         var lam = (ref TypedReference r) => {};
                Diagnostic(ErrorCode.ERR_MethodArgCantBeRefAny, "ref TypedReference r").WithArguments("System.TypedReference").WithLocation(6, 20));
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
            csProject.VerifyEmitDiagnostics(
                // (8,22): warning CS0219: The variable 'message' is assigned but its value is never used
                //         const string message = "The parameter is obsolete";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "message").WithArguments("message").WithLocation(8, 22),
                // (9,35): error CS7014: Attributes are not valid in this context.
                //         Action<int> a = delegate ([ObsoleteAttribute(message)] int x) { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]").WithLocation(9, 35));
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
                // (5,40): error CS1660: Cannot convert lambda expression to type 'string' because it is not a delegate type
                //     public Program(string x) : this(() => x) { }
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "string").WithLocation(5, 40),
                // (8,55): error CS0103: The name 'nulF' does not exist in the current context
                //         ((Action<string>)(f => Console.WriteLine(f)))(nulF);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nulF").WithArguments("nulF").WithLocation(8, 55));
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
                // (6,53): error CS1593: Delegate 'Func<int>' does not take 1 arguments
                //         System.Func<int> f = new System.Func<int>(r => 0);
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "=>").WithArguments("System.Func<int>", "1").WithLocation(6, 53));
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
            var tree = SyntaxFactory.ParseSyntaxTree(source, options: TestOptions.Regular9);
            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(
                // (9,22): error CS1660: Cannot convert lambda expression to type 'IList<C>' because it is not a delegate type
                //         tmp.M((a, b) => c.Add);
                Diagnostic(ErrorCode.ERR_AnonMethToNonDel, "=>").WithArguments("lambda expression", "System.Collections.Generic.IList<C>").WithLocation(9, 22));

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
    Diagnostic(ErrorCode.ERR_SyntaxError, "Unbound2").WithArguments(",").WithLocation(2, 61),
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
    static void M<@x>()
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
    static void M<@x>()
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
            comp.VerifyDiagnostics(
                // (8,37): error CS8370: Feature 'lambda discard parameters' is not available in C# 7.3. Please use language version 9.0 or greater.
                //         Func<int, int, int> f = (_, _) => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(8, 37));

            comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
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

        [Fact]
        public void LambdaAttributes_01()
        {
            var sourceA =
@"using System;
class A : Attribute { }
class B : Attribute { }
partial class Program
{
    static Delegate D1() => (Action)([A] () => { });
    static Delegate D2(int x) => (Func<int, int, int>)((int y, [A][B] int z) => x);
    static Delegate D3() => (Action<int, object>)(([A]_, y) => { });
    Delegate D4() => (Func<int>)([return: A][B] () => GetHashCode());
}";
            var sourceB =
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
partial class Program
{
    static string GetAttributeString(object a)
    {
        return a.GetType().FullName;
    }
    static void Report(Delegate d)
    {
        var m = d.Method;
        var forMethod = ToString(""method"", m.GetCustomAttributes(inherit: false));
        var forReturn = ToString(""return"", m.ReturnTypeCustomAttributes.GetCustomAttributes(inherit: false));
        var forParameters = ToString(""parameter"", m.GetParameters().SelectMany(p => p.GetCustomAttributes(inherit: false)));
        Console.WriteLine(""{0}:{1}{2}{3}"", m.Name, forMethod, forReturn, forParameters);
    }
    static string ToString(string target, IEnumerable<object> attributes)
    {
        var builder = new StringBuilder();
        foreach (var attribute in attributes)
            builder.Append($"" [{target}: {attribute}]"");
        return builder.ToString();
    }
    static void Main()
    {
        Report(D1());
        Report(D2(0));
        Report(D3());
        Report(new Program().D4());
    }
}";

            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.Regular10, options: TestOptions.ReleaseExe);
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>();
            var pairs = exprs.Select(e => (e, model.GetSymbolInfo(e).Symbol)).ToArray();
            var expectedAttributes = new[]
            {
                "[A] () => { }: [method: A]",
                "(int y, [A][B] int z) => x: [parameter: A] [parameter: B]",
                "([A]_, y) => { }: [parameter: A]",
                "[return: A][B] () => GetHashCode(): [method: B] [return: A]",
            };
            AssertEx.Equal(expectedAttributes, pairs.Select(p => getAttributesInternal(p.Item1, p.Item2)));
            AssertEx.Equal(expectedAttributes, pairs.Select(p => getAttributesPublic(p.Item1, p.Item2)));

            CompileAndVerify(comp, expectedOutput:
@"<D1>b__0_0: [method: A]
<D2>b__0: [parameter: A] [parameter: B]
<D3>b__2_0: [parameter: A]
<D4>b__3_0: [method: System.Runtime.CompilerServices.CompilerGeneratedAttribute] [method: B] [return: A]");

            static string getAttributesInternal(LambdaExpressionSyntax expr, ISymbol symbol)
            {
                var method = symbol.GetSymbol<MethodSymbol>();
                return format(expr, method.GetAttributes(), method.GetReturnTypeAttributes(), method.Parameters.SelectMany(p => p.GetAttributes()));
            }

            static string getAttributesPublic(LambdaExpressionSyntax expr, ISymbol symbol)
            {
                var method = (IMethodSymbol)symbol;
                return format(expr, method.GetAttributes(), method.GetReturnTypeAttributes(), method.Parameters.SelectMany(p => p.GetAttributes()));
            }

            static string format(LambdaExpressionSyntax expr, IEnumerable<object> methodAttributes, IEnumerable<object> returnAttributes, IEnumerable<object> parameterAttributes)
            {
                var forMethod = toString("method", methodAttributes);
                var forReturn = toString("return", returnAttributes);
                var forParameters = toString("parameter", parameterAttributes);
                return $"{expr}:{forMethod}{forReturn}{forParameters}";
            }

            static string toString(string target, IEnumerable<object> attributes)
            {
                var builder = new StringBuilder();
                foreach (var attribute in attributes)
                    builder.Append($" [{target}: {attribute}]");
                return builder.ToString();
            }
        }

        [Fact]
        public void LambdaAttributes_02()
        {
            var source =
@"using System;
class AAttribute : Attribute { }
class BAttribute : Attribute { }
class C
{
    static void Main()
    {
        Action<object, object> a;
        a = [A, B] (x, y) => { };
        a = ([A] x, [B] y) => { };
        a = (object x, [A][B] object y) => { };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (9,13): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = [A, B] (x, y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[A, B]").WithArguments("lambda attributes", "10.0").WithLocation(9, 13),
                // (10,14): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = ([A] x, [B] y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[A]").WithArguments("lambda attributes", "10.0").WithLocation(10, 14),
                // (10,21): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = ([A] x, [B] y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[B]").WithArguments("lambda attributes", "10.0").WithLocation(10, 21),
                // (11,24): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = (object x, [A][B] object y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[A]").WithArguments("lambda attributes", "10.0").WithLocation(11, 24),
                // (11,27): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = (object x, [A][B] object y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[B]").WithArguments("lambda attributes", "10.0").WithLocation(11, 27));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaAttributes_03()
        {
            var source =
@"using System;
class AAttribute : Attribute { }
class BAttribute : Attribute { }
class C
{
    static void Main()
    {
        Action<object, object> a = delegate (object x, [A][B] object y) { };
        Func<object, object> f = [A][B] x => x;
    }
}";

            var expectedDiagnostics = new[]
            {
                // (8,56): error CS7014: Attributes are not valid in this context.
                //         Action<object, object> a = delegate (object x, [A][B] object y) { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[A]").WithLocation(8, 56),
                // (8,59): error CS7014: Attributes are not valid in this context.
                //         Action<object, object> a = delegate (object x, [A][B] object y) { };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[B]").WithLocation(8, 59),
                // (9,34): error CS8916: Attributes on lambda expressions require a parenthesized parameter list.
                //         Func<object, object> f = [A][B] x => x;
                Diagnostic(ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression, "[A]").WithLocation(9, 34),
                // (9,37): error CS8916: Attributes on lambda expressions require a parenthesized parameter list.
                //         Func<object, object> f = [A][B] x => x;
                Diagnostic(ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression, "[B]").WithLocation(9, 37)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void LambdaAttributes_04()
        {
            var sourceA =
@"namespace N1
{
    class A1Attribute : System.Attribute { }
}
namespace N2
{
    class A2Attribute : System.Attribute { }
}";
            var sourceB =
@"using N1;
using N2;
class Program
{
    static void Main()
    {
        System.Action a1 = [A1] () => { };
        System.Action<object> a2 = ([A2] object obj) => { };
    }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaAttributes_05()
        {
            var source =
@"class Program
{
    static void Main()
    {
        System.Action a1 = [A1] () => { };
        System.Func<object> a2 = [return: A2] () => null;
        System.Action<object> a3 = ([A3] object obj) => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,29): error CS0246: The type or namespace name 'A1Attribute' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action a1 = [A1] () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A1").WithArguments("A1Attribute").WithLocation(5, 29),
                // (5,29): error CS0246: The type or namespace name 'A1' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action a1 = [A1] () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A1").WithArguments("A1").WithLocation(5, 29),
                // (6,43): error CS0246: The type or namespace name 'A2Attribute' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Func<object> a2 = [return: A2] () => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A2").WithArguments("A2Attribute").WithLocation(6, 43),
                // (6,43): error CS0246: The type or namespace name 'A2' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Func<object> a2 = [return: A2] () => null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A2").WithArguments("A2").WithLocation(6, 43),
                // (7,38): error CS0246: The type or namespace name 'A3Attribute' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action<object> a3 = ([A3] object obj) => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A3").WithArguments("A3Attribute").WithLocation(7, 38),
                // (7,38): error CS0246: The type or namespace name 'A3' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action<object> a3 = ([A3] object obj) => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "A3").WithArguments("A3").WithLocation(7, 38));
        }

        [Fact]
        public void LambdaAttributes_06()
        {
            var source =
@"using System;
class AAttribute : Attribute
{
    public AAttribute(Action a) { }
}
[A([B] () => { })]
class BAttribute : Attribute
{
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,11): error CS1003: Syntax error, ',' expected
                // [A([B] () => { })]
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(6, 11),
                // (6,16): error CS1026: ) expected
                // [A([B] () => { })]
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(6, 16),
                // (6,16): error CS1003: Syntax error, ']' expected
                // [A([B] () => { })]
                Diagnostic(ErrorCode.ERR_SyntaxError, "}").WithArguments("]").WithLocation(6, 16),
                // (6,16): error CS1022: Type or namespace definition, or end-of-file expected
                // [A([B] () => { })]
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(6, 16),
                // (6,17): error CS1022: Type or namespace definition, or end-of-file expected
                // [A([B] () => { })]
                Diagnostic(ErrorCode.ERR_EOFExpected, ")").WithLocation(6, 17));
        }

        [Fact]
        public void LambdaAttributes_BadAttributeLocation()
        {
            var source =
@"using System;

[AttributeUsage(AttributeTargets.Property)]
class PropAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
class MethodAttribute : Attribute { }

[AttributeUsage(AttributeTargets.ReturnValue)]
class ReturnAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Parameter)]
class ParamAttribute : Attribute { }

[AttributeUsage(AttributeTargets.GenericParameter)]
class TypeParamAttribute : Attribute { }

class Program
{
    static void Main()
    {
        Action<object> a =
            [Prop] // 1
            [Return] // 2
            [Method]
            [return: Prop] // 3
            [return: Return]
            [return: Method] // 4
            (
            [Param]
            [TypeParam] // 5
            object o) =>
        {
        };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (23,14): error CS0592: Attribute 'Prop' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                //             [Prop] // 1
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Prop").WithArguments("Prop", "property, indexer").WithLocation(23, 14),
                // (24,14): error CS0592: Attribute 'Return' is not valid on this declaration type. It is only valid on 'return' declarations.
                //             [Return] // 2
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Return").WithArguments("Return", "return").WithLocation(24, 14),
                // (26,22): error CS0592: Attribute 'Prop' is not valid on this declaration type. It is only valid on 'property, indexer' declarations.
                //             [return: Prop] // 3
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Prop").WithArguments("Prop", "property, indexer").WithLocation(26, 22),
                // (28,22): error CS0592: Attribute 'Method' is not valid on this declaration type. It is only valid on 'method' declarations.
                //             [return: Method] // 4
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "Method").WithArguments("Method", "method").WithLocation(28, 22),
                // (31,14): error CS0592: Attribute 'TypeParam' is not valid on this declaration type. It is only valid on 'type parameter' declarations.
                //             [TypeParam] // 5
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "TypeParam").WithArguments("TypeParam", "type parameter").WithLocation(31, 14));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambda = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var symbol = (IMethodSymbol)model.GetSymbolInfo(lambda).Symbol;
            Assert.NotNull(symbol);

            verifyAttributes(symbol.GetAttributes(), "PropAttribute", "ReturnAttribute", "MethodAttribute");
            verifyAttributes(symbol.GetReturnTypeAttributes(), "PropAttribute", "ReturnAttribute", "MethodAttribute");
            verifyAttributes(symbol.Parameters[0].GetAttributes(), "ParamAttribute", "TypeParamAttribute");

            void verifyAttributes(ImmutableArray<AttributeData> attributes, params string[] expectedAttributeNames)
            {
                var actualAttributes = attributes.SelectAsArray(a => a.AttributeClass.GetSymbol());
                var expectedAttributes = expectedAttributeNames.Select(n => comp.GetTypeByMetadataName(n));
                AssertEx.Equal(expectedAttributes, actualAttributes);
            }
        }

        [Fact]
        public void LambdaAttributes_AttributeSemanticModel()
        {
            var source =
@"using System;
class AAttribute : Attribute { }
class BAttribute : Attribute { }
class CAttribute : Attribute { }
class DAttribute : Attribute { }
class Program
{
    static void Main()
    {
        Action a = [A] () => { };
        Func<object> b = [return: B] () => null;
        Action<object> c = ([C] object obj) => { };
        Func<object, object> d = [D] x => x;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (13,34): error CS8916: Attributes on lambda expressions require a parenthesized parameter list.
                //         Func<object, object> d = [D] x => x;
                Diagnostic(ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression, "[D]").WithLocation(13, 34));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var attributeSyntaxes = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().ToImmutableArray();
            Assert.Equal(4, attributeSyntaxes.Length);
            verify(attributeSyntaxes[0], "AAttribute");
            verify(attributeSyntaxes[1], "BAttribute");
            verify(attributeSyntaxes[2], "CAttribute");
            verify(attributeSyntaxes[3], "DAttribute");

            void verify(AttributeSyntax attributeSyntax, string expectedAttributeName)
            {
                var expectedAttributeConstructor = comp.GetTypeByMetadataName(expectedAttributeName).InstanceConstructors.Single().GetPublicSymbol();
                var expectedAttributeType = expectedAttributeConstructor.ContainingType;
                var typeInfo = model.GetTypeInfo(attributeSyntax);
                Assert.Equal(expectedAttributeType, typeInfo.Type);
                Assert.Equal(expectedAttributeType, typeInfo.ConvertedType);
                var symbol = model.GetSymbolInfo(attributeSyntax).Symbol;
                Assert.Equal(expectedAttributeConstructor, symbol);
            }
        }

        [Theory]
        [InlineData("Action a = [A] () => { };")]
        [InlineData("Func<object> f = [return: A] () => null;")]
        [InlineData("Action<int> a = ([A] int i) => { };")]
        public void LambdaAttributes_SpeculativeSemanticModel(string statement)
        {
            string source =
$@"using System;
class AAttribute : Attribute {{ }}
class Program
{{
    static void Main()
    {{
        {statement}
    }}
}}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var a = (IdentifierNameSyntax)tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single().Name;
            Assert.Equal("A", a.Identifier.Text);
            var attrInfo = model.GetSymbolInfo(a);
            var attrType = comp.GetMember<NamedTypeSymbol>("AAttribute").GetPublicSymbol();
            var attrCtor = attrType.GetMember(".ctor");
            Assert.Equal(attrCtor, attrInfo.Symbol);

            // Assert that this is also true for the speculative semantic model
            var newTree = SyntaxFactory.ParseSyntaxTree(source + " ");
            var m = newTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();

            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(m.Body.SpanStart, m, out model));

            a = (IdentifierNameSyntax)newTree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single().Name;
            Assert.Equal("A", a.Identifier.Text);

            var info = model.GetSymbolInfo(a);
            Assert.Equal(attrCtor, info.Symbol);
        }

        [Fact]
        public void LambdaAttributes_DisallowedAttributes()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
namespace System.Runtime.CompilerServices
{
    public class IsReadOnlyAttribute : Attribute { }
    public class IsUnmanagedAttribute : Attribute { }
    public class IsByRefLikeAttribute : Attribute { }
    public class NullableContextAttribute : Attribute { public NullableContextAttribute(byte b) { } }
}
class Program
{
    static void Main()
    {
        Action a =
            [IsReadOnly] // 1
            [IsUnmanaged] // 2
            [IsByRefLike] // 3
            [Extension] // 4
            [NullableContext(0)] // 5
            () => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (15,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsReadOnlyAttribute'. This is reserved for compiler usage.
                //             [IsReadOnly] // 1
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsReadOnly").WithArguments("System.Runtime.CompilerServices.IsReadOnlyAttribute").WithLocation(15, 14),
                // (16,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsUnmanagedAttribute'. This is reserved for compiler usage.
                //             [IsUnmanaged] // 2
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsUnmanaged").WithArguments("System.Runtime.CompilerServices.IsUnmanagedAttribute").WithLocation(16, 14),
                // (17,14): error CS8335: Do not use 'System.Runtime.CompilerServices.IsByRefLikeAttribute'. This is reserved for compiler usage.
                //             [IsByRefLike] // 3
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "IsByRefLike").WithArguments("System.Runtime.CompilerServices.IsByRefLikeAttribute").WithLocation(17, 14),
                // (18,14): error CS1112: Do not use 'System.Runtime.CompilerServices.ExtensionAttribute'. Use the 'this' keyword instead.
                //             [Extension] // 4
                Diagnostic(ErrorCode.ERR_ExplicitExtension, "Extension").WithLocation(18, 14),
                // (19,14): error CS8335: Do not use 'System.Runtime.CompilerServices.NullableContextAttribute'. This is reserved for compiler usage.
                //             [NullableContext(0)] // 5
                Diagnostic(ErrorCode.ERR_ExplicitReservedAttr, "NullableContext(0)").WithArguments("System.Runtime.CompilerServices.NullableContextAttribute").WithLocation(19, 14));
        }

        [Fact]
        public void LambdaAttributes_DisallowedSecurityAttributes()
        {
            var source =
@"using System;
using System.Security;
class Program
{
    static void Main()
    {
        Action a =
            [SecurityCritical] // 1
            [SecuritySafeCriticalAttribute] // 2
            async () => { }; // 3
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,14): error CS4030: Security attribute 'SecurityCritical' cannot be applied to an Async method.
                //             [SecurityCritical] // 1
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecurityCritical").WithArguments("SecurityCritical").WithLocation(8, 14),
                // (9,14): error CS4030: Security attribute 'SecuritySafeCriticalAttribute' cannot be applied to an Async method.
                //             [SecuritySafeCriticalAttribute] // 2
                Diagnostic(ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync, "SecuritySafeCriticalAttribute").WithArguments("SecuritySafeCriticalAttribute").WithLocation(9, 14),
                // (10,22): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //             async () => { }; // 3
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(10, 22));
        }

        [Fact]
        public void LambdaAttributes_ObsoleteAttribute()
        {
            var source =
@"using System;
class Program
{
    static void Report(Action a)
    {
        foreach (var attribute in a.Method.GetCustomAttributes(inherit: false))
            Console.Write(attribute);
    }
    static void Main()
    {
        Report([Obsolete] () => { });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "System.ObsoleteAttribute");

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var symbol = model.GetSymbolInfo(expr).Symbol;
            Assert.Equal("System.ObsoleteAttribute", symbol.GetAttributes().Single().ToString());
        }

        [Fact]
        public void LambdaParameterAttributes_Conditional()
        {
            var source =
@"using System;
using System.Diagnostics;
class Program
{
    static void Report(Action a)
    {
    }
    static void Main()
    {
        Report([Conditional(""DEBUG"")] static () => { });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview, options: TestOptions.ReleaseExe);
            comp.VerifyDiagnostics(
                // (10,17): error CS0577: The Conditional attribute is not valid on 'lambda expression' because it is a constructor, destructor, operator, lambda expression, or explicit interface implementation
                //         Report([Conditional("DEBUG")] static () => { });
                Diagnostic(ErrorCode.ERR_ConditionalOnSpecialMethod, @"Conditional(""DEBUG"")").WithArguments("lambda expression").WithLocation(10, 17));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambda = exprs.SelectAsArray(e => GetLambdaSymbol(model, e)).Single();
            Assert.Equal(new[] { "DEBUG" }, lambda.GetAppliedConditionalSymbols());
        }

        [Fact]
        public void LambdaAttributes_WellKnownAttributes()
        {
            var sourceA =
@"using System;
using System.Runtime.InteropServices;
using System.Security;
class Program
{
    static void Main()
    {
        Action a1 = [DllImport(""MyModule.dll"")] static () => { };
        Action a2 = [DynamicSecurityMethod] () => { };
        Action a3 = [SuppressUnmanagedCodeSecurity] () => { };
        Func<object> a4 = [return: MarshalAs((short)0)] () => null;
    }
}";
            var sourceB =
@"namespace System.Security
{
    internal class DynamicSecurityMethodAttribute : Attribute { }
}";
            var comp = CreateCompilation(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,22): error CS0601: The DllImport attribute must be specified on a method marked 'static' and 'extern'
                //         Action a1 = [DllImport("MyModule.dll")] static () => { };
                Diagnostic(ErrorCode.ERR_DllImportOnInvalidMethod, "DllImport").WithLocation(8, 22));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            Assert.Equal(4, exprs.Length);
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Null(lambdas[0].GetDllImportData()); // [DllImport] is ignored if there are errors.
            Assert.True(lambdas[1].RequiresSecurityObject);
            Assert.True(lambdas[2].HasDeclarativeSecurity);
            Assert.Equal(default, lambdas[3].ReturnValueMarshallingInformation.UnmanagedType);
        }

        [Fact]
        public void LambdaAttributes_Permissions()
        {
            var source =
@"#pragma warning disable 618
using System;
using System.Security.Permissions;
class Program
{
    static void Main()
    {
        Action a1 = [PermissionSet(SecurityAction.Deny)] () => { };
    }
}";
            var comp = CreateCompilationWithMscorlib40(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambda = exprs.SelectAsArray(e => GetLambdaSymbol(model, e)).Single();
            Assert.NotEmpty(lambda.GetSecurityInformation());
        }

        [Fact]
        public void LambdaAttributes_NullableAttributes_01()
        {
            var source =
@"using System;
using System.Diagnostics.CodeAnalysis;
class Program
{
    static void Main()
    {
        Func<object> a1 = [return: MaybeNull][return: NotNull] () => null;
        Func<object, object> a2 = [return: NotNullIfNotNull(""obj"")] (object obj) => obj;
        Func<bool> a4 = [MemberNotNull(""x"")][MemberNotNullWhen(false, ""y"")][MemberNotNullWhen(true, ""z"")] () => true;
    }
}";
            var comp = CreateCompilation(
                new[] { source, MaybeNullAttributeDefinition, NotNullAttributeDefinition, NotNullIfNotNullAttributeDefinition, MemberNotNullAttributeDefinition, MemberNotNullWhenAttributeDefinition },
                parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            Assert.Equal(3, exprs.Length);
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Equal(FlowAnalysisAnnotations.MaybeNull | FlowAnalysisAnnotations.NotNull, lambdas[0].ReturnTypeFlowAnalysisAnnotations);
            Assert.Equal(new[] { "obj" }, lambdas[1].ReturnNotNullIfParameterNotNull);
            Assert.Equal(new[] { "x" }, lambdas[2].NotNullMembers);
            Assert.Equal(new[] { "y" }, lambdas[2].NotNullWhenFalseMembers);
            Assert.Equal(new[] { "z" }, lambdas[2].NotNullWhenTrueMembers);
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_02()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
class Program
{
    static void Main()
    {
        Func<object> a1 = [return: MaybeNull] () => null;
        Func<object?> a2 = [return: NotNull] () => null;
    }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition, NotNullAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,27): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object>' (possibly because of nullability attributes).
                //         Func<object> a1 = [return: MaybeNull] () => null;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "System.Func<object>").WithLocation(8, 27),
                // (9,28): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object?>' (possibly because of nullability attributes).
                //         Func<object?> a2 = [return: NotNull] () => null;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] () =>").WithArguments("lambda expression", "System.Func<object?>").WithLocation(9, 28),
                // (9,52): warning CS8603: Possible null reference return.
                //         Func<object?> a2 = [return: NotNull] () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(9, 52)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_Return()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate object D1();
[return: MaybeNull] delegate object D2();
[return: NotNull] delegate object? D3();

class Program
{
    static void Main()
    {
        Func<object> f1 = [return: MaybeNull] () => null; // 1
        D1 f2 = [return: MaybeNull] () => null; // 2
        D2 f3 = [return: MaybeNull] () => null;
        D3 f4 = [return: MaybeNull] () => null; // 3

        Func<object> f5 = [return: NotNull] () => null;
        D1 f6 = [return: NotNull] () => null;
        D2 f7 = [return: NotNull] () => null; // 4
        D3 f8 = [return: NotNull] () => null;

        Func<object?> f9 = [return: NotNull] () => null; // 5
        D1 f10 = [return: NotNull] object? () => null;
        D2 f11 = [return: NotNull] object? () => null; // 6
        D3 f12 = [return: NotNull] object? () => null;
    }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition, NotNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,27): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object>' (possibly because of nullability attributes).
                //         Func<object> f1 = [return: MaybeNull] () => null; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "System.Func<object>").WithLocation(13, 27),
                // (14,17): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         D1 f2 = [return: MaybeNull] () => null; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "D1").WithLocation(14, 17),
                // (16,17): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 f4 = [return: MaybeNull] () => null; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "D3").WithLocation(16, 17),
                // (18,51): warning CS8603: Possible null reference return.
                //         Func<object> f5 = [return: NotNull] () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(18, 51),
                // (19,41): warning CS8603: Possible null reference return.
                //         D1 f6 = [return: NotNull] () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(19, 41),
                // (20,17): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 f7 = [return: NotNull] () => null; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] () =>").WithArguments("lambda expression", "D2").WithLocation(20, 17),
                // (20,41): warning CS8603: Possible null reference return.
                //         D2 f7 = [return: NotNull] () => null; // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(20, 41),
                // (21,41): warning CS8603: Possible null reference return.
                //         D3 f8 = [return: NotNull] () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(21, 41),
                // (23,28): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object?>' (possibly because of nullability attributes).
                //         Func<object?> f9 = [return: NotNull] () => null; // 5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] () =>").WithArguments("lambda expression", "System.Func<object?>").WithLocation(23, 28),
                // (23,52): warning CS8603: Possible null reference return.
                //         Func<object?> f9 = [return: NotNull] () => null; // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(23, 52),
                // (24,50): warning CS8603: Possible null reference return.
                //         D1 f10 = [return: NotNull] object? () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(24, 50),
                // (25,18): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 f11 = [return: NotNull] object? () => null; // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] object? () =>").WithArguments("lambda expression", "D2").WithLocation(25, 18),
                // (25,50): warning CS8603: Possible null reference return.
                //         D2 f11 = [return: NotNull] object? () => null; // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(25, 50),
                // (26,50): warning CS8603: Possible null reference return.
                //         D3 f12 = [return: NotNull] object? () => null;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(26, 50)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_Return_Suppressed()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate object D1();
[return: MaybeNull] delegate object D2();
[return: NotNull] delegate object? D3();

class Program
{
    static void Main()
    {
        Func<object> f1 = ([return: MaybeNull] () => null)!;
        D1 f2 = ([return: MaybeNull] () => null)!;
        D3 f4 = ([return: MaybeNull] () => null)!;

        D2 f7 = ([return: NotNull] () => null)!;

        Func<object?> f9 = ([return: NotNull] () => null)!;
        D2 f11 = ([return: NotNull] object? () => null)!;
    }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition, NotNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (17,42): warning CS8603: Possible null reference return.
                //         D2 f7 = ([return: NotNull] () => null)!;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(17, 42),
                // (19,53): warning CS8603: Possible null reference return.
                //         Func<object?> f9 = ([return: NotNull] () => null)!;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(19, 53),
                // (20,51): warning CS8603: Possible null reference return.
                //         D2 f11 = ([return: NotNull] object? () => null)!;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(20, 51)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_DelegateCreation_Return()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate object D1();
[return: MaybeNull] delegate object D2();
[return: NotNull] delegate object? D3();

class Program
{
    static void Main()
    {
        _ = new Func<object>([return: MaybeNull] () => null); // 1
        _ = new D1([return: MaybeNull] () => null); // 2
        _ = new D2([return: MaybeNull] () => null);
        _ = new D3([return: MaybeNull] () => null); // 3

        _ = new Func<object>([return: NotNull] () => null);
        _ = new D1([return: NotNull] () => null);
        _ = new D2([return: NotNull] () => null); // 4
        _ = new D3([return: NotNull] () => null);

        _ = new Func<object?>([return: NotNull] () => null); // 5
        _ = new D1([return: NotNull] object? () => null);
        _ = new D2([return: NotNull] object? () => null); // 6
        _ = new D3([return: NotNull] object? () => null);
    }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullAttributeDefinition, NotNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,30): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object>' (possibly because of nullability attributes).
                //         _ = new Func<object>([return: MaybeNull] () => null); // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "System.Func<object>").WithLocation(13, 30),
                // (14,20): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         _ = new D1([return: MaybeNull] () => null); // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "D1").WithLocation(14, 20),
                // (16,20): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         _ = new D3([return: MaybeNull] () => null); // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: MaybeNull] () =>").WithArguments("lambda expression", "D3").WithLocation(16, 20),
                // (18,54): warning CS8603: Possible null reference return.
                //         _ = new Func<object>([return: NotNull] () => null);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(18, 54),
                // (19,44): warning CS8603: Possible null reference return.
                //         _ = new D1([return: NotNull] () => null);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(19, 44),
                // (20,20): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         _ = new D2([return: NotNull] () => null); // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] () =>").WithArguments("lambda expression", "D2").WithLocation(20, 20),
                // (20,44): warning CS8603: Possible null reference return.
                //         _ = new D2([return: NotNull] () => null); // 4
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(20, 44),
                // (21,44): warning CS8603: Possible null reference return.
                //         _ = new D3([return: NotNull] () => null);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(21, 44),
                // (23,31): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<object?>' (possibly because of nullability attributes).
                //         _ = new Func<object?>([return: NotNull] () => null); // 5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] () =>").WithArguments("lambda expression", "System.Func<object?>").WithLocation(23, 31),
                // (23,55): warning CS8603: Possible null reference return.
                //         _ = new Func<object?>([return: NotNull] () => null); // 5
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(23, 55),
                // (24,52): warning CS8603: Possible null reference return.
                //         _ = new D1([return: NotNull] object? () => null);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(24, 52),
                // (25,20): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         _ = new D2([return: NotNull] object? () => null); // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "[return: NotNull] object? () =>").WithArguments("lambda expression", "D2").WithLocation(25, 20),
                // (25,52): warning CS8603: Possible null reference return.
                //         _ = new D2([return: NotNull] object? () => null); // 6
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(25, 52),
                // (26,52): warning CS8603: Possible null reference return.
                //         _ = new D3([return: NotNull] object? () => null);
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(26, 52)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_Parameter()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate void D1(object o);
delegate void D2([AllowNull] object o);
delegate void D3([DisallowNull] object? o);

class Program
{
    static void Main()
    {
        Action<object> x1 = ([AllowNull] object o) => { }; // 1
        D1 x2 = ([AllowNull] object o) => { }; // 2
        D2 x3 = ([AllowNull] object o) => { };
        D3 x4 = ([AllowNull] object o) => { }; // 3

        Action<object> x5 = ([DisallowNull] object o) => { };
        D1 x6 = ([DisallowNull] object o) => { };
        D2 x7 = ([DisallowNull] object o) => { }; // 4
        D3 x8 = ([DisallowNull] object o) => { };

        Action<object?> x9 = ([DisallowNull] object? o) => { }; // 5
        D1 x10 = ([DisallowNull] object? o) => { };
        D2 x11 = ([DisallowNull] object? o) => { }; // 6
        D3 x12 = ([DisallowNull] object? o) => { };
    }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,29): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object>' (possibly because of nullability attributes).
                //         Action<object> x1 = ([AllowNull] object o) => { }; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "System.Action<object>").WithLocation(13, 29),
                // (14,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         D1 x2 = ([AllowNull] object o) => { }; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "D1").WithLocation(14, 17),
                // (16,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 x4 = ([AllowNull] object o) => { }; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(16, 17),
                // (20,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x7 = ([DisallowNull] object o) => { }; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(20, 17),
                // (23,30): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object?>' (possibly because of nullability attributes).
                //         Action<object?> x9 = ([DisallowNull] object? o) => { }; // 5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object? o) =>").WithArguments("o", "lambda expression", "System.Action<object?>").WithLocation(23, 30),
                // (25,18): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x11 = ([DisallowNull] object? o) => { }; // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object? o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(25, 18)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_Parameter_WithoutType()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate void D1(object o);
delegate void D2([AllowNull] object o);
delegate void D3([DisallowNull] object? o);

class Program
{
    static void Main()
    {
        Action<object> x1 = (o) => { o.ToString(); };
        Action<object?> x2 = (o) => { o.ToString(); }; // 1
        D1 x3 = (o) => { o.ToString(); };
        D2 x4 = (o) => { o.ToString(); }; // 2
        D3 x5 = (o) => { o.ToString(); }; // 3, 4
    }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (14,39): warning CS8602: Dereference of a possibly null reference.
                //         Action<object?> x2 = (o) => { o.ToString(); }; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(14, 39),
                // (16,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x4 = (o) => { o.ToString(); }; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(16, 17),
                // (17,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 x5 = (o) => { o.ToString(); }; // 3, 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "(o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(17, 17),
                // (17,26): warning CS8602: Dereference of a possibly null reference.
                //         D3 x5 = (o) => { o.ToString(); }; // 3, 4
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "o").WithLocation(17, 26)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_Parameter_ConditionalAttributes()
        {
            var source =
@"#nullable enable
using System.Diagnostics.CodeAnalysis;

delegate bool D1([MaybeNullWhen(true)] out object o);
delegate bool D2([MaybeNullWhen(false)] out object o);
delegate bool D3([NotNullWhen(true)] out object? o);

class Program
{
    static void Main()
    {
        D1 x1 = bool ([MaybeNullWhen(true)] out object o) => throw null!;
        D1 x2 = bool ([MaybeNullWhen(false)] out object o) => throw null!; // 1
        D1 x3 = bool ([MaybeNull] out object o) => throw null!; // 2
        D1 x4 = bool (out object? o) => throw null!; // 3

        D2 x5 = bool ([MaybeNullWhen(true)] out object o) => throw null!; // 4
        D2 x6 = bool ([MaybeNullWhen(false)] out object o) => throw null!;
        D2 x7 = bool ([MaybeNull] out object o) => throw null!; // 5
        D2 x8 = bool (out object? o) => throw null!; // 6

        D3 x9 = bool ([MaybeNullWhen(true)] out object o) => throw null!; // 7
        D3 x10 = bool ([MaybeNullWhen(false)] out object o) => throw null!;
        D3 x11 = bool ([MaybeNull] out object o) => throw null!; // 8
        D3 x12 = bool (out object? o) => throw null!; // 9
    }
}";
            var comp = CreateCompilation(new[] { source, MaybeNullWhenAttributeDefinition, NotNullWhenAttributeDefinition, MaybeNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         D1 x2 = bool ([MaybeNullWhen(false)] out object o) => throw null!; // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNullWhen(false)] out object o) =>").WithArguments("o", "lambda expression", "D1").WithLocation(13, 17),
                // (14,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         D1 x3 = bool ([MaybeNull] out object o) => throw null!; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNull] out object o) =>").WithArguments("o", "lambda expression", "D1").WithLocation(14, 17),
                // (15,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         D1 x4 = bool (out object? o) => throw null!; // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool (out object? o) =>").WithArguments("o", "lambda expression", "D1").WithLocation(15, 17),
                // (17,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x5 = bool ([MaybeNullWhen(true)] out object o) => throw null!; // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNullWhen(true)] out object o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(17, 17),
                // (19,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x7 = bool ([MaybeNull] out object o) => throw null!; // 5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNull] out object o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(19, 17),
                // (20,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         D2 x8 = bool (out object? o) => throw null!; // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool (out object? o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(20, 17),
                // (22,17): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 x9 = bool ([MaybeNullWhen(true)] out object o) => throw null!; // 7
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNullWhen(true)] out object o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(22, 17),
                // (24,18): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 x11 = bool ([MaybeNull] out object o) => throw null!; // 8
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool ([MaybeNull] out object o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(24, 18),
                // (25,18): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 x12 = bool (out object? o) => throw null!; // 9
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "bool (out object? o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(25, 18)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_AnonymousFunctionConversion_NotNullIfNotNull()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate object? D1(object? o);
[return: NotNullIfNotNull(""o"")] delegate object? D2(object? o);
[return: NotNull] delegate object? D3(object? o);
[return: NotNullIfNotNull(""o"")] delegate object D4(object? o);

class Program
{
    static void Main()
    {
        Func<object?, object> f1 = [return: NotNullIfNotNull(""o"")] (object? o) => null; // 1
        D1 f2 = [return: NotNullIfNotNull(""o"")] (object? o) => null;
        D2 f3 = [return: NotNullIfNotNull(""o"")] (object? o) => null;
        D3 f4 = [return: NotNullIfNotNull(""o"")] (object? o) => null; // 2
        D4 f5 = [return: NotNullIfNotNull(""o"")] (object? o) => null; // 3
    }
}";
            var comp = CreateCompilation(new[] { source, NotNullIfNotNullAttributeDefinition, NotNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (14,83): warning CS8603: Possible null reference return.
                //         Func<object?, object> f1 = [return: NotNullIfNotNull("o")] (object? o) => null; // 1
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(14, 83),
                // (17,17): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         D3 f4 = [return: NotNullIfNotNull("o")] (object? o) => null; // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, @"[return: NotNullIfNotNull(""o"")] (object? o) =>").WithArguments("lambda expression", "D3").WithLocation(17, 17),
                // (18,64): warning CS8603: Possible null reference return.
                //         D4 f5 = [return: NotNullIfNotNull("o")] (object? o) => null; // 3
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "null").WithLocation(18, 64)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_DelegateCreation_Parameter()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

delegate void D1(object o);
delegate void D2([AllowNull] object o);
delegate void D3([DisallowNull] object? o);

class Program
{
    static void Main()
    {
        _ = new Action<object>(([AllowNull] object o) => { }); // 1
        _ = new D1(([AllowNull] object o) => { }); // 2
        _ = new D2(([AllowNull] object o) => { });
        _ = new D3(([AllowNull] object o) => { }); // 3

        _ = new Action<object>(([DisallowNull] object o) => { });
        _ = new D1(([DisallowNull] object o) => { });
        _ = new D2(([DisallowNull] object o) => { }); // 4
        _ = new D3(([DisallowNull] object o) => { });

        _ = new Action<object?>(([DisallowNull] object? o) => { }); // 5
        _ = new D1(([DisallowNull] object? o) => { });
        _ = new D2(([DisallowNull] object? o) => { }); // 6
        _ = new D3(([DisallowNull] object? o) => { });
    }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition });
            comp.VerifyDiagnostics(
                // (13,32): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object>' (possibly because of nullability attributes).
                //         _ = new Action<object>(([AllowNull] object o) => { }); // 1
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "System.Action<object>").WithLocation(13, 32),
                // (14,20): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D1' (possibly because of nullability attributes).
                //         _ = new D1(([AllowNull] object o) => { }); // 2
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "D1").WithLocation(14, 20),
                // (16,20): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D3' (possibly because of nullability attributes).
                //         _ = new D3(([AllowNull] object o) => { }); // 3
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] object o) =>").WithArguments("o", "lambda expression", "D3").WithLocation(16, 20),
                // (20,20): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         _ = new D2(([DisallowNull] object o) => { }); // 4
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(20, 20),
                // (23,33): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'Action<object?>' (possibly because of nullability attributes).
                //         _ = new Action<object?>(([DisallowNull] object? o) => { }); // 5
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object? o) =>").WithArguments("o", "lambda expression", "System.Action<object?>").WithLocation(23, 33),
                // (25,20): warning CS8622: Nullability of reference types in type of parameter 'o' of 'lambda expression' doesn't match the target delegate 'D2' (possibly because of nullability attributes).
                //         _ = new D2(([DisallowNull] object? o) => { }); // 6
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] object? o) =>").WithArguments("o", "lambda expression", "D2").WithLocation(25, 20)
                );
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_NullableAttributes_03()
        {
            var source =
@"#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
class Program
{
    static void Main()
    {
        Action<object> a1 = ([AllowNull] x) => { x.ToString(); };
        Action<object?> a2 = ([DisallowNull] x) => { x.ToString(); };
    }
}";
            var comp = CreateCompilation(new[] { source, AllowNullAttributeDefinition, DisallowNullAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,29): warning CS8622: Nullability of reference types in type of parameter 'x' of 'lambda expression' doesn't match the target delegate 'Action<object>' (possibly because of nullability attributes).
                //         Action<object> a1 = ([AllowNull] x) => { x.ToString(); };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([AllowNull] x) =>").WithArguments("x", "lambda expression", "System.Action<object>").WithLocation(8, 29),
                // (8,50): warning CS8602: Dereference of a possibly null reference.
                //         Action<object> a1 = ([AllowNull] x) => { x.ToString(); };
                Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "x").WithLocation(8, 50),
                // (9,30): warning CS8622: Nullability of reference types in type of parameter 'x' of 'lambda expression' doesn't match the target delegate 'Action<object?>' (possibly because of nullability attributes).
                //         Action<object?> a2 = ([DisallowNull] x) => { x.ToString(); };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([DisallowNull] x) =>").WithArguments("x", "lambda expression", "System.Action<object?>").WithLocation(9, 30)
                );
        }

        [WorkItem(55013, "https://github.com/dotnet/roslyn/issues/55013")]
        [Fact]
        public void NullableTypeArraySwitchPattern()
        {
            var source =
@"#nullable enable
class C
{
    object? field;
    string Prop => @field switch
    {
        string?[] a => ""a""
    };
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,13): warning CS0649: Field 'C.field' is never assigned to, and will always have its default value null
                //     object? field;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "field").WithArguments("C.field", "null").WithLocation(4, 13),
                // (5,27): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive). For example, the pattern '_' is not covered.
                //     string Prop => @field switch
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithArguments("_").WithLocation(5, 27));
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        public void LambdaAttributes_DoesNotReturn()
        {
            var source = @"
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
class Program
{
    static void Main()
    {
        Action a1 = [DoesNotReturn] () => { };
        Action a2 = [DoesNotReturn] () => throw new Exception();
    }
}";
            var comp = CreateCompilation(new[] { source, DoesNotReturnAttributeDefinition });
            comp.VerifyDiagnostics(
                // (9,43): warning CS8763: A method marked [DoesNotReturn] should not return.
                //         Action a1 = [DoesNotReturn] () => { };
                Diagnostic(ErrorCode.WRN_ShouldNotReturn, "{ }").WithLocation(9, 43)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            Assert.Equal(2, exprs.Length);
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Equal(FlowAnalysisAnnotations.DoesNotReturn, lambdas[0].FlowAnalysisAnnotations);
            Assert.Equal(FlowAnalysisAnnotations.DoesNotReturn, lambdas[1].FlowAnalysisAnnotations);
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaAttributes_DoesNotReturn_OnDelegateType()
        {
            var source = @"
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

[return: DoesNotReturn] delegate void DoesNotReturnDelegate();
class Program
{
    static void Main()
    {
        DoesNotReturnDelegate a1 = [DoesNotReturn] () => { }; // 1
        DoesNotReturnDelegate a2 = [DoesNotReturn] () => throw new Exception();
        DoesNotReturnDelegate a3 = () => { };
        DoesNotReturnDelegate a4 = () => throw new Exception();
    }
}";
            var comp = CreateCompilation(new[] { source, DoesNotReturnAttributeDefinition });
            comp.VerifyDiagnostics(
                // (6,10): error CS0592: Attribute 'DoesNotReturn' is not valid on this declaration type. It is only valid on 'method' declarations.
                // [return: DoesNotReturn] delegate void DoesNotReturnDelegate();
                Diagnostic(ErrorCode.ERR_AttributeOnBadSymbolType, "DoesNotReturn").WithArguments("DoesNotReturn", "method").WithLocation(6, 10),
                // (6,39): warning CS8770: Method 'void DoesNotReturnDelegate.Invoke()' lacks `[DoesNotReturn]` annotation to match implemented or overridden member.
                // [return: DoesNotReturn] delegate void DoesNotReturnDelegate();
                Diagnostic(ErrorCode.WRN_DoesNotReturnMismatch, "DoesNotReturnDelegate").WithArguments("void DoesNotReturnDelegate.Invoke()").WithLocation(6, 39),
                // (6,39): warning CS8770: Method 'void DoesNotReturnDelegate.Invoke()' lacks `[DoesNotReturn]` annotation to match implemented or overridden member.
                // [return: DoesNotReturn] delegate void DoesNotReturnDelegate();
                Diagnostic(ErrorCode.WRN_DoesNotReturnMismatch, "DoesNotReturnDelegate").WithArguments("void DoesNotReturnDelegate.Invoke()").WithLocation(6, 39),
                // (11,58): warning CS8763: A method marked [DoesNotReturn] should not return.
                //         DoesNotReturnDelegate a1 = [DoesNotReturn] () => { }; // 1
                Diagnostic(ErrorCode.WRN_ShouldNotReturn, "{ }").WithLocation(11, 58)
                );
        }

        [Fact]
        public void LambdaAttributes_UnmanagedCallersOnly()
        {
            var source =
@"using System;
using System.Runtime.InteropServices;
class Program
{
    static void Main()
    {
        Action a = [UnmanagedCallersOnly] static () => { };
    }
}";
            var comp = CreateCompilation(new[] { source, UnmanagedCallersOnlyAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,21): error CS8896: 'UnmanagedCallersOnly' can only be applied to ordinary static non-abstract, non-virtual methods or static local functions.
                //         Action a = [UnmanagedCallersOnly] static () => { };
                Diagnostic(ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic, "UnmanagedCallersOnly").WithLocation(7, 21));
        }

        [Fact]
        public void LambdaParameterAttributes_OptionalAndDefaultValueAttributes()
        {
            var source =
@"using System;
using System.Runtime.InteropServices;
class Program
{
    static void Main()
    {
        Action<int> a1 = ([Optional, DefaultParameterValue(2)] int i) => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                    // (7,68): warning CS9099: Parameter 1 has default value '2' in lambda but '<missing>' in the target delegate type.
                    //         Action<int> a1 = ([Optional, DefaultParameterValue(2)] int i) => { };
                    Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "i").WithArguments("1", "2", "<missing>").WithLocation(7, 68));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambda = exprs.SelectAsArray(e => GetLambdaSymbol(model, e)).Single();
            var parameter = (SourceParameterSymbol)lambda.Parameters[0];
            Assert.True(parameter.HasOptionalAttribute);
            Assert.True(parameter.HasExplicitDefaultValue);
            Assert.Equal(2, parameter.DefaultValueFromAttributes.Value);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66060")]
        public void LambdaParameterAttributes_OverloadResolution()
        {
            var source = """
                using System.Runtime.InteropServices;

                new C().M(([Optional] int x = 1) => x);
                new C().M((int x = 1) => x);

                class C
                {
                    public void M(D1 d) { }
                    public void M(D2 d) { }
                }

                delegate int D1(int x = 1);
                delegate int D2([Optional, DefaultParameterValue(1)] int x);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (3,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(D1)' and 'C.M(D2)'
                // new C().M(([Optional] int x = 1) => x);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(D1)", "C.M(D2)").WithLocation(3, 9),
                // (4,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(D1)' and 'C.M(D2)'
                // new C().M((int x = 1) => x);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(D1)", "C.M(D2)").WithLocation(4, 9));
        }

        [Fact]
        public void LambdaParameterAttributes_OptionalAndDateTimeConstantAttributes()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
class Program
{
    static void Main()
    {
        var lam = ([Optional, DateTimeConstant(638004778421967416L)] DateTime d) => d;
        Console.Write(lam().Ticks);
    }
}";
            var comp = CreateCompilation(source, options: TestOptions.ReleaseExe);
            CompileAndVerify(comp, expectedOutput: "638004778421967416").VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambda = exprs.SelectAsArray(e => GetLambdaSymbol(model, e)).Single();
            var parameter = (SourceParameterSymbol)lambda.Parameters[0];
            Assert.True(parameter.HasOptionalAttribute);
            Assert.True(parameter.HasExplicitDefaultValue);
            Assert.Equal(new DateTime(638004778421967416L), parameter.DefaultValueFromAttributes.Value);
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void LambdaParameterAttributes_WellKnownAttributes()
        {
            var source =
@"using System;
using System.Runtime.CompilerServices;
class Program
{
    static void Main()
    {
        Action<object> a1 = ([IDispatchConstant] object obj) => { };
        Action<object> a2 = ([IUnknownConstant] object obj) => { };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            Assert.Equal(2, exprs.Length);
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.True(lambdas[0].Parameters[0].IsIDispatchConstant);
            Assert.True(lambdas[1].Parameters[0].IsIUnknownConstant);
        }

        [Fact]
        public void LambdaParameterAttributes_NullableAttributes_01()
        {
            var source =
@"using System;
using System.Diagnostics.CodeAnalysis;
class Program
{
    static void Main()
    {
        Action<object> a1 = ([AllowNull][MaybeNullWhen(false)] object obj) => { };
        Action<object, object> a2 = (object x, [NotNullIfNotNull(""x"")] object y) => { };
    }
}";
            var comp = CreateCompilation(
                new[] { source, AllowNullAttributeDefinition, MaybeNullWhenAttributeDefinition, NotNullIfNotNullAttributeDefinition },
                parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            Assert.Equal(2, exprs.Length);
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Equal(FlowAnalysisAnnotations.AllowNull | FlowAnalysisAnnotations.MaybeNullWhenFalse, lambdas[0].Parameters[0].FlowAnalysisAnnotations);
            Assert.Equal(new[] { "x" }, lambdas[1].Parameters[1].NotNullIfParameterNotNull);
        }

        [Fact, WorkItem(52827, "https://github.com/dotnet/roslyn/issues/52827")]
        [WorkItem(56668, "https://github.com/dotnet/roslyn/issues/56668")]
        public void LambdaParameterAttributes_NullableAttributes_02()
        {
            var source =
@"#nullable enable
using System.Diagnostics.CodeAnalysis;
delegate bool D(out object? obj);
delegate bool D2([NotNullWhen(true)] out object? obj);

class Program
{
    static void Main()
    {
        D d = ([NotNullWhen(true)] out object? obj) =>
            {
                obj = null;
                return true;
            };

        D2 d2 = ([NotNullWhen(true)] out object? obj) =>
            {
                obj = null;
                return false;
            };
     }
}";
            var comp = CreateCompilation(new[] { source, NotNullWhenAttributeDefinition }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,15): warning CS8622: Nullability of reference types in type of parameter 'obj' of 'lambda expression' doesn't match the target delegate 'D' (possibly because of nullability attributes).
                //         D d = ([NotNullWhen(true)] out object? obj) =>
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate, "([NotNullWhen(true)] out object? obj) =>").WithArguments("obj", "lambda expression", "D").WithLocation(10, 15),
                // (13,17): warning CS8762: Parameter 'obj' must have a non-null value when exiting with 'true'.
                //                 return true;
                Diagnostic(ErrorCode.WRN_ParameterConditionallyDisallowsNull, "return true;").WithArguments("obj", "true").WithLocation(13, 17)
                );

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().First();
            var lambda = GetLambdaSymbol(model, expr);
            Assert.Equal(FlowAnalysisAnnotations.NotNullWhenTrue, lambda.Parameters[0].FlowAnalysisAnnotations);
        }

        [Fact]
        public void LambdaReturnType_01()
        {
            var source =
@"using System;
class Program
{
    static void F<T>()
    {
        Func<T> f1 = T () => default;
        Func<T, T> f2 = T (x) => { return x; };
        Func<T, T> f3 = T (T x) => x;
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,22): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Func<T> f1 = T () => default;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "T").WithArguments("lambda return type", "10.0").WithLocation(6, 22),
                // (7,25): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Func<T, T> f2 = T (x) => { return x; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "T").WithArguments("lambda return type", "10.0").WithLocation(7, 25),
                // (8,25): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         Func<T, T> f3 = T (T x) => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "T").WithArguments("lambda return type", "10.0").WithLocation(8, 25));

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaReturnType_02()
        {
            var source =
@"using System;
class Program
{
    static void F<T, U>()
    {
        Func<T> f1;
        Func<U> f2;
        f1 = T () => default;
        f2 = T () => default;
        f1 = U () => default;
        f2 = U () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,19): error CS8934: Cannot convert lambda expression to type 'Func<U>' because the return type does not match the delegate return type
                //         f2 = T () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<U>").WithLocation(9, 19),
                // (10,19): error CS8934: Cannot convert lambda expression to type 'Func<T>' because the return type does not match the delegate return type
                //         f1 = U () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<T>").WithLocation(10, 19));
        }

        [Fact]
        public void LambdaReturnType_03()
        {
            var source =
@"using System;
class Program
{
    static void F<T, U>() where U : T
    {
        Func<T> f1;
        Func<U> f2;
        f1 = T () => default;
        f2 = T () => default;
        f1 = U () => default;
        f2 = U () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,19): error CS8934: Cannot convert lambda expression to type 'Func<U>' because the return type does not match the delegate return type
                //         f2 = T () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<U>").WithLocation(9, 19),
                // (10,19): error CS8934: Cannot convert lambda expression to type 'Func<T>' because the return type does not match the delegate return type
                //         f1 = U () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Func<T>").WithLocation(10, 19));
        }

        [Fact]
        public void LambdaReturnType_04()
        {
            var source =
@"using System;
using System.Linq.Expressions;
class Program
{
    static void F<T, U>()
    {
        Expression<Func<T>> e1;
        Expression<Func<U>> e2;
        e1 = T () => default;
        e2 = T () => default;
        e1 = U () => default;
        e2 = U () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,19): error CS8934: Cannot convert lambda expression to type 'Expression<Func<U>>' because the return type does not match the delegate return type
                //         e2 = T () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression<System.Func<U>>").WithLocation(10, 19),
                // (11,19): error CS8934: Cannot convert lambda expression to type 'Expression<Func<T>>' because the return type does not match the delegate return type
                //         e1 = U () => default;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Linq.Expressions.Expression<System.Func<T>>").WithLocation(11, 19));
        }

        [Fact]
        public void LambdaReturnType_05()
        {
            var source =
@"#nullable enable
using System;
class Program
{
    static void Main()
    {
        Func<dynamic> f1 = object () => default!;
        Func<(int, int)> f2 = (int X, int Y) () => default;
        Func<string?> f3 = string () => default!;
        Func<IntPtr> f4 = nint () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,28): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string?>' (possibly because of nullability attributes).
                //         Func<string?> f3 = string () => default!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string () =>").WithArguments("lambda expression", "System.Func<string?>").WithLocation(9, 28));
        }

        [Fact]
        public void LambdaReturnType_06()
        {
            var source =
@"#nullable enable
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Expression<Func<object>> e1 = dynamic () => default!;
        Expression<Func<(int X, int Y)>> e2 = (int, int) () => default;
        Expression<Func<string>> e3 = string? () => default;
        Expression<Func<nint>> e4 = IntPtr () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,39): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string>' (possibly because of nullability attributes).
                //         Expression<Func<string>> e3 = string? () => default;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string? () =>").WithArguments("lambda expression", "System.Func<string>").WithLocation(10, 39));
        }

        [Fact]
        public void LambdaReturnType_07()
        {
            var source =
@"#nullable enable
using System;
struct S<T> { }
class Program
{
    static void Main()
    {
        Delegate d1 = string? () => default;
        Delegate d2 = string () => default;
        Delegate d3 = S<object?> () => default(S<object?>);
        Delegate d4 = S<object?> () => default(S<object>);
        Delegate d5 = S<object> () => default(S<object?>);
        Delegate d6 = S<object> () => default(S<object>);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,36): warning CS8603: Possible null reference return.
                //         Delegate d2 = string () => default;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "default").WithLocation(9, 36),
                // (11,40): warning CS8619: Nullability of reference types in value of type 'S<object>' doesn't match target type 'S<object?>'.
                //         Delegate d4 = S<object?> () => default(S<object>);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "default(S<object>)").WithArguments("S<object>", "S<object?>").WithLocation(11, 40),
                // (12,39): warning CS8619: Nullability of reference types in value of type 'S<object?>' doesn't match target type 'S<object>'.
                //         Delegate d5 = S<object> () => default(S<object?>);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInAssignment, "default(S<object?>)").WithArguments("S<object?>", "S<object>").WithLocation(12, 39));
        }

        [Fact]
        public void LambdaReturnType_08()
        {
            var source =
@"#nullable enable
using System;
struct S<T> { }
class Program
{
    static void Main()
    {
        Func<string?> f1 = string? () => throw null!;
        Func<string?> f2 = string () => throw null!;
        Func<string> f3 = string? () => throw null!;
        Func<string> f4 = string () => throw null!;
        Func<S<object?>> f5 = S<object?> () => throw null!;
        Func<S<object?>> f6 = S<object> () => throw null!;
        Func<S<object>> f7 = S<object?> () => throw null!;
        Func<S<object>> f8 = S<object> () => throw null!;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (9,28): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string?>' (possibly because of nullability attributes).
                //         Func<string?> f2 = string () => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string () =>").WithArguments("lambda expression", "System.Func<string?>").WithLocation(9, 28),
                // (10,27): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string>' (possibly because of nullability attributes).
                //         Func<string> f3 = string? () => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string? () =>").WithArguments("lambda expression", "System.Func<string>").WithLocation(10, 27),
                // (13,31): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<S<object?>>' (possibly because of nullability attributes).
                //         Func<S<object?>> f6 = S<object> () => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "S<object> () =>").WithArguments("lambda expression", "System.Func<S<object?>>").WithLocation(13, 31),
                // (14,30): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<S<object>>' (possibly because of nullability attributes).
                //         Func<S<object>> f7 = S<object?> () => throw null!;
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "S<object?> () =>").WithArguments("lambda expression", "System.Func<S<object>>").WithLocation(14, 30));
        }

        [Fact]
        public void LambdaReturnType_09()
        {
            var source =
@"#nullable enable
struct S<T> { }
delegate ref T D1<T>();
delegate ref readonly T D2<T>();
class Program
{
    static void Main()
    {
        D1<S<object?>> f1 = (ref S<object?> () => throw null!);
        D1<S<object?>> f2 = (ref S<object> () => throw null!);
        D1<S<object>> f3 = (ref S<object?> () => throw null!);
        D1<S<object>> f4 = (ref S<object> () => throw null!);
        D2<S<object?>> f5 = (ref readonly S<object?> () => throw null!);
        D2<S<object?>> f6 = (ref readonly S<object> () => throw null!);
        D2<S<object>> f7 = (ref readonly S<object?> () => throw null!);
        D2<S<object>> f8 = (ref readonly S<object> () => throw null!);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,30): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D1<S<object?>>' (possibly because of nullability attributes).
                //         D1<S<object?>> f2 = (ref S<object> () => throw null!);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "ref S<object> () =>").WithArguments("lambda expression", "D1<S<object?>>").WithLocation(10, 30),
                // (11,29): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D1<S<object>>' (possibly because of nullability attributes).
                //         D1<S<object>> f3 = (ref S<object?> () => throw null!);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "ref S<object?> () =>").WithArguments("lambda expression", "D1<S<object>>").WithLocation(11, 29),
                // (14,30): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2<S<object?>>' (possibly because of nullability attributes).
                //         D2<S<object?>> f6 = (ref readonly S<object> () => throw null!);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "ref readonly S<object> () =>").WithArguments("lambda expression", "D2<S<object?>>").WithLocation(14, 30),
                // (15,29): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'D2<S<object>>' (possibly because of nullability attributes).
                //         D2<S<object>> f7 = (ref readonly S<object?> () => throw null!);
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "ref readonly S<object?> () =>").WithArguments("lambda expression", "D2<S<object>>").WithLocation(15, 29));
        }

        [Fact]
        public void LambdaReturnType_10()
        {
            var source =
@"delegate T D1<T>(ref T t);
delegate ref T D2<T>(ref T t);
delegate ref readonly T D3<T>(ref T t);
class Program
{
    static void F<T>()
    {
        D1<T> d1;
        D2<T> d2;
        D3<T> d3;
        d1 = T (ref T t) => t;
        d2 = T (ref T t) => t;
        d3 = T (ref T t) => t;
        d1 = (ref T (ref T t) => ref t);
        d2 = (ref T (ref T t) => ref t);
        d3 = (ref T (ref T t) => ref t);
        d1 = (ref readonly T (ref T t) => ref t);
        d2 = (ref readonly T (ref T t) => ref t);
        d3 = (ref readonly T (ref T t) => ref t);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (12,26): error CS8934: Cannot convert lambda expression to type 'D2<T>' because the return type does not match the delegate return type
                //         d2 = T (ref T t) => t;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D2<T>").WithLocation(12, 26),
                // (13,26): error CS8934: Cannot convert lambda expression to type 'D3<T>' because the return type does not match the delegate return type
                //         d3 = T (ref T t) => t;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D3<T>").WithLocation(13, 26),
                // (14,31): error CS8934: Cannot convert lambda expression to type 'D1<T>' because the return type does not match the delegate return type
                //         d1 = (ref T (ref T t) => ref t);
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D1<T>").WithLocation(14, 31),
                // (16,31): error CS8934: Cannot convert lambda expression to type 'D3<T>' because the return type does not match the delegate return type
                //         d3 = (ref T (ref T t) => ref t);
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D3<T>").WithLocation(16, 31),
                // (17,40): error CS8934: Cannot convert lambda expression to type 'D1<T>' because the return type does not match the delegate return type
                //         d1 = (ref readonly T (ref T t) => ref t);
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D1<T>").WithLocation(17, 40),
                // (18,40): error CS8934: Cannot convert lambda expression to type 'D2<T>' because the return type does not match the delegate return type
                //         d2 = (ref readonly T (ref T t) => ref t);
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "D2<T>").WithLocation(18, 40));
        }

        [Fact]
        public void LambdaReturnType_11()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d;
        d = (ref void () => { });
        d = (ref readonly void () => { });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,18): error CS1547: Keyword 'void' cannot be used in this context
                //         d = (ref void () => { });
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(7, 18),
                // (7,26): error CS8917: The delegate type could not be inferred.
                //         d = (ref void () => { });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(7, 26),
                // (8,27): error CS1547: Keyword 'void' cannot be used in this context
                //         d = (ref readonly void () => { });
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(8, 27),
                // (8,35): error CS8917: The delegate type could not be inferred.
                //         d = (ref readonly void () => { });
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(8, 35));
        }

        [WorkItem(55217, "https://github.com/dotnet/roslyn/issues/55217")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void LambdaReturnType_12()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d;
        d = TypedReference () => throw null;
        d = RuntimeArgumentHandle () => throw null;
        d = ArgIterator () => throw null;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,13): error CS1599: The return type of a method, delegate, or function pointer cannot be 'TypedReference'
                //         d = TypedReference () => throw null;
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "TypedReference").WithArguments("System.TypedReference").WithLocation(7, 13),
                // (8,13): error CS1599: The return type of a method, delegate, or function pointer cannot be 'RuntimeArgumentHandle'
                //         d = RuntimeArgumentHandle () => throw null;
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle").WithLocation(8, 13),
                // (9,13): error CS1599: The return type of a method, delegate, or function pointer cannot be 'ArgIterator'
                //         d = ArgIterator () => throw null;
                Diagnostic(ErrorCode.ERR_MethodReturnCantBeRefAny, "ArgIterator").WithArguments("System.ArgIterator").WithLocation(9, 13));
        }

        [Fact]
        public void LambdaReturnType_13()
        {
            var source =
@"static class S { }
delegate S D();
class Program
{
    static void Main()
    {
        D d = S () => default;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,15): error CS0722: 'S': static types cannot be used as return types
                //         D d = S () => default;
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "S").WithArguments("S").WithLocation(7, 15));
        }

        [Fact]
        public void LambdaReturnType_14()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d = async int () => 0;
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (6,35): error CS1983: The return type of an async method must be void, Task, Task<T>, a task-like type, IAsyncEnumerable<T>, or IAsyncEnumerator<T>
                //         Delegate d = async int () => 0;
                Diagnostic(ErrorCode.ERR_BadAsyncReturn, "=>").WithLocation(6, 35),
                // (6,35): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Delegate d = async int () => 0;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 35));
        }

        [Fact]
        public void LambdaReturnType_15()
        {
            var source =
@"using System;
using System.Threading.Tasks;
delegate ref Task D(string s);
class Program
{
    static void Main()
    {
        Delegate d1 = async ref Task (s) => { _ = s.Length; await Task.Yield(); };
        D d2 = async ref Task (s) => { _ = s.Length; await Task.Yield(); };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,29): error CS1073: Unexpected token 'ref'
                //         Delegate d1 = async ref Task (s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(8, 29),
                // (8,42): error CS8917: The delegate type could not be inferred.
                //         Delegate d1 = async ref Task (s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "=>").WithLocation(8, 42),
                // (9,22): error CS1073: Unexpected token 'ref'
                //         D d2 = async ref Task (s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 22));
        }

        [Fact]
        public void LambdaReturnType_16()
        {
            var source =
@"using System;
using System.Threading.Tasks;
delegate ref Task D(string s);
class Program
{
    static void Main()
    {
        Delegate d1 = async ref Task (string s) => { _ = s.Length; await Task.Yield(); };
        D d2 = async ref Task (string s) => { _ = s.Length; await Task.Yield(); };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,29): error CS1073: Unexpected token 'ref'
                //         Delegate d1 = async ref Task (string s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(8, 29),
                // (9,22): error CS1073: Unexpected token 'ref'
                //         D d2 = async ref Task (string s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "ref").WithArguments("ref").WithLocation(9, 22));
        }

        [Fact]
        public void LambdaReturnType_17()
        {
            var source =
@"#nullable enable
using System;
class Program
{
    static void F(string? x, string y)
    {
        Func<string?> f1 = string () => { if (x is null) return x; return y; };
        Func<string> f2 = string? () => { if (x is not null) return x; return y; };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,28): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string?>' (possibly because of nullability attributes).
                //         Func<string?> f1 = string () => { if (x is null) return x; return y; };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string () =>").WithArguments("lambda expression", "System.Func<string?>").WithLocation(7, 28),
                // (7,65): warning CS8603: Possible null reference return.
                //         Func<string?> f1 = string () => { if (x is null) return x; return y; };
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "x").WithLocation(7, 65),
                // (8,27): warning CS8621: Nullability of reference types in return type of 'lambda expression' doesn't match the target delegate 'Func<string>' (possibly because of nullability attributes).
                //         Func<string> f2 = string? () => { if (x is not null) return x; return y; };
                Diagnostic(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate, "string? () =>").WithArguments("lambda expression", "System.Func<string>").WithLocation(8, 27));
        }

        [Fact]
        public void LambdaReturnType_18()
        {
            var source =
@"using System;
class Program
{
    static void F<T, U, V>(T t, U u, V v) where U : T
    {
        Func<T> f1 = T () => u;
        Func<T> f2 = T () => v;
        Func<U> f3 = U () => t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,30): error CS0029: Cannot implicitly convert type 'V' to 'T'
                //         Func<T> f2 = T () => v;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "T").WithLocation(7, 30),
                // (7,30): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<T> f2 = T () => v;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "v").WithArguments("lambda expression").WithLocation(7, 30),
                // (8,30): error CS0266: Cannot implicitly convert type 'T' to 'U'. An explicit conversion exists (are you missing a cast?)
                //         Func<U> f3 = U () => t;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "U").WithLocation(8, 30),
                // (8,30): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<U> f3 = U () => t;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "t").WithArguments("lambda expression").WithLocation(8, 30));
        }

        [Fact]
        public void LambdaReturnType_19()
        {
            var source =
@"using System;
class Program
{
    static void F<T, U, V>(T t, U u, V v) where U : T
    {
        Delegate d;
        d = T () => u;
        d = T () => v;
        d = U () => t;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (8,21): error CS0029: Cannot implicitly convert type 'V' to 'T'
                //         d = T () => v;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "T").WithLocation(8, 21),
                // (8,21): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         d = T () => v;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "v").WithArguments("lambda expression").WithLocation(8, 21),
                // (9,21): error CS0266: Cannot implicitly convert type 'T' to 'U'. An explicit conversion exists (are you missing a cast?)
                //         d = U () => t;
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "U").WithLocation(9, 21),
                // (9,21): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         d = U () => t;
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "t").WithArguments("lambda expression").WithLocation(9, 21));
        }

        [Fact]
        public void LambdaReturnType_20()
        {
            var source =
@"using System;
class Program
{
    static void F<T, U, V>(T t, U u, V v) where U : T
    {
        Func<T> f1 = T () => { if (t is null) return t; return u; };
        Func<U> f2 = U () => { if (t is null) return t; return u; };
        Func<T> f3 = T () => { if (t is null) return t; return v; };
        Func<V> f4 = V () => { if (t is null) return t; return v; };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,54): error CS0266: Cannot implicitly convert type 'T' to 'U'. An explicit conversion exists (are you missing a cast?)
                //         Func<U> f2 = U () => { if (t is null) return t; return u; };
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "t").WithArguments("T", "U").WithLocation(7, 54),
                // (7,54): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<U> f2 = U () => { if (t is null) return t; return u; };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "t").WithArguments("lambda expression").WithLocation(7, 54),
                // (8,64): error CS0029: Cannot implicitly convert type 'V' to 'T'
                //         Func<T> f3 = T () => { if (t is null) return t; return v; };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "v").WithArguments("V", "T").WithLocation(8, 64),
                // (8,64): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<T> f3 = T () => { if (t is null) return t; return v; };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "v").WithArguments("lambda expression").WithLocation(8, 64),
                // (9,54): error CS0029: Cannot implicitly convert type 'T' to 'V'
                //         Func<V> f4 = V () => { if (t is null) return t; return v; };
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "t").WithArguments("T", "V").WithLocation(9, 54),
                // (9,54): error CS1662: Cannot convert lambda expression to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type
                //         Func<V> f4 = V () => { if (t is null) return t; return v; };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturns, "t").WithArguments("lambda expression").WithLocation(9, 54));
        }

        [Fact]
        public void LambdaReturnType_SemanticModel()
        {
            var source =
@"class Program
{
    static void F<T>()
    {
        var x = T () => default;
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambdaSyntax = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Single();

            var expectedType = comp.GetMember<MethodSymbol>("Program.F").TypeParameters.Single().GetPublicSymbol();
            Assert.Equal(TypeKind.TypeParameter, expectedType.TypeKind);
            Assert.Equal("T", expectedType.ToTestDisplayString());

            var method = (IMethodSymbol)model.GetSymbolInfo(lambdaSyntax).Symbol;
            Assert.Equal(MethodKind.LambdaMethod, method.MethodKind);

            var returnTypeSyntax = lambdaSyntax.ReturnType;
            var typeInfo = model.GetTypeInfo(returnTypeSyntax);
            Assert.Equal(expectedType, typeInfo.Type);
            Assert.Equal(expectedType, typeInfo.ConvertedType);

            var symbolInfo = model.GetSymbolInfo(returnTypeSyntax);
            Assert.Equal(expectedType, symbolInfo.Symbol);
        }

        [Fact]
        public void LambdaReturnType_CustomModifiers_01()
        {
            var sourceA =
@".class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed { }
    .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Int16) Invoke () runtime managed { }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed { }
    .method public hidebysig newslot virtual instance int32 modopt([mscorlib]System.Int16) EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed { }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class Program
{
    static void F(D d)
    {
        System.Console.WriteLine(d());
    }
    static void Main()
    {
        F(() => 1);
        F(int () => 2);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview, expectedOutput:
@"1
2");
        }

        [Fact]
        public void LambdaReturnType_CustomModifiers_02()
        {
            var sourceA =
@".class public auto ansi sealed D extends [mscorlib]System.MulticastDelegate
{
    .method public hidebysig specialname rtspecialname instance void .ctor (object 'object', native int 'method') runtime managed { }
    .method public hidebysig newslot virtual instance int32 modreq([mscorlib]System.Int16) Invoke () runtime managed { }
    .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke (class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed { }
    .method public hidebysig newslot virtual instance int32 modreq([mscorlib]System.Int16) EndInvoke (class [mscorlib]System.IAsyncResult result) runtime managed { }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"class Program
{
    static void F(D d)
    {
        System.Console.WriteLine(d());
    }
    static void Main()
    {
        F(() => 1);
        F(int () => 2);
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (5,34): error CS0570: 'D.Invoke()' is not supported by the language
                //         System.Console.WriteLine(d());
                Diagnostic(ErrorCode.ERR_BindToBogus, "d()").WithArguments("D.Invoke()").WithLocation(5, 34),
                // (9,14): error CS0570: 'D.Invoke()' is not supported by the language
                //         F(() => 1);
                Diagnostic(ErrorCode.ERR_BindToBogus, "=>").WithArguments("D.Invoke()").WithLocation(9, 14),
                // (10,18): error CS0570: 'D.Invoke()' is not supported by the language
                //         F(int () => 2);
                Diagnostic(ErrorCode.ERR_BindToBogus, "=>").WithArguments("D.Invoke()").WithLocation(10, 18));
        }

        [Fact]
        public void LambdaReturnType_UseSiteErrors()
        {
            var sourceA =
@".class public sealed A extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RequiredAttributeAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 FF 00 00 ) 
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed { ret }
}";
            var refA = CompileIL(sourceA);

            var sourceB =
@"using System;
class B
{
    static void F<T>(Func<T> f) { }
    static void Main()
    {
        F(A () => default);
    }
}";
            var comp = CreateCompilation(sourceB, references: new[] { refA }, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (7,11): error CS0648: 'A' is a type not supported by the language
                //         F(A () => default);
                Diagnostic(ErrorCode.ERR_BogusType, "A").WithArguments("A").WithLocation(7, 11));
        }

        [Fact]
        public void VarReturnType_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Delegate d;
        d = var () => throw null;
        d = ref var () => throw null;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,13): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         d = var () => throw null;
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(7, 13),
                // (7,13): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         d = var () => throw null;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(7, 13),
                // (8,17): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         d = ref var () => throw null;
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(8, 17),
                // (8,17): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
                //         d = ref var () => throw null;
                Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(8, 17));
        }

        [Fact]
        public void VarReturnType_02()
        {
            var source =
@"using System;
class var { }
class Program
{
    static void Main()
    {
        Delegate d;
        d = var () => default;
        d = ref var (ref var v) => ref v;
        d = @var () => default;
        d = ref @var (ref var v) => ref v;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (2,7): warning CS8981: The type name 'var' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class var { }
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "var").WithArguments("var").WithLocation(2, 7),
                // (8,13): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         d = var () => default;
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(8, 13),
                // (9,17): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         d = ref var (ref var v) => ref v;
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(9, 17));
        }

        [Fact]
        public void VarReturnType_03()
        {
            var source =
@"using System;
class @var { }
class Program
{
    static void Main()
    {
        F(var () => default);
        F(ref var (ref var v) => ref v);
        F(@var () => default);
        F(ref @var (ref var v) => ref v);
        F(() => default(var));
    }
    static void F(Delegate d) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,11): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(var () => default);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(7, 11),
                // (8,15): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(ref var (ref var v) => ref v);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(8, 15));
        }

        [Fact]
        public void VarReturnType_04()
        {
            var source =
@"using System;
struct @var
{
    internal class @other { }
    internal other o;
}
class Program
{
    static void Main()
    {
        F(var () => default);
        F(ref var () => throw null);
        F(var[] () => default);
        F(var? (var v) => v);
        F(var.other (var v) => v.o);
    }
    static void F(Delegate d) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (11,11): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(var () => default);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(11, 11),
                // (12,15): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(ref var () => throw null);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(12, 15));
        }

        [Fact]
        public void VarReturnType_05()
        {
            var source =
@"using System;
using @var = System.Int32;
class Program
{
    static void Main()
    {
        F(var (var v) => v);
        F(@var (var v) => v);
        F(() => default(var));
    }
    static void F(Delegate d) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,11): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(var (var v) => v);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(7, 11));
        }

        [Fact]
        public void VarReturnType_06()
        {
            var source =
@"using System;
class Program
{
    static void M<@var>()
    {
        F(var (var v) => v);
        F(@var (var v) => v);
        F(() => default(var));
    }
    static void F(Delegate d) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,11): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(var () => default);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(6, 11));
        }

        [Fact]
        public void VarReturnType_07()
        {
            var source =
@"using System;
static class @var { }
class Program
{
    static void Main()
    {
        F(var () => default);
    }
    static void F(Delegate d) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,11): error CS8975: The contextual keyword 'var' cannot be used as an explicit lambda return type
                //         F(var () => default);
                Diagnostic(ErrorCode.ERR_LambdaExplicitReturnTypeVar, "var").WithLocation(7, 11),
                // (7,11): error CS0722: 'var': static types cannot be used as return types
                //         F(var () => default);
                Diagnostic(ErrorCode.ERR_ReturnTypeIsStaticClass, "var").WithArguments("var").WithLocation(7, 11));
        }

        [Fact]
        public void AsyncLambdaParameters_01()
        {
            var source =
@"using System;
using System.Threading.Tasks;
delegate Task D(ref string s);
class Program
{
    static void Main()
    {
        Delegate d1 = async (ref string s) => { _ = s.Length; await Task.Yield(); };
        D d2 = async (ref string s) => { _ = s.Length; await Task.Yield(); };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (8,41): error CS1988: Async methods cannot have ref, in or out parameters
                //         Delegate d1 = async (ref string s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "s").WithLocation(8, 41),
                // (9,34): error CS1988: Async methods cannot have ref, in or out parameters
                //         D d2 = async (ref string s) => { _ = s.Length; await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_BadAsyncArgType, "s").WithLocation(9, 34));
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void AsyncLambdaParameters_02()
        {
            var source =
@"using System;
using System.Threading.Tasks;
delegate void D1(TypedReference r);
delegate void D2(RuntimeArgumentHandle h);
delegate void D3(ArgIterator i);
class Program
{
    static void Main()
    {
        D1 d1 = async (TypedReference r) => { await Task.Yield(); };
        D2 d2 = async (RuntimeArgumentHandle h) => { await Task.Yield(); };
        D3 d3 = async (ArgIterator i) => { await Task.Yield(); };
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (10,39): error CS4012: Parameters of type 'TypedReference' cannot be declared in async methods or async lambda expressions.
                //         D1 d1 = async (TypedReference r) => { await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_BadSpecialByRefParameter, "r").WithArguments("System.TypedReference").WithLocation(10, 39),
                // (11,46): error CS4012: Parameters of type 'RuntimeArgumentHandle' cannot be declared in async methods or async lambda expressions.
                //         D2 d2 = async (RuntimeArgumentHandle h) => { await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_BadSpecialByRefParameter, "h").WithArguments("System.RuntimeArgumentHandle").WithLocation(11, 46),
                // (12,36): error CS4012: Parameters of type 'ArgIterator' cannot be declared in async methods or async lambda expressions.
                //         D3 d3 = async (ArgIterator i) => { await Task.Yield(); };
                Diagnostic(ErrorCode.ERR_BadSpecialByRefParameter, "i").WithArguments("System.ArgIterator").WithLocation(12, 36));
        }

        [Fact]
        public void BestType_01()
        {
            var source =
@"using System;
class A { }
class B1 : A { }
class B2 : A { }
interface I { }
class C1 : I { }
class C2 : I { }
class Program
{
    static void F<T>(Func<bool, T> f) { }
    static void Main()
    {
        F((bool b) => { if (b) return new B1(); return new B2(); });
        F((bool b) => { if (b) return new C1(); return new C2(); });
    }
}";

            var expectedDiagnostics = new[]
            {
                // (13,9): error CS0411: The type arguments for method 'Program.F<T>(Func<bool, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F((bool b) => { if (b) return new B1(); return new B2(); });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<bool, T>)").WithLocation(13, 9),
                // (14,9): error CS0411: The type arguments for method 'Program.F<T>(Func<bool, T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F((bool b) => { if (b) return new C1(); return new C2(); });
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<bool, T>)").WithLocation(14, 9)
            };

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(expectedDiagnostics);

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(expectedDiagnostics);
        }

        // As above but with explicit return type.
        [Fact]
        public void BestType_02()
        {
            var source =
@"using System;
class A { }
class B1 : A { }
class B2 : A { }
interface I { }
class C1 : I { }
class C2 : I { }
class Program
{
    static void F<T>(Func<bool, T> f) { Console.WriteLine(typeof(T)); }
    static void Main()
    {
        F(A (bool b) => { if (b) return new B1(); return new B2(); });
        F(I (bool b) => { if (b) return new C1(); return new C2(); });
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (13,11): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(A (bool b) => { if (b) return new B1(); return new B2(); });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "A").WithArguments("lambda return type", "10.0").WithLocation(13, 11),
                // (14,11): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(I (bool b) => { if (b) return new C1(); return new C2(); });
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "I").WithArguments("lambda return type", "10.0").WithLocation(14, 11));

            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput:
@"A
I");
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void BestType_03()
        {
            var source =
@"using System;
class A { }
class B1 : A { }
class B2 : A { }
class Program
{
    static void F<T>(Func<T> x, Func<T> y) { }
    static void Main()
    {
        F(B2 () => null, B2 () => null);
        F(A () => null, B2 () => null);
        F(B1 () => null, B2 () => null);
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (10,11): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(B2 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "B2").WithArguments("lambda return type", "10.0").WithLocation(10, 11),
                // (10,26): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(B2 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "B2").WithArguments("lambda return type", "10.0").WithLocation(10, 26),
                // (11,9): error CS0411: The type arguments for method 'Program.F<T>(Func<T>, Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(A () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T>, System.Func<T>)").WithLocation(11, 9),
                // (11,11): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(A () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "A").WithArguments("lambda return type", "10.0").WithLocation(11, 11),
                // (11,25): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(A () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "B2").WithArguments("lambda return type", "10.0").WithLocation(11, 25),
                // (12,9): error CS0411: The type arguments for method 'Program.F<T>(Func<T>, Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(B1 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T>, System.Func<T>)").WithLocation(12, 9),
                // (12,11): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(B1 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "B1").WithArguments("lambda return type", "10.0").WithLocation(12, 11),
                // (12,26): error CS8773: Feature 'lambda return type' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         F(B1 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "B2").WithArguments("lambda return type", "10.0").WithLocation(12, 26));

            comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics(
                // (11,9): error CS0411: The type arguments for method 'Program.F<T>(Func<T>, Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(A () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T>, System.Func<T>)").WithLocation(11, 9),
                // (12,9): error CS0411: The type arguments for method 'Program.F<T>(Func<T>, Func<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //         F(B1 () => null, B2 () => null);
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "F").WithArguments("Program.F<T>(System.Func<T>, System.Func<T>)").WithLocation(12, 9));
        }

        [Fact]
        public void TypeInference_01()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(Func<object, T> f)
    {
        Console.WriteLine(typeof(T));
    }
    static void Main()
    {
        F(long (o) => 1);
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: @"System.Int64");
        }

        [WorkItem(54257, "https://github.com/dotnet/roslyn/issues/54257")]
        [Fact]
        public void TypeInference_02()
        {
            var source =
@"using System;
class Program
{
    static void F<T>(Func<T, T> f)
    {
        Console.WriteLine(typeof(T));
    }
    static void Main()
    {
        F(int (i) => i);
    }
}";
            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
        }

        // CS4031 is not reported for async lambda in [SecurityCritical] type.
        [Fact]
        [WorkItem(54074, "https://github.com/dotnet/roslyn/issues/54074")]
        public void SecurityCritical_AsyncLambda()
        {
            var source =
@"using System;
using System.Security;
using System.Threading.Tasks;
[SecurityCritical]
class Program
{
    static void Main()
    {
        Func<Task> f = async () => await Task.Yield();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        // CS4031 is not reported for async lambda in [SecurityCritical] type.
        [Fact]
        [WorkItem(54074, "https://github.com/dotnet/roslyn/issues/54074")]
        public void SecurityCritical_AsyncLambda_AttributeArgument()
        {
            var source =
@"using System;
using System.Security;
using System.Threading.Tasks;
class A : Attribute
{
    internal A(int i) { }
}
[SecurityCritical]
[A(F(async () => await Task.Yield()))]
class Program
{
    internal static int F(Func<Task> f) => 0;
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [A(F(async () => await Task.Yield()))]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "F(async () => await Task.Yield())").WithLocation(9, 4));
        }

        private static LambdaSymbol GetLambdaSymbol(SemanticModel model, LambdaExpressionSyntax syntax)
        {
            return model.GetSymbolInfo(syntax).Symbol.GetSymbol<LambdaSymbol>();
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_01()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [A] (x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [A] (x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[A] (x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_02()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [A][A] (x) => x;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [A][A] (x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[A][A] (x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_03()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = ([A] x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,37): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = ([A] x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "x").WithLocation(5, 37)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_04()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = ([A][A] x) => x;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,40): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = ([A][A] x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "x").WithLocation(5, 40)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_05()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int, int>> e = ([A] x, [A] y) => x + y;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,42): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int, int>> e = ([A] x, [A] y) => x + y;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "x").WithLocation(5, 42)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_06()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [return: A] (x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [return: A] (x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[return: A] (x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_07()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [return: A][return: A] (x) => x;

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [return: A][return: A] (x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[return: A][return: A] (x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_08()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [A][return: A] (x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [A][return: A] (x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[A][return: A] (x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_09()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [A] ([A] x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [A] ([A] x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[A] ([A] x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(53910, "https://github.com/dotnet/roslyn/issues/53910")]
        public void WithAttributesToExpressionTree_10()
        {
            var source =
@"
using System;
using System.Linq.Expressions;

Expression<Func<int, int>> e = [return: A] ([A] x) => x;

class A : Attribute { }
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS8972: A lambda expression with attributes cannot be converted to an expression tree
                // Expression<Func<int, int>> e = [return: A] ([A] x) => x;
                Diagnostic(ErrorCode.ERR_LambdaWithAttributesToExpressionTree, "[return: A] ([A] x) => x").WithLocation(5, 32)
                );
        }

        [Fact]
        [WorkItem(60661, "https://github.com/dotnet/roslyn/issues/60661")]
        public void KeywordParameterName_01()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Action<int> a = int => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,25): error CS1041: Identifier expected; 'int' is a keyword
                //         Action<int> a = int => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "int").WithArguments("", "int").WithLocation(6, 25));
        }

        [Fact]
        [WorkItem(60661, "https://github.com/dotnet/roslyn/issues/60661")]
        public void KeywordParameterName_02()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Action<int> a = ref => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,25): error CS1041: Identifier expected; 'ref' is a keyword
                //         Action<int> a = ref => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(6, 25));
        }

        [Fact]
        [WorkItem(60661, "https://github.com/dotnet/roslyn/issues/60661")]
        public void KeywordParameterName_03()
        {
            var source =
@"using System;
class Program
{
    static void Main()
    {
        Action<int> a = ref int => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,21): error CS8171: Cannot initialize a by-value variable with a reference
                //         Action<int> a = ref int => { };
                Diagnostic(ErrorCode.ERR_InitializeByValueVariableWithReference, "a = ref int => { }").WithLocation(6, 21),
                // (6,29): error CS1041: Identifier expected; 'int' is a keyword
                //         Action<int> a = ref int => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "int").WithArguments("", "int").WithLocation(6, 29));
        }

        /// <summary>
        /// Look for usages of "parameter" and verify the index-th one.
        /// </summary>
        private void VerifyParameter(CSharpCompilation comp, int index)
        {
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var parameterUsages = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(i => i.Identifier.ValueText == "parameter")
                .Where(i => i.Ancestors().Any(a => a.IsKind(SyntaxKind.Attribute) || a.IsKind(SyntaxKind.DefaultExpression) || a.IsKind(SyntaxKind.InvocationExpression)))
                .ToArray();

            var parameterUsage = parameterUsages[index];

            Assert.Null(model.GetSymbolInfo(parameterUsage).Symbol);
            Assert.True(model.GetTypeInfo(parameterUsage).Type.IsErrorType());
            Assert.DoesNotContain("parameter", model.LookupSymbols(parameterUsage.Position).ToTestDisplayStrings());
        }

        [Fact]
        public void ParameterScope_NotInMethodAttributeTypeOf()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {

        var _ =
            [My(typeof(parameter))] // 1
            void(int parameter) => { };
    }

    [My(typeof(parameter))] // 2
    void M2(int parameter) { }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string name1) { }
}
");
            comp.VerifyDiagnostics(
                // (8,24): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //             [My(typeof(parameter))] // 1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(8, 24),
                // (12,16): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     [My(typeof(parameter))] // 2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(12, 16)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_NotInMethodAttribute()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ =
            [My(parameter)] // 1
            void (int parameter) => { };
    }

    [My(parameter)] // 2
    void M2(int parameter) { }
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(object o) { }
}
");
            comp.VerifyDiagnostics(
                // (7,17): error CS0103: The name 'parameter' does not exist in the current context
                //             [My(parameter)] // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(7, 17),
                // (11,9): error CS0103: The name 'parameter' does not exist in the current context
                //     [My(parameter)] // 2
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(11, 9)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_NotInMethodAttributeTypeArgument()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ =
            [My<parameter>] // 1
            void (int parameter) => { };
    }

    [My<parameter>] // 2
    void M2(int parameter) { }
}

public class MyAttribute<T> : System.Attribute
{
}
");
            comp.VerifyDiagnostics(
                // (7,17): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //             [My<parameter>] // 1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(7, 17),
                // (11,9): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     [My<parameter>] // 2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(11, 9)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_NotAsMethodAttributeType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ =
            [parameter] // 1
            void (System.Attribute parameter) => { };
    }

    [parameter] // 2
    void M2(System.Attribute parameter) { }
}
");
            comp.VerifyDiagnostics(
                // (7,14): error CS0246: The type or namespace name 'parameterAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //             [parameter] // 1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameterAttribute").WithLocation(7, 14),
                // (7,14): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //             [parameter] // 1
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(7, 14),
                // (11,6): error CS0246: The type or namespace name 'parameterAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     [parameter] // 2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameterAttribute").WithLocation(11, 6),
                // (11,6): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     [parameter] // 2
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(11, 6)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_NotInParameterAttribute()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ = void ([My(parameter)] int parameter) => throw null;
    }

    void M2([My(parameter)] int parameter) => throw null;
}

public class MyAttribute : System.Attribute
{
    public MyAttribute(string name1) { }
}
");
            comp.VerifyDiagnostics(
                // (6,27): error CS0103: The name 'parameter' does not exist in the current context
                //         var _ = void ([My(parameter)] int parameter) => throw null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(6, 27),
                // (9,17): error CS0103: The name 'parameter' does not exist in the current context
                //     void M2([My(parameter)] int parameter) => throw null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(9, 17)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_InParameterDefaultValueNameOf()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ = void (string parameter = nameof(parameter)) => throw null;
    }

    void M2(string parameter = nameof(parameter)) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (6,49): error CS0103: The name 'parameter' does not exist in the current context
                //         var _ = void (string parameter = nameof(parameter)) => throw null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(6, 49),
                // (9,39): error CS0103: The name 'parameter' does not exist in the current context
                //     void M2(string parameter = nameof(parameter)) => throw null;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "parameter").WithArguments("parameter").WithLocation(9, 39));
        }

        [Fact]
        public void ParameterScope_NotAsParameterAttributeType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ = void ([parameter] System.Attribute parameter) => throw null;
    }

    void M2([parameter] System.Attribute parameter) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (6,24): error CS0246: The type or namespace name 'parameterAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //         var _ = void ([parameter] System.Attribute parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameterAttribute").WithLocation(6, 24),
                // (6,24): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //         var _ = void ([parameter] System.Attribute parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(6, 24),
                // (9,14): error CS0246: The type or namespace name 'parameterAttribute' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2([parameter] System.Attribute parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameterAttribute").WithLocation(9, 14),
                // (9,14): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2([parameter] System.Attribute parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(9, 14)
                );

            VerifyParameter(comp, 0);
            VerifyParameter(comp, 1);
        }

        [Fact]
        public void ParameterScope_NotInReturnType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ = parameter (int parameter) => throw null;
    }

    parameter M2(int parameter) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (6,17): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //         var _ = parameter (int parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(6, 17),
                // (9,5): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     parameter M2(int parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(9, 5)
                );
        }

        [Fact]
        public void ParameterScope_NotInParameterType()
        {
            var comp = CreateCompilation(@"
class C
{
    void M()
    {
        var _ = void (parameter parameter) => throw null;
    }

    void M2(parameter parameter) => throw null;
}
");
            comp.VerifyDiagnostics(
                // (6,23): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //         var _ = void (parameter parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(6, 23),
                // (9,13): error CS0246: The type or namespace name 'parameter' could not be found (are you missing a using directive or an assembly reference?)
                //     void M2(parameter parameter) => throw null;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "parameter").WithArguments("parameter").WithLocation(9, 13)
                );
        }

        [Fact, WorkItem(61143, "https://github.com/dotnet/roslyn/issues/61143")]
        public void ParameterScope_LambdaDiscardParameter()
        {
            var comp = CreateCompilation(@"
class AAttribute : System.Attribute
{
    public AAttribute(string s) { }
}

class C
{
    void M(int _)
    {
        System.Func<string, string, int> a = [A(nameof(_))] (_, _) => 0;
    }
}
");
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var discard = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(i => i.Identifier.ValueText == "_")
                .Where(i => i.Ancestors().Any(a => a.IsKind(SyntaxKind.InvocationExpression)))
                .Single();

            Assert.Equal("System.Int32 _", model.GetSymbolInfo(discard).Symbol.ToTestDisplayString());
        }

        [Fact, WorkItem(61143, "https://github.com/dotnet/roslyn/issues/61143")]
        public void ParameterScope_LambdaUnderscoreParameter()
        {
            var comp = CreateCompilation(@"
class AAttribute : System.Attribute
{
    public AAttribute(string s) { }
}

class C
{
    void M(int _)
    {
        System.Func<string, string, int> a = [A(nameof(_))] (_, x) => 0;
    }
}
");
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var underscore = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(i => i.Identifier.ValueText == "_")
                .Where(i => i.Ancestors().Any(a => a.IsKind(SyntaxKind.InvocationExpression)))
                .Single();

            Assert.Equal("System.String _", model.GetSymbolInfo(underscore).Symbol.ToTestDisplayString());
        }

        [WorkItem(62085, "https://github.com/dotnet/roslyn/issues/62085")]
        [Fact]
        public void DuplicateRef()
        {
            var source =
@"delegate void D(ref int i);
class Program
{
    static void Main()
    {
        D d1 = (ref ref int i) => { };
        D d2 = (in ref int i) => { };
        D d3 = (out ref int i) => { };
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (6,21): error CS1107: A parameter can only have one 'ref' modifier
                //         D d1 = (ref ref int i) => { };
                Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(6, 21),
                // (7,16): warning CS9198: Reference kind modifier of parameter 'in int i' doesn't match the corresponding parameter 'ref int i' in target.
                //         D d2 = (in ref int i) => { };
                Diagnostic(ErrorCode.WRN_TargetDifferentRefness, "(in ref int i) => { }").WithArguments("in int i", "ref int i").WithLocation(7, 16),
                // (7,20): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                //         D d2 = (in ref int i) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(7, 20),
                // (8,16): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //         D d3 = (out ref int i) => { };
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "(out ref int i) => { }").WithArguments("i").WithLocation(8, 16),
                // (8,21): error CS8328:  The parameter modifier 'ref' cannot be used with 'out'
                //         D d3 = (out ref int i) => { };
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "out").WithLocation(8, 21),
                // (8,29): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //         D d3 = (out ref int i) => { };
                Diagnostic(ErrorCode.ERR_BadParamRef, "i").WithArguments("1", "ref").WithLocation(8, 29),
                // (8,32): error CS1661: Cannot convert lambda expression to type 'D' because the parameter types do not match the delegate parameter types
                //         D d3 = (out ref int i) => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethParams, "=>").WithArguments("lambda expression", "D").WithLocation(8, 32));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambdas = tree.GetRoot().DescendantNodes().OfType<ParenthesizedLambdaExpressionSyntax>().Select(e => model.GetSymbolInfo(e).Symbol.GetSymbol<LambdaSymbol>()).ToArray();

            Assert.Equal(RefKind.Ref, lambdas[0].Parameters[0].RefKind);
            Assert.Equal(RefKind.In, lambdas[1].Parameters[0].RefKind);
            Assert.Equal(RefKind.Out, lambdas[2].Parameters[0].RefKind);
        }

        [Fact]
        public void StaticPartialLambda()
        {
            CreateCompilation("""
                class C
                {
                    void M()
                    {
                        System.Action x = static partial () => { };
                    }
                }
                """).VerifyDiagnostics(
                // (5,34): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action x = static partial () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(5, 34),
                // (5,45): error CS8934: Cannot convert lambda expression to type 'Action' because the return type does not match the delegate return type
                //         System.Action x = static partial () => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Action").WithLocation(5, 45));
        }

        [Fact]
        public void PartialStaticLambda()
        {
            CreateCompilation("""
                class C
                {
                    void M()
                    {
                        System.Action x = partial static () => { };
                    }
                }
                """).VerifyDiagnostics(
                // (5,27): error CS0103: The name 'partial' does not exist in the current context
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "partial").WithArguments("partial").WithLocation(5, 27),
                // (5,35): error CS1002: ; expected
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "static").WithLocation(5, 35),
                // (5,35): error CS0106: The modifier 'static' is not valid for this item
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "static").WithArguments("static").WithLocation(5, 35),
                // (5,43): error CS8124: Tuple must contain at least two elements.
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(5, 43),
                // (5,45): error CS1001: Identifier expected
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "=>").WithLocation(5, 45),
                // (5,45): error CS1003: Syntax error, ',' expected
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_SyntaxError, "=>").WithArguments(",").WithLocation(5, 45),
                // (5,48): error CS1002: ; expected
                //         System.Action x = partial static () => { };
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(5, 48));
        }

        [Fact]
        public void PartialLambda()
        {
            CreateCompilation("""
                class C
                {
                    void M()
                    {
                        System.Action x = partial () => { };
                    }
                }
                """).VerifyDiagnostics(
                // (5,27): error CS0246: The type or namespace name 'partial' could not be found (are you missing a using directive or an assembly reference?)
                //         System.Action x = partial () => { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "partial").WithArguments("partial").WithLocation(5, 27),
                // (5,38): error CS8934: Cannot convert lambda expression to type 'Action' because the return type does not match the delegate return type
                //         System.Action x = partial () => { };
                Diagnostic(ErrorCode.ERR_CantConvAnonMethReturnType, "=>").WithArguments("lambda expression", "System.Action").WithLocation(5, 38));
        }

        [WorkItem(61013, "https://github.com/dotnet/roslyn/issues/61013")]
        [Fact]
        public void InvalidCast()
        {
            var source = """
                using System;
                #nullable enable
                internal class Program
                {
                    void Main(string[] args)
                    {
                        Choice(args.Length > 0
                            ? (Action)(() => DS1()
                            : () => DS2(args[0]));
                    }

                    void DS1()
                    { }

                    void DS2(string a)
                    { }

                    void Choice(Action a)
                    {
                        a();
                    }
                }
                """;

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular8);

            var syntaxTree = comp.SyntaxTrees[0];
            var action = syntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.ValueText == "Action");
            var model = comp.GetSemanticModel(syntaxTree);
            AssertEx.Equal("System.Action", model.GetTypeInfo(action).Type.ToTestDisplayString());
        }

        [Fact]
        [WorkItem(64392, "https://github.com/dotnet/roslyn/issues/64392")]
        public void ReferToFieldWithinLambdaInTypeAttribute_01()
        {
            var source = @"
[Display(x => $""{Name}"")]
public class Test
{
    [Display(Name = ""Name"")]
    public string Name { get; }
}

public class DisplayAttribute : System.Attribute
{
    public DisplayAttribute() { }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,2): error CS1729: 'DisplayAttribute' does not contain a constructor that takes 1 arguments
                // [Display(x => $"{Name}")]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, @"Display(x => $""{Name}"")").WithArguments("DisplayAttribute", "1").WithLocation(2, 2),
                // (5,14): error CS0246: The type or namespace name 'Name' could not be found (are you missing a using directive or an assembly reference?)
                //     [Display(Name = "Name")]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Name").WithArguments("Name").WithLocation(5, 14)
                );
        }

        [Fact]
        [WorkItem(64392, "https://github.com/dotnet/roslyn/issues/64392")]
        public void ReferToFieldWithinLambdaInTypeAttribute_02()
        {
            var source = @"
[Display(x => Name)]
public class Test
{
    [Display(Name = ""Name"")]
    public string Name { get; }
}

public class DisplayAttribute : System.Attribute
{
    public DisplayAttribute() { }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (2,2): error CS1729: 'DisplayAttribute' does not contain a constructor that takes 1 arguments
                // [Display(x => Name)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Display(x => Name)").WithArguments("DisplayAttribute", "1").WithLocation(2, 2),
                // (5,14): error CS0246: The type or namespace name 'Name' could not be found (are you missing a using directive or an assembly reference?)
                //     [Display(Name = "Name")]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Name").WithArguments("Name").WithLocation(5, 14)
                );
        }

        [Fact, WorkItem(64985, "https://github.com/dotnet/roslyn/issues/64985")]
        public void DelegateConversions_ImplicitlyTypedParameter_RefParameter()
        {
            var source = """
                struct R { }

                delegate R D1(ref R r);

                class Program
                {
                    static void Main()
                    {
                        D1 d1 = r1 => r1; // 1
                        M(r2 => r2); // 2
                    }

                    static void M(D1 d1) { }
                }
                """;

            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyDiagnostics(
                // (9,17): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //         D1 d1 = r1 => r1; // 1
                Diagnostic(ErrorCode.ERR_BadParamRef, "r1").WithArguments("1", "ref").WithLocation(9, 17),
                // (10,11): error CS1676: Parameter 1 must be declared with the 'ref' keyword
                //         M(r2 => r2); // 2
                Diagnostic(ErrorCode.ERR_BadParamRef, "r2").WithArguments("1", "ref").WithLocation(10, 11)
                );

            var syntaxTree = comp.SyntaxTrees[0];
            var root = syntaxTree.GetRoot();
            var model = comp.GetSemanticModel(syntaxTree);

            var lambdas = root.DescendantNodes().OfType<LambdaExpressionSyntax>().ToArray();

            Assert.Equal("r1 => r1", lambdas[0].ToString());
            var lambdaParameter1 = model.GetSymbolInfo(lambdas[0]).Symbol.GetParameters()[0];
            Assert.Equal("? r1", lambdaParameter1.ToTestDisplayString());
            Assert.Equal(RefKind.None, lambdaParameter1.RefKind);

            Assert.Equal("r2 => r2", lambdas[1].ToString());
            var lambdaParameter2 = model.GetSymbolInfo(lambdas[1]).Symbol.GetParameters()[0];
            Assert.Equal("R r2", lambdaParameter2.ToTestDisplayString());
            Assert.Equal(RefKind.None, lambdaParameter2.RefKind);
        }

        [Fact]
        public void LambdaWithExplicitDefaultParam()
        {
            var source =
@"class Program 
{
    public static void Main(string[] args)
    {
        var lam1 = (int x = 7) => x;
        lam1();
    }
}
";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,27): error CS9058: Feature 'lambda optional parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         var lam1 = (int x = 7) => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "=").WithArguments("lambda optional parameters", "12.0").WithLocation(5, 27));

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.CSharp12)]
        public void AnonymousMethodWithExplicitDefaultParam(LanguageVersion languageVersion)
        {
            var source = """
class Program
{
    public void M()
    {
        var lam = delegate(int x = 7) { return x; };
        lam();
    }
}

""";
            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion));
            comp.VerifyDiagnostics(
                // (5,34): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(int x = 7) { return x; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 34));
        }

        [Fact]
        public void LambdaWithImplicitDefaultParam1()
        {
            var source =
@"class Program 
{
    public static void Main(string[] args)
    {
        var lam1 = (x = 7) => x;
        lam1();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,20): error CS8917: The delegate type could not be inferred.
                //         var lam1 = (x = 7) => x;
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(x = 7) => x").WithLocation(5, 20),
                // (5,21): error CS9098:  Default not allowed for implicitly typed lambda parameter 'x' 
                //         var lam1 = (x = 7) => x;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(5, 21));
        }

        [Fact]
        public void LambdaWithImplicitDefaultParam2()
        {
            var source =
@"class Program 
{
    public static void Main(string[] args)
    {
        var lam = (string s = null, x = 7, double d = 3.14) => { };
        lam();
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,19): error CS8917: The delegate type could not be inferred.
                //         var lam = (string s = null, x = 7, double d = 3.14) => { };
                Diagnostic(ErrorCode.ERR_CannotInferDelegateType, "(string s = null, x = 7, double d = 3.14) => { }").WithLocation(5, 19),
                // (5,37): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         var lam = (string s = null, x = 7, double d = 3.14) => { };
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "x").WithLocation(5, 37),
                // (5,37): error CS9098: Implicitly typed lambda parameter 'x' cannot have a default value.
                //         var lam = (string s = null, x = 7, double d = 3.14) => { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedDefaultParameter, "x").WithArguments("x").WithLocation(5, 37));
        }

        [Fact]
        public void LambdaWithDefaultBeforeRequired1()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var lam = (int a = 3, int b) => { return a + b; };
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (5,36): error CS1737: Optional parameters must appear after all required parameters
                    //         var lam = (int a = 3, int b) => { return a + b; };
                    Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(5, 36));
        }

        [Fact]
        public void AnonymousMethodDefaultBeforeRequired1()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var lam = delegate(int a = 3, int b) { return a + b; };
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,34): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(int a = 3, int b) { return a + b; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 34),
                // (5,44): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = delegate(int a = 3, int b) { return a + b; };
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(5, 44));
        }

        [Fact]
        public void LambdaWithDefaultBeforeRequired2()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var lam = (int x, int y = 3, int z) => x + y + z;
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,43): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = (int x, int y = 3, int z) => x + y + z;
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(5, 43));
        }

        [Fact]
        public void AnonymousMethodDefaultBeforeRequired2()
        {
            var source =
@"class Program
{
    public static void Main()
    {
        var lam = delegate(int x, int y = 3, int z) { return x + y + z; };
    }
}
";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (5,41): error CS1065: Default values are not valid in this context.
                    //         var lam = delegate(int x, int y = 3, int z) { return x + y + z; };
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 41),
                    // (5,51): error CS1737: Optional parameters must appear after all required parameters
                    //         var lam = delegate(int x, int y = 3, int z) { return x + y + z; };
                    Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(5, 51));
        }

        [Fact]
        public void LambdaWithDefaultTypeMismatchLiteral()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = (int x = "abcdef") => x;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (5,24): error CS1750: A value of type 'string' cannot be used as a default parameter because there are no standard conversions to type 'int'
                    //         var lam = (int x = "abcdef") => x;
                    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("string", "int").WithLocation(5, 24));
        }

        [Fact]
        public void AnonymousMethodWithDefaultTypeMismatchLiteral()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = delegate(int x = "abcdef") { return x; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,32): error CS1750: A value of type 'string' cannot be used as a default parameter because there are no standard conversions to type 'int'
                //         var lam = delegate(int x = "abcdef") { return x; };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("string", "int").WithLocation(5, 32),
                // (5,34): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(int x = "abcdef") { return x; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 34));
        }

        [Fact]
        public void LambdaWithNonConstantDefault()
        {
            var source = """
class C
{
    object field;
    public object Field => @field;

    public C(object f) { field = f; }

}

class Program
{
    public static void Main()
    {
        var lam = (C c = new C(null)) => c.Field;
    }

}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (14,26): error CS1736: Default parameter value for 'c' must be a compile-time constant
                    //         var lam = (C c = new C(null)) => c.Field;
                    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new C(null)").WithArguments("c").WithLocation(14, 26));
        }

        [Fact]
        public void AnonymousMethodWithNonConstantDefault()
        {
            var source = """
class C
{
    object field;
    public object Field => @field;

    public C(object f) { field = f; }

}

class Program
{
    public static void Main()
    {
        var lam = delegate(C c = new C(null)) { return c.Field; };
    }

}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (14,32): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(C c = new C(null)) { return c.Field; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(14, 32),
                // (14,34): error CS1736: Default parameter value for 'c' must be a compile-time constant
                //         var lam = delegate(C c = new C(null)) { return c.Field; };
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "new C(null)").WithArguments("c").WithLocation(14, 34));
        }

        [Fact]
        public void LambdaWithNonConstantDefaultTypeMismatch()
        {
            var source = """
class Program
{
    static int add(int x, int y) => x + y;
    
    public static void Main(string[] args)
    {
        var lam = (string s = add(1, 2)) => s;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(// (7,31): error CS1736: Default parameter value for 's' must be a compile-time constant
                                   //         var lam = (string s = add(1, 2)) => s;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "add(1, 2)").WithArguments("s").WithLocation(7, 31));
        }

        [Fact]
        public void AnonymousMethodWithNonConstantDefaultTypeMismatch()
        {
            var source = """
class Program
{
    static int add(int x, int y) => x + y;
    
    public static void Main(string[] args)
    {
        var lam = delegate(string s = add(1, 2)) { return s; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,37): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(string s = add(1, 2)) { return s; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(7, 37),
                // (7,39): error CS1736: Default parameter value for 's' must be a compile-time constant
                //         var lam = delegate(string s = add(1, 2)) { return s; };
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "add(1, 2)").WithArguments("s").WithLocation(7, 39));
        }

        [Fact]
        public void LambdaWithComplexConstantExpression()
        {
            var source = """
class Program
{
    const bool b1 = false;
    const bool b2 = true;

    const int num1 = 1;
    const int num2 = 2;
    const int num3 = 3;

    public static void Main(string[] args)
    {
        var fn = (int arg = b1 ? num1 : b2 ? num2 : num3) => arg;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AnonymousMethodWithComplexConstantExpression()
        {
            var source = """
class Program
{
    const bool b1 = false;
    const bool b2 = true;

    const int num1 = 1;
    const int num2 = 2;
    const int num3 = 3;

    public static void Main(string[] args)
    {
        var fn = delegate(int arg = b1 ? num1 : b2 ? num2 : num3) { return arg; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (12,35): error CS1065: Default values are not valid in this context.
                //         var fn = delegate(int arg = b1 ? num1 : b2 ? num2 : num3) { return arg; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(12, 35));
        }

        [Fact]
        public void LambdaDefaultLocalConstantExpression()
        {
            var source = """
class Program
{
    public static void Main(string[] args)
    {
        const int i1 = 1;
        const int i2 = 2;
        var func = (int arg = i1 + i2) => arg + 1;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AnonymousMethodDefaultLocalConstantExpression()
        {
            var source = """
class Program
{
    public static void Main(string[] args)
    {
        const int i1 = 1;
        const int i2 = 2;
        var func = delegate(int arg = i1 + i2) { return arg + 1; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,37): error CS1065: Default values are not valid in this context.
                //         var func = delegate(int arg = i1 + i2) { return arg + 1; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(7, 37));
        }

        [Fact]
        public void AsyncLambdaWithDefaultParameters()
        {
            var source = """
using System.Threading.Tasks;
class Program
{
    public static void Main(string[] args)
    {
        var lam = async (int delay = 10) => await Task.Delay(delay);
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void StaticLambdaWithDefaultParameters()
        {
            var source = """
class Program 
{
    public static void Main()
    {
        var lam = static (string s = "str") => s;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaWithDefaultParametersAndRefOutModifiers()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = (ref int x, out object y, double c = 4.59) => { y = c + (double) x; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void AnonymousMethodWithDefaultParametersAndRefOutModifiers()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = delegate(ref int x, out object y, double c = 4.59) { y = c + (double) x; };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,62): error CS1065: Default values are not valid in this context.
                //         var lam = delegate(ref int x, out object y, double c = 4.59) { y = c + (double) x; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 62));
        }

        [Fact]
        public void LambdaWithMultipleDefaultParameters()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = (int u, string v, object w, int x = 10, int y = 3, int z = 4) => x + y + z; 
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultParamUsageAnalysis()
        {
            var source = """
class Program
{
    void M(int a, int b)
    {
        var _ = (int i = M2(a)) => { }; // parameter 'a' should be considered read/used
    }

    static int M2(int j) => j;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,26): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //         var _ = (int i = M2(a)) => { }; // parameter 'a' should be considered read/used
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M2(a)").WithArguments("i").WithLocation(5, 26));

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            // Find method parameters.
            var method = comp.GetMember<MethodSymbol>("Program.M").GetPublicSymbol();
            Assert.Equal(2, method.Parameters.Length);
            var a = method.Parameters[0];
            Assert.Equal("a", a.Name);
            var b = method.Parameters[1];
            Assert.Equal("b", b.Name);

            // Analyze flow inside method's body.
            var methodSyntax = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences.Single().GetSyntax();
            var dataFlow = model.AnalyzeDataFlow(methodSyntax.Body);
            Assert.Contains(a, dataFlow.ReadInside);
            Assert.DoesNotContain(b, dataFlow.ReadInside);
        }

        [Fact]
        public void AnonymousMethodDefaultParameterUsageAnalysis()
        {
            var source = """
class Program
{
    void M(int a)
    {
       var _ = delegate(int i = M2(a)) { }; // parameter 'a' should be considered read/used
    }

    static int M2(int j) => j;
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,31): error CS1065: Default values are not valid in this context.
                //        var _ = delegate(int i = M2(a)) { }; // parameter 'a' should be considered read/used
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 31),
                // (5,33): error CS1736: Default parameter value for 'i' must be a compile-time constant
                //        var _ = delegate(int i = M2(a)) { }; // parameter 'a' should be considered read/used
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "M2(a)").WithArguments("i").WithLocation(5, 33));
        }

        [Fact]
        public void LambdaDefaultWithinCheckedContext()
        {
            var source = """
class Program
{
    void M()
    {
        var _ = (int i = int.MaxValue + 1) => i;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                    // (5,26): error CS0220: The operation overflows at compile time in checked mode
                    //         var _ = (int i = int.MaxValue + 1) => i;
                    Diagnostic(ErrorCode.ERR_CheckedOverflow, "int.MaxValue + 1").WithLocation(5, 26));
        }

        [Fact]
        public void LambdaDefaultWithinUncheckedContext()
        {
            var source = """
class Program
{
    void M()
    {
        unchecked
        {
            var _ = (int i = int.MaxValue + 1) => i;
        }
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultWithinNestedScope()
        {
            var source = """
class Program
{
    void M()
    {
        {
            const string s = "abcdef";
            var _ = (string str = s) => s;
        }
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultInDifferentNestedScope()
        {
            var source = """
class Program
{
    void M()
    {
        {
            const string s = "abcdef";
        }
        var _ = (string str = s) => str;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,26): warning CS0219: The variable 's' is assigned but its value is never used
                //             const string s = "abcdef";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "s").WithArguments("s").WithLocation(6, 26),
                // (8,31): error CS0103: The name 's' does not exist in the current context
                //         var _ = (string str = s) => s;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(8, 31));
        }

        [Fact]
        public void LambdaDefaultLocalConstantSameScope_PreDefinition()
        {
            var source = """
class Program
{
    void M()
    {
        const string s = "abcdef";
        var _ = (string str = s) => s;
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void LambdaDefaultLocalConstantSameScope_PostDefinition()
        {
            var source = """
class Program
{
    void M()
    {
        var lam = (string str = s) => { };
        const string s = "abcdef";
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (5,27): error CS1750: A value of type 'var' cannot be used as a default parameter because there are no standard conversions to type 'string'
                //         var lam = (string str = s) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "str").WithArguments("var", "string").WithLocation(5, 27),
                // (5,33): error CS0841: Cannot use local variable 's' before it is declared
                //         var lam = (string str = s) => { };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "s").WithArguments("s").WithLocation(5, 33));
        }

        [Fact]
        public void LambdaDefaultSelfReference()
        {
            var source = """
using System;

class Program
{
    public static void Main(string[] args)
    {
        var lam = (Delegate d = lam) => { };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,29): error CS1750: A value of type 'var' cannot be used as a default parameter because there are no standard conversions to type 'Delegate'
                //         var lam = (Delegate d = lam) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "d").WithArguments("var", "System.Delegate").WithLocation(7, 29),
                // (7,33): error CS0841: Cannot use local variable 'lam' before it is declared
                //         var lam = (Delegate d = lam) => { };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "lam").WithArguments("lam").WithLocation(7, 33));
        }

        [Fact]
        public void LambdaDefaultSelfReference_ParameterBefore()
        {
            var source = """
using System;

class Program
{
    public static void Main(string[] args)
    {
        var lam = (int x, Delegate d = lam) => { };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,36): error CS1750: A value of type 'var' cannot be used as a default parameter because there are no standard conversions to type 'Delegate'
                //         var lam = (int x, Delegate d = lam) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "d").WithArguments("var", "System.Delegate").WithLocation(7, 36),
                // (7,40): error CS0841: Cannot use local variable 'lam' before it is declared
                //         var lam = (int x, Delegate d = lam) => { };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "lam").WithArguments("lam").WithLocation(7, 40));
        }

        [Fact]
        public void LambdaDefaultSelfReference_ParameterAfter()
        {
            var source = """
using System;

class Program
{
    public static void Main(string[] args)
    {
        var lam = (Delegate d = lam, int x) => { };
    }
}
""";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,29): error CS1750: A value of type 'var' cannot be used as a default parameter because there are no standard conversions to type 'Delegate'
                //         var lam = (Delegate d = lam, int x) => { };
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "d").WithArguments("var", "System.Delegate").WithLocation(7, 29),
                // (7,33): error CS0841: Cannot use local variable 'lam' before it is declared
                //         var lam = (Delegate d = lam, int x) => { };
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "lam").WithArguments("lam").WithLocation(7, 33),
                // (7,43): error CS1737: Optional parameters must appear after all required parameters
                //         var lam = (Delegate d = lam, int x) => { };
                Diagnostic(ErrorCode.ERR_DefaultValueBeforeRequiredValue, ")").WithLocation(7, 43));
        }

        [Fact]
        public void Lambda_BadDefault_ExplicitReturnType()
        {
            var source = """
class Program
{
    static int f(int x) => 2 * x;
    public static void Main(string[] args)
    {
        var lam = int (int p = f(3)) => p;
    }
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,32): error CS1736: Default parameter value for 'p' must be a compile-time constant
                //         var lam = int (int p = f(3)) => p;
                Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "f(3)").WithArguments("p").WithLocation(6, 32));
        }

        [Fact]
        public void LambdaDefault_LongString()
        {
            var longString = "";
            for (int i = 0; i < 100; i++)
            {
                longString += i.ToString();
            }

            var source = $@"
class Program
{{
    delegate void Del(string s = ""abc"");
    public static void Main(string[] args)
    {{
        Del del = (string s = ""{longString}"") => {{ }};
    }}
}}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (7,27): warning CS9099: Parameter 1 has default value '"0123456..."' in lambda but '"abc"' in the target delegate type.
                //         Del del = (string s = "0123456789101112131415161718192021222324252627282930313233343536373839404142434445464748495051525354555657585960616263646566676869707172737475767778798081828384858687888990919293949596979899") => { };
                Diagnostic(ErrorCode.WRN_OptionalParamValueMismatch, "s").WithArguments("1", @"""0123456...""", @"""abc""").WithLocation(7, 27));
        }

        [Fact]
        public void LambdaDefault_InvalidConstantConversion()
        {
            var source = @"
class Program
{
    public static void Main()
    {
        var lam = (string s = 1) => s;
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,27): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'string'
                //         var lam = (string s = 1) => s;
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("int", "string").WithLocation(6, 27));
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("out")]
        public void LambdaDefault_RefOut(string modifier)
        {
            var source = $$"""
                var lam = void ({{modifier}} int x = 1) => throw null;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,17): error CS1741: A ref or out parameter cannot have a default value
                // var lam = void (ref int x = 1) => throw null;
                Diagnostic(ErrorCode.ERR_RefOutDefaultValue, modifier).WithLocation(1, 17));
        }

        [Fact]
        public void LambdaDefault_ThisModifier()
        {
            var source = """
                var lam = void (this int x = 1) => throw null;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,17): error CS1041: Identifier expected; 'this' is a keyword
                // var lam = void (this int x = 1) => throw null;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this").WithLocation(1, 17));
        }

        [Fact]
        public void LambdaWithDefaultParameterAndParams()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = (int i = 3, params int[] args) => i;
    }   
}
""";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void LambdaWithDefaultParameter_RequiredMissing()
        {
            var source = """
class Program
{
    public static void Main()
    {
        var lam = (int a, int b, int c = 3) => a + b * c;
        lam(5);
    }   
}
""";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,9): error CS7036: There is no argument given that corresponds to the required parameter 'arg2' of '<anonymous delegate>'
                //         lam(5);
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "lam").WithArguments("arg2", "<anonymous delegate>").WithLocation(6, 9));
        }

        [Fact]
        public void LambdaWithDefaultParameter_SymbolInfo()
        {
            var source = """
                using System.Runtime.InteropServices;
                var lam1 = (int a, int b = 1) => a + b;
                var lam2 = ([Optional] int x) => x;
                var lam3 = ([DefaultParameterValue(2)] int x) => x;
                var lam4 = ([Optional, DefaultParameterValue(3)] int x) => x;
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambdas = tree.GetRoot().DescendantNodes()
                .Where(n => n.IsKind(SyntaxKind.ParenthesizedLambdaExpression))
                .Select(n => (Node: n, Symbol: (IMethodSymbol)model.GetSymbolInfo(n).Symbol))
                .ToImmutableArray();
            Assert.Equal(4, lambdas.Length);

            // lam1
            Assert.Equal("(int a, int b = 1) => a + b", lambdas[0].Node.ToString());
            Assert.Equal(2, lambdas[0].Symbol.Parameters.Length);
            Assert.False(lambdas[0].Symbol.Parameters[0].IsOptional);
            Assert.False(lambdas[0].Symbol.Parameters[0].HasExplicitDefaultValue);
            Assert.Throws<InvalidOperationException>(() => lambdas[0].Symbol.Parameters[0].ExplicitDefaultValue);
            Assert.True(lambdas[0].Symbol.Parameters[1].IsOptional);
            Assert.True(lambdas[0].Symbol.Parameters[1].HasExplicitDefaultValue);
            Assert.Equal(1, lambdas[0].Symbol.Parameters[1].ExplicitDefaultValue);

            // lam2
            Assert.Equal("([Optional] int x) => x", lambdas[1].Node.ToString());
            Assert.Equal(1, lambdas[1].Symbol.Parameters.Length);
            Assert.True(lambdas[1].Symbol.Parameters[0].IsOptional);
            Assert.False(lambdas[2].Symbol.Parameters[0].HasExplicitDefaultValue);
            Assert.Throws<InvalidOperationException>(() => lambdas[1].Symbol.Parameters[0].ExplicitDefaultValue);

            // lam3
            Assert.Equal("([DefaultParameterValue(2)] int x) => x", lambdas[2].Node.ToString());
            Assert.Equal(1, lambdas[2].Symbol.Parameters.Length);
            Assert.False(lambdas[2].Symbol.Parameters[0].IsOptional);
            Assert.False(lambdas[2].Symbol.Parameters[0].HasExplicitDefaultValue);
            Assert.Throws<InvalidOperationException>(() => lambdas[2].Symbol.Parameters[0].ExplicitDefaultValue);

            // lam4
            Assert.Equal("([Optional, DefaultParameterValue(3)] int x) => x", lambdas[3].Node.ToString());
            Assert.Equal(1, lambdas[3].Symbol.Parameters.Length);
            Assert.True(lambdas[3].Symbol.Parameters[0].IsOptional);
            Assert.True(lambdas[3].Symbol.Parameters[0].HasExplicitDefaultValue);
            Assert.Equal(3, lambdas[3].Symbol.Parameters[0].ExplicitDefaultValue);
        }

        [Fact]
        public void LambdaWithDefaultParameter_EqualsValueClauseSyntax()
        {
            var source = """
                var lam = (int a, int b = 1) => a + b;
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var lambda = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var equalsValue = lambda.DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();
            Assert.Equal("= 1", equalsValue.ToString());
            var constantValue = model.GetConstantValue(equalsValue.Value);
            Assert.True(constantValue.HasValue);
            Assert.Equal(1, constantValue.Value);
        }

        [Fact]
        public void LambdaWithDefaultParameter_SpeculativeSemanticModel()
        {
            var source = """
                class C
                {
                    public static int M1(int x) => x * 2;
                    public static void M2()
                    {
                        var lam = (int b) => b;
                    }
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var m2 = comp.GetMember<MethodSymbol>("C.M2").GetPublicSymbol();
            var m2Syntax = (MethodDeclarationSyntax)m2.DeclaringSyntaxReferences.Single().GetSyntax();
            var newStmt = SyntaxFactory.ParseStatement("var lam = (int b = M1(4)) => b;");
            var newMethod = m2Syntax.WithBody(SyntaxFactory.Block(newStmt));
            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(m2Syntax.Body.SpanStart, newMethod, out var speculativeModel));
            var newLambda = newMethod.DescendantNodes().OfType<LambdaExpressionSyntax>().Single();
            var newLambdaSymbol = (IMethodSymbol)speculativeModel.GetSymbolInfo(newLambda).Symbol;
            var newParam = newLambdaSymbol.Parameters.Single();
            Assert.True(newParam.HasExplicitDefaultValue);
            Assert.Null(newParam.ExplicitDefaultValue);

            // Ensure errors from default parameter values are not added to declaration table.
            model.GetDiagnostics().Verify();
        }

        [Fact]
        public void LambdaWithDefaultParameter_SameSymbols()
        {
            var source = """
                class C
                {
                    public static void M()
                    {
                        const int N = 10;
                        var lam = (int a = N) => a;
                        var x = N;
                    }
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (7,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         var x = N;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(7, 13));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().ToImmutableArray();
            Assert.Equal(3, decls.Length);

            Assert.Equal("const int N = 10;", decls[0].ToString());
            var constSymbol = model.GetDeclaredSymbol(decls[0].Declaration.Variables.Single());
            Assert.NotNull(constSymbol);

            Assert.Equal("var lam = (int a = N) => a;", decls[1].ToString());
            var defaultValue = decls[1].DescendantNodes().OfType<LambdaExpressionSyntax>().Single()
                .DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;
            var defaultValueSymbol = model.GetSymbolInfo(defaultValue).Symbol;
            Assert.Same(constSymbol, defaultValueSymbol);

            Assert.Equal("var x = N;", decls[2].ToString());
            var lhs = decls[2].DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;
            var lhsSymbol = model.GetSymbolInfo(lhs).Symbol;
            Assert.Same(constSymbol, lhsSymbol);
        }

        [Fact]
        public void LambdaWithDefaultParameter_MemberSemanticModel()
        {
            var source = """
                class C
                {
                    public static void M()
                    {
                        const int N = 10;
                        var lam = (int a = N) => a;
                        lam();
                    }
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = (SyntaxTreeSemanticModel)comp.GetSemanticModel(tree, ignoreAccessibility: false);

            // Ensure MemberSemanticModel is parented to the correct outer SemanticModel.
            var defaultValue = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().Single()
                .DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;
            var defaultValueModel = model.GetMemberModel(defaultValue);
            Assert.Same(model, defaultValueModel.ContainingPublicModelOrSelf);

            // Ensure binder chain is shared with member model for the enclosing method body.
            var methodSyntax = (MethodDeclarationSyntax)comp.GetMember<MethodSymbol>("C.M").GetNonNullSyntaxNode();
            var methodModel = model.GetMemberModel(methodSyntax.Body);
            Assert.NotNull(methodModel);
            var methodBinder = getBinder<BlockBinder>(methodModel.GetEnclosingBinder(methodSyntax.Body.SpanStart));
            var defaultValueBinder = getBinder<BlockBinder>(defaultValueModel.GetEnclosingBinder(defaultValue.SpanStart));
            Assert.Same(methodBinder, defaultValueBinder);

            static T getBinder<T>(Binder binder) where T : Binder
            {
                while (true)
                {
                    if (binder is T t)
                    {
                        return t;
                    }
                    binder = binder.NextRequired;
                }
            }
        }

        [Fact]
        public void LambdaWithDefaultParameter_BindingScope()
        {
            var source = """
                #nullable enable
                class C<T> {
                    public static void M() {
                        var lam = (int a,
                            string b = nameof(a), // 1
                            string c = nameof(lam), // 2
                            string d = nameof(M),
                            string e = nameof(C<T>),
                            T? f = default(T),
                            C<T>? g = default(C<T>)
                            ) => { };
                        lam(1);
                    }
                }
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (5,31): error CS0103: The name 'a' does not exist in the current context
                //             string b = nameof(a), // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(5, 31),
                // (6,31): error CS0841: Cannot use local variable 'lam' before it is declared
                //             string c = nameof(lam), // 2
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "lam").WithArguments("lam").WithLocation(6, 31));
        }

        [Fact]
        public void LambdaWithDefaultParameter_LookupNames()
        {
            var source = """
                class C<T1> {
                    private const int N1 = 10;
                    private int x;
                    private void M1() { }
                    public static void M2<T2>() {
                        const int N2 = 20;
                        int y = 2;
                        var lam = (int a, int b = /*pos*/5, int c = 6) => { };
                        lam(1);
                    }
                }
                """;
            var tree = Parse(source);
            var comp = CreateCompilation(tree);
            var model = comp.GetSemanticModel(tree);
            var names = model.LookupNames(GetPositionForBinding(source));
            Assert.Contains("C", names);
            Assert.Contains("x", names);
            Assert.Contains("y", names);
            Assert.Contains("N1", names);
            Assert.Contains("N2", names);
            Assert.Contains("T1", names);
            Assert.Contains("T2", names);
            Assert.Contains("M1", names);
            Assert.Contains("M2", names);
            Assert.Contains("lam", names);
            Assert.DoesNotContain("a", names);
            Assert.DoesNotContain("b", names);
            Assert.DoesNotContain("c", names);
        }

        [Fact]
        public void ParamsArray_Langversion()
        {
            var source = """
                var lam = (params int[] xs) => xs.Length;
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (1,12): error CS9058: Feature 'lambda params array' is not available in C# 11.0. Please use language version 12.0 or greater.
                // var lam = (params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "params").WithArguments("lambda params array", "12.0").WithLocation(1, 12));

            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics();
        }

        [Theory]
        [InlineData(LanguageVersion.CSharp11)]
        [InlineData(LanguageVersion.CSharp12)]
        public void ParamsArray_Langversion_DelegateSyntax(LanguageVersion languageVersion)
        {
            var source = """
                var lam = delegate (params int[] xs) { return xs.Length; };
                """;

            CreateCompilation(source, parseOptions: TestOptions.Regular.WithLanguageVersion(languageVersion)).VerifyDiagnostics(
                // (1,21): error CS1670: params is not valid in this context
                // var lam = delegate (params int[] xs) { return xs.Length };
                Diagnostic(ErrorCode.ERR_IllegalParams, "params").WithLocation(1, 21));
        }

        [Fact]
        public void ParamsArray_Call()
        {
            var source = """
                var lam = (params int?[] xs) => System.Console.WriteLine(xs?.Length.ToString() ?? "null");
                lam();
                lam(1);
                lam(1, 2, 3);
                lam(new int?[] { 1, 2, 3 });
                lam(null);
                lam((int?)null);
                """;
            CompileAndVerify(source, expectedOutput: """
                0
                1
                3
                3
                null
                1
                """).VerifyDiagnostics();
        }

        [Fact]
        public void ParamsArray_WithDefaultValue()
        {
            var source = """
                var lam = (params int[] xs = null) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS1751: Cannot specify a default value for a parameter collection
                // var lam = (params int[] xs = null) => xs.Length;
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(1, 12));
        }

        [Fact]
        public void ParamsArray_ParamArrayAttribute()
        {
            var source = """
                var lam = ([System.ParamArray] int[] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,13): error CS0674: Do not use 'System.ParamArrayAttribute'/'System.Runtime.CompilerServices.ParamCollectionAttribute'. Use the 'params' keyword instead.
                // var lam = ([System.ParamArray] int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_ExplicitParamArrayOrCollection, "System.ParamArray").WithLocation(1, 13));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66060")]
        public void ParamsArray_ParamArrayAttribute_OverloadResolution()
        {
            var source = """
                using System;

                new C().M(([ParamArray] int[] xs) => xs.Length);
                new C().M((params int[] xs) => xs.Length);

                class C
                {
                    public void M(D d) { }
                    public void M(Func<int[], int> f) { }
                }

                delegate int D(params int[] xs);
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (3,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(D)' and 'C.M(Func<int[], int>)'
                // new C().M(([ParamArray] int[] xs) => xs.Length);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(D)", "C.M(System.Func<int[], int>)").WithLocation(3, 9),
                // (4,9): error CS0121: The call is ambiguous between the following methods or properties: 'C.M(D)' and 'C.M(Func<int[], int>)'
                // new C().M((params int[] xs) => xs.Length);
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("C.M(D)", "C.M(System.Func<int[], int>)").WithLocation(4, 9));
        }

        [Fact]
        public void ParamsArray_Symbol()
        {
            var source = """
                var lam1 = (params int[] xs) => xs.Length;
                var lam2 = (int[] xs) => xs.Length;
                var lam3 = (int[] xs, params int[] ys) => xs.Length + ys.Length;
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Equal(3, lambdas.Length);
            // lam1
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters.Single()).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters.Single()).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters.Single()).IsParamsCollection);
            // lam2
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters.Single()).IsParams);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters.Single()).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters.Single()).IsParamsCollection);
            // lam3
            Assert.Equal(2, lambdas[2].ParameterCount);
            Assert.Equal(2, lambdas[2].Parameters.Length);
            Assert.False(((SourceParameterSymbol)lambdas[2].Parameters[0]).IsParams);
            Assert.False(((SourceParameterSymbol)lambdas[2].Parameters[0]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[2].Parameters[0]).IsParamsCollection);
            Assert.True(((SourceParameterSymbol)lambdas[2].Parameters[1]).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[2].Parameters[1]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[2].Parameters[1]).IsParamsCollection);
        }

        [Fact]
        public void ParamsArray_Symbol_MultipleParamsArrays()
        {
            var source = """
                var lam1 = (params int[] xs, params int[] ys, int[] zs) => xs.Length + ys.Length + zs.Length;
                var lam2 = (params int[] xs, int[] ys, params int[] zs) => xs.Length + ys.Length + zs.Length;
                """;
            var comp = CreateCompilation(source);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>().ToImmutableArray();
            var lambdas = exprs.SelectAsArray(e => GetLambdaSymbol(model, e));
            Assert.Equal(2, lambdas.Length);
            // lam1
            Assert.Equal(3, lambdas[0].ParameterCount);
            Assert.Equal(3, lambdas[0].Parameters.Length);
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters[0]).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters[0]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters[0]).IsParamsCollection);
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters[1]).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[0].Parameters[1]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters[1]).IsParamsCollection);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters[2]).IsParams);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters[2]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[0].Parameters[2]).IsParamsCollection);
            // lam2
            Assert.Equal(3, lambdas[1].ParameterCount);
            Assert.Equal(3, lambdas[1].Parameters.Length);
            Assert.True(((SourceParameterSymbol)lambdas[1].Parameters[0]).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[1].Parameters[0]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters[0]).IsParamsCollection);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters[1]).IsParams);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters[1]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters[1]).IsParamsCollection);
            Assert.True(((SourceParameterSymbol)lambdas[1].Parameters[2]).IsParams);
            Assert.True(((SourceParameterSymbol)lambdas[1].Parameters[2]).IsParamsArray);
            Assert.False(((SourceParameterSymbol)lambdas[1].Parameters[2]).IsParamsCollection);
        }

        [Fact]
        public void ParamsArray_Symbol_ExternalReference()
        {
            var source = """
                static void Report(object obj) => System.Console.WriteLine(obj.GetType());
                var lam1 = (params int[] xs) => xs.Length;
                Report(lam1);
                var lam2 = (int[] xs) => xs.Length;
                Report(lam2);
                var lam3 = (int[] xs, params int[] ys) => xs.Length + ys.Length;
                Report(lam3);
                """;
            CompileAndVerify(source, expectedOutput: """
                <>f__AnonymousDelegate0`2[System.Int32,System.Int32]
                System.Func`2[System.Int32[],System.Int32]
                <>f__AnonymousDelegate1`3[System.Int32[],System.Int32,System.Int32]
                """, symbolValidator: static module =>
                {
                    var lam1 = (NamedTypeSymbol)module.GlobalNamespace.GetMember("<>f__AnonymousDelegate0");
                    Assert.True(lam1.DelegateParameters().Single().IsParams);
                    Assert.True(lam1.DelegateParameters().Single().IsParamsArray);
                    Assert.False(lam1.DelegateParameters().Single().IsParamsCollection);
                    AssertEx.Equal("TResult <>f__AnonymousDelegate0<T1, TResult>.Invoke(params T1[] arg)", lam1.DelegateInvokeMethod.ToTestDisplayString());

                    var lam3 = (NamedTypeSymbol)module.GlobalNamespace.GetMember("<>f__AnonymousDelegate1");
                    var lam3Parameters = lam3.DelegateParameters();
                    Assert.Equal(2, lam3Parameters.Length);
                    Assert.False(lam3Parameters[0].IsParams);
                    Assert.False(lam3Parameters[0].IsParamsArray);
                    Assert.False(lam3Parameters[0].IsParamsCollection);
                    Assert.True(lam3Parameters[1].IsParams);
                    Assert.True(lam3Parameters[1].IsParamsArray);
                    Assert.False(lam3Parameters[1].IsParamsCollection);
                    AssertEx.Equal("TResult <>f__AnonymousDelegate1<T1, T2, TResult>.Invoke(T1 arg1, params T2[] arg2)", lam3.DelegateInvokeMethod.ToTestDisplayString());
                });
        }

        [Fact]
        public void ParamsArray_NotLast()
        {
            var source = """
                var lam = (params int[] xs, int y) => xs.Length + y;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0231: A params parameter must be the last parameter in a parameter list
                // var lam = (params int[] xs, int y) => xs.Length + y;
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] xs").WithLocation(1, 12));
        }

        [Fact]
        public void ParamsArray_Multiple()
        {
            var source = """
                var lam = (params int[] xs, params int[] ys) => xs.Length + ys.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0231: A params parameter must be the last parameter in a parameter list
                // var lam = (params int[] xs, params int[] ys) => xs.Length + ys.Length;
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] xs").WithLocation(1, 12));
        }

        [Fact]
        public void ParamsArray_NotArray()
        {
            var source = """
                var lam = (params int x) => x;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0225: The params parameter must have a valid collection type
                // var lam = (params int x) => x;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(1, 12)
                );
        }

        [Fact]
        public void ParamsArray_Multidimensional()
        {
            var source = """
                var lam = (params int[,] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS0225: The params parameter must have a valid collection type
                // var lam = (params int[,] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(1, 12)
                );
        }

        [Fact]
        public void ParamsArray_Jagged()
        {
            var source = """
                var lam = (params int[][] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Theory]
        [InlineData("ref")]
        [InlineData("out")]
        [InlineData("in")]
        public void ParamsArray_OtherModifiers(string modifier)
        {
            var source = $$"""
                var lam = void (params {{modifier}} int[] xs) => throw null;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,24): error CS1611: The params parameter cannot be declared as ref
                // var lam = void (params ref int[] xs) => throw null;
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, modifier).WithArguments(modifier).WithLocation(1, 24));
        }

        [Fact]
        public void ParamsArray_ThisModifier_01()
        {
            var source = """
                var lam = (this params int[] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,12): error CS1041: Identifier expected; 'this' is a keyword
                // var lam = (this params int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this").WithLocation(1, 12));
        }

        [Fact]
        public void ParamsArray_ThisModifier_02()
        {
            var source = """
                var lam = (params this int[] xs) => xs.Length;
                """;
            CreateCompilation(source).VerifyDiagnostics(
                // (1,19): error CS0027: Keyword 'this' is not available in the current context
                // var lam = (params this int[] xs) => xs.Length;
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(1, 19));
        }
    }
}
