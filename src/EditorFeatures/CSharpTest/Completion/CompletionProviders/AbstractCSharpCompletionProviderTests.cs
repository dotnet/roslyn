// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Xunit;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

public abstract class AbstractCSharpCompletionProviderTests : AbstractCSharpCompletionProviderTests<CSharpTestWorkspaceFixture>
{
}

public abstract class AbstractCSharpCompletionProviderTests<TWorkspaceFixture> : AbstractCompletionProviderTests<TWorkspaceFixture>
    where TWorkspaceFixture : TestWorkspaceFixture, new()
{
    protected const string NonBreakingSpaceString = "\x00A0";

    protected static string GetMarkup(
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source,
        LanguageVersion languageVersion)
        => $@"<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" LanguageVersion=""{languageVersion.ToDisplayString()}"">
        <Document FilePath=""Test2.cs"">
<![CDATA[
{source}
]]>
        </Document>
    </Project>
</Workspace>";
=======
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string source, LanguageVersion languageVersion)
        => $"""
        <Workspace>
            <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true" LanguageVersion="{languageVersion.ToDisplayString()}">
                <Document FilePath="Test2.cs">
        <![CDATA[
        {source}
        ]]>
                </Document>
            </Project>
        </Workspace>
        """;
>>>>>>> upstream/features/collection-expression-arguments

    protected override EditorTestWorkspace CreateWorkspace([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fileContents)
        => EditorTestWorkspace.CreateCSharp(fileContents, composition: GetComposition());

    internal override CompletionService GetCompletionService(Project project)
        => Assert.IsType<CSharpCompletionService>(base.GetCompletionService(project));

    private protected override Task BaseVerifyWorkerAsync(
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, string expectedItemOrNull, string expectedDescriptionOrNull,
=======
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position,
        string expectedItemOrNull, string expectedDescriptionOrNull,
>>>>>>> upstream/features/collection-expression-arguments
        SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool checkForAbsence,
        Glyph? glyph, int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription = null, bool? isComplexTextEdit = null,
        List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null, bool skipSpeculation = false)
    {
        return base.VerifyWorkerAsync(
            code, position, expectedItemOrNull, expectedDescriptionOrNull,
            sourceCodeKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence,
            glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags, options, skipSpeculation: skipSpeculation);
    }

    private protected override Task BaseVerifyWorkerAsync(
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
        SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
=======
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger, bool? hasSuggestionItem,
            SourceCodeKind sourceCodeKind, ItemExpectation[] expectedResults,
>>>>>>> upstream/features/collection-expression-arguments
        List<CompletionFilter> matchingFilters, CompletionItemFlags? flags, CompletionOptions options, bool skipSpeculation = false)
    {
        return base.VerifyWorkerAsync(
            code, position, usePreviousCharAsTrigger, deletedCharTrigger, hasSuggestionItem, sourceCodeKind,
            expectedResults, matchingFilters, flags, options, skipSpeculation);
    }

    private protected override async Task VerifyWorkerAsync(
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, string expectedItemOrNull, string expectedDescriptionOrNull,
=======
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position,
        string expectedItemOrNull, string expectedDescriptionOrNull,
>>>>>>> upstream/features/collection-expression-arguments
        SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
        bool checkForAbsence, Glyph? glyph, int? matchPriority,
        bool? hasSuggestionItem, string displayTextSuffix, string displayTextPrefix, string inlineDescription = null,
        bool? isComplexTextEdit = null, List<CompletionFilter> matchingFilters = null, CompletionItemFlags? flags = null,
        CompletionOptions options = null, bool skipSpeculation = false)
    {
        await VerifyAtPositionAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options, skipSpeculation: skipSpeculation);
        await VerifyInFrontOfCommentAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, options, skipSpeculation: skipSpeculation);
        await VerifyAtEndOfFileAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options);

        // Items cannot be partially written if we're checking for their absence,
        // or if we're verifying that the list will show up (without specifying an actual item)
        if (!checkForAbsence && expectedItemOrNull != null)
        {
            await VerifyAtPosition_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options, skipSpeculation: skipSpeculation);
            await VerifyInFrontOfComment_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, options, skipSpeculation: skipSpeculation);
            await VerifyAtEndOfFile_ItemPartiallyWrittenAsync(code, position, usePreviousCharAsTrigger, deletedCharTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, flags: null, options);
        }
    }

    protected override string ItemPartiallyWritten(string expectedItemOrNull)
        => expectedItemOrNull[0] == '@' ? expectedItemOrNull.Substring(1, 1) : expectedItemOrNull[..1];

    private async Task VerifyInFrontOfCommentAsync(
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
=======
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position, string insertText, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
>>>>>>> upstream/features/collection-expression-arguments
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit, List<CompletionFilter> matchingFilters,
        CompletionOptions options, bool skipSpeculation = false)
    {
        code = code[..position] + insertText + "/**/" + code[position..];
        position += insertText.Length;

        await base.VerifyWorkerAsync(
            code, position, expectedItemOrNull, expectedDescriptionOrNull,
            sourceCodeKind, usePreviousCharAsTrigger, deletedCharTrigger, checkForAbsence, glyph,
            matchPriority, hasSuggestionItem, displayTextSuffix, displayTextPrefix,
            inlineDescription, isComplexTextEdit, matchingFilters, flags: null,
            options, skipSpeculation: skipSpeculation);
    }

