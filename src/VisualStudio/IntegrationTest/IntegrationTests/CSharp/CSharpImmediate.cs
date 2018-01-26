// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpImmediate(VisualStudioInstanceFactory instanceFactory) : base(instanceFactory)
        {
            VisualStudio.SolutionExplorer.CreateSolution(nameof(CSharpInteractive));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [Fact]
        public void DumpLocalVaribleValue()
        {
            VisualStudio.Editor.SetText(@"
class Program
{
    static void Main(string[] args)
    {
        int n1 = 42;
        int n2 = 43;
    }
}
");

            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint("Program.cs", "}");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudio.SendKeys.Send("?n");
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudio.SendKeys.Send("1", VirtualKey.Enter);
            Assert.Contains("?n1\r\n42", VisualStudio.ImmediateWindow.GetText());
        }
    }
}
