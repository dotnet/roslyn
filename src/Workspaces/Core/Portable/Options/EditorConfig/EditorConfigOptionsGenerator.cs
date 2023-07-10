// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Options.EditorConfig
{
    [Export(typeof(EditorConfigOptionsGenerator)), Shared]
    internal class EditorConfigOptionsGenerator
    {
        private readonly IEnumerable<Lazy<IEditorConfigOptionsCollection, LanguageMetadata>> _editorConfigGenerators;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigOptionsGenerator(
            [ImportMany] IEnumerable<Lazy<IEditorConfigOptionsCollection, LanguageMetadata>> generators)
        {
            _editorConfigGenerators = generators;
        }

        public ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetDefaultOptions(string language)
        {
            var builder = ArrayBuilder<(string, ImmutableArray<IOption2>)>.GetInstance();
            builder.AddRange(GetLanguageAgnosticEditorConfigOptions());

            foreach (var generator in _editorConfigGenerators)
            {
                if (generator.Metadata.Language == language)
                {
                    builder.AddRange(generator.Value.GetOptions());
                }
            }

            return builder.ToImmutableAndFree();
        }

        internal static IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetLanguageAgnosticEditorConfigOptions()
        {
            yield return (WorkspacesResources.Core_EditorConfig_Options, FormattingOptions2.Options);
            yield return (WorkspacesResources.dot_NET_Coding_Conventions, GenerationOptions.AllOptions.AddRange(CodeStyleOptions2.AllOptions));
        }
    }
}
