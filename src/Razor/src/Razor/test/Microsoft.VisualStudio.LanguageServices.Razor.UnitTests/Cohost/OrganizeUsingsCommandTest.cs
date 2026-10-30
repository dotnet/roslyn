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
    public Task SortOnly_NoUnused_NoCodeBlock()
       => VerifyRemoveAndSortUsingsAsync(
           input: """
                @using System.Text
                @using System
                @using Microsoft.AspNetCore.Components.Forms

                <div></div>

                @nameof(StringBuilder)
                @nameof(Console)
                """,
           expected: """
                @using System
                @using System.Text

                <div></div>

                @nameof(StringBuilder)
                @nameof(Console)
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

    [Fact]
    public Task SortOnly_AliasesLast_OrderedByAliasName()
        // Alias targets deliberately sort differently from their names.
        => VerifySortUsingsAsync(
            input: """
                @using Z = System
                @using SystemAlias = System.String
                @using Microsoft.AspNetCore.Components
                @using A = System.Text.StringBuilder
                @using b = System.Text
                @using static System.Math
                @using System.Text
                @using System

                <div></div>
                """,
            expected: """
                @using System
                @using System.Text
                @using Microsoft.AspNetCore.Components
                @using static System.Math
                @using A = System.Text.StringBuilder
                @using SystemAlias = System.String
                @using Z = System
                @using b = System.Text

                <div></div>
                """);

    [Fact]
    public Task SortOnly_AliasNames_UseOrdinalOrdering()
        // Alias names are case-sensitive, with no special treatment for an alias named System.
        => VerifySortUsingsAsync(
            input: """
                @using b = System
                @using A = System.Text
                @using a = System.String
                @using System = Microsoft.AspNetCore.Components.ComponentBase
                @using System.Text

                <div></div>
                """,
            expected: """
                @using System.Text
                @using A = System.Text
                @using System = Microsoft.AspNetCore.Components.ComponentBase
                @using a = System.String
                @using b = System

                <div></div>
                """);

    [Fact]
    public Task SortOnly_StaticImports_BetweenNamespacesAndAliases_SystemFirst()
        // Lowercase namespace names still precede statics; static targets use System-first, then ordinal ordering.
        => VerifySortUsingsAsync(
            input: """
                @using static a.Math
                @using Z = System
                @using static System.String
                @using static A.Math
                @using zeta
                @using static System.Math
                @using System.Text
                @using A = System.String

                <div></div>
                """,
            expected: """
                @using System.Text
                @using zeta
                @using static System.Math
                @using static System.String
                @using static A.Math
                @using static a.Math
                @using A = System.String
                @using Z = System

                <div></div>
                """);

    [Fact]
    public Task RemoveAndSort_AliasesUsedOnlyInCSharp()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using Text = System.Text
                @using Unused = System.String
                @using Builder = System.Text.StringBuilder

                <div>@typeof(Text.StringBuilder) @(new Builder())</div>
                """,
            expected: """
                @using Builder = System.Text.StringBuilder
                @using Text = System.Text

                <div>@typeof(Text.StringBuilder) @(new Builder())</div>
                """);

    [Fact]
    public Task RemoveAndSort_StaticImports_UnusedRemoved_UsedSorted()
        => VerifyRemoveAndSortUsingsAsync(
            input: """
                @using static System.String
                @using static System.Convert
                @using static System.Math

                <div>@Abs(-1) @IsNullOrEmpty("")</div>
                """,
            expected: """
                @using static System.Math
                @using static System.String

                <div>@Abs(-1) @IsNullOrEmpty("")</div>
                """);

    private async Task VerifyRemoveAndSortUsingsAsync(TestCode input, string expected, (string filePath, string contents)[]? additionalFiles = null, RazorFileKind? fileKind = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind, additionalFiles: additionalFiles);

        var command = new OrganizeUsingsCommand(RemoteServiceInvoker, VoidSessionTracker.Instance);
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

        var command = new OrganizeUsingsCommand(RemoteServiceInvoker, VoidSessionTracker.Instance);
        var accessor = command.GetTestAccessor();

        Assert.True(accessor.QuerySortUsings());

        var edits = await accessor.ExecuteSortUsingsAsync(document.Project.Solution, document.Id, DisposalToken);

        var sourceText = await document.GetTextAsync(DisposalToken);
        var result = sourceText.WithChanges(edits).ToString();

        AssertEx.EqualOrDiff(expected, result);
    }
}
