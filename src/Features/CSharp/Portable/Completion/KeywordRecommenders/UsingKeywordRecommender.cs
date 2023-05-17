// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UsingKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public UsingKeywordRecommender()
            : base(SyntaxKind.UsingKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // cases:
            //  using (goo) { }
            //  using Goo;
            //  using Goo = Bar;
            //  await using (goo) { }
            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsUsingDirectiveContext(context, forGlobalKeyword: false, cancellationToken) ||
                context.IsAwaitStatementContext(position, cancellationToken);
        }

        internal static bool IsUsingDirectiveContext(CSharpSyntaxContext context, bool forGlobalKeyword, CancellationToken cancellationToken)
        {
            // cases:
            // root: |

            // root: u|

            // extern alias a;
            // |

            // extern alias a;
            // u|

            // using Goo;
            // |

            // using Goo;
            // u|

            // using Goo = Bar;
            // |

            // using Goo = Bar;
            // u|

            // t valid:
            // namespace N {}
            // |

            // namespace N {}
            // u|

            // class C {}
            // |

            // class C {}
            // u|

            // |
            // extern alias a;

            // u|
            // extern alias a;

            var originalToken = context.LeftToken;
            var token = context.TargetToken;

            // root: u|

            // namespace N { u|

            // namespace N; u|

            // extern alias a;
            // u|

            // using Goo;
            // u|

            // using Goo = Bar;
            // u|

            // root: |
            if (token.Kind() == SyntaxKind.None)
            {
                // root namespace

                return IsValidContextAtTheRoot(context, originalToken, cancellationToken);
            }

            if ((token.Kind() == SyntaxKind.OpenBraceToken && token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
                || (token.Kind() == SyntaxKind.SemicolonToken && token.Parent.IsKind(SyntaxKind.FileScopedNamespaceDeclaration)))
            {
                // a child using can't come before externs
                var nextToken = originalToken.GetNextToken(includeSkipped: true);
                if (nextToken.Kind() == SyntaxKind.ExternKeyword)
                {
                    return false;
                }

                return true;
            }

            // extern alias a;
            // |

            // using Goo;
            // |
            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                if (token.Parent is (kind: SyntaxKind.ExternAliasDirective or SyntaxKind.UsingDirective))
                {
                    return true;
                }
            }

            // extern alias a;
            // global |

            // global using Goo;
            // global |

            // global |
            if (!forGlobalKeyword)
            {
                var previousToken = token.GetPreviousToken(includeSkipped: true);
                if (previousToken.Kind() == SyntaxKind.None)
                {
                    // root namespace
                    if (token.Kind() == SyntaxKind.GlobalKeyword)
                    {
                        return true;
                    }
                    else if (token.Kind() == SyntaxKind.IdentifierToken && SyntaxFacts.GetContextualKeywordKind((string)token.Value!) == SyntaxKind.GlobalKeyword)
                    {
                        return IsValidContextAtTheRoot(context, originalToken, cancellationToken);
                    }
                }
                else if (previousToken.Kind() == SyntaxKind.SemicolonToken &&
                    previousToken.Parent is (kind: SyntaxKind.ExternAliasDirective or SyntaxKind.UsingDirective))
                {
                    if (token.Kind() == SyntaxKind.GlobalKeyword)
                    {
                        return true;
                    }
                    else if (token.Kind() == SyntaxKind.IdentifierToken && SyntaxFacts.GetContextualKeywordKind((string)token.Value!) == SyntaxKind.GlobalKeyword)
                    {
                        return true;
                    }
                }
            }

            return false;

            static bool IsValidContextAtTheRoot(CSharpSyntaxContext context, SyntaxToken originalToken, CancellationToken cancellationToken)
            {
                // a using can't come before externs
                var nextToken = originalToken.GetNextToken(includeSkipped: true);
                if (nextToken.Kind() == SyntaxKind.ExternKeyword ||
                    ((CompilationUnitSyntax)context.SyntaxTree.GetRoot(cancellationToken)).Externs.Count > 0)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
