// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.CompleteStatement;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CompleteStatement
{
    public class CSharpCompleteStatementCommandHandlerTests : AbstractCompleteStatementTests
    {
        private string CreateTestWithMethodCall(string code)
        {
            return
@"class C
    {
        static void Main(string[] args)
        {
            int x = 1;
            int y = 2;
            int[] a = { 1,2 }
            " + code + @"

            int z = 4;
        }
    }

    static class ClassC
    {
        internal static int MethodM(int a, int b)
            => a * b;
    }
}";
        }

        #region ArgumentListOfMethodInvocation

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x, y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x,$$ y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, $$y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation5()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y$$)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation6()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y)$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation7()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y""$$)");
            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y"");$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_MissingParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ""y"");$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_CommentsAfter()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y) //Comments");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$ //Comments");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_SemicolonAlreadyExists()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y);");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y);$$;");

            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34176, "https://github.com/dotnet/roslyn/pull/34177")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_StringAsMethodArgument()
        {
            var code = CreateTestWithMethodCall(@"var test = Console.WriteLine( $$""Test"")");

            var expected = CreateTestWithMethodCall(@"var test = Console.WriteLine( ""Test"");$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34176, "https://github.com/dotnet/roslyn/pull/34177")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_StringAsMethodArgument2()
        {
            var code = CreateTestWithMethodCall(@"var test = Console.WriteLine( ""Test""$$ )");

            var expected = CreateTestWithMethodCall(@"var test = Console.WriteLine( ""Test"" );$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_MultiLine()
        {
            var code = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
    x$$, 
    y)");

            var expected = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
    x, 
    y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_MultiLine3()
        {
            var code = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
    x$$, 
    y
    )");

            var expected = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
    x, 
    y
    );$$");

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        #region ArgumentListOfNestedMethodInvocation

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x, y.ToString())");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$, y.ToString())");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, $$y.ToString())");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToS$$tring())");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation5()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString$$())");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation6()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString($$))");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation7()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString()$$)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation8()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString())$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation9()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ClassC.MethodM(4,ClassC.MethodM(5,ClassC.MethodM(6,7$$))))");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, ClassC.MethodM(4,ClassC.MethodM(5,ClassC.MethodM(6,7))));$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation8_SemicolonAlreadyExists()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString($$));");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y.ToString());$$;");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x.ToString(), y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString()$$, y)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition5()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y$$)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_DualPosition_SemicolonAlreadyExists()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y);");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString(), y);$$;");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MultiLine()
        {
            var code = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                y$$)");

            var expected = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                y);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MultiLine2()
        {
            var code = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                y$$
                )");

            var expected = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                y
                );$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MultiLine3()
        {
            var code = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                ""y""$$
                )");

            var expected = CreateTestWithMethodCall(@"
var test = ClassC.MethodM(
                x.ToString(), 
                ""y""
                );$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MissingBothParens()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MissingInnerParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MissingOuterParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y");

            VerifyNoSpecialSemicolonHandling(code);
        }

        #endregion

        #region ArgumentList_Array

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM($$x[0], x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x$$[0], x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[$$0], x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0$$], x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array5()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0]$$, x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array6()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0],$$ x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array7()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], $$x[1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array8()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[$$1])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array9()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1$$])");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array10()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]$$)");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array11()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1])$$");

            var expected = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]);$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array_MissingBoth()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array_MissingOuter()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array_MissingInner()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1)$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        #endregion

        #region FieldInitializer

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void FieldInitializer_NoParens()
        {
            var code =
@"
class C
{
    int i = 4$$
    int j = 5;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void FieldInitializer2()
        {
            var code =
@"
class C
{
    int i = Min(2$$,3)
    int j = 5;
";

            var expected =
@"
class C
{
    int i = Min(2,3);$$
    int j = 5;
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void FieldInitializer2b_MissingParen()
        {
            var code =
@"
class C
{
    int i = Min(2$$,3
    int j = 5;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void FieldInitializer3()
        {
            var code =
@"
class C
{
    int i = Min(Max(4,5$$),3)
    int j = 5;
";

            var expected =
@"
class C
{
    int i = Min(Max(4,5),3);$$
    int j = 5;
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void FieldInitializer3b_MissingInner()
        {
            var code =
@"
class C
{
    int i = Min(Max(4,5$$,3)
    int j = 5;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        #endregion

        #region ForLoop

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopSingleInitializer1()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for (int i = 0$$ )
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopSingleInitializer2()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for (int i = 0$$ i < 5; i++)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopSingleInitializer3()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for (int i = 0$$; i < 3; i = i + 1)
       {
            x = x * 3;
        }
        System.Console.Write(""{0}"", x);
    }
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopSingleInitializer_MissingParen()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for (int i = 0$$
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNoStatements()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ($$
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNoStatements2()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( $$
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNoStatements3()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( ; $$
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNoStatements4()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( ; ;$$
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNoStatements5()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( $$ ;)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer1()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( $$int i = 0, int j = 0)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer2()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int$$ i = 0, int j = 0)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer3()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int i$$ = 0, int j = 0)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer4()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int i = 0, $$int j = 0)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer5()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int i = 0, int j =$$ 0)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer6()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int i = 0, int j = 0$$)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopMultistatementInitializer7()
        {
            var code =
@"
class C
{
    static void Main()
    {
        for ( int i = 0, int j = 0$$)
        int j;
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNewInInitializer1()
        {
            var code =
@"
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
";

            var expected =
@"
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
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNewInInitializer_MissingOneParen()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopNewInInitializer2_MissingBothParens()
        {
            // only adding one closing paren
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopDeclaration()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""$$) i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd"");$$ i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopDeclaration2()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""$$), j=1 i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""), j=1;$$ i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopDeclaration3()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""$$); i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd"");$$; i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopDeclaration4()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""$$), j=1; i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""), j=1;$$; i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopDeclaration_MissingParen()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""$$ i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd"";$$ i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializers()
        {
            // Semicolon location is incorrect https://github.com/dotnet/roslyn/issues/32250
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd""$$) i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd"") i < 10;$$; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializers2()
        {
            // Semicolon location is incorrect https://github.com/dotnet/roslyn/issues/32250
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        int j;
        for (i = s.IndexOf(""bcd""$$), j=1 i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        int j;
        for (i = s.IndexOf(""bcd""), j=1 i < 10;$$; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializers3()
        {
            // Semicolon location is incorrect https://github.com/dotnet/roslyn/issues/32250
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd""$$); i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd"");$$; i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializers4()
        {
            // Semicolon location is incorrect https://github.com/dotnet/roslyn/issues/32250
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        int j;
        for (i = s.IndexOf(""bcd""$$), j=1; i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        int j;
        for (i = s.IndexOf(""bcd""), j=1;$$; i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializers_MissingParen()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd""$$ i < 10; i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        int i;
        for (i = s.IndexOf(""bcd"";$$ i < 10; i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopCondition()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < s.IndexOf(""x""$$) i++)
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < s.IndexOf(""x"");$$ i++)
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopConditionIsNull()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; $$ ; i++)
        {
            Console.WriteLine(""test"");
        }
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopConditionIsNull2()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = Math.Min(3,4$$);  ; i++)
        {
            Console.WriteLine(""test"");
        }
";
            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = Math.Min(3,4);$$;  ; i++)
        {
            Console.WriteLine(""test"");
        }
";
            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopIncrement()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < s.IndexOf(""x""); i = i.IndexOf(""x""$$))
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < s.IndexOf(""x""); i = i.IndexOf(""x"";$$))
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopBody()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < 10; i++)
        {
            i.ToString($$)
        }
";

            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        string s = ""abcdefghij"";
        for (int i = s.IndexOf(""bcd""); i < 10; i++)
        {
            i.ToString();$$
        }
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopObjectInitializer_MissingParen()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        for (Goo f = new Goo { i = 0, s = ""abc""$$ }
    }
}
public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopObjectInitializer()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        for (Goo f = new Goo { i = 0, s = ""abc""$$ } )
    }
}
public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopObjectInitializer_MissingBrace()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        for (Goo f = new Goo { i = 0, s = ""abc""$$
    }
}
public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        #endregion

        #region Indexer

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void Indexer()
        {
            var code =
@"
class SampleCollection<T>
{
    private T[] arr = new T[100];
    private int i;
    public int Property
    {
        get { return arr[i$$] }
        set { arr[i] = value; }
    }
}";
            var expected =
@"
class SampleCollection<T>
{
    private T[] arr = new T[100];
    private int i;
    public int Property
    {
        get { return arr[i];$$ }
        set { arr[i] = value; }
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void Indexer2()
        {
            var code =
@"
class test
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[1$$]
    }
}
";
            var expected =
@"
class test
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[1];$$
    }
}
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void Indexer3()
        {
            var code =
@"
class C
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[Math.Min(2,3$$)]
    }
}
";
            var expected =
@"
class C
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[Math.Min(2,3)];$$
    }
}
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void Indexer4()
        {
            var code =
@"
class C
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[Math.Min(2,3$$)
    }
}
";
            var expected =
@"
class C
{
    int[] array = { 1, 2, 3 };

    void M()
    {
        var i = array[Math.Min(2,3;$$)
    }
}
";

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        #region ObjectInitializer

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ObjectInitializer()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i = 0, s = ""abc"" }$$
    }
}

