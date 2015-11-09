// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class MethodKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public MethodKeywordRecommender()
            : base(SyntaxKind.MethodKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructTypeDeclarations, cancellationToken))
            {
                return true;
            }

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.OpenBracketToken &&
                token.Parent.IsKind(SyntaxKind.AttributeList))
            {
                if (token.GetAncestor<PropertyDeclarationSyntax>() != null ||
                    token.GetAncestor<EventDeclarationSyntax>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
