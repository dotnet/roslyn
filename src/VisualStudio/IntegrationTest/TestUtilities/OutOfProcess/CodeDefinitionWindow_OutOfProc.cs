// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class CodeDefinitionWindow_OutOfProc : OutOfProcComponent
    {
        private readonly CodeDefinitionWindow_InProc _inProc;

        public CodeDefinitionWindow_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _inProc = CreateInProcComponent<CodeDefinitionWindow_InProc>(visualStudioInstance);
        }

        /// <inheritdoc cref="CodeDefinitionWindow_InProc.GetCurrentLineText"/>
        public string GetCurrentLineText() => _inProc.GetCurrentLineText();
        /// <inheritdoc cref="CodeDefinitionWindow_InProc.GetText"/>
        public string GetText() => _inProc.GetText();

        /// <inheritdoc cref="CodeDefinitionWindow_InProc.Show"/>
        public void Show() => _inProc.Show();
    }
}
