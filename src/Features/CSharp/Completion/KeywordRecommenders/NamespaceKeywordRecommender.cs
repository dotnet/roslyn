// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            if (syntaxTree.IsInteractiveOrScript())
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

            // using Foo;
            // |

            // using Foo;
            // n|

            // using Foo = Bar;
            // |

            // using Foo = Bar;
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

            // ns Foo { n|

            // extern alias a;
            // n|

            // using Foo;
            // n|

            // using Foo = Bar;
            // n|

            // a namespace can't come before usings/externs
            // a child namespace can't come before usings/externs
            if (leftToken.GetNextToken(includeSkipped: true).IsUsingOrExternKeyword())
            {
                return false;
            }

            // root: |
            if (token.Kind() == SyntaxKind.None)
            {
                // root namespace
                var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
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

            // using Foo;
            // |
            if (token.Kind() == SyntaxKind.SemicolonToken)
            {
                if (token.Parent.IsKind(SyntaxKind.ExternAliasDirective, SyntaxKind.UsingDirective))
                {
                    return true;
                }
            }

            // class C {}
            // |
            if (token.Kind() == SyntaxKind.CloseBraceToken)
            {
                if (token.Parent is TypeDeclarationSyntax &&
                    !(token.Parent.GetParent() is TypeDeclarationSyntax))
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
                    !(token.Parent.GetParent() is TypeDeclarationSyntax))
                {
                    return true;
                }
            }

            // [assembly: foo]
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
