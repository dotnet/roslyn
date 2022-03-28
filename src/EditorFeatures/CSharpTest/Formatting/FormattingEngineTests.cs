// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    [Trait(Traits.Feature, Traits.Features.Formatting)]
    public class FormattingEngineTests : CSharpFormattingEngineTestBase
    {
        public FormattingEngineTests(ITestOutputHelper output) : base(output) { }

        private static Dictionary<OptionKey2, object> SmartIndentButDoNotFormatWhileTyping()
        {
            return new Dictionary<OptionKey2, object>
            {
                { new OptionKey2(AutoFormattingOptions.Metadata.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.Smart },
                { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnTyping, LanguageNames.CSharp),  false },
                { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnCloseBrace, LanguageNames.CSharp),  false },
            };
        }

        [WpfFact]
        [WorkItem(539682, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539682")]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
            var options = await SyntaxFormattingOptions.FromDocumentAsync(document, CancellationToken.None).ConfigureAwait(false);
            var node = Formatter.Format(syntaxRoot, spans, workspace.Services, options, rules: null, CancellationToken.None);
            Assert.Equal(expected, node.ToFullString());
        }

        [WorkItem(1044118, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1044118")]
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
            var optionSet = new Dictionary<OptionKey2, object>
                            {
                                { new OptionKey2(AutoFormattingOptions.Metadata.SmartIndent, LanguageNames.CSharp), FormattingOptions.IndentStyle.None }
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

            var optionSet = new Dictionary<OptionKey2, object>
            {
                    { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnCloseBrace, LanguageNames.CSharp), false }
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

            var optionSet = new Dictionary<OptionKey2, object>
            {
                { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnTyping, LanguageNames.CSharp), false }
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

            var optionSet = new Dictionary<OptionKey2, object>
            {
                { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnTyping, LanguageNames.CSharp), false }
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

            var optionSet = new Dictionary<OptionKey2, object>
            {
                    { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnSemicolon, LanguageNames.CSharp), false }
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

            var optionSet = new Dictionary<OptionKey2, object>
            {
                    { new OptionKey2(AutoFormattingOptions.Metadata.AutoFormattingOnTyping, LanguageNames.CSharp), false }
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

            AssertFormatAfterTypeChar(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
        public void FormatArbitraryNodeParenthesizedLambdaExpression()
        {
            // code equivalent to an expression synthesized like so:
            // ParenthesizedExpression(ParenthesizedLambdaExpression(ParameterList(), Block()))
            var code = @"(()=>{})";
            var node = SyntaxFactory.ParseExpression(code);
            var expected = @"(() => { })";
            AssertFormatOnArbitraryNode(node, expected);
        }

        [WpfFact]
        [WorkItem(57465, "https://github.com/dotnet/roslyn/issues/57465")]
        public async Task FormatLambdaWithDirective()
        {
            var code = @"namespace N
{
    public class C
    {
        protected void Render()
        {
            if (outer)
            {
                    M(() =>
                        {
#nullable enable
                               if (inner)
                                    {
                                    }
                                }
                        );
                }
        }
    }
}
";
            var expected = @"namespace N
{
    public class C
    {
        protected void Render()
        {
            if (outer)
            {
                M(() =>
                    {
#nullable enable
                        if (inner)
                        {
                        }
                    }
                    );
            }
        }
    }
}
";

            await AssertFormatAsync(expected, code, spans: null);
        }

        [WpfTheory]
        [CombinatorialData]
        [WorkItem(59637, "https://github.com/dotnet/roslyn/issues/59637")]
        public async Task FormatAttributeAtEndOfFile(bool trailingNewLine)
        {
            var endOfFile = trailingNewLine ? Environment.NewLine : "";
            var code = $@"using System.Diagnostics.CodeAnalysis;

[assembly:SuppressMessage(""Globalization"", ""CA1308: Normalize strings to uppercase"", Justification = ""My reason"", Scope = ""member"", Target = ""~M:Method"") ] {endOfFile}";
            var expected = $@"using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(""Globalization"", ""CA1308: Normalize strings to uppercase"", Justification = ""My reason"", Scope = ""member"", Target = ""~M:Method"")]{endOfFile}";

            await AssertFormatAsync(expected, code, spans: null);
            await AssertFormatAsync(expected, expected, spans: null);
        }

        [WorkItem(30787, "https://github.com/dotnet/roslyn/issues/30787")]
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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
        [WpfFact]
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

        [WpfFact]
        public async Task WithExpression()
        {
            var code = @"[|
record C(int Property)
{
    void M()
    {
        _ = this  with  {  Property  =  1  } ;
    }
}
|]";
            var expected = @"
record C(int Property)
{
    void M()
    {
        _ = this with { Property = 1 };
    }
}
";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 4);
        }

        [WpfFact]
        public async Task WithExpression_MultiLine()
        {
            var code = @"[|
record C(int Property, int Property2)
{
    void M()
    {
        _ = this  with
{
Property  =  1,
Property2  =  2
} ;
    }
}
|]";
            var expected = @"
record C(int Property, int Property2)
{
    void M()
    {
        _ = this with
        {
            Property = 1,
            Property2 = 2
        };
    }
}
";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 4);
        }

        [WpfFact]
        public async Task WithExpression_MultiLine_UserPositionedBraces()
        {
            var code = @"[|
record C(int Property, int Property2)
{
    void M()
    {
        _ = this  with
            {
                Property  =  1,
                Property2  =  2
            } ;
    }
}
|]";
            var expected = @"
record C(int Property, int Property2)
{
    void M()
    {
        _ = this with
        {
            Property = 1,
            Property2 = 2
        };
    }
}
";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 4);
        }

        [WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")]
        [WpfFact]
        public void SeparateGroups_KeepMultipleLinesBetweenGroups()
        {
            var code = @"$$
using System.A;
using System.B;


using MS.A;
using MS.B;
";

            var expected = @"$$
using System.A;
using System.B;


using MS.A;
using MS.B;
";

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, true));
        }

        [WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")]
        [WpfFact]
        public void SeparateGroups_KeepMultipleLinesBetweenGroups_FileScopedNamespace()
        {
            var code = @"$$
namespace N;

using System.A;
using System.B;


using MS.A;
using MS.B;
";

            var expected = @"$$
namespace N;

using System.A;
using System.B;


using MS.A;
using MS.B;
";

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, true));
        }

        [WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")]
        [WpfFact]
        public void SeparateGroups_DoNotGroupIfNotSorted()
        {
            var code = @"$$
using System.B;
using System.A;
using MS.B;
using MS.A;
";

            var expected = @"$$
using System.B;
using System.A;
using MS.B;
using MS.A;
";

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, true));
        }

        [WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")]
        [WpfFact]
        public void SeparateGroups_GroupIfSorted()
        {
            var code = @"$$
using System.A;
using System.B;
using MS.A;
using MS.B;
";

            var expected = @"$$
using System.A;
using System.B;

using MS.A;
using MS.B;
";

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, true));
        }

        [WorkItem(25003, "https://github.com/dotnet/roslyn/issues/25003")]
        [WpfFact]
        public void SeparateGroups_GroupIfSorted_RecognizeSystemNotFirst()
        {
            var code = @"$$
using MS.A;
using MS.B;
using System.A;
using System.B;
";

            var expected = @"$$
using MS.A;
using MS.B;

using System.A;
using System.B;
";

            AssertFormatWithView(expected, code, (GenerationOptions.SeparateImportDirectiveGroups, true));
        }

        [WpfFact]
        [WorkItem(58157, "https://github.com/dotnet/roslyn/issues/58157")]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatImplicitObjectCollection()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        $$List<int> list = new()
        {
            1, 2, 3, 4,
        };
    }
}
";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        $$List<int> list = new()
        {
            1, 2, 3, 4,
        };
    }
}
";

            AssertFormatWithView(expected, code);
        }

        [Fact, WorkItem(49492, "https://github.com/dotnet/roslyn/issues/49492")]
        public void PreserveAnnotationsOnMultiLineTrivia()
        {
            var text = @"
namespace TestApp
{
    class Test
    {
    /* __marker__ */
    }
}
";

            var position = text.IndexOf("/* __marker__ */");
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            var root = syntaxTree.GetRoot();

            var annotation = new SyntaxAnnotation("marker");
            var markerTrivia = root.FindTrivia(position, findInsideTrivia: true);
            var annotatedMarkerTrivia = markerTrivia.WithAdditionalAnnotations(annotation);
            root = root.ReplaceTrivia(markerTrivia, annotatedMarkerTrivia);

            using var workspace = new AdhocWorkspace();

            var options = CSharpSyntaxFormattingOptions.Default;

            var formattedRoot = Formatter.Format(root, workspace.Services, options, CancellationToken.None);
            var annotatedTrivia = formattedRoot.GetAnnotatedTrivia("marker");

            Assert.Single(annotatedTrivia);
        }

        private static void AssertFormatAfterTypeChar(string code, string expected, Dictionary<OptionKey2, object> changedOptionSet = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(code);
            if (changedOptionSet != null)
            {
                var options = workspace.Options;
                foreach (var entry in changedOptionSet)
                {
                    options = options.WithChangedOption(entry.Key, entry.Value);
                }

                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));
            }

            var subjectDocument = workspace.Documents.Single();

            var commandHandler = workspace.GetService<FormatCommandHandler>();
            var typedChar = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText(subjectDocument.CursorPosition.Value - 1, 1);
            commandHandler.ExecuteCommand(new TypeCharCommandArgs(subjectDocument.GetTextView(), subjectDocument.GetTextBuffer(), typedChar[0]), () => { }, TestCommandExecutionContext.Create());

            var newSnapshot = subjectDocument.GetTextBuffer().CurrentSnapshot;

            AssertEx.EqualOrDiff(expected, newSnapshot.GetText());
        }
    }
}
