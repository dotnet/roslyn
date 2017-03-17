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
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(true);
        }

        [Fact]
        public void VerifyCompletionListOnEmptyTextAtTopLevel()
        {
            InvokeCompletionList();
            VerifyCompletionItemExists("var", "public", "readonly", "goto");
        }

        [Fact]
        public void VerifySharpRCompletionList()
        {
            InsertCode("#r \"");
            InvokeCompletionList();
            VerifyCompletionItemExists("System");
        }

        [Fact]
        public void VerifyCommitCompletionOnTopLevel()
        {
            InsertCode("pub");
            InvokeCompletionList();
            VerifyCompletionItemExists("public");
            SendKeys(VirtualKey.Tab);
            VerifyLastReplInput("public");
            SendKeys(VirtualKey.Escape);
        }

        [Fact]
        public void VerifyCompletionListForAmbiguousParsingCases()
        {
            InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            SendKeys(VirtualKey.Period);
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
            VerifyCompletionItemExists("ArgumentException");
        }

        [Fact]
        public void VerifySharpLoadCompletionList()
        {
            InsertCode("#load \"");
            InvokeCompletionList();
            VerifyCompletionItemExists("C:");
        }

        [Fact]
        public void VerifyNoCrashOnEnter()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
            SendKeys("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [Fact]
        public void VerifyCorrectIntellisenseSelectionOnEnter()
        {
            VisualStudioWorkspaceOutOfProc.SetUseSuggestionMode(false);
            SendKeys("TimeSpan.FromMin");
            SendKeys(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            WaitForReplOutput("[00:00:00]");
        }

        [Fact]
        public void VerifyCompletionListForLoadMembers()
        {
            using (var temporaryTextFile = new TemporaryTextFile(
                "c.csx", 
                "int x = 2; class Complex { public int foo() { return 4; } }"))
            {
                temporaryTextFile.Create();
                SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
                InvokeCompletionList();
                VerifyCompletionItemExists("x", "Complex");
                SendKeys(VirtualKey.Escape);
            }
        }
    }
}