// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ConvertToRawString;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertToRawString
{
    using VerifyCS = CSharpCodeRefactoringVerifier<
        ConvertRegularStringToRawStringCodeRefactoringProvider>;

    public class ConvertToRegularStringToRawStringStringTests
    {
        private async Task VerifyRefactoringAsync(string testCode, string fixedCode)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                LanguageVersion = LanguageVersionExtensions.CSharpNext,
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotInDirective()
        {
            var code = @"
#line 1 [||]""goo.cs""";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnEmptyString()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]"""";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnEmptyVerbatimString()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]@"""";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnHighSurrogateChar()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\uD800"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestNotOnLowSurrogateChar1()
        {
            var code = @"public class C
{
    void M()
    {
        var v = [||]""\uDC00"";
    }
}";

            await VerifyRefactoringAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertRegularToRawString)]
        public async Task TestOnCombinedSurrogate()
        {
            await VerifyRefactoringAsync(
@"public class C
{
    void M()
    {
        var v = [||]""\uD83D\uDC69"";
    }
}",
@"public class C
{
    void M()
    {
        var v = """"""👩"""""";
    }
}");
        }
    }
}
