// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicImmediate( )
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(BasicImmediate));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.VisualBasic);
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/25814")]
        public void DumpLocalVariableValue()
        {
            VisualStudioInstance.Editor.SetText(@"
Module Module1
    Sub Main()
        Dim n1Var As Integer = 42
        Dim n2Var As Integer = 43
    End Sub
End Module
");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint("Module1.vb", "End Sub");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudioInstance.SendKeys.Send("?");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudioInstance.SendKeys.Send("n1", VirtualKey.Tab, VirtualKey.Enter);
            ExtendedAssert.Contains("?n1Var\r\n42", VisualStudioInstance.ImmediateWindow.GetText());
        }
    }
}
