// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Host;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class IWaitIndicatorExtensions
    {
        public static WaitIndicatorResult Wait(this IWaitIndicator waitIndicator, string titleAndMessage, bool allowCancel, Action<IWaitContext> action)
            => waitIndicator.Wait(titleAndMessage, titleAndMessage, allowCancel, action);

        public static WaitIndicatorResult Wait(this IWaitIndicator waitIndicator, string titleAndMessage, bool allowCancel, bool showProgress, Action<IWaitContext> action)
            => waitIndicator.Wait(titleAndMessage, titleAndMessage, allowCancel, showProgress, action);
    }
}
