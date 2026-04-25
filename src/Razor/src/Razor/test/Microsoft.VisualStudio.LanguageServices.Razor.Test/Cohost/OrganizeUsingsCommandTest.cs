// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class OrganizeUsingsCommandTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task SortOnly_NoUnused()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @using System
                @using Microsoft.AspNetCore.Components.Forms

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task RemoveOnly_AlreadySorted()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System
                @using System.Text

                <div></div>
                """,
            expected: """

                <div></div>
                """);

    [Fact]
    public Task RemoveAndSort_Combined()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @using System.Buffers
                @using System

                <div></div>

                @code
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System

                <div></div>

                @code
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task RemoveAndSort_Combined2()
    => VerifyRemoveAndSortUsingsAsync(
        input: """
                @using System.Text
                @using System.Buffers
                @using System

                <div></div>

                @code
                {
                    public void M(StringBuilder sv)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
        expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder sv)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task NoUsings_NoEdits()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                <div>Hello World</div>
                """,
            expected: """
                <div>Hello World</div>
                """);

    [Fact]
    public Task AllUnused_AllRemoved()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @using System.Buffers

                <div></div>
                """,
            expected: """

                <div></div>
                """);

    [Fact]
    public Task SystemUsings_SortedFirst()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using Microsoft.AspNetCore.Components.Forms
                @using System.Text
                @using System

                <div></div>

                <PageTitle></PageTitle>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                <PageTitle></PageTitle>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task SingleUsing_Used_NoChange()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System

                <div></div>

                @code
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System

                <div></div>

                @code
                {
                    public void M()
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task PageDirective_BeforeUsings()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @page "/mypage"
                @using System.Text
                @using System

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @page "/mypage"
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task PageDirective_BetweenUsings()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @page "/mypage"
                @using System

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text
                @page "/mypage"

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task UsingsSpreadAcrossFile_UnusedRemoved()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @using System

                <div></div>

                @using System.Buffers

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>


                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task UsingsSpreadAcrossFile_AllUsed_ConsolidatedIntoFirstGroup()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System.Text
                @using System

                <div></div>

                @using System.Buffers

                @code
                {
                    public void M(StringBuilder b, ArrayPool<byte> pool)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Buffers
                @using System.Text

                <div></div>


                @code
                {
                    public void M(StringBuilder b, ArrayPool<byte> pool)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task SystemUsings_SortedBeforeNonSystem()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using My.Custom.Namespace
                @using System.Text
                @using System

                <div></div>

                <MyComponent />

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text
                @using My.Custom.Namespace

                <div></div>

                <MyComponent />

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            additionalFiles: [
                (FilePath("MyComponent.razor"), """
                    @namespace My.Custom.Namespace

                    <div>My Component</div>
                    """)]);

    [Fact]
    public Task SortOnly_AlreadySorted()
        => VerifySortUsingsAsync(
            input: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task SortOnly_Unsorted()
        => VerifySortUsingsAsync(
            input: """
                @using System.Text
                @using System

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task SortOnly_SpreadAcrossFile()
        => VerifySortUsingsAsync(
            input: """
                @using System.Text
                @using System

                <div></div>

                @using System.Buffers

                @code
                {
                    public void M(StringBuilder b, ArrayPool<byte> pool)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Buffers
                @using System.Text

                <div></div>


                @code
                {
                    public void M(StringBuilder b, ArrayPool<byte> pool)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task AlreadySorted_NoUnused()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    [Fact]
    public Task Legacy_UsingsWithSemicolons()
        => VerifySortUsingsAsync(
            input: """
                @using System.Text;
                @using System;

                <div></div>
                """,
            expected: """
                @using System;
                @using System.Text;

                <div></div>
                """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_UsingsWithSemicolons_MultipleDirectives()
        => VerifySortUsingsAsync(
            input: """
                @using System.Text;
                @using System.Buffers;
                @using System;

                <div></div>
                """,
            expected: """
                @using System;
                @using System.Buffers;
                @using System.Text;

                <div></div>
                """,
            fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task Legacy_AddTagHelper_NotRemoved()
      => VerifyRemoveAndSortUsingsAsync(
          input: """
                @using System.Text.RegularExpressions;
                @addTagHelper *, UnusedAssembly
                @using System

                <div></div>
                """,
          expected: """
                @addTagHelper *, UnusedAssembly

                <div></div>
                """,
          fileKind: RazorFileKind.Legacy);

    [Fact]
    public Task DuplicateUsings_Removed()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using System
                @using System.Text
                @using System

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System
                @using System.Text

                <div></div>

                @code
                {
                    public void M(StringBuilder b)
                    {
                        Console.WriteLine("");
                    }
                }
                """);

    private async Task VerifyRemoveAndSortUsingsAsync(TestCode input, string expected, (string filePath, string contents)[]? additionalFiles = null, RazorFileKind? fileKind = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind, additionalFiles: additionalFiles);

        var command = new OrganizeUsingsCommand(RemoteServiceInvoker);
        var accessor = command.GetTestAccessor();

        Assert.True(accessor.QueryRemoveAndSortUsings());

        var edits = await accessor.ExecuteRemoveAndSortUsingsAsync(document.Project.Solution, document.Id, DisposalToken);

        var sourceText = await document.GetTextAsync(DisposalToken);
        var result = sourceText.WithChanges(edits).ToString();

        AssertEx.EqualOrDiff(expected, result);
    }

    private async Task VerifySortUsingsAsync(TestCode input, string expected, (string filePath, string contents)[]? additionalFiles = null, RazorFileKind? fileKind = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind, additionalFiles: additionalFiles);

        var command = new OrganizeUsingsCommand(RemoteServiceInvoker);
        var accessor = command.GetTestAccessor();

        Assert.True(accessor.QuerySortUsings());

        var edits = await accessor.ExecuteSortUsingsAsync(document.Project.Solution, document.Id, DisposalToken);

        var sourceText = await document.GetTextAsync(DisposalToken);
        var result = sourceText.WithChanges(edits).ToString();

        AssertEx.EqualOrDiff(expected, result);
    }
}
