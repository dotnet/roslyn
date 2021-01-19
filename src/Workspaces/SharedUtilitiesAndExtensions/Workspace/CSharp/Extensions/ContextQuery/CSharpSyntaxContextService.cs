// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    [ExportLanguageService(typeof(ISyntaxContextService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxContextService : ISyntaxContextService
    {
        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxContextService()
        {
        }

        public Task<SyntaxContext> CreateContextAsync(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => Task.FromResult<SyntaxContext>(CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken));
    }
}
