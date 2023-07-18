// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UnitTests.Fakes
{
    [Export(typeof(IStreamingFindUsagesPresenter))]
    [Shared]
    [PartNotDiscoverable]
    internal sealed class StubStreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StubStreamingFindUsagesPresenter(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public void ClearAll()
        {
        }

        public (FindUsagesContext, CancellationToken) StartSearch(string title, bool supportsReferences)
            => (new SimpleFindUsagesContext(_globalOptions), CancellationToken.None);

        public (FindUsagesContext, CancellationToken) StartSearchWithCustomColumns(string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
            => (new SimpleFindUsagesContext(_globalOptions), CancellationToken.None);
    }
}
