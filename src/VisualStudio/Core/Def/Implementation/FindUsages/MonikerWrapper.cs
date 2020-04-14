// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.LanguageServerIndexFormat;
using VS.IntelliNav.Contracts;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    internal partial class VisualStudioFindSymbolMonikerUsagesService
    {
        private class MonikerWrapper : ISymbolMoniker
        {
            private readonly SymbolMoniker _moniker;

            public MonikerWrapper(SymbolMoniker moniker)
                => _moniker = moniker;

            public string Scheme => _moniker.Scheme;

            public string Identifier => _moniker.Identifier;

            public IPackageInformation? PackageInformation => null;
        }
    }
}
