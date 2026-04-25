// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Extensions;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class CSharpFunctionsTest() : ParserTestBase(layer: TestProject.Layer.Compiler, validateSpanEditHandlers: true, useLegacyTokenizer: true)
{
    [Fact]
    public void Functions_SingleLineControlFlowStatement_Error()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    string GetAnnouncmentText(string message)
    {
        if (message.Length > 0) <p>Message: @message</p>

        if (message == null)
            // Nothing to render
            <p>Message was null</p>

        if (DateTime.Now.ToBinary() % 2 == 0)
            @: <p>The time: @time</p>

        if (message != null) @@SomeGitHubUserName <strong>@message</strong>
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void Functions_SingleLineControlFlowStatement()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    string GetAnnouncmentText(string message)
    {
        if (message.Length > 0) return ""Anouncement: "" + message;
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_DoesNotParseWhenNotSupported()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_2_1,
            @"
@functions {
    void Announcment(string message)
    {
        <h3>@message</h3>
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_ParsesMarkupInsideMethod()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message)
    {
        <h3>@message</h3>
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    // This will parse correctly in Razor, but will generate invalid C#.
    [Fact]
    public void MarkupInFunctionsBlock_ParsesMarkupWithExpressionsMethod()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message) => <h3>@message</h3>
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_DoesNotParseMarkupInString()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message) => ""<h3>@message</h3>"";
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_DoesNotParseMarkupInVerbatimString()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message) => @""<h3>@message</h3>"";
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_CanContainCurlyBraces()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message)
    {
        <div>
            @if (message.Length > 0)
            {
                <p>@message.Length</p>
            }
        </div>
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void MarkupInFunctionsBlock_MarkupCanContainTemplate()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    void Announcment(string message)
    {
        <div>
            @if (message.Length > 0)
            {
                Repeat(@<p>@message.Length</p>);
            }
        </div>
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }

    [Fact]
    public void ReservedKeywordsInFunctionsBlock_WithMarkup()
    {
        ParseDocumentTest(
            RazorLanguageVersion.Version_3_0,
            @"
@functions {
    class Person
    {
        public void DoSomething()
        {
            <p>Just do it!</p>
        }
    }
}
", [FunctionsDirective.Directive], designTime: false);
    }
}
