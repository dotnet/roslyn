// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.QuickInfo;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QuickInfo
{
    public abstract class SemanticQuickInfoSourceTestsBase : AbstractSemanticQuickInfoSourceTests
    {
        protected async Task TestWithOptionsAsync(CSharpParseOptions options, string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            using (var workspace = TestWorkspace.CreateCSharp(markup, options))
            {
                await TestWithOptionsAsync(workspace, expectedResults);
            }
        }

        protected async Task TestWithOptionsAsync(CSharpCompilationOptions options, string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            using (var workspace = TestWorkspace.CreateCSharp(markup, compilationOptions: options))
            {
                await TestWithOptionsAsync(workspace, expectedResults);
            }
        }

        protected async Task TestWithOptionsAsync(TestWorkspace workspace, params Action<QuickInfoItem>[] expectedResults)
        {
            var testDocument = workspace.DocumentWithCursor;
            var position = testDocument.CursorPosition.GetValueOrDefault();
            var documentId = workspace.GetDocumentId(testDocument);
            var document = workspace.CurrentSolution.GetDocument(documentId);

            var service = QuickInfoService.GetService(document);

            await TestWithOptionsAsync(document, service, position, expectedResults);

            // speculative semantic model
            if (await CanUseSpeculativeSemanticModelAsync(document, position))
            {
                var buffer = testDocument.GetTextBuffer();
                using (var edit = buffer.CreateEdit())
                {
                    var currentSnapshot = buffer.CurrentSnapshot;
                    edit.Replace(0, currentSnapshot.Length, currentSnapshot.GetText());
                    edit.Apply();
                }

                await TestWithOptionsAsync(document, service, position, expectedResults);
            }
        }

        private async Task TestWithOptionsAsync(Document document, QuickInfoService service, int position, Action<QuickInfoItem>[] expectedResults)
        {
            var info = await service.GetQuickInfoAsync(document, position, cancellationToken: CancellationToken.None);

            if (expectedResults.Length == 0)
            {
                Assert.Null(info);
            }
            else
            {
                Assert.NotNull(info);

                foreach (var expected in expectedResults)
                {
                    expected(info);
                }
            }
        }

        protected async Task VerifyWithMscorlib45Async(string markup, Action<QuickInfoItem>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""C#"" CommonReferencesNet45=""true"">
        <Document FilePath=""SourceDocument"">
{0}
        </Document>
    </Project>
</Workspace>", SecurityElement.Escape(markup));

            using (var workspace = TestWorkspace.Create(xmlString))
            {
                var position = workspace.Documents.Single(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = workspace.Documents.Where(d => d.Name == "SourceDocument").Single().Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);

                var service = QuickInfoService.GetService(document);

                var info = await service.GetQuickInfoAsync(document, position, cancellationToken: CancellationToken.None);

                if (expectedResults.Length == 0)
                {
                    Assert.Null(info);
                }
                else
                {
                    Assert.NotNull(info);

                    foreach (var expected in expectedResults)
                    {
                        expected(info);
                    }
                }
            }
        }

        protected override async Task TestAsync(string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            await TestWithOptionsAsync(Options.Regular.WithLanguageVersion(LanguageVersion.Latest), markup, expectedResults);
            await TestWithOptionsAsync(Options.Script.WithLanguageVersion(LanguageVersion.Latest), markup, expectedResults);
        }

        protected async Task TestWithUsingsAsync(string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            var markupWithUsings =
@"using System;
using System.Collections.Generic;
using System.Linq;
" + markup;

            await TestAsync(markupWithUsings, expectedResults);
        }

        protected Task TestInClassAsync(string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            var markupInClass = "class C { " + markup + " }";
            return TestWithUsingsAsync(markupInClass, expectedResults);
        }

        protected Task TestInMethodAsync(string markup, params Action<QuickInfoItem>[] expectedResults)
        {
            var markupInMethod = "class C { void M() { " + markup + " } }";
            return TestWithUsingsAsync(markupInMethod, expectedResults);
        }

        protected async Task TestWithReferenceAsync(string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<QuickInfoItem>[] expectedResults)
        {
            await TestWithMetadataReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);
            await TestWithProjectReferenceHelperAsync(sourceCode, referencedCode, sourceLanguage, referencedLanguage, expectedResults);

            // Multi-language projects are not supported.
            if (sourceLanguage == referencedLanguage)
            {
                await TestInSameProjectHelperAsync(sourceCode, referencedCode, sourceLanguage, expectedResults);
            }
        }

        protected async Task TestWithMetadataReferenceHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<QuickInfoItem>[] expectedResults)
        {
            var xmlString = string.Format(@"
<Workspace>
    <Project Language=""{0}"" CommonReferences=""true"">
        <Document FilePath=""SourceDocument"">
{1}
        </Document>
        <MetadataReferenceFromSource Language=""{2}"" CommonReferences=""true"" IncludeXmlDocComments=""true"">
            <Document FilePath=""ReferencedDocument"">
{3}
            </Document>
        </MetadataReferenceFromSource>
    </Project>
</Workspace>", sourceLanguage, SecurityElement.Escape(sourceCode),
               referencedLanguage, SecurityElement.Escape(referencedCode));

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        private async Task TestWithProjectReferenceHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            string referencedLanguage,
            params Action<QuickInfoItem>[] expectedResults)
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

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        private async Task TestInSameProjectHelperAsync(
            string sourceCode,
            string referencedCode,
            string sourceLanguage,
            params Action<QuickInfoItem>[] expectedResults)
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

            await VerifyWithReferenceWorkerAsync(xmlString, expectedResults);
        }

        protected async Task VerifyWithReferenceWorkerAsync(string xmlString, params Action<QuickInfoItem>[] expectedResults)
        {
            using (var workspace = TestWorkspace.Create(xmlString))
            {
                var position = workspace.Documents.First(d => d.Name == "SourceDocument").CursorPosition.Value;
                var documentId = workspace.Documents.First(d => d.Name == "SourceDocument").Id;
                var document = workspace.CurrentSolution.GetDocument(documentId);

                var service = QuickInfoService.GetService(document);

                var info = await service.GetQuickInfoAsync(document, position, cancellationToken: CancellationToken.None);

                if (expectedResults.Length == 0)
                {
                    Assert.Null(info);
                }
                else
                {
                    Assert.NotNull(info);

                    foreach (var expected in expectedResults)
                    {
                        expected(info);
                    }
                }
            }
        }

        protected async Task TestInvalidTypeInClassAsync(string code)
        {
            var codeInClass = "class C { " + code + " }";
            await TestAsync(codeInClass);
        }
    }
}
