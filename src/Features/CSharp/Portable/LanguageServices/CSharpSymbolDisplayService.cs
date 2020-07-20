// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Editor.CSharp.LanguageServices
{
    internal partial class CSharpSymbolDisplayService : AbstractSymbolDisplayService
    {
        public CSharpSymbolDisplayService(HostLanguageServices provider)
            : base(provider.GetService<IAnonymousTypeDisplayService>())
        {
        }

        protected override AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
            => new SymbolDescriptionBuilder(semanticModel, position, workspace, AnonymousTypeDisplayService, cancellationToken);
    }
}
