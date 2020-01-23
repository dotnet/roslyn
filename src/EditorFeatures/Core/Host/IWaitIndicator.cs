// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Host
{
    internal interface IWaitIndicator
    {
        /// <summary>
        /// Schedule the action on the caller's thread and wait for the task to complete.
        /// </summary>
        WaitIndicatorResult Wait(string title, string message, bool allowCancel, bool showProgress, Action<IWaitContext> action);
        IWaitContext StartWait(string title, string message, bool allowCancel, bool showProgress);
    }

    internal static class IWaitIndicatorExtensions
    {
        public static WaitIndicatorResult Wait(
            this IWaitIndicator waitIndicator, string title, string message, bool allowCancel, Action<IWaitContext> action)
        {
            return waitIndicator.Wait(title, message, allowCancel, showProgress: false, action: action);
        }
    }
}
