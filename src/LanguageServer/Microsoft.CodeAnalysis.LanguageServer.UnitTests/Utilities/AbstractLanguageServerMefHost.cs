// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Xunit.Abstractions;
using Microsoft.CodeAnalysis.LanguageServer.Services;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Implementation of <see cref="AbstractLanguageServerHostTests"/> that uses the real MEF export provider and composition logic.
/// Only use this when testing components directly related to MEF composition and caching.  Otherwise prefer using <see cref="AbstractLanguageServerHostTests"/>
/// to benefit from the shared, cached test MEF composition (faster execution).
/// </summary>
public abstract class AbstractLanguageServerMefHost : AbstractLanguageServerHostTests
{
    protected readonly TempDirectory MefCacheDirectory;

    protected AbstractLanguageServerMefHost(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        MefCacheDirectory = TempRoot.CreateDirectory();
    }

    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
    {
        return LanguageServerTestComposition.CreateLanguageServerExportProviderAsync(serverConfiguration, loggerFactory, extensionManager, assemblyLoader, MefCacheDirectory.Path);
    }
}