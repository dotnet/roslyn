// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

public class BasicArgumentProvider : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.VisualBasic;

    public BasicArgumentProvider()
        : base(nameof(BasicArgumentProvider))
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(true);

        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.CSharp, true);
        globalOptions.SetGlobalOption(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, LanguageNames.VisualBasic, true);
    }

    [IdeFact]
    public async Task SimpleTabTabCompletion()
    {
        await SetUpEditorAsync(@"
Public Class Test
    Private f As Object

    Public Sub Method()$$
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task TabTabCompleteObjectEquals()
    {
        await SetUpEditorAsync(@"
Public Class Test
    Public Sub Method()
        $$
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("Object.Equ", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Object.Equals$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Object.Equals(Nothing$$)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task TabTabCompleteNewObject()
    {
        await SetUpEditorAsync(@"
Public Class Test
    Public Sub Method()
        Dim value = $$
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("New Obje", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim value = New Object$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim value = New Object($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Dim value = New Object()$$", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63043")]
    public async Task TabTabCompletionWithArguments()
    {
        await SetUpEditorAsync(@"
Imports System
Public Class Test
    Private f As Integer

    Public Sub Method(provider As IFormatProvider)$$
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(Nothing$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(Nothing$$, provider)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("\"format\"", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$, provider)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\", provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(provider$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString(\"format\"$$)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task FullCycle()
    {
        await SetUpEditorAsync(@"
Imports System
Public Class TestClass
    Public Sub Method()$$
    End Sub

    Sub Test()
    End Sub

    Sub Test(x As Integer)
    End Sub

    Sub Test(x As Integer, y As Integer)
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("Tes", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task ImplicitArgumentSwitching()
    {
        await SetUpEditorAsync(@"
Imports System
Public Class TestClass
    Public Sub Method()$$
    End Sub

    Sub Test()
    End Sub

    Sub Test(x As Integer)
    End Sub

    Sub Test(x As Integer, y As Integer)
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("Tes", HangMitigatingCancellationToken);

        // Trigger the session and type '0' without waiting for the session to finish initializing
        await TestServices.Input.SendAsync([VirtualKeyCode.TAB, VirtualKeyCode.TAB, '0'], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.DOWN, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$, 0)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.UP, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        Test(0$$)", assertCaretPosition: true, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task SmartBreakLineWithTabTabCompletion()
    {
        await SetUpEditorAsync(@"
Public Class Test
    Private f As Object

    Public Sub Method()$$
    End Sub
End Class
", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.RETURN, HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync("f.ToSt", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString$$", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync(VirtualKeyCode.TAB, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync([FeatureAttribute.Workspace, FeatureAttribute.SignatureHelp], HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CurrentLineTextAsync("        f.ToString($$)", assertCaretPosition: true, HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync((VirtualKeyCode.RETURN, VirtualKeyCode.SHIFT), HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync(@"
Public Class Test
    Private f As Object

    Public Sub Method()
        f.ToString()
$$
    End Sub
End Class
", assertCaretPosition: true, HangMitigatingCancellationToken);
    }
}
