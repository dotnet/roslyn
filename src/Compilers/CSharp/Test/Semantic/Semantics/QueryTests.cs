// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class QueryTests : CompilingTestBase
    {
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void DegenerateQueryExpression()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i;
        if (ReferenceEquals(c, r)) throw new Exception();
        // List1<int> r = c.Select(i => i);
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void QueryContinuation()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i into q select q;
        if (ReferenceEquals(c, r)) throw new Exception();
        // List1<int> r = c.Select(i => i);
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Select()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from i in c select i+1;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[2, 3, 4, 5, 6, 7, 8]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupBy01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        var r = from i in c group i by i % 2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[1, 3, 5, 7], 0:[2, 4, 6]]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupBy02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(1, 2, 3, 4, 5, 6, 7);
        var r = from i in c group 10+i by i % 2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[11, 13, 15, 17], 0:[12, 14, 16]]");
        }

        [Fact]
        public void Cast()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<object> c = new List1<object>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from int i in c select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4, 5, 6, 7]");
        }

        [Fact]
        public void Where()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<object> c = new List1<object>(1, 2, 3, 4, 5, 6, 7);
        List1<int> r = from int i in c where i < 5 select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1, 2, 3, 4]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void FromJoinSelect()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(10, 30, 40, 50, 60, 70);
        List1<int> r = from x1 in c1
                      join x2 in c2 on x1 equals x2/10
                      select x1+x2;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 33, 44, 55, 77]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OrderBy()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(28, 51, 27, 84, 27, 27, 72, 64, 55, 46, 39);
        var r =
            from i in c
            orderby i/10 descending, i%10
            select i;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[84, 72, 64, 51, 55, 46, 39, 27, 27, 27, 28]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void GroupJoin()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);
        List1<string> r =
            from x1 in c1
            join x2 in c2 on x1 equals x2 / 10 into g
            select x1 + "":"" + g.ToString();
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52], 7:[75]]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SelectMany01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> c2 = new List1<int>(10, 20, 30);
        List1<int> r = from x in c1 from y in c2 select x + y;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 21, 31, 12, 22, 32, 13, 23, 33]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void SelectMany02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> c2 = new List1<int>(10, 20, 30);
        List1<int> r = from x in c1 from int y in c2 select x + y;
        Console.WriteLine(r);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 21, 31, 12, 22, 32, 13, 23, 33]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Let01()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3);
        List1<int> r1 =
            from int x in c1
            let g = x * 10
            let z = g + x*100
            select x + z;
        System.Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[111, 222, 333]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_FromLet()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C c3 = new C(100, 200, 300);
        C r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            let g = x + y + z
            where (x + y / 10 + z / 100) < 6
            select g;
       Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[111, 211, 311, 121, 221, 131, 112, 212, 122, 113]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_Join01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C r1 =
            from int x in c1
            join y in c2 on x equals y/10
            let z = x+y
            select z;
        Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[11, 22, 33]");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TransparentIdentifiers_Join02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);
        List1<string> r1 = from x1 in c1
                      join x2 in c2 on x1 equals x2 / 10 into g
                      where x1 < 7
                      select x1 + "":"" + g.ToString();
        Console.WriteLine(r1);
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52]]");
        }

        [Fact]
        public void CodegenBug()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c1 = new List1<int>(1, 2, 3, 4, 5, 7);
        List1<int> c2 = new List1<int>(12, 34, 42, 51, 52, 66, 75);

        List1<Tuple<int, List1<int>>> r1 =
            c1
            .GroupJoin(c2, x1 => x1, x2 => x2 / 10, (x1, g) => new Tuple<int, List1<int>>(x1, g))
            ;

        Func1<Tuple<int, List1<int>>, bool> condition = (Tuple<int, List1<int>> TR1) => TR1.Item1 < 7;
        List1<Tuple<int, List1<int>>> r2 =
            r1
            .Where(condition)
            ;
        Func1<Tuple<int, List1<int>>, string> map = (Tuple<int, List1<int>> TR1) => TR1.Item1.ToString() + "":"" + TR1.Item2.ToString();

        List1<string> r3 =
            r2
            .Select(map)
            ;
        string r4 = r3.ToString();
        Console.WriteLine(r4);
        return;
    }
}";
            CompileAndVerify(csSource, expectedOutput: "[1:[12], 2:[], 3:[34], 4:[42], 5:[51, 52]]");
        }

        [Fact]
        public void RangeVariables01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C c3 = new C(100, 200, 300);
        C r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateCompilationWithMscorlib(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[3].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.NotEqual(MethodKind.ReducedExtension, ((IMethodSymbol)info0.CastInfo.Symbol).MethodKind);
            Assert.Null(info0.OperationInfo.Symbol);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            var y = model.GetDeclaredSymbol(q.Body.Clauses[0]);
            Assert.Equal(SymbolKind.RangeVariable, y.Kind);
            Assert.Equal("y", y.Name);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.NotEqual(MethodKind.ReducedExtension, ((IMethodSymbol)info1.OperationInfo.Symbol).MethodKind);

            var info2 = model.GetQueryClauseInfo(q.Body.Clauses[1]);
            var z = model.GetDeclaredSymbol(q.Body.Clauses[1]);
            Assert.Equal(SymbolKind.RangeVariable, z.Kind);
            Assert.Equal("z", z.Name);
            Assert.Equal("Cast", info2.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info2.OperationInfo.Symbol.Name);

            var info3 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.NotNull(info3);
            // what about info3's contents ???

            var xPyPz = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression as BinaryExpressionSyntax;
            var xPy = xPyPz.Left as BinaryExpressionSyntax;
            Assert.Equal(x, model.GetSemanticInfoSummary(xPy.Left).Symbol);
            Assert.Equal(y, model.GetSemanticInfoSummary(xPy.Right).Symbol);
            Assert.Equal(z, model.GetSemanticInfoSummary(xPyPz.Right).Symbol);
        }

        [Fact]
        public void RangeVariables02()
        {
            var csSource = @"
using System;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        var c1 = new int[] {1, 2, 3};
        var c2 = new int[] {10, 20, 30};
        var c3 = new int[] {100, 200, 300};
        var r1 =
            from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateCompilationWithMscorlib(csSource, new[] { LinqAssemblyRef });
            foreach (var dd in compilation.GetDiagnostics()) Console.WriteLine(dd);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[3].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Equal(MethodKind.ReducedExtension, ((IMethodSymbol)info0.CastInfo.Symbol).MethodKind);
            Assert.Null(info0.OperationInfo.Symbol);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            var y = model.GetDeclaredSymbol(q.Body.Clauses[0]);
            Assert.Equal(SymbolKind.RangeVariable, y.Kind);
            Assert.Equal("y", y.Name);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal(MethodKind.ReducedExtension, ((IMethodSymbol)info1.OperationInfo.Symbol).MethodKind);

            var info2 = model.GetQueryClauseInfo(q.Body.Clauses[1]);
            var z = model.GetDeclaredSymbol(q.Body.Clauses[1]);
            Assert.Equal(SymbolKind.RangeVariable, z.Kind);
            Assert.Equal("z", z.Name);
            Assert.Equal("Cast", info2.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info2.OperationInfo.Symbol.Name);

            var info3 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.NotNull(info3);
            // what about info3's contents ???

            var xPyPz = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression as BinaryExpressionSyntax;
            var xPy = xPyPz.Left as BinaryExpressionSyntax;
            Assert.Equal(x, model.GetSemanticInfoSummary(xPy.Left).Symbol);
            Assert.Equal(y, model.GetSemanticInfoSummary(xPy.Right).Symbol);
            Assert.Equal(z, model.GetSemanticInfoSummary(xPyPz.Right).Symbol);
        }

        [Fact]
        public void TestGetSemanticInfo01()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
        C r1 =
            from int x in c1
            from int y in c2
            select x + y;
       Console.WriteLine(r1);
    }
}";
            var compilation = CreateCompilationWithMscorlib(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[2].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("x", model.GetDeclaredSymbol(q.FromClause).Name);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal("y", model.GetDeclaredSymbol(q.Body.Clauses[0]).Name);

            var info2 = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            // what about info2's contents?
        }

        [Fact]
        public void TestGetSemanticInfo02()
        {
            var csSource = LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        List1<int> c = new List1<int>(28, 51, 27, 84, 27, 27, 72, 64, 55, 46, 39);
        var r =
            from i in c
            orderby i/10 descending, i%10
            select i;
        Console.WriteLine(r);
    }
}";
            var compilation = CreateCompilationWithMscorlib(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            dynamic methodM = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = methodM.Body.Statements[1].Declaration.Variables[0].Initializer.Value;

            var info0 = model.GetQueryClauseInfo(q.FromClause);
            Assert.Null(info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("i", model.GetDeclaredSymbol(q.FromClause).Name);
            var i = model.GetDeclaredSymbol(q.FromClause);

            var info1 = model.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Null(info1.CastInfo.Symbol);
            Assert.Null(info1.OperationInfo.Symbol);
            Assert.Null(model.GetDeclaredSymbol(q.Body.Clauses[0]));

            var order = q.Body.Clauses[0] as OrderByClauseSyntax;
            var oinfo0 = model.GetSemanticInfoSummary(order.Orderings[0]);
            Assert.Equal("OrderByDescending", oinfo0.Symbol.Name);

            var oinfo1 = model.GetSemanticInfoSummary(order.Orderings[1]);
            Assert.Equal("ThenBy", oinfo1.Symbol.Name);
        }

        [WorkItem(541774, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541774")]
        [Fact]
        public void MultipleFromClauseIdentifierInExprNotInContext()
        {
            var csSource = @"
class Program
{
    static void Main(string[] args)
    {
        var q2 = from n1 in nums 
                 from n2 in nums
                 select n1;
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(csSource, parseOptions: TestOptions.Regular).VerifyDiagnostics(
            // (6,29): error CS0103: The name 'nums' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nums").WithArguments("nums")
                );
        }

        [WorkItem(541906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541906")]
        [Fact]
        public void NullLiteralFollowingJoinInQuery()
        {
            var csSource = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var query = from int i in new int[]{ 1 } join null on true equals true select i; //CS1031
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(csSource, parseOptions: TestOptions.Regular).VerifyDiagnostics(
                // (8,55): error CS1031: Type expected
                //         var query = from int i in new int[]{ 1 } join null on true equals true select i; //CS1031
                Diagnostic(ErrorCode.ERR_TypeExpected, "null"),
                // (8,55): error CS1001: Identifier expected
                //         var query = from int i in new int[]{ 1 } join null on true equals true select i; //CS1031
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "null"),
                // (8,55): error CS1003: Syntax error, 'in' expected
                //         var query = from int i in new int[]{ 1 } join null on true equals true select i; //CS1031
                Diagnostic(ErrorCode.ERR_SyntaxError, "null").WithArguments("in", "null")
                );
        }

        [WorkItem(541779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541779")]
        [Fact]
        public void MultipleFromClauseQueryExpr()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var nums = new int[] { 3, 4 };

        var q2 = from int n1 in nums 
                 from int n2 in nums
                 select n1;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 3 4 4");
        }

        [WorkItem(541782, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541782")]
        [Fact]
        public void FromSelectQueryExprOnArraysWithTypeImplicit()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var nums = new int[] { 3, 4 };

        var q2 = from n1 in nums select n1;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";
            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 4");
        }


        [WorkItem(541788, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541788")]
        [Fact]
        public void JoinClauseTest()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var q2 =
           from a in Enumerable.Range(1, 13)
           join b in Enumerable.Range(1, 13) on 4 * a equals b
           select a;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "1 2 3");
        }

        [WorkItem(541789, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541789")]
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void WhereClauseTest()
        {
            var csSource = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                where (x > 2)
                select x;

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "3 4");
        }

        [WorkItem(541942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541942")]
        [Fact]
        public void WhereDefinedInType()
        {
            var csSource = @"
using System;

class Y
{
    public int Where(Func<int, bool> predicate)
    {
        return 45;
    }
}

class P
{
    static void Main()
    {
        var src = new Y();
        var query = from x in src
                where x > 0
                select x;

        Console.Write(query);
    }
}";

            CompileAndVerify(csSource, additionalRefs: new[] { LinqAssemblyRef }, expectedOutput: "45");
        }

        [Fact]
        public void GetInfoForSelectExpression01()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            SelectClauseSyntax selectClause = (SelectClauseSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("select", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(selectClause.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            var info2 = semanticModel.GetSemanticInfoSummary(selectClause);
            var m = (MethodSymbol)info2.Symbol;
            Assert.Equal("Select", m.ReducedFrom.Name);
        }

        [Fact]
        public void GetInfoForSelectExpression02()
        {
            string sourceCode = @"
using System;
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x into w
                 select w;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            SelectClauseSyntax selectClause = (SelectClauseSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("select w", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(selectClause.Expression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
        }

        [Fact]
        public void GetInfoForSelectExpression03()
        {
            string sourceCode = @"
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x+1 into w
                 select w+1;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            compilation.VerifyDiagnostics();
            var semanticModel = compilation.GetSemanticModel(tree);

            var e = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("x+1", StringComparison.Ordinal)).Parent;
            var info = semanticModel.GetSemanticInfoSummary(e);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            Assert.Equal("x", info.Symbol.Name);

            e = (IdentifierNameSyntax)tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf("w+1", StringComparison.Ordinal)).Parent;
            info = semanticModel.GetSemanticInfoSummary(e);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
            Assert.Equal(SymbolKind.RangeVariable, info.Symbol.Kind);
            Assert.Equal("w", info.Symbol.Name);

            var e2 = e.Parent as ExpressionSyntax; // w+1
            var info2 = semanticModel.GetSemanticInfoSummary(e2);
            Assert.Equal(SpecialType.System_Int32, info2.Type.SpecialType);
            Assert.Equal("System.Int32 System.Int32.op_Addition(System.Int32 left, System.Int32 right)", info2.Symbol.ToTestDisplayString());
        }

        [WorkItem(541806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541806")]
        [Fact]
        public void GetDeclaredSymbolForQueryContinuation()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select x into w
                 select w;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var queryContinuation = tree.GetRoot().FindToken(sourceCode.IndexOf("into w", StringComparison.Ordinal)).Parent;
            var symbol = semanticModel.GetDeclaredSymbol(queryContinuation);

            Assert.NotNull(symbol);
            Assert.Equal("w", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
        }

        [WorkItem(541899, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541899")]
        [Fact]
        public void ComputeQueryVariableType()
        {
            string sourceCode = @"
using System.Linq;
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = from x in nums
                 select 5;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var selectExpression = tree.GetCompilationUnitRoot().FindToken(sourceCode.IndexOf('5'));
            var info = semanticModel.GetSpeculativeTypeInfo(selectExpression.SpanStart, SyntaxFactory.ParseExpression("x"), SpeculativeBindingOption.BindAsExpression);
            Assert.Equal(SpecialType.System_Int32, info.Type.SpecialType);
        }

        [WorkItem(541893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541893")]
        [Fact]
        public void GetDeclaredSymbolForJoinIntoClause()
        {
            string sourceCode = @"
using System;
using System.Linq;

static class Test
{
    static void Main()
    {
        var qie = from x3 in new int[] { 0 }
                      join x7 in (new int[] { 0 }) on 5 equals 5 into x8
                      select x8;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var joinInto = tree.GetRoot().FindToken(sourceCode.IndexOf("into x8", StringComparison.Ordinal)).Parent;
            var symbol = semanticModel.GetDeclaredSymbol(joinInto);

            Assert.NotNull(symbol);
            Assert.Equal("x8", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
            Assert.Equal("? x8", symbol.ToTestDisplayString());
        }

        [WorkItem(541982, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541982")]
        [WorkItem(543494, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543494")]
        [Fact()]
        public void GetDeclaredSymbolAddAccessorDeclIncompleteQuery()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new[] { 1, 2, 3, 4, 5 };

        var query1 = from  event in expr1 select event;
        var query2 = from int
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var unknownAccessorDecls = tree.GetCompilationUnitRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>();
            var symbols = unknownAccessorDecls.Select(decl => semanticModel.GetDeclaredSymbol(decl));

            Assert.True(symbols.All(s => ReferenceEquals(s, null)));
        }

        [WorkItem(542235, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542235")]
        [Fact]
        public void TwoFromClauseFollowedBySelectClause()
        {
            string sourceCode = @"
using System.Linq;

class Test
{
    public static void Main()
    {

        var q2 = from num1 in new int[] { 4, 5 }
                 from num2 in new int[] { 4, 5 }
                 select num1;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var fromClause1 = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => (n.IsKind(SyntaxKind.FromClause)) && (n.ToString().Contains("num1"))).Single() as FromClauseSyntax;
            var fromClause2 = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => (n.IsKind(SyntaxKind.FromClause)) && (n.ToString().Contains("num2"))).Single() as FromClauseSyntax;

            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);
            var queryInfoForFrom1 = semanticModel.GetQueryClauseInfo(fromClause1);
            var queryInfoForFrom2 = semanticModel.GetQueryClauseInfo(fromClause2);

            Assert.Null(queryInfoForFrom1.CastInfo.Symbol);
            Assert.Null(queryInfoForFrom1.OperationInfo.Symbol);

            Assert.Null(queryInfoForFrom2.CastInfo.Symbol);
            Assert.Equal("SelectMany", queryInfoForFrom2.OperationInfo.Symbol.Name);

            Assert.Null(symbolInfoForSelect.Symbol);
            Assert.Empty(symbolInfoForSelect.CandidateSymbols);
            Assert.Equal(CandidateReason.None, symbolInfoForSelect.CandidateReason);
        }

        [WorkItem(528747, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528747")]
        [Fact]
        public void SemanticInfoForOrderingClauses()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var q1 =
            from x in new int[] { 4, 5 }
            orderby
                x descending,
                x.ToString() ascending,
                x descending
            select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            int count = 0;
            string[] names = { "OrderByDescending", "ThenBy", "ThenByDescending" };
            foreach (var ordering in tree.GetCompilationUnitRoot().DescendantNodes().OfType<OrderingSyntax>())
            {
                var symbolInfo = model.GetSemanticInfoSummary(ordering);
                Assert.Equal(names[count++], symbolInfo.Symbol.Name);
            }
            Assert.Equal(3, count);
        }

        [WorkItem(542266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542266")]
        [Fact]
        public void FromOrderBySelectQueryTranslation()
        {
            string sourceCode = @"
using System;
using System.Collections;
using System.Collections.Generic;

public interface IOrderedEnumerable<TElement> : IEnumerable<TElement>,
    IEnumerable
{
}

public static class Extensions
{
    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
    this IEnumerable<TSource> source,
    Func<TSource, TKey> keySelector)

    {
        return null;
    }

    public static IEnumerable<TResult> Select<TSource, TResult>(
    this IEnumerable<TSource> source,
    Func<TSource, TResult> selector)

    {
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {        

        var q1 = from num in new int[] { 4, 5 }
                 orderby num
                 select num;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);

            Assert.Null(symbolInfoForSelect.Symbol);
        }

        [WorkItem(528756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528756")]
        [Fact]
        public void FromWhereSelectTranslation()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;

public static class Extensions
{

    public static IEnumerable<TSource> Where<TSource>(
    this IEnumerable<TSource> source,
    Func<TSource, bool> predicate)
    {
        return null;
    }
}

class Program
{
    static void Main(string[] args)
    {

        var q1 = from num in System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)
                 select num;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            semanticModel.GetDiagnostics().Verify(
                // (21,30): error CS1935: Could not find an implementation of the query pattern for source type 'System.Collections.Generic.IEnumerable<int>'.  'Select' not found.  Are you missing a reference to 'System.Core.dll' or a using directive for 'System.Linq'?
                //         var q1 = from num in System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)
                Diagnostic(ErrorCode.ERR_QueryNoProviderStandard, "System.Linq.Enumerable.Range(4, 5).Where(n => n > 10)").WithArguments("System.Collections.Generic.IEnumerable<int>", "Select"));
        }

        [WorkItem(528760, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528760")]
        [Fact]
        public void FromJoinSelectTranslation()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q1 = from num in new int[] { 4, 5 }
                 join x1 in new int[] { 4, 5 } on num equals x1
                 select x1 + 5;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var selectClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.SelectClause)).Single() as SelectClauseSyntax;
            var symbolInfoForSelect = semanticModel.GetSemanticInfoSummary(selectClause);

            Assert.Null(symbolInfoForSelect.Symbol);
        }

        [WorkItem(528761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528761")]
        [WorkItem(544585, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544585")]
        [Fact]
        public void OrderingSyntaxWithOverloadResolutionFailure()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int[] numbers = new int[] { 4, 5 };

        var q1 = from num in numbers.Single()
                 orderby (x1) => x1.ToString()
                 select num;
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (10,30): error CS1936: Could not find an implementation of the query pattern for source type 'int'.  'OrderBy' not found.
                //         var q1 = from num in numbers.Single()
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "numbers.Single()").WithArguments("int", "OrderBy")
                );
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var orderingClause = tree.GetCompilationUnitRoot().DescendantNodes().Where(n => n.IsKind(SyntaxKind.AscendingOrdering)).Single() as OrderingSyntax;
            var symbolInfoForOrdering = semanticModel.GetSemanticInfoSummary(orderingClause);

            Assert.Null(symbolInfoForOrdering.Symbol);
        }

        [WorkItem(542292, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542292")]
        [Fact]
        public void EmitIncompleteQueryWithSyntaxErrors()
        {
            string sourceCode = @"
using System.Linq;

class Program
{
    static int Main()
    {
        int [] foo = new int [] {1};
        var q = from x in foo
                select x + 1 into z
                    select z.T
";
            using (var output = new MemoryStream())
            {
                Assert.False(CreateCompilationWithMscorlibAndSystemCore(sourceCode).Emit(output).Success);
            }
        }

        [WorkItem(542294, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542294")]
        [Fact]
        public void EmitQueryWithBindErrors()
        {
            string sourceCode = @"
using System.Linq;
class Program
{
    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        var query = from num in nums
                    let num = 3 // CS1930
                    select num; 
    }
}";
            using (var output = new MemoryStream())
            {
                Assert.False(CreateCompilationWithMscorlibAndSystemCore(sourceCode).Emit(output).Success);
            }
        }

        [WorkItem(542372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542372")]
        [Fact]
        public void BindToIncompleteSelectManyDecl()
        {
            string sourceCode = @"
class P
{
    static C<X> M2<X>(X x)
    {
        return new C<X>(x);
    }

    static void Main()
    {
        C<int> e1 = new C<int>(1);

        var q = from x1 in M2<int>(x1)
                from x2 in e1
                select x1;
    }
}

class C<T>
{
    public C<V> SelectMany";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);

            var diags = semanticModel.GetDiagnostics();

            Assert.NotEmpty(diags);
        }

        [WorkItem(542419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542419")]
        [Fact]
        public void BindIdentifierInWhereErrorTolerance()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var r = args.Where(b => b < > );
        var q = from a in args
                where a <> 
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var diags = semanticModel.GetDiagnostics();
            Assert.NotEmpty(diags);
        }

        [WorkItem(542460, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542460")]
        [Fact]
        public void QueryWithMultipleParseErrorsAndScriptParseOption()
        {
            string sourceCode = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new int[] { 1, 2, 3, 4, 5 };

        var query2 = from int namespace in expr1 select namespace;
        var query25 = from i in expr1 let namespace = expr1 select i;
    }
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Where(x => x.ToFullString() == "from i in expr1 let ").Single();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);

            Assert.Null(symbolInfo.Symbol);
        }

        [WorkItem(542496, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542496")]
        [Fact]
        public void QueryExpressionInFieldInitReferencingAnotherFieldWithScriptParseOption()
        {
            string sourceCode = @"
using System.Linq;
using System.Collections;

class P
{
    double one = 1;

    public IEnumerable e = 
               from x in new int[] { 1, 2, 3 }
               select x + one;
}";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode, parseOptions: TestOptions.Script);
            var tree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(tree);
            var queryExpr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<QueryExpressionSyntax>().Single();
            var symbolInfo = semanticModel.GetSemanticInfoSummary(queryExpr);

            Assert.Null(symbolInfo.Symbol);
        }

        [WorkItem(542559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542559")]
        [Fact]
        public void StaticTypeInFromClause()
        {
            string sourceCode = @"
using System;
using System.Linq;

class C
{
    static void Main()
    {
        var q2 = string.Empty.Cast<GC>().Select(x => x);
        var q1 = from GC x in string.Empty select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (9,18): error CS0718: 'GC': static types cannot be used as type arguments
                //         var q2 = string.Empty.Cast<GC>().Select(x => x);
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "string.Empty.Cast<GC>").WithArguments("System.GC").WithLocation(9, 18),
                // (10,18): error CS0718: 'GC': static types cannot be used as type arguments
                //         var q1 = from GC x in string.Empty select x;
                Diagnostic(ErrorCode.ERR_GenericArgIsStaticClass, "from GC x in string.Empty").WithArguments("System.GC").WithLocation(10, 18)
                );
        }

        [WorkItem(542560, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542560")]
        [Fact]
        public void MethodGroupInFromClause()
        {
            string sourceCode = @"
class Program
{
    static void Main()
    {
        var q1 = from y in Main select y;
        var q2 = Main.Select(y => y);
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (6,28): error CS0119: 'Program.Main()' is a method, which is not valid in the given context
                //         var q1 = from y in Main select y;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Main").WithArguments("Program.Main()", "method").WithLocation(6, 28),
                // (7,18): error CS0119: 'Program.Main()' is a method, which is not valid in the given context
                //         var q2 = Main.Select(y => y);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Main").WithArguments("Program.Main()", "method").WithLocation(7, 18)
                );
        }

        [WorkItem(542558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542558")]
        [Fact]
        public void SelectFromType01()
        {
            string sourceCode = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = from x in C select x;
    }

    static IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "C").Single();
            dynamic main = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = main.Body.Statements[0].Declaration.Variables[0].Initializer.Value;
            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal(null, info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            var infoSelect = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.Equal("Select", infoSelect.Symbol.Name);
        }

        [WorkItem(542558, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542558")]
        [Fact]
        public void SelectFromType02()
        {
            string sourceCode = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = from x in C select x;
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "C").Single();
            dynamic main = (MethodDeclarationSyntax)classC.Members[0];
            QueryExpressionSyntax q = main.Body.Statements[0].Declaration.Variables[0].Initializer.Value;
            var info0 = model.GetQueryClauseInfo(q.FromClause);
            var x = model.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);
            Assert.Equal(null, info0.CastInfo.Symbol);
            Assert.Null(info0.OperationInfo.Symbol);
            var infoSelect = model.GetSemanticInfoSummary(q.Body.SelectOrGroup);
            Assert.Equal("Select", infoSelect.Symbol.Name);
        }

        [WorkItem(542624, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542624")]
        [Fact]
        public void QueryColorColor()
        {
            string sourceCode = @"
using System;
using System.Collections.Generic;

class Color
{
    public static IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}

class Flavor
{
    public IEnumerable<T> Select<T>(Func<int, T> f) { return null; }
}

class Program
{
    Color Color;
    static Flavor Flavor;
    static void Main()
    {
        var q1 = from x in Color select x;
        var q2 = from x in Flavor select x;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (17,11): warning CS0169: The field 'Program.Color' is never used
                //     Color Color;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "Color").WithArguments("Program.Color"),
                // (18,19): warning CS0649: Field 'Program.Flavor' is never assigned to, and will always have its default value null
                //     static Flavor Flavor;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "Flavor").WithArguments("Program.Flavor", "null")
            );
        }

        [WorkItem(542704, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542704")]
        [Fact]
        public void QueryOnSourceWithGroupByMethod()
        {
            string source = @"
delegate T Func<A, T>(A a);

class Y<U>
{
    public U u;
    public Y(U u)
    {
        this.u = u;
    }

    public string GroupBy(Func<U, string> keySelector)
    {
        return null;
    }
}

class Test
{
    static int Main()
    {
        Y<int> src = new Y<int>(2);
        string q1 = src.GroupBy(x => x.GetType().Name); // ok
        string q2 = from x in src group x by x.GetType().Name; // Roslyn CS1501
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void RangeTypeAlreadySpecified()
        {
            string sourceCode =
@"using System.Linq;
using System.Collections;

static class Test
{
    public static void Main2()
    {
        var list = new CastableToArrayList();
        var q = from int x in list
                select x + 1;
    }
}

class CastableToArrayList
{
    public ArrayList Cast<T>() { return null; }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (9,31): error CS1936: Could not find an implementation of the query pattern for source type 'System.Collections.ArrayList'.  'Select' not found.
                //         var q = from int x in list
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "list").WithArguments("System.Collections.ArrayList", "Select")
                );
        }

        [WorkItem(11414, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InvalidQueryWithAnonTypesAndKeywords()
        {
            string source = @"
public class QueryExpressionTest
{
    public static void Main()
    {
        var query7 = from  i in expr1 join  const in expr2 on i equals const select new { i, const };
        var query8 = from int i in expr1  select new { i, const };
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            Assert.NotEmpty(compilation.GetDiagnostics());
        }

        [WorkItem(543787, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543787")]
        [ClrOnlyFact]
        public void GetSymbolInfoOfSelectNodeWhenTypeOfRangeVariableIsErrorType()
        {
            string source = @"
using System.Linq;

class Test
{
    static void V()
    {
    }

    public static int Main()
    {
        var e1 = from i in V() select i;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            var tree = compilation.SyntaxTrees.First();
            var index = source.IndexOf("select i", StringComparison.Ordinal);
            var selectNode = tree.GetCompilationUnitRoot().FindToken(index).Parent as SelectClauseSyntax;
            var model = compilation.GetSemanticModel(tree);
            var symbolInfo = model.GetSymbolInfo(selectNode);
            Assert.NotNull(symbolInfo);
            Assert.Null(symbolInfo.Symbol); // there is no select method to call because the receiver is bad
            var typeInfo = model.GetTypeInfo(selectNode);
            Assert.Equal(SymbolKind.ErrorType, typeInfo.Type.Kind);
        }

        [WorkItem(543790, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543790")]
        [Fact]
        public void GetQueryClauseInfoForQueryWithSyntaxErrors()
        {
            string source = @"
using System.Linq;

class Test
{
	public static void Main ()
	{
        var query8 = from int i in expr1 join int delegate in expr2 on i equals delegate select new { i, delegate };
	}
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            var tree = compilation.SyntaxTrees.First();
            var index = source.IndexOf("join int delegate in expr2 on i equals delegate", StringComparison.Ordinal);
            var joinNode = tree.GetCompilationUnitRoot().FindToken(index).Parent as JoinClauseSyntax;
            var model = compilation.GetSemanticModel(tree);
            var queryInfo = model.GetQueryClauseInfo(joinNode);

            Assert.NotNull(queryInfo);
        }

        [WorkItem(545797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545797")]
        [Fact]
        public void QueryOnNull()
        {
            string source = @"using System;
static class C
{
    static void Main()
    {
        var q = from x in null select x;
    }

    static object Select(this object x, Func<int, int> y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS0186: Use of null is not valid in this context
                //         var q = from x in null select x;
                Diagnostic(ErrorCode.ERR_NullNotValid, "select x")
                );
        }

        [WorkItem(545797, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545797")]
        [Fact]
        public void QueryOnLambda()
        {
            string source = @"using System;
static class C
{
    static void Main()
    {
        var q = from x in y=>y select x;
    }

    static object Select(this object x, Func<int, int> y)
    {
        return null;
    }
}
";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (6,32): error CS1936: Could not find an implementation of the query pattern for source type 'anonymous method'.  'Select' not found.
                //         var q = from x in y=>y select x;
                Diagnostic(ErrorCode.ERR_QueryNoProvider, "select x").WithArguments("anonymous method", "Select")
                );
        }

        [WorkItem(545444, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545444")]
        [Fact]
        public void RefOmittedOnComCall()
        {
            string source = @"using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

[ComImport]
[Guid(""A88A175D-2448-447A-B786-64682CBEF156"")]
public interface IRef1
{
    int M(ref int x, int y);
}

public class Ref1Impl : IRef1
{
    public int M(ref int x, int y) { return x + y; }
}

class Test
{
   public static void Main()
   {
       IRef1 ref1 = new Ref1Impl();
       Expression<Func<int, int, int>> F = (x, y) => ref1.M(x, y);
   }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (22,54): error CS2037: An expression tree lambda may not contain a COM call with ref omitted on arguments
                //        Expression<Func<int, int, int>> F = (x, y) => ref1.M(x, y);
                Diagnostic(ErrorCode.ERR_ComRefCallInExpressionTree, "ref1.M(x, y)")
                );
        }

        [Fact, WorkItem(5728, "https://github.com/dotnet/roslyn/issues/5728")]
        public void RefOmittedOnComCallErr()
        {
            string source = @"
using System;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

[ComImport]
[Guid(""A88A175D-2448-447A-B786-64682CBEF156"")]
public interface IRef1
{
    long M(uint y, ref int x, int z);
    long M(uint y, ref int x, int z, int q);
}

public class Ref1Impl : IRef1
{
    public long M(uint y, ref int x, int z) { return x + y; }
    public long M(uint y, ref int x, int z, int q) { return x + y; }
}

class Test1
{
    static void Test(Expression<Action<IRef1>> e)
    {

    }

    static void Test<U>(Expression<Func<IRef1, U>> e)
    {

    }

    public static void Main()
    {
        Test(ref1 => ref1.M(1, ));
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
    // (34,32): error CS1525: Invalid expression term ')'
    //         Test(ref1 => ref1.M(1, ));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(34, 32)
                );
        }


        [WorkItem(529350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529350")]
        [Fact]
        public void BindLambdaBodyWhenError()
        {
            string source =
@"using System.Linq;

class A
{
    static void Main()
    {
    }
    static void M(System.Reflection.Assembly[] a)
    {
        var q2 = a.SelectMany(assem2 => assem2.UNDEFINED, (assem2, t) => t);

        var q1 = from assem1 in a
                 from t in assem1.UNDEFINED
                 select t;
    }
}";
            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (10,48): error CS1061: 'System.Reflection.Assembly' does not contain a definition for 'UNDEFINED' and no extension method 'UNDEFINED' accepting a first argument of type 'System.Reflection.Assembly' could be found (are you missing a using directive or an assembly reference?)
                //         var q2 = a.SelectMany(assem2 => assem2.UNDEFINED, (assem2, t) => t);
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "UNDEFINED").WithArguments("System.Reflection.Assembly", "UNDEFINED"),
                // (13,35): error CS1061: 'System.Reflection.Assembly' does not contain a definition for 'UNDEFINED' and no extension method 'UNDEFINED' accepting a first argument of type 'System.Reflection.Assembly' could be found (are you missing a using directive or an assembly reference?)
                //                  from t in assem1.UNDEFINED
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "UNDEFINED").WithArguments("System.Reflection.Assembly", "UNDEFINED")
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var assem2 =
                tree.GetCompilationUnitRoot().DescendantNodes(n => n.ToString().Contains("assem2"))
                .Where(e => e.ToString() == "assem2")
                .OfType<ExpressionSyntax>()
                .Single();
            var typeInfo2 = model.GetTypeInfo(assem2);
            Assert.NotEqual(TypeKind.Error, typeInfo2.Type.TypeKind);
            Assert.Equal("Assembly", typeInfo2.Type.Name);

            var assem1 =
                tree.GetCompilationUnitRoot().DescendantNodes(n => n.ToString().Contains("assem1"))
                .Where(e => e.ToString() == "assem1")
                .OfType<ExpressionSyntax>()
                .Single();
            var typeInfo1 = model.GetTypeInfo(assem1);
            Assert.NotEqual(TypeKind.Error, typeInfo1.Type.TypeKind);
            Assert.Equal("Assembly", typeInfo1.Type.Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetQueryClauseInfo()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
    }
}";
            var speculatedSource = @"
        C r1 =
            from int x in c1
            from int y in c2
            select x + y;
";
            var queryStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlib(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[1].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);
            var q = (QueryExpressionSyntax)queryStatement.Declaration.Variables[0].Initializer.Value;

            var info0 = speculativeModel.GetQueryClauseInfo(q.FromClause);
            Assert.Equal("Cast", info0.CastInfo.Symbol.Name);
            Assert.Null(info0.OperationInfo.Symbol);
            Assert.Equal("x", speculativeModel.GetDeclaredSymbol(q.FromClause).Name);

            var info1 = speculativeModel.GetQueryClauseInfo(q.Body.Clauses[0]);
            Assert.Equal("Cast", info1.CastInfo.Symbol.Name);
            Assert.Equal("SelectMany", info1.OperationInfo.Symbol.Name);
            Assert.Equal("y", speculativeModel.GetDeclaredSymbol(q.Body.Clauses[0]).Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetSemanticInfoForSelectClause()
        {
            var csSource = @"
using C = List1<int>;" + LINQ + @"
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C(1, 2, 3);
        C c2 = new C(10, 20, 30);
    }
}";
            var speculatedSource = @"
        C r1 =
            from int x in c1
            select x;
";

            var queryStatement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlib(csSource);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Query").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[1].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);
            var q = (QueryExpressionSyntax)queryStatement.Declaration.Variables[0].Initializer.Value;

            var x = speculativeModel.GetDeclaredSymbol(q.FromClause);
            Assert.Equal(SymbolKind.RangeVariable, x.Kind);
            Assert.Equal("x", x.Name);

            var selectExpression = (q.Body.SelectOrGroup as SelectClauseSyntax).Expression;
            Assert.Equal(x, speculativeModel.GetSemanticInfoSummary(selectExpression).Symbol);

            var selectClauseSymbolInfo = speculativeModel.GetSymbolInfo(q.Body.SelectOrGroup);
            Assert.NotNull(selectClauseSymbolInfo.Symbol);
            Assert.Equal("Select", selectClauseSymbolInfo.Symbol.Name);

            var selectClauseTypeInfo = speculativeModel.GetTypeInfo(q.Body.SelectOrGroup);
            Assert.NotNull(selectClauseTypeInfo.Type);
            Assert.Equal("List1", selectClauseTypeInfo.Type.Name);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetDeclaredSymbolForJoinIntoClause()
        {
            string sourceCode = @"
public class Test
{
    public static void Main()
    { 
    }
}";

            var speculatedSource = @"
                  var qie = from x3 in new int[] { 0 }
                            join x7 in (new int[] { 1 }) on 5 equals 5 into x8
                            select x8;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Test").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.SpanStart, queryStatement, out speculativeModel);

            var queryExpression = (QueryExpressionSyntax)((LocalDeclarationStatementSyntax)queryStatement).Declaration.Variables[0].Initializer.Value;
            JoinIntoClauseSyntax joinInto = ((JoinClauseSyntax)queryExpression.Body.Clauses[0]).Into;
            var symbol = speculativeModel.GetDeclaredSymbol(joinInto);

            Assert.NotNull(symbol);
            Assert.Equal("x8", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
            Assert.Equal("? x8", symbol.ToTestDisplayString());
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetDeclaredSymbolForQueryContinuation()
        {
            string sourceCode = @"
public class Test2
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
    }
}";
            var speculatedSource = @"
                var q2 = from x in nums
                         select x into w
                         select w;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "Test2").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.Statements[0].Span.End, queryStatement, out speculativeModel);
            Assert.True(success);

            var queryExpression = (QueryExpressionSyntax)((LocalDeclarationStatementSyntax)queryStatement).Declaration.Variables[0].Initializer.Value;
            var queryContinuation = queryExpression.Body.Continuation;
            var symbol = speculativeModel.GetDeclaredSymbol(queryContinuation);

            Assert.NotNull(symbol);
            Assert.Equal("w", symbol.Name);
            Assert.Equal(SymbolKind.RangeVariable, symbol.Kind);
        }

        [Fact]
        public void TestSpeculativeSemanticModel_GetSymbolInfoForOrderingClauses()
        {
            string sourceCode = @"
using System.Linq; // Needed for speculative code.

public class QueryExpressionTest
{
    public static void Main()
    {
    }
}";
            var speculatedSource = @"
        var q1 =
            from x in new int[] { 4, 5 }
            orderby
                x descending,
                x.ToString() ascending,
                x descending
            select x;
";

            var queryStatement = SyntaxFactory.ParseStatement(speculatedSource);

            var compilation = CreateCompilationWithMscorlibAndSystemCore(sourceCode);
            compilation.VerifyDiagnostics(
                // (2,1): info CS8019: Unnecessary using directive.
                // using System.Linq; // Needed for speculative code.
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Linq;"));

            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var classC = tree.GetCompilationUnitRoot().ChildNodes().OfType<TypeDeclarationSyntax>().Where(t => t.Identifier.ValueText == "QueryExpressionTest").Single();
            var methodM = (MethodDeclarationSyntax)classC.Members[0];

            SemanticModel speculativeModel;
            bool success = model.TryGetSpeculativeSemanticModel(methodM.Body.SpanStart, queryStatement, out speculativeModel);
            Assert.True(success);

            int count = 0;
            string[] names = { "OrderByDescending", "ThenBy", "ThenByDescending" };
            foreach (var ordering in queryStatement.DescendantNodes().OfType<OrderingSyntax>())
            {
                var symbolInfo = speculativeModel.GetSemanticInfoSummary(ordering);
                Assert.Equal(names[count++], symbolInfo.Symbol.Name);
            }
            Assert.Equal(3, count);
        }

        [Fact]
        public void BrokenQueryPattern()
        {
            string sourceCode =
@"using System;

class Q<T>
{
    public Q<V> SelectMany<U, V>(Func<T, U> f1, Func<T, U, V> f2) { return null; }
    public Q<U> Select<U>(Func<T, U> f1) { return null; }

    //public Q<T> Where(Func<T, bool> f1) { return null; }
    public X Where(Func<T, bool> f1) { return null; }
}

class X
{
    public X Select<U>(Func<int, U> f1) { return null; }
}

class Program
{
    static void Main(string[] args)
    {
        Q<int> q = null;
        var r =
            from x in q
            from y in q
            where x.ToString() == y.ToString()
            select x.ToString();
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(sourceCode).VerifyDiagnostics(
                // (26,20): error CS8016: Transparent identifier member access failed for field 'x' of 'int'.  Does the data being queried implement the query pattern?
                //             select x.ToString();
                Diagnostic(ErrorCode.ERR_UnsupportedTransparentIdentifierAccess, "x").WithArguments("x", "int")
                );
        }
    }
}
