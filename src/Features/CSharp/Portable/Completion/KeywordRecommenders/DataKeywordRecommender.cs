// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class DataKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public DataKeywordRecommender()
            : base(SyntaxKind.DataKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                IsValidContextForType(context, cancellationToken);
        }

        private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsTypeDeclarationContext(
                    validModifiers: SyntaxKindSet.AllTypeModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken))
            {
                return CheckPreviousModifiers(context);
            }

            return false;

            static bool CheckPreviousModifiers(CSharpSyntaxContext context)
            {
                var precedingModifiers = context.PrecedingModifiers;
                return !precedingModifiers.Contains(SyntaxKind.DataKeyword);
            }
        }
    }
}
