// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIntellisense : AbstractInteractiveWindowTest
    {
        public CSharpReplIntellisense(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
            VisualStudio.Workspace.SetUseSuggestionMode(true);
        }

        [Fact]
        public void VerifyCompletionListOnEmptyTextAtTopLevel()
        {
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("var", "public", "readonly", "goto");
        }

        [Fact]
        public void VerifySharpRCompletionList()
        {
            VisualStudio.InteractiveWindow.InsertCode("#r \"");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("System");
        }

        [Fact]
        public void VerifyCommitCompletionOnTopLevel()
        {
            VisualStudio.InteractiveWindow.InsertCode("pub");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("public");
            VisualStudio.SendKeys.Send(VirtualKey.Tab);
            VisualStudio.InteractiveWindow.Verify.LastReplInput("public");
            VisualStudio.SendKeys.Send(VirtualKey.Escape);
        }

        [Fact]
        public void VerifyCompletionListForAmbiguousParsingCases()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            VisualStudio.SendKeys.Send(VirtualKey.Period);
            VisualStudio.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("ArgumentException");
        }

        [Fact]
        public void VerifySharpLoadCompletionList()
        {
            VisualStudio.InteractiveWindow.InsertCode("#load \"");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("C:");
        }

        [Fact]
        public void VerifyNoCrashOnEnter()
        {
            VisualStudio.Workspace.SetUseSuggestionMode(false);
            VisualStudio.SendKeys.Send("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [Fact]
        public void VerifyCorrectIntellisenseSelectionOnEnter()
        {
            VisualStudio.Workspace.SetUseSuggestionMode(false);
            VisualStudio.SendKeys.Send("TimeSpan.FromMin");
            VisualStudio.SendKeys.Send(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForReplOutput("[00:00:00]");
        }

        [Fact]
        public void VerifyCompletionListForLoadMembers()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "c.csx",
                "int x = 2; class Complex { public int foo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                VisualStudio.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudio.InteractiveWindow.InvokeCompletionList();
                VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("x", "Complex");
                VisualStudio.SendKeys.Send(VirtualKey.Escape);
            }
        }
    }
}