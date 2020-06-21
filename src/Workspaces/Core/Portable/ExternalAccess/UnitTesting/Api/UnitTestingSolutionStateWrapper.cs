// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal readonly struct UnitTestingSolutionStateWrapper
    {
        internal SolutionState UnderlyingObject { get; }

        public UnitTestingSolutionStateWrapper(SolutionState underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        public async Task<UnitTestingChecksumWrapper> GetChecksumAsync(CancellationToken cancellationToken)
        {
            var checksum = await UnderlyingObject.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            return new UnitTestingChecksumWrapper(checksum);
        }
    }
}
