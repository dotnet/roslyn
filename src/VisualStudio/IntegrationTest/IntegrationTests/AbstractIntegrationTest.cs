// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CaptureTestName]
    public abstract class AbstractIntegrationTest : IDisposable
    {
        protected readonly VisualStudioInstanceContext VisualStudio;
        protected readonly VisualStudioWorkspace_OutOfProc VisualStudioWorkspaceOutOfProc;
        protected readonly TextViewWindow_OutOfProc TextViewWindow;

        protected AbstractIntegrationTest(
            VisualStudioInstanceFactory instanceFactory, 
            Func<VisualStudioInstanceContext, TextViewWindow_OutOfProc> textViewWindowBuilder)
        {
            VisualStudio = instanceFactory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);
            TextViewWindow = textViewWindowBuilder(VisualStudio);
            VisualStudioWorkspaceOutOfProc = VisualStudio.Instance.VisualStudioWorkspace;
        }

        public void Dispose()
            => VisualStudio.Dispose();

        public void VerifyCurrentTokenType(string tokenType)
        {
            WaitForAsyncOperations(
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification);
            var actualTokenTypes = TextViewWindow.GetCurrentClassifications();
            Assert.Equal(actualTokenTypes.Length, 1);
            Assert.Contains(tokenType, actualTokenTypes[0]);
            Assert.NotEqual("text", tokenType);
        }

        protected void Wait(double seconds)
        {
            var timeout = TimeSpan.FromMilliseconds(seconds * 1000);
            Thread.Sleep(timeout);
        }

        protected KeyPress KeyPress(VirtualKey virtualKey, ShiftState shiftState)
            => new KeyPress(virtualKey, shiftState);

        protected KeyPress Ctrl(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Ctrl);

        protected KeyPress Shift(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Shift);

        protected KeyPress Alt(VirtualKey virtualKey)
            => new KeyPress(virtualKey, ShiftState.Alt);

        protected void ExecuteCommand(string commandName, string argument = "")
            => VisualStudio.Instance.ExecuteCommand(commandName, argument);

        protected void InvokeCompletionList()
        {
            ExecuteCommand(WellKnownCommandNames.Edit_ListMembers);
            WaitForAsyncOperations(FeatureAttribute.CompletionSet);
        }

        protected void VerifyCompletionItemExists(params string[] expectedItems)
        {
            var completionItems = TextViewWindow.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        protected void VerifyCaretPosition(int expectedCaretPosition)
        {
            var position = TextViewWindow.GetCaretPosition();
            Assert.Equal(expectedCaretPosition, position);
        }

        protected void WaitForAsyncOperations(params string[] featuresToWaitFor)
            => VisualStudioWorkspaceOutOfProc.WaitForAsyncOperations(string.Join(";", featuresToWaitFor));
    }
}
