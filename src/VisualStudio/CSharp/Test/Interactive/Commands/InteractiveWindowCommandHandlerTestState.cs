// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Interactive.Commands
{
    /// <summary>
    /// This class creates a mock execution state that allows to send CopyToInteractive and ExeciteInInteractive.
    /// Commands against a mock InteractiveWindow.
    /// </summary>
    internal class InteractiveWindowCommandHandlerTestState : AbstractCommandHandlerTestState
    {
        public InteractiveWindowTestHost TestHost { get; }

        private readonly InteractiveCommandHandler _commandHandler;

        public ITextView WindowTextView => TestHost.Window.TextView;

        public ITextBuffer WindowCurrentLanguageBuffer => TestHost.Window.CurrentLanguageBuffer;

        public ITextSnapshot WindowSnapshot => WindowCurrentLanguageBuffer.CurrentSnapshot;

        public TestInteractiveEvaluator Evaluator => TestHost.Evaluator;

        private ICommandHandler<ExecuteInInteractiveCommandArgs> ExecuteInInteractiveCommandHandler => _commandHandler;

        private ICommandHandler<CopyToInteractiveCommandArgs> CopyToInteractiveCommandHandler => _commandHandler;

        public InteractiveWindowCommandHandlerTestState(XElement workspaceElement)
            : base(workspaceElement, EditorTestCompositions.InteractiveWindow, workspaceKind: null)
        {
            TestHost = new InteractiveWindowTestHost(GetExportedValue<IInteractiveWindowFactoryService>());

            _commandHandler = new TestInteractiveCommandHandler(
                TestHost.Window,
                GetExportedValue<ISendToInteractiveSubmissionProvider>(),
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
            CopyToInteractiveCommandHandler.ExecuteCommand(copyToInteractiveArgs, TestCommandExecutionContext.Create());
        }

        public void ExecuteInInteractive()
        {
            var executeInInteractiveArgs = new ExecuteInInteractiveCommandArgs(TextView, SubjectBuffer);
            ExecuteInInteractiveCommandHandler.ExecuteCommand(executeInInteractiveArgs, TestCommandExecutionContext.Create());
        }

        public CommandState GetStateForExecuteInInteractive()
        {
            var executeInInteractiveArgs = new ExecuteInInteractiveCommandArgs(TextView, SubjectBuffer);
            return ExecuteInInteractiveCommandHandler.GetCommandState(executeInInteractiveArgs);
        }
    }
}
