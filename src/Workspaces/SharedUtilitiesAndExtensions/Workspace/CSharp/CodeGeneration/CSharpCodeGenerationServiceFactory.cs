// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration;

[ExportLanguageServiceFactory(typeof(ICodeGenerationService), LanguageNames.CSharp), Shared]
internal partial class CSharpCodeGenerationServiceFactory : ILanguageServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeGenerationServiceFactory()
    {
    }

    public ILanguageService CreateLanguageService(HostLanguageServices provider)
        => new CSharpCodeGenerationService(provider.LanguageServices);
}
