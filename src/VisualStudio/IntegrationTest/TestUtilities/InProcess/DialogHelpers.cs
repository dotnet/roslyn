using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    public static class DialogHelpers
    {
        public static AutomationElement FindDialog(int visualStudioHWnd, string dialogAutomationName, bool isOpen)
        {
            return Retry(
                () => FindDialogWorker(visualStudioHWnd, dialogAutomationName),
                stoppingCondition: automationElement => isOpen ? automationElement != null : automationElement == null,
                delay: TimeSpan.FromMilliseconds(250));
        }

        private static AutomationElement FindDialogWorker(int visualStudioHWnd, string dialogAutomationName)
        {
            var vsAutomationElement = AutomationElement.FromHandle(new IntPtr(visualStudioHWnd));

            Condition elementCondition = new AndCondition(
                new PropertyCondition(AutomationElement.AutomationIdProperty, dialogAutomationName),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

            return vsAutomationElement.FindFirst(TreeScope.Descendants, elementCondition);
        }

        private static T Retry<T>(Func<T> action, Func<T, bool> stoppingCondition, TimeSpan delay)
        {
            DateTime beginTime = DateTime.UtcNow;
            T retval = default(T);

            do
            {
                try
                {
                    retval = action();
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.

                    Thread.Sleep(delay);
                    continue;
                }

                if (stoppingCondition(retval))
                {
                    return retval;
                }
                else
                {
                    Thread.Sleep(delay);
                }
            }
            while (true);
        }
    }
}
