// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualCSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpEditing : AbstractEditorTest
    {
        private const string TestSource = @"using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApplication1
{
    $$public class Program
    {
        /// <summary/>
        public static void Main(string[] args)
        {
            /* Console.WriteLine(""Uncomment"");*/
            Console.WriteLine(""Hello World"");
        }
    }
}";

        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpEditing(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpEditing))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void IndentUnindentDoNotIntroduceErrors()
        {
            SetUpEditor(TestSource);

            VisualStudio.ExecuteCommand("Edit.SelectAll");
            VisualStudio.ExecuteCommand("Edit.InsertTab");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            Assert.Equal(PrependLines(TestSource, "    "), VisualStudio.Editor.GetText());
            VisualStudio.ExecuteCommand("Edit.TabLeft");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            MarkupTestFile.GetPosition(TestSource, out string source, out int _);
            Assert.Equal(source, VisualStudio.Editor.GetText());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void CommentUncommentDoNotIntroduceErrors()
        {
            SetUpEditor(TestSource);
            VisualStudio.ExecuteCommand("Edit.SelectAll");
            VisualStudio.ExecuteCommand("Edit.CommentSelection");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            Assert.Equal(PrependLines(TestSource, @"//"), VisualStudio.Editor.GetText());
            VisualStudio.ExecuteCommand("Edit.UncommentSelection");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            MarkupTestFile.GetPosition(TestSource, out string source, out int _);
            Assert.Equal(source, VisualStudio.Editor.GetText());

            VisualStudio.Editor.PlaceCaret("Uncomment");
            VisualStudio.ExecuteCommand("Edit.UncommentSelection");
            VisualStudio.Editor.Verify.CurrentLineText(@"Console.WriteLine(""Uncomment"");", trimWhitespace: true);

        }

        [Fact, Trait(Traits.Feature, Traits.Features.Formatting)]
        public void DocCommentGeneration()
        {
            SetUpEditor(TestSource);
            VisualStudio.Editor.SendKeys(VirtualKey.Up, VirtualKey.Enter, "///");
            VisualStudio.Workspace.WaitForAllAsyncOperations();
            VisualStudio.Editor.Verify.TextContains(@"/// <summary>
    /// 
    /// </summary>");

            VisualStudio.Editor.SendKeys("comment!", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"    /// <summary>
    /// comment!
    /// 
    /// </summary>");
        }
    }
}