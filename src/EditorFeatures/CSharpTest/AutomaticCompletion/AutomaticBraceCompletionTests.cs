// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticBraceCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Creation()
        {
            using (var session = await CreateSessionAsync("$$"))
            {
                Assert.NotNull(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_String()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_String2()
        {
            var code = @"class C
{
    string s = @""
$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString1()
        {
            var code = @"class C
{
    string s = $""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString2()
        {
            var code = @"class C
{
    string s = $@""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString3()
        {
            var code = @"class C
{
    string x = ""foo""
    string s = $""{x} $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString4()
        {
            var code = @"class C
{
    string x = ""foo""
    string s = $@""{x} $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString5()
        {
            var code = @"class C
{
    string s = $""{{$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ValidLocation_InterpolatedString6()
        {
            var code = @"class C
{
    string s = $""{}$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_InterpolatedString1()
        {
            var code = @"class C
{
    string s = @""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_InterpolatedString2()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment()
        {
            var code = @"class C
{
    //$$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment2()
        {
            var code = @"class C
{
    /* $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment3()
        {
            var code = @"class C
{
    /// $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task InvalidLocation_Comment4()
        {
            var code = @"class C
{
    /** $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.Null(session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task MultiLine_Comment()
        {
            var code = @"class C
{
    void Method()
    {
        /* */$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);
                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task MultiLine_DocComment()
        {
            var code = @"class C
{
    void Method()
    {
        /** */$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String1()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task String2()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_OpenBrace()
        {
            var code = @"class C $$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_Delete()
        {
            var code = @"class C $$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckBackspace(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_Tab()
        {
            var code = @"class C $$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckTab(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_CloseBrace()
        {
            var code = @"class C $$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckOverType(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Method_OpenBrace_Multiple()
        {
            var code = @"class C
{
    void Method() { $$
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_OpenBrace_Enter()
        {
            var code = @"class C $$";

            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 4);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Class_ObjectInitializer_OpenBrace_Enter()
        {
            var code = @"using System.Collections.Generic;
 
class C
{
    List<C> list = new List<C>
    {
        new C $$
    };
}";

            var expected = @"using System.Collections.Generic;
 
class C
{
    List<C> list = new List<C>
    {
        new C
        {

        }
    };
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Collection_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"using System.Collections.Generic;
 
class C
{
    public void man()
    {
        List<C> list = new List<C> $$
    }
}";

            var expected = @"using System.Collections.Generic;
 
class C
{
    public void man()
    {
        List<C> list = new List<C> {

        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Collection_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"using System.Collections.Generic;
 
class C
{
    public void man()
    {
        List<C> list = new List<C> $$
    }
}";

            var expected = @"using System.Collections.Generic;
 
class C
{
    public void man()
    {
        List<C> list = new List<C>
        {

        }
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Object_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        var foo = new Foo $$
    }
}

class Foo
{
    public int bar;
}";

            var expected = @"class C
{
    public void man()
    {
        var foo = new Foo {

        }
    }
}

class Foo
{
    public int bar;
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task Object_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        var foo = new Foo $$
    }
}

class Foo
{
    public int bar;
}";

            var expected = @"class C
{
    public void man()
    {
        var foo = new Foo
        {

        }
    }
}

class Foo
{
    public int bar;
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayImplicit_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr = {

        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayImplicit_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr =
        {

        }
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayExplicit1_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = new[] $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr = new[] {

        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayExplicit1_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = new[] $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr = new[]
        {

        }
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayExplicit2_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = new int[] $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr = new int[] {

        }
    }
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(1070773)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task ArrayExplicit2_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        int[] arr = new int[] $$
    }
}";

            var expected = @"class C
{
    public void man()
    {
        int[] arr = new int[]
        {

        }
    }
}";
            using (var session = await CreateSessionAsync(code))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                CheckReturn(session.Session, 12, expected);
            }
        }

        [WorkItem(3447, "https://github.com/dotnet/roslyn/issues/3447")]
        [WorkItem(850540)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task BlockIndentationWithAutomaticBraceFormattingDisabled()
        {
            var code = @"class C
{
    public void X()
    $$
}";

            var expected = @"class C
{
    public void X()
    {}
}";

            var expectedAfterReturn = @"class C
{
    public void X()
    {

    }
}";

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp), false },
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

                CheckReturn(session.Session, 4, expectedAfterReturn);
            }
        }

        [WorkItem(2224, "https://github.com/dotnet/roslyn/issues/2224")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task NoSmartOrBlockIndentationWithAutomaticBraceFormattingDisabled()
        {
            var code = @"namespace NS1
{
    public class C1
$$
}";

            var expected = @"namespace NS1
{
    public class C1
{ }
}";

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.None }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());
            }
        }

        [WorkItem(2330, "https://github.com/dotnet/roslyn/issues/2330")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task BlockIndentationWithAutomaticBraceFormatting()
        {
            var code = @"namespace NS1
{
        public class C1
        $$
}";

            var expected = @"namespace NS1
{
        public class C1
        { }
}";

            var expectedAfterReturn = @"namespace NS1
{
        public class C1
        {

        }
}";

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

                CheckReturn(session.Session, 8, expectedAfterReturn);
            }
        }

        [WorkItem(2330, "https://github.com/dotnet/roslyn/issues/2330")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public async Task BlockIndentationWithAutomaticBraceFormattingSecondSet()
        {
            var code = @"namespace NS1
{
        public class C1
        { public class C2 $$

        }
}";

            var expected = @"namespace NS1
{
        public class C1
        { public class C2 { }

        }
}";

            var expectedAfterReturn = @"namespace NS1
{
        public class C1
        { public class C2 {

        }

        }
}";

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using (var session = await CreateSessionAsync(code, optionSet))
            {
                Assert.NotNull(session);

                CheckStart(session.Session);
                Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

                CheckReturn(session.Session, 8, expectedAfterReturn);
            }
        }

        internal async Task<Holder> CreateSessionAsync(string code, Dictionary<OptionKey, object> optionSet = null)
        {
            return CreateSession(
                await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(code),
                BraceCompletionSessionProvider.CurlyBrace.OpenCharacter, BraceCompletionSessionProvider.CurlyBrace.CloseCharacter, optionSet);
        }
    }
}
