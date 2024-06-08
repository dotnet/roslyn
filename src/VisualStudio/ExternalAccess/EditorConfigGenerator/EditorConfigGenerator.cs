// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.EditorConfig;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator
{
    [Export(typeof(IEditorConfigGenerator)), Shared]
    internal sealed class EditorConfigGenerator : IEditorConfigGenerator
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly EditorConfigOptionsGenerator _editorConfigGenerator;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigGenerator(IGlobalOptionService globalOptions,
            EditorConfigOptionsGenerator optionsGenerator)
        {
            _globalOptions = globalOptions;
            _editorConfigGenerator = optionsGenerator;
        }

        public string? Generate(string language)
        {
            var groupOptions = _editorConfigGenerator.GetDefaultOptions(language);
            return EditorConfigFileGenerator.Generate(groupOptions, _globalOptions, language);
        }
    }
}
