// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ElseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(
@"class C {
#if $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestInPreprocessorFollowedBySkippedTokens()
        {
            await VerifyKeywordAsync(
@"#if GOO
#$$
dasd
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHash()
        {
            await VerifyKeywordAsync(
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfExpressionStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfExpressionStatement_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
{
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
{
}
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfWhileStatementBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    while (true)
    {
    }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfWhileStatementBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    while (true)
    {
    }
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfExpressionStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfExpressionStatement_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
    {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
    {
    }
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfWhileStatementBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        while (true)
        {
        }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfWhileStatementBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        while (true)
        {
        }
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseExpressionStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseExpressionStatement_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
    {
    }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
    {
    }
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseWhileStatementBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        while (true)
        {
        }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfNestedIfElseWhileStatementBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        while (true)
        {
        }
$$
else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfNestedIfElseElseExpressionStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
    Console.WriteLine();
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfNestedIfElseElseBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
{
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfNestedIfElseElseWhileStatementBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
else
    while (true)
    {
    }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfExpressionStatementElse()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfBlockElse()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
{
}
else
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfWhileStatementBlockElse()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    while (true)
    {
    }
else
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseExpressionStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    Console.WriteLine();
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
{
}
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseWhileStatementBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    while (true)
    {
    }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfExpressionStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfExpressionStatement_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
    {
    }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
    {
    }
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfWhileStatementBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        while (true)
        {
        }
    $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterIfElseNestedIfWhileStatementBlock_BeforeElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        while (true)
        {
        }
    $$
    else"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseNestedIfElseExpressionStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
        Console.WriteLine();
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseNestedIfElseBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
    {
    }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterIfElseNestedIfElseWhileStatementBlock()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)
    Console.WriteLine();
else
    if (true)
        Console.WriteLine();
    else
        while (true)
        {
        }
$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMemberAccess()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"if (true)string.$$"));
        }
    }
}
