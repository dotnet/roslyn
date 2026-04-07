// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class WhereKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.WhereKeyword)
{
    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            IsQueryContext(context) ||
            IsTypeParameterConstraintContext(context);
    }

    private static bool IsTypeParameterConstraintContext(CSharpSyntaxContext context)
    {
        // cases:
        //   class C<T> |
        //   class C<T> : IGoo |
        //   class C<T> where T : IGoo |
        //   delegate void D<T> |
        //   delegate void D<T> where T : IGoo |
        //   void Goo<T>() |
        //   void Goo<T>() where T : IGoo |
        //   extension<T>(T value) |
        //   extension<T>(T value) where T : IGoo |

        var token = context.TargetToken;

        // class C<T> |

        if (token.Kind() == SyntaxKind.GreaterThanToken)
        {
            var typeParameters = token.GetAncestor<TypeParameterListSyntax>();
            if (typeParameters != null && token == typeParameters.GetLastToken(includeSkipped: true))
            {
                var decl = typeParameters.GetAncestorOrThis<TypeDeclarationSyntax>();

                // Don't offer 'where' after extension<T> | without a parameter list
                // It should only be offered after extension<T>(...) |
                if (decl is not ExtensionBlockDeclarationSyntax &&
                    decl?.TypeParameterList == typeParameters)
                {
                    return true;
                }
            }
        }

        // void Goo<T>() |
        // extension<T>(T value) |
        // delegate void D<T>() |

        if (token.Kind() == SyntaxKind.CloseParenToken &&
            token.Parent is ParameterListSyntax &&
            token.Parent.Parent is MethodDeclarationSyntax { TypeParameterList: not null }
                                or LocalFunctionStatementSyntax { TypeParameterList: not null }
                                or ExtensionBlockDeclarationSyntax { TypeParameterList: not null }
                                or DelegateDeclarationSyntax { TypeParameterList: not null })
        {
            return true;
        }

        // class C<T> : IGoo |
        var baseList = token.GetAncestor<BaseListSyntax>();
        if (baseList?.Parent is TypeDeclarationSyntax typeDecl)
        {
            if (typeDecl.TypeParameterList != null &&
                baseList.Types.Any(t => token == t.GetLastToken(includeSkipped: true)))
            {
                // token is IdentifierName "where"
                // only suggest "where" if token's previous token is also "where"
                if (token.Parent is IdentifierNameSyntax && token.HasMatchingText(SyntaxKind.WhereKeyword))
                {
                    // Check for zero-width tokens in case there is a missing comma in the base list.
                    // For example: class C<T> : Goo where where |
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

        // class C<T> where T : IGoo |
        // delegate void D<T> where T : IGoo |
        if (token.IsLastTokenOfNode<TypeParameterConstraintSyntax>())
        {
            return true;
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
