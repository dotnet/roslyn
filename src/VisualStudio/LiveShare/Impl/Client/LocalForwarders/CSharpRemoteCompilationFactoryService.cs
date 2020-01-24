// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.LocalForwarders
{
    [ExportLanguageServiceFactory(typeof(ICompilationFactoryService), StringConstants.CSharpLspLanguageName), Shared]
    internal class CSharpRemoteCompilationFactoryService : ILanguageServiceFactory
    {
        public ILanguageService? CreateLanguageService(HostLanguageServices languageServices)
        {
            // Don't allow the remote workspace to create C# compilations; since we don't have references, any attempt to use semantics in the workspace
            // on the client side is incorrect.
            return null;
        }
    }
}
