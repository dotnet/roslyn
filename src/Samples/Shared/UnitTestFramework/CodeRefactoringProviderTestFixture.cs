// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Roslyn.UnitTestFramework
{
    public abstract class CodeRefactoringProviderTestFixture : CodeActionProviderTestFixture
    {
        private IEnumerable<CodeAction> GetRefactoring(Document document, TextSpan span)
        {
            var provider = CreateCodeRefactoringProvider();
            var actions = new List<CodeAction>();
            var context = new CodeRefactoringContext(document, span, (a) => actions.Add(a), CancellationToken.None);
            provider.ComputeRefactoringsAsync(context).Wait();
            return actions;
        }

        protected void TestNoActions(string markup)
        {
            if (!markup.Contains('\r'))
            {
                markup = markup.Replace("\n", "\r\n");
            }

            string code;
            TextSpan span;
            MarkupTestFile.GetSpan(markup, out code, out span);

            var document = CreateDocument(code);
            var actions = GetRefactoring(document, span);

            Assert.True(actions == null || actions.Count() == 0);
        }

        protected void Test(
            string markup,
            string expected,
            int actionIndex = 0,
            bool compareTokens = false)
        {
            if (!markup.Contains('\r'))
            {
                markup = markup.Replace("\n", "\r\n");
            }

            if (!expected.Contains('\r'))
            {
                expected = expected.Replace("\n", "\r\n");
            }

            string code;
            TextSpan span;
            MarkupTestFile.GetSpan(markup, out code, out span);

            var document = CreateDocument(code);
            var actions = GetRefactoring(document, span);

            Assert.NotNull(actions);

            var action = actions.ElementAt(actionIndex);
            Assert.NotNull(action);

            var edit = action.GetOperationsAsync(CancellationToken.None).Result.OfType<ApplyChangesOperation>().First();
            VerifyDocument(expected, compareTokens, edit.ChangedSolution.GetDocument(document.Id));
        }

        protected abstract CodeRefactoringProvider CreateCodeRefactoringProvider();
    }
}
