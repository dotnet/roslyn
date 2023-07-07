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
using Microsoft.CodeAnalysis.Options.EditorConfig;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig
{
    [Export(typeof(EditorConfigGeneratorWrapper)), Shared]
    internal sealed class EditorConfigGeneratorWrapper
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly EditorConfigOptionsGenerator _editorConfigGenerator;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigGeneratorWrapper(
            IGlobalOptionService globalOptions,
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
