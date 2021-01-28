// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework.Services;

#pragma warning disable CS0067 // events that are never used

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks
{
    internal class AuthorizationServiceMock : IAuthorizationService
    {
        public event EventHandler? CredentialsChanged;

        public event EventHandler? AuthorizationChanged;

        internal bool Allow { get; set; } = true;

        public ValueTask<bool> CheckAuthorizationAsync(ProtectedOperation operation, CancellationToken cancellationToken = default)
        {
            return new ValueTask<bool>(this.Allow);
        }

        public ValueTask<IReadOnlyDictionary<string, string>> GetCredentialsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
