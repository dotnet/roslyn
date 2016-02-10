// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System.Xml.Linq;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    /// <summary>
    /// This class creates a mock execution state that allows to send CopyToInteractive and ExeciteInInteractive.
    /// Commands against a mock InteractiveWindow.
    /// </summary>
    internal class InteractiveWindowCommandHandlerTestState : AbstractCommandHandlerTestState
    {
        public InteractiveWindowTestHost TestHost { get; }

        private InteractiveCommandHandler _commandHandler;

        public ITextView WindowTextView => TestHost.Window.TextView;

        public ITextBuffer WindowCurrentLanguageBuffer => TestHost.Window.CurrentLanguageBuffer;

        public ITextSnapshot WindowSnapshot => WindowCurrentLanguageBuffer.CurrentSnapshot;

        public TestInteractiveEvaluator Evaluator => TestHost.Evaluator;

        private ICommandHandler<ExecuteInInteractiveCommandArgs> ExecuteInInteractiveCommandHandler => _commandHandler;

        private ICommandHandler<CopyToInteractiveCommandArgs> CopyToInteractiveCommandHandler => _commandHandler;

        public InteractiveWindowCommandHandlerTestState(XElement workspaceElement)
            : base(workspaceElement)
        {
            TestHost = new InteractiveWindowTestHost();

            _commandHandler = new TestInteractiveCommandHandler(
                TestHost.Window,
                GetExportedValue<IContentTypeRegistryService>(),
                GetExportedValue<IEditorOptionsFactoryService>(),
                GetExportedValue<IEditorOperationsFactoryService>());
        }

        public static InteractiveWindowCommandHandlerTestState CreateTestState(string markup)
        {
            var workspaceXml = XElement.Parse($@"
                    <Workspace>
                        <Project Language=""C#"" CommonReferences=""true"">
                            <Document>{markup}</Document>
                        </Project>
                    </Workspace>
                ");

            return new InteractiveWindowCommandHandlerTestState(workspaceXml);
        }

        public void SendCopyToInteractive()
        {
            var copyToInteractiveArgs = new CopyToInteractiveCommandArgs(TextView, SubjectBuffer);
            CopyToInteractiveCommandHandler.ExecuteCommand(copyToInteractiveArgs, () => { });
        }

        public CommandState GetStateForCopyToInteractive()
        {
            var copyToInteractiveArgs = new CopyToInteractiveCommandArgs(TextView, SubjectBuffer);
            return CopyToInteractiveCommandHandler.GetCommandState(
                copyToInteractiveArgs, () => { return CommandState.Unavailable; });
        }

        public void ExecuteInInteractive()
        {
            var executeInInteractiveArgs = new ExecuteInInteractiveCommandArgs(TextView, SubjectBuffer);
            ExecuteInInteractiveCommandHandler.ExecuteCommand(executeInInteractiveArgs, () => { });
        }

        public CommandState GetStateForExecuteInInteractive()
        {
            var executeInInteractiveArgs = new ExecuteInInteractiveCommandArgs(TextView, SubjectBuffer);
            return ExecuteInInteractiveCommandHandler.GetCommandState(
                executeInInteractiveArgs, () => { return CommandState.Unavailable; });
        }
    }
}
