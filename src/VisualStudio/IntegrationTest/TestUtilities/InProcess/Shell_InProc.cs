// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Shell_InProc : InProcComponent
    {
        public static Shell_InProc Create() => new Shell_InProc();

        public IntPtr GetHWnd()
            => GetDTE().MainWindow.HWnd;

        public bool IsUIContextActive(Guid context)
        {
            return UIContext.FromUIContextGuid(context).IsActive;
        }
    }
}