<<<<<<< HEAD
    private async Task VerifyInFrontOfCommentAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
=======
    private Task VerifyInFrontOfCommentAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
>>>>>>> upstream/features/collection-expression-arguments
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters, CompletionOptions options, bool skipSpeculation = false)
        => VerifyInFrontOfCommentAsync(
            code, position, string.Empty, usePreviousCharAsTrigger, deletedCharTrigger,
            expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, options, skipSpeculation: skipSpeculation);

<<<<<<< HEAD
    private protected async Task VerifyInFrontOfComment_ItemPartiallyWrittenAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
=======
    private protected Task VerifyInFrontOfComment_ItemPartiallyWrittenAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code, int position, bool usePreviousCharAsTrigger, char? deletedCharTrigger,
>>>>>>> upstream/features/collection-expression-arguments
        string expectedItemOrNull, string expectedDescriptionOrNull,
        SourceCodeKind sourceCodeKind, bool checkForAbsence, Glyph? glyph,
        int? matchPriority, bool? hasSuggestionItem, string displayTextSuffix,
        string displayTextPrefix, string inlineDescription, bool? isComplexTextEdit,
        List<CompletionFilter> matchingFilters, CompletionOptions options, bool skipSpeculation = false)
        => VerifyInFrontOfCommentAsync(
            code, position, ItemPartiallyWritten(expectedItemOrNull), usePreviousCharAsTrigger, deletedCharTrigger,
            expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind,
            checkForAbsence, glyph, matchPriority, hasSuggestionItem, displayTextSuffix,
            displayTextPrefix, inlineDescription, isComplexTextEdit, matchingFilters, options, skipSpeculation: skipSpeculation);

    protected static string AddInsideMethod([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string text)
    {
        return
            """
            class C
            {
              void F()
              {
            """ + text +
            """
              }
            }
            """;
    }

    protected static string AddUsingDirectives(
        string usingDirectives, [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string text)
    {
        return
usingDirectives +
"""

""" +
text;
    }

    protected async Task VerifySendEnterThroughToEnterAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string initialMarkup,
<<<<<<< HEAD
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string textTypedSoFar,
        EnterKeyRule sendThroughEnterOption, bool expected)
=======
        string textTypedSoFar, EnterKeyRule sendThroughEnterOption, bool expected)
>>>>>>> upstream/features/collection-expression-arguments
    {
        using var workspace = CreateWorkspace(initialMarkup);
        var hostDocument = workspace.DocumentWithCursor;

        var documentId = workspace.GetDocumentId(hostDocument);
        var document = workspace.CurrentSolution.GetDocument(documentId);
        var position = hostDocument.CursorPosition.Value;
        var options = CompletionOptions.Default with { EnterKeyBehavior = sendThroughEnterOption };

        var service = GetCompletionService(document.Project);
        var completionList = await GetCompletionListAsync(service, document, position, RoslynTrigger.Invoke);
        var item = completionList.ItemsList.First(i => (i.DisplayText + i.DisplayTextSuffix).StartsWith(textTypedSoFar));

        Assert.Equal(expected, CommitManager.SendEnterThroughToEditor(service.GetRules(options), item, textTypedSoFar));
    }

    protected void TestCommonIsTextualTriggerCharacter()
    {
        var alwaysTriggerList = new[]
        {
            "goo$$.",
        };

        foreach (var markup in alwaysTriggerList)
        {
            VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: true);
        }

        var triggerOnlyWithLettersList = new[]
        {
            "$$a",
            "$$_"
        };

        foreach (var markup in triggerOnlyWithLettersList)
        {
            VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: true, shouldTriggerWithTriggerOnLettersDisabled: false);
        }

        var neverTriggerList = new[]
        {
            "goo$$x",
            "goo$$_"
        };

        foreach (var markup in neverTriggerList)
        {
            VerifyTextualTriggerCharacter(markup, shouldTriggerWithTriggerOnLettersEnabled: false, shouldTriggerWithTriggerOnLettersDisabled: false);
        }
    }
}
