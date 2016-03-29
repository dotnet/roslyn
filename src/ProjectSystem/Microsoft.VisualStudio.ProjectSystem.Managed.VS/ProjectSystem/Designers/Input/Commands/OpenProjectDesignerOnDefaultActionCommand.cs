// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Input;
using Microsoft.VisualStudio.ProjectSystem.Utilities;

namespace Microsoft.VisualStudio.ProjectSystem.Designers.Input.Commands
{
    // Opens the Project Designer ("Property Pages") when the user double-clicks or presses ENTER on the AppDesigner folder while its selected
    [ProjectCommand(CommandGroup.UIHierarchyWindow, UIHierarchyWindowCommandId.DoubleClick, UIHierarchyWindowCommandId.EnterKey)]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    [OrderPrecedence(1000)] 
    internal class OpenProjectDesignerOnDefaultActionCommand : AbstractOpenProjectDesignerCommand
    {
        [ImportingConstructor]
        public OpenProjectDesignerOnDefaultActionCommand(IProjectDesignerService designerService)
            : base(designerService)
        {
        }
    }
}
