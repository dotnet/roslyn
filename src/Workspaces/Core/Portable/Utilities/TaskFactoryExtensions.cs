// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Roslyn.Utilities
{
    // TODO: revisit https://github.com/dotnet/roslyn/issues/39222

    [SuppressMessage("ApiDesign", "CA1068", Justification = "Matching TPL Signatures")]
    internal static partial class TaskFactoryExtensions
    {
        public static Task SafeStartNew(this TaskFactory factory, Action action, CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            void wrapped()
            {
                try
                {
                    action();
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            return factory.StartNew(wrapped, cancellationToken, TaskCreationOptions.None, scheduler);
        }

        public static Task<TResult> SafeStartNew<TResult>(this TaskFactory factory, Func<TResult> func, CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            TResult wrapped()
            {
                try
                {
                    return func();
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            return factory.StartNew(wrapped, cancellationToken, TaskCreationOptions.None,, scheduler);
        }

        public static Task SafeStartNewFromAsync(this TaskFactory factory, Func<Task> actionAsync, CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            // The one and only place we can call StartNew<>().
            var task = factory.StartNew(actionAsync, cancellationToken, TaskCreationOptions.None, scheduler).Unwrap();
            TaskExtensions.ReportFatalError(task, actionAsync);
            return task;
        }

        public static Task<TResult> SafeStartNewFromAsync<TResult>(this TaskFactory factory, Func<Task<TResult>> funcAsync, CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            // The one and only place we can call StartNew<>().
            var task = factory.StartNew(funcAsync, cancellationToken, TaskCreationOptions.None, scheduler).Unwrap();
            TaskExtensions.ReportFatalError(task, funcAsync);
            return task;
        }
    }
}
