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
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(codeWithMarker))
            {
                var testDocument = workspace.Documents.First();
                var textSpan = testDocument.SelectedSpans.Single();
                var treeAfterExtractMethod = ExtractMethod(workspace, testDocument, succeed: false, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);
            }
        }

        protected async Task ExpectExtractMethodToFailAsync(
            string codeWithMarker,
            string expected,
            bool allowMovingDeclaration = true,
            bool dontPutOutOrRefOnStruct = true,
            CSharpParseOptions parseOptions = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(new[] { codeWithMarker }, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var subjectBuffer = testDocument.TextBuffer;

                var tree = ExtractMethod(workspace, testDocument, succeed: false, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);

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
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(codeWithMarker))
            {
                Assert.NotNull(Record.Exception(() =>
                {
                    var testDocument = workspace.Documents.Single();
                    var tree = ExtractMethod(workspace, testDocument);
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
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(new[] { codeWithMarker }, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var subjectBuffer = testDocument.TextBuffer;

                var tree = ExtractMethod(workspace, testDocument, allowMovingDeclaration: allowMovingDeclaration, dontPutOutOrRefOnStruct: dontPutOutOrRefOnStruct);

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

        protected static SyntaxNode ExtractMethod(
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

            var semanticDocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result;
            var validator = new CSharpSelectionValidator(semanticDocument, testDocument.SelectedSpans.Single(), options);

            var selectedCode = validator.GetValidSelectionAsync(CancellationToken.None).Result;
            if (!succeed && selectedCode.Status.FailedWithNoBestEffortSuggestion())
            {
                return null;
            }

            Assert.True(selectedCode.ContainsValidContext);

            // extract method
            var extractor = new CSharpMethodExtractor((CSharpSelectionResult)selectedCode);
            var result = extractor.ExtractMethodAsync(CancellationToken.None).Result;
            Assert.NotNull(result);
            Assert.Equal(succeed, result.Succeeded || result.SucceededWithSuggestion);

            return result.Document.GetSyntaxRootAsync().Result;
        }

        protected async Task TestSelectionAsync(string codeWithMarker, bool expectedFail = false, CSharpParseOptions parseOptions = null)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(new[] { codeWithMarker }, parseOptions: parseOptions))
            {
                var testDocument = workspace.Documents.Single();
                var namedSpans = testDocument.AnnotatedSpans;

                var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
                Assert.NotNull(document);

                var options = document.Project.Solution.Workspace.Options
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, true);

                var semanticDocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result;
                var validator = new CSharpSelectionValidator(semanticDocument, namedSpans["b"].Single(), options);
                var result = validator.GetValidSelectionAsync(CancellationToken.None).Result;

                Assert.True(expectedFail ? result.Status.Failed() : result.Status.Succeeded());

                if ((result.Status.Succeeded() || result.Status.Flag.HasBestEffort()) && result.Status.Flag.HasSuggestion())
                {
                    Assert.Equal(namedSpans["r"].Single(), result.FinalSpan);
                }
            }
        }

        protected async Task IterateAllAsync(string code)
        {
            using (var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromLinesAsync(new string[] { code }, CodeAnalysis.CSharp.Test.Utilities.TestOptions.Regular))
            {
                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                Assert.NotNull(document);

                var semanticDocument = SemanticDocument.CreateAsync(document, CancellationToken.None).Result;
                var tree = document.GetSyntaxTreeAsync().Result;
                var iterator = tree.GetRoot().DescendantNodesAndSelf().Cast<SyntaxNode>();

                var options = document.Project.Solution.Workspace.Options
                                      .WithChangedOption(ExtractMethodOptions.AllowMovingDeclaration, document.Project.Language, true);

                foreach (var node in iterator)
                {
                    try
                    {
                        var validator = new CSharpSelectionValidator(semanticDocument, node.Span, options);
                        var result = validator.GetValidSelectionAsync(CancellationToken.None).Result;

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
