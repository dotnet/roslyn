// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTests : FormattingEngineTestBase
    {
        private static Dictionary<OptionKey, object> SmartIndentButDoNotFormatWhileTyping()
        {
            return new Dictionary<OptionKey, object>
            {
                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Smart },
                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp),  false },
                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp),  false },
            };
        }

        [WpfFact]
        [WorkItem(539682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539682")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentCommandHandler()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
int y;
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
        int y;
    }
}
";

            AssertFormatWithView(expected, code);
        }

        [WpfFact]
        [WorkItem(539682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539682")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentPasteCommandHandler()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
int y;
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
        int y;
    }
}
";

            AssertFormatWithPasteOrReturn(expected, code, allowDocumentChanges: true);
        }

        [WpfFact]
        [WorkItem(547261, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547261")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatDocumentReadOnlyWorkspacePasteCommandHandler()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
int y;
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int x;$$
int y;
    }
}
";

            AssertFormatWithPasteOrReturn(expected, code, allowDocumentChanges: false);
        }

        [WpfFact]
        [WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatUsingStatementOnReturn()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
                using (null)$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
                using (null)$$
    }
}
";

            AssertFormatWithPasteOrReturn(expected, code, allowDocumentChanges: true, isPaste: false);
        }

        [WpfFact]
        [WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatUsingStatementWhenTypingCloseParen()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
                using (null)$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
        using (null)
    }
}
";

            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact]
        [WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatNotUsingStatementOnReturn()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
                for (;;)$$
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        using (null)
                for (;;)$$
    }
}
";

            AssertFormatWithPasteOrReturn(expected, code, allowDocumentChanges: true, isPaste: false);
        }

        [WorkItem(977133, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/977133")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatRangeOrFormatTokenOnOpenBraceOnSameLine()
        {
            var code = @"class C
{
    public void M()
    {
        if (true)        {$$
    }
}";
            var expected = @"class C
{
    public void M()
    {
        if (true)        {
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(14491, "https://github.com/dotnet/roslyn/pull/14491")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatRangeButFormatTokenOnOpenBraceOnNextLine()
        {
            var code = @"class C
{
    public void M()
    {
        if (true)
            {$$
    }
}";
            var expected = @"class C
{
    public void M()
    {
        if (true)
        {
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(1007071, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007071")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatPragmaWarningInbetweenDelegateDeclarationStatement()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Func <bool> a = delegate ()
#pragma warning disable CA0001
        {
            return true;
        };$$
    }
}";
            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        Func<bool> a = delegate ()
#pragma warning disable CA0001
        {
            return true;
        };
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatHashRegion()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
#region$$
    }
}";
            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        #region
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatHashEndRegion()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
    {
        #region
#endregion$$
    }
}";
            var expected = @"using System;

class Program
{
    static void Main(string[] args)
    {
        #region
        #endregion
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(987373, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/987373")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSpansIndividuallyWithoutCollapsing()
        {
            var code = @"class C
{
    public void M()
    {
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        if(true){}
        [|if(true){}|]
    }
}";
            var expected = @"class C
{
    public void M()
    {
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if(true){}
        if (true) { }
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var subjectDocument = workspace.Documents.Single();
            var spans = subjectDocument.SelectedSpans;

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var syntaxRoot = await document.GetSyntaxRootAsync();

            var node = Formatter.Format(syntaxRoot, spans, workspace);
            Assert.Equal(expected, node.ToFullString());
        }

        [WorkItem(987373, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/987373")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public async Task FormatSpansWithCollapsing()
        {
            var code = @"class C
{
    public void M()
    {
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        [|if(true){}|]
        while(true){}
        [|if(true){}|]
    }
}";
            var expected = @"class C
{
    public void M()
    {
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        if (true) { }
        while (true) { }
        if (true) { }
    }
}";
            using var workspace = TestWorkspace.CreateCSharp(code);
            var subjectDocument = workspace.Documents.Single();
            var spans = subjectDocument.SelectedSpans;
            workspace.Options = workspace.Options.WithChangedOption(FormattingOptions.AllowDisjointSpanMerging, true);

            var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var syntaxRoot = await document.GetSyntaxRootAsync();

            var node = Formatter.Format(syntaxRoot, spans, workspace);
            Assert.Equal(expected, node.ToFullString());
        }

        [WorkItem(1044118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1044118")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void SemicolonInCommentOnLastLineDoesNotFormat()
        {
            var code = @"using System;

class Program
{
    static void Main(string[] args)
        {
        }
}
// ;$$";

            var expected = @"using System;

class Program
{
    static void Main(string[] args)
        {
        }
}
// ;";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideSingleLineRegularComment_1()
        {
            var code = @"class Program
{
                              //        {$$
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              //        {
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideSingleLineRegularComment_2()
        {
            var code = @"class Program
{
                              //        {$$   
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              //        {   
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineRegularComment_1()
        {
            var code = @"class Program
{
    static void Main(int          a/*         {$$       */, int b)
    {

    }
}";

            var expected = @"class Program
{
    static void Main(int          a/*         {       */, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineRegularComment_2()
        {
            var code = @"class Program
{
    static void Main(int          a/*         {$$
        */, int b)
    {

    }
}";

            var expected = @"class Program
{
    static void Main(int          a/*         {
        */, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineRegularComment_3()
        {
            var code = @"class Program
{
    static void Main(int          a/*         {$$    
        */, int b)
    {

    }
}";

            var expected = @"class Program
{
    static void Main(int          a/*         {    
        */, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideSingleLineDocComment_1()
        {
            var code = @"class Program
{
                              ///        {$$
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              ///        {
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideSingleLineDocComment_2()
        {
            var code = @"class Program
{
                              ///        {$$   
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              ///        {   
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineDocComment_1()
        {
            var code = @"class Program
{
                              /**        {$$   **/
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              /**        {   **/
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineDocComment_2()
        {
            var code = @"class Program
{
                              /**        {$$   
                **/
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              /**        {   
                **/
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideMultiLineDocComment_3()
        {
            var code = @"class Program
{
                              /**        {$$
                **/
                       static void Main(int a, int b)
    {

    }
}";

            var expected = @"class Program
{
                              /**        {
                **/
                       static void Main(int a, int b)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideInactiveCode()
        {
            var code = @"class Program
{
                                        #if false
                    {$$
            #endif

    static void Main(string[] args)
    {

    }
}";

            var expected = @"class Program
{
                                        #if false
                    {
            #endif

    static void Main(string[] args)
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideStringLiteral()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var asdas =     ""{$$""        ;
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var asdas =     ""{""        ;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideCharLiteral()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var asdas =     '{$$'        ;
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var asdas =     '{'        ;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(449, "https://github.com/dotnet/roslyn/issues/449")]
        [WorkItem(1077103, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideCommentsOfPreprocessorDirectives()
        {
            var code = @"class Program
{
       #region
        #endregion // a/*{$$*/    
        static void Main(string[] args)
    {
        
    }
}";

            var expected = @"class Program
{
       #region
        #endregion // a/*{*/    
        static void Main(string[] args)
    {
        
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColonInSwitchCase()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int f = 0;
        switch(f)
        {
                 case     1     :$$    break;
        }
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int f = 0;
        switch(f)
        {
            case 1:    break;
        }
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColonInDefaultSwitchCase()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        int f = 0;
        switch(f)
        {
            case 1:    break;
                    default    :$$     break;
        }
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        int f = 0;
        switch(f)
        {
            case 1:    break;
            default:     break;
        }
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(9097, "https://github.com/dotnet/roslyn/issues/9097")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColonInPatternSwitchCase01()
        {
            var code = @"class Program
{
    static void Main()
    {
        switch(f)
        {
                          case  int  i            :$$    break;
        }
    }
}";

            var expected = @"class Program
{
    static void Main()
    {
        switch(f)
        {
            case int i:    break;
        }
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void ColonInLabeledStatement()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
            label1   :$$   int s = 0;
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
            label1:   int s = 0;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInTargetAttribute()
        {
            var code = @"using System;
[method    :$$    C]
class C : Attribute
{
}";

            var expected = @"using System;
[method    :    C]
class C : Attribute
{
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInBaseList()
        {
            var code = @"class C   :$$   Attribute
{
}";

            var expected = @"class C   :   Attribute
{
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInThisConstructor()
        {
            var code = @"class Goo
{
    Goo(int s)   :$$   this()
    {
    }

    Goo()
    {
    }
}";

            var expected = @"class Goo
{
    Goo(int s)   :   this()
    {
    }

    Goo()
    {
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInConditionalOperator()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var vari = goo()     ?    true  :$$  false;
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var vari = goo()     ?    true  :  false;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInArgument()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        Main(args  :$$  args);
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        Main(args  :  args);
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464, "https://github.com/dotnet/roslyn/issues/464")]
        [WorkItem(908729, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInTypeParameter()
        {
            var code = @"class Program<T>
{
    class C1<U>
        where   T  :$$  U
    {

    }
}";

            var expected = @"class Program<T>
{
    class C1<U>
        where   T  :  U
    {

    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(2224, "https://github.com/dotnet/roslyn/issues/2224")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DontSmartFormatBracesOnSmartIndentNone()
        {
            var code = @"class Program<T>
{
    class C1<U>
{$$
}";

            var expected = @"class Program<T>
{
    class C1<U>
{
}";
            var optionSet = new Dictionary<OptionKey, object>
                            {
                                { new OptionKey(FormattingOptions.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.None }
                            };
            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void StillAutoIndentCloseBraceWhenFormatOnCloseBraceIsOff()
        {
            var code = @"namespace N
{
    class C
    {
             // improperly indented code
             int x = 10;
        }$$
}
";

            var expected = @"namespace N
{
    class C
    {
             // improperly indented code
             int x = 10;
    }
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                    { new OptionKey(FeatureOnOffOptions.AutoFormattingOnCloseBrace, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void AutoIndentCloseBraceWhenFormatOnTypingIsOff()
        {
            var code = @"namespace N
{
    class C
    {
             // improperly indented code
             int x = 10;
        }$$
}
";

            var expected = @"namespace N
{
    class C
    {
             // improperly indented code
             int x = 10;
    }
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WorkItem(5873, "https://github.com/dotnet/roslyn/issues/5873")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void KeepTabsInCommentsWhenFormattingIsOff()
        {
            // There are tabs in this test case.  Tools that touch the Roslyn repo should
            // not remove these as we are explicitly testing tab behavior.
            var code =
@"class Program
{
    static void Main()
    {
        return;		/* Comment preceded by tabs */		// This one too
        }$$
}";

            var expected =
@"class Program
{
    static void Main()
    {
        return;		/* Comment preceded by tabs */		// This one too
    }
}";

            var optionSet = new Dictionary<OptionKey, object>
            {
                { new OptionKey(FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WorkItem(5873, "https://github.com/dotnet/roslyn/issues/5873")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DoNotKeepTabsInCommentsWhenFormattingIsOn()
        {
            // There are tabs in this test case.  Tools that touch the Roslyn repo should
            // not remove these as we are explicitly testing tab behavior.
            var code = @"class Program
{
    static void Main()
    {
        return;		/* Comment preceded by tabs */		// This one too
        }$$
}";

            var expected =
@"class Program
{
    static void Main()
    {
        return;     /* Comment preceded by tabs */        // This one too
    }
}";

            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DoNotFormatStatementIfSemicolonOptionIsOff()
        {
            var code =
                @"namespace N
{
    class C
    {
        int x   =   10     ;$$
    }
}
";

            var expected =
@"namespace N
{
    class C
    {
        int x   =   10     ;
    }
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                    { new OptionKey(FeatureOnOffOptions.AutoFormattingOnSemicolon, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void DoNotFormatStatementIfTypingOptionIsOff()
        {
            var code =
                @"namespace N
{
    class C
    {
        int x   =   10     ;$$
    }
}
";

            var expected =
@"namespace N
{
    class C
    {
        int x   =   10     ;
    }
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                    { new OptionKey(FeatureOnOffOptions.AutoFormattingOnTyping, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected, optionSet);
        }

        [WpfFact, WorkItem(4435, "https://github.com/dotnet/roslyn/issues/4435")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void OpenCurlyNotFormattedIfNotAtStartOfLine()
        {
            var code =
@"
class C
{
    public  int     P   {$$
}
";

            var expected =
@"
class C
{
    public  int     P   {
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                { new OptionKey(BraceCompletionOptions.Enable, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, WorkItem(4435, "https://github.com/dotnet/roslyn/issues/4435")]
        [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
        public void OpenCurlyFormattedIfAtStartOfLine()
        {
            var code =
@"
class C
{
    public  int     P
        {$$
}
";

            var expected =
@"
class C
{
    public  int     P
    {
}
";

            var optionSet = new Dictionary<OptionKey, object>
            {
                { new OptionKey(BraceCompletionOptions.Enable, LanguageNames.CSharp), false }
            };

            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly1()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true;$$
    }
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly2()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get { return true;$$
    }
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get { return true;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly3()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get;$$
    }
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatCompleteBlockOnSingleLineIfTypingCloseCurly1()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true; }$$
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true; }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatCompleteBlockOnSingleLineIfTypingCloseCurly2()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get { return true; }$$
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property { get { return true; }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatIncompleteBlockOnMultipleLinesIfTypingCloseCurly1()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true;
    }$$
}";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get
            {
                return true;
            }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatIncompleteBlockOnMultipleLinesIfTypingCloseCurly2()
        {
            var code = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get { return true;
    }
}$$";
            var expected = @"namespace ConsoleApplication1
{
    class Program
    {
        static bool Property
        {
            get
            {
                return true;
            }
        }";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatCompleteBlockOnSingleLineIfTypingSemicolon()
        {
            var code =
@"public class Class1
{
    void M()
    {
        try { }
        catch { return;$$
        x.ToString();
    }
}";
            var expected =
@"public class Class1
{
    void M()
    {
        try { }
        catch { return;
        x.ToString();
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatCompleteBlockOnSingleLineIfTypingCloseCurlyOnLaterLine()
        {
            var code =
@"public class Class1
{
    void M()
    {
        try { }
        catch { return;
        x.ToString();
        }$$
    }
}";
            var expected =
@"public class Class1
{
    void M()
    {
        try { }
        catch
        {
            return;
            x.ToString();
        }
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(7900, "https://github.com/dotnet/roslyn/issues/7900")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatLockStatementWithEmbeddedStatementOnSemicolonDifferentLine()
        {
            var code = @"class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)
                       Console.WriteLine(""d"");$$
    }
}";
            var expected = @"class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)
            Console.WriteLine(""d"");
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(7900, "https://github.com/dotnet/roslyn/issues/7900")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatLockStatementWithEmbeddedStatementOnSemicolonSameLine()
        {
            var code = @"class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l)      Console.WriteLine(""d"");$$
    }
}";
            var expected = @"class C
{
    private object _l = new object();
    public void M()
    {
        lock (_l) Console.WriteLine(""d"");
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(11642, "https://github.com/dotnet/roslyn/issues/11642")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatArbitraryNodeParenthesizedLambdaExpression()
        {
            // code equivalent to an expression synthesized like so:
            // ParenthesizedExpression(ParenthesizedLambdaExpression(ParameterList(), Block()))
            var code = @"(()=>{})";
            var node = SyntaxFactory.ParseExpression(code);
            var expected = @"(() => { })";
            AssertFormatOnArbitraryNode(node, expected);
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff1()
        {
            var code =
@"class Program
{
    void M()
    {
        if (true)
            {$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true)
        {
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff2()
        {
            var code =
@"class Program
{
    void M()
    {
        if (true)
        {}$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true)
        { }
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff3()
        {
            // We only smart indent the { if it's on it's own line.
            var code =
@"class Program
{
    void M()
    {
        if (true){$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true){
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff4()
        {
            // We only smart indent the { if it's on it's own line.
            var code =
@"class Program
{
    void M()
    {
        if (true){}$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true){ }
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff5()
        {
            // Typing the { should not affect the formating of the preceding tokens.
            var code =
@"class Program
{
    void M()
    {
        if ( true )
            {$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if ( true )
        {
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff6()
        {
            // Typing the { should not affect the formating of the preceding tokens.
            var code =
@"class Program
{
    void M()
    {
        if ( true ){$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if ( true ){
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff7()
        {
            var code =
@"class Program
{
    void M()
        {$$
}";

            var expected =
@"class Program
{
    void M()
    {
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff1()
        {
            var code =
@"class Program
{
    void M()
    {
        if (true)
        {
            }$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true)
        {
        }
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff2()
        {
            // Note that the { is not updated since we are not formatting.
            var code =
@"class Program
{
    void M()
    {
        if (true) {
            }$$
    }
}";

            var expected =
@"class Program
{
    void M()
    {
        if (true) {
        }
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff3()
        {
            var code =
@"class Program
{
    void M()
    {
        }$$
}";

            var expected =
@"class Program
{
    void M()
    {
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff4()
        {
            // Should not affect formatting of open brace
            var code =
@"class Program
{
    void M() {
        }$$
}";

            var expected =
@"class Program
{
    void M() {
    }
}";

            AssertFormatAfterTypeChar(code, expected, SmartIndentButDoNotFormatWhileTyping());
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(31907, "https://github.com/dotnet/roslyn/issues/31907")]
        public async Task NullableReferenceTypes()
        {
            var code = @"[|
class MyClass
{
    void MyMethod()
    {
        var returnType = (_useMethodSignatureReturnType ? _methodSignatureOpt !: method).ReturnType;
    }
}
|]";
            var expected = @"
class MyClass
{
    void MyMethod()
    {
        var returnType = (_useMethodSignatureReturnType ? _methodSignatureOpt! : method).ReturnType;
    }
}
";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 4);
        }

        [WorkItem(30518, "https://github.com/dotnet/roslyn/issues/30518")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatGeneratedNodeInInitializer()
        {
            var code = @"new bool[] {
    true,
    true
}";

            var expected = @"new bool[] {
    true,
true == false, true
}";

            var tree = SyntaxFactory.ParseSyntaxTree(code, options: TestOptions.Script);
            var root = tree.GetRoot();

            var entry = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            var newRoot = root.InsertNodesBefore(root.DescendantNodes().Last(), new[] { entry });
            AssertFormatOnArbitraryNode(newRoot, expected);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        [WorkItem(27268, "https://github.com/dotnet/roslyn/issues/27268")]
        public async Task PositionalPattern()
        {
            var code = @"[|
class MyClass
{
    void MyMethod()
    {
        var point = new Point (3, 4);
        if (point is Point (3, 4) _
            && point is Point{x: 3, y: 4} _)
        {
        }
    }
}
|]";
            var expected = @"
class MyClass
{
    void MyMethod()
    {
        var point = new Point(3, 4);
        if (point is Point(3, 4) _
            && point is Point { x: 3, y: 4 } _)
        {
        }
    }
}
";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 4);
        }

        private void AssertFormatAfterTypeChar(string code, string expected, Dictionary<OptionKey, object> changedOptionSet = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code);
            if (changedOptionSet != null)
            {
                var options = workspace.Options;
                foreach (var entry in changedOptionSet)
                {
                    options = options.WithChangedOption(entry.Key, entry.Value);
                }

                workspace.Options = options;
            }

            var subjectDocument = workspace.Documents.Single();

            var commandHandler = workspace.GetService<FormatCommandHandler>();
            var typedChar = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText(subjectDocument.CursorPosition.Value - 1, 1);
            commandHandler.ExecuteCommand(new TypeCharCommandArgs(subjectDocument.GetTextView(), subjectDocument.GetTextBuffer(), typedChar[0]), () => { }, TestCommandExecutionContext.Create());

            var newSnapshot = subjectDocument.GetTextBuffer().CurrentSnapshot;

            Assert.Equal(expected, newSnapshot.GetText());
        }
    }
}
