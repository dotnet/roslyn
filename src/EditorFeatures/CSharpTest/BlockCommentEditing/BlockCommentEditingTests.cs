// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentEditing
{
    public class BlockCommentEditingTests : AbstractTypingCommandHandlerTest<ReturnKeyCommandArgs>
    {
        [WorkItem(11057, "https://github.com/dotnet/roslyn/issues/11057")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EdgeCase0()
        {
            var code = @"
$$/**/
";
            var expected = @"

$$/**/
";
            Verify(code, expected);
        }

        [WorkItem(11057, "https://github.com/dotnet/roslyn/issues/11057")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EdgeCase1()
        {
            var code = @"
/**/$$
";
            var expected = @"
/**/
$$
";
            Verify(code, expected);
        }

        [WorkItem(11056, "https://github.com/dotnet/roslyn/issues/11056")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EdgeCase2()
        {
            var code = @"
$$/* */
";
            var expected = @"

$$/* */
";
            Verify(code, expected);
        }

        [WorkItem(11056, "https://github.com/dotnet/roslyn/issues/11056")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EdgeCase3()
        {
            var code = @"
/* */$$
";
            var expected = @"
/* */
$$
";
            Verify(code, expected);
        }

        [WorkItem(16128, "https://github.com/dotnet/roslyn/issues/16128")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EofCase0()
        {
            var code = @"
/* */$$";
            var expected = @"
/* */
$$";
            Verify(code, expected);
        }

        [WorkItem(16128, "https://github.com/dotnet/roslyn/issues/16128")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EofCase1()
        {
            var code = @"
    /*$$";
            var expected = @"
    /*
     * $$";
            Verify(code, expected);
        }

        [WorkItem(16128, "https://github.com/dotnet/roslyn/issues/16128")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void EofCase2()
        {
            var code = @"
    /***$$";
            var expected = @"
    /***
     * $$";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine0()
        {
            var code = @"
    /*$$
";
            var expected = @"
    /*
     * $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine1()
        {
            var code = @"
    /*$$*/
";
            var expected = @"
    /*
     $$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine2()
        {
            var code = @"
    /*$$ */
";
            var expected = @"
    /*
     *$$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine3()
        {
            var code = @"
    /* $$ 1.
     */
";
            var expected = @"
    /* 
     * $$1.
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine4()
        {
            var code = @"
    /*  1.$$
     */
";
            var expected = @"
    /*  1.
     *  $$
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine5()
        {
            var code = @"
    /********$$
";
            var expected = @"
    /********
     * $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine6()
        {
            var code = @"
    /**$$
";
            var expected = @"
    /**
     * $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine7()
        {
            var code = @"
    /*   $$
";
            var expected = @"
    /*   
     *   $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotInsertOnStartLine0()
        {
            var code = @"
    /$$*
     */
";
            var expected = @"
    /
$$*
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine0()
        {
            var code = @"
    /*
     *$$
";
            var expected = @"
    /*
     *
     * $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine1()
        {
            var code = @"
    /*
     *$$*/
";
            var expected = @"
    /*
     *
     $$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine2()
        {
            var code = @"
    /*
     *$$ */
";
            var expected = @"
    /*
     *
     *$$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine3()
        {
            var code = @"
    /*
     * $$ 1.
     */
";
            var expected = @"
    /*
     * 
     * $$1.
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine4()
        {
            var code = @"
    /*
     *  1.$$
     */
";
            var expected = @"
    /*
     *  1.
     *  $$
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine5()
        {
            var code = @"
    /*
     *   1.
     *   $$
     */
";
            var expected = @"
    /*
     *   1.
     *   
     *   $$
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine6()
        {
            var code = @"
    /*
  $$   *
     */
";
            var expected = @"
    /*
  
     * $$*
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine7()
        {
            var code = @"
    /*
     *************$$
     */
";
            var expected = @"
    /*
     *************
     * $$
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine8()
        {
            var code = @"
    /**
     *$$
     */
";
            var expected = @"
    /**
     *
     * $$
     */
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine9()
        {
            var code = @"
    /**
      *$$
";
            var expected = @"
    /**
      *
      * $$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnEndLine0()
        {
            var code = @"
    /*
     *$$/
";
            var expected = @"
    /*
     *
     *$$/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnEndLine1()
        {
            var code = @"
    /**
     *$$/
";
            var expected = @"
    /**
     *
     *$$/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnEndLine2()
        {
            var code = @"
    /**
      *
      *$$/
";
            var expected = @"
    /**
      *
      *
      *$$/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnEndLine3()
        {
            var code = @"
    /*
  $$   */
";
            var expected = @"
    /*
  
     * $$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnEndLine4()
        {
            var code = @"
    /*
     $$*/
";
            var expected = @"
    /*
     
     $$*/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotInsertInVerbatimString0()
        {
            var code = @"
var code = @""
/*$$
"";
";
            var expected = @"
var code = @""
/*
$$
"";
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotInsertInVerbatimString1()
        {
            var code = @"
var code = @""
/*
 *$$
"";
";
            var expected = @"
var code = @""
/*
 *
$$
"";
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void BoundCheckInsertOnStartLine0()
        {
            var code = @"
    /$$*";
            var expected = @"
    /
$$*";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void BoundCheckInsertOnStartLine1()
        {
            var code = @"
    /*$$ ";
            var expected = @"
    /*
     *$$";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void BoundCheckInsertOnMiddleLine()
        {
            var code = @"
    /*
     *$$ ";
            var expected = @"
    /*
     *
     *$$";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void BoundCheckInsertOnEndLine()
        {
            var code = @"
    /*
     *$$/";
            var expected = @"
    /*
     *
     *$$/";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine2_Tab()
        {
            var code = @"
    /*$$<tab>*/
";
            var expected = @"
    /*
     *$$*/
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine3_Tab()
        {
            var code = @"
    /*<tab>$$<tab>1.
     */
";
            var expected = @"
    /*<tab>
     *<tab>$$1.
     */
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine4_Tab()
        {
            var code = @"
    /* <tab>1.$$
     */
";
            var expected = @"
    /* <tab>1.
     * <tab>$$
     */
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnStartLine6_Tab()
        {
            var code = @"
    /*<tab>$$
";
            var expected = @"
    /*<tab>
     *<tab>$$
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine2_Tab()
        {
            var code = @"
    /*
     *$$<tab>*/
";
            var expected = @"
    /*
     *
     *$$*/
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine3_Tab()
        {
            var code = @"
    /*
     * $$<tab>1.
     */
";
            var expected = @"
    /*
     * 
     * $$1.
     */
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine4_Tab()
        {
            var code = @"
    /*
     * <tab>1.$$
     */
";
            var expected = @"
    /*
     * <tab>1.
     * <tab>$$
     */
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void InsertOnMiddleLine5_Tab()
        {
            var code = @"
    /*
     *<tab> 1.
     *<tab> $$
     */
";
            var expected = @"
    /*
     *<tab> 1.
     *<tab> 
     *<tab> $$
     */
";
            VerifyTabs(code, expected);
        }

        protected override TestWorkspace CreateTestWorkspace(string initialMarkup)
            => TestWorkspace.CreateCSharp(initialMarkup);

        protected override (ReturnKeyCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
            => (new ReturnKeyCommandArgs(textView, textBuffer), "\r\n");

        internal override VSCommanding.ICommandHandler<ReturnKeyCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService)
            => new BlockCommentEditingCommandHandler(undoHistoryRegistry, editorOperationsFactoryService);
    }
}
