// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal class MockDebuggeeModuleMetadataProvider : IDebuggeeModuleMetadataProvider
    {
        public Func<Guid, (int errorCode, string? errorMessage)?>? IsEditAndContinueAvailable;
        public Func<Guid, DebuggeeModuleInfo>? TryGetBaselineModuleInfo;

        public Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            => Task.FromResult((IsEditAndContinueAvailable ?? throw new NotImplementedException())(mvid));

        Task IDebuggeeModuleMetadataProvider.PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            => Task.CompletedTask;

        DebuggeeModuleInfo IDebuggeeModuleMetadataProvider.TryGetBaselineModuleInfo(Guid mvid)
            => (TryGetBaselineModuleInfo ?? throw new NotImplementedException())(mvid);
    }
}
