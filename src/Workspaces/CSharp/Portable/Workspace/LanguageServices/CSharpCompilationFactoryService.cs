// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICompilationFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpCompilationFactoryService : ICompilationFactoryService
    {
        private static readonly CSharpCompilationOptions s_defaultOptions = new(OutputKind.ConsoleApplication, concurrentBuild: false);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCompilationFactoryService()
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

        GeneratorDriver ICompilationFactoryService.CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts)
        {
            return CSharpGeneratorDriver.Create(generators, additionalTexts, (CSharpParseOptions)parseOptions, optionsProvider);
        }
    }
}
