// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIntellisense : AbstractInteractiveWindowTest
    {
        public CSharpReplIntellisense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
        }

        [Fact]
        public void VerifyCompletionListOnEmptyTextAtTopLevel()
        {
            this.InvokeCompletionList();
            this.VerifyCompletionItemExists("var", "public", "readonly", "goto");
        }

        [Fact]
        public void VerifySharpRCompletionList()
        {
            this.InsertCode("#r \"");
            this.InvokeCompletionList();
            this.VerifyCompletionItemExists("System");
        }

        [Fact]
        public void VerifyCommitCompletionOnTopLevel()
        {
            this.InsertCode("pub");
            this.InvokeCompletionList();
            this.VerifyCompletionItemExists("public");
            this.SendKeys(VirtualKey.Tab);
            this.VerifyLastReplInput("public");
            this.SendKeys(VirtualKey.Escape);
        }

        [Fact]
        public void VerifyCompletionListForAmbiguousParsingCases()
        {
            this.InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            this.SendKeys(VirtualKey.Period);
            this.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            this.VerifyCompletionItemExists("ArgumentException");
        }

        [Fact]
        public void VerifySharpLoadCompletionList()
        {
            this.InsertCode("#load \"");
            this.InvokeCompletionList();
            this.VerifyCompletionItemExists("C:");
        }

        [Fact]
        public void VerifyNoCrashOnEnter()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
            this.SendKeys("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [Fact]
        public void VerifyCorrectIntellisenseSelectionOnEnter()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
            this.SendKeys("TimeSpan.FromMin");
            this.SendKeys(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            this.WaitForReplOutput("[00:00:00]");
        }

        [Fact]
        public void VerifyCompletionListForLoadMembers()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "c.csx",
                "int x = 2; class Complex { public int foo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                this.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                this.InvokeCompletionList();
                this.VerifyCompletionItemExists("x", "Complex");
                this.SendKeys(VirtualKey.Escape);
            }
        }
    }
}