// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp;

[UseExportProvider]
public abstract class AbstractSignatureHelpProviderTests<TWorkspaceFixture> : TestBase
    where TWorkspaceFixture : TestWorkspaceFixture, new()
{
    private readonly TestFixtureHelper<TWorkspaceFixture> _fixtureHelper = new();

    internal abstract Type GetSignatureHelpProviderType();

    private protected ReferenceCountedDisposable<TWorkspaceFixture> GetOrCreateWorkspaceFixture()
        => _fixtureHelper.GetOrCreateFixture();

    /// <summary>
    /// Verifies that sighelp comes up at the indicated location in markup ($$), with the indicated span [| ... |].
    /// </summary>
    /// <param name="markup">Input markup with $$ denoting the cursor position, and [| ... |]
    /// denoting the expected sighelp span</param>
    /// <param name="expectedOrderedItemsOrNull">The exact expected sighelp items list. If null, this part of the test is ignored.</param>
    /// <param name="usePreviousCharAsTrigger">If true, uses the last character before $$ to trigger sighelp.
    /// If false, invokes sighelp explicitly at the cursor location.</param>
    /// <param name="sourceCodeKind">The sourcecodekind to run this test on. If null, runs on both regular and script sources.</param>
    protected virtual async Task TestAsync(
        string markup,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
        bool usePreviousCharAsTrigger = false,
        SourceCodeKind? sourceCodeKind = null,
        bool experimental = false)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        if (sourceCodeKind.HasValue)
        {
            await TestSignatureHelpWorkerAsync(markup, sourceCodeKind.Value, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
        }
        else
        {
            await TestSignatureHelpWorkerAsync(markup, SourceCodeKind.Regular, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
            await TestSignatureHelpWorkerAsync(markup, SourceCodeKind.Script, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
        }
    }

    private async Task TestSignatureHelpWorkerAsync(
        string markupWithPositionAndOptSpan,
        SourceCodeKind sourceCodeKind,
        bool experimental,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
        bool usePreviousCharAsTrigger = false)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();
        var options = new MemberDisplayOptions();

        markupWithPositionAndOptSpan = markupWithPositionAndOptSpan.NormalizeLineEndings();

        TextSpan? textSpan = null;
        MarkupTestFile.GetPositionAndSpans(
            markupWithPositionAndOptSpan,
            out var code,
            out var cursorPosition,
            out var textSpans);

        if (textSpans.Any())
        {
            textSpan = textSpans.First();
        }

        var parseOptions = CreateExperimentalParseOptions();

        // regular
        var document1 = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind);
        if (experimental)
        {
            document1 = document1.Project.WithParseOptions(parseOptions).GetDocument(document1.Id);
        }

        await TestSignatureHelpWorkerSharedAsync(workspaceFixture.Target.GetWorkspace(), code, cursorPosition, document1, options, textSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);

        // speculative semantic model
        if (await CanUseSpeculativeSemanticModelAsync(document1, cursorPosition))
        {
            var document2 = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
            if (experimental)
            {
                document2 = document2.Project.WithParseOptions(parseOptions).GetDocument(document2.Id);
            }

            await TestSignatureHelpWorkerSharedAsync(workspaceFixture.Target.GetWorkspace(), code, cursorPosition, document2, options, textSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
        }
    }

    protected abstract ParseOptions CreateExperimentalParseOptions();

    private static async Task<bool> CanUseSpeculativeSemanticModelAsync(Document document, int position)
    {
        var service = document.GetLanguageService<ISyntaxFactsService>();
        var node = (await document.GetSyntaxRootAsync()).FindToken(position).Parent;

        return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
    }

    protected void VerifyTriggerCharacters(char[] expectedTriggerCharacters, char[] unexpectedTriggerCharacters)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        var signatureHelpProviderType = GetSignatureHelpProviderType();
        var signatureHelpProvider = workspaceFixture.Target.GetWorkspace().ExportProvider.GetExportedValues<ISignatureHelpProvider>().Single(provider => provider.GetType() == signatureHelpProviderType);

        foreach (var expectedTriggerCharacter in expectedTriggerCharacters)
        {
            Assert.True(signatureHelpProvider.TriggerCharacters.Contains(expectedTriggerCharacter), "Expected '" + expectedTriggerCharacter + "' to be a trigger character");
        }

        foreach (var unexpectedTriggerCharacter in unexpectedTriggerCharacters)
        {
            Assert.False(signatureHelpProvider.TriggerCharacters.Contains(unexpectedTriggerCharacter), "Expected '" + unexpectedTriggerCharacter + "' to NOT be a trigger character");
        }
    }

    protected virtual async Task VerifyCurrentParameterNameAsync(string markup, string expectedParameterName, SourceCodeKind? sourceCodeKind = null)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        if (sourceCodeKind.HasValue)
        {
            await VerifyCurrentParameterNameWorkerAsync(markup, expectedParameterName, sourceCodeKind.Value);
        }
        else
        {
            await VerifyCurrentParameterNameWorkerAsync(markup, expectedParameterName, SourceCodeKind.Regular);
            await VerifyCurrentParameterNameWorkerAsync(markup, expectedParameterName, SourceCodeKind.Script);
        }
    }

    private static async Task<SignatureHelpState?> GetArgumentStateAsync(int cursorPosition, Document document, ISignatureHelpProvider signatureHelpProvider, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options)
    {
        var items = await signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, options, CancellationToken.None);
        return items == null ? null : new SignatureHelpState(items.SemanticParameterIndex, items.SyntacticArgumentCount, items.ArgumentName, ArgumentNames: default);
    }

    private async Task VerifyCurrentParameterNameWorkerAsync(string markup, string expectedParameterName, SourceCodeKind sourceCodeKind)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), out var code, out int cursorPosition);

        var document = workspaceFixture.Target.UpdateDocument(code, sourceCodeKind);

        var signatureHelpProviderType = GetSignatureHelpProviderType();
        var signatureHelpProvider = workspaceFixture.Target.GetWorkspace().ExportProvider.GetExportedValues<ISignatureHelpProvider>().Single(provider => provider.GetType() == signatureHelpProviderType);
        var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);
        var options = new MemberDisplayOptions();

        _ = await signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, options, CancellationToken.None);
        Assert.Equal(expectedParameterName, (await GetArgumentStateAsync(cursorPosition, document, signatureHelpProvider, triggerInfo, options)).Value.ArgumentName);
    }

    private static void CompareAndAssertCollectionsAndCurrentParameter(
        IEnumerable<SignatureHelpTestItem> expectedTestItems, SignatureHelpItems actualSignatureHelpItems)
    {
        Assert.True(expectedTestItems.Count() == actualSignatureHelpItems.Items.Count, $"Expected {expectedTestItems.Count()} items, but got {actualSignatureHelpItems.Items.Count}");

        for (var i = 0; i < expectedTestItems.Count(); i++)
        {
            CompareSigHelpItemsAndCurrentPosition(
                actualSignatureHelpItems,
                actualSignatureHelpItems.Items.ElementAt(i),
                expectedTestItems.ElementAt(i));
        }
    }

    private static void CompareSigHelpItemsAndCurrentPosition(
        SignatureHelpItems items,
        SignatureHelpItem actualSignatureHelpItem,
        SignatureHelpTestItem expectedTestItem)
    {
        var currentParameterIndex = -1;
        if (expectedTestItem.CurrentParameterIndex != null)
        {
            if (expectedTestItem.CurrentParameterIndex.Value >= 0 && expectedTestItem.CurrentParameterIndex.Value < actualSignatureHelpItem.Parameters.Length)
            {
                currentParameterIndex = expectedTestItem.CurrentParameterIndex.Value;
            }
        }

        var signature = new Signature(applicableToSpan: null, signatureHelpItem: actualSignatureHelpItem, selectedParameterIndex: currentParameterIndex);

        // We're a match if the signature matches...
        // We're now combining the signature and documentation to make classification work.
        if (!string.IsNullOrEmpty(expectedTestItem.MethodDocumentation))
        {
            Assert.Equal(expectedTestItem.Signature + "\r\n" + expectedTestItem.MethodDocumentation, signature.Content);
        }
        else
        {
            AssertEx.Equal(expectedTestItem.Signature, signature.Content);
        }

        if (expectedTestItem.PrettyPrintedSignature != null)
        {
            Assert.Equal(expectedTestItem.PrettyPrintedSignature, signature.PrettyPrintedContent);
        }

        if (expectedTestItem.MethodDocumentation != null)
        {
            Assert.Equal(expectedTestItem.MethodDocumentation, actualSignatureHelpItem.DocumentationFactory(CancellationToken.None).GetFullText());
        }

        if (expectedTestItem.ParameterDocumentation != null)
        {
            Assert.Equal(expectedTestItem.ParameterDocumentation, signature.CurrentParameter.Documentation);
        }

        if (expectedTestItem.CurrentParameterIndex != null)
        {
            Assert.True(expectedTestItem.CurrentParameterIndex == items.SemanticParameterIndex, $"The current parameter is {items.SemanticParameterIndex}, but we expected {expectedTestItem.CurrentParameterIndex}");
        }

        if (expectedTestItem.Description != null)
        {
            Assert.Equal(expectedTestItem.Description, ToString(actualSignatureHelpItem.DescriptionParts));
        }

        // Always get and realise the classified spans, even if no expected spans are passed in, to at least validate that
        // exceptions aren't thrown
        var classifiedSpans = actualSignatureHelpItem.DocumentationFactory(CancellationToken.None).ToClassifiedSpans().ToList();
        if (expectedTestItem.ClassificationTypeNames is { } classificationTypeNames)
        {
            Assert.Equal(string.Join(", ", classificationTypeNames), string.Join(", ", classifiedSpans.Select(s => s.ClassificationType)));
        }
    }

    private static string ToString(IEnumerable<TaggedText> list)
        => string.Concat(list.Select(i => i.ToString()));

    protected async Task TestSignatureHelpInEditorBrowsableContextsAsync(
        string markup,
        string referencedCode,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItemsMetadataReference,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItemsSameSolution,
        string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers = false)
    {
        if (expectedOrderedItemsMetadataReference == null || expectedOrderedItemsSameSolution == null)
        {
            AssertEx.Fail("Expected signature help items must be provided for EditorBrowsable tests. If there are no expected items, provide an empty IEnumerable rather than null.");
        }

        await TestSignatureHelpWithMetadataReferenceHelperAsync(markup, referencedCode, expectedOrderedItemsMetadataReference, sourceLanguage, referencedLanguage, hideAdvancedMembers);
        await TestSignatureHelpWithProjectReferenceHelperAsync(markup, referencedCode, expectedOrderedItemsSameSolution, sourceLanguage, referencedLanguage, hideAdvancedMembers);

        // Multi-language projects are not supported.
        if (sourceLanguage == referencedLanguage)
        {
            await TestSignatureHelpInSameProjectHelperAsync(markup, referencedCode, expectedOrderedItemsSameSolution, sourceLanguage, hideAdvancedMembers);
        }
    }

    public Task TestSignatureHelpWithMetadataReferenceHelperAsync(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                              string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                    <MetadataReferenceFromSource Language="{2}" CommonReferences="true">
                        <Document FilePath="ReferencedDocument">
            {3}
                        </Document>
                    </MetadataReferenceFromSource>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode),
           referencedLanguage, SecurityElement.Escape(referencedCode));

        return VerifyItemWithReferenceWorkerAsync(xmlString, expectedOrderedItems, hideAdvancedMembers);
    }

    public async Task TestSignatureHelpWithProjectReferenceHelperAsync(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                             string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <ProjectReference>ReferencedProject</ProjectReference>
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                </Project>
                <Project Language="{2}" CommonReferences="true" AssemblyName="ReferencedProject">
                    <Document FilePath="ReferencedDocument">
            {3}
                    </Document>
                </Project>

            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode),
           referencedLanguage, SecurityElement.Escape(referencedCode));

        await VerifyItemWithReferenceWorkerAsync(xmlString, expectedOrderedItems, hideAdvancedMembers);
    }

    private async Task TestSignatureHelpInSameProjectHelperAsync(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                      string sourceLanguage, bool hideAdvancedMembers)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferences="true">
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                    <Document FilePath="ReferencedDocument">
            {2}
                    </Document>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(sourceCode), SecurityElement.Escape(referencedCode));

        await VerifyItemWithReferenceWorkerAsync(xmlString, expectedOrderedItems, hideAdvancedMembers);
    }

    protected async Task VerifyItemWithReferenceWorkerAsync(string xmlString, IEnumerable<SignatureHelpTestItem> expectedOrderedItems, bool hideAdvancedMembers)
    {
        using var testWorkspace = EditorTestWorkspace.Create(xmlString);

        var cursorPosition = testWorkspace.Documents.First(d => d.Name == "SourceDocument").CursorPosition.Value;
        var documentId = testWorkspace.Documents.First(d => d.Name == "SourceDocument").Id;
        var document = testWorkspace.CurrentSolution.GetDocument(documentId);

        var options = new MemberDisplayOptions() with { HideAdvancedMembers = hideAdvancedMembers };
        document = testWorkspace.CurrentSolution.GetDocument(documentId);
        var code = (await document.GetTextAsync()).ToString();

        IList<TextSpan> textSpans = null;

        var selectedSpans = testWorkspace.Documents.First(d => d.Name == "SourceDocument").SelectedSpans;
        if (selectedSpans.Any())
        {
            textSpans = selectedSpans;
        }

        TextSpan? textSpan = null;
        if (textSpans != null && textSpans.Any())
        {
            textSpan = textSpans.First();
        }

        await TestSignatureHelpWorkerSharedAsync(testWorkspace, code, cursorPosition, document, options, textSpan, expectedOrderedItems);
    }

    private async Task TestSignatureHelpWorkerSharedAsync(
        EditorTestWorkspace workspace,
        string code,
        int cursorPosition,
        Document document,
        MemberDisplayOptions options,
        TextSpan? textSpan,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
        bool usePreviousCharAsTrigger = false)
    {
        var signatureHelpProviderType = GetSignatureHelpProviderType();
        var signatureHelpProvider = workspace.ExportProvider.GetExportedValues<ISignatureHelpProvider>().Single(provider => provider.GetType() == signatureHelpProviderType);
        var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);

        if (usePreviousCharAsTrigger)
        {
            triggerInfo = new SignatureHelpTriggerInfo(
                SignatureHelpTriggerReason.TypeCharCommand,
                code.ElementAt(cursorPosition - 1));

            if (!signatureHelpProvider.TriggerCharacters.Contains(triggerInfo.TriggerCharacter.Value))
            {
                return;
            }
        }

        var items = await signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, options, CancellationToken.None);

        // If we're expecting 0 items, then there's no need to compare them
        if ((expectedOrderedItemsOrNull == null || !expectedOrderedItemsOrNull.Any()) && items == null)
        {
            return;
        }

        AssertEx.NotNull(items, "Signature help provider returned null for items. Did you forget $$ in the test or is the test otherwise malformed, e.g. quotes not escaped?");

        // Verify the span
        if (textSpan != null)
        {
            Assert.Equal(textSpan, items.ApplicableSpan);
        }

        if (expectedOrderedItemsOrNull != null)
        {
            CompareAndAssertCollectionsAndCurrentParameter(expectedOrderedItemsOrNull, items);
            CompareSelectedIndex(expectedOrderedItemsOrNull, items.SelectedItemIndex);
        }
    }

    private static void CompareSelectedIndex(IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull, int? selectedItemIndex)
    {
        if (expectedOrderedItemsOrNull == null ||
            !expectedOrderedItemsOrNull.Any(i => i.IsSelected))
        {
            return;
        }

        Assert.True(expectedOrderedItemsOrNull.Count(i => i.IsSelected) == 1, "Only one expected item can be marked with 'IsSelected'");
        Assert.True(selectedItemIndex != null, "Expected an item to be selected, but no item was actually selected");

        var counter = 0;
        foreach (var item in expectedOrderedItemsOrNull)
        {
            if (item.IsSelected)
            {
                Assert.True(selectedItemIndex == counter,
                    $"Expected item with index {counter} to be selected, but the actual selected index is {selectedItemIndex}.");
            }
            else
            {
                Assert.True(selectedItemIndex != counter,
                    $"Found unexpected selected item. Actual selected index is {selectedItemIndex}.");
            }

            counter++;
        }
    }

    protected async Task TestSignatureHelpWithMscorlib45Async(
        string markup,
        IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
        string sourceLanguage)
    {
        var xmlString = string.Format("""
            <Workspace>
                <Project Language="{0}" CommonReferencesNet45="true">
                    <Document FilePath="SourceDocument">
            {1}
                    </Document>
                </Project>
            </Workspace>
            """, sourceLanguage, SecurityElement.Escape(markup));

        using var testWorkspace = EditorTestWorkspace.Create(xmlString);

        var cursorPosition = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
        var documentId = testWorkspace.Documents.Where(d => d.Name == "SourceDocument").Single().Id;
        var document = testWorkspace.CurrentSolution.GetDocument(documentId);
        var code = (await document.GetTextAsync()).ToString();
        var options = new MemberDisplayOptions();

        IList<TextSpan> textSpans = null;

        var selectedSpans = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").SelectedSpans;
        if (selectedSpans.Any())
        {
            textSpans = selectedSpans;
        }

        TextSpan? textSpan = null;
        if (textSpans != null && textSpans.Any())
        {
            textSpan = textSpans.First();
        }

        await TestSignatureHelpWorkerSharedAsync(testWorkspace, code, cursorPosition, document, options, textSpan, expectedOrderedItems);
    }
}
