// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpReplIntellisense : AbstractInteractiveWindowTest
    {
        public CSharpReplIntellisense(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper)
        {
        }

        [WpfFact]
        public void VerifyCompletionListOnEmptyTextAtTopLevel()
        {
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("var", "public", "readonly", "goto");
        }

        [WpfFact]
        public void VerifySharpRCompletionList()
        {
            VisualStudio.InteractiveWindow.InsertCode("#r \"");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("System");
        }

        [WpfFact]
        public void VerifyCommitCompletionOnTopLevel()
        {
            VisualStudio.InteractiveWindow.InsertCode("pub");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("public");
            VisualStudio.SendKeys.Send(VirtualKey.Tab);
            VisualStudio.InteractiveWindow.Verify.LastReplInput("public");
            VisualStudio.SendKeys.Send(VirtualKey.Escape);
        }

        [WpfFact]
        public void VerifyCompletionListForAmbiguousParsingCases()
        {
            VisualStudio.InteractiveWindow.InsertCode(@"class C { }
public delegate R Del<T, R>(T arg);
Del<C, System");
            VisualStudio.SendKeys.Send(VirtualKey.Period);
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("ArgumentException");
        }

        [WpfFact]
        public void VerifySharpLoadCompletionList()
        {
            VisualStudio.InteractiveWindow.InsertCode("#load \"");
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("C:");
        }

        [WpfFact]
        public void VerifyNoCrashOnEnter()
        {
            VisualStudio.Editor.SetUseSuggestionMode(false);
            VisualStudio.SendKeys.Send("#help", VirtualKey.Enter, VirtualKey.Enter);
        }

        [WpfFact]
        public void VerifyCorrectIntellisenseSelectionOnEnter()
        {
            VisualStudio.Editor.SetUseSuggestionMode(false);
            VisualStudio.SendKeys.Send("TimeSpan.FromMin");
            VisualStudio.SendKeys.Send(VirtualKey.Enter, "(0d)", VirtualKey.Enter);
            VisualStudio.InteractiveWindow.WaitForReplOutput("[00:00:00]");
        }

        [WpfFact]
        public void VerifyCompletionListForLoadMembers()
        {
            using var temporaryTextFile = new TemporaryTextFile(
                "c.csx",
                "int x = 2; class Complex { public int goo() { return 4; } }");
            temporaryTextFile.Create();
            VisualStudio.InteractiveWindow.SubmitText(string.Format("#load \"{0}\"", temporaryTextFile.FullName));
            VisualStudio.InteractiveWindow.InvokeCompletionList();
            VisualStudio.InteractiveWindow.Verify.CompletionItemsExist("x", "Complex");
            VisualStudio.SendKeys.Send(VirtualKey.Escape);
        }
    }
}
