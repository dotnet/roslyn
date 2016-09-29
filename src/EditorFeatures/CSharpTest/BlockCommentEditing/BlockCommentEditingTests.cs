﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentEditing
{
    public class BlockCommentEditingTests : AbstractBlockCommentEditingTests
    {
        [WorkItem(11057, "https://github.com/dotnet/roslyn/issues/11057")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task EdgeCase0()
        {
            var code = @"
$$/**/
";
            var expected = @"

$$/**/
";
            await VerifyAsync(code, expected);
        }

        [WorkItem(11057, "https://github.com/dotnet/roslyn/issues/11057")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task EdgeCase1()
        {
            var code = @"
/**/$$
";
            var expected = @"
/**/
$$
";
            await VerifyAsync(code, expected);
        }

        [WorkItem(11056, "https://github.com/dotnet/roslyn/issues/11056")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task EdgeCase2()
        {
            var code = @"
$$/* */
";
            var expected = @"

$$/* */
";
            await VerifyAsync(code, expected);
        }

        [WorkItem(11056, "https://github.com/dotnet/roslyn/issues/11056")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task EdgeCase3()
        {
            var code = @"
/* */$$
";
            var expected = @"
/* */
$$
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine0()
        {
            var code = @"
    /*$$
";
            var expected = @"
    /*
     * $$
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine1()
        {
            var code = @"
    /*$$*/
";
            var expected = @"
    /*
     $$*/
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine2()
        {
            var code = @"
    /*$$ */
";
            var expected = @"
    /*
     *$$ */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine3()
        {
            var code = @"
    /* $$ 1.
     */
";
            var expected = @"
    /* 
     * $$ 1.
     */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine4()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine5()
        {
            var code = @"
    /********$$
";
            var expected = @"
    /********
     * $$
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine6()
        {
            var code = @"
    /**$$
";
            var expected = @"
    /**
     * $$
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine7()
        {
            var code = @"
    /*   $$
";
            var expected = @"
    /*   
     *   $$
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task NotInsertOnStartLine0()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine0()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine1()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine2()
        {
            var code = @"
    /*
     *$$ */
";
            var expected = @"
    /*
     *
     *$$ */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine3()
        {
            var code = @"
    /*
     * $$ 1.
     */
";
            var expected = @"
    /*
     * 
     * $$ 1.
     */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine4()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine5()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine6()
        {
            var code = @"
    /*
  $$   *
     */
";
            var expected = @"
    /*
  
     * $$   *
     */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine7()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine8()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine9()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnEndLine0()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnEndLine1()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnEndLine2()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnEndLine3()
        {
            var code = @"
    /*
  $$   */
";
            var expected = @"
    /*
  
     * $$   */
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnEndLine4()
        {
            var code = @"
    /*
     $$*/
";
            var expected = @"
    /*
     
     $$*/
";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task NotInsertInVerbatimString0()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task NotInsertInVerbatimString1()
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
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task BoundCheckInsertOnStartLine0()
        {
            var code = @"
    /$$*";
            var expected = @"
    /
$$*";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task BoundCheckInsertOnStartLine1()
        {
            var code = @"
    /*$$ ";
            var expected = @"
    /*
     *$$ ";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task BoundCheckInsertOnMiddleLine()
        {
            var code = @"
    /*
     *$$ ";
            var expected = @"
    /*
     *
     *$$ ";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task BoundCheckInsertOnEndLine()
        {
            var code = @"
    /*
     *$$/";
            var expected = @"
    /*
     *
     *$$/";
            await VerifyAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine2_Tab()
        {
            var code = @"
    /*$$<tab>*/
";
            var expected = @"
    /*
     *$$<tab>*/
";
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine3_Tab()
        {
            var code = @"
    /*<tab>$$<tab>1.
     */
";
            var expected = @"
    /*<tab>
     *<tab>$$<tab>1.
     */
";
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine4_Tab()
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
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnStartLine6_Tab()
        {
            var code = @"
    /*<tab>$$
";
            var expected = @"
    /*<tab>
     *<tab>$$
";
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine2_Tab()
        {
            var code = @"
    /*
     *$$<tab>*/
";
            var expected = @"
    /*
     *
     *$$<tab>*/
";
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine3_Tab()
        {
            var code = @"
    /*
     * $$<tab>1.
     */
";
            var expected = @"
    /*
     * 
     * $$<tab>1.
     */
";
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine4_Tab()
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
            await VerifyTabsAsync(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public async Task InsertOnMiddleLine5_Tab()
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
            await VerifyTabsAsync(code, expected);
        }

        protected override Task<TestWorkspace> CreateTestWorkspaceAsync(string initialMarkup) => TestWorkspace.CreateCSharpAsync(initialMarkup);

        internal override ICommandHandler<ReturnKeyCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService)
            => new BlockCommentEditingCommandHandler(undoHistoryRegistry, editorOperationsFactoryService);
    }
}
