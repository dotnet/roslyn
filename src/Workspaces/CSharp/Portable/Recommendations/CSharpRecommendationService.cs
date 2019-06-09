// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.CSharp.Recommendations
{
    [ExportLanguageService(typeof(IRecommendationService), LanguageNames.CSharp), Shared]
    internal class CSharpRecommendationService : AbstractRecommendationService<CSharpSyntaxContext>
    {
        [ImportingConstructor]
        public CSharpRecommendationService()
        {
        }

        protected override Task<CSharpSyntaxContext> CreateContext(
            Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => Task.FromResult(CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken));

        protected override AbstractRecommendationServiceRunner<CSharpSyntaxContext> CreateRunner(
            CSharpSyntaxContext context, bool filterOutOfScopeLocals, CancellationToken cancellationToken)
            => new CSharpRecommendationServiceRunner(context, filterOutOfScopeLocals, cancellationToken);
    }
}
