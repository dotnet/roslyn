// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities.TextStructureNavigation
{
    public abstract class AbstractTextStructureNavigatorTests
    {
        protected abstract string ContentType { get; }
        protected abstract TestWorkspace CreateWorkspace(string code);

        protected StringBuilder result = new StringBuilder();

        protected void AssertExtent(string code, int pos)
        {
            using var workspace = CreateWorkspace(code);
            var document = workspace.Documents.First();
            var buffer = document.GetTextBuffer();

            var provider = workspace.GetService<ITextStructureNavigatorProvider>(this.ContentType);

            var navigator = provider.CreateTextStructureNavigator(buffer);

            var extent = navigator.GetExtentOfWord(new SnapshotPoint(buffer.CurrentSnapshot, pos));

            result.AppendLine("            AssertExtent(");
            result.Append("                @\"");

            var spanStart = extent.Span.Span.Start;
            var spanEnd = extent.Span.Span.End;

            result.Append(code[..spanStart].Replace("\"", "\"\""));

            if (pos == spanStart)
            {
                result.Append("{|");
                result.Append(extent.IsSignificant ? "Significant" : "Insignificant");
                result.Append(":$$");
                result.Append(code[spanStart..spanEnd].Replace("\"", "\"\""));
                result.Append("|}");
                result.Append(code[spanEnd..].Replace("\"", "\"\""));
            }
            else if (pos < spanStart)
            {
                result.Append("$$");
                result.Append(code[pos..spanStart].Replace("\"", "\"\""));
                result.Append("{|");
                result.Append(extent.IsSignificant ? "Significant" : "Insignificant");
                result.Append(":");
                result.Append(code[spanStart..spanEnd].Replace("\"", "\"\""));
                result.Append("|}");
                result.Append(code[spanEnd..].Replace("\"", "\"\""));
            }
            else if (pos < spanEnd)
            {
                result.Append("{|");
                result.Append(extent.IsSignificant ? "Significant" : "Insignificant");
                result.Append(":");
                result.Append(code[spanStart..pos].Replace("\"", "\"\""));
                result.Append("$$");
                result.Append(code[pos..spanEnd].Replace("\"", "\"\""));
                result.Append("|}");
                result.Append(code[spanEnd..].Replace("\"", "\"\""));
            }
            else
            {
                result.Append("{|");
                result.Append(extent.IsSignificant ? "Significant" : "Insignificant");
                result.Append(":");
                result.Append(code[spanStart..spanEnd].Replace("\"", "\"\""));
                result.Append("|}");
                result.Append(code[spanEnd..pos].Replace("\"", "\"\""));
                result.Append("$$");
                result.Append(code[spanEnd..].Replace("\"", "\"\""));
            }

            result.Append("\");");

            result.AppendLine();
            result.AppendLine();
        }

        protected void AssertExtent(string code)
        {
            using var workspace = CreateWorkspace(code);
            var document = workspace.Documents.First();
            var buffer = document.GetTextBuffer();

            var provider = workspace.GetService<ITextStructureNavigatorProvider>(this.ContentType);

            var navigator = provider.CreateTextStructureNavigator(buffer);

            var position = document.CursorPosition!.Value;
            var extent = navigator.GetExtentOfWord(new SnapshotPoint(buffer.CurrentSnapshot, position));

            var annotatedSpans = document.AnnotatedSpans;

            var (key, expectedSpans) = annotatedSpans.Single();
            Assert.Equal(expectedSpans.Single(), extent.Span.Span.ToTextSpan());

            if (extent.IsSignificant)
            {
                Assert.Equal("Significant", key);
            }
            else
            {
                Assert.Equal("Insignificant", key);
            }
        }
    }
}
