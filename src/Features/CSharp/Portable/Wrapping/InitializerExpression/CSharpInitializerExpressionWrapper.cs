// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
