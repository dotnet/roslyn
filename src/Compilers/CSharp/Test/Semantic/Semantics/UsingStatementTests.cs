// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) using statements (not directives).
    /// </summary>
    public class UsingStatementTests : CompilingTestBase
    {
        private readonly string _managedClass = @"
class MyManagedType : System.IDisposable
{
    public void Dispose()
    { }
}";

        private readonly string _managedStruct = @"
struct MyManagedType : System.IDisposable
{
    public void Dispose()
    { }
}";

        [Fact]
        public void SemanticModel()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable i = null)
        {
            i.Dispose(); //this makes no sense, but we're only testing binding
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Single());
            Assert.NotNull(declaredSymbol);
            Assert.Equal(SymbolKind.Local, declaredSymbol.Kind);
            var declaredLocal = (LocalSymbol)declaredSymbol;
            Assert.Equal("i", declaredLocal.Name);
            Assert.Equal(SpecialType.System_IDisposable, declaredLocal.Type.SpecialType);

            var memberAccessExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();

            var info = model.GetSymbolInfo(memberAccessExpression.Expression);
            Assert.NotNull(info);
            Assert.Equal(declaredLocal, info.Symbol);

            var lookupSymbol = model.LookupSymbols(memberAccessExpression.SpanStart, name: declaredLocal.Name).Single();
            Assert.Equal(declaredLocal, lookupSymbol);
        }

        [Fact]
        public void MethodGroup()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (Main)
        {
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,16): error CS1674: 'method group': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "Main").WithArguments("method group"));
        }

        [Fact]
        public void Lambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (x => x)
        {
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,16): error CS1674: 'lambda expression': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "x => x").WithArguments("lambda expression"));
        }

        [Fact]
        public void Null()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (null)
        {
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void UnusedVariable()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable d = null)
        {
        }
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedStatement()
        {
            var source = @"
class C
{
    static void Main()
    {
        using (System.IDisposable a = null)
            using (System.IDisposable b = null)
                using (System.IDisposable c = null) ;
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (8,53): warning CS0642: Possible mistaken empty statement
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"));
        }

        [Fact]
        public void ModifyUsingLocal()
        {
            var source = @"
using System;

class C
{
    static void Main()
    {
        using (IDisposable i = null)
        {
            i = null;
            Ref(ref i);
            Out(out i);
        }
    }

    static void Ref(ref IDisposable i) { }
    static void Out(out IDisposable i) { i = null; }
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
    // (10,13): error CS1656: Cannot assign to 'i' because it is a 'using variable'
    //             i = null;
    Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(10, 13),
    // (11,21): error CS1657: Cannot use 'i' as a ref or out value because it is a 'using variable'
    //             Ref(ref i);
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(11, 21),
    // (12,21): error CS1657: Cannot use 'i' as a ref or out value because it is a 'using variable'
    //             Out(out i);
    Diagnostic(ErrorCode.ERR_RefReadonlyLocalCause, "i").WithArguments("i", "using variable").WithLocation(12, 21)
    );
        }

        [Fact]
        public void ImplicitType1()
        {
            var source = @"
using System.IO;

class C
{
    static void Main()
    {
        using (var a = new StreamWriter(""""))
        {
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var declaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Single());

            Assert.Equal("System.IO.StreamWriter a", declaredSymbol.ToTestDisplayString());

            var typeInfo = model.GetSymbolInfo(usingStatement.Declaration.Type);
            Assert.Equal(((LocalSymbol)declaredSymbol).Type.TypeSymbol, typeInfo.Symbol);
        }

        [Fact]
        public void ImplicitType2()
        {
            var source = @"
using System.IO;

class C
{
    static void Main()
    {
        using (var a = new StreamWriter(""""), b = new StreamReader(""""))
        {
        }
    }
}
";

            var compilation = CreateCompilationWithMscorlib(source);
            compilation.VerifyDiagnostics(
                // (8,16): error CS0819: Implicitly-typed variables cannot have multiple declarators
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator, @"var a = new StreamWriter(""""), b = new StreamReader("""")"));

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().Single();

            var firstDeclaredSymbol = model.GetDeclaredSymbol(usingStatement.Declaration.Variables.First());

            Assert.Equal("System.IO.StreamWriter a", firstDeclaredSymbol.ToTestDisplayString());

            var typeInfo = model.GetSymbolInfo(usingStatement.Declaration.Type);
            // lowest/last bound node with associated syntax is being picked up. Fine for now.
            Assert.Equal(((LocalSymbol)model.GetDeclaredSymbol(usingStatement.Declaration.Variables.Last())).Type.TypeSymbol, typeInfo.Symbol);
        }

        [Fact]
        public void ModifyLocalInUsingExpression()
        {
            var source = @"
using System;

class C
{
    void Main()
    {
        IDisposable i = null;
        using (i)
        {
            i = null; //CS0728
            Ref(ref i); //CS0728
            this[out i] = 1; //CS0728
        }
    }

    void Ref(ref IDisposable i) { }
    int this[out IDisposable i] { set { i = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (18,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (11,13): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (12,21): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (13,22): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"));
        }

        [Fact]
        public void ModifyParameterInUsingExpression()
        {
            var source = @"
using System;

class C
{
    void M(IDisposable i)
    {
        using (i)
        {
            i = null; //CS0728
            Ref(ref i); //CS0728
            this[out i] = 1; //CS0728
        }
    }

    void Ref(ref IDisposable i) { }
    int this[out IDisposable i] { set { i = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (17,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (10,13): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (11,21): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"),
                // (12,22): warning CS0728: Possibly incorrect assignment to local 'i' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "i").WithArguments("i"));
        }

        // The object could be created outside the "using" statement 
        [Fact]
        public void ResourceCreatedOutsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        MyManagedType mnObj1 = null;
        using (mnObj1)
        {
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilationWithMscorlib(source);
            VerifyDeclaredSymbolForUsingStatements(compilation);
        }

        // The object created inside the "using" statement but declared no variable
        [Fact]
        public void ResourceCreatedInsideUsingWithNoVarDeclared()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (new MyManagedType())
        {
        }
    }
}
" + _managedStruct;
            var compilation = CreateCompilationWithMscorlib(source);
            VerifyDeclaredSymbolForUsingStatements(compilation);
        }

        // Multiple resource created inside Using
        /// <bug id="10509" project="Roslyn"/>
        [Fact()]
        public void MultipleResourceCreatedInsideUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedType mnObj1 = null, mnObj2 = default(MyManagedType))
        {
        }
    }
}
" + _managedStruct;

            var compilation = CreateCompilationWithMscorlib(source);
            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj1", "mnObj2");
            foreach (var x in symbols)
            {
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol);
            }
        }

        [Fact]
        public void MultipleResourceCreatedInNestedUsing()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        using (MyManagedType mnObj1 = null, mnObj2 = default(MyManagedType))
        {
            using (MyManagedType mnObj3 = null, mnObj4 = default(MyManagedType))
            {
                mnObj3.Dispose(); 
            }
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilationWithMscorlib(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 2, "mnObj3", "mnObj4");
            foreach (var x in symbols)
            {
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 2);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 2);
            }
        }

        [Fact]
        public void ResourceTypeDerivedFromClassImplementIdisposable()
        {
            var source = @"
using System;
class Program
{
    public static void Main(string[] args)
    {
        using (MyManagedTypeDerived mnObj = new MyManagedTypeDerived())
        {
        }
    }
}
class MyManagedTypeDerived : MyManagedType
{ }
" + _managedClass;

            var compilation = CreateCompilationWithMscorlib(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
            }
        }

        [Fact]
        public void LinqInUsing()
        {
            var source = @"
using System;
using System.Linq;
class Program
{
    public static void Main(string[] args)
    {
        using (var mnObj = (from x in ""1"" select new MyManagedType()).First () )
        {
        }
    }
}
" + _managedClass;

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
            }
        }

        [Fact]
        public void LambdaInUsing()
        {
            var source = @"
using System;
using System.Linq;
class Program
{
    public static void Main(string[] args)
    {
        MyManagedType[] mnObjs = { };
        using (var mnObj = mnObjs.Where(x => x.ToString() == "").First())
        {
        }
    }
}
" + _managedStruct;

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "mnObj");
            foreach (var x in symbols)
            {
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
            }
        }

        [Fact]
        public void UsingForGenericType()
        {
            var source = @"
using System;
using System.Collections.Generic;
class Test<T>
{
    public static IEnumerator<T> M<U>(IEnumerable<T> items) where U : IDisposable, new()
    {
        using (U u = new U())
        {
        }
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);

            var symbols = VerifyDeclaredSymbolForUsingStatements(compilation, 1, "u");
            foreach (var x in symbols)
            {
                var localSymbol = (LocalSymbol)x;
                VerifyLookUpSymbolForUsingStatements(compilation, localSymbol, 1);
                VerifySymbolInfoForUsingStatements(compilation, ((LocalSymbol)x).Type.TypeSymbol, 1);
            }
        }

        [Fact]
        public void UsingForGenericTypeWithClassConstraint()
        {
            var source = @"using System;
class A { }
class B : A, IDisposable
{
    void IDisposable.Dispose() { }
}
class C
{
    static void M<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4)
        where T0 : A
        where T1 : A, IDisposable
        where T2 : B
        where T3 : T1
        where T4 : T2
    {
        using (t0) { }
        using (t1) { }
        using (t2) { }
        using (t3) { }
        using (t4) { }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (16,16): error CS1674: 'T0': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "t0").WithArguments("T0").WithLocation(16, 16));
        }

        [WorkItem(543168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543168")]
        [Fact]
        public void EmbeddedDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        using(null) object o = new object();
    }
}
";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (6,20): error CS1023: Embedded statement cannot be a declaration or labeled statement
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "object o = new object();"));
        }

        [WorkItem(529547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529547")]
        [Fact]
        public void UnusedLocal()
        {
            var source = @"
using System;

class C : IDisposable
{
    public void Dispose()
    {
    }
}

struct S : IDisposable
{
    public void Dispose()
    {
    }
}

public class Test
{
    public static void Main()
    {
        using (S s = new S()) { } //fine
        using (C c = new C()) { } //fine
    }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(545331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545331")]
        [Fact]
        public void MissingIDisposable()
        {
            var source = @"
class C
{
    void M()
    {
        using (var v = null) ;
    }
}";

            CreateCompilation(source).VerifyDiagnostics(
                // Related to the using statement:

                // (6,30): warning CS0642: Possible mistaken empty statement
                //         using (var v = null) ;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"),

                // Cascading from the lack of mscorlib:

                // (2,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                // class C
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "C").WithArguments("System.Object"),
                // (4,5): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     void M()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void"),
                // (6,16): error CS0518: Predefined type 'System.Object' is not defined or imported
                //         using (var v = null) ;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "var").WithArguments("System.Object"),
                // (6,20): error CS0815: Cannot assign <null> to an implicitly-typed variable
                //         using (var v = null) ;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "v = null").WithArguments("<null>"),
                // (2,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // class C
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "C").WithArguments("object", "0")
                );
        }

        #region help method

        private UsingStatementSyntax GetUsingStatements(CSharpCompilation compilation, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();
            return usingStatements[index - 1];
        }

        private IEnumerable VerifyDeclaredSymbolForUsingStatements(CSharpCompilation compilation, int index = 1, params string[] variables)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();
            var i = 0;
            foreach (var x in usingStatements[index - 1].Declaration.Variables)
            {
                var symbol = model.GetDeclaredSymbol(x);
                Assert.Equal(SymbolKind.Local, symbol.Kind);
                Assert.Equal(variables[i++].ToString(), symbol.ToDisplayString());
                yield return symbol;
            }
        }

        private SymbolInfo VerifySymbolInfoForUsingStatements(CSharpCompilation compilation, Symbol symbol, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();

            var type = model.GetSymbolInfo(usingStatements[index - 1].Declaration.Type);

            Assert.Equal(symbol, type.Symbol);

            return type;
        }

        private ISymbol VerifyLookUpSymbolForUsingStatements(CSharpCompilation compilation, Symbol symbol, int index = 1)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var usingStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<UsingStatementSyntax>().ToList();

            var actualSymbol = model.LookupSymbols(usingStatements[index - 1].SpanStart, name: symbol.Name).Single();
            Assert.Equal(SymbolKind.Local, actualSymbol.Kind);
            Assert.Equal(symbol, actualSymbol);
            return actualSymbol;
        }

        #endregion
    }
}
