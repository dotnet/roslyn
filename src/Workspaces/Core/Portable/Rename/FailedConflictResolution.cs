// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename
{
    internal struct FailedConflictResolution : IConflictResolution
    {
        public readonly string ErrorMessage;

        public FailedConflictResolution(string errorMessage)
            => ErrorMessage = errorMessage;

        public Task<SerializableConflictResolution> DehydrateAsync(CancellationToken cancellationToken)
            => Task.FromResult(new SerializableConflictResolution(ErrorMessage, resolution: null));
    }
}
