// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public sealed class AutomaticBracketCompletionTests : AbstractAutomaticBraceCompletionTests
{
    [WpfFact]
    public void Creation()
    {
        using var session = CreateSession("$$");
        Assert.NotNull(session);
    }

    [WpfFact]
    public void Attribute_TopLevel()
    {
        using var session = CreateSession("$$");
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Attribute_TopLevel2()
    {
        using var session = CreateSession("using System;$$");
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void InvalidLocation_String()
    {
        var code = """
            class C
            {
                string s = "$$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_String2()
    {
        var code = """
            class C
            {
                string s = @"
            $$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_Comment()
    {
        var code = """
            class C
            {
                //$$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_Comment2()
    {
        var code = """
            class C
            {
                /* $$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_Comment3()
    {
        var code = """
            class C
            {
                /// $$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_Comment4()
    {
        var code = """
            class C
            {
                /** $$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void MultiLine_Comment()
    {
        var code = """
            class C
            {
                void Method()
                {
                    /* */$$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void MultiLine_DocComment()
    {
        var code = """
            class C
            {
                void Method()
                {
                    /** */$$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void String1()
    {
        var code = """
            class C
            {
                void Method()
                {
                    var s = ""$$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void String2()
    {
        var code = """
            class C
            {
                void Method()
                {
                    var s = @""$$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Attribute_OpenBracket()
    {
        var code = """
            $$
            class C { }
            """;

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Attribute_OpenBracket_Delete()
    {
        var code = """
            $$
            class C { }
            """;

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckBackspace(session.Session);
    }

    [WpfFact]
    public void Attribute_OpenBracket_Tab()
    {
        var code = """
            $$
            class C { }
            """;

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckTab(session.Session);
    }

    [WpfFact]
    public void Attribute_OpenBracket_CloseBracket()
    {
        var code = """
            $$
            class C { }
            """;

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckOverType(session.Session);
    }

    [WpfFact]
    public void Array_Multiple_Invalid()
    {
        var code = """
            class C 
            {
                int [$$]
            }
            """;

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Array_Nested()
    {
        var code = """
            class C
            {
                int [] i = new int [arr$$]
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void ListPattern()
    {
        var code = """
            class C
            {
                void M(object o)
                {
                    _ = o is$$
                }
            }
            """;
        using var session = CreateSession(code);
        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M(object o)
                {
                    _ = o is []
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M(object o)
                {
                    _ = o is
                    [

                    ]
                }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69993")]
    public void IndexerInSwitchSection1()
    {
        var code = """
        class Program
        {
            private void M()
            {
                switch (variable.Name)
                {
                    case "v": myarray$$
                }
            }
        }
        """;
        using var session = CreateSession(code);
        CheckStart(session.Session);
        CheckText(session.Session, """
        class Program
        {
            private void M()
            {
                switch (variable.Name)
                {
                    case "v": myarray[]
                }
            }
        }
        """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/69993")]
    public void IndexerInSwitchSection2()
    {
        var code = """
        class Program
        {
            private void M()
            {
                switch (variable.Name)
                {
                    case "w":
                    case "v": myarray$$
                }
            }
        }
        """;
        using var session = CreateSession(code);
        CheckStart(session.Session);
        CheckText(session.Session, """
        class Program
        {
            private void M()
            {
                switch (variable.Name)
                {
                    case "w":
                    case "v": myarray[]
                }
            }
        }
        """);
    }

    internal static Holder CreateSession(string code)
    {
        return CreateSession(
            EditorTestWorkspace.CreateCSharp(code),
            Bracket.OpenCharacter, Bracket.CloseCharacter);
    }
}
