// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.InvertConditional;

namespace Microsoft.CodeAnalysis.CSharp.InvertConditional
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal class CSharpInvertConditionalCodeRefactoringProvider
        : AbstractInvertConditionalCodeRefactoringProvider<ConditionalExpressionSyntax>
    {
        protected override bool ShouldOffer(ConditionalExpressionSyntax conditional, int position)
        {
            if (position > conditional.QuestionToken.Span.End)
            {
                return false;
            }

            if (conditional.ColonToken.IsMissing)
            {
                return false;
            }

            return true;
        }
    }
}
