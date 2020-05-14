// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Fakes
{
    [Export(typeof(IStreamingFindUsagesPresenter))]
    [Shared]
    [PartNotDiscoverable]
    internal class StubStreamingFindUsagesPresenter : IStreamingFindUsagesPresenter
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StubStreamingFindUsagesPresenter()
        {
        }

        public virtual void ClearAll()
        {
        }

        public virtual FindUsagesContext StartSearch(string title, bool supportsReferences)
            => new SimpleFindUsagesContext(CancellationToken.None);

        public virtual FindUsagesContext StartSearchWithCustomColumns(string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
            => new SimpleFindUsagesContext(CancellationToken.None);
    }
}
