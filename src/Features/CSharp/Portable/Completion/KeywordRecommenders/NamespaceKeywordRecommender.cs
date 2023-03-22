// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class NamespaceKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public NamespaceKeywordRecommender()
            : base(SyntaxKind.NamespaceKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;

            // namespaces are illegal in interactive code:
            if (syntaxTree.IsScript())
            {
                return false;
            }

            // cases:
            // root: |

            // root: n|

            // extern alias a;
            // |

            // extern alias a;
            // n|

            // using Goo;
            // |

            // using Goo;
            // n|

            // using Goo = Bar;
            // |

            // using Goo = Bar;
            // n|

            // namespace N {}
            // |

            // namespace N {}
            // n|

            // class C {}
            // |

            // class C {}
            // n|

            var leftToken = context.LeftToken;
            var token = context.TargetToken;

            // root: n|

            // ns Goo { n|

            // extern alias a;
            // n|

            // using Goo;
            // n|

            // using Goo = Bar;
            // n|

            // a namespace can't come before usings/externs
            // a child namespace can't come before usings/externs
            var nextToken = leftToken.GetNextToken(includeSkipped: true);
            if (nextToken.IsUsingOrExternKeyword() ||
                (nextToken.Kind() == SyntaxKind.GlobalKeyword && nextToken.GetAncestor<UsingDirectiveSyntax>()?.GlobalKeyword == nextToken))
            {
                return false;
            }

            // root: |
            if (token.Kind() == SyntaxKind.None)
            {
                // root namespace
                var root = (CompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken);
                if (root.Externs.Count > 0 ||
                    root.Usings.Count > 0)
                {
                    return false;
                }

                return true;
            }

            if (token.Kind() == SyntaxKind.OpenBraceToken &&
                token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                return true;
            }

            // extern alias a;
            // |

            // using Goo;
            // |
            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                if (token.Parent is (kind: SyntaxKind.ExternAliasDirective or SyntaxKind.UsingDirective) &&
                    !token.Parent.Parent.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
                {
                    return true;
                }
            }

            // class C {}
            // |
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                if (token.Parent is TypeDeclarationSyntax &&
                    token.Parent.Parent is not TypeDeclarationSyntax)
                {
                    return true;
                }
                else if (token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    return true;
                }
            }

            // delegate void D();
            // |

            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                if (token.Parent.IsKind(SyntaxKind.DelegateDeclaration) &&
                    token.Parent.Parent is not TypeDeclarationSyntax)
                {
                    return true;
                }
            }

            // [assembly: goo]
            // |

            if (token.Kind() == SyntaxKind.CloseBracketToken &&
                token.Parent.IsKind(SyntaxKind.AttributeList) &&
                token.Parent.IsParentKind(SyntaxKind.CompilationUnit))
            {
                return true;
            }

            return false;
        }
    }
}
