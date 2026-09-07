// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator.Api;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;

public class RoslynEditorConfigFileGenerator
{
    public RoslynEditorConfigFileGenerator()
    {
    }

    public string? Generate(string language)
    {
        try
        {
            var componentModel = (IComponentModel)ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel));
            var editorConfigGenerator = componentModel?.GetService<IEditorConfigGenerator>();
            return editorConfigGenerator?.Generate(language);
        }
        catch (Exception ex)
        {
            LogException(ex, "Unable call roslyn editorconfig generator");
            return null;
        }
    }
}
