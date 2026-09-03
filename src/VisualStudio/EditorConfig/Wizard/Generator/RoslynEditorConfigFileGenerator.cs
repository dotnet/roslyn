// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using System;
using Microsoft.VisualStudio.ComponentModelHost;
using static Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Logger;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator.Api;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfigGenerator;

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
