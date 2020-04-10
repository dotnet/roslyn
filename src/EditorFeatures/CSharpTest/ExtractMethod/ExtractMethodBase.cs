﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    [UseExportProvider]
    public class ExtractMethodBase
    {
        protected async Task ExpectExtractMethodToFailAsync(string codeWithMarker, bool dontPutOutOrRefOnStruct = true, string[] features = null)
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

        protected async Task ExpectExtractMethodToFailAsync(
            string codeWithMarker,
            string expected,
            bool allowMovingDeclaration = true,
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

        protected async Task NotSupported_ExtractMethodAsync(string codeWithMarker)
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

        protected async Task TestExtractMethodAsync(
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
                    Assert.Equal(expected, actual);
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

            var originalOptions = await document.GetOptionsAsync();
            var options = originalOptions.WithChangedOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language, dontPutOutOrRefOnStruct);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var validator = new CSharpSelectionValidator(semanticDocument, testDocument.SelectedSpans.Single(), options);

            var selectedCode = await validator.GetValidSelectionAsync(CancellationToken.None);
            if (!succeed && selectedCode.Status.FailedWithNoBestEffortSuggestion())
            {
                return null;
            }

            Assert.True(selectedCode.ContainsValidContext);

            // extract method
            var extractor = new CSharpMethodExtractor((CSharpSelectionResult)selectedCode, localFunction: false);
            var result = await extractor.ExtractMethodAsync(CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(succeed,
                result.Succeeded ||
                result.SucceededWithSuggestion ||
                (allowBestEffort && result.Status.HasBestEffort()));

            var doc = result.Document;
            return doc == null
                ? null
                : await doc.GetSyntaxRootAsync();
        }

        protected async Task TestSelectionAsync(string codeWithMarker, bool expectedFail = false, CSharpParseOptions parseOptions = null)
        {
            using var workspace = TestWorkspace.CreateCSharp(codeWithMarker, parseOptions: parseOptions);
            var testDocument = workspace.Documents.Single();
            var namedSpans = testDocument.AnnotatedSpans;

            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            Assert.NotNull(document);

            var options = await document.GetOptionsAsync(CancellationToken.None);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var validator = new CSharpSelectionValidator(semanticDocument, namedSpans["b"].Single(), options);
            var result = await validator.GetValidSelectionAsync(CancellationToken.None);

            Assert.True(expectedFail ? result.Status.Failed() : result.Status.Succeeded());

            if ((result.Status.Succeeded() || result.Status.Flag.HasBestEffort()) && result.Status.Flag.HasSuggestion())
            {
                Assert.Equal(namedSpans["r"].Single(), result.FinalSpan);
            }
        }

        protected async Task IterateAllAsync(string code)
        {
            using var workspace = TestWorkspace.CreateCSharp(code, CodeAnalysis.CSharp.Test.Utilities.TestOptions.Regular);
            var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
            Assert.NotNull(document);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var root = await document.GetSyntaxRootAsync();
            var iterator = root.DescendantNodesAndSelf().Cast<SyntaxNode>();

            var originalOptions = await document.GetOptionsAsync();

            foreach (var node in iterator)
            {
                var validator = new CSharpSelectionValidator(semanticDocument, node.Span, originalOptions);
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
