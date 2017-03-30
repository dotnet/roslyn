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
            Project project, string name, CancellationToken cancellationToken)
        {
            ProjectData projectData = null;
            if (TryGetProjectDataId(project, name, out var dataId))
            {
                try
                {
                    projectData = CreateConnection().Find<ProjectData>(dataId);
                }
                catch (Exception ex)
                {
                    StorageDatabaseLogger.LogException(ex);
                }
            }

            return Task.FromResult(GetStream(projectData?.Data));
        }

        public override Task<bool> WriteStreamAsync(
            Project project, string name, Stream stream, CancellationToken cancellationToken)
        {
            if (TryGetProjectDataId(project, name, out var dataId))
            {
                var bytes = GetBytes(stream);

                AddWriteTask(con =>
                {
                    con.InsertOrReplace(
                        new ProjectData { Id = dataId, Data = bytes });
                });

                return SpecializedTasks.True;
            }

            return SpecializedTasks.False;
        }
    }
}