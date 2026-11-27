// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// A smaller helper that lets us invoke methods; but ensures they happen in the right AppDomain in the case of the .NET Framework build host,
/// where we need to work around some marshalling issues. In the .NET Framework case this lives inside of the MSBuild AppDomain, and in .NET Core
/// there is just no such thing so this is in the same AssemblyLoadContext as everything else.
/// </summary>
internal sealed class RpcMethodInvoker
#if NETFRAMEWORK
    : MarshalByRefObject // We need to let this live inside the AppDomain in the .NET Framework case
#endif
{
#pragma warning disable CA1822 // Mark members as static - this is being called across AppDomains, so must be instance
    public object? InvokeMethod(object rpcTarget, MethodInfo method, object?[] arguments, bool lastParameterIsCancellationToken)
#pragma warning restore CA1822
    {
        // Fill in the CancellationToken if the last parameter needs one; this we have to do inside the AppDomain since we can't marshal a CancellationToken
        // (even CancellationToken.None) across the AppDomain boundary.
        if (lastParameterIsCancellationToken)
            arguments[arguments.Length - 1] = CancellationToken.None;

        var result = method.Invoke(rpcTarget, arguments);

#if NETFRAMEWORK

        // If we're inside the AppDomain we create in the .NET Framework case, we can't return a task since those can't be marshalled, so we'll get the underlying value now.
        if (result is Task resultTask)
        {
            result = GetTaskResultAsync(resultTask, method).GetAwaiter().GetResult();
        }

#endif

        return result;
    }

    public static async Task<object?> GetTaskResultAsync(Task task, MethodInfo calledMethod)
    {
        await task.ConfigureAwait(false);

        // If it's actually a Task<T> then get the result; we're looking at the declared return type because in some cases a method might
        // return a Task<T> under the covers as a workaround for the lack of TaskCompletionSource on .NET Framework but we don't want to see
        // that workaround since the result isn't intended to be seen.
        if (calledMethod.ReturnType.IsConstructedGenericType)
        {
            return task.GetType().GetProperty("Result")!.GetValue(task);
        }
        else
        {
            // It's just a simple Task so no result to return
            return null;
        }
    }
}
