// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicImmediate(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicImmediate));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/25814")]
        public void DumpLocalVariableValue()
        {
            VisualStudio.Editor.SetText(@"
Module Module1
    Sub Main()
        Dim n1Var As Integer = 42
        Dim n2Var As Integer = 43
    End Sub
End Module
");

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint("Module1.vb", "End Sub");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudio.SendKeys.Send("?");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.CompletionSet);
            VisualStudio.SendKeys.Send("n1", VirtualKey.Tab, VirtualKey.Enter);
            Assert.Contains("?n1Var\r\n42", VisualStudio.ImmediateWindow.GetText());
        }
    }
}
