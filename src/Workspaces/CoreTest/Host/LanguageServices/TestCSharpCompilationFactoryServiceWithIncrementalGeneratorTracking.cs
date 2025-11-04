// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests;

[ExportLanguageService(typeof(ICompilationFactoryService), LanguageNames.CSharp, ServiceLayer.Test), Shared, PartNotDiscoverable]
internal sealed class TestCSharpCompilationFactoryServiceWithIncrementalGeneratorTracking : ICompilationFactoryService
{
    private static readonly CSharpCompilationOptions s_defaultOptions = new(OutputKind.ConsoleApplication, concurrentBuild: false);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TestCSharpCompilationFactoryServiceWithIncrementalGeneratorTracking()
    {
    }

    Compilation ICompilationFactoryService.CreateCompilation(string assemblyName, CompilationOptions options)
    {
        return CSharpCompilation.Create(
            assemblyName,
            options: (CSharpCompilationOptions)options ?? s_defaultOptions);
    }

    Compilation ICompilationFactoryService.CreateSubmissionCompilation(string assemblyName, CompilationOptions options, Type? hostObjectType)
    {
        return CSharpCompilation.CreateScriptCompilation(
            assemblyName,
            options: (CSharpCompilationOptions)options,
            previousScriptCompilation: null,
            globalsType: hostObjectType);
    }

    CompilationOptions ICompilationFactoryService.GetDefaultCompilationOptions()
        => s_defaultOptions;

    CompilationOptions? ICompilationFactoryService.TryParsePdbCompilationOptions(IReadOnlyDictionary<string, string> compilationOptionsMetadata)
    {
        if (!compilationOptionsMetadata.TryGetValue("output-kind", out var outputKindString) ||
            !Enum.TryParse<OutputKind>(outputKindString, out var outputKind))
        {
            return null;
        }

        return new CSharpCompilationOptions(outputKind: outputKind);
    }

    GeneratorDriver ICompilationFactoryService.CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts, string? generatedFilesBaseDirectory, string? projectName)
    {
        return CSharpGeneratorDriver.Create(generators, additionalTexts, (CSharpParseOptions)parseOptions, optionsProvider, new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true, baseDirectory: TempRoot.Root, projectName: projectName));
    }
}
