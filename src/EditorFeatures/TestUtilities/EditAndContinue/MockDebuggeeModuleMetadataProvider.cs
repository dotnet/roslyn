// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

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

        public Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
        {
            if (IsEditAndContinueAvailable != null)
            {
                return Task.FromResult(IsEditAndContinueAvailable(mvid));
            }

            if (LoadedModules != null)
            {
                return Task.FromResult(LoadedModules.TryGetValue(mvid, out var result) ? result : null);
            }

            throw new NotImplementedException();
        }

        public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
