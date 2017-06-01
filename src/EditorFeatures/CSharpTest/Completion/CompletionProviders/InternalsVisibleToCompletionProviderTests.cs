// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
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
            var ws = workspaceFixture.GetWorkspace();
            var solution = ws.CurrentSolution;
            var projectInfo1 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary1", "ClassLibrary1", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile: s_keyPairFile, strongNameProvider: s_defaultProvider));
            var projectInfo2 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary2", "ClassLibrary2", LanguageNames.CSharp);
            var projectInfo3 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary3", "ClassLibrary3", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, strongNameProvider: s_defaultProvider));
            projectInfo3 = projectInfo3.WithMetadataReferences(new MetadataReference[] { MscorlibRef });
            projectInfo3 = projectInfo3.WithDocuments(new DocumentInfo[] {
                DocumentInfo.Create(DocumentId.CreateNewId(projectInfo3.Id), "AssemblyInfo.cs",
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(
                @"[assembly: System.Reflection.AssemblyKeyFile(""" + s_keyPairFile.Replace(@"\",@"\\") + @""")]"), VersionStamp.Default)))
            });
            var projectInfo4 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary4", "ClassLibrary4", LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoKeyFile: s_keyPairFile, strongNameProvider: s_defaultProvider, delaySign: true));
            solution = solution.AddProject(projectInfo1).AddProject(projectInfo2).AddProject(projectInfo3).AddProject(projectInfo4);
            ws.ChangeSolution(solution);
        }

        internal override CompletionProvider CreateCompletionProvider() => new InternalsVisibleToCompletionProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionContainsOtherAssembliesOfSolution()
        {
            var text = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            await VerifyItemExistsAsync(text, "ClassLibrary1");
            await VerifyItemExistsAsync(text, "ClassLibrary2");
            await VerifyItemExistsAsync(text, "ClassLibrary3");
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
        public async Task CodeCompletionDoesNotContainCurrentAssembly()
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

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionContainsPublicKeyIfKeyIsSpecifiedByAttribute()
        {
            var before = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            var after = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary3, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
";
            await VerifyProviderCommitAsync(before, "ClassLibrary3", after, null, "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletionContainsPublicKeyIfDelayedSigningIsEnabled()
        {
            var before = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")]
";
            var after = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary4, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
";
            await VerifyProviderCommitAsync(before, "ClassLibrary4", after, null, "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task CodeCompletetionIsCanceledIfAttributeIsNotTheBCLAttribute()
        {
            var text = @"
[assembly: Test.InternalsVisibleTo(""$$"")]
namespace Test
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class InternalsVisibleToAttribute: System.Attribute
    {
        public InternalsVisibleToAttribute(string ignore)
        {

        }
    }
}";
            await VerifyNoItemsExistAsync(text);
        }
    }
}