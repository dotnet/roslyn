// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.SimplifyInterpolation;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyInterpolation;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyInterpolation)]
public sealed class SimplifyInterpolationTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpSimplifyInterpolationDiagnosticAnalyzer(), new CSharpSimplifyInterpolationCodeFixProvider());

    [Fact]
    public async Task SubsequentUnnecessarySpansDoNotRepeatTheSmartTag()
    {
        var parameters = new TestParameters(retainNonFixableDiagnostics: true, includeDiagnosticsOutsideSelection: true);

        using var workspace = CreateWorkspaceFromOptions("""
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}3{|Unnecessary:)|}} suffix";
                }
            }
            """, parameters);

        var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters);

        Assert.Equal(
            [
                ("IDE0071", DiagnosticSeverity.Info),
            ],
            diagnostics.Select(d => (d.Descriptor.Id, d.Severity)));
    }

    [Fact]
    public Task ToStringWithNoParameter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString()|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithParameter()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString("|}g{|Unnecessary:")|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(int someValue)
                {
                    _ = $"prefix {someValue:g} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithEscapeSequences()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString("|}\\d \"d\"{|Unnecessary:")|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue:\\d \"d\"} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $@"prefix {someValue{|Unnecessary:[||].ToString(@"|}\d ""d""{|Unnecessary:")|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $@"prefix {someValue:\d ""d""} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithVerbatimEscapeSequencesInsideNonVerbatimInterpolatedString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString(@"|}\d ""d""{|Unnecessary:")|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue:\\d \"d\"} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithNonVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $@"prefix {someValue{|Unnecessary:[||].ToString("|}\\d \"d\"{|Unnecessary:")|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $@"prefix {someValue:\d ""d""} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithStringConstantParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    const string someConst = "some format code";
                    _ = $"prefix {someValue[||].ToString(someConst)} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithCharacterLiteralParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(C someValue)
                {
                    _ = $"prefix {someValue[||].ToString('f')} suffix";
                }

                public string ToString(object obj) => null;
            }
            """);

    [Fact]
    public Task ToStringWithFormatProvider()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue[||].ToString("some format code", System.Globalization.CultureInfo.CurrentCulture)} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithInvariantCultureInsideFormattableStringInvariant()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue[||]{|Unnecessary:.ToString(System.Globalization.CultureInfo.InvariantCulture)|}} suffix");
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue} suffix");
                }
            }
            """);

    [Fact]
    public Task DateTimeFormatInfoInvariantInfoIsRecognized()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue[||]{|Unnecessary:.ToString(System.Globalization.DateTimeFormatInfo.InvariantInfo)|}} suffix");
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue} suffix");
                }
            }
            """);

    [Fact]
    public Task NumberFormatInfoInvariantInfoIsRecognized()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(int someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue[||]{|Unnecessary:.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)|}} suffix");
                }
            }
            """,
            """
            class C
            {
                void M(int someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue} suffix");
                }
            }
            """);

    [Fact]
    public Task ToStringWithInvariantCultureOutsideFormattableStringInvariant()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue[||].ToString(System.Globalization.CultureInfo.InvariantCulture)} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithFormatAndInvariantCultureInsideFormattableStringInvariant()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue[||]{|Unnecessary:.ToString("|}some format code{|Unnecessary:", System.Globalization.CultureInfo.InvariantCulture)|}} suffix");
                }
            }
            """,
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = System.FormattableString.Invariant($"prefix {someValue:some format code} suffix");
                }
            }
            """);

    [Fact]
    public Task ToStringWithFormatAndInvariantCultureOutsideFormattableStringInvariant()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(System.DateTime someValue)
                {
                    _ = $"prefix {someValue[||].ToString("some format code", System.Globalization.CultureInfo.InvariantCulture)} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWithIntegerLiteral()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:)|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWithIntegerLiteral()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,-3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWithComplexConstantExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    const int someConstant = 1;
                    _ = $"prefix {someValue{|Unnecessary:[||].PadLeft(|}(byte)3.3 + someConstant{|Unnecessary:)|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    const int someConstant = 1;
                    _ = $"prefix {someValue,(byte)3.3 + someConstant} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWithSpaceChar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:, ' ')|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWithSpaceChar()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:, ' ')|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,-3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWithNonSpaceChar()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadLeft(3, '\t')} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWithNonSpaceChar()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadRight(3, '\t')} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWithComplexConstantExpressionRequiringParentheses()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    const int someConstant = 1;
                    _ = $"prefix {someValue{|Unnecessary:[||].PadRight(|}(byte)3.3 + someConstant{|Unnecessary:)|}} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    const int someConstant = 1;
                    _ = $"prefix {someValue,-((byte)3.3 + someConstant)} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithNoParameterWhenFormattingComponentIsSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].ToString():goo} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithStringLiteralParameterWhenFormattingComponentIsSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].ToString("bar"):goo} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithNoParameterWhenAlignmentComponentIsSpecified()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString()|},3} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,3} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithNoParameterWhenBothComponentsAreSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].ToString(),3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithStringLiteralParameterWhenBothComponentsAreSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].ToString("some format code"),3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWhenFormattingComponentIsSpecified()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadLeft(3):goo} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWhenFormattingComponentIsSpecified()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadRight(3):goo} suffix";
                }
            }
            """,
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,-3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWhenAlignmentComponentIsSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadLeft(3),3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWhenAlignmentComponentIsSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadRight(3),3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftWhenBothComponentsAreSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadLeft(3),3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task PadRightWhenBothComponentsAreSpecified()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue[||].PadRight(3),3:goo} suffix";
                }
            }
            """);

    [Fact]
    public Task ToStringWithoutFormatThenPadLeft()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}3{|Unnecessary:)|}} suffix";
                }
            }
            """, """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue,3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftThenToStringWithoutFormat()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|}} suffix";
                }
            }
            """, """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3)} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftThenToStringWithoutFormatWhenAlignmentComponentIsSpecified()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|},3} suffix";
                }
            }
            """, """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3),3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftThenPadRight_WithoutAlignment()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3){|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix";
                }
            }
            """, """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3),-3} suffix";
                }
            }
            """);

    [Fact]
    public Task PadLeftThenPadRight_WithAlignment()
        => TestMissingAsync(
            """
            class C
            {
                void M(string someValue)
                {
                    _ = $"prefix {someValue.PadLeft(3)[||].PadRight(3),3} suffix";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnImplicitToStringReceiver()
        => TestMissingAsync(
            """
            class C
            {
                override string ToString() => "Goobar";

                string GetViaInterpolation() => $"Hello {ToString[||]()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnImplicitToStringReceiverWithArg()
        => TestMissingAsync(
            """
            class C
            {
                string ToString(string arg) => "Goobar";

                string GetViaInterpolation() => $"Hello {ToString[||]("g")}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnStaticToStringReceiver()
        => TestMissingAsync(
            """
            class C
            {
                public static string ToString() => "Goobar";

                string GetViaInterpolation() => $"Hello {ToString[||]()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnStaticToStringReceiverWithArg()
        => TestMissingAsync(
            """
            class C
            {
                public static string ToString(string arg) => "Goobar";

                string GetViaInterpolation() => $"Hello {ToString[||]("g")}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnImplicitPadLeft()
        => TestMissingAsync(
            """
            class C
            {
                public string PadLeft(int val) => "";

                void M(string someValue)
                {
                    _ = $"prefix {[||]PadLeft(3)} suffix";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/41381")]
    public Task MissingOnStaticPadLeft()
        => TestMissingAsync(
            """
            class C
            {
                public static string PadLeft(int val) => "";

                void M(string someValue)
                {
                    _ = $"prefix {[||]PadLeft(3)} suffix";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42247")]
    public Task OnConstantAlignment1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq;

            public static class Sample
            {
                public static void PrintRightAligned ( String[] strings )
                {
                    const int maxLength = 1;

                    for ( var i = 0; i < strings.Length; i++ )
                    {
                        var str = strings[i];
                        Console.WriteLine ($"{i}.{str[||].PadRight(maxLength, ' ')}");
                    }
                }
            }
            """,

            """
            using System;
            using System.Linq;

            public static class Sample
            {
                public static void PrintRightAligned ( String[] strings )
                {
                    const int maxLength = 1;

                    for ( var i = 0; i < strings.Length; i++ )
                    {
                        var str = strings[i];
                        Console.WriteLine ($"{i}.{str,-maxLength}");
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42247")]
    public Task MissingOnNonConstantAlignment()
        => TestMissingAsync(
            """
            using System;
            using System.Linq;

            public static class Sample
            {
                public static void PrintRightAligned ( String[] strings )
                {
                    var maxLength = strings.Max(str => str.Length);

                    for ( var i = 0; i < strings.Length; i++ )
                    {
                        var str = strings[i];
                        Console.WriteLine ($"{i}.{str[||].PadRight(maxLength, ' ')}");
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")]
    public Task MissingOnBaseToString()
        => TestMissingAsync(
            """
            class C
            {
                public override string ToString() => $"Test: {base[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")]
    public Task MissingOnBaseToStringEvenWhenNotOverridden()
        => TestMissingAsync(
            """
            class C
            {
                string M() => $"Test: {base[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")]
    public Task MissingOnBaseToStringWithArgument()
        => TestMissingAsync(
            """
            class Base
            {
                public string ToString(string format) => format;
            }

            class Derived : Base
            {
                public override string ToString() => $"Test: {base[||].ToString("a")}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42669")]
    public Task PadLeftSimplificationIsStillOfferedOnBaseToString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public override string ToString() => $"Test: {base.ToString()[||].PadLeft(10)}";
            }
            """,
            """
            class C
            {
                public override string ToString() => $"Test: {base.ToString(),10}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")]
    public Task FormatComponentSimplificationIsNotOfferedOnNonIFormattableType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                string M(TypeNotImplementingIFormattable value) => $"Test: {value[||].ToString("a")}";
            }

            struct TypeNotImplementingIFormattable
            {
                public string ToString(string format) => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")]
    public Task FormatComponentSimplificationIsOfferedOnIFormattableType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M(TypeImplementingIFormattable value) => $"Test: {value[||].ToString("a")}";
            }

            struct TypeImplementingIFormattable : IFormattable
            {
                public string ToString(string format) => "A";

                string IFormattable.ToString(string format, IFormatProvider formatProvider) => "B";
            }
            """,
            """
            using System;

            class C
            {
                string M(TypeImplementingIFormattable value) => $"Test: {value:a}";
            }

            struct TypeImplementingIFormattable : IFormattable
            {
                public string ToString(string format) => "A";

                string IFormattable.ToString(string format, IFormatProvider formatProvider) => "B";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")]
    public Task ParameterlessToStringSimplificationIsStillOfferedOnNonIFormattableType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                string M(TypeNotImplementingIFormattable value) => $"Test: {value[||].ToString()}";
            }

            struct TypeNotImplementingIFormattable
            {
                public string ToString(string format) => "A";
            }
            """,
            """
            class C
            {
                string M(TypeNotImplementingIFormattable value) => $"Test: {value}";
            }

            struct TypeNotImplementingIFormattable
            {
                public string ToString(string format) => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42887")]
    public Task PadLeftSimplificationIsStillOfferedOnNonIFormattableType()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                string M(TypeNotImplementingIFormattable value) => $"Test: {value.ToString("a")[||].PadLeft(10)}";
            }

            struct TypeNotImplementingIFormattable
            {
                public string ToString(string format) => "A";
            }
            """,
            """
            class C
            {
                string M(TypeNotImplementingIFormattable value) => $"Test: {value.ToString("a"),10}";
            }

            struct TypeNotImplementingIFormattable
            {
                public string ToString(string format) => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnRefStructIfInterpolatedStringHandlersUnavailable()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                string M(RefStruct someValue) => $"Test: {someValue[||].ToString()}";
            }

            ref struct RefStruct
            {
                public override string ToString() => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnReadOnlySpanIfInterpolatedStringHandlersUnavailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            
            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }
            
            class C
            {
                string M(ReadOnlySpan<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnSpanIfInterpolatedStringHandlersUnavailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            
            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }
            
            class C
            {
                string M(Span<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnRefStructIfInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                string M(RefStruct someValue) => $"Test: {someValue[||].ToString()}";
            }

            ref struct RefStruct
            {
                public override string ToString() => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsOfferedOnReadOnlySpanOfCharIfInterpolatedStringHandlersAvailable()
        => TestInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }

            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                string M(ReadOnlySpan<char> span) => $"Test: {span[||].ToString()}";
            }
            """,
            """
            using System;

            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }
            
            class C
            {
                string M(ReadOnlySpan<char> span) => $"Test: {span}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsOfferedOnSpanOfCharIfInterpolatedStringHandlersAvailable()
        => TestInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }

            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                string M(Span<char> span) => $"Test: {span[||].ToString()}";
            }
            """,
            """
            using System;
            
            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }
            
            class C
            {
                string M(Span<char> span) => $"Test: {span}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnReadOnlySpanOfIntIfInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                string M(ReadOnlySpan<int> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnSpanOfIntIfInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                string M(Span<int> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnReadOnlySpanIfTargetsFormattableStringAndInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                FormattableString M(ReadOnlySpan<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnSpanIfTargetsFormattableStringAndInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }
            
            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                FormattableString M(Span<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnReadOnlySpanIfTargetsIFormattableAndInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct ReadOnlySpan<T> { }
            }

            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                IFormattable M(ReadOnlySpan<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80132")]
    public Task ToStringSimplificationIsNotOfferedOnSpanIfTargetsIFormattableAndInterpolatedStringHandlersAvailable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            namespace System
            {
                public ref struct Span<T> { }
                public ref struct ReadOnlySpan<T> { }
            }

            namespace System.Runtime.CompilerServices
            {
                public class InterpolatedStringHandlerAttribute { }
            }

            class C
            {
                IFormattable M(Span<char> span) => $"Test: {span[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42936")]
    public Task PadLeftSimplificationIsStillOfferedOnRefStruct()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                string M(RefStruct someValue) => $"Test: {someValue.ToString()[||].PadLeft(10)}";
            }

            ref struct RefStruct
            {
                public override string ToString() => "A";
            }
            """,
            """
            class C
            {
                string M(RefStruct someValue) => $"Test: {someValue.ToString(),10}";
            }

            ref struct RefStruct
            {
                public override string ToString() => "A";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46011")]
    public Task ShadowedToString()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public new string ToString() => "Shadow";
                static string M(C c) => $"{c[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46011")]
    public Task OverridenShadowedToString()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public new string ToString() => "Shadow";
            }

            class B : C
            {
                public override string ToString() => "OverrideShadow";
                static string M(C c) => $"{c[||].ToString()}";
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46011")]
    public Task DoubleOverridenToString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public override string ToString() => "Override";
            }

            class B : C
            {
                public override string ToString() => "OverrideOverride";

                void M(B someValue)
                {
                    _ = $"prefix {someValue{|Unnecessary:[||].ToString()|}} suffix";
                }
            }
            """,
            """
            class C
            {
                public override string ToString() => "Override";
            }

            class B : C
            {
                public override string ToString() => "OverrideOverride";

                void M(B someValue)
                {
                    _ = $"prefix {someValue} suffix";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49647")]
    public Task ConditionalExpressionMustRemainParenthesizedWhenUsingParameterlessToString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? 1 : 2){|Unnecessary:[||].ToString()|}}";
                }
            }
            """,
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? 1 : 2)}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49647")]
    public Task ConditionalExpressionMustRemainParenthesizedWhenUsingParameterizedToString()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? 1 : 2){|Unnecessary:[||].ToString("|}g{|Unnecessary:")|}}";
                }
            }
            """,
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? 1 : 2):g}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49647")]
    public Task ConditionalExpressionMustRemainParenthesizedWhenUsingPadLeft()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? "1" : "2"){|Unnecessary:[||].PadLeft(|}3{|Unnecessary:)|}}";
                }
            }
            """,
            """
            class C
            {
                void M(bool cond)
                {
                    _ = $"{(cond ? "1" : "2"),3}";
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47956")]
    public Task TestNotPassedToFormattableString1()
        => TestMissingAsync(
            """
            class C
            {
                void B(string[] args) => M($"{args[||].Length.ToString()}");

                void M(FormattableString fs)
                {
                    foreach (object o in fs.GetArguments())
                        Console.WriteLine(o?.GetType());
                }
            }
            """);

    [Fact]
    public Task TestNotPassedToIFormattable()
        => TestMissingAsync(
            """
            class C : ICustomFormatter, IFormatProvider
            {
                void B(string[] args) => M($"{args[||].Length.ToString()}");

                void M(IFormattable fs)
                {
                    fs.ToString(null, formatProvider: this);
                }

                object? IFormatProvider.GetFormat(Type? formatType)
                {
                    return formatType == typeof(ICustomFormatter) ? this : null;
                }

                string ICustomFormatter.Format(string? format, object? arg, IFormatProvider? formatProvider)
                {
                    Console.WriteLine(arg?.GetType());
                    return "";
                }
            }
            """);

    [Theory]
    [InlineData("DateTime", "ToLongDateString", "D")]
    [InlineData("DateTime", "ToShortDateString", "d")]
    [InlineData("DateTime", "ToLongTimeString", "T")]
    [InlineData("DateTime", "ToShortTimeString", "t")]
    [InlineData("DateOnly", "ToLongDateString", "D")]
    [InlineData("DateOnly", "ToShortDateString", "d")]
    [InlineData("TimeOnly", "ToLongTimeString", "T")]
    [InlineData("TimeOnly", "ToShortTimeString", "t")]
    public async Task TestWellKnowToStringMethods(string targetType, string targetMethodName, string expectedFormat)
    {
        // Replace polyfills of `DateOnly` and `TimeOnly`
        // with .NET 6+ reference assemblies when porting this test to VerifyCS
        await TestInRegularAndScriptAsync($$$"""
            using System;
            
            class C
            {
                void M({{{targetType}}} obj)
                {
                    _ = $"prefix {obj{|Unnecessary:[||].{{{targetMethodName}}}()|}} suffix";
                }
            }

            namespace System
            {
                public readonly struct DateOnly
                {
                    public string ToLongDateString() => default;
                    public string ToShortDateString() => default;
                }

                public readonly struct TimeOnly
                {
                    public string ToLongTimeString() => default;
                    public string ToShortTimeString() => default;
                }
            }
            """, $$"""
            using System;
            
            class C
            {
                void M({{targetType}} obj)
                {
                    _ = $"prefix {obj:{{expectedFormat}}} suffix";
                }
            }

            namespace System
            {
                public readonly struct DateOnly
                {
                    public string ToLongDateString() => default;
                    public string ToShortDateString() => default;
                }
            
                public readonly struct TimeOnly
                {
                    public string ToLongTimeString() => default;
                    public string ToShortTimeString() => default;
                }
            }
            """);
    }
}
