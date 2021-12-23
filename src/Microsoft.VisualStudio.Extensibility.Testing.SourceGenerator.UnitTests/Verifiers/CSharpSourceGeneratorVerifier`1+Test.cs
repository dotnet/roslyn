// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Microsoft.VisualStudio.Extensibility.Testing.SourceGenerator.UnitTests.Verifiers
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Testing;
    using Microsoft.CodeAnalysis.Testing;
    using Microsoft.CodeAnalysis.Testing.Verifiers;

    public static partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, XUnitVerifier>
        {
            public Test()
            {
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            protected override IEnumerable<ISourceGenerator> GetSourceGenerators()
            {
                yield return new TSourceGenerator().AsSourceGenerator();
            }

            protected override CompilationOptions CreateCompilationOptions()
            {
                var compilationOptions = base.CreateCompilationOptions();
                return compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }
        }
    }
}
