// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICompilationFactoryService : ILanguageService
    {
        Compilation CreateCompilation(string assemblyName, CompilationOptions options);
        Compilation CreateSubmissionCompilation(string assemblyName, CompilationOptions options, Type? hostObjectType);
        CompilationOptions GetDefaultCompilationOptions();
        CompilationOptions? TryParsePdbCompilationOptions(IReadOnlyDictionary<string, string> compilationOptionsMetadata);
        GeneratorDriver CreateGeneratorDriver(ParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts);
    }
}
