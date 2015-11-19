// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.QuickInfo
{
    public abstract class AbstractQuickInfoSourceTests
    {
        [System.Diagnostics.DebuggerStepThrough]
        protected string ExpectedContent(params string[] expectedContent)
        {
            return expectedContent.Join("\r\n");
        }

        protected string FormatCodeWithDocComments(params string[] code)
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

        protected abstract void AssertNoContent(
            TestWorkspace workspace,
            Document document,
            int position);

        protected abstract void AssertContentIs(
            TestWorkspace workspace,
            Document document,
            int position,
            string expectedContent,
            string expectedDocumentationComment = null);
    }
}
