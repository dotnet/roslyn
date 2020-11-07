// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertStringConcatToInterpolated;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertStringConcatToInterpolated
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertStringConcatToInterpolatedRefactoringProvider>;

    // Temporary work around to avoid merge conflicts with master until VS 16.8 is released
    internal static class Traits
    {
        public const string Feature = nameof(Feature);

        internal static class Features
        {
            public const string CodeActionsConvertStringConcatToInterpolated = "CodeActions.ConvertStringConcatToInterpolated";
        }
    }

    public class ConvertStringConcatToInterpolatedTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        public async Task IfExpressionAndConcatenatedTextAreRefactoredToInterpolated()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = (true ? ""t"" : ""f"") [|+|] ""a"";
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = $""{(true ? ""t"" : ""f"")}a"";
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        public async Task IfExpressionSurroundedByConcatenatedTextAreRefactoredToInterpolated()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = ""a"" + (true ? ""t"" : ""f"") [|+|] ""b"";
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = $""a{(true ? ""t"" : ""f"")}b"";
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        public async Task TwoIfExpressionSurroundedByConcatenatedTextAreRefactoredToInterpolated()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = ""a"" + (true ? ""t"" : ""f"") [|+|] ""b"" + (false ? ""t"" : ""f"") + ""c"";
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = $""a{(true ? ""t"" : ""f"")}b{(false ? ""t"" : ""f"")}c"";
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        [InlineData(@"""a"" [|+|] ""b""")]
        [InlineData(@"""a"" [|+|] @""b""")]
        [InlineData(@"""a"" [|+|] @""b"" + ""c""")]
        public async Task DontOfferIfOnlyStringLiteralsAreConcatenated(string concatenations)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {concatenations};
    }}
}}";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = initialMarkup,
                OffersEmptyRefactoring = false,
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        [InlineData(@"""a"" [|+|] ""b"" + (true ? ""t"" : ""f"")",
                   @"$""ab{(true ? ""t"" : ""f"")}""")]
        [InlineData(@"""a"" [|+|] ""b"" + ""c"" + (true ? ""t"" : ""f"")",
                   @"$""abc{(true ? ""t"" : ""f"")}""")]
        [InlineData(@"""a"" [|+|] ""b"" + @""c"" + (true ? ""t"" : ""f"")",
                   @"$""ab{@""c""}{(true ? ""t"" : ""f"")}""")]
        public async Task ContiguousStringLiteralsAreMerged(string before, string after)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {before};
    }}
}}";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {after};
    }}
}}";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        [InlineData(@"""a"" [|+|] $""b{1:000}""",
                   @"$""ab{1:000}""")]
        [InlineData(@"""a"" [|+|] $""b{1:000}c"" + ""d""",
                   @"$""ab{1:000}cd""")]
        [InlineData(@"""a"" [|+|] $@""b{1:000}c"" + ""d""",
                   @"$""a{$@""b{1:000}c""}d""")]
        public async Task ConcatWithInterpolatedStringGetsMerged(string before, string after)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {before};
    }}
}}";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {after};
    }}
}}";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsConvertStringConcatToInterpolated)]
        [InlineData(@"""a"" [|+|] (1 + 1)",
                   @"$""a{1 + 1}""")]
        [InlineData(@"""a"" [|+|] (1 + 1) + (2 + 2)",
                   @"$""a{1 + 1}{2 + 2}""")]
        [InlineData(@"""a"" [|+|] (true ? ""t"": ""f"")",
                   @"$""a{(true ? ""t"": ""f"")}""")]
        public async Task ExpressionParenthesisAreRemovedIfPossible(string before, string after)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {before};
    }}
}}";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {after};
    }}
}}";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }
    }
}
