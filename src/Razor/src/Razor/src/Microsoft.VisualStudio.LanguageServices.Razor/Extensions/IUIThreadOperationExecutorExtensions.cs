// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.Extensions;

internal static class IUIThreadOperationExecutorExtensions
{
    public static T? Execute<T>(
        this IUIThreadOperationExecutor iUIThreadOperationExecutor,
        string title,
        string description,
        bool allowCancellation,
        bool showProgress,
        Func<CancellationToken, Task<T>> func,
        JoinableTaskFactory jtf)
    {
        T? obj = default;
        var result = iUIThreadOperationExecutor.Execute(title, description, allowCancellation, showProgress,
            (context) => jtf.Run(async () => obj = await func(context.UserCancellationToken)));

        if (result == UIThreadOperationStatus.Canceled)
        {
            return default;
        }

        return obj;
    }
}
