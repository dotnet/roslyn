// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Implementation.Formatting;
using Microsoft.CodeAnalysis.Editor.Options;
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
        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
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
        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void NoFormattingInsideCommentsOfPreprocessorDirectves()
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

        private static void AssertFormatAfterTypeChar(string code, string expected)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(code))
            {
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
