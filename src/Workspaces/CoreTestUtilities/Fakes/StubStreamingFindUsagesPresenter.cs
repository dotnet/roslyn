// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Fakes
{
    [Export(typeof(IStreamingFindUsagesPresenter))]
    [Shared]
    [PartNotDiscoverable]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class StubStreamingFindUsagesPresenter() : IStreamingFindUsagesPresenter
    {
        public void ClearAll()
        {
        }

        public (FindUsagesContext, CancellationToken) StartSearch(string title, StreamingFindUsagesPresenterOptions options)
            => (new SimpleFindUsagesContext(), CancellationToken.None);
    }
}
