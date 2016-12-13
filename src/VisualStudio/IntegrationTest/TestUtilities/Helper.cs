using System;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class Helper
    {
        /// <summary>
        /// This method will retry the action represented by the 'action' argument, up to 'timeout'
        /// milliseconds, waiting 'delay' milliseconds after each retry. If a given retry returns a value 
        /// other than default(T), this value is returned.
        /// </summary>
        /// <param name="action">the action to retry</param>
        /// <param name="delay">the amount of time to wait between retries in milliseconds</param>
        /// <param name="timeout">the max amount of time to spend retrying in milliseconds</param>
        /// <typeparam name="T">type of return value</typeparam>
        /// <returns>the return value of 'action'</returns>
        public static T Retry<T>(Func<T> action, int delay, int timeout)
            => Retry(action, TimeSpan.FromMilliseconds(delay), TimeSpan.FromMilliseconds(timeout));

        /// <summary>
        /// This method will retry the action represented by the 'action' argument, up to 'timeout'
        /// milliseconds, waiting 'delay' milliseconds after each retry and will swallow all exceptions. 
        /// If a given retry returns a value other than default(T), this value is returned.
        /// </summary>
        /// <param name="action">the action to retry</param>
        /// <param name="delay">the amount of time to wait between retries in milliseconds</param>
        /// <param name="timeout">the max amount of time to spend retrying in milliseconds</param>
        /// <typeparam name="T">type of return value</typeparam>
        /// <returns>the return value of 'action'</returns>
        public static T RetryIgnoringExceptions<T>(Func<T> action, int delay, int timeout)
            => RetryIgnoringExceptions(action, TimeSpan.FromMilliseconds(delay), TimeSpan.FromMilliseconds(timeout));

        /// <summary>
        /// This method will retry the action represented by the 'action' argument, up to 'timeout',
        /// waiting for 'delay' time after each retry. If a given retry returns a value 
        /// other than default(T), this value is returned.
        /// </summary>
        /// <param name="action">the action to retry</param>
        /// <param name="delay">the amount of time to wait between retries</param>
        /// <param name="timeout">the max amount of time to spend retrying</param>
        /// <typeparam name="T">type of return value</typeparam>
        /// <returns>the return value of 'action'</returns>
        public static T Retry<T>(Func<T> action, TimeSpan delay, TimeSpan timeout)
        {
            var beginTime = DateTime.UtcNow;
            var retval = default(T);

            do
            {
                try
                {
                    retval = action();
                }
                catch (COMException)
                {
                    // Devenv can throw COMExceptions if it's busy when we make DTE calls.
                }

                if (!Equals(default(T), retval))
                {
                    return retval;
                }
                else
                {
                    System.Threading.Thread.Sleep(delay);
                }
            }
            while (beginTime.Add(timeout) > DateTime.UtcNow);

            return retval;
        }

        /// <summary>
        /// This method will retry the action represented by the 'action' argument, up to 'timeout'
        /// milliseconds, waiting 'delay' milliseconds after each retry and will swallow all exceptions. 
        /// If a given retry returns a value other than default(T), this value is returned.
        /// </summary>
        /// <param name="action">the action to retry</param>
        /// <param name="delay">the amount of time to wait between retries in milliseconds</param>
        /// <param name="timeout">the max amount of time to spend retrying in milliseconds</param>
        /// <typeparam name="T">type of return value</typeparam>
        /// <returns>the return value of 'action'</returns>
        public static T RetryIgnoringExceptions<T>(Func<T> action, TimeSpan delay, TimeSpan timeout)
        {
            var beginTime = DateTime.UtcNow;
            var retval = default(T);
            Exception e = null;

            do
            {
                try
                {
                    retval = action();
                }
                catch (Exception ex)
                {
                    e = ex;
                }

                if (!Equals(default(T), retval))
                {
                    return retval;
                }
                else
                {
                    System.Threading.Thread.Sleep(delay);
                }
            }
            while (beginTime.Add(timeout) > DateTime.UtcNow);

            if (Equals(default(T), retval))
            {
                if (e == null)
                {
                    throw new Exception($"Function never assigned a value after {timeout.Minutes} minutes and {timeout.Seconds} seconds and no exceptions were thrown");
                }
                else
                {
                    throw new Exception($"Function never assigned a value after {timeout.Minutes} minutes and {timeout.Seconds} seconds.  Last observed excepton was:{e.Message}{e.StackTrace}");
                }
            }

            return retval;
        }
    }
}