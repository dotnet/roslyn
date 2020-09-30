// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public FormattingAnalyzerTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new FormattingDiagnosticAnalyzer(), new FormattingCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TrailingWhitespace()
        {
            var testCode =
                "class X[| |]" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;
            var expected =
                "class X" + Environment.NewLine +
                "{" + Environment.NewLine +
                "}" + Environment.NewLine;
            await TestInRegularAndScriptAsync(testCode, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task TestMissingSpace()
        {
            var testCode = @"
class TypeName
{
    void Method()
    {
        if$$(true)return;
    }
}
";
            var expected = @"
class TypeName
{
    void Method()
    {
        if (true) return;
    }
}
";

            await TestInRegularAndScriptAsync(testCode, expected);
        }
    }
}
