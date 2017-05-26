using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class InternalsVisibleToCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public InternalsVisibleToCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
            // I needed to configure the workspace here, because CreateWorkspace was never called.
            var ws = workspaceFixture.GetWorkspace();
            var solution = ws.CurrentSolution;
            solution = solution.AddProject(ProjectId.CreateNewId(), "ClassLibrary1", "ClassLibrary1.dll", LanguageNames.CSharp);
            ws.ChangeSolution(solution);
        }

        protected override TestWorkspace CreateWorkspace(string fileContents)
        {
            // This would be the place to configure the workspace, but it is never called.
            return base.CreateWorkspace(fileContents);
        }

        internal override CompletionProvider CreateCompletionProvider() => new InternalsVisibleToCompletionProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionContainsOtherAssemblyOfSolution()
        {
            var text = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            await VerifyItemExistsAsync(text, "ClassLibrary1.dll");
        }
    }
}
