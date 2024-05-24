// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EncapsulateField;

internal class EncapsulateFieldTestState : IDisposable
{
    private readonly EditorTestHostDocument _testDocument;
    public EditorTestWorkspace Workspace { get; }
    public Document TargetDocument { get; }
    public string NotificationMessage { get; private set; }

    public EncapsulateFieldTestState(EditorTestWorkspace workspace)
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
        var workspace = EditorTestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures);

        workspace.GlobalOptions.SetGlobalOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement);
        workspace.GlobalOptions.SetGlobalOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.NeverWithSilentEnforcement);

        return new EncapsulateFieldTestState(workspace);
    }

    public async Task EncapsulateAsync()
    {
        var args = new EncapsulateFieldCommandArgs(_testDocument.GetTextView(), _testDocument.GetTextBuffer());
        var commandHandler = Workspace.ExportProvider.GetCommandHandler<EncapsulateFieldCommandHandler>(PredefinedCommandHandlerNames.EncapsulateField, ContentTypeNames.CSharpContentType);
        var provider = Workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();
        var waiter = (IAsynchronousOperationWaiter)provider.GetListener(FeatureAttribute.EncapsulateField);
        commandHandler.ExecuteCommand(args, TestCommandExecutionContext.Create());
        await waiter.ExpeditedWaitAsync();
    }

    public void Dispose()
        => Workspace?.Dispose();

    public async Task AssertEncapsulateAsAsync(string expected)
    {
        await EncapsulateAsync();
        Assert.Equal(expected, _testDocument.GetTextBuffer().CurrentSnapshot.GetText().ToString());
    }

    public async Task AssertErrorAsync()
    {
        await EncapsulateAsync();
        Assert.NotNull(NotificationMessage);
    }
}
