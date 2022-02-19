﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public class InlineCompletionsTests : AbstractLanguageServerProtocolTests
{
    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestSnippetInfoService));

    [Fact]
    public async Task TestSimpleSnippet()
    {
        var markup =
@"class A
{
    void M()
    {
        if{|tab:|}
    }
}";
        var expectedSnippet =
@"if (${1:true})
        {
            $0
        }";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetIgnoresCase()
    {
        var markup =
@"class A
{
    void M()
    {
        If{|tab:|}
    }
}";
        var expectedSnippet =
@"if (${1:true})
        {
            $0
        }";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetUsesOptionsFromRequest()
    {
        var markup =
@"class A
{
    void M()
    {
        if{|tab:|}
    }
}";
        var expectedSnippet =
@"if (${1:true})
  {
   $0
  }";

        await VerifyMarkupAndExpected(markup, expectedSnippet, options: new LSP.FormattingOptions { TabSize = 1, InsertSpaces = true });
    }

    [Fact]
    public async Task TestSnippetWithMultipleDeclarations()
    {
        var markup =
@"class A
{
    void M()
    {
        for{|tab:|}
    }
}";
        var expectedSnippet =
@"for (int ${1:i} = 0; ${1:i} < ${2:length}; ${1:i}++)
        {
            $0
        }";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithSimpleTypeNameFunctionFullyQualifies()
    {
        var markup =
@"class A
{
    void M()
    {
        cw{|tab:|}
    }
}";
        var expectedSnippet = @"System.Console.WriteLine($0);";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithSimpleTypeNameFunctionWithUsing()
    {
        var markup =
@"using System;
class A
{
    void M()
    {
        cw{|tab:|}
    }
}";
        var expectedSnippet = @"Console.WriteLine($0);";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithClassNameFunction()
    {
        var markup =
@"class A
{
    ctor{|tab:|}
}";
        var expectedSnippet =
@"public A()
    {
        $0
    }";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithClassNameFunctionOutsideOfClass()
    {
        var markup =
@"ctor{|tab:|}";
        var expectedSnippet =
@"public ClassNamePlaceholder ()
{
    $0
}";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithSwitchFunctionOnlyGeneratesDefault()
    {
        var markup =
@"class A
{
    void M()
    {
        switch{|tab:|}
    }
}";
        var expectedSnippet =
@"switch (${1:switch_on})
        {
            default:
        }$0";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    [Fact]
    public async Task TestSnippetWithNoEditableFields()
    {
        var markup =
@"class A
{
    equals{|tab:|}
}";
        var expectedSnippet =
@"// override object.Equals
    public override bool Equals(object obj)
    {
        //       
        // See the full list of guidelines at
        //   http://go.microsoft.com/fwlink/?LinkID=85237  
        // and also the guidance for operator== at
        //   http://go.microsoft.com/fwlink/?LinkId=85238
        //

        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        // TODO: write your implementation of Equals() here
        throw new System.NotImplementedException();
        return base.Equals(obj);$0
    }

    // override object.GetHashCode
    public override int GetHashCode()
    {
        // TODO: write your implementation of GetHashCode() here
        throw new System.NotImplementedException();
        return base.GetHashCode();
    }";

        await VerifyMarkupAndExpected(markup, expectedSnippet);
    }

    private async Task VerifyMarkupAndExpected(string markup, string expected, LSP.FormattingOptions? options = null)
    {
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var locationTyped = testLspServer.GetLocations("tab").Single();

        var document = testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single();

        var result = await GetInlineCompletionsAsync(testLspServer, locationTyped, options ?? new LSP.FormattingOptions { InsertSpaces = true, TabSize = 4 });

        AssertEx.NotNull(result);
        Assert.Single(result.Items);

        var item = result.Items.Single();
        AssertEx.NotNull(item.Range);
        Assert.Equal(LSP.InsertTextFormat.Snippet, item.TextFormat);
        Assert.Equal(expected, item.Text);
    }

    private static async Task<LSP.VSInternalInlineCompletionList> GetInlineCompletionsAsync(
            TestLspServer testLspServer,
            LSP.Location locationTyped,
            LSP.FormattingOptions options)
    {
        var request = new LSP.VSInternalInlineCompletionRequest
        {
            Context = new LSP.VSInternalInlineCompletionContext
            {
                SelectedCompletionInfo = null,
                TriggerKind = LSP.VSInternalInlineCompletionTriggerKind.Explicit
            },
            Position = locationTyped.Range.Start,
            TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri),
            Options = options
        };

        var response = await testLspServer.ExecuteRequestAsync<LSP.VSInternalInlineCompletionRequest, LSP.VSInternalInlineCompletionList>(
            LSP.VSInternalMethods.TextDocumentInlineCompletionName, request, CancellationToken.None);
        Contract.ThrowIfNull(response);
        return response;
    }
}
