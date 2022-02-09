// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Indentation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Wrapping.InitializerExpression;

namespace Microsoft.CodeAnalysis.CSharp.Wrapping.InitializerExpression
{
    internal partial class CSharpInitializerExpressionWrapper : AbstractInitializerExpressionWrapper<InitializerExpressionSyntax, ExpressionSyntax>
    {
        public CSharpInitializerExpressionWrapper() : base(CSharpIndentationService.Instance)
        {
        }

        protected override SeparatedSyntaxList<ExpressionSyntax> GetListItems(InitializerExpressionSyntax listSyntax)
            => listSyntax.Expressions;

        protected override InitializerExpressionSyntax? TryGetApplicableList(SyntaxNode node)
            => node as InitializerExpressionSyntax;

        protected override bool TryGetNewLinesForBracesInObjectCollectionArrayInitializersOption(DocumentOptionSet options)
            => options.GetOption(CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers);
    }
}
