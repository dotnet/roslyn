// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

public class ComponentDiagnosticRazorIntegrationTest : RazorIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;

    internal override bool UseTwoPhaseCompilation => true;

    [Fact]
    public void RejectsEndTagWithNoStartTag()
    {
        // Arrange/Act
        var result = CompileToCSharp(
            "Line1\nLine2\nLine3</mytag>");

        // Assert
        Assert.Collection(result.RazorDiagnostics,
            item =>
            {
                Assert.Equal("RZ9981", item.Id);
                Assert.Equal("Unexpected closing tag 'mytag' with no matching start tag.", item.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    // This used to be a sugar syntax for lambdas, but we don't support that anymore
    [Fact]
    public void OldCodeBlockAttributeSyntax_ReportsError()
    {
        // Arrange/Act
        var generated = CompileToCSharp(@"
<elem attr=@{ DidInvokeCode = true; } />
@functions {
    public bool DidInvokeCode { get; set; } = false;
}");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ9979", diagnostic.Id);
        Assert.NotNull(diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void RejectsTagHelperDirectives()
    {
        // Arrange/Act
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));

        var result = CompileToCSharp(@"
@addTagHelper *, TestAssembly
@tagHelperPrefix th

<MyComponent />
");

        // Assert
        Assert.Collection(result.RazorDiagnostics,
            item =>
            {
                Assert.Equal("RZ9978", item.Id);
                Assert.Equal("""
            The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. Use '@using <namespace>' directive instead.
            """, item.GetMessage(CultureInfo.CurrentCulture));
                Assert.Equal(0, item.Span.LineIndex);
                Assert.Equal(0, item.Span.CharacterIndex);
            },
            item =>
            {
                Assert.Equal("RZ9978", item.Id);
                Assert.Equal("""
            The directives @addTagHelper, @removeTagHelper and @tagHelperPrefix are not valid in a component document. Use '@using <namespace>' directive instead.
            """, item.GetMessage(CultureInfo.CurrentCulture));
                Assert.Equal(1, item.Span.LineIndex);
                Assert.Equal(0, item.Span.CharacterIndex);
            });
    }

    [Fact]
    public void RejectsEmptyTagHelperDirectives()
    {
        // Even with no content, @addTagHelper/@removeTagHelper/@tagHelperPrefix still report RZ9978
        // ("not valid in a component document"), and the diagnostic spans the directive itself (char 0)
        // rather than the (empty) directive content.
        var result = CompileToCSharp("""
            @addTagHelper
            @removeTagHelper
            @tagHelperPrefix
            """);

        Assert.Contains(result.RazorDiagnostics, static d => d.Id == "RZ9978" && d.Span.LineIndex == 0 && d.Span.CharacterIndex == 0);
        Assert.Contains(result.RazorDiagnostics, static d => d.Id == "RZ9978" && d.Span.LineIndex == 1 && d.Span.CharacterIndex == 0);
        Assert.Contains(result.RazorDiagnostics, static d => d.Id == "RZ9978" && d.Span.LineIndex == 2 && d.Span.CharacterIndex == 0);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7271")]
    public void Component_RazorCommentInStartTagAttributeArea_IsIgnored()
    {
        // Arrange
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test
            {
                public class MyComponent : ComponentBase
                {
                    [Parameter] public string Parameter1 { get; set; }
                    [Parameter] public bool Parameter2 { get; set; }
                    [Parameter] public string Parameter3 { get; set; }
                }
            }
            """));

        // Act
        var generated = CompileToCSharp("""
            <MyComponent Parameter1="SomeValue"
                Parameter2="@true" @* NOTE: this does not work! *@
                Parameter3="SomeOtherValue" />
            """);

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
        Assert.DoesNotContain("NOTE: this does not work", generated.Code);
        Assert.Equal(3, generated.Code.Split(
            new[] { "AddComponentParameter" },
            global::System.StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void DirectiveAttribute_ComplexContent_ReportsError()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<input type=""text"" @key=""Foo @Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ9986", diagnostic.Id);
        Assert.Equal(
            "Component attributes do not support complex content (mixed C# and markup). Attribute: '@key', text: 'Foo @Text'",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_OnElement_Level10_DoesNotReportWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            """<div @unknown="value"></div>""",
            configuration: Configuration with { RazorWarningLevel = 10 });

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
        Assert.Contains("AddMarkupContent", generated.Code);
        Assert.Contains("""<div @unknown=\"value\"></div>""", generated.Code);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_OnElement_Level11_ReportsWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            """<div @unknown="value"></div>""",
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        AssertUnboundDirectiveAttributeDiagnostic(Assert.Single(generated.RazorDiagnostics), "@unknown");
        Assert.Contains("AddMarkupContent", generated.Code);
        Assert.Contains("""<div @unknown=\"value\"></div>""", generated.Code);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_OnComponent_Level10_DoesNotReportWarning()
    {
        // Arrange
        AddTestComponent();

        // Act
        var generated = CompileToCSharp(
            """<MyComponent @unknown="value" />""",
            configuration: Configuration with { RazorWarningLevel = 10 });

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
        Assert.Contains("AddComponentParameter", generated.Code);
        Assert.Contains("\"@unknown\"", generated.Code);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_OnComponent_Level11_ReportsWarning()
    {
        // Arrange
        AddTestComponent();

        // Act
        var generated = CompileToCSharp(
            """<MyComponent @unknown="value" />""",
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        AssertUnboundDirectiveAttributeDiagnostic(Assert.Single(generated.RazorDiagnostics), "@unknown");
        Assert.Contains("AddComponentParameter", generated.Code);
        Assert.Contains("\"@unknown\"", generated.Code);
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    [InlineData("@onclickx=\"true\"", "@onclickx")]
    [InlineData("@onclick:unknown=\"true\"", "@onclick:unknown")]
    [InlineData("@ref:suppressField", "@ref:suppressField")]
    public void UnboundDirectiveAttribute_MalformedOrNearMatch_ReportsWarning(string attribute, string attributeName)
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            $"""
            @using Microsoft.AspNetCore.Components.Web
            <div {attribute}></div>
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        AssertUnboundDirectiveAttributeDiagnostic(Assert.Single(generated.RazorDiagnostics), attributeName);
    }

    [Theory, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    [InlineData("<div @@unknown=\"value\"></div>")]
    [InlineData("<div @@unknown></div>")]
    public void UnboundDirectiveAttribute_EscapedName_DoesNotReportWarning(string content)
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_OnEscapedElement_DoesNotReportWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            """<!div @unknown="value"></!div>""",
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7650")]
    public void UnboundDirectiveAttribute_AlreadyBoundDirectives_DoNotReportWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(
            """
            @using Microsoft.AspNetCore.Components.Web
            <div @onclick="HandleClick"
                 @onclick:preventDefault="true"
                 @ref="element"
                 @key="key"
                 @attributes="attributes"></div>
            <input @bind="value" />
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact]
    public void Component_StartsWithLowerCase_ReportsError()
    {
        // Arrange & Act
        var generated = CompileToCSharp("lowerCase.razor", cshtmlContent: @"
<input type=""text"" @bind=""Text"" />
@functions {
    public string Text { get; set; } = ""text"";
}");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10011", diagnostic.Id);
        Assert.Equal(
            "Component 'lowerCase' starts with a lowercase character. Component names cannot start with a lowercase character.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Component_NotFound_ReportsWarning(bool supportLocalizedComponentNames)
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<PossibleComponent></PossibleComponent>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: supportLocalizedComponentNames);

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10012", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "Found markup element with unexpected name 'PossibleComponent'. If this is intended to be a component, add a @using directive for its namespace.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Component_NotFound_StartsWithOtherLetter_WhenLocalizedComponentNamesIsAllowed_ReportsWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<繁体字></繁体字>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: true);

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10012", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "Found markup element with unexpected name '繁体字'. If this is intended to be a component, add a @using directive for its namespace.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Component_NotFound_StartsWithOtherLetter_WhenLocalizedComponentNamesIsDisallowed()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<繁体字></繁体字>

@functions {
    public string Text { get; set; } = ""text"";
}", supportLocalizedComponentNames: false);

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact]
    public void Element_DoesNotStartWithLowerCase_OverrideWithBang_NoWarning()
    {
        // Arrange & Act
        var generated = CompileToCSharp(@"
<!PossibleComponent></!PossibleComponent>");

        // Assert
        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact]
    public void Component_StartAndEndTagCaseMismatch_ReportsError()
    {
        // Arrange & Act
        AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
    }
}
"));
        var generated = CompileToCSharp(@"
<MyComponent></mycomponent>");

        // Assert
        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10013", diagnostic.Id);
        Assert.Equal(
            "The start tag name 'MyComponent' does not match the end tag name 'mycomponent'. Components must have matching start and end tag names (case-sensitive).",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_UnknownParameter_RazorWarningLevel10_DoesNotWarn()
    {
        AddGenericComponentWithKnownParameter();

        const string content = """<MyComponent TValue="string" Unknown="value" />""";

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 10 });

        Assert.Empty(generated.RazorDiagnostics);
        AssertUnknownParameterIsGenerated(generated.Code);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_UnknownParameter_RazorWarningLevel11_Warns()
    {
        AddGenericComponentWithKnownParameter();

        const string content = """<MyComponent TValue="string" Unknown="value" />""";

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10025", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(
            "The component 'MyComponent' does not have a parameter named 'Unknown'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        var unknownParameterIndex = content.IndexOf("Unknown", StringComparison.Ordinal);
        Assert.NotNull(diagnostic.Span.FilePath);
        Assert.Equal(unknownParameterIndex, diagnostic.Span.AbsoluteIndex);
        Assert.Equal(unknownParameterIndex, diagnostic.Span.CharacterIndex);
        Assert.Equal("Unknown".Length, diagnostic.Span.Length);
        AssertUnknownParameterIsGenerated(generated.Code);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_MultipleUnknownParameters_Warn()
    {
        AddGenericComponentWithKnownParameter();

        const string content = """<MyComponent TValue="string" FirstUnknown="value" SecondUnknown="value" />""";

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Collection(
            generated.RazorDiagnostics,
            diagnostic => AssertDiagnostic(diagnostic, "FirstUnknown"),
            diagnostic => AssertDiagnostic(diagnostic, "SecondUnknown"));
        Assert.Contains("\"FirstUnknown\"", generated.Code);
        Assert.Contains("\"SecondUnknown\"", generated.Code);
        Assert.Equal(2, generated.Code.Split(
            ["AddComponentParameter"],
            StringSplitOptions.None).Length - 1);

        void AssertDiagnostic(RazorDiagnostic diagnostic, string parameterName)
        {
            Assert.Equal("RZ10025", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(11, diagnostic.WarningLevel);
            Assert.Equal(
                $"The component 'MyComponent' does not have a parameter named '{parameterName}'.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
            var parameterIndex = content.IndexOf(parameterName, StringComparison.Ordinal);
            Assert.NotNull(diagnostic.Span.FilePath);
            Assert.Equal(parameterIndex, diagnostic.Span.AbsoluteIndex);
            Assert.Equal(parameterIndex, diagnostic.Span.CharacterIndex);
            Assert.Equal(parameterName.Length, diagnostic.Span.Length);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_KnownAndUnknownParameters_Warns()
    {
        AddGenericComponentWithKnownParameter();

        const string content = """<MyComponent TValue="string" Known="known" Unknown="unknown" />""";

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10025", diagnostic.Id);
        Assert.Equal(
            "The component 'MyComponent' does not have a parameter named 'Unknown'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Contains("\"Unknown\"", generated.Code);
        Assert.Equal(2, generated.Code.Split(
            ["AddComponentParameter"],
            StringSplitOptions.None).Length - 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_KnownAndMultipleUnknownParameters_Warn()
    {
        AddGenericComponentWithKnownParameter();

        const string content = """<MyComponent TValue="string" Known="known" FirstUnknown="first" SecondUnknown="second" />""";

        var generated = CompileToCSharp(
            content,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Collection(
            generated.RazorDiagnostics,
            diagnostic => AssertDiagnostic(diagnostic, "FirstUnknown"),
            diagnostic => AssertDiagnostic(diagnostic, "SecondUnknown"));
        Assert.Contains("\"FirstUnknown\"", generated.Code);
        Assert.Contains("\"SecondUnknown\"", generated.Code);
        Assert.Equal(3, generated.Code.Split(
            ["AddComponentParameter"],
            StringSplitOptions.None).Length - 1);

        void AssertDiagnostic(RazorDiagnostic diagnostic, string parameterName)
        {
            Assert.Equal("RZ10025", diagnostic.Id);
            Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal(11, diagnostic.WarningLevel);
            Assert.Equal(
                $"The component 'MyComponent' does not have a parameter named '{parameterName}'.",
                diagnostic.GetMessage(CultureInfo.CurrentCulture));
            var parameterIndex = content.IndexOf(parameterName, StringComparison.Ordinal);
            Assert.NotNull(diagnostic.Span.FilePath);
            Assert.Equal(parameterIndex, diagnostic.Span.AbsoluteIndex);
            Assert.Equal(parameterIndex, diagnostic.Span.CharacterIndex);
            Assert.Equal(parameterName.Length, diagnostic.Span.Length);
        }
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_KnownParameter_WithDifferentCasing_DoesNotWarn()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public abstract class ComponentBaseWithParameter : ComponentBase
            {
                [Parameter] public string Known { get; set; }
            }

            public class MyComponent : ComponentBaseWithParameter
            {
            }
            """));

        var generated = CompileToCSharp(
            """
            <MyComponent Known="first" />
            <MyComponent known="second" />
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_ValidInheritedCaptureUnmatchedValuesParameter_DoesNotWarn()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public abstract class ComponentBaseWithAdditionalAttributes<TValue> : ComponentBase
            {
                [Parameter] public TValue Known { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }
            }

            public class MyComponent : ComponentBaseWithAdditionalAttributes<string>
            {
            }
            """));

        var generated = CompileToCSharp(
            """<MyComponent Known="value" Unknown="value" />""",
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_InvalidCaptureUnmatchedValuesParameters_Warn()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class CaptureDisabled : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = false)]
                public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }
            }

            public class WrongType : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public string AdditionalAttributes { get; set; }
            }

            public class DuplicateCapture : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; }

                [Parameter(CaptureUnmatchedValues = true)]
                public IDictionary<string, object> OtherAttributes { get; set; }
            }

            public class UserDefinedConversionCapture : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public AttributeCollection AdditionalAttributes { get; set; }
            }

            public class AttributeCollection
            {
                public static implicit operator AttributeCollection(Dictionary<string, object> attributes)
                    => new();
            }
            """));

        var generated = CompileToCSharp(
            """
            <CaptureDisabled Unknown="value" />
            <WrongType Unknown="value" />
            <DuplicateCapture Unknown="value" />
            <UserDefinedConversionCapture Unknown="value" />
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Collection(
            generated.RazorDiagnostics,
            diagnostic =>
            {
                Assert.Equal("RZ10025", diagnostic.Id);
                Assert.Equal(
                    "The component 'CaptureDisabled' does not have a parameter named 'Unknown'.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            },
            diagnostic =>
            {
                Assert.Equal("RZ10025", diagnostic.Id);
                Assert.Equal(
                    "The component 'WrongType' does not have a parameter named 'Unknown'.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            },
            diagnostic =>
            {
                Assert.Equal("RZ10025", diagnostic.Id);
                Assert.Equal(
                    "The component 'DuplicateCapture' does not have a parameter named 'Unknown'.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            },
            diagnostic =>
            {
                Assert.Equal("RZ10025", diagnostic.Id);
                Assert.Equal(
                    "The component 'UserDefinedConversionCapture' does not have a parameter named 'Unknown'.",
                    diagnostic.GetMessage(CultureInfo.CurrentCulture));
            });
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_HtmlSplatAndDirectiveAttributes_DoNotWarn()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
            }
            """));

        var generated = CompileToCSharp(
            """
            @using System.Collections.Generic

            <div class="value" data-unknown="value"></div>
            <MyComponent @attributes="new Dictionary<string, object>()" @key="this" @ref="_component" />

            @code {
                private MyComponent _component = default!;
            }
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        Assert.Empty(generated.RazorDiagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11114")]
    public void Component_UnknownParameter_WithSplat_Warns()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
            }
            """));

        var generated = CompileToCSharp(
            """
            @using System.Collections.Generic

            <MyComponent @attributes="new Dictionary<string, object>()" Unknown="value" />
            """,
            configuration: Configuration with { RazorWarningLevel = 11 });

        var diagnostic = Assert.Single(generated.RazorDiagnostics);
        Assert.Equal("RZ10025", diagnostic.Id);
        Assert.Equal(
            "The component 'MyComponent' does not have a parameter named 'Unknown'.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
    }

    private void AddGenericComponentWithKnownParameter()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent<TValue> : ComponentBase
            {
                [Parameter] public TValue Known { get; set; }
            }
            """));
    }

    private static void AssertUnknownParameterIsGenerated(string code)
    {
        Assert.Contains("\"Unknown\"", code);
        Assert.Equal(1, code.Split(
            ["AddComponentParameter"],
            StringSplitOptions.None).Length - 1);
    }

    private void AddTestComponent()
    {
        AdditionalSyntaxTrees.Add(Parse("""
            using Microsoft.AspNetCore.Components;

            namespace Test;

            public class MyComponent : ComponentBase
            {
                [Parameter]
                public string Value { get; set; }
            }
            """));
    }

    private static void AssertUnboundDirectiveAttributeDiagnostic(RazorDiagnostic diagnostic, string attributeName)
    {
        Assert.Equal("RZ10028", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            $"The attribute '{attributeName}' could not be bound to any directive attribute.",
            diagnostic.GetMessage(CultureInfo.CurrentCulture));
        Assert.Equal(attributeName.Length, diagnostic.Span.Length);
    }
}
