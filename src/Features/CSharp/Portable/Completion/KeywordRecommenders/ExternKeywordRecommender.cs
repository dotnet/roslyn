// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ExternKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VirtualKeyword,
            };

        private static readonly ISet<SyntaxKind> s_validGlobalModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
            };

        public ExternKeywordRecommender()
            : base(SyntaxKind.ExternKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                IsExternAliasContext(context) ||
                context.IsGlobalStatementContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, s_validGlobalModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsExternAliasContext(CSharpSyntaxContext context)
        {
            // cases:
            // root: |

            // root: e|

            // extern alias a;
            // |

            // extern alias a;
            // e|

            // all the above, but inside a namespace.
            // usings and other constructs *cannot* precede.

            var token = context.TargetToken;

            // root: |
            if (token.Kind() == SyntaxKind.None)
            {
                // root namespace
                return true;
            }

            if (token.Kind() == SyntaxKind.OpenBraceToken &&
                token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                return true;
            }

            // extern alias a;
            // |
            if (token.Kind() == SyntaxKind.SemicolonToken &&
                token.Parent.IsKind(SyntaxKind.ExternAliasDirective))
            {
                return true;
            }

            return false;
        }
    }
}
