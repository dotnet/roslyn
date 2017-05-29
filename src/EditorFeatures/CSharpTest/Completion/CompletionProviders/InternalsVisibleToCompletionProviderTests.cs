using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class InternalsVisibleToCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        private static readonly string s_keyPairFile = SigningTestHelpers.KeyPairFile;
        private static readonly DesktopStrongNameProvider s_defaultProvider = new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create<string>());
        public InternalsVisibleToCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
            // I needed to configure the workspace here, because CreateWorkspace was never called.
            var ws = workspaceFixture.GetWorkspace();
            var solution = ws.CurrentSolution;
            var pi1 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary1", "ClassLibrary1", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile: s_keyPairFile, strongNameProvider: s_defaultProvider));
            var pi2 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary2", "ClassLibrary2", LanguageNames.CSharp);
            solution = solution.AddProject(pi1).AddProject(pi2);
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
            await VerifyItemExistsAsync(text, "ClassLibrary1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionIsEmptyAtClosingDoubleQuote()
        {
            var text = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""""$$)]
";
            await VerifyNoItemsExistAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionDoesNotContainsCurrentAssembly()
        {
            var text = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            await VerifyItemIsAbsentAsync(text, "Test");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionInsertsAssemblyNameOnCommit()
        {
            var before = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            var after = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary2"")]
";
            await VerifyProviderCommitAsync(before, "ClassLibrary2", after, null, "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionInsertsPublicKeyOnCommit()
        {
            var before = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            var after = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
";
            await VerifyProviderCommitAsync(before, "ClassLibrary1", after, null, "");
        }
    }
}
