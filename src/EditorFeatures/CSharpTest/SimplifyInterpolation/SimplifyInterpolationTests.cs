// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyInterpolation
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyInterpolation)]
    public partial class SimplifyInterpolationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyInterpolationDiagnosticAnalyzer(), new CSharpSimplifyInterpolationCodeFixProvider());

        [Fact]
        public async Task ToString_with_no_parameter()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_string_literal_parameter()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(""|}some format code{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue:some format code} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_escape_sequences()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(""|}\\d \""d\""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue:\\d \""d\""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_verbatim_string_literal_parameter()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(@""|}some format code{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue:some format code} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_verbatim_escape_sequences_inside_verbatim_interpolated_string()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $@""prefix {someValue{|Unnecessary:[||].ToString(@""|}\d """"d""""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $@""prefix {someValue:\d """"d""""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_verbatim_escape_sequences_inside_non_verbatim_interpolated_string()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(@""|}\d """"d""""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue:\\d \""d\""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_non_verbatim_escape_sequences_inside_verbatim_interpolated_string()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $@""prefix {someValue{|Unnecessary:[||].ToString(""|}\\d \""d\""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $@""prefix {someValue:\d """"d""""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_string_constant_parameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int someValue)
    {
        const string someConst = ""some format code"";
        _ = $""prefix {someValue[||].ToString(someConst)} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToString_with_character_literal_parameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(C someValue)
    {
        _ = $""prefix {someValue[||].ToString('f')} suffix"";
    }

    public string ToString(object obj) => null;
}");
        }

        [Fact]
        public async Task ToString_with_format_provider()
        {
            // (If someone is explicitly specifying culture, an implicit form should not be encouraged.)

            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue[||].ToString(""some format code"", System.Globalization.CultureInfo.CurrentCulture)} suffix"";
    }
}");
        }
    }
}
