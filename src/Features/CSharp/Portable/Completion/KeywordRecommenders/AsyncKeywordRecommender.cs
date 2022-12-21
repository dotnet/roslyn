// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AsyncKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AsyncKeywordRecommender()
            : base(SyntaxKind.AsyncKeyword, isValidInPreprocessorContext: false)
        {
        }

        private static readonly ISet<SyntaxKind> s_validModifiers = SyntaxKindSet.AllMemberModifiers.Where(m => m is not (SyntaxKind.AsyncKeyword or SyntaxKind.RequiredKeyword)).ToSet();

        private static readonly ISet<SyntaxKind> s_validLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword
        };

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return InMemberDeclarationContext(position, context, cancellationToken)
                || context.SyntaxTree.IsLambdaDeclarationContext(position, otherModifier: SyntaxKind.StaticKeyword, cancellationToken)
                || context.SyntaxTree.IsLocalFunctionDeclarationContext(position, s_validLocalFunctionModifiers, cancellationToken);
        }

        private static bool InMemberDeclarationContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsGlobalStatementContext
                || context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken)
                || context.IsMemberDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }
    }
}
