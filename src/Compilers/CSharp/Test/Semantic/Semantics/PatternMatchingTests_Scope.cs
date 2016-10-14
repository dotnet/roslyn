// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    [CompilerTrait(CompilerFeature.Patterns)]
    public class PatternMatchingTests_Scope : PatternMatchingTestBase
    {
        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        Dummy(true is var x1, x1);
        {
            Dummy(true is var x1, x1);
        }
        Dummy(true is var x1, x1);
    }

    void Test2()
    {
        Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
        Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        Dummy(true is var x4, x4);
    }

    void Test5()
    {
        Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //    Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            Dummy(true is var x9, x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        Dummy(true is var x10, x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        Dummy(true is var x11, x11);
    }

    void Test12()
    {
        Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (14,31): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(14, 31),
    // (16,27): error CS0128: A local variable named 'x1' is already defined in this scope
    //         Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(16, 27),
    // (21,15): error CS0841: Cannot use local variable 'x2' before it is declared
    //         Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 15),
    // (26,27): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 27),
    // (33,27): error CS0128: A local variable named 'x4' is already defined in this scope
    //         Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(33, 27),
    // (39,13): error CS0128: A local variable named 'x5' is already defined in this scope
    //         var x5 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(39, 13),
    // (39,13): warning CS0219: The variable 'x5' is assigned but its value is never used
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(39, 13),
    // (59,48): error CS0128: A local variable named 'x8' is already defined in this scope
    //         Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 48),
    // (79,15): error CS0841: Cannot use local variable 'x11' before it is declared
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(79, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl[2]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(2 is var x1);
        System.Console.WriteLine(x1);
    }

    static object Test1(bool x)
    {
        return null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Decl.Length);
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test0();
    }

    static object Test0()
    {
        bool test = true;

        if (test)
            Test2(1 is var x1, x1);

        if (test)
        {
            Test2(2 is var x1, x1);
        }

        return null;
    }

    static object Test2(object x, object y)
    {
        System.Console.Write(y);
        return x;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: "12").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Decl.Length);
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        if (true)
            Dummy(true is var x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (15,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(15, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionStatement_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (ExpressionStatementSyntax)SyntaxFactory.ParseStatement(@"
Dummy(11 is var x1, x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact, WorkItem(9258, "https://github.com/dotnet/roslyn/issues/9258")]
        public void PatternVariableOrder()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    static void Dummy(params object[] x) {}

    void Test1(object o1, object o2)
    {
        Dummy(o1 is int i && i < 10,
              o2 is int @i && @i > 10);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (13,25): error CS0128: A local variable named 'i' is already defined in this scope
                //               o2 is int @i && @i > 10);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "@i").WithArguments("i").WithLocation(13, 25),
                // (13,31): error CS0165: Use of unassigned local variable 'i'
                //               o2 is int @i && @i > 10);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "@i").WithArguments("i").WithLocation(13, 31)
                );
        }

        [Fact]
        public void ScopeOfPatternVariables_ReturnStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null; }

    object Test1()
    {
        return Dummy(true is var x1, x1);
        {
            return Dummy(true is var x1, x1);
        }
        return Dummy(true is var x1, x1);
    }

    object Test2()
    {
        return Dummy(x2, true is var x2);
    }

    object Test3(int x3)
    {
        return Dummy(true is var x3, x3);
    }

    object Test4()
    {
        var x4 = 11;
        Dummy(x4);
        return Dummy(true is var x4, x4);
    }

    object Test5()
    {
        return Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //object Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    return Dummy(true is var x6, x6);
    //}

    //object Test7()
    //{
    //    return Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    object Test8()
    {
        return Dummy(true is var x8, x8, false is var x8, x8);
    }

    object Test9(bool y9)
    {
        if (y9)
            return Dummy(true is var x9, x9);
        return null;
    }
    System.Func<object> Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        return Dummy(true is var x10, x10);
                    return null;};
    }

    object Test11()
    {
        Dummy(x11);
        return Dummy(true is var x11, x11);
    }

    object Test12()
    {
        return Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (14,38): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(14, 38),
    // (16,34): error CS0128: A local variable named 'x1' is already defined in this scope
    //         return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(16, 34),
    // (14,13): warning CS0162: Unreachable code detected
    //             return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "return").WithLocation(14, 13),
    // (21,22): error CS0841: Cannot use local variable 'x2' before it is declared
    //         return Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 22),
    // (26,34): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 34),
    // (33,34): error CS0128: A local variable named 'x4' is already defined in this scope
    //         return Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(33, 34),
    // (39,13): error CS0128: A local variable named 'x5' is already defined in this scope
    //         var x5 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(39, 13),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (39,13): warning CS0219: The variable 'x5' is assigned but its value is never used
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(39, 13),
    // (59,55): error CS0128: A local variable named 'x8' is already defined in this scope
    //         return Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 55),
    // (79,15): error CS0841: Cannot use local variable 'x11' before it is declared
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(79, 15),
    // (86,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl[2]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ReturnStatement_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    int Dummy(params object[] x) {return 0;}

    int Test1(bool val)
    {
        if (val)
            return Dummy(true is var x1);

        x1++;
        return 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (15,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(15, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ReturnStatement_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (ReturnStatementSyntax)SyntaxFactory.ParseStatement(@"
return Dummy(11 is var x1, x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(
                tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_ThrowStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Exception Dummy(params object[] x) { return null;}

    void Test1()
    {
        throw Dummy(true is var x1, x1);
        {
            throw Dummy(true is var x1, x1);
        }
        throw Dummy(true is var x1, x1);
    }

    void Test2()
    {
        throw Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
        throw Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
        throw Dummy(true is var x4, x4);
    }

    void Test5()
    {
        throw Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    throw Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //    throw Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
        throw Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
            throw Dummy(true is var x9, x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
                        throw Dummy(true is var x10, x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
        throw Dummy(true is var x11, x11);
    }

    void Test12()
    {
        throw Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (14,37): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             throw Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(14, 37),
    // (16,33): error CS0128: A local variable named 'x1' is already defined in this scope
    //         throw Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(16, 33),
    // (21,21): error CS0841: Cannot use local variable 'x2' before it is declared
    //         throw Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 21),
    // (26,33): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         throw Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 33),
    // (33,33): error CS0128: A local variable named 'x4' is already defined in this scope
    //         throw Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(33, 33),
    // (39,13): error CS0128: A local variable named 'x5' is already defined in this scope
    //         var x5 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(39, 13),
    // (39,9): warning CS0162: Unreachable code detected
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(39, 9),
    // (39,13): warning CS0219: The variable 'x5' is assigned but its value is never used
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(39, 13),
    // (59,54): error CS0128: A local variable named 'x8' is already defined in this scope
    //         throw Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 54),
    // (79,15): error CS0841: Cannot use local variable 'x11' before it is declared
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(79, 15),
    // (86,9): warning CS0162: Unreachable code detected
    //         Dummy(x12);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(86, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl[2]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ThrowStatement_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Exception Dummy(params object[] x) { return null;}

    void Test1(bool val)
    {
        if (val)
            throw Dummy(true is var x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (15,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(15, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_TrowStatement_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (ThrowStatementSyntax)SyntaxFactory.ParseStatement(@"
throw Dummy(11 is var x1, x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(
                tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_If_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true is var x1)
        {
            Dummy(x1);
        }
        else
        {
            System.Console.WriteLine(x1);
        }
    }

    void Test2()
    {
        if (true is var x2)
            Dummy(x2);
        else
            System.Console.WriteLine(x2);
    }

    void Test3()
    {
        if (true is var x3)
            Dummy(x3);
        else
        {
            var x3 = 12;
            System.Console.WriteLine(x3);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        if (true is var x4)
            Dummy(x4);
    }

    void Test5(int x5)
    {
        if (true is var x5)
            Dummy(x5);
    }

    void Test6()
    {
        if (x6 && true is var x6)
            Dummy(x6);
    }

    void Test7()
    {
        if (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        if (true is var x8)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        if (true is var x9)
        {   
            Dummy(x9);
            if (true is var x9) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        if (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }










    void Test12()
    {
        if (y12 is var x12)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    if (y13 is var x13)
    //        let y13 = 12;
    //}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (110,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(110, 13),
    // (36,17): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x3 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(36, 17),
    // (46,25): error CS0128: A local variable named 'x4' is already defined in this scope
    //         if (true is var x4)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(46, 25),
    // (52,25): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (true is var x5)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(52, 25),
    // (58,13): error CS0841: Cannot use local variable 'x6' before it is declared
    //         if (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(58, 13),
    // (66,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(66, 17),
    // (83,19): error CS0841: Cannot use local variable 'x9' before it is declared
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(83, 19),
    // (84,29): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             if (true is var x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(84, 29),
    // (91,13): error CS0103: The name 'y10' does not exist in the current context
    //         if (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(91, 13),
    // (109,13): error CS0103: The name 'y12' does not exist in the current context
    //         if (y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(109, 13),
    // (110,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(110, 17)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref[0]);
            VerifyNotAPatternLocal(model, x3Ref[1]);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").Single();
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_If_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true)
            if (true is var x1)
            {
            }
        
        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (17,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(17, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_If_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (IfStatementSyntax)SyntaxFactory.ParseStatement(@"
if (Dummy(11 is var x1, x1));
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_Lambda_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    System.Action<object> Test1()
    {
        return (o) => let x1 = o;
    }

    System.Action<object> Test2()
    {
        return (o) => let var x2 = o;
    }

    void Test3()
    {
        Dummy((System.Func<object, bool>) (o => o is int x3 && x3 > 0));
    }

    void Test4()
    {
        Dummy((System.Func<object, bool>) (o => x4 && o is int x4));
    }

    void Test5()
    {
        Dummy((System.Func<object, object, bool>) ((o1, o2) => o1 is int x5 && 
                                                               o2 is int x5 && 
                                                               x5 > 0));
    }

    void Test6()
    {
        Dummy((System.Func<object, bool>) (o => o is int x6 && x6 > 0), (System.Func<object, bool>) (o => o is int x6 && x6 > 0));
    }

    void Test7()
    {
        Dummy(x7, 1);
        Dummy(x7, 
             (System.Func<object, bool>) (o => o is int x7 && x7 > 0), 
              x7);
        Dummy(x7, 2); 
    }

    void Test8()
    {
        Dummy(true is var x8 && x8, (System.Func<object, bool>) (o => o is int y8 && x8));
    }

    void Test9()
    {
        Dummy(true is var x9, 
              (System.Func<object, bool>) (o => o is int x9 && 
                                                x9 > 0), x9);
    }

    void Test10()
    {
        Dummy((System.Func<object, bool>) (o => o is int x10 && 
                                                x10 > 0),
              true is var x10, x10);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        Dummy((System.Func<object, bool>) (o => o is int x11 && 
                                                x11 > 0), x11);
    }

    void Test12()
    {
        Dummy((System.Func<object, bool>) (o => o is int x12 && 
                                                x12 > 0), 
              x12);
        var x12 = 11;
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,27): error CS1002: ; expected
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(12, 27),
    // (17,27): error CS1002: ; expected
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(17, 27),
    // (12,23): error CS0103: The name 'let' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(12, 23),
    // (12,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(12, 23),
    // (12,27): error CS0103: The name 'x1' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(12, 27),
    // (12,32): error CS0103: The name 'o' does not exist in the current context
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(12, 32),
    // (12,27): warning CS0162: Unreachable code detected
    //         return (o) => let x1 = o;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "x1").WithLocation(12, 27),
    // (17,23): error CS0103: The name 'let' does not exist in the current context
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(17, 23),
    // (17,23): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(17, 23),
    // (17,36): error CS0103: The name 'o' does not exist in the current context
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(17, 36),
    // (17,27): warning CS0162: Unreachable code detected
    //         return (o) => let var x2 = o;
    Diagnostic(ErrorCode.WRN_UnreachableCode, "var").WithLocation(17, 27),
    // (27,49): error CS0841: Cannot use local variable 'x4' before it is declared
    //         Dummy((System.Func<object, bool>) (o => x4 && o is int x4));
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(27, 49),
    // (33,74): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                                                o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(33, 74),
    // (34,64): error CS0165: Use of unassigned local variable 'x5'
    //                                                                x5 > 0));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(34, 64),
    // (44,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(44, 15),
    // (45,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(45, 15),
    // (47,15): error CS0103: The name 'x7' does not exist in the current context
    //               x7);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(47, 15),
    // (48,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(48, 15),
    // (59,58): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //               (System.Func<object, bool>) (o => o is int x9 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(59, 58),
    // (65,58): error CS0136: A local or parameter named 'x10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x10 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x10").WithArguments("x10").WithLocation(65, 58),
    // (74,58): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(74, 58),
    // (80,58): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         Dummy((System.Func<object, bool>) (o => o is int x12 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(80, 58),
    // (82,15): error CS0841: Cannot use local variable 'x12' before it is declared
    //               x12);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(82, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(5, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[2]);
            VerifyNotInScope(model, x7Ref[3]);
            VerifyNotInScope(model, x7Ref[4]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(2, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[0]);

            var x10Decl = GetPatternDeclarations(tree, "x10").ToArray();
            var x10Ref = GetReferences(tree, "x10").ToArray();
            Assert.Equal(2, x10Decl.Length);
            Assert.Equal(2, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl[0], x10Ref[0]);
            VerifyModelForDeclarationPattern(model, x10Decl[1], x10Ref[1]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(3, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);
            VerifyNotAPatternLocal(model, x11Ref[2]);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(3, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotAPatternLocal(model, x12Ref[1]);
            VerifyNotAPatternLocal(model, x12Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_01()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x in new[] { 1 is var y1 ? y1 : 0, y1}
                  select x + y1;

        Dummy(y1); 
    }

    void Test2()
    {
        var res = from x1 in new[] { 1 is var y2 ? y2 : 0}
                  from x2 in new[] { x1 is var z2 ? z2 : 0, z2, y2}
                  select x1 + x2 + y2 + 
                         z2;

        Dummy(z2); 
    }

    void Test3()
    {
        var res = from x1 in new[] { 1 is var y3 ? y3 : 0}
                  let x2 = x1 is var z3 && z3 > 0 && y3 < 0 
                  select new { x1, x2, y3,
                               z3};

        Dummy(z3); 
    }

    void Test4()
    {
        var res = from x1 in new[] { 1 is var y4 ? y4 : 0}
                  join x2 in new[] { 2 is var z4 ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + 3 is var u4 ? u4 : 0 + 
                                  v4 
                               equals x2 + y4 + z4 + 4 is var v4 ? v4 : 0 +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { 1 is var y5 ? y5 : 0}
                  join x2 in new[] { 2 is var z5 ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + 3 is var u5 ? u5 : 0 + 
                                  v5 
                               equals x2 + y5 + z5 + 4 is var v5 ? v5 : 0 +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }

    void Test6()
    {
        var res = from x in new[] { 1 is var y6 ? y6 : 0}
                  where x > y6 && 1 is var z6 && z6 == 1
                  select x + y6 +
                         z6;

        Dummy(z6); 
    }

    void Test7()
    {
        var res = from x in new[] { 1 is var y7 ? y7 : 0}
                  orderby x > y7 && 1 is var z7 && z7 == 
                          u7,
                          x > y7 && 1 is var u7 && u7 == 
                          z7   
                  select x + y7 +
                         z7 + u7;

        Dummy(z7); 
        Dummy(u7); 
    }

    void Test8()
    {
        var res = from x in new[] { 1 is var y8 ? y8 : 0}
                  select x > y8 && 1 is var z8 && z8 == 1;

        Dummy(z8); 
    }

    void Test9()
    {
        var res = from x in new[] { 1 is var y9 ? y9 : 0}
                  group x > y9 && 1 is var z9 && z9 == 
                        u9
                  by
                        x > y9 && 1 is var u9 && u9 == 
                        z9;   

        Dummy(z9); 
        Dummy(u9); 
    }

    void Test10()
    {
        var res = from x1 in new[] { 1 is var y10 ? y10 : 0}
                  from y10 in new[] { 1 }
                  select x1 + y10;
    }

    void Test11()
    {
        var res = from x1 in new[] { 1 is var y11 ? y11 : 0}
                  let y11 = x1 + 1
                  select x1 + y11;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (25,26): error CS0103: The name 'z2' does not exist in the current context
    //                          z2;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(25, 26),
    // (27,15): error CS0103: The name 'z2' does not exist in the current context
    //         Dummy(z2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z2").WithArguments("z2").WithLocation(27, 15),
    // (35,32): error CS0103: The name 'z3' does not exist in the current context
    //                                z3};
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(35, 32),
    // (37,15): error CS0103: The name 'z3' does not exist in the current context
    //         Dummy(z3); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z3").WithArguments("z3").WithLocation(37, 15),
    // (45,35): error CS0103: The name 'v4' does not exist in the current context
    //                                   v4 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(45, 35),
    // (47,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u4 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(47, 35),
    // (49,32): error CS0103: The name 'u4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(49, 32),
    // (49,36): error CS0103: The name 'v4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(49, 36),
    // (52,15): error CS0103: The name 'u4' does not exist in the current context
    //         Dummy(u4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(52, 15),
    // (53,15): error CS0103: The name 'v4' does not exist in the current context
    //         Dummy(v4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(53, 15),
    // (61,35): error CS0103: The name 'v5' does not exist in the current context
    //                                   v5 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(61, 35),
    // (63,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u5 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(63, 35),
    // (66,32): error CS0103: The name 'u5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(66, 32),
    // (66,36): error CS0103: The name 'v5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(66, 36),
    // (69,15): error CS0103: The name 'u5' does not exist in the current context
    //         Dummy(u5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(69, 15),
    // (70,15): error CS0103: The name 'v5' does not exist in the current context
    //         Dummy(v5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(70, 15),
    // (78,26): error CS0103: The name 'z6' does not exist in the current context
    //                          z6;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(78, 26),
    // (80,15): error CS0103: The name 'z6' does not exist in the current context
    //         Dummy(z6); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z6").WithArguments("z6").WithLocation(80, 15),
    // (87,27): error CS0103: The name 'u7' does not exist in the current context
    //                           u7,
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(87, 27),
    // (89,27): error CS0103: The name 'z7' does not exist in the current context
    //                           z7   
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(89, 27),
    // (91,31): error CS0103: The name 'u7' does not exist in the current context
    //                          z7 + u7;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(91, 31),
    // (91,26): error CS0103: The name 'z7' does not exist in the current context
    //                          z7 + u7;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(91, 26),
    // (93,15): error CS0103: The name 'z7' does not exist in the current context
    //         Dummy(z7); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z7").WithArguments("z7").WithLocation(93, 15),
    // (94,15): error CS0103: The name 'u7' does not exist in the current context
    //         Dummy(u7); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u7").WithArguments("u7").WithLocation(94, 15),
    // (88,52): error CS0165: Use of unassigned local variable 'u7'
    //                           x > y7 && 1 is var u7 && u7 == 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "u7").WithArguments("u7").WithLocation(88, 52),
    // (102,15): error CS0103: The name 'z8' does not exist in the current context
    //         Dummy(z8); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z8").WithArguments("z8").WithLocation(102, 15),
    // (112,25): error CS0103: The name 'z9' does not exist in the current context
    //                         z9;   
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(112, 25),
    // (109,25): error CS0103: The name 'u9' does not exist in the current context
    //                         u9
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(109, 25),
    // (114,15): error CS0103: The name 'z9' does not exist in the current context
    //         Dummy(z9); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "z9").WithArguments("z9").WithLocation(114, 15),
    // (115,15): error CS0103: The name 'u9' does not exist in the current context
    //         Dummy(u9); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u9").WithArguments("u9").WithLocation(115, 15),
    // (108,50): error CS0165: Use of unassigned local variable 'z9'
    //                   group x > y9 && 1 is var z9 && z9 == 
    Diagnostic(ErrorCode.ERR_UseDefViolation, "z9").WithArguments("z9").WithLocation(108, 50),
    // (121,24): error CS1931: The range variable 'y10' conflicts with a previous declaration of 'y10'
    //                   from y10 in new[] { 1 }
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y10").WithArguments("y10").WithLocation(121, 24),
    // (128,23): error CS1931: The range variable 'y11' conflicts with a previous declaration of 'y11'
    //                   let y11 = x1 + 1
    Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y11").WithArguments("y11").WithLocation(128, 23)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y1Decl = GetPatternDeclarations(tree, "y1").Single();
            var y1Ref = GetReferences(tree, "y1").ToArray();
            Assert.Equal(4, y1Ref.Length);
            VerifyModelForDeclarationPattern(model, y1Decl, y1Ref);

            var y2Decl = GetPatternDeclarations(tree, "y2").Single();
            var y2Ref = GetReferences(tree, "y2").ToArray();
            Assert.Equal(3, y2Ref.Length);
            VerifyModelForDeclarationPattern(model, y2Decl, y2Ref);

            var z2Decl = GetPatternDeclarations(tree, "z2").Single();
            var z2Ref = GetReferences(tree, "z2").ToArray();
            Assert.Equal(4, z2Ref.Length);
            VerifyModelForDeclarationPattern(model, z2Decl, z2Ref[0], z2Ref[1]);
            VerifyNotInScope(model, z2Ref[2]);
            VerifyNotInScope(model, z2Ref[3]);

            var y3Decl = GetPatternDeclarations(tree, "y3").Single();
            var y3Ref = GetReferences(tree, "y3").ToArray();
            Assert.Equal(3, y3Ref.Length);
            VerifyModelForDeclarationPattern(model, y3Decl, y3Ref);

            var z3Decl = GetPatternDeclarations(tree, "z3").Single();
            var z3Ref = GetReferences(tree, "z3").ToArray();
            Assert.Equal(3, z3Ref.Length);
            VerifyModelForDeclarationPattern(model, z3Decl, z3Ref[0]);
            VerifyNotInScope(model, z3Ref[1]);
            VerifyNotInScope(model, z3Ref[2]);

            var y4Decl = GetPatternDeclarations(tree, "y4").Single();
            var y4Ref = GetReferences(tree, "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForDeclarationPattern(model, y4Decl, y4Ref);

            var z4Decl = GetPatternDeclarations(tree, "z4").Single();
            var z4Ref = GetReferences(tree, "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForDeclarationPattern(model, z4Decl, z4Ref);

            var u4Decl = GetPatternDeclarations(tree, "u4").Single();
            var u4Ref = GetReferences(tree, "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForDeclarationPattern(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = GetPatternDeclarations(tree, "v4").Single();
            var v4Ref = GetReferences(tree, "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForDeclarationPattern(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = GetPatternDeclarations(tree, "y5").Single();
            var y5Ref = GetReferences(tree, "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForDeclarationPattern(model, y5Decl, y5Ref);

            var z5Decl = GetPatternDeclarations(tree, "z5").Single();
            var z5Ref = GetReferences(tree, "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForDeclarationPattern(model, z5Decl, z5Ref);

            var u5Decl = GetPatternDeclarations(tree, "u5").Single();
            var u5Ref = GetReferences(tree, "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForDeclarationPattern(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = GetPatternDeclarations(tree, "v5").Single();
            var v5Ref = GetReferences(tree, "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForDeclarationPattern(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);

            var y6Decl = GetPatternDeclarations(tree, "y6").Single();
            var y6Ref = GetReferences(tree, "y6").ToArray();
            Assert.Equal(3, y6Ref.Length);
            VerifyModelForDeclarationPattern(model, y6Decl, y6Ref);

            var z6Decl = GetPatternDeclarations(tree, "z6").Single();
            var z6Ref = GetReferences(tree, "z6").ToArray();
            Assert.Equal(3, z6Ref.Length);
            VerifyModelForDeclarationPattern(model, z6Decl, z6Ref[0]);
            VerifyNotInScope(model, z6Ref[1]);
            VerifyNotInScope(model, z6Ref[2]);

            var y7Decl = GetPatternDeclarations(tree, "y7").Single();
            var y7Ref = GetReferences(tree, "y7").ToArray();
            Assert.Equal(4, y7Ref.Length);
            VerifyModelForDeclarationPattern(model, y7Decl, y7Ref);

            var z7Decl = GetPatternDeclarations(tree, "z7").Single();
            var z7Ref = GetReferences(tree, "z7").ToArray();
            Assert.Equal(4, z7Ref.Length);
            VerifyModelForDeclarationPattern(model, z7Decl, z7Ref[0]);
            VerifyNotInScope(model, z7Ref[1]);
            VerifyNotInScope(model, z7Ref[2]);
            VerifyNotInScope(model, z7Ref[3]);

            var u7Decl = GetPatternDeclarations(tree, "u7").Single();
            var u7Ref = GetReferences(tree, "u7").ToArray();
            Assert.Equal(4, u7Ref.Length);
            VerifyNotInScope(model, u7Ref[0]);
            VerifyModelForDeclarationPattern(model, u7Decl, u7Ref[1]);
            VerifyNotInScope(model, u7Ref[2]);
            VerifyNotInScope(model, u7Ref[3]);

            var y8Decl = GetPatternDeclarations(tree, "y8").Single();
            var y8Ref = GetReferences(tree, "y8").ToArray();
            Assert.Equal(2, y8Ref.Length);
            VerifyModelForDeclarationPattern(model, y8Decl, y8Ref);

            var z8Decl = GetPatternDeclarations(tree, "z8").Single();
            var z8Ref = GetReferences(tree, "z8").ToArray();
            Assert.Equal(2, z8Ref.Length);
            VerifyModelForDeclarationPattern(model, z8Decl, z8Ref[0]);
            VerifyNotInScope(model, z8Ref[1]);

            var y9Decl = GetPatternDeclarations(tree, "y9").Single();
            var y9Ref = GetReferences(tree, "y9").ToArray();
            Assert.Equal(3, y9Ref.Length);
            VerifyModelForDeclarationPattern(model, y9Decl, y9Ref);

            var z9Decl = GetPatternDeclarations(tree, "z9").Single();
            var z9Ref = GetReferences(tree, "z9").ToArray();
            Assert.Equal(3, z9Ref.Length);
            VerifyModelForDeclarationPattern(model, z9Decl, z9Ref[0]);
            VerifyNotInScope(model, z9Ref[1]);
            VerifyNotInScope(model, z9Ref[2]);

            var u9Decl = GetPatternDeclarations(tree, "u9").Single();
            var u9Ref = GetReferences(tree, "u9").ToArray();
            Assert.Equal(3, u9Ref.Length);
            VerifyNotInScope(model, u9Ref[0]);
            VerifyModelForDeclarationPattern(model, u9Decl, u9Ref[1]);
            VerifyNotInScope(model, u9Ref[2]);

            var y10Decl = GetPatternDeclarations(tree, "y10").Single();
            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyModelForDeclarationPattern(model, y10Decl, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y11Decl = GetPatternDeclarations(tree, "y11").Single();
            var y11Ref = GetReferences(tree, "y11").ToArray();
            Assert.Equal(2, y11Ref.Length);
            VerifyModelForDeclarationPattern(model, y11Decl, y11Ref[0]);
            VerifyNotAPatternLocal(model, y11Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_03()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test4()
    {
        var res = from x1 in new[] { 1 is var y4 ? y4 : 0}
                  select x1 into x1
                  join x2 in new[] { 2 is var z4 ? z4 : 0, z4, y4}
                            on x1 + y4 + z4 + 3 is var u4 ? u4 : 0 + 
                                  v4 
                               equals x2 + y4 + z4 + 4 is var v4 ? v4 : 0 +
                                  u4 
                  select new { x1, x2, y4, z4, 
                               u4, v4 };

        Dummy(z4); 
        Dummy(u4); 
        Dummy(v4); 
    }

    void Test5()
    {
        var res = from x1 in new[] { 1 is var y5 ? y5 : 0}
                  select x1 into x1
                  join x2 in new[] { 2 is var z5 ? z5 : 0, z5, y5}
                            on x1 + y5 + z5 + 3 is var u5 ? u5 : 0 + 
                                  v5 
                               equals x2 + y5 + z5 + 4 is var v5 ? v5 : 0 +
                                  u5 
                  into g
                  select new { x1, y5, z5, g,
                               u5, v5 };

        Dummy(z5); 
        Dummy(u5); 
        Dummy(v5); 
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (18,35): error CS0103: The name 'v4' does not exist in the current context
    //                                   v4 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(18, 35),
    // (20,35): error CS1938: The name 'u4' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u4 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u4").WithArguments("u4").WithLocation(20, 35),
    // (22,32): error CS0103: The name 'u4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(22, 32),
    // (22,36): error CS0103: The name 'v4' does not exist in the current context
    //                                u4, v4 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(22, 36),
    // (25,15): error CS0103: The name 'u4' does not exist in the current context
    //         Dummy(u4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u4").WithArguments("u4").WithLocation(25, 15),
    // (26,15): error CS0103: The name 'v4' does not exist in the current context
    //         Dummy(v4); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v4").WithArguments("v4").WithLocation(26, 15),
    // (35,35): error CS0103: The name 'v5' does not exist in the current context
    //                                   v5 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(35, 35),
    // (37,35): error CS1938: The name 'u5' is not in scope on the right side of 'equals'.  Consider swapping the expressions on either side of 'equals'.
    //                                   u5 
    Diagnostic(ErrorCode.ERR_QueryInnerKey, "u5").WithArguments("u5").WithLocation(37, 35),
    // (40,32): error CS0103: The name 'u5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(40, 32),
    // (40,36): error CS0103: The name 'v5' does not exist in the current context
    //                                u5, v5 };
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(40, 36),
    // (43,15): error CS0103: The name 'u5' does not exist in the current context
    //         Dummy(u5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u5").WithArguments("u5").WithLocation(43, 15),
    // (44,15): error CS0103: The name 'v5' does not exist in the current context
    //         Dummy(v5); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v5").WithArguments("v5").WithLocation(44, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var y4Decl = GetPatternDeclarations(tree, "y4").Single();
            var y4Ref = GetReferences(tree, "y4").ToArray();
            Assert.Equal(5, y4Ref.Length);
            VerifyModelForDeclarationPattern(model, y4Decl, y4Ref);

            var z4Decl = GetPatternDeclarations(tree, "z4").Single();
            var z4Ref = GetReferences(tree, "z4").ToArray();
            Assert.Equal(6, z4Ref.Length);
            VerifyModelForDeclarationPattern(model, z4Decl, z4Ref);

            var u4Decl = GetPatternDeclarations(tree, "u4").Single();
            var u4Ref = GetReferences(tree, "u4").ToArray();
            Assert.Equal(4, u4Ref.Length);
            VerifyModelForDeclarationPattern(model, u4Decl, u4Ref[0]);
            VerifyNotInScope(model, u4Ref[1]);
            VerifyNotInScope(model, u4Ref[2]);
            VerifyNotInScope(model, u4Ref[3]);

            var v4Decl = GetPatternDeclarations(tree, "v4").Single();
            var v4Ref = GetReferences(tree, "v4").ToArray();
            Assert.Equal(4, v4Ref.Length);
            VerifyNotInScope(model, v4Ref[0]);
            VerifyModelForDeclarationPattern(model, v4Decl, v4Ref[1]);
            VerifyNotInScope(model, v4Ref[2]);
            VerifyNotInScope(model, v4Ref[3]);

            var y5Decl = GetPatternDeclarations(tree, "y5").Single();
            var y5Ref = GetReferences(tree, "y5").ToArray();
            Assert.Equal(5, y5Ref.Length);
            VerifyModelForDeclarationPattern(model, y5Decl, y5Ref);

            var z5Decl = GetPatternDeclarations(tree, "z5").Single();
            var z5Ref = GetReferences(tree, "z5").ToArray();
            Assert.Equal(6, z5Ref.Length);
            VerifyModelForDeclarationPattern(model, z5Decl, z5Ref);

            var u5Decl = GetPatternDeclarations(tree, "u5").Single();
            var u5Ref = GetReferences(tree, "u5").ToArray();
            Assert.Equal(4, u5Ref.Length);
            VerifyModelForDeclarationPattern(model, u5Decl, u5Ref[0]);
            VerifyNotInScope(model, u5Ref[1]);
            VerifyNotInScope(model, u5Ref[2]);
            VerifyNotInScope(model, u5Ref[3]);

            var v5Decl = GetPatternDeclarations(tree, "v5").Single();
            var v5Ref = GetReferences(tree, "v5").ToArray();
            Assert.Equal(4, v5Ref.Length);
            VerifyNotInScope(model, v5Ref[0]);
            VerifyModelForDeclarationPattern(model, v5Decl, v5Ref[1]);
            VerifyNotInScope(model, v5Ref[2]);
            VerifyNotInScope(model, v5Ref[3]);
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_05()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        int y1 = 0, y2 = 0, y3 = 0, y4 = 0, y5 = 0, y6 = 0, y7 = 0, y8 = 0, y9 = 0, y10 = 0, y11 = 0, y12 = 0;

        var res = from x1 in new[] { 1 is var y1 ? y1 : 0}
                  from x2 in new[] { 2 is var y2 ? y2 : 0}
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on 4 is var y4 ? y4 : 0
                          equals 5 is var y5 ? y5 : 0
                  where 6 is var y6 && y6 == 1
                  orderby 7 is var y7 && y7 > 0, 
                          8 is var y8 && y8 > 0 
                  group 9 is var y9 && y9 > 0 
                  by 10 is var y10 && y10 > 0
                  into g
                  let x11 = 11 is var y11 && y11 > 0
                  select 12 is var y12 && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12;

        Dummy(y1, y2, y3, y4, y5, y6, y7, y8, y9, y10, y11, y12); 
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (16,47): error CS0128: A local variable named 'y1' is already defined in this scope
                //         var res = from x1 in new[] { 1 is var y1 ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y1").WithArguments("y1").WithLocation(16, 47),
                // (17,47): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { 2 is var y2 ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(17, 47),
                // (18,47): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(18, 47),
                // (19,36): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on 4 is var y4 ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(19, 36),
                // (20,43): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals 5 is var y5 ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(20, 43),
                // (21,34): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where 6 is var y6 && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(21, 34),
                // (22,36): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby 7 is var y7 && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(22, 36),
                // (23,36): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           8 is var y8 && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(23, 36),
                // (25,32): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by 10 is var y10 && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(25, 32),
                // (24,34): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group 9 is var y9 && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(24, 34),
                // (27,39): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = 11 is var y11 && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(27, 39),
                // (28,36): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select 12 is var y12 && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(28, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = GetPatternDeclarations(tree, id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(3, yRef.Length);

                switch (i)
                {
                    case 1:
                    case 3:
                        VerifyModelForDeclarationPatternDuplicateInSameScope(model, yDecl);
                        VerifyNotAPatternLocal(model, yRef[0]);
                        break;
                    default:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        break;
                }

                VerifyNotAPatternLocal(model, yRef[2]);

                switch (i)
                {
                    case 1:
                    case 3:
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                    default:
                        VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_06()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(1 is int y1, 
              2 is int y2, 
              3 is int y3, 
              4 is int y4, 
              5 is int y5, 
              6 is int y6, 
              7 is int y7, 
              8 is int y8, 
              9 is int y9, 
              10 is int y10, 
              11 is int y11, 
              12 is int y12,
                  from x1 in new[] { 1 is var y1 ? y1 : 0}
                  from x2 in new[] { 2 is var y2 ? y2 : 0}
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on 4 is var y4 ? y4 : 0
                          equals 5 is var y5 ? y5 : 0
                  where 6 is var y6 && y6 == 1
                  orderby 7 is var y7 && y7 > 0, 
                          8 is var y8 && y8 > 0 
                  group 9 is var y9 && y9 > 0 
                  by 10 is var y10 && y10 > 0
                  into g
                  let x11 = 11 is var y11 && y11 > 0
                  select 12 is var y12 && y12 > 0
                  into s
                  select y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (26,47): error CS0128: A local variable named 'y1' is already defined in this scope
                //                   from x1 in new[] { 1 is var y1 ? y1 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y1").WithArguments("y1").WithLocation(26, 47),
                // (27,47): error CS0136: A local or parameter named 'y2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   from x2 in new[] { 2 is var y2 ? y2 : 0}
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y2").WithArguments("y2").WithLocation(27, 47),
                // (28,47): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(28, 47),
                // (29,36): error CS0136: A local or parameter named 'y4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                        on 4 is var y4 ? y4 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y4").WithArguments("y4").WithLocation(29, 36),
                // (30,43): error CS0136: A local or parameter named 'y5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           equals 5 is var y5 ? y5 : 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y5").WithArguments("y5").WithLocation(30, 43),
                // (31,34): error CS0136: A local or parameter named 'y6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   where 6 is var y6 && y6 == 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y6").WithArguments("y6").WithLocation(31, 34),
                // (32,36): error CS0136: A local or parameter named 'y7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   orderby 7 is var y7 && y7 > 0, 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y7").WithArguments("y7").WithLocation(32, 36),
                // (33,36): error CS0136: A local or parameter named 'y8' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                           8 is var y8 && y8 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y8").WithArguments("y8").WithLocation(33, 36),
                // (35,32): error CS0136: A local or parameter named 'y10' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   by 10 is var y10 && y10 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y10").WithArguments("y10").WithLocation(35, 32),
                // (34,34): error CS0136: A local or parameter named 'y9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   group 9 is var y9 && y9 > 0 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y9").WithArguments("y9").WithLocation(34, 34),
                // (37,39): error CS0136: A local or parameter named 'y11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   let x11 = 11 is var y11 && y11 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y11").WithArguments("y11").WithLocation(37, 39),
                // (38,36): error CS0136: A local or parameter named 'y12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                   select 12 is var y12 && y12 > 0
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "y12").WithArguments("y12").WithLocation(38, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 13; i++)
            {
                var id = "y" + i;
                var yDecl = GetPatternDeclarations(tree, id).ToArray();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(2, yDecl.Length);
                Assert.Equal(2, yRef.Length);

                switch (i)
                {
                    case 1:
                    case 3:
                        VerifyModelForDeclarationPattern(model, yDecl[0], yRef);
                        VerifyModelForDeclarationPatternDuplicateInSameScope(model, yDecl[1]);
                        break;
                    case 12:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
                        VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
                        break;

                    default:
                        VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
                        VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_07()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        Dummy(7 is int y3, 
                  from x1 in new[] { 0 }
                  select x1
                  into x1
                  join x3 in new[] { 3 is var y3 ? y3 : 0}
                       on x1 equals x3
                  select y3);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (18,47): error CS0128: A local variable named 'y3' is already defined in this scope
                //                   join x3 in new[] { 3 is var y3 ? y3 : 0}
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "y3").WithArguments("y3").WithLocation(18, 47)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            const string id = "y3";
            var yDecl = GetPatternDeclarations(tree, id).ToArray();
            var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
            Assert.Equal(2, yDecl.Length);
            Assert.Equal(2, yRef.Length);
            VerifyModelForDeclarationPattern(model, yDecl[0], yRef[1]);
            // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
            //VerifyModelForDeclarationPattern(model, yDecl[1], yRef[0]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Query_08()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from x1 in new[] { Dummy(1 is var y1, 
                                           2 is var y2,
                                           3 is var y3,
                                           4 is var y4
                                          ) ? 1 : 0}
                  from y1 in new[] { 1 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by x1
                  into y4
                  select y4;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (19,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //                   from y1 in new[] { 1 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(19, 24),
                // (20,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(20, 24),
                // (22,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(22, 23),
                // (25,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(25, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 5; i++)
            {
                var id = "y" + i;
                var yDecl = GetPatternDeclarations(tree, id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();
                VerifyModelForDeclarationPattern(model, yDecl);
                VerifyNotAPatternLocal(model, yRef);
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        public void ScopeOfPatternVariables_Query_09()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  join y2 in new[] { 0 }
                       on y1 equals y2
                  let y3 = 0
                  group y3 
                  by 1
                  into y4
                  select y4 == null ? 1 : 0
                  into x2
                  join y5 in new[] { Dummy(1 is var y1, 
                                           2 is var y2,
                                           3 is var y3,
                                           4 is var y4,
                                           5 is var y5
                                          ) ? 1 : 0 }
                       on x2 equals y5
                  select x2;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (14,24): error CS1931: The range variable 'y1' conflicts with a previous declaration of 'y1'
                //         var res = from y1 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y1").WithArguments("y1").WithLocation(14, 24),
                // (15,24): error CS1931: The range variable 'y2' conflicts with a previous declaration of 'y2'
                //                   join y2 in new[] { 0 }
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y2").WithArguments("y2").WithLocation(15, 24),
                // (17,23): error CS1931: The range variable 'y3' conflicts with a previous declaration of 'y3'
                //                   let y3 = 0
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y3").WithArguments("y3").WithLocation(17, 23),
                // (20,24): error CS1931: The range variable 'y4' conflicts with a previous declaration of 'y4'
                //                   into y4
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y4").WithArguments("y4").WithLocation(20, 24),
                // (23,24): error CS1931: The range variable 'y5' conflicts with a previous declaration of 'y5'
                //                   join y5 in new[] { Dummy(1 is var y1, 
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "y5").WithArguments("y5").WithLocation(23, 24)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 6; i++)
            {
                var id = "y" + i;
                var yDecl = GetPatternDeclarations(tree, id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).Single();

                switch (i)
                {
                    case 4:
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyModelForDeclarationPattern(model, yDecl);
                        VerifyNotAPatternLocal(model, yRef);
                        break;
                    case 5:
                        VerifyModelForDeclarationPattern(model, yDecl);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef);
                        break;
                    default:
                        VerifyModelForDeclarationPattern(model, yDecl);
                        VerifyNotAPatternLocal(model, yRef);
                        break;
                }
            }
        }

        [Fact]
        [WorkItem(10466, "https://github.com/dotnet/roslyn/issues/10466")]
        [WorkItem(12052, "https://github.com/dotnet/roslyn/issues/12052")]
        public void ScopeOfPatternVariables_Query_10()
        {
            var source =
@"
using System.Linq;

public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        var res = from y1 in new[] { 0 }
                  from x2 in new[] { 1 is var y1 ? y1 : 1 }
                  select y1;
    }

    void Test2()
    {
        var res = from y2 in new[] { 0 }
                  join x3 in new[] { 1 }
                       on 2 is var y2 ? y2 : 0 
                       equals x3
                  select y2;
    }

    void Test3()
    {
        var res = from x3 in new[] { 0 }
                  join y3 in new[] { 1 }
                       on x3 
                       equals 3 is var y3 ? y3 : 0
                  select y3;
    }

    void Test4()
    {
        var res = from y4 in new[] { 0 }
                  where 4 is var y4 && y4 == 1
                  select y4;
    }

    void Test5()
    {
        var res = from y5 in new[] { 0 }
                  orderby 5 is var y5 && y5 > 1, 
                          1 
                  select y5;
    }

    void Test6()
    {
        var res = from y6 in new[] { 0 }
                  orderby 1, 
                          6 is var y6 && y6 > 1 
                  select y6;
    }

    void Test7()
    {
        var res = from y7 in new[] { 0 }
                  group 7 is var y7 && y7 == 3 
                  by y7;
    }

    void Test8()
    {
        var res = from y8 in new[] { 0 }
                  group y8 
                  by 8 is var y8 && y8 == 3;
    }

    void Test9()
    {
        var res = from y9 in new[] { 0 }
                  let x4 = 9 is var y9 && y9 > 0
                  select y9;
    }

    void Test10()
    {
        var res = from y10 in new[] { 0 }
                  select 10 is var y10 && y10 > 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, new[] { SystemCoreRef }, options: TestOptions.DebugExe);

            // error CS0412 is misleading and reported due to preexisting bug https://github.com/dotnet/roslyn/issues/12052
            compilation.VerifyDiagnostics(
                // (15,47): error CS0412: 'y1': a parameter or local variable cannot have the same name as a method type parameter
                //                   from x2 in new[] { 1 is var y1 ? y1 : 1 }
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y1").WithArguments("y1").WithLocation(15, 47),
                // (23,36): error CS0412: 'y2': a parameter or local variable cannot have the same name as a method type parameter
                //                        on 2 is var y2 ? y2 : 0 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y2").WithArguments("y2").WithLocation(23, 36),
                // (33,40): error CS0412: 'y3': a parameter or local variable cannot have the same name as a method type parameter
                //                        equals 3 is var y3 ? y3 : 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y3").WithArguments("y3").WithLocation(33, 40),
                // (40,34): error CS0412: 'y4': a parameter or local variable cannot have the same name as a method type parameter
                //                   where 4 is var y4 && y4 == 1
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y4").WithArguments("y4").WithLocation(40, 34),
                // (47,36): error CS0412: 'y5': a parameter or local variable cannot have the same name as a method type parameter
                //                   orderby 5 is var y5 && y5 > 1, 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y5").WithArguments("y5").WithLocation(47, 36),
                // (56,36): error CS0412: 'y6': a parameter or local variable cannot have the same name as a method type parameter
                //                           6 is var y6 && y6 > 1 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y6").WithArguments("y6").WithLocation(56, 36),
                // (63,34): error CS0412: 'y7': a parameter or local variable cannot have the same name as a method type parameter
                //                   group 7 is var y7 && y7 == 3 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y7").WithArguments("y7").WithLocation(63, 34),
                // (71,31): error CS0412: 'y8': a parameter or local variable cannot have the same name as a method type parameter
                //                   by 8 is var y8 && y8 == 3;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y8").WithArguments("y8").WithLocation(71, 31),
                // (77,37): error CS0412: 'y9': a parameter or local variable cannot have the same name as a method type parameter
                //                   let x4 = 9 is var y9 && y9 > 0
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y9").WithArguments("y9").WithLocation(77, 37),
                // (84,36): error CS0412: 'y10': a parameter or local variable cannot have the same name as a method type parameter
                //                   select 10 is var y10 && y10 > 0;
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "y10").WithArguments("y10").WithLocation(84, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            for (int i = 1; i < 11; i++)
            {
                var id = "y" + i;
                var yDecl = GetPatternDeclarations(tree, id).Single();
                var yRef = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(name => name.Identifier.ValueText == id).ToArray();
                Assert.Equal(i == 10 ? 1 : 2, yRef.Length);

                switch (i)
                {
                    case 4:
                    case 6:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                    case 8:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[1]);
                        // Should be uncommented once https://github.com/dotnet/roslyn/issues/10466 is fixed.
                        //VerifyNotAPatternLocal(model, yRef[0]);
                        break;
                    case 10:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        break;
                    default:
                        VerifyModelForDeclarationPattern(model, yDecl, yRef[0]);
                        VerifyNotAPatternLocal(model, yRef[1]);
                        break;
                }
            }
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        void f(object o) => let x1 = o;
        f(null);
    }

    void Test2()
    {
        void f(object o) => let var x2 = o;
        f(null);
    }

    void Test3()
    {
        bool f (object o) => o is int x3 && x3 > 0;
        f(null);
    }

    void Test4()
    {
        bool f (object o) => x4 && o is int x4;
        f(null);
    }

    void Test5()
    {
        bool f (object o1, object o2) => o1 is int x5 && 
                                         o2 is int x5 && 
                                         x5 > 0;
        f(null, null);
    }

    void Test6()
    {
        bool f1 (object o) => o is int x6 && x6 > 0; bool f2 (object o) => o is int x6 && x6 > 0;
        f1(null);
        f2(null);
    }

    void Test7()
    {
        Dummy(x7, 1);
         
        bool f (object o) => o is int x7 && x7 > 0; 

        Dummy(x7, 2); 
        f(null);
    }

    void Test11()
    {
        var x11 = 11;
        Dummy(x11);
        bool f (object o) => o is int x11 && 
                             x11 > 0;
        f(null);
    }

    void Test12()
    {
        bool f (object o) => o is int x12 && 
                             x12 > 0;
        var x12 = 11;
        Dummy(x12);
        f(null);
    }

    System.Action Test13()
    {
        return () =>
                    {
                        bool f (object o) => o is int x13 && x13 > 0;
                        f(null);
                    };
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (12,33): error CS1002: ; expected
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(12, 33),
    // (18,33): error CS1002: ; expected
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(18, 33),
    // (12,29): error CS0103: The name 'let' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(12, 29),
    // (12,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(12, 29),
    // (12,33): error CS0103: The name 'x1' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(12, 33),
    // (12,38): error CS0103: The name 'o' does not exist in the current context
    //         void f(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(12, 38),
    // (18,29): error CS0103: The name 'let' does not exist in the current context
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(18, 29),
    // (18,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(18, 29),
    // (18,42): error CS0103: The name 'o' does not exist in the current context
    //         void f(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(18, 42),
    // (30,30): error CS0841: Cannot use local variable 'x4' before it is declared
    //         bool f (object o) => x4 && o is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(30, 30),
    // (37,52): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                          o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(37, 52),
    // (38,42): error CS0165: Use of unassigned local variable 'x5'
    //                                          x5 > 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(38, 42),
    // (51,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(51, 15),
    // (55,15): error CS0103: The name 'x7' does not exist in the current context
    //         Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(55, 15),
    // (63,39): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         bool f (object o) => o is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(63, 39),
    // (70,39): error CS0136: A local or parameter named 'x12' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         bool f (object o) => o is int x12 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x12").WithArguments("x12").WithLocation(70, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyNotInScope(model, x7Ref[0]);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyNotAPatternLocal(model, x11Ref[0]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[1]);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[0]);
            VerifyNotAPatternLocal(model, x12Ref[1]);

            var x13Decl = GetPatternDeclarations(tree, "x13").Single();
            var x13Ref = GetReferences(tree, "x13").Single();
            VerifyModelForDeclarationPattern(model, x13Decl, x13Ref);
        }

        [Fact]
        public void ExpressionBodiedLocalFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
        System.Console.WriteLine(Test1());
    }

    static bool Test1()
    {
        bool f() => 1 is int x1 && Dummy(x1); 
        return f();
    }

    static bool Dummy(int x) 
    {
        System.Console.WriteLine(x);
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            CompileAndVerify(compilation, expectedOutput: @"1
True");
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedFunctions_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }


    void Test1(object o) => let x1 = o;

    void Test2(object o) => let var x2 = o;

    bool Test3(object o) => o is int x3 && x3 > 0;

    bool Test4(object o) => x4 && o is int x4;

    bool Test5(object o1, object o2) => o1 is int x5 && 
                                         o2 is int x5 && 
                                         x5 > 0;

    bool Test61 (object o) => o is int x6 && x6 > 0; bool Test62 (object o) => o is int x6 && x6 > 0;

    bool Test71(object o) => o is int x7 && x7 > 0; 
    void Test72() => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Test11(object x11) => 1 is int x11 && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,33): error CS1002: ; expected
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(9, 33),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,36): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 36),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (9,39): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";").WithLocation(9, 39),
    // (11,33): error CS1002: ; expected
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(11, 33),
    // (11,33): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 33),
    // (11,42): error CS0103: The name 'o' does not exist in the current context
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(11, 42),
    // (9,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(9, 29),
    // (9,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test1(object o) => let x1 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(9, 29),
    // (11,29): error CS0103: The name 'let' does not exist in the current context
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(11, 29),
    // (11,29): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
    //     void Test2(object o) => let var x2 = o;
    Diagnostic(ErrorCode.ERR_IllegalStatement, "let").WithLocation(11, 29),
    // (15,29): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4(object o) => x4 && o is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(15, 29),
    // (18,52): error CS0128: A local variable named 'x5' is already defined in this scope
    //                                          o2 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 52),
    // (19,42): error CS0165: Use of unassigned local variable 'x5'
    //                                          x5 > 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x5").WithArguments("x5").WithLocation(19, 42),
    // (24,28): error CS0103: The name 'x7' does not exist in the current context
    //     void Test72() => Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(24, 28),
    // (25,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(25, 27),
    // (27,41): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //     bool Test11(object x11) => 1 is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(27, 41)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").Single();
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_ExpressionBodiedProperties_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }


    bool Test1 => let x1 = 11;

    bool this[int o] => let var x2 = o;

    bool Test3 => 3 is int x3 && x3 > 0;

    bool Test4 => x4 && 4 is int x4;

    bool Test5 => 51 is int x5 && 
                  52 is int x5 && 
                  x5 > 0;

    bool Test61 => 6 is int x6 && x6 > 0; bool Test62 => 6 is int x6 && x6 > 0;

    bool Test71 => 7 is int x7 && x7 > 0; 
    bool Test72 => Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool this[object x11] => 1 is int x11 && 
                             x11 > 0;

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,23): error CS1002: ; expected
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "x1").WithLocation(9, 23),
    // (9,26): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 26),
    // (9,26): error CS1519: Invalid token '=' in class, struct, or interface member declaration
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "=").WithArguments("=").WithLocation(9, 26),
    // (11,29): error CS1002: ; expected
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "var").WithLocation(11, 29),
    // (11,29): error CS0825: The contextual keyword 'var' may only appear within a local variable declaration or in script code
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_TypeVarNotFound, "var").WithLocation(11, 29),
    // (11,38): error CS0103: The name 'o' does not exist in the current context
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(11, 38),
    // (9,19): error CS0103: The name 'let' does not exist in the current context
    //     bool Test1 => let x1 = 11;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(9, 19),
    // (11,25): error CS0103: The name 'let' does not exist in the current context
    //     bool this[int o] => let var x2 = o;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "let").WithArguments("let").WithLocation(11, 25),
    // (15,19): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 => x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(15, 19),
    // (18,29): error CS0128: A local variable named 'x5' is already defined in this scope
    //                   52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 29),
    // (24,26): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 => Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(24, 26),
    // (25,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(25, 27),
    // (27,39): error CS0136: A local or parameter named 'x11' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //     bool this[object x11] => 1 is int x11 && 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x11").WithArguments("x11").WithLocation(27, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").Single();
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 = 3 is int x3 && x3 > 0;

    bool Test4 = x4 && 4 is int x4;

    bool Test5 = 51 is int x5 && 
                 52 is int x5 && 
                 x5 > 0;

    bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;

    bool Test71 = 7 is int x7 && x7 > 0; 
    bool Test72 = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,23): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test3 = 3 is int x3 && x3 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(8, 23),
    // (10,18): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 18),
    // (10,29): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(10, 29),
    // (12,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test5 = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(12, 24),
    // (13,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(13, 24),
    // (13,28): error CS0128: A local variable named 'x5' is already defined in this scope
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 28),
    // (16,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 24),
    // (16,56): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 56),
    // (18,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test71 = 7 is int x7 && x7 > 0; 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(18, 24),
    // (19,25): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 = Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_02()
        {
            var source =
@"
public enum X
{
    Test3 = 3 is int x3 ? x3 : 0,

    Test4 = x4 && 4 is int x4 ? 1 : 0,

    Test5 = 51 is int x5 && 
            52 is int x5 && 
            x5 > 0 ? 1 : 0,

    Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,

    Test71 = 7 is int x7 && x7 > 0 ? 1 : 0, 
    Test72 = x7, 
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugDll);
            compilation.VerifyDiagnostics(
    // (6,13): error CS0841: Cannot use local variable 'x4' before it is declared
    //     Test4 = x4 && 4 is int x4 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(6, 13),
    // (6,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test4 = x4 && 4 is int x4 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(6, 24),
    // (8,19): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test5 = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(8, 19),
    // (9,19): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //             52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(9, 19),
    // (9,23): error CS0128: A local variable named 'x5' is already defined in this scope
    //             52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(9, 23),
    // (12,19): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(12, 19),
    // (12,14): error CS0133: The expression being assigned to 'X.Test61' must be constant
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0 ? 1 : 0").WithArguments("X.Test61").WithLocation(12, 14),
    // (12,59): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(12, 59),
    // (12,54): error CS0133: The expression being assigned to 'X.Test62' must be constant
    //     Test61 = 6 is int x6 && x6 > 0 ? 1 : 0, Test62 = 6 is int x6 && x6 > 0 ? 1 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0 ? 1 : 0").WithArguments("X.Test62").WithLocation(12, 54),
    // (14,19): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test71 = 7 is int x7 && x7 > 0 ? 1 : 0, 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(14, 19),
    // (14,14): error CS0133: The expression being assigned to 'X.Test71' must be constant
    //     Test71 = 7 is int x7 && x7 > 0 ? 1 : 0, 
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "7 is int x7 && x7 > 0 ? 1 : 0").WithArguments("X.Test71").WithLocation(14, 14),
    // (15,14): error CS0103: The name 'x7' does not exist in the current context
    //     Test72 = x7, 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 14),
    // (4,18): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     Test3 = 3 is int x3 ? x3 : 0,
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(4, 18),
    // (4,13): error CS0133: The expression being assigned to 'X.Test3' must be constant
    //     Test3 = 3 is int x3 ? x3 : 0,
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is int x3 ? x3 : 0").WithArguments("X.Test3").WithLocation(4, 13)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_FieldInitializers_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    const bool Test3 = 3 is int x3 && x3 > 0;

    const bool Test4 = x4 && 4 is int x4;

    const bool Test5 = 51 is int x5 && 
                       52 is int x5 && 
                       x5 > 0;

    const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;

    const bool Test71 = 7 is int x7 && x7 > 0; 
    const bool Test72 = x7 > 2; 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,29): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test3 = 3 is int x3 && x3 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(8, 29),
    // (8,24): error CS0133: The expression being assigned to 'X.Test3' must be constant
    //     const bool Test3 = 3 is int x3 && x3 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "3 is int x3 && x3 > 0").WithArguments("X.Test3").WithLocation(8, 24),
    // (10,24): error CS0841: Cannot use local variable 'x4' before it is declared
    //     const bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 24),
    // (10,35): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test4 = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(10, 35),
    // (12,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test5 = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(12, 30),
    // (13,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //                        52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(13, 30),
    // (13,34): error CS0128: A local variable named 'x5' is already defined in this scope
    //                        52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 34),
    // (16,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 30),
    // (16,25): error CS0133: The expression being assigned to 'X.Test61' must be constant
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0").WithArguments("X.Test61").WithLocation(16, 25),
    // (16,62): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 62),
    // (16,57): error CS0133: The expression being assigned to 'X.Test62' must be constant
    //     const bool Test61 = 6 is int x6 && x6 > 0, Test62 = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "6 is int x6 && x6 > 0").WithArguments("X.Test62").WithLocation(16, 57),
    // (18,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     const bool Test71 = 7 is int x7 && x7 > 0; 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(18, 30),
    // (18,25): error CS0133: The expression being assigned to 'X.Test71' must be constant
    //     const bool Test71 = 7 is int x7 && x7 > 0; 
    Diagnostic(ErrorCode.ERR_NotConstantExpression, "7 is int x7 && x7 > 0").WithArguments("X.Test71").WithLocation(18, 25),
    // (19,25): error CS0103: The name 'x7' does not exist in the current context
    //     const bool Test72 = x7 > 2; 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 25),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_PropertyInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Test3 {get;} = 3 is int x3 && x3 > 0;

    bool Test4 {get;} = x4 && 4 is int x4;

    bool Test5 {get;} = 51 is int x5 && 
                 52 is int x5 && 
                 x5 > 0;

    bool Test61 {get;} = 6 is int x6 && x6 > 0; bool Test62 {get;} = 6 is int x6 && x6 > 0;

    bool Test71 {get;} = 7 is int x7 && x7 > 0; 
    bool Test72 {get;} = Dummy(x7, 2); 
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,30): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test3 {get;} = 3 is int x3 && x3 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(8, 30),
    // (10,25): error CS0841: Cannot use local variable 'x4' before it is declared
    //     bool Test4 {get;} = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(10, 25),
    // (10,36): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test4 {get;} = x4 && 4 is int x4;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(10, 36),
    // (12,31): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test5 {get;} = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(12, 31),
    // (13,24): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(13, 24),
    // (13,28): error CS0128: A local variable named 'x5' is already defined in this scope
    //                  52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(13, 28),
    // (16,31): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test61 {get;} = 6 is int x6 && x6 > 0; bool Test62 {get;} = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 31),
    // (16,75): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test61 {get;} = 6 is int x6 && x6 > 0; bool Test62 {get;} = 6 is int x6 && x6 > 0;
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(16, 75),
    // (18,31): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     bool Test71 {get;} = 7 is int x7 && x7 > 0; 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(18, 31),
    // (19,32): error CS0103: The name 'x7' does not exist in the current context
    //     bool Test72 {get;} = Dummy(x7, 2); 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(19, 32),
    // (20,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(20, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ParameterDefault_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test3(bool p = 3 is int x3 && x3 > 0)
    {}

    void Test4(bool p = x4 && 4 is int x4)
    {}

    void Test5(bool p = 51 is int x5 && 
                        52 is int x5 && 
                        x5 > 0)
    {}

    void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    {}

    void Test71(bool p = 7 is int x7 && x7 > 0)
    {
    }

    void Test72(bool p = x7 > 2)
    {}

    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,25): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     void Test3(bool p = 3 is int x3 && x3 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "3 is int x3 && x3 > 0").WithArguments("p").WithLocation(8, 25),
    // (11,25): error CS0841: Cannot use local variable 'x4' before it is declared
    //     void Test4(bool p = x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(11, 25),
    // (11,21): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'bool'
    //     void Test4(bool p = x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "bool").WithLocation(11, 21),
    // (15,35): error CS0128: A local variable named 'x5' is already defined in this scope
    //                         52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(15, 35),
    // (14,21): error CS1750: A value of type '?' cannot be used as a default parameter because there are no standard conversions to type 'bool'
    //     void Test5(bool p = 51 is int x5 && 
    Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "p").WithArguments("?", "bool").WithLocation(14, 21),
    // (19,27): error CS1736: Default parameter value for 'p1' must be a compile-time constant
    //     void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "6 is int x6 && x6 > 0").WithArguments("p1").WithLocation(19, 27),
    // (19,60): error CS1736: Default parameter value for 'p2' must be a compile-time constant
    //     void Test61(bool p1 = 6 is int x6 && x6 > 0, bool p2 = 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "6 is int x6 && x6 > 0").WithArguments("p2").WithLocation(19, 60),
    // (22,26): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     void Test71(bool p = 7 is int x7 && x7 > 0)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "7 is int x7 && x7 > 0").WithArguments("p").WithLocation(22, 26),
    // (26,26): error CS0103: The name 'x7' does not exist in the current context
    //     void Test72(bool p = x7 > 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(26, 26),
    // (29,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(29, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref[0]);
            VerifyModelForDeclarationPattern(model, x6Decl[1], x6Ref[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Attribute_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(p = 3 is int x3 && x3 > 0)]
    [Test(p = x4 && 4 is int x4)]
    [Test(p = 51 is int x5 && 
              52 is int x5 && 
              x5 > 0)]
    [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    [Test(p = 7 is int x7 && x7 > 0)]
    [Test(p = x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

class Test : System.Attribute
{
    public bool p {get; set;}
    public bool p1 {get; set;}
    public bool p2 {get; set;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p = 3 is int x3 && x3 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "3 is int x3 && x3 > 0").WithLocation(8, 15),
    // (9,15): error CS0841: Cannot use local variable 'x4' before it is declared
    //     [Test(p = x4 && 4 is int x4)]
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 15),
    // (11,25): error CS0128: A local variable named 'x5' is already defined in this scope
    //               52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 25),
    // (13,53): error CS0128: A local variable named 'x6' is already defined in this scope
    //     [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(13, 53),
    // (13,16): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p1 = 6 is int x6 && x6 > 0, p2 = 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "6 is int x6 && x6 > 0").WithLocation(13, 16),
    // (14,15): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(p = 7 is int x7 && x7 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "7 is int x7 && x7 > 0").WithLocation(14, 15),
    // (15,15): error CS0103: The name 'x7' does not exist in the current context
    //     [Test(p = x7 > 2)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 15),
    // (16,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Attribute_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    [Test(3 is int x3 && x3 > 0)]
    [Test(x4 && 4 is int x4)]
    [Test(51 is int x5 && 
          52 is int x5 && 
          x5 > 0)]
    [Test(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)]
    [Test(7 is int x7 && x7 > 0)]
    [Test(x7 > 2)]
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

class Test : System.Attribute
{
    public Test(bool p) {}
    public Test(bool p1, bool p2) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (8,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(3 is int x3 && x3 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "3 is int x3 && x3 > 0").WithLocation(8, 11),
    // (9,11): error CS0841: Cannot use local variable 'x4' before it is declared
    //     [Test(x4 && 4 is int x4)]
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(9, 11),
    // (11,21): error CS0128: A local variable named 'x5' is already defined in this scope
    //           52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(11, 21),
    // (13,43): error CS0128: A local variable named 'x6' is already defined in this scope
    //     [Test(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)]
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(13, 43),
    // (14,11): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
    //     [Test(7 is int x7 && x7 > 0)]
    Diagnostic(ErrorCode.ERR_BadAttributeArgument, "7 is int x7 && x7 > 0").WithLocation(14, 11),
    // (15,11): error CS0103: The name 'x7' does not exist in the current context
    //     [Test(x7 > 2)]
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(15, 11),
    // (16,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(16, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    X(byte x)
        : this(3 is int x3 && x3 > 0)
    {}

    X(sbyte x)
        : this(x4 && 4 is int x4)
    {}

    X(short x)
        : this(51 is int x5 && 
               52 is int x5 && 
               x5 > 0)
    {}

    X(ushort x)
        : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    {}

    X(int x)
        : this(7 is int x7 && x7 > 0)
    {}
    X(uint x)
        : this(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    X(params object[] x) {}
    bool Dummy(params object[] x) {return true;}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(3 is int x3 && x3 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 21),
    // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
    //         : this(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
    // (13,27): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 27),
    // (17,22): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(17, 22),
    // (18,22): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(18, 22),
    // (18,26): error CS0128: A local variable named 'x5' is already defined in this scope
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 26),
    // (23,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(23, 21),
    // (23,44): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(23, 44),
    // (23,48): error CS0128: A local variable named 'x6' is already defined in this scope
    //         : this(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(23, 48),
    // (27,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : this(7 is int x7 && x7 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(27, 21),
    // (30,16): error CS0103: The name 'x7' does not exist in the current context
    //         : this(x7, 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
    // (32,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_02()
        {
            var source =
@"
public class X : Y
{
    public static void Main()
    {
    }

    X(byte x)
        : base(3 is int x3 && x3 > 0)
    {}

    X(sbyte x)
        : base(x4 && 4 is int x4)
    {}

    X(short x)
        : base(51 is int x5 && 
               52 is int x5 && 
               x5 > 0)
    {}

    X(ushort x)
        : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    {}

    X(int x)
        : base(7 is int x7 && x7 > 0)
    {}
    X(uint x)
        : base(x7, 2)
    {}
    void Test73() { Dummy(x7, 3); } 

    bool Dummy(params object[] x) {return true;}
}

public class Y
{
    public Y(params object[] x) {}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (9,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(3 is int x3 && x3 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x3").WithLocation(9, 21),
    // (13,16): error CS0841: Cannot use local variable 'x4' before it is declared
    //         : base(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(13, 16),
    // (13,27): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(x4 && 4 is int x4)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x4").WithLocation(13, 27),
    // (17,22): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(51 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(17, 22),
    // (18,22): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x5").WithLocation(18, 22),
    // (18,26): error CS0128: A local variable named 'x5' is already defined in this scope
    //                52 is int x5 && 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(18, 26),
    // (23,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(23, 21),
    // (23,44): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x6").WithLocation(23, 44),
    // (23,48): error CS0128: A local variable named 'x6' is already defined in this scope
    //         : base(6 is int x6 && x6 > 0, 6 is int x6 && x6 > 0)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(23, 48),
    // (27,21): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //         : base(7 is int x7 && x7 > 0)
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x7").WithLocation(27, 21),
    // (30,16): error CS0103: The name 'x7' does not exist in the current context
    //         : base(x7, 2)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(30, 16),
    // (32,27): error CS0103: The name 'x7' does not exist in the current context
    //     void Test73() { Dummy(x7, 3); } 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x7").WithArguments("x7").WithLocation(32, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").ToArray();
            var x5Ref = GetReferences(tree, "x5").Single();
            Assert.Equal(2, x5Decl.Length);
            VerifyModelForDeclarationPattern(model, x5Decl[0], x5Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl[1]);

            var x6Decl = GetPatternDeclarations(tree, "x6").ToArray();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Decl.Length);
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl[0], x6Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl[1]);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(3, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotInScope(model, x7Ref[1]);
            VerifyNotInScope(model, x7Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_03()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D
{
    public D(object o) : this(o is int x && x >= 5) 
    {
        Console.WriteLine(x);
    }

    public D(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27),
    // (13,36): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     public D(object o) : this(o is int x && x >= 5) 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(13, 36)
                );
        }

        [Fact]
        public void ScopeOfPatternVariables_ConstructorInitializers_04()
        {
            var source =
@"using System;
public class X
{
    public static void Main()
    {
        new D(1);
        new D(10);
        new D(1.2);
    }
}
class D : C
{
    public D(object o) : base(o is int x && x >= 5) 
    {
        Console.WriteLine(x);
    }
}

class C
{
    public C(bool b) { Console.WriteLine(b); }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (15,27): error CS0103: The name 'x' does not exist in the current context
    //         Console.WriteLine(x);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x").WithLocation(15, 27),
    // (13,36): error CS8200: Out variable and pattern variable declarations are not allowed within constructor initializers, field initializers, or property initializers.
    //     public D(object o) : base(o is int x && x >= 5) 
    Diagnostic(ErrorCode.ERR_ExpressionVariableInConstructorOrFieldInitializer, "int x").WithLocation(13, 36)
                );
        }

        [Fact]
        public void ScopeOfPatternVariables_SwitchLabelGuard_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test1(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
            case 1 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
            case 2 when Dummy(true is var x1, x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x2, true is var x2):
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x3, x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(int val)
    {
        var x4 = 11;
        switch (val)
        {
            case 0 when Dummy(true is var x4, x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4): Dummy(x4); break;
        }
    }

    void Test5(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x5, x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(int val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case 1 when Dummy(true is var x6, x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when Dummy(true is var x7, x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x8, x8, false is var x8, x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case 1 when Dummy(true is var x9, x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(int val)
    //{
    //    switch (val)
    //    {
    //        case 1 when Dummy(true is var x10, x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(int val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case 1 when Dummy(true is var x11, x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(int val)
    {
        switch (x12 ? val : 0)
        {
            case 0 when Dummy(true is var x12, x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (1 is var x13 ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case 1 when Dummy(true is var x13, x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(int val)
    {
        switch (val)
        {
            case 1 when Dummy(true is var x14, x14):
                Dummy(x14);
                Dummy(true is var x14, x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x15, x15):
            case 1 when Dummy(true is var x15, x15):
                Dummy(x15);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (30,31): error CS0841: Cannot use local variable 'x2' before it is declared
    //             case 0 when Dummy(x2, true is var x2):
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(30, 31),
    // (40,43): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x3, x3):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(40, 43),
    // (51,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x4, x4):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(51, 43),
    // (62,43): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 0 when Dummy(true is var x5, x5):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(62, 43),
    // (102,64): error CS0128: A local variable named 'x8' is already defined in this scope
    //             case 0 when Dummy(true is var x8, x8, false is var x8, x8):
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(102, 64),
    // (112,31): error CS0841: Cannot use local variable 'x9' before it is declared
    //             case 0 when Dummy(x9):
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(112, 31),
    // (119,43): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 1 when Dummy(true is var x9, x9):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(119, 43),
    // (144,17): error CS0103: The name 'x11' does not exist in the current context
    //         switch (x11 ? val : 0)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(144, 17),
    // (146,31): error CS0103: The name 'x11' does not exist in the current context
    //             case 0 when Dummy(x11):
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(146, 31),
    // (147,23): error CS0103: The name 'x11' does not exist in the current context
    //                 Dummy(x11, 0);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(147, 23),
    // (157,17): error CS0103: The name 'x12' does not exist in the current context
    //         switch (x12 ? val : 0)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(157, 17),
    // (162,31): error CS0103: The name 'x12' does not exist in the current context
    //             case 1 when Dummy(x12):
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(162, 31),
    // (163,23): error CS0103: The name 'x12' does not exist in the current context
    //                 Dummy(x12, 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(163, 23),
    // (175,43): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 1 when Dummy(true is var x13, x13):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(175, 43),
    // (185,43): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             case 1 when Dummy(true is var x14, x14):
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(185, 43),
    // (198,43): error CS0128: A local variable named 'x15' is already defined in this scope
    //             case 1 when Dummy(true is var x15, x15):
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(198, 43),
    // (198,48): error CS0165: Use of unassigned local variable 'x15'
    //             case 1 when Dummy(true is var x15, x15):
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(198, 48)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i * 2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[3]);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAPatternLocal(model, x5Ref[2]);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAPatternLocal(model, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);
            VerifyNotAPatternLocal(model, x9Ref[2]);
            VerifyNotAPatternLocal(model, x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = GetPatternDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPattern(model, x14Decl[1], true);

            var x15Decl = GetPatternDeclarations(tree, "x15").ToArray();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_SwitchLabelPattern_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    void Test1(object val)
    {
        switch (val)
        {
            case byte x1 when Dummy(x1):
                Dummy(x1);
                break;
            case int x1 when Dummy(x1):
                Dummy(x1);
                break;
            case long x1 when Dummy(x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(object val)
    {
        switch (val)
        {
            case 0 when Dummy(x2):
            case int x2:
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, object val)
    {
        switch (val)
        {
            case int x3 when Dummy(x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(object val)
    {
        var x4 = 11;
        switch (val)
        {
            case int x4 when Dummy(x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4):
                Dummy(x4);
                break;
        }
    }

    void Test5(object val)
    {
        switch (val)
        {
            case int x5 when Dummy(x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(object val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case int x6 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(object val)
    //{
    //    switch (val)
    //    {
    //        case int x7 when Dummy(x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(object val)
    {
        switch (val)
        {
            case int x8 
                    when Dummy(x8, false is var x8, x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(object val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case int x9 when Dummy(x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(object val)
    //{
    //    switch (val)
    //    {
    //        case int x10 when Dummy(x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(object val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case int x11 when Dummy(x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(object val)
    {
        switch (x12 ? val : 0)
        {
            case int x12 when Dummy(x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (1 is var x13 ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case int x13 when Dummy(x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(object val)
    {
        switch (val)
        {
            case int x14 when Dummy(x14):
                Dummy(x14);
                Dummy(true is var x14, x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(object val)
    {
        switch (val)
        {
            case int x15 when Dummy(x15):
            case long x15 when Dummy(x15):
                Dummy(x15);
                break;
        }
    }

    void Test16(object val)
    {
        switch (val)
        {
            case int x16 when Dummy(x16):
            case 1 when Dummy(true is var x16, x16):
                Dummy(x16);
                break;
        }
    }

    void Test17(object val)
    {
        switch (val)
        {
            case 0 when Dummy(true is var x17, x17):
            case int x17 when Dummy(x17):
                Dummy(x17);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (30,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //             case 0 when Dummy(x2):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(30, 31),
                // (32,23): error CS0165: Use of unassigned local variable 'x2'
                //                 Dummy(x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(32, 23),
                // (41,22): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x3 when Dummy(x3):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(41, 22),
                // (52,22): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x4 when Dummy(x4):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(52, 22),
                // (65,22): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x5 when Dummy(x5):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(65, 22),
                // (106,49): error CS0128: A local variable named 'x8' is already defined in this scope
                //                     when Dummy(x8, false is var x8, x8):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(106, 49),
                // (116,31): error CS0841: Cannot use local variable 'x9' before it is declared
                //             case 0 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(116, 31),
                // (123,22): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x9 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(123, 22),
                // (148,17): error CS0103: The name 'x11' does not exist in the current context
                //         switch (x11 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(148, 17),
                // (150,31): error CS0103: The name 'x11' does not exist in the current context
                //             case 0 when Dummy(x11):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(150, 31),
                // (151,23): error CS0103: The name 'x11' does not exist in the current context
                //                 Dummy(x11, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(151, 23),
                // (161,17): error CS0103: The name 'x12' does not exist in the current context
                //         switch (x12 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(161, 17),
                // (166,31): error CS0103: The name 'x12' does not exist in the current context
                //             case 1 when Dummy(x12):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(166, 31),
                // (167,23): error CS0103: The name 'x12' does not exist in the current context
                //                 Dummy(x12, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(167, 23),
                // (179,22): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x13 when Dummy(x13):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(179, 22),
                // (189,22): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case int x14 when Dummy(x14):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(189, 22),
                // (202,23): error CS0128: A local variable named 'x15' is already defined in this scope
                //             case long x15 when Dummy(x15):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(202, 23),
                // (202,38): error CS0165: Use of unassigned local variable 'x15'
                //             case long x15 when Dummy(x15):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(202, 38),
                // (213,43): error CS0128: A local variable named 'x16' is already defined in this scope
                //             case 1 when Dummy(true is var x16, x16):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x16").WithArguments("x16").WithLocation(213, 43),
                // (213,48): error CS0165: Use of unassigned local variable 'x16'
                //             case 1 when Dummy(true is var x16, x16):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x16").WithArguments("x16").WithLocation(213, 48),
                // (224,22): error CS0128: A local variable named 'x17' is already defined in this scope
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x17").WithArguments("x17").WithLocation(224, 22),
                // (224,37): error CS0165: Use of unassigned local variable 'x17'
                //             case int x17 when Dummy(x17):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x17").WithArguments("x17").WithLocation(224, 37)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i * 2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[3]);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAPatternLocal(model, x5Ref[2]);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAPatternLocal(model, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);
            VerifyNotAPatternLocal(model, x9Ref[2]);
            VerifyNotAPatternLocal(model, x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = GetPatternDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPattern(model, x14Decl[1], true);

            var x15Decl = GetPatternDeclarations(tree, "x15").ToArray();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl[1]);

            var x16Decl = GetPatternDeclarations(tree, "x16").ToArray();
            var x16Ref = GetReferences(tree, "x16").ToArray();
            Assert.Equal(2, x16Decl.Length);
            Assert.Equal(3, x16Ref.Length);
            for (int i = 0; i < x16Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x16Decl[0], x16Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x16Decl[1]);

            var x17Decl = GetPatternDeclarations(tree, "x17").ToArray();
            var x17Ref = GetReferences(tree, "x17").ToArray();
            Assert.Equal(2, x17Decl.Length);
            Assert.Equal(3, x17Ref.Length);
            for (int i = 0; i < x17Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x17Decl[0], x17Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x17Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Switch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        switch (1 is var x1 ? x1 : 0)
        {
            case 0:
                Dummy(x1, 0);
                break;
        }

        Dummy(x1, 1);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        switch (4 is var x4 ? x4 : 0)
        {
            case 4:
                Dummy(x4);
                break;
        }
    }

    void Test5(int x5)
    {
        switch (5 is var x5 ? x5 : 0)
        {
            case 5:
                Dummy(x5);
                break;
        }
    }

    void Test6()
    {
        switch (x6 + 6 is var x6 ? x6 : 0)
        {
            case 6:
                Dummy(x6);
                break;
        }
    }

    void Test7()
    {
        switch (7 is var x7 ? x7 : 0)
        {
            case 7:
                var x7 = 12;
                Dummy(x7);
                break;
        }
    }

    void Test9()
    {
        switch (9 is var x9 ? x9 : 0)
        {
            case 9:
                Dummy(x9, 0);
                switch (9 is var x9 ? x9 : 0)
                {
                    case 9:
                        Dummy(x9, 1);
                        break;
                }
                break;
        }

    }

    void Test10()
    {
        switch (y10 + 10 is var x10 ? x10 : 0)
        {
            case 0 when y10:
                break;
            case y10:
                var y10 = 12;
                Dummy(y10);
                break;
        }
    }

    //void Test11()
    //{
    //    switch (y11 + 11 is var x11 ? x11 : 0)
    //    {
    //        case 0 when y11 > 0:
    //            break;
    //        case y11:
    //            let y11 = 12;
    //            Dummy(y11);
    //            break;
    //    }
    //}

    void Test14()
    {
        switch (Dummy(1 is var x14, 
                  2 is var x14, 
                  x14) ? 1 : 0)
        {
            case 0:
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when y15 > 0:
                break;
            case y15: 
                var y15 = 15;
                Dummy(y15);
                break;
        }
    }

    //void Test16(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when y16 > 0:
    //            break;
    //        case y16: 
    //            let y16 = 16;
    //            Dummy(y16);
    //            break;
    //    }
    //}
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (27,26): error CS0128: A local variable named 'x4' is already defined in this scope
                //         switch (4 is var x4 ? x4 : 0)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(27, 26),
                // (37,26): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         switch (5 is var x5 ? x5 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(37, 26),
                // (47,17): error CS0841: Cannot use local variable 'x6' before it is declared
                //         switch (x6 + 6 is var x6 ? x6 : 0)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(47, 17),
                // (60,21): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 var x7 = 12;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(60, 21),
                // (71,23): error CS0841: Cannot use local variable 'x9' before it is declared
                //                 Dummy(x9, 0);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(71, 23),
                // (72,34): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 switch (9 is var x9 ? x9 : 0)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(72, 34),
                // (85,17): error CS0103: The name 'y10' does not exist in the current context
                //         switch (y10 + 10 is var x10 ? x10 : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 17),
                // (87,25): error CS0841: Cannot use local variable 'y10' before it is declared
                //             case 0 when y10:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y10").WithArguments("y10").WithLocation(87, 25),
                // (89,18): error CS0841: Cannot use local variable 'y10' before it is declared
                //             case y10:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y10").WithArguments("y10").WithLocation(89, 18),
                // (89,18): error CS0150: A constant value is expected
                //             case y10:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "y10").WithLocation(89, 18),
                // (112,28): error CS0128: A local variable named 'x14' is already defined in this scope
                //                   2 is var x14, 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(112, 28),
                // (125,25): error CS0841: Cannot use local variable 'y15' before it is declared
                //             case 0 when y15 > 0:
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y15").WithArguments("y15").WithLocation(125, 25),
                // (127,18): error CS0841: Cannot use local variable 'y15' before it is declared
                //             case y15: 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y15").WithArguments("y15").WithLocation(127, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(3, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(4, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);
            VerifyNotAPatternLocal(model, y10Ref[2]);
            VerifyNotAPatternLocal(model, y10Ref[3]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var y15Ref = GetReferences(tree, "y15").ToArray();
            Assert.Equal(3, y15Ref.Length);
            VerifyNotAPatternLocal(model, y15Ref[0]);
            VerifyNotAPatternLocal(model, y15Ref[1]);
            VerifyNotAPatternLocal(model, y15Ref[2]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Switch_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true)
            switch (1 is var x1 ? 1 : 0)
            {
                case 0:
                    break;
            }

        Dummy(x1, 1);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (19,15): error CS0103: The name 'x1' does not exist in the current context
                //         Dummy(x1, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(19, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Switch_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (SwitchStatementSyntax)SyntaxFactory.ParseStatement(@"
switch (Dummy(11 is var x1, x1)) {}
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,34): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 34),
    // (35,22): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 22),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,38): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 38),
    // (68,22): error CS0103: The name 'y10' does not exist in the current context
    //         using (Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 22),
    // (86,22): error CS0103: The name 'y12' does not exist in the current context
    //         using (Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 22),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 31)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var d = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (var d = Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (var d = Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (var d = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (var d = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (var d = Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (var d = Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (var e = Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (var d = Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (var d = Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (var d = Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (var d = Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (var d = Dummy(1 is var x14, 
                             2 is var x14, 
                             x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,42): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var d = Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 42),
    // (35,30): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (var d = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 30),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,46): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (var e = Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 46),
    // (68,30): error CS0103: The name 'y10' does not exist in the current context
    //         using (var d = Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 30),
    // (86,30): error CS0103: The name 'y12' does not exist in the current context
    //         using (var d = Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 30),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,39): error CS0128: A local variable named 'x14' is already defined in this scope
    //                              2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 39)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d = Dummy(true is var x2, x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        using (System.IDisposable d = Dummy(true is var x4, x4))
            Dummy(x4);
    }

    void Test6()
    {
        using (System.IDisposable d = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        using (System.IDisposable d = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        using (System.IDisposable d = Dummy(true is var x8, x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        using (System.IDisposable d = Dummy(true is var x9, x9))
        {   
            Dummy(x9);
            using (System.IDisposable c = Dummy(true is var x9, x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        using (System.IDisposable d = Dummy(y10 is var x10, x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    using (System.IDisposable d = Dummy(y11 is var x11, x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        using (System.IDisposable d = Dummy(y12 is var x12, x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    using (System.IDisposable d = Dummy(y13 is var x13, x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        using (System.IDisposable d = Dummy(1 is var x14, 
                                            2 is var x14, 
                                            x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,57): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (System.IDisposable d = Dummy(true is var x4, x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 57),
    // (35,45): error CS0841: Cannot use local variable 'x6' before it is declared
    //         using (System.IDisposable d = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 45),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,61): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             using (System.IDisposable c = Dummy(true is var x9, x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 61),
    // (68,45): error CS0103: The name 'y10' does not exist in the current context
    //         using (System.IDisposable d = Dummy(y10 is var x10, x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 45),
    // (86,45): error CS0103: The name 'y12' does not exist in the current context
    //         using (System.IDisposable d = Dummy(y12 is var x12, x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 45),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,54): error CS0128: A local variable named 'x14' is already defined in this scope
    //                                             2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 54)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var x10Decl = GetPatternDeclarations(tree, "x10").Single();
            var x10Ref = GetReferences(tree, "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (var x1 = Dummy(true is var x1, x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable x2 = Dummy(true is var x2, x2))
        {
            Dummy(x2);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (12,43): error CS0128: A local variable named 'x1' is already defined in this scope
                //         using (var x1 = Dummy(true is var x1, x1))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(12, 43),
                // (12,47): error CS0841: Cannot use local variable 'x1' before it is declared
                //         using (var x1 = Dummy(true is var x1, x1))
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(12, 47),
                // (12,47): error CS0165: Use of unassigned local variable 'x1'
                //         using (var x1 = Dummy(true is var x1, x1))
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(12, 47),
                // (20,58): error CS0128: A local variable named 'x2' is already defined in this scope
                //         using (System.IDisposable x2 = Dummy(true is var x2, x2))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 58),
                // (20,62): error CS0165: Use of unassigned local variable 'x2'
                //         using (System.IDisposable x2 = Dummy(true is var x2, x2))
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(20, 62)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
            VerifyNotAPatternLocal(model, x2Ref[0]);
            VerifyNotAPatternLocal(model, x2Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Using_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.IDisposable Dummy(params object[] x) {return null;}

    void Test1()
    {
        using (System.IDisposable d = Dummy(true is var x1, x1), 
                                  x1 = Dummy(x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        using (System.IDisposable d1 = Dummy(true is var x2, x2), 
                                  d2 = Dummy(true is var x2, x2))
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        using (System.IDisposable d1 = Dummy(true is var x3, x3), 
                                  d2 = Dummy(x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        using (System.IDisposable d1 = Dummy(x4), 
                                  d2 = Dummy(true is var x4, x4))
        {
            Dummy(x4);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,35): error CS0128: A local variable named 'x1' is already defined in this scope
    //                                   x1 = Dummy(x1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 35),
    // (22,58): error CS0128: A local variable named 'x2' is already defined in this scope
    //                                   d2 = Dummy(true is var x2, x2))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(22, 58),
    // (39,46): error CS0841: Cannot use local variable 'x4' before it is declared
    //         using (System.IDisposable d1 = Dummy(x4), 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(39, 46)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").ToArray();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(3, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(3, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var d = Dummy(true is var x1, x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        var d = Dummy(true is var x4, x4);
    }

    void Test6()
    {
        var d = Dummy(x6 && true is var x6);
    }

    void Test8()
    {
        var d = Dummy(true is var x8, x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        var d = Dummy(1 is var x14, 
                      2 is var x14, 
                      x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (19,35): error CS0128: A local variable named 'x4' is already defined in this scope
    //         var d = Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(19, 35),
    // (24,23): error CS0841: Cannot use local variable 'x6' before it is declared
    //         var d = Dummy(x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 23),
    // (36,32): error CS0128: A local variable named 'x14' is already defined in this scope
    //                       2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 32)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(true is var x1, x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        object d = Dummy(true is var x4, x4);
    }

    void Test6()
    {
        object d = Dummy(x6 && true is var x6);
    }

    void Test8()
    {
        object d = Dummy(true is var x8, x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        object d = Dummy(1 is var x14, 
                         2 is var x14, 
                         x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (19,38): error CS0128: A local variable named 'x4' is already defined in this scope
    //         object d = Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(19, 38),
    // (24,26): error CS0841: Cannot use local variable 'x6' before it is declared
    //         object d = Dummy(x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 26),
    // (36,35): error CS0128: A local variable named 'x14' is already defined in this scope
    //                          2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 35)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var x1 = 
                 Dummy(true is var x1, x1);
        Dummy(x1);
    }

    void Test2()
    {
        object x2 = 
                    Dummy(true is var x2, x2);
        Dummy(x2);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,36): error CS0128: A local variable named 'x1' is already defined in this scope
    //                  Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 36),
    // (13,40): error CS0841: Cannot use local variable 'x1' before it is declared
    //                  Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(13, 40),
    // (13,40): error CS0165: Use of unassigned local variable 'x1'
    //                  Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(13, 40),
    // (20,39): error CS0128: A local variable named 'x2' is already defined in this scope
    //                     Dummy(true is var x2, x2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 39),
    // (20,43): error CS0165: Use of unassigned local variable 'x2'
    //                     Dummy(true is var x2, x2);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(20, 43)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyNotAPatternLocal(model, x2Ref[0]);
            VerifyNotAPatternLocal(model, x2Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

   object Dummy(params object[] x) {return null;}

    void Test1()
    {
        object d = Dummy(true is var x1, x1), 
               x1 = Dummy(x1);
        Dummy(x1);
    }

    void Test2()
    {
        object d1 = Dummy(true is var x2, x2), 
               d2 = Dummy(true is var x2, x2);
    }

    void Test3()
    {
        object d1 = Dummy(true is var x3, x3), 
               d2 = Dummy(x3);
    }

    void Test4()
    {
        object d1 = Dummy(x4), 
               d2 = Dummy(true is var x4, x4);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,16): error CS0128: A local variable named 'x1' is already defined in this scope
    //                x1 = Dummy(x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 16),
    // (20,39): error CS0128: A local variable named 'x2' is already defined in this scope
    //                d2 = Dummy(true is var x2, x2);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 39),
    // (31,27): error CS0841: Cannot use local variable 'x4' before it is declared
    //         object d1 = Dummy(x4), 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(31, 27)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").ToArray();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    long Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (LocalDeclarationStatementSyntax)SyntaxFactory.ParseStatement(@"
var y1 = Dummy(11 is var x1, x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());

            Assert.Equal("System.Int64 y1", model.LookupSymbols(x1Ref[0].SpanStart, name: "y1").Single().ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_LocalDeclarationStmt_06()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1()
    {
        if (true)
            var d = true is var x1;

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (11,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var d = true is var x1;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var d = true is var x1;").WithLocation(11, 13),
                // (13,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(13, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);

            var d = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(id => id.Identifier.ValueText == "d").Single();
            Assert.Equal("System.Boolean d", model.GetDeclaredSymbol(d).ToTestDisplayString());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var (d, dd) = ((true is var x1), x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        var (d, dd) = ((true is var x4), x4);
    }

    void Test6()
    {
        var (d, dd) = (x6 && (true is var x6), 1);
    }

    void Test8()
    {
        var (d, dd) = ((true is var x8), x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        var (d, dd, ddd) = ((1 is var x14), 
                      (2 is var x14), 
                      x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,37): error CS0128: A local variable named 'x4' is already defined in this scope
                //         var (d, dd) = ((true is var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(19, 37),
                // (24,24): error CS0841: Cannot use local variable 'x6' before it is declared
                //         var (d, dd) = (x6 && (true is var x6), 1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 24),
                // (36,33): error CS0128: A local variable named 'x14' is already defined in this scope
                //                       (2 is var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 33)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        (object d, object dd) = ((true is var x1), x1);
    }
    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        (object d, object dd) = ((true is var x4), x4);
    }

    void Test6()
    {
        (object d, object dd) = (x6 && (true is var x6), 1);
    }

    void Test8()
    {
        (object d, object dd) = ((true is var x8), x8);
        System.Console.WriteLine(x8);
    }

    void Test14()
    {
        (object d, object dd, object ddd) = ((1 is var x14), 
                      (2 is var x14), 
                      x14);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (19,47): error CS0128: A local variable named 'x4' is already defined in this scope
                //         (object d, object dd) = ((true is var x4), x4);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(19, 47),
                // (24,34): error CS0841: Cannot use local variable 'x6' before it is declared
                //         (object d, object dd) = (x6 && (true is var x6), 1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(24, 34),
                // (36,33): error CS0128: A local variable named 'x14' is already defined in this scope
                //                       (2 is var x14), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(36, 33)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x6").Single();
            var x6Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x6").Single();
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").Single();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x14Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x14").ToArray();
            var x14Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x14").Single();
            Assert.Equal(2, x14Decl.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        var (x1, dd) = 
                      ((true is var x1), x1);
        Dummy(x1);
    }

    void Test2()
    {
        (object x2, object dd) = 
                         ((true is var x2), x2);
        Dummy(x2);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (13,37): error CS0128: A local variable named 'x1' is already defined in this scope
                //                       ((true is var x1), x1);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 37),
                // (13,42): error CS0841: Cannot use local variable 'x1' before it is declared
                //                       ((true is var x1), x1);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(13, 42),
                // (13,42): error CS0165: Use of unassigned local variable 'x1'
                //                       ((true is var x1), x1);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(13, 42),
                // (20,40): error CS0128: A local variable named 'x2' is already defined in this scope
                //                          ((true is var x2), x2);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 40),
                // (20,45): error CS0165: Use of unassigned local variable 'x2'
                //                          ((true is var x2), x2);
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(20, 45)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyNotAPatternLocal(model, x2Ref[0]);
            VerifyNotAPatternLocal(model, x2Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        (object d, object x1) = (Dummy((true is var x1), x1), 
                                Dummy(x1));
        Dummy(x1);
    }

    void Test2()
    {
        (object d1, object d2) = (Dummy((true is var x2), x2), 
                    Dummy((true is var x2), x2));
    }

    void Test3()
    {
        (object d1, object d2) = (Dummy((true is var x3), x3), 
                    Dummy(x3));
    }

    void Test4()
    {
        (object d1, object d2) = (Dummy(x4), 
                    Dummy((true is var x4), x4));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (12,53): error CS0128: A local variable named 'x1' is already defined in this scope
                //         (object d, object x1) = (Dummy((true is var x1), x1), 
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(12, 53),
                // (12,58): error CS0165: Use of unassigned local variable 'x1'
                //         (object d, object x1) = (Dummy((true is var x1), x1), 
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(12, 58),
                // (20,40): error CS0128: A local variable named 'x2' is already defined in this scope
                //                     Dummy((true is var x2), x2));
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(20, 40),
                // (31,41): error CS0841: Cannot use local variable 'x4' before it is declared
                //         (object d1, object d2) = (Dummy(x4), 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x4").WithArguments("x4").WithLocation(31, 41)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Ref.Length);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);
            VerifyNotAPatternLocal(model, x1Ref[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").ToArray();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").ToArray();
            Assert.Equal(2, x2Decl.Length);
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl[0], x2Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl[1]);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (DeconstructionDeclarationStatementSyntax)SyntaxFactory.ParseStatement(@"
var (y1, dd) = ((123 is var x1), x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "SpeculateHere").Single().SpanStart, statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());

            Assert.Equal("System.Boolean y1", model.LookupSymbols(x1Ref[0].SpanStart, name: "y1").Single().ToTestDisplayString());
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Tuples)]
        public void ScopeOfPatternVariables_DeconstructionDeclarationStmt_06()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Test1()
    {
        if (true)
            var (d, dd) = ((true is var x1), x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef },
                                                              options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (11,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //             var (d, dd) = ((true is var x1), x1);
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var (d, dd) = ((true is var x1), x1);").WithLocation(11, 13),
                // (13,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(13, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref[0]);
            VerifyNotInScope(model, x1Ref[1]);

            var d = tree.GetRoot().DescendantNodes().OfType<SingleVariableDesignationSyntax>().Where(id => id.Identifier.ValueText == "d").Single();
            Assert.Equal("System.Boolean d", model.GetDeclaredSymbol(d).ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_While_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        while (true is var x1 && x1)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        while (true is var x2 && x2)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        while (true is var x4 && x4 > 0)
            Dummy(x4);
    }

    void Test6()
    {
        while (x6 && true is var x6)
            Dummy(x6);
    }

    void Test7()
    {
        while (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        while (true is var x8 && x8)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        while (true is var x9 && x9)
        {   
            Dummy(x9);
            while (true is var x9 && x9) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        while (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    while (y11 is var x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        while (y12 is var x12)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    while (y13 is var x13)
    //        let y13 = 12;
    //}

    void Test14()
    {
        while (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,28): error CS0128: A local variable named 'x4' is already defined in this scope
    //         while (true is var x4 && x4 > 0)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(29, 28),
    // (35,16): error CS0841: Cannot use local variable 'x6' before it is declared
    //         while (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 16),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (60,19): error CS0841: Cannot use local variable 'x9' before it is declared
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(60, 19),
    // (61,32): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             while (true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 32),
    // (68,16): error CS0103: The name 'y10' does not exist in the current context
    //         while (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 16),
    // (86,16): error CS0103: The name 'y12' does not exist in the current context
    //         while (y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 16),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 31)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_While_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true)
            while (true is var x1)
            {
            }
        
        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (17,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(17, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_While_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (WhileStatementSyntax)SyntaxFactory.ParseStatement(@"
while (Dummy(11 is var x1, x1)) ;
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_Do_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        do
        {
            Dummy(x1);
        }
        while (true is var x1 && x1);
    }

    void Test2()
    {
        do
            Dummy(x2);
        while (true is var x2 && x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        do
            Dummy(x4);
        while (true is var x4 && x4 > 0);
    }

    void Test6()
    {
        do
            Dummy(x6);
        while (x6 && true is var x6);
    }

    void Test7()
    {
        do
        {
            var x7 = 12;
            Dummy(x7);
        }
        while (true is var x7 && x7);
    }

    void Test8()
    {
        do
            Dummy(x8);
        while (true is var x8 && x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        do
        {   
            Dummy(x9);
            do
                Dummy(x9);
            while (true is var x9 && x9); // 2
        }
        while (true is var x9 && x9);
    }

    void Test10()
    {
        do
        {   
            var y10 = 12;
            Dummy(y10);
        }
        while (y10 is var x10);
    }

    //void Test11()
    //{
    //    do
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //    while (y11 is var x11);
    //}

    void Test12()
    {
        do
            var y12 = 12;
        while (y12 is var x12);
    }

    //void Test13()
    //{
    //    do
    //        let y13 = 12;
    //    while (y13 is var x13);
    //}

    void Test14()
    {
        do
        {
            Dummy(x14);
        }
        while (Dummy(1 is var x14, 
                     2 is var x14, 
                     x14));
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (97,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(97, 13),
    // (14,19): error CS0841: Cannot use local variable 'x1' before it is declared
    //             Dummy(x1);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(14, 19),
    // (22,19): error CS0841: Cannot use local variable 'x2' before it is declared
    //             Dummy(x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(22, 19),
    // (33,28): error CS0128: A local variable named 'x4' is already defined in this scope
    //         while (true is var x4 && x4 > 0);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(33, 28),
    // (40,16): error CS0841: Cannot use local variable 'x6' before it is declared
    //         while (x6 && true is var x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(40, 16),
    // (39,19): error CS0841: Cannot use local variable 'x6' before it is declared
    //             Dummy(x6);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(39, 19),
    // (47,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(47, 17),
    // (56,19): error CS0841: Cannot use local variable 'x8' before it is declared
    //             Dummy(x8);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x8").WithArguments("x8").WithLocation(56, 19),
    // (66,19): error CS0841: Cannot use local variable 'x9' before it is declared
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(66, 19),
    // (69,32): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             while (true is var x9 && x9); // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(69, 32),
    // (68,23): error CS0841: Cannot use local variable 'x9' before it is declared
    //                 Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(68, 23),
    // (81,16): error CS0103: The name 'y10' does not exist in the current context
    //         while (y10 is var x10);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(81, 16),
    // (98,16): error CS0103: The name 'y12' does not exist in the current context
    //         while (y12 is var x12);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(98, 16),
    // (97,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(97, 17),
    // (115,31): error CS0128: A local variable named 'x14' is already defined in this scope
    //                      2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(115, 31),
    // (112,19): error CS0841: Cannot use local variable 'x14' before it is declared
    //             Dummy(x14);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(112, 19)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[1]);
            VerifyNotAPatternLocal(model, x7Ref[0]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1], x9Ref[2]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[1]);
            VerifyNotAPatternLocal(model, y10Ref[0]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Do_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        if (true)
            do
            {
            }
            while (true is var x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (18,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(18, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Do_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (DoStatementSyntax)SyntaxFactory.ParseStatement(@"
do {} while (Dummy(11 is var x1, x1));
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_For_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (109,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(109, 17),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (;
             Dummy(true is var x1 && x1)
             ;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;
             Dummy(true is var x2 && x2)
             ;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;
             Dummy(true is var x4 && x4)
             ;)
            Dummy(x4);
    }

    void Test6()
    {
        for (;
             Dummy(x6 && true is var x6)
             ;)
            Dummy(x6);
    }

    void Test7()
    {
        for (;
             Dummy(true is var x7 && x7)
             ;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;
             Dummy(true is var x8 && x8)
             ;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;
             Dummy(true is var x9 && x9)
             ;)
        {   
            Dummy(x9);
            for (;
                 Dummy(true is var x9 && x9) // 2
                 ;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;
             Dummy(y10 is var x10)
             ;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;
    //         Dummy(y11 is var x11)
    //         ;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;
             Dummy(y12 is var x12)
             ;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;
    //         Dummy(y13 is var x13)
    //         ;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (109,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(109, 17),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (;;
             Dummy(true is var x1 && x1)
             )
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (;;
             Dummy(true is var x2 && x2)
             )
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (;;
             Dummy(true is var x4 && x4)
             )
            Dummy(x4);
    }

    void Test6()
    {
        for (;;
             Dummy(x6 && true is var x6)
             )
            Dummy(x6);
    }

    void Test7()
    {
        for (;;
             Dummy(true is var x7 && x7)
             )
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (;;
             Dummy(true is var x8 && x8)
             )
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (;;
             Dummy(true is var x9 && x9)
             )
        {   
            Dummy(x9);
            for (;;
                 Dummy(true is var x9 && x9) // 2
                 )
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (;;
             Dummy(y10 is var x10)
             )
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (;;
    //         Dummy(y11 is var x11)
    //         )
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (;;
             Dummy(y12 is var x12)
             )
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (;;
    //         Dummy(y13 is var x13)
    //         )
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (;;
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             )
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (109,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(109, 17),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29),
    // (16,19): error CS0165: Use of unassigned local variable 'x1'
    //             Dummy(x1);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(16, 19),
    // (25,19): error CS0165: Use of unassigned local variable 'x2'
    //             Dummy(x2);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x2").WithArguments("x2").WithLocation(25, 19),
    // (36,19): error CS0165: Use of unassigned local variable 'x4'
    //             Dummy(x4);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(36, 19),
    // (44,19): error CS0165: Use of unassigned local variable 'x6'
    //             Dummy(x6);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x6").WithArguments("x6").WithLocation(44, 19),
    // (63,19): error CS0165: Use of unassigned local variable 'x8'
    //             Dummy(x8);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x8").WithArguments("x8").WithLocation(63, 19),
    // (71,14): warning CS0162: Unreachable code detected
    //              Dummy(true is var x9 && x9)
    Diagnostic(ErrorCode.WRN_UnreachableCode, "Dummy").WithLocation(71, 14),
    // (74,19): error CS0165: Use of unassigned local variable 'x9'
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(74, 19),
    // (78,23): error CS0165: Use of unassigned local variable 'x9'
    //                 Dummy(x9);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x9").WithArguments("x9").WithLocation(78, 23),
    // (128,19): error CS0165: Use of unassigned local variable 'x14'
    //             Dummy(x14);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x14").WithArguments("x14").WithLocation(128, 19)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_04()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (var b =
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (var b =
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (var b =
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (var b =
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (var b =
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (var b =
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (var b1 =
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (var b2 =
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (var b =
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (var b =
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (var b =
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (var b =
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (var b =
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (109,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(109, 17),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_05()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (bool b =
             Dummy(true is var x1 && x1)
             ;;)
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        for (bool b =
             Dummy(true is var x2 && x2)
             ;;)
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        for (bool b =
             Dummy(true is var x4 && x4)
             ;;)
            Dummy(x4);
    }

    void Test6()
    {
        for (bool b =
             Dummy(x6 && true is var x6)
             ;;)
            Dummy(x6);
    }

    void Test7()
    {
        for (bool b =
             Dummy(true is var x7 && x7)
             ;;)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        for (bool b =
             Dummy(true is var x8 && x8)
             ;;)
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        for (bool b1 =
             Dummy(true is var x9 && x9)
             ;;)
        {   
            Dummy(x9);
            for (bool b2 =
                 Dummy(true is var x9 && x9) // 2
                 ;;)
                Dummy(x9);
        }
    }

    void Test10()
    {
        for (bool b =
             Dummy(y10 is var x10)
             ;;)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    for (bool b =
    //         Dummy(y11 is var x11)
    //         ;;)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        for (bool b =
             Dummy(y12 is var x12)
             ;;)
            var y12 = 12;
    }

    //void Test13()
    //{
    //    for (bool b =
    //         Dummy(y13 is var x13)
    //         ;;)
    //        let y13 = 12;
    //}

    void Test14()
    {
        for (bool b =
             Dummy(1 is var x14, 
                   2 is var x14, 
                   x14)
             ;;)
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (109,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(109, 13),
    // (34,32): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(34, 32),
    // (42,20): error CS0841: Cannot use local variable 'x6' before it is declared
    //              Dummy(x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(42, 20),
    // (53,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(53, 17),
    // (65,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(65, 34),
    // (65,9): warning CS0162: Unreachable code detected
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.WRN_UnreachableCode, "System").WithLocation(65, 9),
    // (76,36): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                  Dummy(true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(76, 36),
    // (85,20): error CS0103: The name 'y10' does not exist in the current context
    //              Dummy(y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(85, 20),
    // (107,20): error CS0103: The name 'y12' does not exist in the current context
    //              Dummy(y12 is var x12)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(107, 20),
    // (109,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(109, 17),
    // (124,29): error CS0128: A local variable named 'x14' is already defined in this scope
    //                    2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(124, 29)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_For_06()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (var x1 =
             Dummy(true is var x1 && x1)
             ;;)
        {}
    }

    void Test2()
    {
        for (var x2 = true;
             Dummy(true is var x2 && x2)
             ;)
        {}
    }

    void Test3()
    {
        for (var x3 = true;;
             Dummy(true is var x3 && x3)
             )
        {}
    }

    void Test4()
    {
        for (bool x4 =
             Dummy(true is var x4 && x4)
             ;;)
        {}
    }

    void Test5()
    {
        for (bool x5 = true;
             Dummy(true is var x5 && x5)
             ;)
        {}
    }

    void Test6()
    {
        for (bool x6 = true;;
             Dummy(true is var x6 && x6)
             )
        {}
    }

    void Test7()
    {
        for (bool x7 = true, b =
             Dummy(true is var x7 && x7)
             ;;)
        {}
    }

    void Test8()
    {
        for (bool b1 = Dummy(true is var x8 && x8), 
             b2 = Dummy(true is var x8 && x8);
             Dummy(true is var x8 && x8);
             Dummy(true is var x8 && x8))
        {}
    }

    void Test9()
    {
        for (bool b = x9, 
             b2 = Dummy(true is var x9 && x9);
             Dummy(true is var x9 && x9);
             Dummy(true is var x9 && x9))
        {}
    }

    void Test10()
    {
        for (var b = x10;
             Dummy(true is var x10 && x10) &&
             Dummy(true is var x10 && x10);
             Dummy(true is var x10 && x10))
        {}
    }

    void Test11()
    {
        for (bool b = x11;
             Dummy(true is var x11 && x11) &&
             Dummy(true is var x11 && x11);
             Dummy(true is var x11 && x11))
        {}
    }

    void Test12()
    {
        for (Dummy(x12);
             Dummy(x12) &&
             Dummy(true is var x12 && x12);
             Dummy(true is var x12 && x12))
        {}
    }

    void Test13()
    {
        for (var b = x13;
             Dummy(x13);
             Dummy(true is var x13 && x13),
             Dummy(true is var x13 && x13))
        {}
    }

    void Test14()
    {
        for (bool b = x14;
             Dummy(x14);
             Dummy(true is var x14 && x14),
             Dummy(true is var x14 && x14))
        {}
    }

    void Test15()
    {
        for (Dummy(x15);
             Dummy(x15);
             Dummy(x15),
             Dummy(true is var x15 && x15))
        {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (13,32): error CS0128: A local variable named 'x1' is already defined in this scope
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(13, 32),
    // (13,38): error CS0841: Cannot use local variable 'x1' before it is declared
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(13, 38),
    // (13,38): error CS0165: Use of unassigned local variable 'x1'
    //              Dummy(true is var x1 && x1)
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(13, 38),
    // (21,32): error CS0128: A local variable named 'x2' is already defined in this scope
    //              Dummy(true is var x2 && x2)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(21, 32),
    // (29,32): error CS0128: A local variable named 'x3' is already defined in this scope
    //              Dummy(true is var x3 && x3)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(29, 32),
    // (37,32): error CS0128: A local variable named 'x4' is already defined in this scope
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(37, 32),
    // (37,38): error CS0165: Use of unassigned local variable 'x4'
    //              Dummy(true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(37, 38),
    // (45,32): error CS0128: A local variable named 'x5' is already defined in this scope
    //              Dummy(true is var x5 && x5)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(45, 32),
    // (53,32): error CS0128: A local variable named 'x6' is already defined in this scope
    //              Dummy(true is var x6 && x6)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(53, 32),
    // (61,32): error CS0128: A local variable named 'x7' is already defined in this scope
    //              Dummy(true is var x7 && x7)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x7").WithArguments("x7").WithLocation(61, 32),
    // (69,37): error CS0128: A local variable named 'x8' is already defined in this scope
    //              b2 = Dummy(true is var x8 && x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(69, 37),
    // (70,32): error CS0128: A local variable named 'x8' is already defined in this scope
    //              Dummy(true is var x8 && x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(70, 32),
    // (71,32): error CS0128: A local variable named 'x8' is already defined in this scope
    //              Dummy(true is var x8 && x8))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(71, 32),
    // (77,23): error CS0841: Cannot use local variable 'x9' before it is declared
    //         for (bool b = x9, 
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(77, 23),
    // (79,32): error CS0128: A local variable named 'x9' is already defined in this scope
    //              Dummy(true is var x9 && x9);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(79, 32),
    // (80,32): error CS0128: A local variable named 'x9' is already defined in this scope
    //              Dummy(true is var x9 && x9))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(80, 32),
    // (86,22): error CS0841: Cannot use local variable 'x10' before it is declared
    //         for (var b = x10;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x10").WithArguments("x10").WithLocation(86, 22),
    // (88,32): error CS0128: A local variable named 'x10' is already defined in this scope
    //              Dummy(true is var x10 && x10);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(88, 32),
    // (89,32): error CS0128: A local variable named 'x10' is already defined in this scope
    //              Dummy(true is var x10 && x10))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(89, 32),
    // (95,23): error CS0841: Cannot use local variable 'x11' before it is declared
    //         for (bool b = x11;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(95, 23),
    // (97,32): error CS0128: A local variable named 'x11' is already defined in this scope
    //              Dummy(true is var x11 && x11);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(97, 32),
    // (98,32): error CS0128: A local variable named 'x11' is already defined in this scope
    //              Dummy(true is var x11 && x11))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(98, 32),
    // (104,20): error CS0841: Cannot use local variable 'x12' before it is declared
    //         for (Dummy(x12);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(104, 20),
    // (105,20): error CS0841: Cannot use local variable 'x12' before it is declared
    //              Dummy(x12) &&
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(105, 20),
    // (107,32): error CS0128: A local variable named 'x12' is already defined in this scope
    //              Dummy(true is var x12 && x12))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x12").WithArguments("x12").WithLocation(107, 32),
    // (113,22): error CS0841: Cannot use local variable 'x13' before it is declared
    //         for (var b = x13;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(113, 22),
    // (114,20): error CS0841: Cannot use local variable 'x13' before it is declared
    //              Dummy(x13);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(114, 20),
    // (116,32): error CS0128: A local variable named 'x13' is already defined in this scope
    //              Dummy(true is var x13 && x13))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x13").WithArguments("x13").WithLocation(116, 32),
    // (122,23): error CS0841: Cannot use local variable 'x14' before it is declared
    //         for (bool b = x14;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(122, 23),
    // (123,20): error CS0841: Cannot use local variable 'x14' before it is declared
    //              Dummy(x14);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(123, 20),
    // (125,32): error CS0128: A local variable named 'x14' is already defined in this scope
    //              Dummy(true is var x14 && x14))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(125, 32),
    // (131,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //         for (Dummy(x15);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(131, 20),
    // (132,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //              Dummy(x15);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(132, 20),
    // (133,20): error CS0841: Cannot use local variable 'x15' before it is declared
    //              Dummy(x15),
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(133, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
            VerifyNotAPatternLocal(model, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);
            VerifyNotAPatternLocal(model, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl);
            VerifyNotAPatternLocal(model, x5Ref);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl);
            VerifyNotAPatternLocal(model, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x7Decl);
            VerifyNotAPatternLocal(model, x7Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(4, x8Decl.Length);
            Assert.Equal(4, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[3]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(3, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[2]);

            var x10Decl = GetPatternDeclarations(tree, "x10").ToArray();
            var x10Ref = GetReferences(tree, "x10").ToArray();
            Assert.Equal(3, x10Decl.Length);
            Assert.Equal(4, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl[0], x10Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[2]);

            var x11Decl = GetPatternDeclarations(tree, "x11").ToArray();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(3, x11Decl.Length);
            Assert.Equal(4, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl[0], x11Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[2]);

            var x12Decl = GetPatternDeclarations(tree, "x12").ToArray();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Decl.Length);
            Assert.Equal(4, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl[0], x12Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x12Decl[1]);

            var x13Decl = GetPatternDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(4, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x13Decl[1]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetPatternDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(4, x15Ref.Length);
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Foreach_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    System.Collections.IEnumerable Dummy(params object[] x) {return null;}

    void Test1()
    {
        foreach (var i in Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        foreach (var i in Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        foreach (var i in Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        foreach (var i in Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        foreach (var i in Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        foreach (var i in Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        foreach (var i1 in Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            foreach (var i2 in Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        foreach (var i in Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    foreach (var i in Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        foreach (var i in Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    foreach (var i in Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        foreach (var i in Dummy(1 is var x14, 
                                2 is var x14, 
                                x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        foreach (var x15 in 
                            Dummy(1 is var x15, x15))
        {
            Dummy(x15);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,45): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var i in Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 45),
    // (35,33): error CS0841: Cannot use local variable 'x6' before it is declared
    //         foreach (var i in Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 33),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,50): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             foreach (var i2 in Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 50),
    // (68,33): error CS0103: The name 'y10' does not exist in the current context
    //         foreach (var i in Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 33),
    // (86,33): error CS0103: The name 'y12' does not exist in the current context
    //         foreach (var i in Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 33),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,42): error CS0128: A local variable named 'x14' is already defined in this scope
    //                                 2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 42),
    // (108,22): error CS0136: A local or parameter named 'x15' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var x15 in 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x15").WithArguments("x15").WithLocation(108, 22)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetPatternDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref[0]);
            VerifyNotAPatternLocal(model, x15Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Lock_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        lock (Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        lock (Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        lock (Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        lock (Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        lock (Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        lock (Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        lock (Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            lock (Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        lock (Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    lock (Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        lock (Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    lock (Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        lock (Dummy(1 is var x14, 
                    2 is var x14, 
                    x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,33): error CS0128: A local variable named 'x4' is already defined in this scope
    //         lock (Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(29, 33),
    // (35,21): error CS0841: Cannot use local variable 'x6' before it is declared
    //         lock (Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 21),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (60,19): error CS0841: Cannot use local variable 'x9' before it is declared
    //             Dummy(x9);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(60, 19),
    // (61,37): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             lock (Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 37),
    // (68,21): error CS0103: The name 'y10' does not exist in the current context
    //         lock (Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 21),
    // (86,21): error CS0103: The name 'y12' does not exist in the current context
    //         lock (Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 21),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,30): error CS0128: A local variable named 'x14' is already defined in this scope
    //                     2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Lock_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) {return null;}

    void Test1()
    {
        if (true)
            lock (Dummy(true is var x1))
            {
            }

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
                // (17,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(17, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Lock_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (LockStatementSyntax)SyntaxFactory.ParseStatement(@"
lock (Dummy(11 is var x1, x1));
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_Fixed_01()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}

    void Test1()
    {
        fixed (int* p = Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(true is var x2 && x2))
            Dummy(x2);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        fixed (int* p = Dummy(true is var x4 && x4))
            Dummy(x4);
    }

    void Test6()
    {
        fixed (int* p = Dummy(x6 && true is var x6))
            Dummy(x6);
    }

    void Test7()
    {
        fixed (int* p = Dummy(true is var x7 && x7))
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        fixed (int* p = Dummy(true is var x8 && x8))
            Dummy(x8);

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        fixed (int* p1 = Dummy(true is var x9 && x9))
        {   
            Dummy(x9);
            fixed (int* p2 = Dummy(true is var x9 && x9)) // 2
                Dummy(x9);
        }
    }

    void Test10()
    {
        fixed (int* p = Dummy(y10 is var x10))
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    fixed (int* p = Dummy(y11 is var x11))
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test12()
    {
        fixed (int* p = Dummy(y12 is var x12))
            var y12 = 12;
    }

    //void Test13()
    //{
    //    fixed (int* p = Dummy(y13 is var x13))
    //        let y13 = 12;
    //}

    void Test14()
    {
        fixed (int* p = Dummy(1 is var x14, 
                              2 is var x14, 
                              x14))
        {
            Dummy(x14);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
    // (87,13): error CS1023: Embedded statement cannot be a declaration or labeled statement
    //             var y12 = 12;
    Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "var y12 = 12;").WithLocation(87, 13),
    // (29,43): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         fixed (int* p = Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(29, 43),
    // (35,31): error CS0841: Cannot use local variable 'x6' before it is declared
    //         fixed (int* p = Dummy(x6 && true is var x6))
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(35, 31),
    // (43,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(43, 17),
    // (53,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(53, 34),
    // (61,48): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             fixed (int* p2 = Dummy(true is var x9 && x9)) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(61, 48),
    // (68,31): error CS0103: The name 'y10' does not exist in the current context
    //         fixed (int* p = Dummy(y10 is var x10))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(68, 31),
    // (86,31): error CS0103: The name 'y12' does not exist in the current context
    //         fixed (int* p = Dummy(y12 is var x12))
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y12").WithArguments("y12").WithLocation(86, 31),
    // (87,17): warning CS0219: The variable 'y12' is assigned but its value is never used
    //             var y12 = 12;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y12").WithArguments("y12").WithLocation(87, 17),
    // (99,40): error CS0128: A local variable named 'x14' is already defined in this scope
    //                               2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 40)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var y12Ref = GetReferences(tree, "y12").Single();
            VerifyNotInScope(model, y12Ref);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Fixed_02()
        {
            var source =
@"
public unsafe class X
{
    public static void Main()
    {
    }

    int[] Dummy(params object[] x) {return null;}
    int[] Dummy(int* x) {return null;}

    void Test1()
    {
        fixed (int* x1 = 
                         Dummy(true is var x1 && x1))
        {
            Dummy(x1);
        }
    }

    void Test2()
    {
        fixed (int* p = Dummy(true is var x2 && x2),
                    x2 = Dummy())
        {
            Dummy(x2);
        }
    }

    void Test3()
    {
        fixed (int* x3 = Dummy(),
                    p = Dummy(true is var x3 && x3))
        {
            Dummy(x3);
        }
    }

    void Test4()
    {
        fixed (int* p1 = Dummy(true is var x4 && x4),
                    p2 = Dummy(true is var x4 && x4))
        {
            Dummy(x4);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe.WithAllowUnsafe(true));
            compilation.VerifyDiagnostics(
    // (14,44): error CS0128: A local variable named 'x1' is already defined in this scope
    //                          Dummy(true is var x1 && x1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(14, 44),
    // (14,50): error CS0165: Use of unassigned local variable 'x1'
    //                          Dummy(true is var x1 && x1))
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(14, 50),
    // (23,21): error CS0128: A local variable named 'x2' is already defined in this scope
    //                     x2 = Dummy())
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(23, 21),
    // (32,43): error CS0128: A local variable named 'x3' is already defined in this scope
    //                     p = Dummy(true is var x3 && x3))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(32, 43),
    // (41,44): error CS0128: A local variable named 'x4' is already defined in this scope
    //                     p2 = Dummy(true is var x4 && x4))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(41, 44)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref[0]);
            VerifyNotAPatternLocal(model, x1Ref[1]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref[0]);
            VerifyNotAPatternLocal(model, x3Ref[1]);

            var x4Decl = GetPatternDeclarations(tree, "x4").ToArray();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Decl.Length);
            Assert.Equal(3, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl[0], x4Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_Yield_01()
        {
            var source =
@"
using System.Collections;

public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    IEnumerable Test1()
    {
        yield return Dummy(true is var x1, x1);
        {
            yield return Dummy(true is var x1, x1);
        }
        yield return Dummy(true is var x1, x1);
    }

    IEnumerable Test2()
    {
        yield return Dummy(x2, true is var x2);
    }

    IEnumerable Test3(int x3)
    {
        yield return Dummy(true is var x3, x3);
    }

    IEnumerable Test4()
    {
        var x4 = 11;
        Dummy(x4);
        yield return Dummy(true is var x4, x4);
    }

    IEnumerable Test5()
    {
        yield return Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //IEnumerable Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //    yield return Dummy(true is var x6, x6);
    //}

    //IEnumerable Test7()
    //{
    //    yield return Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    IEnumerable Test8()
    {
        yield return Dummy(true is var x8, x8, false is var x8, x8);
    }

    IEnumerable Test9(bool y9)
    {
        if (y9)
            yield return Dummy(true is var x9, x9);
    }

    IEnumerable Test11()
    {
        Dummy(x11);
        yield return Dummy(true is var x11, x11);
    }

    IEnumerable Test12()
    {
        yield return Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
    // (16,44): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             yield return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(16, 44),
    // (18,40): error CS0128: A local variable named 'x1' is already defined in this scope
    //         yield return Dummy(true is var x1, x1);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(18, 40),
    // (23,28): error CS0841: Cannot use local variable 'x2' before it is declared
    //         yield return Dummy(x2, true is var x2);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(23, 28),
    // (28,40): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         yield return Dummy(true is var x3, x3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(28, 40),
    // (35,40): error CS0128: A local variable named 'x4' is already defined in this scope
    //         yield return Dummy(true is var x4, x4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(35, 40),
    // (41,13): error CS0128: A local variable named 'x5' is already defined in this scope
    //         var x5 = 11;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(41, 13),
    // (41,13): warning CS0219: The variable 'x5' is assigned but its value is never used
    //         var x5 = 11;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(41, 13),
    // (61,61): error CS0128: A local variable named 'x8' is already defined in this scope
    //         yield return Dummy(true is var x8, x8, false is var x8, x8);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(61, 61),
    // (72,15): error CS0841: Cannot use local variable 'x11' before it is declared
    //         Dummy(x11);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(72, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl[2]);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Yield_02()
        {
            var source =
@"
using System.Collections;

public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    IEnumerable Test1()
    {
        if (true)
            yield return Dummy(true is var x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (17,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(17, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_Yield_03()
        {
            var source =
@"
using System.Collections;

public class X
{
    public static void Main()
    {
    }

    object Dummy(params object[] x) { return null;}

    IEnumerable Test1()
    {
        SpeculateHere();
        yield 0;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (YieldStatementSyntax)SyntaxFactory.ParseStatement(@"
yield return (Dummy(11 is var x1, x1));
");

            bool success = model.TryGetSpeculativeSemanticModel(
                GetReferences(tree, "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void ScopeOfPatternVariables_Catch_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        try {}
        catch when (true is var x1 && x1)
        {
            Dummy(x1);
        }
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);

        try {}
        catch when (true is var x4 && x4)
        {
            Dummy(x4);
        }
    }

    void Test6()
    {
        try {}
        catch when (x6 && true is var x6)
        {
            Dummy(x6);
        }
    }

    void Test7()
    {
        try {}
        catch when (true is var x7 && x7)
        {
            var x7 = 12;
            Dummy(x7);
        }
    }

    void Test8()
    {
        try {}
        catch when (true is var x8 && x8)
        {
            Dummy(x8);
        }

        System.Console.WriteLine(x8);
    }

    void Test9()
    {
        try {}
        catch when (true is var x9 && x9)
        {   
            Dummy(x9);
            try {}
            catch when (true is var x9 && x9) // 2
            {
                Dummy(x9);
            }
        }
    }

    void Test10()
    {
        try {}
        catch when (y10 is var x10)
        {   
            var y10 = 12;
            Dummy(y10);
        }
    }

    //void Test11()
    //{
    //    try {}
    //    catch when (y11 is var x11)
    //    {   
    //        let y11 = 12;
    //        Dummy(y11);
    //    }
    //}

    void Test14()
    {
        try {}
        catch when (Dummy(1 is var x14, 
                          2 is var x14, 
                          x14))
        {
            Dummy(x14);
        }
    }

    void Test15()
    {
        try {}
        catch (System.Exception x15)
              when (Dummy(1 is var x15, x15))
        {
            Dummy(x15);
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
            compilation.VerifyDiagnostics(
    // (25,33): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         catch when (true is var x4 && x4)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(25, 33),
    // (34,21): error CS0841: Cannot use local variable 'x6' before it is declared
    //         catch when (x6 && true is var x6)
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x6").WithArguments("x6").WithLocation(34, 21),
    // (45,17): error CS0136: A local or parameter named 'x7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             var x7 = 12;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x7").WithArguments("x7").WithLocation(45, 17),
    // (58,34): error CS0103: The name 'x8' does not exist in the current context
    //         System.Console.WriteLine(x8);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "x8").WithArguments("x8").WithLocation(58, 34),
    // (68,37): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             catch when (true is var x9 && x9) // 2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(68, 37),
    // (78,21): error CS0103: The name 'y10' does not exist in the current context
    //         catch when (y10 is var x10)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "y10").WithArguments("y10").WithLocation(78, 21),
    // (99,36): error CS0128: A local variable named 'x14' is already defined in this scope
    //                           2 is var x14, 
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(99, 36),
    // (110,36): error CS0128: A local variable named 'x15' is already defined in this scope
    //               when (Dummy(1 is var x15, x15))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(110, 36)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(2, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(3, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[1], x4Ref[2]);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").ToArray();
            Assert.Equal(2, x6Ref.Length);
            VerifyModelForDeclarationPattern(model, x6Decl, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").ToArray();
            Assert.Equal(2, x7Ref.Length);
            VerifyModelForDeclarationPattern(model, x7Decl, x7Ref[0]);
            VerifyNotAPatternLocal(model, x7Ref[1]);

            var x8Decl = GetPatternDeclarations(tree, "x8").Single();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(3, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl, x8Ref[0], x8Ref[1]);
            VerifyNotInScope(model, x8Ref[2]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(2, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref[0], x9Ref[1]);
            VerifyModelForDeclarationPattern(model, x9Decl[1], x9Ref[2], x9Ref[3]);

            var y10Ref = GetReferences(tree, "y10").ToArray();
            Assert.Equal(2, y10Ref.Length);
            VerifyNotInScope(model, y10Ref[0]);
            VerifyNotAPatternLocal(model, y10Ref[1]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(2, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetPatternDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Ref.Length);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl);
            VerifyNotAPatternLocal(model, x15Ref[0]);
            VerifyNotAPatternLocal(model, x15Ref[1]);
        }

        [Fact]
        public void ScopeOfPatternVariables_LabeledStatement_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
a:      Dummy(true is var x1, x1);
        {
b:          Dummy(true is var x1, x1);
        }
c:      Dummy(true is var x1, x1);
    }

    void Test2()
    {
        Dummy(x2, true is var x2);
    }

    void Test3(int x3)
    {
a:      Dummy(true is var x3, x3);
    }

    void Test4()
    {
        var x4 = 11;
        Dummy(x4);
a:      Dummy(true is var x4, x4);
    }

    void Test5()
    {
a:      Dummy(true is var x5, x5);
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6()
    //{
    //    let x6 = 11;
    //    Dummy(x6);
    //a:  Dummy(true is var x6, x6);
    //}

    //void Test7()
    //{
    //a:  Dummy(true is var x7, x7);
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8()
    {
a:      Dummy(true is var x8, x8, false is var x8, x8);
    }

    void Test9(bool y9)
    {
        if (y9)
a:          Dummy(true is var x9, x9);
    }

    System.Action Test10(bool y10)
    {
        return () =>
                {
                    if (y10)
a:                      Dummy(true is var x10, x10);
                };
    }

    void Test11()
    {
        Dummy(x11);
a:      Dummy(true is var x11, x11);
    }

    void Test12()
    {
a:      Dummy(true is var x12, x12);
        Dummy(x12);
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (65,1): error CS1023: Embedded statement cannot be a declaration or labeled statement
                // a:          Dummy(true is var x9, x9);
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "a:          Dummy(true is var x9, x9);").WithLocation(65, 1),
                // (73,1): error CS1023: Embedded statement cannot be a declaration or labeled statement
                // a:                      Dummy(true is var x10, x10);
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "a:                      Dummy(true is var x10, x10);").WithLocation(73, 1),
                // (14,31): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // b:          Dummy(true is var x1, x1);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1").WithLocation(14, 31),
                // (16,27): error CS0128: A local variable named 'x1' is already defined in this scope
                // c:      Dummy(true is var x1, x1);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(16, 27),
                // (12,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x1, x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(12, 1),
                // (14,1): warning CS0164: This label has not been referenced
                // b:          Dummy(true is var x1, x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "b").WithLocation(14, 1),
                // (16,1): warning CS0164: This label has not been referenced
                // c:      Dummy(true is var x1, x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "c").WithLocation(16, 1),
                // (21,15): error CS0841: Cannot use local variable 'x2' before it is declared
                //         Dummy(x2, true is var x2);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(21, 15),
                // (26,27): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // a:      Dummy(true is var x3, x3);
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(26, 27),
                // (26,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x3, x3);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(26, 1),
                // (33,27): error CS0128: A local variable named 'x4' is already defined in this scope
                // a:      Dummy(true is var x4, x4);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(33, 27),
                // (33,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x4, x4);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(33, 1),
                // (39,13): error CS0128: A local variable named 'x5' is already defined in this scope
                //         var x5 = 11;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(39, 13),
                // (38,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x5, x5);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(38, 1),
                // (39,13): warning CS0219: The variable 'x5' is assigned but its value is never used
                //         var x5 = 11;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x5").WithArguments("x5").WithLocation(39, 13),
                // (59,48): error CS0128: A local variable named 'x8' is already defined in this scope
                // a:      Dummy(true is var x8, x8, false is var x8, x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(59, 48),
                // (59,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x8, x8, false is var x8, x8);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(59, 1),
                // (65,1): warning CS0164: This label has not been referenced
                // a:          Dummy(true is var x9, x9);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(65, 1),
                // (73,1): warning CS0164: This label has not been referenced
                // a:                      Dummy(true is var x10, x10);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(73, 1),
                // (79,15): error CS0841: Cannot use local variable 'x11' before it is declared
                //         Dummy(x11);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(79, 15),
                // (80,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x11, x11);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(80, 1),
                // (85,1): warning CS0164: This label has not been referenced
                // a:      Dummy(true is var x12, x12);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(85, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").ToArray();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(3, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl[0], x1Ref[0], x1Ref[2]);
            VerifyModelForDeclarationPattern(model, x1Decl[1], x1Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl[2]);

            var x2Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x2").Single();
            var x2Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x2").Single();
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x3").Single();
            var x3Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x3").Single();
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x4").Single();
            var x4Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x4").ToArray();
            Assert.Equal(2, x4Ref.Length);
            VerifyNotAPatternLocal(model, x4Ref[0]);
            VerifyNotAPatternLocal(model, x4Ref[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);

            var x5Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x5").Single();
            var x5Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x5").ToArray();
            Assert.Equal(2, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref);

            var x8Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x8").ToArray();
            var x8Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(2, x8Ref.Length);
            for (int i = 0; i < x8Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x9").Single();
            var x9Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x9").Single();
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref);

            var x10Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x10").Single();
            var x10Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x10").Single();
            VerifyModelForDeclarationPattern(model, x10Decl, x10Ref);

            var x11Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x11").Single();
            var x11Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x11").ToArray();
            Assert.Equal(2, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref);

            var x12Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x12").Single();
            var x12Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x12").ToArray();
            Assert.Equal(2, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_LabeledStatement_02()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        if (true)
a:          Dummy(true is var x1);

        x1++;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            compilation.VerifyDiagnostics(
                // (13,1): error CS1023: Embedded statement cannot be a declaration or labeled statement
                // a:          Dummy(true is var x1);
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "a:          Dummy(true is var x1);").WithLocation(13, 1),
                // (15,9): error CS0103: The name 'x1' does not exist in the current context
                //         x1++;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x1").WithArguments("x1").WithLocation(15, 9),
                // (13,1): warning CS0164: This label has not been referenced
                // a:          Dummy(true is var x1);
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "a").WithLocation(13, 1)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").Single();
            VerifyModelForDeclarationPattern(model, x1Decl);
            VerifyNotInScope(model, x1Ref);
        }

        [Fact]
        public void ScopeOfPatternVariables_LabeledStatement_03()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    void Dummy(params object[] x) {}

    void Test1()
    {
        SpeculateHere();
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var statement = (LabeledStatementSyntax)SyntaxFactory.ParseStatement(@"
a: b: c:Dummy(11 is var x1, x1);
");

            bool success = model.TryGetSpeculativeSemanticModel(
                tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "SpeculateHere").Single().SpanStart,
                statement, out model);
            Assert.True(success);
            Assert.NotNull(model);
            tree = statement.SyntaxTree;

            var x1Decl = tree.GetRoot().DescendantNodes().OfType<DeclarationPatternSyntax>().Where(p => p.Identifier.ValueText == "x1").Single();
            var x1Ref = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(id => id.Identifier.ValueText == "x1").ToArray();
            Assert.Equal(1, x1Ref.Length);
            VerifyModelForDeclarationPattern(model, x1Decl, x1Ref);
            Assert.Equal("System.Int32", model.GetTypeInfo(x1Ref[0]).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_For_06()
        {
            var source =
@"
public class X
{
    static bool Data = true;
    public static void Main()
    {
    }

    bool Dummy(params object[] x) {return true;}

    void Test1()
    {
        for (var x1 =
             Dummy(Dummy(true, Data is var x1) && x1)
             ;;)
        {}
    }

    void Test2()
    {
        for (var x2 = true;
             Dummy(Dummy(true, Data is var x2) && x2)
             ;)
        {}
    }

    void Test3()
    {
        for (var x3 = true;;
             Dummy(Dummy(true, Data is var x3) && x3)
             )
        {}
    }

    void Test4()
    {
        for (bool x4 =
             Dummy(Dummy(true, Data is var x4) && x4)
             ;;)
        {}
    }

    void Test5()
    {
        for (bool x5 = true;
             Dummy(Dummy(true, Data is var x5) && x5)
             ;)
        {}
    }

    void Test6()
    {
        for (bool x6 = true;;
             Dummy(Dummy(true, Data is var x6) && x6)
             )
        {}
    }

    void Test7()
    {
        for (bool x7 = true, b =
             Dummy(Dummy(true, Data is var x7) && x7)
             ;;)
        {}
    }

    void Test8()
    {
        for (bool b1 = Dummy(Dummy(true, Data is var x8) && x8), 
             b2 = Dummy(Dummy(true, Data is var x8) && x8);
             Dummy(Dummy(true, Data is var x8) && x8);
             Dummy(Dummy(true, Data is var x8) && x8))
        {}
    }

    void Test9()
    {
        for (bool b = x9, 
             b2 = Dummy(Dummy(true, Data is var x9) && x9);
             Dummy(Dummy(true, Data is var x9) && x9);
             Dummy(Dummy(true, Data is var x9) && x9))
        {}
    }

    void Test10()
    {
        for (var b = x10;
             Dummy(Dummy(true, Data is var x10) && x10) &&
             Dummy(Dummy(true, Data is var x10) && x10);
             Dummy(Dummy(true, Data is var x10) && x10))
        {}
    }

    void Test11()
    {
        for (bool b = x11;
             Dummy(Dummy(true, Data is var x11) && x11) &&
             Dummy(Dummy(true, Data is var x11) && x11);
             Dummy(Dummy(true, Data is var x11) && x11))
        {}
    }

    void Test12()
    {
        for (Dummy(x12);
             Dummy(x12) &&
             Dummy(Dummy(true, Data is var x12) && x12);
             Dummy(Dummy(true, Data is var x12) && x12))
        {}
    }

    void Test13()
    {
        for (var b = x13;
             Dummy(x13);
             Dummy(Dummy(true, Data is var x13) && x13),
             Dummy(Dummy(true, Data is var x13) && x13))
        {}
    }

    void Test14()
    {
        for (bool b = x14;
             Dummy(x14);
             Dummy(Dummy(true, Data is var x14) && x14),
             Dummy(Dummy(true, Data is var x14) && x14))
        {}
    }

    void Test15()
    {
        for (Dummy(x15);
             Dummy(x15);
             Dummy(x15),
             Dummy(Dummy(true, Data is var x15) && x15))
        {}
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (14,44): error CS0128: A local variable named 'x1' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x1) && x1)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x1").WithArguments("x1").WithLocation(14, 44),
                // (14,51): error CS0841: Cannot use local variable 'x1' before it is declared
                //              Dummy(Dummy(true, Data is var x1) && x1)
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x1").WithArguments("x1").WithLocation(14, 51),
                // (14,51): error CS0165: Use of unassigned local variable 'x1'
                //              Dummy(Dummy(true, Data is var x1) && x1)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x1").WithArguments("x1").WithLocation(14, 51),
                // (22,44): error CS0128: A local variable named 'x2' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x2) && x2)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x2").WithArguments("x2").WithLocation(22, 44),
                // (30,44): error CS0128: A local variable named 'x3' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x3) && x3)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x3").WithArguments("x3").WithLocation(30, 44),
                // (38,44): error CS0128: A local variable named 'x4' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x4) && x4)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x4").WithArguments("x4").WithLocation(38, 44),
                // (38,51): error CS0165: Use of unassigned local variable 'x4'
                //              Dummy(Dummy(true, Data is var x4) && x4)
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x4").WithArguments("x4").WithLocation(38, 51),
                // (46,44): error CS0128: A local variable named 'x5' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x5) && x5)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x5").WithArguments("x5").WithLocation(46, 44),
                // (54,44): error CS0128: A local variable named 'x6' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x6) && x6)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x6").WithArguments("x6").WithLocation(54, 44),
                // (62,44): error CS0128: A local variable named 'x7' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x7) && x7)
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x7").WithArguments("x7").WithLocation(62, 44),
                // (70,49): error CS0128: A local variable named 'x8' is already defined in this scope
                //              b2 = Dummy(Dummy(true, Data is var x8) && x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(70, 49),
                // (71,44): error CS0128: A local variable named 'x8' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x8) && x8);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(71, 44),
                // (72,44): error CS0128: A local variable named 'x8' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x8) && x8))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(72, 44),
                // (78,23): error CS0841: Cannot use local variable 'x9' before it is declared
                //         for (bool b = x9, 
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(78, 23),
                // (80,44): error CS0128: A local variable named 'x9' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x9) && x9);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(80, 44),
                // (81,44): error CS0128: A local variable named 'x9' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x9) && x9))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x9").WithArguments("x9").WithLocation(81, 44),
                // (87,22): error CS0841: Cannot use local variable 'x10' before it is declared
                //         for (var b = x10;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x10").WithArguments("x10").WithLocation(87, 22),
                // (89,44): error CS0128: A local variable named 'x10' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x10) && x10);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(89, 44),
                // (90,44): error CS0128: A local variable named 'x10' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x10) && x10))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x10").WithArguments("x10").WithLocation(90, 44),
                // (96,23): error CS0841: Cannot use local variable 'x11' before it is declared
                //         for (bool b = x11;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x11").WithArguments("x11").WithLocation(96, 23),
                // (98,44): error CS0128: A local variable named 'x11' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x11) && x11);
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(98, 44),
                // (99,44): error CS0128: A local variable named 'x11' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x11) && x11))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x11").WithArguments("x11").WithLocation(99, 44),
                // (105,20): error CS0841: Cannot use local variable 'x12' before it is declared
                //         for (Dummy(x12);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(105, 20),
                // (106,20): error CS0841: Cannot use local variable 'x12' before it is declared
                //              Dummy(x12) &&
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x12").WithArguments("x12").WithLocation(106, 20),
                // (108,44): error CS0128: A local variable named 'x12' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x12) && x12))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x12").WithArguments("x12").WithLocation(108, 44),
                // (114,22): error CS0841: Cannot use local variable 'x13' before it is declared
                //         for (var b = x13;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(114, 22),
                // (115,20): error CS0841: Cannot use local variable 'x13' before it is declared
                //              Dummy(x13);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x13").WithArguments("x13").WithLocation(115, 20),
                // (117,44): error CS0128: A local variable named 'x13' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x13) && x13))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x13").WithArguments("x13").WithLocation(117, 44),
                // (123,23): error CS0841: Cannot use local variable 'x14' before it is declared
                //         for (bool b = x14;
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(123, 23),
                // (124,20): error CS0841: Cannot use local variable 'x14' before it is declared
                //              Dummy(x14);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x14").WithArguments("x14").WithLocation(124, 20),
                // (126,44): error CS0128: A local variable named 'x14' is already defined in this scope
                //              Dummy(Dummy(true, Data is var x14) && x14))
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x14").WithArguments("x14").WithLocation(126, 44),
                // (132,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //         for (Dummy(x15);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(132, 20),
                // (133,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //              Dummy(x15);
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(133, 20),
                // (134,20): error CS0841: Cannot use local variable 'x15' before it is declared
                //              Dummy(x15),
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x15").WithArguments("x15").WithLocation(134, 20)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").Single();
            var x1Ref = GetReferences(tree, "x1").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x1Decl);
            VerifyNotAPatternLocal(model, x1Ref);

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x2Decl);
            VerifyNotAPatternLocal(model, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x3Decl);
            VerifyNotAPatternLocal(model, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x4Decl);
            VerifyNotAPatternLocal(model, x4Ref);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x5Decl);
            VerifyNotAPatternLocal(model, x5Ref);

            var x6Decl = GetPatternDeclarations(tree, "x6").Single();
            var x6Ref = GetReferences(tree, "x6").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x6Decl);
            VerifyNotAPatternLocal(model, x6Ref);

            var x7Decl = GetPatternDeclarations(tree, "x7").Single();
            var x7Ref = GetReferences(tree, "x7").Single();
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x7Decl);
            VerifyNotAPatternLocal(model, x7Ref);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(4, x8Decl.Length);
            Assert.Equal(4, x8Ref.Length);
            VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[2]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[3]);

            var x9Decl = GetPatternDeclarations(tree, "x9").ToArray();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(3, x9Decl.Length);
            Assert.Equal(4, x9Ref.Length);
            VerifyModelForDeclarationPattern(model, x9Decl[0], x9Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x9Decl[2]);

            var x10Decl = GetPatternDeclarations(tree, "x10").ToArray();
            var x10Ref = GetReferences(tree, "x10").ToArray();
            Assert.Equal(3, x10Decl.Length);
            Assert.Equal(4, x10Ref.Length);
            VerifyModelForDeclarationPattern(model, x10Decl[0], x10Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x10Decl[2]);

            var x11Decl = GetPatternDeclarations(tree, "x11").ToArray();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(3, x11Decl.Length);
            Assert.Equal(4, x11Ref.Length);
            VerifyModelForDeclarationPattern(model, x11Decl[0], x11Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[1]);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x11Decl[2]);

            var x12Decl = GetPatternDeclarations(tree, "x12").ToArray();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(2, x12Decl.Length);
            Assert.Equal(4, x12Ref.Length);
            VerifyModelForDeclarationPattern(model, x12Decl[0], x12Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x12Decl[1]);

            var x13Decl = GetPatternDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(4, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x13Decl[1]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x14Decl[1]);

            var x15Decl = GetPatternDeclarations(tree, "x15").Single();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(4, x15Ref.Length);
            VerifyModelForDeclarationPattern(model, x15Decl, x15Ref);
        }

        [Fact]
        public void Scope_SwitchLabelGuard_01()
        {
            var source =
@"
public class X
{
    public static void Main()
    {
    }

    bool Dummy(params object[] x) { return true; }

    public static int Data = 2;

    void Test1(int val)
    {
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x1), x1):
                Dummy(x1);
                break;
            case 1 when Dummy(Dummy(Data is var x1), x1):
                Dummy(x1);
                break;
            case 2 when Dummy(Dummy(Data is var x1), x1):
                Dummy(x1);
                break;
        }
    }

    void Test2(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x2, Dummy(Data is var x2)):
                Dummy(x2);
                break;
        }
    }

    void Test3(int x3, int val)
    {
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x3), x3):
                Dummy(x3);
                break;
        }
    }

    void Test4(int val)
    {
        var x4 = 11;
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x4), x4):
                Dummy(x4);
                break;
            case 1 when Dummy(x4): Dummy(x4); break;
        }
    }

    void Test5(int val)
    {
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x5), x5):
                Dummy(x5);
                break;
        }
        
        var x5 = 11;
        Dummy(x5);
    }

    //void Test6(int val)
    //{
    //    let x6 = 11;
    //    switch (val)
    //    {
    //        case 0 when Dummy(x6):
    //            Dummy(x6);
    //            break;
    //        case 1 when Dummy(Dummy(Data is var x6), x6):
    //            Dummy(x6);
    //            break;
    //    }
    //}

    //void Test7(int val)
    //{
    //    switch (val)
    //    {
    //        case 0 when Dummy(Dummy(Data is var x7), x7):
    //            Dummy(x7);
    //            break;
    //    }
        
    //    let x7 = 11;
    //    Dummy(x7);
    //}

    void Test8(int val)
    {
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x8), x8, Dummy(Data is var x8), x8):
                Dummy(x8);
                break;
        }
    }

    void Test9(int val)
    {
        switch (val)
        {
            case 0 when Dummy(x9):
                int x9 = 9;
                Dummy(x9);
                break;
            case 2 when Dummy(x9 = 9):
                Dummy(x9);
                break;
            case 1 when Dummy(Dummy(Data is var x9), x9):
                Dummy(x9);
                break;
        }
    }

    //void Test10(int val)
    //{
    //    switch (val)
    //    {
    //        case 1 when Dummy(Dummy(Data is var x10), x10):
    //            Dummy(x10);
    //            break;
    //        case 0 when Dummy(x10):
    //            let x10 = 10;
    //            Dummy(x10);
    //            break;
    //        case 2 when Dummy(x10 = 10, x10):
    //            Dummy(x10);
    //            break;
    //    }
    //}

    void Test11(int val)
    {
        switch (x11 ? val : 0)
        {
            case 0 when Dummy(x11):
                Dummy(x11, 0);
                break;
            case 1 when Dummy(Dummy(Data is var x11), x11):
                Dummy(x11, 1);
                break;
        }
    }

    void Test12(int val)
    {
        switch (x12 ? val : 0)
        {
            case 0 when Dummy(Dummy(Data is var x12), x12):
                Dummy(x12, 0);
                break;
            case 1 when Dummy(x12):
                Dummy(x12, 1);
                break;
        }
    }

    void Test13()
    {
        switch (Dummy(1, Data is var x13) ? x13 : 0)
        {
            case 0 when Dummy(x13):
                Dummy(x13);
                break;
            case 1 when Dummy(Dummy(Data is var x13), x13):
                Dummy(x13);
                break;
        }
    }

    void Test14(int val)
    {
        switch (val)
        {
            case 1 when Dummy(Dummy(Data is var x14), x14):
                Dummy(x14);
                Dummy(Dummy(Data is var x14), x14);
                Dummy(x14);
                break;
        }
    }

    void Test15(int val)
    {
        switch (val)
        {
            case 0 when Dummy(Dummy(Data is var x15), x15):
            case 1 when Dummy(Dummy(Data is var x15), x15):
                Dummy(x15);
                break;
        }
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            compilation.VerifyDiagnostics(
                // (32,31): error CS0841: Cannot use local variable 'x2' before it is declared
                //             case 0 when Dummy(x2, Dummy(Data is var x2)):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x2").WithArguments("x2").WithLocation(32, 31),
                // (42,49): error CS0136: A local or parameter named 'x3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(Dummy(Data is var x3), x3):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x3").WithArguments("x3").WithLocation(42, 49),
                // (53,49): error CS0136: A local or parameter named 'x4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(Dummy(Data is var x4), x4):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x4").WithArguments("x4").WithLocation(53, 49),
                // (64,49): error CS0136: A local or parameter named 'x5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 0 when Dummy(Dummy(Data is var x5), x5):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x5").WithArguments("x5").WithLocation(64, 49),
                // (104,76): error CS0128: A local variable named 'x8' is already defined in this scope
                //             case 0 when Dummy(Dummy(Data is var x8), x8, Dummy(Data is var x8), x8):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x8").WithArguments("x8").WithLocation(104, 76),
                // (114,31): error CS0841: Cannot use local variable 'x9' before it is declared
                //             case 0 when Dummy(x9):
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x9").WithArguments("x9").WithLocation(114, 31),
                // (121,49): error CS0136: A local or parameter named 'x9' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 1 when Dummy(Dummy(Data is var x9), x9):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x9").WithArguments("x9").WithLocation(121, 49),
                // (146,17): error CS0103: The name 'x11' does not exist in the current context
                //         switch (x11 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(146, 17),
                // (148,31): error CS0103: The name 'x11' does not exist in the current context
                //             case 0 when Dummy(x11):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(148, 31),
                // (149,23): error CS0103: The name 'x11' does not exist in the current context
                //                 Dummy(x11, 0);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x11").WithArguments("x11").WithLocation(149, 23),
                // (159,17): error CS0103: The name 'x12' does not exist in the current context
                //         switch (x12 ? val : 0)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(159, 17),
                // (164,31): error CS0103: The name 'x12' does not exist in the current context
                //             case 1 when Dummy(x12):
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(164, 31),
                // (165,23): error CS0103: The name 'x12' does not exist in the current context
                //                 Dummy(x12, 1);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x12").WithArguments("x12").WithLocation(165, 23),
                // (177,49): error CS0136: A local or parameter named 'x13' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 1 when Dummy(Dummy(Data is var x13), x13):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x13").WithArguments("x13").WithLocation(177, 49),
                // (187,49): error CS0136: A local or parameter named 'x14' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             case 1 when Dummy(Dummy(Data is var x14), x14):
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x14").WithArguments("x14").WithLocation(187, 49),
                // (200,49): error CS0128: A local variable named 'x15' is already defined in this scope
                //             case 1 when Dummy(Dummy(Data is var x15), x15):
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "x15").WithArguments("x15").WithLocation(200, 49),
                // (200,55): error CS0165: Use of unassigned local variable 'x15'
                //             case 1 when Dummy(Dummy(Data is var x15), x15):
                Diagnostic(ErrorCode.ERR_UseDefViolation, "x15").WithArguments("x15").WithLocation(200, 55)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var x1Decl = GetPatternDeclarations(tree, "x1").ToArray();
            var x1Ref = GetReferences(tree, "x1").ToArray();
            Assert.Equal(3, x1Decl.Length);
            Assert.Equal(6, x1Ref.Length);
            for (int i = 0; i < x1Decl.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x1Decl[i], x1Ref[i * 2], x1Ref[i * 2 + 1]);
            }

            var x2Decl = GetPatternDeclarations(tree, "x2").Single();
            var x2Ref = GetReferences(tree, "x2").ToArray();
            Assert.Equal(2, x2Ref.Length);
            VerifyModelForDeclarationPattern(model, x2Decl, x2Ref);

            var x3Decl = GetPatternDeclarations(tree, "x3").Single();
            var x3Ref = GetReferences(tree, "x3").ToArray();
            Assert.Equal(2, x3Ref.Length);
            VerifyModelForDeclarationPattern(model, x3Decl, x3Ref);

            var x4Decl = GetPatternDeclarations(tree, "x4").Single();
            var x4Ref = GetReferences(tree, "x4").ToArray();
            Assert.Equal(4, x4Ref.Length);
            VerifyModelForDeclarationPattern(model, x4Decl, x4Ref[0], x4Ref[1]);
            VerifyNotAPatternLocal(model, x4Ref[2]);
            VerifyNotAPatternLocal(model, x4Ref[3]);

            var x5Decl = GetPatternDeclarations(tree, "x5").Single();
            var x5Ref = GetReferences(tree, "x5").ToArray();
            Assert.Equal(3, x5Ref.Length);
            VerifyModelForDeclarationPattern(model, x5Decl, x5Ref[0], x5Ref[1]);
            VerifyNotAPatternLocal(model, x5Ref[2]);

            var x8Decl = GetPatternDeclarations(tree, "x8").ToArray();
            var x8Ref = GetReferences(tree, "x8").ToArray();
            Assert.Equal(2, x8Decl.Length);
            Assert.Equal(3, x8Ref.Length);
            for (int i = 0; i < x8Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x8Decl[0], x8Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x8Decl[1]);

            var x9Decl = GetPatternDeclarations(tree, "x9").Single();
            var x9Ref = GetReferences(tree, "x9").ToArray();
            Assert.Equal(6, x9Ref.Length);
            VerifyNotAPatternLocal(model, x9Ref[0]);
            VerifyNotAPatternLocal(model, x9Ref[1]);
            VerifyNotAPatternLocal(model, x9Ref[2]);
            VerifyNotAPatternLocal(model, x9Ref[3]);
            VerifyModelForDeclarationPattern(model, x9Decl, x9Ref[4], x9Ref[5]);

            var x11Decl = GetPatternDeclarations(tree, "x11").Single();
            var x11Ref = GetReferences(tree, "x11").ToArray();
            Assert.Equal(5, x11Ref.Length);
            VerifyNotInScope(model, x11Ref[0]);
            VerifyNotInScope(model, x11Ref[1]);
            VerifyNotInScope(model, x11Ref[2]);
            VerifyModelForDeclarationPattern(model, x11Decl, x11Ref[3], x11Ref[4]);

            var x12Decl = GetPatternDeclarations(tree, "x12").Single();
            var x12Ref = GetReferences(tree, "x12").ToArray();
            Assert.Equal(5, x12Ref.Length);
            VerifyNotInScope(model, x12Ref[0]);
            VerifyModelForDeclarationPattern(model, x12Decl, x12Ref[1], x12Ref[2]);
            VerifyNotInScope(model, x12Ref[3]);
            VerifyNotInScope(model, x12Ref[4]);

            var x13Decl = GetPatternDeclarations(tree, "x13").ToArray();
            var x13Ref = GetReferences(tree, "x13").ToArray();
            Assert.Equal(2, x13Decl.Length);
            Assert.Equal(5, x13Ref.Length);
            VerifyModelForDeclarationPattern(model, x13Decl[0], x13Ref[0], x13Ref[1], x13Ref[2]);
            VerifyModelForDeclarationPattern(model, x13Decl[1], x13Ref[3], x13Ref[4]);

            var x14Decl = GetPatternDeclarations(tree, "x14").ToArray();
            var x14Ref = GetReferences(tree, "x14").ToArray();
            Assert.Equal(2, x14Decl.Length);
            Assert.Equal(4, x14Ref.Length);
            VerifyModelForDeclarationPattern(model, x14Decl[0], x14Ref);
            VerifyModelForDeclarationPattern(model, x14Decl[1], true);

            var x15Decl = GetPatternDeclarations(tree, "x15").ToArray();
            var x15Ref = GetReferences(tree, "x15").ToArray();
            Assert.Equal(2, x15Decl.Length);
            Assert.Equal(3, x15Ref.Length);
            for (int i = 0; i < x15Ref.Length; i++)
            {
                VerifyModelForDeclarationPattern(model, x15Decl[0], x15Ref[i]);
            }
            VerifyModelForDeclarationPatternDuplicateInSameScope(model, x15Decl[1]);
        }

        [Fact]
        public void Scope_SwitchLabelGuard_02()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                Dummy(x1 is var y1);
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] trash) 
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_03()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                while (Dummy(x1 is var y1)) break;
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data) 
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_04()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                do
                    val = 0;
                while (Dummy(x1 is var y1) && false);
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_05()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                lock ((object)Dummy(x1 is var y1)) {}
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_06()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                if (Dummy(x1 is var y1)) {}
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_07()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                switch (Dummy(x1 is var y1)) 
                {
                    default: break;
                }
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_08()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        foreach (var x in Test(1)) {}
    }

    static System.Collections.IEnumerable Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                yield return Dummy(x1 is var y1);
                System.Console.WriteLine(y1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput: @"2").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var yRef = GetReferences(tree, "y1").Single();

            Assert.Equal("System.Int32", model.GetTypeInfo(yRef).Type.ToTestDisplayString());
        }

        [Fact]
        public void Scope_SwitchLabelGuard_09()
        {
            var source =
@"
public class X
{
    public static int Data = 2;

    public static void Main()
    {
        Test(1);
    }

    static void Test(int val)
    {
        switch (val)
        {
            case 1 when Dummy(123, Data is var x1):
                var z1 = x1 > 0 & Dummy(x1 is var y1);
                System.Console.WriteLine(y1);
                System.Console.WriteLine(z1);
                break;
        }
    }

    static bool Dummy(params object[] data)
    {
        return true;
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular);

            CompileAndVerify(compilation, expectedOutput:
@"2
True").VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();

            var yRef = GetReferences(tree, "y1").Single();
            Assert.Equal("System.Int32", compilation.GetSemanticModel(tree).GetTypeInfo(yRef).Type.ToTestDisplayString());

            var zRef = GetReferences(tree, "z1").Single();
            Assert.Equal("System.Boolean", compilation.GetSemanticModel(tree).GetTypeInfo(zRef).Type.ToTestDisplayString());
        }
    }
}
