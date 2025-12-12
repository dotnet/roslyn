// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests;

[ExportLanguageService(typeof(INoCompilationLanguageService), NoCompilationConstants.LanguageName, ServiceLayer.Test), Shared, PartNotDiscoverable]
internal sealed class NoCompilationLanguageService : INoCompilationLanguageService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NoCompilationLanguageService()
    {
    }
}

internal interface INoCompilationLanguageService : ILanguageService
{
}
