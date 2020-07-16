// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class PropertyKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public PropertyKeywordRecommender()
            : base(SyntaxKind.PropertyKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
            => context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, cancellationToken);
    }
}
