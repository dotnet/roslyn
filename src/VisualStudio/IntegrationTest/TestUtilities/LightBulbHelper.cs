// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class LightBulbHelper
    {
        public static bool WaitForLightBulbSession(ILightBulbBroker broker, Microsoft.VisualStudio.Text.Editor.IWpfTextView view)
            => Helper.Retry(() => {
                if (broker.IsLightBulbSessionActive(view))
                {
                    return true;
                }

                // checking whether there is any suggested action is async up to editor layer and our waiter doesnt track up to that point.
                // so here, we have no other way than sleep (with timeout) to see LB is available.
                HostWaitHelper.PumpingWait(Task.Delay(TimeSpan.FromSeconds(1)));

                return broker.IsLightBulbSessionActive(view);
            }, TimeSpan.FromSeconds(0));
    }
}
