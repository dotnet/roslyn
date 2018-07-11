// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    /// <summary>
    /// Provides a means of interacting with the Visual Studio editor by remoting calls into Visual Studio.
    /// </summary>
    public class Editor_OutOfProc : TextViewWindow_OutOfProc
    {
        private readonly Editor_InProc _editorInProc;
        private readonly VisualStudioInstance _instance;

        internal Editor_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _editorInProc = (Editor_InProc)_textViewWindowInProc;
        }

        internal override TextViewWindow_InProc CreateInProcComponent(VisualStudioInstance visualStudioInstance)
            => CreateInProcComponent<Editor_InProc>(visualStudioInstance);

        public void Activate()
            => _editorInProc.Activate();

        public void SetText(string value)
            => _editorInProc.SetText(value);

        public string GetSelectedText()
            => _editorInProc.GetSelectedText();

        public void MoveCaret(int position)
            => _editorInProc.MoveCaret(position);

        public void InvokeNavigateTo(string text)
        {
            _instance.ExecuteCommand(WellKnownCommandNames.Edit_GoToAll);
            NavigateToSendKeys(text);
            _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.NavigateTo);
        }

        public void NavigateToSendKeys(string keys)
            => _editorInProc.SendKeysToNavigateTo(keys);

        public void WaitForActiveView(string viewName)
            => _editorInProc.WaitForActiveView(viewName);
    }
}
