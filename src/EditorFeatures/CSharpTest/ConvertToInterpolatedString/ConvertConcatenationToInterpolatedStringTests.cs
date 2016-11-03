// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace)
            => new CSharpConvertConcatenationToInterpolatedStringRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestMissingOnSimpleString()
        {
            await TestMissingAsync(
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
            await TestMissingAsync(
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
            await TestMissingAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertToInterpolatedString)]
        public async Task TestWithComplexExpressions()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
            await TestMissingAsync(
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
            await TestMissingAsync(
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
            await TestAsync(
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
            await TestMissingAsync(
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
    }
}