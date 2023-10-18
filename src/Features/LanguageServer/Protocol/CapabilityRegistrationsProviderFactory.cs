// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(ICapabilityRegistrationsProvider)), Shared]
    internal class CapabilityRegistrationsProviderFactory : ILspServiceFactory
    {
        private readonly IEnumerable<Registration> _registrations;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CapabilityRegistrationsProviderFactory([ImportMany] IEnumerable<Registration> registrations)
        {
            _registrations = registrations;
        }

        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            return new CapabilityRegistrationsProvider(_registrations);
        }
    }
}
