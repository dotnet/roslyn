// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Debugging
{
    public partial class ProximityExpressionsGetterTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_1()
        {
            //// using System.Collections.Generic;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 0);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_2()
        {
            //// using System.Collections.Generic;
            //// using Roslyn.Compilers.CSharp;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 35);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_3()
        {
            //// using Roslyn.Compilers.CSharp;
            //// using Roslyn.Services.CSharp.Utilities;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 67);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_4()
        {
            //// using Roslyn.Services.CSharp.Utilities;
            //// using Roslyn.Services.Extensions;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 108);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_5()
        {
            //// using Roslyn.Services.Extensions;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 152);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_6()
        {
            //// 
            //// namespace Roslyn.Services.CSharp.Debugging
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 154);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_7()
        {
            //// namespace Roslyn.Services.CSharp.Debugging
            //// {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 198);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_8()
        {
            //// {
            ////     internal partial class ProximityExpressionsGetter
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 201);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_9()
        {
            ////     internal partial class ProximityExpressionsGetter
            ////     {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 256);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_10()
        {
            ////     {
            ////         private static string ConvertToString(ExpressionSyntax expression)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 263);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_11()
        {
            ////         private static string ConvertToString(ExpressionSyntax expression)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 339);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_12()
        {
            ////         {
            ////             // TODO(cyrusn): Should we strip out comments?
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 350);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_13()
        {
            ////             // TODO(cyrusn): Should we strip out comments?
            ////             return expression.GetFullText();
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 410);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_14()
        {
            ////             return expression.GetFullText();
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 456);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_15()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 467);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_16()
        {
            //// 
            ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, List<string> terms)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 469);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_17()
        {
            ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, List<string> terms)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 584);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_18()
        {
            ////         {
            ////             // Check here rather than at all the call sites...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 595);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "position", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_19()
        {
            ////             // Check here rather than at all the call sites...
            ////             if (expression == null)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 659);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "position", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_20()
        {
            ////             if (expression == null)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 696);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_21()
        {
            ////             {
            ////                 return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 711);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_22()
        {
            ////                 return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 736);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_23()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 751);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_24()
        {
            //// 
            ////             // Collect terms from this expression, which returns flags indicating the validity
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 753);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_25()
        {
            ////             // Collect terms from this expression, which returns flags indicating the validity
            ////             // of this expression as a whole.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 849);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_26()
        {
            ////             // of this expression as a whole.
            ////             var expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 896);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_27()
        {
            ////             var expressionType = ExpressionType.Invalid;
            ////             CollectExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 954);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectExpressionTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_28()
        {
            ////             CollectExpressionTerms(position, expression, terms, ref expressionType);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1040);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "expression", "terms", "expressionType", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_29()
        {
            //// 
            ////             if ((expressionType & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1042);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "expression", "terms", "expressionType", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_30()
        {
            ////             if ((expressionType & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1132);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_31()
        {
            ////             {
            ////                 // If this expression identified itself as a valid term, add it to the
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1147);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expressionType", "terms", "expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_32()
        {
            ////                 // If this expression identified itself as a valid term, add it to the
            ////                 // term table
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1235);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expressionType", "terms", "expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_33()
        {
            ////                 // term table
            ////                 terms.Add(ConvertToString(expression));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1266);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expressionType", "terms", "expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_34()
        {
            ////                 terms.Add(ConvertToString(expression));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1323);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_35()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1338);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm", "terms", "expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_36()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1349);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_37()
        {
            //// 
            ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1351);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_38()
        {
            ////         private static void CollectExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1502);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_39()
        {
            ////         {
            ////             // Check here rather than at all the call sites...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1513);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "position", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_40()
        {
            ////             // Check here rather than at all the call sites...
            ////             if (expression == null)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1577);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "position", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_41()
        {
            ////             if (expression == null)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1614);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_42()
        {
            ////             {
            ////                 return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1629);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_43()
        {
            ////                 return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1654);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_44()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1669);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_45()
        {
            //// 
            ////             switch (expression.Kind)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1671);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_46()
        {
            ////             switch (expression.Kind)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1709);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_47()
        {
            ////             {
            ////                 case SyntaxKind.ThisExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1724);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_48()
        {
            ////                 case SyntaxKind.ThisExpression:
            ////                 case SyntaxKind.BaseExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1773);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_49()
        {
            ////                 case SyntaxKind.BaseExpression:
            ////                     // an op term is ok if it's a "this" or "base" op it allows us to see
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1822);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_50()
        {
            ////                     // an op term is ok if it's a "this" or "base" op it allows us to see
            ////                     // "this.goo" in the autos window note: it's not a VALIDTERM since we don't
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 1913);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_51()
        {
            ////                     // "this.goo" in the autos window note: it's not a VALIDTERM since we don't
            ////                     // want "this" showing up in the auto's window twice.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2010);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_52()
        {
            ////                     // want "this" showing up in the auto's window twice.
            ////                     expressionType = ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2085);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_53()
        {
            ////                     expressionType = ExpressionType.ValidExpression;
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2155);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_54()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2184);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_55()
        {
            //// 
            ////                 case SyntaxKind.IdentifierName:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2186);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_56()
        {
            ////                 case SyntaxKind.IdentifierName:
            ////                     // Name nodes are always valid terms
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2235);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_57()
        {
            ////                     // Name nodes are always valid terms
            ////                     expressionType = ExpressionType.ValidTerm;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2293);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_58()
        {
            ////                     expressionType = ExpressionType.ValidTerm;
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2357);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_59()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2386);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_60()
        {
            //// 
            ////                 case SyntaxKind.CharacterLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2388);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_61()
        {
            ////                 case SyntaxKind.CharacterLiteralExpression:
            ////                 case SyntaxKind.FalseLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2449);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_62()
        {
            ////                 case SyntaxKind.FalseLiteralExpression:
            ////                 case SyntaxKind.NullLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2506);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_63()
        {
            ////                 case SyntaxKind.NullLiteralExpression:
            ////                 case SyntaxKind.NumericLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2562);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_64()
        {
            ////                 case SyntaxKind.NumericLiteralExpression:
            ////                 case SyntaxKind.StringLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2621);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_65()
        {
            ////                 case SyntaxKind.StringLiteralExpression:
            ////                 case SyntaxKind.TrueLiteralExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2679);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_66()
        {
            ////                 case SyntaxKind.TrueLiteralExpression:
            ////                     // Constants can make up a valid term, but we don't consider them valid
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2735);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_67()
        {
            ////                     // Constants can make up a valid term, but we don't consider them valid
            ////                     // terms themselves (since we don't want them to show up in the autos window
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2828);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_68()
        {
            ////                     // terms themselves (since we don't want them to show up in the autos window
            ////                     // on their own).
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2926);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_69()
        {
            ////                     // on their own).
            ////                     expressionType = ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 2965);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_70()
        {
            ////                     expressionType = ExpressionType.ValidExpression;
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3035);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_71()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3064);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_72()
        {
            //// 
            ////                 case SyntaxKind.CastExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3066);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_73()
        {
            ////                 case SyntaxKind.CastExpression:
            ////                     // For a cast, just add the nested expression.  Note: this is technically
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3115);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "terms", "expressionType", "CollectExpressionTerms", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_74()
        {
            ////                     // For a cast, just add the nested expression.  Note: this is technically
            ////                     // unsafe as the cast *may* have side effects.  However, in practice this is
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3210);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "terms", "expressionType", "CollectExpressionTerms", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_75()
        {
            ////                     // unsafe as the cast *may* have side effects.  However, in practice this is
            ////                     // extremely rare, so we allow for this since it's ok in the common case.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3308);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "terms", "expressionType", "CollectExpressionTerms", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_76()
        {
            ////                     // extremely rare, so we allow for this since it's ok in the common case.
            ////                     CollectExpressionTerms(position, ((CastExpressionSyntax)expression).Expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3403);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "terms", "expressionType", "CollectExpressionTerms", "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_77()
        {
            ////                     CollectExpressionTerms(position, ((CastExpressionSyntax)expression).Expression, terms, ref expressionType);
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3532);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_78()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3561);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_79()
        {
            //// 
            ////                 case SyntaxKind.MemberAccessExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3563);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_80()
        {
            ////                 case SyntaxKind.MemberAccessExpression:
            ////                 case SyntaxKind.PointerMemberAccessExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3620);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_81()
        {
            ////                 case SyntaxKind.PointerMemberAccessExpression:
            ////                     CollectMemberAccessExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3684);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectMemberAccessExpressionTerms", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_82()
        {
            ////                     CollectMemberAccessExpressionTerms(position, expression, terms, ref expressionType);
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3790);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectMemberAccessExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_83()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3819);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_84()
        {
            //// 
            ////                 case SyntaxKind.ObjectCreationExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3821);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_85()
        {
            ////                 case SyntaxKind.ObjectCreationExpression:
            ////                     CollectObjectCreationExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3880);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectObjectCreationExpressionTerms", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_86()
        {
            ////                     CollectObjectCreationExpressionTerms(position, expression, terms, ref expressionType);
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 3988);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectObjectCreationExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_87()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4017);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_88()
        {
            //// 
            ////                 case SyntaxKind.ArrayCreationExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4019);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_89()
        {
            ////                 case SyntaxKind.ArrayCreationExpression:
            ////                     CollectArrayCreationExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4077);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectArrayCreationExpressionTerms", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_90()
        {
            ////                     CollectArrayCreationExpressionTerms(position, expression, terms, ref expressionType);
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4184);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectArrayCreationExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_91()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4213);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_92()
        {
            //// 
            ////                 case SyntaxKind.InvocationExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4215);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_93()
        {
            ////                 case SyntaxKind.InvocationExpression:
            ////                     CollectInvocationExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4270);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectInvocationExpressionTerms", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_94()
        {
            ////                     CollectInvocationExpressionTerms(position, expression, terms, ref expressionType);
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4374);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectInvocationExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_95()
        {
            ////                     return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4403);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_96()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4418);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_97()
        {
            //// 
            ////             // +, -, ++, --, !, etc.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4420);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_98()
        {
            ////             // +, -, ++, --, !, etc.
            ////             //
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4458);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_99()
        {
            ////             //
            ////             // This is a valid expression if it doesn't have obvious side effects (i.e. ++, --)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4474);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_100()
        {
            ////             // This is a valid expression if it doesn't have obvious side effects (i.e. ++, --)
            ////             if (expression is PrefixUnaryExpressionSyntax)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4571);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax", "expression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_101()
        {
            ////             if (expression is PrefixUnaryExpressionSyntax)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4631);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PrefixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_102()
        {
            ////             {
            ////                 CollectPrefixUnaryExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4646);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPrefixUnaryExpressionTerms", "PrefixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_103()
        {
            ////                 CollectPrefixUnaryExpressionTerms(position, expression, terms, ref expressionType);
            ////                 return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4747);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPrefixUnaryExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_104()
        {
            ////                 return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4772);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_105()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4787);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PostfixUnaryExpressionSyntax", "PrefixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_106()
        {
            //// 
            ////             if (expression is PostfixUnaryExpressionSyntax)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4789);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PostfixUnaryExpressionSyntax", "PrefixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_107()
        {
            ////             if (expression is PostfixUnaryExpressionSyntax)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4850);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "PostfixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_108()
        {
            ////             {
            ////                 CollectPostfixUnaryExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4865);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPostfixUnaryExpressionTerms", "PostfixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_109()
        {
            ////                 CollectPostfixUnaryExpressionTerms(position, expression, terms, ref expressionType);
            ////                 return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4967);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectPostfixUnaryExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_110()
        {
            ////                 return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 4992);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_111()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5007);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "BinaryExpressionSyntax", "PostfixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_112()
        {
            //// 
            ////             if (expression is BinaryExpressionSyntax)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5009);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "BinaryExpressionSyntax", "PostfixUnaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_113()
        {
            ////             if (expression is BinaryExpressionSyntax)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5064);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "BinaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_114()
        {
            ////             {
            ////                 CollectBinaryExpressionTerms(position, expression, terms, ref expressionType);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5079);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectBinaryExpressionTerms", "BinaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_115()
        {
            ////                 CollectBinaryExpressionTerms(position, expression, terms, ref expressionType);
            ////                 return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5175);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType", "CollectBinaryExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_116()
        {
            ////                 return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5200);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_117()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5215);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "expression", "BinaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_118()
        {
            //// 
            ////             expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5217);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "expression", "BinaryExpressionSyntax" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_119()
        {
            ////             expressionType = ExpressionType.Invalid;
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5271);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_120()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5282);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_121()
        {
            //// 
            ////         private static void CollectMemberAccessExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5284);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_122()
        {
            ////         private static void CollectMemberAccessExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5447);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_123()
        {
            ////         {
            ////             var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5458);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "flags", "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_124()
        {
            ////             var flags = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5507);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_125()
        {
            //// 
            ////             // These operators always have a RHS of a name node, which we know would
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5509);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_126()
        {
            ////             // These operators always have a RHS of a name node, which we know would
            ////             // "claim" to be a valid term, but is not valid without the LHS present.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5595);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_127()
        {
            ////             // "claim" to be a valid term, but is not valid without the LHS present.
            ////             // So, we don't bother collecting anything from the RHS...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5681);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_128()
        {
            ////             // So, we don't bother collecting anything from the RHS...
            ////             var memberAccess = (MemberAccessExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5753);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(MemberAccessExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "memberAccess" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_129()
        {
            ////             var memberAccess = (MemberAccessExpressionSyntax)expression;
            ////             CollectExpressionTerms(position, memberAccess.Expression, terms, ref flags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5827);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "memberAccess", "memberAccess.Expression", "terms", "flags", "CollectExpressionTerms", "expression", "(MemberAccessExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_130()
        {
            ////             CollectExpressionTerms(position, memberAccess.Expression, terms, ref flags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5917);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_131()
        {
            //// 
            ////             // If the LHS says it's a valid term, then we add it ONLY if our PARENT
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 5919);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_132()
        {
            ////             // If the LHS says it's a valid term, then we add it ONLY if our PARENT
            ////             // is NOT another dot/arrow.  This allows the expression 'a.b.c.d' to
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6004);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_133()
        {
            ////             // is NOT another dot/arrow.  This allows the expression 'a.b.c.d' to
            ////             // add both 'a.b.c.d' and 'a.b.c', but not 'a.b' and 'a'.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6087);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_134()
        {
            ////             // add both 'a.b.c.d' and 'a.b.c', but not 'a.b' and 'a'.
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm &&
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6158);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_135()
        {
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm &&
            ////                 !expression.IsParentKind(SyntaxKind.MemberAccessExpression) &&
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6241);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_136()
        {
            ////                 !expression.IsParentKind(SyntaxKind.MemberAccessExpression) &&
            ////                 !expression.IsParentKind(SyntaxKind.PointerMemberAccessExpression))
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6321);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "position", "memberAccess", "memberAccess.Expression", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_137()
        {
            ////                 !expression.IsParentKind(SyntaxKind.PointerMemberAccessExpression))
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6406);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_138()
        {
            ////             {
            ////                 terms.Add(ConvertToString(memberAccess.Expression));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6421);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "memberAccess", "memberAccess.Expression", "ConvertToString", "ExpressionType", "flags", "ExpressionType.ValidTerm", "expression", "SyntaxKind", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_139()
        {
            ////                 terms.Add(ConvertToString(memberAccess.Expression));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6491);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_140()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6506);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_141()
        {
            //// 
            ////             // And this expression itself is a valid term if the LHS is a valid
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6508);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_142()
        {
            ////             // And this expression itself is a valid term if the LHS is a valid
            ////             // expression, and its PARENT is not an invocation.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6589);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_143()
        {
            ////             // expression, and its PARENT is not an invocation.
            ////             if ((flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression &&
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6654);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_144()
        {
            ////             if ((flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression &&
            ////                 !expression.IsParentKind(SyntaxKind.InvocationExpression))
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6749);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_145()
        {
            ////                 !expression.IsParentKind(SyntaxKind.InvocationExpression))
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6825);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_146()
        {
            ////             {
            ////                 expressionType = ExpressionType.ValidTerm;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6840);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "expressionType", "ExpressionType.ValidTerm", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_147()
        {
            ////                 expressionType = ExpressionType.ValidTerm;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6900);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_148()
        {
            ////             }
            ////             else
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6915);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "ExpressionType.ValidTerm", "SyntaxKind.MemberAccessExpression", "SyntaxKind.PointerMemberAccessExpression", "terms", "memberAccess", "memberAccess.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_149()
        {
            ////             else
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6933);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_150()
        {
            ////             {
            ////                 expressionType = ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 6948);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "flags", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_151()
        {
            ////                 expressionType = ExpressionType.ValidExpression;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7014);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_152()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7029);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expression", "SyntaxKind", "SyntaxKind.InvocationExpression", "expressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_153()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7040);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_154()
        {
            //// 
            ////         private static void CollectObjectCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7042);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_155()
        {
            ////         private static void CollectObjectCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7207);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_156()
        {
            ////         {
            ////             // Object creation can *definitely* cause side effects.  So we initially
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7218);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_157()
        {
            ////             // Object creation can *definitely* cause side effects.  So we initially
            ////             // mark this as something invalid.  We allow it as a valid expr if all
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7304);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_158()
        {
            ////             // mark this as something invalid.  We allow it as a valid expr if all
            ////             // the sub arguments are valid terms.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7388);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_159()
        {
            ////             // the sub arguments are valid terms.
            ////             expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7439);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_160()
        {
            ////             expressionType = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7493);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(ObjectCreationExpressionSyntax)expression", "ExpressionType", "expressionType", "ExpressionType.Invalid", "objectionCreation" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_161()
        {
            //// 
            ////             var objectionCreation = (ObjectCreationExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7495);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(ObjectCreationExpressionSyntax)expression", "ExpressionType", "expressionType", "ExpressionType.Invalid", "objectionCreation" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_162()
        {
            ////             var objectionCreation = (ObjectCreationExpressionSyntax)expression;
            ////             if (objectionCreation.ArgumentListOpt != null)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7576);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "objectionCreation", "objectionCreation.ArgumentListOpt", "expression", "(ObjectCreationExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_163()
        {
            ////             if (objectionCreation.ArgumentListOpt != null)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7636);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "objectionCreation", "objectionCreation.ArgumentListOpt" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_164()
        {
            ////             {
            ////                 var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7651);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "objectionCreation", "objectionCreation.ArgumentListOpt", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_165()
        {
            ////                 var flags = ExpressionType.Invalid;
            ////                 CollectArgumentTerms(position, objectionCreation.ArgumentList, terms, ref flags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7704);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_166()
        {
            ////                 CollectArgumentTerms(position, objectionCreation.ArgumentList, terms, ref flags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7806);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_167()
        {
            //// 
            ////                 // If all arguments are terms, then this is possibly a valid expr
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7808);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_168()
        {
            ////                 // If all arguments are terms, then this is possibly a valid expr
            ////                 // that can be used somewhere higher in the stack.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7891);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_169()
        {
            ////                 // that can be used somewhere higher in the stack.
            ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 7959);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "objectionCreation", "objectionCreation.ArgumentListOpt", "terms", "flags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_170()
        {
            ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////                 {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8044);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_171()
        {
            ////                 {
            ////                     expressionType = ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8063);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_172()
        {
            ////                     expressionType = ExpressionType.ValidExpression;
            ////                 }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8133);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_173()
        {
            ////                 }
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8152);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_174()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8167);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "objectionCreation", "objectionCreation.ArgumentListOpt", "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_175()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8178);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_176()
        {
            //// 
            ////         private static void CollectArrayCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8180);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_177()
        {
            ////         private static void CollectArrayCreationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8344);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_178()
        {
            ////         {
            ////             var validTerm = true;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8355);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm", "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_179()
        {
            ////             var validTerm = true;
            ////             var arrayCreation = (ArrayCreationExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8390);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(ArrayCreationExpressionSyntax)expression", "validTerm", "arrayCreation" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_180()
        {
            ////             var arrayCreation = (ArrayCreationExpressionSyntax)expression;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8466);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arrayCreation", "arrayCreation.InitializerOpt", "expression", "(ArrayCreationExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_181()
        {
            //// 
            ////             if (arrayCreation.InitializerOpt != null)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8468);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arrayCreation", "arrayCreation.InitializerOpt", "expression", "(ArrayCreationExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_182()
        {
            ////             if (arrayCreation.InitializerOpt != null)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8523);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arrayCreation", "arrayCreation.InitializerOpt" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_183()
        {
            ////             {
            ////                 var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8538);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "arrayCreation", "arrayCreation.InitializerOpt", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_184()
        {
            ////                 var flags = ExpressionType.Invalid;
            ////                 arrayCreation.Initializer.Expressions.Do(e => CollectExpressionTerms(position, e, terms, ref flags));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8591);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arrayCreation.InitializerOpt.Expressions", "flags", "ExpressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_185()
        {
            ////                 arrayCreation.Initializer.Expressions.Do(e => CollectExpressionTerms(position, e, terms, ref flags));
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8713);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "validTerm", "arrayCreation.InitializerOpt.Expressions" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_186()
        {
            //// 
            ////                 validTerm &= (flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8715);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "validTerm", "arrayCreation.InitializerOpt.Expressions" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_187()
        {
            ////                 validTerm &= (flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8809);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm", "validTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_188()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8824);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm", "arrayCreation", "arrayCreation.InitializerOpt", "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_189()
        {
            //// 
            ////             if (validTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8826);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm", "arrayCreation", "arrayCreation.InitializerOpt", "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_190()
        {
            ////             if (validTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8854);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_191()
        {
            ////             {
            ////                 expressionType = ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8869);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "validTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_192()
        {
            ////                 expressionType = ExpressionType.ValidExpression;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8935);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.ValidExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_193()
        {
            ////             }
            ////             else
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8950);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm", "arrayCreation", "arrayCreation.InitializerOpt", "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_194()
        {
            ////             else
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8968);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_195()
        {
            ////             {
            ////                 expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 8983);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "validTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_196()
        {
            ////                 expressionType = ExpressionType.Invalid;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9041);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_197()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9056);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validTerm", "ExpressionType", "expressionType", "ExpressionType.ValidExpression", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_198()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9067);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_199()
        {
            //// 
            ////         private static void CollectInvocationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9069);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_200()
        {
            ////         private static void CollectInvocationExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9230);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_201()
        {
            ////         {
            ////             // Invocations definitely have side effects.  So we assume this
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9241);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_202()
        {
            ////             // Invocations definitely have side effects.  So we assume this
            ////             // is invalid initially
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9318);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_203()
        {
            ////             // is invalid initially
            ////             expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9355);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_204()
        {
            ////             expressionType = ExpressionType.Invalid;
            ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9409);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "leftFlags", "rightFlags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_205()
        {
            ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9510);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(InvocationExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "invocation" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_206()
        {
            //// 
            ////             var invocation = (InvocationExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9512);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(InvocationExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "invocation" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_207()
        {
            ////             var invocation = (InvocationExpressionSyntax)expression;
            ////             CollectExpressionTerms(position, invocation.Expression, terms, ref leftFlags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9582);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "invocation", "invocation.Expression", "terms", "leftFlags", "CollectExpressionTerms", "expression", "(InvocationExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_208()
        {
            ////             CollectExpressionTerms(position, invocation.Expression, terms, ref leftFlags);
            ////             CollectArgumentTerms(position, invocation.ArgumentList, terms, ref rightFlags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9674);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "invocation", "invocation.ArgumentList", "terms", "rightFlags", "CollectArgumentTerms", "invocation.Expression", "leftFlags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_209()
        {
            ////             CollectArgumentTerms(position, invocation.ArgumentList, terms, ref rightFlags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9767);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "invocation", "invocation.ArgumentList", "terms", "rightFlags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_210()
        {
            //// 
            ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9769);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "invocation", "invocation.ArgumentList", "terms", "rightFlags", "CollectArgumentTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_211()
        {
            ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9854);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_212()
        {
            ////             {
            ////                 terms.Add(ConvertToString(invocation.Expression));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9869);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "invocation", "invocation.Expression", "ConvertToString", "ExpressionType", "leftFlags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_213()
        {
            ////                 terms.Add(ConvertToString(invocation.Expression));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9937);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "invocation", "invocation.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_214()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9952);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "rightFlags", "ExpressionType.ValidExpression", "expressionType", "ExpressionType.ValidTerm", "terms", "invocation", "invocation.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_215()
        {
            //// 
            ////             // We're valid if both children are...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 9954);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "ExpressionType.ValidTerm", "terms", "invocation", "invocation.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_216()
        {
            ////             // We're valid if both children are...
            ////             expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10006);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "ExpressionType.ValidTerm", "terms", "invocation", "invocation.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_217()
        {
            ////             expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10095);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_218()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10106);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_219()
        {
            //// 
            ////         private static void CollectPrefixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10108);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_220()
        {
            ////         private static void CollectPrefixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10270);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_221()
        {
            ////         {
            ////             expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10281);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_222()
        {
            ////             expressionType = ExpressionType.Invalid;
            ////             var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10335);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_223()
        {
            ////             var flags = ExpressionType.Invalid;
            ////             var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10384);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(PrefixUnaryExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "prefixUnaryExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_224()
        {
            ////             var prefixUnaryExpression = (PrefixUnaryExpressionSyntax)expression;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10466);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PrefixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_225()
        {
            //// 
            ////             // Ask our subexpression for terms
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10468);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PrefixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_226()
        {
            ////             // Ask our subexpression for terms
            ////             CollectExpressionTerms(position, prefixUnaryExpression.Operand, terms, ref flags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10516);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PrefixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_227()
        {
            ////             CollectExpressionTerms(position, prefixUnaryExpression.Operand, terms, ref flags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10612);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_228()
        {
            //// 
            ////             // Is our expression a valid term?
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10614);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_229()
        {
            ////             // Is our expression a valid term?
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10662);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_230()
        {
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10743);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_231()
        {
            ////             {
            ////                 terms.Add(ConvertToString(prefixUnaryExpression.Operand));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10758);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_232()
        {
            ////                 terms.Add(ConvertToString(prefixUnaryExpression.Operand));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10834);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_233()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10849);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression", "ExpressionType", "ExpressionType.ValidTerm", "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_234()
        {
            //// 
            ////             if (expression.MatchesKind(SyntaxKind.LogicalNotExpression, SyntaxKind.BitwiseNotExpression, SyntaxKind.NegateExpression, SyntaxKind.PlusExpression))
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 10851);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression", "ExpressionType", "ExpressionType.ValidTerm", "terms", "prefixUnaryExpression", "prefixUnaryExpression.Operand", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_235()
        {
            ////             if (expression.MatchesKind(SyntaxKind.LogicalNotExpression, SyntaxKind.BitwiseNotExpression, SyntaxKind.NegateExpression, SyntaxKind.PlusExpression))
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11014);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_236()
        {
            ////             {
            ////                 // We're a valid expression if our subexpression is...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11029);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expressionType", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_237()
        {
            ////                 // We're a valid expression if our subexpression is...
            ////                 expressionType = flags & ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11101);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expressionType", "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_238()
        {
            ////                 expressionType = flags & ExpressionType.ValidExpression;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11175);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidExpression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_239()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11190);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "SyntaxKind", "SyntaxKind.LogicalNotExpression", "SyntaxKind.BitwiseNotExpression", "SyntaxKind.NegateExpression", "SyntaxKind.PlusExpression", "ExpressionType", "flags", "ExpressionType.ValidExpression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_240()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11201);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_241()
        {
            //// 
            ////         private static void CollectPostfixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11203);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_242()
        {
            ////         private static void CollectPostfixUnaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11366);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_243()
        {
            ////         {
            ////             // ++ and -- are the only postfix operators.  Since they always have side
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11377);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_244()
        {
            ////             // ++ and -- are the only postfix operators.  Since they always have side
            ////             // effects, we never consider this an expression.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11464);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_245()
        {
            ////             // effects, we never consider this an expression.
            ////             expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11527);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "position", "expression", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_246()
        {
            ////             expressionType = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11581);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_247()
        {
            //// 
            ////             var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11583);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "expressionType", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_248()
        {
            ////             var flags = ExpressionType.Invalid;
            ////             var postfixUnaryExpression = (PostfixUnaryExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11632);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(PostfixUnaryExpressionSyntax)expression", "flags", "ExpressionType", "ExpressionType.Invalid", "postfixUnaryExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_249()
        {
            ////             var postfixUnaryExpression = (PostfixUnaryExpressionSyntax)expression;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11716);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PostfixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_250()
        {
            //// 
            ////             // Ask our subexpression for terms
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11718);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PostfixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_251()
        {
            ////             // Ask our subexpression for terms
            ////             CollectExpressionTerms(position, postfixUnaryExpression.Operand, terms, ref flags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11766);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms", "expression", "(PostfixUnaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_252()
        {
            ////             CollectExpressionTerms(position, postfixUnaryExpression.Operand, terms, ref flags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11863);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_253()
        {
            //// 
            ////             // Is our expression a valid term?
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11865);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_254()
        {
            ////             // Is our expression a valid term?
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11913);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_255()
        {
            ////             if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 11994);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "flags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_256()
        {
            ////             {
            ////                 terms.Add(ConvertToString(postfixUnaryExpression.Operand));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12009);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "terms", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_257()
        {
            ////                 terms.Add(ConvertToString(postfixUnaryExpression.Operand));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12086);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_258()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12101);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidTerm", "terms", "postfixUnaryExpression", "postfixUnaryExpression.Operand", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_259()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12112);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_260()
        {
            //// 
            ////         private static void CollectBinaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12114);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_261()
        {
            ////         private static void CollectBinaryExpressionTerms(int position, ExpressionSyntax expression, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12271);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_262()
        {
            ////         {
            ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12282);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "leftFlags", "rightFlags", "position", "expression", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_263()
        {
            ////             ExpressionType leftFlags = ExpressionType.Invalid, rightFlags = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12383);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(BinaryExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "binaryExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_264()
        {
            //// 
            ////             var binaryExpression = (BinaryExpressionSyntax)expression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12385);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "expression", "(BinaryExpressionSyntax)expression", "leftFlags", "ExpressionType", "ExpressionType.Invalid", "rightFlags", "binaryExpression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_265()
        {
            ////             var binaryExpression = (BinaryExpressionSyntax)expression;
            ////             CollectExpressionTerms(position, binaryExpression.Left, terms, ref leftFlags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12457);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "binaryExpression", "binaryExpression.Left", "terms", "leftFlags", "CollectExpressionTerms", "expression", "(BinaryExpressionSyntax)expression" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_266()
        {
            ////             CollectExpressionTerms(position, binaryExpression.Left, terms, ref leftFlags);
            ////             CollectExpressionTerms(position, binaryExpression.Right, terms, ref rightFlags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12549);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "binaryExpression", "binaryExpression.Right", "terms", "rightFlags", "CollectExpressionTerms", "binaryExpression.Left", "leftFlags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_267()
        {
            ////             CollectExpressionTerms(position, binaryExpression.Right, terms, ref rightFlags);
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12643);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "binaryExpression", "binaryExpression.Right", "terms", "rightFlags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_268()
        {
            //// 
            ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12645);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm", "position", "binaryExpression", "binaryExpression.Right", "terms", "rightFlags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_269()
        {
            ////             if ((leftFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12730);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "leftFlags", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_270()
        {
            ////             {
            ////                 terms.Add(ConvertToString(binaryExpression.Left));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12745);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_271()
        {
            ////                 terms.Add(ConvertToString(binaryExpression.Left));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12813);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_272()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12828);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_273()
        {
            //// 
            ////             if ((rightFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12830);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression", "binaryExpression.Left", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_274()
        {
            ////             if ((rightFlags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12916);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_275()
        {
            ////             {
            ////                 terms.Add(ConvertToString(binaryExpression.Right));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 12931);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "terms", "binaryExpression", "binaryExpression.Right", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_276()
        {
            ////                 terms.Add(ConvertToString(binaryExpression.Right));
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13000);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "binaryExpression", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_277()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13015);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_278()
        {
            //// 
            ////             // Many sorts of binops (like +=) will definitely have side effects.  We only
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13017);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_279()
        {
            ////             // Many sorts of binops (like +=) will definitely have side effects.  We only
            ////             // consider this valid if it's a simple expression like +, -, etc.
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13108);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_280()
        {
            ////             // consider this valid if it's a simple expression like +, -, etc.
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13188);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_281()
        {
            //// 
            ////             switch (binaryExpression.Kind)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13190);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_282()
        {
            ////             switch (binaryExpression.Kind)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13234);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_283()
        {
            ////             {
            ////                 case SyntaxKind.AddExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13249);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_284()
        {
            ////                 case SyntaxKind.AddExpression:
            ////                 case SyntaxKind.SubtractExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 13297);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        // Tests 285-302 removed because they were redundant.
        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_303()
        {
            ////                 case SyntaxKind.AsExpression:
            ////                 case SyntaxKind.CoalesceExpression:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14319);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_304()
        {
            ////                 case SyntaxKind.CoalesceExpression:
            ////                     // We're valid if both children are...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14372);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "binaryExpression", "binaryExpression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_305()
        {
            ////                     // We're valid if both children are...
            ////                     expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14432);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType", "binaryExpression", "binaryExpression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_306()
        {
            ////                     expressionType = (leftFlags & rightFlags) & ExpressionType.ValidExpression;
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14529);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "leftFlags", "rightFlags", "ExpressionType", "ExpressionType.ValidExpression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_307()
        {
            ////                     return;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14558);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_308()
        {
            //// 
            ////                 default:
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14560);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_309()
        {
            ////                 default:
            ////                     expressionType = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14586);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid", "binaryExpression", "binaryExpression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_310()
        {
            ////                     expressionType = ExpressionType.Invalid;
            ////                     return;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14648);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "expressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_311()
        {
            ////                     return;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14677);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "rightFlags", "binaryExpression", "binaryExpression.Kind", "ExpressionType", "ExpressionType.ValidTerm", "terms", "binaryExpression.Right", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_312()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14692);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "binaryExpression", "binaryExpression.Kind" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_313()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14703);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_314()
        {
            //// 
            ////         private static void CollectArgumentTerms(int position, ArgumentListSyntax argumentList, IList<string> terms, ref ExpressionType expressionType)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14705);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_315()
        {
            ////         private static void CollectArgumentTerms(int position, ArgumentListSyntax argumentList, IList<string> terms, ref ExpressionType expressionType)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14858);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "argumentList", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_316()
        {
            ////         {
            ////             var validExpr = true;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14869);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "validExpr", "position", "argumentList", "terms", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_317()
        {
            ////             var validExpr = true;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14904);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments", "validExpr" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_318()
        {
            //// 
            ////             // Process the list of expressions.  This is probably a list of
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14906);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments", "validExpr" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_319()
        {
            ////             // Process the list of expressions.  This is probably a list of
            ////             // arguments to a function call(or a list of array index expressions)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 14983);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments", "validExpr" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_320()
        {
            ////             // arguments to a function call(or a list of array index expressions)
            ////             foreach (var arg in argumentList.Arguments)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15066);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments", "validExpr" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_321()
        {
            ////             foreach (var arg in argumentList.Arguments)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15123);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "arg", "argumentList", "argumentList.Arguments" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_322()
        {
            ////             {
            ////                 var flags = ExpressionType.Invalid;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15138);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.Invalid", "arg", "argumentList", "argumentList.Arguments", "flags" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_323()
        {
            ////                 var flags = ExpressionType.Invalid;
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15191);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "arg", "arg.Expression", "terms", "flags", "CollectExpressionTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_324()
        {
            //// 
            ////                 CollectExpressionTerms(position, arg.Expression, terms, ref flags);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15193);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "arg", "arg.Expression", "terms", "flags", "CollectExpressionTerms", "ExpressionType", "ExpressionType.Invalid" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_325()
        {
            ////                 CollectExpressionTerms(position, arg.Expression, terms, ref flags);
            ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15278);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "ExpressionType.ValidTerm", "position", "arg", "arg.Expression", "terms", "flags", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_326()
        {
            ////                 if ((flags & ExpressionType.ValidTerm) == ExpressionType.ValidTerm)
            ////                 {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15363);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_327()
        {
            ////                 {
            ////                     terms.Add(ConvertToString(arg.Expression));
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15382);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "terms", "arg", "arg.Expression", "ConvertToString", "ExpressionType", "ExpressionType.ValidTerm" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_328()
        {
            ////                     terms.Add(ConvertToString(arg.Expression));
            ////                 }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15447);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "terms", "arg", "arg.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_329()
        {
            ////                 }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15466);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidExpression", "validExpr", "ExpressionType.ValidTerm", "terms", "arg", "arg.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_330()
        {
            //// 
            ////                 validExpr &= (flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15468);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidExpression", "validExpr", "ExpressionType.ValidTerm", "terms", "arg", "arg.Expression", "ConvertToString" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_331()
        {
            ////                 validExpr &= (flags & ExpressionType.ValidExpression) == ExpressionType.ValidExpression;
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15574);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "ExpressionType.ValidExpression", "validExpr" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_332()
        {
            ////             }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15589);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType", "arg", "argumentList", "argumentList.Arguments" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_333()
        {
            //// 
            ////             // We're never a valid term, but we're a valid expression if all
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15591);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType", "arg", "argumentList", "argumentList.Arguments" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_334()
        {
            ////             // We're never a valid term, but we're a valid expression if all
            ////             // the list elements are...
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15669);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType", "arg", "argumentList", "argumentList.Arguments" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_335()
        {
            ////             // the list elements are...
            ////             expressionType = validExpr ? ExpressionType.ValidExpression : 0;
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15710);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "flags", "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType", "arg", "argumentList", "argumentList.Arguments" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_336()
        {
            ////             expressionType = validExpr ? ExpressionType.ValidExpression : 0;
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15788);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "ExpressionType", "validExpr", "ExpressionType.ValidExpression", "expressionType" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_337()
        {
            ////         }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15799);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_338()
        {
            //// 
            ////         private static void CollectVariableTerms(int position, SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, List<string> terms)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15801);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_339()
        {
            ////         private static void CollectVariableTerms(int position, SeparatedSyntaxList<VariableDeclaratorSyntax> declarators, List<string> terms)
            ////         {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15944);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "declarators", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_340()
        {
            ////         {
            ////             foreach (var declarator in declarators)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 15955);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarators", "position", "terms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_341()
        {
            ////             foreach (var declarator in declarators)
            ////             {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16008);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarators" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_342()
        {
            ////             {
            ////                 if (declarator.InitializerOpt != null)
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16023);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarator.InitializerOpt", "declarators" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_343()
        {
            ////                 if (declarator.InitializerOpt != null)
            ////                 {
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16079);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarator.InitializerOpt" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_344()
        {
            ////                 {
            ////                     CollectExpressionTerms(position, declarator.Initializer.Value, terms);
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16098);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "declarator.InitializerOpt", "declarator.InitializerOpt.Value", "terms", "CollectExpressionTerms", "declarator" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_345()
        {
            ////                     CollectExpressionTerms(position, declarator.Initializer.Value, terms);
            ////                 }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16193);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "position", "declarator.InitializerOpt", "declarator.InitializerOpt.Value", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_346()
        {
            ////                 }
            ////             }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16212);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarator.InitializerOpt", "position", "declarator.InitializerOpt.Value", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_347()
        {
            ////             }
            ////         }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16227);
            Assert.NotNull(terms);
            AssertEx.SetEqual(new[] { "declarator", "declarators", "position", "declarator.InitializerOpt", "declarator.InitializerOpt.Value", "terms", "CollectExpressionTerms" }, terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_348()
        {
            ////         }
            ////     }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16238);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_349()
        {
            ////     }
            //// }
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16245);
            Assert.Null(terms);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)]
        public void TestAtStartOfLine_350()
        {
            //// }
            //// 
            //// ^
            var tree = GetTree();
            var terms = CSharpProximityExpressionsService.Do(tree, 16248);
            Assert.Null(terms);
        }
    }
}
