// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.SymbolMonikers;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using VS.IntelliNav.Contracts;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    [ExportWorkspaceService(typeof(IFindSymbolMonikerUsagesService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioFindSymbolMonikerUsagesService : AbstractFindSymbolMonikerUsagesService
    {
        private readonly ICodeIndexProvider? _codeIndexProvider;

        [ImportingConstructor]
        public VisualStudioFindSymbolMonikerUsagesService(
            [Import(AllowDefault = true)] ICodeIndexProvider? codeIndexProvider)
        {
            _codeIndexProvider = codeIndexProvider;
        }

        public override async Task<ImmutableArray<ExternalReferenceItem>> FindReferencesByMonikerAsync(
            DefinitionItem definition, ImmutableArray<SymbolMoniker> monikers,
            int page, CancellationToken cancellationToken)
        {
            if (_codeIndexProvider == null)
                return await base.FindReferencesByMonikerAsync(definition, monikers, page, cancellationToken).ConfigureAwait(false);

            var results = await _codeIndexProvider.FindReferencesByMonikerAsync(
                ConvertMonikers(monikers), includeDecleration: false, pageIndex: page, cancellationToken: cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<ExternalReferenceItem>.GetInstance(out var referenceItems);

            foreach (var result in results)
                referenceItems.Add(ConvertResult(definition, result));

            return referenceItems.ToImmutable();
        }

        private ExternalReferenceItem ConvertResult(DefinitionItem definition, string result)
        {
            // todo: shape looks like this:

            //{
            //    "uri": "file:///c:/src/test/MyProject/test.cs",
            //    "range": { "start": { "line": 0, "character": 4 }, "end": { "line": 0, "character": 11 } },
            //    "projectName": "MyProject",
            //    "displayPath": "test/MyProject/test.cs",
            //    "text" : "this is a line preview"
            //}

            throw new NotImplementedException();
        }

        private ImmutableArray<ISymbolMoniker> ConvertMonikers(ImmutableArray<SymbolMoniker> monikers)
            => monikers.SelectAsArray(ConvertMoniker);

        private ISymbolMoniker ConvertMoniker(SymbolMoniker moniker)
            => new MonikerWrapper(moniker);

        private class MonikerWrapper : ISymbolMoniker
        {
            private readonly SymbolMoniker _moniker;

            public MonikerWrapper(SymbolMoniker moniker)
                => _moniker = moniker;

            public string Scheme => _moniker.Scheme;

            public string Identifier => _moniker.Identifier;

            public IPackageInformation? PackageInformation => null;
        }
    }
}
