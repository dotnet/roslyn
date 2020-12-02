// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.BraceCompletion.AbstractBraceCompletionService;
using static Microsoft.CodeAnalysis.CSharp.BraceCompletion.CurlyBraceCompletionService;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticBraceCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void WithExpressionBracesSameLine()
        {
            var code = @"
class C
{
    void M(C c)
    {
        c = c with $$
    }
}";

            var expected = @"
class C
{
    void M(C c)
    {
        c = c with { }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(47381, "https://github.com/dotnet/roslyn/issues/47381")]
        public void ImplicitObjectCreationExpressionBracesSameLine()
        {
            var code = @"
class C
{
    void M(C c)
    {
        c = new() $$
    }
}";

            var expected = @"
class C
{
    void M(C c)
    {
        c = new() { }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void WithExpressionBracesSameLine_Enter()
        {
            var code = @"
class C
{
    void M(C c)
    {
        c = c with $$
    }
}";
            var expected = @"
class C
{
    void M(C c)
    {
        c = c with
        {

        }
    }
}";
            using var session = CreateSession(code);
            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            using var session = CreateSession("$$");
            Assert.NotNull(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_String()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_String2()
        {
            var code = @"class C
{
    string s = @""
$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString1()
        {
            var code = @"class C
{
    string s = $""$$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString2()
        {
            var code = @"class C
{
    string s = $@""$$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString3()
        {
            var code = @"class C
{
    string x = ""goo""
    string s = $""{x} $$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString4()
        {
            var code = @"class C
{
    string x = ""goo""
    string s = $@""{x} $$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString5()
        {
            var code = @"class C
{
    string s = $""{{$$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString6()
        {
            var code = @"class C
{
    string s = $""{}$$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ValidLocation_InterpolatedString7()
        {
            var code = @"class C
{
    string s = $""{}$$
}";

            var expected = @"class C
{
    string s = $""{}{
}
}";

            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
            CheckReturn(session.Session, 0, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_InterpolatedString1()
        {
            var code = @"class C
{
    string s = @""$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_InterpolatedString2()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment()
        {
            var code = @"class C
{
    //$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment2()
        {
            var code = @"class C
{
    /* $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment3()
        {
            var code = @"class C
{
    /// $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment4()
        {
            var code = @"class C
{
    /** $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void MultiLine_Comment()
        {
            var code = @"class C
{
    void Method()
    {
        /* */$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);
            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void MultiLine_DocComment()
        {
            var code = @"class C
{
    void Method()
    {
        /** */$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String1()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String2()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_OpenBrace()
        {
            var code = @"class C $$";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_Delete()
        {
            var code = @"class C $$";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_Tab()
        {
            var code = @"class C $$";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_CloseBrace()
        {
            var code = @"class C $$";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Method_OpenBrace_Multiple()
        {
            var code = @"class C
{
    void Method() { $$
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_OpenBrace_Enter()
        {
            var code = @"class C $$";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 4);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        [WorkItem(47438, "https://github.com/dotnet/roslyn/issues/47438")]
        public void WithExpression()
        {
            var code = @"
record C
{
    void M()
    {
        _ = this with $$
    }
}";

            var expectedBeforeReturn = @"
record C
{
    void M()
    {
        _ = this with { }
    }
}";

            var expectedAfterReturn = @"
record C
{
    void M()
    {
        _ = this with
        {

        }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is $$
    }
}";

            var expectedBeforeReturn = @"
class C
{
    void M()
    {
        _ = this is { }
    }
}";

            var expectedAfterReturn = @"
class C
{
    void M()
    {
        _ = this is
        {

        }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern_Nested()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is { Name: $$ }
    }
}";

            var expectedBeforeReturn = @"
class C
{
    void M()
    {
        _ = this is { Name: { } }
    }
}";

            var expectedAfterReturn = @"
class C
{
    void M()
    {
        _ = this is { Name:
        {

        } }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern_Parentheses1()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is { Name: $$ }
    }
}";
            var expected = @"
class C
{
    void M()
    {
        _ = this is { Name: () }
    }
}";

            using var session = CreateSession(TestWorkspace.CreateCSharp(code), '(', ')');
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern_Parentheses2()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is { Name: { Length: (> 3) and $$ } }
    }
}";
            var expected = @"
class C
{
    void M()
    {
        _ = this is { Name: { Length: (> 3) and () } }
    }
}";

            using var session = CreateSession(TestWorkspace.CreateCSharp(code), '(', ')');
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern_FollowedByInvocation()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is $$
        M();
    }
}";

            var expectedBeforeReturn = @"
class C
{
    void M()
    {
        _ = this is { }
        M();
    }
}";

            var expectedAfterReturn = @"
class C
{
    void M()
    {
        _ = this is
        {

        }
        M();
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void RecursivePattern_WithInvocation_FollowedByInvocation()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this is (1, 2) $$
        M();
    }
}";

            var expectedBeforeReturn = @"
class C
{
    void M()
    {
        _ = this is (1, 2) { }
        M();
    }
}";

            var expectedAfterReturn = @"
class C
{
    void M()
    {
        _ = this is (1, 2)
        {

        }
        M();
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void SwitchExpression()
        {
            var code = @"
class C
{
    void M()
    {
        _ = this switch $$
    }
}";

            var expectedBeforeReturn = @"
class C
{
    void M()
    {
        _ = this switch { }
    }
}";

            var expectedAfterReturn = @"
class C
{
    void M()
    {
        _ = this switch
        {

        }
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expectedBeforeReturn);
            CheckReturn(session.Session, 12, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Class_ObjectInitializer_OpenBrace_Enter()
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
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Collection_Initializer_OpenBraceOnSameLine_Enter()
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
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Collection_Initializer_OpenBraceOnDifferentLine_Enter()
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
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Object_Initializer_OpenBraceOnSameLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        var goo = new Goo $$
    }
}

class Goo
{
    public int bar;
}";

            var expected = @"class C
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
}";
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Object_Initializer_OpenBraceOnDifferentLine_Enter()
        {
            var code = @"class C
{
    public void man()
    {
        var goo = new Goo $$
    }
}

class Goo
{
    public int bar;
}";

            var expected = @"class C
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
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayImplicit_Initializer_OpenBraceOnSameLine_Enter()
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
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayImplicit_Initializer_OpenBraceOnDifferentLine_Enter()
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
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayExplicit1_Initializer_OpenBraceOnSameLine_Enter()
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
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayExplicit1_Initializer_OpenBraceOnDifferentLine_Enter()
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
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayExplicit2_Initializer_OpenBraceOnSameLine_Enter()
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
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { CSharpFormattingOptions2.NewLinesForBracesInObjectCollectionArrayInitializers, false }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(1070773, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070773")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void ArrayExplicit2_Initializer_OpenBraceOnDifferentLine_Enter()
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
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckReturn(session.Session, 12, expected);
        }

        [WorkItem(3447, "https://github.com/dotnet/roslyn/issues/3447")]
        [WorkItem(850540, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/850540")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void BlockIndentationWithAutomaticBraceFormattingDisabled()
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

            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { new OptionKey2(BraceCompletionOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp), false },
                                { new OptionKey2(FormattingOptions2.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

            CheckReturn(session.Session, 4, expectedAfterReturn);
        }

        [WorkItem(2224, "https://github.com/dotnet/roslyn/issues/2224")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void NoSmartOrBlockIndentationWithAutomaticBraceFormattingDisabled()
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

            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { new OptionKey2(FormattingOptions2.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.None }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());
        }

        [WorkItem(2330, "https://github.com/dotnet/roslyn/issues/2330")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void BlockIndentationWithAutomaticBraceFormatting()
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

            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { new OptionKey2(FormattingOptions2.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

            CheckReturn(session.Session, 8, expectedAfterReturn);
        }

        [WorkItem(2330, "https://github.com/dotnet/roslyn/issues/2330")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void BlockIndentationWithAutomaticBraceFormattingSecondSet()
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

            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { new OptionKey2(FormattingOptions2.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

            CheckReturn(session.Session, 8, expectedAfterReturn);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoesNotFormatInsideBracePairInInitializers()
        {
            var code = @"class C
{
    void M()
    {
        var x = new int[]$$
    }
}";

            var expected = @"class C
{
    void M()
    {
        var x = new int[] {}
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckText(session.Session, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void DoesNotFormatOnReturnWithNonWhitespaceInBetween()
        {
            var code = @"class C $$";

            var expected = @"class C { dd
}";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Type(session.Session, "dd");
            CheckReturn(session.Session, 0, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingInsideLambdaInsideInterpolation()
        {
            var code = @"class C
{
    void M(string[] args)
    {
        var s = $""{ args.Select(a => $$)}""
    }
}";
            var expectedAfterStart = @"class C
{
    void M(string[] args)
    {
        var s = $""{ args.Select(a => { })}""
    }
}";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expectedAfterStart, session.Session.SubjectBuffer.CurrentSnapshot.GetText());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_FormattingChangeEntirelyBefore()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aabaa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "b");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Formatting change before insertion location
            // Replace first 'a' with 'cc' to become ccabaa
            var formattingTextChange = new TextChange(new TextSpan(0, 1), "cc");
            incrementalText = incrementalText.WithChanges(formattingTextChange);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, ImmutableArray.Create(formattingTextChange));
            var mergedText = originalText.WithChanges(mergedChanges);

            Assert.Equal(insertionTextChange, mergedChanges[0]);
            Assert.Equal(formattingTextChange, mergedChanges[1]);
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_FormattingChangeEntirelyAfter()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aabaa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "b");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Formatting change entirely after insertion location
            // Replace last 'a' with 'cc' to become aabacc
            var formattingTextChange = new TextChange(new TextSpan(4, 1), "cc");
            incrementalText = incrementalText.WithChanges(formattingTextChange);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, ImmutableArray.Create(formattingTextChange));
            var mergedText = originalText.WithChanges(mergedChanges);

            Assert.Equal(insertionTextChange, mergedChanges[0]);
            // Formatting edit should be shifted to be relative to original text
            Assert.Equal(new TextChange(new TextSpan(3, 1), "cc"), mergedChanges[1]);
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_OverlapsWithEndOfFormattingChange()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aa#$aa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "#$");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Formatting starting before insertion (with overlap), ends inside insertion
            // Replace 'a#' with 'cc' to become acc$aa
            var formattingTextChange = new TextChange(new TextSpan(1, 2), "cc");
            incrementalText = incrementalText.WithChanges(formattingTextChange);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, ImmutableArray.Create(formattingTextChange));
            var mergedText = originalText.WithChanges(mergedChanges);

            // The overlapping text is removed from the initial edit as it is covered by the formatting change.
            Assert.Equal(new TextChange(new TextSpan(2, 0), "$"), mergedChanges[0]);
            // The formatting change span is modified to end before the insertion edit.
            Assert.Equal(new TextChange(new TextSpan(1, 1), "cc"), mergedChanges[1]);
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_OverlapsWithBeginningOfFormattingChange()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aa#$aa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "#$");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Formatting starting inside insertion (with overlap), ends outside insertion
            // Replace '$a' with 'cc' to become aa#cca
            var formattingTextChange = new TextChange(new TextSpan(3, 2), "cc");
            incrementalText = incrementalText.WithChanges(formattingTextChange);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, ImmutableArray.Create(formattingTextChange));
            var mergedText = originalText.WithChanges(mergedChanges);

            // The overlapping text is removed from the initial edit as it is covered by the formatting change.
            Assert.Equal(new TextChange(new TextSpan(2, 0), "#"), mergedChanges[0]);
            // The formatting change span is modified to end before the insertion edit.
            Assert.Equal(new TextChange(new TextSpan(2, 1), "cc"), mergedChanges[1]);
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_NewLineInsideFormattingChange()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aa#$aa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "#$");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Insertion change is entirely inside the formatting change.
            // Replace '#$' with 'ccc' to become aacccaa
            var formattingTextChange = new TextChange(new TextSpan(2, 2), "ccc");
            incrementalText = incrementalText.WithChanges(formattingTextChange);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, ImmutableArray.Create(formattingTextChange));
            var mergedText = originalText.WithChanges(mergedChanges);

            // Only one edit is returned with the new text.
            Assert.Equal(new TextChange(new TextSpan(2, 0), "ccc"), mergedChanges.Single());
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void CurlyBraceFormattingEditMerge_FormattingBeforeAndAfter()
        {
            var originalText = SourceText.From("aaaa");

            // Text should be aa#$%aa
            var insertionTextChange = new TextChange(new TextSpan(2, 0), "#$%");
            var incrementalText = originalText.WithChanges(insertionTextChange);

            // Multiple changes formatting changes before and after the insertion.
            // Result should be ccdd$eeff
            var entirelyBeforeChange = new TextChange(new TextSpan(0, 1), "cc");
            var partiallyBeforeChange = new TextChange(new TextSpan(1, 2), "dd");
            var partiallyAfterChange = new TextChange(new TextSpan(4, 2), "ee");
            var entirelyAfterChange = new TextChange(new TextSpan(6, 1), "ff");
            var changes = ImmutableArray.Create(entirelyBeforeChange, partiallyBeforeChange, partiallyAfterChange, entirelyAfterChange);
            incrementalText = incrementalText.WithChanges(changes);

            var mergedChanges = MergeFormatChangesIntoNewLineChange(insertionTextChange, changes);
            var mergedText = originalText.WithChanges(mergedChanges);

            Assert.Equal(new TextChange(new TextSpan(2, 0), "$"), mergedChanges[0]);
            Assert.Equal(new TextChange(new TextSpan(0, 1), "cc"), mergedChanges[1]);
            Assert.Equal(new TextChange(new TextSpan(1, 1), "dd"), mergedChanges[2]);
            Assert.Equal(new TextChange(new TextSpan(2, 1), "ee"), mergedChanges[3]);
            Assert.Equal(new TextChange(new TextSpan(3, 1), "ff"), mergedChanges[4]);
            Assert.Equal(incrementalText.ToString(), mergedText.ToString());
        }

        internal static Holder CreateSession(string code, Dictionary<OptionKey2, object> optionSet = null)
        {
            return CreateSession(
                TestWorkspace.CreateCSharp(code),
                CurlyBrace.OpenCharacter, CurlyBrace.CloseCharacter, optionSet);
        }
    }
}
