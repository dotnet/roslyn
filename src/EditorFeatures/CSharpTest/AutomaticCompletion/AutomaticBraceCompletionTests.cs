// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion;

[Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
public sealed class AutomaticBraceCompletionTests : AbstractAutomaticBraceCompletionTests
{
    [WpfFact]
    public void WithExpressionBracesSameLine()
    {
        var code = """
            class C
            {
                void M(C c)
                {
                    c = c with $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M(C c)
                {
                    c = c with { }
                }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47381")]
    public void ImplicitObjectCreationExpressionBracesSameLine()
    {
        var code = """
            class C
            {
                void M(C c)
                {
                    c = new() $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M(C c)
                {
                    c = new() { }
                }
            }
            """);
    }

    [WpfFact]
    public void WithExpressionBracesSameLine_Enter()
    {
        var code = """
            class C
            {
                void M(C c)
                {
                    c = c with $$
                }
            }
            """;
        using var session = CreateSession(code);
        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M(C c)
                {
                    c = c with
                    {

                    }
                }
            }
            """);
    }

    [WpfFact]
    public void Creation()
    {
        using var session = CreateSession("$$");
        Assert.NotNull(session);
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
    public void ValidLocation_InterpolatedString1()
    {
        var code = """
            class C
            {
                string s = $"$$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString2()
    {
        var code = """
            class C
            {
                string s = $@"$$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString3()
    {
        var code = """
            class C
            {
                string x = "goo"
                string s = $"{x} $$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString4()
    {
        var code = """
            class C
            {
                string x = "goo"
                string s = $@"{x} $$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString5()
    {
        var code = """
            class C
            {
                string s = $"{{$$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString6()
    {
        var code = """
            class C
            {
                string s = $"{}$$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
    }

    [WpfFact]
    public void ValidLocation_InterpolatedString7()
    {
        var code = """
            class C
            {
                string s = $"{}$$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);
        CheckStart(session.Session);
        CheckReturn(session.Session, 0, """
            class C
            {
                string s = $"{}{
            }
            }
            """);
    }

    [WpfFact]
    public void InvalidLocation_InterpolatedString1()
    {
        var code = """
            class C
            {
                string s = @"$$
            }
            """;
        using var session = CreateSession(code);
        Assert.Null(session);
    }

    [WpfFact]
    public void InvalidLocation_InterpolatedString2()
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
    public void Class_OpenBrace()
    {
        var code = @"class C $$";

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Class_Delete()
    {
        var code = @"class C $$";

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckBackspace(session.Session);
    }

    [WpfFact]
    public void Class_Tab()
    {
        var code = @"class C $$";

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckTab(session.Session);
    }

    [WpfFact]
    public void Class_CloseBrace()
    {
        var code = @"class C $$";

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckOverType(session.Session);
    }

    [WpfFact]
    public void Method_OpenBrace_Multiple()
    {
        var code = """
            class C
            {
                void Method() { $$
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
    }

    [WpfFact]
    public void Class_OpenBrace_Enter()
    {
        var code = @"class C $$";

        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 4);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/47438")]
    public void WithExpression()
    {
        var code = """
            record C
            {
                void M()
                {
                    _ = this with $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            record C
            {
                void M()
                {
                    _ = this with { }
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            record C
            {
                void M()
                {
                    _ = this with
                    {

                    }
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is { }
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M()
                {
                    _ = this is
                    {

                    }
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern_Nested()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is { Name: $$ }
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is { Name: { } }
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M()
                {
                    _ = this is { Name:
                    {

                    } }
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern_Parentheses1()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is { Name: $$ }
                }
            }
            """;
        using var session = CreateSession(EditorTestWorkspace.CreateCSharp(code), '(', ')');
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is { Name: () }
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern_Parentheses2()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is { Name: { Length: (> 3) and $$ } }
                }
            }
            """;
        using var session = CreateSession(EditorTestWorkspace.CreateCSharp(code), '(', ')');
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is { Name: { Length: (> 3) and () } }
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern_FollowedByInvocation()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is $$
                    M();
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is { }
                    M();
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M()
                {
                    _ = this is
                    {

                    }
                    M();
                }
            }
            """);
    }

    [WpfFact]
    public void RecursivePattern_WithInvocation_FollowedByInvocation()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this is (1, 2) $$
                    M();
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this is (1, 2) { }
                    M();
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M()
                {
                    _ = this is (1, 2)
                    {

                    }
                    M();
                }
            }
            """);
    }

    [WpfFact]
    public void SwitchExpression()
    {
        var code = """
            class C
            {
                void M()
                {
                    _ = this switch $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    _ = this switch { }
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            class C
            {
                void M()
                {
                    _ = this switch
                    {

                    }
                }
            }
            """);
    }

    [WpfFact]
    public void Class_ObjectInitializer_OpenBrace_Enter()
    {
        var code = """
            using System.Collections.Generic;

            class C
            {
                List<C> list = new List<C>
                {
                    new C $$
                };
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            using System.Collections.Generic;

            class C
            {
                List<C> list = new List<C>
                {
                    new C
                    {

                    }
                };
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void Collection_Initializer_OpenBraceOnSameLine_Enter()
    {
        var code = """
            using System.Collections.Generic;

            class C
            {
                public void man()
                {
                    List<C> list = new List<C> $$
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) }
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            using System.Collections.Generic;

            class C
            {
                public void man()
                {
                    List<C> list = new List<C> {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void Collection_Initializer_OpenBraceOnDifferentLine_Enter()
    {
        var code = """
            using System.Collections.Generic;

            class C
            {
                public void man()
                {
                    List<C> list = new List<C> $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            using System.Collections.Generic;

            class C
            {
                public void man()
                {
                    List<C> list = new List<C>
                    {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void Object_Initializer_OpenBraceOnSameLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    var goo = new Goo $$
                }
            }

            class Goo
            {
                public int bar;
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) }
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    var goo = new Goo {

                    }
                }
            }

            class Goo
            {
                public int bar;
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void Object_Initializer_OpenBraceOnDifferentLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    var goo = new Goo $$
                }
            }

            class Goo
            {
                public int bar;
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    var goo = new Goo
                    {

                    }
                }
            }

            class Goo
            {
                public int bar;
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayImplicit_Initializer_OpenBraceOnSameLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = $$
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) }
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr = {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayImplicit_Initializer_OpenBraceOnDifferentLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr =
                    {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayExplicit1_Initializer_OpenBraceOnSameLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = new[] $$
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) }
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr = new[] {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayExplicit1_Initializer_OpenBraceOnDifferentLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = new[] $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr = new[]
                    {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayExplicit2_Initializer_OpenBraceOnSameLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = new int[] $$
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, false) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr = new int[] {

                    }
                }
            }
            """);
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
    public void ArrayExplicit2_Initializer_OpenBraceOnDifferentLine_Enter()
    {
        var code = """
            class C
            {
                public void man()
                {
                    int[] arr = new int[] $$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, """
            class C
            {
                public void man()
                {
                    int[] arr = new int[]
                    {

                    }
                }
            }
            """);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/3447")]
    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850540")]
    public void BlockIndentationWithAutomaticBraceFormattingDisabled()
    {
        var code = """
            class C
            {
                public void X()
                $$
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnCloseBrace, false },
            { FormattingOptions2.SmartIndent, FormattingOptions2.IndentStyle.Block },
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            class C
            {
                public void X()
                {}
            }
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

        CheckReturn(session.Session, 4, """
            class C
            {
                public void X()
                {

                }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2224")]
    public void NoSmartOrBlockIndentationWithAutomaticBraceFormattingDisabled()
    {
        var code = """
            namespace NS1
            {
                public class C1
            $$
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { FormattingOptions2.SmartIndent, FormattingOptions2.IndentStyle.None },
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            namespace NS1
            {
                public class C1
            { }
            }
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2330")]
    public void BlockIndentationWithAutomaticBraceFormatting()
    {
        var code = """
            namespace NS1
            {
                    public class C1
                    $$
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { FormattingOptions2.SmartIndent, FormattingOptions2.IndentStyle.Block },
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            namespace NS1
            {
                    public class C1
                    { }
            }
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

        CheckReturn(session.Session, 8, """
            namespace NS1
            {
                    public class C1
                    {

                    }
            }
            """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2330")]
    public void BlockIndentationWithAutomaticBraceFormattingSecondSet()
    {
        var code = """
            namespace NS1
            {
                    public class C1
                    { public class C2 $$

                    }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { FormattingOptions2.SmartIndent, FormattingOptions2.IndentStyle.Block },
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            namespace NS1
            {
                    public class C1
                    { public class C2 { }

                    }
            }
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

        CheckReturn(session.Session, 8, """
            namespace NS1
            {
                    public class C1
                    { public class C2 {

                    }

                    }
            }
            """);
    }

    [WpfFact]
    public void DoesNotFormatInsideBracePairInInitializers()
    {
        var code = """
            class C
            {
                void M()
                {
                    var x = new int[]$$
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            class C
            {
                void M()
                {
                    var x = new int[] {}
                }
            }
            """);
    }

    [WpfFact]
    public void DoesNotFormatOnReturnWithNonWhitespaceInBetween()
    {
        var code = @"class C $$";
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Type(session.Session, "dd");
        CheckReturn(session.Session, 0, """
            class C { dd
            }
            """);
    }

    [WpfFact]
    public void CurlyBraceFormattingInsideLambdaInsideInterpolation()
    {
        var code = """
            class C
            {
                void M(string[] args)
                {
                    var s = $"{ args.Select(a => $$)}"
                }
            }
            """;
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            class C
            {
                void M(string[] args)
                {
                    var s = $"{ args.Select(a => { })}"
                }
            }
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());
    }

    [WpfFact]
    public void CurlyBraceFormatting_DoesNotAddNewLineWhenAlreadyExists()
    {
        var code = @"class C $$";
        using var session = CreateSession(code);
        Assert.NotNull(session);

        CheckStart(session.Session);

        // Sneakily insert a new line between the braces.
        var buffer = session.Session.SubjectBuffer;
        buffer.Insert(10, Environment.NewLine);

        CheckReturn(session.Session, 4, """
            class C
            {

            }
            """);
    }

    [WpfFact]
    public void CurlyBraceFormatting_InsertsCorrectNewLine()
    {
        var code = @"class C $$";

        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { FormattingOptions2.NewLine, "\r" }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 4, result: "class C\r{\r\r}");
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void WithInitializer_Enter(bool bracesOnNewLine)
    {
        var code = """
            record R
            {
                public void man(R r)
                {
                    var r2 = r with $$
                }
            }
            """;
        var expected = bracesOnNewLine ? """
            record R
            {
                public void man(R r)
                {
                    var r2 = r with
                    {

                    }
                }
            }
            """ : """
            record R
            {
                public void man(R r)
                {
                    var r2 = r with {

                    }
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void PropertyPatternClause_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public void man()
                {
                    if (x is string $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public void man()
                {
                    if (x is string
                        {

                        }
                }
            }
            """ : """
            class C
            {
                public void man()
                {
                    if (x is string {

                    }
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ObjectCollectionArrayInitializers, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, bracesOnNewLine ? 16 : 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void Accessor_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public int I
                {
                    get $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public int I
                {
                    get
                    {

                    }
                }
            }
            """ : """
            class C
            {
                public int I
                {
                    get {

                    }
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.Accessors, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void AnonymousMethod_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public void man()
                {
                    Action a = delegate() $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public void man()
                {
                    Action a = delegate()
                    {

                    }
                }
            }
            """ : """
            class C
            {
                public void man()
                {
                    Action a = delegate() {

                    }
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.AnonymousMethods, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void AnonymousType_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public void man()
                {
                    var x = new $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public void man()
                {
                    var x = new
                    {

                    }
                }
            }
            """ : """
            class C
            {
                public void man()
                {
                    var x = new {

                    }
                }
            }
            """;
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.AnonymousTypes, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void If_OpenBraceOnSameLine_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public void man()
                {
                    if (true) $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public void man()
                {
                    if (true)
                    {

                    }
                }
            }
            """ : """
            class C
            {
                public void man()
                {
                    if (true) {

                    }
                }
            }
            """;

        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WorkItem("https://github.com/dotnet/roslyn/issues/50275")]
    [WpfTheory, CombinatorialData]
    public void Else_OpenBraceOnSameLine_Enter(bool bracesOnNewLine)
    {
        var code = """
            class C
            {
                public void man()
                {
                    if (true) {
                    }
                    else $$
                }
            }
            """;

        var expected = bracesOnNewLine ? """
            class C
            {
                public void man()
                {
                    if (true) {
                    }
                    else
                    {

                    }
                }
            }
            """ : """
            class C
            {
                public void man()
                {
                    if (true) {
                    }
                    else {

                    }
                }
            }
            """;

        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { CSharpFormattingOptions2.NewLineBeforeOpenBrace, CSharpFormattingOptions2.NewLineBeforeOpenBrace.DefaultValue.WithFlagValue(NewLineBeforeOpenBracePlacement.ControlBlocks, bracesOnNewLine) }
        };
        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckReturn(session.Session, 12, expected);
    }

    [WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1758005")]
    public void NoFormattingAfterNewlineIfOptionsDisabled()
    {
        var code = """
            namespace NS1
            $$
            """;

        // Those option ensures no additional formatting would happen around added braces, including indention of added newline
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { FormattingOptions2.SmartIndent, FormattingOptions2.IndentStyle.None },
            { AutoFormattingOptionsStorage.FormatOnCloseBrace, false },
        };

        using var session = CreateSession(code, globalOptions);
        Assert.NotNull(session);

        CheckStart(session.Session);
        Assert.Equal("""
            namespace NS1
            {}
            """, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

        CheckReturn(session.Session, 0, """
            namespace NS1
            {

            }
            """);
    }

    [WpfFact]
    public void ModernExtension()
    {
        var code = """
            namespace N
            {
                static class C
                {
                    extension(string s) $$
                }
            }
            """;
        using var session = CreateSession(code, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp14));
        Assert.NotNull(session);

        CheckStart(session.Session);
        CheckText(session.Session, """
            namespace N
            {
                static class C
                {
                    extension(string s) { }
                }
            }
            """);
        CheckReturn(session.Session, 12, """
            namespace N
            {
                static class C
                {
                    extension(string s)
                    {

                    }
                }
            }
            """);
    }

    internal static Holder CreateSession(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        OptionsCollection? globalOptions = null,
        ParseOptions? parseOptions = null)
    {
        return CreateSession(
            EditorTestWorkspace.CreateCSharp(code, parseOptions),
            CurlyBrace.OpenCharacter, CurlyBrace.CloseCharacter, globalOptions);
    }
}
