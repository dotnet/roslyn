// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SQLite
{
    internal partial class SQLitePersistentStorage
    {
        public override Task<Stream> ReadStreamAsync(string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SolutionData data = null;
            if (!_shutdownTokenSource.IsCancellationRequested)
            {
                // Ensure all pending writes are flushed to the DB so that we can locate them if asked to.
                FlushPendingWrites((isSolution, _2, _3) => isSolution);

                try
                {
                    data = CreateConnection().Find<SolutionData>(name);
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return Task.FromResult(GetStream(data?.Data));
        }

        public override Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_shutdownTokenSource.IsCancellationRequested)
            {
                var bytes = GetBytes(stream);

                AddWriteTask(con =>
                {
                    con.InsertOrReplace(
                        new SolutionData { Id = name, Data = bytes });
                });

                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }
    }
}