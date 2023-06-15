// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Export(typeof(IVsService<SVsSolution, IVsSolution2>))]
    [PartNotDiscoverable]
    internal class StubIVsService_SVsSolution : IVsService<SVsSolution, IVsSolution2>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StubIVsService_SVsSolution()
        {
        }

        public Task<IVsSolution2> GetValueAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IVsSolution2?> GetValueOrNullAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
