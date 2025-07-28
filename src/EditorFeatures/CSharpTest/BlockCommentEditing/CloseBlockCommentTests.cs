// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.CSharp.BlockCommentEditing;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.BlockCommentEditing;

[Trait(Traits.Feature, Traits.Features.BlockCommentEditing)]
public sealed class CloseBlockCommentTests : AbstractTypingCommandHandlerTest<TypeCharCommandArgs>
{
    [WpfFact]
    public void ClosedRegularlyAfterAsterisk()
        => Verify("""
            /*
             *
             *$$
            """, """
            /*
             *
             */$$
            """);

    [WpfFact]
    public void ClosedAfterAsteriskSpace1()
        => Verify("""
            /*
             *
             * $$
            """, """
            /*
             *
             */$$
            """);

    [WpfFact]
    public void ClosedAfterAsteriskSpace2()
        => Verify("""
            /*
             * $$
            """, """
            /*
             */$$
            """);

    [WpfFact]
    public void NotClosedAfterSlashAsteriskSpace()
        => Verify("""
            /* $$
            """, """
            /* /$$
            """);

    [WpfFact]
    public void NotClosedAfterSlashDoubleAsteriskSpace()
        => Verify("""
            /** $$
            """, """
            /** /$$
            """);

    [WpfFact]
    public void NotClosedAfterSpaceWithoutAsterisk()
        => Verify("""
            /*
             *
               $$
            """, """
            /*
             *
               /$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk1()
        => Verify("""
            /*
             *
            ** $$
            """, """
            /*
             *
            ** /$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk2()
        => Verify("""
            /*
             *
            /* $$
            """, """
            /*
             *
            /* /$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceBeforeAsterisk3()
        => Verify("""
                /*
                 *
            a    * $$
            """, """
                /*
                 *
            a    * /$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor1()
        => Verify("""
            /*
             *
             * $$/
            """, """
            /*
             *
             * /$$/
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor2()
        => Verify("""
            /*
             *
             * $$*
            """, """
            /*
             *
             * /$$*
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithNonWhitespaceAfterCursor3()
        => Verify("""
            /*
             *
             * $$ a
            """, """
            /*
             *
             * /$$ a
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithWhitespaceAfterCursor()
        => Verify("""
            /*
             *
             * $$ 
            """, """
            /*
             *
             * /$$ 
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskDoubleSpace()
        => Verify("""
            /*
             *
             *  $$
            """, """
            /*
             *
             *  /$$
            """);

    [WpfFact]
    public void ClosedAfterAsteriskSpaceWithNothingBeforeAsterisk()
        => Verify("""
                /*
                 *
            * $$
            """, """
                /*
                 *
            */$$
            """);

    [WpfFact]
    public void ClosedAfterAsteriskSpaceWithTabsBeforeAsterisk()
        => VerifyTabs("""
                /*
                 *
            <tab><tab>* $$
            """, """
                /*
                 *
            <tab><tab>*/$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceWithOptionOff()
        => Verify("""
            /*
             *
             * $$
            """, """
            /*
             *
             * /$$
            """, workspace =>
        {
            var globalOptions = workspace.GetService<IGlobalOptionService>();
            globalOptions.SetGlobalOption(BlockCommentEditingOptionsStorage.AutoInsertBlockCommentStartString, LanguageNames.CSharp, false);
        });

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceOutsideComment()
        => Verify("""
            / *
              *
              * $$
            """, """
            / *
              *
              * /$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceInsideString()
        => Verify("""
            class C
            {
                string s = @"
                /*
                 *
                 * $$
            """, """
            class C
            {
                string s = @"
                /*
                 *
                 * /$$
            """);

    [WpfFact]
    public void ClosedAfterAsteriskSpaceEndOfFile()
        => Verify("""
            /*
             * $$
            """, """
            /*
             */$$
            """);

    [WpfFact]
    public void NotClosedAfterAsteriskSpaceStartOfFile()
        => Verify(@"* $$", @"* /$$");

    [WpfFact]
    public void NotClosedAfterSpaceStartOfFile()
        => Verify(@" $$", @" /$$");

    [WpfFact]
    public void NotClosedAtStartOfFile()
        => Verify(@"$$", @"/$$");

    protected override EditorTestWorkspace CreateTestWorkspace(string initialMarkup)
        => EditorTestWorkspace.CreateCSharp(initialMarkup);

    protected override (TypeCharCommandArgs, string insertionText) CreateCommandArgs(ITextView textView, ITextBuffer textBuffer)
        => (new TypeCharCommandArgs(textView, textBuffer, '/'), "/");

    internal override ICommandHandler<TypeCharCommandArgs> GetCommandHandler(EditorTestWorkspace workspace)
        => Assert.IsType<CloseBlockCommentCommandHandler>(workspace.GetService<ICommandHandler>(ContentTypeNames.CSharpContentType, nameof(CloseBlockCommentCommandHandler)));
}
