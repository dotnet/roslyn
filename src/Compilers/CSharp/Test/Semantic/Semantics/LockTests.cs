// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Tests related to binding (but not lowering) lock statements.
    /// </summary>
    public class LockTests : CompilingTestBase
    {
        [Fact]
        public void SemanticModel()
        {
            var source = @"
class C
{
    static void Main()
    {
        object o = null; //this makes no sense, but we're only testing binding
        lock (o)
        {
            o.ToString();
        }
    }
}
";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var localDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var localSymbol = (ILocalSymbol)model.GetDeclaredSymbol(localDecl.Declaration.Variables.Single());
            Assert.Equal("o", localSymbol.Name);
            Assert.Equal(SpecialType.System_Object, localSymbol.Type.SpecialType);

            var lockStatement = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LockStatementSyntax>().Single();
            var lockExprInfo = model.GetSymbolInfo(lockStatement.Expression);
            Assert.NotEqual(default, lockExprInfo);
            Assert.Equal(localSymbol, lockExprInfo.Symbol);

            var memberAccessExpression = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
            var memberAccessInfo = model.GetSymbolInfo(memberAccessExpression.Expression);
            Assert.NotEqual(default, memberAccessInfo);
            Assert.Equal(localSymbol, memberAccessInfo.Symbol);
        }

        [Fact]
        public void MethodGroup()
        {
            var source = @"
class C
{
    static void Main()
    {
        lock (Main)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0185: 'method group' is not a reference type as required by the lock statement
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "Main").WithArguments("method group"));
        }

        [Fact]
        public void Lambda()
        {
            var source = @"
class C
{
    static void Main()
    {
        lock (x => x)
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0185: 'lambda expression' is not a reference type as required by the lock statement
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "x => x").WithArguments("lambda expression"));
        }

        [Fact]
        public void Null()
        {
            var source = @"
class C
{
    static void Main()
    {
        lock (null)
        {
        }
    }
}
";
            // Dev10 allows this.
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void EmbeddedStatement()
        {
            var source = @"
class C
{
    static void Main()
    {
        object a = null, b = null, c = null;
        lock (a)
            lock (b)
                lock (c) ;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (8,53): warning CS0642: Possible mistaken empty statement
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"));
        }

        [Fact]
        public void ModifyLocalInLockExpression()
        {
            var source = @"
class C
{
    void M()
    {
        C c = null;
        lock (c)
        {
            c = null; //CS0728
            Ref(ref c); //CS0728
            this[out c] = 1; //CS0728
        }
    }

    void Ref(ref C c) { }
    int this[out C c] { set { c = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (16,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (9,13): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"),
                // (10,21): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"),
                // (11,22): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"));
        }

        [Fact]
        public void ModifyParameterInUsingExpression()
        {
            var source = @"
class C
{
    void M(C c)
    {
        lock (c)
        {
            c = null; //CS0728
            Ref(ref c); //CS0728
            this[out c] = 1; //CS0728
        }
    }

    void Ref(ref C c) { }
    int this[out C c] { set { c = null; } } //this is illegal, so if we break this test, we may need a metadata indexer
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (15,14): error CS0631: ref and out are not valid in this context
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "out"),
                // (8,13): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"),
                // (9,21): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"),
                // (10,22): warning CS0728: Possibly incorrect assignment to local 'c' which is the argument to a using or lock statement. The Dispose call or unlocking will happen on the original value of the local.
                Diagnostic(ErrorCode.WRN_AssignmentToLockOrDispose, "c").WithArguments("c"));
        }

        [Fact]
        public void RequireRefType_1()
        {
            var source = @"
class Program
{
    static int x;
    static void Main(string[] args)
    {
        lock (x)
        { }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,15): error CS0185: 'int' is not a reference type as required by the lock statement
                //         lock (x)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "x").WithArguments("int"),
                // (4,16): warning CS0649: Field 'Program.x' is never assigned to, and will always have its default value 0
                //     static int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("Program.x", "0"));
        }

        [Fact]
        public void RequireRefType_2()
        {
            var source = @"
struct Conv
{
    public void TryMe()
    {
        lock (this) { } 
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0185: 'Conv' is not a reference type as required by the lock statement
                //         lock (this) { } 
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "this").WithArguments("Conv"));
        }

        [Fact]
        public void RequireRefType_Nullable()
        {
            var source = @"
class C
{
    public void goo()
    {
        int? a = null;
        lock (a)
        {
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,15): error CS0185: 'int?' is not a reference type as required by the lock statement
                //         lock (a)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "a").WithArguments("int?"));
        }

        [Fact]
        public void PartialMethod()
        {
            var source = @"
partial class C
{
    public static void Main()
    {
        lock (PM)
        {
        }

        lock (PM(1))
        {
        }
    }
    static partial void PM(int p1);
    static partial void PM(int p1)
    {
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0185: 'method group' is not a reference type as required by the lock statement
                //         lock (PM)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "PM").WithArguments("method group"),
                // (10,15): error CS0185: 'void' is not a reference type as required by the lock statement
                //         lock (PM(1))
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "PM(1)").WithArguments("void"));
        }

        // Object could not declare in lock statement
        [Fact]
        public void ObjectDeclaredInLock()
        {
            var source = @"
class Test
{
    public static void Main()
    {
        lock (Res d = new Res ())// Invalid
        {
        }
    }
}
class Res
{
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,19): error CS1073: Unexpected token 'd'
                //         lock (Res d = new Res ())// Invalid
                Diagnostic(ErrorCode.ERR_UnexpectedToken, "d").WithArguments("d").WithLocation(6, 19));
        }

        [Fact]
        public void Lambda_1()
        {
            var source = @"
class Test
{
    public static void Main()
    {
        lock ((ref int y) => { y = y + 1; return y; })     // Invalid
        {
        }

        lock (() => { })     // Invalid
        {
        }
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,15): error CS0185: 'lambda expression' is not a reference type as required by the lock statement
                //         lock ((ref int y) => { y = y + 1; return y; })     // Invalid
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "(ref int y) => { y = y + 1; return y; }").WithArguments("lambda expression"),
                // (10,15): error CS0185: 'lambda expression' is not a reference type as required by the lock statement
                //         lock (() => { })     // Invalid
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "() => { }").WithArguments("lambda expression"));
        }

        // malformed 'lock' statement
        [Fact]
        public void MalformedLock()
        {
            var source = @"
using System.Collections.Generic;
class Test
{
    public static void Main()
    { }
    public IEnumerable<int> B(int C, int D)
    {
        lock ((C + yield return +D).ToString())
    {
            yield return C;
        };
        yield return C;
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (9,26): error CS1026: ) expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "return").WithLocation(9, 26),
                // (9,26): error CS1026: ) expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "return").WithLocation(9, 26),
                // (9,35): error CS1002: ; expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 35),
                // (9,35): error CS1513: } expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 35),
                // (9,47): error CS1002: ; expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 47),
                // (9,47): error CS1513: } expected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 47),
                // (9,20): error CS0103: The name 'yield' does not exist in the current context
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_NameNotInContext, "yield").WithArguments("yield").WithLocation(9, 20),
                // (9,26): error CS1622: Cannot return a value from an iterator. Use the yield return statement to return a value, or yield break to end the iteration.
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.ERR_ReturnInIterator, "return").WithLocation(9, 26),
                // (9,37): warning CS0162: Unreachable code detected
                //         lock ((C + yield return +D).ToString())
                Diagnostic(ErrorCode.WRN_UnreachableCode, "ToString").WithLocation(9, 37)
                );
        }

        [Fact]
        public void StatementInLock()
        {
            var source = @"
class Test
{
    public static void Main()
    {
        System.Random randGen = new System.Random();
        string i, j = ""def"";
        lock ((randGen.NextDouble() > 0.5) ? i = ""abc"" : j)
        {
            System.Console.WriteLine(i);// Invalid
            System.Console.WriteLine(j);
        }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(Diagnostic(ErrorCode.ERR_UseDefViolation, "i").WithArguments("i"));
        }

        [WorkItem(543168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543168")]
        [Fact()]
        public void MalformedLock_1()
        {
            var source = @"
class D
{
    public void goo()
    {
            lock (varnew object)
            {
            }
    }
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (6,26): error CS1525: Invalid expression term 'object'
                //             lock (varnew object)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "object").WithArguments("object").WithLocation(6, 26),
                // (6,19): error CS0103: The name 'varnew' does not exist in the current context
                //             lock (varnew object)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "varnew").WithArguments("varnew").WithLocation(6, 19));
        }

        [Fact]
        public void LockNull()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    void M()
    {
        lock (null)
        {
        }
    }
}
");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var lockStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LockStatementSyntax>().ToList();

            Assert.Null(model.GetSymbolInfo(lockStatements[0].Expression).Symbol);
            Assert.Null(model.GetTypeInfo(lockStatements[0].Expression).Type);
        }

        [Fact]
        public void LockThis()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    void M()
    {
        lock (this)
        {
        }
    }
}
");
            var symbol = compilation.GetTypeByMetadataName("Test");
            VerifySemanticInfoForLockStatements(compilation, symbol);
        }

        [Fact]
        public void LockExpression()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    void M()
    {
        lock (new Test())
        {
        }
    }
}
");
            var symbol = compilation.GetTypeByMetadataName("Test");
            VerifySemanticInfoForLockStatements(compilation, symbol);
        }

        [Fact]
        public void LockTypeParameterExpression()
        {
            var compilation = CreateCompilation(
@"
public class Test
{
    void M<T>(T t) where T : class
    {
        lock (t)
        {
        }
    }
}
");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var localDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeParameterSyntax>().Single();
            var parameterSymbol = model.GetDeclaredSymbol(localDecl);
            VerifySemanticInfoForLockStatements(compilation, parameterSymbol.GetSymbol());
        }

        [Fact]
        public void LockQuery()
        {
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(
@"
using System.Linq;
class Test
{
    public static void Main()
    {
        lock (from x in ""ABC""
              select x)
        {
        }
    }
}
");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var lockStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LockStatementSyntax>().ToList();

            Assert.Null(model.GetSymbolInfo(lockStatements[0].Expression).Symbol);
            Assert.Equal(@"System.Collections.Generic.IEnumerable<char>", model.GetTypeInfo(lockStatements[0].Expression).Type.ToDisplayString());
        }

        [Fact]
        public void LockDelegate()
        {
            var compilation = CreateCompilation(
@"
delegate void D(int p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1= PM)
        {
        }
    }
    static partial void PM(int p1);
    static partial void PM(int p1)
    {
    }
}
");
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var localDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var symbol = (ILocalSymbol)model.GetDeclaredSymbol(localDecl.Declaration.Variables.Single());
            VerifySemanticInfoForLockStatements(compilation, symbol.Type.GetSymbol(), isSymbolNull: true);
        }

        [Fact()]
        public void LockAnonymousTypes()
        {
            var compilation = CreateCompilation(
@"
class Test
{
    public static void Main()
    {
        var b = new { p1 = 10 };
        lock (b)
        {
        }
    }
}
");

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var localDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var symbol = (ILocalSymbol)model.GetDeclaredSymbol(localDecl.Declaration.Variables.Single());
            VerifySemanticInfoForLockStatements(compilation, symbol.Type.GetSymbol());
        }

        [Fact()]
        public void LockTypeOf()
        {
            var compilation = CreateCompilation(
@"
class Test
{
    public static void Main()
    {
        lock (typeof(decimal))
        {
        }
    }
}
");
            var symbol = compilation.GetTypeByMetadataName("System.Type");
            VerifySemanticInfoForLockStatements(compilation, symbol, isSymbolNull: true);
        }

        [Fact()]
        public void LockString()
        {
            var compilation = CreateCompilation(
@"
class Test
{
    public static void Main()
    {
        lock (""abc"")
        {
        }
    }
}
");
            var symbol = compilation.GetSpecialType(SpecialType.System_String);
            VerifySemanticInfoForLockStatements(compilation, symbol, isSymbolNull: true);
        }

        [Fact()]
        public void AssignmentInLock()
        {
            var compilation = CreateCompilation(
@"
class Test
{
    public static void Main()
    {
        object myLock = null;
        lock ((myLock == null).ToString())
        {
            System.Console.WriteLine(myLock.ToString());
        }
    }
}
");
            var symbol = compilation.GetSpecialType(SpecialType.System_String);
            VerifySemanticInfoForLockStatements(compilation, symbol);
        }

        #region help method

        private static void VerifySemanticInfoForLockStatements(CSharpCompilation compilation, Symbol symbol, int index = 1, bool isSymbolNull = false)
        {
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var lockStatements = tree.GetCompilationUnitRoot().DescendantNodes().OfType<LockStatementSyntax>().ToList();
            var symbolInfo = model.GetSymbolInfo(lockStatements[index - 1].Expression);

            if (isSymbolNull == true)
            {
                Assert.Null(symbolInfo.Symbol);
            }
            else
            {
                Assert.NotNull(symbolInfo.Symbol);
            }
            var typeInfo = model.GetTypeInfo(lockStatements[index - 1].Expression);
            Assert.NotNull(typeInfo.Type);
            Assert.NotNull(typeInfo.ConvertedType);

            Assert.Equal(symbol, typeInfo.Type.GetSymbol());
            Assert.Equal(symbol, typeInfo.ConvertedType.GetSymbol());
        }

        #endregion

        [WorkItem(543168, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543168")]
        [Fact]
        public void EmbeddedDeclaration()
        {
            var source = @"
class C
{
    static void Main()
    {
        lock(null) object o = new object();
    }
}
";

            CreateCompilation(source).VerifyDiagnostics(
                // (6,20): error CS1023: Embedded statement cannot be a declaration or labeled statement
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "object o = new object();"));
        }

        [WorkItem(529001, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529001")]
        [WorkItem(1067, "https://github.com/dotnet/roslyn/issues/1067")]
        [Fact]
        public void LockTypeGenericTypeParam()
        {
            var source = @"
class Gen1<T>
{
    public static void Consumer(T monitor1)
    {
        lock (monitor1)
        {
        }
        lock (null) {}
    }
}
class Gen2<T> where T : struct
{
    public static void Consumer(T monitor2)
    {
        lock (monitor2)
        {
        }
    }
}
class Gen3<T> where T : class
{
    public static void Consumer(T monitor3)
    {
        lock (monitor3)
        {
        }
    }
}
";
            var regularCompilation = CreateCompilation(source);
            var strictCompilation = CreateCompilation(source, parseOptions: TestOptions.Regular.WithStrictFeature());

            regularCompilation.VerifyDiagnostics(
                // (16,15): error CS0185: 'T' is not a reference type as required by the lock statement
                //         lock (monitor2)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "monitor2").WithArguments("T").WithLocation(16, 15)
                );
            strictCompilation.VerifyDiagnostics(
                // (16,15): error CS0185: 'T' is not a reference type as required by the lock statement
                //         lock (monitor2)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "monitor2").WithArguments("T").WithLocation(16, 15),
                // (6,15): error CS0185: 'T' is not a reference type as required by the lock statement
                //         lock (monitor1)
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "monitor1").WithArguments("T").WithLocation(6, 15),
                // (9,15): error CS0185: '<null>' is not a reference type as required by the lock statement
                //         lock (null) {}
                Diagnostic(ErrorCode.ERR_LockNeedsReference, "null").WithArguments("<null>").WithLocation(9, 15)
                );
        }
    }
}
