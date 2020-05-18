// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        protected static async Task RunCountTestAsync(XElement input, int cap = 0)
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

        protected static Task RunCountTestAsync(string input, int cap = 0)
            => RunCountTestAsync(XElement.Parse(input), cap);

        protected static async Task RunReferenceTestAsync(XElement input)
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

        protected static Task RunReferenceTestAsync(string input)
            => RunReferenceTestAsync(XElement.Parse(input));

        protected static async Task RunMethodReferenceTestAsync(XElement input)
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

        protected static Task RunMethodReferenceTestAsync(string input)
            => RunMethodReferenceTestAsync(XElement.Parse(input));

        protected static async Task RunFullyQualifiedNameTestAsync(XElement input)
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
                            var actual = await new CodeLensReferencesService().GetFullyQualifiedNameAsync(workspace.CurrentSolution,
                                annotatedDocument.Id, declarationSyntaxNode, CancellationToken.None);
                            Assert.Equal(expected, actual);
                        }
                    }
                }
            }
        }

        protected static Task RunFullyQualifiedNameTestAsync(string input)
            => RunFullyQualifiedNameTestAsync(XElement.Parse(input));
    }
}
