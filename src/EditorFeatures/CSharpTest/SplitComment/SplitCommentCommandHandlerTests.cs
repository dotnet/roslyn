// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitComment;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SplitComment
{
    [UseExportProvider]
    public class SplitCommentCommandHandlerTests
    {
        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issure. This bug does not represent a product
        /// failure.
        /// </summary>
        private void TestWorker(
            string inputMarkup,
            string expectedOutputMarkup,
            Action callback,
            bool verifyUndo = true,
            IndentStyle indentStyle = IndentStyle.Smart)
        {
            using var workspace = TestWorkspace.CreateCSharp(inputMarkup);
            workspace.Options = workspace.Options.WithChangedOption(SmartIndent, LanguageNames.CSharp, indentStyle);

            var document = workspace.Documents.Single();
            var view = document.GetTextView();

            var originalSnapshot = view.TextBuffer.CurrentSnapshot;
            var originalSelections = document.SelectedSpans;

            var snapshotSpans = new List<SnapshotSpan>();
            foreach (var selection in originalSelections)
            {
                snapshotSpans.Add(selection.ToSnapshotSpan(originalSnapshot));
            }
            view.SetMultiSelection(snapshotSpans);

            var undoHistoryRegistry = workspace.GetService<ITextUndoHistoryRegistry>();
            var commandHandler = new SplitCommentCommandHandler(
                undoHistoryRegistry,
                workspace.GetService<IEditorOperationsFactoryService>());

            if (!commandHandler.ExecuteCommand(new ReturnKeyCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create()))
            {
                callback();
            }

            if (expectedOutputMarkup != null)
            {
                MarkupTestFile.GetSpans(expectedOutputMarkup,
                    out var expectedOutput, out ImmutableArray<TextSpan> expectedSpans);

                Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString());

                if (verifyUndo)
                {
                    // Ensure that after undo we go back to where we were to begin with.
                    var history = undoHistoryRegistry.GetHistory(document.GetTextBuffer());
                    history.Undo(count: originalSelections.Count);

                    var currentSnapshot = document.GetTextBuffer().CurrentSnapshot;
                    Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText());
                }
            }
        }

        /// <summary>
        /// verifyUndo is needed because of https://github.com/dotnet/roslyn/issues/28033
        /// Most tests will continue to verifyUndo, but select tests will skip it due to
        /// this known test infrastructure issure. This bug does not represent a product
        /// failure.
        /// </summary>
        private void TestHandled(
            string inputMarkup, string expectedOutputMarkup,
            bool verifyUndo = true, IndentStyle indentStyle = IndentStyle.Smart)
        {
            TestWorker(
                inputMarkup, expectedOutputMarkup,
                callback: () =>
                {
                    Assert.True(false, "Should not reach here.");
                },
                verifyUndo, indentStyle);
        }

        private void TestNotHandled(string inputMarkup)
        {
            var notHandled = false;
            TestWorker(
                inputMarkup, null,
                callback: () =>
                {
                    notHandled = true;
                });

            Assert.True(notHandled);
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitStartOfComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //[||] Test Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        //
        // Test Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitMiddleOfComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test [||]Comment
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test 
        //Comment
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitEndOfComment()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test Comment[||] 
    }
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        // Test Comment
        // 
    }
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentOutsideOfMethod()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
    // Test [||]Comment
}",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
    // Test 
    //Comment
}");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentOutsideOfClass()
        {
            TestHandled(
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
}
// Test [||]Comment
",
@"public class Program
{
    public static void Main(string[] args) 
    { 
        
    }
}
// Test 
//Comment
");
        }

        [WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)]
        public void TestSplitCommentOutsideOfNamespace()
        {
            TestHandled(
@"namespace TestNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }
}
// Test [||]Comment
",
@"namespace TestNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }
}
// Test 
//Comment
");
        }
    }
}
