// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities
{
    public static class WaitHelper
    {
        public static void WaitForDispatchedOperationsToComplete(DispatcherPriority priority)
        {
            Action action = delegate { };
            new FrameworkElement().Dispatcher.Invoke(action, priority);
        }
    }
}