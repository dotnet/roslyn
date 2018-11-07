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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfMethodInvocation_MissingParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x, y$$");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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
        public void ArgumentListOfNestedMethodInvocation_MissingBothParens()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MissingInnerParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$, y)");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentListOfNestedMethodInvocation_MissingOuterParen()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x.ToString($$), y");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array_MissingOuter()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1]$$");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ArgumentList_Array_MissingInner()
        {

            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM(x[0], x[1)$$");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void ForLoopInitializer()
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
        public void ForLoopInitializer_MissingParen()
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
        for (int i = s.IndexOf(""bcd""); i < s.IndexOf(""x""); i = i.IndexOf(""x"");$$)
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void Indexer()
        {
            var code =
@"
class SampleCollection<T>
{
    private T[] arr = new T[100];
    public T this[int i]
    {
        get { return arr[i]$$ }
        set { arr[i] = value; }
    }
}";

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

        #region Don't Complete

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonBeforeClassDeclaration()
        {
            var code =
@"$$
class C
{
}";

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
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

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = $$ClassC.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = C$$lassC.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = Class$$C.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_ClassNameOfMethodInvocation4()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC$$.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation1()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.Meth$$odM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation2()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.$$MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_MethodNameOfMethodInvocation3()
        {
            var code = CreateTestWithMethodCall(@"var test = ClassC.MethodM$$(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonBeforeEquals()
        {
            var code = CreateTestWithMethodCall(@"var test $$= ClassC.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CompleteStatement)]
        public void DontComplete_SemicolonAfterEquals()
        {
            var code = CreateTestWithMethodCall(@"var test =$$ ClassC.MethodM(x,y);");

            var expected = code.Replace("$$", ";$$");

            VerifyTypingSemicolon(code, expected);
        }

        #endregion

        internal override VSCommanding.ICommandHandler GetCommandHandler(TestWorkspace workspace)
        {
            return workspace.ExportProvider.GetExportedValues<VSCommanding.ICommandHandler>().OfType<CompleteStatementCommandHandler>().Single();
        }

        protected override TestWorkspace CreateTestWorkspace(string code)
            => TestWorkspace.CreateCSharp(code);
    }
}
