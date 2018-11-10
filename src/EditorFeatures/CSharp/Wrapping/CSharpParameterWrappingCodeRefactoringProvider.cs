// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Editor.Wrapping
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpParameterWrappingCodeRefactoringProvider 
        : AbstractCSharpWrappingCodeRefactoringProvider<BaseParameterListSyntax, ParameterSyntax>
    {
        protected override string ListName => FeaturesResources.parameter_list;
        protected override string ItemNamePlural => FeaturesResources.parameters;
        protected override string ItemNameSingular => FeaturesResources.parameter;

        protected override SeparatedSyntaxList<ParameterSyntax> GetListItems(BaseParameterListSyntax listSyntax)
            => listSyntax.Parameters;

        protected override BaseParameterListSyntax GetApplicableList(SyntaxNode node)
            => CSharpSyntaxGenerator.GetParameterList(node);

        protected override bool PositionIsApplicable(
            SyntaxNode root, int position, SyntaxNode declaration, BaseParameterListSyntax listSyntax)
        {
            // CSharpSyntaxGenerator.GetParameterList synthesizes a parameter list for simple-lambdas.
            // In that case, we're not applicable in that list.
            if (declaration.Kind() == SyntaxKind.SimpleLambdaExpression)
            {
                return false;
            }

            var generator = CSharpSyntaxGenerator.Instance;
            var attributes = generator.GetAttributes(declaration);

            // We want to offer this feature in the header of the member.  For now, we consider
            // the header to be the part after the attributes, to the end of the parameter list.
            var firstToken = attributes?.Count > 0
                ? attributes.Last().GetLastToken().GetNextToken()
                : declaration.GetFirstToken();

            var lastToken = listSyntax.GetLastToken();

            var headerSpan = TextSpan.FromBounds(firstToken.SpanStart, lastToken.Span.End);
            return headerSpan.IntersectsWith(position);
        }
    }
}
