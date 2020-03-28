﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class VirtualKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ExternKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.UnsafeKeyword,
        };

        public VirtualKeywordRecommender()
            : base(SyntaxKind.VirtualKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (!context.IsMemberDeclarationContext(
                validModifiers: s_validMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken))
            {
                return false;
            }

            var modifiers = context.PrecedingModifiers;
            return !modifiers.Contains(SyntaxKind.PrivateKeyword) || modifiers.Contains(SyntaxKind.ProtectedKeyword);
        }
    }
}
