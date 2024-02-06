// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentEditing
{
    [Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
    public class BlockCommentEditingTests : AbstractTypingCommandHandlerTest<ReturnKeyCommandArgs>
    {
        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11057")]
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11057")]
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11056")]
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11056")]
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
        public void EofCase0()
        {
            var code = @"
/* */$$";
            var expected = @"
/* */
$$";
            Verify(code, expected);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
        public void EofCase1()
        {
            var code = @"
    /*$$";
            var expected = @"
    /*
     * $$";
            Verify(code, expected);
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
        public void EofCase2()
        {
            var code = @"
    /***$$";
            var expected = @"
    /***
     * $$";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InsertOnStartLine2()
        {
            var code = @"
    /*$$ */
";
            var expected = @"
    /*
     * $$*/
";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InsertOnMiddleLine0()
        {
            var code = @"
    /*
     *$$
";
            var expected = @"
    /*
     *
     *$$
";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InsertOnMiddleLine6()
        {
            var code = @"
    /*
  $$   *
     */
";
            var expected = @"
    /*
  
     $$*
     */
";
            Verify(code, expected);
        }

        [WpfFact]
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
     *$$
     */
";
            Verify(code, expected);
        }

        [WpfFact]
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
     *$$
     */
";
            Verify(code, expected);
        }

        [WpfFact]
        public void InsertOnMiddleLine9()
        {
            var code = @"
    /**
      *$$
";
            var expected = @"
    /**
      *
      *$$
";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InsertOnEndLine3()
        {
            var code = @"
    /*
  $$   */
";
            var expected = @"
    /*
  
     $$*/
";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void BoundCheckInsertOnStartLine0()
        {
            var code = @"
    /$$*";
            var expected = @"
    /
$$*";
            Verify(code, expected);
        }

        [WpfFact]
        public void BoundCheckInsertOnStartLine1()
        {
            var code = @"
    /*$$ ";
            var expected = @"
    /*
     * $$";
            Verify(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InsertOnStartLine2_Tab()
        {
            var code = @"
    /*$$<tab>*/
";
            var expected = @"
    /*
     * $$*/
";
            VerifyTabs(code, expected);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void InLanguageConstructTrailingTrivia()
        {
            var code = @"
class C
{
    int i; /*$$
}
";
            var expected = @"
class C
{
    int i; /*
            * $$
}
";
            Verify(code, expected);
        }

        [WpfFact]
        public void InLanguageConstructTrailingTrivia_Tabs()
        {
            var code = @"
class C
{
<tab>int i; /*$$
}
";
            var expected = @"
class C
{
<tab>int i; /*
<tab>        * $$
}
";
            VerifyTabs(code, expected);
        }

        protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
            => EditorTestWorkspace.CreateCSharp(initialMarkup);

        protected override (ReturnKeyCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
            => (new ReturnKeyCommandArgs(textView, textBuffer), "\r\n");

        internal override ICommandHandler<ReturnKeyCommandArgs> GetCommandHandler(EditorTestWorkspace workspace)
            => Assert.IsType<BlockCommentEditingCommandHandler>(workspace.GetService<ICommandHandler>(ContentTypeNames.CSharpContentType, nameof(BlockCommentEditingCommandHandler)));
    }
}
