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
        public async Task ToStringWithNoParameter()
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
        public async Task ToStringWithStringLiteralParameter()
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
        public async Task ToStringWithEscapeSequences()
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
        public async Task ToStringWithVerbatimStringLiteralParameter()
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
        public async Task ToStringWithVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
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
        public async Task ToStringWithVerbatimEscapeSequencesInsideNonVerbatimInterpolatedString()
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
        public async Task ToStringWithNonVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
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
        public async Task ToStringWithStringConstantParameter()
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
        public async Task ToStringWithCharacterLiteralParameter()
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
        public async Task ToStringWithFormatProvider()
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
