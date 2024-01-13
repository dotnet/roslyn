// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    [UseExportProvider]
    public abstract class AbstractQuickInfoSourceTests
    {
        [System.Diagnostics.DebuggerStepThrough]
        protected static string ExpectedContent(params string[] expectedContent)
            => expectedContent.Join("\r\n");

        protected static string FormatCodeWithDocComments(params string[] code)
        {
            var formattedCode = code.Join("\r\n");
            return string.Concat(System.Environment.NewLine, formattedCode);
        }

        protected async Task TestInMethodAndScriptAsync(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            await TestInMethodAsync(code, expectedContent, expectedDocumentationComment);
            await TestInScriptAsync(code, expectedContent, expectedDocumentationComment);
        }

        protected abstract Task TestInClassAsync(string code, string expectedContent, string expectedDocumentationComment = null);

        protected abstract Task TestInMethodAsync(string code, string expectedContent, string expectedDocumentationComment = null);

        protected abstract Task TestInScriptAsync(string code, string expectedContent, string expectedDocumentationComment = null);

        protected abstract Task TestAsync(
            string code,
            string expectedContent,
            string expectedDocumentationComment = null,
            CSharpParseOptions parseOptions = null);

        protected abstract Task AssertNoContentAsync(
            EditorTestWorkspace workspace,
            Document document,
            int position);

        protected abstract Task AssertContentIsAsync(
            EditorTestWorkspace workspace,
            Document document,
            int position,
            string expectedContent,
            string expectedDocumentationComment = null);
    }
}
