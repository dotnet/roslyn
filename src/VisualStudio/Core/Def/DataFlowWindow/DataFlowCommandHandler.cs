// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.DataFlowWindow
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.GoToDataFlow)]
    internal class DataFlowCommandHandler : ICommandHandler<DataFlowEditorCommandArgs>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DataFlowCommandHandler()
        {
        }

        public string DisplayName => "Go to data flow";

        public bool ExecuteCommand(DataFlowEditorCommandArgs args, CommandExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public CommandState GetCommandState(DataFlowEditorCommandArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
