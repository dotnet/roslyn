// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeCleanup;

[ExportLanguageServiceFactory(typeof(ICodeCleanerService), LanguageNames.CSharp), Shared]
internal class CSharpCodeCleanerServiceFactory : ILanguageServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeCleanerServiceFactory()
    {
    }

    public ILanguageService CreateLanguageService(HostLanguageServices provider)
        => new CSharpCodeCleanerService();
}
