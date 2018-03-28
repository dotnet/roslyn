// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
