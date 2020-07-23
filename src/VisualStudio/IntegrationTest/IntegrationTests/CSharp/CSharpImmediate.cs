// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpImmediate : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpImmediate(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);

            VisualStudio.SolutionExplorer.CreateSolution(nameof(CSharpInteractive));
            var testProj = new ProjectUtils.Project("TestProj");
            VisualStudio.SolutionExplorer.AddProject(testProj, WellKnownProjectTemplates.ConsoleApplication, LanguageNames.CSharp);
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/25814")]
        public void DumpLocalVariableValue()
        {
            VisualStudio.Editor.SetText(@"
class Program
{
    static void Main(string[] args)
    {
        int n1Var = 42;
        int n2Var = 43;
    }
}
");

            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
            VisualStudio.Debugger.SetBreakPoint("Program.cs", "}");
            VisualStudio.Debugger.Go(waitForBreakMode: true);
            VisualStudio.ImmediateWindow.ShowImmediateWindow(clearAll: true);
            VisualStudio.SendKeys.Send("?n");
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.CompletionSet);
            VisualStudio.SendKeys.Send("1", VirtualKey.Tab, VirtualKey.Enter);
            Assert.Contains("?n1Var\r\n42", VisualStudio.ImmediateWindow.GetText());
        }
    }
}
