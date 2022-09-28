// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(IGoToSymbolService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptGoToSymbolService : IGoToSymbolService
    {
        private readonly IVSTypeScriptGoToSymbolServiceImplementation _impl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptGoToSymbolService(IVSTypeScriptGoToSymbolServiceImplementation impl)
            => _impl = impl;

        public Task GetSymbolsAsync(GoToSymbolContext context)
            => _impl.GetSymbolsAsync(new VSTypeScriptGoToSymbolContext(context));
    }
}
