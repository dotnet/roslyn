// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic
{
    public class BasicImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicImmediate()
            : base()
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await TestServices.SolutionExplorer.CreateSolutionAsync(nameof(BasicImmediate), HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync(ProjectName, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic, HangMitigatingCancellationToken);
        }

        [IdeFact]
        public async Task DumpLocalVariableValue()
        {
            await TestServices.Editor.SetTextAsync(@"
Module Module1
    Sub Main()
        Dim n1Var As Integer = 42
        Dim n2Var As Integer = 43
    End Sub
End Module
", HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace, HangMitigatingCancellationToken);
            await TestServices.Debugger.SetBreakpointAsync(ProjectName, "Module1.vb", "End Sub", HangMitigatingCancellationToken);
            await TestServices.Debugger.GoAsync(waitForBreakMode: true, HangMitigatingCancellationToken);
            await TestServices.ImmediateWindow.ShowAsync(HangMitigatingCancellationToken);
            await TestServices.ImmediateWindow.ClearAllAsync(HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync("?", HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet, HangMitigatingCancellationToken);
            await TestServices.Input.SendWithoutActivateAsync(["n1", VirtualKeyCode.TAB, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
            Assert.Contains("?n1Var\r\n42", await TestServices.ImmediateWindow.GetTextAsync(HangMitigatingCancellationToken));
        }
    }
}
