// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    // Opens the AppDesigner ("Property Pages") on by selecting the Open menu item on the AppDesigner folder
    [ProjectCommand(CommandGroup.VisualStudioStandard97, VisualStudioStandard97CommandId.Open)]
    [AppliesTo(ProjectCapability.AppDesigner)]
    [OrderPrecedence(1000)] 
    internal class OpenAppDesignerCommand : OpenAppDesignerCommandBase
    {
        [ImportingConstructor]
        public OpenAppDesignerCommand(IUnconfiguredProjectVsServices projectServices)
            : base(projectServices)
        {
        }
    }
}
