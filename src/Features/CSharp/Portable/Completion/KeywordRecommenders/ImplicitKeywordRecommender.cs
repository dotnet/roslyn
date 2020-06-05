﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ImplicitKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.StaticKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.ExternKeyword,
                SyntaxKind.UnsafeKeyword,
            };

        public ImplicitKeywordRecommender()
            : base(SyntaxKind.ImplicitKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsMemberDeclarationContext(validModifiers: s_validMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassStructRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                // operators must be both public and static
                var modifiers = context.PrecedingModifiers;

                return
                    modifiers.Contains(SyntaxKind.PublicKeyword) &&
                    modifiers.Contains(SyntaxKind.StaticKeyword);
            }

            return false;
        }
    }
}
