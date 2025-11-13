// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting;

[Trait(Traits.Feature, Traits.Features.Formatting)]
public sealed class FormattingEngineTests(ITestOutputHelper output) : CSharpFormattingEngineTestBase(output)
{
    private static OptionsCollection SmartIndentButDoNotFormatWhileTyping()
        => new(LanguageNames.CSharp)
        {
            { IndentationOptionsStorage.SmartIndent, FormattingOptions2.IndentStyle.Smart },
            { AutoFormattingOptionsStorage.FormatOnTyping, false },
            { AutoFormattingOptionsStorage.FormatOnCloseBrace, false },
        };

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539682")]
    public void FormatDocumentCommandHandler()
        => AssertFormatWithView("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                        int y;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                int y;
                    }
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539682")]
    public void FormatDocumentPasteCommandHandler()
        => AssertFormatWithPasteOrReturn("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                        int y;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                int y;
                    }
                }
                """, allowDocumentChanges: true);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547261")]
    public void FormatDocumentReadOnlyWorkspacePasteCommandHandler()
        => AssertFormatWithPasteOrReturn("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                int y;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        int x;$$
                int y;
                    }
                }
                """, allowDocumentChanges: false);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
    public void DoNotFormatUsingStatementOnReturn()
        => AssertFormatWithPasteOrReturn("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                                using (null)$$
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                                using (null)$$
                    }
                }
                """, allowDocumentChanges: true, isPaste: false);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
    public void FormatUsingStatementWhenTypingCloseParen()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                                using (null)$$
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                        using (null)
                    }
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")]
    public void FormatNotUsingStatementOnReturn()
        => AssertFormatWithPasteOrReturn("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                                for (;;)$$
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        using (null)
                                for (;;)$$
                    }
                }
                """, allowDocumentChanges: true, isPaste: false);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/977133")]
    public void DoNotFormatRangeOrFormatTokenOnOpenBraceOnSameLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    public void M()
                    {
                        if (true)        {$$
                    }
                }
                """, """
                class C
                {
                    public void M()
                    {
                        if (true)        {
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/14491")]
    public void DoNotFormatRangeButFormatTokenOnOpenBraceOnNextLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    public void M()
                    {
                        if (true)
                            {$$
                    }
                }
                """, """
                class C
                {
                    public void M()
                    {
                        if (true)
                        {
                    }
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1007071")]
    public void FormatPragmaWarningInBetweenDelegateDeclarationStatement()
        => AssertFormatAfterTypeChar("""
                using System;

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
                }
                """, """
                using System;

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
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")]
    public void FormatHashRegion()
        => AssertFormatAfterTypeChar("""
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                #region$$
                    }
                }
                """, """
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        #region
                    }
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")]
    public void FormatHashEndRegion()
        => AssertFormatAfterTypeChar("""
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        #region
                #endregion$$
                    }
                }
                """, """
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        #region
                        #endregion
                    }
                }
                """);

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/987373")]
    public async Task FormatSpansIndividuallyWithoutCollapsing()
    {
        var code = """
                class C
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
                }
                """;
        using var workspace = EditorTestWorkspace.CreateCSharp(code);
        var subjectDocument = workspace.Documents.Single();
        var spans = subjectDocument.SelectedSpans;

        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var syntaxRoot = await document.GetRequiredSyntaxRootAsync(CancellationToken.None);
        var options = CSharpSyntaxFormattingOptions.Default;
        var node = Formatter.Format(syntaxRoot, spans, workspace.Services.SolutionServices, options, rules: default, CancellationToken.None);
        Assert.Equal("""
                class C
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
                }
                """, node.ToFullString());
    }

    [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1044118")]
    public void SemicolonInCommentOnLastLineDoesNotFormat()
        => AssertFormatAfterTypeChar("""
                using System;

                class Program
                {
                    static void Main(string[] args)
                        {
                        }
                }
                // ;$$
                """, """
                using System;

                class Program
                {
                    static void Main(string[] args)
                        {
                        }
                }
                // ;
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideSingleLineRegularComment_1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              //        {$$
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              //        {
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideSingleLineRegularComment_2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              //        {$$   
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              //        {   
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineRegularComment_1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(int          a/*         {$$       */, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                    static void Main(int          a/*         {       */, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineRegularComment_2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(int          a/*         {$$
                        */, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                    static void Main(int          a/*         {
                        */, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineRegularComment_3()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(int          a/*         {$$    
                        */, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                    static void Main(int          a/*         {    
                        */, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideSingleLineDocComment_1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              ///        {$$
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              ///        {
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideSingleLineDocComment_2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              ///        {$$   
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              ///        {   
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineDocComment_1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              /**        {$$   **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              /**        {   **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineDocComment_2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              /**        {$$   
                                **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              /**        {   
                                **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideMultiLineDocComment_3()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                              /**        {$$
                                **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """, """
                class Program
                {
                                              /**        {
                                **/
                                       static void Main(int a, int b)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideInactiveCode()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                                                        #if false
                                    {$$
                            #endif

                    static void Main(string[] args)
                    {

                    }
                }
                """, """
                class Program
                {
                                                        #if false
                                    {
                            #endif

                    static void Main(string[] args)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideStringLiteral()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var asdas =     "{$$"        ;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        var asdas =     "{"        ;
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideCharLiteral()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var asdas =     '{$$'        ;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        var asdas =     '{'        ;
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/449")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077103")]
    public void NoFormattingInsideCommentsOfPreprocessorDirectives()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                       #region
                        #endregion // a/*{$$*/    
                        static void Main(string[] args)
                    {

                    }
                }
                """, """
                class Program
                {
                       #region
                        #endregion // a/*{*/    
                        static void Main(string[] args)
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void ColonInSwitchCase()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        int f = 0;
                        switch(f)
                        {
                                 case     1     :$$    break;
                        }
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        int f = 0;
                        switch(f)
                        {
                            case 1:    break;
                        }
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void ColonInDefaultSwitchCase()
        => AssertFormatAfterTypeChar("""
                class Program
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
                }
                """, """
                class Program
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
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/9097")]
    public void ColonInPatternSwitchCase01()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main()
                    {
                        switch(f)
                        {
                                          case  int  i            :$$    break;
                        }
                    }
                }
                """, """
                class Program
                {
                    static void Main()
                    {
                        switch(f)
                        {
                            case int i:    break;
                        }
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void ColonInLabeledStatement()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                            label1   :$$   int s = 0;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                    label1: int s = 0;
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInTargetAttribute()
        => AssertFormatAfterTypeChar("""
                using System;
                [method    :$$    C]
                class C : Attribute
                {
                }
                """, """
                using System;
                [method    :    C]
                class C : Attribute
                {
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInBaseList()
        => AssertFormatAfterTypeChar("""
                class C   :$$   Attribute
                {
                }
                """, """
                class C   :   Attribute
                {
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInThisConstructor()
        => AssertFormatAfterTypeChar("""
                class Goo
                {
                    Goo(int s)   :$$   this()
                    {
                    }

                    Goo()
                    {
                    }
                }
                """, """
                class Goo
                {
                    Goo(int s)   :   this()
                    {
                    }

                    Goo()
                    {
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInConditionalOperator()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var vari = goo()     ?    true  :$$  false;
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        var vari = goo()     ?    true  :  false;
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInArgument()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        Main(args  :$$  args);
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        Main(args  :  args);
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/464")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908729")]
    public void DoNotFormatColonInTypeParameter()
        => AssertFormatAfterTypeChar("""
                class Program<T>
                {
                    class C1<U>
                        where   T  :$$  U
                    {

                    }
                }
                """, """
                class Program<T>
                {
                    class C1<U>
                        where   T  :  U
                    {

                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/2224")]
    public void DoNotSmartFormatBracesOnSmartIndentNone()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { IndentationOptionsStorage.SmartIndent, FormattingOptions2.IndentStyle.None }
        };
        AssertFormatAfterTypeChar("""
                class Program<T>
                {
                    class C1<U>
                {$$
                }
                """, """
                class Program<T>
                {
                    class C1<U>
                {
                }
                """, globalOptions);
    }

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void StillAutoIndentCloseBraceWhenFormatOnCloseBraceIsOff()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnCloseBrace, false }
        };

        AssertFormatAfterTypeChar("""
                namespace N
                {
                    class C
                    {
                             // improperly indented code
                             int x = 10;
                        }$$
                }
                """, """
                namespace N
                {
                    class C
                    {
                             // improperly indented code
                             int x = 10;
                    }
                }
                """, globalOptions);
    }

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void AutoIndentCloseBraceWhenFormatOnTypingIsOff()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnTyping, false }
        };

        AssertFormatAfterTypeChar("""
                namespace N
                {
                    class C
                    {
                             // improperly indented code
                             int x = 10;
                        }$$
                }
                """, """
                namespace N
                {
                    class C
                    {
                             // improperly indented code
                             int x = 10;
                    }
                }
                """, globalOptions);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/5873")]
    public void KeepTabsInCommentsWhenFormattingIsOff()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnTyping, false }
        };

        AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main()
                    {
                        return;		/* Comment preceded by tabs */		// This one too
                        }$$
                }
                """, """
                class Program
                {
                    static void Main()
                    {
                        return;		/* Comment preceded by tabs */		// This one too
                    }
                }
                """, globalOptions);
    }

    [WpfFact, Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/5873")]
    public void DoNotKeepTabsInCommentsWhenFormattingIsOn()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    static void Main()
                    {
                        return;		/* Comment preceded by tabs */		// This one too
                        }$$
                }
                """, """
                class Program
                {
                    static void Main()
                    {
                        return;     /* Comment preceded by tabs */        // This one too
                    }
                }
                """);

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void DoNotFormatStatementIfSemicolonOptionIsOff()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnSemicolon, false }
        };

        AssertFormatAfterTypeChar("""
                namespace N
                {
                    class C
                    {
                        int x   =   10     ;$$
                    }
                }
                """, """
                namespace N
                {
                    class C
                    {
                        int x   =   10     ;
                    }
                }
                """, globalOptions);
    }

    [WpfFact]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void DoNotFormatStatementIfTypingOptionIsOff()
    {
        var globalOptions = new OptionsCollection(LanguageNames.CSharp)
        {
            { AutoFormattingOptionsStorage.FormatOnTyping, false }
        };

        AssertFormatAfterTypeChar("""
                namespace N
                {
                    class C
                    {
                        int x   =   10     ;$$
                    }
                }
                """, """
                namespace N
                {
                    class C
                    {
                        int x   =   10     ;
                    }
                }
                """, globalOptions);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4435")]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void OpenCurlyNotFormattedIfNotAtStartOfLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    public  int     P   {$$
                }
                """, """
                class C
                {
                    public  int     P   {
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/4435")]
    [Trait(Traits.Feature, Traits.Features.SmartTokenFormatting)]
    public void OpenCurlyFormattedIfAtStartOfLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    public  int     P
                        {$$
                }
                """, """
                class C
                {
                    public  int     P
                    {
                }
                """);

    [WpfFact]
    public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly1()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true;$$
                    }
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true;
                    }
                }
                """);

    [WpfFact]
    public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly2()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get { return true;$$
                    }
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get { return true;
                    }
                }
                """);

    [WpfFact]
    public void DoNotFormatIncompleteBlockOnSingleLineIfNotTypingCloseCurly3()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get;$$
                    }
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get;
                    }
                }
                """);

    [WpfFact]
    public void DoNotFormatCompleteBlockOnSingleLineIfTypingCloseCurly1()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true; }$$
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true; }
                }
                """);

    [WpfFact]
    public void DoNotFormatCompleteBlockOnSingleLineIfTypingCloseCurly2()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get { return true; }$$
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property { get { return true; }
                }
                """);

    [WpfFact]
    public void FormatIncompleteBlockOnMultipleLinesIfTypingCloseCurly1()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true;
                    }$$
                }
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get
                            {
                                return true;
                            }
                }
                """);

    [WpfFact]
    public void FormatIncompleteBlockOnMultipleLinesIfTypingCloseCurly2()
        => AssertFormatAfterTypeChar("""
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get { return true;
                    }
                }$$
                """, """
                namespace ConsoleApplication1
                {
                    class Program
                    {
                        static bool Property
                        {
                            get
                            {
                                return true;
                            }
                        }
                """);

    [WpfFact]
    public void DoNotFormatCompleteBlockOnSingleLineIfTypingSemicolon()
        => AssertFormatAfterTypeChar("""
                public class Class1
                {
                    void M()
                    {
                        try { }
                        catch { return;$$
                        x.ToString();
                    }
                }
                """, """
                public class Class1
                {
                    void M()
                    {
                        try { }
                        catch { return;
                        x.ToString();
                    }
                }
                """);

    [WpfFact]
    public void FormatCompleteBlockOnSingleLineIfTypingCloseCurlyOnLaterLine()
        => AssertFormatAfterTypeChar("""
                public class Class1
                {
                    void M()
                    {
                        try { }
                        catch { return;
                        x.ToString();
                        }$$
                    }
                }
                """, """
                public class Class1
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
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7900")]
    public void FormatLockStatementWithEmbeddedStatementOnSemicolonDifferentLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    private object _l = new object();
                    public void M()
                    {
                        lock (_l)
                                       Console.WriteLine("d");$$
                    }
                }
                """, """
                class C
                {
                    private object _l = new object();
                    public void M()
                    {
                        lock (_l)
                            Console.WriteLine("d");
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/7900")]
    public void FormatLockStatementWithEmbeddedStatementOnSemicolonSameLine()
        => AssertFormatAfterTypeChar("""
                class C
                {
                    private object _l = new object();
                    public void M()
                    {
                        lock (_l)      Console.WriteLine("d");$$
                    }
                }
                """, """
                class C
                {
                    private object _l = new object();
                    public void M()
                    {
                        lock (_l) Console.WriteLine("d");
                    }
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11642")]
    public void FormatArbitraryNodeParenthesizedLambdaExpression()
    {
        // code equivalent to an expression synthesized like so:
        // ParenthesizedExpression(ParenthesizedLambdaExpression(ParameterList(), Block()))
        var code = @"(()=>{})";
        var node = SyntaxFactory.ParseExpression(code);
        AssertFormatOnArbitraryNode(node, @"(() => { })");
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57465")]
    public Task FormatLambdaWithDirective()
        => AssertFormatAsync("""
                namespace N
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
                """, """
                namespace N
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
                """, spans: null);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/57465")]
    public Task FormatLambdaWithComment()
        => AssertFormatAsync("""
                namespace N
                {
                    public class C
                    {
                        protected void Render()
                        {
                            if (true)
                            {
                                M(() =>
                                {
                                    if (true)
                                    {
                                        /* marker */
                                    }
                                });
                            }
                        }
                    }
                }
                """, """
                namespace N
                {
                    public class C
                    {
                        protected void Render()
                        {
                if (true)
                            {
                            M(() => 
                            {
                                if (true)
                                {
                /* marker */
                                                    }
                                                });
                                }
                        }
                    }
                }
                """, spans: null);

    [WpfTheory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/59637")]
    public async Task FormatAttributeAtEndOfFile(bool trailingNewLine)
    {
        var endOfFile = trailingNewLine ? Environment.NewLine : "";
        var expected = $"""
            using System.Diagnostics.CodeAnalysis;

            [assembly: SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "My reason", Scope = "member", Target = "~M:Method")]{endOfFile}
            """;

        await AssertFormatAsync(expected, $"""
            using System.Diagnostics.CodeAnalysis;

            [assembly:SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "My reason", Scope = "member", Target = "~M:Method") ] {endOfFile}
            """, spans: null);
        await AssertFormatAsync(expected, expected, spans: null);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true)
                            {$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true)
                        {
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true)
                        {}$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true)
                        { }
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff3()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true){$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true){
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff4()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true){}$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true){ }
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff5()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if ( true )
                            {$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if ( true )
                        {
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff6()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if ( true ){$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if ( true ){
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentOpenBraceEvenWithFormatWhileTypingOff7()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                        {$$
                }
                """, """
                class Program
                {
                    void M()
                    {
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff1()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true)
                        {
                            }$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff2()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        if (true) {
                            }$$
                    }
                }
                """, """
                class Program
                {
                    void M()
                    {
                        if (true) {
                        }
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff3()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M()
                    {
                        }$$
                }
                """, """
                class Program
                {
                    void M()
                    {
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30787")]
    public void DoSmartIndentCloseBraceEvenWithFormatWhileTypingOff4()
        => AssertFormatAfterTypeChar("""
                class Program
                {
                    void M() {
                        }$$
                }
                """, """
                class Program
                {
                    void M() {
                    }
                }
                """, SmartIndentButDoNotFormatWhileTyping());

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/31907")]
    public Task NullableReferenceTypes()
        => AssertFormatWithBaseIndentAsync("""

                class MyClass
                {
                    void MyMethod()
                    {
                        var returnType = (_useMethodSignatureReturnType ? _methodSignatureOpt! : method).ReturnType;
                    }
                }

                """, """
                [|
                class MyClass
                {
                    void MyMethod()
                    {
                        var returnType = (_useMethodSignatureReturnType ? _methodSignatureOpt !: method).ReturnType;
                    }
                }
                |]
                """, baseIndentation: 4);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/30518")]
    public void FormatGeneratedNodeInInitializer()
    {
        var code = """
                new bool[] {
                    true,
                    true
                }
                """;
        var tree = SyntaxFactory.ParseSyntaxTree(code, options: TestOptions.Script);
        var root = tree.GetRoot();

        var entry = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression), SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
        var newRoot = root.InsertNodesBefore(root.DescendantNodes().Last(), [entry]);
        AssertFormatOnArbitraryNode(newRoot, """
                new bool[] {
                    true,
                true == false, true
                }
                """);
    }

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/27268")]
    public Task PositionalPattern()
        => AssertFormatWithBaseIndentAsync("""

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

                """, """
                [|
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
                |]
                """, baseIndentation: 4);

    [WpfFact]
    public Task WithExpression()
        => AssertFormatWithBaseIndentAsync("""

                record C(int Property)
                {
                    void M()
                    {
                        _ = this with { Property = 1 };
                    }
                }

                """, """
                [|
                record C(int Property)
                {
                    void M()
                    {
                        _ = this  with  {  Property  =  1  } ;
                    }
                }
                |]
                """, baseIndentation: 4);

    [WpfFact]
    public Task WithExpression_MultiLine()
        => AssertFormatWithBaseIndentAsync("""

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

                """, """
                [|
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
                |]
                """, baseIndentation: 4);

    [WpfFact]
    public Task WithExpression_MultiLine_UserPositionedBraces()
        => AssertFormatWithBaseIndentAsync("""

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

                """, """
                [|
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
                |]
                """, baseIndentation: 4);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public void SeparateGroups_KeepMultipleLinesBetweenGroups()
        => AssertFormatWithView("""
                $$
                using System.A;
                using System.B;


                using MS.A;
                using MS.B;
                """, """
                $$
                using System.A;
                using System.B;


                using MS.A;
                using MS.B;
                """, new OptionsCollection(LanguageNames.CSharp) { { GenerationOptions.SeparateImportDirectiveGroups, true } });

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public void SeparateGroups_KeepMultipleLinesBetweenGroups_FileScopedNamespace()
        => AssertFormatWithView("""
                $$
                namespace N;

                using System.A;
                using System.B;


                using MS.A;
                using MS.B;
                """, """
                $$
                namespace N;

                using System.A;
                using System.B;


                using MS.A;
                using MS.B;
                """, new OptionsCollection(LanguageNames.CSharp) { { GenerationOptions.SeparateImportDirectiveGroups, true } });

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public void SeparateGroups_DoNotGroupIfNotSorted()
        => AssertFormatWithView("""
                $$
                using System.B;
                using System.A;
                using MS.B;
                using MS.A;
                """, """
                $$
                using System.B;
                using System.A;
                using MS.B;
                using MS.A;
                """, new OptionsCollection(LanguageNames.CSharp) { { GenerationOptions.SeparateImportDirectiveGroups, true } });

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public void SeparateGroups_GroupIfSorted()
        => AssertFormatWithView("""
                $$
                using System.A;
                using System.B;

                using MS.A;
                using MS.B;
                """, """
                $$
                using System.A;
                using System.B;
                using MS.A;
                using MS.B;
                """, new OptionsCollection(LanguageNames.CSharp) { { GenerationOptions.SeparateImportDirectiveGroups, true } });

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/25003")]
    public void SeparateGroups_GroupIfSorted_RecognizeSystemNotFirst()
        => AssertFormatWithView("""
                $$
                using MS.A;
                using MS.B;

                using System.A;
                using System.B;
                """, """
                $$
                using MS.A;
                using MS.B;
                using System.A;
                using System.B;
                """, new OptionsCollection(LanguageNames.CSharp) { { GenerationOptions.SeparateImportDirectiveGroups, true } });

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/58157")]
    public void FormatImplicitObjectCollection()
        => AssertFormatWithView("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        $$List<int> list = new()
                        {
                            1, 2, 3, 4,
                        };
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        $$List<int> list = new()
                        {
                            1, 2, 3, 4,
                        };
                    }
                }
                """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49492")]
    public void PreserveAnnotationsOnMultiLineTrivia()
    {
        var text = """
                namespace TestApp
                {
                    class Test
                    {
                    /* __marker__ */
                    }
                }
                """;

        var position = text.IndexOf("/* __marker__ */");
        var syntaxTree = CSharpSyntaxTree.ParseText(text);
        var root = syntaxTree.GetRoot();

        var annotation = new SyntaxAnnotation("marker");
        var markerTrivia = root.FindTrivia(position, findInsideTrivia: true);
        var annotatedMarkerTrivia = markerTrivia.WithAdditionalAnnotations(annotation);
        root = root.ReplaceTrivia(markerTrivia, annotatedMarkerTrivia);

        using var workspace = new AdhocWorkspace();

        var options = CSharpSyntaxFormattingOptions.Default;

        var formattedRoot = Formatter.Format(root, workspace.Services.SolutionServices, options, CancellationToken.None);
        var annotatedTrivia = formattedRoot.GetAnnotatedTrivia("marker");

        Assert.Single(annotatedTrivia);
    }

    [WpfFact]
    public void FormatUserDefinedOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static C operator +(C x, C y)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static C operator + ( C x, C y){
                    }
                }
                """);

    [WpfFact]
    public void FormatUserDefinedUnaryOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static C operator ++(C x)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static C operator ++ ( C x){
                    }
                }
                """);

    [WpfFact]
    public void FormatUserDefinedExplicitCastOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static explicit operator C(int x)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static explicit operator C ( int x){
                    }
                }
                """);

    [WpfFact]
    public void FormatUserDefinedOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1
                {
                    abstract static I1 operator + ( I1 x, I1 y);$$
                }
                """, """
                interface I1
                {
                    abstract static I1 operator +(I1 x, I1 y);
                }
                """);

    [WpfFact]
    public void FormatUserDefinedUnaryOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1
                {
                    abstract static I1 operator ++ ( I1 x);$$
                }
                """, """
                interface I1
                {
                    abstract static I1 operator ++(I1 x);
                }
                """);

    [WpfFact]
    public void FormatUserDefinedExplicitCastOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1<T> where T : I1<T>
                {
                    abstract static explicit operator string ( T x);$$
                }
                """, """
                interface I1<T> where T : I1<T>
                {
                    abstract static explicit operator string(T x);
                }
                """);

    [WpfFact]
    public void FormatUserDefinedCheckedOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static C operator checked +(C x, C y)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static C operator checked + ( C x, C y){
                    }
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUserDefinedCheckedUnaryOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static C operator checked ++(C x)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static C operator checked ++ ( C x){
                    }
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUserDefinedExplicitCheckedCastOperator()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static explicit operator checked C(int x)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static explicit operator checked C ( int x){
                    }
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUserDefinedCheckedOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1
                {
                    abstract static I1 operator checked + ( I1 x, I1 y);$$
                }
                """, """
                interface I1
                {
                    abstract static I1 operator checked +(I1 x, I1 y);
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUserDefinedCheckedUnaryOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1
                {
                    abstract static I1 operator checked ++ ( I1 x);$$
                }
                """, """
                interface I1
                {
                    abstract static I1 operator checked ++(I1 x);
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUserDefinedExplicitCheckedCastOperatorOnType()
        => AssertFormatAfterTypeChar("""
                interface I1<T> where T : I1<T>
                {
                    abstract static explicit operator checked string ( T x);$$
                }
                """, """
                interface I1<T> where T : I1<T>
                {
                    abstract static explicit operator checked string(T x);
                }
                """, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12));

    [WpfFact]
    public void FormatUnsignedRightShift()
        => AssertFormatWithView("""
                $$
                class C
                {
                    public static C operator >>>(C x, C y)
                    {
                    }
                }
                """, """
                $$
                class C
                {
                    public static C operator>>> ( C x, C y){
                    }
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceIncrementOperator([CombinatorialValues("++", "--")] string op)
        => AssertFormatWithView($$$"""
                $$
                class C
                {
                    public void operator {{{op}}}()
                    {
                    }
                }
                """, $$$"""
                $$
                class C
                {
                    public void operator{{{op}}} ( ){
                    }
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceIncrementOperator_Checked([CombinatorialValues("++", "--")] string op)
        => AssertFormatWithView($$$"""
                $$
                class C
                {
                    public void operator checked {{{op}}}()
                    {
                    }
                }
                """, $$$"""
                $$
                class C
                {
                    public void operator  checked{{{op}}} ( ){
                    }
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceCompoundAssignmentOperator([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => AssertFormatWithView($$$"""
                $$
                class C
                {
                    public void operator {{{op}}}(C x)
                    {
                    }
                }
                """, $$$"""
                $$
                class C
                {
                    public void operator{{{op}}} ( C x ){
                    }
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceCompoundAssignmentOperator_Checked([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => AssertFormatWithView($$$"""
                $$
                class C
                {
                    public void operator checked {{{op}}}(C x)
                    {
                    }
                }
                """, $$$"""
                $$
                class C
                {
                    public void operator  checked{{{op}}} ( C x ){
                    }
                }
                """);

    [WpfFact]
    public void FormatCollectionExpressionAfterEquals()
        => AssertFormatWithView("""
                $$
                var v = [1, 2, 3];
                """, """
                $$
                var   v  =   [  1  , 2  , 3  ]  ;
                """);

    [WpfFact]
    public void FormatCollectionExpressionAfterEquals2()
        => AssertFormatWithView("""
                class C
                {
                    void M()
                    {
                        List<int> list = [];$$
                    }
                }
                """, """
                class C
                {
                    void M()
                    {
                        List<int> list = [     ]  ;$$
                    }
                }
                """);

    [WpfFact]
    public void FormatUnsignedRightShiftOnType()
        => AssertFormatAfterTypeChar("""
                interface I1
                {
                    abstract static I1 operator >>> ( I1 x, I1 y);$$
                }
                """, """
                interface I1
                {
                    abstract static I1 operator >>>(I1 x, I1 y);
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceIncrementOperatorOnType([CombinatorialValues("++", "--")] string op)
        => AssertFormatAfterTypeChar($$$"""
                interface I1
                {
                    abstract void operator{{{op}}} ( );$$
                }
                """, $$$"""
                interface I1
                {
                    abstract void operator {{{op}}}();
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceIncrementOperatorOnType_Checked([CombinatorialValues("++", "--")] string op)
        => AssertFormatAfterTypeChar($$$"""
                interface I1
                {
                    abstract void operator  checked{{{op}}} ( );$$
                }
                """, $$$"""
                interface I1
                {
                    abstract void operator checked {{{op}}}();
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceCompoundAssignmentOperatorOnType([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => AssertFormatAfterTypeChar($$$"""
                interface I1
                {
                    abstract void operator{{{op}}} ( I1 x );$$
                }
                """, $$$"""
                interface I1
                {
                    abstract void operator {{{op}}}(I1 x);
                }
                """);

    [WpfTheory]
    [CombinatorialData]
    public void FormatInstanceCompoundAssignmentOperatorOnType_Checked([CombinatorialValues("+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>=")] string op)
        => AssertFormatAfterTypeChar($$$"""
                interface I1
                {
                    abstract void operator  checked{{{op}}} ( I1 x );$$
                }
                """, $$$"""
                interface I1
                {
                    abstract void operator checked {{{op}}}(I1 x);
                }
                """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/13981")]
    public void FormatLabeledStatementAfterColon()
        => AssertFormatAfterTypeChar("""
            class C
            {
                void M()
                {
                        foo:$$
                }
            }
            """, """
            class C
            {
                void M()
                {
                foo:
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35105")]
    public void FormatLabelAfterIfStatementWithoutBraces()
        => AssertFormatWithView("""
            class Test
            {
                static void Test()
                {
                    Test();

                label1:
                    Test();
                    if (true)
                        Test();

                        label2:$$
                    Test();

                label3:
                    if (true)
                    {
                        Test();
                    }

                label4:
                    Test();
                }
            }
            """, """
            class Test
            {
                static void Test()
                {
                    Test();

                label1:
                    Test();
                    if (true)
                        Test();

                        label2:$$
                    Test();

                label3:
                    if (true)
                    {
                        Test();
                    }

                label4:
                    Test();
                }
            }
            """);

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/67011")]
    public void FormatClassAfterSemicolon()
        => AssertFormatAfterTypeChar("""
            class C3 ( int a3,int  a4 );$$
            """, """
            class C3(int a3, int a4);
            """);

    private static void AssertFormatAfterTypeChar(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string expected,
        OptionsCollection? globalOptions = null,
        ParseOptions? parseOptions = null)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(code, parseOptions: parseOptions);

        var subjectDocument = workspace.Documents.Single();

        var commandHandler = workspace.GetService<FormatCommandHandler>();
        var typedChar = subjectDocument.GetTextBuffer().CurrentSnapshot.GetText(subjectDocument.CursorPosition!.Value - 1, 1);
        var textView = subjectDocument.GetTextView();

        globalOptions?.SetGlobalOptions(workspace.GlobalOptions);
        workspace.GlobalOptions.SetEditorOptions(textView.Options.GlobalOptions, subjectDocument.Project.Language);

        commandHandler.ExecuteCommand(new TypeCharCommandArgs(textView, subjectDocument.GetTextBuffer(), typedChar[0]), () => { }, TestCommandExecutionContext.Create());

        var newSnapshot = subjectDocument.GetTextBuffer().CurrentSnapshot;

        AssertEx.EqualOrDiff(expected, newSnapshot.GetText());
    }
}
