// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    [Export(typeof(ILspWorkspaceRegistrationService)), Shared]
    internal class VisualStudioLspWorkspaceRegistrationService : ILspWorkspaceRegistrationService
    {
        private readonly object _gate = new();
        private ImmutableArray<Workspace> _registrations = ImmutableArray.Create<Workspace>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioLspWorkspaceRegistrationService()
        {
        }

        public ImmutableArray<Workspace> GetAllRegistrations() => _registrations;

        public void Register(Workspace workspace)
        {
            lock (_gate)
            {
                _registrations = _registrations.Add(workspace);
            }
        }

        public void Unregister(Workspace workspace)
        {
            lock (_gate)
            {
                _registrations = _registrations.Remove(workspace);
            }
        }
    }
}
