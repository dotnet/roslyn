// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class SortAndConsolidateUsingsTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task AliasesLast_OrderedByAliasName()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Z = System
                @using SystemAlias = System.String
                @using Microsoft.AspNetCore.Components
                @using A = System.Text.StringBuilder
                @using b = System.Text
                @using static System.Math
                @using System.Text
                @using System

                <div>Hello</div>
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

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task AliasesLast_AlreadySorted_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System
                @using System.Text
                @using Microsoft.AspNetCore.Components
                @using static System.Math
                @using A = System.Text.StringBuilder
                @using SystemAlias = System.String
                @using Z = System
                @using b = System.Text

                <div>Hello</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task StaticImports_BetweenNamespacesAndAliases_SystemFirst()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]static a.Math
                @using Z = System
                @using static System.String
                @using static A.Math
                @using zeta
                @using static System.Math
                @using System.Text
                @using A = System.String

                <div>Hello</div>
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

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task StaticImports_AlreadySorted_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System.Text
                @using zeta
                @using static System.Math
                @using static System.String
                @using static A.Math
                @using static a.Math
                @using A = System.String
                @using Z = System

                <div>Hello</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task AlreadySortedSingleGroup_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System
                @using System.Text

                <div>Hello</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task SingleUsing_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>Hello</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task UnsortedSingleGroup()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Zebra
                @using Apple

                <div>Hello</div>
                """,
            expected: """
                @using Apple
                @using Zebra

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task SystemDirectivesFirst()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Zebra
                @using System.Text
                @using Apple
                @using System

                <div>Hello</div>
                """,
            expected: """
                @using System
                @using System.Text
                @using Apple
                @using Zebra

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task MultipleGroups_Consolidated()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System
                @page "/"
                @using Zebra

                <div>Hello</div>
                """,
            expected: """
                @using System
                @using Zebra
                @page "/"

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task MultipleGroups_SortedAndConsolidated()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Zebra
                @using Apple
                <div></div>
                @using System.Text
                @using System

                <div>Hello</div>
                """,
            expected: """
                @using System
                @using System.Text
                @using Apple
                @using Zebra
                <div></div>

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task Selection()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [|Zebra|]
                @using Apple

                <div>Hello</div>
                """,
            expected: """
                @using Apple
                @using Zebra

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task CursorNotOnUsing_NotOffered()
    {
        await VerifyCodeActionAsync(
            input: """
                @using Zebra
                @using Apple

                <div>[||]Hello</div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task ThreeGroups_AllConsolidated()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Microsoft.AspNetCore

                @using System
                <div></div>
                @using Zebra

                <div>Hello</div>
                """,
            expected: """
                @using System
                @using Microsoft.AspNetCore
                @using Zebra

                <div></div>

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task ThreeGroups_AllConsolidated_Semicolons()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Microsoft.AspNetCore

                @using System;
                <div></div>
                @using Zebra;

                <div>Hello</div>
                """,
            expected: """
                @using System;
                @using Microsoft.AspNetCore
                @using Zebra;

                <div></div>

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task ThreeGroups_AllConsolidated_ExtraContent()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Microsoft.AspNetCore

                @using System hi there welcome
                <div></div>
                @using Zebra;

                <div>Hello</div>
                """,
            expected: """
                @using System
                @using Microsoft.AspNetCore
                @using Zebra;

                 hi there welcome
                <div></div>

                <div>Hello</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task ThreeGroups_AllConsolidated_ExtraContentInFirstGroup()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]Microsoft.AspNetCore
                @using System;hi there welcome
                @using Zebra

                <div></div>

                @using System.Text;<div>Hello</div>

                <div>World</div>
                """,
            expected: """
                @using System;
                @using System.Text;
                @using Microsoft.AspNetCore
                @using Zebra
                hi there welcome

                <div></div>

                <div>Hello</div>

                <div>World</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }

    [Fact]
    public async Task ThreeGroups_AllConsolidated_ExtraContentOnFirstUsing()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System;hi there welcome
                @using [||]Microsoft.AspNetCore
                @using Zebra

                <div></div>

                @using System.Text;<div>Hello</div>

                <div>World</div>
                """,
            expected: """
                @using System;
                @using System.Text;
                @using Microsoft.AspNetCore
                @using Zebra
                hi there welcome

                <div></div>

                <div>Hello</div>

                <div>World</div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.SortAndConsolidateUsings);
    }
}
