// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticBraceCompletionTests : AbstractAutomaticBraceCompletionTests
    {
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
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
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
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
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
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
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
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
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
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, false }
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

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp), false },
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
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

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.None }
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

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
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

            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Block }
                            };
            using var session = CreateSession(code, optionSet);
            Assert.NotNull(session);

            CheckStart(session.Session);
            Assert.Equal(expected, session.Session.SubjectBuffer.CurrentSnapshot.GetText());

            CheckReturn(session.Session, 8, expectedAfterReturn);
        }

        internal Holder CreateSession(string code, Dictionary<OptionKey, object> optionSet = null)
        {
            return CreateSession(
                TestWorkspace.CreateCSharp(code),
                BraceCompletionSessionProvider.CurlyBrace.OpenCharacter, BraceCompletionSessionProvider.CurlyBrace.CloseCharacter, optionSet);
        }
    }
}
