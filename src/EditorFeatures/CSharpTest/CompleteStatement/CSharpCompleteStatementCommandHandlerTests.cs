// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CompleteStatement;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CompleteStatement;

[Trait(Traits.Feature, Traits.Features.CompleteStatement)]
public sealed class CSharpCompleteStatementCommandHandlerTests : AbstractCompleteStatementTests
{
    private static string CreateTestWithMethodCall(string code)
    {
        return
            """
            class C
                {
                    static void Main(string[] args)
                    {
                        int x = 1;
                        int y = 2;
                        int[] a = { 1,2 };
            """ + code + """
                        int z = 4;
                    }
                }

                static class ClassC
                {
                    internal static int MethodM(int a, int b)
                        => a * b;
                }
            }
            """;
    }

    #region ParameterList

    [WpfTheory]
    [InlineData("extern void M(object o$$)", "extern void M(object o)")]
    [InlineData("partial void M(object o$$)", "partial void M(object o)")]
    [InlineData("abstract void M(object o$$)", "abstract void M(object o)")]
    [InlineData("abstract void M($$object o)", "abstract void M(object o)")]
    [InlineData("abstract void M(object o = default(object$$))", "abstract void M(object o = default(object))")]
    [InlineData("abstract void M(object o = default($$object))", "abstract void M(object o = default(object))")]
    [InlineData("abstract void M(object o = $$default(object))", "abstract void M(object o = default(object))")]
    [InlineData("public record C(int X, $$int Y)", "public record C(int X, int Y)")]
    [InlineData("public record C(int X, int$$ Y)", "public record C(int X, int Y)")]
    [InlineData("public record C(int X, int Y$$)", "public record C(int X, int Y)")]
    [InlineData("public record class C(int X, int Y$$)", "public record class C(int X, int Y)")]
    [InlineData("public record struct C(int X, int Y$$)", "public record struct C(int X, int Y)")]
    [InlineData("public class C(int X, $$int Y)", "public class C(int X, int Y)")]
    [InlineData("public class C(int X, int$$ Y)", "public class C(int X, int Y)")]
    [InlineData("public class C(int X, int Y$$)", "public class C(int X, int Y)")]
    [InlineData("public struct C(int X, $$int Y)", "public struct C(int X, int Y)")]
    [InlineData("public struct C(int X, int$$ Y)", "public struct C(int X, int Y)")]
    [InlineData("public struct C(int X, int Y$$)", "public struct C(int X, int Y)")]
    [InlineData("public interface C(int X, $$int Y)", "public interface C(int X, int Y)")]
    [InlineData("public interface C(int X, int$$ Y)", "public interface C(int X, int Y)")]
    [InlineData("public interface C(int X, int Y$$)", "public interface C(int X, int Y)")]
    public void ParameterList_CouldBeHandled(string signature, string expectedSignature)
        => VerifyTypingSemicolon($$"""
            public class Class1
            {
                {{signature}}
            }
            """, $$"""
            public class Class1
            {
                {{expectedSignature}};$$
            }
            """);

    [WpfFact]
    public void ParameterList_InterfaceMethod()
        => VerifyTypingSemicolon("""
            public interface I
            {
                public void M(object o$$)
            }
            """, """
            public interface I
            {
                public void M(object o);$$
            }
            """);

    [WpfTheory]
    [InlineData("void M$$(object o)")]
    [InlineData("void Me$$thod(object o)")]
    [InlineData("void Method(object o$$")]
    [InlineData("void Method($$object o")]
    [InlineData("partial void Method($$object o) { }")]
    public void ParameterList_NotHandled(string signature)
        => VerifyNoSpecialSemicolonHandling($$"""
            public class Class1
            {
                {{signature}}
            }
            """);

    #endregion

    #region ArgumentListOfMethodInvocation

