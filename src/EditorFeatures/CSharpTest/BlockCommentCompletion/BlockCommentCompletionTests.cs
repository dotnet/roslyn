// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.BlockCommentCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentCompletion
{
    public class BlockCommentCompletionTests : AbstractBlockCommentCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
        public async Task InsertOnStartLine6()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
        public async Task InsertOnEndLine1()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
        public async Task InsertOnEndLine2()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentCompletion)]
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

        protected override Task<TestWorkspace> CreateTestWorkspaceAsync(string initialMarkup) => TestWorkspace.CreateCSharpAsync(initialMarkup);

        internal override ICommandHandler<ReturnKeyCommandArgs> CreateCommandHandler(ITextUndoHistoryRegistry undoHistoryRegistry, IEditorOperationsFactoryService editorOperationsFactoryService)
            => new BlockCommentCompletionCommandHandler(undoHistoryRegistry, editorOperationsFactoryService);
    }
}
