// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Samples.AddOrRemoveRefOutModifier
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(RefOutModifierCodeRefactoringProvider)), Shared]
    internal class RefOutModifierCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            Document document = context.Document;
            TextSpan textSpan = context.Span;
            CancellationToken cancellationToken = context.CancellationToken;

            // shouldn't have selection
            if (!textSpan.IsEmpty)
            {
                return;
            }

            // get applicable actions
            ApplicableActionFinder finder = new ApplicableActionFinder(document, textSpan.Start, cancellationToken);
            (TextSpan span, CodeAction action) = await finder.GetSpanAndActionAsync().ConfigureAwait(false);
            if (action == null || !span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            context.RegisterRefactoring(action);
        }
    }
}
