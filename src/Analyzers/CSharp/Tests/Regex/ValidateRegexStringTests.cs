// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Regex;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpRegexDiagnosticAnalyzer, EmptyCodeFixProvider>;

    public class ValidateRegexStringTests
    {
        private static OptionsCollection OptionOn()
            => new(LanguageNames.CSharp)
            {
                { RegularExpressionsOptions.ReportInvalidRegexPatterns, true }
            };

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarning1()
        {
            var source = @"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(@""[|)|]"");
    }     
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = OptionOn(),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarning2()
        {
            var source = @"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(""[|\u0029|]"");
    }     
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = OptionOn(),
            }.RunAsync();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarningMissing1()
        {
            var source = @"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(@""\u0029"");
    }     
}";
            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = source,
                Options = OptionOn(),
            }.RunAsync();
        }
    }
}
