// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IEnumerableExtensions
    {
        public static Task<IEnumerable<S>> SelectManyAsync<T, S>(
            this IEnumerable<T> sequence,
            Func<T, CancellationToken, Task<IEnumerable<S>>> selector, CancellationToken cancellationToken)
        {
            var whenAllTask = Task.WhenAll(sequence.Select(e => selector(e, cancellationToken)));

            return whenAllTask.SafeContinueWith(allResultsTask =>
                allResultsTask.Result.Flatten(),
                cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}
