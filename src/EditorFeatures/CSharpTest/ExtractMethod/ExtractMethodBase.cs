// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExtractMethod;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    public class ExtractMethodBase
    {
        protected async Task ExpectExtractMethodToFailAsync(string codeWithMarker, bool allowMovingDeclaration = true, bool dontPutOutOrRefOnStruct = true)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(codeWithMarker))
            {
                var testDocument = workspace.Documents.First();
                var textSpan = testDocument.SelectedSpans.Single();
                var treeAfterExtractMethod = await ExtractMethodAsync(workspace, testDocument, succeed: false, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);
            }
        }

        protected async Task ExpectExtractMethodToFailAsync(
            string codeWithMarker,
            string expected,
            bool allowMovingDeclaration = true,
            bool dontPutOutOrRefOnStruct = true,
            CSharpParseOptions parseOptions = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(codeWithMarker, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var subjectBuffer = testDocument.TextBuffer;

                var tree = await ExtractMethodAsync(workspace, testDocument, succeed: false, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);

                using (var edit = subjectBuffer.CreateEdit())
                {
                    edit.Replace(0, edit.Snapshot.Length, tree.ToFullString());
                    edit.Apply();
                }

                Assert.Equal(expected, subjectBuffer.CurrentSnapshot.GetText());
            }
        }

        protected async Task NotSupported_ExtractMethodAsync(string codeWithMarker)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(codeWithMarker))
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
            bool allowMovingDeclaration = true,
            bool dontPutOutOrRefOnStruct = true,
            CSharpParseOptions parseOptions = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(codeWithMarker, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var subjectBuffer = testDocument.TextBuffer;

                var tree = await ExtractMethodAsync(workspace, testDocument, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);

                using (var edit = subjectBuffer.CreateEdit())
                {
                    edit.Replace(0, edit.Snapshot.Length, tree.ToFullString());
                    edit.Apply();
                }

                if (temporaryFailing)
                {
                    Assert.NotEqual(expected, subjectBuffer.CurrentSnapshot.GetText());
                }
                else
                {
                    Assert.Equal(expected, subjectBuffer.CurrentSnapshot.GetText());
                }
            }
        }

        protected static async Task<SyntaxNode> ExtractMethodAsync(
            TestWorkspace workspace,
            TestHostDocument testDocument,
            bool succeed = true,
            bool allowMovingDeclaration = true,
            bool dontPutOutOrRefOnStruct = true)
        {
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            Assert.NotNull(document);

            var options = document.Project.Solution.Workspace.Options
                                  .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, allowMovingDeclaration)
                                  .WithChangedOption(ExtractMethodOptions.DontPutOutOrRefOnStruct, document.Project.Language, dontPutOutOrRefOnStruct);

            var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
            var validator = new CSharpSelectionValidator(semanticDocument, testDocument.SelectedSpans.Single(), options);

            var selectedCode = await validator.GetValidSelectionAsync(CancellationToken.None);
            if (!succeed && selectedCode.Status.FailedWithNoBestEffortSuggestion())
            {
                return null;
            }

            Assert.True(selectedCode.ContainsValidContext);

            // extract method
            var extractor = new CSharpMethodExtractor((CSharpSelectionResult)selectedCode);
            var result = await extractor.ExtractMethodAsync(CancellationToken.None);
            Assert.NotNull(result);
            Assert.Equal(succeed, result.Succeeded || result.SucceededWithSuggestion);

            return await result.Document.GetSyntaxRootAsync();
        }

        protected async Task TestSelectionAsync(string codeWithMarker, bool expectedFail = false, CSharpParseOptions parseOptions = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(codeWithMarker, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var namedSpans = testDocument.AnnotatedSpans;

                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
                Assert.NotNull(document);

                var options = document.Project.Solution.Workspace.Options
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, true);

                var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
                var validator = new CSharpSelectionValidator(semanticDocument, namedSpans["b"].Single(), options);
                var result = await validator.GetValidSelectionAsync(CancellationToken.None);

                Assert.True(expectedFail ? result.Status.Failed() : result.Status.Succeeded());

                if ((result.Status.Succeeded() || result.Status.Flag.HasBestEffort()) && result.Status.Flag.HasSuggestion())
                {
                    Assert.Equal(namedSpans["r"].Single(), result.FinalSpan);
                }
            }
        }

        protected async Task IterateAllAsync(string code)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(code, CodeAnalysis.CSharp.Test.Utilities.TestOptions.Regular))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                Assert.NotNull(document);

                var semanticDocument = await SemanticDocument.CreateAsync(document, CancellationToken.None);
                var root = await document.GetSyntaxRootAsync();
                var iterator = root.DescendantNodesAndSelf().Cast<SyntaxNode>();

                var options = document.Project.Solution.Workspace.Options
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, true);

                foreach (var node in iterator)
                {
                    try
                    {
                        var validator = new CSharpSelectionValidator(semanticDocument, node.Span, options);
                        var result = await validator.GetValidSelectionAsync(CancellationToken.None);

                        // check the obvious case
                        if (!(node is ExpressionSyntax) && !node.UnderValidContext())
                        {
                            Assert.True(result.Status.FailedWithNoBestEffortSuggestion());
                        }
                    }
                    catch (ArgumentException)
                    {
                        // catch and ignore unknown issue. currently control flow analysis engine doesn't support field initializer.
                    }
                }
            }
        }
    }
}
