// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Interactive
{
    [Export(typeof(IInteractiveWindowCommand))]
    [InteractiveWindowRole(InteractiveWindowRoles.Any)]
    internal sealed class ClearScreenCommand : InteractiveWindowCommand
    {
        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            window.Operations.ClearView();
            return ExecutionResult.Succeeded;
        }

        public override string Description
        {
            // TODO: Needs localization...
            get { return "Clears the contents of the REPL editor window, leaving history and execution context intact."; }
        }

        public override IEnumerable<string> Names
        {
            get { yield return "cls"; }
        }
    }
}
