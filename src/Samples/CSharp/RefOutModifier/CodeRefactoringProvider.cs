// *********************************************************
//
// Copyright © Microsoft Corporation
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

using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Roslyn.Samples.CodeAction.AddOrRemoveRefOutModifier
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "AddOrRemoveRefOutModifier"), Shared]
    internal class AddOrRemoveRefOutModifierCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            // shouldn't have selection
            if (!textSpan.IsEmpty)
            {
                return;
            }

            // get applicable actions
            var finder = new ApplicableActionFinder(document, textSpan.Start, cancellationToken);
            var spanAndAction = await finder.GetSpanAndActionAsync().ConfigureAwait(false);
            if (spanAndAction == null || !spanAndAction.Item1.IntersectsWith(textSpan.Start))
            {
                return;
            }

            context.RegisterRefactoring(spanAndAction.Item2);
        }
    }
}