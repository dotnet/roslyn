// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpReplIntellisense : AbstractInteractiveWindowTest
    {
        public CSharpReplIntellisense() : base() { }

        [TestInitialize]
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(true);
            VisualStudioInstance.Workspace.SetUseSuggestionMode(true);
        }

        [TestMethod]
        public void VerifyCompletionListOnEmptyTextAtTopLevel()
        {
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("var", "public", "readonly", "goto");
        }

        [TestMethod]
        public void VerifySharpRCompletionList()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("#r \"");
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("System");
        }

        [TestMethod]
        public void VerifyCommitCompletionOnTopLevel()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("pub");
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("public");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Tab);
            VisualStudioInstance.InteractiveWindow.Verify.LastReplInput("public");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Escape);
        }

        [TestMethod]
        public void VerifyCompletionListForAmbiguousParsingCases()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Period);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("ArgumentException");
        }

        [TestMethod]
        public void VerifySharpLoadCompletionList()
        {
            VisualStudioInstance.InteractiveWindow.InsertCode("#load \"");
            VisualStudioInstance.InteractiveWindow.InvokeCompletionList();
            VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("C:");
        }

        [TestMethod]
        public void VerifyNoCrashOnEnter()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);
            VisualStudioInstance.SendKeys.Send("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [TestMethod]
        public void VerifyCorrectIntellisenseSelectionOnEnter()
        {
            VisualStudioInstance.Workspace.SetUseSuggestionMode(false);
            VisualStudioInstance.SendKeys.Send("TimeSpan.FromMin");
            VisualStudioInstance.SendKeys.Send(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            VisualStudioInstance.InteractiveWindow.WaitForReplOutput("[00:00:00]");
        }

        [TestMethod]
        public void VerifyCompletionListForLoadMembers()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "c.csx",
                "int x = 2; class Complex { public int goo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                VisualStudioInstance.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                VisualStudioInstance.InteractiveWindow.InvokeCompletionList();
                VisualStudioInstance.InteractiveWindow.Verify.CompletionItemsExist("x", "Complex");
                VisualStudioInstance.SendKeys.Send(VirtualKey.Escape);
            }
        }
    }
}
