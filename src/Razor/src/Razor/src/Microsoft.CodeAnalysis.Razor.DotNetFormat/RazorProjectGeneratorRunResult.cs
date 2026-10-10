// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.NET.Sdk.Razor.SourceGenerators;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

// A slimmed-down view of GeneratorRunResult exposing the Razor and generated C# documents needed for formatting.
internal sealed class RazorProjectGeneratorRunResult(RazorGeneratorResult generatorResult, Project project)
{
    private const string RazorSourceGeneratorTypeName = "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator";

    public RazorCodeDocument GetRequiredCodeDocument(string filePath)
        => generatorResult.GetCodeDocument(filePath)
           ?? throw new InvalidOperationException($"The Razor source generator did not produce a code document for '{filePath}'.");

    public async Task<SourceGeneratedDocument> GetRequiredSourceGeneratedDocumentAsync(
        string filePath,
        bool declarationDocument,
        CancellationToken cancellationToken)
    {
        var implementationHintName = generatorResult.GetHintName(filePath)
            ?? throw new InvalidOperationException($"The Razor source generator did not produce a hint name for '{filePath}'.");
        var hintName = declarationDocument
            ? RazorSourceGenerator.GetDeclIdentifierFromHintName(implementationHintName)
            : implementationHintName;

        var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
        return generatedDocuments.SingleOrDefault(document => document.HintName == hintName)
            ?? throw new InvalidOperationException($"The Razor source generator did not produce generated document '{hintName}'.");
    }

    public static async Task<RazorProjectGeneratorRunResult?> TryCreateAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var result = await project.GetSourceGeneratorRunResultAsync(cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return null;
        }

        var expectedAssembly = typeof(RazorSourceGenerator).Assembly;
        // Compare Assembly objects before reading the typed host output so an older or separately loaded Razor generator
        // produces an actionable identity error instead of a misleading cast failure.
        var runResult = result.Results.FirstOrDefault(r => r.Generator.GetGeneratorType().Assembly == expectedAssembly);
        if (runResult.Generator is null)
        {
            var incompatibleGenerator = result.Results.FirstOrDefault(
                r => r.Generator.GetGeneratorType().FullName == RazorSourceGeneratorTypeName);

            if (incompatibleGenerator.Generator is not null)
            {
                var actualAssembly = incompatibleGenerator.Generator.GetGeneratorType().Assembly;
                throw new InvalidOperationException(
                    $"Project '{project.Name}' loaded an incompatible Razor source generator from {GetAssemblyDisplay(actualAssembly)}. " +
                    $"dotnet format requires the Razor compiler at {GetAssemblyDisplay(expectedAssembly)}.");
            }

            return null;
        }

#pragma warning disable RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        if (!runResult.HostOutputs.TryGetValue(nameof(RazorGeneratorResult), out var hostOutput))
#pragma warning restore RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            throw new InvalidOperationException(
                $"The Razor source generator for project '{project.Name}' did not produce its formatting host output. " +
                string.Join(Environment.NewLine, runResult.Diagnostics));
        }

        if (hostOutput is not RazorGeneratorResult generatorResult)
        {
            throw new InvalidOperationException(
                $"The Razor source generator for project '{project.Name}' produced an incompatible formatting host output. " +
                string.Join(Environment.NewLine, runResult.Diagnostics));
        }

        return new(generatorResult, project);
    }

    private static string GetAssemblyDisplay(Assembly assembly)
    {
        var location = assembly.IsDynamic ? "<dynamic assembly>" : assembly.Location;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return $"'{location}' (version '{assembly.GetName().Version}', informational version '{informationalVersion ?? "<unknown>"}')";
    }
}
