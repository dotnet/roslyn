// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
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
