// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTests : FormattingEngineTestBase
    {
        [WpfFact]
        [WorkItem(539682)]
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
        [WorkItem(539682)]
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
        [WorkItem(547261)]
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
        [WorkItem(912965)]
        [Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatUsingStatementOnReturn()
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
        [WorkItem(912965)]
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

        [WorkItem(977133)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatRangeButFormatTokenOnOpenBrace()
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
        if (true) {
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(1007071)]
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

        [WorkItem(771761)]
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

        [WorkItem(771761)]
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

        [WorkItem(987373)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSpansIndividuallyWithoutCollapsing()
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
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
            {
                var subjectDocument = workspace.Documents.Single();
                var spans = subjectDocument.SelectedSpans;

                var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var syntaxRoot = document.GetSyntaxRootAsync().Result;

                var node = Formatter.Format(syntaxRoot, spans, workspace);
                Assert.Equal(expected, node.ToFullString());
            }
        }

        [WorkItem(987373)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void FormatSpansWithCollapsing()
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
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
            {
                var subjectDocument = workspace.Documents.Single();
                var spans = subjectDocument.SelectedSpans;
                workspace.Options = workspace.Options.WithChangedOption(FormattingOptions.AllowDisjointSpanMerging, true);

                var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var syntaxRoot = document.GetSyntaxRootAsync().Result;

                var node = Formatter.Format(syntaxRoot, spans, workspace);
                Assert.Equal(expected, node.ToFullString());
            }
        }

        [WorkItem(1044118)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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



        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(449)]
        [WorkItem(1077103)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInThisConstructor()
        {
            var code = @"class Foo
{
    Foo(int s)   :$$   this()
    {
    }

    Foo()
    {
    }
}";

            var expected = @"class Foo
{
    Foo(int s)   :   this()
    {
    }

    Foo()
    {
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464)]
        [WorkItem(908729)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DoNotFormatColonInConditionalOperator()
        {
            var code = @"class Program
{
    static void Main(string[] args)
    {
        var vari = foo()     ?    true  :$$  false;
    }
}";

            var expected = @"class Program
{
    static void Main(string[] args)
    {
        var vari = foo()     ?    true  :  false;
    }
}";
            AssertFormatAfterTypeChar(code, expected);
        }

        [WorkItem(464)]
        [WorkItem(908729)]
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

        [WorkItem(464)]
        [WorkItem(908729)]
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

        private static void AssertFormatAfterTypeChar(string code, string expected, Dictionary<OptionKey, object> changedOptionSet = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
            {
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

                var textUndoHistory = new Mock<ITextUndoHistoryRegistry>();
                var editorOperationsFactory = new Mock<IEditorOperationsFactoryService>();
                var editorOperations = new Mock<IEditorOperations>();
                editorOperationsFactory.Setup(x => x.GetEditorOperations(subjectDocument.GetTextView())).Returns(editorOperations.Object);

                var commandHandler = new FormatCommandHandler(TestWaitIndicator.Default, textUndoHistory.Object, editorOperationsFactory.Object);
                var typedChar = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText(subjectDocument.CursorPosition.Value - 1, 1);
                commandHandler.ExecuteCommand(new TypeCharCommandArgs(subjectDocument.GetTextView(), subjectDocument.TextBuffer, typedChar[0]), () => { });

                var newSnapshot = subjectDocument.TextBuffer.CurrentSnapshot;

                Assert.Equal(expected, newSnapshot.GetText());
            }
        }
    }
}