public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ObjectInitializer2()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i = 0, s = ""abc""$$ }
    }
}

public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ObjectInitializer3()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i = 0$$, s = ""abc"" }
    }
}

public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ObjectInitializer4()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i =$$ 0, s = ""abc"" }
    }
}

public class Goo
{
    public int i;
    public string s;
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ObjectInitializer_MissingBrace()
        {
            var code =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i = 0, s = ""abc""$$
    }
}

public class Goo
{
    public int i;
    public string s;
}
";
            var expected =
@"
class C
{
    static void Main(string[] args)
    {
        Goo f = new Goo { i = 0, s = ""abc"";$$
    }
}

public class Goo
{
    public int i;
    public string s;
}
";

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        #region Accessors

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors1()
        {
            var code = @"
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
}";

            var expected = @"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors2()
        {
            var code = @"
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
}";

            var expected = @"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors3()
        {
            var code = @"
public class Person
{
   private string firstName;
   private string lastName;
   
   public Person(string first, string last)
   {
      firstName = first;
      lastName = last;
   }

   public string Name => $""{firstName} {lastName}""$$   
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors4()
        {
            var code = @"
public class SaleItem
{
   string name;
   public string Name 
   {
      get => name;
      set => name = value$$
   }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors5()
        {
            var code = @"
public class SaleItem
{
   string name;
   public string Name 
   {
      get => name$$
      set => name = value;
   }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors6()
        {
            var code = @"
public class SaleItem
{
   string name;
   public string Name 
   {
      get => name.ToUpper($$)
      set => name = value;
   }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void PropertyAccessors7()
        {
            var code = @"
public class SaleItem
{
   public string Name 
   { get$$ set; }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        #endregion

        #region ParenthesizeExpression

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Assignment1()
        {
            var code = @"
public class Class1
{
    void M()
    {
        int i = (6*5$$)
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        int i = (6*5);$$
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Assignment2()
        {
            var code = @"
public class Class1
{
    void M()
    {
        int i = (6*Math.Min(4,5$$))
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        int i = (6*Math.Min(4,5));$$
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Assignment3()
        {
            var code = @"
public class Class1
{
    void M()
    {
        int[] array = { 2, 3, 4 };
        int i = (6*array[2$$])
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        int[] array = { 2, 3, 4 };
        int i = (6*array[2]);$$
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_ForLoop()
        {
            var code = @"
public class Class1
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            int j = (i+i$$)
        }
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        for (int i = 0; i < 10; i++)
        {
            int j = (i+i);$$
        }
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_ForLoop2()
        {
            var code = @"
public class Class1
{
    void M()
    {
        for (int i = ((3+2)*4$$); i < 10; i++)
        {
            int j = (i+i);
        }
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        for (int i = ((3+2)*4);$$; i < 10; i++)
        {
            int j = (i+i);
        }
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_ForLoop3()
        {
            var code = @"
public class Class1
{
    void M()
    {
        for (int i = 0; i < ((3+2)*4$$); i++)
        {
            int j = (i+i);
        }
    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        for (int i = 0; i < ((3+2)*4);$$; i++)
        {
            int j = (i+i);
        }
    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_ForEach()
        {
            var code = @"
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
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_GoTo2()
        {
            var code =
@"
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
";

            var expected =
@"
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
";
            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Switch()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Switch2()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_Switch3()
        {
            var code =
@"
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
";
            var expected =
@"
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
";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_While()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_While2()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParenthesizedExpression_While3()
        {
            var code =
@"
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
";
            var expected =
@"
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
";

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ThrowStatement_MissingBoth()
        {
            var code = @"
public class Class1
{
    void M()
    {
        string s = ""Test"";
        throw new Exception(s.ToUpper($$

    }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ThrowStatement()
        {
            var code = @"
public class Class1
{
    void M()
    {
        string s = ""Test"";
        throw new Exception(s.ToUpper($$))

    }
}";

            var expected = @"
public class Class1
{
    void M()
    {
        string s = ""Test"";
        throw new Exception(s.ToUpper());$$

    }
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonBeforeClassDeclaration()
        {
            var code =
@"$$
class C
{
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontCompleteStatment_DocComments()
        {
            var code =
@"
/// Testing $$
class C
{
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_FormatString()
        {
            var code =
@"
class C
{
    void Main()
    {
        Console.WriteLine(String.Format(""{0:##;(##)$$**Zero**}"", 0));
    }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_EmptyStatement()
        {
            var code =
@"
class C
{
    void Main()
    {
        ;$$
    }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_EmptyStatement2()
        {
            var code =
@"
class C
{
    void Main()
    {
        ; $$
    }
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile()
        {
            var code =
@"
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
}";
            var expected =
 @"
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
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile2()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile3()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile4()
        {
            var code =
@"
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
}";

            var expected =
@"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(35260, "https://github.com/dotnet/roslyn/issues/35260")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile5()
        {
            var code =
@"
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
}";

            var expected =
@"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(35260, "https://github.com/dotnet/roslyn/issues/35260")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile6()
        {
            var code =
@"
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
}";

            var expected =
@"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DoWhile_MissingParen()
        {
            var code =
@"
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
}";

            var expected =
@"
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
}";

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Break()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Break2()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Break3()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Checked()
        {
            var code =
@"
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
    }";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Unchecked()
        {
            var code =
@"
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
    }";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Fixed()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Continue()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Continue2()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Continue3()
        {
            var code =
@"
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
}";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_GoTo()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_IfStatement()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Labeled()
        {
            var code =
@"
class Program
{
    static void Main()
    {
        if (true)
            goto labeled;
        labeled$$: return;
    }
}
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_IfStatement2()
        {
            var code =
@"
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
";

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = $$ClassC.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = C$$lassC.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = Class$$C.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC$$.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.Meth$$odM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.$$MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM$$(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonBeforeEquals()
        {
            var code = CreateTestWithMethodCall(@"var test $$= ClassC.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonAfterEquals()
        {
            var code = CreateTestWithMethodCall(@"var test =$$ ClassC.MethodM(x,y)");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_String()
        {
            var code = CreateTestWithMethodCall(@"var s=""Test $$Test""");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_String2()
        {
            var code = CreateTestWithMethodCall(@"var s=""Test Test$$""");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_String3()
        {
            var code = CreateTestWithMethodCall(@"var s=""Test Test""$$");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34176, "https://github.com/dotnet/roslyn/issues/34176")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_VerbatimStringAsMethodArgument_EndOfLine_NotEndOfString()
        {
            var code = @"
            var code = Foo(@""$$
"") ;
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34176, "https://github.com/dotnet/roslyn/issues/34176")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_VerbatimStringAsMethodArgument_EndOfString_NotEndOfLine()
        {

            var code = @"
            var code = Foo(@""  $$"" //comments
);
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_InterpolatedString()
        {
            var code = CreateTestWithMethodCall(@"var s=$""{obj.ToString($$)}""");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute()
        {
            var code = @"
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
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute2()
        {
            var code = @"
[assembly: System.Reflection.AssemblyVersionAttribute(null$$)]
class Program
{
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute3()
        {
            var code = @"
using System.Runtime.CompilerServices;
using System;

class DummyAttribute : Attribute
{
    public DummyAttribute([CallerMemberName$$] string callerName = """")
    {
        Console.WriteLine(""name: "" + callerName);
    }
}

class A
{
    [Dummy]
    public void MyMethod() {
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute4()
        {
            var code = @"
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

[Mark(a: true, b: new object[$$] { ""Hello"", ""World"" })]
static class Program
{
    public static void Main()
    {
        var attr = typeof(Program).GetCustomAttribute<MarkAttribute>();
        Console.Write($""B.Length={attr.B.Length}, B[0]={attr.B[0]}, B[1]={attr.B[1]}"");
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute5()
        {
            var code = @"
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

[Mark(a: true, b: new object[] { ""Hello"", ""World""$$ })]
static class Program
{
    public static void Main()
    {
        var attr = typeof(Program).GetCustomAttribute<MarkAttribute>();
        Console.Write($""B.Length={attr.B.Length}, B[0]={attr.B[0]}, B[1]={attr.B[1]}"");
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Attribute6()
        {
            var code = @"
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
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Using()
        {
            var code = @"
using System.Linq$$
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Using2()
        {
            var code = @"
using System.Linq$$;
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_Using3()
        {
            var code = @"
using System.$$Linq
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(33851, "https://github.com/dotnet/roslyn/issues/33851")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void AtEndOfLineOutsideParens()
        {
            var code = @"
public class Class1
{
    void M()
    {
        string s = ""Test"";
        string t = s.Replace(""T"", ""t"")$$
            .Trim();

    }
}
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(33851, "https://github.com/dotnet/roslyn/issues/33851")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void OutsideParensBeforeSpaceDot()
        {
            var code = @"
public class Class1
{
    void M()
    {
        string s = ""Test"";
        string t = s.Replace(""T"", ""t"")$$ .Trim();

    }
}
";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BeforeAttribute()
        {
            var code = @"
public class C
{
private const string s = 
        @""test""$$

    [Fact]
    public void M()
            {
            }
        }";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ElementBindingExpression()
        {
            var code = @"
class C
{
    void M()
    {
        var data = new int[3];
        var value = data?[0$$]
    }
}";
            var expected = @"
class C
{
    void M()
    {
        var data = new int[3];
        var value = data?[0];$$
    }
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BeforeElementBindingExpression()
        {
            var code = @"
class C
{
    void M()
    {
        var data = new int[3];
        var value = data?$$[0]
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }


        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void AfterElementBindingExpression()
        {
            var code = @"
class C
{
    void M()
    {
        var data = new int[3];
        var value = data?[0]$$
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ImplicitElementAccessSyntax()
        {
            var code = @"
class C
{
    void M()
    {
        var d = new Dictionary<int, int>
        {
            [1$$] = 4,
        }
    }
}";
            var expected = @"
class C
{
    void M()
    {
        var d = new Dictionary<int, int>
        {
            [1];$$ = 4,
        }
    }
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BeforeImplicitElementAccessSyntax()
        {
            var code = @"
class C
{
    void M()
    {
        var d = new Dictionary<int, int>
        {
            $$[1] = 4,
        }
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34666, "https://github.com/dotnet/roslyn/issues/34666")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void AfterImplicitElementAccessSyntax()
        {
            var code = @"
class C
{
    void M()
    {
        var d = new Dictionary<int, int>
        {
            [1]$$ = 4,
        }
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void AttributeParsedAsElementAccessExpression()
        {
            var code = @"
using System;
internal class TestMethodAttribute : Attribute
{
    readonly int i = Foo(3,4$$)

    [Test]
}";
            var expected = @"
using System;
internal class TestMethodAttribute : Attribute
{
    readonly int i = Foo(3,4);$$

    [Test]
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void MemberAccessOffOfMethod()
        {
            var code = @"
class Program
{
    static void Main(string[] args)
    {
        var s = ""Hello"";
        var t = s.ToLower($$).Substring(1);
    }
}";
            var expected = @"
class Program
{
    static void Main(string[] args)
    {
        var s = ""Hello"";
        var t = s.ToLower();$$.Substring(1);
    }
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void LinqQuery()
        {
            var code = @"
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
}";
            var expected = @"
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
            .Select(x2 => x1 + x2));$$;
    }
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void LinqQuery2()
        {
            var code = @"
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
}";
            var expected = @"
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
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BinaryExpression()
        {
            var code = @"
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
}";
            var expected = @"
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
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BinaryExpression2()
        {
            var code = @"
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
}";
            var expected = @"
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
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void AsOperator()
        {
            var code = @"
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
}";
            var expected = @"
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
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void TernaryOperator()
        {
            var code = @"
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
";
            var expected = @"
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
";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34983, "https://github.com/dotnet/roslyn/issues/34983")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void SemicolonInCharacterLiteral()
        {
            var code = @"
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
";
            var expected = @"
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
";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(35260, "https://github.com/dotnet/roslyn/issues/35260")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void IncompleteLambda()
        {
            var code = @"
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
";
            var expected = @"
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
";
            VerifyTypingSemicolon(code, expected);
        }

        internal override VSCommanding.ICommandHandler GetCommandHandler(TestWorkspace workspace)
        {
            return workspace.ExportProvider.GetExportedValues<VSCommanding.ICommandHandler>().OfType<CompleteStatementCommandHandler>().Single();
        }

        [WorkItem(32337, "https://github.com/dotnet/roslyn/issues/32337")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_MultipleCharsSelected()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM([|x[0]|], x[1])");

            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_DelegateDeclaration()
        {
            var code = @"
class C
{
    delegate void Del(string str$$)
}";
            var expected = @"
class C
{
    delegate void Del(string str);$$
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_DelegateDeclaration2()
        {
            var code = @"
class C
{
    public delegate TResult Blah<in T, out TResult$$>(T arg)
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_DelegateDeclaration3()
        {
            var code = @"
class C
{
    public delegate TResult Blah<in T, out TResult>(T arg$$)
}";
            var expected = @"
class C
{
    public delegate TResult Blah<in T, out TResult>(T arg);$$
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_MultilineDelegateDeclaration()
        {
            var code = @"
class C
{
    delegate void Del(string str$$,
        int i,
        string str2)
}";
            var expected = @"
class C
{
    delegate void Del(string str,
        int i,
        string str2);$$
}";
            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_Constructor()
        {
            var code = @"
class D
{
    public D($$)
    {
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_Destructor()
        {
            var code = @"
class D
{
    public D()
    {
    }

    ~D($$)
    {
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(34051, "https://github.com/dotnet/roslyn/issues/34051")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ParameterList_MethodDeclaration()
        {
            var code = @"
class D
{
   void M($$)
    {
    }
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        [WorkItem(917499, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/917499")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
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

        [WorkItem(917499, "https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/917499")]
        [WpfTheory, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        [InlineData("$$/* comments */")]
        [InlineData("/* comments */$$")]
        [InlineData("3$$, /* comments */")]
        [InlineData("3, $$/* comments */")]
        [InlineData("// comments \r\n$$")]
        public void NearComments(string argument)
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(" + argument + ")");

            var expected = CreateTestWithMethodCall(
                @"var test = ClassC.MethodM(" + argument.Remove(argument.IndexOf("$$"), 2) + ");$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WorkItem(923157, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/923157")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void BrokenCode_ReturnIfCaretDoesNotMove()
        {
            var code = @"
class D
{
  public Delegate Task<int> Handles(int num)$$
}";
            VerifyNoSpecialSemicolonHandling(code);
        }

        protected override TestWorkspace CreateTestWorkspace(string code)
            => TestWorkspace.CreateCSharp(code);
    }
}
