// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeLens
{
    [UseExportProvider]
    public abstract class AbstractCodeLensTest
    {
        protected static async Task RunCountTest(XElement input, int cap = 0)
        {
            using var workspace = EditorTestWorkspace.Create(input);
            foreach (var annotatedDocument in workspace.Documents.Where(d => d.AnnotatedSpans.Any()))
            {
                var document = workspace.CurrentSolution.GetDocument(annotatedDocument.Id);
                var syntaxNode = await document.GetSyntaxRootAsync();
                foreach (var annotatedSpan in annotatedDocument.AnnotatedSpans)
                {
                    var isCapped = annotatedSpan.Key.StartsWith("capped");
                    var expected = int.Parse(annotatedSpan.Key[(isCapped ? 6 : 0)..]);

                    foreach (var span in annotatedSpan.Value)
                    {
                        var declarationSyntaxNode = syntaxNode.FindNode(span);
                        var result = await new CodeLensReferencesService().GetReferenceCountAsync(workspace.CurrentSolution, annotatedDocument.Id,
                            declarationSyntaxNode, cap, CancellationToken.None);
                        Assert.NotNull(result);
                        Assert.Equal(expected, result.Value.Count);
                        Assert.Equal(isCapped, result.Value.IsCapped);
                    }
                }
            }
        }

        protected static Task RunCountTest(string input, int cap = 0)
            => RunCountTest(XElement.Parse(input), cap);

        protected static async Task RunReferenceTest(XElement input)
        {
            using var workspace = EditorTestWorkspace.Create(input);
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
                        Assert.True(result.HasValue);
                        Assert.Equal(expected, result.Value.Length);
                    }
                }
            }
        }

        protected static Task RunReferenceTest(string input)
            => RunReferenceTest(XElement.Parse(input));

        protected static async Task RunMethodReferenceTest(XElement input)
        {
            using var workspace = EditorTestWorkspace.Create(input);
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
                        Assert.True(result.HasValue);
                        Assert.Equal(expected, result.Value.Length);
                    }
                }
            }
        }

        protected static Task RunMethodReferenceTest(string input)
            => RunMethodReferenceTest(XElement.Parse(input));

        protected static async Task RunFullyQualifiedNameTest(XElement input)
        {
            using var workspace = EditorTestWorkspace.Create(input);
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

        protected static Task RunFullyQualifiedNameTest(string input)
            => RunFullyQualifiedNameTest(XElement.Parse(input));
    }
}
