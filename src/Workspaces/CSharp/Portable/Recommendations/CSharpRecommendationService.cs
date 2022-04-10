// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    [ExportLanguageService(typeof(IRecommendationService), LanguageNames.CSharp), Shared]
    internal partial class CSharpRecommendationService : AbstractRecommendationService<CSharpSyntaxContext>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpRecommendationService()
        {
        }

        protected override CSharpSyntaxContext CreateContext(Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => CSharpSyntaxContext.CreateContext(document, semanticModel, position, cancellationToken);

        protected override AbstractRecommendationServiceRunner CreateRunner(CSharpSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken)
            => new CSharpRecommendationServiceRunner(context, filterOutOfScopeLocals, cancellationToken);
    }
}
