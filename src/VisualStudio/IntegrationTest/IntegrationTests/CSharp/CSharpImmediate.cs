// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpImmediate( )
            : base()
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            VisualStudioInstance.SolutionExplorer.CreateSolution(nameof(CSharpInteractive));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudioInstance.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/25814")]
        public void DumpLocalVariableValue()
        {
            VisualStudioInstance.Editor.SetText(@"
class Program
{
    static void Main(string[] args)
    {
        int n1Var = 42;
        int n2Var = 43;
    }
}
");

            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudioInstance.Debugger.SetBreakPoint("Program.cs", "}");
            VisualStudioInstance.Debugger.Go(waitForBreakMode: true);
            VisualStudioInstance.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudioInstance.SendKeys.Send("?n");
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudioInstance.SendKeys.Send("1", VirtualKey.Tab, VirtualKey.Enter);
            ExtendedAssert.Contains("?n1Var\r\n42", VisualStudioInstance.ImmediateWindow.GetText());
        }
    }
}
