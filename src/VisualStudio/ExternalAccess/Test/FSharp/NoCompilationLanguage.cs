// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTests;

/// <summary>
/// A language Roslyn has no compilation for, which is what F# is to these tests. It is enough of a language
/// for a workspace to hold a project and its documents, and it keeps C# and Visual Basic out of tests that
/// have nothing to do with either.
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
    [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
    public NoCompilationLanguageService()
    {
    }
}
