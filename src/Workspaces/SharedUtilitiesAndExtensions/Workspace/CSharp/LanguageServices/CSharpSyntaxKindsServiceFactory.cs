// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.LanguageService
{
    [ExportLanguageServiceFactory(typeof(ISyntaxKindsService), LanguageNames.CSharp), Shared]
    internal class CSharpSyntaxKindsServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSyntaxKindsServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices languageServices)
            => CSharpSyntaxKindsService.Instance;

        private sealed class CSharpSyntaxKindsService : CSharpSyntaxKinds, ISyntaxKindsService
        {
            public static new readonly CSharpSyntaxKindsService Instance = new();
        }
    }
}
