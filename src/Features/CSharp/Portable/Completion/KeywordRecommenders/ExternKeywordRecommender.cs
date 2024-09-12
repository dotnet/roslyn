// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

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

    private static readonly ISet<SyntaxKind> s_validLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword
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
            (context.IsGlobalStatementContext && syntaxTree.IsScript()) ||
            syntaxTree.IsGlobalMemberDeclarationContext(position, s_validGlobalModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: s_validModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken) ||
            context.SyntaxTree.IsLocalFunctionDeclarationContext(position, s_validLocalFunctionModifiers, cancellationToken);
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

        // namespace N;
        // |
        if (token.Kind() == SyntaxKind.SemicolonToken &&
            token.Parent.IsKind(SyntaxKind.FileScopedNamespaceDeclaration))
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
