﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    [ExportLanguageServiceFactory(typeof(IDecompiledSourceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpDecompiledSourceServiceFactory : ILanguageServiceFactory
    {
        [ImportingConstructor]
        public CSharpDecompiledSourceServiceFactory()
        {
        }

        public ILanguageService CreateLanguageService(HostLanguageServices provider)
        {
            return new CSharpDecompiledSourceService(provider);
        }
    }
}
