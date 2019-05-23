// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InvertConditional;

namespace Microsoft.CodeAnalysis.CSharp.InvertConditional
{
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.InvertConditional), Shared]
    internal class CSharpInvertConditionalCodeRefactoringProvider
        : AbstractInvertConditionalCodeRefactoringProvider<ConditionalExpressionSyntax>
    {
        [ImportingConstructor]
        public CSharpInvertConditionalCodeRefactoringProvider()
        {
        }

        // Show the feature in the condition of the conditional up through the ? token.
        // Don't offer if the conditional is missing the colon and the conditional is
        // too incomplete.
        protected override bool ShouldOffer(ConditionalExpressionSyntax conditional, int position)
            => position <= conditional.QuestionToken.Span.End &&
               !conditional.ColonToken.IsMissing;
    }
}
