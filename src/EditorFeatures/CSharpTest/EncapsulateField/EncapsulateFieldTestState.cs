// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.Implementation.Notification;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Operations;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    internal class EncapsulateFieldTestState : IDisposable
    {
        private TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document TargetDocument { get; }
        public string NotificationMessage { get; private set; }

        private static readonly ExportProvider s_exportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
            typeof(CSharpEncapsulateFieldService),
            typeof(EditorNotificationServiceFactory),
            typeof(DefaultDocumentSupportsFeatureService)));

        public EncapsulateFieldTestState(TestWorkspace workspace)
        {
            Workspace = workspace;
            _testDocument = Workspace.Documents.Single(d => d.CursorPosition.HasValue || d.SelectedSpans.Any());
            TargetDocument = Workspace.CurrentSolution.GetDocument(_testDocument.Id);

            var notificationService = Workspace.Services.GetService<INotificationService>() as INotificationServiceCallback;
            var callback = new Action<string, string, NotificationSeverity>((message, title, severity) => NotificationMessage = message);
            notificationService.NotificationCallback = callback;
        }

        public static async Task<EncapsulateFieldTestState> CreateAsync(string markup)
        {
            var workspace = await CSharpWorkspaceFactory.CreateWorkspaceFromFileAsync(markup, exportProvider: s_exportProvider);
            return new EncapsulateFieldTestState(workspace);
        }

        public void Encapsulate()
        {
            var args = new EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer());
            var commandHandler = new EncapsulateFieldCommandHandler(TestWaitIndicator.Default, Workspace.GetService<ITextBufferUndoManagerProvider>());
            commandHandler.ExecuteCommand(args, () => { });
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
