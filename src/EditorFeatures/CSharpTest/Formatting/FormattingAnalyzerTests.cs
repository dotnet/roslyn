// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingAnalyzerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
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
