// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security;
using System.Threading;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion
{
    public abstract class AbstractCompletionProviderTests<TWorkspaceFixture> : TestBase, IUseFixture<TWorkspaceFixture>
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected readonly Mock<ICompletionSession> MockCompletionSession;
        internal ICompletionProvider CompletionProvider;
        protected TWorkspaceFixture workspaceFixture;

        public AbstractCompletionProviderTests()
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

            MockCompletionSession = new Mock<ICompletionSession>(MockBehavior.Strict);
        }

        public void SetFixture(TWorkspaceFixture workspaceFixture)
        {
            this.workspaceFixture = workspaceFixture;
            this.CompletionProvider = CreateCompletionProvider();
        }

        protected static bool CanUseSpeculativeSemanticModel(Document document, int position)
        {
            var service = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var node = document.GetSyntaxRootAsync().Result.FindToken(position).Parent;

            return !service.GetMemberBodySpanForSpeculativeBinding(node).IsEmpty;
        }

        private void CheckResults(Document document, int position, string expectedItemOrNull, string expectedDescriptionOrNull, bool usePreviousCharAsTrigger, bool checkForAbsence, Glyph? glyph)
        {
            var code = document.GetTextAsync().Result.ToString();

            CompletionTriggerInfo completionTriggerInfo = new CompletionTriggerInfo();

            if (usePreviousCharAsTrigger)
            {
                completionTriggerInfo = CompletionTriggerInfo.CreateTypeCharTriggerInfo(triggerCharacter: code.ElementAt(position - 1));
            }

            var group = CompletionProvider.GetGroupAsync(document, position, completionTriggerInfo).Result;
            var completions = group == null ? null : group.Items;

            if (checkForAbsence)
            {
                if (completions == null)
                {
                    return;
                }

                if (expectedItemOrNull == null)
                {
                    Assert.Empty(completions);
                }
                else
                {
                    AssertEx.None(
                        completions,
                        c => CompareItems(c.DisplayText, expectedItemOrNull) &&
                            (expectedDescriptionOrNull != null ? c.GetDescriptionAsync().Result.GetFullText() == expectedDescriptionOrNull : true));
                }
            }
            else
            {
                if (expectedItemOrNull == null)
                {
                    Assert.NotEmpty(completions);
                }
                else
                {
                    AssertEx.Any(completions, c => CompareItems(c.DisplayText, expectedItemOrNull)
                        && (expectedDescriptionOrNull != null ? c.GetDescriptionAsync().Result.GetFullText() == expectedDescriptionOrNull : true)
                        && (glyph.HasValue ? c.Glyph == glyph.Value : true));
                }
            }
        }

        private void Verify(string markup, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), out code, out position);

            VerifyWorker(code, position, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, usePreviousCharAsTrigger, checkForAbsence, experimental, glyph);
        }

        protected void VerifyCustomCommitProvider(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind? sourceCodeKind = null, char? commitChar = null)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markupBeforeCommit.NormalizeLineEndings(), out code, out position);

            if (sourceCodeKind.HasValue)
            {
                VerifyCustomCommitProviderWorker(code, position, itemToCommit, expectedCodeAfterCommit, sourceCodeKind.Value, commitChar);
            }
            else
            {
                VerifyCustomCommitProviderWorker(code, position, itemToCommit, expectedCodeAfterCommit, SourceCodeKind.Regular, commitChar);
                VerifyCustomCommitProviderWorker(code, position, itemToCommit, expectedCodeAfterCommit, SourceCodeKind.Script, commitChar);
            }
        }

        protected void VerifyProviderCommit(string markupBeforeCommit, string itemToCommit, string expectedCodeAfterCommit,
            char? commitChar, string textTypedSoFar, SourceCodeKind? sourceCodeKind = null)
        {
            string code;
            int position;
            MarkupTestFile.GetPosition(markupBeforeCommit.NormalizeLineEndings(), out code, out position);

            expectedCodeAfterCommit = expectedCodeAfterCommit.NormalizeLineEndings();
            if (sourceCodeKind.HasValue)
            {
                VerifyProviderCommitWorker(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, sourceCodeKind.Value);
            }
            else
            {
                VerifyProviderCommitWorker(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, SourceCodeKind.Regular);
                VerifyProviderCommitWorker(code, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar, SourceCodeKind.Script);
            }
        }

        protected virtual bool CompareItems(string actualItem, string expectedItem)
        {
            return actualItem.Equals(expectedItem);
        }

        protected void VerifyItemExists(string markup, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false, int? glyph = null)
        {
            if (sourceCodeKind.HasValue)
            {
                Verify(markup, expectedItem, expectedDescriptionOrNull, sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: glyph);
            }
            else
            {
                Verify(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: glyph);
                Verify(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: glyph);
            }
        }

        protected void VerifyItemIsAbsent(string markup, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
        {
            if (sourceCodeKind.HasValue)
            {
                Verify(markup, expectedItem, expectedDescriptionOrNull, sourceCodeKind.Value, usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
            }
            else
            {
                Verify(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Regular, usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
                Verify(markup, expectedItem, expectedDescriptionOrNull, SourceCodeKind.Script, usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
            }
        }

        protected void VerifyAnyItemExists(string markup, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
        {
            if (sourceCodeKind.HasValue)
            {
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: null);
            }
            else
            {
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: null);
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: false, experimental: experimental, glyph: null);
            }
        }

        protected void VerifyNoItemsExist(string markup, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
        {
            if (sourceCodeKind.HasValue)
            {
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: sourceCodeKind.Value, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
            }
            else
            {
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Regular, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
                Verify(markup, expectedItemOrNull: null, expectedDescriptionOrNull: null, sourceCodeKind: SourceCodeKind.Script, usePreviousCharAsTrigger: usePreviousCharAsTrigger, checkForAbsence: true, experimental: experimental, glyph: null);
            }
        }

        internal abstract ICompletionProvider CreateCompletionProvider();

        /// <summary>
        /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
        /// </summary>
        /// <param name="code">The source code (not markup).</param>
        /// <param name="expectedItemOrNull">The expected item. If this is null, verifies that *any* item shows up for this CompletionProvider (or no items show up if checkForAbsence is true).</param>
        /// <param name="expectedDescriptionOrNull">If this is null, the Description for the item is ignored.</param>
        /// <param name="usePreviousCharAsTrigger">Whether or not the previous character in markup should be used to trigger IntelliSense for this provider. If false, invokes it through the invoke IntelliSense command.</param>
        /// <param name="checkForAbsence">If true, checks for absence of a specific item (or that no items are returned from this CompletionProvider)</param>
        protected virtual void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            if (experimental)
            {
                foreach (var project in workspaceFixture.Workspace.Projects)
                {
                    workspaceFixture.Workspace.OnParseOptionsChanged(project.Id, CreateExperimentalParseOptions(project.ParseOptions));
                }
            }

            Glyph? expectedGlyph = null;
            if (glyph.HasValue)
            {
                expectedGlyph = (Glyph)glyph.Value;
            }

            var document1 = workspaceFixture.UpdateDocument(code, sourceCodeKind);
            CheckResults(document1, position, expectedItemOrNull, expectedDescriptionOrNull, usePreviousCharAsTrigger, checkForAbsence, expectedGlyph);

            if (CanUseSpeculativeSemanticModel(document1, position))
            {
                var document2 = workspaceFixture.UpdateDocument(code, sourceCodeKind, cleanBeforeUpdate: false);
                CheckResults(document2, position, expectedItemOrNull, expectedDescriptionOrNull, usePreviousCharAsTrigger, checkForAbsence, expectedGlyph);
            }
        }

        protected virtual ParseOptions CreateExperimentalParseOptions(ParseOptions parseOptions)
        {
            return parseOptions;
        }

        /// <summary>
        /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
        /// </summary>
        /// <param name="codeBeforeCommit">The source code (not markup).</param>
        /// <param name="position">Position where intellisense is invoked.</param>
        /// <param name="itemToCommit">The item to commit from the completion provider.</param>
        /// <param name="expectedCodeAfterCommit">The expected code after commit.</param>
        protected virtual void VerifyCustomCommitProviderWorker(string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit, SourceCodeKind sourceCodeKind, char? commitChar = null)
        {
            var document1 = workspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            VerifyCustomCommitProviderCheckResults(document1, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);

            if (CanUseSpeculativeSemanticModel(document1, position))
            {
                var document2 = workspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                VerifyCustomCommitProviderCheckResults(document2, codeBeforeCommit, position, itemToCommit, expectedCodeAfterCommit, commitChar);
            }
        }

        private void VerifyCustomCommitProviderCheckResults(Document document, string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitChar)
        {
            var textBuffer = workspaceFixture.Workspace.Documents.Single().TextBuffer;

            var completions = CompletionProvider.GetGroupAsync(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()).Result.Items;
            var completionItem = completions.First(i => CompareItems(i.DisplayText, itemToCommit));

            var customCommitCompletionProvider = CompletionProvider as ICustomCommitCompletionProvider;
            if (customCommitCompletionProvider != null)
            {
                var textView = workspaceFixture.Workspace.Documents.Single().GetTextView();
                VerifyCustomCommitWorker(customCommitCompletionProvider, completionItem, textView, textBuffer, codeBeforeCommit, expectedCodeAfterCommit, commitChar);
            }
            else
            {
                throw new Exception();
            }
        }

        internal virtual void VerifyCustomCommitWorker(ICustomCommitCompletionProvider customCommitCompletionProvider,
            CompletionItem completionItem,
            ITextView textView,
            ITextBuffer textBuffer,
            string codeBeforeCommit,
            string expectedCodeAfterCommit,
            char? commitChar = null)
        {
            int expectedCaretPosition;
            string actualExpectedCode = null;
            MarkupTestFile.GetPosition(expectedCodeAfterCommit, out actualExpectedCode, out expectedCaretPosition);

            if (commitChar.HasValue && !customCommitCompletionProvider.IsCommitCharacter(completionItem, commitChar.Value, string.Empty))
            {
                Assert.Equal(codeBeforeCommit, actualExpectedCode);
                return;
            }

            customCommitCompletionProvider.Commit(completionItem, textView, textBuffer, textView.TextSnapshot, commitChar);

            string actualCodeAfterCommit = textBuffer.CurrentSnapshot.AsText().ToString();
            var caretPosition = textView.Caret.Position.BufferPosition.Position;

            Assert.Equal(actualExpectedCode, actualCodeAfterCommit);
            Assert.Equal(expectedCaretPosition, caretPosition);
        }

        /// <summary>
        /// Override this to change parameters or return without verifying anything, e.g. for script sources. Or to test in other code contexts.
        /// </summary>
        /// <param name="codeBeforeCommit">The source code (not markup).</param>
        /// <param name="position">Position where intellisense is invoked.</param>
        /// <param name="itemToCommit">The item to commit from the completion provider.</param>
        /// <param name="expectedCodeAfterCommit">The expected code after commit.</param>
        protected virtual void VerifyProviderCommitWorker(string codeBeforeCommit, int position, string itemToCommit, string expectedCodeAfterCommit,
            char? commitChar, string textTypedSoFar, SourceCodeKind sourceCodeKind)
        {
            var document1 = workspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind);
            VerifyProviderCommitCheckResults(document1, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar);

            if (CanUseSpeculativeSemanticModel(document1, position))
            {
                var document2 = workspaceFixture.UpdateDocument(codeBeforeCommit, sourceCodeKind, cleanBeforeUpdate: false);
                VerifyProviderCommitCheckResults(document2, position, itemToCommit, expectedCodeAfterCommit, commitChar, textTypedSoFar);
            }
        }

        private void VerifyProviderCommitCheckResults(Document document, int position, string itemToCommit, string expectedCodeAfterCommit, char? commitChar, string textTypedSoFar)
        {
            var textBuffer = workspaceFixture.Workspace.Documents.Single().TextBuffer;
            var textSnapshot = textBuffer.CurrentSnapshot.AsText();

            var completions = CompletionProvider.GetGroupAsync(document, position, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo()).Result.Items;
            var completionItem = completions.First(i => CompareItems(i.DisplayText, itemToCommit));

            var textChange = CompletionProvider.IsCommitCharacter(completionItem, commitChar.HasValue ? commitChar.Value : ' ', textTypedSoFar)
                ? CompletionProvider.GetTextChange(completionItem, commitChar, textTypedSoFar)
                : new TextChange();

            var oldText = document.GetTextAsync().Result;
            var newText = oldText.WithChanges(textChange);
            Assert.Equal(expectedCodeAfterCommit, newText.ToString());
        }

        protected void VerifyItemInEditorBrowsableContexts(string markup, string referencedCode, string item, int expectedSymbolsSameSolution, int expectedSymbolsMetadataReference,
                                                           string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers = false)
        {
            CompletionProvider = CreateCompletionProvider();

            VerifyItemWithMetadataReference(markup, referencedCode, item, expectedSymbolsMetadataReference, sourceLanguage, referencedLanguage, hideAdvancedMembers);
            VerifyItemWithProjectReference(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage, referencedLanguage, hideAdvancedMembers);

            // If the source and referenced languages are different, then they cannot be in the same project
            if (sourceLanguage == referencedLanguage)
            {
                VerifyItemInSameProject(markup, referencedCode, item, expectedSymbolsSameSolution, sourceLanguage, hideAdvancedMembers);
            }
        }

        private void VerifyItemWithMetadataReference(string markup, string metadataReferenceCode, string expectedItem, int expectedSymbols,
                                                           string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
            <Document FilePath=""ReferencedDocument"">
{3}
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(metadataReferenceCode));

            VerifyItemWithReferenceWorker(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected void VerifyItemWithAliasedMetadataReferences(string markup, string metadataAlias, string expectedItem, int expectedSymbols,
                                                   string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" Aliases=""{3}, global"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
            <Document FilePath=""ReferencedDocument"">
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(metadataAlias));

            VerifyItemWithReferenceWorker(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        protected void VerifyItemWithProjectReference(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage, string referencedLanguage, bool hideAdvancedMembers)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <ProjectReference>ReferencedProject</ProjectReference>
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
    <Project Language=""{2}"" CommonReferences=""true"" AssemblyName=""ReferencedProject"" IncludeXmlDocComments=""true"" DocumentationMode=""Diagnose"">
        <Document FilePath=""ReferencedDocument"">
{3}
        </Document>
    </Project>
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), referencedLanguage, SecurityElement.Escape(referencedCode));

            VerifyItemWithReferenceWorker(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        private void VerifyItemInSameProject(string markup, string referencedCode, string expectedItem, int expectedSymbols, string sourceLanguage, bool hideAdvancedMembers)
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
    
</Workspace>", sourceLanguage, SecurityElement.Escape(markup), SecurityElement.Escape(referencedCode));

            VerifyItemWithReferenceWorker(xmlString, expectedItem, expectedSymbols, hideAdvancedMembers);
        }

        private void VerifyItemWithReferenceWorker(string xmlString, string expectedItem, int expectedSymbols, bool hideAdvancedMembers)
        {
            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var optionsService = testWorkspace.Services.GetService<IOptionService>();
                int cursorPosition = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                DocumentId docId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                Document doc = solution.GetDocument(docId);

                optionsService.SetOptions(optionsService.GetOptions().WithChangedOption(Microsoft.CodeAnalysis.Completion.CompletionOptions.HideAdvancedMembers, doc.Project.Language, hideAdvancedMembers));

                CompletionTriggerInfo completionTriggerInfo = new CompletionTriggerInfo();
                var completions = CompletionProvider.GetGroupAsync(doc, cursorPosition, completionTriggerInfo).Result;

                if (expectedSymbols >= 1)
                {
                    AssertEx.Any(completions.Items, c => CompareItems(c.DisplayText, expectedItem));

                    // Throw if multiple to indicate a bad test case
                    var description = completions.Items.Single(c => CompareItems(c.DisplayText, expectedItem)).GetDescriptionAsync().Result;

                    if (expectedSymbols == 1)
                    {
                        Assert.DoesNotContain("+", description.GetFullText(), StringComparison.Ordinal);
                    }
                    else
                    {
                        Assert.Contains(GetExpectedOverloadSubstring(expectedSymbols), description.GetFullText(), StringComparison.Ordinal);
                    }
                }
                else
                {
                    if (completions != null)
                    {
                        AssertEx.None(completions.Items, c => CompareItems(c.DisplayText, expectedItem));
                    }
                }
            }
        }

        protected void VerifyItemWithMscorlib45(string markup, string expectedItem, string expectedDescription, string sourceLanguage)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferencesNet45=""true""> 
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(markup));

            VerifyItemWithMscorlib45Worker(xmlString, expectedItem, expectedDescription);
        }

        private void VerifyItemWithMscorlib45Worker(string xmlString, string expectedItem, string expectedDescription)
        {
            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                int cursorPosition = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                DocumentId docId = testWorkspace.Documents.Single(d => d.Name == "SourceDocument").Id;
                Document doc = solution.GetDocument(docId);

                CompletionTriggerInfo completionTriggerInfo = new CompletionTriggerInfo();
                var completions = CompletionProvider.GetGroupAsync(doc, cursorPosition, completionTriggerInfo).Result;
                var item = completions.Items.FirstOrDefault(i => i.DisplayText == expectedItem);
                Assert.Equal(expectedDescription, item.GetDescriptionAsync().Result.GetFullText());
            }
        }

        private const char NonBreakingSpace = (char)0x00A0;

        private string GetExpectedOverloadSubstring(int expectedSymbols)
        {
            if (expectedSymbols <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedSymbols));
            }

            return "+" + NonBreakingSpace + (expectedSymbols - 1) + NonBreakingSpace + FeaturesResources.Overload;
        }

        protected void VerifyItemInLinkedFiles(string xmlString, string expectedItem, string expectedDescription)
        {
            using (var testWorkspace = TestWorkspaceFactory.CreateWorkspace(xmlString))
            {
                var optionsService = testWorkspace.Services.GetService<IOptionService>();
                int cursorPosition = testWorkspace.Documents.First().CursorPosition.Value;
                var solution = testWorkspace.CurrentSolution;
                var textContainer = testWorkspace.Documents.First().TextBuffer.AsTextContainer();
                var currentContextDocumentId = testWorkspace.GetDocumentIdInCurrentContext(textContainer);
                Document doc = solution.GetDocument(currentContextDocumentId);

                CompletionTriggerInfo completionTriggerInfo = new CompletionTriggerInfo();
                var completions = CompletionProvider.GetGroupAsync(doc, cursorPosition, completionTriggerInfo).WaitAndGetResult(CancellationToken.None);

                var item = completions.Items.Single(c => c.DisplayText == expectedItem);
                Assert.NotNull(item);
                if (expectedDescription != null)
                {
                    var actualDescription = item.GetDescriptionAsync().Result.GetFullText();
                    Assert.Equal(expectedDescription, actualDescription);
                }
            }
        }
    }
}
