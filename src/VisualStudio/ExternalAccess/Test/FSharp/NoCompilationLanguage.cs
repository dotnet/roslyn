// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

/// <summary>
/// A language Roslyn has no compilation for, as F# is: enough for a workspace to hold a project and its documents.
/// </summary>
internal static class NoCompilationLanguage
{
    public const string Name = "NoCompilation";

    public static HostServices CreateHostServices()
        => MefHostServices.Create([typeof(Workspace).Assembly, typeof(NoCompilationLanguageService).Assembly]);
}

internal interface INoCompilationLanguageService : ILanguageService
{
}

[ExportLanguageService(typeof(INoCompilationLanguageService), NoCompilationLanguage.Name), Shared]
internal sealed class NoCompilationLanguageService : INoCompilationLanguageService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public NoCompilationLanguageService()
    {
    }
}
