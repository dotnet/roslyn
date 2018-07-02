// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIntellisense : AbstractIdeInteractiveWindowTest
    {
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(true);
        }

        [IdeFact]
        public async Task VerifyCompletionListOnEmptyTextAtTopLevelAsync()
        {
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("var", "public", "readonly", "goto");
        }

        [IdeFact]
        public async Task VerifySharpRCompletionListAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("#r \"");
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("System");
        }

        [IdeFact]
        public async Task VerifyCommitCompletionOnTopLevelAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("pub");
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("public");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Tab);
            VisualStudio.InteractiveWindow.Verify.LastReplInput("public");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Escape);
        }

        [IdeFact]
        public async Task VerifyCompletionListForAmbiguousParsingCasesAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Period);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.CompletionSet);
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("ArgumentException");
        }

        [IdeFact]
        public async Task VerifySharpLoadCompletionListAsync()
        {
            VisualStudio.InteractiveWindow.InsertCode("#load \"");
            await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();
            await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("C:");
        }

        [IdeFact]
        public async Task VerifyNoCrashOnEnterAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);
            await VisualStudio.SendKeys.SendAsync("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [IdeFact]
        public async Task VerifyCorrectIntellisenseSelectionOnEnterAsync()
        {
            await VisualStudio.Workspace.SetUseSuggestionModeAsync(false);
            await VisualStudio.SendKeys.SendAsync("TimeSpan.FromMin");
            await VisualStudio.SendKeys.SendAsync(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            await VisualStudio.InteractiveWindow.WaitForReplOutputAsync("[00:00:00]");
        }

        [IdeFact]
        public async Task VerifyCompletionListForLoadMembersAsync()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "c.csx",
                "int x = 2; class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                await VisualStudio.InteractiveWindow.SubmitTextAsync(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                await VisualStudio.InteractiveWindow.InvokeCompletionListAsync();
                await VisualStudio.InteractiveWindow.Verify.CompletionItemsExistAsync("x", "Complex");
                await VisualStudio.SendKeys.SendAsync(VirtualKey.Escape);
            }
        }
    }
}
