// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Progression;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGeneratorWrapper
{
    [Export(typeof(EditorConfigGeneratorWrapper)), Shared]
    internal sealed class EditorConfigGeneratorWrapper
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly EditorConfigOptionsGenerator _editorConfigGenerator;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorConfigGeneratorWrapper(IComponentModel componentModel)
        {
            _editorConfigGenerator = componentModel.GetService<EditorConfigOptionsGenerator>();
            _globalOptions = componentModel.GetService<IGlobalOptionService>();
        }

        public string? Generate(string language)
        {
            var groupOptions = _editorConfigGenerator.GetDefaultOptions(language);
            return EditorConfigFileGenerator.Generate(groupOptions, _globalOptions, language);
        }
    }
}
