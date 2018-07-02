// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class CSharpInteractiveWindow_InProc2 : InteractiveWindow_InProc2
    {
        private const string ViewCommand = "View.C#Interactive";
        private const string WindowTitle = "C# Interactive";

        public CSharpInteractiveWindow_InProc2(TestServices testServices)
            : base(testServices, ViewCommand, WindowTitle)
        {
        }

        protected override async Task<IInteractiveWindow> AcquireInteractiveWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = await GetComponentModelAsync();
            var vsInteractiveWindowProvider = componentModel.GetService<CSharpVsInteractiveWindowProvider>();
            var vsInteractiveWindow = vsInteractiveWindowProvider.Open(instanceId: 0, focus: true);

            return vsInteractiveWindow.InteractiveWindow;
        }
    }
}
