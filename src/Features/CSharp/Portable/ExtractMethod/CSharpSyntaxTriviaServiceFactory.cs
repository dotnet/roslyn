// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

[ExportLanguageServiceFactory(typeof(ISyntaxTriviaService), LanguageNames.CSharp), Shared]
internal class CSharpSyntaxTriviaServiceFactory : ILanguageServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSyntaxTriviaServiceFactory()
    {
    }

    public ILanguageService CreateLanguageService(HostLanguageServices provider)
        => CSharpSyntaxTriviaService.Instance;
}
