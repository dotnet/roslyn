// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.SourceGeneration;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A <see cref="GeneratorDriver"/> implementation for the CSharp language.
    /// </summary>
    public sealed class CSharpGeneratorDriver : GeneratorDriver
    {
        /// <summary>
        /// Creates a new instance of <see cref="CSharpGeneratorDriver"/>
        /// </summary>
        /// <param name="parseOptions">The <see cref="CSharpParseOptions"/> that should be used when parsing generated files.</param>
        /// <param name="generators">The generators that will run as part of this driver.</param>
        /// <param name="optionsProvider">An <see cref="AnalyzerConfigOptionsProvider"/> that can be used to retrieve analyzer config values by the generators in this driver.</param>
        /// <param name="additionalTexts">A list of <see cref="AdditionalText"/>s available to generators in this driver.</param>
        internal CSharpGeneratorDriver(CSharpParseOptions parseOptions, ImmutableArray<ISourceGenerator> generators, AnalyzerConfigOptionsProvider optionsProvider, ImmutableArray<AdditionalText> additionalTexts, GeneratorDriverOptions driverOptions)
            : base(parseOptions, generators, optionsProvider, additionalTexts, driverOptions)
        {
        }

        private CSharpGeneratorDriver(GeneratorDriverState state)
            : base(state)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="CSharpGeneratorDriver"/> with the specified <see cref="ISourceGenerator"/>s and default options
        /// </summary>
        /// <param name="generators">The generators to create this driver with</param>
        /// <returns>A new <see cref="CSharpGeneratorDriver"/> instance.</returns>
        public static CSharpGeneratorDriver Create(params ISourceGenerator[] generators)
            => Create(generators, additionalTexts: null);

        /// <summary>
        /// Creates a new instance of <see cref="CSharpGeneratorDriver"/> with the specified <see cref="IIncrementalGenerator"/>s and default options
        /// </summary>
        /// <param name="incrementalGenerators">The incremental generators to create this driver with</param>
        /// <returns>A new <see cref="CSharpGeneratorDriver"/> instance.</returns>
        public static CSharpGeneratorDriver Create(params IIncrementalGenerator[] incrementalGenerators)
            => Create(incrementalGenerators.Select(GeneratorExtensions.AsSourceGenerator), additionalTexts: null);

        /// <summary>
        /// Creates a new instance of <see cref="CSharpGeneratorDriver"/> with the specified <see cref="ISourceGenerator"/>s and the provided options or default.
        /// </summary>
        /// <param name="generators">The generators to create this driver with</param>
        /// <param name="additionalTexts">A list of <see cref="AdditionalText"/>s available to generators in this driver, or <c>null</c> if there are none.</param>
        /// <param name="parseOptions">The <see cref="CSharpParseOptions"/> that should be used when parsing generated files, or <c>null</c> to use <see cref="CSharpParseOptions.Default"/></param>
        /// <param name="optionsProvider">An <see cref="AnalyzerConfigOptionsProvider"/> that can be used to retrieve analyzer config values by the generators in this driver, or <c>null</c> if there are none.</param>
        /// <param name="driverOptions">A <see cref="GeneratorDriverOptions"/> that controls the behavior of the created driver.</param>
        /// <returns>A new <see cref="CSharpGeneratorDriver"/> instance.</returns>
        public static CSharpGeneratorDriver Create(IEnumerable<ISourceGenerator> generators, IEnumerable<AdditionalText>? additionalTexts = null, CSharpParseOptions? parseOptions = null, AnalyzerConfigOptionsProvider? optionsProvider = null, GeneratorDriverOptions driverOptions = default)
            => new CSharpGeneratorDriver(parseOptions ?? CSharpParseOptions.Default, generators.ToImmutableArray(), optionsProvider ?? CompilerAnalyzerConfigOptionsProvider.Empty, additionalTexts.AsImmutableOrEmpty(), driverOptions);

        // 3.11 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CSharpGeneratorDriver Create(IEnumerable<ISourceGenerator> generators, IEnumerable<AdditionalText>? additionalTexts, CSharpParseOptions? parseOptions, AnalyzerConfigOptionsProvider? optionsProvider)
            => Create(generators, additionalTexts, parseOptions, optionsProvider, driverOptions: default);

        internal override SyntaxTree ParseGeneratedSourceText(GeneratedSourceText input, string fileName, CancellationToken cancellationToken)
            => CSharpSyntaxTree.ParseTextLazy(input.Text, (CSharpParseOptions)_state.ParseOptions, fileName);

        internal override GeneratorDriver FromState(GeneratorDriverState state) => new CSharpGeneratorDriver(state);

        internal override CommonMessageProvider MessageProvider => CSharp.MessageProvider.Instance;

        internal override string SourceExtension => ".cs";

        internal override string EmbeddedAttributeDefinition => """
            // <auto-generated/>
            namespace Microsoft.CodeAnalysis
            {
                internal sealed partial class EmbeddedAttribute : global::System.Attribute
                {
                }
            }
            """;

        internal override ISyntaxHelper SyntaxHelper => CSharpSyntaxHelper.Instance;
    }
}
