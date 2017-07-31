﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ConvertToInterpolatedString;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToInterpolatedString
{
    public class ConvertConcatenationToInterpolatedStringTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertConcatenationToInterpolatedStringRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnSimpleString()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnConcatenatedStrings1()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"" + ""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnConcatenatedStrings2()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""string"" + [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringOnLeft()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = [||]""string"" + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestRightSideOfString()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""string""[||] + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""string{1}"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithStringOnRight()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1}string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithComplexExpressionOnLeft()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + 2 + [||]""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1 + 2}string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"
public class C
{
    void M()
    {
        var v =
            // Leading trivia
            1 + 2 + [||]""string"" /* trailing trivia */;
    }
}",
@"
public class C
{
    void M()
    {
        var v =
            // Leading trivia
            $""{1 + 2}string"" /* trailing trivia */;
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithComplexExpressions()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + 2 + [||]""string"" + 3 + 4;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""{1 + 2}string{3}{4}"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithEscapes1()
        {
            await TestInRegularAndScriptAsync(
@"
public class C
{
    void M()
    {
        var v = ""\r"" + 2 + [||]""string"" + 3 + ""\n"";
    }
}",
@"
public class C
{
    void M()
    {
        var v = $""\r{2}string{3}\n"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithEscapes2()
        {
            await TestInRegularAndScriptAsync(
@"
public class C
{
    void M()
    {
        var v = ""\\r"" + 2 + [||]""string"" + 3 + ""\\n"";
    }
}",
@"
public class C
{
    void M()
    {
        var v = $""\\r{2}string{3}\\n"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithVerbatimString1()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]@""string"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $@""{1}string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithMixedStringTypes1()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + [||]@""string"" + 2 + ""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingWithMixedStringTypes2()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = 1 + @""string"" + 2 + [||]""string"";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithOverloadedOperator()
        {
            await TestInRegularAndScriptAsync(
@"public class D
{
    public static bool operator +(D d, string s) => false;
    public static bool operator +(string s, D d) => false;
}

public class C
{
    void M()
    {
        D d = null;
        var v = 1 + [||]""string"" + d;
    }
}",
@"public class D
{
    public static bool operator +(D d, string s) => false;
    public static bool operator +(string s, D d) => false;
}

public class C
{
    void M()
    {
        D d = null;
        var v = $""{1}string"" + d;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithOverloadedOperator2()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class D
{
    public static bool operator +(D d, string s) => false;
    public static bool operator +(string s, D d) => false;
}

public class C
{
    void M()
    {
        D d = null;
        var v = d + [||]""string"" + 1;
    }
}");
        }

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + ""C"";
    }
}",
@"public class C
{
    void M()
    {
        var v = $""A{1}BC"";
    }
}");
        }

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations2()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + [||]""B"" + ""C"" + 1;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""ABC{1}"";
    }
}");
        }

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations3()
        {
            await TestInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + ""C"" + 2 +""D""+ ""E""+ ""F"" + 3;
    }
}",
@"public class C
{
    void M()
    {
        var v = $""A{1}BC{2}DEF{3}"";
    }
}");
        }

        [WorkItem(16820, "https://github.com/dotnet/roslyn/issues/16820")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithMultipleStringConcatinations4()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class C
{
    void M()
    {
        var v = ""A"" + 1 + [||]""B"" + @""C"";
    }
}");
        }
    }
}
