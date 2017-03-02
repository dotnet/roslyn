// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.FindAllReferences;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class Shell_InProc : InProcComponent
    {
        public static Shell_InProc Create() => new Shell_InProc();

        public string GetActiveWindowCaption()
            => InvokeOnUIThread(() => GetDTE().ActiveWindow.Caption);

        public int GetHWnd()
            => GetDTE().MainWindow.HWnd;
    }
}
