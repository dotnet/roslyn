// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// This class allows you to wait for a event to fire using signaling.
    /// </summary>
    public sealed class EventWaiter : IDisposable
    {
        private ManualResetEvent eventSignal = new ManualResetEvent(false);
        private Exception capturedException;

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
                    this.capturedException = ex;
                }
                finally
                {
                    eventSignal.Set();
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
            var result =  eventSignal.WaitOne(timeout);
            eventSignal.Reset();
            return result;
        }

        /// <summary>
        /// Use this method to block the test until the operation enclosed in the Wrap method completes
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public void WaitForEventToFire()
        {
            eventSignal.WaitOne();
            eventSignal.Reset();
            return;
        }

        /// <summary>
        /// IDisposable Implementation.  Note that this is where we throw our captured exceptions.
        /// </summary>
        public void Dispose()
        {
            eventSignal.Dispose();
            if (this.capturedException != null)
            {
                throw this.capturedException;
            }
        }
    }
}
