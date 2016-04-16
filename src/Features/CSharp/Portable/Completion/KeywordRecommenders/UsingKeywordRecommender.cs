// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            //  using (foo) { }
            //  using Foo;
            //  using Foo = Bar;
            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsUsingDirectiveContext(context, cancellationToken);
        }

        private static bool IsUsingDirectiveContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // cases:
            // root: |

            // root: u|

            // extern alias a;
            // |

            // extern alias a;
            // u|

            // using Foo;
            // |

            // using Foo;
            // u|

            // using Foo = Bar;
            // |

            // using Foo = Bar;
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

            // ns Foo { u|

            // extern alias a;
            // u|

            // using Foo;
            // u|

            // using Foo = Bar;
            // u|

            // root: |
            if (token.Kind() == SyntaxKind.None)
            {
                // root namespace

                // a using can't come before externs
                var nextToken = originalToken.GetNextToken(includeSkipped: true);
                if (nextToken.Kind() == SyntaxKind.ExternKeyword ||
                    ((CompilationUnitSyntax)context.SyntaxTree.GetRoot(cancellationToken)).Externs.Count > 0)
                {
                    return false;
                }

                return true;
            }

            if (token.Kind() == SyntaxKind.OpenBraceToken &&
                token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                var ns = (NamespaceDeclarationSyntax)token.Parent;

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

            // using Foo;
            // |
            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                if (token.Parent.IsKind(SyntaxKind.ExternAliasDirective) ||
                    token.Parent.IsKind(SyntaxKind.UsingDirective))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
