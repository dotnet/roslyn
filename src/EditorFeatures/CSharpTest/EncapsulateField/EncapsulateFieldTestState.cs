// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.Implementation.Notification;
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField
{
    internal class EncapsulateFieldTestState : IDisposable
    {
        private readonly TestHostDocument _testDocument;
        public TestWorkspace Workspace { get; }
        public Document TargetDocument { get; }
        public string NotificationMessage { get; private set; }

        private static readonly IExportProviderFactory s_exportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                    typeof(CSharpEncapsulateFieldService),
                    typeof(EditorNotificationServiceFactory),
                    typeof(DefaultTextBufferSupportsFeatureService)));

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
            var exportProvider = s_exportProviderFactory.CreateExportProvider();
            var workspace = TestWorkspace.CreateCSharp(markup, exportProvider: exportProvider);
            workspace.Options = workspace.Options
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement)
                .WithChangedOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement);
            return new EncapsulateFieldTestState(workspace);
        }

        public void Encapsulate()
        {
            var args = new EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer());
            var commandHandler = new EncapsulateFieldCommandHandler(Workspace.GetService<ITextBufferUndoManagerProvider>(),
                Workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>());
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
