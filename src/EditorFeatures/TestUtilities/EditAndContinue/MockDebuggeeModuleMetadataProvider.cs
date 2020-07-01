// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockDebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
    {
        public Func<Guid, (int errorCode, string? errorMessage)?>? IsEditAndContinueAvailable;
        public Dictionary<Guid, (int errorCode, string? errorMessage)?>? LoadedModules;

        public ValueTask<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            if (IsEditAndContinueAvailable != null)
            {
                return new(IsEditAndContinueAvailable(mvid));
            }

            if (LoadedModules != null)
            {
                return new(LoadedModules.TryGetValue(mvid, out var result) ? result : null);
            }

            throw new NotImplementedException();
        }

        public ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            => default;
    }
}
