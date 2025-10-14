// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed partial class BuildHostProjectFileInfoLoaderFactory : IProjectFileInfoLoaderFactory
{
    public static readonly IProjectFileInfoLoaderFactory Instance = new BuildHostProjectFileInfoLoaderFactory();

    private BuildHostProjectFileInfoLoaderFactory()
    {
    }

    public ProjectFileInfoLoader Create(
        ImmutableDictionary<string, string> properties,
        ProjectFileExtensionRegistry projectFileExtensionRegistry,
        ProjectLoadOperationRunner operationRunner,
        DiagnosticReporter diagnosticReporter,
        ILoggerFactory loggerFactory)
        => new Loader(properties, projectFileExtensionRegistry, operationRunner, diagnosticReporter, loggerFactory);
}
