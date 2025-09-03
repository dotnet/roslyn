// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteExportProviderBuilder : ExportProviderBuilder
{
    internal static readonly ImmutableArray<string> RemoteHostAssemblyNames =
        MefHostServices.DefaultAssemblyNames
            .Add("Microsoft.CodeAnalysis.Remote.ServiceHub")
            .Add("Microsoft.CodeAnalysis.Remote.Workspaces")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.AspNetCore")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.Razor.Features")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.Extensions")
            .Add("Microsoft.CodeAnalysis.ExternalAccess.Copilot")
            .Add("Microsoft.VisualStudio.Copilot.Roslyn.SemanticSearch");

    private static ExportProvider? s_instance;
    internal static ExportProvider ExportProvider
        => s_instance ?? throw new InvalidOperationException($"Default export provider not initialized. Call {nameof(InitializeAsync)} first.");

    private StringBuilder? _errorMessages;
    private readonly TraceSource _traceLogger;

    private RemoteExportProviderBuilder(
        ImmutableArray<string> assemblyPaths,
        Resolver resolver,
        string cacheDirectory,
        string catalogPrefix,
        TraceSource traceLogger)
        : base(assemblyPaths, resolver, cacheDirectory, catalogPrefix)
    {
        _traceLogger = traceLogger;
    }

    public static async Task<string?> InitializeAsync(string localSettingsDirectory, TraceSource traceLogger, CancellationToken cancellationToken)
    {
        var assemblyPaths = ImmutableArray.CreateBuilder<string>(initialCapacity: RemoteHostAssemblyNames.Length);

        foreach (var assemblyName in RemoteHostAssemblyNames)
        {
            var assemblyPath = MefHostServicesHelpers.TryFindNearbyAssemblyLocation(assemblyName);
            if (assemblyPath != null)
            {
                assemblyPaths.Add(assemblyPath);
                traceLogger.TraceInformation($"Located {assemblyPath} for the MEF composition.");
            }
            else
            {
                traceLogger.TraceEvent(TraceEventType.Warning, 0, $"Could not find assembly '{assemblyName}' near '{typeof(MefHostServicesHelpers).Assembly.Location}'");
            }
        }

        var builder = new RemoteExportProviderBuilder(
            assemblyPaths: assemblyPaths.ToImmutable(),
            resolver: new Resolver(SimpleAssemblyLoader.Instance),
            cacheDirectory: Path.Combine(localSettingsDirectory, "Roslyn", "RemoteHost", "Cache"),
            catalogPrefix: "RoslynRemoteHost",
            traceLogger);

        s_instance = await builder.CreateExportProviderAsync(cancellationToken).ConfigureAwait(false);

        return builder._errorMessages?.ToString();
    }

    protected override void LogError(string message, Exception exception)
    {
        // We'll log just the message to _errorMessages (since that gets displayed to the user in a gold bar), but we'll log the
        // full exception to the logger.
        _errorMessages ??= new StringBuilder();
        _errorMessages.AppendLine($"{message}: {exception.Message}");
        _traceLogger.TraceEvent(TraceEventType.Error, 0, $"{message}: {exception}");
    }

    protected override void LogTrace(string message)
    {
        _traceLogger.TraceEvent(TraceEventType.Information, 0, message);
    }

    protected override bool ContainsUnexpectedErrors(IEnumerable<string> erroredParts)
    {
        // Verify that we have exactly the MEF errors that we expect.  If we have less or more this needs to be updated to assert the expected behavior.
        var expectedErrorPartsSet = new HashSet<string>(["PythiaSignatureHelpProvider", "VSTypeScriptAnalyzerService", "CodeFixService", "CSharpMapCodeService", "CopilotSemanticSearchQueryExecutor"]);
        return erroredParts.Any(part => !expectedErrorPartsSet.Contains(part));
    }

    private sealed class SimpleAssemblyLoader : IAssemblyLoader
    {
        public static readonly IAssemblyLoader Instance = new SimpleAssemblyLoader();

        public Assembly LoadAssembly(AssemblyName assemblyName)
            => Assembly.Load(assemblyName);

        public Assembly LoadAssembly(string assemblyFullName, string? codeBasePath)
        {
            var assemblyName = new AssemblyName(assemblyFullName);
            if (!string.IsNullOrEmpty(codeBasePath))
            {
                // Set the codebase path, if known, as a hint for the assembly loader.
#pragma warning disable SYSLIB0044 // https://github.com/dotnet/roslyn/issues/71510
                assemblyName.CodeBase = codeBasePath;
#pragma warning restore SYSLIB0044
            }

            return LoadAssembly(assemblyName);
        }
    }
}
