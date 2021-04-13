// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Storage
{
    internal class StorageDatabaseLogger
    {
        private const string Kind = nameof(Kind);
        private const string Reason = nameof(Reason);

        private static readonly StorageDatabaseLogger Instance = new();

#pragma warning disable IDE0052 // Remove unread private members - hold onto last exception to make investigation easier
        private Exception? _reportedException;
        private string? _reportedExceptionMessage;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly ConcurrentDictionary<Type, Exception> _set = new(concurrencyLevel: 2, capacity: 10);

        internal static void LogException(Exception ex)
            => Instance.LogExceptionWorker(ex);

        private void LogExceptionWorker(Exception ex)
        {
            // hold onto last exception to make investigation easier
            _reportedException = ex;
            _reportedExceptionMessage = ex.ToString();

            // we already reported about this exception. also don't hold onto too many exceptions.
            if (_set.Count > 10 || !_set.TryAdd(ex.GetType(), ex))
            {
                return;
            }

            Logger.Log(FunctionId.StorageDatabase_Exceptions, KeyValueLogMessage.Create(m =>
            {
                // this is okay since it is our exception
                m[Kind] = ex.GetType().ToString();
                m[Reason] = ex.ToString();
            }));
        }
    }
}
