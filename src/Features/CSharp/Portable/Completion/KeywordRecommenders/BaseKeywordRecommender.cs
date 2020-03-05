﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class BaseKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public BaseKeywordRecommender()
            : base(SyntaxKind.BaseKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // We need to at least be in a type declaration context.  This prevents us from showing
            // calls to 'base' in things like top level repl statements and whatnot.
            if (context.ContainingTypeDeclaration != null)
            {
                return
                    IsConstructorInitializerContext(position, context) ||
                    IsInstanceExpressionOrStatement(context);
            }

            return false;
        }

        private static bool IsInstanceExpressionOrStatement(CSharpSyntaxContext context)
        {
            if (context.IsInstanceContext)
            {
                return context.IsNonAttributeExpressionContext || context.IsStatementContext;
            }

            return false;
        }

        private bool IsConstructorInitializerContext(int position, CSharpSyntaxContext context)
        {
            // cases:
            //   Goo() : |

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.ColonToken &&
                token.Parent is ConstructorInitializerSyntax &&
                token.Parent.IsParentKind(SyntaxKind.ConstructorDeclaration) &&
                token.Parent.Parent.IsParentKind(SyntaxKind.ClassDeclaration))
            {
                var constructor = token.GetAncestor<ConstructorDeclarationSyntax>();
                if (constructor.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
