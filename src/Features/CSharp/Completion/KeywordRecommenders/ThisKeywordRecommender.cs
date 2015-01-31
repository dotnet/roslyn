// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ThisKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ThisKeywordRecommender()
            : base(SyntaxKind.ThisKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsInstanceExpressionOrStatement(context) ||
                IsExtensionMethodParameterContext(context, cancellationToken) ||
                IsConstructorInitializerContext(context);
        }

        private static bool IsInstanceExpressionOrStatement(CSharpSyntaxContext context)
        {
            if (context.IsInstanceContext)
            {
                return context.IsNonAttributeExpressionContext || context.IsStatementContext;
            }

            return false;
        }

        private bool IsConstructorInitializerContext(CSharpSyntaxContext context)
        {
            // cases:
            //   Foo() : |

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.ColonToken &&
                token.Parent is ConstructorInitializerSyntax &&
                token.Parent.IsParentKind(SyntaxKind.ConstructorDeclaration))
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

        private static bool IsExtensionMethodParameterContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // TODO(cyrusn): lambda/anon methods can have out/ref parameters
            if (!context.SyntaxTree.IsParameterModifierContext(context.Position, context.LeftToken, cancellationToken, allowableIndex: 0))
            {
                return false;
            }

            var token = context.LeftToken;
            var method = token.GetAncestor<MethodDeclarationSyntax>();
            var typeDecl = method.GetAncestorOrThis<TypeDeclarationSyntax>();

            if (method == null || typeDecl == null)
            {
                return false;
            }

            if (typeDecl.Kind() != SyntaxKind.ClassDeclaration)
            {
                return false;
            }

            if (!method.Modifiers.Any(t => t.Kind() == SyntaxKind.StaticKeyword))
            {
                return false;
            }

            if (!typeDecl.Modifiers.Any(t => t.Kind() == SyntaxKind.StaticKeyword))
            {
                return false;
            }

            return true;
        }
    }
}
