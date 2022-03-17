// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

[ExportLanguageService(typeof(IGlobalIdeOptionsProvider), LanguageNames.CSharp), Shared]
internal sealed class CSharpGlobalIdeOptionsProvider : IGlobalIdeOptionsProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpGlobalIdeOptionsProvider()
    {
    }

    public SimplifierOptions GetSimplifierOptions(IGlobalOptionService globalOptions)
        => globalOptions.GetCSharpSimplifierOptions();
}
