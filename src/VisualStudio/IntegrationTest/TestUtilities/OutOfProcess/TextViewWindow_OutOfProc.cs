// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            WaitForQuickInfo();
            return _textViewWindowInProc.GetQuickInfo();
        }

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

        /// <summary>
        /// Invokes the lightbulb without waiting for diagnostics
        /// Compare to <see cref="InvokeCodeActionList"/>
        /// </summary>
        public void InvokeCodeActionListWithoutWaiting()
        {
            ShowLightBulb();
            WaitForLightBulbSession();
        }

        public void InvokeQuickInfo()
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_QuickInfo);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.QuickInfo);
        }
    }
}
