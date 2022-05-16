// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    [UseExportProvider]
    public class ExtractMethodBase
    {
        protected static async Task ExpectExtractMethodToFailAsync(string codeWithMarker, bool dontPutOutOrRefOnStruct = true, string[] features = null)
        {
            ParseOptions parseOptions = null;
            if (features != null)
            {
                var featuresMapped = features.Select(x => new KeyValuePair<string, string>(x, string.Empty));
                parseOptions = new CSharpParseOptions().WithFeatures(featuresMapped);
            }

            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, parseOptions: parseOptions);
            var testDocument = workspace.Documents.First();
            var textSpan = testDocument.SelectedSpans.Single();
            var treeAfterExtractMethod = await ExtractMethodAsync(workspace, testDocument, succeed: false, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);
        }

        protected static async Task ExpectExtractMethodToFailAsync(
            string codeWithMarker,
            string expected,
            bool dontPutOutOrRefOnStruct = true,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, parseOptions: parseOptions);
            var testDocument = workspace.Documents.Single();
            var subjectBuffer = testDocument.GetTextBuffer();

            var tree = await ExtractMethodAsync(workspace, testDocument, succeed: false, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);

            using (var edit = subjectBuffer.CreateEdit())
            {
                edit.Replace(0, edit.Snapshot.Length, tree.ToFullString());
                edit.Apply();
            }

            if (expected == "")
                Assert.True(false, subjectBuffer.CurrentSnapshot.GetText());

            Assert.Equal(expected, subjectBuffer.CurrentSnapshot.GetText());
        }

        protected static async Task NotSupported_ExtractMethodAsync(string codeWithMarker)
        {
            using (var workspace = TestWorkspace.CreateCSharp(codeWithMarker))
            {
                Assert.NotNull(await Record.ExceptionAsync(async () =>
                {
                    var testDocument = workspace.Documents.Single();
                    var tree = await ExtractMethodAsync(workspace, testDocument);
                }));
            }
        }

        protected static async Task TestExtractMethodAsync(
            string codeWithMarker,
            string expected,
            bool temporaryFailing = false,
            bool dontPutOutOrRefOnStruct = true,
            bool allowBestEffort = false,
            CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, parseOptions: parseOptions);
            var testDocument = workspace.Documents.Single();
            var subjectBuffer = testDocument.GetTextBuffer();

            var tree = await ExtractMethodAsync(
                workspace, testDocument,
                dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct,
                allowBestEffort: allowBestEffort);

            using (var edit = subjectBuffer.CreateEdit())
            {
                edit.Replace(0, edit.Snapshot.Length, tree.ToFullString());
                edit.Apply();
            }

            var actual = subjectBuffer.CurrentSnapshot.GetText();
            if (temporaryFailing)
            {
                Assert.NotEqual(expected, actual);
            }
            else
            {
                if (expected != "")
                {
                    AssertEx.EqualOrDiff(expected, actual);
                }
                else
                {
                    // print out the entire diff to make adding tests simpler.
                    Assert.Equal((object)expected, actual);
                }
            }
        }

        protected static async Task<SyntaxNode> ExtractMethodAsync(
            TestWorkspace workspace,
            TestHostDocument testDocument,
            bool succeed = true,
            bool dontPutOutOrRefOnStruct = true,
            bool allowBestEffort = false)
        {
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            Assert.NotNull(document);

            var options = new ExtractMethodGenerationOptions(
                ExtractOptions: new ExtractMethodOptions(dontPutOutOrRefOnStruct),
                CodeGenerationOptions: CodeGenerationOptions.GetDefault(document.Project.LanguageServices),
                AddImportOptions: AddImportPlacementOptions.Default,
                NamingPreferences: _ => NamingStylePreferences.Default);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var validator = new CSharpSelectionValidator(semanticDocument, testDocument.SelectedSpans.Single(), options.ExtractOptions, localFunction: false);

            var selectedCode = await validator.GetValidSelectionAsync(CancellationToken.None);
            if (!succeed && selectedCode.Status.FailedWithNoBestEffortSuggestion())
            {
                return null;
            }

            Assert.True(selectedCode.ContainsValidContext);

            // extract method
            var extractor = new CSharpMethodExtractor((CSharpSelectionResult)selectedCode, options, localFunction: false);
            var result = await extractor.ExtractMethodAsync(CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(succeed,
                result.Succeeded ||
                result.SucceededWithSuggestion ||
                (allowBestEffort && result.Status.HasBestEffort()));

            var (doc, _) = await result.GetFormattedDocumentAsync(CodeCleanupOptions.GetDefault(document.Project.LanguageServices), CancellationToken.None);
            return doc == null
                ? null
                : await doc.GetSyntaxRootAsync();
        }

        protected static async Task TestSelectionAsync(string codeWithMarker, bool expectedFail = false, CSharpParseOptions parseOptions = null, TextSpan? textSpanOverride = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, parseOptions: parseOptions);
            var testDocument = workspace.Documents.Single();
            var namedSpans = testDocument.AnnotatedSpans;

            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            Assert.NotNull(document);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var validator = new CSharpSelectionValidator(semanticDocument, textSpanOverride ?? namedSpans["b"].Single(), ExtractMethodOptions.Default, localFunction: false);
            var result = await validator.GetValidSelectionAsync(CancellationToken.None);

            Assert.True(expectedFail ? result.Status.Failed() : result.Status.Succeeded());

            if ((result.Status.Succeeded() || result.Status.Flag.HasBestEffort()) && result.Status.Flag.HasSuggestion())
            {
                Assert.Equal(namedSpans["r"].Single(), result.FinalSpan);
            }
        }

        protected static async Task IterateAllAsync(string code)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, CodeAnalysis.CSharp.Test.Utilities.TestOptions.Regular);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            Assert.NotNull(document);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var root = await document.GetSyntaxRootAsync();
            var iterator = root.DescendantNodesAndSelf().Cast<SyntaxNode>();

            foreach (var node in iterator)
            {
                var validator = new CSharpSelectionValidator(semanticDocument, node.Span, ExtractMethodOptions.Default, localFunction: false);
                var result = await validator.GetValidSelectionAsync(CancellationToken.None);

                // check the obvious case
                if (!(node is ExpressionSyntax) && !node.UnderValidContext())
                {
                    Assert.True(result.Status.FailedWithNoBestEffortSuggestion());
                }
            }
        }
    }
}
