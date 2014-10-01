// *********************************************************
//
// Copyright ? Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

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
            var context = new CodeRefactoringContext(document, span, CancellationToken.None);
            return provider.GetRefactoringsAsync(context).Result;
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
