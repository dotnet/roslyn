// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
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
    public sealed class CloseBlockCommentTests : AbstractTypingCommandHandlerTest<TypeCharCommandArgs>
    {
        [WpfFact]
        public void ClosedRegularlyAfterAsterisk()
        {
            var code = """
                /*
                 *
                 *$$
                """;
            var expected = """
                /*
                 *
                 */$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void ClosedAfterAsteriskSpace1()
        {
            var code = """
                /*
                 *
                 * $$
                """;
            var expected = """
                /*
                 *
                 */$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void ClosedAfterAsteriskSpace2()
        {
            var code = """
                /*
                 * $$
                """;
            var expected = """
                /*
                 */$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterSlashAsteriskSpace()
        {
            var code = """
                /* $$
                """;
            var expected = """
                /* /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterSlashDoubleAsteriskSpace()
        {
            var code = """
                /** $$
                """;
            var expected = """
                /** /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterSpaceWithoutAsterisk()
        {
            var code = """
                /*
                 *
                   $$
                """;
            var expected = """
                /*
                 *
                   /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk1()
        {
            var code = """
                /*
                 *
                ** $$
                """;
            var expected = """
                /*
                 *
                ** /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk2()
        {
            var code = """
                /*
                 *
                /* $$
                """;
            var expected = """
                /*
                 *
                /* /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk3()
        {
            var code = """
                    /*
                     *
                a    * $$
                """;
            var expected = """
                    /*
                     *
                a    * /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor1()
        {
            var code = """
                /*
                 *
                 * $$/
                """;
            var expected = """
                /*
                 *
                 * /$$/
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor2()
        {
            var code = """
                /*
                 *
                 * $$*
                """;
            var expected = """
                /*
                 *
                 * /$$*
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor3()
        {
            var code = """
                /*
                 *
                 * $$ a
                """;
            var expected = """
                /*
                 *
                 * /$$ a
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithWhitespaceAfterCursor()
        {
            // Note: There is a single trailing space after the cursor.
            var code = """
                /*
                 *
                 * $$ 
                """;
            var expected = """
                /*
                 *
                 * /$$ 
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskDoubleSpace()
        {
            var code = """
                /*
                 *
                 *  $$
                """;
            var expected = """
                /*
                 *
                 *  /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void ClosedAfterAsteriskSpaceWithNothingBeforeAsterisk()
        {
            var code = """
                    /*
                     *
                * $$
                """;
            var expected = """
                    /*
                     *
                */$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void ClosedAfterAsteriskSpaceWithTabsBeforeAsterisk()
        {
            var code = """
                    /*
                     *
                <tab><tab>* $$
                """;
            var expected = """
                    /*
                     *
                <tab><tab>*/$$
                """;
            VerifyTabs(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceWithOptionOff()
        {
            var code = """
                /*
                 *
                 * $$
                """;
            var expected = """
                /*
                 *
                 * /$$
                """;
            Verify(code, expected, workspace =>
            {
                var globalOptions = workspace.GetService<IGlobalOptionService>();
                globalOptions.SetGlobalOption(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, LanguageNames.CSharp, false);
            });
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceOutsideComment()
        {
            var code = """
                / *
                  *
                  * $$
                """;
            var expected = """
                / *
                  *
                  * /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceInsideString()
        {
            var code = """
                class C
                {
                    string s = @"
                    /*
                     *
                     * $$
                """;
            var expected = """
                class C
                {
                    string s = @"
                    /*
                     *
                     * /$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void ClosedAfterAsteriskSpaceEndOfFile()
        {
            var code = """
                /*
                 * $$
                """;
            var expected = """
                /*
                 */$$
                """;
            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterAsteriskSpaceStartOfFile()
        {
            var code = @"* $$";
            var expected = @"* /$$";

            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAfterSpaceStartOfFile()
        {
            var code = @" $$";
            var expected = @" /$$";

            Verify(code, expected);
        }

        [WpfFact]
        public void NotClosedAtStartOfFile()
        {
            var code = @"$$";
            var expected = @"/$$";

            Verify(code, expected);
        }

        protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
            => EditorTestWorkspace.CreateCSharp(initialMarkup);

        protected override (TypeCharCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
            => (new TypeCharCommandArgs(textView, textBuffer, '/'), "/");

        internal override ICommandHandler<TypeCharCommandArgs> GetCommandHandler(EditorTestWorkspace workspace)
            => Assert.IsType<CloseBlockCommentCommandHandler>(workspace.GetService<ICommandHandler>(ContentTypeNames.CSharpContentType, nameof(CloseBlockCommentCommandHandler)));
    }
}
