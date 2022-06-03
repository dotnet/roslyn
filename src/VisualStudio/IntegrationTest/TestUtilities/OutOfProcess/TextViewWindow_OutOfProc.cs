// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public abstract partial class TextViewWindow_OutOfProc : OutOfProcComponent
    {
        public Verifier<TextViewWindow_OutOfProc> Verify { get; }

        internal readonly TextViewWindow_InProc _textViewWindowInProc;
        private readonly VisualStudioInstance _instance;

        internal TextViewWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _textViewWindowInProc = CreateInProcComponent(visualStudioInstance);
            _instance = visualStudioInstance;
            Verify = new Verifier<TextViewWindow_OutOfProc>(this, visualStudioInstance);
        }

        internal abstract TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance);

        public int GetCaretPosition()
            => _textViewWindowInProc.GetCaretPosition();

        public int GetCaretColumn()
            => _textViewWindowInProc.GetCaretColumn();

        public string[] GetCompletionItems()
        {
            WaitForCompletionSet();
            return _textViewWindowInProc.GetCompletionItems();
        }

        public int GetVisibleColumnCount()
            => _textViewWindowInProc.GetVisibleColumnCount();

        public void PlaceCaret(
            string marker,
            int charsOffset = 0,
            int occurrence = 0,
            bool extendSelection = false,
            bool selectBlock = false)
            => _textViewWindowInProc.PlaceCaret(
                marker,
                charsOffset,
                occurrence,
                extendSelection,
                selectBlock);

        public string[] GetCurrentClassifications()
            => _textViewWindowInProc.GetCurrentClassifications();

        public string GetQuickInfo()
            => _textViewWindowInProc.GetQuickInfo();

        public void VerifyTags(string tagTypeName, int expectedCount)
            => _textViewWindowInProc.VerifyTags(tagTypeName, expectedCount);

        public void ShowLightBulb()
            => _textViewWindowInProc.ShowLightBulb();

        public void WaitForLightBulbSession()
            => _textViewWindowInProc.WaitForLightBulbSession();

        public bool IsLightBulbSessionExpanded()
            => _textViewWindowInProc.IsLightBulbSessionExpanded();

        public void DismissLightBulbSession()
            => _textViewWindowInProc.DismissLightBulbSession();

        public void DismissCompletionSessions()
        {
            WaitForCompletionSet();
            _textViewWindowInProc.DismissCompletionSessions();
        }

        public string[] GetLightBulbActions()
            => _textViewWindowInProc.GetLightBulbActions();

        public bool ApplyLightBulbAction(string action, FixAllScope? fixAllScope, bool blockUntilComplete = true)
            => _textViewWindowInProc.ApplyLightBulbAction(action, fixAllScope, blockUntilComplete);

        public void InvokeCompletionList()
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_ListMembers);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.CompletionSet);
        }

        public void InvokeCodeActionList()
        {
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.DiagnosticService);

            ShowLightBulb();
            WaitForLightBulbSession();
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.LightBulb);
        }

        public void InvokeQuickInfo()
            => _textViewWindowInProc.InvokeQuickInfo();
    }
}
