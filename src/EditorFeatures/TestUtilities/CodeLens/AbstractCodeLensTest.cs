// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens
{
    [UseExportProvider]
    public abstract class AbstractCodeLensTest
    {
        protected static async Task RunCountTest(XElement input, int cap = 0)
        {
            using (var workspace = TestWorkspace.Create(input))
            {
                foreach (var annotatedDocument in workspace.Documents.Where(d => d.AnnotatedSpans.Any()))
                {
                    var document = workspace.CurrentSolution.GetDocument(annotatedDocument.Id);
                    var syntaxNode = await document.GetSyntaxRootAsync();
                    foreach (var annotatedSpan in annotatedDocument.AnnotatedSpans)
                    {
                        var isCapped = annotatedSpan.Key.StartsWith("capped");
                        var expected = int.Parse(annotatedSpan.Key.Substring(isCapped ? 6 : 0));

                        foreach (var span in annotatedSpan.Value)
                        {
                            var declarationSyntaxNode = syntaxNode.FindNode(span);
                            var result = await new CodeLensReferencesService().GetReferenceCountAsync(workspace.CurrentSolution, annotatedDocument.Id,
                                declarationSyntaxNode, cap, CancellationToken.None);
                            Assert.NotNull(result);
                            Assert.Equal(expected, result.Count);
                            Assert.Equal(isCapped, result.IsCapped);
                        }
                    }
                }
            }
        }

        protected static Task RunCountTest(string input, int cap = 0)
        {
            return RunCountTest(XElement.Parse(input), cap);
        }

        protected static async Task RunReferenceTest(XElement input)
        {
            using (var workspace = TestWorkspace.Create(input))
            {
                foreach (var annotatedDocument in workspace.Documents.Where(d => d.AnnotatedSpans.Any()))
                {
                    var document = workspace.CurrentSolution.GetDocument(annotatedDocument.Id);
                    var syntaxNode = await document.GetSyntaxRootAsync();
                    foreach (var annotatedSpan in annotatedDocument.AnnotatedSpans)
                    {
                        var expected = int.Parse(annotatedSpan.Key);

                        foreach (var span in annotatedSpan.Value)
                        {
                            var declarationSyntaxNode = syntaxNode.FindNode(span);
                            var result = await new CodeLensReferencesService().FindReferenceLocationsAsync(workspace.CurrentSolution,
                                annotatedDocument.Id, declarationSyntaxNode, CancellationToken.None);
                            var count = result.Count();
                            Assert.Equal(expected, count);
                        }
                    }
                }
            }
        }

        protected static Task RunReferenceTest(string input)
        {
            return RunReferenceTest(XElement.Parse(input));
        }

        protected static async Task RunMethodReferenceTest(XElement input)
        {
            using (var workspace = TestWorkspace.Create(input))
            {
                foreach (var annotatedDocument in workspace.Documents.Where(d => d.AnnotatedSpans.Any()))
                {
                    var document = workspace.CurrentSolution.GetDocument(annotatedDocument.Id);
                    var syntaxNode = await document.GetSyntaxRootAsync();
                    foreach (var annotatedSpan in annotatedDocument.AnnotatedSpans)
                    {
                        var expected = int.Parse(annotatedSpan.Key);

                        foreach (var span in annotatedSpan.Value)
                        {
                            var declarationSyntaxNode = syntaxNode.FindNode(span);
                            var result = await new CodeLensReferencesService().FindReferenceMethodsAsync(workspace.CurrentSolution,
                                annotatedDocument.Id, declarationSyntaxNode, CancellationToken.None);
                            var count = result.Count();
                            Assert.Equal(expected, count);
                        }
                    }
                }
            }
        }

        protected static Task RunMethodReferenceTest(string input)
        {
            return RunMethodReferenceTest(XElement.Parse(input));
        }

        protected static async Task RunFullyQualifiedNameTest(XElement input)
        {
            using (var workspace = TestWorkspace.Create(input))
            {
                foreach (var annotatedDocument in workspace.Documents.Where(d => d.AnnotatedSpans.Any()))
                {
                    var document = workspace.CurrentSolution.GetDocument(annotatedDocument.Id);
                    var syntaxNode = await document.GetSyntaxRootAsync();
                    foreach (var annotatedSpan in annotatedDocument.AnnotatedSpans)
                    {
                        var expected = annotatedSpan.Key;

                        foreach (var span in annotatedSpan.Value)
                        {
                            var declarationSyntaxNode = syntaxNode.FindNode(span);
                            var actual = await new CodeLensReferencesService().GetFullyQualifiedName(workspace.CurrentSolution,
                                annotatedDocument.Id, declarationSyntaxNode, CancellationToken.None);
                            Assert.Equal(expected, actual);
                        }
                    }
                }
            }
        }

        protected static Task RunFullyQualifiedNameTest(string input)
        {
            return RunFullyQualifiedNameTest(XElement.Parse(input));
        }
    }
}
