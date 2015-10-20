// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp.Presentation;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
{
    public abstract class AbstractSignatureHelpProviderTests<TWorkspaceFixture> : TestBase, IClassFixture<TWorkspaceFixture>
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected TWorkspaceFixture workspaceFixture;

        internal abstract ISignatureHelpProvider CreateSignatureHelpProvider();

        protected AbstractSignatureHelpProviderTests(TWorkspaceFixture workspaceFixture)
        {
            this.workspaceFixture = workspaceFixture;
        }

        public override void Dispose()
        {
            this.workspaceFixture.CloseTextView();
            base.Dispose();
        }

        /// <summary>
        /// Verifies that sighelp comes up at the indicated location in markup ($$), with the indicated span [| ... |].
        /// </summary>
        /// <param name="markup">Input markup with $$ denoting the cursor position, and [| ... |]
        /// denoting the expected sighelp span</param>
        /// <param name="expectedOrderedItemsOrNull">The exact expected sighelp items list. If null, this part of the test is ignored.</param>
        /// <param name="usePreviousCharAsTrigger">If true, uses the last character before $$ to trigger sighelp.
        /// If false, invokes sighelp explicitly at the cursor location.</param>
        /// <param name="sourceCodeKind">The sourcecodekind to run this test on. If null, runs on both regular and script sources.</param>
        protected virtual void Test(
            string markup,
            IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
            bool usePreviousCharAsTrigger = false,
            SourceCodeKind? sourceCodeKind = null,
            bool experimental = false)
        {
            if (sourceCodeKind.HasValue)
            {
                TestSignatureHelpWorker(markup, sourceCodeKind.Value, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
            }
            else
            {
                TestSignatureHelpWorker(markup, SourceCodeKind.Regular, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
                TestSignatureHelpWorker(markup, SourceCodeKind.Script, experimental, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
            }
        }

        private void TestSignatureHelpWorker(
            string markupWithPositionAndOptSpan,
            SourceCodeKind sourceCodeKind,
            bool experimental,
            IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
            bool usePreviousCharAsTrigger = false)
        {
            markupWithPositionAndOptSpan = markupWithPositionAndOptSpan.NormalizeLineEndings();

            string code;
            int cursorPosition;
            IList<TextSpan> textSpans;
            TextSpan? textSpan = null;
            MarkupTestFile.GetPositionAndSpans(
                markupWithPositionAndOptSpan,
                out code,
                out cursorPosition,
                out textSpans);

            if (textSpans.Any())
            {
                textSpan = textSpans.First();
            }

            var parseOptions = CreateExperimentalParseOptions();

            // regular
            var document1 = workspaceFixture.UpdateDocument(code, sourceCodeKind);
            if (experimental)
            {
                document1 = document1.Project.WithParseOptions(parseOptions).GetDocument(document1.Id);
            }

            TestSignatureHelpWorkerShared(code, cursorPosition, sourceCodeKind, document1, textSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);

            // speculative semantic model
            if (CanUseSpeculativeSemanticModel(document1, cursorPosition))
            {
                var document2 = workspaceFixture.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
                if (experimental)
                {
                    document2 = document2.Project.WithParseOptions(parseOptions).GetDocument(document2.Id);
                }

                TestSignatureHelpWorkerShared(code, cursorPosition, sourceCodeKind, document2, textSpan, expectedOrderedItemsOrNull, usePreviousCharAsTrigger);
            }
        }

        protected abstract ParseOptions CreateExperimentalParseOptions();

        private static bool CanUseSpeculativeSemanticModel(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = document.GetSyntaxRootAsync().Result.FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        protected virtual void VerifyTriggerCharacters(char[] expectedTriggerCharacters, char[] unexpectedTriggerCharacters, SourceCodeKind? sourceCodeKind = null)
        {
            if (sourceCodeKind.HasValue)
            {
                VerifyTriggerCharactersWorker(expectedTriggerCharacters, unexpectedTriggerCharacters, sourceCodeKind.Value);
            }
            else
            {
                VerifyTriggerCharactersWorker(expectedTriggerCharacters, unexpectedTriggerCharacters, SourceCodeKind.Regular);
                VerifyTriggerCharactersWorker(expectedTriggerCharacters, unexpectedTriggerCharacters, SourceCodeKind.Script);
            }
        }

        private void VerifyTriggerCharactersWorker(char[] expectedTriggerCharacters, char[] unexpectedTriggerCharacters, SourceCodeKind sourceCodeKind)
        {
            ISignatureHelpProvider signatureHelpProvider = CreateSignatureHelpProvider();

            foreach (var expectedTriggerCharacter in expectedTriggerCharacters)
            {
                Assert.True(signatureHelpProvider.IsTriggerCharacter(expectedTriggerCharacter), "Expected '" + expectedTriggerCharacter + "' to be a trigger character");
            }

            foreach (var unexpectedTriggerCharacter in unexpectedTriggerCharacters)
            {
                Assert.False(signatureHelpProvider.IsTriggerCharacter(unexpectedTriggerCharacter), "Expected '" + unexpectedTriggerCharacter + "' to NOT be a trigger character");
            }
        }

        protected virtual void VerifyCurrentParameterName(string markup, string expectedParameterName, SourceCodeKind? sourceCodeKind = null)
        {
            if (sourceCodeKind.HasValue)
            {
                VerifyCurrentParameterNameWorker(markup, expectedParameterName, sourceCodeKind.Value);
            }
            else
            {
                VerifyCurrentParameterNameWorker(markup, expectedParameterName, SourceCodeKind.Regular);
                VerifyCurrentParameterNameWorker(markup, expectedParameterName, SourceCodeKind.Script);
            }
        }

        private static SignatureHelpState GetArgumentState(int cursorPosition, Document document, ISignatureHelpProvider signatureHelpProvider, SignatureHelpTriggerInfo triggerInfo)
        {
            var items = signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, CancellationToken.None).WaitAndGetResult(CancellationToken.None);
            return items == null ? null : new SignatureHelpState(items.ArgumentIndex, items.ArgumentCount, items.ArgumentName, null);
        }

        private void VerifyCurrentParameterNameWorker(string markup, string expectedParameterName, SourceCodeKind sourceCodeKind)
        {
            string code;
            int cursorPosition;
            MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), out code, out cursorPosition);

            var document = workspaceFixture.UpdateDocument(code, sourceCodeKind);

            var signatureHelpProvider = CreateSignatureHelpProvider();
            var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);
            var items = signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, CancellationToken.None).Result;
            Assert.Equal(expectedParameterName, GetArgumentState(cursorPosition, document, signatureHelpProvider, triggerInfo).ArgumentName);
        }

        private void CompareAndAssertCollectionsAndCurrentParameter(
            IEnumerable<SignatureHelpTestItem> expectedTestItems, SignatureHelpItems actualSignatureHelpItems, ISignatureHelpProvider signatureHelpProvider, Document document, int cursorPosition)
        {
            Assert.Equal(expectedTestItems.Count(), actualSignatureHelpItems.Items.Count());

            for (int i = 0; i < expectedTestItems.Count(); i++)
            {
                CompareSigHelpItemsAndCurrentPosition(
                    actualSignatureHelpItems,
                    actualSignatureHelpItems.Items.ElementAt(i),
                    expectedTestItems.ElementAt(i),
                    signatureHelpProvider,
                    document,
                    cursorPosition,
                    actualSignatureHelpItems.ApplicableSpan);
            }
        }

        private void CompareSigHelpItemsAndCurrentPosition(
            SignatureHelpItems items,
            SignatureHelpItem actualSignatureHelpItem,
            SignatureHelpTestItem expectedTestItem,
            ISignatureHelpProvider signatureHelpProvider,
            Document document,
            int cursorPosition,
            TextSpan applicableSpan)
        {
            int currentParameterIndex = -1;
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
                Assert.Equal(expectedTestItem.Signature, signature.Content);
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
                Assert.Equal(expectedTestItem.CurrentParameterIndex, items.ArgumentIndex);
            }

            if (expectedTestItem.Description != null)
            {
                Assert.Equal(expectedTestItem.Description, ToString(actualSignatureHelpItem.DescriptionParts));
            }
        }

        private string ToString(IEnumerable<SymbolDisplayPart> list)
        {
            return string.Concat(list.Select(i => i.ToString()));
        }

        protected void TestSignatureHelpInEditorBrowsableContexts(
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

            TestSignatureHelpWithMetadataReferenceHelper(markup, referencedCode, expectedOrderedItemsMetadataReference, sourceLanguage, referencedLanguage, hideAdvancedMembers);
            TestSignatureHelpWithProjectReferenceHelper(markup, referencedCode, expectedOrderedItemsSameSolution, sourceLanguage, referencedLanguage, hideAdvancedMembers);

            // Multi-language projects are not supported.
            if (sourceLanguage == referencedLanguage)
            {
                TestSignatureHelpInSameProjectHelper(markup, referencedCode, expectedOrderedItemsSameSolution, sourceLanguage, hideAdvancedMembers);
            }
        }

        public void TestSignatureHelpWithMetadataReferenceHelper(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                                  string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"">
            <Document FilePath=""ReferencedDocument"">
{3}
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode),
               referencedLanguage, SecurityElement.Escape(referencedCode));

            VerifyItemWithReferenceWorker(xmlString, expectedOrderedItems, hideAdvancedMembers);
        }

        public void TestSignatureHelpWithProjectReferenceHelper(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                                 string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <ProjectReference>ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"">
        <Document FilePath=""ReferencedDocument"">
{3}
        </Document>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode),
               referencedLanguage, SecurityElement.Escape(referencedCode));

            VerifyItemWithReferenceWorker(xmlString, expectedOrderedItems, hideAdvancedMembers);
        }

        private void TestSignatureHelpInSameProjectHelper(string sourceCode, string referencedCode, IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
                                                          string sourceLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <Document FilePath=""ReferencedDocument"">
{2}
        </Document>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode), SecurityElement.Escape(referencedCode));

            VerifyItemWithReferenceWorker(xmlString, expectedOrderedItems, hideAdvancedMembers);
        }

        protected void VerifyItemWithReferenceWorker(string xmlString, IEnumerable<SignatureHelpTestItem> expectedOrderedItems, bool hideAdvancedMembers)
        {
            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var optionsService = testWorkspace.Services.GetService<IOptionService>();
                var cursorPosition = testWorkspace.Documents.First(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = testWorkspace.Documents.First(d => d.Name == "SourceDocument").Id;
                var document = testWorkspace.CurrentSolution.GetDocument(documentId);
                var code = document.GetTextAsync().Result.ToString();

                optionsService.SetOptions(optionsService.GetOptions().WithChangedOption(Microsoft.CodeAnalysis.Completion.CompletionOptions.HideAdvancedMembers, document.Project.Language, hideAdvancedMembers));

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

                TestSignatureHelpWorkerShared(code, cursorPosition, SourceCodeKind.Regular, document, textSpan, expectedOrderedItems);
            }
        }

        private void TestSignatureHelpWorkerShared(
            string code,
            int cursorPosition,
            SourceCodeKind sourceCodeKind,
            Document document,
            TextSpan? textSpan,
            IEnumerable<SignatureHelpTestItem> expectedOrderedItemsOrNull = null,
            bool usePreviousCharAsTrigger = false)
        {
            var signatureHelpProvider = CreateSignatureHelpProvider();
            var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);

            if (usePreviousCharAsTrigger)
            {
                triggerInfo = new SignatureHelpTriggerInfo(
                    SignatureHelpTriggerReason.TypeCharCommand,
                    code.ElementAt(cursorPosition - 1));

                if (!signatureHelpProvider.IsTriggerCharacter(triggerInfo.TriggerCharacter.Value))
                {
                    return;
                }
            }

            var items = signatureHelpProvider.GetItemsAsync(document, cursorPosition, triggerInfo, CancellationToken.None).Result;

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
                CompareAndAssertCollectionsAndCurrentParameter(expectedOrderedItemsOrNull, items, signatureHelpProvider, document, cursorPosition);
            }
        }

        protected void TestSignatureHelpWithMscorlib45(
            string markup,
            IEnumerable<SignatureHelpTestItem> expectedOrderedItems,
            string sourceLanguage)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferencesNet45=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup));

            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var cursorPosition = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = testWorkspace.Documents.Where(d => d.Name == "SourceDocument").Single().Id;
                var document = testWorkspace.CurrentSolution.GetDocument(documentId);
                var code = document.GetTextAsync().Result.ToString();

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

                TestSignatureHelpWorkerShared(code, cursorPosition, SourceCodeKind.Regular, document, textSpan, expectedOrderedItems);
            }
        }
    }
}
