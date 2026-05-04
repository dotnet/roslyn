// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities;

public static class WaitHelper
{
    public static void WaitForDispatchedOperationsToComplete(DispatcherPriority priority)
    {
        Action action = delegate { };
#pragma warning disable VSTHRD001 // Avoid legacy thread switching APIs
        new FrameworkElement().Dispatcher.Invoke(action, priority);
#pragma warning restore VSTHRD001 // Avoid legacy thread switching APIs
    }
}
