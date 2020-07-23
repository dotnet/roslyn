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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyInterpolation
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyInterpolation)]
    public partial class SimplifyInterpolationTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyInterpolationDiagnosticAnalyzer(), new CSharpSimplifyInterpolationCodeFixProvider());

        [Fact]
        public async Task SubsequentUnnecessarySpansDoNotRepeatTheSmartTag()
        {
            var parameters = new TestParameters(retainNonFixableDiagnostics: true, includeDiagnosticsOutsideSelection: true);

            using var workspace = CreateWorkspaceFromOptions(@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}3{|Unnecessary:)|}} suffix"";
    }
}", parameters);

            var diagnostics = await GetDiagnosticsWorkerAsync(workspace, parameters);

            Assert.Equal(
                new[] {
                    ("IDE0071", DiagnosticSeverity.Info),
                    ("IDE0071WithoutSuggestion", DiagnosticSeverity.Hidden),
                    ("IDE0071WithoutSuggestion", DiagnosticSeverity.Hidden),
                },
                diagnostics.Select(d => (d.Descriptor.Id, d.Severity)));
        }

        [Fact]
        public async Task ToStringWithNoParameter()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithParameter()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(""|}g{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(int someValue)
    {
        _ = $""prefix {someValue:g} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithEscapeSequences()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(""|}\\d \""d\""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue:\\d \""d\""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $@""prefix {someValue{|Unnecessary:[||].ToString(@""|}\d """"d""""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $@""prefix {someValue:\d """"d""""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithVerbatimEscapeSequencesInsideNonVerbatimInterpolatedString()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString(@""|}\d """"d""""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue:\\d \""d\""} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithNonVerbatimEscapeSequencesInsideVerbatimInterpolatedString()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(System.DateTime someValue)
    {
        _ = $@""prefix {someValue{|Unnecessary:[||].ToString(""|}\\d \""d\""{|Unnecessary:"")|}} suffix"";
    }
}",
@"class C
{
    void M(System.DateTime someValue)
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
    void M(System.DateTime someValue)
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
    void M(System.DateTime someValue)
    {
        _ = $""prefix {someValue[||].ToString(""some format code"", System.Globalization.CultureInfo.CurrentCulture)} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWithIntegerLiteral()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:)|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWithIntegerLiteral()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,-3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWithComplexConstantExpression()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        const int someConstant = 1;
        _ = $""prefix {someValue{|Unnecessary:[||].PadLeft(|}(byte)3.3 + someConstant{|Unnecessary:)|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        const int someConstant = 1;
        _ = $""prefix {someValue,(byte)3.3 + someConstant} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWithSpaceChar()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].PadLeft(|}3{|Unnecessary:, ' ')|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWithSpaceChar()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].PadRight(|}3{|Unnecessary:, ' ')|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,-3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWithNonSpaceChar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadLeft(3, '\t')} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWithNonSpaceChar()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadRight(3, '\t')} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWithComplexConstantExpressionRequiringParentheses()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        const int someConstant = 1;
        _ = $""prefix {someValue{|Unnecessary:[||].PadRight(|}(byte)3.3 + someConstant{|Unnecessary:)|}} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        const int someConstant = 1;
        _ = $""prefix {someValue,-((byte)3.3 + someConstant)} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithNoParameterWhenFormattingComponentIsSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].ToString():goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithStringLiteralParameterWhenFormattingComponentIsSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].ToString(""bar""):goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithNoParameterWhenAlignmentComponentIsSpecified()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|},3} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,3} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithNoParameterWhenBothComponentsAreSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].ToString(),3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithStringLiteralParameterWhenBothComponentsAreSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].ToString(""some format code""),3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWhenFormattingComponentIsSpecified()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadLeft(3):goo} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWhenFormattingComponentIsSpecified()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadRight(3):goo} suffix"";
    }
}",
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,-3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWhenAlignmentComponentIsSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadLeft(3),3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWhenAlignmentComponentIsSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadRight(3),3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftWhenBothComponentsAreSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadLeft(3),3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadRightWhenBothComponentsAreSpecified()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue[||].PadRight(3),3:goo} suffix"";
    }
}");
        }

        [Fact]
        public async Task ToStringWithoutFormatThenPadLeft()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|}{|Unnecessary:.PadLeft(|}3{|Unnecessary:)|}} suffix"";
    }
}", @"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue,3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftThenToStringWithoutFormat()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|}} suffix"";
    }
}", @"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3)} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftThenToStringWithoutFormatWhenAlignmentComponentIsSpecified()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].ToString()|},3} suffix"";
    }
}", @"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3),3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftThenPadRight_WithoutAlignment()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3){|Unnecessary:[||].PadRight(|}3{|Unnecessary:)|}} suffix"";
    }
}", @"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3),-3} suffix"";
    }
}");
        }

        [Fact]
        public async Task PadLeftThenPadRight_WithAlignment()
        {
            await TestMissingAsync(
@"class C
{
    void M(string someValue)
    {
        _ = $""prefix {someValue.PadLeft(3)[||].PadRight(3),3} suffix"";
    }
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnImplicitToStringReceiver()
        {
            await TestMissingAsync(
@"class C
{
    override string ToString() => ""Goobar"";

    string GetViaInterpolation() => $""Hello {ToString[||]()}"";
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnImplicitToStringReceiverWithArg()
        {
            await TestMissingAsync(
@"class C
{
    string ToString(string arg) => ""Goobar"";

    string GetViaInterpolation() => $""Hello {ToString[||](""g"")}"";
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnStaticToStringReceiver()
        {
            await TestMissingAsync(
@"class C
{
    public static string ToString() => ""Goobar"";

    string GetViaInterpolation() => $""Hello {ToString[||]()}"";
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnStaticToStringReceiverWithArg()
        {
            await TestMissingAsync(
@"class C
{
    public static string ToString(string arg) => ""Goobar"";

    string GetViaInterpolation() => $""Hello {ToString[||](""g"")}"";
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnImplicitPadLeft()
        {
            await TestMissingAsync(
@"class C
{
    public string PadLeft(int val) => """";

    void M(string someValue)
    {
        _ = $""prefix {[||]PadLeft(3)} suffix"";
    }
}");
        }

        [Fact, WorkItem(41381, "https://github.com/dotnet/roslyn/issues/41381")]
        public async Task MissingOnStaticPadLeft()
        {
            await TestMissingAsync(
@"class C
{
    public static string PadLeft(int val) => """";

    void M(string someValue)
    {
        _ = $""prefix {[||]PadLeft(3)} suffix"";
    }
}");
        }

        [Fact, WorkItem(42247, "https://github.com/dotnet/roslyn/issues/42247")]
        public async Task OnConstantAlignment1()
        {
            await TestInRegularAndScript1Async(
@"
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
            Console.WriteLine ($""{i}.{str[||].PadRight(maxLength, ' ')}"");
        }
    }
}",

@"
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
            Console.WriteLine ($""{i}.{str,-maxLength}"");
        }
    }
}");
        }

        [Fact, WorkItem(42247, "https://github.com/dotnet/roslyn/issues/42247")]
        public async Task MissingOnNonConstantAlignment()
        {
            await TestMissingAsync(
@"
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
            Console.WriteLine ($""{i}.{str[||].PadRight(maxLength, ' ')}"");
        }
    }
}");
        }

        [Fact, WorkItem(42669, "https://github.com/dotnet/roslyn/issues/42669")]
        public async Task MissingOnBaseToString()
        {
            await TestMissingAsync(
@"class C
{
    public override string ToString() => $""Test: {base[||].ToString()}"";
}");
        }

        [Fact, WorkItem(42669, "https://github.com/dotnet/roslyn/issues/42669")]
        public async Task MissingOnBaseToStringEvenWhenNotOverridden()
        {
            await TestMissingAsync(
@"class C
{
    string M() => $""Test: {base[||].ToString()}"";
}");
        }

        [Fact, WorkItem(42669, "https://github.com/dotnet/roslyn/issues/42669")]
        public async Task MissingOnBaseToStringWithArgument()
        {
            await TestMissingAsync(
@"class Base
{
    public string ToString(string format) => format;
}

class Derived : Base
{
    public override string ToString() => $""Test: {base[||].ToString(""a"")}"";
}");
        }

        [Fact, WorkItem(42669, "https://github.com/dotnet/roslyn/issues/42669")]
        public async Task PadLeftSimplificationIsStillOfferedOnBaseToString()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    public override string ToString() => $""Test: {base.ToString()[||].PadLeft(10)}"";
}",
@"class C
{
    public override string ToString() => $""Test: {base.ToString(),10}"";
}");
        }

        [Fact, WorkItem(42887, "https://github.com/dotnet/roslyn/issues/42887")]
        public async Task FormatComponentSimplificationIsNotOfferedOnNonIFormattableType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    string M(TypeNotImplementingIFormattable value) => $""Test: {value[||].ToString(""a"")}"";
}

struct TypeNotImplementingIFormattable
{
    public string ToString(string format) => ""A"";
}");
        }

        [Fact, WorkItem(42887, "https://github.com/dotnet/roslyn/issues/42887")]
        public async Task FormatComponentSimplificationIsOfferedOnIFormattableType()
        {
            await TestInRegularAndScript1Async(
@"using System;

class C
{
    string M(TypeImplementingIFormattable value) => $""Test: {value[||].ToString(""a"")}"";
}

struct TypeImplementingIFormattable : IFormattable
{
    public string ToString(string format) => ""A"";

    string IFormattable.ToString(string format, IFormatProvider formatProvider) => ""B"";
}",
@"using System;

class C
{
    string M(TypeImplementingIFormattable value) => $""Test: {value:a}"";
}

struct TypeImplementingIFormattable : IFormattable
{
    public string ToString(string format) => ""A"";

    string IFormattable.ToString(string format, IFormatProvider formatProvider) => ""B"";
}");
        }

        [Fact, WorkItem(42887, "https://github.com/dotnet/roslyn/issues/42887")]
        public async Task ParameterlessToStringSimplificationIsStillOfferedOnNonIFormattableType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    string M(TypeNotImplementingIFormattable value) => $""Test: {value[||].ToString()}"";
}

struct TypeNotImplementingIFormattable
{
    public string ToString(string format) => ""A"";
}",
@"class C
{
    string M(TypeNotImplementingIFormattable value) => $""Test: {value}"";
}

struct TypeNotImplementingIFormattable
{
    public string ToString(string format) => ""A"";
}");
        }

        [Fact, WorkItem(42887, "https://github.com/dotnet/roslyn/issues/42887")]
        public async Task PadLeftSimplificationIsStillOfferedOnNonIFormattableType()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    string M(TypeNotImplementingIFormattable value) => $""Test: {value.ToString(""a"")[||].PadLeft(10)}"";
}

struct TypeNotImplementingIFormattable
{
    public string ToString(string format) => ""A"";
}",
@"class C
{
    string M(TypeNotImplementingIFormattable value) => $""Test: {value.ToString(""a""),10}"";
}

struct TypeNotImplementingIFormattable
{
    public string ToString(string format) => ""A"";
}");
        }

        [Fact, WorkItem(42936, "https://github.com/dotnet/roslyn/issues/42936")]
        public async Task ToStringSimplificationIsNotOfferedOnRefStruct()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    string M(RefStruct someValue) => $""Test: {someValue[||].ToString()}"";
}

ref struct RefStruct
{
    public override string ToString() => ""A"";
}");
        }

        [Fact, WorkItem(42936, "https://github.com/dotnet/roslyn/issues/42936")]
        public async Task PadLeftSimplificationIsStillOfferedOnRefStruct()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    string M(RefStruct someValue) => $""Test: {someValue.ToString()[||].PadLeft(10)}"";
}

ref struct RefStruct
{
    public override string ToString() => ""A"";
}",
@"class C
{
    string M(RefStruct someValue) => $""Test: {someValue.ToString(),10}"";
}

ref struct RefStruct
{
    public override string ToString() => ""A"";
}");
        }

        [Fact, WorkItem(46011, "https://github.com/dotnet/roslyn/issues/46011")]
        public async Task ShadowedToString()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public new string ToString() => ""Shadow"";
    static string M(C c) => $""{c[||].ToString()}"";
}");
        }

        [Fact, WorkItem(46011, "https://github.com/dotnet/roslyn/issues/46011")]
        public async Task OverridenShadowedToString()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    public new string ToString() => ""Shadow"";
}

class B : C
{
    public override string ToString() => ""OverrideShadow"";
    static string M(C c) => $""{c[||].ToString()}"";
}");
        }

        [Fact, WorkItem(46011, "https://github.com/dotnet/roslyn/issues/46011")]
        public async Task DoubleOverridenToString()
        {
            await TestInRegularAndScript1Async(
@"class C
{
    public override string ToString() => ""Override"";
}

class B : C
{
    public override string ToString() => ""OverrideOverride"";

    void M(B someValue)
    {
        _ = $""prefix {someValue{|Unnecessary:[||].ToString()|}} suffix"";
    }
}",
@"class C
{
    public override string ToString() => ""Override"";
}

class B : C
{
    public override string ToString() => ""OverrideOverride"";

    void M(B someValue)
    {
        _ = $""prefix {someValue} suffix"";
    }
}");
        }
    }
}
