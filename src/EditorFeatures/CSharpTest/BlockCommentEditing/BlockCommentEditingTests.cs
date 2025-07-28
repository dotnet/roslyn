// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentEditing;

[Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
public sealed class BlockCommentEditingTests : AbstractTypingCommandHandlerTest<ReturnKeyCommandArgs>
{
    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11057")]
    public void EdgeCase0()
        => Verify(@"
$$/**/
", @"

$$/**/
");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11057")]
    public void EdgeCase1()
        => Verify(@"
/**/$$
", @"
/**/
$$
");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11056")]
    public void EdgeCase2()
        => Verify(@"
$$/* */
", @"

$$/* */
");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/11056")]
    public void EdgeCase3()
        => Verify(@"
/* */$$
", @"
/* */
$$
");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
    public void EofCase0()
        => Verify(@"
/* */$$", @"
/* */
$$");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
    public void EofCase1()
        => Verify(@"
    /*$$", @"
    /*
     * $$");

    [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/16128")]
    public void EofCase2()
        => Verify(@"
    /***$$", @"
    /***
     * $$");

    [WpfFact]
    public void InsertOnStartLine0()
        => Verify(@"
    /*$$
", @"
    /*
     * $$
");

    [WpfFact]
    public void InsertOnStartLine1()
        => Verify(@"
    /*$$*/
", @"
    /*
     $$*/
");

    [WpfFact]
    public void InsertOnStartLine2()
        => Verify(@"
    /*$$ */
", @"
    /*
     * $$*/
");

    [WpfFact]
    public void InsertOnStartLine3()
        => Verify(@"
    /* $$ 1.
     */
", @"
    /* 
     * $$1.
     */
");

    [WpfFact]
    public void InsertOnStartLine4()
        => Verify(@"
    /*  1.$$
     */
", @"
    /*  1.
     *  $$
     */
");

    [WpfFact]
    public void InsertOnStartLine5()
        => Verify(@"
    /********$$
", @"
    /********
     * $$
");

    [WpfFact]
    public void InsertOnStartLine6()
        => Verify(@"
    /**$$
", @"
    /**
     * $$
");

    [WpfFact]
    public void InsertOnStartLine7()
        => Verify(@"
    /*   $$
", @"
    /*   
     *   $$
");

    [WpfFact]
    public void NotInsertOnStartLine0()
        => Verify(@"
    /$$*
     */
", @"
    /
$$*
     */
");

    [WpfFact]
    public void InsertOnMiddleLine0()
        => Verify(@"
    /*
     *$$
", @"
    /*
     *
     *$$
");

    [WpfFact]
    public void InsertOnMiddleLine1()
        => Verify(@"
    /*
     *$$*/
", @"
    /*
     *
     $$*/
");

    [WpfFact]
    public void InsertOnMiddleLine2()
        => Verify(@"
    /*
     *$$ */
", @"
    /*
     *
     *$$*/
");

    [WpfFact]
    public void InsertOnMiddleLine3()
        => Verify(@"
    /*
     * $$ 1.
     */
", @"
    /*
     * 
     * $$1.
     */
");

    [WpfFact]
    public void InsertOnMiddleLine4()
        => Verify(@"
    /*
     *  1.$$
     */
", @"
    /*
     *  1.
     *  $$
     */
");

    [WpfFact]
    public void InsertOnMiddleLine5()
        => Verify(@"
    /*
     *   1.
     *   $$
     */
", @"
    /*
     *   1.
     *   
     *   $$
     */
");

    [WpfFact]
    public void InsertOnMiddleLine6()
        => Verify(@"
    /*
  $$   *
     */
", @"
    /*
  
     $$*
     */
");

    [WpfFact]
    public void InsertOnMiddleLine7()
        => Verify(@"
    /*
     *************$$
     */
", @"
    /*
     *************
     *$$
     */
");

    [WpfFact]
    public void InsertOnMiddleLine8()
        => Verify(@"
    /**
     *$$
     */
", @"
    /**
     *
     *$$
     */
");

    [WpfFact]
    public void InsertOnMiddleLine9()
        => Verify(@"
    /**
      *$$
", @"
    /**
      *
      *$$
");

    [WpfFact]
    public void InsertOnEndLine0()
        => Verify(@"
    /*
     *$$/
", @"
    /*
     *
     *$$/
");

    [WpfFact]
    public void InsertOnEndLine1()
        => Verify(@"
    /**
     *$$/
", @"
    /**
     *
     *$$/
");

    [WpfFact]
    public void InsertOnEndLine2()
        => Verify(@"
    /**
      *
      *$$/
", @"
    /**
      *
      *
      *$$/
");

    [WpfFact]
    public void InsertOnEndLine3()
        => Verify(@"
    /*
  $$   */
", @"
    /*
  
     $$*/
");

    [WpfFact]
    public void InsertOnEndLine4()
        => Verify(@"
    /*
     $$*/
", @"
    /*
     
     $$*/
");

    [WpfFact]
    public void NotInsertInVerbatimString0()
        => Verify(@"
var code = @""
/*$$
"";
", @"
var code = @""
/*
$$
"";
");

    [WpfFact]
    public void NotInsertInVerbatimString1()
        => Verify(@"
var code = @""
/*
 *$$
"";
", @"
var code = @""
/*
 *
$$
"";
");

    [WpfFact]
    public void BoundCheckInsertOnStartLine0()
        => Verify(@"
    /$$*", @"
    /
$$*");

    [WpfFact]
    public void BoundCheckInsertOnStartLine1()
        => Verify(@"
    /*$$ ", @"
    /*
     * $$");

    [WpfFact]
    public void BoundCheckInsertOnMiddleLine()
        => Verify(@"
    /*
     *$$ ", @"
    /*
     *
     *$$");

    [WpfFact]
    public void BoundCheckInsertOnEndLine()
        => Verify(@"
    /*
     *$$/", @"
    /*
     *
     *$$/");

    [WpfFact]
    public void InsertOnStartLine2_Tab()
        => VerifyTabs(@"
    /*$$<tab>*/
", @"
    /*
     * $$*/
");

    [WpfFact]
    public void InsertOnStartLine3_Tab()
        => VerifyTabs(@"
    /*<tab>$$<tab>1.
     */
", @"
    /*<tab>
     *<tab>$$1.
     */
");

    [WpfFact]
    public void InsertOnStartLine4_Tab()
        => VerifyTabs(@"
    /* <tab>1.$$
     */
", @"
    /* <tab>1.
     * <tab>$$
     */
");

    [WpfFact]
    public void InsertOnStartLine6_Tab()
        => VerifyTabs(@"
    /*<tab>$$
", @"
    /*<tab>
     *<tab>$$
");

    [WpfFact]
    public void InsertOnMiddleLine2_Tab()
        => VerifyTabs(@"
    /*
     *$$<tab>*/
", @"
    /*
     *
     *$$*/
");

    [WpfFact]
    public void InsertOnMiddleLine3_Tab()
        => VerifyTabs(@"
    /*
     * $$<tab>1.
     */
", @"
    /*
     * 
     * $$1.
     */
");

    [WpfFact]
    public void InsertOnMiddleLine4_Tab()
        => VerifyTabs(@"
    /*
     * <tab>1.$$
     */
", @"
    /*
     * <tab>1.
     * <tab>$$
     */
");

    [WpfFact]
    public void InsertOnMiddleLine5_Tab()
        => VerifyTabs(@"
    /*
     *<tab> 1.
     *<tab> $$
     */
", @"
    /*
     *<tab> 1.
     *<tab> 
     *<tab> $$
     */
");

    [WpfFact]
    public void InLanguageConstructTrailingTrivia()
        => Verify(@"
class C
{
    int i; /*$$
}
", @"
class C
{
    int i; /*
            * $$
}
");

    [WpfFact]
    public void InLanguageConstructTrailingTrivia_Tabs()
        => VerifyTabs(@"
class C
{
<tab>int i; /*$$
}
", @"
class C
{
<tab>int i; /*
<tab>        * $$
}
");

    protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
        => EditorTestWorkspace.CreateCSharp(initialMarkup);

    protected override (ReturnKeyCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
        => (new ReturnKeyCommandArgs(textView, textBuffer), "\r\n");

    internal override ICommandHandler<ReturnKeyCommandArgs> GetCommandHandler(EditorTestWorkspace workspace)
        => Assert.IsType<BlockCommentEditingCommandHandler>(workspace.GetService<ICommandHandler>(ContentTypeNames.CSharpContentType, nameof(BlockCommentEditingCommandHandler)));
}
