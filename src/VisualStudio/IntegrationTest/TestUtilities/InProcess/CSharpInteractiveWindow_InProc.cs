// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;

namespace Roslyn.VisualStudio.Test.Utilities.InProcess
{
    internal class CSharpInteractiveWindow_InProc : InteractiveWindow_InProc
    {
        private const string ViewCommand = "View.C#Interactive";
        private const string WindowTitle = "C# Interactive";

        private CSharpInteractiveWindow_InProc(): base(ViewCommand, WindowTitle) { }

        public static CSharpInteractiveWindow_InProc Create()
        {
            return new CSharpInteractiveWindow_InProc();
        }

        protected override IInteractiveWindow AcquireInteractiveWindow()
        {
            return InvokeOnUIThread(() =>
            {
                var componentModel = GetComponentModel();
                var vsInteractiveWindowProvider = componentModel.GetService<CSharpVsInteractiveWindowProvider>();
                var vsInteractiveWindow = vsInteractiveWindowProvider.Open(instanceId: 0, focus: true);

                return vsInteractiveWindow.InteractiveWindow;
            });
        }
    }
}
