// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Text;
using Roslyn.UnitTestFramework;
using Xunit;

namespace Roslyn.UnitTestFramework
{
    public abstract class CompletionProviderTestFixture
    {
        private readonly string language;

        protected CompletionProviderTestFixture(string language)
        {
            this.language = language;
        }

        protected abstract ICompletionProvider CreateProvider();

        protected void VerifyCompletion(string markup)
        {
            var group = GetCompletionGroup(markup);
            Assert.True(group.Items.Any());
        }

        protected void VerifyNoCompletion(string markup)
        {
            var group = GetCompletionGroup(markup);
            Assert.True(group == null || !group.Items.Any());
        }

        protected void VerifyCompletionContains(string itemDisplayText, string markup)
        {
            var group = GetCompletionGroup(markup);
            Assert.True(group.Items.Any(item => item.DisplayText == itemDisplayText));
        }

        protected void VerifyCompletionDoesNotContain(string itemDisplayText, string markup)
        {
            var group = GetCompletionGroup(markup);
            Assert.False(group.Items.Any(item => item.DisplayText == itemDisplayText));
        }

        private CompletionItemGroup GetCompletionGroup(string markup)
        {
            var provider = CreateProvider();
            string code;
            int cursorPosition;
            MarkupTestFile.GetPosition(markup, out code, out cursorPosition);
            var document = CreateDocument(code);
            var triggerInfo = CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo();
            return provider.GetGroupAsync(document, cursorPosition, triggerInfo, CancellationToken.None).Result;
        }

        private Document CreateDocument(string code)
        {
            var docName = "Test." + this.language == LanguageNames.CSharp ? "cs" : "vb";
            var solutionId = SolutionId.CreateNewId("TestSolution");
            var projectId = ProjectId.CreateNewId(debugName: "TestProject");
            var documentId = DocumentId.CreateNewId(projectId, debugName: docName);

            var solution = Solution
                .Create(solutionId)
                .AddProject(projectId, "TestProject", "TestProject", this.language)
                .AddMetadataReference(projectId, MetadataReference.CreateAssemblyReference("mscorlib"))
                .AddMetadataReference(projectId, MetadataReference.CreateAssemblyReference("System.Core"));

            if (this.language == LanguageNames.VisualBasic)
            {
                solution = solution.AddMetadataReference(projectId, MetadataReference.CreateAssemblyReference("Microsoft.VisualBasic"));
            }

            var document = solution
                .AddDocument(documentId, docName, SourceText.From(code))
                .GetDocument(documentId);
            var diags = document.GetSemanticModelAsync().Result.GetDiagnostics().ToArray();

            foreach (var diag in diags)
            {
                Console.WriteLine(diag.ToString());
            }

            return document;
        }
    }
}
#endif