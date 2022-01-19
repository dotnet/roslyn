// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageServiceFactory(typeof(IFormattingService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptFormattingServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptFormattingServiceFactory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public ILanguageService? CreateLanguageService(HostLanguageServices languageServices)
        {
            var impl = languageServices.GetService<IVSTypeScriptFormattingServiceImplementation>();
            if (impl is null)
                return null;

            return new VSTypeScriptFormattingService(impl);
        }
    }
}
