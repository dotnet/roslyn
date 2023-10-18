// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal class CapabilityRegistrationsProvider : ICapabilityRegistrationsProvider
    {
        private readonly IEnumerable<Registration> _registrations;

        public CapabilityRegistrationsProvider(IEnumerable<Registration> registrations)
        {
            _registrations = registrations;
        }

        public ImmutableArray<Registration> GetRegistrations()
            => _registrations.ToImmutableArrayOrEmpty();
    }
}
