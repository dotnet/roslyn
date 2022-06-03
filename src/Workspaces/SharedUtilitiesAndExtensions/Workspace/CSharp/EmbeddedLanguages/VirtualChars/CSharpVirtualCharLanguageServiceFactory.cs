// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars
{
    [ExportLanguageServiceFactory(typeof(IVirtualCharLanguageService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpVirtualCharLanguageServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpVirtualCharLanguageServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => CSharpVirtualCharLanguageService.Instance;

        private sealed class CSharpVirtualCharLanguageService : CSharpVirtualCharService, IVirtualCharLanguageService
        {
            internal static new readonly CSharpVirtualCharLanguageService Instance = new();

            private CSharpVirtualCharLanguageService()
            {
            }
        }
    }
}
