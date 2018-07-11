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

        public void ShowLightBulb()
            => _textViewWindowInProc.ShowLightBulb();

        public void WaitForLightBulbSession()
            => _textViewWindowInProc.WaitForLightBulbSession();

        public bool IsLightBulbSessionExpanded()
            => _textViewWindowInProc.IsLightBulbSessionExpanded();

        public string[] GetLightBulbActions()
            => _textViewWindowInProc.GetLightBulbActions();

        public void ApplyLightBulbAction(string action, FixAllScope? fixAllScope, bool blockUntilComplete = true)
            => _textViewWindowInProc.ApplyLightBulbAction(action, fixAllScope, blockUntilComplete);

        public void InvokeCodeActionList()
        {
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);

            ShowLightBulb();
            WaitForLightBulbSession();
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
        }
    }
}
