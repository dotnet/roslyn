// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    internal class EncapsulateFieldTestState : IDisposable
    {
        private readonly TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document TargetDocument { get; }
        public string NotificationMessage { get; private set; }

        public EncapsulateFieldTestState(TestWorkspace workspace)
        {
            Workspace = workspace;
            _testDocument = Workspace.Documents.Single(d => d.CursorPosition.HasValue || d.SelectedSpans.Any());
            TargetDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);

            var notificationService = Workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
            var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => NotificationMessage = message);
            notificationService.NotificationCallback = callback;
        }

        public static EncapsulateFieldTestState Create(string markup)
        {
            var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement)
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement)));

            return new EncapsulateFieldTestState(workspace);
        }

        public void Encapsulate()
        {
            var args = new EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer());
            var commandHandler = Workspace.ExportProvider.GetCommandHandler<EncapsulateFieldCommandHandler>(PredefinedCommandHandlerNames.EncapsulateField, ContentTypeNames.CSharpContentType);
            commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create());
        }

        public void Dispose()
        {
            if (Workspace != null)
            {
                Workspace.Dispose();
            }
        }

        public void AssertEncapsulateAs(string expected)
        {
            Encapsulate();
            Assert.Equal(expected, _testDocument.GetTextBuffer().CurrentSnapshot.GetText().ToString());
        }

        public void AssertError()
        {
            Encapsulate();
            Assert.NotNull(NotificationMessage);
        }
    }
}
