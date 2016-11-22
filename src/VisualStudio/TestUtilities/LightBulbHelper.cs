using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Test.Utilities;

namespace Roslyn.VisualStudio.Test.Utilities
{
    public static class LightBulbHelper
    {
        public static bool WaitForLightBulbSession(ILightBulbBroker broker, Microsoft.VisualStudio.Text.Editor.IWpfTextView view)
        {
            return Helper.Retry<bool>(() =>
            {
                if (broker.IsLightBulbSessionActive(view))
                {
                    return true;
                }

                // checking whether there is any suggested action is async up to editor layer and our waiter doesnt track up to that point.
                // so here, we have no other way than sleep (with timeout) to see LB is available.
                HostWaitHelper.PumpingWait(Task.Delay(TimeSpan.FromSeconds(1)));

                return broker.IsLightBulbSessionActive(view);
            }, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(20));
        }
    }
}
