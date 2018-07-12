// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicImmediate : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(BasicImmediate));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [IdeFact]
        public async Task DumpLocalVariableValueAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
Module Module1
    Sub Main()
        Dim n1Var As Integer = 42
        Dim n2Var As Integer = 43
    End Sub
End Module
");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync("Module1.vb", "End Sub");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.ImmediateWindow.ShowImmediateWindowAsync(clearAll: true);
            await VisualStudio.SendKeys.SendAsync("?");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet);
            await VisualStudio.SendKeys.SendAsync("n1", VirtualKey.Tab, VirtualKey.Enter);
            Assert.Contains("?n1Var\r\n42", await VisualStudio.ImmediateWindow.GetTextAsync());
        }
    }
}
