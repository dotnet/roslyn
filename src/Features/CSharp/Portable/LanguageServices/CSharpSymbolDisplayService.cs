// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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

        public override ImmutableArray<SymbolDisplayPart> ToDisplayParts(ISymbol symbol, SymbolDisplayFormat format = null)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayParts(symbol, format);
        }

        public override ImmutableArray<SymbolDisplayPart> ToMinimalDisplayParts(SemanticModel semanticModel, int position, ISymbol symbol, SymbolDisplayFormat format)
        {
            return symbol.ToMinimalDisplayParts(semanticModel, position, format);
        }

        protected override AbstractSymbolDescriptionBuilder CreateDescriptionBuilder(Workspace workspace, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            return new SymbolDescriptionBuilder(this, semanticModel, position, workspace, this.AnonymousTypeDisplayService, cancellationToken);
        }
    }
}
