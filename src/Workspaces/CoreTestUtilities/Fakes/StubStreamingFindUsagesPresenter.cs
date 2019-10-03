// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return new SimpleFindUsagesContext(CancellationToken.None);
        }

        public virtual FindUsagesContext StartSearchWithCustomColumns(string title, bool supportsReferences, bool includeContainingTypeAndMemberColumns, bool includeKindColumn)
        {
            return new SimpleFindUsagesContext(CancellationToken.None);
        }
    }
}
