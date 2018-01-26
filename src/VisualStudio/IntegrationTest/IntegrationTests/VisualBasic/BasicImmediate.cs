// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class BasicImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicImmediate(VisualStudioInstanceFactory instanceFactory) : base(instanceFactory)
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(BasicImmediate));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [Fact]
        public void DumpLocalVaribleValue()
        {
            VisualStudio.Editor.SetText(@"
Module Module1
    Sub Main()
        Dim n1 As Integer = 42
        Dim n2 As Integer = 43
    End Sub
End Module
");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint("Module1.vb", "End Sub");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudio.SendKeys.Send("?");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudio.SendKeys.Send("n1", VirtualKey.Enter);
            Assert.Contains("?n1\r\n42", VisualStudio.ImmediateWindow.GetText());
        }
    }
}