    [WpfFact]
    public void ArgumentListOfMethodInvocation1()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x, y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation2()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation3()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x,$$ y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation4()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, $$y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation5()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y$$)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation6()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y)$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation7()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y""$$)");
        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y"");$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation_MissingParen()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y"");$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation_CommentsAfter()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y) //Comments");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$ //Comments");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation_SemicolonAlreadyExists()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y);");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WorkItem(34176, "https://github.com/dotnet/roslyn/pull/34177")]
    [WpfTheory]
    [InlineData("""
        $$ "Test"
        """)]
    [InlineData("""
        $$"Test"
        """)]
    [InlineData("""
        "Test"$$
        """)]
    [InlineData("""
        "Test" $$
        """)]

    // Verbatim strings
    [InlineData("""
        $$ @"Test"
        """)]
    [InlineData("""
        $$@"Test"
        """)]
    [InlineData("@\"Test\"$$ ")]
    [InlineData("@\"Test\" $$")]

    // Raw strings
    [InlineData(""""
        $$ """Test"""
        """")]
    [InlineData(""""
        $$"""Test"""
        """")]
    [InlineData(""""
        """Test"""$$
        """")]
    [InlineData(""""
        """Test""" $$
        """")]

    // UTF-8 strings
    [InlineData("$$ \"Test\"u8")]
    [InlineData(" $$\"Test\"u8")]
    [InlineData("""
        "Test"u8$$
        """)]
    [InlineData("""
        "Test"u8 $$
        """)]
    public void ArgumentListOfMethodInvocation_OutsideStringAsMethodArgument(string argument)
    {
        var code = CreateTestWithMethodCall($@"var test = Console.WriteLine({argument})");

        var expected = CreateTestWithMethodCall($@"var test = Console.WriteLine({argument.Replace("$$", "")});$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WorkItem(34176, "https://github.com/dotnet/roslyn/pull/34177")]
    [WpfTheory]
    [InlineData("""
        "Test$$"
        """)]
    [InlineData("""
        @"Test$$"
        """)]
    [InlineData("""
        @$$"Test"
        """)]
    [InlineData(""""
        """Test$$"""
        """")]
    [InlineData(""""
        """Test"$$""
        """")]
    [InlineData(""""
        """Test""$$"
        """")]
    [InlineData("""
        "Test$$"u8
        """)]
    [InlineData("""
        "Test"$$u8
        """)]
    public void ArgumentListOfMethodInvocation_InsideStringAsMethodArgument(string argument)
    {
        var code = CreateTestWithMethodCall($@"var test = Console.WriteLine({argument})");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation_MultiLine()
    {
        var code = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                x$$, 
                y)
            """);

        var expected = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                x, 
                y);$$
            """);

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfMethodInvocation_MultiLine3()
    {
        var code = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                x$$, 
                y
                )
            """);

        var expected = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                x, 
                y
                );$$
            """);

        VerifyTypingSemicolon(code, expected);
    }

    #endregion

    #region ArgumentListOfNestedMethodInvocation

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation1()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x, y.ToString())");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation2()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y.ToString())");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation3()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, $$y.ToString())");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation4()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToS$$tring())");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation5()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString$$())");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation6()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString($$))");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation7()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString()$$)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation8()
    {

        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString())$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation9()
    {

        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ClassC.MethodM(4,ClassC.MethodM(5,ClassC.MethodM(6,7$$))))");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ClassC.MethodM(4,ClassC.MethodM(5,ClassC.MethodM(6,7))));$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation8_SemicolonAlreadyExists()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString($$));");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition1()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition2()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x.ToString(), y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition3()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition4()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString()$$, y)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition5()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y$$)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_DualPosition_SemicolonAlreadyExists()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y);");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MultiLine()
    {
        var code = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            y$$)
            """);

        var expected = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            y);$$
            """);

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MultiLine2()
    {
        var code = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            y$$
                            )
            """);

        var expected = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            y
                            );$$
            """);

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MultiLine3()
    {
        var code = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            "y"$$
                            )
            """);

        var expected = CreateTestWithMethodCall("""
            var test = ClassC.MethodM(
                            x.ToString(), 
                            "y"
                            );$$
            """);

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MissingBothParens()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MissingInnerParen()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentListOfNestedMethodInvocation_MissingOuterParen()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y");

        VerifyNoSpecialSemicolonHandling(code);
    }

    #endregion

    #region ArgumentList_Array

    [WpfFact]
    public void ArgumentList_Array1()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x[0], x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array2()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$[0], x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array3()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[$$0], x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array4()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0$$], x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array5()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0]$$, x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array6()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0],$$ x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array7()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], $$x[1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array8()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[$$1])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array9()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1$$])");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array10()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]$$)");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array11()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1])$$");

        var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact]
    public void ArgumentList_Array_MissingBoth()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentList_Array_MissingOuter()
    {

        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void ArgumentList_Array_MissingInner()
    {

        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1)$$");

        VerifyNoSpecialSemicolonHandling(code);
    }

    #endregion

    #region FieldInitializer

    [WpfFact]
    public void FieldInitializer_NoParens()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                int i = 4$$
                int j = 5;
            """);

    [WpfFact]
    public void FieldInitializer2()
        => VerifyTypingSemicolon("""
            class C
            {
                int i = Min(2$$,3)
                int j = 5;
            """, """
            class C
            {
                int i = Min(2,3);$$
                int j = 5;
            """);

    [WpfFact]
    public void FieldInitializer2b_MissingParen()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                int i = Min(2$$,3
                int j = 5;
            """);

    [WpfFact]
    public void FieldInitializer3()
        => VerifyTypingSemicolon("""
            class C
            {
                int i = Min(Max(4,5$$),3)
                int j = 5;
            """, """
            class C
            {
                int i = Min(Max(4,5),3);$$
                int j = 5;
            """);

    [WpfFact]
    public void FieldInitializer3b_MissingInner()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                int i = Min(Max(4,5$$,3)
                int j = 5;
            """);

    #endregion

    #region ForLoop

    [WpfFact]
    public void ForLoopSingleInitializer1()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for (int i = 0$$ )
                    int j;
            """);

    [WpfFact]
    public void ForLoopSingleInitializer2()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for (int i = 0$$ i < 5; i++)
                    int j;
            """);

    [WpfFact]
    public void ForLoopSingleInitializer3()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for (int i = 0$$; i < 3; i = i + 1)
                   {
                        x = x * 3;
                    }
                    System.Console.Write("{0}", x);
                }
            }
            """);

    [WpfFact]
    public void ForLoopSingleInitializer_MissingParen()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for (int i = 0$$
                    int j;
            """);

    [WpfFact]
    public void ForLoopNoStatements()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ($$
                    int j;
            """);

    [WpfFact]
    public void ForLoopNoStatements2()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( $$
                    int j;
            """);

    [WpfFact]
    public void ForLoopNoStatements3()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( ; $$
                    int j;
            """);

    [WpfFact]
    public void ForLoopNoStatements4()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( ; ;$$
                    int j;
            """);

    [WpfFact]
    public void ForLoopNoStatements5()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( $$ ;)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer1()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( $$int i = 0, int j = 0)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer2()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int$$ i = 0, int j = 0)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer3()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int i$$ = 0, int j = 0)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer4()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int i = 0, $$int j = 0)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer5()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int i = 0, int j =$$ 0)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer6()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int i = 0, int j = 0$$)
                    int j;
            """);

    [WpfFact]
    public void ForLoopMultistatementInitializer7()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main()
                {
                    for ( int i = 0, int j = 0$$)
                    int j;
            """);

    [WpfFact]
    public void ForLoopNewInInitializer1()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    for (C1 i = new C1($$))
                    int j;
                }
            }
            public class C1
            {
                public static C1 operator ++(C1 obj)
                {
                    return obj;
                }
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    for (C1 i = new C1();$$)
                    int j;
                }
            }
            public class C1
            {
                public static C1 operator ++(C1 obj)
                {
                    return obj;
                }
            }
            """);

    [WpfFact]
    public void ForLoopNewInInitializer_MissingOneParen()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    for (C1 i = new C1()$$
                    int j;
                }
            }
            public class C1
            {
                public static C1 operator ++(C1 obj)
                {
                    return obj;
                }
            }
            """);

    [WpfFact]
    public void ForLoopNewInInitializer2_MissingBothParens()
    {
        // only adding one closing paren

        VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    for (C1 i = new C1($$
                    int j;
                }
            }
            public class C1
            {
                public static C1 operator ++(C1 obj)
                {
                    return obj;
                }
            }
            """);
    }

    [WpfFact]
    public void ForLoopDeclaration()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"$$) i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd");$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopDeclaration2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"$$), j=1 i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"), j=1;$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopDeclaration3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"$$); i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd");$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopDeclaration4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"$$), j=1; i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"), j=1;$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopDeclaration_MissingParen()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"$$ i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd";$$ i < 10; i++)
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/32250")]
    public void ForLoopInitializers()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd"$$) i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd");$$ i < 10; i++)
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/32250")]
    public void ForLoopInitializers2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    int j;
                    for (i = s.IndexOf("bcd"$$), j=1 i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    int j;
                    for (i = s.IndexOf("bcd"), j=1;$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopInitializers3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd"$$); i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd");$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopInitializers4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    int j;
                    for (i = s.IndexOf("bcd"$$), j=1; i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    int j;
                    for (i = s.IndexOf("bcd"), j=1;$$ i < 10; i++)
            """);
    [WpfFact]
    public void ForLoopInitializers_MissingParen()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd"$$ i < 10; i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    int i;
                    for (i = s.IndexOf("bcd";$$ i < 10; i++)
            """);

    [WpfFact]
    public void ForLoopCondition()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < s.IndexOf("x"$$) i++)
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < s.IndexOf("x");$$ i++)
            """);

    [WpfFact]
    public void ForLoopConditionIsNull()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    for (int i = 0; $$ ; i++)
                    {
                        Console.WriteLine("test");
                    }
            """);

    [WpfFact]
    public void ForLoopConditionIsNull2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    for (int i = Math.Min(3,4$$);  ; i++)
                    {
                        Console.WriteLine("test");
                    }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    for (int i = Math.Min(3,4);$$  ; i++)
                    {
                        Console.WriteLine("test");
                    }
            """);

    [WpfFact]
    public void ForLoopIncrement()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < s.IndexOf("x"); i = i.IndexOf("x"$$))
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < s.IndexOf("x"); i = i.IndexOf("x";$$))
            """);

    [WpfFact]
    public void ForLoopBody()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < 10; i++)
                    {
                        i.ToString($$)
                    }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    string s = "abcdefghij";
                    for (int i = s.IndexOf("bcd"); i < 10; i++)
                    {
                        i.ToString();$$
                    }
            """);

    [WpfFact]
    public void ForLoopObjectInitializer_MissingParen()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    for (Goo f = new Goo { i = 0, s = "abc"$$ }
                }
            }
            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ForLoopObjectInitializer()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    for (Goo f = new Goo { i = 0, s = "abc"$$ } )
                }
            }
            public class Goo
            {
                public int i;
                public string s;
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    for (Goo f = new Goo { i = 0, s = "abc" };$$ )
                }
            }
            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ForLoopObjectInitializer_MissingBrace()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    for (Goo f = new Goo { i = 0, s = "abc"$$
                }
            }
            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    #endregion

    #region Indexer

    [WpfFact]
    public void Indexer()
        => VerifyTypingSemicolon("""
            class SampleCollection<T>
            {
                private T[] arr = new T[100];
                private int i;
                public int Property
                {
                    get { return arr[i$$] }
                    set { arr[i] = value; }
                }
            }
            """, """
            class SampleCollection<T>
            {
                private T[] arr = new T[100];
                private int i;
                public int Property
                {
                    get { return arr[i];$$ }
                    set { arr[i] = value; }
                }
            }
            """);

    [WpfFact]
    public void Indexer2()
        => VerifyTypingSemicolon("""
            class test
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[1$$]
                }
            }
            """, """
            class test
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[1];$$
                }
            }
            """);

    [WpfFact]
    public void Indexer3()
        => VerifyTypingSemicolon("""
            class C
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[Math.Min(2,3$$)]
                }
            }
            """, """
            class C
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[Math.Min(2,3)];$$
                }
            }
            """);

    [WpfFact]
    public void Indexer4()
        => VerifyTypingSemicolon("""
            class C
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[Math.Min(2,3$$)
                }
            }
            """, """
            class C
            {
                int[] array = { 1, 2, 3 };

                void M()
                {
                    var i = array[Math.Min(2,3;$$)
                }
            }
            """);

    #endregion

    #region ArrayInitializer (explicit type)

    [WpfFact]
    public void ArrayInitializer()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0, "abc" }$$
                }
            }

            """);

    [WpfFact]
    public void ArrayInitializer2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0, "abc"$$ }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0, "abc" };$$
                }
            }

            """);

    [WpfFact]
    public void ArrayInitializer3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0$$, "abc" }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0, "abc" };$$
                }
            }

            """);

    [WpfFact]
    public void ArrayInitializer4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { $$ 0, "abc" }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] {  0, "abc" };$$
                }
            }

            """);

    [WpfFact]
    public void ArrayInitializer_MissingBrace()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new object[] { 0, "abc"$$
                }
            }

            """);

    #endregion

    #region ArrayInitializer (implicit type)

    [WpfFact]
    public void ImplicitTypeArrayInitializer()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0, 1 }$$
                }
            }

            """);

    [WpfFact]
    public void ImplicitTypeArrayInitializer2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0, 1$$ }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void ImplicitTypeArrayInitializer3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0$$, 1 }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void ImplicitTypeArrayInitializer4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { $$ 0, 1 }
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] {  0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void ImplicitTypeArrayInitializer_MissingBrace()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    var f = new[] { 0, 1$$
                }
            }

            """);

    #endregion

    #region Collection Expression

    [WpfFact]
    public void CollectionExpression()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0, "abc" ]$$
                }
            }

            """);

    [WpfFact]
    public void CollectionExpression2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0, "abc"$$ ]
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0, "abc" ];$$
                }
            }

            """);

    [WpfFact]
    public void CollectionExpression3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0$$, "abc" ]
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0, "abc" ];$$
                }
            }

            """);

    [WpfFact]
    public void CollectionExpression4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ $$ 0, "abc" ]
                }
            }

            """, """
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [  0, "abc" ];$$
                }
            }

            """);

    [WpfFact]
    public void CollectionExpression_MissingBrace()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    object[] f = [ 0, "abc"$$
                }
            }

            """);

    #endregion

    #region CollectionInitializer

    [WpfFact]
    public void CollectionInitializer()
        => VerifyNoSpecialSemicolonHandling("""
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0, 1 }$$
                }
            }

            """);

    [WpfFact]
    public void CollectionInitializer2()
        => VerifyTypingSemicolon("""
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0, 1$$ }
                }
            }

            """, """
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void CollectionInitializer3()
        => VerifyTypingSemicolon("""
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0$$, 1 }
                }
            }

            """, """
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void CollectionInitializer4()
        => VerifyTypingSemicolon("""
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { $$ 0, 1 }
                }
            }

            """, """
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> {  0, 1 };$$
                }
            }

            """);

    [WpfFact]
    public void CollectionInitializer_MissingBrace()
        => VerifyNoSpecialSemicolonHandling("""
            using System.Collections.Generic;
            class C
            {
                static void Main(string[] args)
                {
                    var f = new List<int> { 0, 1$$
                }
            }

            """);

    #endregion

    #region ObjectInitializer

    [WpfFact]
    public void ObjectInitializer()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc" }$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ObjectInitializer2()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc"$$ }
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc" };$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ObjectInitializer3()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0$$, s = "abc" }
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc" };$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ObjectInitializer4()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i =$$ 0, s = "abc" }
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc" };$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    [WpfFact]
    public void ObjectInitializer_MissingBrace()
        => VerifyTypingSemicolon("""
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc"$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """, """
            class C
            {
                static void Main(string[] args)
                {
                    Goo f = new Goo { i = 0, s = "abc";$$
                }
            }

            public class Goo
            {
                public int i;
                public string s;
            }
            """);

    #endregion

    #region Accessors

    [WpfFact]
    public void PropertyAccessors1()
        => VerifyTypingSemicolon("""
            public class ClassC
            {
                private int xValue = 7;
                public int XValue
                {
                    get
                    {
                        return Math.Min(xValue$$, 1)
                    } 
                }
            }
            """, """
            public class ClassC
            {
                private int xValue = 7;
                public int XValue
                {
                    get
                    {
                        return Math.Min(xValue, 1);$$
                    } 
                }
            }
            """);

    [WpfFact]
    public void PropertyAccessors2()
        => VerifyTypingSemicolon("""
            public class ClassC
            {
                private int xValue = 7;
                public int XValue
                {
                    get
                    {
                        return Math.Min(Math.Max(xValue,0$$), 1)
                    } 
                }
            }
            """, """
            public class ClassC
            {
                private int xValue = 7;
                public int XValue
                {
                    get
                    {
                        return Math.Min(Math.Max(xValue,0), 1);$$
                    } 
                }
            }
            """);

    [WpfFact]
    public void PropertyAccessors3()
        => VerifyNoSpecialSemicolonHandling("""
            public class Person
            {
               private string firstName;
               private string lastName;

               public Person(string first, string last)
               {
                  firstName = first;
                  lastName = last;
               }

               public string Name => $"{firstName} {lastName}"$$   
            }
            """);

    [WpfFact]
    public void PropertyAccessors4()
        => VerifyNoSpecialSemicolonHandling("""
            public class SaleItem
            {
               string name;
               public string Name 
               {
                  get => name;
                  set => name = value$$
               }
            }
            """);

    [WpfFact]
    public void PropertyAccessors5()
        => VerifyNoSpecialSemicolonHandling("""
            public class SaleItem
            {
               string name;
               public string Name 
               {
                  get => name$$
                  set => name = value;
               }
            }
            """);

    [WpfFact]
    public void PropertyAccessors6()
        => VerifyTypingSemicolon("""
            public class SaleItem
            {
               string name;
               public string Name 
               {
                  get => name.ToUpper($$)
                  set => name = value;
               }
            }
            """, """
            public class SaleItem
            {
               string name;
               public string Name 
               {
                  get => name.ToUpper();$$
                  set => name = value;
               }
            }
            """);

    [WpfFact]
    public void PropertyAccessors7()
        => VerifyNoSpecialSemicolonHandling("""
            public class SaleItem
            {
               public string Name 
               { get$$ set; }
            }
            """);

    [WpfFact]
    public void PropertyInitializer1()
        => VerifyTypingSemicolon("""
            public class C
            {
               public static C MyProp { get; } = new C($$)
            }
            """, """
            public class C
            {
               public static C MyProp { get; } = new C();$$
            }
            """);

    [WpfFact]
    public void PropertyAttribute1()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                public int P
                {
                    [My(typeof(C$$))]
                    get
                    {
                        return 0;
                    }
                }
            }
            """);

    #endregion

    #region ParenthesizeExpression

    [WpfFact]
    public void ParenthesizedExpression_Assignment1()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    int i = (6*5$$)
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    int i = (6*5);$$
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_Assignment2()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    int i = (6*Math.Min(4,5$$))
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    int i = (6*Math.Min(4,5));$$
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_Assignment3()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    int[] array = { 2, 3, 4 };
                    int i = (6*array[2$$])
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    int[] array = { 2, 3, 4 };
                    int i = (6*array[2]);$$
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_ForLoop()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int j = (i+i$$)
                    }
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    for (int i = 0; i < 10; i++)
                    {
                        int j = (i+i);$$
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_ForLoop2()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    for (int i = ((3+2)*4$$); i < 10; i++)
                    {
                        int j = (i+i);
                    }
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    for (int i = ((3+2)*4);$$ i < 10; i++)
                    {
                        int j = (i+i);
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_ForLoop3()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    for (int i = 0; i < ((3+2)*4$$); i++)
                    {
                        int j = (i+i);
                    }
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    for (int i = 0; i < ((3+2)*4);$$ i++)
                    {
                        int j = (i+i);
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_ForEach()
        => VerifyNoSpecialSemicolonHandling("""
            public class Class1
            {
                static void Main(string[] args)
                {
                    foreach (int i in M((2*3)+4$$))
                    {

                    }
                }

                private static int[] M(int i)
                {
                    int[] value = { 2, 3, 4 };
                    return value;
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_GoTo2()
        => VerifyTypingSemicolon("""
            static void Main()
            {
                int n = 1;
                switch (n)
                {
                    case 1:
                        goto case (2+1$$)
                    case 3:
                        break
                    default:
                        break;
                }
            }
            """, """
            static void Main()
            {
                int n = 1;
                switch (n)
                {
                    case 1:
                        goto case (2+1);$$
                    case 3:
                        break
                    default:
                        break;
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_Switch()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    switch (i$$)
                    {
                        case 1:
                        case 2:
                        case 3:
                            break;
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_Switch2()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    switch (4*(i+2$$))
                    {
                        case 1:
                        case 2:
                        case 3:
                            break;
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_Switch3()
        => VerifyTypingSemicolon("""
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    switch (i)
                    {
                        case 1:
                            Console.WriteLine(4*(i+2$$))
                        case 2:
                        case 3:
                            break;
                    }
                }
            }
            """, """
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    switch (i)
                    {
                        case 1:
                            Console.WriteLine(4*(i+2));$$
                        case 2:
                        case 3:
                            break;
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_While()
        => VerifyNoSpecialSemicolonHandling("""
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    while (i<4$$)
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_While2()
        => VerifyNoSpecialSemicolonHandling("""
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    while (i<Math.Max(4,5$$))
                    {
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [WpfFact]
    public void ParenthesizedExpression_While3()
        => VerifyTypingSemicolon("""
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    while (i<Math.Max(4,5))
                    {
                        Console.WriteLine(i$$)
                    }
                }
            }
            """, """
            using System;
            class Program
            {
                static void Main()
                {
                    int i = 3;
                    while (i<Math.Max(4,5))
                    {
                        Console.WriteLine(i);$$
                    }
                }
            }
            """);

    #endregion

    [WpfTheory]
    [InlineData("default(object$$)", "default(object)")]
    [InlineData("default($$object)", "default(object)")]
    public void DefaultExpression_Handled(string expression, string expectedExpression)
        => VerifyTypingSemicolon($$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expression}}
                }
            }
            """, $$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expectedExpression}};$$
                }
            }
            """);

    [WpfTheory, WorkItem("https://github.com/dotnet/roslyn/issues/52137")]
    [InlineData("typeof(object$$)", "typeof(object)")]
    [InlineData("typeof($$object)", "typeof(object)")]
    public void TypeOfExpression_Handled(string expression, string expectedExpression)
        => VerifyTypingSemicolon($$"""
            public class Class1
            {
                void M()
                {
                    var x = {{expression}}
                }
            }
            """, $$"""
            public class Class1
            {
                void M()
                {
                    var x = {{expectedExpression}};$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/52365")]
    public void TupleExpression_Handled()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    var x = (0, 0$$)
                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    var x = (0, 0);$$
                }
            }
            """);

    [WpfTheory]
    [InlineData("default$$(object)")]
    [InlineData("def$$ault(object)")]
    [InlineData("default(object$$")]
    [InlineData("default($$object")]
    public void DefaultExpression_NotHandled(string expression)
        => VerifyNoSpecialSemicolonHandling($$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expression}}
                }
            }
            """);

    [WpfTheory]
    [InlineData("checked(3 + 3$$)", "checked(3 + 3)")]
    [InlineData("checked($$3 + 3)", "checked(3 + 3)")]
    [InlineData("unchecked(3 + 3$$)", "unchecked(3 + 3)")]
    [InlineData("unchecked($$3 + 3)", "unchecked(3 + 3)")]
    public void CheckedExpression_Handled(string expression, string expectedExpression)
        => VerifyTypingSemicolon($$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expression}}
                }
            }
            """, $$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expectedExpression}};$$
                }
            }
            """);

    [WpfTheory]
    [InlineData("checked$$(3 + 3)")]
    [InlineData("che$$cked(3 + 3)")]
    [InlineData("checked(3 + 3$$")]
    [InlineData("checked($$3 + 3")]
    [InlineData("unchecked$$(3 + 3)")]
    [InlineData("unche$$cked(3 + 3)")]
    [InlineData("unchecked(3 + 3$$")]
    [InlineData("unchecked($$3 + 3")]
    public void CheckedExpression_NotHandled(string expression)
        => VerifyNoSpecialSemicolonHandling($$"""
            public class Class1
            {
                void M()
                {
                    int i = {{expression}}
                }
            }
            """);

    [WpfFact]
    public void ThrowStatement_MissingBoth()
        => VerifyNoSpecialSemicolonHandling("""
            public class Class1
            {
                void M()
                {
                    string s = "Test";
                    throw new Exception(s.ToUpper($$

                }
            }
            """);

    [WpfFact]
    public void ThrowStatement()
        => VerifyTypingSemicolon("""
            public class Class1
            {
                void M()
                {
                    string s = "Test";
                    throw new Exception(s.ToUpper($$))

                }
            }
            """, """
            public class Class1
            {
                void M()
                {
                    string s = "Test";
                    throw new Exception(s.ToUpper());$$

                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_SemicolonBeforeClassDeclaration()
        => VerifyNoSpecialSemicolonHandling("""
            $$
            class C
            {
            }
            """);

    [WpfFact]
    public void DoNotCompleteStatement_DocComments()
        => VerifyNoSpecialSemicolonHandling("""
            /// Testing $$
            class C
            {
            }
            """);

    [WpfFact]
    public void DoNotComplete_FormatString()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                void Main()
                {
                    Console.WriteLine(String.Format("{0:##;(##)$$**Zero**}", 0));
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_EmptyStatement()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                void Main()
                {
                    ;$$
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_EmptyStatement2()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                void Main()
                {
                    ; $$
                }
            }
            """);

    [WpfFact]
    public void DoWhile()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n$$ < 5)
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < 5);$$
                }
            }
            """);

    [WpfFact]
    public void DoWhile2()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < 5)$$
                }
            }
            """);

    [WpfFact]
    public void DoWhile3()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while $$(n < 5)
                }
            }
            """);

    [WpfFact]
    public void DoWhile4()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,$$5))
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,5));$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35260")]
    public void DoWhile5()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while ($$n < Min(4,5))
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,5));$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35260")]
    public void DoWhile6()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,5)$$)
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,5));$$
                }
            }
            """);

    [WpfFact]
    public void DoWhile_MissingParen()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,$$5)
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                    } while (n < Min(4,;$$5)
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Break()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                        break$$
                    } while (n < 5);
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Break2()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                        bre$$ak
                    } while (n < 5);
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Break3()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
                void M()
                {
                    int n = 0;
                    do
                    {
                        Console.WriteLine(n);
                        n++;
                        $$break
                    } while (n < 5);
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Checked()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
               {
                   static void Main(string[] args)
                   {
                       int num;
                       // assign maximum value
                       num = int.MaxValue;
                       try
                       {
                           checked$$
                           {
                               num = num + 1;
                               Console.WriteLine(num);
                           }
                       }
                       catch (Exception e)
                       {
                           Console.WriteLine(e.ToString());
                       }
                       Console.ReadLine();
                   }
               }
            """);

    [WpfFact]
    public void DoNotComplete_Unchecked()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
               {
                   static void Main(string[] args)
                   {
                       int num;
                       // assign maximum value
                       num = int.MaxValue;
                       try
                       {
                           unchecked$$
                           {
                               num = num + 1;
                               Console.WriteLine(num);
                           }
                       }
                       catch (Exception e)
                       {
                           Console.WriteLine(e.ToString());
                       }
                       Console.ReadLine();
                   }
               }
            """);

    [WpfFact]
    public void DoNotComplete_Fixed()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                static void Main()
                {
                    Console.WriteLine(Transform());
                }

                unsafe static string Transform()
                {
                    string value = System.IO.Path.GetRandomFileName();
                    fixed$$ (char* pointer = value)
                    {
                        for (int i = 0; pointer[i] != '\0'; ++i)
                        {
                            pointer[i]++;
                        }
                        return new string(pointer);
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Continue()
        => VerifyNoSpecialSemicolonHandling("""
            class ContinueTest
            {
                static void Main()
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        if (i < 9)
                        {
                            continue$$
                        }
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Continue2()
        => VerifyNoSpecialSemicolonHandling("""
            class ContinueTest
            {
                static void Main()
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        if (i < 9)
                        {
                            cont$$inue
                        }
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Continue3()
        => VerifyNoSpecialSemicolonHandling("""
            class ContinueTest
            {
                static void Main()
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        if (i < 9)
                        {
                            $$continue
                        }
                        Console.WriteLine(i);
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_GoTo()
        => VerifyNoSpecialSemicolonHandling("""
            static void Main()
            {
                int n = 1;
                switch (n)
                {
                    case 1:
                        goto $$case 3;                
                        break;
                    case 3:
                        break
                    default:
                        break;
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_IfStatement()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                void M()
                {
                    int x = 0;
                    if (x == 0$$)
                    {
                        return;
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Labeled()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                static void Main()
                {
                    if (true)
                        goto labeled;
                    labeled$$: return;
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_IfStatement2()
        => VerifyNoSpecialSemicolonHandling("""
            class Program
            {
                void M()
                {
                    int x = 0;
                    if (x == Math.Min(4,5$$))
                    {
                        return;
                    }
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_ClassNameOfMethodInvocation1()
    {
        var code = CreateTestWithMethodCall(@"var test = $$ClassC.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_ClassNameOfMethodInvocation2()
    {
        var code = CreateTestWithMethodCall(@"var test = C$$lassC.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_ClassNameOfMethodInvocation3()
    {
        var code = CreateTestWithMethodCall(@"var test = Class$$C.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_ClassNameOfMethodInvocation4()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC$$.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_MethodNameOfMethodInvocation1()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.Meth$$odM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_MethodNameOfMethodInvocation2()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.$$MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_MethodNameOfMethodInvocation3()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM$$(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_SemicolonBeforeEquals()
    {
        var code = CreateTestWithMethodCall(@"var test $$= ClassC.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_SemicolonAfterEquals()
    {
        var code = CreateTestWithMethodCall(@"var test =$$ ClassC.MethodM(x,y)");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfTheory]
    [InlineData("""
        "Test $$Test"
        """)]
    [InlineData("""
        "Test Test$$"
        """)]
    [InlineData("""
        "Test Test"$$
        """)]
    public void DoNotComplete_String(string literal)
    {
        var code = CreateTestWithMethodCall($@"var s={literal}");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/49929")]
    [InlineData("""
        "$$
        """)]
    [InlineData("""
        "$$Test Test
        """)]
    [InlineData("""
        "Test Test$$
        """)]
    [InlineData(""""
        """$$
        """")]
    [InlineData(""""
        """$$Test Test
        """")]
    [InlineData(""""
        """Test Test$$
        """")]
    public void DoNotComplete_UnterminatedString(string literal)
    {
        var code = CreateTestWithMethodCall(
            $"""
            Test(
                {literal}
            )
            """);

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfTheory]
    [InlineData("""
        "Test $$Test"u8
        """)]
    [InlineData("""
        "Test Test$$"u8
        """)]
    [InlineData("""
        "Test Test"$$u8
        """)]
    [InlineData("""
        "Test Test"u8$$
        """)]
    public void DoNotComplete_Utf8String(string literal)
    {
        var code = CreateTestWithMethodCall($@"var test={literal}");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfTheory]
    [InlineData("'T$$'")]
    [InlineData("'$$'")]
    [InlineData("'$$T'")]
    [InlineData("'T'$$")]
    public void DoNotComplete_CharLiteral(string literal)
    {
        var code = CreateTestWithMethodCall($"var s={literal}");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfTheory]
    [WorkItem("https://github.com/dotnet/roslyn/issues/49929")]
    [InlineData("'T$$")]
    [InlineData("'$$T")]
    [InlineData("'$$")]
    public void DoNotComplete_UnterminatedCharLiteral(string literal)
    {
        var code = CreateTestWithMethodCall(
            $"""
            Test(
                {literal}
            )
            """);

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34176")]
    public void DoNotComplete_VerbatimStringAsMethodArgument_EndOfLine_NotEndOfString()
        => VerifyNoSpecialSemicolonHandling("""
                        var code = Foo(@"$$
            ") ;
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34176")]
    public void DoNotComplete_VerbatimStringAsMethodArgument_EndOfString_NotEndOfLine()
        => VerifyNoSpecialSemicolonHandling("""
                        var code = Foo(@"  $$" //comments
            );
            """);

    [WpfFact]
    public void DoNotComplete_InterpolatedString()
    {
        var code = CreateTestWithMethodCall("""
            var s=$"{obj.ToString($$)}"
            """);

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact]
    public void DoNotComplete_Attribute()
        => VerifyNoSpecialSemicolonHandling("""
            using System;

            class Program
            {
                static void Main()
                {
                    // Warning: 'Program.Test()' is obsolete
                    Test();
                }

                [Obsolete$$]
                static void Test()
                {
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Attribute2()
        => VerifyNoSpecialSemicolonHandling("""
            [assembly: System.Reflection.AssemblyVersionAttribute(null$$)]
            class Program
            {
            }
            """);

    [WpfFact]
    public void DoNotComplete_Attribute3()
        => VerifyNoSpecialSemicolonHandling("""
            using System.Runtime.CompilerServices;
            using System;

            class DummyAttribute : Attribute
            {
                public DummyAttribute([CallerMemberName$$] string callerName = "")
                {
                    Console.WriteLine("name: " + callerName);
                }
            }

            class A
            {
                [Dummy]
                public void MyMethod() {
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Attribute4()
        => VerifyNoSpecialSemicolonHandling("""
            using System;
            using System.Reflection;

            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool a, params object[] b)
                {
                    B = b;
                }
                public object[] B { get; }
            }

            [Mark(a: true, b: new object[$$] { "Hello", "World" })]
            static class Program
            {
                public static void Main()
                {
                    var attr = typeof(Program).GetCustomAttribute<MarkAttribute>();
                    Console.Write($"B.Length={attr.B.Length}, B[0]={attr.B[0]}, B[1]={attr.B[1]}");
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Attribute5()
        => VerifyNoSpecialSemicolonHandling("""
            using System;
            using System.Reflection;

            sealed class MarkAttribute : Attribute
            {
                public MarkAttribute(bool a, params object[] b)
                {
                    B = b;
                }
                public object[] B { get; }
            }

            [Mark(a: true, b: new object[] { "Hello", "World"$$ })]
            static class Program
            {
                public static void Main()
                {
                    var attr = typeof(Program).GetCustomAttribute<MarkAttribute>();
                    Console.Write($"B.Length={attr.B.Length}, B[0]={attr.B[0]}, B[1]={attr.B[1]}");
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Attribute6()
        => VerifyNoSpecialSemicolonHandling("""
            using System;

            class Program
            {
                static void Main()
                {
                    // Warning: 'Program.Test()' is obsolete
                    Test();
                }

                [Obsolete$$
                static void Test()
                {
                }
            }
            """);

    [WpfFact]
    public void DoNotComplete_Using()
        => VerifyNoSpecialSemicolonHandling("""
            using System.Linq$$

            """);

    [WpfFact]
    public void DoNotComplete_Using2()
        => VerifyNoSpecialSemicolonHandling("""
            using System.Linq$$;
            """);

    [WpfFact]
    public void DoNotComplete_Using3()
        => VerifyNoSpecialSemicolonHandling("""
            using System.$$Linq
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33851")]
    public void AtEndOfLineOutsideParens()
        => VerifyNoSpecialSemicolonHandling("""
            public class Class1
            {
                void M()
                {
                    string s = "Test";
                    string t = s.Replace("T", "t")$$
                        .Trim();

                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/33851")]
    public void OutsideParensBeforeSpaceDot()
        => VerifyNoSpecialSemicolonHandling("""
            public class Class1
            {
                void M()
                {
                    string s = "Test";
                    string t = s.Replace("T", "t")$$ .Trim();

                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void BeforeAttribute()
        => VerifyNoSpecialSemicolonHandling("""
            public class C
            {
            private const string s = 
                    @"test"$$

                [Fact]
                public void M()
                        {
                        }
                    }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void ElementBindingExpression()
        => VerifyTypingSemicolon("""
            class C
            {
                void M()
                {
                    var data = new int[3];
                    var value = data?[0$$]
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var data = new int[3];
                    var value = data?[0];$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void BeforeElementBindingExpression()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                void M()
                {
                    var data = new int[3];
                    var value = data?$$[0]
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void AfterElementBindingExpression()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                void M()
                {
                    var data = new int[3];
                    var value = data?[0]$$
                }
            }
            """);

    [WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void ImplicitElementAccessSyntax()
        => VerifyTypingSemicolon("""
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        [1$$] = 4,
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        [1] = 4,
                    };$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void BeforeImplicitElementAccessSyntax()
        => VerifyTypingSemicolon("""
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        $$[1] = 4,
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        [1] = 4,
                    };$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34666")]
    public void AfterImplicitElementAccessSyntax()
        => VerifyTypingSemicolon("""
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        [1]$$ = 4,
                    }
                }
            }
            """, """
            class C
            {
                void M()
                {
                    var d = new Dictionary<int, int>
                    {
                        [1] = 4,
                    };$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void AttributeParsedAsElementAccessExpression()
        => VerifyTypingSemicolon("""
            using System;
            internal class TestMethodAttribute : Attribute
            {
                readonly int i = Foo(3,4$$)

                [Test]
            }
            """, """
            using System;
            internal class TestMethodAttribute : Attribute
            {
                readonly int i = Foo(3,4);$$

                [Test]
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void MemberAccessOffOfMethod()
        => VerifyTypingSemicolon("""
            class Program
            {
                static void Main(string[] args)
                {
                    var s = "Hello";
                    var t = s.ToLower($$).Substring(1);
                }
            }
            """, """
            class Program
            {
                static void Main(string[] args)
                {
                    var s = "Hello";
                    var t = s.ToLower();$$.Substring(1);
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void LinqQuery()
        => VerifyTypingSemicolon("""
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void Main(string[] args)
                {
                    List<int> c1 = new List<int> { 1, 2, 3, 4, 5, 7 };
                    List<int> c2 = new List<int> { 10, 30, 40, 50, 60, 70 };
                    var c3 = c1.SelectMany(x1 => c2
                        .Where(x2 => object.Equals(x1, x2 / 10$$))
                        .Select(x2 => x1 + x2));
                }
            }
            """, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void Main(string[] args)
                {
                    List<int> c1 = new List<int> { 1, 2, 3, 4, 5, 7 };
                    List<int> c2 = new List<int> { 10, 30, 40, 50, 60, 70 };
                    var c3 = c1.SelectMany(x1 => c2
                        .Where(x2 => object.Equals(x1, x2 / 10))
                        .Select(x2 => x1 + x2));$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void LinqQuery2()
        => VerifyTypingSemicolon("""
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void Main(string[] args)
                {
                    List<int> c = new List<int> { 1, 2, 3, 4, 5, 7 };
                    var d = c
                        .Where(x => x == 4$$)
                        .Select(x => x + x);
                }
            }
            """, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void Main(string[] args)
                {
                    List<int> c = new List<int> { 1, 2, 3, 4, 5, 7 };
                    var d = c
                        .Where(x => x == 4);$$
                        .Select(x => x + x);
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void BinaryExpression()
        => VerifyTypingSemicolon("""
            class D
            {
                void M()
                {
                    int i = Foo(4$$) + 1
                }

                private int Foo(int v)
                {
                    return v;
                }
            }
            """, """
            class D
            {
                void M()
                {
                    int i = Foo(4);$$ + 1
                }

                private int Foo(int v)
                {
                    return v;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void BinaryExpression2()
        => VerifyTypingSemicolon("""
            class D
            {
                void M()
                {
                    int i = Foo(Foo(4$$) + 1) + 2
                }

                private int Foo(int v)
                {
                    return v;
                }
            }
            """, """
            class D
            {
                void M()
                {
                    int i = Foo(Foo(4) + 1);$$ + 2
                }

                private int Foo(int v)
                {
                    return v;
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void AsOperator()
        => VerifyTypingSemicolon("""
            class D
            {
                void M()
                {
                    string i = Foo(4$$) as string
                }

                object Foo(int v)
                {
                    return v.ToString();
                }
            }
            """, """
            class D
            {
                void M()
                {
                    string i = Foo(4);$$ as string
                }

                object Foo(int v)
                {
                    return v.ToString();
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void TernaryOperator()
        => VerifyTypingSemicolon("""
            class Query
            {
                void Main(string[] args)
                {
                    int j = 0;
                    int k = 0;
                    int i = j < k ? Foo(j$$) : Foo(3)
                }

                private int Foo(int j)
                {
                    return j;
                }
            """, """
            class Query
            {
                void Main(string[] args)
                {
                    int j = 0;
                    int k = 0;
                    int i = j < k ? Foo(j);$$ : Foo(3)
                }

                private int Foo(int j)
                {
                    return j;
                }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34983")]
    public void SemicolonInCharacterLiteral()
        => VerifyTypingSemicolon("""
            class D
            {
                void Main(string[]args)
                {
                    M('$$')
                }

                void M(char c)
                {
                }
            }
            """, """
            class D
            {
                void Main(string[]args)
                {
                    M(';$$')
                }

                void M(char c)
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35260")]
    public void IncompleteLambda()
        => VerifyTypingSemicolon("""
            using System;

            class C
            {
                public void Test()
                {
                    C c = new C();
                    c.M(z =>
                    {
                    return 0$$)
                    }

                private void M(Func<object, int> p) { }
            }
            """, """
            using System;

            class C
            {
                public void Test()
                {
                    C c = new C();
                    c.M(z =>
                    {
                    return 0;$$)
                    }

                private void M(Func<object, int> p) { }
            }
            """);

    internal override ICommandHandler GetCommandHandler(EditorTestWorkspace workspace)
        => workspace.ExportProvider.GetExportedValues<ICommandHandler>().OfType<CompleteStatementCommandHandler>().Single();

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/32337")]
    public void ArgumentList_MultipleCharsSelected()
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM([|x[0]|], x[1])");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_DelegateDeclaration()
        => VerifyTypingSemicolon("""
            class C
            {
                delegate void Del(string str$$)
            }
            """, """
            class C
            {
                delegate void Del(string str);$$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_DelegateDeclaration2()
        => VerifyNoSpecialSemicolonHandling("""
            class C
            {
                public delegate TResult Blah<in T, out TResult$$>(T arg)
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_DelegateDeclaration3()
        => VerifyTypingSemicolon("""
            class C
            {
                public delegate TResult Blah<in T, out TResult>(T arg$$)
            }
            """, """
            class C
            {
                public delegate TResult Blah<in T, out TResult>(T arg);$$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_MultilineDelegateDeclaration()
        => VerifyTypingSemicolon("""
            class C
            {
                delegate void Del(string str$$,
                    int i,
                    string str2)
            }
            """, """
            class C
            {
                delegate void Del(string str,
                    int i,
                    string str2);$$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_Constructor()
        => VerifyNoSpecialSemicolonHandling("""
            class D
            {
                public D($$)
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_Destructor()
        => VerifyNoSpecialSemicolonHandling("""
            class D
            {
                public D()
                {
                }

                ~D($$)
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/34051")]
    public void ParameterList_MethodDeclaration()
        => VerifyNoSpecialSemicolonHandling("""
            class D
            {
               void M($$)
                {
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54709")]
    public void YieldReturn()
        => VerifyTypingSemicolon("""
            class D
            {
                private static IEnumerable<int> M()
                {
                    yield return GetNumber($$)
                }
            }
            """, """
            class D
            {
                private static IEnumerable<int> M()
                {
                    yield return GetNumber();$$
                }
            }
            """);

    [WorkItem("https://github.com/dotnet/roslyn/issues/71933")]
    [WpfFact]
    public void InsideDisabledCode()
    {
        var code = CreateTestWithMethodCall("""

            Console.WriteLine(
            #if false
                // Comment$$
                "$$"$$
            #endif
            );

            """);

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/917499")]
    [WpfTheory]
    [InlineData("/$$* comments */")]
    [InlineData("/*$$ comments */")]
    [InlineData("/* comments $$*/")]
    [InlineData("/* comments *$$/")]
    [InlineData("3, /* comments$$ */")]
    [InlineData("/$$/ comments ")]
    [InlineData("//$$ comments ")]
    [InlineData("// comments $$")]
    public void InsideComments(string argument)
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(" + argument + ")");

        VerifyNoSpecialSemicolonHandling(code);
    }

    [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/917499")]
    [WpfTheory]
    [InlineData("$$/* comments */")]
    [InlineData("/* comments */$$")]
    [InlineData("3$$, /* comments */")]
    [InlineData("3, $$/* comments */")]
    [InlineData("""
        // comments 
        $$
        """)]
    public void NearComments(string argument)
    {
        var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(" + argument + ")");

        var expected = CreateTestWithMethodCall(
            @"var test = ClassC.MethodM(" + argument.Remove(argument.IndexOf("$$"), 2) + ");$$");

        VerifyTypingSemicolon(code, expected);
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/923157")]
    public void BrokenCode_ReturnIfCaretDoesNotMove()
        => VerifyNoSpecialSemicolonHandling("""
            class D
            {
              public Delegate Task<int> Handles(int num)$$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/37874")]
    public void TestWithSettingTurnedOff()
    {
        var code = """
            public class ClassC
            {
                private int xValue = 7;
                public int XValue
                {
                    get
                    {
                        return Math.Min(xValue$$, 1)
                    } 
                }
            }
            """;
        var expected = code.Replace("$$", ";$$");

        Verify(code, expected, ExecuteTest,
            setOptions: workspace =>
            {
                var globalOptions = workspace.GetService<IGlobalOptionService>();
                globalOptions.SetGlobalOption(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, false);
            });
    }

    [WpfFact]
    public void TestSwitchExpression()
        => VerifyTypingSemicolon("""
            public class Bar
            {
                public void Test(string myString)
                {
                    var a = myString switch
                    {
                        "Hello" => 1,
                        "World" => 2,
                        _ => 3$$
                    }
                }
            }
            """, """
            public class Bar
            {
                public void Test(string myString)
                {
                    var a = myString switch
                    {
                        "Hello" => 1,
                        "World" => 2,
                        _ => 3
                    };$$
                }
            }
            """);

    [WpfFact]
    public void TestNotInBracesSwitchExpression()
        => VerifyNoSpecialSemicolonHandling("""
            public class Bar
            {
                public void Test(string myString)
                {
                    var a = myString switch
                    $${
                        "Hello" => 1,
                        "World" => 2,
                        _ => 3
                    }
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/70224")]
    public void TestNotBeforeKeywordInSwitchExpression()
        => VerifyNoSpecialSemicolonHandling("""
            public class Bar
            {
                public void Test(string myString)
                {
                    var a = myString$$ switch
                    {
                        "Hello" => 1,
                        "World" => 2,
                        _ => 3
                    }
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54213")]
    public void AfterNewInField1()
        => VerifyTypingSemicolon("""
            public class C
            {
                public List<int> list = new$$
            }
            """, """
            public class C
            {
                public List<int> list = new();$$
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54213")]
    public void AfterNewInField2()
        => VerifyTypingSemicolon("""
            public class C
            {
                List<int> list1 = new$$
                List<int> list2;
            }
            """, """
            public class C
            {
                List<int> list1 = new();$$
                List<int> list2;
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54213")]
    public void AfterNewInLocalDeclaration1()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    List<int> list = new$$
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    List<int> list = new();$$
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/54213")]
    public void AfterNewInLocalDeclaration2()
        => VerifyTypingSemicolon("""
            public class C
            {
                void M()
                {
                    List<int> list = new$$
                    List<int> list2;
                }
            }
            """, """
            public class C
            {
                void M()
                {
                    List<int> list = new();$$
                    List<int> list2;
                }
            }
            """);

    protected override EditorTestWorkspace CreateTestWorkspace(string code)
        => EditorTestWorkspace.CreateCSharp(code);
}
