﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Microsoft.VisualStudio.InteractiveWindow.Commands
{
    [Export(typeof(IInteractiveWindowCommand))]
    internal sealed class ClearScreenCommand : InteractiveWindowCommand
    {
        public override Task<ExecutionResult> Execute(IInteractiveWindow window, string arguments)
        {
            window.Operations.ClearView();
            return ExecutionResult.Succeeded;
        }

        public override string Description
        {
            get { return "Clears the contents of the REPL editor window, leaving history and execution context intact."; }
        }

        public override IEnumerable<string> Names
        {
            get { yield return "cls"; }
        }
    }
}
