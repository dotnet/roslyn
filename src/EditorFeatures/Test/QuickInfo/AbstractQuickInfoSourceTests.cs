// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        protected void TestInClass(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            var codeInClass = "class C {" + code + "}";
            Test(codeInClass, expectedContent, expectedDocumentationComment);
        }

        protected void TestInMethod(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            var codeInClass = @"class C
{
    void M()
    {
        " + code + @"
    }
}";
            Test(codeInClass, expectedContent, expectedDocumentationComment);
        }

        protected void Test(string code, string expectedContent, string expectedDocumentationComment = null)
        {
            code = @"using System;
using System.Collections.Generic;
using System.Linq;
" + code;
            TestWithoutUsings(code, expectedContent, expectedDocumentationComment);
        }

        protected void TestWithoutUsings(string initialMarkup, string expectedContent, string expectedDocumentationComment = null)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(initialMarkup))
            {
                var testDocument = workspace.Documents.Single();
                var position = testDocument.CursorPosition.Value;
                var document = workspace.CurrentSolution.Projects.First().Documents.First();

                if (string.IsNullOrEmpty(expectedContent))
                {
                    AssertNoContent(workspace, document, position);
                }
                else
                {
                    AssertContentIs(workspace, document, position, expectedContent, expectedDocumentationComment);
                }
            }
        }

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
