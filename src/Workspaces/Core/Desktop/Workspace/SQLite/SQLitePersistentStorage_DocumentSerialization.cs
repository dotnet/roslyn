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
        public override Task<Stream> ReadStreamAsync(
            Document document, string name, CancellationToken cancellationToken)
        {
            if (!TryGetDocumentDataId(document, name, out var dataId))
            {
                return SpecializedTasks.Default<Stream>();
            }

            DocumentData documentData = null;
            try
            {
                documentData = CreateConnection().Find<DocumentData>(dataId);
            }
            catch (Exception ex)
            {
                StorageDatabaseLogger.LogException(ex);
            }

            return Task.FromResult(GetStream(documentData?.Data));
        }

        public override Task<bool> WriteStreamAsync(
            Document document, string name, Stream stream, CancellationToken cancellationToken)
        {
            if (TryGetDocumentDataId(document, name, out var dataId))
            {
                var bytes = GetBytes(stream);

                AddWriteTask(con =>
                {
                    con.InsertOrReplace(
                        new DocumentData { Id = dataId, Data = bytes });
                });

                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }
    }
}