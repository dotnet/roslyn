// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
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
    public sealed class CloseBlockCommentTests : AbstractTypingCommandHandlerTest<TypeCharCommandArgs>
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedRegularlyAfterAsterisk()
        {
            var code = @"
    /*
     *
     *$$
";
            var expected = @"
    /*
     *
     */$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedAfterAsteriskSpace1()
        {
            var code = @"
    /*
     *
     * $$
";
            var expected = @"
    /*
     *
     */$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedAfterAsteriskSpace2()
        {
            var code = @"
    /*
     * $$
";
            var expected = @"
    /*
     */$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterSlashAsteriskSpace()
        {
            var code = @"
    /* $$
";
            var expected = @"
    /* /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterSlashDoubleAsteriskSpace()
        {
            var code = @"
    /** $$
";
            var expected = @"
    /** /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterSpaceWithoutAsterisk()
        {
            var code = @"
    /*
     *
       $$
";
            var expected = @"
    /*
     *
       /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk1()
        {
            var code = @"
    /*
     *
    ** $$
";
            var expected = @"
    /*
     *
    ** /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk2()
        {
            var code = @"
    /*
     *
    /* $$
";
            var expected = @"
    /*
     *
    /* /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk3()
        {
            var code = @"
    /*
     *
a    * $$
";
            var expected = @"
    /*
     *
a    * /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor1()
        {
            var code = @"
    /*
     *
     * $$/
";
            var expected = @"
    /*
     *
     * /$$/
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor2()
        {
            var code = @"
    /*
     *
     * $$*
";
            var expected = @"
    /*
     *
     * /$$*
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor3()
        {
            var code = @"
    /*
     *
     * $$ a
";
            var expected = @"
    /*
     *
     * /$$ a
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithWhitespaceAfterCursor()
        {
            // Note: There is a single trailing space after the cursor.
            var code = @"
    /*
     *
     * $$ 
";
            var expected = @"
    /*
     *
     * /$$ 
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskDoubleSpace()
        {
            var code = @"
    /*
     *
     *  $$
";
            var expected = @"
    /*
     *
     *  /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedAfterAsteriskSpaceWithNothingBeforeAsterisk()
        {
            var code = @"
    /*
     *
* $$
";
            var expected = @"
    /*
     *
*/$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedAfterAsteriskSpaceWithTabsBeforeAsterisk()
        {
            var code = @"
    /*
     *
<tab><tab>* $$
";
            var expected = @"
    /*
     *
<tab><tab>*/$$
";
            VerifyTabs(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceWithOptionOff()
        {
            var code = @"
    /*
     *
     * $$
";
            var expected = @"
    /*
     *
     * /$$
";
            Verify(code, expected, workspace =>
            {
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(
                    workspace.CurrentSolution.Options.WithChangedOption(FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp, false)));
            });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceOutsideComment()
        {
            var code = @"
   / *
     *
     * $$
";
            var expected = @"
   / *
     *
     * /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceInsideString()
        {
            var code = @"
class C
{
    string s = @""
    /*
     *
     * $$
";
            var expected = @"
class C
{
    string s = @""
    /*
     *
     * /$$
";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void ClosedAfterAsteriskSpaceEndOfFile()
        {
            var code = @"
    /*
     * $$";
            var expected = @"
    /*
     */$$";
            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterAsteriskSpaceStartOfFile()
        {
            var code = @"* $$";
            var expected = @"* /$$";

            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAfterSpaceStartOfFile()
        {
            var code = @" $$";
            var expected = @" /$$";

            Verify(code, expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
        public void NotClosedAtStartOfFile()
        {
            var code = @"$$";
            var expected = @"/$$";

            Verify(code, expected);
        }

        protected override TestWorkspace CreateTestWorkspace(string initialMarkup)
            => TestWorkspace.CreateCSharp(initialMarkup);

        protected override (TypeCharCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
            => (new TypeCharCommandArgs(textView, textBuffer, '/'), "/");

        internal override ICommandHandler<TypeCharCommandArgs> GetCommandHandler(TestWorkspace workspace)
            => Assert.IsType<CloseBlockCommentCommandHandler>(workspace.GetService<ICommandHandler>(ContentTypeNames.CSharpContentType, nameof(CloseBlockCommentCommandHandler)));
    }
}
