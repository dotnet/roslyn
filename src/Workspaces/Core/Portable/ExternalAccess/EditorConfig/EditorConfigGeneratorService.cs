// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig
{
    [Export(typeof(EditorConfigGenerator)), Shared]
    internal class EditorConfigGenerator

    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly ImmutableArray<Lazy<IEditorConfigGeneratorCollection, LanguageMetadata>> _editorConfigGenerators;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigGenerator(
            IGlobalOptionService globalOptions,
            [ImportMany] IEnumerable<Lazy<IEditorConfigGeneratorCollection, LanguageMetadata>> generators)
        {
            _globalOptions = globalOptions;
            _editorConfigGenerators = generators.ToImmutableArray();
        }

        public string? Generate(string language)
        {
            var groupOptions = GetDefaultOptions(language);
            return EditorConfigFileGenerator.Generate(groupOptions, _globalOptions, language);
        }

        public ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetDefaultOptions(string language)
        {
            var generators = _editorConfigGenerators.Where(b => b.Metadata.Language == language);
            foreach (var generator in generators)
            {
                return generator.Value.GetEditorConfigOptions();
            }

            return default;
        }
    }
}
