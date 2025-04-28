// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// This class allows you to wait for a event to fire using signaling.
    /// </summary>
    public sealed class EventWaiter : IDisposable
    {
        private readonly ManualResetEvent _eventSignal = new ManualResetEvent(false);
        private Exception _capturedException;

        /// <summary>
        /// Returns the lambda given with method calls to this class inserted of the form:
        /// 
        /// try
        ///     execute given lambda.
        ///     
        /// catch
        ///     capture exception.
        ///     
        /// finally
        ///     signal async operation has completed.
        /// </summary>
        /// <typeparam name="T">Type of delegate to return.</typeparam>
        /// <param name="input">lambda or delegate expression.</param>
        /// <returns>The lambda given with method calls to this class inserted.</returns>
        public EventHandler<T> Wrap<T>(EventHandler<T> input)
        {
            return (sender, args) =>
            {
                try
                {
                    input(sender, args);
                }
                catch (Exception ex)
                {
                    _capturedException = ex;
                }
                finally
                {
                    _eventSignal.Set();
                }
            };
        }

        public Action<TEventArgs> Wrap<TEventArgs>(Action<TEventArgs> input)
        {
            return args =>
            {
                try
                {
                    input(args);
                }
                catch (Exception ex)
                {
                    _capturedException = ex;
                }
                finally
                {
                    _eventSignal.Set();
                }
            };
        }

        /// <summary>
        /// Use this method to block the test until the operation enclosed in the Wrap method completes
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public bool WaitForEventToFire(TimeSpan timeout)
        {
            var result = _eventSignal.WaitOne(timeout);
            _eventSignal.Reset();
            return result;
        }

        /// <summary>
        /// Use this method to block the test until the operation enclosed in the Wrap method completes
        /// </summary>
        /// <returns></returns>
        public void WaitForEventToFire()
        {
            _eventSignal.WaitOne();
            _eventSignal.Reset();
            return;
        }

        /// <summary>
        /// IDisposable Implementation.  Note that this is where we throw our captured exceptions.
        /// </summary>
        public void Dispose()
        {
            _eventSignal.Dispose();
            if (_capturedException != null)
            {
                throw _capturedException;
            }
        }
    }
}
