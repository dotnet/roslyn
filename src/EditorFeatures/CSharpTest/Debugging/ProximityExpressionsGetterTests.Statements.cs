// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Debugging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Debugging;

[Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
public partial class ProximityExpressionsGetterTests
{
    [Fact]
    public void TestAtStartOfStatement_0()
    {
        //// Line 11

        ////         private static string ConvertToString(ExpressionSyntax expression)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 347, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_1()
    {
        //// Line 13

        ////             // TODO(cyrusn): Should we strip out comments?
        ////             return expression.GetFullText();
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 422, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_2()
    {
        //// Line 17

        ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, List<string> terms)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 592, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_3()
    {
        //// Line 19

        ////             // Check here rather than at all the call sites...
        ////             if (expression == null)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 671, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "position", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_4()
    {
        //// Line 20

        ////             if (expression == null)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 708, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_5()
    {
        //// Line 21

        ////             {
        ////                 return;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 727, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_6()
    {
        //// Line 26

        ////             // of this expression as a whole.
        ////             var expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 908, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expression", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_7()
    {
        //// Line 27

        ////             var expressionType = ExpressionType.Invalid;
        ////             CollectExpressionTerms(position, expression, terms, ref expressionType);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 966, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectExpressionTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_8()
    {
        //// Line 29

        //// 
        ////             if ((expressionType & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1054, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "expression", "terms", "expressionType", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_9()
    {
        //// Line 30

        ////             if ((expressionType & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1144, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_10()
    {
        //// Line 33

        ////                 // term table
        ////                 terms.Add(ConvertToString(expression));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1282, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "terms", "expressionType", "expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_11()
    {
        //// Line 38

        ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1510, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_12()
    {
        //// Line 40

        ////             // Check here rather than at all the call sites...
        ////             if (expression == null)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1589, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "position", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_13()
    {
        //// Line 41

        ////             if (expression == null)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1626, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_14()
    {
        //// Line 42

        ////             {
        ////                 return;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1645, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_15()
    {
        //// Line 45

        //// 
        ////             switch (expression.Kind)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 1683, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_16()
    {
        //// Line 52

        ////                     // want "this" showing up in the auto's window twice.
        ////                     expressionType = ExpressionType.ValidExpression;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 2105, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_17()
    {
        //// Line 53

        ////                     expressionType = ExpressionType.ValidExpression;
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 2175, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_18()
    {
        //// Line 57

        ////                     // Name nodes are always valid terms
        ////                     expressionType = ExpressionType.ValidTerm;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 2313, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm", "expression", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_19()
    {
        //// Line 58

        ////                     expressionType = ExpressionType.ValidTerm;
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 2377, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_20()
    {
        //// Line 69

        ////                     // on their own).
        ////                     expressionType = ExpressionType.ValidExpression;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 2985, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_21()
    {
        //// Line 70

        ////                     expressionType = ExpressionType.ValidExpression;
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3055, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_22()
    {
        //// Line 76

        ////                     // extremely rare, so we allow for this since it's ok in the common case.
        ////                     CollectExpressionTerms(position, ((CastExpressionSyntax)expression).Expression, terms, ref expressionType);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3423, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "terms", "expressionType", "CollectExpressionTerms", "expression", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_23()
    {
        //// Line 77

        ////                     CollectExpressionTerms(position, ((CastExpressionSyntax)expression).Expression, terms, ref expressionType);
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3552, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "position", "terms", "expressionType", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_24()
    {
        //// Line 81

        ////                 case SyntaxKind.PointerMemberAccessExpression:
        ////                     CollectMemberAccessExpressionTerms(position, expression, terms, ref expressionType);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3704, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectMemberAccessExpressionTerms", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_25()
    {
        //// Line 82

        ////                     CollectMemberAccessExpressionTerms(position, expression, terms, ref expressionType);
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3810, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectMemberAccessExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_26()
    {
        //// Line 85

        ////                 case SyntaxKind.ObjectCreationExpression:
        ////                     CollectObjectCreationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 3900, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectObjectCreationExpressionTerms", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_27()
    {
        //// Line 86

        ////                     CollectObjectCreationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4008, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectObjectCreationExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_28()
    {
        //// Line 89

        ////                 case SyntaxKind.ArrayCreationExpression:
        ////                     CollectArrayCreationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4097, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectArrayCreationExpressionTerms", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_29()
    {
        //// Line 90

        ////                     CollectArrayCreationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4204, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectArrayCreationExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_30()
    {
        //// Line 93

        ////                 case SyntaxKind.InvocationExpression:
        ////                     CollectInvocationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4290, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectInvocationExpressionTerms", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_31()
    {
        //// Line 94

        ////                     CollectInvocationExpressionTerms(position, expression, terms, ref expressionType);
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4394, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectInvocationExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_32()
    {
        //// Line 100

        ////             // This is a valid expression if it doesn't have obvious side effects (i.e. ++, --)
        ////             if (expression is PrefixUnaryExpressionSyntax)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4583, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_33()
    {
        //// Line 101

        ////             if (expression is PrefixUnaryExpressionSyntax)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4643, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_34()
    {
        //// Line 102

        ////             {
        ////                 CollectPrefixUnaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4662, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPrefixUnaryExpressionTerms", "PrefixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_35()
    {
        //// Line 103

        ////                 CollectPrefixUnaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 return;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4763, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPrefixUnaryExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_36()
    {
        //// Line 106

        //// 
        ////             if (expression is PostfixUnaryExpressionSyntax)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4801, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "PostfixUnaryExpressionSyntax", "PrefixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_37()
    {
        //// Line 107

        ////             if (expression is PostfixUnaryExpressionSyntax)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4862, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "PostfixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_38()
    {
        //// Line 108

        ////             {
        ////                 CollectPostfixUnaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4881, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPostfixUnaryExpressionTerms", "PostfixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_39()
    {
        //// Line 109

        ////                 CollectPostfixUnaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 return;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 4983, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPostfixUnaryExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_40()
    {
        //// Line 112

        //// 
        ////             if (expression is BinaryExpressionSyntax)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5021, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "BinaryExpressionSyntax", "PostfixUnaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_41()
    {
        //// Line 113

        ////             if (expression is BinaryExpressionSyntax)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5076, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "BinaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_42()
    {
        //// Line 114

        ////             {
        ////                 CollectBinaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5095, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectBinaryExpressionTerms", "BinaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_43()
    {
        //// Line 115

        ////                 CollectBinaryExpressionTerms(position, expression, terms, ref expressionType);
        ////                 return;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5191, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectBinaryExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_44()
    {
        //// Line 118

        //// 
        ////             expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5229, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "expression", "BinaryExpressionSyntax" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_45()
    {
        //// Line 122

        ////         private static void CollectMemberAccessExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5455, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_46()
    {
        //// Line 123

        ////         {
        ////             var flags = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5470, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "flags", "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_47()
    {
        //// Line 128

        ////             // So, we don't bother collecting anything from the RHS...
        ////             var memberAccess = (MemberAccessExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5765, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_48()
    {
        //// Line 129

        ////             var memberAccess = (MemberAccessExpressionSyntax)expression;
        ////             CollectExpressionTerms(position, memberAccess.Expression, terms, ref flags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 5839, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "memberAccess", "memberAccess.Expression", "terms", "flags", "CollectExpressionTerms", "expression", "(MemberAccessExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_49()
    {
        //// Line 134

        ////             // add both 'a.b.c.d' and 'a.b.c', but not 'a.b' and 'a'.
        ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm &&
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6170, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "flags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_50()
    {
        //// Line 137

        ////                 !expression.IsParentKind(SyntaxKind.PointerMemberAccessExpression))
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6418, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_51()
    {
        //// Line 138

        ////             {
        ////                 terms.Add(ConvertToString(memberAccess.Expression));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6437, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_52()
    {
        //// Line 143

        ////             // expression, and its PARENT is not an invocation.
        ////             if ((flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression &&
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6666, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_53()
    {
        //// Line 145

        ////                 !expression.IsParentKind(SyntaxKind.InvocationExpression))
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6837, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_54()
    {
        //// Line 146

        ////             {
        ////                 expressionType = ExpressionType.ValidTerm;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6856, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "expressionType", "ExpressionType.ValidTerm", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_55()
    {
        //// Line 149

        ////             else
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6945, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_56()
    {
        //// Line 150

        ////             {
        ////                 expressionType = ExpressionType.ValidExpression;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 6964, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "expressionType", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_57()
    {
        //// Line 155

        ////         private static void CollectObjectCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7215, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_58()
    {
        //// Line 159

        ////             // the sub arguments are valid terms.
        ////             expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7451, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_59()
    {
        //// Line 161

        //// 
        ////             var objectionCreation = (ObjectCreationExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7507, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(ObjectCreationExpressionSyntax)expression", "ExpressionType", "expressionType", "ExpressionType.Invalid", "objectionCreation" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_60()
    {
        //// Line 162

        ////             var objectionCreation = (ObjectCreationExpressionSyntax)expression;
        ////             if (objectionCreation.ArgumentListOpt != null)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7588, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "objectionCreation", "objectionCreation.ArgumentListOpt", "expression", "(ObjectCreationExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_61()
    {
        //// Line 163

        ////             if (objectionCreation.ArgumentListOpt != null)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7648, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "objectionCreation", "objectionCreation.ArgumentListOpt" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_62()
    {
        //// Line 164

        ////             {
        ////                 var flags = ExpressionType.Invalid;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7667, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "objectionCreation", "objectionCreation.ArgumentListOpt", "flags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_63()
    {
        //// Line 165

        ////                 var flags = ExpressionType.Invalid;
        ////                 CollectArgumentTerms(position, objectionCreation.ArgumentList, terms, ref flags);
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7720, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_64()
    {
        //// Line 169

        ////                 // that can be used somewhere higher in the stack.
        ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 7975, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_65()
    {
        //// Line 170

        ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////                 {
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8060, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_66()
    {
        //// Line 171

        ////                 {
        ////                     expressionType = ExpressionType.ValidExpression;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8083, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "expressionType", "ExpressionType.ValidExpression", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_67()
    {
        //// Line 177

        ////         private static void CollectArrayCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8352, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_68()
    {
        //// Line 178

        ////         {
        ////             var validTerm = true;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8367, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "validTerm", "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_69()
    {
        //// Line 179

        ////             var validTerm = true;
        ////             var arrayCreation = (ArrayCreationExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8402, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(ArrayCreationExpressionSyntax)expression", "validTerm", "arrayCreation" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_70()
    {
        //// Line 181

        //// 
        ////             if (arrayCreation.InitializerOpt != null)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8480, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "arrayCreation", "arrayCreation.InitializerOpt", "expression", "(ArrayCreationExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_71()
    {
        //// Line 182

        ////             if (arrayCreation.InitializerOpt != null)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8535, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "arrayCreation", "arrayCreation.InitializerOpt" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_72()
    {
        //// Line 183

        ////             {
        ////                 var flags = ExpressionType.Invalid;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8554, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "arrayCreation", "arrayCreation.InitializerOpt", "flags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_73()
    {
        //// Line 184

        ////                 var flags = ExpressionType.Invalid;
        ////                 arrayCreation.Initializer.Expressions.Do(e => CollectExpressionTerms(position, e, terms, ref flags));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8607, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "arrayCreation.InitializerOpt.Expressions", "flags", "ExpressionType", "ExpressionType.Invalid" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_74()
    {
        //// Line 186

        //// 
        ////                 validTerm &= (flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8731, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "validTerm", "arrayCreation.InitializerOpt.Expressions" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_75()
    {
        //// Line 189

        //// 
        ////             if (validTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8838, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "validTerm", "arrayCreation", "arrayCreation.InitializerOpt", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_76()
    {
        //// Line 190

        ////             if (validTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8866, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "validTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_77()
    {
        //// Line 191

        ////             {
        ////                 expressionType = ExpressionType.ValidExpression;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8885, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "validTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_78()
    {
        //// Line 194

        ////             else
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8980, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "validTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_79()
    {
        //// Line 195

        ////             {
        ////                 expressionType = ExpressionType.Invalid;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 8999, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "validTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_80()
    {
        //// Line 200

        ////         private static void CollectInvocationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9238, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_81()
    {
        //// Line 203

        ////             // is invalid initially
        ////             expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9367, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_82()
    {
        //// Line 204

        ////             expressionType = ExpressionType.Invalid;
        ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9421, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "leftFlags", "rightFlags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_83()
    {
        //// Line 206

        //// 
        ////             var invocation = (InvocationExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9524, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(InvocationExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "invocation" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_84()
    {
        //// Line 207

        ////             var invocation = (InvocationExpressionSyntax)expression;
        ////             CollectExpressionTerms(position, invocation.Expression, terms, ref leftFlags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9594, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "invocation", "invocation.Expression", "terms", "leftFlags", "CollectExpressionTerms", "expression", "(InvocationExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_85()
    {
        //// Line 208

        ////             CollectExpressionTerms(position, invocation.Expression, terms, ref leftFlags);
        ////             CollectArgumentTerms(position, invocation.ArgumentList, terms, ref rightFlags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9686, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "invocation", "invocation.ArgumentList", "terms", "rightFlags", "CollectArgumentTerms", "invocation.Expression", "leftFlags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_86()
    {
        //// Line 210

        //// 
        ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9781, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "invocation", "invocation.ArgumentList", "terms", "rightFlags", "CollectArgumentTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_87()
    {
        //// Line 211

        ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9866, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_88()
    {
        //// Line 212

        ////             {
        ////                 terms.Add(ConvertToString(invocation.Expression));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 9885, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "terms", "invocation", "invocation.Expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_89()
    {
        //// Line 216

        ////             // We're valid if both children are...
        ////             expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10018, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "ExpressionType.ValidTerm", "terms", "invocation", "invocation.Expression", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_90()
    {
        //// Line 220

        ////         private static void CollectPrefixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10278, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_91()
    {
        //// Line 221

        ////         {
        ////             expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10293, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_92()
    {
        //// Line 222

        ////             expressionType = ExpressionType.Invalid;
        ////             var flags = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10347, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "flags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_93()
    {
        //// Line 223

        ////             var flags = ExpressionType.Invalid;
        ////             var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10396, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(PrefixUnaryExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "prefixUnaryExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_94()
    {
        //// Line 226

        ////             // Ask our subexpression for terms
        ////             CollectExpressionTerms(position, prefixUnaryExpression.Operand, terms, ref flags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10528, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PrefixUnaryExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_95()
    {
        //// Line 229

        ////             // Is our expression a valid term?
        ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10674, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_96()
    {
        //// Line 230

        ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10755, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_97()
    {
        //// Line 231

        ////             {
        ////                 terms.Add(ConvertToString(prefixUnaryExpression.Operand));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10774, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_98()
    {
        //// Line 234

        //// 
        ////             if (expression.MatchesKind(SyntaxKind.LogicalNotExpression, SyntaxKind.BitwiseNotExpression, SyntaxKind.NegateExpression, SyntaxKind.PlusExpression))
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 10863, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression", "ExpressionType", "ExpressionType.ValidTerm", "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_99()
    {
        //// Line 235

        ////             if (expression.MatchesKind(SyntaxKind.LogicalNotExpression, SyntaxKind.BitwiseNotExpression, SyntaxKind.NegateExpression, SyntaxKind.PlusExpression))
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11026, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_100()
    {
        //// Line 237

        ////                 // We're a valid expression if our subexpression is...
        ////                 expressionType = flags & ExpressionType.ValidExpression;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11117, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_101()
    {
        //// Line 242

        ////         private static void CollectPostfixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11374, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_102()
    {
        //// Line 245

        ////             // effects, we never consider this an expression.
        ////             expressionType = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11539, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_103()
    {
        //// Line 247

        //// 
        ////             var flags = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11595, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "flags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_104()
    {
        //// Line 248

        ////             var flags = ExpressionType.Invalid;
        ////             var postfixUnaryExpression = (PostfixUnaryExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11644, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(PostfixUnaryExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "postfixUnaryExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_105()
    {
        //// Line 251

        ////             // Ask our subexpression for terms
        ////             CollectExpressionTerms(position, postfixUnaryExpression.Operand, terms, ref flags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11778, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PostfixUnaryExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_106()
    {
        //// Line 254

        ////             // Is our expression a valid term?
        ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 11925, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_107()
    {
        //// Line 255

        ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12006, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_108()
    {
        //// Line 256

        ////             {
        ////                 terms.Add(ConvertToString(postfixUnaryExpression.Operand));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12025, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "terms", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_109()
    {
        //// Line 261

        ////         private static void CollectBinaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12279, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_110()
    {
        //// Line 262

        ////         {
        ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12294, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "leftFlags", "rightFlags", "position", "expression", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_111()
    {
        //// Line 264

        //// 
        ////             var binaryExpression = (BinaryExpressionSyntax)expression;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12397, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "expression", "(BinaryExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "binaryExpression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_112()
    {
        //// Line 265

        ////             var binaryExpression = (BinaryExpressionSyntax)expression;
        ////             CollectExpressionTerms(position, binaryExpression.Left, terms, ref leftFlags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12469, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "binaryExpression", "binaryExpression.Left", "terms", "leftFlags", "CollectExpressionTerms", "expression", "(BinaryExpressionSyntax)expression" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_113()
    {
        //// Line 266

        ////             CollectExpressionTerms(position, binaryExpression.Left, terms, ref leftFlags);
        ////             CollectExpressionTerms(position, binaryExpression.Right, terms, ref rightFlags);
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12561, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "binaryExpression", "binaryExpression.Right", "terms", "rightFlags", "CollectExpressionTerms", "binaryExpression.Left", "leftFlags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_114()
    {
        //// Line 268

        //// 
        ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12657, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "binaryExpression", "binaryExpression.Right", "terms", "rightFlags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_115()
    {
        //// Line 269

        ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12742, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_116()
    {
        //// Line 270

        ////             {
        ////                 terms.Add(ConvertToString(binaryExpression.Left));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12761, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_117()
    {
        //// Line 273

        //// 
        ////             if ((rightFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12842, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_118()
    {
        //// Line 274

        ////             if ((rightFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12928, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "rightFlags", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_119()
    {
        //// Line 275

        ////             {
        ////                 terms.Add(ConvertToString(binaryExpression.Right));
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 12947, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "terms", "rightFlags", "binaryExpression", "binaryExpression.Right", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_120()
    {
        //// Line 281

        //// 
        ////             switch (binaryExpression.Kind)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 13202, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_121()
    {
        //// Line 305

        ////                     // We're valid if both children are...
        ////                     expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14452, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "binaryExpression", "binaryExpression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_122()
    {
        //// Line 306

        ////                     expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14549, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_123()
    {
        //// Line 309

        ////                 default:
        ////                     expressionType = ExpressionType.Invalid;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14606, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "binaryExpression", "binaryExpression.Kind" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_124()
    {
        //// Line 310

        ////                     expressionType = ExpressionType.Invalid;
        ////                     return;
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14668, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_125()
    {
        //// Line 315

        ////         private static void CollectArgumentTerms(int position, ArgumentListSyntax argumentList, IList<string> terms, ref ExpressionType expressionType)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14866, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "argumentList", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_126()
    {
        //// Line 316

        ////         {
        ////             var validExpr = true;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 14881, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "validExpr", "position", "argumentList", "terms", "expressionType" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_127()
    {
        //// Line 320

        ////             // arguments to a function call(or a list of array index expressions)
        ////             foreach (var arg in argumentList.Arguments)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15078, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments", "validExpr" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_128()
    {
        //// Line 321

        ////             foreach (var arg in argumentList.Arguments)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15135, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_129()
    {
        //// Line 322

        ////             {
        ////                 var flags = ExpressionType.Invalid;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15154, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "arg", "argumentList", "argumentList.Arguments", "flags" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_130()
    {
        //// Line 324

        //// 
        ////                 CollectExpressionTerms(position, arg.Expression, terms, ref flags);
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15209, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "arg", "arg.Expression", "terms", "flags", "CollectExpressionTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_131()
    {
        //// Line 325

        ////                 CollectExpressionTerms(position, arg.Expression, terms, ref flags);
        ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15294, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "arg", "arg.Expression", "terms", "flags", "CollectExpressionTerms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_132()
    {
        //// Line 326

        ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
        ////                 {
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15379, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_133()
    {
        //// Line 327

        ////                 {
        ////                     terms.Add(ConvertToString(arg.Expression));
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15402, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "terms", "arg", "arg.Expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_134()
    {
        //// Line 330

        //// 
        ////                 validExpr &= (flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression;
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15484, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "validExpr", "ExpressionType.ValidTerm", "terms", "arg", "arg.Expression", "ConvertToString" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_135()
    {
        //// Line 335

        ////             // the list elements are...
        ////             expressionType = validExpr ? ExpressionType.ValidExpression : 0;
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15722, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "flags", "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType", "arg", "argumentList", "argumentList.Arguments" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_136()
    {
        //// Line 339

        ////         private static void CollectVariableTerms(int position, SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, List<string> terms)
        ////         {
        ////         ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15952, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "declarators", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_137()
    {
        //// Line 340

        ////         {
        ////             foreach (var declarator in declarators)
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 15967, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "declarator", "declarators", "position", "terms" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_138()
    {
        //// Line 341

        ////             foreach (var declarator in declarators)
        ////             {
        ////             ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 16020, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "declarator", "declarators" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_139()
    {
        //// Line 342

        ////             {
        ////                 if (declarator.InitializerOpt != null)
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 16039, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "declarator", "declarator.InitializerOpt", "declarators" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_140()
    {
        //// Line 343

        ////                 if (declarator.InitializerOpt != null)
        ////                 {
        ////                 ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 16095, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "declarator", "declarator.InitializerOpt" }, terms);
    }

    [Fact]
    public void TestAtStartOfStatement_141()
    {
        //// Line 344

        ////                 {
        ////                     CollectExpressionTerms(position, declarator.Initializer.Value, terms);
        ////                     ^
        var tree = GetTree();
        var terms = CSharpProximityExpressionsService.GetProximityExpressions(tree, 16118, cancellationToken: default);
        Assert.NotNull(terms);
        AssertEx.SetEqual(new[] { "position", "declarator.InitializerOpt", "declarator.InitializerOpt.Value", "terms", "CollectExpressionTerms", "declarator" }, terms);
    }
}
