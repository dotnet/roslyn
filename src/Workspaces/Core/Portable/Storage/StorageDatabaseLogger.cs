// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Storage
{
    internal class StorageDatabaseLogger
    {
        private const string Kind = nameof(Kind);
        private const string Reason = nameof(Reason);

        private static readonly StorageDatabaseLogger Instance = new StorageDatabaseLogger();

        private Exception _reportedException;
        private string _reportedExceptionMessage;

        private readonly ConcurrentDictionary<Type, Exception> _set = new ConcurrentDictionary<Type, Exception>(concurrencyLevel: 2, capacity: 10);

        internal static void LogException(Exception ex)
        {
            Instance.LogExceptionWorker(ex);
        }

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
