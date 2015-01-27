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
    internal class WhereKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public WhereKeywordRecommender()
            : base(SyntaxKind.WhereKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsQueryContext(context) ||
                IsTypeParameterConstraintContext(context);
        }

        private bool IsTypeParameterConstraintContext(CSharpSyntaxContext context)
        {
            // cases:
            //   class C<T> |
            //   class C<T> : IFoo |
            //   class C<T> where T : IFoo |
            //   delegate void D<T> |
            //   delegate void D<T> where T : IFoo |
            //   void Foo<T>() |
            //   void Foo<T>() where T : IFoo |

            var token = context.TargetToken;

            // class C<T> |

            if (token.Kind() == SyntaxKind.GreaterThanToken)
            {
                var typeParameters = token.GetAncestor<TypeParameterListSyntax>();
                if (typeParameters != null && token == typeParameters.GetLastToken(includeSkipped: true))
                {
                    var decl = typeParameters.GetAncestorOrThis<TypeDeclarationSyntax>();
                    if (decl != null && decl.TypeParameterList == typeParameters)
                    {
                        return true;
                    }
                }
            }

            // delegate void D<T>() |
            if (token.Kind() == SyntaxKind.CloseParenToken &&
                token.Parent.IsKind(SyntaxKind.ParameterList) &&
                token.Parent.IsParentKind(SyntaxKind.DelegateDeclaration))
            {
                var decl = token.GetAncestor<DelegateDeclarationSyntax>();
                if (decl != null && decl.TypeParameterList != null)
                {
                    return true;
                }
            }

            // void Foo<T>() |

            if (token.Kind() == SyntaxKind.CloseParenToken &&
                token.Parent.IsKind(SyntaxKind.ParameterList) &&
                token.Parent.IsParentKind(SyntaxKind.MethodDeclaration))
            {
                var decl = token.GetAncestor<MethodDeclarationSyntax>();
                if (decl != null && decl.Arity > 0)
                {
                    return true;
                }
            }

            // class C<T> : IFoo |
            var baseList = token.GetAncestor<BaseListSyntax>();
            if (baseList.GetParent() is TypeDeclarationSyntax)
            {
                var typeDecl = baseList.GetParent() as TypeDeclarationSyntax;
                if (typeDecl.TypeParameterList != null &&
                    typeDecl.BaseList.Types.Any(t => token == t.GetLastToken(includeSkipped: true)))
                {
                    // token is IdentifierName "where"
                    // only suggest "where" if token's previous token is also "where"
                    if (token.Parent is IdentifierNameSyntax && token.HasMatchingText(SyntaxKind.WhereKeyword))
                    {
                        // Check for zero-width tokens in case there is a missing comma in the base list.
                        // For example: class C<T> : Foo where where |
                        return token
                            .GetPreviousToken(includeZeroWidth: true)
                            .IsKindOrHasMatchingText(SyntaxKind.WhereKeyword);
                    }

                    // System.|
                    // Not done typing the qualified name
                    if (token.IsKind(SyntaxKind.DotToken))
                    {
                        return false;
                    }

                    return true;
                }
            }

            // class C<T> where T : IFoo |
            // delegate void D<T> where T : IFoo |
            var constraintClause = token.GetAncestor<TypeParameterConstraintClauseSyntax>();

            if (constraintClause != null)
            {
                if (constraintClause.Constraints.Any(c => token == c.GetLastToken(includeSkipped: true)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsQueryContext(CSharpSyntaxContext context)
        {
            var token = context.TargetToken;

            // var q = from x in y
            //         |
            if (!token.IntersectsWith(context.Position) &&
                token.IsLastTokenOfQueryClause())
            {
                return true;
            }

            return false;
        }
    }
}
