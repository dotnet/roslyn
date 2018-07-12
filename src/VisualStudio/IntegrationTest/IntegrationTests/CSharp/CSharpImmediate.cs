// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpImmediate : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await VisualStudio.SolutionExplorer.CreateSolutionAsync(nameof(CSharpInteractive));
            await VisualStudio.SolutionExplorer.AddProjectAsync("TestProj", WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [IdeFact]
        public async Task DumpLocalVariableValueAsync()
        {
            await VisualStudio.Editor.SetTextAsync(@"
class Program
{
    static void Main(string[] args)
    {
        int n1Var = 42;
        int n2Var = 43;
    }
}
");

            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Workspace);
            await VisualStudio.Debugger.SetBreakPointAsync("Program.cs", "}");
            await VisualStudio.Debugger.GoAsync(waitForBreakMode: true);
            await VisualStudio.ImmediateWindow.ShowImmediateWindowAsync(clearAll: true);
            await VisualStudio.SendKeys.SendAsync("?n");
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet);
            await VisualStudio.SendKeys.SendAsync("1", VirtualKey.Tab, VirtualKey.Enter);
            Assert.Contains("?n1Var\r\n42", await VisualStudio.ImmediateWindow.GetTextAsync());
        }
    }
}
